using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ChromaFrontier.Core.Tests
{
    public class MetaTests
    {
        private static readonly Card Mine = Card.Of(Skill.Mine);
        private static readonly Card Warp = Card.Of(Skill.Warp);

        [Test]
        public void Collection_StartsWithStarterCards_AndDeckIsUnitsPlusSkills()
        {
            var c = Collection.Starter();
            Assert.AreEqual(6, c.Skills.Count);
            Assert.AreEqual(1, c.Owned(Mine));
            Assert.AreEqual(0, c.Owned(Warp));
            Assert.AreEqual(12 + 6, c.BuildDeck().Count);
            Assert.IsFalse(c.Add(Mine), "가진 장수보다 많이 넣을 수 없다");
            Assert.IsFalse(c.Add(Warp), "안 가진 카드는 못 넣는다");
        }

        [Test]
        public void Collection_BuyUpToMaxCopies_AndSkillSlotsCapTheDeck()
        {
            var c = Collection.Starter();
            Assert.IsTrue(c.Buy(Warp));
            Assert.AreEqual(1, c.InDeck(Warp), "빈자리가 있으면 산 카드는 바로 덱에 들어간다");
            Assert.IsTrue(c.Buy(Warp));
            Assert.IsFalse(c.Buy(Warp), "한 종류는 MaxCopies 장까지");
            Assert.IsTrue(c.Buy(Mine));
            Assert.IsTrue(c.Buy(Card.Of(Skill.Echo)));
            Assert.AreEqual(CardShop.SkillSlots, c.Skills.Count);
            Assert.IsTrue(c.Buy(Card.Of(Skill.Lock)), "덱이 꽉 차도 살 수는 있다");
            Assert.AreEqual(0, c.InDeck(Card.Of(Skill.Lock)));
            Assert.IsFalse(c.Add(Card.Of(Skill.Lock)));
            Assert.IsTrue(c.Remove(Warp));
            Assert.IsTrue(c.Add(Card.Of(Skill.Lock)));
        }

        [Test]
        public void Collection_SerializeRoundTrips_AndBrokenSaveFallsBackToStarter()
        {
            var c = Collection.Starter();
            c.Buy(Warp);
            c.Remove(Mine);
            var back = Collection.Parse(c.Serialize());
            Assert.AreEqual(c.Serialize(), back.Serialize());
            Assert.AreEqual(1, back.Owned(Warp));
            Assert.AreEqual(0, back.InDeck(Mine));

            Assert.AreEqual(Collection.Starter().Serialize(), Collection.Parse("").Serialize());
            Assert.AreEqual(Collection.Starter().Serialize(), Collection.Parse("garbage").Serialize());
            // 안 가진 카드가 덱에 적혀 있으면 버린다
            var cheat = Collection.Parse("mine:1|mine,mine,lock");
            Assert.AreEqual(1, cheat.Skills.Count);
        }

        [Test]
        public void Rewards_WinPaysMoreThanLoss_AndFirstClearBonus()
        {
            var stage = StageCatalog.Get(0);
            var boss = StageCatalog.Get(9);
            Assert.AreEqual(Rewards.Loss, Rewards.ForBattle(stage, 0, false, 0));
            Assert.Greater(Rewards.ForBattle(stage, 3, false, 0), Rewards.ForBattle(stage, 1, false, 0));
            Assert.Greater(Rewards.ForBattle(boss, 1, true, 0), Rewards.ForBattle(stage, 1, true, 0));
            Assert.AreEqual(Rewards.ForBattle(stage, 1, false, 0) + 2 * Rewards.PerObjective, Rewards.ForBattle(stage, 1, false, 2));
        }

        [Test]
        public void QuestLines_HaveMatchingTargetsAndRewards_InIncreasingOrder()
        {
            foreach (var q in QuestCatalog.Lines)
            {
                Assert.AreEqual(q.Targets.Length, q.Rewards.Length, q.Id);
                for (var i = 1; i < q.Targets.Length; i++) Assert.Greater(q.Targets[i], q.Targets[i - 1], q.Id);
                Assert.IsTrue(q.Finished(q.Targets.Length));
            }
        }

        // ---------------------------------------------------------------- 게임 중 목표

        /// <summary>플레이어 물 유닛이 (1,1)에 있는 판. 첫 손패는 화염부터.</summary>
        private static Game FusionGame(ObjectiveDef objective)
        {
            var b = Board.Parse(
                "PP...A",
                "PP...A",
                "PP...A");
            b[new Cell(1, 1)].Unit = Element.Blue;
            b[new Cell(1, 1)].UnitOwner = Side.Player;
            var deck = new List<Card> { Card.Unit(Element.Red), Card.Unit(Element.Yellow), Card.Unit(Element.Blue), Card.Unit(Element.Red), Card.Unit(Element.Yellow), Card.Unit(Element.Blue) };
            var g = Game.Create(b, deck, deck, 1, new StageRules { MaxTurns = 99 }, shuffle: false);
            g.SetObjectives(new[] { objective });
            return g;
        }

        private static ObjectiveDef Def(ObjectiveGoal goal, int target, ObjectiveReward reward, int amount = 0) =>
            new() { Id = "t", Goal = goal, Target = target, Reward = reward, Amount = amount, Text = "t", RewardText = "t" };

        [Test]
        public void Objective_Fusion_ClaimsCellsNextToMyLand_RightAway()
        {
            var g = FusionGame(Def(ObjectiveGoal.Fusions, 1, ObjectiveReward.Claim, 3));
            var r = g.Play(0, new Cell(2, 1));
            Assert.AreEqual(Element.Purple, r.Fused);
            Assert.IsTrue(g.Objectives[0].Done);
            Assert.AreEqual(1, g.ObjectiveEvents.Count);
            var cells = g.ObjectiveEvents[0].Cells;
            Assert.AreEqual(3, cells.Count);
            Assert.IsTrue(cells.All(c => g.Board[c].Owner == Side.Player));
            Assert.AreEqual(1, g.StatsOf(Side.Player)[Stat.Fusions]);
        }

        [Test]
        public void Objective_ExtraTurn_LetsPlayerMoveAgainAfterUnit()
        {
            var g = FusionGame(Def(ObjectiveGoal.Fusions, 1, ObjectiveReward.ExtraTurn));
            g.Play(0, new Cell(2, 1));
            Assert.IsTrue(g.BonusTurnTaken);
            Assert.AreEqual(Side.Player, g.Current);
            Assert.AreEqual(0, g.Turn, "추가 턴은 Turn 을 늘리지 않는다");
            g.Play(0, new Cell(2, 0));
            Assert.AreEqual(Side.Ai, g.Current, "보상은 한 번뿐");
        }

        [Test]
        public void Objective_Shield_ProtectsFrontline()
        {
            var g = FusionGame(Def(ObjectiveGoal.Captured, 1, ObjectiveReward.Shield));
            g.Play(0, new Cell(2, 1));
            Assert.IsTrue(g.Objectives[0].Done);
            foreach (var c in g.ObjectiveEvents[0].Cells)
            {
                Assert.AreEqual(Side.Player, g.Board[c].Owner);
                Assert.IsTrue(g.Board[c].IsProtectedFrom(Side.Ai));
            }
            Assert.IsFalse(g.Board[new Cell(0, 0)].IsProtectedFrom(Side.Ai), "안쪽 땅은 그대로 (폭풍 침묵 범위 밖)");
        }

        [Test]
        public void Objective_Beacon_PlantsFlagOnMyLand()
        {
            var g = FusionGame(Def(ObjectiveGoal.BigMove, 2, ObjectiveReward.Beacon));
            g.Play(0, new Cell(2, 1));
            var at = g.ObjectiveEvents[0].Cells.Single();
            Assert.AreEqual(Side.Player, g.Board[at].Beacon);
        }

        [Test]
        public void Objective_InSimulationClone_DoesNotTouchRealGame()
        {
            var g = FusionGame(Def(ObjectiveGoal.Fusions, 1, ObjectiveReward.Claim, 3));
            var sim = g.Clone();
            sim.Play(0, new Cell(2, 1));
            Assert.IsTrue(sim.Objectives[0].Done);
            Assert.IsFalse(g.Objectives[0].Done);
            Assert.AreEqual(0, g.StatsOf(Side.Player)[Stat.Fusions]);
        }

        [Test]
        public void Stats_MineBlast_CountsForTheMineOwner()
        {
            var b = Board.Parse(
                "P..A",
                "P..A");
            b[new Cell(2, 0)].Mine = Side.Player;
            var deck = new List<Card> { Card.Unit(Element.Red), Card.Unit(Element.Red), Card.Unit(Element.Red), Card.Unit(Element.Red), Card.Unit(Element.Red), Card.Unit(Element.Red) };
            var g = Game.Create(b, deck, deck, 1, new StageRules { MaxTurns = 99 }, shuffle: false);
            g.SetObjectives(new[] { Def(ObjectiveGoal.MinesBlown, 1, ObjectiveReward.Claim, 1) });
            g.Play(0, new Cell(1, 1));
            g.Play(0, new Cell(2, 0)); // AI 가 지뢰를 밟는다
            Assert.IsTrue(g.LastResult.MineTriggered);
            Assert.AreEqual(1, g.StatsOf(Side.Player)[Stat.MinesBlown]);
            Assert.IsTrue(g.Objectives[0].Done, "상대 차례에 일어난 일로도 목표가 이뤄진다");
        }

        [Test]
        public void Pick_IsDeterministic_DistinctAndSkipsImpossibleGoals()
        {
            var units = CardShop.BuildDeck(new Card[0]);
            for (var seed = 1; seed < 60; seed++)
            {
                var a = ObjectiveCatalog.Pick(seed, units);
                Assert.AreEqual(ObjectiveCatalog.PerGame, a.Count);
                Assert.AreEqual(a.Count, a.Distinct().Count());
                Assert.IsFalse(a.Any(d => d.Goal == ObjectiveGoal.MinesBlown || d.Goal == ObjectiveGoal.Skills), "스킬 없는 덱");
                CollectionAssert.AreEqual(a, ObjectiveCatalog.Pick(seed, units));
            }
        }

        [Test]
        public void StageGame_UsesGivenPlayerDeck_AndHasObjectives()
        {
            var deck = CardShop.BuildDeck(new[] { Mine, Mine });
            var g = StageCatalog.Get(3).CreateGame(5, deck);
            Assert.AreEqual(ObjectiveCatalog.PerGame, g.Objectives.Count);
            var d = g.DeckOf(Side.Player);
            Assert.AreEqual(deck.Count, d.Hand.Count + d.DrawCount);
        }

        // ---------------------------------------------------------------- 강화·소모품

        [Test]
        public void Collection_Upgrade_UpToMaxLevel_AndSurvivesSave()
        {
            var c = Collection.Starter();
            Assert.IsFalse(c.CanUpgrade(Warp), "안 가진 카드는 강화 못 한다");
            Assert.IsTrue(c.Upgrade(Mine));
            Assert.IsTrue(c.Upgrade(Mine));
            Assert.IsFalse(c.Upgrade(Mine), "MaxLevel 까지");
            Assert.AreEqual(CardShop.MaxLevel, c.Level(Mine));
            var back = Collection.Parse(c.Serialize());
            Assert.AreEqual(3, back.Level(Mine));
            Assert.AreEqual(1, back.Level(Card.Of(Skill.Echo)));
            Assert.AreEqual(3, back.Levels()[Mine]);
            // 강화가 생기기 전 저장 (두 칸짜리)도 읽힌다
            Assert.AreEqual(1, Collection.Parse("mine:1|mine").Level(Mine));
            Assert.Greater(CardShop.UpgradePrice(CardShop.Find(Mine), 3), CardShop.UpgradePrice(CardShop.Find(Mine), 2));
        }

        [Test]
        public void MineLevel3_BlastsFiveByFive()
        {
            var b = Board.Parse(
                ".....",
                ".....",
                ".....",
                ".....",
                ".....");
            var at = new Cell(2, 2);
            b[at].Mine = Side.Player;
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Ai, at, Element.Red, Cell.Orthogonal[0], mineLevel: 3);
            Assert.IsTrue(r.MineTriggered);
            Assert.AreEqual(25, b.Count(Side.Player));
        }

        [Test]
        public void TransmuteLevels_ClaimMoreLand()
        {
            int Claimed(int level)
            {
                var b = Board.Parse(
                    "...",
                    "...",
                    "...");
                var at = new Cell(1, 1);
                b[at].Unit = Element.Red;
                b[at].UnitOwner = Side.Ai;
                var r = Resolver.CastSkill(b, new Rng(1), Side.Player, Card.Transmute(Element.Blue), at, default, default, level);
                Assert.AreEqual(Element.Blue, b[at].Unit);
                return r.Captured.Count;
            }
            Assert.AreEqual(0, Claimed(1));
            Assert.AreEqual(1, Claimed(2));
            Assert.AreEqual(5, Claimed(3));
        }

        [Test]
        public void Loadout_PioneerFirstStrikeAndExtraObjective()
        {
            var stage = StageCatalog.Get(0);
            var plain = stage.CreateGame(9, CardShop.BuildDeck(new Card[0]));
            var l = new Loadout();
            l.Boosts.Add(Boost.Pioneer);
            l.Boosts.Add(Boost.FirstStrike);
            l.Boosts.Add(Boost.ExtraObjective);
            var g = stage.CreateGame(9, CardShop.BuildDeck(new Card[0]), l);
            Assert.AreEqual(plain.Board.Count(Side.Player) + BoostShop.PioneerCells, g.Board.Count(Side.Player));
            Assert.AreEqual(ObjectiveCatalog.PerGame + 1, g.Objectives.Count);
            var hand = g.DeckOf(Side.Player).Hand;
            var i = 0;
            while (!hand[i].IsUnit) i++;
            g.Play(i, g.LegalCells(Side.Player)[0]);
            Assert.AreEqual(Side.Player, g.Current, "선공: 첫 턴에 한 번 더");
        }

        [Test]
        public void BeaconLevel_ClaimsMorePerRound_OnlyForPlayer()
        {
            var g = FusionGame(Def(ObjectiveGoal.Tier3, 99, ObjectiveReward.Claim, 1));
            var l = new Loadout();
            l.Levels[Card.Of(Skill.Beacon)] = 2;
            g.ApplyLoadout(l);
            Assert.AreEqual(2, g.LevelOf(Side.Player, Card.Of(Skill.Beacon)));
            Assert.AreEqual(1, g.LevelOf(Side.Ai, Card.Of(Skill.Beacon)));
            Assert.AreEqual(2, g.Clone().LevelOf(Side.Player, Card.Of(Skill.Beacon)), "복제본도 같은 단계");
        }
    }
}
