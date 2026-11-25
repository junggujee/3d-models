using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 바다(장면에 하나 — Seaside 가 붙이고 Setup). 물결 계산은 OceanMath(셰이더와 같은 식).
    /// <b>최적화</b>
    ///  · 물 메시 하나(동심원 고리 72 × 128칸 ≈ 9천 점, 안쪽 0.8m → 바깥 1.7km 로 점점 성기게)를 카메라 아래로 따라다니게(2m 단위로 맞춰 흔들림 방지) — 그리기 1번
    ///  · 파도는 GPU 정점 셰이더에서(CPU 는 수영·배·부표가 묻는 몇 점만 계산), 먼 곳은 물결 0(깜빡임 없음)
    ///  · 반사·굴절 카메라, 깊이 텍스처, 실시간 반사 프로브 없음 — 하늘색 그라데이션 반사 + 미리 구운 바다 깊이 그림(시작할 때 1번, 바닥 충돌체에 광선)
    ///  · 섬 안쪽 깊숙이(마을)에 있으면 물 메시를 끔(어차피 땅에 가려짐)
    ///  · 잔물결 노멀·거품 노이즈는 시작할 때 코드로 128² 한 장씩
    /// 물속: 카메라가 수면 아래면 안개(짙은 청록) + 화면 물색 덧칠, 나오면 원래대로.
    /// 부표(Float_*): 파도 높이에 맞춰 출렁·기울임.
    /// 질문: SurfaceAt(위치) = 그 자리 수면 높이(월드 y) · WaterDepth(x, z) = 해수면에서 바닥까지 깊이(뭍이면 음수) · Underwater(위치).
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class Ocean : MonoBehaviour
    {
        public static Ocean Instance { get; private set; }
        public static bool Exists => Instance != null;

        [Header("바다")] public float seaLevel = -0.8f; [Tooltip("파도가 나아가는 방향(도, +Z 에서 시계 방향) — 보통 바다 → 물가")] public float waveAngle;
        [Header("깊이 그림")] public Vector3 depthCenter; public float depthSize = 640f; public int depthRes = 224;
        [Header("섬")] public Vector3 islandCenter; public float islandRadius = 586f;
        [Header("모양")] public float fadeStart = 180f, fadeEnd = 420f, fogStart = 300f, fogEnd = 1500f;
        public Color underwaterFog = new Color(0.05f, 0.32f, 0.4f), underwaterTint = new Color(0.08f, 0.45f, 0.55f, 0.22f);

        OceanWave[] waves; float[] depth; Texture2D depthTex; bool baked; Material mat; Transform surface; MeshRenderer surfaceRenderer;
        readonly List<(Transform t, Vector3 basePos, Quaternion baseRot, float phase)> floats = new();
        Light sun; Camera cam; bool under; bool savedFog; Color savedFogColor; FogMode savedFogMode; float savedFogDensity; CanvasGroup tint;
        static readonly int P_W0 = Shader.PropertyToID("_OceanWave0"), P_W1 = Shader.PropertyToID("_OceanWave1"), P_W2 = Shader.PropertyToID("_OceanWave2"), P_W3 = Shader.PropertyToID("_OceanWave3"),
            P_Amp = Shader.PropertyToID("_OceanAmp"), P_Par = Shader.PropertyToID("_OceanParams"), P_Rect = Shader.PropertyToID("_OceanDepthRect"), P_Tex = Shader.PropertyToID("_OceanDepthTex"),
            P_Sun = Shader.PropertyToID("_OceanSunDir"), P_SunC = Shader.PropertyToID("_OceanSunColor"), P_Fog = Shader.PropertyToID("_OceanFog");

        public static float Time => UnityEngine.Time.time;
        public float MaxWave => waves != null ? OceanMath.MaxAmplitude(waves) : 0f;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (under) SetUnderwater(false); if (depthTex) Destroy(depthTex); }

        /// <summary>Seaside 가 부름: 바다 높이·파도 방향·깊이 그림 영역·섬·바닥 충돌체(깊이 굽기)·부표.</summary>
        public void Setup(float level, float angle, Vector3 depthAt, Collider floor, Vector3 island, float radius, IEnumerable<Transform> floaters)
        {
            seaLevel = level; waveAngle = angle; depthCenter = depthAt; islandCenter = island; islandRadius = radius;
            waves = OceanMath.Waves(waveAngle);
            Bake(floor);
            BuildSurface();
            if (floaters != null) foreach (var f in floaters) if (f != null) floats.Add((f, f.position, f.rotation, Random.Range(0f, 6.28f)));
        }

        // ── 질문 ─────────────────────────────
        /// <summary>파도에 맞춰 출렁일 물체 더하기(용궁 임금님 부표 등).</summary>
        public void AddFloat(Transform t) { if (t != null) floats.Add((t, t.position, t.rotation, Random.Range(0f, 6.28f))); }
        public static float SurfaceAt(Vector3 p) => Instance == null ? float.NegativeInfinity : Instance.seaLevel + Instance.WaveHeight(p.x, p.z);
        public static bool Underwater(Vector3 p) => Instance != null && p.y < SurfaceAt(p);
        /// <summary>해수면에서 바닥까지(m). 섬 안(뭍)은 음수, 깊이 그림 밖은 40.</summary>
        public static float WaterDepth(float x, float z) => Instance == null ? -10f : Instance.DepthAt(x, z);
        public float WaveHeight(float x, float z) => waves == null ? 0f : OceanMath.HeightAt(waves, x, z, Time, baked ? (System.Func<float, float, float>)DepthAt : null);

        float DepthAt(float x, float z)
        {
            if (!baked) return 40f;
            float h = depthSize * 0.5f, u = (x - (depthCenter.x - h)) / depthSize * (depthRes - 1), v = (z - (depthCenter.z - h)) / depthSize * (depthRes - 1);
            if (u < 0 || v < 0 || u > depthRes - 1 || v > depthRes - 1) return 40f;
            int i = Mathf.Min((int)u, depthRes - 2), j = Mathf.Min((int)v, depthRes - 2); float fu = u - i, fv = v - j;
            float a = depth[j * depthRes + i], b = depth[j * depthRes + i + 1], c = depth[(j + 1) * depthRes + i], d = depth[(j + 1) * depthRes + i + 1];
            return Mathf.Lerp(Mathf.Lerp(a, b, fu), Mathf.Lerp(c, d, fu), fv);
        }

        // ── 깊이 굽기: 바닥 충돌체에만 위에서 광선(뭍은 바닥 메시가 없으니 -2) ──
        void Bake(Collider floor)
        {
            depth = new float[depthRes * depthRes]; var px = new Color32[depthRes * depthRes]; float h = depthSize * 0.5f;
            for (int j = 0; j < depthRes; j++)
                for (int i = 0; i < depthRes; i++)
                {
                    float x = depthCenter.x - h + depthSize * i / (depthRes - 1), z = depthCenter.z - h + depthSize * j / (depthRes - 1);
                    float d = -2f;
                    if (floor != null && floor.Raycast(new Ray(new Vector3(x, seaLevel + 30f, z), Vector3.down), out var hit, 120f)) d = seaLevel - hit.point.y;
                    else if (InIslandOutside(x, z)) d = 40f;
                    d = Mathf.Clamp(d, -2f, 40f); depth[j * depthRes + i] = d;
                    px[j * depthRes + i] = new Color32((byte)Mathf.RoundToInt(Mathf.Clamp01((d + 2f) / 42f) * 255f), (byte)Mathf.RoundToInt(Mathf.Clamp01((d + 2f) / 6f) * 255f), 0, 255);
                }
            depthTex = new Texture2D(depthRes, depthRes, TextureFormat.RGBA32, false, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "OceanDepth" };
            depthTex.SetPixels32(px); depthTex.Apply(false, true); baked = true;
            Shader.SetGlobalTexture(P_Tex, depthTex);
            Shader.SetGlobalVector(P_Rect, new Vector4(depthCenter.x - h, depthCenter.z - h, 1f / depthSize, 1f));
        }
        bool InIslandOutside(float x, float z) { var d = new Vector2(x - islandCenter.x, z - islandCenter.z); return d.magnitude > islandRadius + 950f; }

        // ── 물 메시(동심원 고리) ─────────────────
        void BuildSurface()
        {
            const int Rings = 72, Seg = 128; float r = 0f, step = 0.8f;
            var verts = new List<Vector3> { Vector3.zero }; var tris = new List<int>();
            for (int i = 1; i <= Rings; i++)
            {
                r += step; step *= 1.07f;
                for (int s = 0; s < Seg; s++) { float a = 2f * Mathf.PI * s / Seg; verts.Add(new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r)); }
            }
            for (int s = 0; s < Seg; s++) { tris.Add(0); tris.Add(1 + (s + 1) % Seg); tris.Add(1 + s); }
            for (int i = 0; i < Rings - 1; i++)
                for (int s = 0; s < Seg; s++)
                {
                    int a = 1 + i * Seg + s, b = 1 + i * Seg + (s + 1) % Seg, c = a + Seg, d = b + Seg;
                    tris.Add(a); tris.Add(b); tris.Add(c); tris.Add(b); tris.Add(d); tris.Add(c);
                }
            var mesh = new Mesh { name = "OceanSurface", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(r * 2f, 8f, r * 2f));   // 정점이 셰이더에서 움직이니 넉넉하게(가려짐 판정)
            var go = new GameObject("OceanSurface"); go.transform.SetParent(transform, false); surface = go.transform;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; surfaceRenderer = go.AddComponent<MeshRenderer>();
            surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; surfaceRenderer.receiveShadows = false;
            surfaceRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off; surfaceRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            var sh = Shader.Find("MoominAction/Ocean");
            if (sh != null) { mat = new Material(sh) { name = "Ocean" }; mat.SetTexture("_DetailNormal", DetailNormal()); mat.SetTexture("_FoamTex", FoamNoise()); }
            else
            {   // 셰이더가 없으면(빌드에서 빠짐 등) 평평한 반투명 물 — 파도 계산은 그대로(수영·배는 동작)
                Debug.LogWarning("[바다] MoominAction/Ocean 셰이더를 찾지 못해 단순한 물로 그림(Resources/Shaders/MoominOcean.shader 확인)");
                var s2 = Shader.Find("Universal Render Pipeline/Unlit"); if (s2 == null) s2 = Shader.Find("Unlit/Color");
                mat = new Material(s2) { color = new Color(0.1f, 0.45f, 0.55f) };
            }
            surfaceRenderer.sharedMaterial = mat;
        }

        /// <summary>잔물결 노멀(이음매 없는 128²): 정수 주파수 사인 합의 기울기 → RG.</summary>
        static Texture2D DetailNormal()
        {
            const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, name = "OceanDetailNormal" }; var px = new Color32[N * N];
            var f = new (int fx, int fy, float a, float ph)[] { (1, 2, 0.5f, 0.3f), (3, -1, 0.35f, 1.7f), (-2, 5, 0.25f, 2.9f), (6, 3, 0.15f, 4.1f), (-7, -4, 0.12f, 0.8f), (9, -8, 0.08f, 5.2f) };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N, gx = 0f, gy = 0f;
                    foreach (var (fx, fy, a, ph) in f) { float c = Mathf.Cos(2f * Mathf.PI * (fx * u + fy * v) + ph) * a * 2f * Mathf.PI; gx += c * fx; gy += c * fy; }
                    gx *= 0.06f; gy *= 0.06f;
                    px[y * N + x] = new Color32((byte)(Mathf.Clamp01(0.5f - gx * 0.5f) * 255), (byte)(Mathf.Clamp01(0.5f - gy * 0.5f) * 255), 255, 255);
                }
            t.SetPixels32(px); t.Apply(true, true); return t;
        }
        /// <summary>거품 노이즈(이음매 없는 값 노이즈 2겹).</summary>
        static Texture2D FoamNoise()
        {
            const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { wrapMode = TextureWrapMode.Repeat, name = "OceanFoamNoise" }; var px = new Color32[N * N];
            var rng = new System.Random(11); float[] g8 = new float[64], g32 = new float[1024];
            for (int i = 0; i < g8.Length; i++) g8[i] = (float)rng.NextDouble(); for (int i = 0; i < g32.Length; i++) g32[i] = (float)rng.NextDouble();
            float Val(float[] g, int n, float u, float v)
            {
                float x = u * n, y = v * n; int i = (int)x, j = (int)y; float fx = x - i, fy = y - j; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
                float a = g[(j % n) * n + i % n], b = g[(j % n) * n + (i + 1) % n], c = g[((j + 1) % n) * n + i % n], d = g[((j + 1) % n) * n + (i + 1) % n];
                return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
            }
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++) { float v = Val(g8, 8, x / (float)N, y / (float)N) * 0.6f + Val(g32, 32, x / (float)N, y / (float)N) * 0.4f; byte b = (byte)(Mathf.Clamp01(v) * 255); px[y * N + x] = new Color32(b, b, b, 255); }
            t.SetPixels32(px); t.Apply(true, true); return t;
        }

        // ── 매 프레임 ─────────────────────────
        void LateUpdate()
        {
            if (waves == null) return;
            if (cam == null) cam = Camera.main; if (sun == null) FindSun();
            Vector3 cp = cam != null ? cam.transform.position : transform.position;
            // 섬 안쪽 깊숙이(마을)면 물 끄기
            float fromIsland = new Vector2(cp.x - islandCenter.x, cp.z - islandCenter.z).magnitude;
            bool show = fromIsland > islandRadius - 300f || cp.y < seaLevel + 2f;
            if (surfaceRenderer != null && surfaceRenderer.enabled != show) surfaceRenderer.enabled = show;
            if (surface != null) surface.position = new Vector3(Mathf.Round(cp.x / 2f) * 2f, seaLevel, Mathf.Round(cp.z / 2f) * 2f);
            // 셰이더 전역
            Shader.SetGlobalVector(P_W0, W(0)); Shader.SetGlobalVector(P_W1, W(1)); Shader.SetGlobalVector(P_W2, W(2)); Shader.SetGlobalVector(P_W3, W(3));
            Shader.SetGlobalVector(P_Amp, new Vector4(waves[0].amp, waves[1].amp, waves[2].amp, waves[3].amp));
            Shader.SetGlobalVector(P_Par, new Vector4(Time, seaLevel, fadeStart, fadeEnd));
            Shader.SetGlobalVector(P_Fog, new Vector4(fogStart, fogEnd, 0, 0));
            if (sun != null) { Shader.SetGlobalVector(P_Sun, -sun.transform.forward); Shader.SetGlobalVector(P_SunC, sun.color * Mathf.Clamp(sun.intensity, 0.6f, 1.4f)); }
            else { Shader.SetGlobalVector(P_Sun, new Vector3(0.3f, 0.8f, 0.4f).normalized); Shader.SetGlobalVector(P_SunC, Color.white); }
            // 부표
            for (int i = 0; i < floats.Count; i++)
            {
                var (t, bp, br, ph) = floats[i]; if (t == null) continue;
                float y = SurfaceAt(bp), sx = SurfaceAt(bp + Vector3.right * 0.5f) - y, sz = SurfaceAt(bp + Vector3.forward * 0.5f) - y;
                t.position = new Vector3(bp.x, y - 0.05f, bp.z);
                t.rotation = Quaternion.Euler(-sz * 60f, 0, sx * 60f) * Quaternion.AngleAxis(Mathf.Sin(Time * 0.7f + ph) * 8f, Vector3.up) * br;
            }
            // 물속
            bool nowUnder = cam != null && cp.y < SurfaceAt(cp) - 0.05f && WaterDepth(cp.x, cp.z) > 0f && !AirDome.Inside(cp);   // 용궁 공기방울 안은 물속이 아님
            if (nowUnder != under) SetUnderwater(nowUnder);
        }
        Vector4 W(int i) => new Vector4(waves[i].dirX, waves[i].dirZ, waves[i].k, waves[i].omega);
        void FindSun()
        {
            sun = RenderSettings.sun;
            if (sun == null)
            {
#if UNITY_2023_1_OR_NEWER
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional) { sun = l; break; }
#else
                foreach (var l in FindObjectsOfType<Light>()) if (l.type == LightType.Directional) { sun = l; break; }
#endif
            }
        }

        void SetUnderwater(bool on)
        {
            under = on;
            if (on)
            {
                savedFog = RenderSettings.fog; savedFogColor = RenderSettings.fogColor; savedFogMode = RenderSettings.fogMode; savedFogDensity = RenderSettings.fogDensity;
                RenderSettings.fog = true; RenderSettings.fogColor = underwaterFog; RenderSettings.fogMode = FogMode.Exponential; RenderSettings.fogDensity = 0.055f;
                if (cam != null) cam.backgroundColor = underwaterFog;
            }
            else { RenderSettings.fog = savedFog; RenderSettings.fogColor = savedFogColor; RenderSettings.fogMode = savedFogMode; RenderSettings.fogDensity = savedFogDensity; }
            if (tint == null) BuildTint();
            if (tint != null) tint.alpha = on ? 1f : 0f;
        }
        void BuildTint()
        {
            var go = new GameObject("UnderwaterTint"); go.transform.SetParent(transform, false);
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = -10;
            tint = go.AddComponent<CanvasGroup>(); tint.blocksRaycasts = false; tint.interactable = false;
            var img = new GameObject("Tint", typeof(RectTransform), typeof(UnityEngine.UI.Image)); img.transform.SetParent(go.transform, false);
            var rt = (RectTransform)img.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var im = img.GetComponent<UnityEngine.UI.Image>(); im.color = underwaterTint; im.raycastTarget = false;
        }
    }
}
