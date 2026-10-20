using System;
using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>PvP 맵 하나. 1:1 은 P/A 시작 땅 한 곳씩, 2:2 는 팀마다 두 곳(대각선으로 같은 팀).</summary>
    public sealed class PvpMap
    {
        public string Id, Name, Note;
        public string[] Map;
        public int MaxTurns = Balance.MaxTurns;
        public int LavaEvery;

        public StageRules Rules => new() { MaxTurns = MaxTurns, LavaEvery = LavaEvery };
    }

    /// <summary>
    /// PvP 대전 (방을 만들고 들어온 사람끼리 1:1 / 2:2). 서버 없이 모든 기기가 같은 seed 와 같은 수 목록으로
    /// 같은 Game 을 재생한다(결정적 lockstep). 수는 PvpMove 문자열로 주고받고, 차례가 아닌 자리의 수나 규칙에 어긋난 수는
    /// 모든 기기에서 똑같이 무시된다. 카드 강화와 소모품은 쓰지 않는다(모두 Lv1). 덱은 각자 편성한 스킬 덱.
    /// </summary>
    public static class PvpCatalog
    {
        /// <summary>한 차례 제한 시간(초). 넘기면 그 자리의 기기가 패 버리기로 차례를 넘긴다.</summary>
        public const int TurnSeconds = 40;

        // 보상(결정): 승리·무승부·패배
        public const int WinGems = 20, DrawGems = 10, LoseGems = 5;

        public static readonly PvpMap[] Duel =
        {
            new()
            {
                Id = "meadow", Name = "푸른 초원", Note = "기믹 없는 8x8 평원",
                Map = new[] { "......AA", "......AA", "........", "........", "........", "........", "PP......", "PP......" },
            },
            new()
            {
                Id = "lake", Name = "얼음 호수", Note = "가운데 얼음 고리에 놓으면 미끄러진다",
                Map = new[] { "......AA", "......AA", "..~~~~..", "..~..~..", "..~..~..", "..~~~~..", "PP......", "PP......" },
            },
            new()
            {
                Id = "bridge", Name = "끊어진 다리", Note = "낭떠러지 사이 좁은 길을 두고 싸우는 9x9",
                Map = new[] { "....#..AA", "....#..AA", "....#....", ".........", "###...###", ".........", "....#....", "PP..#....", "PP..#...." },
            },
        };

        public static readonly PvpMap[] Team =
        {
            new()
            {
                Id = "plain", Name = "넓은 초원", Note = "기믹 없는 10x10. 같은 팀은 대각선 두 모서리",
                MaxTurns = 24,
                Map = new[] { "AA......PP", "AA......PP", "..........", "..........", "..........", "..........", "..........", "..........", "PP......AA", "PP......AA" },
            },
            new()
            {
                Id = "cross", Name = "얼음 십자로", Note = "가운데 낭떠러지를 얼음이 두른 10x10",
                MaxTurns = 24,
                Map = new[] { "AA......PP", "AA......PP", "..........", "....~~....", "...~##~...", "...~##~...", "....~~....", "..........", "PP......AA", "PP......AA" },
            },
            new()
            {
                Id = "volcano", Name = "화산 분지", Note = "3라운드마다 화산이 터져 양 팀 유닛이 하나씩 폭발",
                MaxTurns = 24, LavaEvery = 3,
                Map = new[] { "AA......PP", "AA......PP", "..........", "..........", "....##....", "....##....", "..........", "..........", "PP......AA", "PP......AA" },
            },
        };

        /// <summary>팀 인원(1 또는 2)의 맵 목록.</summary>
        public static PvpMap[] MapsFor(int teamSize) => teamSize == 2 ? Team : Duel;

        public static PvpMap Find(int teamSize, string id)
        {
            var maps = MapsFor(teamSize);
            foreach (var m in maps)
                if (m.Id == id) return m;
            return maps[0];
        }

        /// <summary>자리 수만큼의 스킬 덱(CardShop 코드 목록 문자열, 쉼표)으로 판을 만든다. 유닛은 원소별 4장씩 붙는다.</summary>
        public static Game CreateGame(PvpMap map, IList<string> seatDecks, int seed)
        {
            var decks = new List<List<Card>>();
            foreach (var codes in seatDecks) decks.Add(CardShop.BuildDeck(ParseSkills(codes)));
            return Game.CreateSeats(Board.Parse(map.Map), decks, seed, map.Rules);
        }

        /// <summary>스킬 카드 코드 목록 (덱 편성 그대로 보낸다).</summary>
        public static string SkillCodes(Collection c)
        {
            var list = new List<string>();
            foreach (var card in c.Skills) list.Add(CardShop.Find(card).Code);
            return string.Join(",", list);
        }

        /// <summary>코드 목록을 스킬 카드로. 모르는 코드는 버리고, 한 종류 MaxCopies 장·전체 최대 슬롯까지만.</summary>
        public static List<Card> ParseSkills(string codes)
        {
            var list = new List<Card>();
            if (string.IsNullOrEmpty(codes)) return list;
            var count = new Dictionary<string, int>();
            foreach (var code in codes.Split(','))
            {
                var e = CardShop.Find(code);
                if (e == null || list.Count >= CardShop.SkillSlots + 2) continue; // 별의 길 슬롯 보상 두 번까지
                count.TryGetValue(code, out var n);
                if (n >= CardShop.MaxCopies) continue;
                count[code] = n + 1;
                list.Add(e.Card);
            }
            return list;
        }

        /// <summary>판이 끝났을 때 그 팀이 받는 결정.</summary>
        public static int Reward(Side winner, Side team) => winner == team ? WinGems : winner == Side.None ? DrawGems : LoseGems;
    }

    public enum PvpMoveKind : byte { Play, Cast, Pass }

    /// <summary>
    /// PvP 의 수 하나. 문자열 "자리,종류,손패,x,y,a,b" 로 주고받는다 (종류 P 유닛, C 스킬, M 패 버리기·시간 초과).
    /// 유닛이면 (a,b) 는 물 방향, 스킬이면 공간 왜곡의 두 번째 칸이다.
    /// </summary>
    public readonly struct PvpMove
    {
        public readonly int Seat;
        public readonly PvpMoveKind Kind;
        public readonly int Hand;
        public readonly Cell At, Extra;

        public PvpMove(int seat, PvpMoveKind kind, int hand, Cell at, Cell extra)
        {
            Seat = seat;
            Kind = kind;
            Hand = hand;
            At = at;
            Extra = extra;
        }

        public static PvpMove Play(int seat, int hand, Cell at, Cell dir) => new(seat, PvpMoveKind.Play, hand, at, dir);
        public static PvpMove Cast(int seat, int hand, Cell at, Cell target2) => new(seat, PvpMoveKind.Cast, hand, at, target2);
        public static PvpMove Pass(int seat) => new(seat, PvpMoveKind.Pass, 0, default, default);

        public string Encode()
        {
            var k = Kind == PvpMoveKind.Play ? "P" : Kind == PvpMoveKind.Cast ? "C" : "M";
            return $"{Seat},{k},{Hand},{At.X},{At.Y},{Extra.X},{Extra.Y}";
        }

        public static bool TryDecode(string s, out PvpMove m)
        {
            m = default;
            if (string.IsNullOrEmpty(s)) return false;
            var p = s.Split(',');
            if (p.Length != 7) return false;
            var n = new int[7];
            for (var i = 0; i < 7; i++)
                if (i != 1 && !int.TryParse(p[i], out n[i])) return false;
            PvpMoveKind kind;
            switch (p[1])
            {
                case "P": kind = PvpMoveKind.Play; break;
                case "C": kind = PvpMoveKind.Cast; break;
                case "M": kind = PvpMoveKind.Pass; break;
                default: return false;
            }
            m = new PvpMove(n[0], kind, n[2], new Cell(n[3], n[4]), new Cell(n[5], n[6]));
            return true;
        }

        /// <summary>규칙에 맞으면 판에 두고 true. 차례가 아닌 자리이거나 둘 수 없는 수면 판을 건드리지 않고 false.</summary>
        public bool TryApply(Game g)
        {
            if (g.IsOver || Seat != g.CurrentSeat) return false;
            var hand = g.DeckOf(g.Current).Hand;
            switch (Kind)
            {
                case PvpMoveKind.Pass:
                    g.Mulligan();
                    return true;
                case PvpMoveKind.Play:
                {
                    if (Hand < 0 || Hand >= hand.Count || !hand[Hand].IsUnit || !g.CanPlace(g.Current, At)) return false;
                    var blue = hand[Hand].Element == Element.Blue;
                    if (blue && Array.IndexOf(Cell.Orthogonal, Extra) < 0) return false;
                    g.Play(Hand, At, blue ? Extra : Cell.Orthogonal[0]);
                    return true;
                }
                default:
                {
                    if (Hand < 0 || Hand >= hand.Count || hand[Hand].IsUnit || !g.CanCast(g.Current, hand[Hand], At, Extra)) return false;
                    g.Cast(Hand, At, Extra);
                    return true;
                }
            }
        }
    }
}
