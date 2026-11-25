using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    /// <summary>
    /// 마을 대장간(blender/build_smithy.py → Resources/World/Smithy.fbx, 맵 좌표 그대로).
    /// 마을 동쪽에 홀로 서 있던 작은 집을 헐고(`build_smithy.py --carve` 가 City_TownE·City_Collision 에서 지움) 그 자리에 세웠다.
    /// GameManager.Start 가 NavMesh 전에 Ensure():
    ///  · 충돌(Smithy_Collision, 보이지 않음) · 등불(이름에 Glow) · 화덕 불빛(흔들림) · 굴뚝 연기 · 둘레 NavMesh(NavMeshBaker.Extra)
    /// Start(NavMesh 뒤): 풍선고래 대장장이(GameManager.balloonSmithPrefab)를 모루 앞에 세움.
    /// </summary>
    public class Smithy : MonoBehaviour
    {
        public static Smithy Instance { get; private set; }
        readonly System.Collections.Generic.Dictionary<string, Transform> marks = new();
        Light forge; ParticleSystem smoke; float flicker; bool inited;
        public Vector3 Center { get; private set; }

        public Vector3 Mark(string n, Vector3 fb) => marks.TryGetValue(n, out var t) && t != null ? t.position : fb;
        public Transform MarkT(string n) => marks.TryGetValue(n, out var t) ? t : null;
        /// <summary>대장간 앞마당 25m 안(장소 이름·알림용).</summary>
        public static bool Near(Vector3 p) { if (Instance == null) return false; var d = p - Instance.Center; d.y = 0; return d.magnitude < 25f; }

        public static Smithy Ensure()
        {
            if (Instance != null) return Instance;
#if UNITY_2023_1_OR_NEWER
            var found = FindFirstObjectByType<Smithy>();
#else
            var found = FindObjectOfType<Smithy>();
#endif
            if (found != null) { found.Init(); return found; }
            var prefab = Resources.Load<GameObject>("World/Smithy");
            if (prefab == null) { Debug.Log("[대장간] Resources/World/Smithy.fbx 없음 — blender/build_smithy.py"); return null; }
            var go = Instantiate(prefab); go.name = "Smithy";
            var world = GameObject.Find("World"); if (world != null) go.transform.SetParent(world.transform, false);
            var s = go.AddComponent<Smithy>(); s.Init(); return s;
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Init()
        {
            if (inited) return; inited = true; Instance = this;
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Mark_") && !marks.ContainsKey(t.name)) marks[t.name] = t;
            Center = Mark("Mark_Yard", transform.position);
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>();
                if (!mf.name.StartsWith("Smithy_Collision")) continue;
                var mc = mf.gameObject.GetComponent<MeshCollider>(); if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
                if (r != null) r.enabled = false;
            }
            Seaside.GlowLamps(gameObject);
            Physics.SyncTransforms();
            NavMeshBaker.Extra.Add(new Bounds(Center + Vector3.up * 3f, new Vector3(26f, 16f, 26f)));
            var fg = new GameObject("ForgeLight"); fg.transform.SetParent(transform, true); fg.transform.position = Mark("Mark_Forge", Center) + Vector3.up * 0.4f;
            forge = fg.AddComponent<Light>(); forge.type = LightType.Point; forge.color = new Color(1f, 0.55f, 0.2f); forge.range = 14f; forge.intensity = 3.2f; forge.shadows = LightShadows.None;
            BuildSmoke();
        }

        /// <summary>굴뚝 연기(회색 덩이가 천천히 올라가며 퍼짐).</summary>
        void BuildSmoke()
        {
            var go = new GameObject("ChimneySmoke"); go.transform.SetParent(transform, true); go.transform.position = Mark("Mark_Smoke", Center + Vector3.up * 12f);
            smoke = go.AddComponent<ParticleSystem>();
            var main = smoke.main; main.startLifetime = 4.5f; main.startSpeed = 1.1f; main.startSize = 1.0f; main.maxParticles = 60;
            main.startColor = new Color(0.78f, 0.76f, 0.74f, 0.5f); main.gravityModifier = -0.04f; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = smoke.emission; em.rateOverTime = 7f;
            var sh = smoke.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.radius = 0.35f; sh.angle = 12f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var sz = smoke.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 2.4f));
            var col = smoke.colorOverLifetime; col.enabled = true;
            col.color = new ParticleSystem.MinMaxGradient(new Gradient
            {
                colorKeys = new[] { new GradientColorKey(new Color(0.8f, 0.78f, 0.76f), 0f), new GradientColorKey(new Color(0.88f, 0.88f, 0.9f), 1f) },
                alphaKeys = new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.45f, 0.15f), new GradientAlphaKey(0f, 1f) },
            });
            var r = smoke.GetComponent<ParticleSystemRenderer>(); r.material = new Material(Shader.Find("Sprites/Default")); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Start() { Init(); SpawnSmith(); }

        void SpawnSmith()
        {
            if (BalloonSmith.Current != null) return;
            var gm = GameManager.Instance; var prefab = gm != null ? gm.balloonSmithPrefab : null;
            if (prefab == null) { Debug.Log("[대장간] GameManager.balloonSmithPrefab 이 비어 있음 — blender/build_balloon_whale.py + Moomin 메뉴"); return; }
            Vector3 at = Mark("Mark_Smith", Center), look = Mark("Mark_SmithLook", at + transform.forward);
            if (NavMesh.SamplePosition(at, out var hit, 4f, NavMesh.AllAreas)) at = hit.position;
            Vector3 f = look - at; f.y = 0;
            var go = Instantiate(prefab, at, f.sqrMagnitude > 0.01f ? Quaternion.LookRotation(f) : Quaternion.identity); go.name = "BalloonSmith";
            go.AddComponent<BalloonSmith>().Setup(this);
        }

        void Update()
        {
            if (forge == null || UIState.Paused) return;
            flicker += Time.deltaTime * 7f;
            forge.intensity = 2.8f + 0.7f * Mathf.PerlinNoise(flicker, 0.3f);    // 불이 일렁임
        }
    }
}
