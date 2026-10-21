using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>상점: 스킬 카드 · 아이템(무르기, 소모품 이용권) · 스킨(땅 타일 재질·색, 보드 테마)을 결정으로 산다.</summary>
    public static partial class Hud
    {
        private static readonly string[] ShopTabs = { "스킬 카드", "아이템", "스킨" };

        private static void DrawShopPanel(GameBootstrap gb)
        {
            Dim(0.6f);
            Nine(R(10, 36, RefW - 20, 904), "UI/panel", 28, Color.white);
            Text(R(24, 48, 300, 44), "상점", 30, Gold, TextAnchor.MiddleLeft, 0.7f);
            GemPill(R(RefW - 260, 50, 124, 40));
            if (Button(R(RefW - 128, 48, 104, 44), new Color(0.4f, 0.42f, 0.58f), "닫기", 18)) gb.OpenPanel(LobbyPanel.None);

            for (var i = 0; i < ShopTabs.Length; i++)
            {
                var r = R(24 + i * 166, 100, 158, 48);
                if (Button(r, i == gb.ShopTab ? new Color(0.3f, 0.6f, 1f) : new Color(0.35f, 0.38f, 0.55f), ShopTabs[i], 19))
                {
                    gb.ShopTab = i;
                    gb.PanelMessage = "";
                }
            }

            if (gb.ShopTab == 0) DrawShopCards(gb);
            else if (gb.ShopTab == 1) DrawShopItems(gb);
            else DrawShopSkins(gb);

            Text(R(24, 884, RefW - 48, 40), gb.PanelMessage, 16, Gold, TextAnchor.MiddleCenter, 0.5f);
            SwallowInput();
        }

        private static void NotEnough(GameBootstrap gb, int price) =>
            gb.PanelMessage = $"결정이 {price - Progress.Gems}개 모자라요. 판을 하거나 퀘스트를 깨서 모으세요.";

        // ---------------------------------------------------------------- 스킬 카드

        private static void DrawShopCards(GameBootstrap gb)
        {
            Text(R(24, 154, RefW - 48, 24), $"한 종류에 {CardShop.MaxCopies}장까지 가질 수 있어요. 산 카드는 덱에 빈자리가 있으면 바로 들어가요.", 13, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft, 0f);
            var cards = CardShop.All;
            for (var i = 0; i < cards.Length; i++)
            {
                var e = cards[i];
                var r = R(24 + (i % 3) * 166, 186 + (i / 3) * 228, 158, 218);
                var owned = Progress.Cards.Owned(e.Card);
                Nine(r, "UI/panel", 28, owned > 0 ? Color.white : new Color(0.75f, 0.78f, 0.9f));
                var face = new Rect(r.x + 29 * _s, r.y + 10 * _s, 100 * _s, 100 * _s);
                Image(face, IconFor(e.Card), owned > 0 ? Color.white : new Color(1f, 1f, 1f, 0.55f));
                Text(new Rect(r.x + 4 * _s, r.y + 112 * _s, r.width - 8 * _s, 28 * _s), Palette.Name(e.Card), 17, Color.white);
                Text(new Rect(r.x + 4 * _s, r.y + 138 * _s, r.width - 8 * _s, 22 * _s), $"가짐 {owned}/{CardShop.MaxCopies}  ·  Lv{Progress.Cards.Level(e.Card)}", 13, Soft, TextAnchor.MiddleCenter, 0f);
                var buy = new Rect(r.x + 12 * _s, r.yMax - 54 * _s, r.width - 24 * _s, 44 * _s);
                GUI.enabled = Progress.Cards.CanBuy(e.Card);
                if (GUI.enabled ? PriceButton(buy, new Color(1f, 0.55f, 0.2f), "", e.Price, 17) : Button(buy, new Color(0.45f, 0.47f, 0.6f), "다 가짐", 16))
                    Buy(gb, e.Card, e.Price);
                GUI.enabled = true;
            }
        }

        // ---------------------------------------------------------------- 아이템

        private static void DrawShopItems(GameBootstrap gb)
        {
            Text(R(24, 154, RefW - 48, 24), "이용권을 사 두면 출격 준비에서 결정 대신 써요. 무르기는 판 중에 손패 위 버튼으로 써요.", 13, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft, 0f);
            var y = 188f;
            void Row(string icon, string name, string note, string have, string prefix, int price, System.Func<bool> buy, string done)
            {
                var r = R(24, y, RefW - 48, 96);
                Nine(r, "UI/panel", 28, Color.white);
                Image(new Rect(r.x + 12 * _s, r.y + 14 * _s, 68 * _s, 68 * _s), icon, Color.white);
                Text(new Rect(r.x + 90 * _s, r.y + 10 * _s, 250 * _s, 32 * _s), name, 20, Color.white, TextAnchor.MiddleLeft);
                Text(new Rect(r.x + 90 * _s, r.y + 42 * _s, 250 * _s, 22 * _s), note, 13, Soft, TextAnchor.MiddleLeft, 0f);
                Text(new Rect(r.x + 90 * _s, r.y + 64 * _s, 250 * _s, 22 * _s), have, 14, Gold, TextAnchor.MiddleLeft, 0f);
                if (PriceButton(new Rect(r.xMax - 142 * _s, r.y + 22 * _s, 130 * _s, 52 * _s), new Color(1f, 0.55f, 0.2f), prefix, price, 17))
                {
                    if (buy()) gb.PanelMessage = done;
                    else NotEnough(gb, price);
                }
                y += 104;
            }
            Row("UI/icon_quest", "무르기", "판 중에 내 지난 수를 되돌려요", $"보유 {Progress.Undos}", "", BoostShop.UndoPrice, Progress.BuyUndo, "무르기 1개를 샀어요.");
            Row("UI/icon_quest", $"무르기 {BoostShop.UndoPack}개 묶음", $"{BoostShop.UndoPack}개를 {BoostShop.UndoPackPrice / BoostShop.UndoPrice}개 값에", $"보유 {Progress.Undos}", "", BoostShop.UndoPackPrice, Progress.BuyUndoPack, $"무르기 {BoostShop.UndoPack}개를 샀어요.");
            foreach (var b in BoostShop.All)
            {
                var entry = b;
                Row("UI/star_on", $"{b.Name} 이용권", b.Text, $"보유 {Progress.Tickets(b.Boost)}장", "", b.Price, () => Progress.BuyTicket(entry), $"{b.Name} 이용권을 샀어요. 출격 준비에서 켜면 써요.");
            }
        }

        // ---------------------------------------------------------------- 스킨

        private static readonly string[] SkinTabs = { "땅 재질", "땅 색", "보드 테마" };

        private static void DrawShopSkins(GameBootstrap gb)
        {
            TileSkins.Split(Progress.TileSkin, out var curM, out var curC);
            // 지금 내 땅 미리보기
            var pv = R(24, 156, RefW - 48, 92);
            Nine(pv, "UI/panel", 28, Color.white);
            for (var k = 0; k < 3; k++)
                Image(new Rect(pv.x + (12 + k * 52) * _s, pv.y + (16 + (k % 2) * 12) * _s, 72 * _s, 55 * _s), TileSkins.Sprite(curM, curC), Color.white);
            Text(new Rect(pv.x + 210 * _s, pv.y + 10 * _s, pv.width - 220 * _s, 34 * _s), $"내 땅: {TileSkins.Material(curM).Name} · {TileSkins.Color(curC).Name}", 19, Color.white, TextAnchor.MiddleLeft);
            Text(new Rect(pv.x + 210 * _s, pv.y + 44 * _s, pv.width - 220 * _s, 40 * _s), "재질과 색을 따로 골라 맞춰요. 2:2 대전에서는 팀 색이 파랑·빨강으로 고정되고 재질만 보여요.", 12, Soft, TextAnchor.UpperLeft, 0f);

            for (var i = 0; i < SkinTabs.Length; i++)
            {
                var r = R(24 + i * 166, 256, 158, 42);
                if (Button(r, i == gb.SkinTab ? new Color(0.6f, 0.45f, 0.85f) : new Color(0.35f, 0.38f, 0.55f), SkinTabs[i], 17))
                {
                    gb.SkinTab = i;
                    gb.PanelMessage = "";
                }
            }

            if (gb.SkinTab == 2)
            {
                DrawThemeGrid(gb, 308);
                return;
            }
            var material = gb.SkinTab == 0;
            var parts = material ? TileSkins.Materials : TileSkins.Colors;
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var r = R(24 + (i % 4) * 124, 308 + (i / 4) * 214, 118, 204);
                var owned = Progress.SkinOwned(part, material);
                var using_ = material ? part.Id == curM : part.Id == curC;
                if (using_) Nine(Inset(r, -4), "UI/button", 22, Gold);
                Nine(r, "UI/panel", 28, owned ? Color.white : new Color(0.75f, 0.78f, 0.9f));
                var sprite = material ? TileSkins.Sprite(part.Id, curC) : TileSkins.Sprite(curM, part.Id);
                Image(new Rect(r.x + 9 * _s, r.y + 16 * _s, 100 * _s, 76 * _s), sprite, Color.white);
                Text(new Rect(r.x, r.y + 100 * _s, r.width, 30 * _s), part.Name, 18, Color.white);
                var btn = new Rect(r.x + 8 * _s, r.yMax - 56 * _s, r.width - 16 * _s, 46 * _s);
                if (using_) Text(btn, "사용 중", 16, Gold);
                else if (owned)
                {
                    if (Button(btn, new Color(0.3f, 0.7f, 0.45f), "사용하기", 15))
                        gb.UseSkin(material ? TileSkins.Join(part.Id, curC) : TileSkins.Join(curM, part.Id));
                }
                else if (PriceButton(btn, new Color(1f, 0.55f, 0.2f), "", part.Price, 16))
                {
                    if (Progress.BuySkin(part, material))
                    {
                        gb.UseSkin(material ? TileSkins.Join(part.Id, curC) : TileSkins.Join(curM, part.Id));
                        gb.PanelMessage = $"{part.Name}{(material ? " 재질" : "")}을(를) 샀어요!";
                    }
                    else NotEnough(gb, part.Price);
                }
            }
        }
    }
}
