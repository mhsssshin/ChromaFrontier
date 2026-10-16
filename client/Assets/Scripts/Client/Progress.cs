using System;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 오프라인 진행 저장 (LocalStore: WebGL 은 브라우저 localStorage, 그 밖은 PlayerPrefs).
    /// 해금된 스테이지 수, 스테이지별 최고 별, 난이도 보정, 튜토리얼 완료·진행 레슨, 본 팁, 재화(결정), 카드 소장·덱, 퀘스트 누적 기록·받은 단계.
    /// 값은 바뀌는 즉시 저장된다.
    /// </summary>
    public static class Progress
    {
        // 해금·별은 모드마다 따로: 쉬움은 예전 키 그대로(cf.unlocked), 보통·어려움은 뒤에 .1 / .2
        private const string UnlockedKey = "cf.unlocked", StarsKey = "cf.stars", ModeKey = "cf.mode";
        private const string TutorialKey = "cf.tutorial", LessonKey = "cf.tutorialLesson", TipPrefix = "cf.tip.";

        private static readonly int[][] _stars = new int[StageCatalog.Modes][];

        private static string Suffix(int mode) => mode == 0 ? "" : "." + mode;

        /// <summary>지금 하는 모드 (0 쉬움, 1 보통, 2 어려움). 열린 모드까지만.</summary>
        public static int Mode
        {
            get
            {
                var m = Mathf.Clamp(LocalStore.GetInt(ModeKey, 0), 0, StageCatalog.Modes - 1);
                while (m > 0 && !ModeUnlocked(m)) m--;
                return m;
            }
            set => LocalStore.SetInt(ModeKey, value);
        }

        /// <summary>앞 모드의 마지막 스테이지(5-10)를 깨면 열린다.</summary>
        public static bool ModeUnlocked(int mode) => mode == 0 || (mode < StageCatalog.Modes && StarsOf(mode - 1, StageCatalog.Count - 1) > 0);

        public static int UnlockedIn(int mode) => Mathf.Clamp(LocalStore.GetInt(UnlockedKey + Suffix(mode), 1), 1, StageCatalog.Count);

        /// <summary>지금 모드에서 플레이할 수 있는 스테이지 수 (1 이면 첫 스테이지만).</summary>
        public static int Unlocked
        {
            get => UnlockedIn(Mode);
            private set => LocalStore.SetInt(UnlockedKey + Suffix(Mode), value);
        }

        public static bool IsUnlocked(int index) => index < Unlocked;

        /// <summary>튜토리얼을 끝냈거나 건너뛰었는가 (처음 실행이면 false → 튜토리얼부터). 한 번 끝내면 다시 나오지 않는다.</summary>
        public static bool TutorialDone
        {
            get => LocalStore.GetInt(TutorialKey, 0) == 1;
            set => LocalStore.SetInt(TutorialKey, value ? 1 : 0);
        }

        /// <summary>튜토리얼을 하다 나갔을 때 이어서 시작할 레슨.</summary>
        public static int TutorialLesson
        {
            get => Mathf.Max(0, LocalStore.GetInt(LessonKey, 0));
            set => LocalStore.SetInt(LessonKey, value);
        }

        public static bool TipSeen(string id) => LocalStore.GetInt(TipPrefix + id, 0) == 1;

        public static void MarkTipSeen(string id) => LocalStore.SetInt(TipPrefix + id, 1);

        public static int StarsOf(int index) => StarsOf(Mode, index);

        public static int StarsOf(int mode, int index) => Load(mode)[index];

        public static int TotalStars()
        {
            var n = 0;
            foreach (var s in Load(Mode)) n += s;
            return n;
        }

        /// <summary>판 결과 기록 (지금 모드): 최고 별 갱신, 이기면 다음 스테이지 해금.</summary>
        public static void Record(int index, int stars)
        {
            var mode = Mode;
            var arr = Load(mode);
            arr[index] = Math.Max(arr[index], stars);
            if (stars > 0 && index + 1 >= Unlocked) Unlocked = Math.Min(StageCatalog.Count, index + 2);
            LocalStore.SetString(StarsKey + Suffix(mode), string.Join(",", arr));
        }

        // ---------------------------------------------------------------- 재화·덱·퀘스트

        private const string GemsKey = "cf.gems", CardsKey = "cf.cards", StatPrefix = "cf.stat.", QuestPrefix = "cf.quest.", UndoKey = "cf.items.undo";

        private static Collection _cards;

        /// <summary>재화 (크로마 결정). 판을 끝내거나 퀘스트를 깨면 받고, 스킬 카드를 사는 데 쓴다.</summary>
        public static int Gems
        {
            get => Mathf.Max(0, LocalStore.GetInt(GemsKey, 0));
            private set => LocalStore.SetInt(GemsKey, Mathf.Max(0, value));
        }

        /// <summary>카드 소장과 덱 편성. 바꾼 뒤에는 SaveCards().</summary>
        public static Collection Cards => _cards ??= Collection.Parse(LocalStore.GetString(CardsKey, ""));

        public static void SaveCards() => LocalStore.SetString(CardsKey, Cards.Serialize());

        /// <summary>카드 한 장을 산다. 결정이 모자라거나 더 가질 수 없으면 false.</summary>
        public static bool BuyCard(Card card)
        {
            var e = CardShop.Find(card);
            if (e == null || !Cards.CanBuy(card) || Gems < e.Price) return false;
            Gems -= e.Price;
            Cards.Buy(card);
            SaveCards();
            return true;
        }

        /// <summary>카드를 한 단계 강화한다. 결정이 모자라거나 더 못 올리면 false.</summary>
        public static bool UpgradeCard(Card card)
        {
            var e = CardShop.Find(card);
            if (e == null || !Cards.CanUpgrade(card)) return false;
            var price = CardShop.UpgradePrice(e, Cards.Level(card) + 1);
            if (Gems < price) return false;
            Gems -= price;
            Cards.Upgrade(card);
            SaveCards();
            return true;
        }

        public static bool SpendGems(int n)
        {
            if (Gems < n) return false;
            Gems -= n;
            return true;
        }

        /// <summary>모아 둔 무르기 횟수 (출격 준비 창에서 산다).</summary>
        public static int Undos
        {
            get => Mathf.Max(0, LocalStore.GetInt(UndoKey, 0));
            private set => LocalStore.SetInt(UndoKey, Mathf.Max(0, value));
        }

        public static bool BuyUndo()
        {
            if (!SpendGems(BoostShop.UndoPrice)) return false;
            Undos++;
            return true;
        }

        public static bool UseUndo()
        {
            if (Undos <= 0) return false;
            Undos--;
            return true;
        }

        public static int StatOf(Stat s) => Mathf.Max(0, LocalStore.GetInt(StatPrefix + s.ToString().ToLowerInvariant(), 0));

        private static void AddStat(Stat s, int n)
        {
            if (n > 0) LocalStore.SetInt(StatPrefix + s.ToString().ToLowerInvariant(), StatOf(s) + n);
        }

        /// <summary>받은 단계 수 (0 = 첫 단계 진행 중).</summary>
        public static int QuestClaimed(QuestLine q) => Mathf.Max(0, LocalStore.GetInt(QuestPrefix + q.Id, 0));

        public static bool QuestReady(QuestLine q)
        {
            var k = QuestClaimed(q);
            return !q.Finished(k) && StatOf(q.Stat) >= q.Targets[k];
        }

        public static int QuestsReady()
        {
            var n = 0;
            foreach (var q in QuestCatalog.Lines)
                if (QuestReady(q)) n++;
            return n;
        }

        /// <summary>퀘스트 보상을 받고 다음 단계를 연다. 받은 결정 수 (못 받으면 0).</summary>
        public static int ClaimQuest(QuestLine q)
        {
            if (!QuestReady(q)) return 0;
            var k = QuestClaimed(q);
            Gems += q.Rewards[k];
            LocalStore.SetInt(QuestPrefix + q.Id, k + 1);
            return q.Rewards[k];
        }

        /// <summary>받을 수 있는 퀘스트 보상을 모두 받는다. 기록이 여러 단계를 넘었으면 그 단계들도 이어서 받는다.</summary>
        public static int ClaimAllQuests(out int claims)
        {
            var total = 0;
            claims = 0;
            foreach (var q in QuestCatalog.Lines)
                while (QuestReady(q))
                {
                    total += ClaimQuest(q);
                    claims++;
                }
            return total;
        }

        /// <summary>
        /// 판 결과 기록: 별·해금(Record) + 판 기록을 퀘스트 누적에 더하고 결정을 준다. 받은 결정 수.
        /// </summary>
        /// <param name="newMode">이 판으로 다음 모드가 열렸다 (그 모드로 바로 넘어간다)</param>
        public static int RecordBattle(int index, int stars, Game g, out bool firstClear, out bool newMode)
        {
            var mode = Mode;
            firstClear = stars > 0 && StarsOf(index) == 0;
            var wasOpen = ModeUnlocked(mode + 1);
            Record(index, stars);
            newMode = mode + 1 < StageCatalog.Modes && !wasOpen && ModeUnlocked(mode + 1);
            if (newMode) Mode = mode + 1;
            var m = g.StatsOf(Side.Player);
            for (var s = 0; s < (int)Stat.Count; s++) AddStat((Stat)s, m.Counts[s]);
            AddStat(Stat.Games, 1);
            AddStat(Stat.Wins, stars > 0 ? 1 : 0);
            AddStat(Stat.Objectives, g.ObjectivesDone);
            var gems = Rewards.ForBattle(StageCatalog.Get(index), stars, firstClear, g.ObjectivesDone, mode);
            Gems += gems;
            return gems;
        }

        /// <summary>개발 빌드 전용: 결정 지급.</summary>
        public static void AddGemsDebug(int n) => Gems += n;

        /// <summary>개발 빌드 전용: 모든 스테이지 해금.</summary>
        public static void UnlockAll() => Unlocked = StageCatalog.Count;

        /// <summary>개발 빌드 전용: 진행 초기화 (해금·별·모드·튜토리얼·팁 전부).</summary>
        public static void ResetAll()
        {
            LocalStore.Clear();
            for (var m = 0; m < _stars.Length; m++) _stars[m] = null;
            _cards = null;
        }

        private static int[] Load(int mode)
        {
            if (_stars[mode] != null) return _stars[mode];
            var arr = _stars[mode] = new int[StageCatalog.Count];
            var parts = LocalStore.GetString(StarsKey + Suffix(mode), "").Split(',');
            for (var i = 0; i < parts.Length && i < arr.Length; i++)
                if (int.TryParse(parts[i], out var v)) arr[i] = Mathf.Clamp(v, 0, 3);
            return arr;
        }
    }
}
