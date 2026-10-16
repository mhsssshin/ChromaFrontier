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
    public static class Hud
    {
        private const float RefW = 540f, RefH = 960f;
        private const float TexScale = 0.5f; // UI 텍스처는 기준 좌표의 2배로 그려져 있다

        private static float _s = 1f, _ox, _oy;
        private static Font _font;
        private static float _retryArmedUntil; // 전투 상단 '다시' 버튼: 이 시각까지 한 번 더 누르면 다시 시작
        private static GUIStyle _label, _button;

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
            if (gb.Mode == AppScreen.Lobby)
            {
                DrawLobby(gb);
                return;
            }

            var g = gb.Game;
            var e = Event.current;
            if (e.type == EventType.MouseMove || e.type == EventType.Repaint) gb.SetHover(e.mousePosition);

            if (gb.Tutorial) DrawCoach(gb);
            else DrawTop(gb, g);
            DrawHand(gb, g);
            if (gb.Tutorial) DrawFinger(gb);
            if (gb.ActiveTip != null)
            {
                DrawTip(gb, gb.ActiveTip);
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

        private static string OffsetName(int o) => o < 0 ? "쉬움" : o > 0 ? "어려움" : "보통";

        private static Color OffsetColor(int o) => o < 0 ? new Color(0.35f, 0.8f, 0.45f) : o > 0 ? new Color(0.85f, 0.3f, 0.45f) : new Color(0.95f, 0.7f, 0.25f);

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
            // 개발 빌드 대기실 단축키: U = 전체 스테이지 해금
            var e = Event.current;
            if (Debug.isDebugBuild && e.type == EventType.KeyDown && e.keyCode == KeyCode.U)
            {
                Progress.UnlockAll();
                e.Use();
            }

            var stage = StageCatalog.Get(gb.LobbyStage);
            var chapter = StageCatalog.Chapters[stage.Chapter];

            // 제목 + 별 합계
            Image(R(8, 4, 200, 86), "UI/logo_title", Color.white);
            if (Button(R(RefW - 254, 22, 96, 44), new Color(0.3f, 0.7f, 0.55f), "배우기", 18)) gb.StartTutorial(0);
            var starPill = R(RefW - 150, 22, 136, 44);
            Nine(starPill, "UI/panel", 28, Color.white);
            Image(new Rect(starPill.x + 8 * _s, starPill.y + 6 * _s, 32 * _s, 32 * _s), "UI/star_on", Color.white);
            Text(new Rect(starPill.x + 40 * _s, starPill.y, starPill.width - 44 * _s, starPill.height), $"{Progress.TotalStars()} / {StageCatalog.Count * 3}", 18, Color.white);

            // 챕터 탭
            for (var c = 0; c < StageCatalog.Chapters.Length; c++)
            {
                var r = R(14 + c * 104, 92, 96, 44);
                var open = Progress.IsUnlocked(c * StageCatalog.PerChapter);
                GUI.enabled = open;
                var tint = c == stage.Chapter ? new Color(0.3f, 0.6f, 1f) : new Color(0.35f, 0.38f, 0.55f);
                if (Button(r, tint, open ? $"{c + 1}장" : $"{c + 1}장 잠김", 18)) gb.SelectLobbyChapter(c);
                GUI.enabled = true;
            }
            Text(R(16, 140, RefW - 32, 26), $"{stage.Chapter + 1}장 {chapter.Name} — {chapter.Theme}", 15, Soft);

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
                if (index == gb.LobbyStage) Nine(Inset(r, -4), "UI/button", 22, Gold);
                var tint = !open ? new Color(0.3f, 0.32f, 0.42f) : s.IsBoss ? new Color(0.85f, 0.35f, 0.4f) : new Color(0.3f, 0.55f, 0.95f);
                if (Button(r, tint) && open) gb.SelectLobbyStage(index);
                if (open)
                {
                    Text(new Rect(r.x, r.y + 8 * _s, r.width, 36 * _s), s.Code, 22, Color.white);
                    Stars(new Rect(r.x, r.y + 52 * _s, r.width, 26 * _s), Progress.StarsOf(index), 22);
                }
                else Image(Inset(r, 20), "UI/icon_lock", new Color(1f, 1f, 1f, 0.7f));
                if (s.IsBoss) Image(new Rect(r.xMax - 26 * _s, r.y - 10 * _s, 30 * _s, 30 * _s), "UI/crown", Color.white);
            }

            // 난이도 보정 + 출격
            var offset = Progress.LevelOffset;
            if (Button(R(24, 854, 200, 76), OffsetColor(offset))) gb.CycleLevelOffset();
            Text(R(24, 860, 200, 30), "난이도 보정", 15, Color.white);
            Text(R(24, 888, 200, 34), OffsetName(Progress.LevelOffset), 22, Color.white);
            GUI.enabled = Progress.IsUnlocked(gb.LobbyStage);
            if (Button(R(240, 854, 276, 76), new Color(1f, 0.55f, 0.2f), "출격!", 32)) gb.StartStage(gb.LobbyStage);
            GUI.enabled = true;
        }

        // ---------------------------------------------------------------- 전투 상단

        private static void DrawTop(GameBootstrap gb, Game g)
        {
            var b = g.Board;
            var total = Mathf.Max(1, b.Playable);
            var p = b.Count(Side.Player);
            var a = b.Count(Side.Ai);

            if (Button(R(14, 14, 66, 44), new Color(0.4f, 0.42f, 0.58f), "나가기", 16)) gb.ExitToLobby();

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
            Nine(turnPill, "UI/panel", 28, Color.white);
            Text(turnPill, $"턴 {Mathf.Min(g.Turn + 1, g.MaxTurns)} / {g.MaxTurns}", 17, Color.white);

            var stagePill = R(278, 14, 112, 44);
            Nine(stagePill, "UI/panel", 28, gb.Stage.IsBoss ? new Color(1f, 0.75f, 0.7f) : Color.white);
            Text(stagePill, gb.Stage.IsBoss ? $"보스 {gb.Stage.Code}" : $"스테이지 {gb.Stage.Code}", 15, gb.Stage.IsBoss ? Gold : Color.white);

            var who = R(RefW - 144, 14, 130, 44);
            var mine = g.Current == Side.Player; // 팁 카드가 떠 있어도 실제 차례를 보여 준다
            var whoColor = g.IsOver ? new Color(0.6f, 0.6f, 0.7f) : mine ? Palette.Player : Palette.Ai;
            Nine(who, "UI/button", 22, whoColor);
            Text(who, g.IsOver ? "게임 종료" : mine ? "내 차례" : "AI 차례", 19, Color.white);

            Text(R(0, 66, RefW, 30), "영토 점유율", 22, Color.white, TextAnchor.MiddleCenter, 0.6f);
            var bar = R(36, 98, RefW - 72, 46);
            Nine(bar, "UI/bar_frame", 30, Color.white);
            var inner = Inset(bar, 7);
            var pw = inner.width * p / total;
            var aw = inner.width * a / total;
            if (pw > 1f) Nine(new Rect(inner.x, inner.y, Mathf.Max(pw, inner.height), inner.height), "UI/bar_fill", 22, Palette.Player);
            if (aw > 1f) Nine(new Rect(inner.xMax - Mathf.Max(aw, inner.height), inner.y, Mathf.Max(aw, inner.height), inner.height), "UI/bar_fill", 22, Palette.Ai);
            Text(Inset(inner, 4), $"{p * 100 / total}%", 19, Color.white, TextAnchor.MiddleLeft, 0.7f);
            Text(Inset(inner, 4), $"{a * 100 / total}%", 19, Color.white, TextAnchor.MiddleRight, 0.7f);

            var msg = R(24, 152, RefW - 48, 54);
            Nine(msg, "UI/panel", 28, new Color(1f, 1f, 1f, 0.85f));
            Text(Inset(msg, 5), gb.Message, 15, Soft, TextAnchor.MiddleCenter, 0f);
        }

        // ---------------------------------------------------------------- 손패

        /// <summary>
        /// 손패: HandView 의 위치·회전·크기로 카드를 그린다 (부채꼴, 뽑기·뒤집기·내기·끌기 모션).
        /// 카드는 회전돼 있어서 GUI.Button 대신 직접 판정한다 (위에 그려진 카드부터). 카드를 누르면 끌기가 시작된다.
        /// </summary>
        private static void DrawHand(GameBootstrap gb, Game g)
        {
            var deck = g.DeckOf(Side.Player);
            var hv = gb.Hand;
            var e = Event.current;

            // 덱 더미: 남은 장수만큼 뒷면이 겹쳐 쌓여 보인다. (튜토리얼에서는 숨김)
            if (!gb.Tutorial) DrawDeckPile(deck);

            if (gb.Tutorial)
            {
                // 튜토리얼: 패 버리기 대신, 레슨을 마치면 "다음" 버튼
                if (gb.LessonDone)
                {
                    var last = gb.LessonIndex + 1 >= TutorialCatalog.Count;
                    if (Button(R(RefW - 254, 700, 240, 60), new Color(1f, 0.55f, 0.2f), last ? "실전 시작!" : "다음 →", 24)) gb.NextLesson();
                }
            }
            else
            {
                GUI.enabled = gb.PlayerTurn;
                if (Button(R(RefW - 214, 712, 200, 48), new Color(0.55f, 0.5f, 0.95f), "패 버리기 (턴 소모)", 18))
                    gb.Mulligan();
                GUI.enabled = true;
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

            foreach (var v in hv.Leaving) DrawCard(v, false, true);
            foreach (var i in order) DrawCard(hv.Cards[i], i == gb.Selected, gb.PlayerTurn && gb.CanUse(hv.Cards[i].Card));
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

        private static void DrawCard(HandView.Visual v, bool selected, bool enabled)
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
            if (Button(R(14, 14, 104, 44), new Color(0.4f, 0.42f, 0.58f), "건너뛰기", 17)) gb.SkipTutorial();
            var pill = R(130, 14, RefW - 144, 44);
            Nine(pill, "UI/panel", 28, Color.white);
            Text(pill, $"튜토리얼 {gb.LessonIndex + 1} / {TutorialCatalog.Count}  ·  {gb.Lesson.Title}", 18, Color.white);
            // 레슨 진행 점
            for (var i = 0; i < TutorialCatalog.Count; i++)
            {
                var dot = R(RefW * 0.5f - TutorialCatalog.Count * 9 + i * 18, 62, 12, 12);
                Image(dot, i <= gb.LessonIndex ? "UI/star_on" : "UI/star_off", Color.white);
            }

            var bubble = R(14, 80, RefW - 28, 124);
            Nine(bubble, "UI/panel", 28, gb.LessonDone ? new Color(0.75f, 1f, 0.8f) : Color.white);
            var bob = Mathf.Sin(Time.time * 3f) * 3f * _s;
            Image(new Rect(bubble.x + 6 * _s, bubble.y + 14 * _s + bob, 76 * _s, 90 * _s), "Units/unit_blue", Color.white);
            Text(new Rect(bubble.x + 6 * _s, bubble.yMax - 26 * _s, 76 * _s, 22 * _s), "방울이", 13, Soft, TextAnchor.MiddleCenter, 0f);
            var head = gb.LessonDone ? "잘했어요!" : "따라 해 보세요";
            Text(new Rect(bubble.x + 92 * _s, bubble.y + 8 * _s, bubble.width - 100 * _s, 26 * _s), head, 18, gb.LessonDone ? Gold : new Color(0.75f, 0.9f, 1f), TextAnchor.MiddleLeft);
            Text(new Rect(bubble.x + 92 * _s, bubble.y + 34 * _s, bubble.width - 104 * _s, 84 * _s), gb.Message, 15, Soft, TextAnchor.UpperLeft, 0f);
        }

        /// <summary>손가락 시범: 카드에서 목표 칸까지(또는 물을 흘릴 방향으로) 끄는 동작을 반복해서 보여 준다.</summary>
        private static void DrawFinger(GameBootstrap gb)
        {
            if (Event.current.type != EventType.Repaint || !gb.HintGesture(out var from, out var to, out var fromCard)) return;
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

            var panel = R(50, 270, 440, 400);
            Nine(panel, "UI/panel", 28, Color.white);
            var w = g.Winner;
            var title = w == Side.Player ? "승리!" : w == Side.Ai ? "패배" : "무승부";
            Text(R(50, 284, 440, 70), title, 54, w == Side.Player ? Gold : w == Side.Ai ? new Color(1f, 0.6f, 0.55f) : Color.white, TextAnchor.MiddleCenter, 0.7f);
            Text(R(50, 350, 440, 28), $"{gb.Stage.Code} {gb.Stage.Name}", 17, Soft);
            Stars(R(50, 384, 440, 56), gb.LastStars, 50);
            var total = Mathf.Max(1, g.Board.Playable);
            Text(R(50, 446, 440, 34), $"나 {g.Board.Count(Side.Player) * 100 / total}%   vs   AI {g.Board.Count(Side.Ai) * 100 / total}%", 22, Color.white);
            var note = g.Stuck == Side.Player ? "내가 놓을 칸이 없어 게임이 끝났어요"
                : g.Stuck == Side.Ai ? "AI가 놓을 칸이 없어 게임이 끝났어요"
                : "별: 승리 1개 · 점유율 50% 이상 2개 · 65% 이상 3개";
            Text(R(50, 482, 440, 28), note, 14, new Color(1f, 0.9f, 0.7f));

            var hasNext = w == Side.Player && gb.Stage.Index + 1 < StageCatalog.Count;
            if (hasNext && Button(R(80, 522, 380, 62), new Color(1f, 0.55f, 0.2f), "다음 스테이지", 24)) gb.NextStage();
            var y = hasNext ? 596 : 530;
            if (Button(R(80, y, 184, 58), Palette.Player, "다시 하기", 21)) gb.Retry();
            if (Button(R(276, y, 184, 58), new Color(0.4f, 0.42f, 0.58f), "대기실", 21)) gb.ExitToLobby();
        }
    }
}
