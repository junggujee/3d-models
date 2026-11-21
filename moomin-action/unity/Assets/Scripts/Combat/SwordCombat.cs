using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 스파크 검 3연타 콤보. 입력 → 공격 애니메이션(Attack1/2/3) → 타격 시점에 앞쪽 부채꼴 안의 적에게 피해·넉백·스파크.
    /// 애니메이션 이벤트 없이 타이머로 처리(Blender 클립 길이: 24f/24f/30f @30fps, 타격 프레임 11).
    /// </summary>
    public class SwordCombat : MonoBehaviour
    {
        [System.Serializable] public struct Step { public string state; public float duration, hitTime, damage, knockback, range, angle; }
        public Animator animator; public SwordGlow glow; public Transform sparkOrigin; public LayerMask hitMask = ~0;
        public Step[] combo = {
            new Step { state = "Attack1", duration = 0.80f, hitTime = 0.37f, damage = 12, knockback = 4.5f, range = 1.9f, angle = 110 },
            new Step { state = "Attack2", duration = 0.80f, hitTime = 0.37f, damage = 14, knockback = 5.0f, range = 1.9f, angle = 110 },
            new Step { state = "Attack3", duration = 1.00f, hitTime = 0.40f, damage = 26, knockback = 9.0f, range = 2.3f, angle = 160 } };
        public float comboWindow = 0.45f;
        PlayerController pc; int index = -1; float t; bool hitDone, buffered;
        public bool Attacking => index >= 0;

        void Awake() { pc = GetComponent<PlayerController>(); if (!animator) animator = GetComponentInChildren<Animator>(); if (!glow) glow = GetComponentInChildren<SwordGlow>(); }

        void Update()
        {
            if (pc && pc.locked) { index = -1; return; }
            if (InputAdapter.AttackPressed()) { if (index < 0) Begin(0); else buffered = true; }
            if (index < 0) { if (pc) pc.moveMultiplier = 1f; if (glow) glow.Attacking = false; return; }
            var s = combo[index]; t += Time.deltaTime;
            if (pc) pc.moveMultiplier = 0.15f; if (glow) glow.Attacking = true;
            if (!hitDone && t >= s.hitTime) { hitDone = true; DoHit(s); }
            if (hitDone && buffered && t >= s.hitTime + 0.1f && index < combo.Length - 1) { Begin(index + 1); return; }
            if (t >= s.duration + (index < combo.Length - 1 ? 0f : 0.1f)) { index = -1; }
        }

        void Begin(int i) { index = i; t = 0; hitDone = false; buffered = false; if (animator) animator.CrossFadeInFixedTime(combo[i].state, 0.06f); }

        void DoHit(Step s)
        {
            Vector3 origin = transform.position + Vector3.up * 0.8f; bool any = false;
            foreach (var c in Physics.OverlapSphere(origin, s.range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(transform)) continue;
                var d = c.GetComponentInParent<IDamageable>(); if (d == null || d.IsDead) continue;
                Vector3 to = c.bounds.center - origin; to.y = 0;
                if (Vector3.Angle(transform.forward, to) > s.angle * 0.5f && to.magnitude > 0.8f) continue;
                Vector3 hp = c.ClosestPoint(origin + transform.forward * 0.5f);
                d.TakeDamage(s.damage, hp, to.normalized * s.knockback + Vector3.up * 2f);
                SparkBurst.Spawn(hp, -to.normalized + Vector3.up * 0.5f, index == 2 ? 55 : 28, index == 2 ? 8f : 5f); any = true;
            }
            if (!any && sparkOrigin) SparkBurst.Spawn(sparkOrigin.position, transform.forward, 10, 3f);   // 헛스윙도 칼 끝에서 불꽃
        }
    }
}
