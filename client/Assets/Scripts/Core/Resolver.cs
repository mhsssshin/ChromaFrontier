using System;
using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>밸런스 상수. 숫자는 여기서만 조정한다.</summary>
    public static class Balance
    {
        // 새 스킬 (단계 = 카드 강화 Lv1~3)
        public static int SurgeRadius(int level) => level >= 3 ? 2 : 1;                 // 폭주: 3x3 → (Lv2 + 십자 2칸) → 5x5
        public static int BarrierTicks(int level) => (level >= 2 ? 3 : 2) * TicksPerRound; // 방벽: 2턴 → 3턴
        public static int BarrierRadius(int level) => level >= 3 ? 2 : 1;               //       3x3 → 5x5
        public static int SplitCopies(int level) => level >= 3 ? 4 : level;             // 분열: 1 → 2 → 4칸
        public static int QuakeRadius(int level) => level >= 3 ? 2 : 1;                 // 지진: 3x3 → 5x5 (Lv2 부순 칸 차지)

        public const int MaxTurns = 20;        // 양측 합산
        public const int HandSize = 5;
        public const int RedCross = 1;         // 화염: 십자 반경
        public const int BlueLength = 3;       // 물: 직선 길이 (배치 칸 제외)
        public const int YellowBolts = 4;      // 번개: 무작위 칸 수
        public const int YellowRange = 2;      // 번개: 체비쇼프 반경
        public const int OrangeRay = 3;        // 마그마: 8방향 광선 길이
        public const int PurpleRadius = 2;     // 폭풍: 맨해튼 반경
        // 지속 효과는 "내 턴 기준"으로 센다. 타이머는 양측 턴마다 1씩 줄므로 한 라운드 = 2틱.
        public const int TicksPerRound = 2;
        public const int PurpleShield = 2 * TicksPerRound; // 폭풍: 침묵 2턴
        public const int WhiteRadius = 2;      // 백색: 성역 체비쇼프 반경
        public const int WhiteShield = 5 * TicksPerRound;  // 백색: 성역 5턴
        public const int BlackRadius = 2;      // 흑색: 흡수 체비쇼프 반경
        public const int Tier3Boost = 1;       // 2단계 유닛을 촉매로 쓴 3단계: 반경 +1
        public const int RedSurgeCross = 2;    // 업화(화염+화염): 3x3 + 십자 2칸
        public const int BlueSurgeLength = 4;  // 해일(물+물): 길이 4, 폭 3
        public const int YellowSurgeBolts = 8; // 뇌우(번개+번개): 무작위 8칸
        public const int YellowSurgeRange = 3; //   반경 3
        public const int LockTurns = 2 * TicksPerRound;    // 영역 잠금: 2턴 (강화하면 한 단계마다 +1턴, LockTurnsAt)
        public const int LockRadius = 1;       // 영역 잠금: 체비쇼프 반경 (1 = 3x3)
        public const int LockFromTurn = 6;     // 영역 잠금은 양측 합산 6턴째(각자 4번째 턴)부터 손패에 들어오고 쓸 수 있다
        public const int MineRadius = 1;       // 잠복 지뢰: 폭발 체비쇼프 반경
        public const int BeaconClaims = 1;     // 거점: 라운드마다 주변(8칸)에서 점유하는 칸 수 (강화 단계마다 배)

        public static int LockTurnsAt(int level) => LockTurns + (level - 1) * TicksPerRound;
    }

    public sealed class PlayResult
    {
        public Side Actor;
        public Cell At;                               // 유닛이 실제로 놓인 칸 (얼음이면 미끄러진 칸)
        public bool Slid;                             // 얼음에서 미끄러졌다
        public Element Placed;                        // 유닛 카드면 원소, 스킬이면 None
        public Skill Skill;                           // 스킬 카드면 종류
        public Element Bleached = Element.None;       // 탈색·원소 변환으로 바뀌기 전 원래 원소
        public bool MineTriggered;                    // 상대 지뢰를 밟았다 (유닛은 사라지고 Captured 는 지뢰 주인의 것)
        public Element Fused = Element.None;          // 융합 결과 (2·3단계), 없으면 None
        public readonly List<Cell> Catalysts = new(); // 융합에 소모된 이웃 유닛 칸
        public readonly List<Cell> Captured = new();  // 이번 수로 소유가 바뀐 칸
        public int Stolen;                            // Captured 중 원래 상대 타일이던 칸 수
        public bool StoleCatalyst;                    // 상대 유닛을 촉매로 썼는가
        public bool ExtraTurn;
        public bool Boosted;                          // 2단계 유닛을 촉매로 쓴 3단계 (반경 강화)
        public bool Resonance;                        // 같은 원소 이웃과 공명 (업화·해일·뇌우: 기본 효과 강화)
    }

    /// <summary>
    /// 카드 효과를 보드에 적용한다. 유닛 순서: (상대 지뢰면 폭발하고 끝) → 배치 칸 점유 → 기본 원소 효과 → 이웃과 융합.
    /// 반응에 휘말린 칸은 전부 카드를 낸 쪽의 영토가 된다. 대상 검증은 Game 이 한다.
    /// </summary>
    public static class Resolver
    {
        /// <param name="mineLevel">상대(지뢰 주인)의 지뢰 강화 단계 — 폭발 범위</param>
        public static PlayResult PlaceUnit(Board board, Rng rng, Side side, Cell at, Element element, Cell blueDir, int mineLevel = 1)
        {
            var r = new PlayResult { Actor = side, Placed = element };

            // 얼음: 한 칸 더 미끄러진다 (밀고 나온 방향으로, 막혀 있으면 제자리)
            if (board[at].Terrain == Terrain.Ice)
            {
                var to = at + SlideDir(board, at, side);
                if (CanLand(board, to, side))
                {
                    at = to;
                    r.Slid = true;
                }
            }
            r.At = at;

            ref var t = ref board[at];
            if (t.Mine != Side.None && t.Mine != side)
            {
                var owner = t.Mine;
                t.Mine = Side.None;
                r.MineTriggered = true;
                foreach (var c in board.Cells())
                    if (InMineBlast(c, at, mineLevel)) Claim(board, c, owner, r);
                return r;
            }
            t.Unit = element;
            t.UnitOwner = side;
            Claim(board, at, side, r);

            // 다른 원소와 융합할 수 있으면 융합이 우선. 아니면 같은 원소 이웃과 공명해 기본 효과가 강화된다.
            // 침묵 지대에 놓은 유닛은 기본 효과만 낸다.
            var silenced = board[at].Terrain == Terrain.Silence;
            List<Cell> tier3 = null;
            Cell? tier2 = null;
            var fuses = !silenced && FindFusion(board, at, side, element, out tier3, out tier2);
            var echo = fuses || silenced ? null : PickNeighbor(board, at, side, element);
            if (echo.HasValue)
            {
                r.Resonance = true;
                Consume(board, echo.Value, side, r);
            }

            BaseEffect(board, rng, side, at, element, blueDir, r.Resonance, r);

            if (fuses) ApplyFusion(board, at, side, element, tier3, tier2, r);
            return r;
        }

        /// <summary>지뢰 폭발 범위: Lv1 3x3, Lv2 3x3 + 십자 2칸, Lv3 5x5.</summary>
        public static bool InMineBlast(Cell c, Cell at, int level)
        {
            var d = c.Chebyshev(at);
            if (level >= 3) return d <= Balance.MineRadius + 1;
            if (level == 2 && (c.X == at.X || c.Y == at.Y)) return d <= Balance.MineRadius + 1;
            return d <= Balance.MineRadius;
        }

        private static void ClaimAround(Board board, Cell at, Side side, PlayResult r)
        {
            foreach (var d in Cell.Orthogonal)
                if (board.InBounds(at + d)) Claim(board, at + d, side, r);
        }

        /// <summary>1단계 원소의 기본 효과 (surge = 공명으로 강화된 업화·해일·뇌우).</summary>
        private static void BaseEffect(Board board, Rng rng, Side side, Cell at, Element element, Cell blueDir, bool surge, PlayResult r)
        {
            switch (element)
            {
                case Element.Red:
                    if (surge)
                    {
                        // 업화: 주변 3x3 + 십자 2칸
                        foreach (var d in Cell.Around8)
                            if (board.InBounds(at + d)) Claim(board, at + d, side, r);
                        Cross(board, at, Balance.RedSurgeCross, side, r);
                    }
                    else Cross(board, at, Balance.RedCross, side, r);
                    break;
                case Element.Blue:
                    foreach (var c in WaterPath(board, at, blueDir, surge)) Claim(board, c, side, r);
                    break;
                case Element.Yellow:
                    Lightning(board, rng, at, side, r,
                        surge ? Balance.YellowSurgeBolts : Balance.YellowBolts,
                        surge ? Balance.YellowSurgeRange : Balance.YellowRange);
                    break;
            }
        }

        /// <summary>
        /// 물이 at 에서 dir 로 흐르며 덮는 칸. 판 밖·낭떠러지에서 멈춘다.
        /// 해일(surge, 물+물 공명)이면 BlueSurgeLength 만큼 길고, 양옆으로 한 칸씩 넓다.
        /// </summary>
        public static List<Cell> WaterPath(Board board, Cell at, Cell dir, bool surge)
        {
            var cells = new List<Cell>();
            var side = new Cell(dir.Y, dir.X); // 흐름에 수직인 방향
            var length = surge ? Balance.BlueSurgeLength : Balance.BlueLength;
            for (var i = 1; i <= length; i++)
            {
                var c = at + dir * i;
                if (!board.InBounds(c) || board[c].Blocked) break;
                cells.Add(c);
                if (!surge) continue;
                foreach (var s in new[] { c + side, c + side * -1 })
                    if (board.InBounds(s) && !board[s].Blocked) cells.Add(s);
            }
            if (surge)
                foreach (var s in new[] { at + side, at + side * -1 })
                    if (board.InBounds(s) && !board[s].Blocked) cells.Add(s);
            return cells;
        }

        /// <summary>
        /// 스킬 효과. target2 는 공간 왜곡의 두 번째 칸, echoDir 는 물 유닛 재점화의 방향에만 쓴다.
        /// level 은 카드 강화 단계 (CardShop.LevelText). 지뢰·거점의 강화는 터질 때·자랄 때 적용된다.
        /// </summary>
        public static PlayResult CastSkill(Board board, Rng rng, Side side, Card card, Cell target, Cell target2 = default, Cell echoDir = default, int level = 1)
        {
            var skill = card.Skill;
            var r = new PlayResult { Actor = side, Skill = skill, At = target };
            switch (skill)
            {
                case Skill.Warp:
                    board[target].SwapContents(ref board[target2]); // 지형은 제자리
                    if (level >= 2)
                    {
                        Claim(board, target, side, r);
                        Claim(board, target2, side, r);
                    }
                    if (level >= 3)
                    {
                        ClaimAround(board, target, side, r);
                        ClaimAround(board, target2, side, r);
                    }
                    break;
                case Skill.Lock:
                    foreach (var c in board.Cells())
                        if (c.Chebyshev(target) <= Balance.LockRadius && !board[c].Blocked)
                            board[c].Lock = Balance.LockTurnsAt(level);
                    break;
                case Skill.Bleach:
                    r.Bleached = board[target].Unit;
                    board[target].Unit = Element.Grey;
                    if (level >= 2) Claim(board, target, side, r);
                    if (level >= 3) ClaimAround(board, target, side, r);
                    break;
                case Skill.Mine:
                    board[target].Mine = side;
                    break;
                case Skill.Transmute:
                    r.Bleached = board[target].Unit;
                    board[target].Unit = card.Element;
                    if (level >= 2) Claim(board, target, side, r);
                    if (level >= 3) ClaimAround(board, target, side, r);
                    break;
                case Skill.Echo:
                    r.Placed = board[target].Unit;
                    BaseEffect(board, rng, side, target, board[target].Unit, echoDir, level >= 3, r);
                    if (level == 2) ClaimAround(board, target, side, r);
                    break;
                case Skill.Beacon:
                    board[target].Beacon = side;
                    break;
                case Skill.Surge:
                {
                    r.Placed = board[target].Unit;
                    var rad = Balance.SurgeRadius(level);
                    foreach (var c in board.Cells())
                    {
                        var dx = Math.Abs(c.X - target.X);
                        var dy = Math.Abs(c.Y - target.Y);
                        var inside = c.Chebyshev(target) <= rad || (level == 2 && (dx == 0 || dy == 0) && dx + dy <= 2);
                        if (inside) Claim(board, c, side, r);
                    }
                    break;
                }
                case Skill.Siphon:
                    r.Bleached = board[target].Unit;
                    board[target].UnitOwner = side;
                    Claim(board, target, side, r);
                    r.StoleCatalyst = true;
                    if (level >= 2) ClaimAround(board, target, side, r);
                    if (level >= 3)
                        foreach (var d in Cell.Around8)
                            if (board.InBounds(target + d)) Claim(board, target + d, side, r);
                    break;
                case Skill.Barrier:
                {
                    var rad = Balance.BarrierRadius(level);
                    foreach (var c in board.Cells())
                        if (c.Chebyshev(target) <= rad && board[c].Owner == side && !board[c].Blocked)
                        {
                            board[c].Shield = Math.Max(board[c].Shield, Balance.BarrierTicks(level));
                            r.Catalysts.Add(c);
                        }
                    break;
                }
                case Skill.Split:
                {
                    var element = board[target].Unit;
                    r.Placed = element;
                    var left = Balance.SplitCopies(level);
                    foreach (var d in Cell.Orthogonal)
                    {
                        if (left == 0) break;
                        var c = target + d;
                        if (!CanSplitInto(board, c, side)) continue;
                        board[c].Unit = element;
                        board[c].UnitOwner = side;
                        Claim(board, c, side, r);
                        if (!r.Captured.Contains(c)) r.Captured.Add(c);
                        r.Catalysts.Add(c);
                        left--;
                    }
                    break;
                }
                case Skill.Quake:
                {
                    var rad = Balance.QuakeRadius(level);
                    foreach (var c in board.Cells())
                    {
                        if (c.Chebyshev(target) > rad || !IsQuakeTarget(board, c, side)) continue;
                        board[c].Unit = Element.None;
                        board[c].UnitOwner = Side.None;
                        r.Catalysts.Add(c);
                        if (level >= 2) Claim(board, c, side, r);
                    }
                    break;
                }
            }
            return r;
        }

        /// <summary>분열로 복제가 들어갈 수 있는 칸: 빈 평지(잠금·지뢰·상대 보호 없음).</summary>
        public static bool CanSplitInto(Board board, Cell c, Side side)
        {
            if (!board.InBounds(c)) return false;
            ref var t = ref board[c];
            return !t.Blocked && !t.HasUnit && t.Lock == 0 && t.Mine == Side.None && !t.IsProtectedFrom(side);
        }

        /// <summary>지진이 부술 수 있는 유닛: 상대 것이고 보호받지 않는다.</summary>
        public static bool IsQuakeTarget(Board board, Cell c, Side side) =>
            board.InBounds(c) && board[c].HasUnit && board[c].UnitOwner == side.Opponent() && !board[c].IsProtectedFrom(side);

        /// <summary>
        /// 융합. 놓은 원소 + 상하좌우 이웃 유닛(1·2단계) 1~2개의 색을 합쳐 빨·파·노가 다 모이면 3단계(트리니티).
        /// 2단계 유닛은 색 두 개를 가진 촉매라서, 2단계 옆에 남은 한 색을 놓기만 해도 3단계가 된다 (3차 조합).
        /// 이때 2단계 촉매를 쓰면 3단계 효과 반경이 Tier3Boost 만큼 커진다 (Boosted).
        /// 3단계가 안 되면, 다른 1단계 원소 이웃과 2단계를 만든다. 촉매는 상대 유닛을 우선 쓴다 (흑색/가로채기 유도).
        /// </summary>
        private static bool FindFusion(Board board, Cell at, Side side, Element placed, out List<Cell> tier3, out Cell? tier2)
        {
            tier3 = null;
            tier2 = null;
            var neighbors = new List<Cell>();
            foreach (var d in Cell.Orthogonal)
            {
                var n = at + d;
                if (board.InBounds(n) && board[n].Unit.Colors() != 0 && board[n].Terrain != Terrain.Silence) neighbors.Add(n);
            }

            // 3단계: 촉매 1~2개로 세 색을 모으는 조합 중 최선 (흑색 > 2단계 촉매 사용 > 촉매 적게)
            List<Cell> bestSet = null;
            var bestScore = int.MinValue;
            void Consider(List<Cell> set)
            {
                var mask = placed.Colors();
                var steal = false;
                var boosted = false;
                foreach (var c in set)
                {
                    mask |= board[c].Unit.Colors();
                    steal |= board[c].UnitOwner != side;
                    boosted |= board[c].Unit.Tier() == 2;
                }
                if (mask != Elements.AllColors) return;
                var score = (steal ? 100 : 0) + (boosted ? 10 : 0) - set.Count;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestSet = set;
                }
            }
            for (var i = 0; i < neighbors.Count; i++)
            {
                Consider(new List<Cell> { neighbors[i] });
                for (var j = i + 1; j < neighbors.Count; j++) Consider(new List<Cell> { neighbors[i], neighbors[j] });
            }
            if (bestSet != null)
            {
                tier3 = bestSet;
                return true;
            }

            // 2단계: 다른 1단계 원소 이웃 하나
            foreach (var e in Elements.Base)
            {
                if (e == placed) continue;
                var n = PickNeighbor(board, at, side, e);
                if (n.HasValue && (!tier2.HasValue || board[n.Value].UnitOwner != side)) tier2 = n;
            }
            return tier2.HasValue;
        }

        private static void ApplyFusion(Board board, Cell at, Side side, Element placed, List<Cell> tier3, Cell? tier2, PlayResult r)
        {
            if (tier3 != null)
            {
                var stolen = false;
                foreach (var c in tier3)
                {
                    stolen |= board[c].UnitOwner != side;
                    r.Boosted |= board[c].Unit.Tier() == 2;
                }
                foreach (var c in tier3) Consume(board, c, side, r);
                var boost = r.Boosted ? Balance.Tier3Boost : 0;
                if (stolen) Black(board, at, side, r, boost); else White(board, at, side, r, boost);
                return;
            }

            var any = tier2.Value;
            var fused = Elements.Fuse(placed, board[any].Unit);
            Consume(board, any, side, r);
            r.Fused = fused;
            board[at].Unit = fused;
            switch (fused)
            {
                case Element.Orange:
                    foreach (var d in Cell.Around8)
                        for (var i = 1; i <= Balance.OrangeRay; i++)
                        {
                            var c = at + d * i;
                            if (!board.InBounds(c)) break;
                            Claim(board, c, side, r);
                        }
                    break;
                case Element.Purple:
                    foreach (var c in board.Cells())
                        if (c.Manhattan(at) <= Balance.PurpleRadius) Claim(board, c, side, r);
                    foreach (var c in board.Cells())
                        if (c.Manhattan(at) <= Balance.PurpleRadius && board[c].Owner == side)
                            board[c].Shield = Balance.PurpleShield;
                    break;
                case Element.Green:
                    Gust(board, at, side, r);
                    break;
            }
        }

        /// <summary>
        /// 얼음 위 미끄러짐 방향: 내 땅에서 밀고 나온 방향(맞닿은 내 칸 → 놓은 칸).
        /// 내 땅 위에 놓았으면 상대 쪽 앞방향 (플레이어 +X, AI −X).
        /// </summary>
        public static Cell SlideDir(Board board, Cell at, Side side)
        {
            foreach (var d in Cell.Orthogonal)
            {
                var n = at + d * -1;
                if (board.InBounds(n) && !board[n].Blocked && board[n].Owner == side && board[at].Owner != side) return d;
            }
            return side == Side.Player ? new Cell(1, 0) : new Cell(-1, 0);
        }

        /// <summary>미끄러진 유닛이 멈출 수 있는 칸인가.</summary>
        public static bool CanLand(Board board, Cell c, Side side) =>
            board.InBounds(c) && !board[c].Blocked && !board[c].HasUnit && board[c].Lock == 0 && !board[c].IsProtectedFrom(side);

        private static Cell? PickNeighbor(Board board, Cell at, Side side, Element want)
        {
            Cell? own = null;
            foreach (var d in Cell.Orthogonal)
            {
                var n = at + d;
                if (!board.InBounds(n) || board[n].Unit != want || board[n].Terrain == Terrain.Silence) continue;
                if (board[n].UnitOwner != side) return n;
                own ??= n;
            }
            return own;
        }

        private static void Consume(Board board, Cell c, Side side, PlayResult r)
        {
            if (board[c].UnitOwner != side) r.StoleCatalyst = true;
            board[c].Unit = Element.None;
            board[c].UnitOwner = Side.None;
            r.Catalysts.Add(c);
            Claim(board, c, side, r);
        }

        /// <summary>흑색: 반경 안의 상대 타일을 전부 흡수하고 추가 턴.</summary>
        private static void Black(Board board, Cell at, Side side, PlayResult r, int boost)
        {
            r.Fused = Element.Black;
            board[at].Unit = Element.Black;
            var foe = side.Opponent();
            foreach (var c in board.Cells())
                if (c.Chebyshev(at) <= Balance.BlackRadius + boost && board[c].Owner == foe) Claim(board, c, side, r);
            r.ExtraTurn = true;
        }

        /// <summary>백색: 주변 (1+boost) 반경 점유 후 성역 반경 안의 내 타일 전부에 보호막(상대가 못 바꿈).</summary>
        private static void White(Board board, Cell at, Side side, PlayResult r, int boost)
        {
            r.Fused = Element.White;
            board[at].Unit = Element.White;
            foreach (var c in board.Cells())
                if (c.Chebyshev(at) <= 1 + boost) Claim(board, c, side, r);
            foreach (var c in board.Cells())
                if (c.Chebyshev(at) <= Balance.WhiteRadius + boost && board[c].Owner == side)
                    board[c].Shield = Balance.WhiteShield;
        }

        /// <summary>
        /// 돌풍: 주변 8칸의 내용(소유·유닛)을 한 칸 바깥으로 밀어낸 뒤 그 8칸을 점유한다.
        /// 밀려날 곳이 판 밖·낭떠러지·보호된 칸이면 그 칸 내용은 그냥 덮인다.
        /// </summary>
        private static void Gust(Board board, Cell at, Side side, PlayResult r)
        {
            var snapshot = board.Clone();
            foreach (var d in Cell.Around8)
            {
                var from = at + d;
                var to = from + d;
                if (!board.InBounds(from) || snapshot[from].Blocked) continue;
                if (board.InBounds(to) && !board[to].Blocked && !board[to].IsProtectedFrom(snapshot[from].Owner))
                {
                    var src = snapshot[from];
                    if (board[to].Owner != src.Owner)
                    {
                        if (src.Owner == side) TrackCapture(board, to, side, r);
                        board[to].SetOwner(src.Owner);
                    }
                    if (src.HasUnit && !board[to].HasUnit)
                    {
                        board[to].Unit = src.Unit;
                        board[to].UnitOwner = src.UnitOwner;
                        board[from].Unit = Element.None;
                        board[from].UnitOwner = Side.None;
                    }
                }
                Claim(board, from, side, r);
            }
        }

        private static void Cross(Board board, Cell at, int radius, Side side, PlayResult r)
        {
            foreach (var d in Cell.Orthogonal)
                for (var i = 1; i <= radius; i++)
                {
                    var c = at + d * i;
                    if (!board.InBounds(c)) break;
                    Claim(board, c, side, r);
                }
        }

        private static void Lightning(Board board, Rng rng, Cell at, Side side, PlayResult r, int bolts, int range)
        {
            var pool = new List<Cell>();
            foreach (var c in board.Cells())
                if (c != at && c.Chebyshev(at) <= range && !board[c].Blocked) pool.Add(c);
            for (var i = 0; i < bolts && pool.Count > 0; i++)
            {
                var k = rng.Next(pool.Count);
                Claim(board, pool[k], side, r);
                pool[k] = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
            }
        }

        private static void Claim(Board board, Cell c, Side side, PlayResult r)
        {
            ref var t = ref board[c];
            if (t.Blocked || t.Owner == side || t.IsProtectedFrom(side)) return;
            TrackCapture(board, c, side, r);
            t.SetOwner(side);
        }

        private static void TrackCapture(Board board, Cell c, Side side, PlayResult r)
        {
            if (board[c].Owner == side.Opponent()) r.Stolen++;
            if (!r.Captured.Contains(c)) r.Captured.Add(c);
        }
    }
}
