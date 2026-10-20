using NUnit.Framework;

namespace ChromaFrontier.Core.Tests
{
    public class PuzzleTests
    {
        private static PuzzleDef Tiny() => new()
        {
            Name = "t", Hint = "t", Moves = 2,
            Map = new[] { "....", "....", "PP..", "PP.." },
            Units = new[] { "....", "....", "....", "...." },
            Hand = "R R",
        };

        [Test]
        public void Solo_OpponentNeverMoves_AndUsedCardsDoNotComeBack()
        {
            var g = Tiny().CreateGame();
            Assert.AreEqual(2, g.DeckOf(Side.Player).Hand.Count);
            g.Play(0, new Cell(2, 2));
            Assert.AreEqual(Side.Player, g.Current, "상대는 두지 않는다");
            Assert.AreEqual(1, g.Turn);
            Assert.AreEqual(1, g.DeckOf(Side.Player).Hand.Count, "쓴 카드는 다시 안 들어온다");
            Assert.IsFalse(g.IsOver);
            g.Play(0, new Cell(1, 1));
            Assert.IsTrue(g.IsOver, "수를 다 썼다");
            Assert.IsFalse(g.Solved);
        }

        [Test]
        public void Solo_FillingEveryCell_SolvesRightAway()
        {
            var p = new PuzzleDef
            {
                Name = "t", Hint = "t", Moves = 3,
                Map = new[] { "PPP", "P.P", "PPP" },
                Units = new[] { "...", "...", "..." },
                Hand = "R R R",
            };
            var g = p.CreateGame();
            g.Play(0, new Cell(1, 1));
            Assert.IsTrue(g.Solved);
            Assert.IsTrue(g.IsOver, "남은 수가 있어도 다 물들이면 끝");
        }

        [Test]
        public void EveryFloor_IsSolvableInItsMoves_ButNotInFewer()
        {
            Assert.AreEqual(30, PuzzleCatalog.Count);
            var errors = new System.Collections.Generic.List<string>();
            foreach (var p in PuzzleCatalog.Floors)
            {
                if (string.IsNullOrEmpty(p.Name) || string.IsNullOrEmpty(p.Hint)) errors.Add($"{p.Floor}층: 이름·힌트");
                var r = PuzzleSolver.Solve(p.CreateGame());
                if (r.Solution == null) errors.Add($"{p.Floor}층 {p.Name}: 안 풀린다");
                if (p.Moves <= 1) continue;
                var less = PuzzleSolver.Solve(p.CreateGame(p.Moves - 1));
                if (less.Solution != null) errors.Add($"{p.Floor}층 {p.Name}: {p.Moves - 1}수로도 풀린다");
                if (less.Exhausted) errors.Add($"{p.Floor}층: 탐색 상한");
            }
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void Floors_UseNoRandomCards()
        {
            foreach (var p in PuzzleCatalog.Floors)
                foreach (var c in p.Cards())
                {
                    Assert.AreNotEqual(Element.Yellow, c.IsUnit ? c.Element : Element.None, $"{p.Floor}층: 번개 카드는 무작위");
                    Assert.IsFalse(c.Skill == Skill.Mine || c.Skill == Skill.Lock || c.Skill == Skill.Beacon, $"{p.Floor}층: 상대가 없으면 의미 없는 스킬");
                }
        }
    }
}
