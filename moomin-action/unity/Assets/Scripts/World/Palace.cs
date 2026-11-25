using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 바다 밑 용궁(blender/build_palace.py → Resources/World/Palace.fbx, 맵 좌표 그대로). 선착장 끝에서 바다 쪽 165m, 바다 밑 분지(-42m)에 있음.
    /// GameManager.Start 가 NavMesh 전에 Ensure():
    ///  · 충돌(Palace_Collision, 보이지 않음) · 공기방울 돔(Palace_Dome → Bubble 셰이더 + AirDome: 안은 물이 없어 걷고 싸움)
    ///  · 용궁 둘레 NavMesh(NavMeshBaker.Extra) · 등불·수정 빛(이름에 Glow) · 점광원 3개(결투장 파랑 · 왕좌의 방 따뜻한 빛 · 돔 꼭대기)
    ///  · 바다 위 임금님 부표(Float_KingBuoy — 파도에 출렁, 여기서 잠수하면 용궁)
    /// 매 프레임(가까울 때만): 해파리 둥실 · 물고기 떼가 돔 둘레를 돎 · 위에서 내려오는 빛줄기(카메라를 향해 돔) · 돔 꼭대기에서 수면으로 공기방울 기둥
    /// Start(NavMesh 뒤): 고래고래 임금님(GameManager.enemyPrefabs 의 Goraegorae + WhaleKing) 을 결투장에 — 쓰러지면 5분 뒤(플레이어가 돔 밖일 때) 다시.
    /// </summary>
    public class Palace : MonoBehaviour
    {
        public static Palace Instance { get; private set; }
        public const float RespawnSeconds = 300f;
        readonly Dictionary<string, Transform> marks = new();
        readonly List<(Transform t, Vector3 basePos, float ph, Vector3 scale)> jellies = new();
        readonly List<(Transform t, float speed)> schools = new();
        readonly List<Transform> shafts = new();
        public Vector3 Center { get; private set; }
        public float DomeRadius { get; private set; } = 36f;
        public Transform[] Patrol { get; private set; }
        float respawnAt = -1f, nextBubble; bool inited; Transform player;

        public Vector3 Mark(string n, Vector3 fb) => marks.TryGetValue(n, out var t) && t != null ? t.position : fb;
        public Transform MarkT(string n) => marks.TryGetValue(n, out var t) ? t : null;

        public static Palace Ensure()
        {
            if (Instance != null) return Instance;
#if UNITY_2023_1_OR_NEWER
            var found = FindFirstObjectByType<Palace>();
#else
            var found = FindObjectOfType<Palace>();
#endif
            if (found != null) { found.Init(); return found; }
            var prefab = Resources.Load<GameObject>("World/Palace");
            if (prefab == null) { Debug.Log("[용궁] Resources/World/Palace.fbx 없음 — blender/build_palace.py"); return null; }
            var go = Instantiate(prefab); go.name = "Palace";
            var world = GameObject.Find("World"); if (world != null) go.transform.SetParent(world.transform, false);
            var p = go.AddComponent<Palace>(); p.Init(); return p;
        }
        /// <summary>돔 안이거나 돔 둘레(수평 60m 안) — 장소 이름 · 웨이브 멈춤.</summary>
        public static bool Near(Vector3 p) { if (Instance == null) return false; var d = p - Instance.Center; d.y = 0; return d.magnitude < 60f; }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Init()
        {
            if (inited) return; inited = true; Instance = this;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Mark_") && !marks.ContainsKey(t.name)) marks[t.name] = t;
                else if (t.name.StartsWith("Jelly_")) jellies.Add((t, t.position, Random.Range(0f, 6.28f), t.localScale));
                else if (t.name.StartsWith("FishSchool_")) schools.Add((t, Random.Range(5f, 9f) * (schools.Count % 2 == 0 ? 1f : -1f)));
            }
            Center = Mark("Mark_DomeCenter", transform.position);
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                string n = mf.name; var r = mf.GetComponent<MeshRenderer>();
                if (n.StartsWith("Palace_Collision"))
                {
                    var mc = mf.gameObject.GetComponent<MeshCollider>(); if (mc == null) mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh;
                    if (r != null) r.enabled = false;
                }
                else if (n.StartsWith("Palace_Dome") && r != null)
                {
                    r.sharedMaterial = AirDome.BubbleMaterial(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
                    DomeRadius = Mathf.Max(5f, r.bounds.extents.x);
                }
                else if (r != null && (n.StartsWith("Jelly_") || n.StartsWith("FishSchool_") || n.StartsWith("Palace_Decor"))) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var domeGo = new GameObject("AirDome"); domeGo.transform.SetParent(transform, false); domeGo.transform.position = Center; domeGo.AddComponent<AirDome>().radius = DomeRadius;
            Seaside.GlowLamps(gameObject);
            Physics.SyncTransforms();
            NavMeshBaker.Extra.Add(new Bounds(Center + Vector3.up * 10f, new Vector3(DomeRadius * 2.1f, 40f, DomeRadius * 2.1f)));
            if (Ocean.Instance != null) { var kb = transform.Find("Float_KingBuoy"); if (kb == null) foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name.StartsWith("Float_KingBuoy")) { kb = t; break; } Ocean.Instance.AddFloat(kb); }
            var pts = new List<Transform>(); for (int i = 1; i <= 10; i++) { var t = MarkT("Mark_Patrol_" + i); if (t != null) pts.Add(t); } Patrol = pts.ToArray();
            AddLight(Mark("Mark_Arena", Center) + Vector3.up * 8f, new Color(0.45f, 0.65f, 1f), 30f, 2.2f);
            AddLight(Mark("Mark_HallLight", Center), new Color(1f, 0.85f, 0.6f), 20f, 2.6f);
            AddLight(Center + Vector3.up * (DomeRadius - 4f), new Color(0.6f, 0.85f, 1f), 50f, 1.2f);
            BuildShafts();
        }

        void AddLight(Vector3 at, Color c, float range, float intensity)
        {
            var l = new GameObject("PalaceLight").AddComponent<Light>(); l.transform.SetParent(transform, true); l.transform.position = at;
            l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
        }

        /// <summary>위에서 비스듬히 내려오는 빛줄기(길쭉한 반투명 판 5장 — 카메라 쪽으로 돌아섬).</summary>
        void BuildShafts()
        {
            var m = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.65f, 0.88f, 1f, 0.07f), renderQueue = 3050 };
            for (int i = 0; i < 5; i++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.name = "LightShaft"; q.transform.SetParent(transform, true);
                float a = i * 1.26f; q.transform.position = Center + new Vector3(Mathf.Cos(a) * 9f, DomeRadius * 0.5f, Mathf.Sin(a) * 9f);
                q.transform.localScale = new Vector3(2.5f + i % 3, DomeRadius * 1.05f, 1f);
                var r = q.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; shafts.Add(q.transform);
            }
        }

        void Start() { Init(); SpawnKing(); }

        void SpawnKing()
        {
            var gm = GameManager.Instance; if (gm == null || WhaleKing.Current != null) return;
            GameObject prefab = null; foreach (var e in gm.enemyPrefabs) if (e != null && e.name == "Goraegorae") { prefab = e; break; }
            if (prefab == null) { Debug.LogWarning("[용궁] Goraegorae 프리팹이 GameManager.enemyPrefabs 에 없어 임금님을 만들지 못함"); return; }
            Vector3 at = Mark("Mark_BossSpawn", Center), look = Mark("Mark_BossLook", at + Vector3.back); Vector3 f = look - at; f.y = 0;
            var go = Instantiate(prefab, at + Vector3.up * 0.1f, f.sqrMagnitude > 0.01f ? Quaternion.LookRotation(f) : Quaternion.identity); go.name = "WhaleKing";
            var k = go.AddComponent<WhaleKing>(); int pl = Experience.Instance != null ? Experience.Instance.level : 1; k.Setup(this, Mathf.Max(12, pl + 2));
        }
        public void KingDefeated(bool party = false) { respawnAt = Time.time + (party ? 8f : RespawnSeconds); respawnParty = party; }
        bool respawnParty;

        // ── 생일 파티(퀘스트 「삐진 고래고래 임금님」을 끝내면) ─────────────
        GameObject party; readonly List<GameObject> guests = new(); readonly List<(Transform t, Vector3 basePos, float ph)> balloons = new(); bool partyOn; float nextConfetti;
        static readonly (string name, Color scarf, string[] lines)[] Guests =
        {
            ("뽀글이", new Color(0.45f, 0.75f, 1f), new[] { "happy|Laugh|임금님 생일 축하해요~! 공기방울 장식은 제가 다 불었어요!", "happy|Yes|딸기 케이크 냄새… 너무 좋아요. 아직 먹으면 안 되겠죠?" }),
            ("반짝이", new Color(1f, 0.82f, 0.3f), new[] { "happy|Laugh|저는 수정 등불을 반짝반짝 닦았어요! 생일 파티는 반짝여야죠!", "surprised|Surprised|임금님이 재대결 이야기만 나오면 눈이 반짝해요. 진짜 좋아하나 봐요!" }),
            ("퐁당이", new Color(0.55f, 0.9f, 0.65f), new[] { "happy|Wave|퐁당! 물속 파티는 처음이에요. 공기방울 안이라 숨도 잘 쉬어져요!", "happy|Laugh|선물 상자 중에 제 거는 분홍 리본이에요. 비밀이에요!" }),
            ("말랑이", new Color(1f, 0.55f, 0.75f), new[] { "happy|Laugh|생일 노래 연습 많이 했어요! 끼룩끼룩~ 아니 랄랄라~", "sad|Sad|맨 위 큰 딸기는 임금님 거래요… 히잉." }),
            ("철썩이", new Color(0.86f, 0.3f, 0.27f), new[] { "happy|Laugh|어이, 털복숭이 친구! 나도 배 타고 왔지. 바다가 이렇게 즐거운 건 처음이야!", "happy|Yes|케이크는 땅콩빵 가게에 특별히 부탁했어. 딸기가 마흔 개라고!" }),
        };

        void SetParty(bool on)
        {
            partyOn = on;
            if (on && party == null)
            {
                var prefab = Resources.Load<GameObject>("World/Party");
                if (prefab == null) { Debug.Log("[용궁] Resources/World/Party.fbx 없음 — blender/build_party.py"); return; }
                party = Instantiate(prefab); party.name = "Party"; party.transform.SetParent(transform.parent, false);
                foreach (var t in party.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("Balloon_")) balloons.Add((t, t.position, Random.Range(0f, 6.28f)));
                    if (!t.name.StartsWith("Party_Collision")) continue;
                    var mf = t.GetComponent<MeshFilter>(); var mc = t.gameObject.AddComponent<MeshCollider>(); if (mf != null) mc.sharedMesh = mf.sharedMesh;
                    var r = t.GetComponent<MeshRenderer>(); if (r != null) r.enabled = false;
                    var ob = t.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>(); ob.carving = true; ob.shape = UnityEngine.AI.NavMeshObstacleShape.Capsule; ob.radius = 2.4f; ob.height = 2f;
                }
                Seaside.GlowLamps(party); Physics.SyncTransforms();
                SpawnGuests();
            }
            if (party != null) party.SetActive(on);
            foreach (var g in guests) if (g != null) g.SetActive(on);
        }
        void SpawnGuests()
        {
            var gm = GameManager.Instance; if (gm == null || gm.npcPrefab == null) return;
            var cake = Mark("Mark_Cake", Center); int i = 0;
            foreach (var t in party.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Mark_Guest_") || i >= Guests.Length) continue;
                var (name, scarf, lines) = Guests[i++]; Vector3 look = cake - t.position; look.y = 0;
                var go = Instantiate(gm.npcPrefab, t.position, look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look) : Quaternion.identity); go.name = "Guest_" + name;
                if (!go.TryGetComponent<Npc>(out var npc)) npc = go.AddComponent<Npc>();
                npc.npcName = name; npc.place = "임금님의 생일 파티"; npc.scarfColor = scarf; npc.chatter = lines; npc.wanderRadius = 0.6f; npc.ApplyLook();
                if (name == "철썩이") { npc.portrait = "fisher"; NpcAccessory.Attach(go, "Npc/FisherHat", "head"); }
                go.AddComponent<PartyGuest>(); guests.Add(go);
            }
        }

        void Update()
        {
            if (UIState.Paused) return;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) player = p.transform; }
            if (respawnAt > 0f && Time.time >= respawnAt && (respawnParty || player == null || !AirDome.Inside(player.position))) { respawnAt = -1f; SpawnKing(); }
            bool wantParty = Quests.Log.IsDone(Quests.GrumpyKing);
            if (wantParty != partyOn)
            {
                SetParty(wantParty);
                if (wantParty && WhaleKing.Current != null && !WhaleKing.Current.Peaceful) WhaleKing.Current.EnterParty();
                if (wantParty && respawnAt > 0f) { respawnAt = Time.time + 1f; respawnParty = true; }   // 파티 시작: 임금님 바로 다시
            }
            var cam = Camera.main; if (cam == null) return; Vector3 cp = cam.transform.position;
            if ((cp - Center).sqrMagnitude > 160f * 160f) return;                      // 멀면 꾸밈 움직임 생략
            float t = Time.time;
            for (int i = 0; i < jellies.Count; i++)
            {
                var (tr, bp, ph, sc) = jellies[i]; if (tr == null) continue;
                tr.position = bp + new Vector3(Mathf.Sin(t * 0.3f + ph) * 1.2f, Mathf.Sin(t * 0.8f + ph) * 1.4f, Mathf.Cos(t * 0.25f + ph) * 1.2f);
                tr.localScale = sc * (1f + 0.08f * Mathf.Sin(t * 2.2f + ph));   // 둥실 + 오므렸다 폈다
            }
            for (int i = 0; i < balloons.Count && partyOn; i++)
            {
                var (b, bp, ph) = balloons[i]; if (b == null) continue;
                b.position = bp + new Vector3(Mathf.Sin(t * 0.7f + ph) * 0.15f, Mathf.Sin(t * 1.1f + ph) * 0.25f, Mathf.Cos(t * 0.6f + ph) * 0.15f);
            }
            if (partyOn && t >= nextConfetti && player != null && AirDome.Inside(player.position))
            {   // 색종이 펑!
                nextConfetti = t + Random.Range(2.5f, 4.5f); var c = Mark("Mark_Cake", Center) + Vector3.up * 4f + Random.insideUnitSphere * 3f;
                for (int k = 0; k < 4; k++) SparkBurst.Spawn(c, Vector3.up, 14, 4f, Color.HSVToRGB(Random.value, 0.5f, 1f));
            }
            for (int i = 0; i < schools.Count; i++) { var (tr, sp) = schools[i]; if (tr != null) tr.RotateAround(Center, Vector3.up, sp * Time.deltaTime); }
            foreach (var s in shafts) { if (s == null) continue; Vector3 d = s.position - cp; d.y = 0; if (d.sqrMagnitude > 0.01f) s.rotation = Quaternion.LookRotation(d) * Quaternion.Euler(0, 0, 8f); }
            if (t >= nextBubble)
            {   // 돔 꼭대기 → 수면 공기방울 기둥(용궁 길잡이 — 이 안에서 헤엄치면 숨이 참, PlayerSwim)
                nextBubble = t + 0.12f; float top = Center.y + DomeRadius, sea = Ocean.Instance != null ? Ocean.Instance.seaLevel : top + 6f;
                WaterFx.Bubble(new Vector3(Center.x, Random.Range(top, Mathf.Max(top, sea - 0.5f)), Center.z) + Random.insideUnitSphere * 1.2f, 2);
            }
        }
    }
}
