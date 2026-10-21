using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>온라인 화면: 로그인, 계정 창, PvP 로비·방, PvP 판의 위쪽 줄과 결과.</summary>
    public static partial class Hud
    {
        private static float _leaveArmedUntil; // PvP 판 '나가기': 이 시각까지 한 번 더 누르면 나간다

        /// <summary>
        /// 팝업을 여는 버튼(Google 로그인 등): 누르는 순간 net.js 에 걸어 두어 브라우저가 포인터를 떼는 이벤트 안에서 팝업을 연다.
        /// 클릭이 끝나면 한 번 더 부르지만 net.js 가 한 번만 실행한다.
        /// </summary>
        private static bool PopupButton(Rect r, Color tint, string label, int size, string action)
        {
            var e = Event.current;
            if (GUI.enabled && e.type == EventType.MouseDown && r.Contains(e.mousePosition)) Online.Arm(action);
            if (!Button(r, tint, label, size)) return false;
            Online.Run(action);
            return true;
        }

        private static void OnlineError(float y)
        {
            if (!string.IsNullOrEmpty(Online.Error) && Time.unscaledTime - Online.ErrorAt < 6f)
                Text(R(20, y, RefW - 40, 40), Online.Error, 15, new Color(1f, 0.75f, 0.7f), TextAnchor.MiddleCenter, 0.7f);
        }

        private static string Dots => new string('.', 1 + (int)(Time.unscaledTime * 2f) % 3);

        // ---------------------------------------------------------------- 로그인

        /// <summary>
        /// 로그인: Google / 게스트(익명 계정, 이 브라우저에서만) / 로그인 없이 오프라인. 로그인하면 진행이 클라우드에 저장되고 대전을 할 수 있다.
        /// 로그인 전에 이 기기에서 하던 기록과 계정 기록이 둘 다 있으면 어느 쪽을 쓸지 고르게 한다.
        /// </summary>
        private static void DrawLogin(GameBootstrap gb)
        {
            Image(R(RefW * 0.5f - 80, 24, 160, 160), "UI/logo_emblem", Color.white);
            Image(R(RefW * 0.5f - 180, 176, 360, 155), "UI/logo_title", Color.white);

            if (Online.ConflictLocal != null)
            {
                DrawConflict();
                return;
            }

            var panel = R(40, 340, RefW - 80, 214);
            if (!Online.AuthKnown || gb.LoginWaitingSignOut || (Online.SignedIn && !Online.Synced))
            {
                Nine(panel, "UI/panel", 28, Color.white);
                var what = Online.SignedIn ? $"{Online.Name} 님의 기록을 불러오는 중" : "로그인 상태를 확인하는 중";
                Text(panel, what + Dots, 20, Color.white);
                if (Online.SignedIn && Button(R(150, 570, 240, 50), new Color(0.45f, 0.47f, 0.6f), "로그아웃", 17)) gb.SignOut();
                OnlineError(630);
                return;
            }

            // (보드 장식은 카메라가 아래 75%~98% 띠에 그린다)
            Nine(panel, "UI/panel", 28, Color.white);
            Text(R(56, 350, RefW - 112, 56), "로그인하면 진행이 클라우드에 저장되고\n다른 사람과 대전할 수 있어요.", 17, Soft);
            GUI.enabled = Online.Ready;
            PopupButton(R(70, 412, RefW - 140, 62), new Color(0.95f, 0.95f, 0.98f), "", 22, "google");
            Rich(R(70, 412, RefW - 140, 62), "<color=#4285F4>G</color><color=#EA4335>o</color><color=#FBBC05>o</color><color=#4285F4>g</color><color=#34A853>l</color><color=#EA4335>e</color><color=#3a3a4a>로 로그인</color>", 22, Color.white);
            if (Button(R(70, 482, RefW - 140, 56), new Color(0.3f, 0.7f, 0.55f), "게스트로 시작", 20)) Online.Run("guest");
            GUI.enabled = true;
            Text(R(40, 560, RefW - 80, 40), "게스트 기록은 이 브라우저에만 묶여요. 나중에 계정 창에서 Google 을 연결하면 그대로 옮겨져요.", 13, Soft, TextAnchor.MiddleCenter, 0.6f);
            if (Online.Failed) Text(R(40, 600, RefW - 80, 30), "온라인에 연결하지 못했어요. 오프라인으로 할 수 있어요.", 15, new Color(1f, 0.8f, 0.7f));
            else if (!Online.Ready) Text(R(40, 600, RefW - 80, 30), "온라인 기능을 불러오는 중" + Dots, 15, Soft);
            if (Button(R(150, 636, 240, 48), new Color(0.45f, 0.47f, 0.6f), "로그인 없이 하기", 17)) gb.PlayOffline();
            OnlineError(690);
        }

        /// <summary>로그인 전에 하던 이 기기의 기록 vs 계정에 저장된 기록.</summary>
        private static void DrawConflict()
        {
            var panel = R(30, 340, RefW - 60, 340);
            Nine(panel, "UI/panel", 28, Color.white);
            Text(R(40, 350, RefW - 80, 40), "어느 기록으로 할까요?", 24, Gold, TextAnchor.MiddleCenter, 0.7f);
            Text(R(40, 390, RefW - 80, 40), "이 계정에 저장된 기록이 있어요. 고르지 않은 쪽은 사라져요.", 14, Soft);
            if (Button(R(50, 436, RefW - 100, 100), new Color(0.35f, 0.5f, 0.9f))) Online.Resolve(true);
            Text(R(50, 444, RefW - 100, 40), "계정에 저장된 기록", 21, Color.white);
            Text(R(50, 482, RefW - 100, 40), Online.ConflictRemote, 15, Soft);
            if (Button(R(50, 550, RefW - 100, 100), new Color(0.3f, 0.65f, 0.55f))) Online.Resolve(false);
            Text(R(50, 558, RefW - 100, 40), "이 기기의 기록 (계정에 덮어쓰기)", 21, Color.white);
            Text(R(50, 596, RefW - 100, 40), Online.ConflictLocal, 15, Soft);
        }

        // ---------------------------------------------------------------- 계정 창

        private static void DrawAccountPanel(GameBootstrap gb)
        {
            Dim(0.6f);
            var panel = R(30, 200, RefW - 60, 520);
            Nine(panel, "UI/panel", 28, Color.white);
            Text(R(46, 212, 300, 44), "계정 · 설정", 30, Gold, TextAnchor.MiddleLeft, 0.7f);
            if (Button(R(RefW - 130, 214, 84, 42), new Color(0.4f, 0.42f, 0.58f), "닫기", 17)) gb.OpenPanel(LobbyPanel.None);

            var y = 270f;
            void Row(string label, string value)
            {
                Text(R(50, y, 120, 36), label, 16, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft, 0f);
                Text(R(170, y, RefW - 220, 36), value, 18, Color.white, TextAnchor.MiddleLeft, 0f);
                y += 40;
            }

            if (!Online.Available)
            {
                Text(R(50, 280, RefW - 100, 120), "이 버전은 오프라인 전용이에요.\n로그인·클라우드 저장·대전은 웹 버전에서 할 수 있어요.", 17, Soft);
            }
            else if (!Online.SignedIn)
            {
                Row("상태", "로그인 안 함 (이 기기에만 저장)");
                Text(R(50, y + 6, RefW - 100, 60), "로그인하면 지금 기록을 계정에 올리고, 다른 기기에서도 이어서 할 수 있어요.", 15, Soft);
                if (Button(R(110, y + 80, RefW - 220, 60), new Color(0.35f, 0.5f, 0.9f), "로그인하기", 21)) gb.GoLogin();
            }
            else
            {
                Row("이름", Online.Name);
                Row("계정", Online.IsGuest ? "게스트 (이 브라우저에만)" : "Google");
                var saved = Time.unscaledTime - Online.SavedAt;
                Row("저장", saved < 60f ? $"클라우드에 저장됨 ({Mathf.FloorToInt(saved)}초 전)" : "클라우드에 자동 저장 중");
                Row("대전", $"{Progress.PvpWins}승 {Progress.PvpDraws}무 {Progress.PvpLosses}패");
                if (PopupButton(R(50, y + 10, 210, 56), new Color(0.35f, 0.5f, 0.9f), "이름 바꾸기", 18, "name")) { }
                if (Online.IsGuest && PopupButton(R(280, y + 10, 210, 56), new Color(0.95f, 0.95f, 0.98f), "", 18, "link")) { }
                if (Online.IsGuest) Text(R(280, y + 10, 210, 56), "Google 연결", 18, new Color(0.23f, 0.23f, 0.3f), TextAnchor.MiddleCenter, 0f);
                if (Online.IsGuest) Text(R(50, y + 72, RefW - 100, 40), "Google 을 연결하면 다른 기기에서도 이 기록으로 이어서 할 수 있어요.", 13, Soft);
                if (Button(R(170, y + 120, 200, 52), new Color(0.75f, 0.35f, 0.35f), "로그아웃", 18)) gb.SignOut();
            }
            if (Button(R(46, 664, 200, 44), new Color(0.3f, 0.7f, 0.55f), "튜토리얼 다시 보기", 15)) gb.StartTutorial(0);
            OnlineError(600);
            SwallowInput();
        }

        // ---------------------------------------------------------------- PvP 로비

        private static void DrawPvpLobby(GameBootstrap gb)
        {
            Fill(R(0, 0, RefW, 130), "UI/fade_top", Color.white);
            if (Button(R(14, 14, 100, 44), new Color(0.4f, 0.42f, 0.58f), "← 홈", 16)) gb.ShowHome();
            Text(R(0, 14, RefW, 44), "대전", 28, Gold, TextAnchor.MiddleCenter, 0.7f);
            GemPill(R(RefW - 132, 14, 118, 44));

            if (!Online.SignedIn)
            {
                var p = R(40, 300, RefW - 80, 260);
                Nine(p, "UI/panel", 28, Color.white);
                Text(R(50, 320, RefW - 100, 90), "대전은 로그인해야 할 수 있어요.\n방을 만들거나 들어가 다른 사람과 1:1, 2:2 로 겨뤄요.", 18, Soft);
                if (Button(R(130, 440, RefW - 260, 64), new Color(0.35f, 0.5f, 0.9f), "로그인하기", 22)) gb.GoLogin();
                return;
            }

            Text(R(20, 64, RefW - 40, 26), $"{Online.Name}  ·  전적 {Progress.PvpWins}승 {Progress.PvpDraws}무 {Progress.PvpLosses}패", 16, Color.white);
            Text(R(20, 88, RefW - 40, 22), $"이기면 결정 {PvpCatalog.WinGems}, 비기면 {PvpCatalog.DrawGems}, 지면 {PvpCatalog.LoseGems}  ·  덱은 편성 그대로, 강화는 적용 안 돼요", 13, Soft, TextAnchor.MiddleCenter, 0f);
            if (Button(R(14, 116, 250, 70), new Color(0.3f, 0.6f, 1f), "1:1 방 만들기", 22)) gb.CreateRoom(1);
            if (Button(R(276, 116, 250, 70), new Color(0.85f, 0.45f, 0.3f), "2:2 방 만들기", 22)) gb.CreateRoom(2);

            var list = R(14, 196, RefW - 28, 700);
            Nine(list, "UI/panel", 28, Color.white);
            Text(R(30, 204, 300, 36), "열린 방", 20, Gold, TextAnchor.MiddleLeft, 0.6f);
            if (Online.Rooms.Count == 0)
                Text(R(30, 400, RefW - 60, 80), "열린 방이 없어요.\n방을 만들고 친구에게 들어오라고 하세요!", 18, Soft);
            for (var i = 0; i < Online.Rooms.Count && i < 8; i++)
            {
                var room = Online.Rooms[i];
                var r = R(26, 246 + i * 80, RefW - 52, 72);
                var full = room.Filled >= room.seats.Length;
                Nine(r, "UI/panel", 28, full ? new Color(0.7f, 0.72f, 0.8f) : new Color(1f, 1f, 1f, 0.95f));
                var chip = new Rect(r.x + 10 * _s, r.y + 16 * _s, 56 * _s, 40 * _s);
                Nine(chip, "UI/button", 22, room.size == 2 ? new Color(0.85f, 0.45f, 0.3f) : new Color(0.3f, 0.6f, 1f));
                Text(chip, room.size == 2 ? "2:2" : "1:1", 17, Color.white, TextAnchor.MiddleCenter, 0f);
                Text(new Rect(r.x + 76 * _s, r.y + 6 * _s, r.width - 210 * _s, 32 * _s), room.title, 18, Color.white, TextAnchor.MiddleLeft, 0.5f);
                Text(new Rect(r.x + 76 * _s, r.y + 38 * _s, r.width - 210 * _s, 26 * _s), $"{PvpCatalog.Find(room.size, room.map).Name}  ·  {room.Filled}/{room.seats.Length}명", 14, Soft, TextAnchor.MiddleLeft, 0f);
                GUI.enabled = !full;
                if (Button(new Rect(r.xMax - 124 * _s, r.y + 12 * _s, 112 * _s, 48 * _s), new Color(1f, 0.55f, 0.2f), full ? "꽉 참" : "입장", 19)) gb.JoinRoom(room);
                GUI.enabled = true;
            }
            if (Time.time < gb.ToastUntil) Text(R(20, 856, RefW - 40, 30), gb.Toast, 16, Gold, TextAnchor.MiddleCenter, 0.7f);
            OnlineError(904);
        }

        // ---------------------------------------------------------------- PvP 방

        private static Color TeamColor(int seat, Side me) => Game.SideOfSeat(seat) == me ? Palette.Mine : Palette.Foe;

        private static void DrawPvpRoom(GameBootstrap gb)
        {
            var room = Online.Room;
            if (room == null) return;
            var me = Game.SideOfSeat(Mathf.Max(0, Online.MySeat));
            if (Button(R(14, 14, 90, 44), new Color(0.4f, 0.42f, 0.58f), "나가기", 16)) gb.LeaveRoom();
            Text(R(110, 14, RefW - 220, 44), room.title, 20, Color.white, TextAnchor.MiddleCenter, 0.7f);
            var mode = R(RefW - 104, 14, 90, 44);
            Nine(mode, "UI/button", 22, room.size == 2 ? new Color(0.85f, 0.45f, 0.3f) : new Color(0.3f, 0.6f, 1f));
            Text(mode, room.size == 2 ? "2:2" : "1:1", 20, Color.white);

            // (맵 미리보기는 카메라가 8%~34% 띠에 그린다)
            var map = PvpCatalog.Find(room.size, room.map);
            var info = R(14, 336, RefW - 28, 76);
            Nine(info, "UI/panel", 28, Color.white);
            Text(new Rect(info.x + 14 * _s, info.y + 6 * _s, info.width - 160 * _s, 34 * _s), $"맵: {map.Name}  ·  {map.MaxTurns}턴", 19, Color.white, TextAnchor.MiddleLeft);
            Text(new Rect(info.x + 14 * _s, info.y + 40 * _s, info.width - 160 * _s, 28 * _s), map.Note, 14, Soft, TextAnchor.MiddleLeft, 0f);
            if (gb.IsHost && Button(new Rect(info.xMax - 140 * _s, info.y + 14 * _s, 128 * _s, 48 * _s), new Color(0.3f, 0.65f, 0.8f), "맵 바꾸기", 17)) gb.CycleRoomMap();

            // 자리: 왼쪽 우리 팀, 오른쪽 상대 팀 (내가 2팀이면 좌우가 바뀐다)
            for (var col = 0; col < 2; col++)
            {
                var team = me == Side.Player ? col : 1 - col;
                Text(R(14 + col * 262, 422, 250, 32), col == 0 ? $"{team + 1}팀 (우리)" : $"{team + 1}팀", 18, col == 0 ? new Color(0.6f, 0.8f, 1f) : new Color(1f, 0.7f, 0.65f), TextAnchor.MiddleCenter, 0.6f);
                for (var k = 0; k < room.size; k++)
                {
                    var seat = team + k * 2;
                    var s = room.seats[seat];
                    var r = R(14 + col * 262, 458 + k * 118, 250, 108);
                    if (s.Empty)
                    {
                        if (Button(r, new Color(0.45f, 0.47f, 0.6f))) Online.MoveSeat(seat);
                        Text(new Rect(r.x, r.y + 20 * _s, r.width, 30 * _s), "빈 자리", 20, Color.white);
                        Text(new Rect(r.x, r.y + 56 * _s, r.width, 26 * _s), "눌러서 이 자리로", 14, Soft, TextAnchor.MiddleCenter, 0f);
                        continue;
                    }
                    if (seat == Online.MySeat) Nine(Inset(r, -4), "UI/button", 22, Gold);
                    Nine(r, "UI/button", 22, TeamColor(seat, me));
                    var host = s.uid == room.host;
                    if (host) Image(new Rect(r.x + 8 * _s, r.y + 8 * _s, 30 * _s, 30 * _s), "UI/crown", Color.white);
                    Text(new Rect(r.x + 8 * _s, r.y + 14 * _s, r.width - 16 * _s, 36 * _s), s.name + (seat == Online.MySeat ? " (나)" : ""), 20, Color.white);
                    var status = host ? "방장" : s.ready ? "준비 완료!" : "준비 중...";
                    Text(new Rect(r.x, r.y + 56 * _s, r.width, 30 * _s), status, 16, host || s.ready ? Gold : Soft, TextAnchor.MiddleCenter, 0.5f);
                }
            }

            var orderY = 458 + room.size * 118 + 4;
            Text(R(14, orderY, RefW - 28, 26), room.size == 2 ? "차례: 1팀 → 2팀 → 1팀 짝 → 2팀 짝 (같은 팀은 땅을 함께 써요)" : "차례: 1팀이 먼저 둬요", 14, Soft, TextAnchor.MiddleCenter, 0f);
            Text(R(14, 806, RefW - 28, 30), gb.IsHost ? "모두 준비하면 시작할 수 있어요." : "준비를 누르고 방장이 시작하기를 기다려요.", 15, Color.white, TextAnchor.MiddleCenter, 0.5f);
            if (gb.IsHost)
            {
                GUI.enabled = gb.CanStartRoom;
                if (Button(R(110, 844, RefW - 220, 80), new Color(1f, 0.55f, 0.2f), "시작!", 30)) Online.StartGame();
                GUI.enabled = true;
            }
            else if (Online.MySeat >= 0)
            {
                var ready = room.seats[Online.MySeat].ready;
                if (Button(R(110, 844, RefW - 220, 80), ready ? new Color(0.45f, 0.47f, 0.6f) : new Color(0.3f, 0.75f, 0.45f), ready ? "준비 취소" : "준비!", 28))
                    Online.SetReady(!ready);
            }
            OnlineError(764);
        }

        // ---------------------------------------------------------------- PvP 판

        private static string SeatLabel(GameBootstrap gb, int seat)
        {
            var p = gb.Pvp;
            if (seat == p.MySeat) return "나";
            return p.NameOf(seat);
        }

        private static void DrawPvpTop(GameBootstrap gb, Game g)
        {
            var p = gb.Pvp;
            var b = g.Board;
            var total = Mathf.Max(1, b.Playable);
            var mine = b.Count(gb.Me);
            var theirs = b.Count(gb.Me.Opponent());

            var armed = Time.unscaledTime < _leaveArmedUntil;
            if (Button(R(14, 14, 76, 44), armed ? new Color(0.95f, 0.45f, 0.3f) : new Color(0.4f, 0.42f, 0.58f), armed ? "기권?" : "나가기", armed ? 15 : 16))
            {
                if (armed || gb.BattleOver) { _leaveArmedUntil = 0f; gb.LeavePvp(); }
                else _leaveArmedUntil = Time.unscaledTime + 3f;
            }

            var turnPill = R(96, 14, 108, 44);
            Nine(turnPill, "UI/panel", 28, Color.white);
            Text(turnPill, $"턴 {Mathf.Min(g.Turn + 1, g.MaxTurns)} / {g.MaxTurns}", 16, Color.white);

            var left = gb.PvpTimeLeft;
            var timer = R(210, 14, 84, 44);
            Nine(timer, "UI/panel", 28, left < 10f ? new Color(1f, 0.6f, 0.55f) : Color.white);
            Text(timer, $"{Mathf.CeilToInt(left)}초", 18, left < 10f ? new Color(1f, 0.85f, 0.4f) : Color.white);

            var who = R(300, 14, RefW - 314, 44);
            var seat = g.CurrentSeat;
            var friendly = Game.SideOfSeat(seat) == gb.Me;
            Nine(who, "UI/button", 22, g.IsOver ? new Color(0.6f, 0.6f, 0.7f) : friendly ? Palette.Mine : Palette.Foe);
            var label = g.IsOver ? "게임 종료" : seat == p.MySeat ? "내 차례!" : friendly ? $"짝 {p.NameOf(seat)} 차례" : $"{p.NameOf(seat)} 차례";
            if (!g.IsOver && p.Left(seat)) label += " (나감)";
            Text(who, label, 17, Color.white);

            Text(R(0, 66, RefW, 30), "영토 점유율", 22, Color.white, TextAnchor.MiddleCenter, 0.6f);
            var bar = R(36, 98, RefW - 72, 46);
            Nine(bar, "UI/bar_frame", 30, Color.white);
            var inner = Inset(bar, 7);
            var pw = inner.width * mine / total;
            var aw = inner.width * theirs / total;
            if (pw > 1f) Nine(new Rect(inner.x, inner.y, Mathf.Max(pw, inner.height), inner.height), "UI/bar_fill", 22, Palette.Mine);
            if (aw > 1f) Nine(new Rect(inner.xMax - Mathf.Max(aw, inner.height), inner.y, Mathf.Max(aw, inner.height), inner.height), "UI/bar_fill", 22, Palette.Foe);
            Text(Inset(inner, 4), $"우리 {mine * 100 / total}%", 18, Color.white, TextAnchor.MiddleLeft, 0.7f);
            Text(Inset(inner, 4), $"{theirs * 100 / total}% 상대", 18, Color.white, TextAnchor.MiddleRight, 0.7f);

            var msg = R(24, 152, RefW - 48, 54);
            Nine(msg, "UI/panel", 28, new Color(1f, 1f, 1f, 0.85f));
            Text(Inset(msg, 5), gb.Message, 15, Soft, TextAnchor.MiddleCenter, 0f);

            // 자리 줄: 차례 순서대로, 지금 차례는 금빛
            var n = p.Seats;
            const float gap = 6f;
            var w = (RefW - 24f - (n - 1) * gap) / n;
            for (var i = 0; i < n; i++)
            {
                var r = R(12 + i * (w + gap), 210, w, 46);
                var tint = p.Left(i) ? new Color(0.5f, 0.5f, 0.55f) : TeamColor(i, gb.Me);
                if (i == seat && !g.IsOver) Nine(Inset(r, -3), "UI/button", 22, Gold);
                Nine(r, "UI/button", 22, tint);
                Text(r, SeatLabel(gb, i) + (p.Left(i) ? " · 나감" : ""), n > 2 ? 14 : 17, Color.white, TextAnchor.MiddleCenter, 0.5f);
            }
        }

        /// <summary>내 차례가 되면 화면 가운데에 크게 "내 차례!" (1.8초, 통통 튀며 나타났다 사라짐).</summary>
        private static void DrawMyTurnBanner(GameBootstrap gb)
        {
            var t = Time.unscaledTime - gb.Pvp.MyTurnAt;
            if (t < 0f || t > 1.8f || gb.BattleOver) return;
            var k = EaseOutBack(Mathf.Clamp01(t / 0.35f));
            var a = Mathf.Clamp01((1.8f - t) / 0.4f);
            var w = 360f * k;
            var h = 120f * k;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            var r = R(RefW * 0.5f - w * 0.5f, 380f - h * 0.5f, w, h);
            Nine(Inset(r, -5), "UI/button", 22, new Color(Gold.r, Gold.g, Gold.b, a));
            Nine(r, "UI/button", 22, new Color(Palette.Mine.r, Palette.Mine.g, Palette.Mine.b, a));
            if (k > 0.6f)
            {
                Text(new Rect(r.x, r.y + 8 * _s, r.width, 64 * _s), "내 차례!", 46, new Color(Gold.r, Gold.g, Gold.b, a), TextAnchor.MiddleCenter, 0.8f);
                Text(new Rect(r.x, r.y + 72 * _s, r.width, 32 * _s), $"카드를 끌어 놓으세요 · {PvpCatalog.TurnSeconds}초", 16, new Color(1f, 1f, 1f, a), TextAnchor.MiddleCenter, 0.5f);
            }
            GUI.color = prev;
        }

        private static void DrawPvpResult(GameBootstrap gb, Game g)
        {
            Dim(0.45f);
            var p = gb.Pvp;
            var winner = p.ForfeitWinner != Side.None ? p.ForfeitWinner : g.Winner;
            var won = winner == gb.Me;
            Nine(R(50, 250, 440, 420), "UI/panel", 28, Color.white);
            var title = won ? "승리!" : winner == Side.None ? "무승부" : "패배";
            Text(R(50, 264, 440, 70), title, 54, won ? Gold : winner == Side.None ? Color.white : new Color(1f, 0.6f, 0.55f), TextAnchor.MiddleCenter, 0.7f);
            Text(R(50, 332, 440, 28), $"{(p.Room.size == 2 ? "2:2" : "1:1")} 대전  ·  {p.Map.Name}", 17, Soft);
            var total = Mathf.Max(1, g.Board.Playable);
            Text(R(50, 372, 440, 34), $"우리 {g.Board.Count(gb.Me) * 100 / total}%   vs   상대 {g.Board.Count(gb.Me.Opponent()) * 100 / total}%", 22, Color.white);
            var note = p.ForfeitWinner != Side.None ? (won ? "상대 팀이 모두 나가서 이겼어요" : "우리 팀이 모두 나갔어요")
                : g.Stuck != Side.None ? (g.Stuck == gb.Me ? "우리 팀이 놓을 칸이 없어 게임이 끝났어요" : "상대 팀이 놓을 칸이 없어 게임이 끝났어요")
                : "턴을 모두 써서 점유율로 승패를 가렸어요";
            Text(R(50, 408, 440, 26), note, 14, new Color(1f, 0.9f, 0.7f));

            var gemRow = R(110, 446, 320, 44);
            Nine(gemRow, "UI/panel", 28, new Color(1f, 0.95f, 0.75f));
            Image(new Rect(gemRow.x + 10 * _s, gemRow.y + 5 * _s, 34 * _s, 34 * _s), "UI/icon_gem", Color.white);
            Text(new Rect(gemRow.x + 48 * _s, gemRow.y, gemRow.width - 56 * _s, gemRow.height), $"+{gb.LastGems}  (보유 {Progress.Gems})", 20, Gold, TextAnchor.MiddleLeft);
            Text(R(50, 498, 440, 26), $"전적 {Progress.PvpWins}승 {Progress.PvpDraws}무 {Progress.PvpLosses}패", 16, Soft);
            if (Button(R(110, 560, 320, 70), new Color(1f, 0.55f, 0.2f), "대전 로비로", 24)) gb.LeavePvp();
        }
    }
}
