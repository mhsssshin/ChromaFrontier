using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ChromaFrontier.Client
{
    [Serializable]
    public sealed class SeatInfo
    {
        public string uid = "", name = "", deck = "", skin = "", emote = "";
        public bool ready, left;
        public bool Empty => string.IsNullOrEmpty(uid);
    }

    [Serializable]
    public sealed class RoomInfo
    {
        public string id = "", title = "", host = "", status = "wait", map = "";
        public int size = 1, seed;
        public double created;
        public SeatInfo[] seats = Array.Empty<SeatInfo>();

        public int Filled
        {
            get
            {
                var n = 0;
                foreach (var s in seats)
                    if (!s.Empty) n++;
                return n;
            }
        }
    }

    [Serializable]
    internal sealed class NetMsg
    {
        public string t = "", uid = "", name = "", provider = "", err = "", move = "", localSummary = "", remoteSummary = "";
        public int n;
        public RoomInfo room;
        public RoomInfo[] rooms;
    }

    /// <summary>
    /// 온라인 기능: Firebase 로그인(Google·게스트), 클라우드 저장, PvP 방. WebGL 에서만 동작하고(템플릿의 net.js 가 실제 일을 함),
    /// 에디터·Android 에서는 Available 이 false 라 오프라인으로만 논다.
    /// 클라우드 저장은 net.js 가 알아서 한다: 로그인하면 계정 기록과 이 기기 기록을 맞추고(필요하면 고르게 함),
    /// 그 뒤로 LocalStore 에 쓸 때마다 잠시 뒤 올린다. 기록이 바뀌면 "reload" 가 와서 Progress 를 다시 읽는다.
    /// net.js 의 응답은 매 프레임 Poll() 로 가져온다.
    /// </summary>
    public static class Online
    {
        public const string FirebaseConfig =
            "{\"apiKey\":\"AIzaSyDB4UjsowyhfXa-IiVFw9HsbioqHe_KJpA\",\"authDomain\":\"chroma-frontier.firebaseapp.com\"," +
            "\"databaseURL\":\"https://chroma-frontier-default-rtdb.asia-southeast1.firebasedatabase.app\",\"projectId\":\"chroma-frontier\"," +
            "\"storageBucket\":\"chroma-frontier.firebasestorage.app\",\"messagingSenderId\":\"7878005785\"," +
            "\"appId\":\"1:7878005785:web:e9b09744c5e8cb2bc20688\",\"measurementId\":\"G-NJSX9C95VY\"}";

        private const string OfflineKey = "cf.local.offline"; // 로그인 없이 하기로 했다 (클라우드에 올리지 않는 키)

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int CF_NetAvailable();
        [DllImport("__Internal")] private static extern void CF_NetCall(string name, string a, string b, string c, string d, string e);
        [DllImport("__Internal")] private static extern string CF_NetPoll();
        public static bool Available => CF_NetAvailable() == 1;
        private static void Call(string name, string a = "", string b = "", string c = "", string d = "", string e = "") => CF_NetCall(name, a, b, c, d, e);
        private static string PollRaw() => CF_NetPoll();
#else
        public static bool Available => false;
        private static void Call(string name, string a = "", string b = "", string c = "", string d = "", string e = "") { }
        private static string PollRaw() => null;
#endif

        public static bool Ready { get; private set; }     // SDK 를 불러왔다
        public static bool Failed { get; private set; }    // SDK 를 못 불러왔다 (네트워크 없음 등)
        public static bool AuthKnown { get; private set; } // 로그인 상태를 알았다
        public static bool Synced { get; private set; }    // 로그인 뒤 클라우드 기록 맞추기를 끝냈다
        public static string Uid { get; private set; } = "";
        public static string Name { get; private set; } = "";
        public static string Provider { get; private set; } = ""; // google | guest
        public static string Error { get; private set; } = "";
        public static float ErrorAt { get; private set; } = -99f;
        public static float SavedAt { get; private set; } = -99f;
        public static string ConflictLocal { get; private set; } // 고르기를 기다리는 중이면 요약, 아니면 null
        public static string ConflictRemote { get; private set; }

        public static readonly List<RoomInfo> Rooms = new();
        public static RoomInfo Room { get; private set; }   // 들어가 있는 방 (없으면 null)
        public static int MySeat { get; private set; } = -1;
        public static bool RoomGone { get; private set; }   // 방이 사라졌다 (방장이 나감 등). 읽으면 지운다

        /// <summary>받은 수 (번호, 문자열). PvP 판이 가져간다.</summary>
        public static readonly Queue<KeyValuePair<int, string>> Moves = new();

        /// <summary>클라우드 기록을 이 기기로 받아 Progress 를 다시 읽어야 한다.</summary>
        public static event Action Reloaded;

        public static bool SignedIn => !string.IsNullOrEmpty(Uid);
        public static bool IsGuest => Provider == "guest";

        public static bool OfflineChosen
        {
            get => LocalStore.GetInt(OfflineKey, 0) == 1;
            set => LocalStore.SetInt(OfflineKey, value ? 1 : 0);
        }

        private static bool _started;

        public static void Start()
        {
            if (_started || !Available) return;
            _started = true;
            Call("init", FirebaseConfig);
        }

        public static void Poll()
        {
            if (!Available) return;
            for (var guard = 0; guard < 64; guard++)
            {
                var raw = PollRaw();
                if (string.IsNullOrEmpty(raw)) return;
                NetMsg m;
                try { m = JsonUtility.FromJson<NetMsg>(raw); }
                catch (Exception e)
                {
                    Debug.LogWarning("[Online] " + e.Message);
                    continue;
                }
                Handle(m);
            }
        }

        private static void Handle(NetMsg m)
        {
            switch (m.t)
            {
                case "ready": Ready = true; break;
                case "offline":
                    Failed = true;
                    AuthKnown = true;
                    break;
                case "auth":
                    AuthKnown = true;
                    if (string.IsNullOrEmpty(m.uid))
                    {
                        Uid = Name = Provider = "";
                        Synced = false;
                        Room = null;
                        MySeat = -1;
                    }
                    else
                    {
                        if (m.uid != Uid) Synced = false;
                        Uid = m.uid;
                        Name = m.name;
                        Provider = m.provider;
                        Error = "";
                    }
                    break;
                case "synced":
                    Synced = true;
                    ConflictLocal = ConflictRemote = null;
                    break;
                case "saved": SavedAt = Time.unscaledTime; break;
                case "conflict":
                    ConflictLocal = m.localSummary;
                    ConflictRemote = m.remoteSummary;
                    break;
                case "reload":
                    LocalStore.DropLegacy();
                    Progress.Reload();
                    Reloaded?.Invoke();
                    break;
                case "err":
                    Error = ErrorText(m.err);
                    ErrorAt = Time.unscaledTime;
                    Debug.LogWarning("[Online] " + m.err);
                    break;
                case "rooms":
                    Rooms.Clear();
                    if (m.rooms != null) Rooms.AddRange(m.rooms);
                    break;
                case "room":
                    Room = m.room;
                    MySeat = m.n;
                    break;
                case "roomGone":
                    if (Room != null) RoomGone = true;
                    Room = null;
                    MySeat = -1;
                    Moves.Clear();
                    break;
                case "move":
                    Moves.Enqueue(new KeyValuePair<int, string>(m.n, m.move));
                    break;
            }
        }

        private static string ErrorText(string code) => code switch
        {
            "auth/popup-blocked" => "팝업이 막혔어요. 브라우저에서 팝업을 허용해 주세요.",
            "auth/popup-closed-by-user" or "auth/cancelled-popup-request" => "로그인 창이 닫혔어요.",
            "auth/operation-not-allowed" => "이 로그인 방식이 Firebase 에서 꺼져 있어요.",
            "auth/unauthorized-domain" => "이 주소가 Firebase 승인 도메인에 없어요.",
            "auth/credential-already-in-use" => "그 Google 계정은 이미 다른 기록에 연결돼 있어요. 로그아웃 후 Google 로 로그인하세요.",
            "auth/network-request-failed" => "네트워크에 연결할 수 없어요.",
            "PERMISSION_DENIED" or "permission_denied" => "DB 권한이 없어요 (보안 규칙 확인 필요).",
            "room-full" => "자리가 없거나 이미 시작한 방이에요.",
            "not-ready" => "모두 자리에 앉고 준비해야 시작할 수 있어요.",
            _ => code != null && code.Contains("ermission") ? "DB 권한이 없어요 (보안 규칙 확인 필요)." : "온라인 오류: " + code,
        };

        public static bool TakeRoomGone()
        {
            var g = RoomGone;
            RoomGone = false;
            return g;
        }

        // ---------------------------------------------------------------- 요청

        /// <summary>
        /// 팝업이 필요한 동작(google, link)은 버튼을 누르는 순간 Arm 으로 걸어 두고, 클릭이 끝나면 Run 을 부른다.
        /// 브라우저는 사용자 입력 이벤트 안에서만 팝업을 허락하는데, Unity 는 한 프레임 늦게 클릭을 받기 때문이다. 실제로는 한 번만 실행된다.
        /// </summary>
        public static void Arm(string action) => Call("arm", action);
        public static void Run(string action) => Call("run", action);

        public static void SignOut() => Call("signOut");
        public static void Resolve(bool keepRemote) => Call("resolve", keepRemote ? "remote" : "local");
        public static void SaveNow() => Call("saveNow");

        public static void WatchRooms(bool on) => Call("watchRooms", on ? "1" : "");
        public static void CreateRoom(string title, int size, string map, string deck, string skin) => Call("createRoom", title, size.ToString(), map, deck, skin);
        public static void JoinRoom(string id, string deck, string skin) => Call("joinRoom", id, deck, skin);
        public static void MoveSeat(int seat) => CallInt("moveSeat", seat);
        public static void SetReady(bool on) => CallBool("setReady", on);
        public static void SetMap(string map) => Call("setMap", map);
        public static void SetDeck(string deck) => Call("setDeck", deck);
        public static void StartGame() => Call("start");
        public static void Finish() => Call("finish");
        public static void SendEmote(int index) => Call("emote", index.ToString());

        public static void LeaveRoom()
        {
            Call("leaveRoom");
            Room = null;
            MySeat = -1;
            Moves.Clear();
        }

        public static void SendMove(int n, string move, bool onlyIfEmpty) => Call("sendMove", n.ToString(), move, onlyIfEmpty ? "1" : "");

        // net.js 쪽은 문자열을 받으므로 숫자·참거짓은 그쪽에서 바꾼다
        private static void CallInt(string name, int a) => Call(name, a.ToString());
        private static void CallBool(string name, bool on) => Call(name, on ? "1" : "");
    }
}
