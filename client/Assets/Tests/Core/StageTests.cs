using NUnit.Framework;

namespace ChromaFrontier.Core.Tests
{
    public class StageTests
    {
        [Test]
        public void Catalog_Has50Stages_WithBossEveryTen()
        {
            Assert.AreEqual(StageCatalog.Count, StageCatalog.All.Length);
            for (var i = 0; i < StageCatalog.Count; i++)
                Assert.AreEqual(i % 10 == 9, StageCatalog.All[i].IsBoss, StageCatalog.All[i].Code);
        }

        [Test]
        public void EveryStage_StartsWithMovesForBothSides_AndPlaysToTheEnd()
        {
            foreach (var s in StageCatalog.All)
            {
                var g = s.CreateGame(s.Index + 1);
                Assert.Greater(g.LegalCells(Side.Player).Count, 0, s.Code);
                var rng = new Rng(s.Index + 7);
                for (var guard = 0; !g.IsOver && guard < 200; guard++)
                    AiBrain.Apply(g, AiBrain.Choose(g, rng, AiLevel.Normal, g.Current == Side.Ai ? s.Persona : null));
                Assert.IsTrue(g.IsOver, s.Code);
            }
        }

        [Test]
        public void Ignis_FieldsOnlyFireUnits()
        {
            foreach (var c in StageCatalog.Get(9).AiDeck)
                Assert.IsTrue(!c.IsUnit || c.Element == Element.Red);
        }

        [Test]
        public void Ice_SlidesOneMoreTileAwayFromOwnLand()
        {
            var b = Board.Parse(
                "....",
                ".~..",
                ".P..",
                "....");
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(1, 1), Element.Red, default);
            Assert.IsTrue(r.Slid);
            Assert.AreEqual(new Cell(1, 0), r.At);
            Assert.AreEqual(Element.Red, b[new Cell(1, 0)].Unit);
            Assert.IsFalse(b[new Cell(1, 1)].HasUnit);
        }

        [Test]
        public void Silence_BlocksFusion()
        {
            var b = Board.Parse(
                "...",
                ".s.",
                "...");
            b[new Cell(1, 2)].Unit = Element.Blue;
            b[new Cell(1, 2)].UnitOwner = Side.Player;
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(1, 1), Element.Red, default);
            Assert.AreEqual(Element.None, r.Fused);
            Assert.AreEqual(Element.Blue, b[new Cell(1, 2)].Unit);
        }

        [Test]
        public void Warp_LeavesTerrainInPlace()
        {
            var b = Board.Parse("~P.", "...", "..A");
            Resolver.CastSkill(b, Side.Player, Skill.Warp, new Cell(0, 0), new Cell(1, 0));
            Assert.AreEqual(Terrain.Ice, b[new Cell(0, 0)].Terrain);
            Assert.AreEqual(Side.Player, b[new Cell(0, 0)].Owner);
        }

        /// <summary>플레이어·AI 가 한 번씩 멀리건해서 라운드 하나를 끝낸다.</summary>
        private static void PassRound(Game g)
        {
            g.Mulligan();
            g.Mulligan();
        }

        [Test]
        public void Conveyor_ShiftsContentsAndDropsAtTheEnd()
        {
            var g = Game.Create(Board.Parse(
                "P....",
                ">>>..",
                "....A"), Game.StarterDeck(), Game.StarterDeck(), 1);
            g.Board[new Cell(0, 1)].Owner = Side.Player;
            g.Board[new Cell(2, 1)].Owner = Side.Ai;
            PassRound(g);
            Assert.AreEqual(Side.None, g.Board[new Cell(0, 1)].Owner);
            Assert.AreEqual(Side.Player, g.Board[new Cell(1, 1)].Owner);
            Assert.AreEqual(Side.None, g.Board[new Cell(2, 1)].Owner); // 벨트 끝의 AI 땅은 떨어져 사라짐
        }

        [Test]
        public void BlackHole_EmptiesNeighborsEachRound()
        {
            var g = Game.Create(Board.Parse(
                "P....",
                ".PO..",
                "....A"), Game.StarterDeck(), Game.StarterDeck(), 1);
            PassRound(g);
            Assert.AreEqual(Side.None, g.Board[new Cell(1, 1)].Owner);
            Assert.AreEqual(Side.Player, g.Board[new Cell(0, 0)].Owner);
        }

        [Test]
        public void Volcano_BlowsUpOneRandomUnitOfEachSide_AndLeavesNoLava()
        {
            var g = Game.Create(Board.Parse(
                "P....",
                ".....",
                ".....",
                ".....",
                "....A"), Game.StarterDeck(), Game.StarterDeck(), 3, new StageRules { LavaEvery = 1, LavaCount = 1 });
            foreach (var (x, y, side) in new[] { (1, 1, Side.Player), (2, 1, Side.Player), (3, 3, Side.Ai), (2, 3, Side.Ai) })
            {
                ref var t = ref g.Board[new Cell(x, y)];
                t.Owner = t.UnitOwner = side;
                t.Unit = Element.Red;
            }
            var playable = g.Board.Playable;
            PassRound(g);
            Assert.AreEqual(2, g.RoundEvents.Count);
            var sides = new System.Collections.Generic.HashSet<Side>();
            foreach (var ev in g.RoundEvents)
            {
                var t = g.Board[ev.Cell];
                Assert.IsFalse(t.HasUnit);
                Assert.AreEqual(Side.None, t.Owner);
                Assert.IsFalse(t.Blocked);
                sides.Add(ev.Cell.X + ev.Cell.Y <= 3 ? Side.Player : Side.Ai);
            }
            Assert.AreEqual(2, sides.Count);           // 양쪽에서 하나씩
            Assert.AreEqual(playable, g.Board.Playable); // 막힌 칸이 생기지 않는다
        }
    }
}
