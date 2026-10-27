using NUnit.Framework;

namespace ChromaFrontier.Core.Tests
{
    public class RulesTests
    {
        private static Board Empty() => new(8, 8);

        private static void Unit(Board b, int x, int y, Element e, Side owner)
        {
            var c = new Cell(x, y);
            b[c].Unit = e;
            b[c].UnitOwner = owner;
            b[c].Owner = owner;
        }

        [Test]
        public void Fuse_CoversAllPairs()
        {
            Assert.AreEqual(Element.Green, Elements.Fuse(Element.Blue, Element.Yellow));
            Assert.AreEqual(Element.Purple, Elements.Fuse(Element.Red, Element.Blue));
            Assert.AreEqual(Element.Orange, Elements.Fuse(Element.Yellow, Element.Red));
            Assert.AreEqual(Element.None, Elements.Fuse(Element.Red, Element.Red));
        }

        [Test]
        public void Placement_RequiresFrontline()
        {
            var g = Game.CreateDefault(1);
            Assert.IsTrue(g.CanPlace(Side.Player, new Cell(0, 7)));  // 내 타일
            Assert.IsTrue(g.CanPlace(Side.Player, new Cell(2, 7)));  // 상하좌우 인접
            Assert.IsFalse(g.CanPlace(Side.Player, new Cell(2, 5))); // 대각선만 인접
            Assert.IsFalse(g.CanPlace(Side.Player, new Cell(7, 0))); // 적 타일
        }

        [Test]
        public void Red_ClaimsCross()
        {
            var b = Empty();
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.AreEqual(5, r.Captured.Count);
            Assert.AreEqual(Side.Player, b[new Cell(3, 2)].Owner);
            Assert.AreEqual(Side.None, b[new Cell(4, 4)].Owner);
        }

        [Test]
        public void Blue_ClaimsLineInDirection()
        {
            var b = Empty();
            Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(0, 0), Element.Blue, new Cell(1, 0));
            for (var x = 0; x <= Balance.BlueLength; x++)
                Assert.AreEqual(Side.Player, b[new Cell(x, 0)].Owner);
            Assert.AreEqual(Side.None, b[new Cell(Balance.BlueLength + 1, 0)].Owner);
        }

        [Test]
        public void TwoElements_FuseIntoTier2_AndConsumeCatalyst()
        {
            var b = Empty();
            Unit(b, 3, 4, Element.Blue, Side.Player);
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.AreEqual(Element.Purple, r.Fused);
            Assert.AreEqual(Element.Purple, b[new Cell(3, 3)].Unit);
            Assert.IsFalse(b[new Cell(3, 4)].HasUnit);
            Assert.Greater(b[new Cell(3, 1)].Shield, 0); // 반경 2 침묵
        }

        [Test]
        public void EnemyTilesCaughtInReaction_GoToPlacer()
        {
            var b = Empty();
            Unit(b, 3, 4, Element.Yellow, Side.Ai);
            for (var x = 0; x < 8; x++) b[new Cell(x, 6)].Owner = Side.Ai;
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.AreEqual(Element.Orange, r.Fused);
            Assert.IsTrue(r.StoleCatalyst);
            Assert.AreEqual(Side.Player, b[new Cell(3, 6)].Owner); // 마그마 광선 3칸
        }

        [Test]
        public void ThreeElements_WithEnemyCatalyst_MakeBlackAndExtraTurn()
        {
            var b = Empty();
            Unit(b, 2, 3, Element.Blue, Side.Player);
            Unit(b, 4, 3, Element.Yellow, Side.Ai);
            b[new Cell(5, 5)].Owner = Side.Ai;
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.AreEqual(Element.Black, r.Fused);
            Assert.IsTrue(r.ExtraTurn);
            Assert.AreEqual(Side.Player, b[new Cell(5, 5)].Owner);
        }

        [Test]
        public void ThreeOwnElements_MakeWhiteSanctuary()
        {
            var b = Empty();
            Unit(b, 2, 3, Element.Blue, Side.Player);
            Unit(b, 4, 3, Element.Yellow, Side.Player);
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.AreEqual(Element.White, r.Fused);
            Assert.AreEqual(Balance.WhiteShield, b[new Cell(3, 3)].Shield);

            // 성역은 상대가 바꿀 수 없다.
            Resolver.PlaceUnit(b, new Rng(1), Side.Ai, new Cell(3, 5), Element.Red, default);
            Assert.AreEqual(Side.Player, b[new Cell(3, 4)].Owner);
        }

