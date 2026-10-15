using System.Collections.Generic;
using UnityEngine;

namespace ChromaFrontier.Client
{
    /// <summary>
    /// Resources/Art 로더 (art/build.mjs 가 구운 PNG). 이름은 "폴더/파일" 형식, 한 번 읽으면 캐시.
    /// 없는 에셋은 경고 후 null — 호출 쪽이 그냥 안 그려지게 둔다.
    /// </summary>
    public static class Art
    {
        private static readonly Dictionary<string, Sprite> Sprites = new();
        private static readonly Dictionary<string, Texture2D> Textures = new();
        private static readonly Dictionary<string, Sprite[]> Anims = new();

        public static Sprite Sprite(string name)
        {
            if (Sprites.TryGetValue(name, out var s)) return s;
            s = Resources.Load<Sprite>("Art/" + name);
            if (s == null) Debug.LogWarning($"[Art] missing sprite: {name}");
            return Sprites[name] = s;
        }

        public static Texture2D Texture(string name)
        {
            if (Textures.TryGetValue(name, out var t)) return t;
            t = Resources.Load<Texture2D>("Art/" + name);
            if (t == null) Debug.LogWarning($"[Art] missing texture: {name}");
            return Textures[name] = t;
        }

        /// <summary>프레임 애니메이션: prefix_0 .. prefix_{count-1}</summary>
        public static Sprite[] Anim(string prefix, int count)
        {
            if (Anims.TryGetValue(prefix, out var a)) return a;
            a = new Sprite[count];
            for (var i = 0; i < count; i++) a[i] = Sprite($"{prefix}_{i}");
            return Anims[prefix] = a;
        }
    }
}
