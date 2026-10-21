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
        public const int SkillSlots = 9; // 덱에 넣을 수 있는 스킬 카드 수 (기본값, 별의 길 보상으로 늘어난다)
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
        public int Slots = CardShop.SkillSlots;     // 덱에 넣을 수 있는 스킬 카드 수 (별의 길 보상으로 늘어난다)

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

        public bool CanAdd(Card card) => Skills.Count < Slots && InDeck(card) < Owned(card);

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
                    if (Owned(e.Card) >= copy && Skills.Count < Slots) Skills.Add(e.Card);
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
        public static Collection Parse(string s, int slots = CardShop.SkillSlots)
        {
            if (string.IsNullOrEmpty(s) || s.IndexOf('|') < 0)
            {
                var starter = Starter();
                starter.Slots = slots;
                return starter;
            }
            var c = new Collection { Slots = slots };
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
        public const int UndoPack = 5, UndoPackPrice = 60; // 상점 무르기 묶음 (5개를 4개 값에)

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

    /// <summary>별의 길 보상 종류.</summary>
    public enum RoadReward : byte
    {
        Gems,        // 결정 Amount 개
        Undos,       // 무르기 Amount 회
        Slot,        // 덱 스킬 슬롯 +1
        ThemeUnlock, // 보드 테마를 상점에서 살 수 있게 된다 (받기 없이 별만 모으면 열림)
        ThemeGift,   // 보드 테마를 그냥 준다
    }

    public sealed class RoadStep
    {
        public int Stars;
        public RoadReward Kind;
        public int Amount;
        public string Theme; // ThemeUnlock / ThemeGift

        /// <summary>받기 버튼으로 받는 보상인가 (테마 해금은 별만 모으면 저절로 열린다).</summary>
        public bool Claimable => Kind != RoadReward.ThemeUnlock;
    }

    /// <summary>
    /// 별의 길: 스테이지(모드마다 3개씩), 크로마 첨탑(층마다 3개), 퀘스트(단계마다 1개)에서 모은 별의 합으로
    /// 단계 보상을 받고 칭호가 오른다. 보상은 결정·무르기·덱 슬롯·보드 테마.
    /// </summary>
    public static class StarRoad
    {
        public const int TowerStarsPerFloor = 3; // 깨기 · 무르기 없이 · 힌트 없이

        public static readonly RoadStep[] Steps =
        {
            new() { Stars = 5, Kind = RoadReward.Gems, Amount = 30 },
            new() { Stars = 12, Kind = RoadReward.Undos, Amount = 2 },
            new() { Stars = 20, Kind = RoadReward.ThemeUnlock, Theme = "blossom" },
            new() { Stars = 30, Kind = RoadReward.Gems, Amount = 50 },
            new() { Stars = 42, Kind = RoadReward.Slot },
            new() { Stars = 55, Kind = RoadReward.Undos, Amount = 3 },
            new() { Stars = 70, Kind = RoadReward.ThemeUnlock, Theme = "autumn" },
            new() { Stars = 90, Kind = RoadReward.Gems, Amount = 80 },
            new() { Stars = 110, Kind = RoadReward.Undos, Amount = 5 },
            new() { Stars = 135, Kind = RoadReward.ThemeUnlock, Theme = "snow" },
            new() { Stars = 160, Kind = RoadReward.Gems, Amount = 120 },
            new() { Stars = 190, Kind = RoadReward.Slot },
            new() { Stars = 225, Kind = RoadReward.ThemeUnlock, Theme = "desert" },
            new() { Stars = 260, Kind = RoadReward.Gems, Amount = 150 },
            new() { Stars = 300, Kind = RoadReward.Undos, Amount = 8 },
            new() { Stars = 340, Kind = RoadReward.ThemeUnlock, Theme = "night" },
            new() { Stars = 390, Kind = RoadReward.Gems, Amount = 200 },
            new() { Stars = 440, Kind = RoadReward.ThemeUnlock, Theme = "sky" },
            new() { Stars = 500, Kind = RoadReward.Gems, Amount = 300 },
            new() { Stars = 560, Kind = RoadReward.ThemeGift, Theme = "golden" },
        };

        /// <summary>별 수에 따른 칭호 (오름차순).</summary>
        public static readonly (int Stars, string Title)[] Ranks =
        {
            (0, "새싹 크로마술사"), (20, "견습 크로마술사"), (60, "크로마술사"), (120, "숙련 크로마술사"),
            (200, "원소 조율사"), (300, "원소 현자"), (420, "첨탑의 현자"), (560, "크로마 마스터"),
        };

        /// <summary>모을 수 있는 별: 스테이지 50 × 3 × 모드 3 + 첨탑 층 × 3 + 퀘스트 단계 수.</summary>
        public static int MaxStars => StageCatalog.Count * 3 * StageCatalog.Modes + PuzzleCatalog.Count * TowerStarsPerFloor + QuestTiers;

        public static int QuestTiers
        {
            get
            {
                var n = 0;
                foreach (var q in QuestCatalog.Lines) n += q.Targets.Length;
                return n;
            }
        }

        public static int RankIndex(int stars)
        {
            var k = 0;
            for (var i = 0; i < Ranks.Length; i++)
                if (stars >= Ranks[i].Stars) k = i;
            return k;
        }

        public static string RankOf(int stars) => Ranks[RankIndex(stars)].Title;

        public static string Text(RoadStep s) => s.Kind switch
        {
            RoadReward.Gems => $"결정 {s.Amount}개",
            RoadReward.Undos => $"무르기 {s.Amount}회",
            RoadReward.Slot => "덱 스킬 슬롯 +1",
            RoadReward.ThemeUnlock => $"테마 「{ThemeShop.Find(s.Theme).Name}」 판매 시작",
            RoadReward.ThemeGift => $"테마 「{ThemeShop.Find(s.Theme).Name}」 선물",
            _ => "",
        };

        /// <summary>받은 단계 중 덱 슬롯 보상 수만큼 늘어난 덱 스킬 슬롯.</summary>
        public static int SlotsFor(Func<int, bool> claimed)
        {
            var n = CardShop.SkillSlots;
            for (var i = 0; i < Steps.Length; i++)
                if (Steps[i].Kind == RoadReward.Slot && claimed(i)) n++;
            return n;
        }
    }

    /// <summary>보드 꾸미기: 빈 칸(풀밭)·장식·바다 그림을 바꾼다. 내 땅(파랑)과 상대 땅(빨강) 색은 그대로라 게임에는 영향이 없다.</summary>
    public sealed class ThemeDef
    {
        public string Id;    // 그림 이름 뒤에 붙는 꼬리 ("" = 기본)
        public string Name, Note;
        public int Price;    // 결정 (0 = 사지 않음: 기본이거나 별의 길 선물)
    }

    public static class ThemeShop
    {
        public static readonly ThemeDef[] All =
        {
            new() { Id = "", Name = "초원", Note = "처음 섬의 풀밭" },
            new() { Id = "blossom", Name = "벚꽃 동산", Note = "꽃잎이 흩날리는 봄 언덕", Price = 250 },
            new() { Id = "autumn", Name = "가을 숲", Note = "단풍이 물든 황금 들판", Price = 300 },
            new() { Id = "snow", Name = "설원", Note = "눈 덮인 겨울 섬", Price = 350 },
            new() { Id = "desert", Name = "노을 사막", Note = "모래 언덕과 노을 바다", Price = 400 },
            new() { Id = "night", Name = "별밤", Note = "반딧불이 반짝이는 밤의 섬", Price = 500 },
            new() { Id = "sky", Name = "하늘 정원", Note = "구름 위에 뜬 정원", Price = 600 },
            new() { Id = "golden", Name = "황금 성역", Note = "크로마 마스터의 땅 (별의 길 선물)" },
        };

        public static ThemeDef Find(string id)
        {
            foreach (var t in All)
                if (t.Id == id) return t;
            return null;
        }

        /// <summary>이 테마를 여는 별의 길 단계 (기본 테마면 null).</summary>
        public static RoadStep StepOf(string id)
        {
            foreach (var s in StarRoad.Steps)
                if (s.Theme == id) return s;
            return null;
        }
    }

    /// <summary>타일 스킨의 한 갈래: 재질(풀·나무·대리석…) 또는 색. 내 땅 타일 그림 = 재질 × 색.</summary>
    public sealed class SkinPart
    {
        public string Id, Name;
        public int Price;        // 결정 (0 = 처음부터 가짐)
        public string HexA, HexB; // 색: 윗면 진한 쪽·밝은 쪽 (art/tiles.mjs 의 SKIN_COLORS 와 같게)
        public string Note = "";
    }

    /// <summary>
    /// 유닛 스킨 세트: 원소 유닛 9종(1~3단계 + 무속성)의 그림을 통째로 바꾼다. 그림 이름은 Units/unit_원소_세트
    /// (기본 세트는 꼬리 없음). 원소는 색과 실루엣으로 그대로 알아볼 수 있게 그린다 (art/units_*.mjs).
    /// 대전에서는 각 팀 유닛이 그 팀 사람의 세트로 보인다. 싱글 모드의 AI 는 늘 기본.
    /// </summary>
    public static class UnitSkins
    {
        public static readonly SkinPart[] All =
        {
            new() { Id = "", Name = "기본", Note = "처음 원소 친구들" },
            new() { Id = "halloween", Name = "할로윈", Price = 500, Note = "잭오랜턴·물귀신·묘비 번개·검은 고양이…" },
        };

        public static SkinPart Find(string id)
        {
            foreach (var s in All)
                if (s.Id == (id ?? "")) return s;
            return All[0];
        }

        public static string Sprite(Element e, string set) =>
            "Units/unit_" + e.ToString().ToLowerInvariant() + (string.IsNullOrEmpty(set) || Find(set).Id == "" ? "" : "_" + Find(set).Id);
    }

    /// <summary>
    /// 내 땅 타일 스킨: 재질 7종 × 색 8종을 따로 사서 맞춘다. 빨강은 상대 팀 색이라 팔지 않는다(PvP 에서 색이 겹치면 상대를 빨강으로).
    /// 그림은 art/tiles.mjs 가 조합마다 굽는다 (Tiles/skin_재질_색, 기본 재질의 파랑·빨강은 예전 tile_player·tile_ai).
    /// 2:2 대전에서는 팀 색을 파랑(우리)·빨강(상대)으로 고정하고 재질만 각자 스킨을 쓴다.
    /// </summary>
    public static class TileSkins
    {
        public const string Default = "plain:blue";
        public const string EnemyColor = "red";

        public static readonly SkinPart[] Materials =
        {
            new() { Id = "plain", Name = "기본" },
            new() { Id = "grass", Name = "풀밭", Price = 150 },
            new() { Id = "wood", Name = "나무", Price = 200 },
            new() { Id = "stone", Name = "돌길", Price = 250 },
            new() { Id = "water", Name = "물결", Price = 300 },
            new() { Id = "marble", Name = "대리석", Price = 350 },
            new() { Id = "crystal", Name = "크리스탈", Price = 450 },
        };

        public static readonly SkinPart[] Colors =
        {
            new() { Id = "blue", Name = "파랑", HexA = "#2f7fe8", HexB = "#6db4ff" },
            new() { Id = "sky", Name = "하늘", Price = 100, HexA = "#1fa8d0", HexB = "#7fe2f5" },
            new() { Id = "navy", Name = "남색", Price = 100, HexA = "#3442b0", HexB = "#7f8cf0" },
            new() { Id = "purple", Name = "보라", Price = 150, HexA = "#8048d8", HexB = "#c09cff" },
            new() { Id = "pink", Name = "분홍", Price = 150, HexA = "#d84f9c", HexB = "#ff9fd0" },
            new() { Id = "orange", Name = "주황", Price = 150, HexA = "#e8801f", HexB = "#ffc070" },
            new() { Id = "white", Name = "백색", Price = 200, HexA = "#aab2c6", HexB = "#ffffff" },
            new() { Id = "black", Name = "흑색", Price = 250, HexA = "#2e2e3e", HexB = "#6a6a86" },
        };

        /// <summary>상대 팀 색 (상점에서는 안 판다).</summary>
        public static readonly SkinPart Red = new() { Id = EnemyColor, Name = "빨강", HexA = "#e2463f", HexB = "#ff8a78" };

        public static SkinPart Material(string id) => Find(Materials, id) ?? Materials[0];
        public static SkinPart Color(string id) => id == EnemyColor ? Red : Find(Colors, id) ?? Colors[0];

        private static SkinPart Find(SkinPart[] list, string id)
        {
            foreach (var p in list)
                if (p.Id == id) return p;
            return null;
        }

        /// <summary>"재질:색" 을 나눈다. 모르는 값은 기본으로.</summary>
        public static void Split(string skin, out string material, out string color)
        {
            var parts = (skin ?? "").Split(':');
            material = Material(parts.Length > 0 ? parts[0] : "").Id;
            color = Color(parts.Length > 1 ? parts[1] : "").Id;
        }

        public static string Join(string material, string color) => material + ":" + color;

        /// <summary>그 조합의 타일 그림 이름.</summary>
        public static string Sprite(string material, string color)
        {
            if (material == "plain" && color == "blue") return "Tiles/tile_player";
            if (material == "plain" && color == EnemyColor) return "Tiles/tile_ai";
            return $"Tiles/skin_{material}_{color}";
        }

        public static string Sprite(string skin)
        {
            Split(skin, out var m, out var c);
            return Sprite(m, c);
        }

        /// <summary>같은 색이면(대전 1:1) 상대 쪽 색을 빨강으로 바꾼 스킨.</summary>
        public static string AgainstMine(string theirs, string mine)
        {
            Split(theirs, out var tm, out var tc);
            Split(mine, out _, out var mc);
            return Join(tm, tc == mc ? EnemyColor : tc);
        }

        /// <summary>대전 자리 스킨 "재질:색:유닛세트" 의 유닛 세트 (없으면 기본 "").</summary>
        public static string UnitPart(string skin)
        {
            var parts = (skin ?? "").Split(':');
            return parts.Length > 2 ? UnitSkins.Find(parts[2]).Id : "";
        }

        /// <summary>재질은 그대로, 색만 바꾼 스킨 (2:2 팀 색 고정).</summary>
        public static string WithColor(string skin, string color)
        {
            Split(skin, out var m, out _);
            return Join(m, color);
        }
    }
}
