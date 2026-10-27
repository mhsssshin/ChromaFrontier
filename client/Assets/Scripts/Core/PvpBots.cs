using System;

namespace ChromaFrontier.Core
{
    /// <summary>봇이 이모티콘을 보낼 만한 순간.</summary>
    public enum BotMood : byte { Start, Good, Hurt, Win, Lose }

    /// <summary>
    /// 대전 봇 한 명. 빠른 대전에서 사람이 안 모이면 들어와 사람처럼 둔다: 실력(AiLevel)과 성향(AiPersona)이 다르고,
    /// 생각하는 시간이 들쭉날쭉하고(가끔 오래 망설임), 자기 버릇대로 이모티콘을 보낸다. 덱과 스킨도 각자 것.
    /// </summary>
    public sealed class PvpBot
    {
        public string Id, Name;
        public AiLevel Level;
        public AiPersona Persona = AiPersona.Default;
        public float ThinkMin, ThinkMax;  // 보통 생각 시간(초)
        public float Hesitate;            // 오래 망설일 확률
        public float Chatty;              // 이모티콘을 보낼 확률 (0~1)
        public int[] Good, Hurt, Win, Lose, Hello; // 이모티콘 번호 (EmotePacks 전체 번호)
        public string Deck, Skin;         // 스킬 덱(CardShop 코드), 자리 스킨 "재질:색:유닛세트"

        public string Uid => PvpBots.UidPrefix + Id;
    }

    /// <summary>
    /// 대전 봇 10명. 서버가 없으므로 봇의 수는 방에 있는 사람 기기 하나(나가지 않은 사람 중 가장 앞 자리)가 대신 계산해
    /// 보통 수처럼 보낸다. 다른 기기에는 사람의 수와 똑같이 보인다. 봇의 uid 는 "bot:" 으로 시작한다.
    /// </summary>
    public static class PvpBots
    {
        public const string UidPrefix = "bot:";

        // 이모티콘 번호 (EmotePacks: 기본 0~7, 킹받는 8~15, 귀요미 16~23, 멘붕 24~31)
        private const int Like = 0, Angry = 1, Gasp = 2, Sob = 3, Lol = 4, Nice = 5, Hmm = 6, Gg = 7;
        private const int Tongue = 8, Whatever = 9, Tease = 10, Smug = 11, Nope = 12, Yawn = 13, Clap = 14, ReallyThere = 15;
        private const int Shy = 16, Sparkle = 17, Kiss = 18, Sleepy = 19, Flower = 20, Peace = 21, Love = 22, Fighting = 23;
        private const int Meltdown = 24, SoulOut = 25, Tears = 26, Boom = 27, Zen = 28, Blank = 29, Boing = 30, Flop = 31;

