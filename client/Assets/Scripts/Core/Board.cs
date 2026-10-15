using System.Collections.Generic;

namespace ChromaFrontier.Core
{
    /// <summary>칸의 지형 (스테이지 기믹). 공간 왜곡으로도 옮겨지지 않는다.</summary>
    public enum Terrain : byte
    {
        Ground,
        Chasm,     // 낭떠러지: 배치·점유 불가 (끊어진 다리)
        Ice,       // 얼음: 여기 놓은 유닛은 한 칸 더 미끄러진다 (얼음성)
        Silence,   // 침묵 지대: 여기 놓은 유닛은 융합·공명하지 않고, 여기 선 유닛은 촉매가 되지 못한다
        BlackHole, // 블랙홀: 배치·점유 불가, 매 라운드 끝에 주변 8칸을 빨아들여 비운다
        Conveyor,  // 컨베이어 벨트: 매 라운드 끝에 칸의 내용이 벨트 방향으로 한 칸 이동
    }

    public struct Tile
    {
        public Terrain Terrain;
        public Side Owner;
        public Element Unit;      // None = 유닛 없음
        public Side UnitOwner;
        public int Shield;        // > 0 이면 Owner 가 아닌 쪽은 이 칸을 바꿀 수 없다 (보라 침묵, 백색 성역). 턴마다 1씩 감소.
        public int Lock;          // > 0 이면 아무도 유닛을 놓을 수 없다 (영역 잠금). 턴마다 1씩 감소.
        public int Lava;          // > 0 이면 용암: 배치·점유 불가 (화산 지대). 턴마다 1씩 감소, 0 이 되면 식어서 평지.
        public Side Mine;         // 잠복 지뢰를 묻은 쪽. 상대에게는 보이지 않는다.

        public bool HasUnit => Unit != Element.None;

        /// <summary>배치·점유가 불가능한 칸 (낭떠러지, 블랙홀, 용암). 점유율 계산에서도 빠진다.</summary>
        public bool Blocked => Terrain == Terrain.Chasm || Terrain == Terrain.BlackHole || Lava > 0;

        /// <summary>by 쪽이 이 칸을 바꿀 수 없는가 (상대 소유의 보호된 칸).</summary>
        public bool IsProtectedFrom(Side by) => Shield > 0 && Owner != Side.None && Owner != by;

        /// <summary>지형을 뺀 칸의 내용 (공간 왜곡·컨베이어가 옮기는 것).</summary>
        public void SwapContents(ref Tile o)
        {
            var terrain = Terrain;
            var otherTerrain = o.Terrain;
            var lava = Lava;
            var otherLava = o.Lava;
            (this, o) = (o, this);
            Terrain = terrain;
            o.Terrain = otherTerrain;
            Lava = lava;
            o.Lava = otherLava;
        }

        public void ClearContents()
        {
            Owner = Side.None;
            Unit = Element.None;
            UnitOwner = Side.None;
            Shield = 0;
            Lock = 0;
            Mine = Side.None;
        }
    }

    /// <summary>
    /// 격자 좌표 (X, Y). 화면에서는 아이소메트릭으로 X 가 오른쪽 아래, Y 가 왼쪽 아래로 뻗는다.
    /// </summary>
    public sealed class Board
    {
        public readonly int Width, Height;
        private readonly Tile[] _tiles;

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new Tile[width * height];
        }

        private Board(Board src)
        {
            Width = src.Width;
            Height = src.Height;
            _tiles = (Tile[])src._tiles.Clone();
        }

        public Board Clone() => new(this);

        public bool InBounds(Cell c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

        public ref Tile this[Cell c] => ref _tiles[c.Y * Width + c.X];

        public IEnumerable<Cell> Cells()
        {
            for (var y = 0; y < Height; y++)
                for (var x = 0; x < Width; x++)
                    yield return new Cell(x, y);
        }

        public int Count(Side s)
        {
            var n = 0;
            foreach (var t in _tiles)
                if (!t.Blocked && t.Owner == s) n++;
            return n;
        }

        public int Playable
        {
            get
            {
                var n = 0;
                foreach (var t in _tiles)
                    if (!t.Blocked) n++;
                return n;
            }
        }

        /// <summary>전선 규칙: 내 타일이거나 내 타일과 상하좌우로 맞닿은 칸.</summary>
        public bool IsFrontline(Cell c, Side s)
        {
            if (this[c].Owner == s) return true;
            foreach (var d in Cell.Orthogonal)
            {
                var n = c + d;
                if (InBounds(n) && !this[n].Blocked && this[n].Owner == s) return true;
            }
            return false;
        }

        /// <summary>턴이 끝날 때마다 보호막·잠금·용암 남은 턴을 1씩 줄인다.</summary>
        public void TickTimers()
        {
            for (var i = 0; i < _tiles.Length; i++)
            {
                if (_tiles[i].Shield > 0) _tiles[i].Shield--;
                if (_tiles[i].Lock > 0) _tiles[i].Lock--;
                if (_tiles[i].Lava > 0) _tiles[i].Lava--;
            }
        }

        /// <summary>viewer 가 모르는 상대 지뢰를 지운다 (AI 가 숨은 지뢰를 보고 피하지 않도록 시뮬레이션 보드에 적용).</summary>
        public void HideMinesFrom(Side viewer)
        {
            for (var i = 0; i < _tiles.Length; i++)
                if (_tiles[i].Mine != Side.None && _tiles[i].Mine != viewer) _tiles[i].Mine = Side.None;
        }

        /// <summary>
        /// ASCII 지도로 보드를 만든다. 한 줄 = y 한 줄, 한 글자 = x 한 칸.
        ///   . 평지   # 낭떠러지   ~ 얼음   s 침묵 지대   O 블랙홀   &gt; 컨베이어 벨트
        ///   P 플레이어 시작 땅   A AI 시작 땅
        /// </summary>
        public static Board Parse(params string[] rows)
        {
            var b = new Board(rows[0].Length, rows.Length);
            for (var y = 0; y < rows.Length; y++)
                for (var x = 0; x < rows[y].Length; x++)
                {
                    ref var t = ref b[new Cell(x, y)];
                    switch (rows[y][x])
                    {
                        case '#': t.Terrain = Terrain.Chasm; break;
                        case '~': t.Terrain = Terrain.Ice; break;
                        case 's': t.Terrain = Terrain.Silence; break;
                        case 'O': t.Terrain = Terrain.BlackHole; break;
                        case '>': t.Terrain = Terrain.Conveyor; break;
                        case 'P': t.Owner = Side.Player; break;
                        case 'A': t.Owner = Side.Ai; break;
                    }
                }
            return b;
        }
    }
}
