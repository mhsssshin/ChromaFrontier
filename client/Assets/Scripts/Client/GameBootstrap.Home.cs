using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 메인 화면(홈): 내 땅 스킨으로 꾸민 작은 섬을 보여 주고, 싱글 모드(스테이지 대기실) / 멀티 모드(대전 로비) 로 나뉜다.
    /// 아래 줄은 덱·상점·첨탑, 위 줄은 결정·별의 길·계정. 덱·상점 등 창(LobbyPanel)은 홈과 스테이지 대기실 둘 다 위에 뜬다.
    /// </summary>
    public sealed partial class GameBootstrap
    {
        public void ShowHome()
        {
            Mode = AppScreen.Home;
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
            BuildHomeBoard();
        }

        /// <summary>내 땅이 섬 가운데를 채우고 원소 유닛이 몇 개 선 장식 보드 (스킨·테마 미리보기).</summary>
        private void BuildHomeBoard()
        {
            _previewBoard = Board.Parse(
                "...PP...",
                "..PPPP..",
                ".PPPPPP.",
                "PPPPPPPP",
                "PPPPPPPP",
                ".PPPPPP.",
                "..PPPP..",
                "...PP...");
            void Unit(int x, int y, Element e)
            {
                var c = new Cell(x, y);
                _previewBoard[c].Unit = e;
                _previewBoard[c].UnitOwner = Side.Player;
            }
            Unit(3, 2, Element.Red);
            Unit(5, 4, Element.Blue);
            Unit(2, 4, Element.Yellow);
            Unit(4, 5, Element.White);
            _view?.Destroy();
            _view = new BoardView(transform, _previewBoard.Width, _previewBoard.Height);
            SnapCamera();
        }

        /// <summary>스킨·테마를 바꾼 뒤 지금 화면의 미리보기를 다시 그린다.</summary>
        private void RefreshPreview()
        {
            if (Mode == AppScreen.Home) BuildHomeBoard();
            else if (Mode == AppScreen.Lobby) SelectLobbyStage(LobbyStage);
        }

        /// <summary>싱글 모드: 지금 모드에서 마지막으로 열린 스테이지로.</summary>
        public void ShowSingle() => ShowLobby(Mathf.Min(Progress.Unlocked - 1, StageCatalog.Count - 1));
    }
}
