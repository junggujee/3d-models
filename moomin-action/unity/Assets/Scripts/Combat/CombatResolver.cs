using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 공격 대상 탐색과 피해 처리. <b>코인 판정(SkillExecutor, 공격마다 1번 → AttackContext)과 대상별 피해 판정(여기)을 분리.</b>
    /// 히트박스가 열린 동안 대상을 찾아
    ///  · 단일 공격: 사거리·각도 안에서 <b>한 명</b>(조준 대상 우선, 없으면 가장 가까운 적)
    ///  · 광역 공격(isAreaAttack): 공격자 중심 areaRadius 안의 <b>모든 적</b> — 같은 AttackContext(같은 코인 결과·위력)를 모두에게
    ///  · 상대도 히트박스가 열려 있고 나를 겨누고 있으면 → 합(ClashManager)
    ///  · 아니면 대상마다 따로: ① 회피(회피 코인 위력 ≥ 공격 위력이면 피해 없음) ② 가드(피해 × (1-감소율)) ③ 피해 D = P × M × R
    /// 한 캐릭터에 콜라이더가 여러 개여도 한 코인에 한 번만(hitTargets + 중복 제거).
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class CombatResolver : MonoBehaviour
    {
        public LayerMask targetMask = ~0; public Transform attackPoint; public Transform sparkOrigin;
        CharacterStats stats; SkillExecutor executor; LockOnSystem lockOn; EnemyCombatController enemy; static readonly Collider[] buffer = new Collider[64];
        readonly List<CharacterStats> found = new(), picked = new();

        void Awake() { stats = GetComponent<CharacterStats>(); executor = GetComponent<SkillExecutor>(); lockOn = GetComponent<LockOnSystem>(); enemy = GetComponent<EnemyCombatController>(); }
        Vector3 Origin => (attackPoint ? attackPoint.position : transform.position + Vector3.up * 0.8f);

        /// <summary>코인이 판정된 순간 호출(첫 검사). 이후 히트박스가 열려 있는 동안 Tick 으로 계속 검사.</summary>
        public void ResolveAttack(AttackContext attack, SkillDefinition skill)
        {
            if (skill.vfx != SkillVfx.None) SkillVfxPlayer.Play(skill, transform, attack.isHeads);   // 빔·충격파·물기둥(코인마다 한 번)
            if (skill.isProjectile) { Projectile.Launch(this, attack, AimTarget(), LaunchPoint); return; }   // 원거리: 맞히는 건 투사체가(ApplyHit)
            Tick(attack, skill);
            if (!attack.isHit && !attack.clashed && sparkOrigin) SparkBurst.Spawn(sparkOrigin.position, transform.forward, 8, 3f, attack.isHeads ? SparkBurst.Gold : (Color?)null);   // 헛스윙
        }

        public void Tick(AttackContext attack, SkillDefinition skill)
        {
            if (attack.cancelled || !stats.IsAlive || (executor != null && executor.Held) || skill.isProjectile) return;
            if (!skill.isAreaAttack && attack.hitTargets.Count > 0) return;   // 단일 공격: 이 코인은 이미 한 명에게 판정됨
            foreach (var target in FindTargets(skill))
            {
                if (attack.cancelled) return;
                if (attack.hitTargets.Contains(target)) continue;
                // 클래시: 양쪽 공격이 동시에 활성화되어 있을 때만
                var other = target.GetComponent<SkillExecutor>();
                var theirs = other != null ? other.CurrentAttack : null;
                if (skill.MeleeClashable && theirs != null && theirs.HitboxOpen && !theirs.clashed && !attack.clashed && theirs.skill.MeleeClashable && other.resolver.IsTargeting(stats, theirs.skill))
                {
                    ClashManager.Instance.Resolve(attack, theirs);
                    return;   // 이 공격은 합으로 넘어감(결과는 ClashManager 가 연출 뒤 적용)
                }
                ApplyHit(attack, target);
            }
        }

        /// <summary>투사체가 나가는 곳(검 끝 · 공격 지점 · 몸 앞 가슴 높이).</summary>
        Vector3 LaunchPoint => sparkOrigin ? sparkOrigin.position : attackPoint ? attackPoint.position : transform.position + transform.forward * 0.5f * transform.lossyScale.z + Vector3.up * 0.45f * transform.lossyScale.y;
        /// <summary>조준 대상: 플레이어 = 락온, 적 = 쫓는 대상. 없으면 null(앞으로 직선).</summary>
        CharacterStats AimTarget()
        {
            if (lockOn != null && lockOn.Target != null) return lockOn.Target.Stats;
            if (enemy != null && enemy.Target != null) return enemy.Target.GetComponentInParent<CharacterStats>();
            return null;
        }

        /// <summary>직선 광역(빔) 안인가: 앞으로 0 ~ areaRadius, 옆으로 ±areaWidth/2 (+여유).</summary>
        bool InLine(Vector3 p, SkillDefinition skill, float pad)
        {
            Vector3 local = transform.InverseTransformDirection(p - transform.position); local.x *= transform.lossyScale.x; local.z *= transform.lossyScale.z;
            return local.z >= -pad && local.z <= skill.areaRadius + pad && Mathf.Abs(local.x) <= skill.areaWidth * 0.5f + pad;
        }

        /// <summary>내 공격 범위·각도 안에 이 대상이 있는가(클래시 조건).</summary>
        public bool IsTargeting(CharacterStats target, SkillDefinition skill)
        {
            Vector3 to = target.transform.position - transform.position; to.y = 0;
            if (skill.isAreaAttack) return skill.areaShape == AreaShape.Line ? InLine(target.transform.position, skill, 0.6f) : to.magnitude <= skill.areaRadius + 0.6f;
            return to.magnitude <= skill.range + 0.6f && (skill.angle >= 360f || Vector3.Angle(transform.forward, to) <= skill.angle * 0.5f + 15f);
        }

        List<CharacterStats> FindTargets(SkillDefinition skill)
        {
            found.Clear(); picked.Clear();
            bool area = skill.isAreaAttack; int mask = area && skill.targetLayers.value != 0 ? skill.targetLayers.value : targetMask.value;
            bool line = area && skill.areaShape == AreaShape.Line;
            Vector3 center = line ? transform.position + transform.forward * skill.areaRadius * 0.5f + Vector3.up * 0.8f : area ? transform.position + Vector3.up * 0.8f : Origin;
            float radius = line ? Mathf.Sqrt(skill.areaRadius * skill.areaRadius * 0.25f + skill.areaWidth * skill.areaWidth * 0.25f) + 0.5f : area ? skill.areaRadius : skill.range;
            int n = Physics.OverlapSphereNonAlloc(center, radius, buffer, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) found.Add(buffer[i].GetComponentInParent<CharacterStats>());
            foreach (var t in DefenseRules.Unique(found))   // 콜라이더가 여러 개여도 캐릭터당 한 번
            {
                if (t == stats || t.team == stats.team || !t.IsAlive) continue;
                if (line && !InLine(t.transform.position, skill, 0.4f)) continue;   // 빔: 앞쪽 직사각형 안만
                if (!area) { Vector3 to = t.transform.position - transform.position; to.y = 0; if (skill.angle < 360f && to.magnitude > 0.8f && Vector3.Angle(transform.forward, to) > skill.angle * 0.5f) continue; }
                picked.Add(t);
            }
            if (area || picked.Count <= 1) return picked;
            // 단일 공격: 조준 대상(플레이어 = 락온, 적 = 쫓는 대상)이 범위 안이면 그 대상, 아니면 가장 가까운 적 한 명
            CharacterStats want = lockOn != null && lockOn.Target != null ? lockOn.Target.Stats : enemy != null && enemy.Target != null ? enemy.Target.GetComponentInParent<CharacterStats>() : null;
            CharacterStats best = picked.Contains(want) ? want : null; float bd = float.MaxValue;
            if (best == null) foreach (var t in picked) { float d = (t.transform.position - transform.position).sqrMagnitude; if (d < bd) { bd = d; best = t; } }
            picked.Clear(); picked.Add(best); return picked;
        }

        /// <summary>대상 한 명에 대한 판정(같은 코인이면 광역이라도 같은 attack.power): ① 회피 → ② 가드(패링) → ③ 피해 D = P × M × R.</summary>
        public void ApplyHit(AttackContext attack, CharacterStats target)
        {
            if (!target.IsAlive || attack.hitTargets.Contains(target)) return;
            attack.hitTargets.Add(target); var skill = attack.skill;
            Vector3 to = target.transform.position - transform.position; to.y = 0; Vector3 hitPoint = target.transform.position + Vector3.up * 0.8f - to.normalized * 0.3f;
            var def = target.GetComponent<DefenseController>();
            // ① 회피: 회피 코인 위력 ≥ 공격 위력이면 피해 없음(회피 유지), 아니면 회피가 끝나고 피해
            if (skill.canBeEvaded && def != null && def.IsEvading && def.TryEvade(attack.power))
            {
                SparkBurst.Spawn(hitPoint, -to.normalized + Vector3.up, 14, 4f, new Color(0.75f, 0.9f, 1f)); Evaded?.Invoke(attack, target); return;
            }
            float r = target.DamageTakenMultiplier;
            // ② 가드: 위력 비교 없이 피해 감소율만큼 줄임(가드 시작 직후면 패링 — DefenseController.parryEnabled)
            bool facing = Vector3.Angle(target.transform.forward, -to) <= 70f;
            if (target.Guarding && skill.canBeGuarded && (def == null || !def.guardRequiresFacing || facing))
            {
                if ((def == null || def.parryEnabled) && target.IsParryWindow && facing)   // 패링: 피해 없음, 공격자 경직
                {
                    target.ChangeSanity(CharacterStats.ParrySuccess, "패링"); attack.cancelled = true;
                    SparkBurst.Spawn(hitPoint, -to.normalized + Vector3.up, 50, 7f, SparkBurst.Gold); HitStop.Do(0.14f);
                    if (TryGetComponent<ICombatant>(out var me)) me.Stagger(1.0f, 1.5f); ClashManager.RaiseParry(target, stats);
                    var de = target.GetComponent<EnergyManager>(); if (de != null) de.Gain(de.onParryGain, "패링"); return;
                }
                r *= def != null ? def.GetDamageMultiplier() : target.guardDamageMultiplier; target.ChangeSanity(CharacterStats.GuardBlock, "방어");
            }
            int aff = SkillAttributes.Affinity(skill.attribute, SkillAttributes.DefenderAttribute(target)); attack.affinity = aff; r *= SkillAttributes.DamageMultiplier(aff);   // 속성 상성
            float m = skill.damageMultiplier * (executor ? executor.weaponMultiplier : 1f) * stats.DamageDealtMultiplier;
            int dmg = EquipmentMath.Damage(attack.power, stats.Attack, m, r);   // 공격력 × (최종 위력 ÷ 10) × 배율 × 받는 쪽 보정
            bool wasInvulnerable = target.Invulnerable;
            target.TakeDamage(dmg, hitPoint, to.normalized * skill.knockback + Vector3.up * 1.5f);
            if (wasInvulnerable) return;   // 회피됨
            attack.isHit = true; stats.ChangeSanity(CharacterStats.HitLanded, "명중");
            ApplyStatusAndEnergy(attack, target, hitPoint);
            SparkBurst.Spawn(hitPoint, -to.normalized + Vector3.up * 0.5f, attack.isHeads ? 40 : 22, attack.isHeads ? 7f : 4.5f, attack.isHeads ? SparkBurst.Gold : (stats.team == Team.Enemy ? SparkBurst.Crimson : (Color?)null));
            DamageDealt?.Invoke(attack, target, dmg);
        }
        public static event System.Action<AttackContext, CharacterStats, int> DamageDealt;
        /// <summary>회피 코인으로 피함: (공격, 피한 대상).</summary>
        public static event System.Action<AttackContext, CharacterStats> Evaded;
        /// <summary>진동 폭발: (공격, 대상, 경직 피해, 경직이 깨졌는가) — HUD 연출용.</summary>
        public static event System.Action<AttackContext, CharacterStats, int, bool> TremorBurst;

        /// <summary>
        /// 명중 후 처리 순서(한 공격·대상당 1회 — hitTargets 로 보장):
        /// ① 침잠 발동(이미 쌓인 것) ② 이번 코인 결과(앞/뒷면)로 새 상태이상 부여 ③ 진동 폭발(스킬 설정 시) ④ 공격자 에너지 획득.
        /// 부여보다 폭발이 뒤라서, 같은 공격으로 준 진동도 바로 터질 수 있음.
        /// </summary>
        void ApplyStatusAndEnergy(AttackContext attack, CharacterStats target, Vector3 hitPoint)
        {
            var skill = attack.skill; var se = target.GetComponent<StatusEffectManager>();
            if (se == null) se = target.gameObject.AddComponent<StatusEffectManager>();
            se.OnHitTaken(attack);
            if (target.IsAlive) se.ApplyFromSkill(skill, attack.isHeads);
            var mySe = executor != null ? executor.status : GetComponent<StatusEffectManager>(); if (mySe != null) mySe.ApplyFromSkill(skill, attack.isHeads, self: true);   // 힘(자신)
            var energy = executor != null ? executor.energy : GetComponent<EnergyManager>();
            int sd = 0; bool broke = skill.tremorBurstEnabled && target.IsAlive && se.TryTremorBurst(attack, out sd);
            if (sd > 0)
            {
                SparkBurst.Spawn(hitPoint, Vector3.up, broke ? 70 : 35, broke ? 9f : 6f, SparkBurst.Gold); HitStop.Do(broke ? 0.16f : 0.07f);
                if (broke && energy != null) energy.Gain(energy.onStaggerBreakGain, "경직 깨뜨림");
                TremorBurst?.Invoke(attack, target, sd, broke);
            }
            if (energy != null) energy.Gain(energy.onHitGain + skill.energyGain, "명중");
        }
    }
}
