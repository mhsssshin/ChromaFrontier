using System;
using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>PVE 스테이지 하나: 맵(지형·시작 땅), 라운드 기믹, 상대(성향·난이도·덱).</summary>
    public sealed class StageDef
    {
        public int Index;          // 0..49
        public int Chapter;        // 0..4
        public int InChapter;      // 0..9 (9 = 보스)
        public string Name;
        public string MapNote;     // 맵 기믹 설명
        public bool IsBoss;
        public string Opponent;    // 상대 이름
        public string OpponentNote;
        public AiPersona Persona = AiPersona.Default;
        public AiLevel Level;      // 기본 난이도 (대기실 난이도 보정 전)
        public string[] Map;
        public StageRules Rules = new();
        public List<Card> AiDeck = Game.StarterDeck();

        public string Code => $"{Chapter + 1}-{InChapter + 1}";

        /// <summary>판을 만든다. 플레이어 덱을 안 주면 기본 덱. 플레이어의 게임 중 목표도 이때 뽑는다.</summary>
        /// <param name="loadout">플레이어의 카드 강화 단계와 소모품 (없으면 기본)</param>
        public Game CreateGame(int seed, List<Card> playerDeck = null, Loadout loadout = null)
        {
            playerDeck ??= Game.StarterDeck();
            var g = Game.Create(Board.Parse(Map), playerDeck, AiDeck, seed, Rules);
            var extra = loadout != null && loadout.Boosts.Contains(Boost.ExtraObjective) ? 1 : 0;
            g.SetObjectives(ObjectiveCatalog.Pick(seed, playerDeck, ObjectiveCatalog.PerGame + extra));
            if (loadout != null) g.ApplyLoadout(loadout);
            return g;
        }
    }

    public sealed class ChapterDef
    {
        public string Name, Theme;
    }

    /// <summary>
    /// 스테이지 50개 (5챕터 × 10, 각 챕터 10번째는 보스). PRD §5 의 맵 기믹과 보스 성향을 챕터별로 배치했다.
    ///   1장 푸른 초원 — 기믹 없음 / 보스 이그니스(어그로)
    ///   2장 화산 지대 — 몇 라운드마다 양쪽 유닛 무작위 폭발 / 보스 강화 이그니스
    ///   3장 얼음성 — 얼음 칸에 놓으면 한 칸 더 미끄러짐 / 보스 글라시아(통제)
    ///   4장 끊어진 다리 — 낭떠러지로 병목 / 보스 호문클루스(약탈)
    ///   5장 공허의 탑 — 컨베이어·블랙홀·침묵 지대 / 보스 강화 호문클루스
    /// 맵은 Board.Parse 의 ASCII 지도. 난이도는 뒤로 갈수록 올라가고 보스는 한 단계 더 높다.
    /// </summary>
    public static class StageCatalog
    {
        public const int Count = 50;
        public const int PerChapter = 10;

        public static readonly ChapterDef[] Chapters =
        {
            new() { Name = "푸른 초원", Theme = "기믹 없는 기본 전장" },
            new() { Name = "화산 지대", Theme = "몇 라운드마다 화산이 터져 양쪽 유닛이 무작위로 폭발한다" },
            new() { Name = "얼음성", Theme = "얼음 칸에 놓은 유닛은 한 칸 더 미끄러진다" },
            new() { Name = "끊어진 다리", Theme = "낭떠러지가 길을 끊어 좁은 다리를 두고 싸운다" },
            new() { Name = "공허의 탑", Theme = "컨베이어 벨트, 블랙홀, 침묵 지대" },
        };

        // ---------------------------------------------------------------- 보스·상대 성향 (PRD §5)
        public static readonly AiPersona Ignis = new() { Id = "ignis", StealWeight = 0.6f, LockWeight = 0.4f };
        public static readonly AiPersona IgnisPlus = new() { Id = "ignis+", StealWeight = 0.9f, LockWeight = 0.4f };
        public static readonly AiPersona Glacia = new() { Id = "glacia", ShieldWeight = 0.4f, LockWeight = 2.2f, BleachWeight = 1.6f };
        public static readonly AiPersona Homunculus = new() { Id = "homunculus", CatalystSteal = 5f, LoneUnitPenalty = 2.5f, MineWeight = 1.3f, WarpBonus = 1.5f };
        public static readonly AiPersona HomunculusPlus = new() { Id = "homunculus+", CatalystSteal = 7f, LoneUnitPenalty = 2f, MineWeight = 1.5f, WarpBonus = 2f };

        /// <summary>AI 덱. 원소 변환은 빨·파·노 순서로 색을 돌려 넣는다.</summary>
        private static List<Card> Deck(int red, int blue, int yellow, int warp, int lockN, int bleach, int mine, int transmute = 0, int echo = 0, int beacon = 0)
        {
            var d = new List<Card>();
            void Add(Card c, int n) { for (var i = 0; i < n; i++) d.Add(c); }
            Add(Card.Unit(Element.Red), red);
            Add(Card.Unit(Element.Blue), blue);
            Add(Card.Unit(Element.Yellow), yellow);
            Add(Card.Of(Skill.Warp), warp);
            Add(Card.Of(Skill.Lock), lockN);
            Add(Card.Of(Skill.Bleach), bleach);
            Add(Card.Of(Skill.Mine), mine);
            for (var i = 0; i < transmute; i++) d.Add(Card.Transmute(Elements.Base[i % 3]));
            Add(Card.Of(Skill.Echo), echo);
            Add(Card.Of(Skill.Beacon), beacon);
            return d;
        }

        private static StageDef[] _all;
        public static StageDef[] All => _all ??= Build();

        public static StageDef Get(int index) => All[Math.Max(0, Math.Min(Count - 1, index))];

        /// <summary>별: 승리 1개, 점유율 50% 이상 2개, 65% 이상 3개. 패배·무승부 0개.</summary>
        public static int Stars(Game g)
        {
            if (g.Winner != Side.Player) return 0;
            var share = (float)g.Board.Count(Side.Player) / Math.Max(1, g.Board.Playable);
            return share >= 0.65f ? 3 : share >= 0.5f ? 2 : 1;
        }

        /// <summary>대기실 난이도 보정(-1 쉬움, 0 보통, +1 어려움)을 더한 실제 AI 난이도.</summary>
        public static AiLevel LevelFor(StageDef s, int offset) => (AiLevel)Math.Max(0, Math.Min((int)AiLevel.Hard, (int)s.Level + offset));

        private static StageDef[] Build()
        {
            var list = new StageDef[Count];
            for (var i = 0; i < Count; i++)
            {
                var ch = i / PerChapter;
                var k = i % PerChapter;
                var s = new StageDef
                {
                    Index = i,
                    Chapter = ch,
                    InChapter = k,
                    IsBoss = k == PerChapter - 1,
                    Name = $"{Chapters[ch].Name} {k + 1}",
                    Opponent = "떠돌이 원소술사",
                    OpponentNote = "평범하게 가장 이득인 수를 둔다.",
                };
                // 완만하게: 1~2장 초반 입문, 2장 후반~3장 쉬움, 4~5장 보통, 5장 막바지 어려움. 보스는 한 단계 위.
                var baseLevel = i < 15 ? AiLevel.Beginner : i < 30 ? AiLevel.Easy : i < 45 ? AiLevel.Normal : AiLevel.Hard;
                s.Level = s.IsBoss ? (AiLevel)Math.Min((int)AiLevel.Hard, (int)baseLevel + 1) : baseLevel;
                switch (ch)
                {
                    case 0: Meadow(s, k); break;
                    case 1: Volcano(s, k); break;
                    case 2: IceCastle(s, k); break;
                    case 3: Bridges(s, k); break;
                    default: VoidTower(s, k); break;
                }
                list[i] = s;
            }
            return list;
        }

        /// <summary>w×h 맵: terrain(x, y) 로 지형 글자를 정하고, 왼쪽 아래·오른쪽 위 2x2 에 시작 땅을 놓는다.</summary>
        private static string[] MakeMap(int w, int h, Func<int, int, char> terrain)
        {
            var rows = new string[h];
            for (var y = 0; y < h; y++)
            {
                var row = new char[w];
                for (var x = 0; x < w; x++)
                {
                    var start = x < 2 && y >= h - 2 ? 'P' : x >= w - 2 && y < 2 ? 'A' : '\0';
                    row[x] = start != '\0' ? start : terrain(x, y);
                }
                rows[y] = new string(row);
            }
            return rows;
        }

        private static int Hash(int x, int y, int salt)
        {
            var h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(salt * 83492791);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (int)(h % 100);
        }

        // ---------------------------------------------------------------- 1장 푸른 초원
        private static void Meadow(StageDef s, int k)
        {
            var size = k < 3 ? 7 : 8;
            s.Map = MakeMap(size, size, (x, y) => '.');
            s.MapNote = k < 3 ? "작은 7x7 평원. 기본 규칙을 익히자." : "기믹 없는 8x8 평원.";
            s.AiDeck = Deck(5, 5, 5, 0, 0, 0, 0); // 1장 상대는 스킬 없이 유닛만
            if (!s.IsBoss) return;
            s.Name = "불꽃의 이그니스";
            s.Opponent = "이그니스";
            s.OpponentNote = "어그로형: 화염 유닛만 내며 방어를 무시하고 전선을 밀고 들어온다.";
            s.Persona = Ignis;
            s.AiDeck = Deck(14, 0, 0, 0, 0, 2, 2, echo: 2);
        }

        // ---------------------------------------------------------------- 2장 화산 지대
        private static void Volcano(StageDef s, int k)
        {
            s.Map = MakeMap(8, 8, (x, y) => k >= 5 && Hash(x, y, s.Index) < 6 ? '#' : '.');
            s.Rules.LavaEvery = k < 4 ? 3 : 2;
            s.Rules.LavaCount = k < 6 ? 1 : 2;
            s.MapNote = $"{s.Rules.LavaEvery}라운드마다 화산이 터져 양쪽 유닛이 {s.Rules.LavaCount}개씩 무작위로 폭발한다.";
            s.AiDeck = Deck(6, 3, 3, 1, 1, 1, 2, transmute: 2, echo: 1, beacon: 1);
            if (!s.IsBoss) return;
            s.Name = "용암의 이그니스";
            s.Opponent = "이그니스 (각성)";
            s.OpponentNote = "어그로형 강화: 화염만으로 더 거세게 내 땅을 빼앗으러 온다.";
            s.Persona = IgnisPlus;
            s.Rules.LavaEvery = 2;
            s.Rules.LavaCount = 2;
            s.MapNote = "2라운드마다 화산이 터져 양쪽 유닛이 2개씩 무작위로 폭발한다.";
            s.AiDeck = Deck(16, 0, 0, 0, 0, 1, 2, echo: 3);
        }

        // ---------------------------------------------------------------- 3장 얼음성
        private static void IceCastle(StageDef s, int k)
        {
            var density = s.IsBoss ? 75 : 20 + k * 6;
            s.Map = MakeMap(8, 8, (x, y) => Hash(x, y, s.Index) < density ? '~' : '.');
            s.MapNote = s.IsBoss ? "거의 모든 칸이 얼음. 놓은 유닛이 한 칸 더 미끄러진다." : "얼음 칸에 놓은 유닛은 밀고 나온 방향으로 한 칸 더 미끄러진다.";
            s.AiDeck = Deck(3, 6, 3, 1, 1, 1, 2, transmute: 2, echo: 1, beacon: 1);
            if (!s.IsBoss) return;
            s.Name = "빙결의 글라시아";
            s.Opponent = "글라시아";
            s.OpponentNote = "통제형: 물·보라 위주에 잠금과 탈색으로 접근을 원천 차단한다.";
            s.Persona = Glacia;
            s.AiDeck = Deck(4, 8, 0, 0, 3, 3, 1, beacon: 2);
        }

        // ---------------------------------------------------------------- 4장 끊어진 다리
        private static readonly Func<int, int, bool>[] BridgeHoles =
        {
            (x, y) => (x == 3 || x == 4) && (y == 3 || y == 4),                                   // 가운데 구덩이
            (x, y) => x == y && x != 2 && x != 5,                                                  // 대각선 강 + 다리 둘
            (x, y) => (x == 3 && y != 1 && y != 6) || (y == 4 && x != 1 && x != 6),                // 십자 협곡
            (x, y) => x % 3 == 1 && y % 3 == 1,                                                    // 흩어진 구멍
            (x, y) => x == y && x != 3 && x != 4,                                                  // 대각선 강 + 가운데 다리
        };

        private static void Bridges(StageDef s, int k)
        {
            var holes = BridgeHoles[k % BridgeHoles.Length];
            var icy = k >= 5;
            s.Map = MakeMap(8, 8, (x, y) => holes(x, y) ? '#' : icy && x + y == 7 ? '~' : '.');
            s.MapNote = icy ? "낭떠러지가 길을 끊고, 가운데 대각선은 얼음이다." : "낭떠러지가 길을 끊는다. 좁은 다리를 먼저 잡아라.";
            if (!s.IsBoss) return;
            s.Name = "모방자 호문클루스";
            s.Opponent = "호문클루스";
            s.OpponentNote = "약탈형: 유닛을 아끼다가 내 유닛 옆에만 놓아 콤보를 훔쳐 간다.";
            s.Persona = Homunculus;
            s.Map = MakeMap(8, 8, (x, y) => BridgeHoles[1](x, y) ? '#' : '.');
            s.MapNote = "대각선 강을 두 다리로만 건널 수 있다.";
            s.AiDeck = Deck(4, 4, 4, 3, 0, 1, 2, transmute: 3);
        }

        // ---------------------------------------------------------------- 5장 공허의 탑 (9x9)
        private static void VoidTower(StageDef s, int k)
        {
            s.Rules.MaxTurns = 22;
            bool Conveyor(int x, int y, int row) => y == row && x >= 1 && x <= 7;
            Func<int, int, char> map;
            switch (k)
            {
                case 0: map = (x, y) => Conveyor(x, y, 4) ? '>' : '.'; s.MapNote = "가운데 줄이 컨베이어 벨트: 매 라운드 오른쪽으로 한 칸씩 흘러가고 끝에서 떨어진다."; break;
                case 1: map = (x, y) => x == 4 && y == 4 ? 'O' : '.'; s.MapNote = "가운데 블랙홀이 매 라운드 주변 8칸을 빨아들여 비운다."; break;
                case 2: map = (x, y) => Math.Abs(x - 4) <= 1 && Math.Abs(y - 4) <= 1 ? 's' : '.'; s.MapNote = "가운데 3x3 침묵 지대: 거기서는 융합·공명이 일어나지 않는다."; break;
                case 3: map = (x, y) => Conveyor(x, y, 2) || Conveyor(x, y, 6) ? '>' : '.'; s.MapNote = "컨베이어 벨트 두 줄."; break;
                case 4: map = (x, y) => x == 4 && y == 4 ? 'O' : (x == 2 || x == 6) && (y == 2 || y == 6) ? 's' : '.'; s.MapNote = "블랙홀과 네 귀퉁이의 침묵 지대."; break;
                case 5: map = (x, y) => Conveyor(x, y, 4) ? '>' : x == 4 && (y == 1 || y == 7) ? 'O' : '.'; s.MapNote = "컨베이어 벨트와 위아래 블랙홀."; break;
                case 6: map = (x, y) => Conveyor(x, y, 4) ? '>' : Math.Abs(x - y) <= 0 && x != 4 ? 's' : '.'; s.MapNote = "컨베이어 벨트와 대각선 침묵 지대."; break;
                case 7: map = (x, y) => (x == 2 && y == 4) || (x == 6 && y == 4) ? 'O' : Conveyor(x, y, 1) || Conveyor(x, y, 7) ? '>' : '.'; s.MapNote = "블랙홀 둘과 가장자리 컨베이어 벨트."; break;
                case 8: map = (x, y) => x == 4 && y == 4 ? 'O' : Conveyor(x, y, 2) || Conveyor(x, y, 6) ? '>' : Math.Abs(x - 4) + Math.Abs(y - 4) == 2 ? 's' : '.'; s.MapNote = "블랙홀, 침묵 고리, 컨베이어 벨트 두 줄."; break;
                default: map = (x, y) => x == 4 && (y == 2 || y == 6) ? 'O' : Conveyor(x, y, 4) ? '>' : Math.Abs(x - 4) == 2 && Math.Abs(y - 4) == 2 ? 's' : '.'; s.MapNote = "공허의 탑 꼭대기: 블랙홀 둘, 컨베이어, 침묵 지대가 모두 있다."; break;
            }
            s.Map = MakeMap(9, 9, map);
            s.AiDeck = Deck(4, 4, 4, 2, 1, 1, 1, transmute: 2, echo: 1, beacon: 1);
            if (!s.IsBoss) return;
            s.Name = "공허의 호문클루스";
            s.Opponent = "호문클루스 (각성)";
            s.OpponentNote = "약탈형 강화: 공간 왜곡까지 써 가며 내 콤보를 빼앗는다.";
            s.Persona = HomunculusPlus;
            s.AiDeck = Deck(4, 4, 4, 4, 0, 1, 1, transmute: 3, beacon: 1);
        }
    }
}
