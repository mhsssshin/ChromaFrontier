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
                Intro = "아래 화염 카드를 끌어서 빛나는 칸에 놓아 보세요.\n(카드를 눌러 고른 뒤 빛나는 칸을 눌러도 돼요)",
                Outro = "좋아요! 화염은 놓인 칸과 십자 방향으로 땅을 넓혀요.\n카드는 내 땅(파랑)이나 그 바로 옆 칸에만 놓을 수 있어요.",
                Map = BaseMap,
                Card = Card.Unit(Element.Red),
                Target = new Cell(2, 4),
                Check = r => r.Placed == Element.Red,
            },
            new()
            {
                Title = "물의 방향",
                Intro = "이번엔 물 카드예요.\n빛나는 칸에 끌어다 놓아 보세요.",
                WaterIntro = "이제 오른쪽 아래로 보드를 쓸어 넘기거나\n화살표 칸을 눌러 물을 흘려 보세요.",
                Outro = "물은 고른 방향으로 3칸 흘러요.\n길게 뻗어 나가 상대 쪽으로 다가갈 때 좋아요.",
                Map = BaseMap,
                Card = Card.Unit(Element.Blue),
                Target = new Cell(2, 5),
                WaterDir = new Cell(1, 0),
                Check = r => r.Placed == Element.Blue,
            },
            new()
            {
                Title = "번개",
                Intro = "번개 카드를 빛나는 칸에 놓아 보세요.",
                Outro = "번개는 주변 무작위 4칸에 떨어져요.\n어디로 튈지 모르는 대신 넓게 퍼지죠.",
                Map = BaseMap,
                Card = Card.Unit(Element.Yellow),
                Target = new Cell(2, 4),
                Check = r => r.Placed == Element.Yellow,
            },
            new()
            {
                Title = "융합",
                Intro = "다른 원소끼리 맞닿으면 융합해요!\n물 유닛 옆에 화염을 놓아 보세요.",
                Outro = "빨강 + 파랑 = 보라(폭풍)! 넓게 점유하고 그 땅을 2턴 동안 지켜요.\n파랑+노랑은 초록(돌풍), 빨강+노랑은 주황(마그마)이에요.",
                Map = BaseMap,
                Setup = b => Put(b, 2, 4, Element.Blue, Side.Player),
                Card = Card.Unit(Element.Red),
                Target = new Cell(3, 4),
                Check = r => r.Fused == Element.Purple,
            },
            new()
            {
                Title = "가로채기",
                Intro = "상대 유닛도 융합 재료가 돼요!\n상대의 번개 유닛 옆에 화염을 놓아 보세요.",
                Outro = "반응에 휘말린 칸은 상대 땅이어도 모두 내 것!\n상대가 놓은 유닛을 재료로 삼는 게 역전의 열쇠예요.",
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
                Intro = "2단계 유닛은 색 두 개를 품고 있어요.\n초록(파랑+노랑) 옆에 빠진 빨강을 놓아 보세요!",
                Outro = "3원색 완성! 상대 유닛을 썼으니 흑색 —\n주변 상대 땅을 통째로 흡수하고 한 번 더 둘 수 있어요.\n내 유닛만으로 완성하면 백색: 내 땅을 5턴 동안 지켜요.",
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
                Intro = "같은 원소끼리는 공명해요.\n화염 옆에 화염을 놓아 보세요.",
                Outro = "공명! 화염은 업화(3x3 + 십자 2칸),\n물은 해일(넓고 긴 물결), 번개는 뇌우(8칸)로 강해져요.",
                Map = BaseMap,
                Setup = b => Put(b, 2, 4, Element.Red, Side.Player),
                Card = Card.Unit(Element.Red),
                Target = new Cell(2, 3),
                Check = r => r.Resonance,
            },
            new()
            {
                Title = "스킬 카드",
                Intro = "스킬은 보드 어디에나 쓸 수 있고 턴을 쓰지 않아요.\n탈색 카드를 상대 물 유닛에 놓아 재료로 못 쓰게 만들어요.",
                Outro = "스킬은 한 턴에 한 장, 쓰고 나서 유닛도 놓을 수 있어요.\n변환·재점화·거점·지뢰 등으로 배치를 도와요. 20턴 뒤 땅이 많으면 승리!",
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
