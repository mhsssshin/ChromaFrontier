using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 전투 입력 (드래그 앤 드롭). 손가락이 칸을 가리지 않도록, 끄는 동안 노리는 칸(_aim)은 손가락보다 조금 위이고
    /// 가장 가까운 고를 수 있는 칸에 자석처럼 붙는다. 노리는 칸에는 결과 미리보기가 뜬다.
    ///   · 카드를 끌어 보드에 놓기 → 그 칸에 낸다 (손패 쪽에서 떼면 취소)
    ///   · 카드를 탭 → 선택만 (다시 탭하면 해제). 선택한 채로 보드를 누르고 끌다 떼도 낸다
    ///   · 물: 놓은 뒤, 보드를 원하는 방향으로 쓸어 넘기거나 화살표 줄을 탭해서 방향을 정한다
    ///   · 공간 왜곡: 첫 칸에 놓은 뒤, 보드를 눌러 끌어 두 번째 칸을 고른다
    /// </summary>
    public sealed partial class GameBootstrap
    {
        private const float CardLift = 74f;    // 카드를 끌 때 노리는 칸이 손가락보다 위에 있는 거리 (기준 좌표 px)
        private const float DragStart = 12f;   // 이만큼 움직여야 끌기로 본다 (기준 좌표 px)
        private const float SwipeMin = 26f;    // 물 방향 쓸기 최소 길이 (기준 좌표 px)
        private const float CardSnap = 1.6f, BoardSnap = 1.25f; // 자석 반경 (마름모 반지름 단위)

        private int _dragCard = -1;
        private bool _dragMoved, _wasSelected, _boardPress;
        private Vector2 _pressGui, _pointerGui;
        private Cell? _aim;

        /// <summary>끌고 있는 카드 (움직이기 시작한 뒤에만), 없으면 -1.</summary>
        public int DraggingCard => _dragCard >= 0 && _dragMoved ? _dragCard : -1;
        public Vector2 PointerGui => _pointerGui;

        private float UiScale => Mathf.Min(Screen.width / 540f, Screen.height / 960f);

        public void Select(int handIndex)
        {
            if (!PlayerTurn) return;
            if (!CanUse(Game.DeckOf(Side.Player).Hand[handIndex]))
            {
                Message = "스킬은 한 턴에 한 장만 쓸 수 있어요. 유닛을 놓으세요.";
                return;
            }
            Selected = handIndex;
            _warpFirst = null;
            _blueAt = _blueTap = null;
            Message = Tutorial
                ? "좋아요! 이제 <color=#d0471a>'여기!'</color> 표시가 있는 칸을 눌러요.\n(카드를 끌어다 놓아도 돼요)"
                : Palette.TargetPrompt(SelectedCard.Value, false);
        }

        private void Deselect()
        {
            Selected = -1;
            _warpFirst = null;
            _blueAt = _blueTap = null;
            _dragCard = -1;
            _dragMoved = _boardPress = false;
            _aim = null;
        }

        public void BeginCardDrag(int handIndex, Vector2 gui)
        {
            if (!PlayerTurn) return;
            _wasSelected = Selected == handIndex;
            if (!_wasSelected) Select(handIndex);
            if (Selected != handIndex) return;
            _dragCard = handIndex;
            _dragMoved = false;
            _pressGui = _pointerGui = gui;
        }

        public void PointerDown(Vector2 gui)
        {
            if (PlayerTurn && Selected < 0 && Tutorial)
                Message = "먼저 <color=#d0471a>아래 카드</color>를 눌러 고르세요.\n그다음 <color=#d0471a>'여기!'</color> 칸을 누르면 돼요.";
            if (!PlayerTurn || Selected < 0) return;
            _boardPress = true;
            _pressGui = _pointerGui = gui;
        }

        public void PointerMove(Vector2 gui)
        {
            _pointerGui = gui;
            if (_dragCard >= 0 && (gui - _pressGui).magnitude > DragStart * UiScale) _dragMoved = true;
        }

        public void PointerUp(Vector2 gui)
        {
            _pointerGui = gui;
            BuildHighlight(); // 누른 뒤 프레임이 한 번도 안 지났어도 고를 수 있는 칸이 준비되게
            if (_dragCard >= 0)
            {
                var moved = _dragMoved;
                _dragCard = -1;
                _dragMoved = false;
                if (!moved)
                {
                    if (_wasSelected) Deselect(); // 고른 카드를 다시 탭하면 해제
                    return;
                }
                // 노리는 칸은 손을 뗀 순간의 위치로 다시 계산한다 (빠르게 끌면 프레임 계산이 못 따라온다)
                if (SelectedCard is { } card && Snap(_pointerGui - new Vector2(0f, CardLift * UiScale), CardSnap) is { } target) Commit(card, target);
                else Message = Tutorial ? "조금 빗나갔어요! 카드를 <color=#d0471a>'여기!'</color> 칸 위까지 끌어 와서 놓아요." : "카드를 밝게 빛나는 칸 위로 끌어다 놓으세요.";
                _aim = null;
                return;
            }

            if (!_boardPress) return;
            _boardPress = false;
            if (!(SelectedCard is { } c)) return;
            if (_blueAt.HasValue)
            {
                if (WaterDir() is { } dir) CommitWater(dir);
                return;
            }
            if (Snap(_pointerGui, BoardSnap) is { } t) Commit(c, t);
            _aim = null;
        }

        /// <summary>물 방향: 충분히 쓸었으면 쓴 방향, 아니면 탭한 칸이 놓인 화살표 줄.</summary>
        private Cell? WaterDir()
        {
            if (!(_blueAt is { } at)) return null;
            var delta = _pointerGui - _pressGui;
            if (delta.magnitude >= SwipeMin * UiScale)
            {
                var a = ScreenToWorld(_pointerGui) - ScreenToWorld(_pressGui);
                var u = a.x / BoardView.HalfW;  // X − Y
                var v = -a.y / BoardView.HalfH; // X + Y
                var gx = (u + v) * 0.5f;
                var gy = (v - u) * 0.5f;
                var dir = Mathf.Abs(gx) >= Mathf.Abs(gy) ? new Cell(gx > 0 ? 1 : -1, 0) : new Cell(0, gy > 0 ? 1 : -1);
                return BlueLine(at, dir).Count > 0 ? dir : (Cell?)null;
            }
            return Snap(_pointerGui, BoardSnap) is { } cell ? BlueDirToward(at, cell) : null;
        }

        /// <summary>노리는 칸을 정한 뒤 미리보기를 채운다. 매 프레임 BuildHighlight 뒤에 부른다.</summary>
        private void UpdateAim()
        {
            _aim = null;
            if (!PlayerTurn || !(SelectedCard is { } card)) return;
            if (_dragCard >= 0 && _dragMoved)
                _aim = Snap(_pointerGui - new Vector2(0f, CardLift * UiScale), CardSnap);
            else if (_boardPress && !_blueAt.HasValue)
                _aim = Snap(_pointerGui, BoardSnap);

            if (_blueAt is { } at)
            {
                var dir = _boardPress ? WaterDir() : _hover is { } h ? BlueDirToward(at, h) : null;
                if (dir is { } d) AddPreview(card, at + d);
                return;
            }
            var focus = _aim ?? (_dragCard < 0 && !_boardPress && _hover is { } hv && _hl.Legal.Contains(hv) ? hv : (Cell?)null);
            if (focus is { } f) AddPreview(card, f);
        }

        /// <summary>선택한 카드·단계에 맞춰 고를 수 있는 칸(Legal)과 화살표를 채운다.</summary>
        private void BuildHighlight()
        {
            _hl.Clear();
            if (!PlayerTurn) return;
            _hl.Hover = _dragCard >= 0 || _boardPress ? _aim : _hover;
            if (!(SelectedCard is { } card))
            {
                // 튜토리얼: 카드를 고르기 전에도 목표 칸을 보여 준다
                if (Tutorial && !LessonDone)
                {
                    _hl.Dim = true;
                    _hl.Legal.Add(Lesson.Target);
                }
                return;
            }
            _hl.Dim = true;

            if (card.IsUnit && _blueAt is { } at)
            {
                _hl.Picked = at;
                if (_blueTap is { } tap && tap != at)
                {
                    _hl.Slides[tap] = at + tap * -1;
                    _hl.Ghost = at;
                    _hl.GhostUnit = card.Element;
                }
                foreach (var d in Cell.Orthogonal)
                {
                    var line = BlueLine(at, d);
                    if (line.Count == 0) continue;
                    _hl.Arrows[line[0]] = d;
                    foreach (var c in line) _hl.Legal.Add(c);
                }
            }
            else if (card.IsUnit)
            {
                foreach (var c in Game.LegalCells(Side.Player))
                {
                    _hl.Legal.Add(c);
                    if (SlideTarget(c) is { } to) _hl.Slides[c] = to + c * -1;
                }
            }
            else
            {
                _hl.Picked = _warpFirst;
                foreach (var c in Game.Board.Cells())
                    if (_warpFirst.HasValue
                            ? Game.CanCast(Side.Player, card, _warpFirst.Value, c)
                            : Game.CanTarget(Side.Player, card, c))
                        _hl.Legal.Add(c);
            }
            RestrictToLesson();
        }

        /// <summary>f 를 고르면 바뀔 칸. 화염·물 방향은 실제로 시뮬레이션해서(융합·공명·얼음 포함) 보여 준다. 번개는 무작위라 놓는 칸만.</summary>
        private void AddPreview(Card card, Cell f)
        {
            _hl.Preview.Add(f);
            if (card.IsUnit)
            {
                if (!_blueAt.HasValue && SlideTarget(f) is { } land)
                {
                    _hl.Preview.Add(land);
                    _hl.Ghost = land;
                    _hl.GhostUnit = card.Element;
                }
                PlayResult r = null;
                if (_blueTap is { } tap && _blueAt is { } at)
                {
                    if (BlueDirToward(at, f) is { } d) r = Game.CloneFor(Side.Player).Play(Selected, tap, d);
                }
                else if (card.Element == Element.Red)
                {
                    r = Game.CloneFor(Side.Player).Play(Selected, f);
                }
                if (r != null)
                {
                    _hl.Preview.Add(r.At);
                    foreach (var c in r.Captured) _hl.Preview.Add(c);
                }
                return;
            }
            if (card.Skill == Skill.Lock)
                foreach (var c in Game.Board.Cells())
                    if (c.Chebyshev(f) <= Balance.LockRadius) _hl.Preview.Add(c);
            if (card.Skill == Skill.Echo && Game.CanTarget(Side.Player, card, f))
                foreach (var c in Game.CloneFor(Side.Player).Cast(Selected, f).Captured) _hl.Preview.Add(c);
        }

        /// <summary>정한 칸으로 카드를 낸다 (물·공간 왜곡은 다음 단계로 넘어갈 수 있음).</summary>
        private void Commit(Card card, Cell cell)
        {
            if (card.IsUnit)
            {
                if (card.Element == Element.Blue)
                {
                    // 얼음이면 미끄러져 멈출 칸에서 물이 흐른다.
                    _blueTap = cell;
                    _blueAt = SlideTarget(cell) ?? cell;
                    Message = Tutorial ? Lesson.WaterIntro : "물이 흐를 방향으로 보드를 쓸어 넘기세요 (화살표 줄을 탭해도 돼요)";
                    return;
                }
                Game.Play(Selected, cell);
                Deselect();
                AfterMove(Side.Player, cell, default);
                CheckLesson(Game.LastResult);
                return;
            }

            if (card.Skill == Skill.Warp && !_warpFirst.HasValue)
            {
                _warpFirst = cell;
                Message = Palette.TargetPrompt(card, true);
                return;
            }

            var first = _warpFirst ?? cell;
            var second = _warpFirst.HasValue ? cell : default;
            Game.Cast(Selected, first, second);
            Deselect();
            AfterMove(Side.Player, first, second);
            CheckLesson(Game.LastResult);
            if (PlayerTurn && !Tutorial) Message += "\n" + Palette.AfterSkill;
        }

        private void CommitWater(Cell dir)
        {
            var tap = _blueTap.Value;
            Game.Play(Selected, tap, dir);
            Deselect();
            AfterMove(Side.Player, tap, default);
            CheckLesson(Game.LastResult);
        }

        /// <summary>얼음 칸 c 에 유닛을 놓으면 미끄러져 멈출 칸 (미끄러지지 않으면 null).</summary>
        private Cell? SlideTarget(Cell c)
        {
            if (Game.Board[c].Terrain != Terrain.Ice) return null;
            var to = c + Resolver.SlideDir(Game.Board, c, Side.Player);
            return Resolver.CanLand(Game.Board, to, Side.Player) ? to : (Cell?)null;
        }

        /// <summary>물 방향 단계에서 화살표가 서는 줄 (길이 BlueLength, 판 밖·낭떠러지에서 멈춤).</summary>
        private List<Cell> BlueLine(Cell at, Cell dir) => Resolver.WaterPath(Game.Board, at, dir, false);

        /// <summary>target 이 at 에서 뻗는 물 줄 위에 있으면 그 방향.</summary>
        private Cell? BlueDirToward(Cell at, Cell target)
        {
            foreach (var d in Cell.Orthogonal)
                if (BlueLine(at, d).Contains(target)) return d;
            return null;
        }

        /// <summary>gui 위치에서 radius(마름모 반지름 단위) 안의 가장 가까운 고를 수 있는 칸.</summary>
        private Cell? Snap(Vector2 gui, float radius)
        {
            var world = ScreenToWorld(gui);
            if (_view.TryPick(world, out var hit) && _hl.Legal.Contains(hit)) return hit;
            Cell? best = null;
            var bestD = radius * radius;
            foreach (var c in _hl.Legal)
            {
                var p = _view.ToWorld(c);
                var dx = (world.x - p.x) / BoardView.HalfW;
                var dy = (world.y - p.y) / BoardView.HalfH;
                var d = dx * dx + dy * dy;
                if (d < bestD)
                {
                    bestD = d;
                    best = c;
                }
            }
            return best;
        }

        public void SetHover(Vector2 gui) => _hover = _view != null && _view.TryPick(ScreenToWorld(gui), out var c) ? c : (Cell?)null;

        private Vector3 ScreenToWorld(Vector2 gui) => _cam.ScreenToWorldPoint(new Vector3(gui.x, Screen.height - gui.y, 0f));
    }
}
