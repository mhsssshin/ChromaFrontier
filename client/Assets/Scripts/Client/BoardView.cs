using System.Collections.Generic;
using ChromaFrontier.Core;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>입력 상태에 따라 보드에 얹는 강조 표시. GameBootstrap 이 매 프레임 채운다.</summary>
    public sealed class Highlight
    {
        public readonly HashSet<Cell> Legal = new();          // 고를 수 있는 칸: 밝게 반짝
        public readonly HashSet<Cell> Preview = new();        // 지금 고르면 영향받는 칸: 노란 테두리
        public readonly Dictionary<Cell, Cell> Arrows = new(); // 칸 → 물 방향 화살표
        public readonly Dictionary<Cell, Cell> Slides = new(); // 얼음 칸 → 미끄러질 방향 (반투명 화살표)
        public Cell? Ghost;                                    // 유닛이 실제로 멈출 칸 (얼음): 반투명 유닛
        public Element GhostUnit;
        public bool Dim;    // 카드를 골랐을 때 Legal 이 아닌 칸을 어둡게
        public Cell? Picked; // 이미 고른 칸 (공간 왜곡 첫 칸, 물 배치 칸)
        public Cell? Hover;

        public void Clear()
        {
            Legal.Clear();
            Preview.Clear();
            Arrows.Clear();
            Slides.Clear();
            Ghost = null;
            GhostUnit = Element.None;
            Dim = false;
            Picked = null;
            Hover = null;
        }
    }

    /// <summary>
    /// 아이소메트릭 보드. 칸 (X, Y) → 월드 ((X - Y) * HalfW, -(X + Y) * HalfH). 보드 중심이 월드 원점.
    /// 매 프레임 Board 상태를 읽어 스프라이트를 맞추고, 직전 프레임과 달라진 칸(소유·유닛)에 튀어 오르기/등장 연출을 건다.
    /// 그리는 순서: 바다·거품 → 타일(+오버레이) → 장식·팀 링·유닛 (x+y 가 클수록 앞).
    /// </summary>
    public sealed class BoardView
    {
        public const float HalfW = 1f, HalfH = 0.5f;
        private const float BounceTime = 0.45f, PopTime = 0.35f, FlashTime = 0.5f;
        private const int UnitLayer = 1000;
        private static readonly Color DimTile = new(0.38f, 0.4f, 0.48f);
        private static readonly Color DimProp = new(0.55f, 0.57f, 0.65f);

        private sealed class Cellview
        {
            public Transform Root;
            public SpriteRenderer Tile, Terrain, Glow, Select, Arrow, Lock, Shield, Decor, Ring, Mine, Beacon, Unit, Ghost;
            public Side LastOwner;
            public Element LastUnit;
            public float Bounce, Pop, Flash;
        }

        private readonly int _w, _h;
        private readonly Vector3 _offset;
        private readonly Cellview[] _cells;
        private readonly Transform _root;
        private readonly SpriteRenderer _sea, _foam;

        public BoardView(Transform parent, int width, int height)
        {
            _w = width;
            _h = height;
            _offset = new Vector3(-(width - height) * 0.5f * HalfW, (width + height - 2) * 0.5f * HalfH, 0f);
            _root = new GameObject("Board").transform;
            _root.SetParent(parent, false);

            _sea = NewRenderer(_root, "Sea", Art.Sprite("Bg/sea"), Vector3.zero, -200);
            _foam = NewRenderer(_root, "Foam", Art.Sprite("Bg/foam"), new Vector3(0f, -0.2f, 0f), -100);
            _foam.transform.localScale = new Vector3((width + height) / 16f, (width + height) / 16f, 1f);

            _cells = new Cellview[width * height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                {
                    var c = new Cell(x, y);
                    var depth = (x + y) * 10;
                    var root = new GameObject($"Cell {x},{y}").transform;
                    root.SetParent(_root, false);
                    root.position = ToWorld(c);
                    var v = _cells[y * width + x] = new Cellview { Root = root, LastOwner = (Side)255, LastUnit = Element.None };
                    v.Tile = NewRenderer(root, "Tile", null, Vector3.zero, depth);
                    v.Terrain = NewRenderer(root, "Terrain", null, Vector3.zero, depth + 1);
                    v.Lock = NewRenderer(root, "Lock", Art.Sprite("Tiles/overlay_lock"), Vector3.zero, depth + 2);
                    v.Shield = NewRenderer(root, "Shield", Art.Sprite("Tiles/overlay_shield"), Vector3.zero, depth + 3);
                    v.Glow = NewRenderer(root, "Glow", Art.Sprite("Tiles/overlay_glow"), Vector3.zero, depth + 4);
                    v.Select = NewRenderer(root, "Select", Art.Sprite("Tiles/overlay_select"), Vector3.zero, depth + 5);
                    // 화살표는 유닛이 선 칸에서도 보이도록 유닛보다 위에 그린다.
                    v.Arrow = NewRenderer(root, "Arrow", Art.Sprite("Tiles/overlay_arrow"), Vector3.zero, UnitLayer + depth + 5);
                    v.Decor = NewRenderer(root, "Decor", DecorFor(x, y), new Vector3(0.35f, 0.12f, 0f), UnitLayer + depth);
                    v.Ring = NewRenderer(root, "Ring", null, new Vector3(0f, -0.02f, 0f), UnitLayer + depth + 1);
                    v.Mine = NewRenderer(root, "Mine", Art.Sprite("Units/mine"), new Vector3(0f, 0.02f, 0f), UnitLayer + depth + 1);
                    v.Beacon = NewRenderer(root, "Beacon", null, new Vector3(-0.42f, 0.42f, 0f), UnitLayer + depth + 1);
                    v.Unit = NewRenderer(root, "Unit", null, new Vector3(0f, 0.04f, 0f), UnitLayer + depth + 2);
                    v.Ghost = NewRenderer(root, "Ghost", null, new Vector3(0f, 0.04f, 0f), UnitLayer + depth + 3);
                }
        }

        public void Destroy() => Object.Destroy(_root.gameObject);

        /// <summary>보드 전체가 들어가는 월드 크기 (가로, 세로).</summary>
        public Vector2 WorldSize => new((_w + _h) * HalfW, (_w + _h) * HalfH + 0.5f);

        public Vector3 ToWorld(Cell c) => new Vector3((c.X - c.Y) * HalfW, -(c.X + c.Y) * HalfH, 0f) + _offset;

        public bool TryPick(Vector3 world, out Cell cell)
        {
            var local = world - _offset;
            var a = local.x / HalfW;  // X - Y
            var b = -local.y / HalfH; // X + Y
            cell = new Cell(Mathf.FloorToInt((a + b) * 0.5f + 0.5f), Mathf.FloorToInt((b - a) * 0.5f + 0.5f));
            return cell.X >= 0 && cell.Y >= 0 && cell.X < _w && cell.Y < _h;
        }

        /// <summary>바다 배경이 카메라 화면을 꽉 채우게 맞춘다.</summary>
        public void FitBackground(Camera cam)
        {
            var sprite = _sea.sprite;
            if (sprite == null) return;
            var h = cam.orthographicSize * 2f;
            var w = h * cam.aspect;
            var size = sprite.bounds.size;
            var s = Mathf.Max(w / size.x, h / size.y);
            _sea.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            _sea.transform.localScale = new Vector3(s, s, 1f);
        }

        public void Flash(IEnumerable<Cell> cells)
        {
            foreach (var c in cells) _cells[c.Y * _w + c.X].Flash = FlashTime;
        }

        public void Sync(Board board, Highlight hl, float time, float dt)
        {
            var pulse = 0.5f + 0.5f * Mathf.Sin(time * 5f);
            _foam.color = new Color(1f, 1f, 1f, 0.85f + 0.15f * Mathf.Sin(time * 1.3f));

            foreach (var c in board.Cells())
            {
                var v = _cells[c.Y * _w + c.X];
                var t = board[c];

                if (t.Owner != v.LastOwner)
                {
                    if (v.LastOwner != (Side)255) v.Bounce = BounceTime;
                    v.LastOwner = t.Owner;
                }
                if (t.Unit != v.LastUnit)
                {
                    if (t.Unit != Element.None) v.Pop = PopTime;
                    v.LastUnit = t.Unit;
                    v.Unit.sprite = t.HasUnit ? Art.Sprite("Units/unit_" + t.Unit.ToString().ToLowerInvariant()) : null;
                }

                // 낭떠러지는 아예 비워 바다가 보이게. 블랙홀은 전용 타일로 그리고 그 위에는 아무것도 없다.
                v.Root.gameObject.SetActive(t.Terrain != Terrain.Chasm);
                if (t.Terrain == Terrain.Chasm) continue;
                v.Tile.sprite = TileSprite(t, c);
                v.Terrain.sprite = TerrainOverlay(t.Terrain);
                v.Terrain.enabled = v.Terrain.sprite != null && !t.Blocked;
                if (t.Blocked)
                {
                    v.Tile.color = hl.Dim ? DimTile : Color.white;
                    v.Lock.enabled = v.Shield.enabled = v.Glow.enabled = v.Select.enabled = v.Arrow.enabled = false;
                    v.Decor.enabled = v.Mine.enabled = v.Unit.enabled = v.Ring.enabled = v.Ghost.enabled = v.Beacon.enabled = false;
                    v.Root.position = ToWorld(c);
                    continue;
                }

                // 소유가 바뀐 칸은 살짝 튀어 오른다.
                var lift = 0f;
                if (v.Bounce > 0f)
                {
                    v.Bounce -= dt;
                    lift = Mathf.Sin(Mathf.Clamp01(1f - v.Bounce / BounceTime) * Mathf.PI) * 0.22f;
                }
                v.Root.position = ToWorld(c) + new Vector3(0f, lift, 0f);

                v.Lock.enabled = t.Lock > 0;
                v.Shield.enabled = t.Shield > 0;
                if (v.Shield.enabled) v.Shield.color = new Color(1f, 1f, 1f, 0.6f + 0.3f * pulse);

                // 카드를 고르면 고를 수 없는 칸은 어둡게, 고를 수 있는 칸은 밝게 반짝인다.
                var isLegal = hl.Legal.Contains(c);
                var isPicked = hl.Picked.HasValue && hl.Picked.Value == c;
                var isHover = hl.Hover.HasValue && hl.Hover.Value == c;
                var dimmed = hl.Dim && !isLegal && !isPicked && !hl.Preview.Contains(c);
                var shade = dimmed ? DimTile : Color.white;
                v.Tile.color = shade;
                v.Decor.color = v.Mine.color = v.Ring.color = v.Beacon.color = dimmed ? DimProp : Color.white;
                v.Unit.color = dimmed ? DimProp : Color.white;

                var glow = isLegal ? 0.22f + 0.28f * pulse : 0f;
                if (isHover && isLegal) glow = 0.6f;
                if (v.Flash > 0f)
                {
                    glow = Mathf.Max(glow, v.Flash / FlashTime * 0.9f);
                    v.Flash -= dt;
                }
                v.Glow.enabled = glow > 0.01f;
                v.Glow.color = new Color(1f, 1f, 1f, glow);
                v.Select.enabled = isPicked || hl.Preview.Contains(c) || (isHover && isLegal);

                // 물 방향 화살표는 진하게, 얼음 미끄러짐 방향은 반투명하게.
                var water = hl.Arrows.TryGetValue(c, out var dir);
                var slide = !water && hl.Slides.TryGetValue(c, out dir);
                v.Arrow.enabled = water || slide;
                if (v.Arrow.enabled)
                {
                    // 그림은 +X(화면 오른쪽 아래). 화면 방향 = (X−Y, −(X+Y)) 이므로 그 부호대로 뒤집는다.
                    v.Arrow.flipX = dir.X - dir.Y < 0;
                    v.Arrow.flipY = dir.X + dir.Y < 0;
                    v.Arrow.color = new Color(1f, 1f, 1f, water ? 0.75f + 0.25f * pulse : 0.3f + 0.2f * pulse);
                }

                // 얼음에서 실제로 멈출 칸: 반투명 유닛
                v.Ghost.enabled = hl.Ghost.HasValue && hl.Ghost.Value == c && !t.HasUnit && hl.GhostUnit != Element.None;
                if (v.Ghost.enabled)
                {
                    v.Ghost.sprite = Art.Sprite("Units/unit_" + hl.GhostUnit.ToString().ToLowerInvariant());
                    v.Ghost.color = new Color(1f, 1f, 1f, 0.4f + 0.15f * pulse);
                }

                v.Mine.enabled = t.Mine == Side.Player && !t.HasUnit;
                v.Beacon.enabled = t.Beacon != Side.None && t.Beacon == t.Owner;
                if (v.Beacon.enabled) v.Beacon.sprite = Art.Sprite(t.Beacon == Side.Ai ? "Units/beacon_ai" : "Units/beacon_player");
                v.Decor.enabled = v.Decor.sprite != null && !t.HasUnit && !v.Mine.enabled && t.Terrain == Terrain.Ground;

                v.Unit.enabled = v.Ring.enabled = t.HasUnit;
                if (!t.HasUnit) continue;
                v.Ring.sprite = Art.Sprite(t.UnitOwner == Side.Ai ? "Units/ring_ai" : "Units/ring_player");

                // 등장: 위에서 떨어지며 통통 튐. 평소: 둥실둥실, 불꽃은 일렁임.
                var scale = 1f;
                var drop = 0f;
                if (v.Pop > 0f)
                {
                    v.Pop -= dt;
                    var k = Mathf.Clamp01(1f - v.Pop / PopTime);
                    scale = EaseOutBack(k);
                    drop = (1f - k) * 0.8f;
                }
                var phase = (c.X * 0.9f + c.Y * 1.3f);
                var bob = Mathf.Sin(time * 2.4f + phase) * 0.04f;
                var flicker = t.Unit == Element.Red || t.Unit == Element.Orange ? Mathf.Sin(time * 11f + phase) * 0.04f : 0f;
                v.Unit.transform.localPosition = new Vector3(0f, 0.04f + bob + drop, 0f);
                v.Unit.transform.localScale = new Vector3(scale * (1f + flicker), scale * (1f - flicker * 0.5f), 1f);
            }
        }

        private static Sprite TileSprite(in Tile t, Cell c)
        {
            if (t.Terrain == Terrain.BlackHole) return Art.Sprite("Tiles/tile_blackhole");
            return t.Owner switch
            {
                Side.Player => Art.Sprite("Tiles/tile_player"),
                Side.Ai => Art.Sprite("Tiles/tile_ai"),
                _ => Art.Sprite((c.X + c.Y) % 2 == 0 ? "Tiles/tile_grass_a" : "Tiles/tile_grass_b"),
            };
        }

        private static Sprite TerrainOverlay(Terrain terrain) => terrain switch
        {
            Terrain.Ice => Art.Sprite("Tiles/overlay_ice"),
            Terrain.Silence => Art.Sprite("Tiles/overlay_silence"),
            Terrain.Conveyor => Art.Sprite("Tiles/overlay_conveyor"),
            _ => null,
        };

        /// <summary>칸마다 고정된 무작위 장식 (약 1/4 칸). 유닛이 서면 숨긴다.</summary>
        private static Sprite DecorFor(int x, int y)
        {
            var h = (uint)(x * 73856093) ^ (uint)(y * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            if (h % 100 >= 24) return null;
            string[] names = { "Decor/tree_round", "Decor/tree_pine", "Decor/rock", "Decor/bush" };
            return Art.Sprite(names[(h / 100) % (uint)names.Length]);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static SpriteRenderer NewRenderer(Transform parent, string name, Sprite sprite, Vector3 localPos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.sortingOrder = order;
            return r;
        }
    }
}