        public static readonly PvpBot[] All =
        {
            new()
            {
                Id = "potato", Name = "새벽감자", Level = AiLevel.Normal, ThinkMin = 3f, ThinkMax = 8f, Hesitate = 0.15f, Chatty = 0.35f,
                Good = new[] { Sparkle, Peace, Like }, Hurt = new[] { Gasp, Sob }, Win = new[] { Gg, Flower }, Lose = new[] { Gg, Sob }, Hello = new[] { Fighting, Like },
                Deck = "tr_red,tr_blue,tr_yellow,bleach,mine,surge,beacon,split", Skin = "grass:sky:",
            },
            new()
            {
                Id = "fist", Name = "불꽃주먹99", Level = AiLevel.Hard, Persona = new AiPersona { Id = "fist", StealWeight = 0.6f },
                ThinkMin = 1.5f, ThinkMax = 4.5f, Hesitate = 0.05f, Chatty = 0.5f,
                Good = new[] { Lol, Tongue, Smug, Tease }, Hurt = new[] { Angry, Nope }, Win = new[] { Tongue, Gg, Smug }, Lose = new[] { Angry, Gg }, Hello = new[] { Tease },
                Deck = "tr_red,tr_red,surge,surge,siphon,quake,mine,bleach,beacon", Skin = "stone:orange:halloween",
            },
            new()
            {
                Id = "water", Name = "물멍중", Level = AiLevel.Easy, ThinkMin = 4f, ThinkMax = 10f, Hesitate = 0.25f, Chatty = 0.15f,
                Good = new[] { Like, Sparkle }, Hurt = new[] { Hmm, Zen }, Win = new[] { Gg }, Lose = new[] { Gg, Zen }, Hello = new[] { Sleepy },
                Deck = "tr_blue,tr_blue,tr_yellow,bleach,mine,surge", Skin = "water:sky:",
            },
            new()
            {
                Id = "kim", Name = "chroma_kim", Level = AiLevel.Normal, ThinkMin = 2.5f, ThinkMax = 6f, Hesitate = 0.1f, Chatty = 0.25f,
                Good = new[] { Like, Lol }, Hurt = new[] { Gasp, Hmm }, Win = new[] { Gg }, Lose = new[] { Gg, Nice }, Hello = new[] { Like },
                Deck = "tr_red,tr_blue,tr_yellow,bleach,mine,surge,warp,lock,barrier", Skin = "plain:navy:",
            },
            new()
            {
                Id = "bolt", Name = "번개는내꺼", Level = AiLevel.Normal, Persona = new AiPersona { Id = "bolt", StealWeight = 0.4f, MineWeight = 1.3f },
                ThinkMin = 2f, ThinkMax = 5f, Hesitate = 0.08f, Chatty = 0.45f,
                Good = new[] { Lol, Smug, Whatever }, Hurt = new[] { Angry, ReallyThere, Gasp }, Win = new[] { Smug, Gg }, Lose = new[] { Gg, Angry }, Hello = new[] { Like, Tease },
                Deck = "tr_yellow,tr_yellow,mine,mine,surge,bleach,siphon,quake", Skin = "crystal:purple:",
            },
            new()
            {
                Id = "panda", Name = "느긋한판다", Level = AiLevel.Easy, ThinkMin = 5f, ThinkMax = 12f, Hesitate = 0.3f, Chatty = 0.3f,
                Good = new[] { Shy, Flower, Peace }, Hurt = new[] { Sob, Sleepy }, Win = new[] { Love, Gg }, Lose = new[] { Gg, Tears }, Hello = new[] { Flower, Kiss },
                Deck = "tr_red,tr_blue,bleach,mine,surge,barrier", Skin = "wood:pink:",
            },
            new()
            {
                Id = "rich", Name = "결정부자", Level = AiLevel.Hard, Persona = new AiPersona { Id = "rich", ShieldWeight = 0.3f, LockWeight = 1.8f, BleachWeight = 1.4f },
                ThinkMin = 3f, ThinkMax = 7f, Hesitate = 0.12f, Chatty = 0.1f,
                Good = new[] { Hmm, Like }, Hurt = new[] { Hmm }, Win = new[] { Gg }, Lose = new[] { Gg, Nice }, Hello = new[] { Like },
                Deck = "lock,lock,bleach,bleach,barrier,barrier,warp,tr_blue,mine", Skin = "marble:white:jeheon",
            },
            new()
            {
                Id = "bunny", Name = "하양토끼", Level = AiLevel.Normal, ThinkMin = 3f, ThinkMax = 7f, Hesitate = 0.15f, Chatty = 0.35f,
                Good = new[] { Sparkle, Kiss, Peace }, Hurt = new[] { Tears, Gasp }, Win = new[] { Love, Peace }, Lose = new[] { Gg, Sob }, Hello = new[] { Shy, Fighting },
                Deck = "tr_red,tr_blue,tr_yellow,split,split,surge,beacon,bleach", Skin = "grass:white:",
            },
            new()
            {
                Id = "uncle", Name = "삼촌왔다", Level = AiLevel.Easy, ThinkMin = 2f, ThinkMax = 6f, Hesitate = 0.2f, Chatty = 0.45f,
                Good = new[] { Boom, Lol, Boing }, Hurt = new[] { Meltdown, SoulOut, Flop }, Win = new[] { Lol, Gg }, Lose = new[] { Flop, Gg }, Hello = new[] { Boing, Like },
                Deck = "tr_red,tr_yellow,mine,surge,beacon,quake,bleach", Skin = "stone:black:",
            },
            new()
            {
                Id = "zero", Name = "zeroSum", Level = AiLevel.Hard, Persona = new AiPersona { Id = "zero", CatalystSteal = 4f, LoneUnitPenalty = 1.5f, WarpBonus = 1f },
                ThinkMin = 2f, ThinkMax = 5f, Hesitate = 0.06f, Chatty = 0.06f,
                Good = new[] { Hmm }, Hurt = new[] { Hmm }, Win = new[] { Gg }, Lose = new[] { Gg }, Hello = new[] { Like },
                Deck = "warp,warp,siphon,siphon,tr_red,tr_blue,tr_yellow,quake,bleach", Skin = "crystal:navy:",
            },
        };

        public static bool IsBot(string uid) => uid != null && uid.StartsWith(UidPrefix, StringComparison.Ordinal);

        public static PvpBot Find(string uid)
        {
            if (!IsBot(uid)) return null;
            foreach (var b in All)
                if (b.Uid == uid) return b;
            return null;
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
}
