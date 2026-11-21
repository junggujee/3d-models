using System.Collections.Generic;
using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>
    /// 스킬 일러스트 v2: 인물 실루엣 + 화려한 빛 연출 + 하프톤 + 먹 번짐 + 속성색 포인트(흑백 만화풍).
    /// SkillDefinition.illustration 에 그림을 넣으면 그 그림을 쓰고, 비어 있으면 motif 로 코드 생성(스킬 id 로 모양이 고정).
    ///   베기 = 큰 빛의 호 · 찌르기 = 광선과 수렴선 · 내려찍기 = 아래서 터지는 가시 폭발 · 돌진 = 가로 속도선 · 연타 = 엇갈린 궤적
    /// 획마다 영향 범위(bbox)만 계산해서 스킬당 수십 ms 안에 그려지고, 한 번 그리면 캐시.
    /// (같은 알고리즘의 Python 시제품: tools/illustration_preview.py)
    /// </summary>
    public static class SkillIllustration
    {
        const int W = 384, H = 200; const float ASP = (float)W / H;
        static readonly Dictionary<string, Sprite> cache = new();
        static float[] lum, glow, acc; static System.Random rng;
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static int StableHash(string s) { unchecked { int h = (int)2166136261; foreach (char c in s) h = (h ^ c) * 16777619; return h; } }

        public static Sprite Get(SkillDefinition s)
        {
            if (s == null) return null; if (s.illustration != null) return s.illustration;
            string key = s.skillId + "_" + s.motif + "_" + s.attribute; if (cache.TryGetValue(key, out var sp)) return sp;
            rng = new System.Random(StableHash(s.skillId)); lum = new float[W * H]; glow = new float[W * H]; acc = new float[W * H];
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++) lum[j * W + i] = 0.06f + 0.08f * (j + .5f) / H + (float)rng.NextDouble() * 0.03f;

            float fx = R(0.45f, 0.7f); float[] pose;
            switch (s.motif)
            {
                case SkillMotif.Slash:
                    pose = new[] { 15f, 70f, 200f, -60f, -120f, 30f }; Arc(fx + 0.5f, 0.2f, 1.0f, 0.75f, 180f, 75f, 0.05f, 1f, 0.3f, 24);
                    for (int k = 0; k < 8; k++) Arc(fx + 0.5f + R(-.1f, .1f), 0.2f + R(-.1f, .1f), R(.85f, 1.1f), R(.6f, .85f), R(170f, 190f), R(70f, 90f), R(.002f, .01f), R(.5f, .9f), R(0, 1) > .6f ? 1 : 0, 24);
                    break;
                case SkillMotif.Thrust:
                    pose = new[] { 35f, 5f, 190f, -30f, -150f, 2f }; Stroke(fx + .2f, .47f, ASP, .5f, .05f, 1f, .2f, true);
                    for (int k = 0; k < 18; k++) { float y = R(.1f, .9f); Stroke(R(fx + .3f, ASP), y, ASP + .2f, .5f + (y - .5f) * .3f, R(.002f, .008f), R(.4f, .9f), R(0, 1) > .7f ? 1 : 0, true); }
                    break;
                case SkillMotif.Smash:
                    pose = new[] { 5f, 100f, 80f, -70f, -110f, 95f }; { float cx = fx + .35f, cy = .05f; for (int k = 0; k < 26; k++) { float a = R(15f, 165f) * Mathf.Deg2Rad, l = R(.3f, 1f); Stroke(cx, cy, cx + Mathf.Cos(a) * l, cy + Mathf.Sin(a) * l, R(.01f, .04f), R(.6f, 1f), R(0, 1) > .6f ? 1 : 0, true); } }
                    break;
                case SkillMotif.Rush:
                    pose = new[] { 45f, -10f, 160f, -20f, -170f, -5f }; for (int k = 0; k < 36; k++) { float y = R(.05f, .95f), x0 = R(0, ASP); Stroke(x0, y, x0 - R(.3f, .9f), y, R(.002f, .012f), R(.3f, .9f), R(0, 1) > .75f ? 1 : 0, true); }
                    break;
                default:
                    pose = new[] { 20f, 40f, 210f, -55f, -125f, 60f }; for (int k = 0; k < 6; k++) Arc(fx + R(-.2f, .6f), R(.2f, .8f), R(.3f, .6f), R(.3f, .5f), R(0, 360), R(0, 360), R(.006f, .025f), R(.6f, 1f), R(0, 1) > .5f ? 1 : 0, 14);
                    break;
            }
            // 먹 번짐
            for (int k = 0; k < 40; k++) { float cx = R(0, ASP), cy = R(0, 1), r = R(.003f, .03f); ForBox(cx - r, cy - r, cx + r, cy + r, (idx, x, y) => { if (Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) < r) lum[idx] = Mathf.Min(lum[idx], 0.03f); }); }
            // 빛 번짐 합성 + 하프톤
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
            {
                int idx = j * W + i; float v = Mathf.Clamp01(lum[idx] + glow[idx] * 0.35f);
                if (v > .15f && v < .6f) { float gx = i % 6 - 3f, gy = j % 6 - 3f; v += Mathf.Sqrt(gx * gx + gy * gy) < v * 6f * 0.75f ? .15f : -.08f; }
                lum[idx] = v;
            }
            Figure(pose, fx);
            // 색 입히기
            var c = SkillAttributes.ColorOf(s.attribute); var px = new Color[W * H];
            for (int k = 0; k < px.Length; k++)
            {
                float v = Mathf.Clamp01(lum[k]), a = Mathf.Clamp01(acc[k]) * (v > .3f ? 1f : 0f), m = Mathf.Max(v, .6f);
                px[k] = new Color(v * (1 - a * .8f) + c.r * a * m, v * (1 - a * .8f) + c.g * a * m, v * (1 - a * .8f) + c.b * a * m, 1f);
            }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; tex.SetPixels(px); tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(.5f, .5f), 100f); cache[key] = sp; lum = glow = acc = null; return sp;
        }

        delegate void PixelFn(int idx, float x, float y);
        static void ForBox(float x0, float y0, float x1, float y1, PixelFn f)
        {
            int i0 = Mathf.Max(0, (int)(x0 / ASP * W)), i1 = Mathf.Min(W - 1, (int)(x1 / ASP * W) + 1), j0 = Mathf.Max(0, (int)(y0 * H)), j1 = Mathf.Min(H - 1, (int)(y1 * H) + 1);
            for (int j = j0; j <= j1; j++) for (int i = i0; i <= i1; i++) f(j * W + i, (i + .5f) / W * ASP, (j + .5f) / H);
        }

        static float Seg(float x, float y, float ax, float ay, float bx, float by, out float h)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay; h = Mathf.Clamp01((pax * bax + pay * bay) / Mathf.Max(1e-9f, bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h; return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>붓 획: 밝은 심 + 주변 빛 번짐 + (선택) 속성색.</summary>
        static void Stroke(float ax, float ay, float bx, float by, float w, float l, float ac, bool taper)
        {
            const float pad = 0.12f;
            ForBox(Mathf.Min(ax, bx) - w - pad, Mathf.Min(ay, by) - w - pad, Mathf.Max(ax, bx) + w + pad, Mathf.Max(ay, by) + w + pad, (idx, x, y) =>
            {
                float dd = Seg(x, y, ax, ay, bx, by, out float h); float t = taper ? Mathf.Sin(Mathf.PI * Mathf.Clamp(h, .02f, .98f)) : 1f; float d = dd - w * t;
                float core = Mathf.Clamp01(0.5f - d * H * 0.8f); lum[idx] = Mathf.Max(lum[idx], core * l); glow[idx] += Mathf.Exp(-Mathf.Max(d, 0) * 40f) * w * 6f * l; acc[idx] = Mathf.Max(acc[idx], core * ac);
            });
        }

        static void Arc(float cx, float cy, float rx, float ry, float a0, float a1, float w, float l, float ac, int n)
        {
            for (int k = 0; k < n; k++)
            {
                float t0 = (float)k / n, t1 = (k + 1f) / n, A0 = Mathf.Lerp(a0, a1, t0) * Mathf.Deg2Rad, A1 = Mathf.Lerp(a0, a1, t1) * Mathf.Deg2Rad;
                Stroke(cx + Mathf.Cos(A0) * rx, cy + Mathf.Sin(A0) * ry, cx + Mathf.Cos(A1) * rx, cy + Mathf.Sin(A1) * ry, w * Mathf.Max(.12f, Mathf.Sin(Mathf.PI * (k + .5f) / n)), l, ac, false);
            }
        }

        /// <summary>인물 실루엣(캡슐 SDF): 검은 몸 + 빛 쪽 가장자리 하이라이트 + 밝은 칼날. pose = 몸 기울기, 앞팔, 뒷팔, 앞다리, 뒷다리, 무기 각도(도).</summary>
        static void Figure(float[] pose, float ox)
        {
            float lean = pose[0] * Mathf.Deg2Rad, fa = pose[1] * Mathf.Deg2Rad, ba = pose[2] * Mathf.Deg2Rad, fl = pose[3] * Mathf.Deg2Rad, bl = pose[4] * Mathf.Deg2Rad, wa = pose[5] * Mathf.Deg2Rad;
            var hip = new Vector2(ox, 0.35f); var up = new Vector2(Mathf.Sin(lean), Mathf.Cos(lean)); var chest = hip + up * 0.22f; var head = chest + up * 0.09f;
            Vector2[] Limb(Vector2 o, float ang, float l1, float l2, float bend) { var e = o + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * l1; var w = e + new Vector2(Mathf.Cos(ang + bend), Mathf.Sin(ang + bend)) * l2; return new[] { e, w }; }
            var f = Limb(chest, fa, .12f, .11f, -.3f); var b = Limb(chest, ba, .11f, .1f, .4f); var fk = Limb(hip, fl, .15f, .15f, -.5f); var bk = Limb(hip, bl, .15f, .15f, .6f);
            var cape = chest + new Vector2(-.18f, -.12f); var tip = f[1] + new Vector2(Mathf.Cos(wa), Mathf.Sin(wa)) * .34f;
            var caps = new (Vector2 a, Vector2 b, float r)[] { (hip, chest, .05f), (chest, f[0], .022f), (f[0], f[1], .02f), (chest, b[0], .022f), (b[0], b[1], .02f), (hip, fk[0], .03f), (fk[0], fk[1], .026f), (hip, bk[0], .03f), (bk[0], bk[1], .026f), (chest, cape, .045f) };
            ForBox(ox - .6f, 0f, ox + .7f, 1f, (idx, x, y) =>
            {
                float d = Mathf.Sqrt((x - head.x) * (x - head.x) + (y - head.y) * (y - head.y)) - .05f;
                foreach (var c in caps) d = Mathf.Min(d, Seg(x, y, c.a.x, c.a.y, c.b.x, c.b.y, out _) - c.r);
                float rim = Mathf.Clamp01(1 - Mathf.Abs(d) * H * 0.35f) * Mathf.Clamp01(glow[idx]);
                if (d < 0) lum[idx] = 0.02f + 0.15f * rim; lum[idx] = Mathf.Max(lum[idx], rim * 0.8f);
                if (Seg(x, y, f[1].x, f[1].y, tip.x, tip.y, out _) - .008f < 0) lum[idx] = 0.9f;
            });
        }
    }
}
