using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// IMGUI HUD. 기준 화면 540x960(세로)의 좌표로 배치하고, 실제 화면에 맞춰 사각형과 글자 크기를 직접 키운다
    /// (GUI.matrix 로 확대하면 글자가 흐려지므로 쓰지 않음). 한글 표시를 위해 Resources/Fonts/Pretendard-Bold 를 쓴다.
    /// 스킨은 Resources/Art/UI (art/ui.mjs, 기준 좌표의 2배 해상도). 패널·버튼·카드 틀은 Nine(...) 으로 9-slice.
    /// 화면: 대기실(DrawLobby) / 전투(DrawTop + DrawHand + DrawResult).
    /// </summary>
    public static partial class Hud
    {
        private const float RefW = 540f, RefH = 960f;
        private const float TexScale = 0.5f; // UI 텍스처는 기준 좌표의 2배로 그려져 있다

        private static float _s = 1f, _ox, _oy;
        private static Font _font;
        private static float _retryArmedUntil; // 전투 상단 '다시' 버튼: 이 시각까지 한 번 더 누르면 다시 시작
        private static GUIStyle _label, _button, _rich;
        private static readonly Color Ink = new(0.24f, 0.16f, 0.08f);      // 밝은 말풍선 위 글자
        private static readonly Color InkSoft = new(0.5f, 0.38f, 0.24f);

        private static readonly Color Gold = new(1f, 0.88f, 0.3f);
        private static readonly Color Soft = new(1f, 0.98f, 0.85f);

        public static void Draw(GameBootstrap gb)
        {
            Layout();
            if (gb.Mode == AppScreen.Splash)
            {
                DrawSplash(gb);
                return;
            }
            if (gb.Mode == AppScreen.Tower)
            {
                DrawTower(gb);
                return;
            }
            if (gb.Mode == AppScreen.Login)
            {
                DrawLogin(gb);
                return;
            }
            if (gb.Mode == AppScreen.PvpLobby)
            {
                DrawPvpLobby(gb);
                return;
            }
            if (gb.Mode == AppScreen.PvpRoom)
            {
                DrawPvpRoom(gb);
                return;
            }
            if (gb.Mode == AppScreen.Lobby || gb.Mode == AppScreen.Home)
            {
                if (gb.Panel == LobbyPanel.None || Event.current.type == EventType.Repaint)
                {
                    if (gb.Mode == AppScreen.Home) DrawHome(gb);
                    else DrawLobby(gb);
                }
                if (gb.Panel == LobbyPanel.Deck) DrawDeckPanel(gb);
                else if (gb.Panel == LobbyPanel.Quest) DrawQuestPanel(gb);
                else if (gb.Panel == LobbyPanel.Prep) DrawPrepPanel(gb);
                else if (gb.Panel == LobbyPanel.Road) DrawRoadPanel(gb);
                else if (gb.Panel == LobbyPanel.Account) DrawAccountPanel(gb);
                else if (gb.Panel == LobbyPanel.Shop) DrawShopPanel(gb);
                return;
            }

            var g = gb.Game;
            var e = Event.current;
            if (e.type == EventType.MouseMove || e.type == EventType.Repaint) gb.SetHover(e.mousePosition);

            if (gb.Tutorial) DrawCoach(gb);
            else if (gb.Puzzle != null) DrawPuzzleTop(gb, g);
            else if (gb.Pvp != null) DrawPvpTop(gb, g);
            else DrawTop(gb, g);
            DrawHand(gb, g);
            DrawCine(gb);
            if (gb.Pvp != null)
            {
                DrawEmotes(gb);
                DrawMyTurnBanner(gb);
                if (gb.EmoteOpen && !gb.BattleOver) DrawEmotePalette(gb);
            }
            if (gb.Tutorial) DrawFinger(gb);
            if (gb.Concept != null)
            {
                DrawConcept(gb, gb.Concept);
                return;
            }
            if (gb.ActiveTip != null)
            {
                DrawTip(gb, gb.ActiveTip);
                return;
            }
            if (g.IsOver && !gb.Tutorial && gb.Puzzle != null)
            {
                DrawPuzzleResult(gb, g);
                return;
            }
            if (gb.Pvp != null && gb.BattleOver)
            {
                DrawPvpResult(gb, g);
                return;
            }
            if (g.IsOver && !gb.Tutorial)
            {
                DrawResult(gb, g);
                return;
            }

            // 버튼·카드가 쓰지 않은 입력은 보드로
            switch (e.type)
            {
                case EventType.MouseDown:
                    gb.PointerDown(e.mousePosition);
                    e.Use();
                    break;
                case EventType.MouseDrag:
                    gb.PointerMove(e.mousePosition);
                    e.Use();
                    break;
                case EventType.MouseUp:
                    gb.PointerUp(e.mousePosition);
                    e.Use();
                    break;
            }
        }

        /// <summary>IMGUI 화면 좌표 → 기준 좌표(540x960).</summary>
        public static Vector2 ToRef(Vector2 gui) => new((gui.x - _ox) / _s, (gui.y - _oy) / _s);

        private static void Layout()
        {
            _s = Mathf.Min(UnityEngine.Screen.width / RefW, UnityEngine.Screen.height / RefH);
            _ox = (UnityEngine.Screen.width - RefW * _s) * 0.5f;
            _oy = (UnityEngine.Screen.height - RefH * _s) * 0.5f;
            if (_font == null) _font = Resources.Load<Font>("Fonts/Pretendard-Bold");
            _label ??= new GUIStyle { font = _font, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = Color.white } };
            _button ??= new GUIStyle(_label);
            _rich ??= new GUIStyle(_label) { richText = true };
        }

        // ---------------------------------------------------------------- 그리기 도구

        private static Rect R(float x, float y, float w, float h) => new(_ox + x * _s, _oy + y * _s, w * _s, h * _s);

        private static Rect Inset(Rect r, float px) => new(r.x + px * _s, r.y + px * _s, r.width - 2 * px * _s, r.height - 2 * px * _s);

        /// <summary>9-slice: 텍스처 테두리 border(텍스처 px)는 늘리지 않고, 가운데만 늘린다.</summary>
        private static void Nine(Rect r, string tex, float border, Color tint)
        {
            var t = Art.Texture(tex);
            if (t == null || Event.current.type != EventType.Repaint) return;
            var prev = GUI.color;
            GUI.color = tint;
            var b = Mathf.Min(border * TexScale * _s, r.width * 0.5f, r.height * 0.5f);
            float u = border / t.width, v = border / t.height;
            // 조각 경계를 정수 픽셀에 맞춰야 반투명 조각이 겹쳐 그려진 세로줄·가로줄이 안 생긴다.
            float[] xs = { Mathf.Round(r.x), Mathf.Round(r.x + b), Mathf.Round(r.xMax - b), Mathf.Round(r.xMax) };
            float[] ys = { Mathf.Round(r.y), Mathf.Round(r.y + b), Mathf.Round(r.yMax - b), Mathf.Round(r.yMax) };
            float[] us = { 0f, u, 1f - u, 1f };
            float[] vs = { 1f, 1f - v, v, 0f }; // 텍스처 좌표는 아래에서 위
            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                {
                    var dst = Rect.MinMaxRect(xs[j], ys[i], xs[j + 1], ys[i + 1]);
                    if (dst.width <= 0f || dst.height <= 0f) continue;
                    var src = Rect.MinMaxRect(us[j], vs[i + 1], us[j + 1], vs[i]);
                    GUI.DrawTextureWithTexCoords(dst, t, src);
                }
            GUI.color = prev;
        }

        private static void Image(Rect r, string tex, Color tint)
        {
            var t = Art.Texture(tex);
            if (t == null) return;
            var prev = GUI.color;
            GUI.color = tint;
            GUI.DrawTexture(r, t, ScaleMode.ScaleToFit);
            GUI.color = prev;
        }

        /// <summary>리치 텍스트(&lt;color&gt;) 글자. 그림자 없이 (밝은 말풍선 위 진한 글자용).</summary>
        private static void Rich(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            _rich.fontSize = Mathf.RoundToInt(size * _s);
            _rich.alignment = anchor;
            _rich.normal.textColor = color;
            GUI.Label(r, text, _rich);
        }

        private static void Text(Rect r, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter, float shadow = 0.45f)
        {
            _label.fontSize = Mathf.RoundToInt(size * _s);
            _label.alignment = anchor;
            if (shadow > 0f)
            {
                _label.normal.textColor = new Color(0.05f, 0.08f, 0.2f, shadow * color.a);
                GUI.Label(new Rect(r.x + 1.5f * _s, r.y + 2f * _s, r.width, r.height), text, _label);
            }
            _label.normal.textColor = color;
            GUI.Label(r, text, _label);
        }

        private static bool Button(Rect r, Color tint)
        {
            var hot = GUI.enabled && r.Contains(Event.current.mousePosition);
            Nine(r, "UI/button", 22, GUI.enabled ? (hot ? Color.Lerp(tint, Color.white, 0.2f) : tint) : Color.Lerp(tint, Color.gray, 0.6f));
            return GUI.Button(r, GUIContent.none, _button);
        }

        private static bool Button(Rect r, Color tint, string label, int size)
        {
            var clicked = Button(r, tint);
            Text(r, label, size, GUI.enabled ? Color.white : new Color(0.85f, 0.85f, 0.9f));
            return clicked;
        }

        private static void Stars(Rect r, int stars, float size)
        {
            for (var i = 0; i < 3; i++)
            {
                var x = r.center.x + (i - 1) * size * _s * 1.05f - size * _s * 0.5f;
                Image(new Rect(x, r.center.y - size * _s * 0.5f, size * _s, size * _s), i < stars ? "UI/star_on" : "UI/star_off", Color.white);
            }
        }

        private static string LevelName(AiLevel l) => l switch { AiLevel.Beginner => "입문", AiLevel.Easy => "쉬움", AiLevel.Normal => "보통", _ => "어려움" };


        private static Color ModeColor(int m) => m == 0 ? new Color(0.35f, 0.8f, 0.45f) : m == 2 ? new Color(0.85f, 0.3f, 0.45f) : new Color(0.95f, 0.7f, 0.25f);

        private static string ModeName(int m) => StageCatalog.ModeNames[m] + " 모드";

        /// <summary>상대의 얼굴로 쓸 유닛 그림.</summary>
        private static string Portrait(StageDef s) => s.Persona.Id switch
        {
            "ignis" or "ignis+" => "Units/unit_red",
            "glacia" => "Units/unit_blue",
            "homunculus" or "homunculus+" => "Units/unit_black",
            _ => "Units/unit_grey",
        };

        // ---------------------------------------------------------------- 스플래시

        /// <summary>
        /// 게임 스플래시: 엠블럼이 통통 튀며 나타나고, 제목·부제가 차례로 떠오른 뒤 "화면을 터치해서 시작"이 깜빡인다.
        /// 아래쪽에는 원소 유닛이 놓인 장식 보드가 보인다 (GameBootstrap.ShowSplash). 아무 곳이나 누르면 다음으로.
        /// </summary>
        private static void DrawSplash(GameBootstrap gb)
        {
            var t = gb.SplashTime;
            float Appear(float start, float dur) => Mathf.Clamp01((t - start) / dur);

            // 엠블럼: 0.0~0.6초 튀어 오르며 등장, 이후 둥실둥실
            var k = Appear(0.05f, 0.6f);
            var bob = Mathf.Sin(t * 2f) * 5f;
            var size = 270f * EaseOutBack(k);
            Image(R(RefW * 0.5f - size * 0.5f, 28f + (270f - size) * 0.5f + bob, size, size), "UI/logo_emblem", new Color(1f, 1f, 1f, k));

            // 영문 로고 (art/logo.mjs): 엠블럼 뒤를 이어 통통 튀며 등장
            var ta = Appear(0.4f, 0.55f);
            var lw = 480f * EaseOutBack(ta);
            var lh = lw * 440f / 1024f;
            Image(R(RefW * 0.5f - lw * 0.5f, 268f + (206f - lh) * 0.5f, lw, lh), "UI/logo_title", new Color(1f, 1f, 1f, ta));

            var sa = Appear(0.8f, 0.5f);
            Text(R(0, 474 + (1f - sa) * 14f, RefW, 28), "원소를 섞어 땅을 빼앗아라!", 19, new Color(1f, 1f, 1f, sa), TextAnchor.MiddleCenter, 0.7f);

            // 터치 안내: 1.2초 뒤부터 깜빡임
            if (t > 1.2f)
            {
                var blink = 0.55f + 0.45f * Mathf.Sin((t - 1.2f) * 4f);
                var bar = R(110, 872, RefW - 220, 50);
                Nine(bar, "UI/panel", 28, new Color(1f, 1f, 1f, 0.8f * blink));
                Text(bar, "화면을 터치해서 시작", 20, new Color(1f, 1f, 1f, blink), TextAnchor.MiddleCenter, 0.5f);
            }
            Text(R(0, 930, RefW - 12, 22), $"v{Application.version}" + (Debug.isDebugBuild ? " · 개발 빌드" : ""), 12, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleRight, 0f);

            var e = Event.current;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseUp || (e.type == EventType.KeyDown && e.keyCode != KeyCode.None))
            {
                gb.FinishSplash();
                e.Use();
            }
        }

        private static float EaseOutBack(float k)
        {
            if (k >= 1f) return 1f;
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
        }

        // ---------------------------------------------------------------- 대기실

        private static void DrawLobby(GameBootstrap gb)
        {
            DebugKeys();
            var stage = StageCatalog.Get(gb.LobbyStage);
            var chapter = StageCatalog.Chapters[stage.Chapter];

            // 위 줄: 홈 · 제목 · 결정
            Fill(R(0, 0, RefW, 130), "UI/fade_top", Color.white);
            if (PillButton(R(14, 22, 92, 44), "← 홈", 17)) gb.ShowHome();
            Text(R(110, 22, RefW - 250, 44), "싱글 모드", 26, Gold, TextAnchor.MiddleCenter, 0.7f);
            var gems = R(RefW - 134, 22, 120, 44);
            GemPill(gems);
            if (GUI.Button(gems, GUIContent.none, GUIStyle.none)) gb.OpenPanel(LobbyPanel.Shop);

            // 챕터 탭
            for (var c = 0; c < StageCatalog.Chapters.Length; c++)
            {
                var r = R(14 + c * 104, 92, 96, 44);
                var open = Progress.IsUnlocked(c * StageCatalog.PerChapter);
                GUI.enabled = open;
                var label = open ? $"{c + 1}장" : $"{c + 1}장 잠김";
                if (c == stage.Chapter ? CardButton(r, new Color(0.36f, 0.62f, 1f), label, 19) : PillButton(r, label, 17)) gb.SelectLobbyChapter(c);
                GUI.enabled = true;
            }
            Text(R(16, 140, RefW - 32, 26), $"[{ModeName(Progress.Mode)}]  {stage.Chapter + 1}장 {chapter.Name} — {chapter.Theme}", 15, Soft);

            // (보드 미리보기는 카메라가 165~465 띠에 그린다)

            // 스테이지 정보
            var info = R(16, 470, RefW - 32, 158);
            Nine(info, "UI/panel", 28, Color.white);
            var face = new Rect(info.x + 10 * _s, info.y + 12 * _s, 70 * _s, 82 * _s);
            Image(face, Portrait(stage), Color.white);
            if (stage.IsBoss) Image(new Rect(face.x + 44 * _s, face.y - 6 * _s, 30 * _s, 30 * _s), "UI/crown", Color.white);
            var tx = info.x + 90 * _s;
            var tw = info.width - 100 * _s;
            Text(new Rect(tx, info.y + 8 * _s, tw, 30 * _s), $"{stage.Code}  {stage.Name}", 21, stage.IsBoss ? Gold : Color.white, TextAnchor.MiddleLeft);
            Text(new Rect(tx, info.y + 38 * _s, tw, 22 * _s), $"상대: {stage.Opponent}  ·  AI 난이도 {LevelName(gb.StageLevel(stage))}", 15, new Color(1f, 0.8f, 0.75f), TextAnchor.MiddleLeft, 0f);
            Text(new Rect(tx, info.y + 60 * _s, tw, 40 * _s), stage.OpponentNote, 14, Soft, TextAnchor.UpperLeft, 0f);
            Text(new Rect(info.x + 12 * _s, info.y + 104 * _s, info.width - 24 * _s, 44 * _s), "맵: " + stage.MapNote, 14, new Color(0.75f, 0.9f, 1f), TextAnchor.UpperLeft, 0f);

            // 스테이지 노드 (챕터 10개, 2줄)
            var first = stage.Chapter * StageCatalog.PerChapter;
            for (var k = 0; k < StageCatalog.PerChapter; k++)
            {
                var index = first + k;
                var s = StageCatalog.Get(index);
                var r = R(34 + (k % 5) * 96, 640 + (k / 5) * 100, 88, 92);
                var open = Progress.IsUnlocked(index);
                if (index == gb.LobbyStage)
                {
                    Image(Inset(r, -18), "UI/glow", new Color(1f, 0.85f, 0.4f, 0.6f));
                    Nine(Inset(r, -4), "UI/button", 22, Gold);
                }
                var tint = !open ? new Color(0.3f, 0.32f, 0.42f) : s.IsBoss ? new Color(0.85f, 0.35f, 0.4f) : new Color(0.3f, 0.55f, 0.95f);
                if (CardButton(r, tint) && open) gb.SelectLobbyStage(index);
                if (open)
                {
                    Text(new Rect(r.x, r.y + 8 * _s, r.width, 36 * _s), s.Code, 22, Color.white);
                    Stars(new Rect(r.x, r.y + 52 * _s, r.width, 26 * _s), Progress.StarsOf(index), 22);
                }
                else Image(Inset(r, 20), "UI/icon_lock", new Color(1f, 1f, 1f, 0.7f));
                if (s.IsBoss) Image(new Rect(r.xMax - 26 * _s, r.y - 10 * _s, 30 * _s, 30 * _s), "UI/crown", Color.white);
            }

            // 모드 + 퀘스트 + 출격
            Fill(R(0, 760, RefW, 200), "UI/fade_bottom", Color.white);
            var mode = Progress.Mode;
            if (CardButton(R(14, 854, 140, 76), ModeColor(mode))) gb.CycleMode();
            Text(R(14, 862, 140, 26), "모드", 14, Color.white);
            Text(R(14, 888, 140, 34), StageCatalog.ModeNames[mode], 22, Color.white);
            if (Time.time < gb.ModeHintUntil && mode + 1 < StageCatalog.Modes)
                Text(R(0, 832, RefW, 22), $"{ModeName(mode)} 5-10 을 깨면 {ModeName(mode + 1)}가 열려요", 14, Gold, TextAnchor.MiddleCenter, 0.7f);
            if (IconButton(R(162, 854, 120, 76), new Color(0.6f, 0.45f, 0.85f), "UI/icon_quest", "퀘스트")) gb.OpenPanel(LobbyPanel.Quest);
            Badge(R(262, 846, 30, 30), Progress.QuestsReady());
            if (Time.time < gb.ToastUntil) Text(R(0, 808, RefW, 22), gb.Toast, 14, Gold, TextAnchor.MiddleCenter, 0.7f);
            GUI.enabled = Progress.IsUnlocked(gb.LobbyStage);
            if (CardButton(R(290, 854, 236, 76), new Color(1f, 0.55f, 0.2f), "출격!", 30, true)) gb.OpenPanel(LobbyPanel.Prep);
            GUI.enabled = true;
        }

        /// <summary>아이콘 + 글자 버튼 (대기실 아래 줄).</summary>
        private static bool IconButton(Rect r, Color tint, string icon, string label)
        {
            var clicked = CardButton(r, tint);
            Image(new Rect(r.center.x - 20 * _s, r.y + 6 * _s, 40 * _s, 40 * _s), icon, Color.white);
            Text(new Rect(r.x, r.yMax - 30 * _s, r.width, 26 * _s), label, 16, Color.white);
            return clicked;
        }

        /// <summary>보유 결정 표시.</summary>
        private static void GemPill(Rect r)
        {
            Nine(r, "UI/pill", 22, Color.white);
            Image(new Rect(r.x + 6 * _s, r.y + (r.height - 34 * _s) * 0.5f, 34 * _s, 34 * _s), "UI/icon_gem", Color.white);
            Text(new Rect(r.x + 38 * _s, r.y, r.width - 44 * _s, r.height), Progress.Gems.ToString(), 18, Color.white);
        }

        /// <summary>결정 아이콘 + 값을 얹은 버튼.</summary>
        private static bool PriceButton(Rect r, Color tint, string prefix, int price, int size)
        {
            var clicked = Button(r, tint);
            var label = string.IsNullOrEmpty(prefix) ? price.ToString() : $"{prefix} {price}";
            var icon = size * 1.25f * _s;
            var textW = (label.Length * size * 0.62f) * _s;
            var x = r.center.x - (icon + textW) * 0.5f;
            Image(new Rect(x, r.center.y - icon * 0.5f, icon, icon), "UI/icon_gem", GUI.enabled ? Color.white : new Color(1f, 1f, 1f, 0.5f));
            Text(new Rect(x + icon, r.y, textW + 4 * _s, r.height), label, size, Color.white, TextAnchor.MiddleLeft);
            return clicked;
        }

        private static void Dim(float alpha)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0.05f, alpha);
            GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        /// <summary>창이 떠 있는 동안 아래로 입력이 새지 않게.</summary>
        private static void SwallowInput()
        {
            var e = Event.current;
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp) e.Use();
        }

        // ---------------------------------------------------------------- 덱 편성

        /// <summary>
        /// 덱 편성: 스킬 카드 9종을 3x3 으로. 가진 카드는 −/+ 로 덱에 넣고 빼고, 덜 가진 카드는 결정으로 산다.
        /// 유닛은 원소별 4장 고정이고 스킬은 Collection.Slots 장까지 (기본 9, 별의 길 보상으로 늘어남).
        /// </summary>
        private static void DrawDeckPanel(GameBootstrap gb)
        {
            Dim(0.6f);
            var cards = Progress.Cards;
            Nine(R(10, 36, RefW - 20, 904), "UI/panel", 28, Color.white);
            Text(R(24, 48, 300, 44), "덱 편성", 30, Gold, TextAnchor.MiddleLeft, 0.7f);
            GemPill(R(RefW - 150, 50, 124, 40));
            var units = CardShop.UnitsPerElement * Elements.Base.Length;
            Text(R(24, 96, RefW - 48, 24), $"유닛 {units}장(원소별 {CardShop.UnitsPerElement}장) + 스킬 {cards.Skills.Count}/{cards.Slots}장 = 총 {units + cards.Skills.Count}장", 15, Soft, TextAnchor.MiddleLeft, 0f);
            Text(R(24, 120, RefW - 48, 22), $"결정으로 카드를 사고 강화하세요. 같은 카드는 {CardShop.MaxCopies}장까지, 강화는 Lv{CardShop.MaxLevel}까지.", 13, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft, 0f);

            const float cw = 160f, ch = 212f, gap = 8f;
            var x0 = (RefW - (3 * cw + 2 * gap)) * 0.5f;
            for (var i = 0; i < CardShop.All.Length; i++)
                DrawShopCard(gb, CardShop.All[i], R(x0 + i % 3 * (cw + gap), 150 + i / 3 * (ch + gap), cw, ch));

            if (PillButton(R(36, 814, 220, 56), "가진 카드로 채우기", 17))
            {
                cards.FillDefault();
                Progress.SaveCards();
                gb.PanelMessage = "가진 카드로 덱을 다시 채웠어요.";
            }
            if (Button(R(284, 814, 220, 56), new Color(1f, 0.55f, 0.2f), "완료", 22)) gb.OpenPanel(LobbyPanel.None);
            Text(R(24, 874, RefW - 48, 50), gb.PanelMessage, 15, Gold, TextAnchor.MiddleCenter, 0f);
            SwallowInput();
        }

        private static void DrawShopCard(GameBootstrap gb, CardShop.Entry entry, Rect r)
        {
            var cards = Progress.Cards;
            var card = entry.Card;
            var owned = cards.Owned(card);
            var inDeck = cards.InDeck(card);
            var level = cards.Level(card);

            if (inDeck > 0) Nine(Inset(r, -4), "UI/button", 22, new Color(1f, 0.85f, 0.3f));
            Nine(r, "UI/card_skill", 28, owned > 0 ? Color.white : new Color(0.6f, 0.6f, 0.68f));
            var art = new Rect(r.x + 10 * _s, r.y + 8 * _s, r.width - 20 * _s, 52 * _s);
            Image(art, "UI/card_art", Color.Lerp(Palette.Of(card), Color.white, 0.25f));
            Image(Inset(art, 3), IconFor(card), owned > 0 ? Color.white : new Color(1f, 1f, 1f, 0.55f));
            if (owned == 0) Image(new Rect(art.xMax - 26 * _s, art.y + 2 * _s, 24 * _s, 24 * _s), "UI/icon_lock", Color.white);
            else LevelBadge(new Rect(art.x + 3 * _s, art.y + 3 * _s, 38 * _s, 20 * _s), level);
            Text(new Rect(r.x, r.y + 62 * _s, r.width, 22 * _s), Palette.Name(card), 16, Color.white, TextAnchor.MiddleCenter, 0f);
            var hint = level > 1 ? $"Lv{level}: " + CardShop.LevelText(card)[level - 1] : Palette.Hint(card);
            Text(new Rect(r.x + 4 * _s, r.y + 84 * _s, r.width - 8 * _s, 20 * _s), hint, 11, level > 1 ? Gold : new Color(0.78f, 0.8f, 0.95f), TextAnchor.MiddleCenter, 0f);
            Text(new Rect(r.x, r.y + 104 * _s, r.width, 20 * _s), owned == 0 ? "아직 없음" : $"덱 {inDeck}  ·  보유 {owned}/{CardShop.MaxCopies}", 13, owned == 0 ? new Color(0.85f, 0.85f, 0.9f) : Soft, TextAnchor.MiddleCenter, 0f);

            // 강화 (Lv1 → MaxLevel). 실수로 결정을 쓰지 않게, 처음 누르면 효과를 보여 주고 한 번 더 누르면 강화한다.
            var up = new Rect(r.x + 8 * _s, r.y + 170 * _s, r.width - 16 * _s, 34 * _s);
            if (owned > 0 && level < CardShop.MaxLevel)
            {
                var price = CardShop.UpgradePrice(entry, level + 1);
                var armed = _upgradeArmed.HasValue && _upgradeArmed.Value.Equals(card) && Time.unscaledTime < _upgradeArmedUntil;
                if (PriceButton(up, armed ? new Color(0.95f, 0.45f, 0.3f) : new Color(0.62f, 0.42f, 0.95f), armed ? "확인" : $"Lv{level + 1}", price, 14))
                {
                    var next = $"{Palette.Name(card)} Lv{level + 1}: {CardShop.LevelText(card)[level]}";
                    if (!armed)
                    {
                        _upgradeArmed = card;
                        _upgradeArmedUntil = Time.unscaledTime + 4f;
                        gb.PanelMessage = next + "\n한 번 더 누르면 강화해요.";
                    }
                    else
                    {
                        _upgradeArmed = null;
                        gb.PanelMessage = Progress.UpgradeCard(card) ? "강화 완료! " + next : $"결정이 {price - Progress.Gems}개 모자라요.";
                    }
                }
            }
            else if (owned > 0) Text(up, $"강화 완료 (Lv{CardShop.MaxLevel})", 13, Gold, TextAnchor.MiddleCenter, 0f);

            var y = r.y + 128 * _s;
            var h = 38 * _s;
            if (owned == 0)
            {
                if (PriceButton(new Rect(r.x + 8 * _s, y, r.width - 16 * _s, h), new Color(0.95f, 0.55f, 0.25f), "해금", entry.Price, 16))
                    Buy(gb, card, entry.Price);
                return;
            }
            GUI.enabled = inDeck > 0;
            if (Button(new Rect(r.x + 6 * _s, y, 40 * _s, h), new Color(0.45f, 0.48f, 0.65f), "−", 24))
            {
                cards.Remove(card);
                Progress.SaveCards();
                gb.PanelMessage = "";
            }
            GUI.enabled = cards.CanAdd(card);
            if (Button(new Rect(r.x + 50 * _s, y, 40 * _s, h), new Color(0.3f, 0.6f, 1f), "+", 24))
            {
                cards.Add(card);
                Progress.SaveCards();
                gb.PanelMessage = "";
            }
            GUI.enabled = true;
            if (owned < CardShop.MaxCopies)
            {
                if (PriceButton(new Rect(r.x + 94 * _s, y, r.width - 100 * _s, h), new Color(0.95f, 0.55f, 0.25f), "", entry.Price, 14))
                    Buy(gb, card, entry.Price);
            }
            else Text(new Rect(r.x + 94 * _s, y, r.width - 100 * _s, h), "최대", 14, new Color(0.85f, 0.85f, 0.9f), TextAnchor.MiddleCenter, 0f);
        }

        private static Card? _upgradeArmed;
        private static float _upgradeArmedUntil;

        private static void LevelBadge(Rect r, int level)
        {
            Nine(r, "UI/button", 22, level >= CardShop.MaxLevel ? new Color(1f, 0.6f, 0.2f) : level > 1 ? new Color(0.62f, 0.42f, 0.95f) : new Color(0.35f, 0.38f, 0.5f));
            Text(r, $"Lv{level}", 12, Color.white, TextAnchor.MiddleCenter, 0f);
        }

        // ---------------------------------------------------------------- 출격 준비

        /// <summary>출격 준비: 이번 판에만 쓰는 소모품을 고르고(출격할 때 값을 낸다), 판 중에 쓸 무르기를 사 둔다.</summary>
        private static void DrawPrepPanel(GameBootstrap gb)
        {
            Dim(0.6f);
            var stage = StageCatalog.Get(gb.LobbyStage);
            Nine(R(20, 150, RefW - 40, 650), "UI/panel", 28, Color.white);
            Text(R(36, 162, 300, 44), "출격 준비", 30, Gold, TextAnchor.MiddleLeft, 0.7f);
            GemPill(R(RefW - 160, 166, 124, 40));
            Text(R(36, 208, RefW - 72, 24), $"{stage.Code} {stage.Name} — 소모품은 이번 판에만 쓰고, 출격할 때 결정을 내요.", 13, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft, 0f);

            for (var i = 0; i < BoostShop.All.Length; i++)
            {
                var b = BoostShop.All[i];
                var on = gb.Boosts.Contains(b.Boost);
                var r = R(36, 240 + i * 90, RefW - 72, 82);
                if (Button(r, on ? new Color(0.3f, 0.72f, 0.42f) : new Color(0.4f, 0.42f, 0.58f)))
                {
                    if (on) gb.Boosts.Remove(b.Boost); else gb.Boosts.Add(b.Boost);
                    gb.PanelMessage = "";
                }
                var box = new Rect(r.x + 14 * _s, r.center.y - 18 * _s, 36 * _s, 36 * _s);
                Nine(box, "UI/panel", 28, Color.white);
                if (on) Image(Inset(box, 3), "UI/star_on", Color.white);
                Text(new Rect(r.x + 62 * _s, r.y + 10 * _s, 260 * _s, 30 * _s), b.Name, 20, Color.white, TextAnchor.MiddleLeft);
                Text(new Rect(r.x + 62 * _s, r.y + 44 * _s, 300 * _s, 26 * _s), b.Text, 14, Soft, TextAnchor.MiddleLeft, 0f);
                var price = new Rect(r.xMax - 96 * _s, r.y, 84 * _s, r.height);
                var tickets = Progress.Tickets(b.Boost);
                if (tickets > 0)
                    Text(new Rect(price.x - 20 * _s, r.y, 104 * _s, r.height), $"이용권\n{tickets}장", 16, Gold, TextAnchor.MiddleCenter);
                else
                {
                    Image(new Rect(price.x, r.center.y - 15 * _s, 30 * _s, 30 * _s), "UI/icon_gem", Color.white);
                    Text(new Rect(price.x + 30 * _s, r.y, 54 * _s, r.height), b.Price.ToString(), 20, Color.white, TextAnchor.MiddleLeft);
                }
            }

            // 무르기: 모아 두고 판 중에 쓴다 (손패 위 무르기 버튼)
            var u = R(36, 240 + BoostShop.All.Length * 90, RefW - 72, 82);
            Nine(u, "UI/panel", 28, new Color(0.75f, 0.95f, 1f));
            Text(new Rect(u.x + 18 * _s, u.y + 10 * _s, 260 * _s, 30 * _s), $"무르기  (보유 {Progress.Undos})", 20, Color.white, TextAnchor.MiddleLeft);
            Text(new Rect(u.x + 18 * _s, u.y + 44 * _s, 320 * _s, 26 * _s), "판 중에 내 지난 수를 되돌려요", 14, Soft, TextAnchor.MiddleLeft, 0f);
            if (PriceButton(new Rect(u.xMax - 130 * _s, u.y + 16 * _s, 118 * _s, 50 * _s), new Color(0.3f, 0.65f, 0.8f), "+1", BoostShop.UndoPrice, 16))
                gb.PanelMessage = Progress.BuyUndo() ? $"무르기를 샀어요 (보유 {Progress.Undos})" : $"결정이 {BoostShop.UndoPrice - Progress.Gems}개 모자라요.";

            var cost = gb.BoostCost;
            Text(R(36, 616, RefW - 72, 30), cost > 0 ? $"고른 소모품: 결정 {cost}개" : "소모품 없이 출격해도 돼요", 16, cost > 0 ? Gold : Soft, TextAnchor.MiddleCenter, 0f);
            Text(R(36, 648, RefW - 72, 44), gb.PanelMessage, 15, Gold, TextAnchor.MiddleCenter, 0f);

            if (PillButton(R(46, 704, 200, 66), "취소", 22)) gb.OpenPanel(LobbyPanel.None);
            var go = R(262, 704, 232, 66);
            if (cost > 0 ? PriceButton(go, new Color(1f, 0.55f, 0.2f), "출격", cost, 24) : Button(go, new Color(1f, 0.55f, 0.2f), "출격!", 28))
                gb.Sortie();
            SwallowInput();
        }

        private static void Buy(GameBootstrap gb, Card card, int price)
        {
            if (Progress.BuyCard(card))
                gb.PanelMessage = $"{Palette.Name(card)} 카드를 얻었어요!" + (Progress.Cards.InDeck(card) < Progress.Cards.Owned(card) ? " (덱이 꽉 차서 보관함에 넣었어요)" : "");
            else
                gb.PanelMessage = $"결정이 {price - Progress.Gems}개 모자라요. 판을 하거나 퀘스트를 깨서 모으세요.";
        }

        // ---------------------------------------------------------------- 퀘스트

        /// <summary>퀘스트: 누적 기록으로 채우는 단계형 목표. 채우면 받기 버튼으로 결정을 받고 다음 단계가 열린다.</summary>
        private static void DrawQuestPanel(GameBootstrap gb)
        {
            Dim(0.6f);
            Nine(R(10, 36, RefW - 20, 904), "UI/panel", 28, Color.white);
            Text(R(24, 48, 300, 44), "퀘스트", 30, Gold, TextAnchor.MiddleLeft, 0.7f);
            GemPill(R(RefW - 150, 50, 124, 40));
            Text(R(24, 98, RefW - 48, 24), "목표를 채우면 결정을 받고, 받은 단계마다 별의 길 별이 1개씩 쌓여요.", 13, new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft, 0f);

            var lines = QuestCatalog.Lines;
            for (var i = 0; i < lines.Length; i++)
            {
                var q = lines[i];
                var r = R(22, 130 + i * 76, RefW - 44, 70);
                var claimed = Progress.QuestClaimed(q);
                var finished = q.Finished(claimed);
                var ready = Progress.QuestReady(q);
                Nine(r, "UI/panel", 28, ready ? new Color(1f, 0.92f, 0.6f) : finished ? new Color(0.7f, 0.72f, 0.8f) : Color.white);

                var title = finished ? q.Title(claimed) + " — 모두 완료" : q.Title(claimed);
                Text(new Rect(r.x + 14 * _s, r.y + 6 * _s, r.width - 150 * _s, 26 * _s), title, 16, Color.white, TextAnchor.MiddleLeft);
                Text(new Rect(r.xMax - 210 * _s, r.y + 6 * _s, 80 * _s, 26 * _s), $"{Mathf.Min(claimed + 1, q.Targets.Length)}/{q.Targets.Length}단계", 12, Soft, TextAnchor.MiddleRight, 0f);

                var target = q.Targets[Mathf.Min(claimed, q.Targets.Length - 1)];
                var cur = finished ? target : Mathf.Min(Progress.StatOf(q.Stat), target);
                var bar = new Rect(r.x + 14 * _s, r.y + 38 * _s, r.width - 150 * _s, 22 * _s);
                Nine(bar, "UI/bar_frame", 30, Color.white);
                var inner = Inset(bar, 4);
                var fw = inner.width * cur / Mathf.Max(1, target);
                if (fw > 1f) Nine(new Rect(inner.x, inner.y, Mathf.Max(fw, inner.height), inner.height), "UI/bar_fill", 22, ready || finished ? Gold : new Color(0.4f, 0.75f, 1f));
                Text(bar, $"{cur} / {target}", 12, Color.white, TextAnchor.MiddleCenter, 0.7f);

                var btn = new Rect(r.xMax - 124 * _s, r.y + 10 * _s, 112 * _s, 50 * _s);
                if (finished)
                {
                    Text(btn, "완료", 18, new Color(0.85f, 0.85f, 0.9f));
                    continue;
                }
                GUI.enabled = ready;
                if (PriceButton(btn, new Color(1f, 0.55f, 0.2f), ready ? "받기" : "", q.Rewards[claimed], 15))
                {
                    var got = Progress.ClaimQuest(q);
                    if (got > 0) gb.PanelMessage = $"결정 {got}개를 받았어요!";
                }
                GUI.enabled = true;
            }

            // 모두 받기: 받을 보상이 있는 퀘스트를 한 번에 (넘은 단계까지 이어서)
            var readyCount = Progress.QuestsReady();
            GUI.enabled = readyCount > 0;
            if (Button(R(50, 822, 210, 56), new Color(0.3f, 0.75f, 0.4f), readyCount > 0 ? $"모두 받기 ({readyCount})" : "모두 받기", 20))
            {
                var got = Progress.ClaimAllQuests(out var claims);
                if (got > 0) gb.PanelMessage = $"보상 {claims}개, 결정 {got}개를 받았어요!";
            }
            GUI.enabled = true;
            if (Button(R(280, 822, 210, 56), new Color(1f, 0.55f, 0.2f), "닫기", 22)) gb.OpenPanel(LobbyPanel.None);
            Text(R(24, 880, RefW - 48, 44), gb.PanelMessage, 16, Gold, TextAnchor.MiddleCenter, 0f);
            SwallowInput();
        }

        // ---------------------------------------------------------------- 별의 길·보드 테마

        /// <summary>
        /// 별의 길: 모든 곳에서 모은 별(스테이지 모든 모드·첨탑·퀘스트)과 칭호, 단계 보상(한 쪽에 7개씩), 보드 테마 상점.
        /// </summary>
        private static void DrawRoadPanel(GameBootstrap gb)
        {
            Dim(0.6f);
            Nine(R(10, 36, RefW - 20, 904), "UI/panel", 28, Color.white);
            Text(R(24, 48, 300, 44), "별의 길", 30, Gold, TextAnchor.MiddleLeft, 0.7f);
            GemPill(R(RefW - 150, 50, 124, 40));

            // 칭호와 별
            var stars = Progress.AllStars();
            var card = R(22, 98, RefW - 44, 132);
            Nine(card, "UI/panel", 28, new Color(1f, 0.92f, 0.7f));
            Image(new Rect(card.x + 12 * _s, card.y + 14 * _s, 64 * _s, 64 * _s), "UI/star_on", Color.white);
            var rank = StarRoad.RankIndex(stars);
            Text(new Rect(card.x + 86 * _s, card.y + 8 * _s, card.width - 96 * _s, 32 * _s), StarRoad.Ranks[rank].Title, 23, Gold, TextAnchor.MiddleLeft);
            Text(new Rect(card.x + 86 * _s, card.y + 40 * _s, card.width - 96 * _s, 26 * _s), $"별 {stars} / {StarRoad.MaxStars}", 17, Color.white, TextAnchor.MiddleLeft);
            var bar = new Rect(card.x + 86 * _s, card.y + 70 * _s, card.width - 100 * _s, 22 * _s);
            Nine(bar, "UI/bar_frame", 30, Color.white);
            var inner = Inset(bar, 4);
            var hasNext = rank + 1 < StarRoad.Ranks.Length;
            var from = StarRoad.Ranks[rank].Stars;
            var to = hasNext ? StarRoad.Ranks[rank + 1].Stars : from;
            var fw = hasNext ? inner.width * (stars - from) / Mathf.Max(1, to - from) : inner.width;
            if (fw > 1f) Nine(new Rect(inner.x, inner.y, Mathf.Max(fw, inner.height), inner.height), "UI/bar_fill", 22, Gold);
            Text(bar, hasNext ? $"다음 칭호 「{StarRoad.Ranks[rank + 1].Title}」까지 ★{to - stars}" : "최고 칭호!", 12, Color.white, TextAnchor.MiddleCenter, 0.7f);
            Text(new Rect(card.x + 12 * _s, card.y + 98 * _s, card.width - 24 * _s, 26 * _s),
                $"스테이지 ★{Progress.StageStars()} (모드 3개)  ·  첨탑 ★{Progress.TowerStars()}  ·  퀘스트 ★{Progress.QuestStars()}", 13, Soft, TextAnchor.MiddleCenter, 0f);

            // 탭
            for (var t = 0; t < 2; t++)
            {
                var label = t == 0 ? (Progress.RoadReadyCount() > 0 ? $"보상 ({Progress.RoadReadyCount()})" : "보상") : "보드 테마";
                if (Button(R(22 + t * 252, 240, 244, 46), gb.RoadTab == t ? new Color(0.3f, 0.6f, 1f) : new Color(0.35f, 0.38f, 0.55f), label, 19))
                {
                    gb.RoadTab = t;
                    gb.PanelMessage = "";
                }
            }
            if (gb.RoadTab == 0) DrawRoadSteps(gb, stars);
            else DrawThemeGrid(gb, 296);

            if (Button(R(280, 822, 210, 56), new Color(1f, 0.55f, 0.2f), "닫기", 22)) gb.OpenPanel(LobbyPanel.None);
            Text(R(24, 880, RefW - 48, 44), gb.PanelMessage, 16, Gold, TextAnchor.MiddleCenter, 0f);
            SwallowInput();
        }

        private static void DrawRoadSteps(GameBootstrap gb, int stars)
        {
            var steps = StarRoad.Steps;
            var pages = (steps.Length + GameBootstrap.RoadPerPage - 1) / GameBootstrap.RoadPerPage;
            gb.RoadPage = Mathf.Clamp(gb.RoadPage, 0, pages - 1);
            for (var k = 0; k < GameBootstrap.RoadPerPage; k++)
            {
                var i = gb.RoadPage * GameBootstrap.RoadPerPage + k;
                if (i >= steps.Length) break;
                var s = steps[i];
                var r = R(22, 296 + k * 64, RefW - 44, 58);
                var reached = stars >= s.Stars;
                var claimed = Progress.RoadClaimed(i);
                var ready = Progress.RoadReady(i);
                Nine(r, "UI/panel", 28, ready ? new Color(1f, 0.92f, 0.6f) : reached ? new Color(0.7f, 0.72f, 0.8f) : Color.white);
                var pill = new Rect(r.x + 8 * _s, r.y + 9 * _s, 84 * _s, 40 * _s);
                Nine(pill, "UI/button", 22, reached ? new Color(0.85f, 0.6f, 0.2f) : new Color(0.35f, 0.38f, 0.55f));
                Image(new Rect(pill.x + 4 * _s, pill.y + 6 * _s, 28 * _s, 28 * _s), "UI/star_on", reached ? Color.white : new Color(1f, 1f, 1f, 0.5f));
                Text(new Rect(pill.x + 30 * _s, pill.y, pill.width - 32 * _s, pill.height), s.Stars.ToString(), 17, Color.white);
                Text(new Rect(r.x + 102 * _s, r.y, r.width - 230 * _s, r.height), StarRoad.Text(s), 16, Color.white, TextAnchor.MiddleLeft);

                var btn = new Rect(r.xMax - 120 * _s, r.y + 8 * _s, 110 * _s, 42 * _s);
                if (!s.Claimable) Text(btn, reached ? "열림" : $"★{s.Stars - stars} 더", 15, reached ? Gold : Soft);
                else if (claimed) Text(btn, "받음", 16, new Color(0.85f, 0.85f, 0.9f));
                else if (ready)
                {
                    if (Button(btn, new Color(0.3f, 0.75f, 0.4f), "받기", 18) && Progress.ClaimRoad(i))
                        gb.PanelMessage = $"{StarRoad.Text(s)}을(를) 받았어요!";
                }
                else Text(btn, $"★{s.Stars - stars} 더", 15, Soft);
            }

            GUI.enabled = gb.RoadPage > 0;
            if (PillButton(R(22, 750, 64, 50), "◀", 20)) gb.RoadPage--;
            GUI.enabled = gb.RoadPage + 1 < pages;
            if (PillButton(R(164, 750, 64, 50), "▶", 20)) gb.RoadPage++;
            GUI.enabled = true;
            Text(R(86, 750, 78, 50), $"{gb.RoadPage + 1} / {pages}", 17, Color.white);
            var ready2 = Progress.RoadReadyCount();
            GUI.enabled = ready2 > 0;
            if (Button(R(240, 750, 278, 50), new Color(0.3f, 0.75f, 0.4f), ready2 > 0 ? $"모두 받기 ({ready2})" : "모두 받기", 19))
            {
                var n = Progress.ClaimAllRoad();
                if (n > 0) gb.PanelMessage = $"보상 {n}개를 받았어요!";
            }
            GUI.enabled = true;
        }

        /// <summary>보드 테마: 빈 칸·장식·바다 그림. 별의 길에서 열리면 결정으로 산다.</summary>
        /// <summary>보드 테마 8종 2x4 (별의 길 창과 상점에서 같이 쓴다). top = 첫 줄 y.</summary>
        private static void DrawThemeGrid(GameBootstrap gb, float top)
        {
            var current = Progress.Theme;
            var all = ThemeShop.All;
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                var r = R(22 + i % 2 * 252, top + i / 2 * 128, 244, 122);
                var owned = Progress.ThemeOwned(t.Id);
                var inUse = t.Id == current;
                Nine(r, "UI/panel", 28, inUse ? new Color(1f, 0.92f, 0.6f) : Color.white);
                var tail = t.Id == "" ? "" : "_" + t.Id;
                var tileRect = new Rect(r.x + 8 * _s, r.y + 16 * _s, 84 * _s, 64 * _s);
                Image(tileRect, "Tiles/tile_grass_a" + tail, Color.white);
                Image(new Rect(tileRect.x + 34 * _s, tileRect.y - 14 * _s, 32 * _s, 40 * _s), "Decor/tree_round" + tail, Color.white);
                Text(new Rect(r.x + 98 * _s, r.y + 8 * _s, r.width - 104 * _s, 26 * _s), t.Name, 17, inUse ? Gold : Color.white, TextAnchor.MiddleLeft);
                Text(new Rect(r.x + 98 * _s, r.y + 34 * _s, r.width - 104 * _s, 40 * _s), t.Note, 12, Soft, TextAnchor.UpperLeft, 0f);

                var btn = new Rect(r.x + 98 * _s, r.yMax - 46 * _s, r.width - 108 * _s, 38 * _s);
                var step = ThemeShop.StepOf(t.Id);
                if (inUse) Text(btn, "사용 중", 16, Gold);
                else if (owned)
                {
                    if (Button(btn, Palette.Player, "사용하기", 16))
                    {
                        gb.UseTheme(t.Id);
                        gb.PanelMessage = $"보드 테마를 「{t.Name}」(으)로 바꿨어요.";
                    }
                }
                else if (Progress.ThemeForSale(t))
                {
                    GUI.enabled = Progress.Gems >= t.Price;
                    if (PriceButton(btn, new Color(1f, 0.55f, 0.2f), "", t.Price, 16) && Progress.BuyTheme(t))
                    {
                        gb.UseTheme(t.Id);
                        gb.PanelMessage = $"「{t.Name}」을(를) 사서 바로 적용했어요!";
                    }
                    GUI.enabled = true;
                }
                else Text(btn, step == null ? "" : step.Kind == RoadReward.ThemeGift ? $"★{step.Stars} 선물" : $"★{step.Stars} 에 열림", 15, Soft);
            }
        }

        // ---------------------------------------------------------------- 크로마 첨탑

        /// <summary>
        /// 첨탑 화면: 고른 층의 판 미리보기, 층 정보(수·카드·힌트), 층 버튼(아래에서 위로 쌓이는 6x5), 도전 버튼.
        /// </summary>
        private static void DrawTower(GameBootstrap gb)
        {
            var floor = gb.TowerFloor;
            var p = PuzzleCatalog.Floors[floor];
            if (PillButton(R(14, 14, 90, 44), "← 홈", 15)) gb.ShowHome();
            Text(R(110, 10, 300, 52), "크로마 첨탑", 30, Gold, TextAnchor.MiddleCenter, 0.7f);
            GemPill(R(RefW - 136, 14, 122, 44));
            Text(R(16, 66, RefW - 32, 26), $"정해진 카드로 몇 수 안에 모든 칸을 내 색으로!   {Progress.TowerCleared} / {PuzzleCatalog.Count}층", 14, Soft);

            // (판 미리보기는 카메라가 13%~43% 띠에 그린다)

            var info = R(16, 418, RefW - 32, 150);
            Nine(info, "UI/panel", 28, Color.white);
            var cleared = Progress.FloorCleared(floor);
            Text(new Rect(info.x + 14 * _s, info.y + 8 * _s, info.width - 140 * _s, 32 * _s), $"{p.Floor}층  {p.Name}", 22, cleared ? Gold : Color.white, TextAnchor.MiddleLeft);
            Stars(new Rect(info.xMax - 124 * _s, info.y + 10 * _s, 112 * _s, 28 * _s), Progress.FloorStars(floor), 26);
            Text(new Rect(info.x + 14 * _s, info.y + 40 * _s, info.width - 28 * _s, 24 * _s), $"{p.Moves}수 · 첫 보상 결정 {Rewards.ForFloor(floor)} · ★ 깨기·무르기 없이·힌트 없이", 14, new Color(1f, 0.8f, 0.75f), TextAnchor.MiddleLeft, 0f);
            // 손패 미리보기
            var cards = p.Cards();
            for (var i = 0; i < cards.Count; i++)
            {
                var c = new Rect(info.x + 14 * _s + i * 52 * _s, info.y + 70 * _s, 46 * _s, 46 * _s);
                Image(c, "UI/card_art", Color.Lerp(Palette.Of(cards[i]), Color.white, 0.25f));
                Image(Inset(c, 4), IconFor(cards[i]), Color.white);
            }
            var hintBtn = new Rect(info.xMax - 104 * _s, info.y + 74 * _s, 92 * _s, 38 * _s);
            if (Button(hintBtn, new Color(0.45f, 0.48f, 0.65f), gb.HintShown ? "힌트 닫기" : "힌트", 15)) gb.ToggleHint();
            Text(new Rect(info.x + 14 * _s, info.y + 120 * _s, info.width - 28 * _s, 26 * _s), gb.HintShown ? "힌트: " + p.Hint : "카드: " + HandNames(cards), 13, gb.HintShown ? Gold : Soft, TextAnchor.MiddleLeft, 0f);

            // 층 버튼: 1층이 왼쪽 아래, 위로 쌓인다
            const int cols = 6;
            var rows = (PuzzleCatalog.Count + cols - 1) / cols;
            for (var i = 0; i < PuzzleCatalog.Count; i++)
            {
                var row = i / cols;
                var col = i % cols;
                var r = R(18 + col * 85, 582 + (rows - 1 - row) * 52, 79, 46);
                var open = Progress.FloorOpen(i);
                var done = Progress.FloorCleared(i);
                if (i == floor) Nine(Inset(r, -3), "UI/button", 22, Gold);
                var tint = !open ? new Color(0.3f, 0.32f, 0.42f) : done ? new Color(0.25f, 0.62f, 0.7f) : new Color(1f, 0.55f, 0.2f);
                if (Button(r, tint) && open) gb.SelectFloor(i);
                if (open && done)
                {
                    Text(new Rect(r.x, r.y + 2 * _s, r.width, 24 * _s), $"{i + 1}층", 16, Color.white);
                    Stars(new Rect(r.x, r.y + 25 * _s, r.width, 17 * _s), Progress.FloorStars(i), 16);
                }
                else if (open) Text(r, $"{i + 1}층", 17, Color.white);
                else Image(Inset(r, 10), "UI/icon_lock", new Color(1f, 1f, 1f, 0.7f));
            }

            GUI.enabled = Progress.FloorOpen(floor);
            if (Button(R(110, 856, 320, 76), new Color(1f, 0.55f, 0.2f), cleared ? "다시 도전" : "도전!", 30)) gb.StartFloor(floor);
            GUI.enabled = true;
        }

        private static string HandNames(List<Card> cards)
        {
            var names = new List<string>();
            foreach (var c in cards) names.Add(Palette.Name(c));
            return string.Join(", ", names);
        }

        /// <summary>첨탑 전투 상단: 나가기·다시(바로), 남은 수, 층, 힌트, 칸 수 바, 안내.</summary>
        private static void DrawPuzzleTop(GameBootstrap gb, Game g)
        {
            Fill(R(0, 0, RefW, 300), "UI/fade_top", Color.white);
            var b = g.Board;
            var total = Mathf.Max(1, b.Playable);
            var mine = b.Count(Side.Player);
            if (PillButton(R(14, 14, 66, 44), "나가기", 16)) gb.ExitToLobby();
            if (PillButton(R(86, 14, 66, 44), "다시", 16)) gb.Retry();

            var left = gb.MovesLeft;
            var movePill = R(160, 14, 132, 44);
            Nine(movePill, "UI/button", 22, left <= 1 ? new Color(0.95f, 0.45f, 0.3f) : new Color(0.3f, 0.55f, 0.95f));
            Text(movePill, $"남은 수 {left}", 19, Color.white);
            var floorPill = R(298, 14, 104, 44);
            Nine(floorPill, "UI/pill", 22, Color.white);
            Text(floorPill, $"{gb.Puzzle.Floor}층", 18, Gold);
            if (Button(R(RefW - 130, 14, 116, 44), new Color(0.45f, 0.48f, 0.65f), gb.HintShown ? "힌트 닫기" : "힌트", 17)) gb.ToggleHint();

            Text(R(0, 66, RefW, 30), "모든 칸을 내 색으로", 22, Color.white, TextAnchor.MiddleCenter, 0.6f);
            var bar = R(36, 98, RefW - 72, 46);
            Nine(bar, "UI/pill", 22, Color.white);
            var inner = Inset(bar, 7);
            var pw = inner.width * mine / total;
            if (pw > 1f) Nine(new Rect(inner.x, inner.y, Mathf.Max(pw, inner.height), inner.height), "UI/bar_fill", 22, Palette.Player);
            Text(inner, $"{mine} / {total}칸", 19, Color.white, TextAnchor.MiddleCenter, 0.7f);

            var msg = R(24, 152, RefW - 48, 64);
            Nine(msg, "UI/pill", 22, new Color(1f, 1f, 1f, 0.85f));
            Text(Inset(msg, 5), gb.Message, 16, gb.HintShown ? Gold : Soft, TextAnchor.MiddleCenter, 0f);
        }

        /// <summary>첨탑 결과: 성공이면 결정과 다음 층, 실패면 남은 칸과 힌트.</summary>
        private static void DrawPuzzleResult(GameBootstrap gb, Game g)
        {
            if (gb.CineActive) return; // 마지막 수의 합성 연출을 다 보고 나서
            Dim(0.45f);
            var solved = g.Solved;
            var p = gb.Puzzle;
            var hasNext = solved && p.Floor < PuzzleCatalog.Count;
            var canUndo = !solved && gb.CanUndo;
            Nine(R(50, 250, 440, hasNext || canUndo ? 366 : 296), "UI/panel", 28, Color.white);
            Text(R(50, 264, 440, 70), solved ? "성공!" : "실패", 54, solved ? Gold : new Color(1f, 0.6f, 0.55f), TextAnchor.MiddleCenter, 0.7f);
            var total = g.Board.Playable;
            var mine = g.Board.Count(Side.Player);
            if (solved)
            {
                Stars(R(50, 324, 440, 30), Progress.FloorStars(p.Floor - 1), 28);
                var bits = gb.LastFloorStars;
                var got = (bits & 1) + ((bits >> 1) & 1) + ((bits >> 2) & 1);
                var missing = new List<string>();
                if ((Progress.FloorStarBits(p.Floor - 1) & Progress.FloorNoUndo) == 0) missing.Add("무르기 없이");
                if ((Progress.FloorStarBits(p.Floor - 1) & Progress.FloorNoHint) == 0) missing.Add("힌트 없이");
                var starNote = got > 0 ? $"새 별 +{got}" : missing.Count > 0 ? "남은 별: " + string.Join(" · ", missing) : "별 3개 모두 모았어요";
                Text(R(50, 356, 440, 24), $"{p.Floor}층 {p.Name}  ·  {starNote}", 16, got > 0 ? Gold : Soft);
                var played = p.Cards().Count - g.DeckOf(Side.Player).Hand.Count; // 흑색의 추가 턴은 수를 안 써서 Turn 대신 쓴 카드 수
                Text(R(50, 382, 440, 30), $"카드 {played}장으로 모든 칸을 물들였어요!", 18, Color.white);
                var gemRow = R(110, 418, 320, 42);
                Nine(gemRow, "UI/panel", 28, new Color(1f, 0.95f, 0.75f));
                Image(new Rect(gemRow.x + 10 * _s, gemRow.y + 4 * _s, 34 * _s, 34 * _s), "UI/icon_gem", Color.white);
                Text(new Rect(gemRow.x + 48 * _s, gemRow.y, gemRow.width - 56 * _s, gemRow.height), gb.LastFirstClear ? $"+{gb.LastGems}  (처음 깬 보상)" : "이미 깬 층 (결정 없음)", 18, Gold, TextAnchor.MiddleLeft);
            }
            else
            {
                Text(R(50, 330, 440, 28), $"{p.Floor}층 {p.Name}", 18, Soft);
                Text(R(50, 362, 440, 34), $"{total - mine}칸이 남았어요 ({mine}/{total})", 20, Color.white);
                if (gb.HintShown) Text(R(66, 400, 408, 64), "힌트: " + p.Hint, 15, Gold, TextAnchor.MiddleCenter, 0f);
                else
                {
                    if (Button(R(170, 402, 200, 40), new Color(0.45f, 0.48f, 0.65f), "힌트 보기", 17)) gb.ToggleHint();
                    if (gb.HintFree(p.Floor - 1)) Text(R(50, 444, 440, 22), "힌트를 보면 이 층의 '힌트 없이' 별은 못 받아요", 12, Soft, TextAnchor.MiddleCenter, 0f);
                }
            }
            var y = hasNext || canUndo ? 546 : 476;
            if (hasNext && Button(R(80, 478, 380, 58), new Color(1f, 0.55f, 0.2f), $"{p.Floor + 1}층으로", 24)) gb.NextStage();
            if (canUndo && Button(R(80, 478, 380, 58), new Color(0.3f, 0.65f, 0.8f), "마지막 수 무르기", 22)) gb.Undo();
            if (Button(R(80, y, 184, 58), Palette.Player, "다시 하기", 21)) gb.Retry();
            if (PillButton(R(276, y, 184, 58), "첨탑으로", 21)) gb.ExitToLobby();
        }

        // ---------------------------------------------------------------- 전투 상단

        private static void DrawTop(GameBootstrap gb, Game g)
        {
            var b = g.Board;
            var total = Mathf.Max(1, b.Playable);
            var p = b.Count(Side.Player);
            var a = b.Count(Side.Ai);

            Fill(R(0, 0, RefW, 300), "UI/fade_top", Color.white);
            if (PillButton(R(14, 14, 66, 44), "나가기", 16)) gb.ExitToLobby();

            // 다시하기: 같은 스테이지를 새 판으로. 실수로 눌러 판을 날리지 않게 3초 안에 한 번 더 눌러야 한다.
            // (튜토리얼에서는 숨김. 게임이 끝났으면 결과 창에 다시 하기가 있다.)
            if (!gb.Tutorial)
            {
                var armed = Time.unscaledTime < _retryArmedUntil;
                if (Button(R(86, 14, 66, 44), armed ? new Color(0.95f, 0.45f, 0.3f) : new Color(0.4f, 0.42f, 0.58f), armed ? "한 번 더" : "다시", armed ? 14 : 16))
                {
                    if (armed || g.IsOver) { _retryArmedUntil = 0f; gb.Retry(); }
                    else _retryArmedUntil = Time.unscaledTime + 3f;
                }
            }

            var turnPill = R(160, 14, 112, 44);
            Nine(turnPill, "UI/pill", 22, Color.white);
            Text(turnPill, $"턴 {Mathf.Min(g.Turn + 1, g.MaxTurns)} / {g.MaxTurns}", 17, Color.white);

            var stagePill = R(278, 14, 112, 44);
            Nine(stagePill, "UI/pill", 22, gb.Stage.IsBoss ? new Color(1f, 0.75f, 0.7f) : Color.white);
            Text(stagePill, gb.Stage.IsBoss ? $"보스 {gb.Stage.Code}" : $"스테이지 {gb.Stage.Code}", 15, gb.Stage.IsBoss ? Gold : Color.white);

            var who = R(RefW - 144, 14, 130, 44);
            var mine = g.Current == Side.Player; // 팁 카드가 떠 있어도 실제 차례를 보여 준다
            var whoColor = g.IsOver ? new Color(0.6f, 0.6f, 0.7f) : mine ? Palette.Player : Palette.Ai;
            if (mine && !g.IsOver) Image(Inset(who, -12), "UI/glow", new Color(whoColor.r, whoColor.g, whoColor.b, 0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 4f)));
            Nine(who, "UI/button", 22, whoColor);
            Text(who, g.IsOver ? "게임 종료" : mine ? "내 차례" : "AI 차례", 19, Color.white);

            Text(R(0, 66, RefW, 30), "영토 점유율", 17, Soft, TextAnchor.MiddleCenter, 0.6f);
            var bar = R(36, 98, RefW - 72, 46);
            Nine(bar, "UI/pill", 22, Color.white);
            var inner = Inset(bar, 7);
            var pw = inner.width * p / total;
            var aw = inner.width * a / total;
            if (pw > 1f) Nine(new Rect(inner.x, inner.y, Mathf.Max(pw, inner.height), inner.height), "UI/bar_fill", 22, Palette.Mine);
            if (aw > 1f) Nine(new Rect(inner.xMax - Mathf.Max(aw, inner.height), inner.y, Mathf.Max(aw, inner.height), inner.height), "UI/bar_fill", 22, Palette.Foe);
            Text(Inset(inner, 4), $"{p * 100 / total}%", 19, Color.white, TextAnchor.MiddleLeft, 0.7f);
            Text(Inset(inner, 4), $"{a * 100 / total}%", 19, Color.white, TextAnchor.MiddleRight, 0.7f);

            var msg = R(24, 152, RefW - 48, 54);
            Nine(msg, "UI/pill", 22, new Color(1f, 1f, 1f, 0.85f));
            Text(Inset(msg, 5), gb.Message, 15, Soft, TextAnchor.MiddleCenter, 0f);

            DrawObjectives(gb, g);
        }

        /// <summary>게임 중 목표 줄: 목표·진행·보상. 이루면 초록으로 바뀌고, 이룬 순간 금빛으로 반짝인다.</summary>
        private static void DrawObjectives(GameBootstrap gb, Game g)
        {
            var n = g.Objectives.Count;
            if (n == 0) return;
            const float gap = 6f;
            var w = (RefW - 24f - (n - 1) * gap) / n;
            for (var i = 0; i < n; i++)
            {
                var o = g.Objectives[i];
                var r = R(12 + i * (w + gap), 210, w, 46);
                var since = gb.ObjectiveFlash.TryGetValue(o.Def.Id, out var t) ? Time.time - t : 99f;
                var tint = o.Done ? new Color(0.55f, 0.95f, 0.6f, 0.92f) : new Color(1f, 1f, 1f, 0.8f);
                if (since < 1.5f) tint = Color.Lerp(tint, Gold, 0.5f + 0.5f * Mathf.Sin(since * 12f));
                Nine(r, "UI/pill", 22, tint);
                var head = o.Done ? "완료! " + o.Def.Text : $"{o.Def.Text} {ObjectiveProgress(g, o.Def)}";
                var small = n > ObjectiveCatalog.PerGame; // 소모품 목표 추가로 4칸이면 글자를 줄인다
                Text(new Rect(r.x + 4 * _s, r.y + 3 * _s, r.width - 8 * _s, 22 * _s), head, small ? 11 : 13, Color.white, TextAnchor.MiddleCenter, 0.6f);
                Text(new Rect(r.x + 4 * _s, r.y + 23 * _s, r.width - 8 * _s, 20 * _s), "→ " + o.Def.RewardText, small ? 11 : 12, o.Done ? new Color(0.85f, 1f, 0.85f) : Gold, TextAnchor.MiddleCenter, 0f);
            }
        }

        private static string ObjectiveProgress(Game g, ObjectiveDef d)
        {
            var v = g.ProgressOf(d);
            return d.Goal switch
            {
                ObjectiveGoal.Share => $"(지금 {v}%)",
                ObjectiveGoal.BigMove => $"(최고 {v}칸)",
                _ => $"{Mathf.Min(v, d.Target)}/{d.Target}",
            };
        }

        // ---------------------------------------------------------------- 손패

        /// <summary>
        /// 손패: HandView 의 위치·회전·크기로 카드를 그린다 (부채꼴, 뽑기·뒤집기·내기·끌기 모션).
        /// 카드는 회전돼 있어서 GUI.Button 대신 직접 판정한다 (위에 그려진 카드부터). 카드를 누르면 끌기가 시작된다.
        /// </summary>
        private static void DrawHand(GameBootstrap gb, Game g)
        {
            Fill(R(0, 660, RefW, 300), "UI/fade_bottom", Color.white);
            var deck = gb.MyDeck;
            var hv = gb.Hand;
            var e = Event.current;

            // 덱 더미: 남은 장수만큼 뒷면이 겹쳐 쌓여 보인다. (튜토리얼에서는 숨김)
            if (!gb.Tutorial && gb.Puzzle == null) DrawDeckPile(deck);

            if (gb.Tutorial)
            {
                // 튜토리얼: 패 버리기 대신, 레슨을 마치면 "다음" 버튼
                if (gb.LessonDone)
                {
                    var last = gb.LessonIndex + 1 >= TutorialCatalog.Count;
                    var glow = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                    if (Button(R(RefW - 274, 690, 260, 72), Color.Lerp(new Color(1f, 0.55f, 0.2f), new Color(1f, 0.75f, 0.35f), glow), last ? "실전 시작!" : "다음 →", 28)) gb.NextLesson();
                }
            }
            else
            {
                if (gb.Puzzle != null)
                {
                    // 첨탑: 패 버리기 없음, 무르기는 공짜
                    GUI.enabled = gb.CanUndo;
                    if (Button(R(RefW - 214, 712, 200, 48), new Color(0.3f, 0.65f, 0.8f), "한 수 무르기", 18)) gb.Undo();
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = gb.PlayerTurn;
                    if (Button(R(RefW - 214, 712, 200, 48), new Color(0.55f, 0.5f, 0.95f), "패 버리기 (턴 소모)", 18))
                        gb.Mulligan();
                    GUI.enabled = gb.CanUndo;
                    if (gb.Pvp == null && Button(R(RefW - 346, 712, 126, 48), new Color(0.3f, 0.65f, 0.8f), $"무르기 {Progress.Undos}", 18))
                        gb.Undo();
                    GUI.enabled = true;
                    if (gb.Pvp != null && !gb.BattleOver) DrawEmoteButton(gb);
                }
            }

            var order = DrawOrder(hv, gb.Selected, gb.DraggingCard);
            if (gb.DraggingCard < 0 && (e.type == EventType.Repaint || e.type == EventType.MouseMove))
                hv.Hover = gb.PlayerTurn ? HitCard(hv, order, e.mousePosition) : -1;
            if (e.type == EventType.MouseDown && gb.PlayerTurn)
            {
                var hit = HitCard(hv, order, e.mousePosition);
                if (hit >= 0)
                {
                    gb.BeginCardDrag(hit, e.mousePosition);
                    e.Use();
                }
            }

            foreach (var v in hv.Leaving) DrawCard(v, false, true, g.LevelOf(gb.Me, v.Card));
            foreach (var i in order) DrawCard(hv.Cards[i], i == gb.Selected, gb.PlayerTurn && gb.CanUse(hv.Cards[i].Card), g.LevelOf(gb.Me, hv.Cards[i].Card));
        }

        private static void DrawDeckPile(Deck deck)
        {
            var pile = Mathf.Clamp((deck.DrawCount + 3) / 4, deck.DrawCount > 0 ? 1 : 0, 4);
            for (var k = 0; k < pile; k++)
                DrawBack(HandView.DeckPos + new Vector2(-k * 2f, -k * 2.5f), -6f, 0.55f, 1f);
            var badge = R(HandView.DeckPos.x + 14, HandView.DeckPos.y + 18, 46, 30);
            Nine(badge, "UI/button", 22, new Color(0.25f, 0.3f, 0.55f));
            Text(badge, deck.DrawCount.ToString(), 17, Color.white, TextAnchor.MiddleCenter, 0f);
            Text(R(HandView.DeckPos.x + 64, HandView.DeckPos.y - 14, 120, 28), $"무덤 {deck.DiscardCount}", 16, Color.white, TextAnchor.MiddleLeft);
        }

        /// <summary>그리는 순서: 나머지 → 마우스 올린 카드 → 고른 카드 → 끄는 카드 (위에 그린 것이 클릭도 먼저 받는다).</summary>
        private static List<int> DrawOrder(HandView hv, int selected, int dragging)
        {
            var order = new List<int>();
            for (var i = 0; i < hv.Cards.Count; i++)
                if (i != selected && i != hv.Hover && i != dragging) order.Add(i);
            if (hv.Hover >= 0 && hv.Hover < hv.Cards.Count && hv.Hover != selected && hv.Hover != dragging) order.Add(hv.Hover);
            if (selected >= 0 && selected < hv.Cards.Count && selected != dragging) order.Add(selected);
            if (dragging >= 0 && dragging < hv.Cards.Count) order.Add(dragging);
            return order;
        }

        public static Vector2 ToScreenPos(Vector2 refPos) => new(_ox + refPos.x * _s, _oy + refPos.y * _s);

        private static int HitCard(HandView hv, List<int> order, Vector2 mouse)
        {
            for (var k = order.Count - 1; k >= 0; k--)
            {
                var v = hv.Cards[order[k]];
                if (v.Delay > 0f) continue;
                var local = Quaternion.Euler(0f, 0f, -v.Rot) * (mouse - ToScreenPos(v.Pos));
                if (Mathf.Abs(local.x) <= HandView.CardW * 0.5f * _s * v.Scale && Mathf.Abs(local.y) <= HandView.CardH * 0.5f * _s * v.Scale)
                    return order[k];
            }
            return -1;
        }

        private static Rect BeginCard(Vector2 refPos, float rot, float scaleX, float scaleY, out Matrix4x4 saved)
        {
            saved = GUI.matrix;
            var c = ToScreenPos(refPos);
            GUIUtility.RotateAroundPivot(rot, c);
            GUIUtility.ScaleAroundPivot(new Vector2(scaleX, scaleY), c);
            return new Rect(c.x - HandView.CardW * 0.5f * _s, c.y - HandView.CardH * 0.5f * _s, HandView.CardW * _s, HandView.CardH * _s);
        }

        private static void DrawBack(Vector2 refPos, float rot, float scale, float alpha)
        {
            var r = BeginCard(refPos, rot, scale, scale, out var saved);
            Image(r, "UI/card_back", new Color(1f, 1f, 1f, alpha));
            GUI.matrix = saved;
        }

        private static void DrawCard(HandView.Visual v, bool selected, bool enabled, int level)
        {
            if (v.Delay > 0f) return;
            if (!v.FaceUp)
            {
                var rb = BeginCard(v.Pos, v.Rot, v.Scale * v.FlipWidth, v.Scale, out var savedBack);
                Image(rb, "UI/card_back", new Color(1f, 1f, 1f, v.Alpha));
                GUI.matrix = savedBack;
                return;
            }

            var card = v.Card;
            var a = v.Alpha;
            var r = BeginCard(v.Pos, v.Rot, v.Scale * v.FlipWidth, v.Scale, out var saved);
            if (selected) Nine(Inset(r, -5), "UI/button", 22, new Color(1f, 0.9f, 0.3f, a));
            var paper = enabled ? Color.white : new Color(0.72f, 0.72f, 0.78f);
            paper.a = a;
            Nine(r, card.IsUnit ? "UI/card_unit" : "UI/card_skill", 28, paper);

            var art = new Rect(r.x + 9 * _s, r.y + 9 * _s, r.width - 18 * _s, 72 * _s);
            var artTint = Color.Lerp(Palette.Of(card), Color.white, 0.25f);
            artTint.a = a;
            Image(art, "UI/card_art", artTint);
            Image(Inset(art, 5), IconFor(card), new Color(1f, 1f, 1f, a));
            if (level > 1) LevelBadge(new Rect(art.x + 3 * _s, art.y + 3 * _s, 38 * _s, 20 * _s), level);

            var ink = card.IsUnit ? new Color(0.3f, 0.18f, 0.08f, a) : new Color(0.95f, 0.95f, 1f, a);
            var subInk = card.IsUnit ? new Color(0.45f, 0.35f, 0.25f, a) : new Color(0.75f, 0.78f, 0.95f, a);
            Text(new Rect(r.x, art.yMax + 5 * _s, r.width, 26 * _s), Palette.Name(card), card.IsUnit ? 20 : 16, ink, TextAnchor.MiddleCenter, 0f);
            Text(new Rect(r.x + 6 * _s, art.yMax + 32 * _s, r.width - 12 * _s, 30 * _s), Palette.Hint(card), 12, subInk, TextAnchor.UpperCenter, 0f);
            GUI.matrix = saved;
        }

        private static string IconFor(Card c) => c.IsUnit
            ? "UI/icon_" + c.Element.ToString().ToLowerInvariant()
            : "UI/icon_" + c.Skill.ToString().ToLowerInvariant();

        // ---------------------------------------------------------------- 튜토리얼

        /// <summary>튜토리얼 상단: 건너뛰기, 진행도, 가이드 '방울이'의 말풍선.</summary>
        private static void DrawCoach(GameBootstrap gb)
        {
            if (PillButton(R(14, 14, 104, 44), "건너뛰기", 17)) gb.SkipTutorial();
            var pill = R(130, 14, RefW - 144, 44);
            Nine(pill, "UI/panel", 28, Color.white);
            Text(pill, $"튜토리얼 {gb.LessonIndex + 1} / {TutorialCatalog.Count}  ·  {gb.Lesson.Title}", 18, Color.white);
            // 레슨 진행 점
            for (var i = 0; i < TutorialCatalog.Count; i++)
            {
                var dot = R(RefW * 0.5f - TutorialCatalog.Count * 9 + i * 18, 62, 12, 12);
                Image(dot, i <= gb.LessonIndex ? "UI/star_on" : "UI/star_off", Color.white);
            }

            // 말풍선: 밝은 종이에 진한 큰 글자 (어두운 패널 위 작은 글자는 잘 안 보였다)
            var bubble = R(10, 80, RefW - 20, 172);
            Nine(bubble, "UI/bubble", 28, gb.LessonDone ? new Color(0.85f, 1f, 0.85f) : Color.white);
            var bob = Mathf.Sin(Time.unscaledTime * 3f) * 3f * _s;
            Image(new Rect(bubble.x + 8 * _s, bubble.y + 18 * _s + bob, 84 * _s, 98 * _s), "Units/unit_blue", Color.white);
            Rich(new Rect(bubble.x + 8 * _s, bubble.yMax - 34 * _s, 84 * _s, 24 * _s), "방울이", 14, InkSoft);
            var head = gb.LessonDone ? "잘했어요!" : "따라 해 보세요";
            Rich(new Rect(bubble.x + 104 * _s, bubble.y + 12 * _s, bubble.width - 116 * _s, 30 * _s), head, 21, gb.LessonDone ? new Color(0.15f, 0.55f, 0.2f) : new Color(0.85f, 0.4f, 0.1f), TextAnchor.MiddleLeft);
            Rich(new Rect(bubble.x + 104 * _s, bubble.y + 46 * _s, bubble.width - 118 * _s, 116 * _s), gb.Message, 19, Ink, TextAnchor.UpperLeft);
        }

        /// <summary>손가락 시범: 카드에서 목표 칸까지(또는 물을 흘릴 방향으로) 끄는 동작을 반복해서 보여 준다.</summary>
        private static void DrawFinger(GameBootstrap gb)
        {
            if (Event.current.type != EventType.Repaint) return;
            // 목표 칸 위에 통통 튀는 "여기!" 표시
            if (gb.TutorialTarget(out var target, out var label))
            {
                var hop = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 10f * _s;
                var tag = new Rect(target.x - 56 * _s, target.y - 86 * _s - hop, 112 * _s, 42 * _s);
                Nine(tag, "UI/button", 22, new Color(1f, 0.85f, 0.3f));
                Rich(tag, label, 19, Ink);
            }
            if (!gb.HintGesture(out var from, out var to, out var fromCard)) return;
            const float cycle = 2f;
            var t = Time.time % cycle / cycle;
            float move = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.72f, t));
            var alpha = t < 0.08f ? t / 0.08f : t > 0.88f ? (1f - t) / 0.12f : 1f;
            var pos = Vector2.Lerp(from, to, move);
            // 누르는 순간·떼는 순간 살짝 작아진다
            var press = t > 0.12f && t < 0.8f ? 0.9f : 1f;
            var size = 64f * _s * press;
            if (fromCard && move > 0.02f && move < 0.98f)
                Image(new Rect(pos.x - 24 * _s, pos.y + 6 * _s, 48 * _s, 66 * _s), "UI/card_back", new Color(1f, 1f, 1f, 0.55f * alpha));
            // 손끝(그림의 약 (22, 8)/96)이 pos 에 오도록
            Image(new Rect(pos.x - size * 0.23f, pos.y - size * 0.08f, size, size), "UI/finger", new Color(1f, 1f, 1f, alpha));
        }

        /// <summary>튜토리얼 개념 카드: 레슨 전에 그림과 짧은 설명. 열려 있는 동안 다른 입력은 막는다.</summary>
        private static void DrawConcept(GameBootstrap gb, ConceptPage page)
        {
            Dim(0.55f);
            var panel = R(24, 214, RefW - 48, 476);
            Nine(panel, "UI/bubble", 28, Color.white);
            Rich(R(24, 230, RefW - 48, 24), $"튜토리얼 {gb.LessonIndex + 1} / {TutorialCatalog.Count}", 15, InkSoft);
            Rich(R(24, 254, RefW - 48, 50), page.Title, 34, new Color(0.85f, 0.4f, 0.1f));

            // 그림 줄: 그림은 96, 기호는 44 폭
            var widths = new List<float>();
            var total = 0f;
            foreach (var p in page.Picture)
            {
                var w = p.Contains("/") ? 96f : 44f;
                widths.Add(w);
                total += w;
            }
            var scale = Mathf.Min(1f, (RefW - 80f) / total);
            var x = RefW * 0.5f - total * scale * 0.5f;
            for (var i = 0; i < page.Picture.Length; i++)
            {
                var w = widths[i] * scale;
                var p = page.Picture[i];
                if (p.Contains("/"))
                {
                    var bob = Mathf.Sin(Time.unscaledTime * 3f + i) * 3f;
                    foreach (var layer in p.Split('|')) Image(R(x + 4 * scale, 316 + bob, w - 8 * scale, 104 * scale), layer, Color.white);
                }
                else Rich(R(x, 330, w, 70), p, 34, InkSoft);
                x += w;
            }

            Rich(R(48, 432, RefW - 96, 150), page.Body, 20, Ink);

            for (var i = 0; i < gb.ConceptCount; i++)
                Image(R(RefW * 0.5f - gb.ConceptCount * 10 + i * 20, 588, 14, 14), i <= gb.ConceptIndex ? "UI/star_on" : "UI/star_off", Color.white);
            var lastPage = gb.ConceptIndex + 1 >= gb.ConceptCount;
            if (Button(R(140, 612, 260, 64), new Color(1f, 0.55f, 0.2f), lastPage ? "해 볼게요!" : "다음", 26)) gb.NextConcept();
            SwallowInput();
        }

        /// <summary>합성 연출: 처음에 섬광, 화면 아래쪽에 융합 이름 띠가 튀어나온다.</summary>
        private static void DrawCine(GameBootstrap gb)
        {
            if (!gb.CineActive || Event.current.type != EventType.Repaint) return;
            var t = gb.CineTime;
            var a = gb.CineAmount;
            var prev = GUI.color;
            var flash = Mathf.Clamp01((gb.CineStrong ? 0.75f : 0.5f) - t * 2.6f);
            if (flash > 0f)
            {
                GUI.color = new Color(1f, 1f, 1f, flash);
                GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.whiteTexture);
            }
            var color = Palette.Of(gb.CineElement);
            var strip = R(0, 590, RefW, 104);
            GUI.color = new Color(0f, 0f, 0.05f, 0.6f * a);
            GUI.DrawTexture(strip, Texture2D.whiteTexture);
            GUI.color = new Color(color.r, color.g, color.b, 0.9f * a);
            GUI.DrawTexture(R(0, 590, RefW, 4), Texture2D.whiteTexture);
            GUI.DrawTexture(R(0, 690, RefW, 4), Texture2D.whiteTexture);
            GUI.color = prev;
            var pop = 1f + 0.5f * Mathf.Exp(-t * 8f);
            var slide = (1f - Mathf.Clamp01(t / 0.2f)) * 60f;
            Image(R(24 - slide, 586, 90, 105), "Units/unit_" + gb.CineElement.ToString().ToLowerInvariant(), new Color(1f, 1f, 1f, a));
            var title = Color.Lerp(color, Color.white, 0.6f); // 흑색처럼 어두운 색도 띠 위에서 읽히게
            title.a = a;
            Text(R(110 + slide, 596, RefW - 130, 54), gb.CineTitle, Mathf.RoundToInt(38 * pop), title, TextAnchor.MiddleLeft, 0.9f);
            Text(R(112 + slide, 650, RefW - 130, 32), gb.CineSub, 17, new Color(1f, 0.98f, 0.85f, a), TextAnchor.MiddleLeft, 0.7f);
        }

        /// <summary>처음 만나는 맵 기믹·보스 설명 카드. 열려 있는 동안 다른 입력은 막는다.</summary>
        private static void DrawTip(GameBootstrap gb, TipDef tip)
        {
            var e = Event.current;
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0.05f, 0.55f);
            GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            var panel = R(36, 230, RefW - 72, 440);
            Nine(panel, "UI/panel", 28, Color.white);
            Text(R(36, 246, RefW - 72, 30), "새로운 것을 만났어요!", 16, new Color(0.75f, 0.9f, 1f));
            Text(R(36, 274, RefW - 72, 48), tip.Title, 32, Gold, TextAnchor.MiddleCenter, 0.7f);

            var n = tip.Pictures.Length;
            const float pw = 110f, gap = 18f;
            var x0 = RefW * 0.5f - (n * pw + (n - 1) * gap) * 0.5f;
            for (var i = 0; i < n; i++)
            {
                var r = R(x0 + i * (pw + gap), 330, pw, 92);
                foreach (var layer in tip.Pictures[i]) Image(r, layer, Color.white);
            }
            Text(R(56, 432, RefW - 112, 150), tip.Body, 16, Soft, TextAnchor.UpperCenter, 0f);
            if (Button(R(170, 590, 200, 60), new Color(1f, 0.55f, 0.2f), "확인", 24)) gb.DismissTip();

            // 팁이 떠 있는 동안 보드·손패로 입력이 새지 않게
            if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag || e.type == EventType.MouseUp) e.Use();
        }

        // ---------------------------------------------------------------- 결과

        private static void DrawResult(GameBootstrap gb, Game g)
        {
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0.05f, 0.45f);
            GUI.DrawTexture(new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;

            var w = g.Winner;
            var hasNext = w == Side.Player && (gb.Stage.Index + 1 < StageCatalog.Count || gb.LastNewMode);
            Nine(R(50, 250, 440, hasNext ? 462 : 400), "UI/panel", 28, Color.white);
            if (w == Side.Player) Image(R(110, 200, 320, 200), "UI/glow", new Color(1f, 0.85f, 0.4f, 0.7f));
            var title = w == Side.Player ? "승리!" : w == Side.Ai ? "패배" : "무승부";
            Text(R(50, 264, 440, 70), title, 54, w == Side.Player ? Gold : w == Side.Ai ? new Color(1f, 0.6f, 0.55f) : Color.white, TextAnchor.MiddleCenter, 0.7f);
            Text(R(50, 330, 440, 28), $"{gb.Stage.Code} {gb.Stage.Name}", 17, Soft);
            Stars(R(50, 364, 440, 56), gb.LastStars, 50);
            var total = Mathf.Max(1, g.Board.Playable);
            Text(R(50, 426, 440, 34), $"나 {g.Board.Count(Side.Player) * 100 / total}%   vs   AI {g.Board.Count(Side.Ai) * 100 / total}%", 22, Color.white);
            var note = gb.LastNewMode ? $"{ModeName(Progress.Mode)}가 열렸어요! 1-1부터 다시 도전하세요"
                : g.Stuck == Side.Player ? "내가 놓을 칸이 없어 게임이 끝났어요"
                : g.Stuck == Side.Ai ? "AI가 놓을 칸이 없어 게임이 끝났어요"
                : "별: 승리 1개 · 점유율 50% 이상 2개 · 65% 이상 3개";
            Text(R(50, 460, 440, 26), note, gb.LastNewMode ? 17 : 14, gb.LastNewMode ? Gold : new Color(1f, 0.9f, 0.7f));

            // 받은 결정 (+ 첫 클리어·목표 보너스)
            var gemRow = R(110, 492, 320, 44);
            Nine(gemRow, "UI/pill", 22, Color.white);
            Image(new Rect(gemRow.x + 10 * _s, gemRow.y + 5 * _s, 34 * _s, 34 * _s), "UI/icon_gem", Color.white);
            Text(new Rect(gemRow.x + 48 * _s, gemRow.y, gemRow.width - 56 * _s, gemRow.height), $"+{gb.LastGems}  (보유 {Progress.Gems})", 20, Gold, TextAnchor.MiddleLeft);
            var extras = new List<string>();
            if (gb.LastFirstClear) extras.Add(gb.Stage.IsBoss ? "보스 첫 클리어 보너스" : "첫 클리어 보너스");
            if (g.Objectives.Count > 0) extras.Add($"목표 {g.ObjectivesDone}/{g.Objectives.Count} 달성");
            if (Progress.QuestsReady() > 0) extras.Add("받을 퀘스트 보상이 있어요");
            if (Progress.RoadReadyCount() > 0) extras.Add("별의 길 보상이 있어요");
            Text(R(50, 538, 440, 24), string.Join("  ·  ", extras), 13, Soft, TextAnchor.MiddleCenter, 0f);

            if (hasNext && CardButton(R(80, 568, 380, 62), new Color(1f, 0.55f, 0.2f), gb.LastNewMode ? $"{ModeName(Progress.Mode)} 1-1" : "다음 스테이지", 24))
            {
                if (gb.LastNewMode) gb.StartNewMode(); else gb.NextStage();
            }
            var y = hasNext ? 640 : 576;
            if (CardButton(R(80, y, 184, 58), Palette.Player, "다시 하기", 21)) gb.Retry();
            if (PillButton(R(276, y, 184, 58), "대기실", 21)) gb.ExitToLobby();
        }
    }
}
