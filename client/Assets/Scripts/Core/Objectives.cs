using System;
using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>한 판 동안 한 쪽이 쌓은 기록 (게임 중 목표와 퀘스트 누적에 쓴다).</summary>
    public sealed class MatchStats
    {
        public readonly int[] Counts = new int[(int)Stat.Count];
        public int BestMove; // 한 수에 차지한 가장 많은 칸

        public int this[Stat s] => Counts[(int)s];

        public void Add(Stat s, int n = 1) => Counts[(int)s] += n;

        public MatchStats Clone()
        {
            var m = new MatchStats { BestMove = BestMove };
            Array.Copy(Counts, m.Counts, Counts.Length);
            return m;
        }
    }

    public enum ObjectiveGoal : byte
    {
        Fusions,    // 융합 N번
        Resonances, // 공명 N번
        Steals,     // 상대 유닛으로 융합 N번
        Tier3,      // 3단계 N번
        BigMove,    // 한 수에 N칸 이상 차지
        Skills,     // 스킬 N번
        Share,      // 점유율 N% 이상
        MinesBlown, // 내 지뢰 N번 폭발
        Captured,   // 이번 판에 땅 N칸 차지
    }

    public enum ObjectiveReward : byte
    {
        Claim,     // 내 땅과 맞닿은 칸 Amount 개를 차지 (상대 땅 우선)
        Shield,    // 내 전선(내 것이 아닌 칸과 맞닿은 내 땅)을 2턴 보호
        Beacon,    // 주변이 가장 넓게 열린 내 땅에 거점을 세운다
        ExtraTurn, // 내 다음 유닛 배치 뒤 한 번 더 둔다
    }

    /// <summary>게임 중 목표 하나: 판마다 몇 개를 뽑아 플레이어에게 주고, 달성하는 즉시 보상 효과가 발동한다.</summary>
    public sealed class ObjectiveDef
    {
        public string Id;
        public ObjectiveGoal Goal;
        public int Target;
        public ObjectiveReward Reward;
        public int Amount;
        public string Text;       // 목표 문구
        public string RewardText; // 효과 문구
    }

    public sealed class Objective
    {
        public readonly ObjectiveDef Def;
        public bool Done;
        public Objective(ObjectiveDef def) { Def = def; }
        public Objective Clone() => new(Def) { Done = Done };
    }

    /// <summary>방금 달성한 게임 중 목표와 그 효과로 바뀐 칸 (뷰 연출용).</summary>
    public readonly struct ObjectiveEvent
    {
        public readonly ObjectiveDef Def;
        public readonly List<Cell> Cells;
        public ObjectiveEvent(ObjectiveDef def, List<Cell> cells) { Def = def; Cells = cells; }
    }

    public static class ObjectiveCatalog
    {
        public const int PerGame = 3;
        public const int ShieldTicks = 2 * Balance.TicksPerRound;

        public static readonly ObjectiveDef[] All =
        {
            new() { Id = "fusion2", Goal = ObjectiveGoal.Fusions, Target = 2, Reward = ObjectiveReward.Claim, Amount = 3, Text = "융합 2번", RewardText = "땅 3칸 흡수" },
            new() { Id = "resonance", Goal = ObjectiveGoal.Resonances, Target = 1, Reward = ObjectiveReward.Shield, Text = "공명 1번", RewardText = "전선 2턴 보호" },
            new() { Id = "steal", Goal = ObjectiveGoal.Steals, Target = 1, Reward = ObjectiveReward.ExtraTurn, Text = "상대 유닛으로 융합", RewardText = "한 번 더 두기" },
            new() { Id = "tier3", Goal = ObjectiveGoal.Tier3, Target = 1, Reward = ObjectiveReward.Claim, Amount = 5, Text = "3단계 유닛 만들기", RewardText = "땅 5칸 흡수" },
            new() { Id = "bigmove", Goal = ObjectiveGoal.BigMove, Target = 6, Reward = ObjectiveReward.Beacon, Text = "한 수에 6칸 차지", RewardText = "거점 세우기" },
            new() { Id = "skills3", Goal = ObjectiveGoal.Skills, Target = 3, Reward = ObjectiveReward.Beacon, Text = "스킬 3번 쓰기", RewardText = "거점 세우기" },
            new() { Id = "share45", Goal = ObjectiveGoal.Share, Target = 45, Reward = ObjectiveReward.Shield, Text = "점유율 45%", RewardText = "전선 2턴 보호" },
            new() { Id = "mine", Goal = ObjectiveGoal.MinesBlown, Target = 1, Reward = ObjectiveReward.Claim, Amount = 3, Text = "지뢰 터뜨리기", RewardText = "땅 3칸 흡수" },
            new() { Id = "land15", Goal = ObjectiveGoal.Captured, Target = 15, Reward = ObjectiveReward.ExtraTurn, Text = "땅 15칸 차지", RewardText = "한 번 더 두기" },
        };

        /// <summary>
        /// 이번 판의 목표 PerGame 개를 고른다 (seed 로 결정적). 덱으로 이룰 수 없는 목표는 뺀다:
        /// 지뢰가 없으면 지뢰 목표, 스킬이 3장 미만이면 스킬 목표.
        /// </summary>
        public static List<ObjectiveDef> Pick(int seed, IReadOnlyCollection<Card> deck)
        {
            int skills = 0, mines = 0;
            foreach (var c in deck)
            {
                if (!c.IsUnit) skills++;
                if (c.Skill == Skill.Mine) mines++;
            }
            var pool = new List<ObjectiveDef>();
            foreach (var d in All)
            {
                if (d.Goal == ObjectiveGoal.MinesBlown && mines == 0) continue;
                if (d.Goal == ObjectiveGoal.Skills && skills < d.Target) continue;
                pool.Add(d);
            }
            var rng = new Rng(seed * 7919 + 13);
            var picked = new List<ObjectiveDef>();
            while (picked.Count < PerGame && pool.Count > 0)
            {
                var k = rng.Next(pool.Count);
                picked.Add(pool[k]);
                pool.RemoveAt(k);
            }
            return picked;
        }
    }
}
