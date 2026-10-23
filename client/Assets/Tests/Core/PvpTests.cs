using System.Collections.Generic;
using NUnit.Framework;

namespace ChromaFrontier.Core.Tests
{
    public class PvpTests
    {
        private static readonly string[] Decks4 = { "tr_red,bleach,mine,surge,siphon", "split,beacon,warp,lock,barrier", "tr_blue,tr_yellow,quake,split", "" };

        /// <summary>AI 가 고른 수를 PvpMove 로 바꾼다 (실제 기기가 보내는 것과 같은 모양).</summary>
        private static PvpMove ToMove(Game g, AiBrain.Move m)
        {
            if (m.Mulligan) return PvpMove.Pass(g.CurrentSeat);
            return m.IsSkill ? PvpMove.Cast(g.CurrentSeat, m.HandIndex, m.At, m.Target2) : PvpMove.Play(g.CurrentSeat, m.HandIndex, m.At, m.Dir);
        }

        /// <summary>한 기기가 두는 대로 수를 문자열로 남기고, 다른 기기가 그 문자열만으로 재생하면 같은 판이 된다.</summary>
        [Test]
        public void Replay_FromEncodedMoves_ReachesTheSameBoard([Values(1, 2)] int teamSize)
        {
            foreach (var map in PvpCatalog.MapsFor(teamSize))
            {
                var decks = teamSize == 2 ? Decks4 : new[] { Decks4[0], Decks4[1] };
                var host = PvpCatalog.CreateGame(map, decks, 1234);
                var log = new List<string>();
                var rng = new Rng(5);
                for (var guard = 0; !host.IsOver && guard < 300; guard++)
                {
                    var move = ToMove(host, AiBrain.Choose(host, rng, AiLevel.Normal, null));
                    log.Add(move.Encode());
                    Assert.IsTrue(move.TryApply(host), map.Id + " " + move.Encode());
                }
                Assert.IsTrue(host.IsOver, map.Id);

                var guest = PvpCatalog.CreateGame(map, decks, 1234);
                foreach (var s in log)
                {
                    Assert.IsTrue(PvpMove.TryDecode(s, out var m), s);
                    Assert.IsTrue(m.TryApply(guest), map.Id + " " + s);
                }
                Assert.IsTrue(guest.IsOver, map.Id);
                Assert.AreEqual(host.Turn, guest.Turn, map.Id);
                foreach (var c in host.Board.Cells())
                {
                    Assert.AreEqual(host.Board[c].Owner, guest.Board[c].Owner, map.Id + c);
                    Assert.AreEqual(host.Board[c].Unit, guest.Board[c].Unit, map.Id + c);
                }
            }
        }

        [Test]
        public void TeamGame_RotatesSeats_TeamsAlternate_EachSeatPlaysItsOwnHand()
        {
            var g = PvpCatalog.CreateGame(PvpCatalog.Team[0], Decks4, 7);
            Assert.AreEqual(4, g.Seats);
            for (var turn = 0; turn < 8; turn++)
            {
                var seat = turn % 4;
                Assert.AreEqual(seat, g.CurrentSeat);
                Assert.AreEqual(Game.SideOfSeat(seat), g.Current);
                Assert.AreSame(g.DeckOfSeat(seat), g.DeckOf(g.Current));
                var other = g.DeckOfSeat((seat + 2) % 4); // 같은 팀의 다른 사람
                var before = new List<Card>(other.Hand);
                Assert.IsTrue(PvpMove.Pass(seat).TryApply(g));
                CollectionAssert.AreEqual(before, other.Hand, "짝의 손패는 그대로");
            }
        }

        [Test]
        public void Moves_FromTheWrongSeat_OrIllegal_AreIgnored()
        {
            var g = PvpCatalog.CreateGame(PvpCatalog.Duel[0], new[] { "", "" }, 3);
            var unit = g.DeckOf(Side.Player).Hand.FindIndex(c => c.IsUnit);
            Assert.IsFalse(PvpMove.Play(1, unit, new Cell(2, 6), Cell.Orthogonal[0]).TryApply(g), "상대 자리");
            Assert.IsFalse(PvpMove.Play(0, unit, new Cell(5, 1), Cell.Orthogonal[0]).TryApply(g), "전선이 아닌 칸");
            Assert.IsFalse(PvpMove.Play(0, 9, new Cell(2, 6), Cell.Orthogonal[0]).TryApply(g), "없는 손패");
            Assert.AreEqual(0, g.Turn);
            Assert.IsFalse(PvpMove.TryDecode("0,X,0,0,0,0,0", out _));
            Assert.IsTrue(PvpMove.TryDecode(PvpMove.Cast(1, 2, new Cell(3, 4), new Cell(5, 6)).Encode(), out var m));
            Assert.AreEqual(PvpMoveKind.Cast, m.Kind);
            Assert.AreEqual(new Cell(5, 6), m.Extra);
        }

        [Test]
        public void ParseSkills_DropsUnknownCodes_AndCapsCopies()
        {
            var list = PvpCatalog.ParseSkills("mine,mine,mine,nope,warp");
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(2, list.FindAll(c => c.Skill == Skill.Mine).Count);
        }
    }
}
