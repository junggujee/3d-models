using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 플레이어의 전투 입력 관리(명세 v2 2-1): 좌클릭 일반 공격(3연타), Q/E/R 스킬, 우클릭 방어(누른 직후 0.2초 패링) → SkillExecutor.
    /// 행동 상태(Idle·Move·Attack·Recovery·Dodge·Guard·Stagger·Dead)를 정리해 이동·회피 가능 여부를 알려 주고, 피격 시 경직시킨다.
    /// </summary>
    [RequireComponent(typeof(CharacterStats), typeof(SkillExecutor))]
    public class CombatManager : MonoBehaviour, ICombatant
    {
        [Header("스킬(비워 두면 기본값)")] public SkillDefinition basic1, basic2, basic3, skill1, skill2, ultimate;
        public float hitStagger = 0.35f;
        public CharacterStats Stats => stats; public SkillExecutor Executor => executor;
        public CombatState State { get; private set; }
        CharacterStats stats; SkillExecutor executor; PlayerController pc; Animator animator; float staggerUntil; bool moving, buffered; int basicIndex = -1;
        SkillDefinition[] basics;

        void Awake()
        {
            stats = GetComponent<CharacterStats>(); executor = GetComponent<SkillExecutor>(); pc = GetComponent<PlayerController>(); animator = GetComponentInChildren<Animator>();
            // 비어 있으면 기본값(Unity 의 '가짜 null' 때문에 ??= 대신 == null 로 검사)
            if (basic1 == null) basic1 = SkillLibrary.Get("BASIC_1"); if (basic2 == null) basic2 = SkillLibrary.Get("BASIC_2"); if (basic3 == null) basic3 = SkillLibrary.Get("BASIC_3");
            if (skill1 == null) skill1 = SkillLibrary.Get("SKILL_001"); if (skill2 == null) skill2 = SkillLibrary.Get("SKILL_002"); if (ultimate == null) ultimate = SkillLibrary.Get("SKILL_003");
            basics = new[] { basic1, basic2, basic3 };
            stats.Damaged += (d, p, k) => { if (stats.IsAlive) Stagger(hitStagger); };
            stats.Died += () => { executor.Interrupt(); stats.Guarding = false; if (animator) animator.CrossFadeInFixedTime("Death", 0.05f); };
            executor.SkillFinished += (s, done) => { if (!buffered) basicIndex = -1; };   // 연타가 이어지면 StartBasic 이 다시 설정
        }

        public SkillDefinition SlotSkill(int slot) => slot switch { 0 => basic1, 1 => skill1, 2 => skill2, _ => ultimate };   // HUD: 0 좌클릭, 1 Q, 2 E, 3 R

        public bool IsStaggered => Time.time < staggerUntil;
        public bool CanDodge => stats.IsAlive && !IsStaggered && executor.CanCancel;
        public float MoveMultiplier => !stats.IsAlive || IsStaggered ? 0f : executor.IsExecuting ? (executor.Phase == SkillPhase.Recovery ? 0.5f : 0.15f) : stats.Guarding ? 0.35f : 1f;
        public void SetLocomotion(bool isMoving) => moving = isMoving;

        public void Stagger(float seconds, float vulnerable = 1f)
        {
            if (!stats.IsAlive) return; executor.Interrupt(); pc?.CancelRoll(); stats.Guarding = false; buffered = false; basicIndex = -1;
            staggerUntil = Mathf.Max(staggerUntil, Time.time + seconds); if (vulnerable > 1f) stats.SetVulnerable(vulnerable, seconds);
            if (animator) animator.CrossFadeInFixedTime("Hit", 0.04f);
        }

        void Update()
        {
            State = !stats.IsAlive ? CombatState.Dead : IsStaggered ? CombatState.Stagger : pc != null && pc.Rolling ? CombatState.Dodge
                  : executor.IsExecuting ? (executor.Phase == SkillPhase.Recovery ? CombatState.Recovery : CombatState.Attack) : stats.Guarding ? CombatState.Guard : moving ? CombatState.Move : CombatState.Idle;
            if (UIState.Paused || !stats.IsAlive || stats.InClash || IsStaggered || (pc != null && pc.Rolling)) { if (stats.Guarding && !IsStaggered) stats.Guarding = false; return; }

            // 방어(우클릭 유지): 공격 중이 아닐 때
            bool guardHeld = InputAdapter.GuardHeld();
            if (guardHeld && !stats.Guarding && executor.CanCancel) { if (executor.IsExecuting) executor.Interrupt(); stats.Guarding = true; stats.GuardStartTime = Time.time; if (animator) animator.CrossFadeInFixedTime("Guard", 0.06f); }
            else if (!guardHeld && stats.Guarding) { stats.Guarding = false; if (animator) animator.CrossFadeInFixedTime("Locomotion", 0.12f); }

            // 일반 공격 3연타: 회복 구간에 다음 타로 이어짐
            if (InputAdapter.AttackPressed())
            {
                if (!executor.IsExecuting) StartBasic(0);
                else if (basicIndex >= 0) buffered = true;
            }
            if (buffered && executor.IsExecuting && executor.Phase == SkillPhase.Recovery && basicIndex >= 0 && basicIndex < basics.Length - 1) { buffered = false; StartBasic(basicIndex + 1); }

            // Q/E/R
            int k = InputAdapter.SkillPressed();
            if (k >= 0 && executor.CanCancel) { var s = k == 0 ? skill1 : k == 1 ? skill2 : ultimate; stats.Guarding = false; pc?.FaceForAttack(); if (executor.UseSkill(s)) basicIndex = -1; }
        }

        void StartBasic(int i) { stats.Guarding = false; pc?.FaceForAttack(); if (executor.UseSkill(basics[i])) basicIndex = i; else basicIndex = -1; }
    }
}
