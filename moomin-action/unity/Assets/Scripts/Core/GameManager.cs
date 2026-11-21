using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoominAction
{
    /// <summary>웨이브 스폰·점수·게임오버 상태. UI(HUD/메뉴)는 UIRoot 가 담당하고 이 컴포넌트가 자동 생성함. 쓰러진 뒤 R 키로 재시작.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameObject enemyPrefab; [Tooltip("웨이브에 섞어 나올 적들(비어 있으면 enemyPrefab 만). 보스는 enemyPrefab 으로 만듦")] public List<GameObject> enemyPrefabs = new(); public CharacterStats player; public float spawnRadius = 14f, wavePause = 4f; public int firstWave = 3, perWave = 2, bossEvery = 3;
        public int Score { get; private set; } public int Wave { get; private set; } public bool IsOver { get; private set; }
        public int EnemiesAlive { get { alive.RemoveAll(g => g == null); return alive.Count; } }
        public event Action<int> WaveStarted, WaveCleared;
        readonly List<GameObject> alive = new(); float nextWave = 2f; bool waveActive;

        void Awake() { Instance = this; }
        void Start()
        {
            if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.GetComponent<CharacterStats>(); }
            var baker = GetComponent<NavMeshBaker>(); if (baker == null) baker = gameObject.AddComponent<NavMeshBaker>(); baker.Build();
            if (player) player.Died += () => IsOver = true;
            #if UNITY_2023_1_OR_NEWER
            bool hasUi = FindFirstObjectByType<UI.UIRoot>() != null;
#else
            bool hasUi = FindObjectOfType<UI.UIRoot>() != null;
#endif
            if (!hasUi) { var go = new GameObject("UIRoot"); var ui = go.AddComponent<UI.UIRoot>(); ui.player = player; }
        }
        public void AddScore(int s) { Score += s; }

        void Update()
        {
            if (IsOver) { if (InputAdapter.RestartPressed()) { UIState.Reset(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); } return; }
            if (UIState.Paused) return;
            if (waveActive && EnemiesAlive == 0) { waveActive = false; if (player) player.inCombat = false; WaveCleared?.Invoke(Wave); nextWave = Time.time + wavePause; }
            if (!waveActive && Time.time >= nextWave && enemyPrefab && player) SpawnWave();
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
            for (int i = 0; i < n; i++)
            {
                Vector3 p = SpawnPoint(spawnRadius + UnityEngine.Random.value * 4f);
                var prefab = enemyPrefabs.Count > 0 ? enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Count)] : enemyPrefab; if (prefab == null) prefab = enemyPrefab;
                alive.Add(Instantiate(prefab, p, Quaternion.identity));
            }
            if (boss) { Vector3 bp = SpawnPoint(spawnRadius); var b = Instantiate(enemyPrefab, bp, Quaternion.identity); b.GetComponent<EnemyCombatController>()?.ConfigureBoss(); alive.Add(b); }
            WaveStarted?.Invoke(Wave);
        }
    }
}
