using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 공격 충돌과 피해 처리(명세 v2 6장). 히트박스가 열린 동안 매 프레임 범위·각도 안의 상대 팀을 찾아
    ///  · 상대도 히트박스가 열려 있고 나를 겨누고 있으면 → 클래시(ClashManager)
    ///  · 아니면 → 피해 D = P × M × R (한 공격당 대상별 1회, 방어·패링·회피 반영)
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class CombatResolver : MonoBehaviour
    {
        public LayerMask targetMask = ~0; public Transform attackPoint; public Transform sparkOrigin;
        CharacterStats stats; SkillExecutor executor; static readonly Collider[] buffer = new Collider[32];
        readonly List<CharacterStats> found = new();

        void Awake() { stats = GetComponent<CharacterStats>(); executor = GetComponent<SkillExecutor>(); }
        Vector3 Origin => (attackPoint ? attackPoint.position : transform.position + Vector3.up * 0.8f);

        /// <summary>코인이 판정된 순간 호출(첫 검사). 이후 히트박스가 열려 있는 동안 Tick 으로 계속 검사.</summary>
        public void ResolveAttack(AttackContext attack, SkillDefinition skill)
        {
            Tick(attack, skill);
            if (!attack.isHit && !attack.clashed && sparkOrigin) SparkBurst.Spawn(sparkOrigin.position, transform.forward, 8, 3f, attack.isHeads ? SparkBurst.Gold : (Color?)null);   // 헛스윙
        }

        public void Tick(AttackContext attack, SkillDefinition skill)
        {
            if (attack.cancelled || !stats.IsAlive) return;
            foreach (var target in FindTargets(skill))
            {
                if (attack.cancelled) return;
                if (attack.hitTargets.Contains(target)) continue;
                // 클래시: 양쪽 공격이 동시에 활성화되어 있을 때만
                var other = target.GetComponent<SkillExecutor>();
                var theirs = other != null ? other.CurrentAttack : null;
                if (skill.clashEnabled && theirs != null && theirs.HitboxOpen && !theirs.clashed && !attack.clashed && theirs.skill.clashEnabled && other.resolver.IsTargeting(stats, theirs.skill))
                {
                    ClashManager.Instance.Resolve(attack, theirs);
                    if (attack.cancelled) return; continue;
                }
                ApplyHit(attack, target);
            }
        }

        /// <summary>내 공격 범위·각도 안에 이 대상이 있는가(클래시 조건).</summary>
        public bool IsTargeting(CharacterStats target, SkillDefinition skill)
        {
            Vector3 to = target.transform.position - transform.position; to.y = 0;
            return to.magnitude <= skill.range + 0.6f && (skill.angle >= 360f || Vector3.Angle(transform.forward, to) <= skill.angle * 0.5f + 15f);
        }

        List<CharacterStats> FindTargets(SkillDefinition skill)
        {
            found.Clear(); int n = Physics.OverlapSphereNonAlloc(Origin, skill.range, buffer, targetMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var t = buffer[i].GetComponentInParent<CharacterStats>();
                if (t == null || t == stats || t.team == stats.team || !t.IsAlive || found.Contains(t)) continue;
                Vector3 to = t.transform.position - transform.position; to.y = 0;
                if (skill.angle < 360f && to.magnitude > 0.8f && Vector3.Angle(transform.forward, to) > skill.angle * 0.5f) continue;
                found.Add(t);
            }
            return found;
        }

        /// <summary>명중 처리: 방어(정면)·패링·회피 무적 → 피해 D = P × M × R.</summary>
        public void ApplyHit(AttackContext attack, CharacterStats target)
        {
            if (!target.IsAlive || attack.hitTargets.Contains(target)) return;
            attack.hitTargets.Add(target); var skill = attack.skill;
            Vector3 to = target.transform.position - transform.position; to.y = 0; Vector3 hitPoint = target.transform.position + Vector3.up * 0.8f - to.normalized * 0.3f;
            float r = target.DamageTakenMultiplier;
            bool facing = Vector3.Angle(target.transform.forward, -to) <= 70f;
            if (target.Guarding && facing && !skill.unblockable)
            {
                if (target.IsParryWindow)   // 패링: 피해 없음, 공격자 경직
                {
                    target.ChangeSanity(CharacterStats.ParrySuccess, "패링"); attack.cancelled = true;
                    SparkBurst.Spawn(hitPoint, -to.normalized + Vector3.up, 50, 7f, SparkBurst.Gold); HitStop.Do(0.14f);
                    GetComponent<ICombatant>()?.Stagger(1.0f, 1.5f); ClashManager.RaiseParry(target, stats); return;
                }
                r *= target.guardDamageMultiplier; target.ChangeSanity(CharacterStats.GuardBlock, "방어");
            }
            int aff = SkillAttributes.Affinity(skill.attribute, SkillAttributes.DefenderAttribute(target)); attack.affinity = aff; r *= SkillAttributes.DamageMultiplier(aff);   // 속성 상성
            float m = skill.damageMultiplier * (executor ? executor.weaponMultiplier : 1f) * stats.DamageDealtMultiplier;
            int dmg = DamageCalculator.Final(attack.power, m, r);
            bool wasInvulnerable = target.Invulnerable;
            target.TakeDamage(dmg, hitPoint, to.normalized * skill.knockback + Vector3.up * 1.5f);
            if (wasInvulnerable) return;   // 회피됨
            attack.isHit = true; stats.ChangeSanity(CharacterStats.HitLanded, "명중");
            var se = target.GetComponent<StatusEffects>(); if (se == null) se = target.gameObject.AddComponent<StatusEffects>();   // 상태이상: 진동 발동 → 새로 부여
            se.OnHitTaken(); se.Apply(skill.onHitStatus, skill.onHitStacks);
            SparkBurst.Spawn(hitPoint, -to.normalized + Vector3.up * 0.5f, attack.isHeads ? 40 : 22, attack.isHeads ? 7f : 4.5f, attack.isHeads ? SparkBurst.Gold : (stats.team == Team.Enemy ? SparkBurst.Crimson : (Color?)null));
            DamageDealt?.Invoke(attack, target, dmg);
        }
        public static event System.Action<AttackContext, CharacterStats, int> DamageDealt;
    }
}
