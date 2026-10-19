using System;
using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>
    /// 튜토리얼 레슨 하나: 작은 연습 보드에서 카드 한 장을 정해진 칸(Target)에 내 보는 것.
    /// 놓을 수 있는 칸은 Target 하나로 좁혀지고, 낸 결과가 Check 를 만족하면 레슨 완료.
    /// </summary>
    public sealed class Lesson
    {
        public string Title;
        // 안내 문구는 IMGUI 리치 텍스트(<color>)로 강조한다 (밝은 말풍선 위 진한 글자 기준 색)
        public string Intro;       // 시작할 때 안내
        public string WaterIntro;  // 물: 칸에 놓은 뒤 방향 안내
        public string Outro;       // 완료 후 설명
        public string[] Map;
        public Action<Board> Setup;
        public Card Card;
        public Cell Target;
        public Cell? WaterDir;     // 물 레슨: 흘려야 할 방향
        public Func<PlayResult, bool> Check;

        public Game CreateGame()
        {
            var board = Board.Parse(Map);
            Setup?.Invoke(board);
            return Game.Create(board, new[] { Card }, Game.StarterDeck(), 1, new StageRules { MaxTurns = 99 }, shuffle: false);
        }
    }

    /// <summary>
    /// 체험형 튜토리얼 8단계: 기본 원소 3종 → 융합 → 가로채기 → 3차 조합(흑색) → 공명 → 스킬.
    /// 6x6 보드, 플레이어 땅은 왼쪽 아래(x 0~1, y 4~5), 상대 땅은 오른쪽 위.
    /// </summary>
    public static class TutorialCatalog
    {
        private static readonly string[] BaseMap =
        {
            "....AA",
            "....AA",
            "......",
            "......",
            "PP....",
            "PP....",
        };

        private static void Put(Board b, int x, int y, Element e, Side side)
        {
            var c = new Cell(x, y);
            b[c].Unit = e;
            b[c].UnitOwner = side;
            b[c].Owner = side;
        }

        private static void Own(Board b, Side side, Func<int, int, bool> where)
        {
            foreach (var c in b.Cells())
                if (where(c.X, c.Y)) b[c].Owner = side;
        }

        public static readonly Lesson[] Lessons =
        {
            new()
            {
                Title = "카드 놓기",
                Intro = "아래 <color=#e0442a>화염</color> 카드를 손가락으로 끌어서\n<color=#d0471a>반짝이는 칸</color>에 놓아 보세요.",
                Outro = "<color=#e0442a>화염</color>은 놓은 칸과 <color=#d0471a>위아래 양옆</color> 칸을 차지해요.\n파란 칸이 늘었죠? 이게 <color=#2a6fe0>내 땅</color>이에요.",
                Map = BaseMap,
                Card = Card.Unit(Element.Red),
                Target = new Cell(2, 4),
                Check = r => r.Placed == Element.Red,
            },
            new()
            {
                Title = "물의 방향",
                Intro = "<color=#2a6fe0>물</color> 카드를 <color=#d0471a>반짝이는 칸</color>에 끌어다 놓아 보세요.",
                WaterIntro = "이제 물이 흐를 방향을 골라요.\n<color=#d0471a>화살표 칸</color>을 누르거나 그쪽으로 쓸어 넘기세요.",
                Outro = "<color=#2a6fe0>물</color>은 고른 방향으로 <color=#d0471a>곧게 3칸</color> 흘러요.\n멀리 뻗어 나갈 때 좋아요.",
                Map = BaseMap,
                Card = Card.Unit(Element.Blue),
                Target = new Cell(2, 5),
                WaterDir = new Cell(1, 0),
                Check = r => r.Placed == Element.Blue,
            },
            new()
            {
                Title = "번개",
                Intro = "<color=#c48a00>번개</color> 카드를 <color=#d0471a>반짝이는 칸</color>에 놓아 보세요.",
                Outro = "<color=#c48a00>번개</color>는 주변 <color=#d0471a>아무 데나 4칸</color>에 떨어져요.\n어디로 튈지 모르지만 넓게 퍼져요.",
                Map = BaseMap,
                Card = Card.Unit(Element.Yellow),
                Target = new Cell(2, 4),
                Check = r => r.Placed == Element.Yellow,
            },
            new()
            {
                Title = "융합",
                Intro = "<color=#2a6fe0>물</color> 유닛 바로 옆에 <color=#e0442a>화염</color>을 놓아 보세요.\n두 원소가 <color=#d0471a>합쳐져요</color>!",
                Outro = "<color=#e0442a>화염</color> + <color=#2a6fe0>물</color> = <color=#8a3ad0>폭풍</color>! 넓게 차지하고 2턴 동안 지켜요.\n<color=#2a6fe0>물</color>+<color=#c48a00>번개</color> = <color=#2a9a40>돌풍</color>,  <color=#e0442a>화염</color>+<color=#c48a00>번개</color> = <color=#e07010>마그마</color>",
                Map = BaseMap,
                Setup = b => Put(b, 2, 4, Element.Blue, Side.Player),
                Card = Card.Unit(Element.Red),
                Target = new Cell(3, 4),
                Check = r => r.Fused == Element.Purple,
            },
            new()
            {
                Title = "가로채기",
                Intro = "<color=#d0471a>상대</color>의 <color=#c48a00>번개</color> 유닛 옆에 <color=#e0442a>화염</color>을 놓아 보세요.\n상대 유닛도 재료로 쓸 수 있어요!",
                Outro = "상대 유닛으로 <color=#e07010>마그마</color>를 만들었어요!\n휘말린 칸은 <color=#d0471a>상대 땅이어도 전부 내 것</color>이 돼요.",
                Map = BaseMap,
                Setup = b =>
                {
                    Own(b, Side.Ai, (x, y) => x >= 3 && y <= 3);
                    b[new Cell(2, 4)].Owner = Side.Player;
                    Put(b, 3, 3, Element.Yellow, Side.Ai);
                },
                Card = Card.Unit(Element.Red),
                Target = new Cell(3, 4),
                Check = r => r.Fused == Element.Orange && r.StoleCatalyst,
            },
            new()
            {
                Title = "3원색 완성",
                Intro = "<color=#2a9a40>초록</color>은 <color=#2a6fe0>파랑</color>+<color=#c48a00>노랑</color>을 품고 있어요.\n옆에 빠진 <color=#e0442a>빨강</color>을 놓아 세 색을 모아 보세요!",
                Outro = "3원색 완성! 상대 유닛을 썼으니 <color=#d0471a>흑색</color>:\n주변 상대 땅을 흡수하고 <color=#d0471a>한 번 더</color> 둬요.\n내 유닛만 쓰면 백색: 내 땅을 5턴 지켜요.",
                Map = BaseMap,
                Setup = b =>
                {
                    Own(b, Side.Ai, (x, y) => x >= 3 && y <= 3);
                    b[new Cell(2, 4)].Owner = Side.Player;
                    Put(b, 3, 3, Element.Green, Side.Ai);
                },
                Card = Card.Unit(Element.Red),
                Target = new Cell(3, 4),
                Check = r => r.Fused == Element.Black && r.Boosted,
            },
            new()
            {
                Title = "공명",
                Intro = "<color=#e0442a>화염</color> 옆에 또 <color=#e0442a>화염</color>을 놓아 보세요.\n같은 원소끼리는 <color=#d0471a>힘이 세져요</color>.",
                Outro = "공명! <color=#e0442a>화염</color>은 업화로 훨씬 넓게 터져요.\n<color=#2a6fe0>물</color>은 해일, <color=#c48a00>번개</color>는 뇌우가 돼요.",
                Map = BaseMap,
                Setup = b => Put(b, 2, 4, Element.Red, Side.Player),
                Card = Card.Unit(Element.Red),
                Target = new Cell(2, 3),
                Check = r => r.Resonance,
            },
            new()
            {
                Title = "스킬 카드",
                Intro = "<color=#d0471a>탈색</color> 카드를 상대의 <color=#2a6fe0>물</color> 유닛에 놓아 보세요.\n상대가 그 유닛을 재료로 못 쓰게 돼요.",
                Outro = "스킬은 <color=#d0471a>한 턴에 한 장</color>, 쓰고 나서 유닛도 놓을 수 있어요.\n이제 실전이에요! 턴이 끝났을 때 <color=#2a6fe0>땅이 많으면</color> 승리!",
                Map = BaseMap,
                Setup = b =>
                {
                    Own(b, Side.Ai, (x, y) => x >= 3 && y <= 2);
                    Put(b, 3, 2, Element.Blue, Side.Ai);
                    Put(b, 4, 2, Element.Yellow, Side.Ai);
                },
                Card = Card.Of(Skill.Bleach),
                Target = new Cell(3, 2),
                Check = r => r.Skill == Skill.Bleach,
            },
        };

        public static int Count => Lessons.Length;
    }
}
