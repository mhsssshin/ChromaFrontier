using System.Collections.Generic;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// 프레임 애니메이션 이펙트 재생기 (Resources/Art/Fx). SpriteRenderer 를 풀링해 재사용한다.
    /// delay 로 칸마다 시차를 줘서 반응이 퍼져 나가는 느낌을 낸다.
    /// </summary>
    public sealed class FxView
    {
        public static readonly (string name, int frames) Burst = ("Fx/burst", 6);
        public static readonly (string name, int frames) Splash = ("Fx/splash", 5);
        public static readonly (string name, int frames) Zap = ("Fx/zap", 4);
        public static readonly (string name, int frames) Ring = ("Fx/ring", 5);
        public static readonly (string name, int frames) Sparkle = ("Fx/sparkle", 4);
        public static readonly (string name, int frames) Smoke = ("Fx/smoke", 5);
        public static readonly (string name, int frames) Void = ("Fx/void", 6);
        public static readonly (string name, int frames) Holy = ("Fx/holy", 6);

        private const int SortingBase = 5000;

        private sealed class Play
        {
            public SpriteRenderer R;
            public Sprite[] Frames;
            public float Delay, Time, Fps;
        }

        private readonly Transform _root;
        private readonly List<Play> _active = new();
        private readonly Stack<SpriteRenderer> _pool = new();

        public FxView(Transform parent)
        {
            _root = new GameObject("Fx").transform;
            _root.SetParent(parent, false);
        }

        public void Destroy() => Object.Destroy(_root.gameObject);

        public void Spawn((string name, int frames) anim, Vector3 pos, Color tint, float scale = 1f, float delay = 0f, float fps = 16f)
        {
            var frames = Art.Anim(anim.name, anim.frames);
            if (frames[0] == null) return;
            var r = _pool.Count > 0 ? _pool.Pop() : NewRenderer();
            r.transform.position = pos;
            r.transform.localScale = Vector3.one * scale;
            r.color = tint;
            r.sprite = frames[0];
            r.enabled = delay <= 0f;
            r.sortingOrder = SortingBase + _active.Count;
            _active.Add(new Play { R = r, Frames = frames, Delay = delay, Fps = fps });
        }

        public void Update(float dt)
        {
            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var p = _active[i];
                if (p.Delay > 0f)
                {
                    p.Delay -= dt;
                    if (p.Delay > 0f) continue;
                    p.R.enabled = true;
                }
                p.Time += dt;
                var frame = (int)(p.Time * p.Fps);
                if (frame >= p.Frames.Length)
                {
                    p.R.enabled = false;
                    _pool.Push(p.R);
                    _active.RemoveAt(i);
                    continue;
                }
                p.R.sprite = p.Frames[frame];
            }
        }

        private SpriteRenderer NewRenderer()
        {
            var go = new GameObject("fx");
            go.transform.SetParent(_root, false);
            return go.AddComponent<SpriteRenderer>();
        }
    }
}
