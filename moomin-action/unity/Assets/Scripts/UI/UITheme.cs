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
                font = Resources.Load<Font>("Fonts/NanumMyeongjo");   // 프로젝트에 넣은 나눔명조(OFL) — 어느 PC 에서나 같은 모양
                if (font != null) return font;
                try { font = Font.CreateDynamicFontFromOSFont(new[] { "Nanum Myeongjo", "NanumMyeongjo", "Noto Serif CJK KR", "Noto Serif KR", "Batang", "AppleMyungjo", "Malgun Gothic", "Arial" }, 32); } catch { }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { } }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
                return font;
            }
        }

        static Font titleFont;
        /// <summary>타이틀 제목용 굵은 명조(나눔명조 ExtraBold, 없으면 기본 글꼴).</summary>
        public static Font TitleFont { get { if (titleFont == null) titleFont = Resources.Load<Font>("Fonts/NanumMyeongjoExtraBold"); return titleFont != null ? titleFont : Font; } }

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

        // ── 타이틀 테마: 파랑 + 갈색 + 하양(엘든링 + 중세). 그림은 tools/title_art.py → Resources/UI/Title/*.png
        public static readonly Color Navy = new Color(0.05f, 0.08f, 0.14f, 0.94f), Blue = new Color(0.40f, 0.60f, 0.86f), BlueBright = new Color(0.70f, 0.86f, 1f), BlueDeep = new Color(0.16f, 0.27f, 0.46f),
            Brown = new Color(0.45f, 0.29f, 0.16f), BrownLight = new Color(0.70f, 0.51f, 0.32f), Ivory = new Color(0.96f, 0.94f, 0.88f), Mist = new Color(0.68f, 0.73f, 0.80f);

        /// <summary>메뉴 색 묶음(게임 안 = 금박, 타이틀 = 파랑·갈색·하양). 저장 슬롯·결과 화면이 어디서 열리든 그곳 테마로.</summary>
        public struct Palette
        {
            public Color accent, bright, dim, text, muted, dimBg; public Sprite frame, row, highlight, marker; public string markerGlyph;
        }
        public static Palette InGame => new Palette { accent = Gold, bright = GoldBright, dim = GoldDim, text = Cream, muted = Muted, dimBg = Dim, frame = Frame, row = Slot, highlight = Highlight, marker = null, markerGlyph = "»" };
        public static Palette TitleStyle => new Palette { accent = Blue, bright = Ivory, dim = Brown, text = new Color(0.88f, 0.86f, 0.80f), muted = Mist, dimBg = new Color(0.02f, 0.03f, 0.06f, 0.78f), frame = TitleArt("SlotFrame", 20, Frame), row = TitleRow, highlight = TitleArt("MenuGlow", 0, Highlight), marker = TitleArt("Marker", 0, null), markerGlyph = "◆" };

        static readonly System.Collections.Generic.Dictionary<string, Sprite> titleArt = new();
        /// <summary>Resources/UI/Title/이름.png → 스프라이트(그림은 2배 해상도: pixelsPerUnit 200, border = UI 단위). 없으면 fallback.</summary>
        public static Sprite TitleArt(string name, float border, Sprite fallback)
        {
            if (titleArt.TryGetValue(name, out var s)) return s != null ? s : fallback;
            var tex = Resources.Load<Texture2D>("UI/Title/" + name);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear; float b = border * 2f;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 200f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b)); s.name = name;
            }
            titleArt[name] = s; return s != null ? s : fallback;
        }

        static Sprite titleRow;
        /// <summary>타이틀 테마 목록 한 줄(저장 슬롯): 남색 바탕 + 갈색 테두리 + 안쪽 옅은 하얀 선.</summary>
        public static Sprite TitleRow => titleRow ??= Make(32, 32, (i, j) =>
        {
            int d = Mathf.Min(Mathf.Min(i, j), Mathf.Min(31 - i, 31 - j));
            if (d < 1) return BrownLight; if (d < 2) return new Color(0.02f, 0.03f, 0.05f, 1f); if (d < 3) return new Color(Ivory.r, Ivory.g, Ivory.b, 0.22f); return new Color(0.06f, 0.09f, 0.15f, 0.92f);
        }, new Vector4(6, 6, 6, 6));

        static Sprite bottomShade;
        /// <summary>타이틀 아래 그늘: 아래에서 위로 옅어짐(안내 글씨 자리).</summary>
        public static Sprite BottomShade => bottomShade ??= Make(4, 64, (i, j) => { float v = j / 63f; return new Color(0.03f, 0.05f, 0.10f, 0.55f * (1f - v) * (1f - v)); }, Vector4.zero);

        static Sprite leftShade;
        /// <summary>타이틀 왼쪽 그늘: 짙은 남색이 왼쪽에서 오른쪽으로 옅어짐(글씨가 풍경 위에서도 읽히게).</summary>
        public static Sprite LeftShade => leftShade ??= Make(256, 4, (i, j) =>
        {
            float u = i / 255f, a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.05f, 1f, u)); return new Color(0.03f, 0.05f, 0.10f, 0.92f * a);
        }, Vector4.zero);
    }
}
