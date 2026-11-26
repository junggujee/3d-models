using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 웨이브 스폰·점수·게임오버 상태. UI(HUD/메뉴)는 UIRoot 가 담당하고 이 컴포넌트가 자동 생성함.
    /// 불러오기: 시작할 때 SaveSystem.Pending 을 적용(다른 컴포넌트의 Start 보다 먼저 — 카메라가 불러온 위치·방향을 따라가게). 웨이브를 끝낼 때마다 자동 저장.
    /// 쓰러지면 HUD 의 게임 오버 화면(결과 + 마지막 저장에서 다시 / 타이틀로).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameObject enemyPrefab; [Tooltip("웨이브에 섞어 나올 적들(비어 있으면 enemyPrefab 만). 보스는 enemyPrefab 으로 만듦")] public List<GameObject> enemyPrefabs = new(); public CharacterStats player; public float spawnRadius = 14f, wavePause = 4f; public int firstWave = 3, perWave = 2, bossEvery = 3;
        public int Score { get; private set; } public int Wave { get; private set; } public bool IsOver { get; private set; }
        public int EnemiesAlive { get { alive.RemoveAll(g => g == null); return alive.Count; } }
        public event Action<int> WaveStarted, WaveCleared;
        /// <summary>웨이브 진행 중(적이 남아 있음) — 이때는 저장 불가.</summary>
        public bool InWave => waveActive;
        [Tooltip("웨이브를 끝낼 때마다 자동 저장 슬롯에 저장")] public bool autoSave = true;
        [Header("자연")] [Tooltip("땅콩빵(자원) · 야생 하프물범(펫) — 비우면 생성 안 함")] public GameObject peanutPrefab, sealPrefab;
        [Header("마을")] [Tooltip("조랭하프족 NPC(조랭이·몽글이·포실이) — 비우면 생성 안 함")] public GameObject npcPrefab;
        [Tooltip("풍선고래 대장장이 — 비우면 Resources/Npc/BalloonWhale 에서 찾음")] public GameObject balloonSmithPrefab;
        readonly List<GameObject> alive = new(); float nextWave = 2f; bool waveActive;

        void Awake() { Instance = this; }

        /// <summary>웨이브에 섞을 적 고르기: 등장 웨이브(minWave)·한 웨이브 최대 수(maxPerWave)를 지킴(거대한 고래고래는 2웨이브부터 1마리).</summary>
        GameObject PickPrefab(Dictionary<GameObject, int> counts)
        {
            var ok = new List<GameObject>();
            foreach (var e in enemyPrefabs)
            {
                if (e == null) continue; var ecc = e.GetComponent<EnemyCombatController>();
                if (ecc != null && (Wave < ecc.minWave || (ecc.maxPerWave > 0 && counts.TryGetValue(e, out var c) && c >= ecc.maxPerWave))) continue;
                ok.Add(e);
            }
            return ok.Count > 0 ? ok[UnityEngine.Random.Range(0, ok.Count)] : enemyPrefab;
        }
        void Start()
        {
            if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.GetComponent<CharacterStats>(); }
            RunStats.Hook(); StunFx.Hook(); Hunts.Hook(); SaveSystem.ApplyPending(this);
            Seaside.Ensure();   // 남쪽 바다·선착장(충돌·바다·선착장 NavMesh 자리) — NavMesh 굽기 전에
            Palace.Ensure();    // 바다 밑 용궁(공기방울 돔 · 결투장 · 고래고래 임금님)
            Smithy.Ensure();    // 마을 대장간(화덕 불빛 · 굴뚝 연기 · 풍선고래 대장장이)
            if (player && !player.TryGetComponent<PlayerSwim>(out _)) player.gameObject.AddComponent<PlayerSwim>();   // 헤엄·잠수(예전 프리팹에도)
            var baker = GetComponent<NavMeshBaker>(); if (baker == null) baker = gameObject.AddComponent<NavMeshBaker>(); baker.Build();
            CastleGate.Ensure(this);   // 성문 지기 맨두(NavMesh 구운 뒤 — 성 둘레 단 위)
            if (player) player.Died += () => IsOver = true;
            #if UNITY_2023_1_OR_NEWER
            bool hasUi = FindFirstObjectByType<UI.UIRoot>() != null;
#else
            bool hasUi = FindObjectOfType<UI.UIRoot>() != null;
