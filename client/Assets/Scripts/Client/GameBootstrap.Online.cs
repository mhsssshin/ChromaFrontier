using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// PvP 한 판의 동기화 상태. 모든 기기가 같은 seed 로 판을 만들고 수 목록(Log, 번호 순)을 차례대로 재생한다.
    /// 내 수는 바로 두고 보낸다(번호 = 지금까지 둔 수). 이미 둔 번호의 수가 다르게 오면(시간 초과 대리 수와 겹침 등)
    /// 처음부터 다시 재생한다. 규칙에 어긋난 수도 번호는 차지하므로 모든 기기가 똑같이 건너뛴다.
    /// </summary>
    public sealed class PvpSession
    {
        public RoomInfo Room;
        public int MySeat;
        public PvpMap Map;
        public string[] Decks;
        public readonly Dictionary<int, string> Log = new();
        public int Applied;                 // 판에 둔(또는 건너뛴) 수의 개수
        public float TurnStarted;           // 지금 차례가 시작된 시각 (실제 시간)
        public int TurnKey = -1;
        public int ProxySent = -1;          // 나간 사람 대신 보낸 수의 번호
        public Side ForfeitWinner = Side.None; // 한 팀이 모두 나가 판이 끝났다
        public bool Recorded;

        public int Seats => Room.size * 2;
        public bool Left(int seat) => seat < Room.seats.Length && Room.seats[seat].left;
        public string NameOf(int seat) => seat < Room.seats.Length ? Room.seats[seat].name : "?";
    }

    /// <summary>로그인 화면, 계정, PvP 로비·방·판 (온라인 기능은 Online 이 net.js 와 주고받는다).</summary>
    public sealed partial class GameBootstrap
    {
        public PvpSession Pvp { get; private set; }

        /// <summary>내 팀 (PvP 2팀이면 Ai). 화면의 '나'.</summary>
        public Side Me => Pvp != null ? Game.SideOfSeat(Pvp.MySeat) : Side.Player;

        /// <summary>내 손패가 든 덱 (2:2 에서는 같은 팀이라도 짝의 덱과 따로).</summary>
        public Deck MyDeck => Pvp != null ? Game.DeckOfSeat(Pvp.MySeat) : Game.DeckOf(Side.Player);

        /// <summary>판이 끝났다 (PvP 는 한 팀이 모두 나간 경우도).</summary>
        public bool BattleOver => Game.IsOver || (Pvp != null && Pvp.ForfeitWinner != Side.None);

        public string Toast { get; private set; } = "";
        public float ToastUntil { get; private set; }

        public void ShowToast(string text, float seconds = 3f)
        {
            Toast = text;
            ToastUntil = Time.time + seconds;
        }

        private int _actorSeat; // 방금 둔 자리 (메시지 이름)

        private string WhoName(Side actor)
        {
            if (Pvp == null) return actor == Side.Player ? "나" : "AI";
            if (_actorSeat == Pvp.MySeat) return "나";
            var name = Pvp.NameOf(_actorSeat);
            return Game.SideOfSeat(_actorSeat) == Me ? "짝 " + name : name;
        }

        /// <summary>유닛을 둔다 (PvP 면 보내기까지). 둘 수 없는 수면 false.</summary>
        private bool PlayUnit(int hand, Cell at, Cell? dir)
        {
            if (Pvp != null) return SendPvp(PvpMove.Play(Pvp.MySeat, hand, at, dir ?? Cell.Orthogonal[0]));
            Game.Play(hand, at, dir);
            return true;
        }

        private bool CastSkill(int hand, Cell a, Cell b)
        {
            if (Pvp != null) return SendPvp(PvpMove.Cast(Pvp.MySeat, hand, a, b));
            Game.Cast(hand, a, b);
            return true;
        }

        private bool SendPvp(PvpMove m)
        {
            var enc = m.Encode();
            if (!m.TryApply(Game))
            {
                Message = "지금은 둘 수 없는 수예요.";
                return false;
            }
            _actorSeat = Pvp.MySeat;
            Pvp.Log[Pvp.Applied] = enc;
            Online.SendMove(Pvp.Applied, enc, false);
            Pvp.Applied++;
            return true;
        }

        // ---------------------------------------------------------------- 로그인

        public bool LoginWaitingSignOut { get; private set; }

        /// <summary>로그인 화면을 거쳐야 하는가: 웹이고, 오프라인으로 하기로 하지 않았고, 아직 로그인·동기화 전.</summary>
        private bool NeedLogin => Online.Available && !Online.OfflineChosen && !(Online.SignedIn && Online.Synced);

        public void ShowLogin()
        {
            if (Mode != AppScreen.Splash) BuildSplashBoard();
            Mode = AppScreen.Login;
            Panel = LobbyPanel.None;
            Game = null;
            Pvp = null;
            Palette.Viewer = Side.Player;
        }

        /// <summary>스플래시·로그인 다음: 처음이면 튜토리얼, 아니면 대기실.</summary>
        private void EnterGame()
        {
            if (!Progress.TutorialDone) StartTutorial(Progress.TutorialLesson);
            else ShowLobby(Mathf.Min(Progress.Unlocked - 1, StageCatalog.Count - 1));
        }

        public void PlayOffline()
        {
            Online.OfflineChosen = true;
            EnterGame();
        }

        public void SignOut()
        {
            Online.SignOut();
            LoginWaitingSignOut = true;
            ShowLogin();
        }

        /// <summary>오프라인으로 하던 중 로그인하러 간다.</summary>
        public void GoLogin()
        {
            Online.OfflineChosen = false;
            ShowLogin();
        }

        private void UpdateLogin()
        {
            if (LoginWaitingSignOut && !Online.SignedIn) LoginWaitingSignOut = false;
            if (!LoginWaitingSignOut && Online.SignedIn && Online.Synced) EnterGame();
        }

        /// <summary>클라우드 기록을 받았다: 보이는 화면을 새 기록으로 다시 그린다.</summary>
        private void OnReloaded()
        {
            if (Mode == AppScreen.Lobby) ShowLobby(Mathf.Min(Progress.Unlocked - 1, StageCatalog.Count - 1));
        }

        // ---------------------------------------------------------------- PvP 로비·방

        public void ShowPvpLobby()
        {
            if (!Online.Available)
            {
                ShowToast("대전은 웹 버전에서 로그인해야 할 수 있어요.");
                return;
            }
            Mode = AppScreen.PvpLobby;
            Panel = LobbyPanel.None;
            EndCine();
            Game = null;
            Pvp = null;
            Palette.Viewer = Side.Player;
            _view?.Destroy();
            _view = null;
            _fx?.Destroy();
            _fx = null;
            Online.WatchRooms(true);
        }

        public static string MyDeckCodes => PvpCatalog.SkillCodes(Progress.Cards);

        public void CreateRoom(int teamSize)
        {
            if (!Online.SignedIn) return;
            Online.CreateRoom($"{Online.Name}의 방", teamSize, PvpCatalog.MapsFor(teamSize)[0].Id, MyDeckCodes);
        }

        public void JoinRoom(RoomInfo r)
        {
            if (Online.SignedIn) Online.JoinRoom(r.id, MyDeckCodes);
        }

        private string _roomMap = "";

        private void ShowPvpRoom()
        {
            Mode = AppScreen.PvpRoom;
            Online.WatchRooms(false);
            _roomMap = "";
            UpdateRoomPreview();
        }

        private void UpdateRoomPreview()
        {
            var r = Online.Room;
            if (r == null || (r.map == _roomMap && _view != null)) return;
            _roomMap = r.map;
            _view?.Destroy();
            _previewBoard = Board.Parse(PvpCatalog.Find(r.size, r.map).Map);
            _view = new BoardView(transform, _previewBoard.Width, _previewBoard.Height);
            SnapCamera();
        }

        public bool IsHost => Online.Room != null && Online.Room.host == Online.Uid;

        public bool CanStartRoom
        {
            get
            {
                var r = Online.Room;
                if (r == null || !IsHost) return false;
                foreach (var s in r.seats)
                    if (s.Empty || (!s.ready && s.uid != r.host)) return false;
                return true;
            }
        }

        public void CycleRoomMap()
        {
            var r = Online.Room;
            if (r == null || !IsHost) return;
            var maps = PvpCatalog.MapsFor(r.size);
            var i = System.Array.FindIndex(maps, m => m.Id == r.map);
            Online.SetMap(maps[(i + 1) % maps.Length].Id);
        }

        public void LeaveRoom()
        {
            Online.LeaveRoom();
            ShowPvpLobby();
        }

        private void UpdatePvpScreens()
        {
            if (Mode == AppScreen.PvpLobby)
            {
                if (Online.Room != null) ShowPvpRoom();
                return;
            }
            // 방
            if (Online.Room == null)
            {
                var gone = Online.TakeRoomGone();
                ShowPvpLobby();
                if (gone) ShowToast("방이 닫혔어요 (방장이 나갔어요).");
                return;
            }
            if (Online.Room.status != "wait")
            {
                StartPvpBattle(Online.Room);
                return;
            }
            Palette.Viewer = Game.SideOfSeat(Mathf.Max(0, Online.MySeat)); // 미리보기도 내 팀 시작 땅이 파랑
            UpdateRoomPreview();
        }

        // ---------------------------------------------------------------- PvP 판

        private void StartPvpBattle(RoomInfo room)
        {
            var map = PvpCatalog.Find(room.size, room.map);
            var decks = new string[room.size * 2];
            for (var i = 0; i < decks.Length; i++) decks[i] = i < room.seats.Length ? room.seats[i].deck : "";
            var pvp = new PvpSession { Room = room, MySeat = Online.MySeat, Map = map, Decks = decks };
            BeginBattle(null, PvpCatalog.CreateGame(map, decks, room.seed));
            Pvp = pvp;
            Palette.Viewer = Me;
            Pvp.TurnStarted = Time.unscaledTime;
            var mates = new List<string>();
            for (var i = 0; i < pvp.Seats; i++)
                if (i != pvp.MySeat && Game.SideOfSeat(i) == Me) mates.Add(pvp.NameOf(i));
            Message = $"{map.Name} — {map.Note}\n" + (Game.SideOfSeat(pvp.MySeat) == Side.Player ? "우리 팀이 먼저 둬요." : "상대 팀이 먼저 둬요.")
                + (mates.Count > 0 ? $" 짝: {mates[0]}" : "");
        }

        /// <summary>처음부터 다시 재생한다 (이미 둔 번호의 수가 바뀌었을 때). 연출 없이.</summary>
        private void ResyncPvp()
        {
            var p = Pvp;
            Game = PvpCatalog.CreateGame(p.Map, p.Decks, p.Room.seed);
            p.Applied = 0;
            while (p.Log.TryGetValue(p.Applied, out var s))
            {
                if (PvpMove.TryDecode(s, out var m)) m.TryApply(Game);
                p.Applied++;
            }
            Hand.Reset();
            Deselect();
            EndCine();
            Message = "판을 다시 맞췄어요.";
        }

        private void UpdatePvp()
        {
            var p = Pvp;
            if (Online.Room != null && Online.Room.id == p.Room.id) p.Room = Online.Room;

            // 받은 수
            var resync = false;
            while (Online.Moves.Count > 0)
            {
                var kv = Online.Moves.Dequeue();
                if (p.Log.TryGetValue(kv.Key, out var old) && old == kv.Value) continue;
                if (kv.Key < p.Applied) resync = true;
                p.Log[kv.Key] = kv.Value;
            }
            if (resync) ResyncPvp();

            // 다음 수를 둔다 (상대 수는 연출이 보이게 조금씩 띄워서)
            if (!BattleOver && _aiTimer <= 0f && p.Log.TryGetValue(p.Applied, out var next))
            {
                p.Applied++;
                var actor = Game.Current;
                var seat = Game.CurrentSeat;
                if (PvpMove.TryDecode(next, out var m) && m.TryApply(Game))
                {
                    _actorSeat = seat;
                    if (seat == p.MySeat) Deselect();
                    AfterMove(actor, m.At, m.Kind == PvpMoveKind.Cast ? m.Extra : default);
                }
            }
            if (_aiTimer > 0f) _aiTimer -= Time.deltaTime;

            // 한 팀이 모두 나가면 남은 팀이 이긴다
            if (!BattleOver)
            {
                for (var team = 0; team < 2; team++)
                {
                    var all = true;
                    for (var i = team; i < p.Seats; i += 2) all &= p.Left(i);
                    if (all) p.ForfeitWinner = Game.SideOfSeat(team + 1);
                }
            }

            // 차례 시간
            var key = Game.TurnsTaken * 8 + Game.CurrentSeat;
            if (key != p.TurnKey)
            {
                p.TurnKey = key;
                p.TurnStarted = Time.unscaledTime;
            }
            var waited = Time.unscaledTime - p.TurnStarted;
            var caughtUp = !p.Log.ContainsKey(p.Applied);
            if (!BattleOver && caughtUp)
            {
                if (Game.CurrentSeat == p.MySeat && waited > PvpCatalog.TurnSeconds && ActiveTip == null)
                {
                    Deselect();
                    _actorSeat = p.MySeat;
                    if (SendPvp(PvpMove.Pass(p.MySeat)))
                    {
                        AfterMove(Me, default, default);
                        Message = "시간 초과 — 패 버리기로 차례를 넘겼어요";
                    }
                }
                else if (Game.CurrentSeat != p.MySeat && p.ProxySent != p.Applied && ProxyWriter(p)
                         && (p.Left(Game.CurrentSeat) ? waited > 1.5f : waited > PvpCatalog.TurnSeconds + 20f))
                {
                    // 나간 사람(또는 응답 없는 사람)의 차례는 남은 사람 중 가장 앞 자리가 패 버리기로 넘긴다.
                    // 그 번호가 비어 있을 때만 쓴다 (본인이 늦게 보낸 수와 겹치면 먼저 들어간 쪽이 남는다).
                    p.ProxySent = p.Applied;
                    Online.SendMove(p.Applied, PvpMove.Pass(Game.CurrentSeat).Encode(), true);
                }
            }

            // 결과
            if (BattleOver && !p.Recorded)
            {
                p.Recorded = true;
                var winner = p.ForfeitWinner != Side.None ? p.ForfeitWinner : Game.Winner;
                LastGems = Progress.RecordPvp(winner, Me);
                if (IsHost) Online.Finish();
                Deselect();
            }
        }

        /// <summary>대신 넘기는 수를 쓰는 사람: 나가지 않은 자리 중 가장 앞.</summary>
        private static bool ProxyWriter(PvpSession p)
        {
            for (var i = 0; i < p.Seats; i++)
                if (!p.Left(i)) return i == p.MySeat;
            return false;
        }

        /// <summary>판 중에 나간다 (진 것으로 치고 보상 없음). 판이 끝났으면 그냥 나간다.</summary>
        public void LeavePvp()
        {
            Online.LeaveRoom();
            Pvp = null;
            Palette.Viewer = Side.Player;
            ShowPvpLobby();
        }

        public float PvpTimeLeft => Pvp == null ? 0f : Mathf.Max(0f, PvpCatalog.TurnSeconds - (Time.unscaledTime - Pvp.TurnStarted));
    }
}
