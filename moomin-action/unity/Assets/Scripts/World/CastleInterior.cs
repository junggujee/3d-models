using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 하프물범 여왕의 성 — 성 안(blender/build_castle_hall.py → Resources/World/CastleHall.fbx, 맵 좌표 그대로).
    /// 지금은 알현실(왕좌의 방)만. 실내는 성 위 하늘(높이 260m)에 따로 있는 닫힌 공간이고, 바깥과는 문으로만 오감.
    ///  · 바깥 문(CastleDoor): 성 팔각 성벽 앞(맨두가 지키는 성문 안쪽). F "성 안으로" → 실내 정문 앞
    ///    맨두를 통과해야 열림(CastleGate.Passed — 꿀바른 땅콩빵). 아니면 "근위대의 허락이 필요해요"
    ///  · 실내 정문: F "성 밖으로" → 바깥 문 앞
    ///  · 충돌(CastleHall_Collision) · 스테인드글라스·촛불(이름에 Glow) · 점광원(샹들리에 3 · 창 · 왕좌) · NavMesh(실내 바닥)
    /// GameManager.Start 가 NavMesh 전에 Ensure().
    /// </summary>
    public class CastleInterior : MonoBehaviour
    {
        public static CastleInterior Instance { get; private set; }
        readonly Dictionary<string, Transform> marks = new();
        readonly List<(Light l, float baseI, float ph)> flicker = new();
        bool inited;
        public Vector3 Mark(string n, Vector3 fb) => marks.TryGetValue(n, out var t) && t != null ? t.position : fb;
        /// <summary>성 안(실내 공간 근처)에 있는가 — 장소 이름 · 웨이브 · 날씨용.</summary>
        public static bool Inside(Vector3 p) { if (Instance == null) return false; var d = p - Instance.Mark("Mark_HallCenter", Instance.transform.position); return Mathf.Abs(d.y) < 40f && new Vector2(d.x, d.z).magnitude < 60f; }

        public static CastleInterior Ensure()
        {
            if (Instance != null) return Instance;
#if UNITY_2023_1_OR_NEWER
            var found = FindFirstObjectByType<CastleInterior>();
#else
            var found = FindObjectOfType<CastleInterior>();
#endif
            if (found != null) { found.Init(); return found; }
            var prefab = Resources.Load<GameObject>("World/CastleHall");
            if (prefab == null) { Debug.Log("[성 안] Resources/World/CastleHall.fbx 없음 — blender/build_castle_hall.py"); return null; }
            var go = Instantiate(prefab); go.name = "CastleHall";
            var world = GameObject.Find("World"); if (world != null) go.transform.SetParent(world.transform, false);
            var c = go.AddComponent<CastleInterior>(); c.Init(); return c;
        }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Init()
        {
            if (inited) return; inited = true; Instance = this;
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Mark_") && !marks.ContainsKey(t.name)) marks[t.name] = t;
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>(); bool collider = mf.name.StartsWith("CastleHall_Collision");
                if (collider || mf.name.StartsWith("CastleDoor"))
                {
                    var mc = mf.gameObject.GetComponent<MeshCollider>(); if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
                }
                if (collider && r != null) r.enabled = false;
                else if (r != null && mf.name.StartsWith("CastleHall")) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // 실내는 그림자 대신 등불 빛
            }
            Seaside.GlowLamps(gameObject);
            Physics.SyncTransforms();
            Vector3 hc = Mark("Mark_HallCenter", transform.position);
            NavMeshBaker.Extra.Add(new Bounds(hc + Vector3.up * 4f, new Vector3(70f, 14f, 70f)));
            for (int i = 1; i <= 3; i++) AddLight(Mark("Mark_Chandelier_" + i, hc + Vector3.up * 12f), new Color(1f, 0.82f, 0.6f), 26f, 2.4f, true);
            AddLight(Mark("Mark_Window", hc), new Color(0.65f, 0.75f, 1f), 30f, 1.8f, false);
            AddLight(Mark("Mark_ThroneSeat", hc) + Vector3.up * 4f, new Color(1f, 0.9f, 0.7f), 16f, 1.6f, false);
            BuildDoors();
        }

        void AddLight(Vector3 at, Color c, float range, float intensity, bool candle)
        {
            var l = new GameObject("HallLight").AddComponent<Light>(); l.transform.SetParent(transform, true); l.transform.position = at;
            l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
            if (candle) flicker.Add((l, intensity, Random.Range(0f, 10f)));
        }

        /// <summary>바깥 문 ↔ 실내 정문(SeaGate 를 그대로 씀 — 화면이 어두워졌다 밝아지며 건너감).</summary>
        void BuildDoors()
        {
            Vector3 outFront = Mark("Mark_DoorFront", transform.position), outLook = Mark("Mark_DoorLook", outFront + Vector3.forward);
            Vector3 inArrive = Mark("Mark_Arrive", outFront + Vector3.up * 260f), inLook = Mark("Mark_ArriveLook", inArrive + Vector3.right), inExit = Mark("Mark_ExitDoor", inArrive);
            var a = new GameObject("CastleDoor_Out").AddComponent<SeaGate>(); a.transform.SetParent(transform, true); a.signHeight = 8.2f;
            a.Configure(outFront, inArrive, inLook, "F   성 안으로 들어가기", "하프물범 여왕의 성", false);
            a.arriveText = "하프물범 여왕의 성 — 알현실"; a.canUse = () => CastleGate.Passed;
            a.blockedText = "성문 지기 맨두의 허락이 필요해요 (꿀바른 땅콩빵)";
            a.onArrive = () => Quests.AnnounceNow("하프물범 여왕의 성", "알현실");
            var b = new GameObject("CastleDoor_In").AddComponent<SeaGate>(); b.transform.SetParent(transform, true); b.signHeight = 9.5f;
            b.Configure(inExit, outFront, outLook, "F   성 밖으로 나가기", "정문", false);
            b.arriveText = "성 앞뜰로 나왔다";
        }

        void Update()
        {
            if (UIState.Paused || flicker.Count == 0) return;
            float t = Time.time;
            foreach (var (l, bi, ph) in flicker) if (l != null) l.intensity = bi * (0.9f + 0.1f * Mathf.PerlinNoise(t * 3f + ph, ph));   // 촛불 일렁임
        }
    }
}
