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
        public float MyTurnAt = -99f;        // 내 차례가 시작된 시각 (가운데 알림)
        public string[] SeenEmote;           // 자리마다 마지막으로 본 이모티콘 값 (처음 값은 띄우지 않음)
        public readonly List<EmoteShow> Emotes = new(); // 지금 떠 있는 이모티콘
        public int[] Claim;                  // 2:2: 칸마다 마지막으로 차지한 자리 (-1 = 모름) — 그 사람 재질로 그린다
        public int BotFor = -1;              // 봇 생각 시간을 잡아 둔 수 번호
        public float BotAt;                  // 봇이 수를 낼 시각 (실제 시간)
        public readonly Rng BotRng = new(System.Environment.TickCount);
        public readonly List<EmoteShow> BotEmotes = new(); // 봇이 보낼 이모티콘 (At = 보낼 시각)
        public float[] BotEmotedAt;          // 자리마다 봇이 마지막으로 이모티콘을 보낸 시각
        public bool IsBot(int seat) => seat < Room.seats.Length && Room.seats[seat].IsBot;
        public PvpBot BotOf(int seat) => seat < Room.seats.Length ? PvpBots.Find(Room.seats[seat].uid) : null;
        public string SkinOf(int seat) => seat < Room.seats.Length && !string.IsNullOrEmpty(Room.seats[seat].skin) ? Room.seats[seat].skin : TileSkins.Default;

        public int Seats => Room.size * 2;
        public bool Left(int seat) => seat < Room.seats.Length && Room.seats[seat].left;
        public string NameOf(int seat) => seat < Room.seats.Length ? Room.seats[seat].name : "?";
    }

    /// <summary>대전 이모티콘 하나: 누가(자리), 무엇을(번호), 언제(실제 시간).</summary>
    public readonly struct EmoteShow
    {
        public readonly int Seat, Index;
        public readonly float At;
        public EmoteShow(int seat, int index, float at) { Seat = seat; Index = index; At = at; }
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
            if (!ApplyPvpMove(m))
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
            else ShowHome();
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
            if (Mode == AppScreen.Lobby) ShowSingle();
            else if (Mode == AppScreen.Home) ShowHome();
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
            CreatingRoom = false;
            MatchingSince = -1f;
            Online.WatchRooms(true);
        }

        public static string MyDeckCodes => PvpCatalog.SkillCodes(Progress.Cards);

        // 방 만들기 창 (로비 위에 뜬다)
        public bool CreatingRoom { get; set; }
        public int NewRoomSize { get; set; } = 1;
        public bool NewRoomLocked { get; set; }
        public string NewRoomTitle { get; private set; } = "";
        public string NewRoomPassword { get; private set; } = "";
        public string DefaultRoomTitle => $"{Online.Name}의 방";

        /// <summary>방 목록 검색어 (방 제목·사람 이름). 비번방도 찾을 수 있다.</summary>
        public string RoomSearch { get; private set; } = "";
        public int RoomPage { get; set; }

        /// <summary>빠른 대전을 눌러 방을 찾는 중 (방에 들어가면 끝).</summary>
        public float MatchingSince { get; private set; } = -1f;

        /// <summary>내가 만든 비번방의 비밀번호 (방 화면에 보여 줘서 친구에게 알려 줄 수 있게).</summary>
        public string MyRoomPassword { get; private set; } = "";

        public void OpenCreateRoom()
        {
            CreatingRoom = true;
            NewRoomTitle = "";
            NewRoomPassword = "";
        }

        public void AskRoomTitle() => Online.Ask("title", "방 제목 (24자까지)", NewRoomTitle == "" ? DefaultRoomTitle : NewRoomTitle);
        public void AskRoomPassword() => Online.Ask("password", "비밀번호 (12자까지)", NewRoomPassword);
        public void AskRoomSearch() => Online.Ask("search", "찾을 방 제목이나 이름", RoomSearch);

        public void ClearRoomSearch()
        {
            RoomSearch = "";
            RoomPage = 0;
        }

        private void OnAnswer(string key, string text)
        {
            switch (key)
            {
                case "title": NewRoomTitle = text; break;
                case "password": NewRoomPassword = text.Length > 12 ? text.Substring(0, 12) : text; break;
                case "search":
                    RoomSearch = text;
                    RoomPage = 0;
                    break;
            }
        }

        public bool CanCreateRoom => !NewRoomLocked || NewRoomPassword != "";

        public void CreateRoom()
        {
            if (!Online.SignedIn || !CanCreateRoom) return;
            var title = NewRoomTitle == "" ? DefaultRoomTitle : NewRoomTitle;
            var pw = NewRoomLocked ? NewRoomPassword : "";
            MyRoomPassword = pw;
            Online.CreateRoom(title, NewRoomSize, PvpCatalog.MapsFor(NewRoomSize)[0].Id, MyDeckCodes, Progress.SeatSkin, pw);
            CreatingRoom = false;
        }

        /// <summary>빠른 대전: 자리가 빈 공개방에 들어가고, 없으면 빠른 대전 방을 만든다. 사람이 안 오면 봇이 채운다.</summary>
        public void QuickMatch(int teamSize)
        {
            if (!Online.SignedIn || MatchingSince >= 0f) return;
            MatchingSince = Time.unscaledTime;
            MyRoomPassword = "";
            var maps = PvpCatalog.MapsFor(teamSize);
            Online.QuickJoin(teamSize, MyDeckCodes, Progress.SeatSkin, maps[Random.Range(0, maps.Length)].Id);
        }

        public void JoinRoom(RoomInfo r)
        {
            if (!Online.SignedIn) return;
            MyRoomPassword = "";
            Online.JoinRoom(r.id, MyDeckCodes, Progress.SeatSkin);
        }

        /// <summary>검색어에 맞는 방 (제목 또는 자리에 앉은 사람 이름). 검색어가 없으면 전부.</summary>
        public List<RoomInfo> VisibleRooms
        {
            get
            {
                var list = new List<RoomInfo>();
                var q = RoomSearch.Trim();
                foreach (var r in Online.Rooms)
                {
                    if (q == "" || r.title.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0) list.Add(r);
                    else
                        foreach (var s in r.seats)
                            if (!s.Empty && s.name.IndexOf(q, System.StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                list.Add(r);
                                break;
                            }
                }
                return list;
            }
        }

        private string _roomMap = "";

        private void ShowPvpRoom()
        {
            Mode = AppScreen.PvpRoom;
            Online.WatchRooms(false);
            CreatingRoom = false;
            MatchingSince = -1f;
            _roomMap = "";
            _botJoinAt = _autoStartAt = -1f;
            _botReadyAt.Clear();
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
                else if (MatchingSince >= 0f && (Online.ErrorAt > MatchingSince || Time.unscaledTime - MatchingSince > 15f)) MatchingSince = -1f;
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
            UpdateRoomBots(Online.Room);
            Palette.Viewer = Game.SideOfSeat(Mathf.Max(0, Online.MySeat)); // 미리보기도 내 팀 시작 땅이 파랑
            UpdateRoomPreview();
            RoomSkins(Online.Room, Mathf.Max(0, Online.MySeat), out var mineSkin, out var foeSkin);
            BoardView.UseSkins(mineSkin, foeSkin);
        }

        // ---------------------------------------------------------------- 빠른 대전 방: 봇 채우기·자동 시작

        private float _botJoinAt = -1f, _autoStartAt = -1f;
        private readonly Dictionary<string, float> _botReadyAt = new();

        /// <summary>빠른 대전 방이면 저절로 시작한다 (준비 버튼 없음).</summary>
        public bool QuickRoom => Online.Room != null && Online.Room.quick;

        /// <summary>
        /// 빠른 대전 방의 방장 기기: 사람이 안 오면 빈 자리에 봇을 하나씩 앉힌다(처음은 6~13초, 다음은 2~7초 뒤 — 사람이 들어오는 것처럼).
        /// 봇은 1~4초 뒤 준비하고, 모두 준비되면 잠시 뒤 시작한다.
        /// </summary>
        private void UpdateRoomBots(RoomInfo r)
        {
            if (!IsHost || !r.quick || r.status != "wait") return;
            var now = Time.unscaledTime;
            var empty = 0;
            var bots = 0;
            foreach (var s in r.seats)
            {
                if (s.Empty) empty++;
                else if (s.IsBot) bots++;
            }
            if (empty == 0) _botJoinAt = -1f;
            else if (_botJoinAt < 0f) _botJoinAt = now + (bots == 0 ? Random.Range(6f, 13f) : Random.Range(2f, 7f));
            else if (now >= _botJoinAt)
            {
                _botJoinAt = -1f;
                var bot = PickBot(r);
                if (bot != null) Online.BotJoin(bot);
            }

            for (var i = 0; i < r.seats.Length; i++)
            {
                var s = r.seats[i];
                if (!s.IsBot || s.ready) continue;
                if (!_botReadyAt.TryGetValue(s.uid, out var at)) _botReadyAt[s.uid] = now + Random.Range(1f, 4f);
                else if (now >= at)
                {
                    _botReadyAt[s.uid] = now + 5f; // 혹시 안 바뀌면 다시
                    Online.BotReady(i);
                }
            }

            if (!CanStartRoom) _autoStartAt = -1f;
            else if (_autoStartAt < 0f) _autoStartAt = now + 1.5f;
            else if (now >= _autoStartAt)
            {
                _autoStartAt = now + 5f;
                Online.StartGame();
            }
        }

        private static PvpBot PickBot(RoomInfo r)
        {
            var free = new List<PvpBot>();
            foreach (var b in PvpBots.All)
            {
                var taken = false;
                foreach (var s in r.seats) taken |= s.uid == b.Uid;
                if (!taken) free.Add(b);
            }
            return free.Count == 0 ? null : free[Random.Range(0, free.Count)];
        }

        // ---------------------------------------------------------------- PvP 판: 봇

        /// <summary>
        /// 봇 차례면 봇을 맡은 기기(나가지 않은 사람 중 가장 앞 자리)가 생각 시간만큼 기다렸다가 봇의 수를 보낸다.
        /// 그 번호가 비어 있을 때만 쓰므로 맡은 기기가 바뀌는 순간 둘이 보내도 하나만 남는다.
        /// </summary>
        private void UpdateBots(PvpSession p, bool caughtUp)
        {
            var now = Time.unscaledTime;
            var driver = ProxyWriter(p);
            // 미뤄 둔 이모티콘
            for (var i = p.BotEmotes.Count - 1; i >= 0; i--)
            {
                var e = p.BotEmotes[i];
                if (now < e.At) continue;
                p.BotEmotes.RemoveAt(i);
                if (driver) Online.BotEmote(e.Seat, e.Index);
            }

            if (BattleOver || !caughtUp || !driver) return;
            var seat = Game.CurrentSeat;
            var bot = p.IsBot(seat) ? p.BotOf(seat) : null;
            if (bot == null) return;
            if (p.BotFor != p.Applied)
            {
                p.BotFor = p.Applied;
                p.BotAt = now + PvpBots.ThinkSeconds(Game, bot, p.BotRng);
                return;
            }
            if (now < p.BotAt || p.ProxySent == p.Applied || _aiTimer > 0f) return;
            p.ProxySent = p.Applied;
            Online.SendMove(p.Applied, PvpBots.ChooseMove(Game, bot, p.BotRng).Encode(), true);
        }

        /// <summary>봇 자리의 이모티콘을 잠시 뒤 보내도록 걸어 둔다 (봇마다 8초에 한 번까지, 성격대로 안 보낼 수도 있다).</summary>
        private void QueueBotEmote(PvpSession p, int seat, BotMood mood, float minDelay, float maxDelay)
        {
            var bot = p.BotOf(seat);
            if (bot == null || !ProxyWriter(p)) return;
            p.BotEmotedAt ??= new float[p.Seats];
            var at = Time.unscaledTime + Random.Range(minDelay, maxDelay);
            if (at - p.BotEmotedAt[seat] < 8f) return;
            var idx = PvpBots.Reaction(bot, mood, p.BotRng);
            if (idx < 0 || idx >= EmoteCount) return;
            p.BotEmotedAt[seat] = at;
            p.BotEmotes.Add(new EmoteShow(seat, idx, at));
        }

        /// <summary>수 하나가 판을 크게 바꿨으면 봇 하나가 반응한다: 자기 수로 많이 얻으면 기뻐하고, 상대 수에 많이 잃으면 속상해한다.</summary>
        private void BotReact(PvpSession p, int actorSeat, int[] before)
        {
            var b = Game.Board;
            var gain = new int[2];
            for (var t = 0; t < 2; t++)
            {
                var side = Game.SideOfSeat(t);
                gain[t] = b.Count(side) - before[t] - (b.Count(side.Opponent()) - before[1 - t]);
            }
            var candidates = new List<(int seat, BotMood mood)>();
            for (var i = 0; i < p.Seats; i++)
            {
                if (!p.IsBot(i)) continue;
                var g = gain[i % 2];
                if (i == actorSeat && g >= 6) candidates.Add((i, BotMood.Good));
                else if (Game.SideOfSeat(actorSeat) != Game.SideOfSeat(i) && g <= -6) candidates.Add((i, BotMood.Hurt));
            }
            if (candidates.Count == 0) return;
            var pick = candidates[Random.Range(0, candidates.Count)];
            QueueBotEmote(p, pick.seat, pick.mood, 0.8f, 2.5f);
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
            ApplyPvpSkins();
            var mates = new List<string>();
            for (var i = 0; i < pvp.Seats; i++)
                if (i != pvp.MySeat && Game.SideOfSeat(i) == Me) mates.Add(pvp.NameOf(i));
            Message = $"{map.Name} — {map.Note}\n" + (Game.SideOfSeat(pvp.MySeat) == Side.Player ? "우리 팀이 먼저 둬요." : "상대 팀이 먼저 둬요.")
                + (mates.Count > 0 ? $" 짝: {mates[0]}" : "");
            for (var i = 0; i < pvp.Seats; i++)
                if (pvp.IsBot(i)) QueueBotEmote(pvp, i, BotMood.Start, 2f, 7f);
        }

        /// <summary>처음부터 다시 재생한다 (이미 둔 번호의 수가 바뀌었을 때). 연출 없이.</summary>
        private void ResyncPvp()
        {
            var p = Pvp;
            Game = PvpCatalog.CreateGame(p.Map, p.Decks, p.Room.seed);
            p.Applied = 0;
            InitClaims();
            while (p.Log.TryGetValue(p.Applied, out var s))
            {
                if (PvpMove.TryDecode(s, out var m)) ApplyPvpMove(m);
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
            UpdateEmotes(p);

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
                var before = new[] { Game.Board.Count(Side.Player), Game.Board.Count(Side.Ai) };
                if (PvpMove.TryDecode(next, out var m) && ApplyPvpMove(m))
                {
                    _actorSeat = seat;
                    if (seat == p.MySeat) Deselect();
                    AfterMove(actor, m.At, m.Kind == PvpMoveKind.Cast ? m.Extra : default);
                    BotReact(p, seat, before);
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
                if (Game.CurrentSeat == p.MySeat && !BattleOver && !Game.ExtraTurnNow)
                    ShowBanner("내 차례!", $"카드를 끌어 놓으세요 · {PvpCatalog.TurnSeconds}초", Palette.Mine);
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
                else if (Game.CurrentSeat != p.MySeat && !p.IsBot(Game.CurrentSeat) && p.ProxySent != p.Applied && ProxyWriter(p)
                         && (p.Left(Game.CurrentSeat) ? waited > 1.5f : waited > PvpCatalog.TurnSeconds + 20f))
                {
                    // 나간 사람(또는 응답 없는 사람)의 차례는 남은 사람 중 가장 앞 자리가 패 버리기로 넘긴다.
                    // 그 번호가 비어 있을 때만 쓴다 (본인이 늦게 보낸 수와 겹치면 먼저 들어간 쪽이 남는다).
                    p.ProxySent = p.Applied;
                    Online.SendMove(p.Applied, PvpMove.Pass(Game.CurrentSeat).Encode(), true);
                }
            }

            UpdateBots(p, caughtUp);

            // 결과
            if (BattleOver && !p.Recorded)
            {
                p.Recorded = true;
                var winner = p.ForfeitWinner != Side.None ? p.ForfeitWinner : Game.Winner;
                for (var i = 0; i < p.Seats; i++)
                    if (p.IsBot(i)) QueueBotEmote(p, i, winner == Game.SideOfSeat(i) ? BotMood.Win : BotMood.Lose, 1f, 4f);
                LastGems = Progress.RecordPvp(winner, Me);
                if (IsHost) Online.Finish();
                Deselect();
            }
        }

        // ---------------------------------------------------------------- PvP 타일 스킨

        /// <summary>
        /// 1:1: 내 땅은 내 스킨, 상대 땅은 상대 스킨(색이 같으면 상대를 빨강으로).
        /// 2:2: 팀 색은 우리 파랑·상대 빨강으로 고정하고, 칸마다 그 칸을 차지한 사람의 재질로 그린다.
        /// </summary>
        private static string SeatSkin(RoomInfo r, int seat) =>
            seat < r.seats.Length && !string.IsNullOrEmpty(r.seats[seat].skin) ? r.seats[seat].skin : TileSkins.Default;

        /// <summary>방·판의 팀 그림: 1:1 은 각자 스킨(색이 같으면 상대 빨강), 2:2 는 파랑·빨강에 재질만 (상대는 그 팀 앞 자리 재질).</summary>
        private static void RoomSkins(RoomInfo r, int mySeat, out string mine, out string foe)
        {
            var me = SeatSkin(r, mySeat);
            var foeSeat = (mySeat + 1) % (r.size * 2);
            if (r.size == 1)
            {
                mine = me;
                foe = TileSkins.AgainstMine(SeatSkin(r, foeSeat), me);
                return;
            }
            mine = TileSkins.WithColor(me, "blue");
            foe = TileSkins.WithColor(SeatSkin(r, foeSeat), TileSkins.EnemyColor);
        }

        private void ApplyPvpSkins()
        {
            var p = Pvp;
            RoomSkins(p.Room, p.MySeat, out var mineSkin, out var foeSkin);
            BoardView.UseSkins(mineSkin, foeSkin);
            BoardView.UseUnitSkins(TileSkins.UnitPart(SeatSkin(p.Room, p.MySeat)), TileSkins.UnitPart(SeatSkin(p.Room, (p.MySeat + 1) % p.Seats)));
            if (p.Room.size == 1)
            {
                BoardView.CellTile = null;
                p.Claim = null;
                return;
            }
            InitClaims();
            BoardView.CellTile = c =>
            {
                var s = Pvp;
                if (s?.Claim == null || Game == null) return null;
                var k = c.Y * Game.Board.Width + c.X;
                if (k >= s.Claim.Length || s.Claim[k] < 0) return null;
                var seat = s.Claim[k];
                return TileSkins.Sprite(TileSkins.WithColor(s.SkinOf(seat), Game.SideOfSeat(seat) == Me ? "blue" : TileSkins.EnemyColor));
            };
        }

        /// <summary>2:2 시작 땅: 팀의 두 사람 중 보드 왼쪽 절반은 앞 자리, 오른쪽은 뒤 자리 것으로.</summary>
        private void InitClaims()
        {
            var p = Pvp;
            if (p == null || p.Room.size != 2) return;
            var b = Game.Board;
            p.Claim = new int[b.Width * b.Height];
            foreach (var c in b.Cells())
            {
                var owner = b[c].Owner;
                var team = owner == Side.Player ? 0 : owner == Side.Ai ? 1 : -1;
                p.Claim[c.Y * b.Width + c.X] = team < 0 ? -1 : team + (c.X < b.Width / 2 ? 0 : 2);
            }
        }

        /// <summary>PvP 수를 두고, 2:2 면 주인이 바뀐 칸을 그 수를 둔 사람(상대 팀 칸이 되면 그 팀 앞 자리) 것으로 적는다.</summary>
        private bool ApplyPvpMove(PvpMove m)
        {
            var p = Pvp;
            var b = Game.Board;
            Side[] before = null;
            if (p?.Claim != null)
            {
                before = new Side[p.Claim.Length];
                foreach (var c in b.Cells()) before[c.Y * b.Width + c.X] = b[c].Owner;
            }
            var seat = Game.CurrentSeat;
            if (!m.TryApply(Game)) return false;
            if (before == null) return true;
            foreach (var c in Game.Board.Cells())
            {
                var k = c.Y * b.Width + c.X;
                var owner = Game.Board[c].Owner;
                if (owner == before[k]) continue;
                p.Claim[k] = owner == Side.None ? -1 : owner == Game.SideOfSeat(seat) ? seat : (owner == Side.Player ? 0 : 1);
            }
            return true;
        }

        /// <summary>대신 넘기는 수·봇의 수를 쓰는 사람: 나가지 않은 사람 자리 중 가장 앞.</summary>
        private static bool ProxyWriter(PvpSession p)
        {
            for (var i = 0; i < p.Seats; i++)
                if (!p.Left(i) && !p.IsBot(i)) return i == p.MySeat;
            return false;
        }

        /// <summary>판 중에 나간다 (기권: 패배로 기록하고 보상 없음). 판이 끝났으면 그냥 나간다.</summary>
        public void LeavePvp()
        {
            var forfeit = Pvp != null && !Pvp.Recorded;
            if (forfeit) Progress.RecordPvpForfeit();
            Online.LeaveRoom();
            Pvp = null;
            Palette.Viewer = Side.Player;
            ShowPvpLobby();
            if (forfeit) ShowToast("기권했어요 — 패배로 기록했어요.");
        }

        // ---------------------------------------------------------------- 이모티콘

        public static int EmoteCount => EmotePacks.Total;
        public int EmotePackTab { get; set; } // 팔레트에서 보고 있는 팩
        public const float EmoteCooldown = 3f, EmoteShowTime = 3f;
        public bool EmoteOpen { get; set; }
        public float EmoteReadyAt { get; private set; }

        public void SendEmote(int index)
        {
            EmoteOpen = false;
            if (Pvp == null || Time.unscaledTime < EmoteReadyAt) return;
            EmoteReadyAt = Time.unscaledTime + EmoteCooldown;
            Online.SendEmote(index); // 방을 거쳐 돌아오면(내 것 포함) UpdateEmotes 가 띄운다
        }

        /// <summary>방 자리의 이모티콘 값이 바뀌었으면 띄운다. 오래된 것은 지운다.</summary>
        private void UpdateEmotes(PvpSession p)
        {
            var seats = p.Room.seats;
            if (p.SeenEmote == null)
            {
                p.SeenEmote = new string[seats.Length];
                for (var i = 0; i < seats.Length; i++) p.SeenEmote[i] = seats[i].emote;
            }
            for (var i = 0; i < seats.Length && i < p.SeenEmote.Length; i++)
            {
                var v = seats[i].emote ?? "";
                if (v == p.SeenEmote[i]) continue;
                p.SeenEmote[i] = v;
                var colon = v.IndexOf(':');
                if (colon <= 0 || !int.TryParse(v.Substring(0, colon), out var idx) || idx < 0 || idx >= EmoteCount) continue;
                p.Emotes.RemoveAll(e => e.Seat == i);
                p.Emotes.Add(new EmoteShow(i, idx, Time.unscaledTime));
            }
            p.Emotes.RemoveAll(e => Time.unscaledTime - e.At > EmoteShowTime);
        }

        public float PvpTimeLeft => Pvp == null ? 0f : Mathf.Max(0f, PvpCatalog.TurnSeconds - (Time.unscaledTime - Pvp.TurnStarted));
    }
}
