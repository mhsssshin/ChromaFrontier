using System.Collections.Generic;
using System.Linq;
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

        /// <summary>봇 10명: 덱·스킨·이모티콘이 올바르고, 생각 시간은 차례 제한 안이다.</summary>
        [Test]
        public void Bots_HaveValidDecksSkinsAndEmotes()
        {
            Assert.AreEqual(10, PvpBots.All.Length);
            var ids = new HashSet<string>();
            foreach (var b in PvpBots.All)
            {
                Assert.IsTrue(ids.Add(b.Id), b.Id);
                Assert.IsTrue(PvpBots.IsBot(b.Uid));
                Assert.AreSame(b, PvpBots.Find(b.Uid));
                Assert.AreEqual(b.Deck.Split(',').Length, PvpCatalog.ParseSkills(b.Deck).Count, b.Id + " 덱에 버려지는 카드가 있다");
                TileSkins.Split(b.Skin, out var mat, out var color);
                Assert.AreEqual(b.Skin.Split(':')[0], mat, b.Id);
                Assert.AreEqual(b.Skin.Split(':')[1], color, b.Id);
                Assert.AreEqual(b.Skin.Split(':')[2], TileSkins.UnitPart(b.Skin), b.Id);
                foreach (var pool in new[] { b.Good, b.Hurt, b.Win, b.Lose, b.Hello })
                    foreach (var e in pool) Assert.That(e, Is.InRange(0, EmotePacks.Total - 1), b.Id);
            }
            Assert.IsFalse(PvpBots.IsBot("abc"));
            Assert.IsNull(PvpBots.Find("bot:nobody"));
        }

        /// <summary>봇끼리 모든 대전 맵을 끝까지 둔다: 봇이 고른 수는 늘 규칙에 맞고, 생각 시간은 차례 제한보다 짧다.</summary>
        [Test]
        public void Bots_PlayEveryMapToTheEnd_WithLegalMoves([Values(1, 2)] int teamSize)
        {
            var rng = new Rng(11);
            var k = 0;
            foreach (var map in PvpCatalog.MapsFor(teamSize))
            {
                var bots = new PvpBot[teamSize * 2];
                var decks = new string[bots.Length];
                for (var i = 0; i < bots.Length; i++)
                {
                    bots[i] = PvpBots.All[k++ % PvpBots.All.Length];
                    decks[i] = bots[i].Deck;
                }
                var g = PvpCatalog.CreateGame(map, decks, 99 + k);
                for (var guard = 0; !g.IsOver && guard < 300; guard++)
                {
                    var bot = bots[g.CurrentSeat];
                    var think = PvpBots.ThinkSeconds(g, bot, rng);
                    Assert.That(think, Is.InRange(0.5f, PvpCatalog.TurnSeconds - 5f), bot.Id);
                    var move = PvpBots.ChooseMove(g, bot, rng);
                    Assert.IsTrue(move.TryApply(g), map.Id + " " + bot.Id + " " + move.Encode());
                }
                Assert.IsTrue(g.IsOver, map.Id);
            }
        }

        /// <summary>봇 방 일정: 같은 시각이면 어느 기기에서나 같은 방, 한 봇은 한 번에 한 방에만, 방 번호는 진짜 방과 같은 모양.</summary>
        [Test]
        public void BotRooms_AreDeterministic_AndBotsAreInOneRoomAtATime()
        {
            var start = 1_790_000_000_000L; // 2026년 어느 날
            var counts = new List<int>();
            var ids = new HashSet<string>();
            for (var m = 0; m < 24 * 60; m += 3)
            {
                var now = start + m * PvpBotRooms.Minute + 17_000;
                var rooms = PvpBotRooms.Open(now);
                var again = PvpBotRooms.Open(now);
                Assert.AreEqual(rooms.Count, again.Count);
                for (var i = 0; i < rooms.Count; i++) Assert.AreEqual(rooms[i].Id, again[i].Id);

                var seated = new HashSet<string>();
                var busy = PvpBotRooms.Busy(now);
                foreach (var r in rooms)
                {
                    ids.Add(r.Id);
                    Assert.AreEqual(20, r.Id.Length);
                    Assert.AreEqual('-', r.Id[0]);
                    Assert.IsNotEmpty(r.Title);
                    Assert.AreEqual(r.Size * 2, r.Seats.Length);
                    Assert.AreEqual(r.MapId, PvpCatalog.Find(r.Size, r.MapId).Id);
                    Assert.IsTrue(r.Seated(0, now), "방장은 늘 있다");
                    Assert.Less(r.Created, r.EndsAt);
                    for (var i = 0; i < r.Seats.Length; i++)
                    {
                        if (!r.Seated(i, now)) continue;
                        Assert.IsTrue(seated.Add(r.Seats[i].Uid), $"{r.Seats[i].Name} 가 두 방에 동시에 있다");
                        Assert.IsTrue(busy.Contains(r.Seats[i].Uid));
                    }
                }
                counts.Add(rooms.Count);
            }
            var avg = 0.0;
            foreach (var c in counts) avg += c;
            avg /= counts.Count;
            var none = counts.FindAll(c => c == 0).Count;
            TestContext.WriteLine($"열린 봇 방: 평균 {avg:F1}, 최대 {counts.Max()}, 빈 목록 {none}/{counts.Count}번, 하루 방 {ids.Count}개");
            Assert.That(avg, Is.InRange(2.0, 8.0));
            Assert.Less(none, counts.Count / 20, "목록이 비어 있는 때가 너무 많다");
        }
    }
}
