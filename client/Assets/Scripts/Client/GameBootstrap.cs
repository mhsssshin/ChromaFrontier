using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    public enum AppScreen { Splash, Lobby, Battle, Tower, Login, PvpLobby, PvpRoom }

    /// <summary>대기실 위에 뜨는 창.</summary>
    public enum LobbyPanel { None, Deck, Quest, Prep, Road, Account }

    /// <summary>
    /// 씬의 유일한 진입점 (씬에는 카메라 + 이것뿐). 대기실(스테이지 선택) ↔ 전투를 오가며,
    /// 전투에서는 플레이어 입력과 AI 차례를 Game 에 전달하고 매 프레임 BoardView 를 동기화한다. HUD는 IMGUI (Hud).
    /// 입력은 GameBootstrap.Input.cs, 메시지·이펙트는 GameBootstrap.Effects.cs.
    /// </summary>
    public sealed partial class GameBootstrap : MonoBehaviour
    {
        private const float AiDelay = 0.8f;

        public AppScreen Mode { get; private set; } = AppScreen.Lobby;
        public Game Game { get; private set; }
        public StageDef Stage { get; private set; }
        public int Selected { get; private set; } = -1;
        public string Message { get; private set; } = "";
        public HandView Hand { get; } = new();
        public int LastStars { get; private set; }
        public int LastGems { get; private set; }        // 직전 판에서 받은 결정
        public bool LastFirstClear { get; private set; }
        public bool LastNewMode { get; private set; }    // 직전 판으로 다음 모드가 열렸다
        public float ModeHintUntil { get; private set; } // 대기실: 아직 열린 모드가 하나뿐일 때 안내를 이 시각까지
        public readonly Dictionary<string, float> ObjectiveFlash = new(); // 게임 중 목표를 이룬 시각 (HUD 반짝임)

        // 대기실 창 (덱 편성·퀘스트)
        public LobbyPanel Panel { get; private set; }
        public string PanelMessage { get; set; } = "";
        public readonly HashSet<Boost> Boosts = new(); // 출격 준비 창에서 고른 소모품 (출격할 때 값을 낸다)

        // 무르기: 내 차례가 시작될 때마다 판을 복제해 둔다
        private readonly List<Game> _history = new();
        private int _snapKey = -1;

        // 대기실
        public int LobbyStage { get; private set; }
        public int LobbyChapter => LobbyStage / StageCatalog.PerChapter;

        private BoardView _view;
        private FxView _fx;
        private Board _previewBoard;
        private float _shake;
        private Camera _cam;
        private Rng _aiRng;
        private float _aiTimer;
        private Cell? _hover;
        private Cell? _warpFirst;
        private Cell? _blueAt, _blueTap; // 물: 미끄러진 뒤 물이 흐를 칸 / 실제로 고른 칸
        private int _lastRound;
        private bool _recorded;
        private readonly Highlight _hl = new();
        private float _camSize;
        private Vector3 _camPos;

        private void Start()
        {
            _cam = Camera.main;
            _cam.backgroundColor = Palette.Background;
            // 개발 빌드: 주소에 ?unlock 을 붙이면 전체 스테이지 해금 (대기실에서 U 키도 같음), ?reset 은 진행 초기화
            if (Debug.isDebugBuild && Application.absoluteURL.Contains("reset")) Progress.ResetAll();
            if (Debug.isDebugBuild && Application.absoluteURL.Contains("unlock")) Progress.UnlockAll();
            // 아주 옛 버전이 PlayerPrefs 에만 남긴 값을 클라우드에 올리기 전에 localStorage 로 옮겨 둔다 (읽으면 옮겨진다)
            _ = Progress.AllStars() + Progress.Gems + (Progress.TutorialDone ? 1 : 0);
            Online.Reloaded += OnReloaded;
            Online.Start();
            ShowSplash();
        }

        // ---------------------------------------------------------------- 화면 전환

        /// <summary>스플래시가 뜬 뒤 흐른 시간 (등장 애니메이션·입력 대기에 쓴다).</summary>
        public float SplashTime { get; private set; }

        /// <summary>
        /// 게임 스플래시: 엠블럼·제목 아래로 원소 유닛들이 놓인 장식 보드가 떠 있고, 이따금 반짝인다.
        /// 터치하면 처음 실행은 튜토리얼로, 아니면 대기실로.
        /// </summary>
        private void ShowSplash()
        {
            Mode = AppScreen.Splash;
            SplashTime = 0f;
            BuildSplashBoard();
        }

        /// <summary>스플래시·로그인 화면 뒤의 장식 보드.</summary>
        private void BuildSplashBoard()
        {
            _previewBoard = Board.Parse(
                "....AA",
                "...AAA",
                "...A..",
                "..P...",
                "PPP...",
                "PP....");
            void Unit(int x, int y, Element e, Side side)
            {
                var c = new Cell(x, y);
                _previewBoard[c].Unit = e;
                _previewBoard[c].UnitOwner = side;
                _previewBoard[c].Owner = side;
            }
            Unit(1, 4, Element.Red, Side.Player);
            Unit(2, 3, Element.Blue, Side.Player);
            Unit(0, 5, Element.Yellow, Side.Player);
            Unit(2, 2, Element.White, Side.Player);
            Unit(4, 1, Element.Purple, Side.Ai);
            Unit(3, 2, Element.Orange, Side.Ai);
            Unit(5, 0, Element.Green, Side.Ai);
            _view?.Destroy();
            _fx?.Destroy();
            _view = new BoardView(transform, _previewBoard.Width, _previewBoard.Height);
            _fx = new FxView(transform);
            SnapCamera();
        }

        public void FinishSplash()
        {
            if (Mode != AppScreen.Splash || SplashTime < 0.8f) return;
            if (NeedLogin) ShowLogin();
            else EnterGame();
        }

        private void UpdateSplash()
        {
            SplashTime += Time.deltaTime;
            // IMGUI 이벤트가 빠지는 경우(웹에서 아주 빠른 탭 등)에도 넘어가도록 입력도 직접 확인
            if (Mode == AppScreen.Splash && (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown)) FinishSplash();
            if (Mode == AppScreen.Login) UpdateLogin();
            if (Mode != AppScreen.Splash && Mode != AppScreen.Login) return;
            _hl.Clear();
            _view.Sync(_previewBoard, _hl, Time.time, Time.deltaTime);
            // 0.7초마다 유닛 하나가 반짝인다
            if (Mathf.FloorToInt(SplashTime / 0.7f) != Mathf.FloorToInt((SplashTime - Time.deltaTime) / 0.7f))
            {
                var units = new List<Cell>();
                foreach (var c in _previewBoard.Cells())
                    if (_previewBoard[c].HasUnit) units.Add(c);
                var pick = units[Random.Range(0, units.Count)];
                var color = Palette.Of(_previewBoard[pick].Unit);
                _fx.Spawn(FxView.Sparkle, _view.ToWorld(pick) + new Vector3(0f, 0.5f, 0f), Color.Lerp(color, Color.white, 0.5f), 1.1f);
                _fx.Spawn(FxView.Ring, _view.ToWorld(pick), Color.Lerp(color, Color.white, 0.3f), 1.2f, 0.05f);
            }
            _fx.Update(Time.deltaTime);
            FitCamera();
        }

        /// <summary>화면별로 보드가 들어갈 세로 띠 (화면 위에서부터 비율).</summary>
        private void Band(out float top, out float bottom)
        {
            switch (Mode)
            {
                case AppScreen.Splash: top = 0.5f; bottom = 0.88f; break;
                case AppScreen.Login: top = 0.75f; bottom = 0.98f; break;
                case AppScreen.PvpRoom: top = 0.08f; bottom = 0.34f; break;
                case AppScreen.Lobby: top = 0.17f; bottom = 0.485f; break;
                case AppScreen.Tower: top = 0.13f; bottom = 0.43f; break;
                default: top = 0.27f; bottom = 0.75f; break; // 위쪽 HUD 아래에 게임 중 목표 줄이 있다
            }
        }

        public void ShowLobby(int stage)
        {
            Mode = AppScreen.Lobby;
            Panel = LobbyPanel.None;
            Tutorial = false;
            Pvp = null;
            Palette.Viewer = Side.Player;
            Online.WatchRooms(false);
            EndCine();
            _tips.Clear();
            Game = null;
            _fx?.Destroy();
            _fx = null;
            SelectLobbyStage(stage);
        }

        /// <summary>대기실에서 스테이지를 고르면 그 맵을 보드로 미리 보여 준다.</summary>
        public void SelectLobbyStage(int index)
        {
            LobbyStage = Mathf.Clamp(index, 0, StageCatalog.Count - 1);
            _view?.Destroy();
            _previewBoard = Board.Parse(StageCatalog.Get(LobbyStage).Map);
            _view = new BoardView(transform, _previewBoard.Width, _previewBoard.Height);
            SnapCamera();
        }

        public void SelectLobbyChapter(int chapter)
        {
            var first = chapter * StageCatalog.PerChapter;
            if (!Progress.IsUnlocked(first)) return;
            // 그 챕터에서 해금된 마지막 스테이지
            var last = Mathf.Min(first + StageCatalog.PerChapter, Progress.Unlocked) - 1;
            SelectLobbyStage(last);
        }

        public void OpenPanel(LobbyPanel p)
        {
            Panel = p;
            PanelMessage = "";
            if (p == LobbyPanel.Road) RoadPage = FirstOpenRoadStep() / RoadPerPage;
        }

        // ---------------------------------------------------------------- 별의 길

        public const int RoadPerPage = 7;
        public int RoadTab { get; set; }  // 0 보상, 1 보드 테마
        public int RoadPage { get; set; }

        /// <summary>받을 수 있거나 아직 못 이른 첫 단계 (별의 길을 열면 그 쪽으로).</summary>
        private static int FirstOpenRoadStep()
        {
            for (var i = 0; i < StarRoad.Steps.Length; i++)
                if (!Progress.RoadClaimed(i)) return i;
            return StarRoad.Steps.Length - 1;
        }

        /// <summary>보드 테마를 바꾸고 대기실 미리보기를 다시 그린다.</summary>
        public void UseTheme(string id)
        {
            Progress.Theme = id;
            SelectLobbyStage(LobbyStage);
        }

        /// <summary>대기실 모드 버튼: 열린 모드끼리 돌아가며 바꾼다. 하나뿐이면 여는 방법을 잠깐 보여 준다.</summary>
        public void CycleMode()
        {
            var next = (Progress.Mode + 1) % StageCatalog.Modes;
            if (!Progress.ModeUnlocked(next)) next = 0;
            if (next == Progress.Mode)
            {
                ModeHintUntil = Time.time + 3f;
                return;
            }
            Progress.Mode = next;
            SelectLobbyStage(Mathf.Min(Progress.Unlocked - 1, StageCatalog.Count - 1));
        }

        /// <summary>새로 열린 모드의 1-1 (결과 창).</summary>
        public void StartNewMode() => StartStage(0);

        public AiLevel StageLevel(StageDef s) => StageCatalog.LevelForMode(s, Progress.Mode);

        public int BoostCost
        {
            get
            {
                var n = 0;
                foreach (var b in BoostShop.All)
                    if (Boosts.Contains(b.Boost)) n += b.Price;
                return n;
            }
        }

        /// <summary>출격 준비 창의 출격: 고른 소모품 값을 내고 판을 시작한다.</summary>
        public void Sortie()
        {
            if (!Progress.IsUnlocked(LobbyStage)) return;
            if (!Progress.SpendGems(BoostCost))
            {
                PanelMessage = $"결정이 {BoostCost - Progress.Gems}개 모자라요. 소모품을 빼고 출격하세요.";
                return;
            }
            var loadout = new Loadout { Levels = Progress.Cards.Levels() };
            foreach (var b in Boosts) loadout.Boosts.Add(b);
            Boosts.Clear();
            Panel = LobbyPanel.None;
            StartStage(LobbyStage, loadout);
        }

        /// <param name="loadout">없으면 카드 강화만 (다시·다음 스테이지는 소모품 없이)</param>
        public void StartStage(int index, Loadout loadout = null)
        {
            if (!Progress.IsUnlocked(index)) return;
            Tutorial = false;
            var stage = StageCatalog.Get(index);
            var seed = System.Environment.TickCount;
            loadout ??= new Loadout { Levels = Progress.Cards.Levels() };
            BeginBattle(stage, stage.CreateGame(seed, Progress.Cards.BuildDeck(), loadout));
            _aiRng = new Rng(seed * 31 + 7);
            Message = $"{Stage.Code} {Stage.Name} — {Stage.MapNote}";
            var used = new List<string>();
            foreach (var b in BoostShop.All)
                if (loadout.Boosts.Contains(b.Boost)) used.Add(b.Name);
            if (used.Count > 0) Message = $"{Stage.Code} {Stage.Name}\n소모품: " + string.Join(" · ", used);
            if (Game.PioneerCells.Count > 0) _view.Flash(Game.PioneerCells);
            QueueTips(stage);
        }

        /// <summary>되돌아갈 판: 이번 차례에 스킬을 썼으면 이번 차례 처음, 아니면 내 지난 차례 처음. 없으면 -1.</summary>
        private int UndoTarget
        {
            get
            {
                if (_history.Count == 0) return -1;
                // 마지막 복제 뒤로 차례가 넘어갔으면(판이 끝난 마지막 수) 그 복제가, 아니면 스킬을 썼을 때만 이번 차례 처음이 대상
                var moved = _history[_history.Count - 1].TurnsTaken != Game.TurnsTaken;
                return moved || Game.SkillUsed ? _history.Count - 1 : _history.Count - 2;
            }
        }

        /// <summary>무르기를 쓸 수 있는가. 첨탑에서는 공짜이고, 판이 끝난 뒤(실패)에도 마지막 수를 무를 수 있다.</summary>
        public bool CanUndo => !Tutorial && Pvp == null && UndoTarget >= 0 && (Puzzle != null
            ? Mode == AppScreen.Battle && ActiveTip == null && !CineActive
            : PlayerTurn && Progress.Undos > 0);

        /// <summary>무르기: 내 지난 수(와 그 뒤 AI 의 수)를 되돌린다. 무르기 1회를 쓴다.</summary>
        public void Undo()
        {
            if (!CanUndo) return;
            var i = UndoTarget;
            if (Puzzle == null && !Progress.UseUndo()) return; // 첨탑에서는 공짜
            if (Puzzle != null) _towerUndo = true;              // 대신 '무르기 없이' 별은 못 받는다
            _history.RemoveRange(i + 1, _history.Count - i - 1);
            Game = _history[i].Clone();
            _snapKey = Game.TurnsTaken;
            _recorded = false; // 첨탑: 실패한 판을 물렀으면 다시 기록할 수 있게
            _lastRound = Game.Round;
            Deselect();
            Message = Puzzle != null ? "한 수 물렀어요." : $"무르기: 내 지난 수 전으로 돌아갔어요 (남은 무르기 {Progress.Undos})";
        }

        private void BeginBattle(StageDef stage, Game game)
        {
            Mode = AppScreen.Battle;
            Puzzle = null;
            Pvp = null;
            Palette.Viewer = Side.Player;
            Stage = stage;
            Game = game;
            _aiRng = new Rng(1);
            _view?.Destroy();
            _fx?.Destroy();
            _view = new BoardView(transform, Game.Board.Width, Game.Board.Height);
            _fx = new FxView(transform);
            Hand.Reset();
            Deselect();
            EndCine();
            _lastRound = 0;
            _recorded = false;
            LastStars = 0;
            LastGems = 0;
            LastFirstClear = false;
            LastNewMode = false;
            ObjectiveFlash.Clear();
            _history.Clear();
            _snapKey = -1;
            SnapCamera();
        }

        public void Retry()
        {
            if (Puzzle != null)
            {
                StartFloor(TowerFloor);
                return;
            }
            // 방금 다음 모드가 열려 모드가 바뀌었으면 그 스테이지는 아직 잠겨 있다
            if (Progress.IsUnlocked(Stage.Index)) StartStage(Stage.Index);
            else ExitToLobby();
        }

        public void NextStage()
        {
            if (Puzzle != null) StartFloor(Mathf.Min(TowerFloor + 1, PuzzleCatalog.Count - 1));
            else StartStage(Mathf.Min(Stage.Index + 1, StageCatalog.Count - 1));
        }

        public void ExitToLobby()
        {
            if (Pvp != null)
            {
                LeavePvp();
                return;
            }
            if (Puzzle != null)
            {
                ShowTower(TowerFloor);
                return;
            }
            var index = Stage?.Index ?? LobbyStage;
            ShowLobby(index >= 0 && Progress.IsUnlocked(index) ? index : Progress.Unlocked - 1);
        }

        public bool PlayerTurn => Mode == AppScreen.Battle && !BattleOver
            && (Pvp == null ? Game.Current == Side.Player : Game.CurrentSeat == Pvp.MySeat && !Pvp.Log.ContainsKey(Pvp.Applied))
            && !(Tutorial && LessonDone) && ActiveTip == null && Concept == null;

        /// <summary>지금 낼 수 있는 카드인가: 스킬은 한 턴에 한 장.</summary>
        public bool CanUse(Card c) => c.IsUnit || !Game.SkillUsed;

        private Card? SelectedCard => Game != null && Selected >= 0 && Selected < MyDeck.Hand.Count
            ? MyDeck.Hand[Selected]
            : (Card?)null;

        public void Mulligan()
        {
            if (!PlayerTurn) return;
            if (Pvp != null)
            {
                if (!SendPvp(PvpMove.Pass(Pvp.MySeat))) return;
            }
            else Game.Mulligan();
            Deselect();
            AfterMove(Me, default, default);
        }

        // ---------------------------------------------------------------- 프레임

        private void Update()
        {
            Online.Poll();
            if (Mode == AppScreen.Splash || Mode == AppScreen.Login)
            {
                UpdateSplash();
                return;
            }
            if (Mode == AppScreen.PvpLobby || Mode == AppScreen.PvpRoom)
            {
                UpdatePvpScreens();
                if (Mode == AppScreen.PvpRoom && _view != null)
                {
                    _hl.Clear();
                    _view.Sync(_previewBoard, _hl, Time.time, Time.deltaTime);
                    FitCamera();
                }
                if (Mode != AppScreen.Battle) return;
            }
            if (Mode == AppScreen.Lobby || Mode == AppScreen.Tower)
            {
                _hl.Clear();
                _view.Sync(_previewBoard, _hl, Time.time, Time.deltaTime);
                FitCamera();
                return;
            }

            UpdateCine();
            if (Pvp != null) UpdatePvp();
            else if (!Tutorial && !Game.IsOver && Game.Current == Side.Ai)
            {
                _aiTimer -= Time.deltaTime;
                if (_aiTimer <= 0f)
                {
                    var move = AiBrain.Choose(Game, _aiRng, StageLevel(Stage), Stage.Persona);
                    AiBrain.Apply(Game, move);
                    AfterMove(Side.Ai, move.At, move.Target2);
                    if (Game.Current == Side.Player && !Game.IsOver && Game.LegalCells(Side.Player).Count == 0)
                        Message += "\n잠긴 칸 때문에 놓을 곳이 없어요. 스킬을 쓰거나 패 버리기를 하세요.";
                }
            }
            PlayRoundEffects();

            // 무르기용: 내 차례가 새로 시작되면 판을 복제해 둔다
            if (!Tutorial && Pvp == null && !Game.IsOver && Game.Current == Side.Player && Game.TurnsTaken != _snapKey)
            {
                _snapKey = Game.TurnsTaken;
                _history.Add(Game.Clone());
                if (_history.Count > 40) _history.RemoveAt(0);
            }

            if (!Tutorial && Game.IsOver && !_recorded && Puzzle != null)
            {
                _recorded = true;
                LastGems = Progress.RecordFloor(TowerFloor, Game.Solved, !_towerUndo, !_hintFloors.Contains(TowerFloor), out var firstFloor, out var newStars);
                LastFirstClear = firstFloor;
                LastFloorStars = newStars;
                Deselect();
            }
            if (!Tutorial && Pvp == null && Game.IsOver && !_recorded)
            {
                _recorded = true;
                LastStars = StageCatalog.Stars(Game);
                LastGems = Progress.RecordBattle(Stage.Index, LastStars, Game, out var first, out var newMode);
                LastFirstClear = first;
                LastNewMode = newMode;
                Deselect();
            }

            BuildHighlight();
            CineHighlight();
            UpdateAim();
            _view.Sync(Game.Board, _hl, Time.time, Time.deltaTime);
            _fx.Update(Time.deltaTime);
            // 튜토리얼 레슨을 마치면 손패를 비워 둔다 (쓴 카드가 무덤에서 다시 들어와 헷갈리지 않게)
            var hand = Tutorial && LessonDone ? (IReadOnlyList<Card>)System.Array.Empty<Card>() : MyDeck.Hand;
            Hand.Update(hand, Selected, Time.deltaTime, DraggingCard, Hud.ToRef(_pointerGui));
            FitCamera();
        }

        // ---------------------------------------------------------------- 카메라

        /// <summary>
        /// 보드가 HUD 사이의 빈 띠(화면 위에서 top~bottom 비율)에 들어오게 맞춘다.
        /// 전투에서 카드를 고르면 고를 수 있는 칸 쪽으로 자동 확대한다 (최대 1.8배).
        /// </summary>
        private void FitCamera()
        {
            var lobby = Mode != AppScreen.Battle;
            Band(out var top, out var bottom);
            var size = _view.WorldSize;
            var full = Frame(new Vector2(0f, 0f), size + new Vector2(0.8f, 0.6f), top, bottom, out var fullPos);
            var targetSize = full;
            var targetPos = fullPos;

            if (!lobby && (Selected >= 0 || Tutorial) && _hl.Legal.Count > 0)
            {
                var min = new Vector2(float.MaxValue, float.MaxValue);
                var max = new Vector2(float.MinValue, float.MinValue);
                void Grow(Cell c)
                {
                    var p = _view.ToWorld(c);
                    min = Vector2.Min(min, new Vector2(p.x - BoardView.HalfW, p.y - BoardView.HalfH));
                    max = Vector2.Max(max, new Vector2(p.x + BoardView.HalfW, p.y + BoardView.HalfH + 0.4f));
                }
                foreach (var c in _hl.Legal) Grow(c);
                if (_hl.Picked is { } picked) Grow(picked);
                var zoom = Frame((min + max) * 0.5f, max - min + new Vector2(2f, 1.6f), top, bottom, out var zoomPos);
                targetSize = Mathf.Clamp(zoom, full / 1.8f, full);
                targetPos = targetSize >= full - 0.01f ? fullPos : zoomPos;
            }

            // 합성 연출: 합성한 칸으로 클로즈업 (슬로모션 중이라 실제 시간으로 움직인다)
            var cine = CineAmount;
            if (cine > 0f)
            {
                var closeSize = Mathf.Max(Frame(_cineFocus, new Vector2(4.6f, 3.4f), top, bottom, out var closePos), full / 2.4f);
                targetSize = Mathf.Lerp(targetSize, closeSize, cine);
                targetPos = Vector3.Lerp(targetPos, closePos, cine);
            }
            var t = 1f - Mathf.Exp(-(CineActive ? 14f : 7f) * (CineActive ? Time.unscaledDeltaTime : Time.deltaTime));
            _camSize = Mathf.Lerp(_camSize, targetSize, t);
            _camPos = Vector3.Lerp(_camPos, targetPos, t);
            _cam.orthographicSize = _camSize;

            var shake = Vector2.zero;
            if (_shake > 0f)
            {
                _shake -= Time.deltaTime;
                shake = Random.insideUnitCircle * (_shake * 0.6f);
            }
            _cam.transform.position = _camPos + new Vector3(shake.x, shake.y, 0f);
            _view.FitBackground(_cam);
        }

        /// <summary>center 를 중심으로 size(가로·세로) 영역이 화면 띠 [top, bottom] 안에 들어오는 카메라 크기와 위치.</summary>
        private float Frame(Vector2 center, Vector2 size, float top, float bottom, out Vector3 pos)
        {
            var aspect = (float)UnityEngine.Screen.width / Mathf.Max(1, UnityEngine.Screen.height);
            var band = bottom - top;
            var ortho = Mathf.Max(size.x * 0.5f / aspect, size.y * 0.5f / band);
            var mid = (top + bottom) * 0.5f; // 화면 위에서부터 비율
            pos = new Vector3(center.x, center.y - ortho * (1f - 2f * mid), -10f);
            return ortho;
        }

        /// <summary>화면 전환 직후에는 보간 없이 바로 맞춘다.</summary>
        private void SnapCamera()
        {
            _camSize = 0f;
            Band(out var top, out var bottom);
            _camSize = Frame(Vector2.zero, _view.WorldSize + new Vector2(0.8f, 0.6f), top, bottom, out _camPos);
        }

        private void OnGUI() => Hud.Draw(this);
    }
}
