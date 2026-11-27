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
        public static bool Inside(Vector3 p) { if (Instance == null) return false; if (InDungeon(p)) return true; var d = p - Instance.Mark("Mark_HallCenter", Instance.transform.position); return Mathf.Abs(d.y) < 40f && new Vector2(d.x, d.z).magnitude < 60f; }

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
            BuildDungeon();
        }

        void Start() { SpawnQueen(); }

        // ── 하프물범 여왕: 조랭하프(GameManager.npcPrefab) × 1.8 + 왕관·망토·보석 지팡이(Resources/Npc/Queen*) · 왕좌 계단 위
        public const float QueenScale = 1.8f;
        /// <summary>여왕이 "지하감옥으로 가라" 한 뒤 — 대화창이 닫히면 끌려감.</summary>
        [System.NonSerialized] public bool pendingDungeon;
        public Npc Queen { get; private set; }
        void SpawnQueen()
        {
            var gm = GameManager.Instance; if (gm == null || gm.npcPrefab == null || Queen != null) return;
            Vector3 at = Mark("Mark_Throne", transform.position), look = Mark("Mark_Arrive", at - Vector3.right) - at; look.y = 0;
            var go = Instantiate(gm.npcPrefab, at, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity); go.name = "NPC_하프물범여왕";
            go.transform.localScale = Vector3.one * QueenScale;
            if (go.TryGetComponent<UnityEngine.AI.NavMeshAgent>(out var ag)) ag.enabled = false;            // 왕좌 앞에서 움직이지 않음
            if (!go.TryGetComponent<Npc>(out var npc)) npc = go.AddComponent<Npc>();
            npc.npcName = "하프물범 여왕"; npc.portrait = "queen"; npc.place = "하프물범 여왕의 성"; npc.story = QueenStory.Build; npc.questId = Quests.QueenLamps;
            npc.wanderRadius = 0f; npc.interactRange = 4.2f; npc.scarf = false; npc.ApplyLook();
            foreach (var part in new[] { ("Npc/QueenCrown", "head"), ("Npc/QueenCape", "body"), ("Npc/QueenScepter", "flip.R") }) NpcAccessory.Attach(go, part.Item1, part.Item2);
            Seaside.GlowLamps(go);                                                                          // 지팡이 사파이어 빛
            Queen = npc;
        }

        // ── 지하감옥: 여왕에게 고래 말을 하면 끌려감 → 철창 문을 세 번 흔들면 열림 → 복도 끝 계단으로 알현실에
        GameObject dungeon; Transform cellDoor; float doorOpen, doorTarget; int shakes; float shakeT;
        void BuildDungeon()
        {
            var prefab = Resources.Load<GameObject>("World/CastleDungeon");
            if (prefab == null) { Debug.Log("[성 안] Resources/World/CastleDungeon.fbx 없음 — blender/build_castle_dungeon.py"); return; }
            dungeon = Instantiate(prefab, transform.parent); dungeon.name = "CastleDungeon";
            foreach (var t in dungeon.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Mark_") && !marks.ContainsKey(t.name)) marks[t.name] = t;
                if (t.name == "DungeonCellDoor") cellDoor = t;
            }
            foreach (var mf in dungeon.GetComponentsInChildren<MeshFilter>(true))
            {
                var r = mf.GetComponent<MeshRenderer>(); bool col = mf.name.Contains("_Collision");
                if (col || mf.name == "DungeonCellDoor")
                {
                    var mc = mf.gameObject.GetComponent<MeshCollider>(); if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
                }
                if (col && r != null) r.enabled = false;
            }
            Seaside.GlowLamps(dungeon);
            Vector3 dc = Mark("Mark_DungeonCenter", dungeon.transform.position);
            NavMeshBaker.Extra.Add(new Bounds(dc + Vector3.up * 2f, new Vector3(34f, 10f, 34f)));
            for (int i = 1; i <= 5; i++) AddLight(Mark("Mark_Torch_" + i, dc), new Color(1f, 0.6f, 0.3f), 9f, 1.6f, true);
            AddLight(Mark("Mark_Moon", dc), new Color(0.6f, 0.72f, 1f), 6f, 1.2f, false);
            var up = new GameObject("DungeonStairs").AddComponent<SeaGate>(); up.transform.SetParent(dungeon.transform, true); up.signHeight = 3.6f;
            up.Configure(Mark("Mark_ExitStairs", dc), Mark("Mark_Arrive", transform.position), Mark("Mark_ArriveLook", transform.position), "F   계단 올라가기", "알현실로", false);
            up.arriveText = "알현실로 슬그머니 돌아왔다…";
        }
        /// <summary>지하감옥에 있는가.</summary>
        public static bool InDungeon(Vector3 p) { if (Instance == null || Instance.dungeon == null) return false; var d = p - Instance.Mark("Mark_DungeonCenter", p); return Mathf.Abs(d.y) < 12f && new Vector2(d.x, d.z).magnitude < 24f; }
        /// <summary>여왕이 "지하감옥으로 가라" — 화면이 어두워졌다가 감방 안으로. 철창 문은 다시 잠김.</summary>
        public void SendToDungeon()
        {
            if (dungeon == null) return; var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return; var t = p.transform;
            shakes = 0; doorTarget = 0f;
            SceneFader.Cover(() =>
            {
                var cc = t.GetComponent<CharacterController>(); if (cc != null) cc.enabled = false;
                Vector3 at = Mark("Mark_CellArrive", t.position), look = Mark("Mark_CellLook", at + Vector3.forward) - at; look.y = 0;
                t.SetPositionAndRotation(at + Vector3.up * 0.05f, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : t.rotation);
                if (cc != null) cc.enabled = true; Physics.SyncTransforms();
                if (t.TryGetComponent<PlayerController>(out var pc)) pc.ResetFall();
                var cam = Camera.main; if (cam != null && cam.TryGetComponent<ThirdPersonCamera>(out var tpc)) tpc.SnapBehind();
                Quests.AnnounceNow("지하감옥", "고래 말을 했다가 끌려왔다…");
                Toast.Show("철컹! 감방에 갇혔다. 철창 문을 흔들어 보자 (F)");
            });
        }

        void Update()
        {
            if (UIState.Paused) return;
            if (pendingDungeon) { pendingDungeon = false; SendToDungeon(); }
            float t = Time.time;
            foreach (var (l, bi, ph) in flicker) if (l != null) l.intensity = bi * (0.9f + 0.1f * Mathf.PerlinNoise(t * 3f + ph, ph));   // 촛불·횃불 일렁임
            if (cellDoor == null) return;
            doorOpen = Mathf.MoveTowards(doorOpen, doorTarget, Time.deltaTime * 1.6f);
            float wob = shakeT > t ? Mathf.Sin(t * 60f) * 3f * (shakeT - t) : 0f;
            cellDoor.localRotation = Quaternion.Euler(0f, -100f * doorOpen + wob, 0f) * Quaternion.identity;
            if (doorTarget < 0.5f)
            {
                var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return;
                Vector3 df = Mark("Mark_CellDoorFront", cellDoor.position); Vector3 d = p.transform.position - df; d.y = 0;
                if (d.magnitude < 2.2f && Mathf.Abs(p.transform.position.y - df.y) < 2f) Interaction.Offer(this, d.magnitude, "F   철창 문 흔들기", ShakeDoor);
            }
        }
        void ShakeDoor()
        {
            shakes++; shakeT = Time.time + 0.4f; ThirdPersonCamera.Shake(0.12f, 0.2f);
            string[] say = { "덜컹! …꿈쩍도 안 한다.", "덜컹덜컹!! 경첩이 삐걱거린다…", "끼이익— 철컥! 낡은 자물쇠가 툭 떨어졌다!" };
            Toast.Show(say[Mathf.Clamp(shakes - 1, 0, 2)]);
            if (shakes >= 3) { doorTarget = 1f; Quests.AnnounceNow("탈출!", "복도 끝 계단으로 알현실에 돌아가자"); }
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

    }
}
