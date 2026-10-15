using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>
    /// 기본 탐욕 AI. 모든 후보 수에 점수를 매겨 최고점을 둔다 (동점은 무작위).
    /// - 유닛: 시뮬레이션해서 (내 칸 − 상대 칸)이 얼마나 늘었나.
    /// - 탈색/잠금/지뢰: 칸 수가 바로 안 바뀌므로 휴리스틱 점수 + 손패에 스킬이 쌓일수록 가산 (손패 막힘 방지).
    /// - 공간 왜곡: 턴을 안 쓰므로 2수 탐색 — 유닛을 내 전선 옆으로 끌어온 뒤 그 옆에 유닛을 놓는 수까지 본다 (후보 수 제한).
    /// 시뮬레이션은 CloneFor(me) 로 상대의 숨은 지뢰를 모르는 상태에서 한다.
    /// 둘 곳이 없으면 멀리건.
    /// 난이도(AiLevel)는 점수에 섞는 무작위·실수 확률·공간 왜곡 사용 여부로 낮춘다 (Profile).
    /// 성향(AiPersona)은 점수 가중치로 보스의 개성을 낸다.
    /// </summary>
    public enum AiLevel : byte { Easy, Normal, Hard }

    /// <summary>
    /// AI 성향: 수 점수에 더하는 가중치. 기본값(0, 1)이면 보통 탐욕 AI.
    /// </summary>
    public sealed class AiPersona
    {
        public string Id = "default";
        public float StealWeight;          // 상대 땅을 빼앗은 칸마다 가산 (어그로: 수비보다 전진)
        public float CatalystSteal;        // 상대 유닛을 촉매로 훔쳤을 때 가산 (약탈)
        public float LoneUnitPenalty;      // 상대 유닛과 맞닿지 않은 곳에 유닛을 놓으면 감점 (약탈: 유닛을 아낀다)
        public float ShieldWeight;         // 수 뒤 내 보호된 칸이 늘어난 만큼 가산 (통제)
        public float LockWeight = 1f, BleachWeight = 1f, MineWeight = 1f, WarpBonus;

        public static readonly AiPersona Default = new();
    }

    public static class AiBrain
    {
        private const float SkillPressure = 0.8f;
        private const float WarpCost = 0.5f;

        /// <summary>
        /// 난이도별 실수 정도. Noise = 후보 점수마다 더하는 무작위(0~Noise), Mistake = 아무 후보나 고를 확률,
        /// WarpCandidates = 공간 왜곡 2수 탐색 후보 수 (0 이면 공간 왜곡을 안 씀).
        /// </summary>
        private static (float noise, float mistake, int warpCandidates) Profile(AiLevel level) => level switch
        {
            AiLevel.Easy => (8f, 0.45f, 0),
            AiLevel.Normal => (4f, 0.2f, 12),
            _ => (0f, 0f, 40),
        };

        public struct Move
        {
            public bool Mulligan;
            public int HandIndex;
            public bool IsSkill;
            public Cell At;
            public Cell Dir;     // 물 방향
            public Cell Target2; // 공간 왜곡 두 번째 칸
            public float Score;
        }

        public static Move Choose(Game game, Rng tieBreak, AiLevel level = AiLevel.Hard, AiPersona persona = null)
        {
            persona ??= AiPersona.Default;
            var (noise, mistake, warpCandidates) = Profile(level);
            var blunder = mistake > 0f && tieBreak.Next(1000) < mistake * 1000f;
            var me = game.Current;
            var foe = me.Opponent();
            var view = game.CloneFor(me);
            var hand = view.DeckOf(me).Hand;
            var baseDiff = Diff(view, me);
            var skillsInHand = 0;
            foreach (var c in hand)
                if (!c.IsUnit) skillsInHand++;
            var pressure = SkillPressure * (skillsInHand - 1);

            var best = new Move { Mulligan = true, Score = float.MinValue };
            var ties = 0;
            var seen = 0;
            void Offer(Move m)
            {
                if (blunder)
                {
                    // 실수하는 턴: 후보 중 아무거나 (저수지 표본 추출)
                    if (tieBreak.Next(++seen) == 0) best = m;
                    return;
                }
                if (noise > 0f) m.Score += noise * tieBreak.Next(1000) / 1000f;
                if (m.Score > best.Score)
                {
                    best = m;
                    ties = 1;
                }
                else if (m.Score == best.Score && tieBreak.Next(++ties) == 0)
                {
                    best = m;
                }
            }

            var legal = view.LegalCells(me);
            for (var h = 0; h < hand.Count; h++)
            {
                var card = hand[h];
                if (card.IsUnit)
                {
                    foreach (var at in legal)
                        foreach (var d in DirsFor(card))
                            Offer(new Move { HandIndex = h, At = at, Dir = d, Score = ScoreUnit(view, h, at, d, me, baseDiff, persona) });
                    continue;
                }

                if (card.Skill == Skill.Warp)
                {
                    if (warpCandidates > 0)
                        foreach (var m in WarpMoves(view, h, me, baseDiff, warpCandidates, tieBreak, persona)) Offer(m);
                    continue;
                }

                foreach (var c in view.Board.Cells())
                {
                    if (!view.CanTarget(me, card.Skill, c)) continue;
                    var score = card.Skill switch
                    {
                        Skill.Bleach => ScoreBleach(view, c, me, foe) * persona.BleachWeight,
                        Skill.Lock => ScoreLock(view, c, me, foe) * persona.LockWeight,
                        Skill.Mine => ScoreMine(view, c, me, foe) * persona.MineWeight,
                        _ => float.MinValue,
                    };
                    if (score > 0f) Offer(new Move { HandIndex = h, IsSkill = true, At = c, Score = score + pressure });
                }
            }
            return best;
        }

        public static void Apply(Game game, Move m)
        {
            if (m.Mulligan) game.Mulligan();
            else if (m.IsSkill) game.Cast(m.HandIndex, m.At, m.Target2);
            else game.Play(m.HandIndex, m.At, m.Dir);
        }

        private static int Diff(Game g, Side me) => g.Board.Count(me) - g.Board.Count(me.Opponent());

        private static Cell[] DirsFor(Card c) => c.Element == Element.Blue ? Cell.Orthogonal : new[] { Cell.Orthogonal[0] };

        private static float ScoreUnit(Game view, int h, Cell at, Cell d, Side me, int baseDiff, AiPersona persona)
        {
            var sim = view.Clone();
            var r = sim.Play(h, at, d);
            var score = Diff(sim, me) - baseDiff + (r.ExtraTurn ? 3f : 0f);
            score += persona.StealWeight * r.Stolen;
            if (r.StoleCatalyst) score += persona.CatalystSteal;
            if (persona.LoneUnitPenalty > 0f && !TouchesEnemyUnit(view.Board, at, me)) score -= persona.LoneUnitPenalty;
            if (persona.ShieldWeight > 0f) score += persona.ShieldWeight * (Shielded(sim.Board, me) - Shielded(view.Board, me));
            return score;
        }

        private static bool TouchesEnemyUnit(Board b, Cell at, Side me)
        {
            foreach (var d in Cell.Orthogonal)
            {
                var n = at + d;
                if (b.InBounds(n) && b[n].HasUnit && b[n].UnitOwner == me.Opponent()) return true;
            }
            return false;
        }

        private static int Shielded(Board b, Side me)
        {
            var n = 0;
            foreach (var c in b.Cells())
                if (b[c].Owner == me && b[c].Shield > 0) n++;
            return n;
        }

        /// <summary>상대가 다음 턴에 융합 촉매로 쓸 수 있는 유닛일수록, 내가 쓸 수 없는 유닛일수록 높다.</summary>
        private static float ScoreBleach(Game view, Cell c, Side me, Side foe)
        {
            var t = view.Board[c];
            if (!t.Unit.IsBase()) return 0f;
            float foeAdj = 0f, meAdj = 0f;
            foreach (var d in Cell.Orthogonal)
            {
                if (view.CanPlace(foe, c + d)) foeAdj = 2f;
                if (view.CanPlace(me, c + d)) meAdj = 1.5f;
            }
            return foeAdj - meAdj - (t.UnitOwner == me ? 1f : 0f);
        }

        /// <summary>잠금 범위에서 상대가 둘 수 있는 칸은 이득, 내가 둘 수 있는 칸은 손해.</summary>
        private static float ScoreLock(Game view, Cell center, Side me, Side foe)
        {
            var score = 0f;
            foreach (var c in view.Board.Cells())
            {
                if (c.Chebyshev(center) > Balance.LockRadius) continue;
                if (view.CanPlace(foe, c)) score += 0.5f;
                if (view.CanPlace(me, c)) score -= 0.5f;
            }
            return score;
        }

        /// <summary>상대가 놓을 만한 전선 칸일수록, 터졌을 때 먹을 칸이 많을수록 높다.</summary>
        private static float ScoreMine(Game view, Cell c, Side me, Side foe)
        {
            if (!view.CanPlace(foe, c)) return 0.2f;
            var score = 1.5f;
            foreach (var n in view.Board.Cells())
                if (n.Chebyshev(c) <= Balance.MineRadius && view.Board[n].Owner != me) score += 0.2f;
            return score;
        }

        /// <summary>
        /// 1단계 유닛 칸 a 를, 내가 둘 수 있는 칸과 맞닿은 칸 b 로 옮긴 뒤 b 옆에 유닛을 놓는 최선의 후속 수까지 점수에 넣는다.
        /// </summary>
        private static IEnumerable<Move> WarpMoves(Game view, int h, Side me, int baseDiff, int maxCandidates, Rng rng, AiPersona persona)
        {
            var legal = view.LegalCells(me);
            var legalSet = new HashSet<Cell>(legal);
            var units = new List<Cell>();
            var spots = new List<Cell>();
            foreach (var c in view.Board.Cells())
            {
                if (!view.CanTarget(me, Skill.Warp, c)) continue;
                var t = view.Board[c];
                if (t.Unit.IsBase()) units.Add(c);
                else if (!t.HasUnit)
                    foreach (var d in Cell.Orthogonal)
                        if (legalSet.Contains(c + d)) { spots.Add(c); break; }
            }

            var pairs = new List<(Cell a, Cell b)>();
            foreach (var a in units)
                foreach (var b in spots)
                    if (a != b && a.Manhattan(b) > 1) pairs.Add((a, b));
            for (var i = 0; i < pairs.Count && i < maxCandidates; i++)
            {
                var j = i + rng.Next(pairs.Count - i);
                (pairs[i], pairs[j]) = (pairs[j], pairs[i]);
            }

            for (var i = 0; i < pairs.Count && i < maxCandidates; i++)
            {
                var (a, b) = pairs[i];
                var sim = view.Clone();
                sim.Cast(h, a, b);
                var follow = BestFollowUp(sim, b, me, persona);
                if (follow == float.MinValue) continue;
                var score = Diff(sim, me) - baseDiff + follow - WarpCost + persona.WarpBonus;
                yield return new Move { HandIndex = h, IsSkill = true, At = a, Target2 = b, Score = score };
            }
        }

        private static float BestFollowUp(Game sim, Cell near, Side me, AiPersona persona)
        {
            var best = float.MinValue;
            var baseDiff = Diff(sim, me);
            var hand = sim.DeckOf(me).Hand;
            foreach (var d0 in Cell.Orthogonal)
            {
                var at = near + d0;
                if (!sim.CanPlace(me, at)) continue;
                for (var h = 0; h < hand.Count; h++)
                {
                    if (!hand[h].IsUnit) continue;
                    foreach (var d in DirsFor(hand[h]))
                    {
                        var s = ScoreUnit(sim, h, at, d, me, baseDiff, persona);
                        if (s > best) best = s;
                    }
                }
            }
            return best;
        }
    }
}
