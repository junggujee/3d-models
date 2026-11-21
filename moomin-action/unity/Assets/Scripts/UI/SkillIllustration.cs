using System.Collections.Generic;
using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>
    /// 스킬 일러스트가 없을 때 코드로 그리는 흑백 스케치(어두운 바탕 + 하얀 붓 획). 동작(SkillMotif)과 스킬 id 로 모양이 정해져 스킬마다 다름.
    /// 베기 = 큰 대각선 호 · 찌르기 = 한 점으로 모이는 선 · 내려찍기 = 아래에서 퍼지는 균열 · 돌진 = 가로 속도선 · 연타 = 짧은 획 여러 개.
    /// </summary>
    public static class SkillIllustration
    {
        static readonly Dictionary<string, Sprite> cache = new(); static Sprite hex, hexRing, banner;
        const int W = 256, H = 144;

        public static Sprite Get(SkillDefinition s)
        {
            if (s == null) return null; if (s.illustration != null) return s.illustration;
            string key = s.skillId + "_" + s.motif; if (cache.TryGetValue(key, out var sp)) return sp;
            var rng = new System.Random(s.skillId.GetHashCode()); var segs = new List<(Vector2 a, Vector2 b, float w, float lum)>(); var arcs = new HashSet<int>();   // 호 조각은 끝을 가늘게 하지 않음(구슬처럼 끊겨 보이지 않게)
            float R(float lo, float hi) => lo + (float)rng.NextDouble() * (hi - lo);
            switch (s.motif)
            {
                case SkillMotif.Slash:
                    for (int k = 0; k < 4; k++) { float off = R(-0.25f, 0.25f), w = k == 0 ? 0.035f : R(0.006f, 0.02f); Arc(segs, arcs, new Vector2(0.5f + off, 1.25f), R(0.75f, 0.95f), R(200f, 225f), R(315f, 340f), w, k == 0 ? 1f : R(0.55f, 0.85f)); }
                    break;
                case SkillMotif.Thrust:
                    { var tip = new Vector2(R(0.72f, 0.85f), R(0.4f, 0.6f)); segs.Add((new Vector2(0.05f, tip.y + R(-0.1f, 0.1f)), tip, 0.04f, 1f));
                      for (int k = 0; k < 14; k++) { float a = R(140f, 220f) * Mathf.Deg2Rad; var from = tip + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R(0.35f, 0.9f); segs.Add((from, tip + (from - tip) * R(0.05f, 0.3f), R(0.003f, 0.01f), R(0.4f, 0.8f))); } }
                    break;
                case SkillMotif.Smash:
                    { var c = new Vector2(R(0.4f, 0.6f), 0.12f); segs.Add((new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.12f), 0.02f, 0.9f));
                      for (int k = 0; k < 9; k++) { var p = c; float a = R(20f, 160f) * Mathf.Deg2Rad; for (int j = 0; j < 4; j++) { var q = p + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * R(0.08f, 0.2f); segs.Add((p, q, Mathf.Lerp(0.018f, 0.004f, j / 3f), R(0.6f, 1f))); p = q; a += R(-0.5f, 0.5f); } } }
                    break;
                case SkillMotif.Rush:
                    for (int k = 0; k < 22; k++) { float y = R(0.08f, 0.92f), x0 = R(0f, 0.5f); segs.Add((new Vector2(x0, y), new Vector2(x0 + R(0.25f, 0.6f), y + R(-0.02f, 0.02f)), R(0.003f, 0.014f), R(0.4f, 1f))); }
                    segs.Add((new Vector2(0.55f, 0.35f), new Vector2(0.92f, 0.5f), 0.05f, 1f)); segs.Add((new Vector2(0.55f, 0.65f), new Vector2(0.92f, 0.5f), 0.05f, 1f));
                    break;
                default:   // Flurry
                    for (int k = 0; k < 16; k++) { var c = new Vector2(R(0.15f, 0.85f), R(0.15f, 0.85f)); float a = R(0f, Mathf.PI), l = R(0.1f, 0.3f); var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * l; segs.Add((c - d, c + d, R(0.005f, 0.022f), R(0.5f, 1f))); }
                    break;
            }
            var px = new Color[W * H]; float aspect = (float)W / H;
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
            {
                float x = (i + .5f) / W, y = (j + .5f) / H; float bg = Mathf.Lerp(0.07f, 0.16f, y) + ((i * 73 + j * 151) % 17) / 17f * 0.025f; float lum = bg;
                for (int si = 0; si < segs.Count; si++)
                {
                    var sg = segs[si];
                    var pa = new Vector2((x - sg.a.x) * aspect, y - sg.a.y); var ba = new Vector2((sg.b.x - sg.a.x) * aspect, sg.b.y - sg.a.y);
                    float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-6f, Vector2.Dot(ba, ba))); float taper = arcs.Contains(si) ? 1f : Mathf.Sin(Mathf.PI * Mathf.Clamp(h, 0.03f, 0.97f));   // 붓 획처럼 양끝이 가늘게
                    float d = (pa - ba * h).magnitude - sg.w * taper; float a = Mathf.Clamp01(0.5f - d * H * 0.7f); lum = Mathf.Max(lum, Mathf.Lerp(bg, sg.lum, a));
                }
                px[j * W + i] = new Color(lum, lum, lum, 1f);
            }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; tex.SetPixels(px); tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(.5f, .5f), 100f); cache[key] = sp; return sp;
        }

        static void Arc(List<(Vector2, Vector2, float, float)> segs, HashSet<int> arcs, Vector2 c, float r, float a0, float a1, float w, float lum)
        {
            const int N = 10; Vector2 P(float t) { float a = Mathf.Lerp(a0, a1, t) * Mathf.Deg2Rad; return c + new Vector2(Mathf.Cos(a) * r * 0.6f, Mathf.Sin(a) * r); }
            for (int k = 0; k < N; k++) { arcs.Add(segs.Count); segs.Add((P(k / (float)N), P((k + 1) / (float)N), w * Mathf.Max(0.15f, Mathf.Sin(Mathf.PI * (k + 0.5f) / N)), lum)); }
        }

        // ── 패널용 모양(흰색, Image.color 로 속성색을 입힘)
        static Sprite Make(int w, int h, System.Func<float, float, float> alpha, Vector4 border = default)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; var c = new Color[w * h];
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) c[j * w + i] = new Color(1, 1, 1, alpha((i + .5f) / w, (j + .5f) / h));
            t.SetPixels(c); t.Apply(); return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
        static float HexD(float x, float y) { x = Mathf.Abs(x * 2 - 1); y = Mathf.Abs(y * 2 - 1); return Mathf.Max(x * 0.866f + y * 0.5f, y) - 0.9f; }   // 뾰족한 위아래 육각형
        public static Sprite Hex => hex ??= Make(128, 128, (x, y) => Mathf.Clamp01(0.5f - HexD(x, y) * 64f));
        public static Sprite HexRing => hexRing ??= Make(128, 128, (x, y) => Mathf.Clamp01(0.5f - (Mathf.Abs(HexD(x, y) + 0.06f) - 0.05f) * 64f));
        /// <summary>오른쪽 끝이 비스듬하고 오른쪽으로 갈수록 옅어지는 이름 띠.</summary>
        public static Sprite Banner => banner ??= Make(128, 32, (x, y) => (x < 1f - (1f - y) * 0.12f ? 1f : 0f) * Mathf.Lerp(1f, 0.55f, x), new Vector4(8, 0, 24, 0));
    }
}
