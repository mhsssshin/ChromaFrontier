using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 메인 화면: 위 그늘 띠 위에 로고·결정·별의 길·계정, 이름표, 내 땅 섬(카메라) + 꾸미기 캡슐,
    /// 그림을 얹은 모드 카드 두 장(싱글 | 멀티), 아래 유리 독(덱·상점·첨탑).
    /// 바다 배경 위에 반투명 유리(UI/glass, UI/pill)를 겹쳐 바다가 비쳐 보이게 한다.
    /// </summary>
    public static partial class Hud
    {
        private static readonly Color SingleTint = new(0.36f, 0.62f, 1f);
        private static readonly Color MultiTint = new(0.95f, 0.45f, 0.55f);

        private static void DrawHome(GameBootstrap gb)
        {
            DebugKeys();
            Fill(R(0, 0, RefW, 150), "UI/fade_top", Color.white);
            Fill(R(0, 740, RefW, 220), "UI/fade_bottom", Color.white);

            // 위 줄: 로고 · 결정 · 별 · 계정
            Image(R(6, 6, 164, 70), "UI/logo_title", Color.white);
            var gems = R(222, 20, 122, 44);
            GemPill(gems);
            if (GUI.Button(gems, GUIContent.none, GUIStyle.none)) gb.OpenPanel(LobbyPanel.Shop);
            var stars = R(350, 20, 108, 44);
            Nine(stars, "UI/pill", 22, Color.white);
            Image(new Rect(stars.x + 6 * _s, stars.y + 5 * _s, 34 * _s, 34 * _s), "UI/star_on", Color.white);
            Text(new Rect(stars.x + 38 * _s, stars.y, stars.width - 44 * _s, stars.height), Progress.AllStars().ToString(), 18, Color.white);
            if (GUI.Button(stars, GUIContent.none, GUIStyle.none)) gb.OpenPanel(LobbyPanel.Road);
            Badge(R(440, 12, 24, 24), Progress.RoadReadyCount());
            var user = R(466, 14, 58, 58);
            if (HotGlow(user)) Image(Inset(user, -10), "UI/glow", new Color(1f, 1f, 1f, 0.6f));
            Image(user, "UI/icon_user", Online.SignedIn ? Color.white : new Color(0.8f, 0.82f, 0.9f, 0.85f));
            if (GUI.Button(user, GUIContent.none, GUIStyle.none)) gb.OpenPanel(LobbyPanel.Account);

            // 이름표
            var name = Online.SignedIn ? Online.Name : "로그인 안 함";
            var tag = R(120, 84, RefW - 240, 34);
            Nine(tag, "UI/pill", 22, Color.white);
            Rich(tag, $"<color=#ffffff>{name}</color>  <color=#ffd84a>· {StarRoad.RankOf(Progress.AllStars())}</color>", 15, Color.white);

            // (내 땅 섬은 카메라가 12%~48% 띠에 그린다) — 아래 꾸미기 캡슐
            TileSkins.Split(Progress.TileSkin, out var m, out var c);
            var skin = R(140, 466, RefW - 280, 44);
            if (HotGlow(skin)) Nine(Inset(skin, -3), "UI/glass", 28, new Color(1f, 1f, 1f, 0.6f));
            Nine(skin, "UI/glass", 28, Color.white);
            Image(new Rect(skin.x + 10 * _s, skin.y + 6 * _s, 42 * _s, 32 * _s), TileSkins.Sprite(m, c), Color.white);
            Text(new Rect(skin.x + 56 * _s, skin.y, skin.width - 64 * _s, skin.height), $"{TileSkins.Material(m).Name} · {TileSkins.Color(c).Name}  꾸미기 ›", 16, Color.white, TextAnchor.MiddleLeft, 0.6f);
            if (GUI.Button(skin, GUIContent.none, GUIStyle.none))
            {
                gb.OpenPanel(LobbyPanel.Shop);
                gb.ShopTab = 2;
            }

            // 모드 카드
            var stage = StageCatalog.Get(Mathf.Min(Progress.Unlocked - 1, StageCatalog.Count - 1));
            if (ModeCard(R(18, 526, 246, 270), SingleTint, "싱글 모드", $"{ModeName(Progress.Mode)} · {stage.Code}", "스테이지 · 퀘스트", r => SingleArt(r, m, c)))
                gb.ShowSingle();
            Badge(R(240, 516, 30, 30), Progress.QuestsReady());
            if (ModeCard(R(276, 526, 246, 270), MultiTint, "멀티 모드", "1:1 · 2:2 대전", $"전적 {Progress.PvpWins}승 {Progress.PvpDraws}무 {Progress.PvpLosses}패", r => MultiArt(r, m, c)))
                gb.ShowPvpLobby();

            if (Time.time < gb.ToastUntil) Text(R(0, 800, RefW, 24), gb.Toast, 15, Gold, TextAnchor.MiddleCenter, 0.7f);

            // 아래 유리 독: 덱 · 상점 · 첨탑
            var dock = R(16, 830, RefW - 32, 112);
            Nine(dock, "UI/glass", 28, Color.white);
            string[] icons = { "UI/icon_deck", "UI/icon_shop", "UI/icon_tower" };
            string[] labels = { "덱", "상점", "첨탑" };
            var w = (RefW - 32f) / 3f;
            for (var i = 0; i < 3; i++)
            {
                var r = R(16 + i * w, 830, w, 112);
                if (i > 0) Line(new Rect(r.x, r.y + 22 * _s, 1.5f * _s, r.height - 44 * _s), new Color(1f, 1f, 1f, 0.45f));
                var hot = HotGlow(r);
                if (hot) Image(new Rect(r.center.x - 50 * _s, r.y + 2 * _s, 100 * _s, 80 * _s), "UI/glow", new Color(1f, 1f, 1f, 0.7f));
                var size = hot ? 70f : 64f;
                Image(new Rect(r.center.x - size * 0.5f * _s, r.y + (40 - size * 0.5f) * _s, size * _s, size * _s), icons[i], Color.white);
                Text(new Rect(r.x, r.y + 76 * _s, r.width, 28 * _s), labels[i], 18, Color.white, TextAnchor.MiddleCenter, 0.7f);
                if (!GUI.Button(r, GUIContent.none, GUIStyle.none)) continue;
                if (i == 0) gb.OpenPanel(LobbyPanel.Deck);
                else if (i == 1) gb.OpenPanel(LobbyPanel.Shop);
                else gb.ShowTower(Mathf.Min(Progress.TowerCleared, PuzzleCatalog.Count - 1));
            }
        }

        /// <summary>그림을 얹은 큰 모드 카드: 위쪽 그림(후광 위) · 제목 · 짙은 캡슐 안 설명 두 줄.</summary>
        private static bool ModeCard(Rect r, Color tint, string title, string line1, string line2, System.Action<Rect> art)
        {
            var hot = HotGlow(r);
            if (hot) Image(Inset(r, -18), "UI/glow", new Color(tint.r, tint.g, tint.b, 0.55f));
            Nine(r, "UI/mode_card", 28, hot ? Color.Lerp(tint, Color.white, 0.15f) : tint);
            var artRect = new Rect(r.x + 14 * _s, r.y + 12 * _s, r.width - 28 * _s, 140 * _s);
            Image(new Rect(artRect.center.x - 90 * _s, artRect.y - 6 * _s, 180 * _s, 150 * _s), "UI/glow", new Color(1f, 1f, 1f, 0.75f));
            art(artRect);
            Text(new Rect(r.x, r.y + 150 * _s, r.width, 46 * _s), title, 31, Color.white, TextAnchor.MiddleCenter, 0.8f);
            var info = new Rect(r.x + 16 * _s, r.y + 200 * _s, r.width - 32 * _s, 54 * _s);
            Nine(info, "UI/pill", 22, new Color(1f, 1f, 1f, 0.85f));
            Text(new Rect(info.x, info.y + 4 * _s, info.width, 24 * _s), line1, 16, Color.white, TextAnchor.MiddleCenter, 0.4f);
            Text(new Rect(info.x, info.y + 27 * _s, info.width, 22 * _s), line2, 13, Soft, TextAnchor.MiddleCenter, 0f);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>싱글 카드 그림: 내 땅 네 칸(마름모) 위에 불·물·번개 유닛.</summary>
        private static void SingleArt(Rect a, string material, string color)
        {
            var tile = TileSkins.Sprite(material, color);
            var cx = a.center.x;
            var top = a.y + 52 * _s;
            const float k = 0.66f;
            var dx = 64 * k * _s;
            var dy = 32 * k * _s;
            IsoTile(cx, top, k, tile);
            IsoTile(cx - dx, top + dy, k, tile);
            IsoTile(cx + dx, top + dy, k, tile);
            IsoTile(cx, top + 2 * dy, k, tile);
            var t = Time.unscaledTime * 2.2f;
            IsoUnit(cx, top, k, "Units/unit_red", t);
            IsoUnit(cx - dx, top + dy, k, "Units/unit_blue", t + 1f);
            IsoUnit(cx + dx, top + dy, k, "Units/unit_yellow", t + 2f);
        }

        /// <summary>멀티 카드 그림: 우리 땅(내 스킨) 물 유닛 vs 상대 땅(빨강) 불 유닛, 사이에 칼과 VS.</summary>
        private static void MultiArt(Rect a, string material, string color)
        {
            const float k = 0.66f;
            var y = a.y + 78 * _s;
            var lx = a.center.x - 58 * _s;
            var rx = a.center.x + 58 * _s;
            IsoTile(lx, y, k, TileSkins.Sprite(material, color));
            IsoTile(rx, y, k, TileSkins.Sprite("plain", TileSkins.EnemyColor));
            IsoUnit(lx, y, k, "Units/unit_blue", Time.unscaledTime * 2.2f);
            IsoUnit(rx, y, k, "Units/unit_red", Time.unscaledTime * 2.2f + 1.5f);
            var pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 4f);
            Image(new Rect(a.center.x - 22 * _s, a.y + 4 * _s, 44 * _s, 44 * _s), "UI/icon_pvp", Color.white);
            Text(new Rect(a.center.x - 40 * pulse * _s, a.y + 50 * _s, 80 * pulse * _s, 44 * _s), "VS", Mathf.RoundToInt(30 * pulse), Gold, TextAnchor.MiddleCenter, 0.9f);
        }

        /// <summary>윗면 가운데가 (cx, cy) 인 타일 그림 (캔버스 136x104, 윗면 중심 68,34).</summary>
        private static void IsoTile(float cx, float cy, float k, string sprite) =>
            Image(new Rect(cx - 68 * k * _s, cy - 34 * k * _s, 136 * k * _s, 104 * k * _s), sprite, Color.white);

        /// <summary>바닥점이 (cx, cy) 인 유닛 그림 (캔버스 96x112, 바닥점 48,100). 둥실둥실.</summary>
        private static void IsoUnit(float cx, float cy, float k, string sprite, float phase)
        {
            var bob = Mathf.Sin(phase) * 3f * _s;
            Image(new Rect(cx - 48 * k * _s, cy - 100 * k * _s + 4 * k * _s + bob, 96 * k * _s, 112 * k * _s), sprite, Color.white);
        }

        /// <summary>마우스가 올라가 있는가 (창이 떠 있으면 아니다).</summary>
        private static bool HotGlow(Rect r) => GUI.enabled && r.Contains(Event.current.mousePosition);

        /// <summary>단색 사각형 (구분선 등).</summary>
        private static void Line(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        /// <summary>비율을 무시하고 사각형을 꽉 채워 그린다 (그늘 띠 등).</summary>
        private static void Fill(Rect r, string tex, Color tint)
        {
            var t = Art.Texture(tex);
            if (t == null) return;
            var prev = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        /// <summary>빨간 숫자 배지 (0 이면 안 그림).</summary>
        private static void Badge(Rect r, int n)
        {
            if (n <= 0) return;
            Nine(r, "UI/button", 22, new Color(0.95f, 0.25f, 0.3f));
            Text(r, n.ToString(), 14, Color.white, TextAnchor.MiddleCenter, 0f);
        }

        /// <summary>개발 빌드 단축키: U = 전체 해금, G = 결정 500.</summary>
        private static void DebugKeys()
        {
            var e = Event.current;
            if (!Debug.isDebugBuild || e.type != EventType.KeyDown) return;
            if (e.keyCode == KeyCode.U)
            {
                Progress.UnlockAll();
                e.Use();
            }
            else if (e.keyCode == KeyCode.G)
            {
                Progress.AddGemsDebug(500);
                e.Use();
            }
        }
    }
}
