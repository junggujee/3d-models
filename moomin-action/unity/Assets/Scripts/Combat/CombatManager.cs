using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 플레이어 전투 흐름 관리(설계서 1·2-2장): 상태 머신(Idle·Move·Attack·Recovery·Dodge·Guard·Clash·Stagger·Dead), 방어(우클릭)·패링,
    /// 적 공격이 닿았을 때의 처리 순서: 회피 무적 → 내 타격과 합 → 방어 합 → 일방 공격(적의 남은 코인이 하나씩 타격).
    /// </summary>
    [RequireComponent(typeof(Health), typeof(SanitySystem))]
    public class CombatManager : MonoBehaviour
    {
        public float parryWindow = 0.2f, guardMoveMultiplier = 0.35f, strongHitDamage = 14f;
        public CombatState State { get; private set; }
        public float GuardStartTime { get; private set; }
        public event Action<CombatState, CombatState> StateChanged; public event Action Evaded;
        Animator animator; Health health; SanitySystem sanity; SkillManager skills; PlayerController pc; float stateTimer = -1f;

        void Awake()
        {
            health = GetComponent<Health>(); sanity = GetComponent<SanitySystem>(); skills = GetComponent<SkillManager>(); pc = GetComponent<PlayerController>(); animator = GetComponentInChildren<Animator>();
            health.Died += () => SetState(CombatState.Dead);
        }

        public bool CanDodge => State == CombatState.Idle || State == CombatState.Move || State == CombatState.Recovery || State == CombatState.Guard;
        public bool CanGuard => State == CombatState.Idle || State == CombatState.Move || State == CombatState.Recovery;
        public bool IsParryWindow => State == CombatState.Guard && Time.time - GuardStartTime <= parryWindow;
        public float MoveMultiplier => State switch { CombatState.Attack => 0.15f, CombatState.Recovery => 0.5f, CombatState.Guard => guardMoveMultiplier, CombatState.Stagger => 0f, CombatState.Clash => 0f, CombatState.Dead => 0f, _ => 1f };

        public void SetState(CombatState s, float duration = -1f)
        {
            if (State == CombatState.Dead) return; var old = State; State = s; stateTimer = duration;
            if (old != s) StateChanged?.Invoke(old, s);
        }
        public void SetLocomotion(bool moving) { if (State == CombatState.Idle || State == CombatState.Move) SetState(moving ? CombatState.Move : CombatState.Idle); }
        public void Stagger(float seconds, bool playAnim = true) { if (State == CombatState.Dead) return; skills?.Cancel(); pc?.CancelRoll(); SetState(CombatState.Stagger, seconds); if (playAnim && animator) animator.CrossFadeInFixedTime("Hit", 0.04f); }
        public void Recover(float seconds) { skills?.Cancel(); SetState(CombatState.Recovery, seconds); }

        void Update()
        {
            if (UIState.Paused || State == CombatState.Dead) return;
            if (stateTimer > 0f) { stateTimer -= Time.deltaTime; if (stateTimer <= 0f) { stateTimer = -1f; if (State == CombatState.Stagger || State == CombatState.Recovery || State == CombatState.Clash) { SetState(CombatState.Idle); if (animator) animator.CrossFadeInFixedTime("Locomotion", 0.15f); } } }
            bool held = InputAdapter.GuardHeld();
            if (held && State != CombatState.Guard && CanGuard && (skills == null || skills.Current == null)) { GuardStartTime = Time.time; SetState(CombatState.Guard); if (animator) animator.CrossFadeInFixedTime("Guard", 0.06f); }
            else if (!held && State == CombatState.Guard) { SetState(CombatState.Idle); if (animator) animator.CrossFadeInFixedTime("Locomotion", 0.12f); }
        }

        /// <summary>적의 공격이 닿는 순간 EnemyAI 가 호출.</summary>
        public void ReceiveEnemyAttack(EnemyAI e)
        {
            if (State == CombatState.Dead || health.IsDead) return;
            if (health.Invulnerable) { sanity.Add(SanitySystem.DodgeSuccess, "회피"); Evaded?.Invoke(); e.OnAttackEvaded(); return; }
            if (skills != null && skills.InHitWindow && skills.TryClashNow(e)) return;
            Vector3 to = e.transform.position - transform.position; to.y = 0;
            var guard = SkillLibrary.Get("GUARD");
            if (State == CombatState.Guard && !e.CurrentAttack.unblockable && Vector3.Angle(transform.forward, to) <= guard.angle * 0.5f) { ClashManager.ResolveGuard(this, e, IsParryWindow); return; }
            e.HitPlayerOneSided(this, e.CurrentAttack.coinCount, 1f);
        }

        /// <summary>적 코인 하나의 타격. 연타 도중 구르기로 무적이 되면 남은 타격은 빗나감.</summary>
        public bool TakeEnemyHit(EnemyAI e, int power, float multiplier, float knockback)
        {
            if (health.IsDead) return false;
            if (health.Invulnerable) { sanity.Add(SanitySystem.DodgeSuccess, "회피"); Evaded?.Invoke(); return false; }
            int dmg = DamageCalculator.Final(power, multiplier * sanity.DamageTakenMultiplier);
            Vector3 dir = transform.position - e.transform.position; dir.y = 0;
            health.TakeDamage(dmg, transform.position + Vector3.up, dir.normalized * knockback + Vector3.up * 1.5f);
            if (dmg >= strongHitDamage) sanity.Add(SanitySystem.StrongHitTaken, "강한 공격 피격");
            return true;
        }
    }
}
