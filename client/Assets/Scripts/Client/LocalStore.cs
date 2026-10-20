using System.Runtime.InteropServices;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 오프라인 저장소. WebGL 에서는 브라우저 localStorage (Plugins/WebGL/LocalStorage.jslib), 그 밖(에디터·Android)에서는 PlayerPrefs.
    /// 키는 모두 "cf." 로 시작한다. 예전에 PlayerPrefs 에 남은 값은 처음 읽을 때 localStorage 로 옮긴다.
    /// </summary>
    public static class LocalStore
    {
        public const string Prefix = "cf.";

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern string CF_StorageGet(string key);
        [DllImport("__Internal")] private static extern void CF_StorageSet(string key, string value);
        [DllImport("__Internal")] private static extern void CF_StorageClear(string prefix);

        private static string Read(string key) => CF_StorageGet(key);
        private static void Write(string key, string value) => CF_StorageSet(key, value);
        private static void Flush() { }
#else
        private static string Read(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
        private static void Write(string key, string value) => PlayerPrefs.SetString(key, value);
        private static void Flush() => PlayerPrefs.Save();
#endif

        public static string GetString(string key, string fallback = "")
        {
            var v = Read(key);
            if (v != null) return v;
#if UNITY_WEBGL && !UNITY_EDITOR
            // 예전 버전이 PlayerPrefs 에 문자열로 남긴 값 옮기기
            if (PlayerPrefs.HasKey(key))
            {
                var old = PlayerPrefs.GetString(key, null);
                if (!string.IsNullOrEmpty(old))
                {
                    Write(key, old);
                    return old;
                }
            }
#endif
            return fallback;
        }

        public static int GetInt(string key, int fallback = 0)
        {
            var v = Read(key);
            if (v != null && int.TryParse(v, out var n)) return n;
#if UNITY_WEBGL && !UNITY_EDITOR
            // 예전 버전이 PlayerPrefs 에 정수로 남긴 값 옮기기
            if (PlayerPrefs.HasKey(key))
            {
                var old = PlayerPrefs.GetInt(key, fallback);
                Write(key, old.ToString());
                return old;
            }
#else
            // 에디터·Android 에서 예전 PlayerPrefs.SetInt 로 남긴 값 (문자열로 읽으면 비어 있다)
            if (PlayerPrefs.HasKey(key)) return PlayerPrefs.GetInt(key, fallback);
#endif
            return fallback;
        }

        public static void SetString(string key, string value)
        {
            Write(key, value);
            Flush();
        }

        public static void SetInt(string key, int value) => SetString(key, value.ToString());

        /// <summary>
        /// 저장값이 통째로 바뀌었을 때(클라우드 기록을 받음, 로그아웃) 예전 PlayerPrefs 값을 버린다.
        /// 남겨 두면 지운 키를 처음 읽을 때 옛 값이 localStorage 로 되살아난다 (WebGL 만).
        /// </summary>
        public static void DropLegacy()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
#endif
        }

        /// <summary>이 게임의 저장값을 모두 지운다.</summary>
        public static void Clear()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CF_StorageClear(Prefix);
#endif
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
