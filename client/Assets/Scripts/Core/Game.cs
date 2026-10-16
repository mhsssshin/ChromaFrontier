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

    public enum RoundEventKind : byte { Lava, BlackHole, Conveyor }

    /// <summary>라운드 끝 기믹이 일으킨 일 (뷰 연출용).</summary>
    public readonly struct RoundEvent
    {
        public readonly RoundEventKind Kind;
        public readonly Cell Cell;
        public RoundEvent(RoundEventKind kind, Cell cell) { Kind = kind; Cell = cell; }
    }

    /// <summary>
    /// 한 판의 권위 상태. 플레이어와 AI가 번갈아 1장씩, 양측 합산 MaxTurns 턴이 끝나면 점유 칸 수로 승패.
    /// 플레이어 → AI 가 한 라운드이고, 라운드가 끝날 때 맵 기믹(컨베이어·블랙홀·용암)이 움직인다.
    /// 뷰는 이 상태를 읽기만 하고, 수는 Play / Cast / Mulligan 으로만 둔다.
    /// </summary>
    public sealed class Game
    {
        public Board Board { get; private set; }
        public Side Current { get; private set; }
        public int Turn { get; private set; }  // 끝난 턴 수 (추가 턴은 세지 않음)
        public int Round { get; private set; } // 끝난 라운드 수
        public StageRules Rules { get; private set; } = new();
        public PlayResult LastResult { get; private set; } // 패 버리기(멀리건)면 null
        public readonly List<RoundEvent> RoundEvents = new(); // 직전 라운드 끝에 일어난 기믹

        private Rng _rng;
        private Deck _player, _ai;

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

        /// <summary>기본 덱 20장: 유닛 12 (원소별 4) + 스킬 8 (종류별 2).</summary>
        public static List<Card> StarterDeck()
        {
            var list = new List<Card>();
            foreach (var e in Elements.Base)
                for (var i = 0; i < 4; i++) list.Add(Card.Unit(e));
            foreach (var s in new[] { Skill.Warp, Skill.Lock, Skill.Bleach, Skill.Mine })
                for (var i = 0; i < 2; i++) list.Add(Card.Of(s));
            return list;
        }

        public Game Clone() => new()
        {
            Board = Board.Clone(),
            Current = Current,
            Turn = Turn,
            Round = Round,
            Rules = Rules,
            Stuck = Stuck,
            _rng = _rng.Clone(),
            _player = _player.Clone(),
            _ai = _ai.Clone(),
        };

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

        /// <summary>스킬의 (첫 번째) 대상으로 c 를 고를 수 있는가. 스킬은 거리 제한이 없다.</summary>
        public bool CanTarget(Side s, Skill skill, Cell c)
        {
            if (!Board.InBounds(c)) return false;
            ref var t = ref Board[c];
            if (t.Blocked) return false;
            return skill switch
            {
                Skill.Warp => !t.IsProtectedFrom(s),
                Skill.Lock => Turn >= Rules.LockFromTurn, // 초반에는 못 쓴다 (손패에도 안 들어오지만 만일을 위해)
                Skill.Bleach => t.HasUnit && t.Unit != Element.Grey && !t.IsProtectedFrom(s),
                // 빈 칸이면 어디든. 상대 지뢰가 있는지는 모르므로 막지 않는다 (덮어쓴다). 내 지뢰 위에는 못 묻는다.
                Skill.Mine => !t.HasUnit && t.Mine != s,
                _ => false,
            };
        }

        public bool CanCast(Side s, Skill skill, Cell a, Cell b) =>
            CanTarget(s, skill, a) && (skill != Skill.Warp || (a != b && CanTarget(s, Skill.Warp, b)));

        /// <summary>
        /// 현재 차례인 쪽이 손패 handIndex 의 유닛을 at 에 놓는다. 물의 방향을 null 로 주면 가장 많이 먹는 방향을 고른다.
        /// </summary>
        public PlayResult Play(int handIndex, Cell at, Cell? blueDir = null)
        {
            var deck = DeckOf(Current);
            var element = deck.Hand[handIndex].Element;
            var dir = blueDir ?? (element == Element.Blue ? BestBlueDirection(handIndex, at) : Cell.Orthogonal[0]);
            deck.Play(handIndex);
            var r = Resolver.PlaceUnit(Board, _rng, Current, at, element, dir);
            LastResult = r;
            EndTurn(r.ExtraTurn);
            return r;
        }

        /// <summary>손패 handIndex 의 스킬을 쓴다. 공간 왜곡은 턴을 끝내지 않는다 (같은 턴에 한 장 더).</summary>
        public PlayResult Cast(int handIndex, Cell target, Cell target2 = default)
        {
            var skill = DeckOf(Current).Play(handIndex).Skill;
            var r = Resolver.CastSkill(Board, Current, skill, target, target2);
            LastResult = r;
            if (skill != Skill.Warp) EndTurn(false);
            else CheckStuck(); // 공간 왜곡으로 내 전선이 사라졌을 수도 있다
            return r;
        }

        public void Mulligan()
        {
            DeckOf(Current).Mulligan(_rng, HoldLockFor(false));
            LastResult = null;
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

        private void EndTurn(bool extraTurn)
        {
            DeckOf(Current).Refill(_rng, HoldLockFor(extraTurn));
            Board.TickTimers();
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

        /// <summary>라운드 끝 기믹: 컨베이어 이동 → 블랙홀 흡입 → (주기마다) 화산 분출.</summary>
        private void EndRound()
        {
            Round++;
            RoundEvents.Clear();
            MoveConveyors();
            SuckBlackHoles();
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
                    Board[c].Owner = Side.None;
                    RoundEvents.Add(new RoundEvent(RoundEventKind.Lava, c));
                }
            }
        }
    }
}
