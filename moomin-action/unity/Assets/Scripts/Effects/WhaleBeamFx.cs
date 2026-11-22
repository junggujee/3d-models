using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 고래빔 연출(프리팹 없음). 한 번 쏘면 스킬이 끝날 때까지 <b>계속 유지</b>되고, 코인(판정)마다 한 번씩 불끈 커짐(Pulse).
    ///  · 겹: 바깥 안개(푸른 빛 번짐) · 몸통(색이 하늘색 ↔ 청록 ↔ 연보라 ↔ 흰빛으로 계속 바뀜, 결이 앞으로 흐름) · 흰 심(맥동)
    ///  · 앞으로 흘러가는 빛 고리 6개, 빔을 따라 흐르는 반짝이 입자, 끝부분 물보라, 주둥이 섬광, 깜빡이는 점광원 2개
    ///  · 주인이 쓰러지거나 경직·합 패배로 스킬이 끊기면 0.4초 동안 가늘어지며 사라짐.
    /// 기 모으기(ChargeFx)는 예고(windup) 동안 주둥이 앞에서 빛 구슬이 커지고 주변 빛이 빨려 들어감.
    /// </summary>
    public class WhaleBeamFx : MonoBehaviour
    {
        static readonly Dictionary<Transform, WhaleBeamFx> active = new();
        static Material baseMat, particleMat; static Texture2D streak, ring;
        /// <summary>빔이 나오는 주둥이(로컬, 빔 자세 기준 — 머리를 든 상태).</summary>
        public static readonly Vector3 MouthLocal = new Vector3(0f, 0.9f, 1.35f);

        Transform owner; SkillExecutor ex; SkillDefinition skill; float length, width, t, fadeT = -1f, kick, nextShake; bool heads;
        Transform haze, body, core, flare, endPoint; Material hazeM, bodyM, coreM, flareM; readonly List<Transform> rings = new(); readonly List<Material> ringM = new();
        ParticleSystem sparkle, splash; Light mouthLight, endLight;

        // ── 공용 재료
        internal static Material Mat(Color c, Texture tex = null)
        {
            if (baseMat == null) { var sh = Shader.Find("Sprites/Default"); if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit"); baseMat = new Material(sh); }
            var m = new Material(baseMat) { color = c, renderQueue = 3100 }; if (tex != null) m.mainTexture = tex; return m;
        }
        internal static Material ParticleMat()
        {
            if (particleMat != null) return particleMat;
            var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit"); if (sh == null) sh = Shader.Find("Particles/Standard Unlit"); if (sh == null) sh = Shader.Find("Sprites/Default");
            particleMat = new Material(sh); particleMat.SetColor("_BaseColor", Color.white); return particleMat;
        }
        static Texture2D Streak()   // 길이 방향 결(노이즈 줄무늬) — 스크롤하면 빛이 흐르는 것처럼 보임
        {
            if (streak != null) return streak; const int W = 64, H = 256; streak = new Texture2D(W, H, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat };
            var px = new Color[W * H]; var r = new System.Random(7); var cols = new float[W]; for (int i = 0; i < W; i++) cols[i] = (float)r.NextDouble();
            for (int j = 0; j < H; j++) for (int i = 0; i < W; i++)
            {
                float v = 0.55f + 0.45f * Mathf.Sin(j / (float)H * Mathf.PI * 2f * (2 + (int)(cols[i] * 4)) + cols[i] * 6f);
                float band = 0.6f + 0.4f * Mathf.Sin(j / (float)H * Mathf.PI * 8f + i * 0.3f); px[j * W + i] = new Color(1, 1, 1, Mathf.Clamp01(v * band));
            }
            streak.SetPixels(px); streak.Apply(); return streak;
        }
        internal static Texture2D Ring()
        {
            if (ring != null) return ring; const int N = 96; ring = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var px = new Color[N * N];
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) { float x = (i + .5f) / N * 2 - 1, y = (j + .5f) / N * 2 - 1, d = Mathf.Sqrt(x * x + y * y); px[j * N + i] = new Color(1, 1, 1, Mathf.Clamp01(1f - Mathf.Abs(d - 0.82f) / 0.14f)); }
            ring.SetPixels(px); ring.Apply(); return ring;
        }
        internal static Transform Prim(PrimitiveType type, Transform parent, Material m)
        {
            var go = GameObject.CreatePrimitive(type); Destroy(go.GetComponent<Collider>()); go.transform.SetParent(parent, false);
            var r = go.GetComponent<Renderer>(); r.material = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; return go.transform;
        }
        internal static Vector3 Mouth(Transform owner) => owner.TransformPoint(MouthLocal);

        /// <summary>코인 판정마다 호출: 빔이 없으면 발사, 있으면 불끈(굵어지고 번쩍).</summary>
        public static void FireOrPulse(SkillDefinition s, Transform owner, bool heads)
        {
            if (active.TryGetValue(owner, out var fx) && fx != null && fx.fadeT < 0f) { fx.Pulse(heads); return; }
            var go = new GameObject("WhaleBeam"); fx = go.AddComponent<WhaleBeamFx>(); fx.Init(s, owner, heads); active[owner] = fx;
        }

        void Init(SkillDefinition s, Transform o, bool h)
        {
            owner = o; skill = s; heads = h; ex = o.GetComponent<SkillExecutor>(); length = s.areaRadius; width = s.areaWidth * 0.42f;
            hazeM = Mat(new Color(0.3f, 0.6f, 1f, 0.22f), Streak()); bodyM = Mat(new Color(0.5f, 0.85f, 1f, 0.6f), Streak()); coreM = Mat(new Color(1f, 1f, 1f, 0.95f)); flareM = Mat(new Color(0.8f, 0.95f, 1f, 0.7f));
            haze = Prim(PrimitiveType.Cylinder, transform, hazeM); body = Prim(PrimitiveType.Cylinder, transform, bodyM); core = Prim(PrimitiveType.Cylinder, transform, coreM); flare = Prim(PrimitiveType.Sphere, transform, flareM);
            foreach (var c in new[] { haze, body, core }) c.localRotation = Quaternion.Euler(90, 0, 0);   // 실린더 길이(Y) → 앞(Z)
            for (int i = 0; i < 6; i++) { var m = Mat(new Color(0.75f, 0.95f, 1f, 0.6f), Ring()); ringM.Add(m); rings.Add(Prim(PrimitiveType.Quad, transform, m)); }
            endPoint = new GameObject("End").transform; endPoint.SetParent(transform, false);
            sparkle = Particles(transform, 160f, new Color(0.85f, 0.97f, 1f), new Color(0.4f, 0.75f, 1f), 0.05f, 0.16f, 0.45f, true);
            var sh = sparkle.shape; sh.shapeType = ParticleSystemShapeType.Box; var vel = sparkle.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local; vel.z = new ParticleSystem.MinMaxCurve(8f, 14f);
            splash = Particles(endPoint, 90f, Color.white, new Color(0.45f, 0.8f, 1f), 0.06f, 0.2f, 0.7f, false);
            var ssh = splash.shape; ssh.shapeType = ParticleSystemShapeType.Hemisphere; ssh.radius = 0.4f; var sm = splash.main; sm.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f); sm.gravityModifier = 1.4f; sm.simulationSpace = ParticleSystemSimulationSpace.World;
            endPoint.localRotation = Quaternion.Euler(-90, 0, 0);
            mouthLight = Light(transform, 7f); endLight = Light(endPoint, 5f);
            Follow(); kick = 1f; Burst(); ThirdPersonCamera.Shake(0.12f, 0.45f);
        }
        static ParticleSystem Particles(Transform parent, float rate, Color a, Color b, float s0, float s1, float life, bool stretch)
        {
            var go = new GameObject("Particles"); go.transform.SetParent(parent, false); var ps = go.AddComponent<ParticleSystem>(); ps.Stop();
            var m = ps.main; m.loop = true; m.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life); m.startSize = new ParticleSystem.MinMaxCurve(s0, s1); m.startSpeed = 0.5f; m.simulationSpace = ParticleSystemSimulationSpace.Local; m.maxParticles = 600;
            var em = ps.emission; em.rateOverTime = rate;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) }, new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) }); col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.material = ParticleMat(); if (stretch) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.04f; r.lengthScale = 2f; }
            ps.Play(); return ps;
        }
        static Light Light(Transform parent, float range)
        {
            var go = new GameObject("Light"); go.transform.SetParent(parent, false); var l = go.AddComponent<Light>(); l.type = LightType.Point; l.range = range; l.color = new Color(0.55f, 0.85f, 1f); l.intensity = 0f; return l;
        }

        public void Pulse(bool h) { kick = h ? 1.2f : 0.8f; heads = heads || h; Burst(); ThirdPersonCamera.Shake(h ? 0.1f : 0.07f, 0.25f); }
        void Burst()
        {
            Vector3 m = transform.position; SparkBurst.Spawn(m, transform.forward, 36, 9f, new Color(0.6f, 0.9f, 1f));
            SparkBurst.Spawn(endPoint.position, -transform.forward + Vector3.up, 26, 7f, Color.white); ClashFlash.Spawn(m, new Color(0.55f, 0.85f, 1f), 10f, 9f, 0.25f);
        }

        void Follow()
        {
            if (owner == null) return;
            transform.position = Mouth(owner);
            Vector3 fwd = owner.forward; fwd.y = 0; fwd.Normalize();
            float drop = Mathf.Atan2(Mathf.Max(0f, transform.position.y - owner.position.y - 0.6f), length) * Mathf.Rad2Deg;   // 끝이 땅 가까이(플레이어 높이)로 오게 살짝 아래로
            transform.rotation = Quaternion.LookRotation(fwd) * Quaternion.Euler(drop, 0, 0);
        }

        void LateUpdate()
        {
            if (UIState.Paused) return;
            float dt = Time.deltaTime; t += dt; kick = Mathf.MoveTowards(kick, 0f, dt * 2.5f);
            bool alive = owner != null && ex != null && ex.stats.IsAlive && ex.IsExecuting && ex.CurrentSkill == skill;
            if (fadeT < 0f && (!alive || t > 8f)) { fadeT = 0f; var e1 = sparkle.emission; e1.enabled = false; var e2 = splash.emission; e2.enabled = false; }
            if (fadeT >= 0f) { fadeT += dt; if (fadeT > 0.45f) { if (owner != null && active.TryGetValue(owner, out var f) && f == this) active.Remove(owner); Destroy(gameObject); return; } }
            Follow();
            float grow = Mathf.Clamp01(t / 0.1f), L = length * grow, fade = fadeT < 0f ? 1f : 1f - fadeT / 0.45f;
            float w = width * (heads ? 1.15f : 1f) * (1f + 0.07f * Mathf.Sin(t * 42f) + 0.05f * Mathf.Sin(t * 17f) + 0.45f * kick) * Mathf.Lerp(0.25f, 1f, fade);
            void Cyl(Transform c, float ww) { c.localScale = new Vector3(ww, L * 0.5f, ww); c.localPosition = new Vector3(0, 0, L * 0.5f); }
            Cyl(haze, w * 1.9f); Cyl(body, w); Cyl(core, w * (0.38f + 0.08f * Mathf.Sin(t * 60f)));
            flare.localScale = Vector3.one * w * (1.7f + 0.3f * Mathf.Sin(t * 30f) + kick); endPoint.localPosition = new Vector3(0, 0, L);
            // 색: 하늘색 ↔ 청록 ↔ 연보라 ↔ 흰빛으로 계속 바뀜 + 판정 때 흰 번쩍
            float hue = Mathf.Repeat(0.52f + 0.07f * Mathf.Sin(t * 2.3f) + 0.04f * Mathf.Sin(t * 5.1f), 1f);
            Color bc = Color.Lerp(Color.HSVToRGB(hue, 0.55f, 1f), Color.white, 0.25f + 0.5f * kick); bc.a = 0.6f * fade; bodyM.color = bc;
            Color hc = Color.HSVToRGB(Mathf.Repeat(hue + 0.04f, 1f), 0.6f, 1f); hc.a = (0.18f + 0.1f * Mathf.Sin(t * 9f)) * fade; hazeM.color = hc;
            coreM.color = new Color(1f, 1f, 1f, 0.95f * fade); flareM.color = new Color(0.8f, 0.95f, 1f, (0.55f + 0.3f * kick) * fade);
            float scrollLen = Mathf.Max(1f, L / 3f); bodyM.mainTextureScale = new Vector2(1f, scrollLen); bodyM.mainTextureOffset = new Vector2(t * 0.3f, -t * 3.2f);
            hazeM.mainTextureScale = new Vector2(1f, scrollLen * 0.5f); hazeM.mainTextureOffset = new Vector2(-t * 0.2f, -t * 1.6f);
            // 앞으로 흘러가는 고리
            for (int i = 0; i < rings.Count; i++)
            {
                float u = Mathf.Repeat(t * 0.9f + i / (float)rings.Count, 1f);
                rings[i].localPosition = new Vector3(0, 0, L * u); rings[i].localScale = Vector3.one * w * (1.6f + 0.8f * Mathf.Sin(u * Mathf.PI)) ; rings[i].localRotation = Quaternion.Euler(0, 0, t * 90f + i * 30f);
                var rc = ringM[i].color; rc.a = 0.55f * Mathf.Sin(u * Mathf.PI) * fade; ringM[i].color = rc;
            }
            var sh = sparkle.shape; sh.scale = new Vector3(w * 1.4f, w * 1.4f, L); sh.position = new Vector3(0, 0, L * 0.5f);
            mouthLight.intensity = (5f + 2f * Mathf.Sin(t * 25f) + 6f * kick) * fade; endLight.intensity = (3f + 1.5f * Mathf.Sin(t * 19f + 1f)) * fade * grow;
            if (Time.time >= nextShake && fadeT < 0f) { nextShake = Time.time + 0.15f; ThirdPersonCamera.Shake(0.035f, 0.2f); }
        }
        void OnDestroy() { foreach (var m in new[] { hazeM, bodyM, coreM, flareM }) if (m != null) Destroy(m); foreach (var m in ringM) if (m != null) Destroy(m); }
    }

    /// <summary>고래빔 기 모으기: 주둥이 앞 빛 구슬이 커지고, 주변의 빛 입자·고리가 빨려 들어가며 빛이 점점 세짐. 예고(windup) 시간 동안.</summary>
    public class WhaleChargeFx : MonoBehaviour
    {
        Transform owner, orb; SkillExecutor ex; float t, dur; Material orbM; readonly List<Transform> rings = new(); readonly List<Material> ringM = new(); ParticleSystem gather; Light glow;
        public static void Play(Transform owner, float duration)
        {
            var go = new GameObject("WhaleCharge"); var c = go.AddComponent<WhaleChargeFx>(); c.owner = owner; c.dur = Mathf.Max(0.2f, duration); c.ex = owner.GetComponent<SkillExecutor>();
            c.orbM = WhaleBeamFx.Mat(new Color(0.75f, 0.95f, 1f, 0.8f)); c.orb = WhaleBeamFx.Prim(PrimitiveType.Sphere, go.transform, c.orbM);
            for (int i = 0; i < 2; i++) { var m = WhaleBeamFx.Mat(new Color(0.6f, 0.9f, 1f, 0.5f), WhaleBeamFx.Ring()); c.ringM.Add(m); c.rings.Add(WhaleBeamFx.Prim(PrimitiveType.Quad, go.transform, m)); }
            var pg = new GameObject("Gather"); pg.transform.SetParent(go.transform, false); var ps = pg.AddComponent<ParticleSystem>(); ps.Stop();
            var m0 = ps.main; m0.loop = true; m0.startLifetime = 0.45f; m0.startSpeed = -5f; m0.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f); m0.simulationSpace = ParticleSystemSimulationSpace.Local; m0.startColor = new Color(0.7f, 0.95f, 1f);
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 2.4f; var em = ps.emission; em.rateOverTime = 40f;
            var r = pg.GetComponent<ParticleSystemRenderer>(); r.material = WhaleBeamFx.ParticleMat(); r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.05f; ps.Play(); c.gather = ps;
            var lg = new GameObject("Glow"); lg.transform.SetParent(go.transform, false); c.glow = lg.AddComponent<Light>(); c.glow.type = LightType.Point; c.glow.color = new Color(0.55f, 0.85f, 1f); c.glow.range = 6f;
            go.transform.position = WhaleBeamFx.Mouth(owner);
        }
        void LateUpdate()
        {
            if (UIState.Paused) return;
            t += Time.deltaTime; float k = Mathf.Clamp01(t / dur);
            bool ok = owner != null && ex != null && ex.stats.IsAlive && ex.IsExecuting && ex.Phase == SkillPhase.Windup;
            if (!ok || t > dur + 0.15f) { Destroy(gameObject); return; }
            transform.position = WhaleBeamFx.Mouth(owner); transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(owner.forward, Vector3.up));
            orb.localScale = Vector3.one * (0.15f + 1.0f * k * k) * (1f + 0.12f * Mathf.Sin(t * (20f + 40f * k)));
            orbM.color = Color.Lerp(new Color(0.5f, 0.8f, 1f, 0.6f), new Color(1f, 1f, 1f, 0.95f), k);
            for (int i = 0; i < rings.Count; i++)   // 고리가 바깥에서 주둥이로 좁혀 들어옴
            {
                float u = Mathf.Repeat(t * (1.2f + 1.5f * k) + i * 0.5f, 1f); rings[i].localScale = Vector3.one * Mathf.Lerp(4f, 0.4f, u); rings[i].localRotation = Quaternion.Euler(0, 0, t * 120f * (i == 0 ? 1 : -1));
                var c = ringM[i].color; c.a = 0.6f * Mathf.Sin(u * Mathf.PI) * (0.4f + 0.6f * k); ringM[i].color = c;
            }
            var em = gather.emission; em.rateOverTime = 40f + 260f * k; glow.intensity = 1f + 9f * k * k;
            if (k > 0.6f) ThirdPersonCamera.Shake(0.02f * k, 0.1f);
        }
        void OnDestroy() { if (orbM != null) Destroy(orbM); foreach (var m in ringM) if (m != null) Destroy(m); }
    }
}
