using System;
using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>스테이지별 규칙 (맵 기믹 중 지형이 아닌 것). 지형 기믹은 Board 의 Terrain 이 갖는다.</summary>
    public sealed class StageRules
    {
        public int MaxTurns = Balance.MaxTurns; // 양측 합산
        public int LavaEvery;                   // 화산: N 라운드마다 분출 (0 = 없음)
        public int LavaCount = 1;               //   분출 한 번에 양쪽에서 각각 터지는 유닛 수
        public int LockFromTurn = Balance.LockFromTurn; // 영역 잠금을 뽑고 쓸 수 있는 첫 턴 (양측 합산)
    }

    public enum RoundEventKind : byte { Lava, BlackHole, Conveyor, Beacon }

    /// <summary>라운드 끝 기믹이 일으킨 일 (뷰 연출용).</summary>
    public readonly struct RoundEvent
    {
        public readonly RoundEventKind Kind;
        public readonly Cell Cell;
        public RoundEvent(RoundEventKind kind, Cell cell) { Kind = kind; Cell = cell; }
    }

    /// <summary>
    /// 한 판의 권위 상태. 플레이어와 AI가 번갈아 1장씩, 양측 합산 MaxTurns 턴이 끝나면 점유 칸 수로 승패.
    /// 플레이어 → AI 가 한 라운드이고, 라운드가 끝날 때 맵 기믹(컨베이어·블랙홀·거점·화산)이 움직인다.
    /// 뷰는 이 상태를 읽기만 하고, 수는 Play / Cast / Mulligan 으로만 둔다.
    /// 턴을 끝내는 것은 Play(유닛)와 Mulligan 뿐이다. 스킬(Cast)은 턴마다 한 장까지 쓰는 보조 행동이다.
    /// </summary>
    public sealed class Game
    {
        public Board Board { get; private set; }
        public Side Current { get; private set; }
        public int Turn { get; private set; }  // 끝난 턴 수 (추가 턴은 세지 않음)
        public int Round { get; private set; } // 끝난 라운드 수
        public StageRules Rules { get; private set; } = new();
        public PlayResult LastResult { get; private set; } // 패 버리기(멀리건)면 null
        public bool SkillUsed { get; private set; }        // 이번 턴에 스킬을 이미 썼다 (턴마다 한 장)
        public readonly List<RoundEvent> RoundEvents = new(); // 직전 라운드 끝에 일어난 기믹
        public readonly List<Objective> Objectives = new();   // 플레이어의 게임 중 목표 (달성하면 바로 효과 발동)
        public readonly List<ObjectiveEvent> ObjectiveEvents = new(); // 직전 수에서 달성한 목표
        public bool BonusTurnTaken { get; private set; }     // 직전 수 뒤에 목표 보상으로 한 번 더 두게 됐다
        public int TurnsTaken { get; private set; }          // 끝난 차례 수 (추가 턴 포함). 뷰가 차례가 바뀐 것을 알아챈다
        public readonly List<Cell> PioneerCells = new();     // 소모품 개척으로 시작할 때 받은 칸

        private Rng _rng;
        private Deck _player, _ai;
        private MatchStats _playerStats = new(), _aiStats = new();
        private bool _bonusTurn; // 목표 보상·선공: 플레이어가 다음에 유닛을 놓은 뒤 한 번 더
        private Dictionary<Card, int> _levels; // 플레이어 카드 강화 단계 (판 중에 안 바뀌어 복제본과 공유). AI 는 늘 Lv1

        private Game() { }

        public static Game CreateDefault(int seed)
        {
            var board = new Board(8, 8);
            // 플레이어는 화면 왼쪽 모서리, AI는 오른쪽 모서리에서 시작 (각 2x2).
            for (var y = 6; y < 8; y++)
                for (var x = 0; x < 2; x++)
                    board[new Cell(x, y)].Owner = Side.Player;
            for (var y = 0; y < 2; y++)
                for (var x = 6; x < 8; x++)
                    board[new Cell(x, y)].Owner = Side.Ai;
            return Create(board, StarterDeck(), StarterDeck(), seed);
        }

        /// <param name="shuffle">false 면 두 덱을 목록 순서대로 뽑는다 (튜토리얼)</param>
        public static Game Create(Board board, IEnumerable<Card> playerDeck, IEnumerable<Card> aiDeck, int seed, StageRules rules = null, bool shuffle = true)
        {
            var rng = new Rng(seed);
            rules ??= new StageRules();
            return new Game
            {
                Board = board,
                Current = Side.Player,
                Rules = rules,
                _rng = rng,
                // 첫 손패: 플레이어는 0턴, AI 는 1턴에 쓴다
                _player = new Deck(playerDeck, rng, shuffle, holdLock: 0 < rules.LockFromTurn),
                _ai = new Deck(aiDeck, rng, shuffle, holdLock: 1 < rules.LockFromTurn),
            };
        }

        /// <summary>기본 덱 21장: 유닛 12 (원소별 4) + 스킬 9 (왜곡·잠금·탈색·지뢰·재점화·거점 1장씩, 원소 변환 3색).</summary>
        public static List<Card> StarterDeck()
        {
            var list = new List<Card>();
            foreach (var e in Elements.Base)
                for (var i = 0; i < 4; i++) list.Add(Card.Unit(e));
            foreach (var s in new[] { Skill.Warp, Skill.Lock, Skill.Bleach, Skill.Mine, Skill.Echo, Skill.Beacon })
                list.Add(Card.Of(s));
            foreach (var e in Elements.Base) list.Add(Card.Transmute(e));
            return list;
        }

        public Game Clone()
        {
            var g = new Game
            {
                Board = Board.Clone(),
                Current = Current,
                Turn = Turn,
                Round = Round,
                Rules = Rules,
                Stuck = Stuck,
                SkillUsed = SkillUsed,
                _rng = _rng.Clone(),
                _player = _player.Clone(),
                _ai = _ai.Clone(),
                _playerStats = _playerStats.Clone(),
                _aiStats = _aiStats.Clone(),
                _bonusTurn = _bonusTurn,
                _levels = _levels,
                TurnsTaken = TurnsTaken,
            };
            foreach (var o in Objectives) g.Objectives.Add(o.Clone());
            return g;
        }

        /// <summary>플레이어의 게임 중 목표를 정한다 (판 시작 때).</summary>
        public void SetObjectives(IEnumerable<ObjectiveDef> defs)
        {
            Objectives.Clear();
            foreach (var d in defs) Objectives.Add(new Objective(d));
        }

        public MatchStats StatsOf(Side s) => s == Side.Player ? _playerStats : _aiStats;

        /// <summary>그 쪽 카드의 강화 단계 (AI 는 1).</summary>
        public int LevelOf(Side s, Card c) => s == Side.Player && _levels != null && _levels.TryGetValue(c, out var n) ? n : 1;

        /// <summary>판 시작 때 플레이어의 강화 단계와 소모품을 적용한다 (목표 추가는 StageDef.CreateGame 이 목표를 뽑을 때).</summary>
        public void ApplyLoadout(Loadout l)
        {
            _levels = l.Levels;
            if (l.Boosts.Contains(Boost.Pioneer)) PioneerCells.AddRange(ClaimFrontier(Side.Player, BoostShop.PioneerCells));
            if (l.Boosts.Contains(Boost.FirstStrike)) _bonusTurn = true;
        }

        /// <summary>viewer 가 알 수 있는 정보만 남긴 복제본 (상대 지뢰 제거). AI 시뮬레이션용.</summary>
        public Game CloneFor(Side viewer)
        {
            var g = Clone();
            g.Board.HideMinesFrom(viewer);
            return g;
        }

        /// <summary>자기 차례에 유닛을 놓을 칸이 하나도 없어 판을 끝낸 쪽 (없으면 None).</summary>
        public Side Stuck { get; private set; }

        public int MaxTurns => Rules.MaxTurns;

        /// <summary>양측 합산 MaxTurns 가 끝났거나, 차례인 쪽이 놓을 칸이 없으면 그 자리에서 끝 (점유 칸 수로 승패).</summary>
        public bool IsOver => Turn >= MaxTurns || Stuck != Side.None;

        public Deck DeckOf(Side s) => s == Side.Player ? _player : _ai;

        public Side Winner
        {
            get
            {
                var p = Board.Count(Side.Player);
                var a = Board.Count(Side.Ai);
                return p > a ? Side.Player : a > p ? Side.Ai : Side.None;
            }
        }

        public bool CanPlace(Side s, Cell c)
        {
            if (!Board.InBounds(c)) return false;
            ref var t = ref Board[c];
            if (t.Blocked || t.HasUnit || t.Lock > 0 || t.IsProtectedFrom(s)) return false;
            return Board.IsFrontline(c, s);
        }

        public List<Cell> LegalCells(Side s)
        {
            var list = new List<Cell>();
            foreach (var c in Board.Cells())
                if (CanPlace(s, c)) list.Add(c);
            return list;
        }

        /// <summary>스킬 카드의 (첫 번째) 대상으로 c 를 고를 수 있는가. 스킬은 거리 제한이 없고, 턴마다 한 장만 쓴다.</summary>
        public bool CanTarget(Side s, Card card, Cell c)
        {
            if (SkillUsed || card.IsUnit || !Board.InBounds(c)) return false;
            ref var t = ref Board[c];
            if (t.Blocked) return false;
            return card.Skill switch
            {
                Skill.Warp => !t.IsProtectedFrom(s),
                Skill.Lock => Turn >= Rules.LockFromTurn, // 초반에는 못 쓴다 (손패에도 안 들어오지만 만일을 위해)
                Skill.Bleach => t.HasUnit && t.Unit != Element.Grey && !t.IsProtectedFrom(s),
                // 빈 칸이면 어디든. 상대 지뢰가 있는지는 모르므로 막지 않는다 (덮어쓴다). 내 지뢰 위에는 못 묻는다.
                Skill.Mine => !t.HasUnit && t.Mine != s,
                Skill.Transmute => (t.Unit.IsBase() || t.Unit == Element.Grey) && t.Unit != card.Element && !t.IsProtectedFrom(s),
                Skill.Echo => t.Unit.IsBase() && t.UnitOwner == s,
                Skill.Beacon => t.Owner == s && t.Beacon == Side.None,
                _ => false,
            };
        }

        public bool CanTarget(Side s, Skill skill, Cell c) => CanTarget(s, Card.Of(skill), c);

        public bool CanCast(Side s, Card card, Cell a, Cell b) =>
            CanTarget(s, card, a) && (card.Skill != Skill.Warp || (a != b && CanTarget(s, card, b)));

        /// <summary>
        /// 현재 차례인 쪽이 손패 handIndex 의 유닛을 at 에 놓는다. 물의 방향을 null 로 주면 가장 많이 먹는 방향을 고른다.
        /// </summary>
        public PlayResult Play(int handIndex, Cell at, Cell? blueDir = null)
        {
            var deck = DeckOf(Current);
            var element = deck.Hand[handIndex].Element;
            var dir = blueDir ?? (element == Element.Blue ? BestBlueDirection(handIndex, at) : Cell.Orthogonal[0]);
            deck.Play(handIndex);
            var r = Resolver.PlaceUnit(Board, _rng, Current, at, element, dir, LevelOf(Current.Opponent(), Card.Of(Skill.Mine)));
            LastResult = r;
            AfterResolve(r);
            var extra = r.ExtraTurn;
            if (!extra && _bonusTurn && Current == Side.Player)
            {
                extra = true;
                _bonusTurn = false;
                BonusTurnTaken = true;
            }
            EndTurn(extra);
            return r;
        }

        /// <summary>
        /// 손패 handIndex 의 스킬을 쓴다. 턴은 끝나지 않는다 (이어서 유닛을 놓거나 패 버리기). 턴마다 한 장.
        /// 물 유닛 재점화는 가장 많이 먹는 방향으로 흐른다.
        /// </summary>
        public PlayResult Cast(int handIndex, Cell target, Cell target2 = default)
        {
            var deck = DeckOf(Current);
            var card = deck.Hand[handIndex];
            var echoDir = card.Skill == Skill.Echo && Board[target].Unit == Element.Blue ? BestEchoDirection(target) : Cell.Orthogonal[0];
            deck.Play(handIndex);
            var r = Resolver.CastSkill(Board, _rng, Current, card, target, target2, echoDir, LevelOf(Current, card));
            LastResult = r;
            SkillUsed = true;
            AfterResolve(r);
            CheckStuck(); // 공간 왜곡으로 내 전선이 사라졌을 수도 있다
            return r;
        }

        private Cell BestEchoDirection(Cell at)
        {
            var best = Cell.Orthogonal[0];
            var bestScore = int.MinValue;
            foreach (var d in Cell.Orthogonal)
            {
                var b = Board.Clone();
                Resolver.CastSkill(b, _rng.Clone(), Current, Card.Of(Skill.Echo), at, default, d, LevelOf(Current, Card.Of(Skill.Echo)));
                var score = b.Count(Current) - b.Count(Current.Opponent());
                if (score > bestScore)
                {
                    bestScore = score;
                    best = d;
                }
            }
            return best;
        }

        public void Mulligan()
        {
            DeckOf(Current).Mulligan(_rng, HoldLockFor(false));
            LastResult = null;
            ObjectiveEvents.Clear();
            BonusTurnTaken = false;
            EndTurn(false);
        }

        public Cell BestBlueDirection(int handIndex, Cell at)
        {
            var best = Cell.Orthogonal[0];
            var bestScore = int.MinValue;
            var me = Current;
            foreach (var d in Cell.Orthogonal)
            {
                var sim = CloneFor(me);
                sim.Play(handIndex, at, d);
                var score = sim.Board.Count(me) - sim.Board.Count(me.Opponent());
                if (score > bestScore)
                {
                    bestScore = score;
                    best = d;
                }
            }
            return best;
        }

        /// <summary>수 하나를 기록에 더하고, 플레이어의 게임 중 목표를 확인한다.</summary>
        private void AfterResolve(PlayResult r)
        {
            ObjectiveEvents.Clear();
            BonusTurnTaken = false;
            Tally(r);
            CheckObjectives();
        }

        private void Tally(PlayResult r)
        {
            var s = StatsOf(r.Actor);
            if (r.Skill != Skill.None) s.Add(Stat.Skills);
            if (r.MineTriggered)
            {
                // 밟은 쪽이 아니라 지뢰 주인의 기록
                s = StatsOf(r.Actor.Opponent());
                s.Add(Stat.MinesBlown);
            }
            if (r.Fused != Element.None)
            {
                s.Add(Stat.Fusions);
                if (r.Fused.Tier() == 3) s.Add(Stat.Tier3);
                if (r.StoleCatalyst) s.Add(Stat.Steals);
            }
            if (r.Resonance) s.Add(Stat.Resonances);
            s.Add(Stat.Captured, r.Captured.Count);
            s.BestMove = Math.Max(s.BestMove, r.Captured.Count);
        }

        /// <summary>목표의 현재 진행 값.</summary>
        public int ProgressOf(ObjectiveDef d)
        {
            var s = _playerStats;
            return d.Goal switch
            {
                ObjectiveGoal.Fusions => s[Stat.Fusions],
                ObjectiveGoal.Resonances => s[Stat.Resonances],
                ObjectiveGoal.Steals => s[Stat.Steals],
                ObjectiveGoal.Tier3 => s[Stat.Tier3],
                ObjectiveGoal.BigMove => s.BestMove,
                ObjectiveGoal.Skills => s[Stat.Skills],
                ObjectiveGoal.Share => Board.Count(Side.Player) * 100 / Math.Max(1, Board.Playable),
                ObjectiveGoal.MinesBlown => s[Stat.MinesBlown],
                ObjectiveGoal.Captured => s[Stat.Captured],
                _ => 0,
            };
        }

        public int ObjectivesDone
        {
            get
            {
                var n = 0;
                foreach (var o in Objectives)
                    if (o.Done) n++;
                return n;
            }
        }

        /// <summary>달성한 목표의 보상을 바로 낸다. 보상으로 다른 목표(점유율)가 이뤄질 수 있어 더 없을 때까지 반복한다.</summary>
        private void CheckObjectives()
        {
            if (IsOver) return;
            bool again;
            do
            {
                again = false;
                foreach (var o in Objectives)
                {
                    if (o.Done || ProgressOf(o.Def) < o.Def.Target) continue;
                    o.Done = true;
                    ObjectiveEvents.Add(new ObjectiveEvent(o.Def, ApplyReward(o.Def)));
                    again = true;
                }
            } while (again);
        }

        private List<Cell> ApplyReward(ObjectiveDef d)
        {
            const Side me = Side.Player;
            var cells = new List<Cell>();
            switch (d.Reward)
            {
                case ObjectiveReward.Claim:
                    cells.AddRange(ClaimFrontier(me, d.Amount));
                    break;
                case ObjectiveReward.Shield:
                    foreach (var c in Board.Cells())
                    {
                        if (Board[c].Owner != me) continue;
                        foreach (var dir in Cell.Orthogonal)
                        {
                            var n = c + dir;
                            if (!Board.InBounds(n) || Board[n].Blocked || Board[n].Owner == me) continue;
                            Board[c].Shield = Math.Max(Board[c].Shield, ObjectiveCatalog.ShieldTicks);
                            cells.Add(c);
                            break;
                        }
                    }
                    break;
                case ObjectiveReward.Beacon:
                    Cell? best = null;
                    var bestOpen = 0;
                    foreach (var c in Board.Cells())
                    {
                        if (Board[c].Owner != me || Board[c].Beacon != Side.None) continue;
                        var open = 0;
                        foreach (var dir in Cell.Around8)
                        {
                            var n = c + dir;
                            if (Board.InBounds(n) && !Board[n].Blocked && Board[n].Owner != me) open++;
                        }
                        if (open > bestOpen)
                        {
                            bestOpen = open;
                            best = c;
                        }
                    }
                    if (best is { } b)
                    {
                        Board[b].Beacon = me;
                        cells.Add(b);
                    }
                    break;
                case ObjectiveReward.ExtraTurn:
                    _bonusTurn = true;
                    break;
            }
            return cells;
        }

        /// <summary>한 칸씩 n 칸: 내 땅과 맞닿은 칸 중 상대 땅을 먼저, 없으면 빈 땅.</summary>
        private List<Cell> ClaimFrontier(Side me, int n)
        {
            var cells = new List<Cell>();
            for (var i = 0; i < n; i++)
            {
                var enemy = new List<Cell>();
                var neutral = new List<Cell>();
                foreach (var c in Board.Cells())
                {
                    ref var t = ref Board[c];
                    if (t.Blocked || t.Owner == me || t.IsProtectedFrom(me) || !Board.IsFrontline(c, me)) continue;
                    (t.Owner == Side.None ? neutral : enemy).Add(c);
                }
                var pool = enemy.Count > 0 ? enemy : neutral;
                if (pool.Count == 0) break;
                var pick = pool[_rng.Next(pool.Count)];
                Board[pick].SetOwner(me);
                cells.Add(pick);
            }
            return cells;
        }

        private void EndTurn(bool extraTurn)
        {
            TurnsTaken++;
            DeckOf(Current).Refill(_rng, HoldLockFor(extraTurn));
            Board.TickTimers();
            SkillUsed = false;
            if (!extraTurn)
            {
                Turn++;
                Current = Current.Opponent();
                if (Current == Side.Player) EndRound();
            }
            CheckStuck();
        }

        /// <summary>
        /// 지금 채우는 손패는 이 쪽의 다음 차례에 쓰인다: 보통은 Turn + 2, 추가 턴이면 지금 바로(Turn).
        /// 그때가 아직 LockFromTurn 전이면 영역 잠금을 뽑지 않는다 (시작하자마자 상대 시작 땅을 잠그는 것을 막는다).
        /// </summary>
        private bool HoldLockFor(bool extraTurn) => (extraTurn ? Turn : Turn + 2) < Rules.LockFromTurn;

        /// <summary>
        /// 놓을 칸이 없으면 판을 끝낸다. 단, 영역 잠금은 곧 풀리므로 잠금 때문에만 막힌 것은 치지 않는다
        /// (그 턴에는 스킬을 쓰거나 패 버리기를 하면 된다).
        /// </summary>
        private void CheckStuck()
        {
            if (Turn >= MaxTurns || Stuck != Side.None) return;
            foreach (var c in Board.Cells())
                if (CanPlaceIgnoringLock(Current, c)) return;
            Stuck = Current;
        }

        private bool CanPlaceIgnoringLock(Side s, Cell c)
        {
            ref var t = ref Board[c];
            if (t.Blocked || t.HasUnit || t.IsProtectedFrom(s)) return false;
            return Board.IsFrontline(c, s);
        }

        /// <summary>라운드 끝 기믹: 컨베이어 이동 → 블랙홀 흡입 → 거점 확장 → (주기마다) 화산 분출.</summary>
        private void EndRound()
        {
            Round++;
            RoundEvents.Clear();
            MoveConveyors();
            SuckBlackHoles();
            GrowBeacons();
            if (Rules.LavaEvery > 0 && Round % Rules.LavaEvery == 0) EruptLava();
        }

        /// <summary>컨베이어 칸의 내용이 +X 로 한 칸 이동한다. 벨트 끝의 내용은 떨어져 사라진다.</summary>
        private void MoveConveyors()
        {
            for (var y = 0; y < Board.Height; y++)
                for (var x = Board.Width - 1; x >= 0; x--)
                {
                    var c = new Cell(x, y);
                    if (Board[c].Terrain != Terrain.Conveyor) continue;
                    var next = new Cell(x + 1, y);
                    if (Board.InBounds(next) && Board[next].Terrain == Terrain.Conveyor)
                    {
                        Board[c].SwapContents(ref Board[next]);
                    }
                    else
                    {
                        Board[c].ClearContents();
                        RoundEvents.Add(new RoundEvent(RoundEventKind.Conveyor, c));
                    }
                }
        }

        /// <summary>블랙홀 주변 8칸을 비운다 (유닛·소유가 빨려 들어감). 보호된 칸은 버틴다.</summary>
        private void SuckBlackHoles()
        {
            foreach (var hole in Board.Cells())
            {
                if (Board[hole].Terrain != Terrain.BlackHole) continue;
                foreach (var d in Cell.Around8)
                {
                    var c = hole + d;
                    if (!Board.InBounds(c) || Board[c].Blocked || Board[c].Shield > 0) continue;
                    if (Board[c].Owner == Side.None && !Board[c].HasUnit) continue;
                    Board[c].ClearContents();
                    RoundEvents.Add(new RoundEvent(RoundEventKind.BlackHole, c));
                }
            }
        }

        /// <summary>
        /// 화산 분출: 양쪽에서 각각 무작위 유닛 LavaCount 개가 터진다. 유닛은 사라지고 그 칸은 주인 없는 땅이 된다.
        /// 한 번 터지고 끝이라 칸이 막히지는 않는다. 보호된 칸의 유닛은 버틴다.
        /// </summary>
        private void EruptLava()
        {
            foreach (var side in new[] { Side.Player, Side.Ai })
            {
                var pool = new List<Cell>();
                foreach (var c in Board.Cells())
                    if (Board[c].HasUnit && Board[c].UnitOwner == side && Board[c].Shield == 0) pool.Add(c);
                for (var i = 0; i < Rules.LavaCount && pool.Count > 0; i++)
                {
                    var k = _rng.Next(pool.Count);
                    var c = pool[k];
                    pool.RemoveAt(k);
                    Board[c].Unit = Element.None;
                    Board[c].UnitOwner = Side.None;
                    Board[c].SetOwner(Side.None);
                    RoundEvents.Add(new RoundEvent(RoundEventKind.Lava, c));
                }
            }
        }

        /// <summary>
        /// 거점마다 주변 8칸 중 아직 내 것이 아닌 칸을 BeaconClaims × 강화 단계 개 무작위로 점유한다 (상대 땅 포함, 보호된 칸은 못 함).
        /// 칸 주인이 바뀌면 거점은 이미 무너졌다 (Tile.SetOwner). 거점끼리는 칸 순서대로 처리한다.
        /// </summary>
        private void GrowBeacons()
        {
            var beacons = new List<Cell>();
            foreach (var c in Board.Cells())
                if (Board[c].Beacon != Side.None) beacons.Add(c);
            foreach (var at in beacons)
            {
                var side = Board[at].Beacon;
                if (side == Side.None) continue; // 앞선 거점이 이 칸을 빼앗았다
                if (Board[at].Owner != side)
                {
                    Board[at].Beacon = Side.None;
                    continue;
                }
                var pool = new List<Cell>();
                foreach (var d in Cell.Around8)
                {
                    var c = at + d;
                    if (Board.InBounds(c) && !Board[c].Blocked && Board[c].Owner != side && !Board[c].IsProtectedFrom(side)) pool.Add(c);
                }
                var claims = Balance.BeaconClaims * LevelOf(side, Card.Of(Skill.Beacon));
                for (var i = 0; i < claims && pool.Count > 0; i++)
                {
                    var k = _rng.Next(pool.Count);
                    Board[pool[k]].SetOwner(side);
                    RoundEvents.Add(new RoundEvent(RoundEventKind.Beacon, pool[k]));
                    pool.RemoveAt(k);
                }
            }
        }
    }
}
