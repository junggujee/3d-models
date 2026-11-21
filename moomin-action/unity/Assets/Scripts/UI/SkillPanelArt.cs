using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>스킬 패널용 붓 질감 스프라이트(흰색 — Image.color 로 속성색을 입힘). 거친 테두리·이름 띠·육각형·팔각 코인·빛 번짐·작은 아이콘.</summary>
    public static class SkillPanelArt
    {
        static Sprite frame, fill, banner, hexFill, hexRing, glow, iconCoin, iconMin, iconMax, iconBleed, iconBurn, iconTremor; static Sprite[] coins;
        static float Hash(int x, int y) { unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float Noise(float x, float y) { int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float tx = x - xi, ty = y - yi; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            return Mathf.Lerp(Mathf.Lerp(Hash(xi, yi), Hash(xi + 1, yi), tx), Mathf.Lerp(Hash(xi, yi + 1), Hash(xi + 1, yi + 1), tx), ty); }
        static Sprite Make(int w, int h, System.Func<int, int, Color> px, Vector4 border = default)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; var c = new Color[w * h];
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) c[j * w + i] = px(i, j);
            t.SetPixels(c); t.Apply(); return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
        static Color A(float a) => new Color(1, 1, 1, Mathf.Clamp01(a));

        /// <summary>거친 붓 테두리(9분할): 굵은 바깥선(먹 번짐) + 가는 안쪽 선 + 모서리 긁힘.</summary>
        public static Sprite Frame => frame ??= Make(128, 128, (i, j) =>
        {
            int d = Mathf.Min(Mathf.Min(i, j), Mathf.Min(127 - i, 127 - j)); float n = Noise(i * .35f, j * .35f), n2 = Noise(i * .08f + 9, j * .08f);
            float outer = d < 3 + n * 4 ? (n2 > .25f ? 1f : .5f) : 0f, inner = Mathf.Abs(d - 11 - n * 2) < 0.8f ? .7f : 0f;
            bool corner = (i < 34 || i > 93) && (j < 34 || j > 93); float scratch = corner && Mathf.Abs((i - j) % 9) < 1 && d < 20 && Hash(i / 9, j / 9) > .5f ? .6f : 0f;
            return A(Mathf.Max(outer, Mathf.Max(inner, scratch)));
        }, new Vector4(34, 34, 34, 34));
        /// <summary>어두운 반투명 바탕(얼룩).</summary>
        public static Sprite Fill => fill ??= Make(64, 64, (i, j) => { float n = Noise(i * .2f, j * .2f); return new Color(.04f + n * .03f, .035f + n * .025f, .03f + n * .02f, .88f); }, new Vector4(8, 8, 8, 8));
        /// <summary>이름 띠: 오른쪽 끝이 비스듬하고 붓으로 끌어 낸 줄무늬·거친 끝.</summary>
        public static Sprite Banner => banner ??= Make(256, 64, (i, j) =>
        {
            float x = i / 255f, y = j / 63f, edge = 0.86f + (y - .5f) * .25f + (Noise(j * .5f, 3) - .5f) * .08f; if (x > edge) return A(0);
            float streak = .75f + .25f * Noise(i * .02f, j * .6f); float rough = y < .06f + Noise(i * .3f, 1) * .08f || y > .94f - Noise(i * .3f, 5) * .08f ? 0f : 1f;
            return A(streak * rough * Mathf.Lerp(1f, .7f, x));
        }, new Vector4(12, 0, 60, 0));
        static float HexD(float x, float y) { x = Mathf.Abs(x * 2 - 1); y = Mathf.Abs(y * 2 - 1); return Mathf.Max(x * 0.866f + y * 0.5f, y) - 0.9f; }
        public static Sprite HexFill => hexFill ??= Make(128, 128, (i, j) => A(0.5f - HexD((i + .5f) / 128, (j + .5f) / 128) * 64f));
        /// <summary>두꺼운 육각 테두리 + 안쪽 가는 선, 붓 질감.</summary>
        public static Sprite HexRing => hexRing ??= Make(128, 128, (i, j) =>
        {
            float d = HexD((i + .5f) / 128, (j + .5f) / 128), n = Noise(i * .3f, j * .3f);
            float thick = Mathf.Abs(d + .07f) < .05f + n * .025f ? 1f : 0f, thin = Mathf.Abs(d + .19f) < .012f ? .8f : 0f; return A(Mathf.Max(thick * (.75f + .25f * n), thin));
        });
        public static Sprite Glow => glow ??= Make(64, 64, (i, j) => { float x = (i + .5f) / 32 - 1, y = (j + .5f) / 32 - 1; return A(Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(x * x + y * y)), 2f)); });

        /// <summary>팔각 코인: 0 대기(테두리만) · 1 앞면(금색, 양각) · 2 뒷면(회색) · 3 중단(붉은 금).</summary>
        public static Sprite Coin(int look)
        {
            if (coins == null)
            {
                coins = new Sprite[4];
                for (int k = 0; k < 4; k++)
                {
                    int kk = k;
                    coins[k] = Make(64, 64, (i, j) =>
                    {
                        float x = (i + .5f) / 32 - 1, y = (j + .5f) / 32 - 1; float oct = Mathf.Max(Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)), (Mathf.Abs(x) + Mathf.Abs(y)) * 0.7071f) - .9f;
                        float a = Mathf.Clamp01(.5f - oct * 32), r = Mathf.Sqrt(x * x + y * y);
                        switch (kk)
                        {
                            case 1: { var c = Color.Lerp(new Color(1f, .9f, .5f), new Color(.8f, .55f, .15f), (y * -.5f + .5f) * .8f + r * .3f); if (oct > -.12f) c *= .7f; if (Mathf.Abs(r - .45f) < .04f) c = Color.Lerp(c, new Color(1f, .97f, .8f), .6f); c.a = a; return c; }
                            case 2: { var c = Color.Lerp(new Color(.55f, .53f, .5f), new Color(.3f, .29f, .28f), r); if (oct > -.12f) c *= .75f; c.a = a; return c; }
                            case 3: { var c = new Color(.35f, .07f, .07f); if (Mathf.Abs(Mathf.Abs(x) - Mathf.Abs(y)) < .1f && r < .7f) c = new Color(.95f, .25f, .2f); if (oct > -.12f) c = new Color(.6f, .15f, .12f); c.a = a; return c; }
                            default: return new Color(.75f, .58f, .25f, oct > -.14f ? a : 0f);
                        }
                    });
                }
            }
            return coins[Mathf.Clamp(look, 0, 3)];
        }

        // ── 작은 아이콘(흰색)
        static Sprite Icon(System.Func<float, float, float> sdf) => Make(48, 48, (i, j) => A(.5f - sdf((i + .5f) / 24 - 1, (j + .5f) / 24 - 1) * 24f));
        public static Sprite IconCoin => iconCoin ??= Icon((x, y) => { float r = Mathf.Sqrt(x * x + y * y); return Mathf.Max(r - .8f, -Mathf.Max(Mathf.Abs(r - .45f) - .1f, -(r - .2f))); });
        public static Sprite IconMin => iconMin ??= Icon((x, y) => Mathf.Max(Mathf.Abs(x) * .9f + y * .45f - .45f, -y - .7f));   // 아래 화살표 비슷한 방패
        public static Sprite IconMax => iconMax ??= Icon((x, y) => Mathf.Max(Mathf.Abs(x) - .2f + y * .9f - .5f, Mathf.Abs(y) - .85f) );   // 위로 뾰족
        public static Sprite StatusIcon(StatusType t) => t switch
        {
            StatusType.Bleed => iconBleed ??= Icon((x, y) => Mathf.Min(Mathf.Sqrt(x * x + (y + .25f) * (y + .25f)) - .55f, Mathf.Abs(x) * 1.6f + (y - .25f) - .55f)),   // 핏방울
            StatusType.Burn => iconBurn ??= Icon((x, y) => Mathf.Min(Mathf.Sqrt(x * x + (y + .35f) * (y + .35f)) - .5f, Mathf.Abs(x + Mathf.Sin(y * 3) * .15f) * 1.4f + (y - .2f) * .9f - .45f)),   // 불꽃
            _ => iconTremor ??= Icon((x, y) => { float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x); float s = Mathf.Repeat(r * 9f - a * 1.43f, Mathf.PI * 2f); return Mathf.Max(r - .9f, Mathf.Abs(s - Mathf.PI) / 9f - .12f); }),   // 소용돌이
        };
    }
}
