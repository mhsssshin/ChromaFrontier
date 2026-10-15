using System;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 오프라인 진행 저장 (LocalStore: WebGL 은 브라우저 localStorage, 그 밖은 PlayerPrefs).
    /// 해금된 스테이지 수, 스테이지별 최고 별, 난이도 보정, 튜토리얼 완료·진행 레슨, 본 팁.
    /// 값은 바뀌는 즉시 저장된다.
    /// </summary>
    public static class Progress
    {
        private const string UnlockedKey = "cf.unlocked", StarsKey = "cf.stars", OffsetKey = "cf.levelOffset";
        private const string TutorialKey = "cf.tutorial", LessonKey = "cf.tutorialLesson", TipPrefix = "cf.tip.";

        private static int[] _stars;

        /// <summary>플레이할 수 있는 스테이지 수 (1 이면 첫 스테이지만).</summary>
        public static int Unlocked
        {
            get => Mathf.Clamp(LocalStore.GetInt(UnlockedKey, 1), 1, StageCatalog.Count);
            private set => LocalStore.SetInt(UnlockedKey, value);
        }

        /// <summary>대기실 난이도 보정: -1 쉬움, 0 보통, +1 어려움.</summary>
        public static int LevelOffset
        {
            get => Mathf.Clamp(LocalStore.GetInt(OffsetKey, -1), -1, 1);
            set => LocalStore.SetInt(OffsetKey, value);
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

        public static int StarsOf(int index)
        {
            Load();
            return _stars[index];
        }

        public static int TotalStars()
        {
            Load();
            var n = 0;
            foreach (var s in _stars) n += s;
            return n;
        }

        /// <summary>판 결과 기록: 최고 별 갱신, 이기면 다음 스테이지 해금.</summary>
        public static void Record(int index, int stars)
        {
            Load();
            _stars[index] = Math.Max(_stars[index], stars);
            if (stars > 0 && index + 1 >= Unlocked) Unlocked = Math.Min(StageCatalog.Count, index + 2);
            LocalStore.SetString(StarsKey, string.Join(",", _stars));
        }

        /// <summary>개발 빌드 전용: 모든 스테이지 해금.</summary>
        public static void UnlockAll() => Unlocked = StageCatalog.Count;

        /// <summary>개발 빌드 전용: 진행 초기화 (해금·별·난이도·튜토리얼·팁 전부).</summary>
        public static void ResetAll()
        {
            LocalStore.Clear();
            _stars = null;
        }

        private static void Load()
        {
            if (_stars != null) return;
            _stars = new int[StageCatalog.Count];
            var parts = LocalStore.GetString(StarsKey, "").Split(',');
            for (var i = 0; i < parts.Length && i < _stars.Length; i++)
                if (int.TryParse(parts[i], out var v)) _stars[i] = Mathf.Clamp(v, 0, 3);
        }
    }
}
