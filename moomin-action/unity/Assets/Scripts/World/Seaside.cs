using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 남쪽 바다 · 선착장(blender/build_seaside.py → Resources/World/Seaside.fbx, 맵과 같은 좌표라 원점에 그대로 놓으면 맞음).
    /// GameManager.Start 가 NavMesh 를 굽기 전에 Ensure() → 장면에 없으면 만들고:
    ///  · 충돌: Seaside_Floor(모래·바다 밑 메시 그대로) · Seaside_Collision · Seaside_GateCollision(보이지 않는 상자)
    ///  · Ocean(해수면 = Mark_SeaLevel, 파도는 바다 → 물가 방향, 깊이 그림은 선착장 둘레 640m) + 부표(Float_*)
    ///  · NavMesh: 선착장 둘레를 따로 구움(NavMeshBaker.Extra — 마을 NavMesh 는 성벽 안만)
    ///  · 바닷길 문 2개(SeaGate): 마을 남쪽 성벽 안쪽 Gate_Town ↔ 선착장 Gate_Harbor — 맵이 성벽과 산맥으로 둘러싸여 걸어서는 바다에 못 가므로
    /// Start(NavMesh 뒤): 통통배(Resources/World/Boat) 를 Mark_Boat 에 · 어부 철썩이(조랭하프족 + 노란 어부 모자)를 Mark_Fisher 에.
    /// 선착장 근처(InHarbor)에서는 웨이브가 새로 시작하지 않음(GameManager).
    /// </summary>
    public class Seaside : MonoBehaviour
    {
        public static Seaside Instance { get; private set; }
        public const float HarborRadius = 320f;
        readonly Dictionary<string, Transform> marks = new();
        public Vector3 Harbor { get; private set; }
        public Vector3 HarborSea { get; private set; }
        public Vector3 Dock => Mark("Mark_Dock", Harbor);

        public Vector3 Mark(string name, Vector3 fallback) => marks.TryGetValue(name, out var t) && t != null ? t.position : fallback;

        /// <summary>장면에 바다가 없으면 Resources 에서 만들고 준비. 반환: 있으면 그것.</summary>
        public static Seaside Ensure()
        {
            if (Instance != null) return Instance;
#if UNITY_2023_1_OR_NEWER
            var found = FindFirstObjectByType<Seaside>();
#else
            var found = FindObjectOfType<Seaside>();
#endif
            if (found != null) { found.Init(); return found; }
            var prefab = Resources.Load<GameObject>("World/Seaside");
            if (prefab == null) { Debug.Log("[바다] Resources/World/Seaside.fbx 없음 — blender/build_seaside.py"); return null; }
            var go = Instantiate(prefab); go.name = "Seaside";
            var world = GameObject.Find("World"); if (world != null) go.transform.SetParent(world.transform, false);   // 맵(City_*)과 같은 부모·같은 변환
            var s = go.AddComponent<Seaside>(); s.Init(); return s;
        }

        bool inited;
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Init()
        {
            if (inited) return; inited = true; Instance = this;
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Mark_") && !marks.ContainsKey(t.name)) marks[t.name] = t;
            Collider floor = null;
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.name;
                if (n.StartsWith("Seaside_Floor")) { var mc = mf.gameObject.GetComponent<MeshCollider>(); if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; floor = mc; mf.gameObject.isStatic = true; }
                else if (n.StartsWith("Seaside_Collision") || n.StartsWith("Seaside_GateCollision"))
                {
                    var mc = mf.gameObject.GetComponent<MeshCollider>(); if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
                    var mr = mf.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false;
                }
                var r = mf.GetComponent<MeshRenderer>();
                if (r != null && !n.StartsWith("Float_")) { r.shadowCastingMode = n.StartsWith("Seaside_Floor") || n.StartsWith("Seaside_Decor") ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On; }
            }
            GlowLamps(gameObject);
            Physics.SyncTransforms();
            Harbor = Mark("Mark_Harbor", transform.position); HarborSea = Mark("Mark_HarborSea", Harbor + Vector3.forward * 60f);
            Vector3 toShore = Harbor - HarborSea; toShore.y = 0f; float angle = Mathf.Atan2(toShore.x, toShore.z) * Mathf.Rad2Deg;
            var island = Mark("Mark_IslandCenter", Harbor - (HarborSea - Harbor).normalized * 586f);
            var floats = new List<Transform>(); foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Float_")) floats.Add(t);
            var ocean = GetComponent<Ocean>(); if (ocean == null) ocean = gameObject.AddComponent<Ocean>();
            ocean.Setup(Mark("Mark_SeaLevel", Harbor + Vector3.down * 0.8f).y, angle, Vector3.Lerp(Harbor, HarborSea, 0.6f), floor, island, Vector2.Distance(new Vector2(island.x, island.z), new Vector2(Harbor.x, Harbor.z)), floats);
            NavMeshBaker.Extra.Add(new Bounds(Harbor + (HarborSea - Harbor) * 0.2f, new Vector3(150f, 40f, 150f)));
            // 바닷길 문
            AddGate("Gate_Town", "Mark_GateTown", "Mark_Arrive", "Mark_ArriveLook", "F   남쪽 바닷길 — 선착장으로", "남쪽 바닷길", true);
            AddGate("Gate_Harbor", "Mark_GateHarbor", "Mark_Town", "Mark_TownLook", "F   마을로 돌아가기", "마을 가는 길", false);
        }

        void AddGate(string gateName, string front, string dest, string look, string prompt, string label, bool toHarbor)
        {
            var g = transform.Find(gateName); Transform go = g; if (go == null) foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith(gateName)) { go = t; break; }
            var host = go != null ? go.gameObject : new GameObject(gateName);
            var sg = host.GetComponent<SeaGate>(); if (sg == null) sg = host.AddComponent<SeaGate>();
            sg.Configure(Mark(front, host.transform.position), Mark(dest, Harbor), Mark(look, Harbor), prompt, label, toHarbor);
        }

        /// <summary>이름에 Glow 가 든 재질(등불)을 빛나게.</summary>
        public static void GlowLamps(GameObject root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials; bool any = false;
                for (int i = 0; i < ms.Length; i++)
                {
                    if (ms[i] == null || !ms[i].name.Contains("Glow")) continue;
                    var m = new Material(ms[i]); m.EnableKeyword("_EMISSION"); var c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color; m.SetColor("_EmissionColor", c * 2.2f); ms[i] = m; any = true;
                }
                if (any) r.sharedMaterials = ms;
            }
        }

        void Start()
        {
            Init();
            var bp = Resources.Load<GameObject>("World/Boat");
            if (bp != null && Boat.Current == null)
            {
                Vector3 p = Mark("Mark_Boat", HarborSea), bow = Mark("Mark_BoatBow", p + Vector3.forward); Vector3 f = bow - p; f.y = 0;
                var b = Instantiate(bp, p, f.sqrMagnitude > 0.01f ? Quaternion.LookRotation(f) : Quaternion.identity); b.name = "Boat";
                if (!b.TryGetComponent<Boat>(out _)) b.AddComponent<Boat>();
            }
            var gm = GameManager.Instance;
            if (gm != null && gm.npcPrefab != null) SpawnFisher(gm.npcPrefab);
        }

        void SpawnFisher(GameObject npcPrefab)
        {
            Vector3 at = Mark("Mark_Fisher", Harbor); Vector3 look = Mark("Mark_Arrive", Harbor) - at; look.y = 0;
            var go = Instantiate(npcPrefab, at, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity); go.name = "NPC_철썩이";
            if (!go.TryGetComponent<Npc>(out var npc)) npc = go.AddComponent<Npc>();
            npc.npcName = "철썩이"; npc.portrait = "fisher"; npc.place = "남쪽 선착장"; npc.story = HarborStory.Fisher; npc.wanderRadius = 1.2f;
            npc.scarfColor = new Color(0.86f, 0.3f, 0.27f); npc.furTint = new Color(0.98f, 0.97f, 0.95f); npc.ApplyLook();   // 초상화(fisher_*)와 같은 빨간 목도리
            NpcAccessory.Attach(go, "Npc/FisherHat", "head");
        }

        /// <summary>선착장 근처(웨이브 멈춤 · 저장 장소 이름).</summary>
        public static bool InHarbor(Vector3 p)
        {
            if (Instance == null) return false; var d = p - Instance.Harbor; d.y = 0; return d.magnitude < HarborRadius;
        }
    }
}
