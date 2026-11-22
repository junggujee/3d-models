using System.Collections.Generic;
using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>
    /// 체력·경험치 바 그림: Resources/UI/Bars/*.png(tools/bar_art.py 로 생성)를 불러 스프라이트로. 없으면 UITheme 기본 스프라이트.
    /// 9분할 테두리(그림 px)는 bar_art.py 와 같아야 함. 그림은 2배 해상도라 pixelsPerUnit 200(테두리 28px → UI 14).
    /// </summary>
    public static class BarArt
    {
        static readonly Dictionary<string, Sprite> cache = new();
        static readonly Dictionary<string, Vector4> BORDER = new() { { "Frame", new Vector4(28, 28, 28, 28) }, { "Tag", new Vector4(14, 14, 14, 14) } };
        static Sprite Load(string name, Sprite fallback)
        {
            if (cache.TryGetValue(name, out var s)) return s != null ? s : fallback;
            var tex = Resources.Load<Texture2D>("UI/Bars/" + name);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
                BORDER.TryGetValue(name, out var b);
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 200f, 0, SpriteMeshType.FullRect, b); s.name = name;
            }
            cache[name] = s; return s != null ? s : fallback;
        }
        public static bool HasArt => Load("Frame", null) != null;
        public static Sprite Frame => Load("Frame", UITheme.Bar);
        public static Sprite Fill => Load("Fill", UITheme.White);
        public static Sprite Back => Load("Back", UITheme.White);
        public static Sprite Binding => Load("Binding", null);
        public static Sprite Tag => Load("Tag", UITheme.White);
        public static Sprite Badge => Load("Badge", UITheme.Slot);
    }
}
