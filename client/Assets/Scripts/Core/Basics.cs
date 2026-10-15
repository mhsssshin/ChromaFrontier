using System;

namespace ChromaFrontier.Core
{
    public enum Side : byte { None, Player, Ai }

    /// <summary>
    /// 원소. 1단계(빨·파·노)만 덱에 들어가고, 2단계(초·보·주)와 3단계(백·흑)는 보드 위 융합으로만 생긴다.
    /// </summary>
    public enum Element : byte { None, Red, Blue, Yellow, Green, Purple, Orange, White, Black, Grey }

    public static class Sides
    {
        public static Side Opponent(this Side s) => s switch
        {
            Side.Player => Side.Ai,
            Side.Ai => Side.Player,
            _ => Side.None,
        };
    }

    public static class Elements
    {
        public static readonly Element[] Base = { Element.Red, Element.Blue, Element.Yellow };

        public static bool IsBase(this Element e) => e == Element.Red || e == Element.Blue || e == Element.Yellow;

        public static int Tier(this Element e) => e switch
        {
            Element.Red or Element.Blue or Element.Yellow => 1,
            Element.Green or Element.Purple or Element.Orange => 2,
            Element.White or Element.Black => 3,
            _ => 0,
        };

        /// <summary>빨·파·노 비트 마스크. 2단계는 색 두 개를 품는다. 3단계·무속성은 0 (더 이상 촉매가 아님).</summary>
        public const int AllColors = 7;

        public static int Colors(this Element e) => e switch
        {
            Element.Red => 1,
            Element.Blue => 2,
            Element.Yellow => 4,
            Element.Purple => 1 | 2,
            Element.Green => 2 | 4,
            Element.Orange => 1 | 4,
            _ => 0,
        };

        /// <summary>서로 다른 1단계 원소 둘의 2단계 결과. 조합이 안 되면 None.</summary>
        public static Element Fuse(Element a, Element b)
        {
            if (a == b || !a.IsBase() || !b.IsBase()) return Element.None;
            if (a != Element.Red && b != Element.Red) return Element.Green;    // 파랑 + 노랑
            if (a != Element.Yellow && b != Element.Yellow) return Element.Purple; // 파랑 + 빨강
            return Element.Orange;                                              // 빨강 + 노랑
        }
    }

    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X, Y;

        public Cell(int x, int y) { X = x; Y = y; }

        public static readonly Cell[] Orthogonal = { new(0, -1), new(1, 0), new(0, 1), new(-1, 0) };
        public static readonly Cell[] Around8 =
        {
            new(0, -1), new(1, -1), new(1, 0), new(1, 1), new(0, 1), new(-1, 1), new(-1, 0), new(-1, -1),
        };

        public static Cell operator +(Cell a, Cell b) => new(a.X + b.X, a.Y + b.Y);
        public static Cell operator *(Cell a, int k) => new(a.X * k, a.Y * k);
        public static bool operator ==(Cell a, Cell b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Cell a, Cell b) => !(a == b);

        public int Chebyshev(Cell o) => Math.Max(Math.Abs(X - o.X), Math.Abs(Y - o.Y));
        public int Manhattan(Cell o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y);

        public bool Equals(Cell o) => this == o;
        public override bool Equals(object obj) => obj is Cell c && this == c;
        public override int GetHashCode() => X * 397 ^ Y;
        public override string ToString() => $"({X},{Y})";
    }

    /// <summary>결정적 난수 (xorshift32). 상태를 복제할 수 있어서 AI가 Game 째로 복제해 수를 시뮬레이션한다.</summary>
    public sealed class Rng
    {
        private uint _s;

        public Rng(int seed)
        {
            _s = (uint)seed ^ 0x9E3779B9u;
            if (_s == 0) _s = 1;
        }

        private Rng(uint state, bool _) { _s = state; }

        public Rng Clone() => new(_s, true);

        public uint NextUInt()
        {
            var x = _s;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return _s = x;
        }

        public int Next(int maxExclusive) => (int)(NextUInt() % (uint)maxExclusive);
    }
}