        [Test]
        public void Tier2PlusMissingColor_MakesBoostedWhite()
        {
            var b = Empty();
            Unit(b, 3, 4, Element.Purple, Side.Player); // 빨강 + 파랑
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Yellow, default);
            Assert.AreEqual(Element.White, r.Fused);
            Assert.IsTrue(r.Boosted);
            Assert.IsFalse(b[new Cell(3, 4)].HasUnit);
            Assert.AreEqual(Side.Player, b[new Cell(5, 5)].Owner); // 강화된 점유 반경 2
        }

        [Test]
        public void EnemyTier2_AsCatalyst_MakesBoostedBlack()
        {
            var b = Empty();
            Unit(b, 3, 4, Element.Green, Side.Ai); // 파랑 + 노랑
            b[new Cell(6, 6)].Owner = Side.Ai;       // 반경 3 (강화 전 2로는 못 닿음)
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.AreEqual(Element.Black, r.Fused);
            Assert.IsTrue(r.Boosted);
            Assert.IsTrue(r.ExtraTurn);
            Assert.AreEqual(Side.Player, b[new Cell(6, 6)].Owner);
        }

        [Test]
        public void Tier2WithItsOwnColor_DoesNotFuse()
        {
            var b = Empty();
            Unit(b, 3, 4, Element.Purple, Side.Player);
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.AreEqual(Element.None, r.Fused);
            Assert.AreEqual(Element.Purple, b[new Cell(3, 4)].Unit);
        }

        [Test]
        public void FireFire_Resonance_ClaimsSquarePlusLongCross()
        {
            var b = Empty();
            Unit(b, 3, 4, Element.Red, Side.Player);
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.IsTrue(r.Resonance);
            Assert.AreEqual(Element.None, r.Fused);
            Assert.AreEqual(Element.Red, b[new Cell(3, 3)].Unit);
            Assert.IsFalse(b[new Cell(3, 4)].HasUnit);                 // 공명에 쓴 이웃은 소모
            Assert.AreEqual(Side.Player, b[new Cell(4, 4)].Owner);   // 3x3 대각선
            Assert.AreEqual(Side.Player, b[new Cell(3, 1)].Owner);   // 십자 2칸
        }

        [Test]
        public void WaterWater_Resonance_IsLongerAndWider()
        {
            var b = Empty();
            Unit(b, 1, 2, Element.Blue, Side.Player);
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(1, 3), Element.Blue, new Cell(1, 0));
            Assert.IsTrue(r.Resonance);
            Assert.AreEqual(Side.Player, b[new Cell(1 + Balance.BlueSurgeLength, 3)].Owner);
            Assert.AreEqual(Side.Player, b[new Cell(3, 2)].Owner); // 폭 3
            Assert.AreEqual(Side.Player, b[new Cell(3, 4)].Owner);
        }

        [Test]
        public void LightningLightning_Resonance_StrikesMoreTiles()
        {
            var b = Empty();
            Unit(b, 3, 4, Element.Yellow, Side.Player);
            var r = Resolver.PlaceUnit(b, new Rng(5), Side.Player, new Cell(3, 3), Element.Yellow, default);
            Assert.IsTrue(r.Resonance);
            Assert.GreaterOrEqual(r.Captured.Count, Balance.YellowSurgeBolts);
        }

        [Test]
        public void FusionBeatsResonance()
        {
            var b = Empty();
            Unit(b, 3, 4, Element.Red, Side.Player);
            Unit(b, 3, 2, Element.Blue, Side.Player);
            var r = Resolver.PlaceUnit(b, new Rng(1), Side.Player, new Cell(3, 3), Element.Red, default);
            Assert.IsFalse(r.Resonance);
            Assert.AreEqual(Element.Purple, r.Fused);
        }

        [Test]
        public void GameEnds_WhenCurrentSideHasNowhereToPlace()
        {
            var b = Empty();
            b[new Cell(0, 7)].Owner = Side.Player;
            b[new Cell(7, 0)].Owner = Side.Ai;
            // 플레이어 땅과 그 이웃을 전부 막는다 (유닛으로 채움)
            Unit(b, 0, 7, Element.Grey, Side.Player);
            Unit(b, 1, 7, Element.Grey, Side.Ai);
            Unit(b, 0, 6, Element.Grey, Side.Ai);
            var deck = Game.StarterDeck();
            var g = Game.Create(b, deck, deck, 1);
            Assert.IsFalse(g.IsOver);
            g.Mulligan(); // 플레이어 → AI
            g.Mulligan(); // AI → 플레이어: 놓을 칸이 없음
            Assert.IsTrue(g.IsOver);
            Assert.AreEqual(Side.Player, g.Stuck);
        }

        [Test]
        public void EarlyHands_NeverHoldAreaLock()
        {
            for (var seed = 0; seed < 40; seed++)
            {
                var g = Game.CreateDefault(seed);
                var rng = new Rng(seed + 3);
                while (!g.IsOver && g.Turn < Balance.LockFromTurn)
                {
                    foreach (var c in g.DeckOf(g.Current).Hand)
                        Assert.AreNotEqual(Skill.Lock, c.Skill, $"seed {seed} turn {g.Turn}");
                    AiBrain.Apply(g, AiBrain.Choose(g, rng));
                }
            }
        }

        [Test]
        public void LockingOpponentsWholeFrontline_DoesNotEndTheGame()
        {
            var g = Game.Create(Game.CreateDefault(1).Board, new[] { Card.Of(Skill.Lock), Card.Unit(Element.Red) },
                Game.StarterDeck(), 1, new StageRules { LockFromTurn = 0 });
            var lockIndex = g.DeckOf(Side.Player).Hand.FindIndex(c => c.Skill == Skill.Lock);
            g.Cast(lockIndex, new Cell(6, 1)); // AI 시작 땅(6~7, 0~1)과 그 이웃을 전부 잠금
            Assert.AreEqual(0, g.LegalCells(Side.Ai).Count);
            Assert.IsFalse(g.IsOver);
            Assert.AreEqual(Side.None, g.Stuck);
        }

        [Test]
        public void Deck_RecyclesDiscardAndKeepsHandFull()
        {
            var rng = new Rng(3);
            var deck = new Deck(Game.StarterDeck(), rng);
            for (var i = 0; i < 40; i++)
            {
                deck.Play(0);
                deck.Refill(rng);
                Assert.AreEqual(Balance.HandSize, deck.Hand.Count);
            }
        }

        [Test]
        public void Deck_GuaranteesAUnitInHand()
        {
            var cards = new System.Collections.Generic.List<Card>();
            for (var i = 0; i < 10; i++) cards.Add(Card.Of(Skill.Lock));
            cards.Add(Card.Unit(Element.Red));
            for (var seed = 0; seed < 20; seed++)
            {
                var rng = new Rng(seed);
                var deck = new Deck(cards, rng);
                Assert.IsTrue(deck.HandHasUnit, $"seed {seed}");
                deck.Play(deck.Hand.FindIndex(c => c.IsUnit));
                deck.Refill(rng);
                Assert.IsTrue(deck.HandHasUnit, $"seed {seed} after replay");
            }
        }

        /// <summary>손패를 지정한 카드로 채운 판 (덱이 그 카드뿐이라 손패 5장이 모두 같다).</summary>
        private static Game WithHand(Card card)
        {
            var b = Empty();
            b[new Cell(0, 7)].Owner = Side.Player;
            b[new Cell(7, 0)].Owner = Side.Ai;
            var deck = new System.Collections.Generic.List<Card>();
            for (var i = 0; i < 8; i++) deck.Add(card);
            return Game.Create(b, deck, Game.StarterDeck(), 1, new StageRules { LockFromTurn = 0 });
        }

        [Test]
        public void Warp_SwapsTilesWithoutEndingTurn()
        {
            var g = WithHand(Card.Of(Skill.Warp));
            Unit(g.Board, 5, 5, Element.Blue, Side.Ai);
            g.Cast(0, new Cell(5, 5), new Cell(1, 7));
            Assert.AreEqual(Element.Blue, g.Board[new Cell(1, 7)].Unit);
            Assert.AreEqual(Side.Ai, g.Board[new Cell(1, 7)].Owner);
            Assert.IsFalse(g.Board[new Cell(5, 5)].HasUnit);
            Assert.AreEqual(Side.Player, g.Current);
            Assert.AreEqual(0, g.Turn);
        }

        [Test]
        public void Lock_BlocksPlacementForBothSides()
        {
            var g = WithHand(Card.Of(Skill.Lock));
            Assert.IsTrue(g.CanPlace(Side.Player, new Cell(1, 7)));
            g.Cast(0, new Cell(1, 6));
            Assert.IsFalse(g.CanPlace(Side.Player, new Cell(1, 7)));
            Assert.AreEqual(Side.Player, g.Current); // 스킬은 턴을 쓰지 않는다
        }

        [Test]
        public void Lock_LastsTwoOfMyTurns()
        {
            var g = WithHand(Card.Of(Skill.Lock));
            var cell = new Cell(4, 4);
            g.Cast(0, cell);                                   // 내 턴 1: 잠금 (턴은 계속)
            for (var i = 0; i < 4; i++)                        // 내 턴 1 끝까지, AI 턴, 내 턴 2, AI 턴 동안 유지
            {
                Assert.Greater(g.Board[cell].Lock, 0, $"step {i}");
                g.Mulligan();
            }
            Assert.AreEqual(Side.Player, g.Current);
            Assert.AreEqual(0, g.Board[cell].Lock);            // 내 턴 3에는 풀림
        }

        [Test]
        public void Bleach_MakesUnitInertForFusion()
        {
            var g = WithHand(Card.Of(Skill.Bleach));
            Unit(g.Board, 3, 4, Element.Blue, Side.Ai);
            g.Cast(0, new Cell(3, 4));
            Assert.AreEqual(Element.Grey, g.Board[new Cell(3, 4)].Unit);
            var r = Resolver.PlaceUnit(g.Board, new Rng(1), Side.Ai, new Cell(3, 3), Element.Red, default);
            Assert.AreEqual(Element.None, r.Fused);
        }

        [Test]
        public void Mine_CanBeBuriedOnAnyEmptyCell_AndExplodesWhenEnemyPlacesOnIt()
        {
            var g = WithHand(Card.Of(Skill.Mine));
            Assert.IsTrue(g.CanTarget(Side.Player, Skill.Mine, new Cell(4, 4)));  // 주인 없는 칸
            Assert.IsTrue(g.CanTarget(Side.Player, Skill.Mine, new Cell(7, 0)));  // 상대 땅
            Unit(g.Board, 3, 3, Element.Red, Side.Ai);
            Assert.IsFalse(g.CanTarget(Side.Player, Skill.Mine, new Cell(3, 3))); // 유닛이 선 칸은 안 됨
            g.Cast(0, new Cell(6, 0));                                            // 상대 전선 바로 앞
            Assert.IsFalse(g.CanTarget(Side.Player, Skill.Mine, new Cell(6, 0))); // 내 지뢰 위에 또는 안 됨
            var r = Resolver.PlaceUnit(g.Board, new Rng(1), Side.Ai, new Cell(6, 0), Element.Red, default);
            Assert.IsTrue(r.MineTriggered);
            Assert.IsFalse(g.Board[new Cell(6, 0)].HasUnit);
            Assert.AreEqual(Side.Player, g.Board[new Cell(7, 0)].Owner); // 폭발 범위의 상대 땅도 내 것
            Assert.AreEqual(Side.None, g.Board[new Cell(6, 0)].Mine);
        }

        [Test]
        public void Ai_DoesNotDodgeHiddenMine()
        {
            // AI 가 (6,0)에 화염을 놓으면 내 물·번개를 촉매로 흑색이 되는 최고의 수. 그 칸에 몰래 지뢰를 묻는다.
            var b = Empty();
            for (var y = 0; y < 3; y++)
                for (var x = 3; x < 7; x++) b[new Cell(x, y)].Owner = Side.Player;
            b[new Cell(6, 0)].Owner = Side.None;
            b[new Cell(7, 0)].Owner = Side.Ai;
            Unit(b, 5, 0, Element.Yellow, Side.Player);
            Unit(b, 6, 1, Element.Blue, Side.Player);
            var mines = new System.Collections.Generic.List<Card>();
            var reds = new System.Collections.Generic.List<Card>();
            for (var i = 0; i < 8; i++)
            {
                mines.Add(Card.Of(Skill.Mine));
                reds.Add(Card.Unit(Element.Red));
            }
            var g = Game.Create(b, mines, reds, 1);
            g.Cast(0, new Cell(6, 0));
            g.Mulligan(); // 스킬은 턴을 안 쓰므로 패 버리기로 턴을 넘긴다
            var move = AiBrain.Choose(g, new Rng(7));
            Assert.AreEqual(new Cell(6, 0), move.At);
            AiBrain.Apply(g, move);
            Assert.IsTrue(g.LastResult.MineTriggered);
        }

        [Test]
        public void CloneFor_HidesOpponentMines()
        {
            var g = WithHand(Card.Of(Skill.Mine));
            g.Cast(0, new Cell(0, 7));
            Assert.AreEqual(Side.None, g.CloneFor(Side.Ai).Board[new Cell(0, 7)].Mine);
            Assert.AreEqual(Side.Player, g.CloneFor(Side.Player).Board[new Cell(0, 7)].Mine);
        }

        [Test]
        public void Skill_DoesNotEndTurn_AndOnlyOnePerTurn()
        {
            var g = WithHand(Card.Of(Skill.Mine));
            g.Cast(0, new Cell(4, 4));
            Assert.AreEqual(Side.Player, g.Current);
            Assert.AreEqual(0, g.Turn);
            Assert.IsFalse(g.CanTarget(Side.Player, Skill.Mine, new Cell(5, 5))); // 두 번째 스킬은 못 씀
            g.Mulligan();
            g.Mulligan();
            Assert.IsTrue(g.CanTarget(Side.Player, Skill.Mine, new Cell(5, 5)));  // 다음 내 턴에는 다시 됨
        }

        [Test]
        public void Transmute_RecolorsUnit_AndSetsUpFusion()
        {
            var g = WithHand(Card.Transmute(Element.Blue));
            Unit(g.Board, 1, 7, Element.Red, Side.Player);
            Unit(g.Board, 3, 3, Element.Red, Side.Ai);
            Assert.IsFalse(g.CanTarget(Side.Player, g.DeckOf(Side.Player).Hand[0], new Cell(4, 4))); // 유닛 없음
            g.Cast(0, new Cell(3, 3));                         // 상대 화염을 물로
            Assert.AreEqual(Element.Blue, g.Board[new Cell(3, 3)].Unit);
            Assert.AreEqual(Side.Ai, g.Board[new Cell(3, 3)].UnitOwner);
            var r = Resolver.PlaceUnit(g.Board, new Rng(1), Side.Player, new Cell(3, 4), Element.Red, default);
            Assert.AreEqual(Element.Purple, r.Fused);          // 물 + 화염 = 폭풍
        }

        /// <summary>바꾼 유닛 옆에 다른 원소가 있으면 그 자리에서 융합한다. 상대 유닛을 바꿨어도 반응하면 내 것이 된다.</summary>
        [Test]
        public void Transmute_FusesWithNeighbor_AndTakesTheUnit()
        {
            var g = WithHand(Card.Transmute(Element.Red));
            Unit(g.Board, 3, 3, Element.Yellow, Side.Ai);    // 상대 번개를
            Unit(g.Board, 4, 3, Element.Blue, Side.Player);  // 내 물 옆에서 화염으로
            var r = g.Cast(0, new Cell(3, 3));
            Assert.AreEqual(Element.Purple, r.Fused);        // 화염 + 물 = 폭풍
            Assert.AreEqual(Element.Purple, g.Board[new Cell(3, 3)].Unit);
            Assert.AreEqual(Side.Player, g.Board[new Cell(3, 3)].UnitOwner);
            Assert.AreEqual(Side.Player, g.Board[new Cell(3, 3)].Owner);
            CollectionAssert.Contains(r.Catalysts, new Cell(4, 3));
            Assert.IsFalse(g.Board[new Cell(4, 3)].HasUnit);
        }

        /// <summary>융합할 이웃이 없고 같은 원소 이웃이 있으면 공명한다 (화염 → 업화: 3x3 + 십자 2칸).</summary>
        [Test]
        public void Transmute_ResonatesWithSameElementNeighbor()
        {
            var g = WithHand(Card.Transmute(Element.Red));
            Unit(g.Board, 3, 3, Element.Yellow, Side.Player);
            Unit(g.Board, 3, 4, Element.Red, Side.Player);
            var r = g.Cast(0, new Cell(3, 3));
            Assert.IsTrue(r.Resonance);
            Assert.AreEqual(Element.Red, r.Placed);
            Assert.IsFalse(g.Board[new Cell(3, 4)].HasUnit);
            Assert.AreEqual(Element.Red, g.Board[new Cell(3, 3)].Unit);
            foreach (var c in new[] { new Cell(2, 2), new Cell(4, 4), new Cell(3, 1), new Cell(5, 3) })
                Assert.AreEqual(Side.Player, g.Board[c].Owner, c.ToString());
        }

        [Test]
        public void Transmute_OnSilence_OnlyRecolors()
        {
            var g = WithHand(Card.Transmute(Element.Red));
            g.Board[new Cell(3, 3)].Terrain = Terrain.Silence;
            Unit(g.Board, 3, 3, Element.Yellow, Side.Ai);
            Unit(g.Board, 4, 3, Element.Blue, Side.Player);
            var r = g.Cast(0, new Cell(3, 3));
            Assert.AreEqual(Element.None, r.Fused);
            Assert.IsFalse(r.Resonance);
            Assert.AreEqual(Side.Ai, g.Board[new Cell(3, 3)].UnitOwner);
            Assert.IsTrue(g.Board[new Cell(4, 3)].HasUnit);
        }

        /// <summary>변환으로 흑색이 되면 (스킬은 차례를 안 끝내므로) 이어서 유닛을 놓은 뒤 한 번 더 둔다.</summary>
        [Test]
        public void Transmute_IntoBlack_GivesExtraTurnAfterNextUnit()
        {
            var b = Empty();
            b[new Cell(0, 7)].Owner = Side.Player;
            b[new Cell(7, 0)].Owner = Side.Ai;
            var deck = new System.Collections.Generic.List<Card> { Card.Transmute(Element.Red) };
            for (var i = 0; i < 8; i++) deck.Add(Card.Unit(Element.Red));
            var g = Game.Create(b, deck, Game.StarterDeck(), 1, new StageRules { LockFromTurn = 0 }, shuffle: false);
            Unit(g.Board, 3, 3, Element.Yellow, Side.Player);
            Unit(g.Board, 4, 3, Element.Blue, Side.Ai);      // 상대 유닛이 촉매 → 흑색
            Unit(g.Board, 3, 4, Element.Yellow, Side.Ai);
            var hand = g.DeckOf(Side.Player).Hand;
            var t = hand.FindIndex(c => c.Skill == Skill.Transmute);
            var r = g.Cast(t, new Cell(3, 3));
            Assert.AreEqual(Element.Black, r.Fused);
            Assert.AreEqual(Side.Player, g.Current);
            var u = g.DeckOf(Side.Player).Hand.FindIndex(c => c.IsUnit);
            g.Play(u, g.LegalCells(Side.Player)[0]);
            Assert.AreEqual(Side.Player, g.Current, "흑색의 한 번 더");
            Assert.IsTrue(g.ExtraTurnNow);
            g.Play(g.DeckOf(Side.Player).Hand.FindIndex(c => c.IsUnit), g.LegalCells(Side.Player)[0]);
            Assert.AreEqual(Side.Ai, g.Current, "한 번만");
        }

        [Test]
        public void Echo_RepeatsBaseEffectWithoutConsumingUnit()
        {
            var g = WithHand(Card.Of(Skill.Echo));
            Unit(g.Board, 3, 3, Element.Red, Side.Player);
            Unit(g.Board, 5, 5, Element.Red, Side.Ai);
            Assert.IsFalse(g.CanTarget(Side.Player, Skill.Echo, new Cell(5, 5))); // 상대 유닛은 안 됨
            var r = g.Cast(0, new Cell(3, 3));
            Assert.AreEqual(4, r.Captured.Count);              // 십자 4칸
            Assert.AreEqual(Side.Player, g.Board[new Cell(3, 2)].Owner);
            Assert.AreEqual(Element.Red, g.Board[new Cell(3, 3)].Unit);
        }

        [Test]
        public void Surge_ClaimsThreeByThreeAroundMyUnit_UnitStays()
        {
            var g = WithHand(Card.Of(Skill.Surge));
            Unit(g.Board, 3, 3, Element.Green, Side.Player);
            Unit(g.Board, 5, 5, Element.Red, Side.Ai);
            Assert.IsFalse(g.CanTarget(Side.Player, Skill.Surge, new Cell(5, 5)));
            var r = g.Cast(0, new Cell(3, 3));
            Assert.AreEqual(8, r.Captured.Count); // 가운데는 이미 내 땅
            Assert.AreEqual(Side.Player, g.Board[new Cell(2, 2)].Owner);
            Assert.AreEqual(Element.Green, g.Board[new Cell(3, 3)].Unit);
        }

        [Test]
        public void Siphon_StealsEnemyUnitAndTile_ButNotTier3()
        {
            var g = WithHand(Card.Of(Skill.Siphon));
            Unit(g.Board, 4, 4, Element.Blue, Side.Ai);
            Unit(g.Board, 6, 6, Element.Black, Side.Ai);
            Assert.IsFalse(g.CanTarget(Side.Player, Skill.Siphon, new Cell(6, 6)));
            g.Cast(0, new Cell(4, 4));
            Assert.AreEqual(Side.Player, g.Board[new Cell(4, 4)].UnitOwner);
            Assert.AreEqual(Side.Player, g.Board[new Cell(4, 4)].Owner);
        }

        [Test]
        public void Barrier_ShieldsMyTilesOnly()
        {
            var g = WithHand(Card.Of(Skill.Barrier));
            g.Board[new Cell(1, 7)].Owner = Side.Player;
            g.Board[new Cell(1, 6)].Owner = Side.Ai;
            g.Cast(0, new Cell(0, 7));
            Assert.Greater(g.Board[new Cell(1, 7)].Shield, 0);
            Assert.AreEqual(0, g.Board[new Cell(1, 6)].Shield);
            Assert.IsTrue(g.Board[new Cell(1, 7)].IsProtectedFrom(Side.Ai));
        }

        [Test]
        public void Split_CopiesUnitIntoEmptyNeighbor()
        {
            var g = WithHand(Card.Of(Skill.Split));
            Unit(g.Board, 3, 3, Element.Yellow, Side.Player);
            var r = g.Cast(0, new Cell(3, 3));
            Assert.AreEqual(1, r.Catalysts.Count);
            var c = r.Catalysts[0];
            Assert.AreEqual(Element.Yellow, g.Board[c].Unit);
            Assert.AreEqual(Side.Player, g.Board[c].UnitOwner);
            Assert.AreEqual(1, c.Manhattan(new Cell(3, 3)));
        }

        [Test]
        public void Quake_DestroysEnemyUnitsOnly_AndNeedsATarget()
        {
            var g = WithHand(Card.Of(Skill.Quake));
            Unit(g.Board, 4, 4, Element.Blue, Side.Ai);
            Unit(g.Board, 5, 5, Element.Red, Side.Player);
            Assert.IsFalse(g.CanTarget(Side.Player, Skill.Quake, new Cell(1, 1)));
            g.Cast(0, new Cell(4, 5));
            Assert.IsFalse(g.Board[new Cell(4, 4)].HasUnit);
            Assert.IsTrue(g.Board[new Cell(5, 5)].HasUnit);
        }

        [Test]
        public void ExtraTurn_EndsGameAtOnce_WhenOpponentHasNoLandLeft()
        {
            // 상대 땅은 한 칸뿐이고, 화염 십자로 그 칸을 빼앗는다. 선공(한 번 더)이 남아 있어도 바로 끝나야 한다.
            var b = Empty();
            b[new Cell(3, 3)].Owner = Side.Player;
            b[new Cell(3, 5)].Owner = Side.Ai;
            var deck = new System.Collections.Generic.List<Card>();
            for (var i = 0; i < 8; i++) deck.Add(Card.Unit(Element.Red));
            var g = Game.Create(b, deck, Game.StarterDeck(), 1);
            var l = new Loadout();
            l.Boosts.Add(Boost.FirstStrike);
            g.ApplyLoadout(l);
            g.Play(0, new Cell(3, 4));
            Assert.AreEqual(0, g.Board.Count(Side.Ai));
            Assert.IsTrue(g.IsOver, "한 번 더 둘 차례지만 상대 땅이 없으면 바로 끝");
            Assert.AreEqual(Side.Player, g.Winner);
        }

        [Test]
        public void ExtraTurn_Flag_IsSetOnBonusTurn()
        {
            var b = Empty();
            b[new Cell(0, 7)].Owner = Side.Player;
            b[new Cell(7, 0)].Owner = Side.Ai;
            var deck = new System.Collections.Generic.List<Card>();
            for (var i = 0; i < 8; i++) deck.Add(Card.Unit(Element.Red));
            var g = Game.Create(b, deck, Game.StarterDeck(), 1);
            var l = new Loadout();
            l.Boosts.Add(Boost.FirstStrike);
            g.ApplyLoadout(l);
            g.Play(0, new Cell(1, 7));
            Assert.IsTrue(g.ExtraTurnNow);
            g.Play(0, new Cell(2, 7));
            Assert.IsFalse(g.ExtraTurnNow);
        }

        [Test]
        public void OldEchoSave_BecomesSurge()
        {
            var c = Collection.Parse("echo:2|echo|echo:3");
            Assert.AreEqual(2, c.Owned(Card.Of(Skill.Surge)));
            Assert.AreEqual(1, c.InDeck(Card.Of(Skill.Surge)));
            Assert.AreEqual(3, c.Level(Card.Of(Skill.Surge)));
        }

        [Test]
        public void Beacon_GrowsEachRound_AndFallsWhenCaptured()
        {
            var g = WithHand(Card.Of(Skill.Beacon));
            var at = new Cell(0, 7);
            g.Cast(0, at);
            Assert.AreEqual(Side.Player, g.Board[at].Beacon);
            var before = g.Board.Count(Side.Player);
            g.Mulligan();
            g.Mulligan();                                       // 라운드 끝: 주변 한 칸 점유
            Assert.AreEqual(before + Balance.BeaconClaims, g.Board.Count(Side.Player));
            Assert.AreEqual(RoundEventKind.Beacon, g.RoundEvents[0].Kind);
            Resolver.PlaceUnit(g.Board, new Rng(1), Side.Ai, new Cell(1, 7), Element.Red, default); // 십자로 거점 칸을 빼앗음
            Assert.AreEqual(Side.Ai, g.Board[at].Owner);
            Assert.AreEqual(Side.None, g.Board[at].Beacon);
        }

        [Test]
        public void AiVsAi_UsesEverySkillAcrossGames()
        {
            var used = new System.Collections.Generic.HashSet<Skill>();
            for (var seed = 0; seed < 20; seed++)
            {
                var g = Game.CreateDefault(seed);
                var tie = new Rng(seed + 100);
                for (var guard = 0; !g.IsOver && guard < 200; guard++)
                {
                    AiBrain.Apply(g, AiBrain.Choose(g, tie));
                    if (g.LastResult != null && g.LastResult.Skill != Skill.None) used.Add(g.LastResult.Skill);
                }
            }
            TestContext.WriteLine(string.Join(", ", used));
            CollectionAssert.IsSupersetOf(used, new[] { Skill.Warp, Skill.Lock, Skill.Bleach, Skill.Mine, Skill.Transmute, Skill.Echo, Skill.Beacon });
        }

        /// <summary>AI 끼리 둘 때 턴마다 스킬을 쓰는 비율과 스킬별 횟수 (상세 출력으로 확인).</summary>
        [Test]
        public void AiVsAi_SkillUsageReport()
        {
            var counts = new System.Collections.Generic.Dictionary<Skill, int>();
            int turns = 0, skillTurns = 0;
            for (var seed = 0; seed < 30; seed++)
            {
                var g = Game.CreateDefault(seed);
                var tie = new Rng(seed + 100);
                for (var guard = 0; !g.IsOver && guard < 200; guard++)
                {
                    var turn = g.Turn;
                    var side = g.Current;
                    AiBrain.Apply(g, AiBrain.Choose(g, tie, AiLevel.Normal));
                    var r = g.LastResult;
                    if (r != null && r.Skill != Skill.None)
                    {
                        counts[r.Skill] = counts.TryGetValue(r.Skill, out var n) ? n + 1 : 1;
                        skillTurns++;
                    }
                    if (g.Turn != turn || g.Current != side) turns++;
                }
            }
            var parts = new System.Collections.Generic.List<string>();
            foreach (var kv in counts) parts.Add($"{kv.Key} {kv.Value}");
            TestContext.WriteLine($"skill per turn {(float)skillTurns / turns:P0} ({skillTurns}/{turns}) — {string.Join(", ", parts)}");
            Assert.Greater(skillTurns, turns / 5); // 스킬이 실제로 자주 쓰인다
        }

        /// <summary>플레이어 자리를 어려움 AI 가 대신 두고, 상대 AI 난이도별로 플레이어 승률을 잰다.</summary>
        private static float PlayerWinRate(AiLevel aiLevel, int games)
        {
            var wins = 0f;
            for (var seed = 0; seed < games; seed++)
            {
                var g = Game.CreateDefault(seed * 7919 + 13);
                var rng = new Rng(seed + 1);
                for (var guard = 0; !g.IsOver && guard < 200; guard++)
                    AiBrain.Apply(g, AiBrain.Choose(g, rng, g.Current == Side.Player ? AiLevel.Perfect : aiLevel));
                wins += g.Winner == Side.Player ? 1f : g.Winner == Side.None ? 0.5f : 0f;
            }
            return wins / games;
        }

        [Test]
        public void AiLevels_EasierLevelsLoseMoreOften()
        {
            var beginner = PlayerWinRate(AiLevel.Beginner, 60);
            var easy = PlayerWinRate(AiLevel.Easy, 60);
            var normal = PlayerWinRate(AiLevel.Normal, 60);
            var hard = PlayerWinRate(AiLevel.Hard, 60);
            var perfect = PlayerWinRate(AiLevel.Perfect, 60);
            TestContext.WriteLine($"player(perfect AI) win rate vs beginner {beginner:P0}, easy {easy:P0}, normal {normal:P0}, hard {hard:P0}, perfect {perfect:P0}");
            Assert.GreaterOrEqual(beginner, easy);
            Assert.Greater(easy, normal);
            Assert.Greater(normal, hard);
            Assert.Greater(hard, perfect);
        }

        [Test]
        public void AiVsAi_GameEndsAfterMaxTurns()
        {
            var g = Game.CreateDefault(7);
            var tie = new Rng(9);
            for (var guard = 0; !g.IsOver && guard < 200; guard++)
                AiBrain.Apply(g, AiBrain.Choose(g, tie));
            Assert.IsTrue(g.IsOver);
            Assert.AreEqual(g.Board.Playable, g.Board.Count(Side.Player) + g.Board.Count(Side.Ai) + g.Board.Count(Side.None));
        }
    }
}
