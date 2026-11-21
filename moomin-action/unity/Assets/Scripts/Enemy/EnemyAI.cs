using UnityEngine;

namespace MoominAction
{
    /// <summary>그림자 덩어리: 플레이어를 쫓아와 선딜 후 박치기. 통통 튀는 움직임은 코드로(스쿼시), 피격 시 넉백, 죽으면 쪼그라듦.</summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        public float speed = 2.4f, aggroRange = 25f, attackRange = 1.25f, windup = 0.55f, attackCooldown = 1.4f, damage = 9f, gravity = -20f; public int score = 10;
        public Transform visual; Transform target; CharacterController cc; Health hp; Vector3 knock, vel; float nextAttack, windupT = -1, deadT = -1;

        void Awake() { cc = GetComponent<CharacterController>(); hp = GetComponent<Health>(); if (!visual) visual = transform.childCount > 0 ? transform.GetChild(0) : transform; hp.Damaged += (a, p, k) => { knock = k; }; hp.Died += () => { deadT = 0; GameManager.Instance?.AddScore(score); DropLoot(); }; }
        /// <summary>드랍: 금화 항상, 별조각 40%, 붉은 약병 25%, 불꽃 가루 30%, 은빛 깃털 8%, 무기 3%.</summary>
        void DropLoot()
        {
            Vector3 p = transform.position + Vector3.up * 0.5f;
            ItemPickup.Spawn("gold", Random.Range(5, 16), p);
            if (Random.value < 0.40f) ItemPickup.Spawn("shard_star", 1, p); if (Random.value < 0.25f) ItemPickup.Spawn("potion_red", 1, p);
            if (Random.value < 0.30f) ItemPickup.Spawn("dust_fire", Random.Range(1, 4), p); if (Random.value < 0.08f) ItemPickup.Spawn("feather_silver", 1, p);
            if (Random.value < 0.03f) ItemPickup.Spawn("sword_ember", 1, p);
        }
        void Start() { var p = GameObject.FindGameObjectWithTag("Player"); if (p) target = p.transform; }

        void Update()
        {
            if (UIState.Paused) return;
            if (deadT >= 0) { deadT += Time.deltaTime; visual.localScale = Vector3.one * Mathf.Max(0, 1 - deadT * 2.5f); if (deadT > 0.4f) Destroy(gameObject); return; }
            knock = Vector3.Lerp(knock, Vector3.zero, 8f * Time.deltaTime);
            Vector3 move = Vector3.zero; float bob = 0;
            if (target)
            {
                Vector3 to = target.position - transform.position; to.y = 0; float d = to.magnitude;
                if (d < aggroRange && windupT < 0 && knock.sqrMagnitude < 1f)
                {
                    if (d > attackRange) { move = to.normalized * speed; bob = Mathf.Abs(Mathf.Sin(Time.time * 8f)); }
                    else if (Time.time >= nextAttack) windupT = 0;
                    if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 10f * Time.deltaTime);
                }
                if (windupT >= 0)
                {
                    windupT += Time.deltaTime; visual.localScale = new Vector3(1.15f, 0.8f, 1.15f);   // 웅크림
                    if (windupT >= windup)
                    {
                        windupT = -1; nextAttack = Time.time + attackCooldown;
                        if (d < attackRange + 0.5f) { var h = target.GetComponent<IDamageable>(); h?.TakeDamage(damage, target.position + Vector3.up, to.normalized * 5f + Vector3.up * 2f); }
                        visual.localScale = new Vector3(0.85f, 1.2f, 0.85f);
                    }
                }
            }
            if (windupT < 0) visual.localScale = Vector3.Lerp(visual.localScale, new Vector3(1 + 0.08f * bob, 1 - 0.12f * bob, 1 + 0.08f * bob), 12f * Time.deltaTime);
            if (cc.isGrounded && vel.y < 0) vel.y = -2f; vel.y += gravity * Time.deltaTime;
            cc.Move((move + knock + new Vector3(0, vel.y, 0)) * Time.deltaTime);
        }
    }
}
