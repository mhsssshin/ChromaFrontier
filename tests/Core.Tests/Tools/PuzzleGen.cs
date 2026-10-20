using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ChromaFrontier.Core;
using NUnit.Framework;

namespace ChromaFrontier.Tools
{
    /// <summary>
    /// 크로마 첨탑 퍼즐 생성기 (도구, 평소 테스트에서는 안 돈다).
    /// 무작위 판·카드를 만들어 풀이기로 걸러 낸다: Moves 수로 풀리고, 한 수 적게는 안 풀리고, 첫 묘수가 적고,
    /// (위층일수록) 욕심쟁이로는 안 풀리는 것만 남긴다. 결과는 build/puzzle-candidates.txt 에 C# 코드로 쓴다.
    ///   dotnet test tests/Core.Tests --filter "FullyQualifiedName~PuzzleGen" -e CF_GEN_TIER=3
    /// </summary>
    [Explicit]
    public class PuzzleGen
    {
        private sealed class Tier
        {
            public int Id, W, H, MovesMin, MovesMax, UnitsMin, UnitsMax, Skills, AiMin, AiMax, PreAi, PrePl, Want, Trials;
            public double Chasm, Ice, Silence, Tier2Ai, Tier2Pl, Neutral = 1;
            public string[] SkillPool = Array.Empty<string>();
            public bool GreedyFail, MustUseSkill;
            public int MaxFirst = 3;
        }

        private static readonly Tier[] Tiers =
        {
            new() { Id = 1, W = 4, H = 4, MovesMin = 1, MovesMax = 1, UnitsMin = 1, UnitsMax = 1, AiMin = 3, AiMax = 7, PreAi = 2, PrePl = 2, Want = 30, Trials = 20000, MaxFirst = 3, Tier2Ai = 0.5, Tier2Pl = 0.5, Neutral = 0.3 },
            new() { Id = 2, W = 4, H = 4, MovesMin = 1, MovesMax = 2, UnitsMin = 2, UnitsMax = 2, AiMin = 4, AiMax = 8, PreAi = 3, PrePl = 1, Want = 30, Trials = 20000, MaxFirst = 2, Tier2Ai = 0.3 },
            new() { Id = 3, W = 5, H = 5, MovesMin = 2, MovesMax = 2, UnitsMin = 2, UnitsMax = 3, AiMin = 5, AiMax = 10, PreAi = 3, PrePl = 2, Want = 30, Trials = 20000, MaxFirst = 2, GreedyFail = true, Tier2Ai = 0.3 },
            new() { Id = 4, W = 5, H = 5, MovesMin = 2, MovesMax = 2, UnitsMin = 2, UnitsMax = 2, Skills = 1, SkillPool = new[] { "Tr", "Tb", "Ty", "Bl", "Ec", "Wp" }, AiMin = 5, AiMax = 10, PreAi = 3, PrePl = 2, Want = 30, Trials = 20000, MaxFirst = 2, GreedyFail = true, MustUseSkill = true, Tier2Ai = 0.3 },
            new() { Id = 5, W = 6, H = 5, Want = 16, MovesMin = 2, MovesMax = 3, UnitsMin = 3, UnitsMax = 3, Skills = 1, SkillPool = new[] { "Tr", "Tb", "Ty", "Bl", "Ec", "Wp" }, AiMin = 6, AiMax = 12, PreAi = 4, PrePl = 2, Chasm = 0.08, Ice = 0.1, Silence = 0.05, Trials = 20000, MaxFirst = 2, GreedyFail = true, Tier2Ai = 0.35 },
            new() { Id = 6, W = 6, H = 6, Want = 6, MovesMin = 3, MovesMax = 3, UnitsMin = 3, UnitsMax = 4, Skills = 1, SkillPool = new[] { "Tr", "Tb", "Ty", "Bl", "Ec", "Wp" }, AiMin = 8, AiMax = 14, PreAi = 5, PrePl = 2, Chasm = 0.1, Ice = 0.1, Silence = 0.05, Trials = 6000, MaxFirst = 2, GreedyFail = true, Tier2Ai = 0.35 },
        };

