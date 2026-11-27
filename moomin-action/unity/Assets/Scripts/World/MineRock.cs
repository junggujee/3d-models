using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 지하 광산 노역장의 캘 수 있는 바위(CastleDungeon.fbx 의 MineRock_Template 을 Mark_Rock_* 자리마다 복제).
    /// 노역용 곡괭이가 있으면 F 로 곡괭이질 — MineMath.HitsPerRock 번이면 부서짐 → 돌덩이 1~2 + 확률로 광석(등급별) → 25초 뒤 다시 솟음.
    /// 곡괭이질 연출: 민미 Attack1 동작 · 돌가루 불똥 · 화면 살짝 흔들림 · 바위 흔들림/움츠림. 부서질 때 크게 터짐 + 광석 등급 알림.
    /// </summary>
    public class MineRock : MonoBehaviour
    {
        int hits; float respawnAt = -1f, wobble; Vector3 baseScale; Renderer[] rends; Collider[] cols; Transform player; Animator playerAnim;
        static readonly Color Dust = new Color(0.72f, 0.58f, 0.44f);

        void Awake() { baseScale = transform.localScale; rends = GetComponentsInChildren<Renderer>(true); cols = GetComponentsInChildren<Collider>(true); }
        bool Broken => respawnAt >= 0f;

        void Update()
        {
            if (UIState.Paused) return;
            if (Broken)
            {
                if (Time.time < respawnAt) return;
                respawnAt = -1f; hits = 0; Show(true); transform.localScale = baseScale * 0.2f; return;
            }
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 6f * Time.deltaTime);
            if (wobble > 0f) { wobble -= Time.deltaTime; transform.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 70f) * 6f * wobble, 0f, 0f); }
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return; player = p.transform; playerAnim = p.GetComponentInChildren<Animator>(); }
            Vector3 d = player.position - transform.position; d.y = 0; float dist = d.magnitude;
            if (dist < 2.2f && Mathf.Abs(player.position.y - transform.position.y) < 2f) Interaction.Offer(this, dist, $"F   곡괭이질  ({hits}/{MineMath.HitsPerRock})", Swing);
        }

        void Swing()
        {
            var inv = player != null ? player.GetComponent<Inventory>() : null; if (inv == null) return;
            if (inv.Count(MineMath.Pickaxe) <= 0) { Toast.Show("맨손으로는 못 캐요… 간수 하프에게 곡괭이를 받자"); return; }
            Vector3 to = transform.position - player.position; to.y = 0; if (to.sqrMagnitude > 0.01f) player.rotation = Quaternion.LookRotation(to);
            if (playerAnim != null && playerAnim.HasState(0, Animator.StringToHash("Attack1"))) playerAnim.CrossFadeInFixedTime("Attack1", 0.05f);
            hits++; wobble = 0.35f; transform.localScale = baseScale * 0.9f;
            Vector3 at = transform.position + Vector3.up * 0.6f - to.normalized * 0.4f;
            SparkBurst.Spawn(at, -to.normalized + Vector3.up, 18, 4f, Dust); SparkBurst.Spawn(at, Vector3.up, 6, 3f, new Color(1f, 0.85f, 0.5f));
            ThirdPersonCamera.Shake(0.08f, 0.12f);
            if (hits >= MineMath.HitsPerRock) Break(inv);
        }

        void Break(Inventory inv)
        {
            int rub = MineMath.RollRubble(Random.value); string ore = MineMath.RollOre(Random.value);
            Give(inv, MineMath.Rubble, rub);
            string msg = $"돌덩이 +{rub}  ({MineMath.Progress(inv.Count(MineMath.Rubble))})";
            if (ore != null)
            {
                Give(inv, ore, 1); var d = ItemDatabase.Get(ore); string g = MineMath.Grade(ore);
                msg += $"  ·  {d.name} [{g}]";
                if (g == "S" || g == "SS") { Quests.AnnounceNow(g == "SS" ? "희귀 광석!!" : "귀한 광석!", d.name + "  [" + g + "]"); SparkBurst.Spawn(transform.position + Vector3.up, Vector3.up, 50, 6f, d.color); }
            }
            Toast.Show(msg);
            SparkBurst.Spawn(transform.position + Vector3.up * 0.5f, Vector3.up, 40, 6f, Dust); ThirdPersonCamera.Shake(0.2f, 0.25f);
            if (MineMath.QuotaMet(inv.Count(MineMath.Rubble)) && inv.Count(MineMath.Rubble) - rub < MineMath.Quota) Quests.AnnounceNow("할당량 달성!", "간수 하프에게 돌덩이를 내자");
            Show(false); respawnAt = Time.time + MineMath.RespawnSeconds;
        }
        static void Give(Inventory inv, string id, int n) { int left = inv.Add(id, n, silent: true); if (left > 0) ItemPickup.Spawn(id, left, inv.transform.position + Vector3.up * 0.5f); }
        void Show(bool on) { foreach (var r in rends) if (r != null) r.enabled = on; foreach (var c in cols) if (c != null) c.enabled = on; }
    }
}
