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
        public GameObject enemyPrefab; public Health player; public float spawnRadius = 14f, wavePause = 4f; public int firstWave = 3, perWave = 2, bossEvery = 3;
        public int Score { get; private set; } public int Wave { get; private set; } public bool IsOver { get; private set; }
        public int EnemiesAlive { get { alive.RemoveAll(g => g == null); return alive.Count; } }
        public event Action<int> WaveStarted, WaveCleared;
        readonly List<GameObject> alive = new(); float nextWave = 2f; bool waveActive; SanitySystem sanity;

        void Awake() { Instance = this; }
        void Start()
        {
            if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.GetComponent<Health>(); }
            if (player) player.Died += () => IsOver = true; sanity = player ? player.GetComponent<SanitySystem>() : null;
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
            if (waveActive && EnemiesAlive == 0) { waveActive = false; if (sanity) sanity.inCombat = false; WaveCleared?.Invoke(Wave); nextWave = Time.time + wavePause; }
            if (!waveActive && Time.time >= nextWave && enemyPrefab && player) SpawnWave();
        }

        void SpawnWave()
        {
            Wave++; waveActive = true; if (sanity) sanity.inCombat = true; bool boss = bossEvery > 0 && Wave % bossEvery == 0; int n = boss ? Mathf.Max(1, firstWave - 1) + Wave / bossEvery : firstWave + (Wave - 1) * perWave;
            for (int i = 0; i < n; i++)
            {
                float a = UnityEngine.Random.value * Mathf.PI * 2; Vector3 p = player.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (spawnRadius + UnityEngine.Random.value * 4f);
                if (Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out var h, 80f)) p = h.point + Vector3.up * 0.2f;
                alive.Add(Instantiate(enemyPrefab, p, Quaternion.identity));
            }
            if (boss) { Vector3 bp = player.transform.position + player.transform.forward * spawnRadius; var b = Instantiate(enemyPrefab, bp, Quaternion.identity); b.GetComponent<EnemyAI>()?.ConfigureBoss(); alive.Add(b); }
            WaveStarted?.Invoke(Wave);
        }
    }
}