        [Test]
        public void Generate()
        {
            var tierEnv = Environment.GetEnvironmentVariable("CF_GEN_TIER");
            var tiers = string.IsNullOrEmpty(tierEnv) ? Tiers : Tiers.Where(t => tierEnv.Split(',').Contains(t.Id.ToString())).ToArray();
            var seedEnv = Environment.GetEnvironmentVariable("CF_GEN_SEED");
            var rnd = new Random(string.IsNullOrEmpty(seedEnv) ? 12345 : int.Parse(seedEnv));
            var outPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../build/puzzle-candidates.txt");
            var sb = new StringBuilder();
            foreach (var tier in tiers)
            {
                var found = new List<(PuzzleDef def, int first, int nodes, string sol)>();
                var keys = new HashSet<string>();
                for (var trial = 0; trial < tier.Trials && found.Count < tier.Want; trial++)
                {
                    var def = Random(tier, rnd);
                    var key = string.Join("/", def.Map) + string.Join("/", def.Units) + def.Hand;
                    if (!keys.Add(key)) continue;
                    var game = def.CreateGame();
                    if (game.IsOver || game.LegalCells(Side.Player).Count == 0) continue;
                    var r = PuzzleSolver.Solve(game, 80_000);
                    if (r.Solution == null) continue;
                    if (def.Moves > 1 && PuzzleSolver.Solve(def.CreateGame(def.Moves - 1), 80_000) is var less && (less.Solution != null || less.Exhausted)) continue;
                    if (tier.GreedyFail && PuzzleSolver.GreedySolves(game)) continue;
                    if (tier.MustUseSkill)
                    {
                        var noSkill = new PuzzleDef { Map = def.Map, Units = def.Units, Moves = def.Moves, Hand = string.Join(" ", def.Hand.Split(' ').Where(t => t == "R" || t == "B")) };
                        var ns = PuzzleSolver.Solve(noSkill.CreateGame(), 80_000);
                        if (ns.Solution != null || ns.Exhausted) continue;
                    }
                    var first = PuzzleSolver.CountWinningFirstActions(game, tier.MaxFirst + 1, 80_000);
                    if (first > tier.MaxFirst) continue;
                    found.Add((def, first, r.Nodes, Describe(def, r.Solution)));
                }
                sb.AppendLine($"// ===== tier {tier.Id}: {found.Count}");
                foreach (var (def, first, nodes, sol) in found.OrderBy(f => f.first).ThenByDescending(f => f.nodes))
                {
                    sb.AppendLine($"// first={first} nodes={nodes} sol: {sol}");
                    sb.AppendLine($"new() {{ Name = \"\", Hint = \"\", Moves = {def.Moves},");
                    sb.AppendLine($"    Map = new[] {{ {string.Join(", ", def.Map.Select(m => $"\"{m}\""))} }},");
                    sb.AppendLine($"    Units = new[] {{ {string.Join(", ", def.Units.Select(m => $"\"{m}\""))} }},");
                    sb.AppendLine($"    Hand = \"{def.Hand}\" }},");
                }
                TestContext.Progress.WriteLine($"tier {tier.Id}: {found.Count}");
            }
            File.AppendAllText(outPath, sb.ToString());
        }

        /// <summary>풀이 순서와 그 수에서 일어난 일 (이름·힌트를 붙일 때 참고).</summary>
        private static string Describe(PuzzleDef def, List<PuzzleSolver.Step> sol)
        {
            var g = def.CreateGame();
            var parts = new List<string>();
            foreach (var s in sol)
            {
                var hand = g.DeckOf(Side.Player).Hand;
                var i = hand.IndexOf(s.Card);
                PlayResult r = s.Card.IsUnit ? g.Play(i, s.At, s.Dir) : g.Cast(i, s.At, s.At2);
                var tags = new List<string>();
                if (r.Fused != Element.None) tags.Add(r.Fused + (r.Boosted ? "+" : "") + (r.StoleCatalyst ? "!" : ""));
                if (r.Resonance) tags.Add("res");
                if (r.Slid) tags.Add("slid");
                if (r.ExtraTurn) tags.Add("extra");
                parts.Add(s + (tags.Count > 0 ? "[" + string.Join(",", tags) + "]" : ""));
            }
            return string.Join("  ", parts);
        }

        private static PuzzleDef Random(Tier t, Random rnd)
        {
            int w = t.W, h = t.H;
            var map = new char[h, w];
            var units = new char[h, w];
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    map[y, x] = '.';
                    units[y, x] = '.';
                }
            // 내 땅: 왼쪽 아래 구석 2~4칸
            var mine = new List<(int x, int y)> { (0, h - 1), (1, h - 1), (0, h - 2), (1, h - 2) };
            var nMine = rnd.Next(2, 5);
            foreach (var (x, y) in mine.Take(nMine)) map[y, x] = 'P';
            // 상대 땅: 오른쪽 위에서 퍼져 나간 덩어리
            var aiCells = new List<(int x, int y)> { (w - 1, 0) };
            map[0, w - 1] = 'A';
            var aiTarget = rnd.Next(t.AiMin, t.AiMax + 1);
            var guard = 0;
            while (aiCells.Count < aiTarget && guard++ < 500)
            {
                var (bx, by) = aiCells[rnd.Next(aiCells.Count)];
                var d = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }[rnd.Next(4)];
                int nx = bx + d.Item1, ny = by + d.Item2;
                if (nx < 0 || ny < 0 || nx >= w || ny >= h || map[ny, nx] != '.') continue;
                if (nx <= 1 && ny >= h - 2) continue;
                map[ny, nx] = 'A';
                aiCells.Add((nx, ny));
            }
            // 지형 (빈 땅에만)
            for (var y = 0; y < h; y++)
                for (var x = 0; x < w; x++)
                {
                    if (map[y, x] != '.') continue;
                    if (rnd.NextDouble() > t.Neutral)
                    {
                        map[y, x] = 'A';
                        aiCells.Add((x, y));
                        continue;
                    }
                    var p = rnd.NextDouble();
                    if (p < t.Chasm) map[y, x] = '#';
                    else if (p < t.Chasm + t.Ice) map[y, x] = '~';
                    else if (p < t.Chasm + t.Ice + t.Silence) map[y, x] = 's';
                }
            // 상대 유닛
            var nAi = rnd.Next(1, t.PreAi + 1);
            for (var k = 0; k < nAi; k++)
            {
                var (x, y) = aiCells[rnd.Next(aiCells.Count)];
                if (units[y, x] != '.') continue;
                var pool = rnd.NextDouble() < t.Tier2Ai ? "gvo" : "rby";
                units[y, x] = pool[rnd.Next(pool.Length)];
            }
            // 내 유닛
            var nPl = rnd.Next(0, t.PrePl + 1);
            for (var k = 0; k < nPl; k++)
            {
                var (x, y) = mine[rnd.Next(nMine)];
                if (units[y, x] != '.') continue;
                var pool = rnd.NextDouble() < t.Tier2Pl ? "gvo" : "rby";
                units[y, x] = char.ToUpperInvariant(pool[rnd.Next(pool.Length)]);
            }
            var hand = new List<string>();
            var nUnits = rnd.Next(t.UnitsMin, t.UnitsMax + 1);
            for (var k = 0; k < nUnits; k++) hand.Add(rnd.Next(2) == 0 ? "R" : "B");
            for (var k = 0; k < t.Skills; k++) hand.Add(t.SkillPool[rnd.Next(t.SkillPool.Length)]);
            var moves = Math.Min(nUnits, rnd.Next(t.MovesMin, t.MovesMax + 1));
            string Row(char[,] g, int y) => new string(Enumerable.Range(0, w).Select(x => g[y, x]).ToArray());
            return new PuzzleDef
            {
                Moves = moves,
                Map = Enumerable.Range(0, h).Select(y => Row(map, y)).ToArray(),
                Units = Enumerable.Range(0, h).Select(y => Row(units, y)).ToArray(),
                Hand = string.Join(" ", hand),
            };
        }
    }
}
