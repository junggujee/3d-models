using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>중세 판타지 + 엘든링풍 UI 테마: 어두운 양피지 바탕, 금박 이중 테두리와 모서리 장식, 크림색 명조체 글씨. 이미지 파일 없이 코드로 스프라이트를 생성.</summary>
    public static class UITheme
    {
        public static readonly Color Ink = new Color(0.055f, 0.047f, 0.04f, 0.95f), Gold = new Color(0.80f, 0.65f, 0.36f), GoldBright = new Color(1f, 0.86f, 0.5f), GoldDim = new Color(0.45f, 0.36f, 0.2f),
            Cream = new Color(0.92f, 0.87f, 0.76f), Muted = new Color(0.62f, 0.57f, 0.47f), Red = new Color(0.66f, 0.09f, 0.09f), RedDark = new Color(0.3f, 0.04f, 0.05f), Green = new Color(0.36f, 0.62f, 0.26f), Dim = new Color(0, 0, 0, 0.72f);

        static Font font; static Sprite frame, bar, highlight, white, slot;
        public static Font Font
        {
            get
            {
                if (font != null) return font;
                try { font = Font.CreateDynamicFontFromOSFont(new[] { "Nanum Myeongjo", "NanumMyeongjo", "Noto Serif CJK KR", "Noto Serif KR", "Batang", "AppleMyungjo", "Malgun Gothic", "Arial" }, 32); } catch { }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                return font;
            }
        }

        static Sprite Make(int w, int h, System.Func<int, int, Color> px, Vector4 border)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; var c = new Color[w * h];
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) c[j * w + i] = px(i, j);
            t.SetPixels(c); t.Apply(); return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>장식 테두리 패널(9분할): 금색 바깥선 + 안쪽 가는 선 + 네 모서리 다이아몬드.</summary>
        public static Sprite Frame => frame ??= Make(48, 48, (i, j) =>
        {
            int n = 48, d = Mathf.Min(Mathf.Min(i, j), Mathf.Min(n - 1 - i, n - 1 - j));
            foreach (var cx in new[] { 10, n - 11 }) foreach (var cy in new[] { 10, n - 11 }) { int m = Mathf.Abs(i - cx) + Mathf.Abs(j - cy); if (m <= 4) return GoldBright; if (m == 5) return GoldDim; }
            if (d < 2) return Gold; if (d < 5) return new Color(0.09f, 0.075f, 0.06f, 0.97f); if (d == 5) return GoldDim; return Ink;
        }, new Vector4(18, 18, 18, 18));

        /// <summary>작은 칸(아이템 슬롯)용 단순 테두리.</summary>
        public static Sprite Slot => slot ??= Make(32, 32, (i, j) => { int d = Mathf.Min(Mathf.Min(i, j), Mathf.Min(31 - i, 31 - j)); if (d < 1) return GoldDim; if (d < 2) return new Color(0.02f, 0.02f, 0.02f, 1); return new Color(0.07f, 0.06f, 0.05f, 0.92f); }, new Vector4(5, 5, 5, 5));

        /// <summary>게이지 틀(얇은 금테).</summary>
        public static Sprite Bar => bar ??= Make(16, 16, (i, j) => { int d = Mathf.Min(Mathf.Min(i, j), Mathf.Min(15 - i, 15 - j)); if (d < 1) return Gold; if (d < 2) return Color.black; return new Color(0.03f, 0.025f, 0.02f, 0.95f); }, new Vector4(4, 4, 4, 4));

        /// <summary>선택 막대: 위아래 금색 선 + 은은한 금빛 바탕, 양끝으로 갈수록 투명(대화 선택지 느낌).</summary>
        public static Sprite Highlight => highlight ??= Make(64, 32, (i, j) =>
        {
            float u = i / 63f, fade = Mathf.SmoothStep(0, 1, Mathf.Min(u, 1 - u) * 5f); bool line = j < 2 || j > 29;
            if (line) return new Color(Gold.r, Gold.g, Gold.b, fade); return new Color(0.85f, 0.65f, 0.3f, 0.16f * fade);
        }, new Vector4(22, 8, 22, 8));

        public static Sprite White => white ??= Make(4, 4, (i, j) => Color.white, Vector4.zero);
    }
}
