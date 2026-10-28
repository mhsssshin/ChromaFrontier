using System;
using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>봇이 이모티콘을 보낼 만한 순간.</summary>
    public enum BotMood : byte { Start, Good, Hurt, Win, Lose }

    /// <summary>
    /// 대전 봇 한 명. 미리 방을 만들어 두거나(PvpBotRooms) 빠른 대전에서 사람이 안 모이면 들어와 사람처럼 둔다: 실력(AiLevel)과 성향(AiPersona)이 다르고,
    /// 생각하는 시간이 들쭉날쭉하고(가끔 오래 망설임), 자기 버릇대로 이모티콘을 보낸다. 덱과 스킨도 각자 것.
    /// </summary>
    public sealed class PvpBot
    {
        public string Id, Uid, Name;     // Uid: Firebase 계정 uid 와 같은 모양 (봇인지 티 나지 않게)
        public AiLevel Level;
        public AiPersona Persona = AiPersona.Default;
        public float ThinkMin, ThinkMax;  // 보통 생각 시간(초)
        public float Hesitate;            // 오래 망설일 확률
        public float Chatty;              // 이모티콘을 보낼 확률 (0~1)
        public int[] Good, Hurt, Win, Lose, Hello; // 이모티콘 번호 (EmotePacks 전체 번호)
        public string Deck, Skin;         // 스킬 덱(CardShop 코드), 자리 스킨 "재질:색:유닛세트"
    }

    /// <summary>
    /// 대전 봇 10명. 서버가 없으므로 봇의 수는 방에 있는 사람 기기 하나(나가지 않은 사람 중 가장 앞 자리)가 대신 계산해
    /// 보통 수처럼 보낸다. 다른 기기에는 사람의 수와 똑같이 보인다. 봇의 uid 도 사람 계정 uid 와 같은 모양이라
    /// 방·DB 어디에도 봇이라는 표시가 없다 (봇인지는 이 목록으로만 안다).
    /// </summary>
    public static class PvpBots
    {
        // 이모티콘 번호 (EmotePacks: 기본 0~7, 킹받는 8~15, 귀요미 16~23, 멘붕 24~31)
        private const int Like = 0, Angry = 1, Gasp = 2, Sob = 3, Lol = 4, Nice = 5, Hmm = 6, Gg = 7;
        private const int Tongue = 8, Whatever = 9, Tease = 10, Smug = 11, Nope = 12, Yawn = 13, Clap = 14, ReallyThere = 15;
        private const int Shy = 16, Sparkle = 17, Kiss = 18, Sleepy = 19, Flower = 20, Peace = 21, Love = 22, Fighting = 23;
        private const int Meltdown = 24, SoulOut = 25, Tears = 26, Boom = 27, Zen = 28, Blank = 29, Boing = 30, Flop = 31;

        public static readonly PvpBot[] All =
        {
            new()
            {
                Id = "potato", Uid = "njsul7ClrNCfiJ0QAdOm0Nlqxvx6", Name = "새벽감자", Level = AiLevel.Normal, ThinkMin = 3f, ThinkMax = 8f, Hesitate = 0.15f, Chatty = 0.35f,
                Good = new[] { Sparkle, Peace, Like }, Hurt = new[] { Gasp, Sob }, Win = new[] { Gg, Flower }, Lose = new[] { Gg, Sob }, Hello = new[] { Fighting, Like },
                Deck = "tr_red,tr_blue,tr_yellow,bleach,mine,surge,beacon,split", Skin = "grass:sky:",
            },
            new()
            {
                Id = "fist", Uid = "Cl2ANdaWrqNNdFn8517N0CrFMfDp", Name = "불꽃주먹99", Level = AiLevel.Hard, Persona = new AiPersona { Id = "fist", StealWeight = 0.6f },
                ThinkMin = 1.5f, ThinkMax = 4.5f, Hesitate = 0.05f, Chatty = 0.5f,
                Good = new[] { Lol, Tongue, Smug, Tease }, Hurt = new[] { Angry, Nope }, Win = new[] { Tongue, Gg, Smug }, Lose = new[] { Angry, Gg }, Hello = new[] { Tease },
                Deck = "tr_red,tr_red,surge,surge,siphon,quake,mine,bleach,beacon", Skin = "stone:orange:halloween",
            },
            new()
            {
                Id = "water", Uid = "7iWJJwJxvZtLzPfKPSYoaTsXyKOV", Name = "물멍중", Level = AiLevel.Easy, ThinkMin = 4f, ThinkMax = 10f, Hesitate = 0.25f, Chatty = 0.15f,
                Good = new[] { Like, Sparkle }, Hurt = new[] { Hmm, Zen }, Win = new[] { Gg }, Lose = new[] { Gg, Zen }, Hello = new[] { Sleepy },
                Deck = "tr_blue,tr_blue,tr_yellow,bleach,mine,surge", Skin = "water:sky:",
            },
            new()
            {
                Id = "kim", Uid = "GVfwavHE5f5g8gToI5DoyBzTLhWI", Name = "chroma_kim", Level = AiLevel.Normal, ThinkMin = 2.5f, ThinkMax = 6f, Hesitate = 0.1f, Chatty = 0.25f,
                Good = new[] { Like, Lol }, Hurt = new[] { Gasp, Hmm }, Win = new[] { Gg }, Lose = new[] { Gg, Nice }, Hello = new[] { Like },
                Deck = "tr_red,tr_blue,tr_yellow,bleach,mine,surge,warp,lock,barrier", Skin = "plain:navy:",
            },
            new()
            {
                Id = "bolt", Uid = "8TA2HQZu1ayV3ukzvvqYiR1n8wOW", Name = "번개는내꺼", Level = AiLevel.Normal, Persona = new AiPersona { Id = "bolt", StealWeight = 0.4f, MineWeight = 1.3f },
                ThinkMin = 2f, ThinkMax = 5f, Hesitate = 0.08f, Chatty = 0.45f,
                Good = new[] { Lol, Smug, Whatever }, Hurt = new[] { Angry, ReallyThere, Gasp }, Win = new[] { Smug, Gg }, Lose = new[] { Gg, Angry }, Hello = new[] { Like, Tease },
                Deck = "tr_yellow,tr_yellow,mine,mine,surge,bleach,siphon,quake", Skin = "crystal:purple:",
            },
            new()
            {
                Id = "panda", Uid = "n928JAWyJ7Zv3cHTlCyDqGiEMm6r", Name = "느긋한판다", Level = AiLevel.Easy, ThinkMin = 5f, ThinkMax = 12f, Hesitate = 0.3f, Chatty = 0.3f,
                Good = new[] { Shy, Flower, Peace }, Hurt = new[] { Sob, Sleepy }, Win = new[] { Love, Gg }, Lose = new[] { Gg, Tears }, Hello = new[] { Flower, Kiss },
                Deck = "tr_red,tr_blue,bleach,mine,surge,barrier", Skin = "wood:pink:",
            },
            new()
            {
                Id = "rich", Uid = "YN37Wo0tRwzZRclQI0jYPN3G8OUH", Name = "결정부자", Level = AiLevel.Hard, Persona = new AiPersona { Id = "rich", ShieldWeight = 0.3f, LockWeight = 1.8f, BleachWeight = 1.4f },
                ThinkMin = 3f, ThinkMax = 7f, Hesitate = 0.12f, Chatty = 0.1f,
                Good = new[] { Hmm, Like }, Hurt = new[] { Hmm }, Win = new[] { Gg }, Lose = new[] { Gg, Nice }, Hello = new[] { Like },
                Deck = "lock,lock,bleach,bleach,barrier,barrier,warp,tr_blue,mine", Skin = "marble:white:jeheon",
            },
            new()
            {
                Id = "bunny", Uid = "6g3iApYGT6JNFRctN5BzvhjzAGHQ", Name = "하양토끼", Level = AiLevel.Normal, ThinkMin = 3f, ThinkMax = 7f, Hesitate = 0.15f, Chatty = 0.35f,
                Good = new[] { Sparkle, Kiss, Peace }, Hurt = new[] { Tears, Gasp }, Win = new[] { Love, Peace }, Lose = new[] { Gg, Sob }, Hello = new[] { Shy, Fighting },
                Deck = "tr_red,tr_blue,tr_yellow,split,split,surge,beacon,bleach", Skin = "grass:white:",
            },
            new()
            {
                Id = "uncle", Uid = "qws30Ile0c3lTio4M8rOFLj4Vqvl", Name = "삼촌왔다", Level = AiLevel.Easy, ThinkMin = 2f, ThinkMax = 6f, Hesitate = 0.2f, Chatty = 0.45f,
                Good = new[] { Boom, Lol, Boing }, Hurt = new[] { Meltdown, SoulOut, Flop }, Win = new[] { Lol, Gg }, Lose = new[] { Flop, Gg }, Hello = new[] { Boing, Like },
                Deck = "tr_red,tr_yellow,mine,surge,beacon,quake,bleach", Skin = "stone:black:",
            },
            new()
            {
                Id = "zero", Uid = "rhcdMAoKAWftycLvcLdwr5U2D7eg", Name = "zeroSum", Level = AiLevel.Hard, Persona = new AiPersona { Id = "zero", CatalystSteal = 4f, LoneUnitPenalty = 1.5f, WarpBonus = 1f },
                ThinkMin = 2f, ThinkMax = 5f, Hesitate = 0.06f, Chatty = 0.06f,
                Good = new[] { Hmm }, Hurt = new[] { Hmm }, Win = new[] { Gg }, Lose = new[] { Gg }, Hello = new[] { Like },
                Deck = "warp,warp,siphon,siphon,tr_red,tr_blue,tr_yellow,quake,bleach", Skin = "crystal:navy:",
            },
        };

        public static bool IsBot(string uid) => Find(uid) != null;

        public static PvpBot Find(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            foreach (var b in All)
                if (b.Uid == uid) return b;
            return null;
        }

        /// <summary>봇 uid 목록 (쉼표). net.js 가 방을 정리할 때 사람이 남았는지 본다.</summary>
        public static string UidList()
        {
            var list = new string[All.Length];
            for (var i = 0; i < All.Length; i++) list[i] = All[i].Uid;
            return string.Join(",", list);
        }

        /// <summary>
        /// 이번 수까지 생각하는 시간(초). 성격마다 범위가 다르고, 첫 차례·손에 스킬이 많을 때 조금 더 걸리고, 가끔 오래 망설인다.
        /// 스킬을 쓴 뒤 이어 두는 수는 짧다. 차례 제한보다 넉넉히 짧게 자른다.
        /// </summary>
        public static float ThinkSeconds(Game g, PvpBot bot, Rng rng)
        {
            float R() => rng.Next(1000) / 1000f;
            if (g.SkillUsed) return 0.8f + R() * 1.8f;
            var t = bot.ThinkMin + (bot.ThinkMax - bot.ThinkMin) * R() * R() * 1.4f; // 짧은 쪽에 몰리고 가끔 길다
            if (g.TurnsTaken < g.Seats) t += 1.5f + R() * 2f;                         // 첫 차례: 손패를 살펴본다
            foreach (var c in g.DeckOf(g.Current).Hand)
                if (!c.IsUnit) t += 0.3f;
            if (g.ExtraTurnNow) t *= 0.6f;
            if (R() < bot.Hesitate) t += 4f + R() * 9f;
            return Math.Min(t, PvpCatalog.TurnSeconds - 9f);
        }

        /// <summary>봇이 둘 수 (지금 차례 자리로). 실력·성향대로 AiBrain 이 고른다 — 숨은 지뢰는 보지 않는다.</summary>
        public static PvpMove ChooseMove(Game g, PvpBot bot, Rng rng)
        {
            var m = AiBrain.Choose(g, rng, bot.Level, bot.Persona);
            var seat = g.CurrentSeat;
            if (m.Mulligan) return PvpMove.Pass(seat);
            return m.IsSkill ? PvpMove.Cast(seat, m.HandIndex, m.At, m.Target2) : PvpMove.Play(seat, m.HandIndex, m.At, m.Dir);
        }

        /// <summary>그 순간 보낼 이모티콘 번호, 안 보내면 -1. 판 끝 인사는 자주, 나머지는 성격(Chatty)만큼.</summary>
        public static int Reaction(PvpBot bot, BotMood mood, Rng rng)
        {
            var pool = mood switch
            {
                BotMood.Start => bot.Hello,
                BotMood.Good => bot.Good,
                BotMood.Hurt => bot.Hurt,
                BotMood.Win => bot.Win,
                _ => bot.Lose,
            };
            var chance = mood switch
            {
                BotMood.Win or BotMood.Lose => 0.45f + bot.Chatty,
                BotMood.Start => bot.Chatty * 0.6f,
                _ => bot.Chatty,
            };
            if (pool == null || pool.Length == 0 || rng.Next(1000) >= chance * 1000f) return -1;
            return pool[rng.Next(pool.Length)];
        }
    }

    /// <summary>봇이 만든 대기 방 하나. 자리마다 봇(null = 빈 자리)과 들어온·준비한 시각.</summary>
    public sealed class BotRoom
    {
        public string Id, Title, MapId, Password = ""; // Password 가 비어 있으면 공개방
        public int Size;
        public bool Quick;
        public long Created, EndsAt;                    // 만든 시각, 목록에서 사라지는 시각 (다 차서 시작하거나 방장이 나감), ms
        public PvpBot[] Seats;
        public long[] JoinAt;                           // 자리마다 들어온 시각
        public long[] ReadyAt;                          // 자리마다 준비한 시각 (방장은 만든 시각)
        public PvpBot Host => Seats[0];
        public bool Locked => Password != "";

        /// <summary>그 시각에 자리 i 에 봇이 앉아 있는가.</summary>
        public bool Seated(int i, long now) => Seats[i] != null && JoinAt[i] <= now;
    }

    /// <summary>
    /// 봇이 미리 만들어 두는 방. 서버가 없으므로 모든 기기가 시각만으로 같은 일정을 계산한다: 1분마다 쉬고 있는 봇이
    /// 방을 만들고(가끔은 안 만듦), 다른 봇이 하나둘 들어와 다 차면 시작해서 목록에서 사라진다. 방장이 기다리다 나가기도 하고,
    /// 비번방은 친구(봇)가 오기만 기다린다. 방에 있는 동안과 판을 하는 동안 그 봇은 다른 방에 나타나지 않는다.
    /// 사람이 이 방에 들어가면 같은 방 번호로 실제 방이 만들어진다 (net.js joinRoom).
    /// </summary>
    public static class PvpBotRooms
    {
        public const long Minute = 60_000;
        private const int Lookback = 45; // 몇 분 전 일정부터 계산하나 (봇이 판을 마치고 돌아오는 시간까지)
        private const string PushChars = "-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz";

        private static readonly string[] OpenTitles =
        {
            "{0}의 방", "{0}의 방", "{0}의 방", "한판 ㄱㄱ", "초보만 와요", "매너겜 해요", "아무나 오세요", "심심하다",
            "빠르게 한판", "물덱 연습중", "고수 환영", "즐겜해요", "ㄱㄱㄱ", "같이 해요~", "지는 사람 GG", "번개덱 상대 구함",
        };
        private static readonly string[] TeamTitles = { "2:2 팀전 ㄱ", "팀전 하실 분", "2대2 즐겜", "{0}의 방" };
        private static readonly string[] LockedTitles = { "친구만", "{0} 친구방", "들어오지 마세요", "연습방", "약속방" };

        /// <summary>지금(now, ms) 열려 있는 봇 방. 최근 것이 앞.</summary>
        public static List<BotRoom> Open(long now)
        {
            var list = new List<BotRoom>();
            foreach (var (room, _) in Schedule(now))
                if (room.Created <= now && now < room.EndsAt) list.Add(room);
            list.Sort((a, b) => b.Created.CompareTo(a.Created));
            return list;
        }

        /// <summary>지금 방에 있거나 판을 하고 있는 봇의 uid (빠른 대전에 부를 때 뺀다).</summary>
        public static HashSet<string> Busy(long now)
        {
            var set = new HashSet<string>();
            foreach (var (room, until) in Schedule(now))
                for (var i = 0; i < room.Seats.Length; i++)
                    if (room.Seated(i, now) && now < until) set.Add(room.Seats[i].Uid);
            return set;
        }

        private static List<(BotRoom room, long busyUntil)> Schedule(long now)
        {
            var result = new List<(BotRoom, long)>();
            var bots = PvpBots.All;
            var busyUntil = new long[bots.Length];
            var nowBucket = now / Minute;
            for (var b = nowBucket - Lookback; b <= nowBucket; b++)
            {
              for (var k = 0; k < 2; k++) // 한 분에 방이 둘 생기기도 한다
              {
                var rng = new Rng(unchecked((int)(b * 2654435761L) ^ 0x2F6B3A1D ^ (k * 0x45D9F3B)));
                if (rng.Next(100) >= (k == 0 ? 85 : 45)) continue; // 이번 분에는 아무도 방을 안 만듦
                var created = b * Minute + rng.Next((int)Minute);
                var host = PickFree(busyUntil, created, rng, null);
                if (host < 0) continue;

                var size = rng.Next(100) < 25 ? 2 : 1;
                var locked = rng.Next(100) < 15;
                var quick = !locked && rng.Next(100) < 25;
                var maps = PvpCatalog.MapsFor(size);
                var room = new BotRoom
                {
                    Id = PushId(created, rng),
                    Size = size,
                    Quick = quick,
                    MapId = maps[rng.Next(maps.Length)].Id,
                    Created = created,
                    Seats = new PvpBot[size * 2],
                    JoinAt = new long[size * 2],
                    ReadyAt = new long[size * 2],
                };
                var hostBot = bots[host];
                string pattern;
                if (locked) pattern = LockedTitles[rng.Next(LockedTitles.Length)];
                else if (size == 2 && rng.Next(2) == 0) pattern = TeamTitles[rng.Next(TeamTitles.Length)];
                else pattern = OpenTitles[rng.Next(OpenTitles.Length)];
                room.Title = quick ? hostBot.Name + "의 빠른 대전" : string.Format(pattern, hostBot.Name);
                if (locked) room.Password = (1000 + rng.Next(9000)).ToString();
                room.Seats[0] = hostBot;
                room.JoinAt[0] = room.ReadyAt[0] = created;
                var taken = new List<int> { host };

                // 다른 봇이 하나씩 들어온다. 방장이 기다리다 나가기도 하고, 비번방은 친구 한 명만 오고 끝날 수도 있다.
                var abandons = locked || rng.Next(100) < 15;
                var giveUp = created + 3 * Minute + rng.Next(7 * (int)Minute);
                var t = created;
                var full = true;
                for (var i = 1; i < room.Seats.Length; i++)
                {
                    t += (quick ? 20_000 : 60_000) + rng.Next(quick ? 100_000 : 360_000);
                    var who = (abandons && t >= giveUp) || (locked && rng.Next(100) < 40) ? -1 : PickFree(busyUntil, t, rng, taken);
                    if (who < 0)
                    {
                        full = false;
                        break;
                    }
                    taken.Add(who);
                    room.Seats[i] = bots[who];
                    room.JoinAt[i] = t;
                    room.ReadyAt[i] = t + 2_000 + rng.Next(7_000);
                }
                if (full)
                {
                    var lastReady = 0L;
                    foreach (var r in room.ReadyAt) lastReady = Math.Max(lastReady, r);
                    room.EndsAt = lastReady + 2_000 + rng.Next(5_000);
                }
                else room.EndsAt = Math.Max(giveUp, t);

                // 다 차서 시작했으면 판을 하는 동안(2~5분) 다른 데 나타나지 않는다
                var until = full ? room.EndsAt + 2 * Minute + rng.Next(3 * (int)Minute) : room.EndsAt;
                foreach (var w in taken) busyUntil[w] = Math.Max(busyUntil[w], until);
                result.Add((room, until));
              }
            }
            return result;
        }

        private static int PickFree(long[] busyUntil, long at, Rng rng, List<int> exclude)
        {
            var free = new List<int>();
            for (var i = 0; i < busyUntil.Length; i++)
                if (busyUntil[i] <= at && (exclude == null || !exclude.Contains(i))) free.Add(i);
            return free.Count == 0 ? -1 : free[rng.Next(free.Count)];
        }

        /// <summary>Firebase push 키와 같은 모양의 방 번호 (시각 8자 + 무작위 12자, 맨 앞은 "-").</summary>
        private static string PushId(long created, Rng rng)
        {
            var c = new char[20];
            var t = created;
            for (var i = 7; i >= 0; i--)
            {
                c[i] = PushChars[(int)(t % 64)];
                t /= 64;
            }
            for (var i = 8; i < 20; i++) c[i] = PushChars[rng.Next(64)];
            return new string(c);
        }
    }
}
