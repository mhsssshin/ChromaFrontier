using System;
using System.Collections.Generic;
using System.Text;

namespace ChromaFrontier.Core
{
    /// <summary>
    /// 크로마 첨탑의 한 층: 정해진 판과 카드로 Moves 수 안에 모든 칸을 차지하는 퍼즐. 상대는 두지 않는다 (StageRules.Solo).
    /// 흑색의 추가 턴은 수를 쓰지 않고, 스킬은 한 턴에 한 장까지 수와 따로 쓴다. 무작위가 끼면 풀이가 운이 되므로
    /// 손패에 번개(무작위 낙뢰)·지뢰·잠금·거점은 넣지 않는다 (판에 미리 놓인 번개 유닛은 융합 재료로만 쓴다).
    /// </summary>
    public sealed class PuzzleDef
    {
        public int Floor;       // 1부터
        public string Name;
        public string Hint;
        public int Moves;       // 둘 수 있는 수 (유닛을 놓는 횟수)
        public string[] Map;    // Board.Parse 기호: '.' 빈 땅, 'P' 내 땅, 'A' 상대 땅, '#' 낭떠러지, '~' 얼음, 's' 침묵
        public string[] Units;  // 유닛: r b y g v(보라) o(주황) w k(흑) x(회색). 대문자는 내 유닛, 소문자는 상대 유닛, '.' 없음
        public string Hand;     // 카드(공백 구분): R B Y, Tr Tb Ty(원소 변환), Bl(탈색), Wp(공간 왜곡), Ec(재점화)

        public Board CreateBoard()
        {
            var b = Board.Parse(Map);
            for (var y = 0; y < Units.Length; y++)
                for (var x = 0; x < Units[y].Length; x++)
                {
                    var ch = Units[y][x];
                    if (ch == '.') continue;
                    var e = UnitOf(char.ToLowerInvariant(ch));
                    var side = char.IsUpper(ch) ? Side.Player : Side.Ai;
                    ref var t = ref b[new Cell(x, y)];
                    t.Unit = e;
                    t.UnitOwner = side;
                    t.Owner = side;
                }
            return b;
        }

        public static Element UnitOf(char c) => c switch
        {
            'r' => Element.Red,
            'b' => Element.Blue,
            'y' => Element.Yellow,
            'g' => Element.Green,
            'v' => Element.Purple,
            'o' => Element.Orange,
            'w' => Element.White,
            'k' => Element.Black,
            'x' => Element.Grey,
            _ => throw new ArgumentException($"unit '{c}'"),
        };

        public List<Card> Cards() => ParseHand(Hand);

        public static List<Card> ParseHand(string hand)
        {
            var list = new List<Card>();
            foreach (var tok in hand.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                list.Add(tok switch
                {
                    "R" => Card.Unit(Element.Red),
                    "B" => Card.Unit(Element.Blue),
                    "Y" => Card.Unit(Element.Yellow),
                    "Tr" => Card.Transmute(Element.Red),
                    "Tb" => Card.Transmute(Element.Blue),
                    "Ty" => Card.Transmute(Element.Yellow),
                    "Bl" => Card.Of(Skill.Bleach),
                    "Wp" => Card.Of(Skill.Warp),
                    "Ec" => Card.Of(Skill.Echo),
                    _ => throw new ArgumentException($"card '{tok}'"),
                });
            return list;
        }

        /// <param name="moves">수 제한을 바꿔서 (풀이기가 "더 적은 수로도 풀리는가"를 볼 때)</param>
        public Game CreateGame(int? moves = null)
        {
            var rules = new StageRules { MaxTurns = moves ?? Moves, Solo = true, LockFromTurn = 0 };
            var ai = new List<Card>();
            for (var i = 0; i < Balance.HandSize; i++) ai.Add(Card.Unit(Element.Red)); // 두지 않지만 덱은 있어야 한다
            return Game.Create(CreateBoard(), Cards(), ai, 1, rules, shuffle: false);
        }
    }

    /// <summary>
    /// 첨탑 풀이기: 가능한 모든 수(유닛 배치·물 방향·스킬 대상)를 깊이 우선으로 둬 보고 모든 칸을 차지하는 순서를 찾는다.
    /// 같은 상태(보드·손패·턴)는 한 번만 본다. 테스트가 모든 층이 풀리는지, 더 적은 수로는 안 풀리는지 확인한다.
    /// </summary>
    public static class PuzzleSolver
    {
        public sealed class Step
        {
            public Card Card;
            public Cell At, At2, Dir;

            public override string ToString()
            {
                if (Card.IsUnit)
                    return Card.Element == Element.Blue ? $"{Card}@{At.X},{At.Y}→{Dir.X},{Dir.Y}" : $"{Card}@{At.X},{At.Y}";
                return Card.Skill == Skill.Warp ? $"{Card}@{At.X},{At.Y}↔{At2.X},{At2.Y}" : $"{Card}@{At.X},{At.Y}";
            }
        }

        public sealed class Result
        {
            public List<Step> Solution; // 없으면 null
            public int Nodes;
            public bool Exhausted;      // 노드 상한에 걸려 끝까지 못 봤다
        }

