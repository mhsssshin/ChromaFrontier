using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>메인 화면: 위 줄(로고·결정·별의 길·계정) · 내 땅 섬 미리보기 · 싱글 | 멀티 · 아래 줄(덱·상점·첨탑).</summary>
    public static partial class Hud
    {
        private static void DrawHome(GameBootstrap gb)
        {
            DebugKeys();

            // 위 줄
            Image(R(4, 8, 168, 72), "UI/logo_title", Color.white);
            var gems = R(232, 22, 120, 44);
            GemPill(gems);
            if (GUI.Button(gems, GUIContent.none, GUIStyle.none)) gb.OpenPanel(LobbyPanel.Shop);
            var starPill = R(358, 22, 100, 44);
            if (Button(starPill, new Color(0.85f, 0.6f, 0.2f))) gb.OpenPanel(LobbyPanel.Road);
            Image(new Rect(starPill.x + 6 * _s, starPill.y + 6 * _s, 32 * _s, 32 * _s), "UI/star_on", Color.white);
            Text(new Rect(starPill.x + 36 * _s, starPill.y, starPill.width - 40 * _s, starPill.height), Progress.AllStars().ToString(), 18, Color.white);
            Badge(R(440, 12, 26, 26), Progress.RoadReadyCount());
            if (Button(R(464, 22, 64, 44), Online.SignedIn ? new Color(0.35f, 0.5f, 0.9f) : new Color(0.45f, 0.47f, 0.6f), Online.SignedIn ? "계정" : "로그인", 15))
                gb.OpenPanel(LobbyPanel.Account);

            var who = Online.SignedIn ? Online.Name + "  ·  " : "";
            Text(R(0, 84, RefW, 30), who + StarRoad.RankOf(Progress.AllStars()), 18, Gold, TextAnchor.MiddleCenter, 0.7f);

            // (내 땅 섬은 카메라가 12%~48% 띠에 그린다)
            TileSkins.Split(Progress.TileSkin, out var m, out var c);
            var skin = R(110, 470, RefW - 220, 40);
            if (Button(skin, new Color(0.6f, 0.45f, 0.85f)))
            {
                gb.OpenPanel(LobbyPanel.Shop);
                gb.ShopTab = 2;
            }
            Text(skin, $"내 땅: {TileSkins.Material(m).Name} · {TileSkins.Color(c).Name}   꾸미기 ›", 16, Color.white);

            // 싱글 | 멀티
            var single = R(20, 528, 244, 260);
            if (Button(single, new Color(0.3f, 0.6f, 1f))) gb.ShowSingle();
            Image(new Rect(single.center.x - 56 * _s, single.y + 22 * _s, 112 * _s, 130 * _s), "Units/unit_red", Color.white);
            Text(new Rect(single.x, single.y + 156 * _s, single.width, 44 * _s), "싱글 모드", 30, Color.white, TextAnchor.MiddleCenter, 0.7f);
            var stage = StageCatalog.Get(Mathf.Min(Progress.Unlocked - 1, StageCatalog.Count - 1));
            Text(new Rect(single.x, single.y + 200 * _s, single.width, 26 * _s), $"{ModeName(Progress.Mode)} · {stage.Code}", 16, Soft, TextAnchor.MiddleCenter, 0.5f);
            Text(new Rect(single.x, single.y + 224 * _s, single.width, 24 * _s), "스테이지 · 퀘스트", 13, Soft, TextAnchor.MiddleCenter, 0f);
            Badge(R(242, 518, 30, 30), Progress.QuestsReady());

            var multi = R(276, 528, 244, 260);
            if (Button(multi, new Color(0.85f, 0.4f, 0.5f))) gb.ShowPvpLobby();
            Image(new Rect(multi.center.x - 56 * _s, multi.y + 30 * _s, 112 * _s, 112 * _s), "UI/icon_pvp", Color.white);
            Text(new Rect(multi.x, multi.y + 156 * _s, multi.width, 44 * _s), "멀티 모드", 30, Color.white, TextAnchor.MiddleCenter, 0.7f);
            Text(new Rect(multi.x, multi.y + 200 * _s, multi.width, 26 * _s), "1:1 · 2:2 대전", 16, Soft, TextAnchor.MiddleCenter, 0.5f);
            Text(new Rect(multi.x, multi.y + 224 * _s, multi.width, 24 * _s), $"전적 {Progress.PvpWins}승 {Progress.PvpDraws}무 {Progress.PvpLosses}패", 13, Soft, TextAnchor.MiddleCenter, 0f);

            if (Time.time < gb.ToastUntil) Text(R(0, 800, RefW, 24), gb.Toast, 15, Gold, TextAnchor.MiddleCenter, 0.7f);

            // 아래 줄: 덱 · 상점 · 첨탑
            if (IconButton(R(20, 836, 158, 96), new Color(0.35f, 0.5f, 0.9f), "UI/icon_deck", "덱")) gb.OpenPanel(LobbyPanel.Deck);
            if (IconButton(R(191, 836, 158, 96), new Color(1f, 0.6f, 0.25f), "UI/icon_gem", "상점")) gb.OpenPanel(LobbyPanel.Shop);
            if (IconButton(R(362, 836, 158, 96), new Color(0.25f, 0.62f, 0.7f), "UI/icon_tower", "첨탑")) gb.ShowTower(Mathf.Min(Progress.TowerCleared, PuzzleCatalog.Count - 1));
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
