using System;
using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>
    /// 카드 상점: 덱에 넣을 수 있는 스킬 카드와 값(결정). 한 종류를 MaxCopies 장까지 가질 수 있고, 한 장씩 산다.
    /// 처음에는 Starter 장수만큼 갖고 시작한다. 유닛 카드는 덱 편성 대상이 아니다 (항상 원소별 4장).
    /// </summary>
    public static class CardShop
    {
        public const int MaxCopies = 2;  // 한 종류를 덱에 넣을 수 있는(가질 수 있는) 최대 장수
        public const int SkillSlots = 9; // 덱에 넣을 수 있는 스킬 카드 수
        public const int UnitsPerElement = 4;
        public const int MaxLevel = 3;   // 카드 강화 단계 (종류마다, Lv1 = 기본)

        public sealed class Entry
        {
            public Card Card;
            public string Code;  // 저장용 이름
            public int Starter;  // 처음부터 가진 장수
            public int Price;    // 한 장 값
        }

        public static readonly Entry[] All =
        {
            new() { Card = Card.Transmute(Element.Red), Code = "tr_red", Starter = 1, Price = 60 },
            new() { Card = Card.Transmute(Element.Blue), Code = "tr_blue", Starter = 1, Price = 60 },
            new() { Card = Card.Transmute(Element.Yellow), Code = "tr_yellow", Starter = 1, Price = 60 },
            new() { Card = Card.Of(Skill.Bleach), Code = "bleach", Starter = 1, Price = 80 },
            new() { Card = Card.Of(Skill.Mine), Code = "mine", Starter = 1, Price = 80 },
            new() { Card = Card.Of(Skill.Echo), Code = "echo", Starter = 1, Price = 80 },
            new() { Card = Card.Of(Skill.Beacon), Code = "beacon", Starter = 0, Price = 120 },
            new() { Card = Card.Of(Skill.Warp), Code = "warp", Starter = 0, Price = 160 },
            new() { Card = Card.Of(Skill.Lock), Code = "lock", Starter = 0, Price = 200 },
        };

        public static Entry Find(Card c)
        {
            foreach (var e in All)
                if (e.Card.Equals(c)) return e;
            return null;
        }

        public static Entry Find(string code)
        {
            foreach (var e in All)
                if (e.Code == code) return e;
            return null;
        }

        /// <summary>강화 값: Lv2 는 한 장 값의 1.5배, Lv3 는 3배.</summary>
        public static int UpgradePrice(Entry e, int toLevel) => toLevel <= 2 ? e.Price * 3 / 2 : e.Price * 3;

        /// <summary>단계별 효과 (index 0 = Lv1). 실제 효과는 Resolver.CastSkill / PlaceUnit(지뢰) / Game.GrowBeacons(거점).</summary>
        public static string[] LevelText(Card c) => c.Skill switch
        {
            Skill.Transmute or Skill.Bleach => new[] { "기본 효과", "바꾼 칸을 내 땅으로", "+ 상하좌우 칸도 내 땅으로" },
            Skill.Echo => new[] { "기본 효과", "+ 상하좌우 칸도 내 땅으로", "공명처럼 강화 (업화·해일·뇌우)" },
            Skill.Mine => new[] { "폭발 3x3", "폭발 3x3 + 십자 2칸", "폭발 5x5" },
            Skill.Beacon => new[] { "라운드마다 1칸 점유", "라운드마다 2칸 점유", "라운드마다 3칸 점유" },
            Skill.Warp => new[] { "기본 효과", "바꾼 두 칸을 내 땅으로", "+ 두 칸의 상하좌우도 내 땅으로" },
            Skill.Lock => new[] { "2턴 잠금", "3턴 잠금", "4턴 잠금" },
            _ => new[] { "", "", "" },
        };

        /// <summary>유닛 12장 + 고른 스킬 카드.</summary>
        public static List<Card> BuildDeck(IEnumerable<Card> skills)
        {
            var list = new List<Card>();
            foreach (var e in Elements.Base)
                for (var i = 0; i < UnitsPerElement; i++) list.Add(Card.Unit(e));
            list.AddRange(skills);
            return list;
        }
    }

    /// <summary>
    /// 플레이어의 카드 소장과 덱 편성 (스킬 카드만). 저장은 클라이언트가 Serialize 문자열로 한다.
    /// 덱의 각 카드는 가진 장수를 넘을 수 없고, 덱은 스킬 SkillSlots 장까지.
    /// </summary>
    public sealed class Collection
    {
        private readonly Dictionary<string, int> _owned = new();
        private readonly Dictionary<string, int> _levels = new(); // 강화 단계 (없으면 Lv1)
        public readonly List<Card> Skills = new(); // 덱에 넣은 스킬 카드

        public static Collection Starter()
        {
            var c = new Collection();
            foreach (var e in CardShop.All)
                if (e.Starter > 0) c._owned[e.Code] = e.Starter;
            c.FillDefault();
            return c;
        }

        public int Owned(Card card)
        {
            var e = CardShop.Find(card);
            return e != null && _owned.TryGetValue(e.Code, out var n) ? n : 0;
        }

        public int InDeck(Card card)
        {
            var n = 0;
            foreach (var c in Skills)
                if (c.Equals(card)) n++;
            return n;
        }

        public bool CanBuy(Card card) => CardShop.Find(card) != null && Owned(card) < CardShop.MaxCopies;

        /// <summary>한 장 산다 (값 계산은 부르는 쪽). 덱에 빈자리가 있으면 바로 넣는다.</summary>
        public bool Buy(Card card)
        {
            if (!CanBuy(card)) return false;
            _owned[CardShop.Find(card).Code] = Owned(card) + 1;
            if (CanAdd(card)) Skills.Add(card);
            return true;
        }

        public int Level(Card card)
        {
            var e = CardShop.Find(card);
            return e != null && _levels.TryGetValue(e.Code, out var n) ? n : 1;
        }

        /// <summary>한 장이라도 가진 카드를 한 단계 강화할 수 있는가 (값 계산은 부르는 쪽).</summary>
        public bool CanUpgrade(Card card) => Owned(card) > 0 && Level(card) < CardShop.MaxLevel;

        public bool Upgrade(Card card)
        {
            if (!CanUpgrade(card)) return false;
            _levels[CardShop.Find(card).Code] = Level(card) + 1;
            return true;
        }

        /// <summary>판에 넘길 강화 단계 (Lv2 이상만).</summary>
        public Dictionary<Card, int> Levels()
        {
            var d = new Dictionary<Card, int>();
            foreach (var e in CardShop.All)
                if (Level(e.Card) > 1) d[e.Card] = Level(e.Card);
            return d;
        }

        public bool CanAdd(Card card) => Skills.Count < CardShop.SkillSlots && InDeck(card) < Owned(card);

        public bool Add(Card card)
        {
            if (!CanAdd(card)) return false;
            Skills.Add(card);
            return true;
        }

        public bool Remove(Card card) => Skills.Remove(card);

        /// <summary>가진 카드로 덱을 다시 채운다 (상점 순서대로, 종류마다 한 장씩 돌아가며).</summary>
        public void FillDefault()
        {
            Skills.Clear();
            for (var copy = 1; copy <= CardShop.MaxCopies; copy++)
                foreach (var e in CardShop.All)
                    if (Owned(e.Card) >= copy && Skills.Count < CardShop.SkillSlots) Skills.Add(e.Card);
        }

        public List<Card> BuildDeck() => CardShop.BuildDeck(Skills);

        /// <summary>"owned|deck|levels" — owned 는 code:n 목록, deck 은 code 목록, levels 는 code:단계 목록.</summary>
        public string Serialize()
        {
            var owned = new List<string>();
            foreach (var kv in _owned) owned.Add($"{kv.Key}:{kv.Value}");
            var deck = new List<string>();
            foreach (var c in Skills) deck.Add(CardShop.Find(c).Code);
            var levels = new List<string>();
            foreach (var kv in _levels) levels.Add($"{kv.Key}:{kv.Value}");
            return string.Join(",", owned) + "|" + string.Join(",", deck) + "|" + string.Join(",", levels);
        }

        /// <summary>저장 문자열을 읽는다. 비었거나 깨졌으면 처음 상태. 규칙에 어긋나는 덱 카드는 버린다.</summary>
        public static Collection Parse(string s)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('|') < 0) return Starter();
            var c = new Collection();
            var parts = s.Split('|');
            foreach (var item in parts[0].Split(','))
            {
                var kv = item.Split(':');
                if (kv.Length == 2 && CardShop.Find(kv[0]) != null && int.TryParse(kv[1], out var n))
                    c._owned[kv[0]] = Math.Max(0, Math.Min(CardShop.MaxCopies, n));
            }
            foreach (var code in parts[1].Split(','))
            {
                var e = CardShop.Find(code);
                if (e != null) c.Add(e.Card);
            }
            if (parts.Length > 2) // 강화가 생기기 전 저장에는 없다
                foreach (var item in parts[2].Split(','))
                {
                    var kv = item.Split(':');
                    if (kv.Length == 2 && CardShop.Find(kv[0]) != null && int.TryParse(kv[1], out var n) && n > 1)
                        c._levels[kv[0]] = Math.Min(CardShop.MaxLevel, n);
                }
            return c;
        }
    }

    /// <summary>누적 기록 항목 (퀘스트 진행). 한 판의 기록은 Game.StatsOf 가 같은 항목으로 센다.</summary>
    public enum Stat
    {
        Games,      // 판 수 (판이 끝날 때)
        Wins,       // 승리
        Fusions,    // 융합 (2·3단계 모두)
        Tier3,      // 3단계(백색·흑색) 완성
        Resonances, // 공명
        Steals,     // 상대 유닛을 촉매로 쓴 융합
        Skills,     // 스킬 사용
        Captured,   // 차지한 칸 (유닛·스킬·지뢰 폭발)
        MinesBlown, // 내 지뢰가 터졌다
        Objectives, // 게임 중 목표 달성
        Count,
    }

    /// <summary>퀘스트 줄: 같은 기록으로 목표가 점점 커진다. 보상을 받으면 다음 단계가 열린다.</summary>
    public sealed class QuestLine
    {
        public string Id;
        public Stat Stat;
        public string Format; // {0} = 목표
        public int[] Targets;
        public int[] Rewards;

        public bool Finished(int claimed) => claimed >= Targets.Length;
        public string Title(int claimed) => string.Format(Format, Targets[Math.Min(claimed, Targets.Length - 1)]);
    }

    public static class QuestCatalog
    {
        public static readonly QuestLine[] Lines =
        {
            new() { Id = "games", Stat = Stat.Games, Format = "{0}판 플레이하기", Targets = new[] { 3, 10, 25, 50, 100 }, Rewards = new[] { 10, 20, 30, 40, 60 } },
            new() { Id = "wins", Stat = Stat.Wins, Format = "{0}번 승리하기", Targets = new[] { 2, 6, 15, 30, 60 }, Rewards = new[] { 15, 25, 40, 50, 75 } },
            new() { Id = "fusion", Stat = Stat.Fusions, Format = "융합 {0}번 하기", Targets = new[] { 5, 20, 50, 120, 250 }, Rewards = new[] { 10, 20, 30, 45, 60 } },
            new() { Id = "tier3", Stat = Stat.Tier3, Format = "3단계 유닛 {0}번 만들기", Targets = new[] { 1, 5, 15, 40 }, Rewards = new[] { 15, 30, 45, 75 } },
            new() { Id = "resonance", Stat = Stat.Resonances, Format = "공명 {0}번 하기", Targets = new[] { 3, 10, 30, 80 }, Rewards = new[] { 10, 20, 35, 50 } },
            new() { Id = "steal", Stat = Stat.Steals, Format = "상대 유닛으로 융합 {0}번", Targets = new[] { 2, 10, 30, 60 }, Rewards = new[] { 15, 25, 40, 60 } },
            new() { Id = "skills", Stat = Stat.Skills, Format = "스킬 카드 {0}번 쓰기", Targets = new[] { 5, 25, 60, 150 }, Rewards = new[] { 10, 20, 30, 50 } },
            new() { Id = "land", Stat = Stat.Captured, Format = "땅 {0}칸 차지하기", Targets = new[] { 100, 400, 1200, 3000 }, Rewards = new[] { 10, 25, 40, 60 } },
            new() { Id = "objectives", Stat = Stat.Objectives, Format = "게임 중 목표 {0}개 달성", Targets = new[] { 3, 15, 40, 100 }, Rewards = new[] { 15, 30, 45, 75 } },
        };
    }

    /// <summary>판 보상 (결정).</summary>
    public static class Rewards
    {
        // 카드를 금방 다 사지 않도록 낮게 잡았다 (강화·소모품까지 쓸 곳이 많다)
        public const int Loss = 5, WinBase = 10, PerStar = 5, FirstClear = 15, BossFirstClear = 50, PerObjective = 3;

        /// <summary>크로마 첨탑 층을 처음 깼을 때 (floor 0부터). 다시 깨면 없다.</summary>
        public static int ForFloor(int floor) => 10 + floor * 2;

        /// <param name="mode">모드 (StageCatalog.Modes). 보통은 1.5배, 어려움은 2배.</param>
        public static int ForBattle(StageDef stage, int stars, bool firstClear, int objectives, int mode = 0) =>
            ((stars > 0 ? WinBase + PerStar * stars : Loss)
             + (firstClear ? (stage.IsBoss ? BossFirstClear : FirstClear) : 0)
             + PerObjective * objectives) * (2 + mode) / 2;
    }

    /// <summary>판 시작 전 소모품: 출격할 때 결정을 내고 그 판에만 쓴다.</summary>
    public enum Boost : byte { Pioneer, ExtraObjective, FirstStrike }

    public static class BoostShop
    {
        public sealed class Entry
        {
            public Boost Boost;
            public string Name, Text;
            public int Price;
        }

        public const int PioneerCells = 3; // 개척: 시작 땅 옆으로 더 갖는 칸 수
        public const int UndoPrice = 15;   // 무르기 1회 값 (모아 두고 판 중에 쓴다)

        public static readonly Entry[] All =
        {
            new() { Boost = Boost.Pioneer, Name = "개척", Text = "시작 땅 옆 3칸을 더 갖고 시작", Price = 20 },
            new() { Boost = Boost.ExtraObjective, Name = "목표 추가", Text = "게임 중 목표가 4개", Price = 15 },
            new() { Boost = Boost.FirstStrike, Name = "선공", Text = "첫 턴에 유닛을 두 번 놓는다", Price = 25 },
        };
    }

    /// <summary>판에 가지고 들어가는 것: 카드 강화 단계와 소모품.</summary>
    public sealed class Loadout
    {
        public Dictionary<Card, int> Levels = new();
        public readonly HashSet<Boost> Boosts = new();
    }
}
