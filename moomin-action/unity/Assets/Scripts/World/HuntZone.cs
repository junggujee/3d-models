using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace MoominAction
{
    /// <summary>
    /// 토벌 구역: 한 종류의 일반 몬스터가 저절로 생기는 곳(HuntCatalog 의 구역마다 하나, HuntZoneSpawner 가 만듦).
    ///  · 플레이어가 activeRange 안에 있을 때만 생성 — 최대 maxAlive 마리, 하나가 쓰러지면 respawn 초 뒤 다시(처음엔 바로)
    ///  · 생기는 자리: 반지름 안의 걸을 수 있는 곳(플레이어 6m 안은 피함), 퐁 하는 불꽃
    ///  · 레벨 = HuntMath.ZoneLevel(구역 최소, 내 레벨) + 종류 보정
    ///  · 구역 몬스터는 덜 공격적(aggroRange 12)이고 구역에서 멀리 끌려가면 집으로 돌아감(EnemyBrain.home/leash)
    ///  · 가운데 위 표지판(이름 + 몬스터 · 레벨) — 가까이 가면 보임
    /// </summary>
    public class HuntZone : MonoBehaviour
    {
        public static readonly List<HuntZone> All = new();
        public string zoneId, zoneName, kind, monsterName; public GameObject prefab;
        public float radius = 9f, respawn = 7f, activeRange = 60f; public int maxAlive = 4, minLevel = 1;
        readonly List<EnemyCombatController> alive = new(); float nextSpawn; Transform player, sign; Text signText; CanvasGroup signGroup;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static HuntZone Find(string id) { foreach (var z in All) if (z != null && z.zoneId == id) return z; return null; }
        /// <summary>구역 안(가장자리 4m 여유, 높이 무시)?</summary>
        public static bool Contains(string id, Vector3 p)
        {
            var z = Find(id); if (z == null) return false; var d = p - z.transform.position; d.y = 0; return d.magnitude <= z.radius + 4f;
        }

        void Start() { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) player = p.transform; BuildSign(); }

        void Update()
        {
            if (UIState.Paused || prefab == null) return;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return; player = p.transform; }
            for (int i = alive.Count - 1; i >= 0; i--)
            {
                var e = alive[i]; if (e != null && e.Stats != null && e.Stats.IsAlive) continue;
                alive.RemoveAt(i); nextSpawn = Mathf.Max(nextSpawn, Time.time + respawn);
            }
            float d = Vector3.Distance(player.position, transform.position);
            if (d <= activeRange && alive.Count < maxAlive && Time.time >= nextSpawn) { Spawn(); nextSpawn = Time.time + 0.6f; }
            UpdateSign(d);
        }

        void Spawn()
        {
            for (int t = 0; t < 10; t++)
            {
                var r = Random.insideUnitCircle * radius; var p = transform.position + new Vector3(r.x, 0, r.y);
                if (!NavMesh.SamplePosition(p, out var hit, 3f, NavMesh.AllAreas)) continue;
                if (Vector3.Distance(hit.position, player.position) < 6f) continue;
                var go = Instantiate(prefab, hit.position + Vector3.up * 0.1f, Quaternion.Euler(0, Random.Range(0f, 360f), 0)); go.name = kind + "_" + zoneId;
                if (!go.TryGetComponent<EnemyCombatController>(out var ec)) { Destroy(go); return; }
                ec.kind = kind; ec.zoneId = zoneId;
                int pl = Experience.Instance != null ? Experience.Instance.level : 1;
                ec.SetLevel(HuntMath.ZoneLevel(minLevel, pl, Random.Range(-1, 2)) + ec.levelOffset);
                if (go.TryGetComponent<EnemyBrain>(out var brain)) { brain.aggroRange = 12f; brain.home = transform; brain.leash = radius + 16f; }
                alive.Add(ec); SparkBurst.Spawn(hit.position + Vector3.up * 0.3f, Vector3.up, 14, 2.5f, new Color(1f, 0.8f, 0.95f));
                return;
            }
        }

        // ── 표지판 ─────────────────────────
        void BuildSign()
        {
            var go = new GameObject("ZoneSign", typeof(RectTransform)); go.transform.SetParent(transform, false); go.transform.localPosition = new Vector3(0, 2.6f, 0);
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(600, 160); rt.localScale = Vector3.one * 0.008f;
            signGroup = go.AddComponent<CanvasGroup>(); signGroup.alpha = 0f;
            var tgo = new GameObject("Text", typeof(RectTransform)); tgo.transform.SetParent(go.transform, false); ((RectTransform)tgo.transform).sizeDelta = new Vector2(600, 160);
            var t = tgo.AddComponent<Text>(); t.font = UI.UITheme.HeadingFont; t.fontSize = 56; t.alignment = TextAnchor.MiddleCenter; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.color = UI.UITheme.GoldBright;
            var o = tgo.AddComponent<Outline>(); o.effectColor = new Color(0.2f, 0.11f, 0.04f, 0.9f); o.effectDistance = new Vector2(3, -3);
            t.text = $"◆ {zoneName} ◆\n<size=34><color=#F4E8CF>{monsterName} 출몰 · Lv.{minLevel}~</color></size>";
            sign = go.transform; signText = t;
        }
        void UpdateSign(float d)
        {
            if (sign == null) return;
            float want = d < 32f && d > radius * 0.5f ? 1f : 0f; signGroup.alpha = Mathf.MoveTowards(signGroup.alpha, want, Time.deltaTime * 2f);
            if (signGroup.alpha <= 0f) return;
            var cam = Camera.main; if (cam != null) sign.rotation = Quaternion.LookRotation(sign.position - cam.transform.position);
        }
    }
}
