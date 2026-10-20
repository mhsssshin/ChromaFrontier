using System;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 오프라인 진행 저장 (LocalStore: WebGL 은 브라우저 localStorage, 그 밖은 PlayerPrefs).
    /// 해금된 스테이지 수, 스테이지별 최고 별, 튜토리얼 완료·진행 레슨, 본 팁, 재화(결정), 카드 소장·덱, 퀘스트 누적 기록·받은 단계,
    /// 첨탑 층·층별 별, 별의 길 받은 단계, 보드 테마.
    /// 값은 바뀌는 즉시 저장된다.
    /// </summary>
    public static class Progress
    {
        // 해금·별은 모드마다 따로: 쉬움은 예전 키 그대로(cf.unlocked), 보통·어려움은 뒤에 .1 / .2
        private const string UnlockedKey = "cf.unlocked", StarsKey = "cf.stars", ModeKey = "cf.mode", TowerKey = "cf.tower";
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

        /// <summary>한 모드의 스테이지 별 합계.</summary>
        public static int ModeStars(int mode)
        {
            var n = 0;
            foreach (var s in Load(mode)) n += s;
            return n;
        }

        /// <summary>모든 모드의 스테이지 별.</summary>
        public static int StageStars()
        {
            var n = 0;
            for (var m = 0; m < StageCatalog.Modes; m++) n += ModeStars(m);
            return n;
        }

        /// <summary>퀘스트 별: 받은 단계마다 1개.</summary>
        public static int QuestStars()
        {
            var n = 0;
            foreach (var q in QuestCatalog.Lines) n += Mathf.Min(QuestClaimed(q), q.Targets.Length);
            return n;
        }

        /// <summary>별의 길에 쓰는 전체 별: 스테이지(모든 모드) + 첨탑 + 퀘스트.</summary>
        public static int AllStars() => StageStars() + TowerStars() + QuestStars();

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
        private static int[] _floorStars;

        /// <summary>재화 (크로마 결정). 판을 끝내거나 퀘스트를 깨면 받고, 스킬 카드를 사는 데 쓴다.</summary>
        public static int Gems
        {
            get => Mathf.Max(0, LocalStore.GetInt(GemsKey, 0));
            private set => LocalStore.SetInt(GemsKey, Mathf.Max(0, value));
        }

        /// <summary>카드 소장과 덱 편성. 바꾼 뒤에는 SaveCards().</summary>
        public static Collection Cards => _cards ??= Collection.Parse(LocalStore.GetString(CardsKey, ""), StarRoad.SlotsFor(RoadClaimed));

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

        /// <summary>크로마 첨탑에서 깬 층 수 (다음에 열린 층 = 이 값, 0부터).</summary>
        public static int TowerCleared
        {
            get => Mathf.Clamp(LocalStore.GetInt(TowerKey, 0), 0, PuzzleCatalog.Count);
            private set => LocalStore.SetInt(TowerKey, value);
        }

        public static bool FloorOpen(int floor) => floor >= 0 && floor < PuzzleCatalog.Count && floor <= TowerCleared;

        public static bool FloorCleared(int floor) => floor < TowerCleared;

        // 층별 별 (비트: 1 깨기, 2 무르기 없이, 4 힌트 없이)
        public const int FloorClear = 1, FloorNoUndo = 2, FloorNoHint = 4;
        private const string FloorStarsKey = "cf.tower.stars";

        /// <summary>층에서 받은 별 비트. 별이 생기기 전에 깬 층은 '깨기' 별 하나.</summary>
        public static int FloorStarBits(int floor)
        {
            if (_floorStars == null)
            {
                _floorStars = new int[PuzzleCatalog.Count];
                var parts = LocalStore.GetString(FloorStarsKey, "").Split(',');
                for (var i = 0; i < parts.Length && i < _floorStars.Length; i++)
                    if (int.TryParse(parts[i], out var v)) _floorStars[i] = v & 7;
            }
            var bits = _floorStars[floor];
            return FloorCleared(floor) ? bits | FloorClear : bits;
        }

        public static int FloorStars(int floor)
        {
            var b = FloorStarBits(floor);
            return (b & 1) + ((b >> 1) & 1) + ((b >> 2) & 1);
        }

        public static int TowerStars()
        {
            var n = 0;
            for (var i = 0; i < PuzzleCatalog.Count; i++) n += FloorStars(i);
            return n;
        }

        /// <summary>
        /// 첨탑 한 판 기록: 처음 깬 층이면 다음 층을 열고 결정을 준다. 깼으면 별(깨기·무르기 없이·힌트 없이)을 더한다. 받은 결정 수.
        /// </summary>
        /// <param name="newStars">이번에 새로 받은 별 비트</param>
        public static int RecordFloor(int floor, bool solved, bool noUndo, bool noHint, out bool firstClear, out int newStars)
        {
            firstClear = solved && floor == TowerCleared;
            newStars = 0;
            if (!solved) return 0;
            var before = FloorStarBits(floor);
            var bits = before | FloorClear | (noUndo ? FloorNoUndo : 0) | (noHint ? FloorNoHint : 0);
            newStars = bits & ~before;
            _floorStars[floor] = bits;
            LocalStore.SetString(FloorStarsKey, string.Join(",", _floorStars));
            if (!firstClear) return 0;
            TowerCleared = floor + 1;
            var gems = Rewards.ForFloor(floor);
            Gems += gems;
            return gems;
        }

        // ---------------------------------------------------------------- 별의 길·보드 테마

        private const string RoadKey = "cf.road", ThemesKey = "cf.themes", ThemeKey = "cf.theme";

        /// <summary>별의 길 단계를 받았는가 (테마 해금 단계는 별만 모으면 받은 것으로 친다).</summary>
        public static bool RoadClaimed(int step)
        {
            var s = StarRoad.Steps[step];
            if (!s.Claimable) return AllStars() >= s.Stars;
            return ("," + LocalStore.GetString(RoadKey, "") + ",").Contains("," + step + ",");
        }

        public static bool RoadReady(int step) => StarRoad.Steps[step].Claimable && !RoadClaimed(step) && AllStars() >= StarRoad.Steps[step].Stars;

        public static int RoadReadyCount()
        {
            var n = 0;
            for (var i = 0; i < StarRoad.Steps.Length; i++)
                if (RoadReady(i)) n++;
            return n;
        }

        /// <summary>별의 길 보상을 받는다. 못 받으면 false.</summary>
        public static bool ClaimRoad(int step)
        {
            if (!RoadReady(step)) return false;
            var s = StarRoad.Steps[step];
            var list = LocalStore.GetString(RoadKey, "");
            LocalStore.SetString(RoadKey, list.Length == 0 ? step.ToString() : list + "," + step);
            switch (s.Kind)
            {
                case RoadReward.Gems: Gems += s.Amount; break;
                case RoadReward.Undos: Undos += s.Amount; break;
                case RoadReward.Slot: Cards.Slots = StarRoad.SlotsFor(RoadClaimed); break;
                case RoadReward.ThemeGift: GiveTheme(s.Theme); break;
            }
            return true;
        }

        /// <summary>받을 수 있는 별의 길 보상을 모두 받는다. 받은 개수.</summary>
        public static int ClaimAllRoad()
        {
            var n = 0;
            for (var i = 0; i < StarRoad.Steps.Length; i++)
                if (ClaimRoad(i)) n++;
            return n;
        }

        public static bool ThemeOwned(string id) => id == "" || ("," + LocalStore.GetString(ThemesKey, "") + ",").Contains("," + id + ",");

        /// <summary>상점에서 살 수 있는가: 별의 길에서 열렸고, 값이 있고, 아직 없다.</summary>
        public static bool ThemeForSale(ThemeDef t)
        {
            var step = ThemeShop.StepOf(t.Id);
            return t.Price > 0 && !ThemeOwned(t.Id) && step != null && AllStars() >= step.Stars;
        }

        private static void GiveTheme(string id)
        {
            if (ThemeOwned(id)) return;
            var list = LocalStore.GetString(ThemesKey, "");
            LocalStore.SetString(ThemesKey, list.Length == 0 ? id : list + "," + id);
        }

        public static bool BuyTheme(ThemeDef t)
        {
            if (!ThemeForSale(t) || !SpendGems(t.Price)) return false;
            GiveTheme(t.Id);
            return true;
        }

        /// <summary>지금 쓰는 보드 테마 (가진 것만, 아니면 기본).</summary>
        public static string Theme
        {
            get
            {
                var id = LocalStore.GetString(ThemeKey, "");
                return ThemeShop.Find(id) != null && ThemeOwned(id) ? id : "";
            }
            set
            {
                if (ThemeOwned(value)) LocalStore.SetString(ThemeKey, value);
            }
        }

        /// <summary>개발 빌드 전용: 결정 지급.</summary>
        public static void AddGemsDebug(int n) => Gems += n;

        /// <summary>개발 빌드 전용: 모든 스테이지 해금.</summary>
        public static void UnlockAll()
        {
            Unlocked = StageCatalog.Count;
            TowerCleared = PuzzleCatalog.Count - 1;
        }

        /// <summary>개발 빌드 전용: 진행 초기화 (해금·별·모드·튜토리얼·팁 전부).</summary>
        public static void ResetAll()
        {
            LocalStore.Clear();
            Reload();
        }

        /// <summary>저장소가 통째로 바뀌었을 때(클라우드 기록을 받음, 로그아웃) 기억해 둔 값을 버리고 다시 읽게 한다.</summary>
        public static void Reload()
        {
            for (var m = 0; m < _stars.Length; m++) _stars[m] = null;
            _cards = null;
            _floorStars = null;
        }

        // ---------------------------------------------------------------- PvP 전적

        private const string PvpWinKey = "cf.pvp.win", PvpDrawKey = "cf.pvp.draw", PvpLoseKey = "cf.pvp.lose";

        public static int PvpWins => LocalStore.GetInt(PvpWinKey, 0);
        public static int PvpDraws => LocalStore.GetInt(PvpDrawKey, 0);
        public static int PvpLosses => LocalStore.GetInt(PvpLoseKey, 0);

        /// <summary>대전 결과: 전적을 남기고 결정을 준다. 받은 결정 수.</summary>
        public static int RecordPvp(Side winner, Side team)
        {
            var key = winner == team ? PvpWinKey : winner == Side.None ? PvpDrawKey : PvpLoseKey;
            LocalStore.SetInt(key, LocalStore.GetInt(key, 0) + 1);
            var gems = PvpCatalog.Reward(winner, team);
            Gems += gems;
            return gems;
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