        public static Result Solve(Game g, int maxNodes = 3_000_000)
        {
            var r = new Result();
            var path = new List<Step>();
            var seen = new HashSet<string>();
            if (Dfs(g, path, seen, r, maxNodes)) r.Solution = new List<Step>(path);
            return r;
        }

        /// <summary>성공으로 이어지는 서로 다른 첫 행동의 수 (유일한 묘수인지 볼 때). cap 에서 멈춘다.</summary>
        public static int CountWinningFirstActions(Game g, int cap = 99, int maxNodes = 3_000_000)
        {
            var n = 0;
            foreach (var (step, next) in Actions(g))
            {
                if (next.Solved) n++;
                else
                {
                    var r = new Result();
                    if (!next.IsOver && Dfs(next, new List<Step>(), new HashSet<string>(), r, maxNodes)) n++;
                }
                if (n >= cap) break;
            }
            return n;
        }

        /// <summary>욕심쟁이: 매 턴 당장 가장 많은 칸을 차지하는 유닛 수만 둔다 (스킬 안 씀). 이걸로 풀리면 쉬운 문제.</summary>
        public static bool GreedySolves(Game g)
        {
            g = g.Clone();
            while (!g.IsOver)
            {
                Game best = null;
                var bestScore = int.MinValue;
                foreach (var (step, next) in Actions(g))
                {
                    if (!step.Card.IsUnit) continue;
                    var score = next.Board.Count(Side.Player);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = next;
                    }
                }
                if (best == null) return false;
                g = best;
            }
            return g.Solved;
        }

        private static bool Dfs(Game g, List<Step> path, HashSet<string> seen, Result r, int maxNodes)
        {
            if (g.Solved) return true;
            if (g.IsOver) return false;
            if (++r.Nodes > maxNodes)
            {
                r.Exhausted = true;
                return false;
            }
            if (!seen.Add(Key(g))) return false;
            foreach (var (step, next) in Actions(g))
            {
                path.Add(step);
                if (Dfs(next, path, seen, r, maxNodes)) return true;
                path.RemoveAt(path.Count - 1);
                if (r.Exhausted) return false;
            }
            return false;
        }

        /// <summary>지금 둘 수 있는 모든 행동과 그 뒤의 판. 같은 카드가 여러 장이면 한 번만 본다.</summary>
        public static IEnumerable<(Step, Game)> Actions(Game g)
        {
            var hand = g.DeckOf(Side.Player).Hand;
            var tried = new HashSet<Card>();
            for (var i = 0; i < hand.Count; i++)
            {
                var card = hand[i];
                if (!tried.Add(card)) continue;
                if (card.IsUnit)
                {
                    foreach (var c in g.LegalCells(Side.Player))
                    {
                        if (card.Element == Element.Blue)
                        {
                            foreach (var d in Cell.Orthogonal)
                            {
                                var sim = g.Clone();
                                sim.Play(i, c, d);
                                yield return (new Step { Card = card, At = c, Dir = d }, sim);
                            }
                        }
                        else
                        {
                            var sim = g.Clone();
                            sim.Play(i, c, Cell.Orthogonal[0]);
                            yield return (new Step { Card = card, At = c }, sim);
                        }
                    }
                }
                else if (!g.SkillUsed)
                {
                    var targets = new List<Cell>();
                    foreach (var c in g.Board.Cells())
                        if (g.CanTarget(Side.Player, card, c)) targets.Add(c);
                    if (card.Skill == Skill.Warp)
                    {
                        for (var a = 0; a < targets.Count; a++)
                            for (var b = a + 1; b < targets.Count; b++)
                            {
                                var ta = g.Board[targets[a]];
                                var tb = g.Board[targets[b]];
                                // 내용이 같은 두 칸을 바꾸는 것은 의미가 없다
                                if (ta.Owner == tb.Owner && ta.Unit == tb.Unit && ta.UnitOwner == tb.UnitOwner) continue;
                                var sim = g.Clone();
                                sim.Cast(i, targets[a], targets[b]);
                                yield return (new Step { Card = card, At = targets[a], At2 = targets[b] }, sim);
                            }
                    }
                    else
                    {
                        foreach (var c in targets)
                        {
                            var sim = g.Clone();
                            sim.Cast(i, c);
                            yield return (new Step { Card = card, At = c }, sim);
                        }
                    }
                }
            }
        }

        private static string Key(Game g)
        {
            var sb = new StringBuilder();
            sb.Append(g.Turn).Append(g.SkillUsed ? '1' : '0');
            foreach (var c in g.Board.Cells())
            {
                var t = g.Board[c];
                sb.Append((char)('a' + (int)t.Owner)).Append((char)('a' + (int)t.Unit)).Append((char)('a' + (int)t.UnitOwner)).Append(t.Shield > 0 ? '+' : '-');
            }
            var hand = new List<string>();
            foreach (var card in g.DeckOf(Side.Player).Hand) hand.Add(card.ToString());
            hand.Sort(StringComparer.Ordinal);
            sb.Append('|').Append(string.Join(",", hand));
            return sb.ToString();
        }
    }
}
