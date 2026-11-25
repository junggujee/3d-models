using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 물 연출(SparkBurst 처럼 장면마다 ParticleSystem 하나씩을 풀로 두고 Emit — 부를 때 새 물체를 만들지 않음).
    ///  · Splash: 물방울(위로 튀었다 떨어짐) — 물에 들어갈 때 · 배 뱃머리
    ///  · Bubbles: 공기 방울(천천히 떠오르며 흔들) — 잠수 중 숨
    ///  · Smoke: 통통배 굴뚝 연기(부풀며 떠오르다 사라짐) — "통통"
    ///  · Foam: 물 위 흰 거품(퍼지며 사라짐) — 배 뒤 물결 · 헤엄칠 때
    /// </summary>
    public static class WaterFx
    {
        static ParticleSystem splash, bubbles, smoke, foam; static Material mat;

        static Material Mat()
        {
            if (mat != null) return mat;
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit"); if (sh == null) sh = Shader.Find("Particles/Standard Unlit"); if (sh == null) sh = Shader.Find("Sprites/Default");
            mat = new Material(sh) { name = "WaterFx" }; if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            var tex = SoftDot(); if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex); mat.mainTexture = tex;
            return mat;
        }
        /// <summary>가운데가 진하고 가장자리가 흐린 동그라미(32²).</summary>
        static Texture2D SoftDot()
        {
            const int N = 32; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "WaterFxDot" }; var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) { float dx = (x + 0.5f) / N * 2 - 1, dy = (y + 0.5f) / N * 2 - 1, a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy)); px[y * N + x] = new Color32(255, 255, 255, (byte)(a * a * 255)); }
            t.SetPixels32(px); t.Apply(); return t;
        }

        static ParticleSystem Make(string name, float gravity, int max, ParticleSystemRenderMode mode, Gradient fade, bool grow, float noise)
        {
            var go = new GameObject(name); var ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = max; main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = gravity; main.startSpeed = 0f;
            var em = ps.emission; em.enabled = false; var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true; col.color = fade;
            if (grow) { var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 0.6f, 1, 2.2f)); }
            if (noise > 0) { var n = ps.noise; n.enabled = true; n.strength = noise; n.frequency = 0.8f; n.quality = ParticleSystemNoiseQuality.Low; }
            var r = go.GetComponent<ParticleSystemRenderer>(); r.material = Mat(); r.renderMode = mode; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            if (mode == ParticleSystemRenderMode.Stretch) { r.lengthScale = 1.6f; r.velocityScale = 0.03f; }
            if (mode == ParticleSystemRenderMode.HorizontalBillboard) r.alignment = ParticleSystemRenderSpace.World;
            ps.Play(); return ps;
        }
        static Gradient Fade(float a0, float holdTo)
        {
            var g = new Gradient(); g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(a0, 0), new GradientAlphaKey(a0, holdTo), new GradientAlphaKey(0, 1) }); return g;
        }
        static ParticleSystem Splashes() { if (splash == null) splash = Make("WaterSplashPool", 1.2f, 1500, ParticleSystemRenderMode.Stretch, Fade(0.9f, 0.6f), false, 0); return splash; }
        static ParticleSystem Bubbles() { if (bubbles == null) bubbles = Make("WaterBubblePool", -0.12f, 600, ParticleSystemRenderMode.Billboard, Fade(0.8f, 0.8f), false, 0.4f); return bubbles; }
        static ParticleSystem Smokes() { if (smoke == null) smoke = Make("BoatSmokePool", -0.04f, 400, ParticleSystemRenderMode.Billboard, Fade(0.7f, 0.3f), true, 0.3f); return smoke; }
        static ParticleSystem Foams() { if (foam == null) foam = Make("WaterFoamPool", 0f, 1500, ParticleSystemRenderMode.HorizontalBillboard, Fade(0.75f, 0.35f), true, 0); return foam; }

        static void Emit(ParticleSystem ps, ParticleSystem.EmitParams ep) { if (!ps.isPlaying) ps.Play(); ps.Emit(ep, 1); }

        /// <summary>물방울 튀기기(물에 풍덩 · 뱃머리). up = 위로 세기.</summary>
        public static void Splash(Vector3 at, int count = 24, float up = 3.5f, float spread = 1.6f)
        {
            var ps = Splashes(); var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                var r = Random.insideUnitCircle * spread; ep.position = at + new Vector3(r.x * 0.3f, 0.05f, r.y * 0.3f);
                ep.velocity = new Vector3(r.x, Random.Range(0.5f, 1f) * up, r.y); ep.startLifetime = Random.Range(0.5f, 1.0f); ep.startSize = Random.Range(0.04f, 0.11f);
                ep.startColor = new Color(0.85f, 0.95f, 1f, 0.9f); Emit(ps, ep);
            }
        }
        /// <summary>공기 방울(물속).</summary>
        public static void Bubble(Vector3 at, int count = 4)
        {
            var ps = Bubbles(); var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                ep.position = at + Random.insideUnitSphere * 0.08f; ep.velocity = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(0.5f, 0.9f), Random.Range(-0.1f, 0.1f));
                ep.startLifetime = Random.Range(1.5f, 2.6f); ep.startSize = Random.Range(0.04f, 0.1f); ep.startColor = new Color(0.85f, 0.97f, 1f, 0.85f); Emit(ps, ep);
            }
        }
        /// <summary>굴뚝 연기 한 뭉치("통").</summary>
        public static void Smoke(Vector3 at, Vector3 wind, float strength = 1f)
        {
            var ps = Smokes(); var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < 3; i++)
            {
                ep.position = at + Random.insideUnitSphere * 0.05f; ep.velocity = Vector3.up * Random.Range(0.8f, 1.3f) * strength + wind + Random.insideUnitSphere * 0.15f;
                ep.startLifetime = Random.Range(1.4f, 2.2f); ep.startSize = Random.Range(0.22f, 0.34f) * (0.8f + 0.3f * strength); float g = Random.Range(0.55f, 0.72f); ep.startColor = new Color(g, g, g, 0.75f);
                ep.rotation = Random.Range(0f, 360f); Emit(ps, ep);
            }
        }
        /// <summary>물 위 거품(배 뒤 물결 · 헤엄).</summary>
        public static void Foam(Vector3 at, Vector3 drift, int count = 2, float size = 0.6f)
        {
            var ps = Foams(); var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                var r = Random.insideUnitCircle * 0.3f; ep.position = at + new Vector3(r.x, 0.03f, r.y); ep.velocity = drift + new Vector3(r.x, 0, r.y);
                ep.startLifetime = Random.Range(1.2f, 2.2f); ep.startSize = Random.Range(0.7f, 1.2f) * size; ep.startColor = new Color(0.95f, 0.98f, 1f, 0.75f); ep.rotation = Random.Range(0f, 360f); Emit(ps, ep);
            }
        }
    }
}
