using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    public enum AppScreen { Splash, Lobby, Battle }

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
            if (!Progress.TutorialDone) StartTutorial(Progress.TutorialLesson);
            else ShowLobby(Mathf.Min(Progress.Unlocked - 1, StageCatalog.Count - 1));
        }

        private void UpdateSplash()
        {
            SplashTime += Time.deltaTime;
            // IMGUI 이벤트가 빠지는 경우(웹에서 아주 빠른 탭 등)에도 넘어가도록 입력도 직접 확인
            if (Input.GetMouseButtonDown(0) || Input.touchCount > 0 || Input.anyKeyDown) FinishSplash();
            if (Mode != AppScreen.Splash) return;
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
                case AppScreen.Lobby: top = 0.17f; bottom = 0.485f; break;
                default: top = 0.215f; bottom = 0.75f; break;
            }
        }

        public void ShowLobby(int stage)
        {
            Mode = AppScreen.Lobby;
            Tutorial = false;
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

        public void CycleLevelOffset() => Progress.LevelOffset = Progress.LevelOffset >= 1 ? -1 : Progress.LevelOffset + 1;

        public AiLevel StageLevel(StageDef s) => StageCatalog.LevelFor(s, Progress.LevelOffset);

        public void StartStage(int index)
        {
            if (!Progress.IsUnlocked(index)) return;
            Tutorial = false;
            var stage = StageCatalog.Get(index);
            var seed = System.Environment.TickCount;
            BeginBattle(stage, stage.CreateGame(seed));
            _aiRng = new Rng(seed * 31 + 7);
            Message = $"{Stage.Code} {Stage.Name} — {Stage.MapNote}";
            QueueTips(stage);
        }

        private void BeginBattle(StageDef stage, Game game)
        {
            Mode = AppScreen.Battle;
            Stage = stage;
            Game = game;
            _aiRng = new Rng(1);
            _view?.Destroy();
            _fx?.Destroy();
            _view = new BoardView(transform, Game.Board.Width, Game.Board.Height);
            _fx = new FxView(transform);
            Hand.Reset();
            Deselect();
            _lastRound = 0;
            _recorded = false;
            LastStars = 0;
            SnapCamera();
        }

        public void Retry() => StartStage(Stage.Index);

        public void NextStage() => StartStage(Mathf.Min(Stage.Index + 1, StageCatalog.Count - 1));

        public void ExitToLobby() => ShowLobby(Stage?.Index ?? LobbyStage);

        public bool PlayerTurn => Mode == AppScreen.Battle && !Game.IsOver && Game.Current == Side.Player
            && !(Tutorial && LessonDone) && ActiveTip == null;

        private Card? SelectedCard => Game != null && Selected >= 0 && Selected < Game.DeckOf(Side.Player).Hand.Count
            ? Game.DeckOf(Side.Player).Hand[Selected]
            : (Card?)null;

        public void Mulligan()
        {
            if (!PlayerTurn) return;
            Game.Mulligan();
            Deselect();
            AfterMove(Side.Player, default, default);
        }

        // ---------------------------------------------------------------- 프레임

        private void Update()
        {
            if (Mode == AppScreen.Splash)
            {
                UpdateSplash();
                return;
            }
            if (Mode == AppScreen.Lobby)
            {
                _hl.Clear();
                _view.Sync(_previewBoard, _hl, Time.time, Time.deltaTime);
                FitCamera();
                return;
            }

            if (!Tutorial && !Game.IsOver && Game.Current == Side.Ai)
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

            if (!Tutorial && Game.IsOver && !_recorded)
            {
                _recorded = true;
                LastStars = StageCatalog.Stars(Game);
                Progress.Record(Stage.Index, LastStars);
                Deselect();
            }

            BuildHighlight();
            UpdateAim();
            _view.Sync(Game.Board, _hl, Time.time, Time.deltaTime);
            _fx.Update(Time.deltaTime);
            // 튜토리얼 레슨을 마치면 손패를 비워 둔다 (쓴 카드가 무덤에서 다시 들어와 헷갈리지 않게)
            var hand = Tutorial && LessonDone ? (IReadOnlyList<Card>)System.Array.Empty<Card>() : Game.DeckOf(Side.Player).Hand;
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

            var t = 1f - Mathf.Exp(-7f * Time.deltaTime);
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
