using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>스킬 패널용 붓 질감 스프라이트(흰색 — Image.color 로 속성색을 입힘). 거친 테두리·이름 띠·육각형·팔각 코인·빛 번짐·작은 아이콘.</summary>
    public static class SkillPanelArt
    {
        static Sprite frame, fill, banner, bannerInk, slashes, hexFill, hexRing, glow, iconCoin, iconMin, iconMax, iconBleed, iconBurn, iconTremor, iconSinking, iconParalysis; static Sprite[] coins;
        static float Hash(int x, int y) { unchecked { int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        public static float Noise(float x, float y) { int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float tx = x - xi, ty = y - yi; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            return Mathf.Lerp(Mathf.Lerp(Hash(xi, yi), Hash(xi + 1, yi), tx), Mathf.Lerp(Hash(xi, yi + 1), Hash(xi + 1, yi + 1), tx), ty); }
        static Sprite Make(int w, int h, System.Func<int, int, Color> px, Vector4 border = default)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; var c = new Color[w * h];
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) c[j * w + i] = px(i, j);
            t.SetPixels(c); t.Apply(); return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
        static Color A(float a) => new Color(1, 1, 1, Mathf.Clamp01(a));

        // ── 획 그리기(알파만): 테두리·이름 띠·긁힌 자국. Python 미리보기(tools/skill_panel_mock.py)와 같은 해시·같은 순서로 그림.
        static float H1(int k) => Hash(k * 7919 + 13, k * 104729 + 7);
        static void Seg(float[] a, int w, int h, float x0, float y0, float x1, float y1, float r0, float r1, float alpha)
        {
            int i0 = Mathf.Max(0, (int)(Mathf.Min(x0, x1) - Mathf.Max(r0, r1) - 2)), i1 = Mathf.Min(w - 1, (int)(Mathf.Max(x0, x1) + Mathf.Max(r0, r1) + 2));
            int j0 = Mathf.Max(0, (int)(Mathf.Min(y0, y1) - Mathf.Max(r0, r1) - 2)), j1 = Mathf.Min(h - 1, (int)(Mathf.Max(y0, y1) + Mathf.Max(r0, r1) + 2));
            float bx = x1 - x0, by = y1 - y0, bb = Mathf.Max(1e-6f, bx * bx + by * by);
            for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++)
            {
                float px = i + .5f - x0, py = j + .5f - y0, t = Mathf.Clamp01((px * bx + py * by) / bb), dx = px - bx * t, dy = py - by * t;
                float d = Mathf.Sqrt(dx * dx + dy * dy) - Mathf.Lerp(r0, r1, t), v = Mathf.Clamp01(.5f - d) * alpha; int k = j * w + i; if (v > a[k]) a[k] = v;
            }
        }
        static Sprite FromAlpha(int w, int h, float[] a, Vector4 border = default) => Make(w, h, (i, j) => A(a[j * w + i]), border);

        /// <summary>
        /// 거친 금박 테두리(9분할, 흰색 → 속성색): 끊어질 듯 흔들리는 가는 바깥선 + 모서리 쪽 안쪽 겹선 + 모서리 꺾쇠 + 밖으로 튀어나간 긁힘.
        /// </summary>
        public static Sprite Frame => frame ??= BuildFrame();
        static Sprite BuildFrame()
        {
            const int S = 128; var a = new float[S * S]; int k = 0;
            // 바깥선: 네 변을 짧은 조각으로 나눠 조금씩 흔들림
            for (int side = 0; side < 4; side++)
                for (float p = 2; p < S - 2; k++)
                {
                    float len = 8 + H1(k) * 22, o0 = 5 + (H1(k + 500) - .5f) * 1.6f, o1 = 5 + (H1(k + 900) - .5f) * 1.6f, q = Mathf.Min(S - 2, p + len);
                    if (H1(k + 1300) > .06f) EdgeSeg(a, S, side, p, o0, q, o1, 1.3f + H1(k + 1700) * .7f, 1f);
                    p = q + (H1(k + 2100) > .8f ? 2 : 0);
                }
            // 안쪽 겹선(모서리 근처만) + 모서리 꺾쇠
            for (int c = 0; c < 4; c++)
            {
                bool rx = c == 1 || c == 2, ry = c >= 2;
                float X(float v) => rx ? S - v : v; float Y(float v) => ry ? S - v : v;
                Seg(a, S, S, X(11), Y(11), X(11 + 26 + H1(c + 40) * 10), Y(11), .7f, .7f, .75f); Seg(a, S, S, X(11), Y(11), X(11), Y(11 + 26 + H1(c + 50) * 10), .7f, .7f, .75f);
                Seg(a, S, S, X(1.5f), Y(1.5f), X(19), Y(1.5f), 2f, .8f, 1f); Seg(a, S, S, X(1.5f), Y(1.5f), X(1.5f), Y(19), 2f, .8f, 1f);
                // 긁힘: 모서리에서 대각선으로 밖으로 튀어나간 가는 획
                for (int n = 0; n < 4; n++, k++)
                {
                    float sx = 3 + H1(k + 3000) * 26, sy = 3 + H1(k + 3100) * 26, l = 8 + H1(k + 3200) * 18, ang = (200 + H1(k + 3300) * 50) * Mathf.Deg2Rad;
                    Seg(a, S, S, X(sx), Y(sy), X(sx + Mathf.Cos(ang) * l), Y(sy + Mathf.Sin(ang) * l), .55f + H1(k + 3400) * .4f, .1f, .5f + H1(k + 3500) * .4f);
                }
            }
            return FromAlpha(S, S, a, new Vector4(34, 34, 34, 34));
        }
        static void EdgeSeg(float[] a, int S, int side, float p0, float o0, float p1, float o1, float r, float alpha)
        {
            switch (side)
            {
                case 0: Seg(a, S, S, p0, o0, p1, o1, r, r, alpha); break;                 // 아래
                case 1: Seg(a, S, S, p0, S - o0, p1, S - o1, r, r, alpha); break;         // 위
                case 2: Seg(a, S, S, o0, p0, o1, p1, r, r, alpha); break;                 // 왼쪽
                default: Seg(a, S, S, S - o0, p0, S - o1, p1, r, r, alpha); break;        // 오른쪽
            }
        }
        /// <summary>어두운 반투명 바탕(얼룩).</summary>
        public static Sprite Fill => fill ??= Make(64, 64, (i, j) => { float n = Noise(i * .2f, j * .2f); return new Color(.05f + n * .03f, .04f + n * .025f, .03f + n * .02f, .86f); }, new Vector4(8, 8, 8, 8));
        /// <summary>이름 띠 바탕(어두운 반투명, 오른쪽 끝 비스듬·거친 위아래 끝).</summary>
        public static Sprite Banner => banner ??= Make(256, 64, (i, j) =>
        {
            float x = i / 255f, y = j / 63f, edge = 0.9f + (y - .5f) * .2f + (Noise(j * .5f, 3) - .5f) * .06f; if (x > edge) return A(0);
            float rough = y < .08f + Noise(i * .3f, 1) * .08f || y > .92f - Noise(i * .3f, 5) * .08f ? 0f : 1f;
            return new Color(1, 1, 1, rough * (.82f + .18f * Noise(i * .05f, j * .3f)));
        }, new Vector4(12, 0, 60, 0));
        /// <summary>이름 띠 위의 속성색 먹 긁힘: 오른쪽으로 갈수록 진한 가로 붓질 + 위아래 테두리 획 + 대각선 긁힘.</summary>
        public static Sprite BannerInk => bannerInk ??= BuildBannerInk();
        static Sprite BuildBannerInk()
        {
            const int W = 256, Hh = 64; var a = new float[W * Hh]; int k = 7000;
            for (int n = 0; n < 46; n++, k++)   // 가로 붓질(오른쪽에 몰림)
            {
                float x0 = 40 + Mathf.Pow(H1(k), .55f) * 190, l = 20 + H1(k + 100) * 70, y = 6 + H1(k + 200) * 52;
                Seg(a, W, Hh, x0, y, Mathf.Min(232, x0 + l), y + (H1(k + 300) - .5f) * 4, .4f + H1(k + 400) * 1.6f, .2f, .25f + .55f * Mathf.Clamp01((x0 - 40) / 170f));
            }
            Seg(a, W, Hh, 2, 4, 236, 5, .9f, .5f, .9f); Seg(a, W, Hh, 2, 60, 222, 59, .9f, .5f, .9f);   // 위아래 획
            for (int n = 0; n < 6; n++, k++) { float x = 150 + H1(k) * 90; Seg(a, W, Hh, x, 2, x + 10 + H1(k + 50) * 14, 62, .5f, .2f, .6f); }   // 대각 긁힘
            return FromAlpha(W, Hh, a, new Vector4(12, 0, 60, 0));
        }
        /// <summary>클래시 연출용 붉은 긁힌 자국(화면 모서리). 흰색 → Image.color 로 붉게.</summary>
        public static Sprite Slashes => slashes ??= BuildSlashes();
        static Sprite BuildSlashes()
        {
            const int S = 256; var a = new float[S * S]; int k = 9000;
            for (int n = 0; n < 9; n++, k++)
            {
                float x0 = H1(k) * 120 - 20, y0 = 256 - H1(k + 10) * 40, l = 150 + H1(k + 20) * 170, ang = (-38 - H1(k + 30) * 22) * Mathf.Deg2Rad;
                float r = 1.2f + H1(k + 40) * 4.5f; int seg = 10;
                for (int m = 0; m < seg; m++)
                {
                    float t0 = (float)m / seg, t1 = (m + 1f) / seg, w0 = r * Mathf.Sin(Mathf.PI * Mathf.Lerp(.08f, .95f, t0)), w1 = r * Mathf.Sin(Mathf.PI * Mathf.Lerp(.08f, .95f, t1));
                    Seg(a, S, S, x0 + Mathf.Cos(ang) * l * t0, y0 + Mathf.Sin(ang) * l * t0, x0 + Mathf.Cos(ang) * l * t1, y0 + Mathf.Sin(ang) * l * t1, w0, w1, .55f + H1(k + 50) * .45f);
                }
            }
            return FromAlpha(S, S, a);
        }
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
        // ── 상태이상·에너지 아이콘: 그림(Resources/StatusIcons/*.png, tools/status_icons.py 로 생성)이 있으면 그것, 없으면 아래 흰색 SDF 아이콘
        static readonly System.Collections.Generic.Dictionary<string, Sprite> art = new();
        static Sprite Art(string name)
        {
            if (art.TryGetValue(name, out var s)) return s;
            var tex = Resources.Load<Texture2D>("StatusIcons/" + name);
            if (tex != null) { tex.wrapMode = TextureWrapMode.Clamp; s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f); s.name = name; }
            art[name] = s; return s;
        }
        static string ArtName(StatusType t) => t == StatusType.None ? null : t.ToString();   // Resources/StatusIcons/<이름>.png
        public static bool HasArt(StatusType t) { var n = ArtName(t); return n != null && Art(n) != null; }
        public static Sprite StatusIcon(StatusType t) { var n = ArtName(t); var a = n != null ? Art(n) : null; return a != null ? a : StatusIconFallback(t); }
        /// <summary>Image.color 에 넣을 값: 그림 아이콘은 이미 색이 있으므로 흰색, 대체 아이콘은 상태이상 색.</summary>
        public static Color StatusTint(StatusType t) => HasArt(t) ? Color.white : StatusInfo.ColorOf(t);
        public static Sprite EnergyIcon => Art("Energy");
        static Sprite StatusIconFallback(StatusType t) => t switch
        {
            StatusType.PowerUp => IconMax, StatusType.PowerDown => IconMin,
            StatusType.Paralysis => iconParalysis ??= Icon((x, y) => Mathf.Max(Mathf.Abs(Mathf.Sqrt(x * x + y * y) - .62f) - .1f, -(Mathf.Abs(y - x * .4f + Mathf.Sin(x * 9f) * .18f) - .09f))),   // 끊긴 고리
            StatusType.Bleed => iconBleed ??= Icon((x, y) => Mathf.Min(Mathf.Sqrt(x * x + (y + .25f) * (y + .25f)) - .55f, Mathf.Abs(x) * 1.6f + (y - .25f) - .55f)),   // 핏방울
            StatusType.Burn => iconBurn ??= Icon((x, y) => Mathf.Min(Mathf.Sqrt(x * x + (y + .35f) * (y + .35f)) - .5f, Mathf.Abs(x + Mathf.Sin(y * 3) * .15f) * 1.4f + (y - .2f) * .9f - .45f)),   // 불꽃
            StatusType.Sinking => iconSinking ??= Icon((x, y) => Mathf.Max(Mathf.Min(Mathf.Abs(y - .35f - Mathf.Sin(x * 5f) * .12f) - .11f, Mathf.Max(Mathf.Abs(x) * 1.1f - y * .9f - .55f, y + .05f)), Mathf.Abs(x) - .85f)),   // 물결 + 아래 화살표
            _ => iconTremor ??= Icon((x, y) => { float r = Mathf.Sqrt(x * x + y * y), a = Mathf.Atan2(y, x); float s = Mathf.Repeat(r * 9f - a * 1.43f, Mathf.PI * 2f); return Mathf.Max(r - .9f, Mathf.Abs(s - Mathf.PI) / 9f - .12f); }),   // 소용돌이
        };
    }
}
