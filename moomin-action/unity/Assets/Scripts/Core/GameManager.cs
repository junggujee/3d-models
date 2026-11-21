using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoominAction
{
    /// <summary>웨이브 스폰, 점수, 간단한 HUD(OnGUI라 UI 패키지 없이 동작). 게임오버 시 R 키로 재시작.</summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameObject enemyPrefab; public Health player; public float spawnRadius = 14f, wavePause = 3f; public int firstWave = 3, perWave = 2;
        public int Score { get; private set; } public int Wave { get; private set; }
        readonly List<GameObject> alive = new(); float nextWave = 2f; bool over;

        void Awake() { Instance = this; }
        void Start() { if (!player) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.GetComponent<Health>(); } if (player) player.Died += () => over = true; }
        public void AddScore(int s) { Score += s; }

        void Update()
        {
            if (over) { if (InputAdapter.RestartPressed()) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); return; }
            alive.RemoveAll(g => g == null);
            if (alive.Count == 0 && Time.time >= nextWave && enemyPrefab && player) SpawnWave();
            if (alive.Count > 0) nextWave = Time.time + wavePause;
        }

        void SpawnWave()
        {
            Wave++; int n = firstWave + (Wave - 1) * perWave;
            for (int i = 0; i < n; i++)
            {
                float a = Random.value * Mathf.PI * 2; Vector3 p = player.transform.position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (spawnRadius + Random.value * 4f);
                if (Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out var h, 80f)) p = h.point + Vector3.up * 0.2f;
                alive.Add(Instantiate(enemyPrefab, p, Quaternion.identity));
            }
        }

        void OnGUI()
        {
            var big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold }; big.normal.textColor = Color.white;
            if (player) { float r = player.Current / player.maxHealth; GUI.color = new Color(0, 0, 0, .6f); GUI.DrawTexture(new Rect(20, 20, 304, 28), Texture2D.whiteTexture); GUI.color = Color.Lerp(new Color(.9f, .2f, .2f), new Color(.3f, .9f, .4f), r); GUI.DrawTexture(new Rect(22, 22, 300 * r, 24), Texture2D.whiteTexture); GUI.color = Color.white; }
            GUI.Label(new Rect(20, 54, 400, 34), $"웨이브 {Wave}   점수 {Score}", big);
            GUI.Label(new Rect(20, Screen.height - 34, 700, 28), "WASD 이동 · Shift 달리기 · Space 점프 · 좌클릭/J 공격(3연타) · Esc 커서 해제", GUI.skin.label);
            if (over) { var c = new GUIStyle(big) { alignment = TextAnchor.MiddleCenter, fontSize = 40 }; GUI.Label(new Rect(0, Screen.height / 2 - 60, Screen.width, 80), "쓰러졌어요…  R 키로 다시 시작", c); }
        }
    }
}