#endif
            if (!hasUi) { var go = new GameObject("UIRoot"); var ui = go.AddComponent<UI.UIRoot>(); ui.player = player; }
            if ((peanutPrefab != null || sealPrefab != null) && !TryGetComponent<NatureSpawner>(out _)) { var ns = gameObject.AddComponent<NatureSpawner>(); ns.peanutPrefab = peanutPrefab; ns.sealPrefab = sealPrefab; }   // NavMesh 를 구운 뒤
            if (npcPrefab != null && !TryGetComponent<VillageSpawner>(out _)) gameObject.AddComponent<VillageSpawner>().npcPrefab = npcPrefab;
            if (enemyPrefabs.Count > 0 && !TryGetComponent<HuntZoneSpawner>(out _)) gameObject.AddComponent<HuntZoneSpawner>().prefabs = enemyPrefabs;   // 토벌 구역(NavMesh 를 구운 뒤)
            if (!TryGetComponent<InteractionRunner>(out _)) gameObject.AddComponent<InteractionRunner>();   // F 상호작용(펫·NPC) 정리
        }
        public void AddScore(int s) { Score += s; }
        void OnDestroy() { RunStats.Unhook(); StunFx.Unhook(); Hunts.Unhook(); if (Instance == this) Instance = null; }

        /// <summary>불러오기: 저장된 웨이브까지 끝낸 상태로(다음 웨이브는 잠시 뒤 시작).</summary>
        public void RestoreProgress(int wave, int score) { Wave = Mathf.Max(0, wave); Score = Mathf.Max(0, score); waveActive = false; nextWave = Time.time + wavePause; }

        void Update()
        {
            if (IsOver) return;
            if (UIState.Paused) return;
            SaveSystem.PlayTime += Time.unscaledDeltaTime;
            if (waveActive && EnemiesAlive == 0)
            {
                waveActive = false; if (player) player.inCombat = false; WaveCleared?.Invoke(Wave); nextWave = Time.time + wavePause;
                if (autoSave && SaveSystem.CanSave(out _)) SaveSystem.Save(SaveFormat.AutoSlot);
            }
            if (!waveActive && Time.time >= nextWave && enemyPrefab && player)
            {
                if (Seaside.InHarbor(player.transform.position)) nextWave = Time.time + 2f;   // 선착장에서는 쉬어 감(웨이브 멈춤)
                else SpawnWave();
            }
        }

        /// <summary>
        /// 적 생성 위치: 플레이어 둘레에서 플레이어와 비슷한 높이의 걸을 수 있는 곳(NavMesh)만 고름 — 건물 지붕·벽 안·산 위에 생기지 않게.
        /// NavMesh 가 없으면 플레이어 높이에서 아래로 쏜 레이로 대체.
        /// </summary>
        Vector3 SpawnPoint(float radius)
        {
            Vector3 c = player.transform.position;
            for (int tryN = 0; tryN < 16; tryN++)
            {
                float a = UnityEngine.Random.value * Mathf.PI * 2, r = radius * (tryN < 8 ? 1f : 0.6f); Vector3 p = c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                if (UnityEngine.AI.NavMesh.SamplePosition(p, out var nh, 4f, UnityEngine.AI.NavMesh.AllAreas) && Mathf.Abs(nh.position.y - c.y) < 3f
                    && !UnityEngine.AI.NavMesh.Raycast(c, nh.position, out _, UnityEngine.AI.NavMesh.AllAreas)) return nh.position + Vector3.up * 0.1f;   // 플레이어와 길로 이어진 곳
            }
            Vector3 q = c + new Vector3(radius, 0, 0);
            return Physics.Raycast(q + Vector3.up * 2f, Vector3.down, out var h, 10f) ? h.point + Vector3.up * 0.2f : c + Vector3.forward * 3f;
        }

        void SpawnWave()
        {
            Wave++; waveActive = true; if (player) player.inCombat = true; bool boss = bossEvery > 0 && Wave % bossEvery == 0; int n = boss ? Mathf.Max(1, firstWave - 1) + Wave / bossEvery : firstWave + (Wave - 1) * perWave;
            var counts = new Dictionary<GameObject, int>();
            for (int i = 0; i < n; i++)
            {
                Vector3 p = SpawnPoint(spawnRadius + UnityEngine.Random.value * 4f);
                var prefab = PickPrefab(counts); if (prefab == null) prefab = enemyPrefab;
                counts[prefab] = counts.TryGetValue(prefab, out var c0) ? c0 + 1 : 1;
                var go = Instantiate(prefab, p, Quaternion.identity); alive.Add(go);
                if (go.TryGetComponent<EnemyCombatController>(out var ec)) ec.SetLevel(LevelMath.EnemyLevel(Wave, UnityEngine.Random.Range(-1, 2), ec.levelOffset, false));
            }
            if (boss)
            {   // 보스 웨이브: 곰곰이(잠꾸러기 곰 대장 — 사골곰뼈) ↔ 거대한 그림자 번갈아(첫 보스는 곰곰이)
                GameObject bear = null; foreach (var e in enemyPrefabs) if (e != null && e.name == "SleepyBear") { bear = e; break; }
                bool bearTurn = bear != null && (Wave / bossEvery) % 2 == 1;
                Vector3 bp = SpawnPoint(spawnRadius); var b = Instantiate(bearTurn ? bear : enemyPrefab, bp, Quaternion.identity);
                if (b.TryGetComponent<EnemyCombatController>(out var becc))
                {   // 곰곰이는 중간 보스: 보스 레벨 +3 없이(웨이브 + 곰 +1)
                    if (bearTurn) { becc.ConfigureBearBoss(); becc.SetLevel(LevelMath.EnemyLevel(Wave, 0, becc.levelOffset, false)); Quests.AnnounceNow("중간 보스", "곰곰이 — 잠꾸러기 곰들의 대장"); }
                    else { becc.ConfigureBoss(); becc.SetLevel(LevelMath.EnemyLevel(Wave, 0, becc.levelOffset, true)); }
                }
                alive.Add(b);
            }
            WaveStarted?.Invoke(Wave);
        }
    }
}
