using System.Collections.Generic;
using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>
    /// 스킬 일러스트 v3(흑백 먹선풍): 인물 실루엣 + 마른 붓결 빛 획 + 연기·먹 얼룩 바탕 + 흰 먹 튀김 + 하프톤 + 비네트, 속성색은 아주 조금.
    /// SkillDefinition.illustration 에 그림을 넣으면 그 그림을 쓰고, 비어 있으면 motif 로 코드 생성(스킬 id 로 모양이 고정).
    ///   베기 = 큰 빛의 호 · 찌르기 = 광선과 수렴선 · 내려찍기 = 아래서 터지는 가시 폭발 · 돌진 = 가로 속도선 · 연타 = 엇갈린 궤적
    /// 획마다 영향 범위(bbox)만 계산해서 스킬당 수십 ms 안에 그려지고, 한 번 그리면 캐시.
    /// (같은 알고리즘의 Python 시제품: tools/illustration_preview.py)
    /// </summary>
    public static class SkillIllustration
    {
        const int W = 384, H = 200; const float ASP = (float)W / H;
        static readonly Dictionary<string, Sprite> cache = new();
        static float[] lum, glow, acc, neon; static System.Random rng;
        /// <summary>레이저 블레이더 스킬: 칼날 둘레 네온 번짐(neon) + 동작 획을 시안 광선색으로.</summary>
        static readonly Color NeonCyan = new Color(0.25f, 0.78f, 1f);
        static float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        static int StableHash(string s) { unchecked { int h = (int)2166136261; foreach (char c in s) h = (h ^ c) * 16777619; return h; } }

        public static Sprite Get(SkillDefinition s)
        {
            if (s == null) return null; if (s.illustration != null) return s.illustration;
            string key = s.skillId + "_" + s.motif + "_" + s.attribute + "_" + s.subject; if (cache.TryGetValue(key, out var sp)) return sp;
            rng = new System.Random(StableHash(s.skillId)); lum = new float[W * H]; glow = new float[W * H]; acc = new float[W * H];
            int seed = StableHash(s.skillId) & 0xff;
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
            {
                float x = (i + .5f) / W * ASP, y = (j + .5f) / H;   // 어두운 종이 + 먹 얼룩 + 거친 입자
                lum[j * W + i] = 0.13f + 0.08f * y + (float)rng.NextDouble() * 0.05f + (SkillPanelArt.Noise(x * 4 + seed, y * 4) - .5f) * 0.2f + (SkillPanelArt.Noise(x * 13, y * 13 + seed) - .5f) * 0.06f;
            }
            float fx = R(0.45f, 0.7f); float[] pose;
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)   // 연기 같은 밝은 배경(실루엣이 보이게)
            {
                float x = (i + .5f) / W * ASP, y = (j + .5f) / H, dx = x - fx - .35f, dy = y - .5f;
                lum[j * W + i] += 0.22f * Mathf.Exp(-(dx * dx / .25f + dy * dy / .35f)) * (0.6f + 0.8f * SkillPanelArt.Noise(x * 6, y * 6 + 3));
            }
            WhalePose? wp = null; CubePose? cp = null;
            if (s.subject == IllustrationSubject.Whale) { wp = WhaleScene(s.motif, fx); pose = null; }
            else if (s.subject == IllustrationSubject.Slime || s.subject == IllustrationSubject.Block || s.subject == IllustrationSubject.Seal) { var cc = CubeScene(s.motif, fx, s.subject == IllustrationSubject.Block); cc.seal = s.subject == IllustrationSubject.Seal; cp = cc; pose = null; }
            else switch (s.motif)
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
            LaserScene(s.skillId, fx);   // 레이저 블레이더: 스킬마다 다른 빛 무늬(네온은 색 입힐 때)
            // 먹 얼룩(방울 + 주변 작은 튀김)
            for (int k = 0; k < 18; k++)
            {
                float cx = R(0, ASP), cy = R(0, 1), r = R(.006f, .03f);
                for (int m = 0; m < 6; m++) { float ox = m == 0 ? cx : cx + R(-.08f, .08f), oy = m == 0 ? cy : cy + R(-.06f, .06f), rr = m == 0 ? r : r * R(.12f, .35f); Blot(ox, oy, rr, false); }
            }
            // 흰 먹 튀김(연출 쪽에 몰림)
            for (int k = 0; k < 70; k++) Blot(fx + R(-.3f, 1f), R(.05f, .95f), R(.0015f, .006f), true);
            // 빛 번짐 합성 + 대비 + 하프톤
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
            {
                int idx = j * W + i; float v = Mathf.Clamp01(lum[idx] + glow[idx] * 0.35f); v = v * v * (3 - 2 * v);
                if (v > .15f && v < .6f) { float gx = i % 6 - 3f, gy = j % 6 - 3f; v += Mathf.Sqrt(gx * gx + gy * gy) < v * 6f * 0.75f ? .15f : -.08f; }
                lum[idx] = v;
            }
            bool laser = s.skillId.StartsWith("LASER"); neon = new float[W * H];
            if (wp.HasValue) WhaleFigure(wp.Value); else if (cp.HasValue) CubeFigure(cp.Value); else Figure(pose, fx, s.skillId.StartsWith("SHIELD") ? 1 : s.skillId.StartsWith("DUAL") ? 2 : laser ? 3 : 0);
            // 색 입히기
            var c = SkillAttributes.ColorOf(s.attribute); var px = new Color[W * H];
            for (int k = 0; k < px.Length; k++)
            {
                float x = (k % W + .5f) / W, y = (k / W + .5f) / H, vd = Mathf.Sqrt((x - .5f) * 1.6f * (x - .5f) * 1.6f + (y - .5f) * 1.6f * (y - .5f) * 1.6f);
                float v = Mathf.Clamp01(lum[k] * Mathf.Clamp(1 - (vd - .5f) * .9f, .5f, 1f));   // 비네트
                float a = Mathf.Clamp01(acc[k]) * (v > .3f ? 1f : 0f) * .45f, m = Mathf.Max(v, .6f);   // 속성색은 아주 조금
                px[k] = new Color(v * (1 - a * .8f) + c.r * a * m, v * (1 - a * .8f) + c.g * a * m, v * (1 - a * .8f) + c.b * a * m, 1f);
                if (laser)   // 빛나는 레이저: 칼날 둘레 네온 + 동작 획(빛 번짐)이 시안으로, 가운데는 흰 심지
                {
                    float n = Mathf.Clamp01(neon[k] + Mathf.Clamp01(glow[k] - 0.1f) * 0.9f), core = n * n * n * n;
                    var neonCol = Color.Lerp(NeonCyan, Color.white, core); px[k] = Color.Lerp(px[k], neonCol, Mathf.Clamp01(n * 1.1f)); px[k].a = 1f;
                }
            }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; tex.SetPixels(px); tex.Apply();
            sp = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(.5f, .5f), 100f); cache[key] = sp; lum = glow = acc = neon = null; return sp;
        }

        // ── 고래고래 실루엣(옆모습, 오른쪽을 봄) — tools/illustration_preview.py 의 whale() 과 같은 도형
        struct WhalePose { public float cx, cy, s, tilt, jaw, curl; public WhalePose(float cx, float cy, float s, float tilt, float jaw, float curl) { this.cx = cx; this.cy = cy; this.s = s; this.tilt = tilt * Mathf.Deg2Rad; this.jaw = jaw; this.curl = curl; } }
        static Vector2 WP(WhalePose w, float lx, float ly) { float c = Mathf.Cos(w.tilt), s = Mathf.Sin(w.tilt); return new Vector2(w.cx + (lx * c - ly * s) * w.s, w.cy + (lx * s + ly * c) * w.s); }
        static float Ell(float x, float y, Vector2 c, float rx, float ry, float rot)
        {
            float ca = Mathf.Cos(rot), sa = Mathf.Sin(rot), dx = x - c.x, dy = y - c.y, u = dx * ca + dy * sa, v = -dx * sa + dy * ca;
            return (Mathf.Sqrt(u * u / (rx * rx) + v * v / (ry * ry)) - 1f) * Mathf.Min(rx, ry);
        }
        static float WhaleSdf(float x, float y, WhalePose w)
        {
            float s = w.s, t = w.tilt;
            float d = Ell(x, y, WP(w, 0, 0), .32f * s, .13f * s, t);                                   // 몸
            d = Mathf.Min(d, Ell(x, y, WP(w, .2f, .03f), .14f * s, .12f * s, t));                     // 볼록한 이마
            d = Mathf.Min(d, Ell(x, y, WP(w, .37f, -.005f), .13f * s, .035f * s, t + w.jaw * .5f));   // 위 주둥이
            d = Mathf.Min(d, Ell(x, y, WP(w, .33f, -.05f), .11f * s, .028f * s, t - w.jaw));          // 아래턱
            d = Mathf.Min(d, Ell(x, y, WP(w, .02f, .14f), .05f * s, .07f * s, t - .5f));              // 등지느러미
            d = Mathf.Min(d, Ell(x, y, WP(w, .14f, -.12f), .08f * s, .028f * s, t - .6f));            // 가슴지느러미
            Vector2 prev = WP(w, -.22f, 0f);
            for (int k = 1; k <= 8; k++)                                                                // 가늘어지는 꼬리
            {
                float u = k / 8f; Vector2 p = WP(w, -.22f - .34f * u, w.curl * .18f * u * u);
                d = Mathf.Min(d, Seg(x, y, prev.x, prev.y, p.x, p.y, out _) - (.1f - .075f * (k - 1) / 8f) * s); prev = p;
            }
            float ft = t + Mathf.Atan2(w.curl * .36f, -.34f);
            for (int sg = -1; sg <= 1; sg += 2) d = Mathf.Min(d, Ell(x, y, new Vector2(prev.x - .03f * s, prev.y + sg * .05f * s), .09f * s, .025f * s, ft + sg * 1.0f));   // 갈라진 꼬리 지느러미
            return d;
        }
        /// <summary>고래 기술별 연출(빛 획)을 그리고 고래 자세를 돌려줌. 사람 기술의 switch 와 같은 역할.</summary>
        static WhalePose WhaleScene(SkillMotif m, float fx)
        {
            WhalePose w;
            switch (m)
            {
                case SkillMotif.Beam:   // 주둥이를 들고 앞으로 굵은 빛줄기 + 결 + 고리
                {
                    w = new WhalePose(fx - .25f, .4f, 1f, 14f, .45f, -.4f); var tip = WP(w, .5f, -.02f);
                    Stroke(tip.x, tip.y, ASP + .1f, tip.y + .1f, .075f, 1f, .3f, false); Stroke(tip.x, tip.y, ASP + .1f, tip.y + .1f, .03f, 1f, 0f, false);
                    for (int k = 0; k < 22; k++) { float o = R(-.12f, .12f); Stroke(tip.x + R(0, .3f), tip.y + o, ASP + .1f, tip.y + .1f + o * 1.6f, R(.002f, .008f), R(.5f, .95f), R(0, 1) > .6f ? 1 : 0, true); }
                    for (int k = 0; k < 5; k++) { float cx = tip.x + .15f + k * (ASP - tip.x) / 5f; Arc(cx, tip.y + .1f * (cx - tip.x) / (ASP - tip.x), .025f, .13f, 0, 360, .006f, .8f, .4f, 16); }
                    break;
                }
                case SkillMotif.Leap:   // 하늘에서 내리꽂힘 + 땅에서 솟는 충격 가시 + 낮게 퍼지는 충격파
                {
                    w = new WhalePose(fx + .3f, .6f, .8f, -28f, .1f, .7f); float cx = fx + .45f, cy = .04f;
                    for (int k = 0; k < 24; k++) { float a = R(10f, 170f) * Mathf.Deg2Rad, l = R(.15f, .55f); Stroke(cx, cy, cx + Mathf.Cos(a) * l * 1.6f, cy + Mathf.Sin(a) * l, R(.008f, .03f), R(.6f, 1f), R(0, 1) > .6f ? 1 : 0, true); }
                    for (int k = 0; k < 3; k++) Arc(cx, cy, .3f + k * .18f, .05f + k * .025f, 0, 180, .01f, .8f, .3f, 20);
                    for (int k = 0; k < 14; k++) { float x0 = fx + R(-.1f, .5f); Stroke(x0, R(.75f, 1f), x0 + .1f, R(.45f, .6f), R(.002f, .006f), R(.4f, .8f), 0, true); }
                    break;
                }
                case SkillMotif.Spout:  // 숨구멍에서 하늘로 치솟는 물기둥 + 흩날리는 물방울
                {
                    w = new WhalePose(fx + .15f, .26f, .95f, 4f, .05f, .2f); var hole = WP(w, .16f, .15f);
                    for (int k = 0; k < 10; k++) { float o = R(-.035f, .035f); Stroke(hole.x + o, hole.y, hole.x + o * 2.5f, 1.05f, R(.01f, .03f), R(.7f, 1f), R(0, 1) > .5f ? 1 : 0, false); }
                    for (int k = 0; k < 40; k++) { float a = R(20f, 160f) * Mathf.Deg2Rad, l = R(.15f, .5f); float px = hole.x + Mathf.Cos(a) * l * 1.3f, py = .9f + Mathf.Sin(a) * l * .2f - l * .4f; Stroke(px, py, px + .01f, py - .02f, R(.004f, .012f), R(.6f, 1f), 0, true); }
                    break;
                }
                case SkillMotif.Rush:   // 들이받음: 뒤로 흐르는 속도선 + 주둥이 끝 충격
                {
                    w = new WhalePose(fx + .32f, .45f, .9f, -6f, .1f, .1f); var tip = WP(w, .5f, -.02f);
                    for (int k = 0; k < 34; k++) { float y = R(.1f, .9f), x0 = R(0, fx); Stroke(x0, y, x0 - R(.3f, .8f), y, R(.002f, .012f), R(.3f, .9f), R(0, 1) > .75f ? 1 : 0, true); }
                    for (int k = 0; k < 16; k++) { float a = R(-80f, 80f) * Mathf.Deg2Rad; Stroke(tip.x, tip.y, tip.x + Mathf.Cos(a) * R(.1f, .3f), tip.y + Mathf.Sin(a) * R(.1f, .3f), R(.006f, .02f), R(.7f, 1f), R(0, 1) > .6f ? 1 : 0, true); }
                    break;
                }
                case SkillMotif.Barrage: // 회전 탄막: 고래 둘레로 소용돌이치며 퍼지는 빛 구슬 + 도는 궤적
                {
                    w = new WhalePose(fx + .2f, .45f, .85f, 0f, .25f, .2f); float cx = fx + .2f, cy = .45f;
                    for (int k = 0; k < 26; k++) { float t = k / 26f, a = t * 4.2f * Mathf.PI, r = .25f + t * .55f; Arc(cx + Mathf.Cos(a) * r * 1.2f, cy + Mathf.Sin(a) * r * .8f, .028f, .028f, 0, 360, .009f, R(.75f, 1f), 1, 10); }
                    for (int k = 0; k < 4; k++) Arc(cx, cy, .3f + k * .17f, .2f + k * .12f, R(0f, 90f), R(250f, 360f), .004f, .6f, .3f, 22);
                    break;
                }
                case SkillMotif.Bomb:    // 딸기 폭탄: 숨구멍에서 던진 딸기가 포물선으로 떨어져 퍽 터짐
                {
                    w = new WhalePose(fx + .35f, .3f, .9f, 8f, .2f, .3f); var hole = WP(w, .16f, .15f);
                    for (int k = 0; k < 4; k++)
                    {
                        float tx = hole.x - .2f - k * .28f, ty = R(.05f, .2f), px = hole.x, py = hole.y;
                        for (int j = 1; j <= 10; j++) { float u = j / 10f, nx = Mathf.Lerp(hole.x, tx, u), ny = Mathf.Lerp(hole.y, ty, u) + Mathf.Sin(u * Mathf.PI) * .35f; Stroke(px, py, nx, ny, .003f, .5f, 0, true); px = nx; py = ny; }
                        Arc(tx, ty + .05f, .05f, .06f, 0, 360, .016f, 1f, 1, 14); Stroke(tx - .03f, ty + .11f, tx + .03f, ty + .11f, .012f, .9f, 0, false);
                        for (int j = 0; j < 8; j++) { float a = R(0f, 6.28f); Stroke(tx, ty, tx + Mathf.Cos(a) * .12f, ty + Mathf.Sin(a) * .08f, R(.004f, .01f), R(.6f, 1f), 1, true); }
                    }
                    break;
                }
                case SkillMotif.Bubbles: // 버블 감옥: 입에서 나온 커다란 물방울들(반짝임)
                {
                    w = new WhalePose(fx - .1f, .4f, .9f, 4f, .45f, -.2f); var tip = WP(w, .5f, -.02f);
                    for (int k = 0; k < 6; k++)
                    {
                        float r = R(.06f, .16f), bx = tip.x + .15f + k * .16f + R(-.03f, .03f), by = tip.y + R(-.15f, .2f);
                        Arc(bx, by, r, r, 0, 360, .006f, 1f, .4f, 20); Arc(bx - r * .35f, by + r * .35f, r * .3f, r * .3f, 100, 170, .006f, 1f, 1, 8);
                    }
                    break;
                }
                case SkillMotif.Flurry: // 지느러미 주변에 엇갈린 궤적
                {
                    w = new WhalePose(fx + .3f, .5f, .9f, 6f, .05f, -.3f); var fp = WP(w, .14f, -.12f);
                    for (int k = 0; k < 7; k++) Arc(fp.x + R(-.05f, .25f), fp.y + R(-.1f, .05f), R(.12f, .3f), R(.1f, .22f), R(0, 360), R(0, 360), R(.006f, .022f), R(.6f, 1f), R(0, 1) > .5f ? 1 : 0, 14);
                    break;
                }
                default:                // 꼬리 휘두르기: 꼬리를 휘감는 큰 호
                {
                    w = new WhalePose(fx + .25f, .42f, .9f, 0f, .05f, .9f);
                    Arc(fx + .05f, .35f, .62f, .42f, 200f, 40f, .045f, 1f, .3f, 26);
                    for (int k = 0; k < 7; k++) Arc(fx + .05f + R(-.05f, .05f), .35f + R(-.05f, .05f), R(.5f, .7f), R(.32f, .48f), R(190f, 215f), R(30f, 60f), R(.002f, .009f), R(.5f, .9f), R(0, 1) > .6f ? 1 : 0, 26);
                    break;
                }
            }
            return w;
        }
        /// <summary>고래 실루엣(검정) + 빛 쪽 가장자리 하이라이트 + 눈의 흰 점 + 숨구멍 물방울.</summary>
        static void WhaleFigure(WhalePose w)
        {
            var eye = WP(w, .27f, .02f); var hole = WP(w, .16f, .15f);
            ForBox(0f, 0f, ASP, 1f, (idx, x, y) =>
            {
                float d = WhaleSdf(x, y, w); float rim = Mathf.Clamp01(1 - Mathf.Abs(d) * H * 0.35f) * Mathf.Clamp01(glow[idx]);
                if (d < 0) lum[idx] = 0.02f + 0.15f * rim; lum[idx] = Mathf.Max(lum[idx], rim * 0.8f);
                if ((x - eye.x) * (x - eye.x) + (y - eye.y) * (y - eye.y) < .012f * w.s * .012f * w.s) lum[idx] = 0.95f;
                for (int k = 0; k < 3; k++)
                {
                    float dx = k == 0 ? 0f : k == 1 ? .03f : -.03f, dy = k == 0 ? .05f : k == 1 ? .075f : .07f, rr = (k == 0 ? .018f : .012f) * w.s;
                    float px = hole.x + dx * w.s, py = hole.y + dy * w.s; if ((x - px) * (x - px) + (y - py) * (y - py) < rr * rr) lum[idx] = 0.9f;
                }
            });
        }

        // ── 일반 몬스터 실루엣(핑크큐브말랑이 · 그린볼런) — tools/illustration_preview.py 의 cube()/gear() 와 같은 도형
        struct CubePose
        {
            public float cx, cy, s, sx, sy, tilt; public bool block, seal; public bool gear; public float gx, gy, gr;
            public CubePose(float cx, float cy, float s, float sx, float sy, float tiltDeg, bool block) { this.cx = cx; this.cy = cy; this.s = s; this.sx = sx; this.sy = sy; tilt = tiltDeg * Mathf.Deg2Rad; this.block = block; seal = false; gear = false; gx = gy = gr = 0; }
        }
        static Vector2 CP(CubePose c, float lx, float ly) { float ct = Mathf.Cos(c.tilt), st = Mathf.Sin(c.tilt); return new Vector2(c.cx + (lx * c.sx * ct - ly * c.sy * st) * c.s, c.cy + (lx * c.sx * st + ly * c.sy * ct) * c.s); }
        static float SMin(float a, float b, float k) { float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) * k); return b * (1 - h) + a * h - h * (1 - h) / k; }
        static float BoxD(float u, float v, float hx, float hy) { float qx = Mathf.Abs(u) - hx, qy = Mathf.Abs(v) - hy; return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0); }
        static float CubeSdf(float x, float y, CubePose c)
        {
            float ct = Mathf.Cos(c.tilt), st = Mathf.Sin(c.tilt), dx = x - c.cx, dy = y - c.cy;
            float u = (dx * ct + dy * st) / (c.s * c.sx), v = (-dx * st + dy * ct) / (c.s * c.sy), k = c.s * Mathf.Min(c.sx, c.sy);
            if (c.seal)
            {   // 하프물범 옆모습: 통통한 타원 몸 + 앞·뒤 지느러미(오른쪽을 봄)
                float d0 = (Mathf.Sqrt(u * u / (.3f * .3f) + (v + .02f) * (v + .02f) / (.19f * .19f)) - 1f) * .19f;
                d0 = Mathf.Min(d0, (Mathf.Sqrt((u - .08f) * (u - .08f) / (.09f * .09f) + (v + .2f) * (v + .2f) / (.035f * .035f)) - 1f) * .035f);
                d0 = Mathf.Min(d0, (Mathf.Sqrt((u + .32f) * (u + .32f) / (.08f * .08f) + (v + .08f) * (v + .08f) / (.03f * .03f)) - 1f) * .03f);
                return d0 * k;
            }
            if (!c.block)
            {   // 혹 3×3 이 부드럽게 뭉친 네모
                float d = float.MaxValue;
                for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) { float e = new Vector2(u - i * .13f, v - j * .13f).magnitude - .085f; d = d == float.MaxValue ? e : SMin(d, e, 40f); }
                return d * k;
            }
            float b = BoxD(u, v, .2f, .2f);
            for (int i = -1; i <= 1; i += 2) for (int j = -1; j <= 1; j += 2) b = Mathf.Max(b, -BoxD(u - i * .2f, v - j * .2f, .055f, .055f));   // 계단 모서리
            b = Mathf.Max(b, -BoxD(u, v, .07f, .07f)) * k;                                                                                         // 가운데 구멍
            if (!c.gear) { var g = CP(c, .24f, 0f); b = Mathf.Min(b, GearSdf(x, y, g.x, g.y, .085f * c.s)); }                                     // 옆에 붙은 톱니
            return b;
        }
        static float GearSdf(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx, dy = y - cy, rr = Mathf.Sqrt(dx * dx + dy * dy), th = Mathf.Atan2(dy, dx);
            float R = Mathf.Cos(8 * th) > 0.3f ? r : r * .74f; return Mathf.Max(rr - R, -(rr - r * .3f));
        }
        /// <summary>일반 몬스터 기술별 연출(획) + 자세. Rush 말랑 박치기 · Thrust 쭈욱 늘이기 · Flurry 통통 연속 · Smash 블럭 박치기 · Throw 톱니 던지기.</summary>
        static CubePose CubeScene(SkillMotif m, float fx, bool block)
        {
            CubePose c;
            switch (m)
            {
                case SkillMotif.Thrust:
                {
                    c = new CubePose(fx + .28f, .36f, 1f, 2.1f, .68f, 0f, block); float tx = fx + .28f + .21f * 2.1f, ty = .36f;
                    for (int k = 0; k < 18; k++) { float y = R(.15f, .75f); Stroke(R(tx - .1f, ASP), y, ASP + .2f, .36f + (y - .36f) * .3f, R(.002f, .008f), R(.4f, .9f), R(0, 1) > .7f ? 1 : 0, true); }
                    for (int k = 0; k < 12; k++) { float a = R(-70f, 70f) * Mathf.Deg2Rad; Stroke(tx, ty, tx + Mathf.Cos(a) * R(.08f, .22f), ty + Mathf.Sin(a) * R(.08f, .22f), R(.005f, .015f), R(.7f, 1f), R(0, 1) > .6f ? 1 : 0, true); }
                    break;
                }
                case SkillMotif.Flurry:
                    c = new CubePose(fx + .38f, .5f, .85f, .88f, 1.18f, -6f, block);
                    for (int k = 0; k < 3; k++) Arc(fx - .45f + k * .3f, .22f, .15f, .2f + k * .03f, 180f, 0f, .012f, .9f, .4f, 16);
                    for (int k = 0; k < 3; k++) Arc(fx - .3f + k * .3f, .12f, .07f, .02f, 0f, 360f, .006f, .6f, .2f, 12);
                    break;
                case SkillMotif.Smash:
                {
                    c = new CubePose(fx + .25f, .4f, 1f, 1f, 1f, -22f, block); float ix = fx + .55f, iy = .2f;
                    for (int k = 0; k < 22; k++) { float a = R(-60f, 120f) * Mathf.Deg2Rad, l = R(.15f, .5f); Stroke(ix, iy, ix + Mathf.Cos(a) * l, iy + Mathf.Sin(a) * l, R(.008f, .03f), R(.6f, 1f), R(0, 1) > .6f ? 1 : 0, true); }
                    break;
                }
                case SkillMotif.Throw:
                    c = new CubePose(fx - .1f, .38f, .9f, 1f, 1f, 8f, block) { gear = true, gx = fx + .62f, gy = .55f, gr = .09f };
                    for (int k = 0; k < 16; k++) { float y = R(.42f, .68f), x0 = R(fx + .05f, fx + .5f); Stroke(x0, y, x0 - R(.15f, .45f), y - .04f, R(.002f, .01f), R(.4f, .9f), R(0, 1) > .6f ? 1 : 0, true); }
                    for (int k = 0; k < 3; k++) { float a0 = R(0f, 90f) + 120f * k; Arc(c.gx, c.gy, .13f + k * .03f, .13f + k * .03f, a0, a0 + 70f, .006f, .9f, .5f, 10); }
                    break;
                default:   // Rush: 옆으로 눌리며 들이받음 + 속도선
                    c = new CubePose(fx + .3f, .4f, 1f, 1.25f, .84f, -8f, block);
                    for (int k = 0; k < 34; k++) { float y = R(.1f, .9f), x0 = R(0, fx); Stroke(x0, y, x0 - R(.3f, .8f), y, R(.002f, .012f), R(.3f, .9f), R(0, 1) > .75f ? 1 : 0, true); }
                    break;
            }
            return c;
        }
        /// <summary>실루엣(검정) + 가장자리 빛. 핑크큐브 = 흰 눈알·검은 눈동자·흰 미소, 던진 톱니 = 밝게(구멍은 어둡게).</summary>
        static void CubeFigure(CubePose c)
        {
            ForBox(0f, 0f, ASP, 1f, (idx, x, y) =>
            {
                float d = CubeSdf(x, y, c); float rim = Mathf.Clamp01(1 - Mathf.Abs(d) * H * 0.35f) * Mathf.Clamp01(glow[idx]);
                if (d < 0) lum[idx] = 0.02f + 0.15f * rim; lum[idx] = Mathf.Max(lum[idx], rim * 0.8f);
                if (c.seal)
                {   // 검은 자수 눈 대신 흰 점 눈(실루엣 위) · 회색 퐁퐁 코
                    var e = CP(c, .2f, .03f); if ((x - e.x) * (x - e.x) + (y - e.y) * (y - e.y) < (.022f * c.s) * (.022f * c.s)) lum[idx] = 0.92f;
                    var n = CP(c, .29f, -.04f); if ((x - n.x) * (x - n.x) + (y - n.y) * (y - n.y) < (.03f * c.s) * (.03f * c.s)) lum[idx] = 0.6f;
                }
                else if (!c.block)
                {
                    for (int sg = -1; sg <= 1; sg += 2)
                    {
                        var e = CP(c, sg * .052f, .06f); float ex = x - e.x, ey = y - e.y;
                        if (ex * ex + ey * ey < (.036f * c.s) * (.036f * c.s)) lum[idx] = 0.92f;
                        if ((ex - .004f) * (ex - .004f) + ey * ey < (.015f * c.s) * (.015f * c.s)) lum[idx] = 0.03f;
                    }
                    for (int k = 0; k < 14; k++)
                    {
                        float t0 = -1 + 2f * k / 14, t1 = -1 + 2f * (k + 1) / 14; var a = CP(c, .11f * t0, -.045f + .035f * t0 * t0); var b = CP(c, .11f * t1, -.045f + .035f * t1 * t1);
                        if (Seg(x, y, a.x, a.y, b.x, b.y, out _) < .012f * c.s) lum[idx] = 0.85f;
                    }
                }
                if (c.gear && c.seal)
                {   // 날아가는 물방울(밝은 원 + 어두운 반짝임 자리)
                    float dd = (x - c.gx) * (x - c.gx) + (y - c.gy) * (y - c.gy); if (dd < c.gr * c.gr) lum[idx] = dd < (c.gr * .55f) * (c.gr * .55f) ? 0.75f : 0.92f;
                }
                else if (c.gear)
                {
                    if (GearSdf(x, y, c.gx, c.gy, c.gr) < 0) lum[idx] = 0.9f;
                    if ((x - c.gx) * (x - c.gx) + (y - c.gy) * (y - c.gy) < (c.gr * .3f) * (c.gr * .3f)) lum[idx] = 0.05f;
                }
            });
        }

        static void Blot(float cx, float cy, float r, bool white) => ForBox(cx - r, cy - r, cx + r, cy + r, (idx, x, y) =>
        {
            if ((x - cx) * (x - cx) + (y - cy) * (y - cy) >= r * r) return;
            if (white) glow[idx] = 2.5f; else lum[idx] = Mathf.Min(lum[idx], 0.03f);
        });

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
                float L = Mathf.Max(1e-9f, Mathf.Sqrt((bx - ax) * (bx - ax) + (by - ay) * (by - ay))), across = ((x - ax) * -(by - ay) + (y - ay) * (bx - ax)) / L;
                float core = Mathf.Clamp01(0.5f - d * H * 0.8f) * Mathf.Clamp01(0.45f + 0.75f * SkillPanelArt.Noise(h * L * 8, across * 260 + 17));   // 마른 붓결
                lum[idx] = Mathf.Max(lum[idx], core * l); glow[idx] += Mathf.Exp(-Mathf.Max(d, 0) * 40f) * w * 6f * l; acc[idx] = Mathf.Max(acc[idx], core * ac);
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

        /// <summary>레이저 블레이더 스킬 무늬(난수 없음 — tools/illustration_preview.py 의 laser_scene 과 같음).</summary>
        static void LaserScene(string id, float fx)
        {
            switch (id)
            {
                case "LASER_2": Arc(fx + .45f, .45f, .95f, .16f, 200f, -20f, .03f, 1f, 0f, 28); Arc(fx + .4f, .52f, .7f, .1f, 195f, -10f, .015f, .9f, 0f, 24); break;   // 넓게 휩쓰는 가로 광선
                case "LASER_3": Stroke(fx + .5f, .02f, fx + .5f, .98f, .022f, 1f, 0f, true); Stroke(fx - .1f, .5f, fx + 1.1f, .5f, .02f, 1f, 0f, true); break;   // 십자 광선
                case "LASER_Q":   // 날아가는 광륜(고리) + 뒤로 남는 줄
                    Arc(fx + 1.05f, .6f, .17f, .1f, 0f, 360f, .018f, 1f, 0f, 30); Arc(fx + 1.05f, .6f, .1f, .06f, 0f, 360f, .01f, 1f, 0f, 24);
                    for (int k = 0; k < 5; k++) { float y = .52f + k * .04f; Stroke(fx + .35f, y, fx + .88f, .6f + (y - .6f) * .4f, .006f, .8f, 0f, true); }
                    break;
                case "LASER_E": foreach (var (rx, ry) in new[] { (.5f, .18f), (.72f, .26f), (.95f, .34f) }) Arc(fx, .42f, rx, ry, 0f, 360f, .012f, .9f, 0f, 36); break;   // 겹겹 빛 고리
                case "LASER_R":   // 몸을 감싸는 큰 광륜 + 바퀴살
                    Arc(fx + .05f, .5f, .85f, .48f, 0f, 360f, .03f, 1f, 0f, 40);
                    for (int k = 0; k < 16; k++) { float a = k * Mathf.PI * 2f / 16f; Stroke(fx + .05f + Mathf.Cos(a) * .3f, .5f + Mathf.Sin(a) * .17f, fx + .05f + Mathf.Cos(a) * .55f, .5f + Mathf.Sin(a) * .31f, .008f, .85f, 0f, true); }
                    break;
            }
        }

        /// <summary>인물 실루엣(캡슐 SDF): 검은 몸 + 빛 쪽 가장자리 하이라이트 + 밝은 칼날. pose = 몸 기울기, 앞팔, 뒷팔, 앞다리, 뒷다리, 무기 각도(도).</summary>
        /// <summary>무기 모양(밝은 실루엣)까지의 거리: 0 검(앞손에서 무기 각도로 뻗은 날) · 1 베개 방패(앞손의 땅콩 모양) · 2 회전 쌍검(양손 끝 바람개비 두 날).</summary>
        static float PropDist(float x, float y, int prop, Vector2 front, Vector2 back, float wa)
        {
            var dir = new Vector2(Mathf.Cos(wa), Mathf.Sin(wa));
            if (prop == 1)
            {
                var c = front + dir * .05f; var n = new Vector2(-dir.y, dir.x);
                float a = Mathf.Sqrt((x - (c.x + n.x * .055f)) * (x - (c.x + n.x * .055f)) + (y - (c.y + n.y * .055f)) * (y - (c.y + n.y * .055f))) - .07f;
                float b = Mathf.Sqrt((x - (c.x - n.x * .055f)) * (x - (c.x - n.x * .055f)) + (y - (c.y - n.y * .055f)) * (y - (c.y - n.y * .055f))) - .07f;
                return Mathf.Min(a, b);
            }
            if (prop == 2)
            {
                float d = 9f;
                foreach (var (h, sg) in new[] { (front, 1f), (back, -1f) })
                {
                    var c = h + dir * .04f; var n = new Vector2(-dir.y, dir.x);
                    for (int k = -1; k <= 1; k += 2)
                    {
                        var m1 = c + n * (.09f * k) + dir * (.03f * sg * k); var t1 = c + n * (.17f * k) + dir * (.09f * sg * k);   // 휘어 감긴 초승달 날
                        d = Mathf.Min(d, Seg(x, y, c.x, c.y, m1.x, m1.y, out _) - .011f); d = Mathf.Min(d, Seg(x, y, m1.x, m1.y, t1.x, t1.y, out _) - .006f);
                    }
                    d = Mathf.Min(d, Mathf.Sqrt((x - c.x) * (x - c.x) + (y - c.y) * (y - c.y)) - .016f);
                }
                return d;
            }
            if (prop == 3)   // 레이저 블레이더: 손의 고리를 가운데로 양쪽으로 뻗은 긴 광선
            {
                var a = front + dir * .5f; var b = front - dir * .38f;
                float d = Seg(x, y, b.x, b.y, a.x, a.y, out _) - .009f; float rr = Mathf.Sqrt((x - front.x) * (x - front.x) + (y - front.y) * (y - front.y));
                return Mathf.Min(d, Mathf.Abs(rr - .03f) - .007f);
            }
            var tip = front + dir * .34f; return Seg(x, y, front.x, front.y, tip.x, tip.y, out _) - .008f;
        }

        static void Figure(float[] pose, float ox, int prop = 0)
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
                float pd = PropDist(x, y, prop, f[1], b[1], wa); if (pd < 0) lum[idx] = prop == 1 ? 0.82f : prop == 3 ? 1f : 0.9f;
                if (prop == 3) neon[idx] = Mathf.Max(neon[idx], Mathf.Exp(-Mathf.Max(pd, 0f) * 38f));   // 광선 칼날 번짐
            });
        }
    }
}
