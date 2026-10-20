using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 크로마 첨탑: 층마다 정해진 판과 카드로 몇 수 안에 모든 칸을 내 색으로 물들이는 퍼즐 (PuzzleCatalog).
    /// 상대는 두지 않는다. 한 층을 깨면 다음 층이 열리고, 처음 깰 때 결정을 받는다. 무르기·다시는 공짜다.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        public PuzzleDef Puzzle { get; private set; }  // 첨탑 층을 두는 중이면 그 층 (아니면 null)
        public int TowerFloor { get; private set; }    // 첨탑에서 고른 층 (0부터)
        public bool HintShown { get; private set; }
        public int LastFloorStars { get; private set; } // 직전 첨탑 판에서 새로 받은 별 비트 (Progress.FloorClear 등)

        private bool _towerUndo;                                                     // 이번 도전에서 무르기를 썼다
        private readonly System.Collections.Generic.HashSet<int> _hintFloors = new(); // 이번 실행에서 힌트를 본 층

        private static readonly TipDef TowerTip = new()
        {
            Id = "tower", Title = "크로마 첨탑",
            Body = "상대는 두지 않아요. 정해진 카드로 정해진 수 안에\n모든 칸을 내 색(파랑)으로 물들이면 성공!\n흑색을 만들면 수를 쓰지 않고 한 번 더 둘 수 있어요.\n막히면 무르기와 다시를 마음껏 쓰세요 (공짜).",
            Pictures = new[] { new[] { "Tiles/tile_player" }, new[] { "Units/unit_black" } },
        };

        public void ShowTower(int floor)
        {
            Mode = AppScreen.Tower;
            Panel = LobbyPanel.None;
            Tutorial = false;
            Puzzle = null;
            EndCine();
            _tips.Clear();
            Game = null;
            _fx?.Destroy();
            _fx = null;
            SelectFloor(floor);
        }

        /// <summary>첨탑에서 층을 고르면 그 판을 미리 보여 준다.</summary>
        public void SelectFloor(int floor)
        {
            TowerFloor = Mathf.Clamp(floor, 0, PuzzleCatalog.Count - 1);
            HintShown = false;
            _view?.Destroy();
            _previewBoard = PuzzleCatalog.Floors[TowerFloor].CreateBoard();
            _view = new BoardView(transform, _previewBoard.Width, _previewBoard.Height);
            SnapCamera();
        }

        public void StartFloor(int floor)
        {
            if (!Progress.FloorOpen(floor)) return;
            var p = PuzzleCatalog.Floors[floor];
            Tutorial = false;
            BeginBattle(new StageDef { Index = -1, Name = p.Name }, p.CreateGame());
            Puzzle = p;
            TowerFloor = floor;
            HintShown = false;
            _towerUndo = false;
            LastFloorStars = 0;
            Message = GoalText(p);
            _tips.Clear();
            if (!Progress.TipSeen(TowerTip.Id)) _tips.Enqueue(TowerTip);
        }

        public static string GoalText(PuzzleDef p) => $"{p.Floor}층 · {p.Name}\n{p.Moves}수 안에 모든 칸을 내 땅으로!";

        public void ToggleHint()
        {
            HintShown = !HintShown;
            if (HintShown) _hintFloors.Add(TowerFloor);
            if (Puzzle != null && Mode == AppScreen.Battle) Message = HintShown ? "힌트: " + Puzzle.Hint : GoalText(Puzzle);
        }

        /// <summary>이 층의 '힌트 없이' 별을 아직 받을 수 있는가 (이번 실행에서 힌트를 안 봤다).</summary>
        public bool HintFree(int floor) => !_hintFloors.Contains(floor);

        /// <summary>남은 수 (흑색의 추가 턴은 수를 쓰지 않는다).</summary>
        public int MovesLeft => Puzzle == null ? 0 : Mathf.Max(0, Puzzle.Moves - Game.Turn);
    }
}
