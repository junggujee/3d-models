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
        CharacterStats stats; DefenseController defense; SkillExecutor executor; PlayerController pc; Animator animator; float staggerUntil; bool moving, buffered; int basicIndex = -1;
        SkillDefinition[] basics;
        /// <summary>지금 무기 묶음(스파크 검 · 베개 방패 · 회전 쌍검). 바뀌면 KitChanged(HUD 스킬 칸·시트가 다시 그림).</summary>
        public WeaponKit Kit { get; private set; }
        public event System.Action<WeaponKit> KitChanged;
        /// <summary>스킬을 못 쓴 이유(예: 결투 고조 부족) — HUD 알림.</summary>
        public event System.Action<string> Refused;
        SkillDefinition sparkB1, sparkB2, sparkB3, sparkS1, sparkS2, sparkU; string guardAnim = "Guard";

        void Awake()
        {
            stats = GetComponent<CharacterStats>(); defense = GetComponent<DefenseController>(); executor = GetComponent<SkillExecutor>(); pc = GetComponent<PlayerController>(); animator = GetComponentInChildren<Animator>();
            // 비어 있으면 기본값(Unity 의 '가짜 null' 때문에 ??= 대신 == null 로 검사)
            if (basic1 == null) basic1 = SkillLibrary.Get("BASIC_1"); if (basic2 == null) basic2 = SkillLibrary.Get("BASIC_2"); if (basic3 == null) basic3 = SkillLibrary.Get("BASIC_3");
            if (skill1 == null) skill1 = SkillLibrary.Get("SKILL_001"); if (skill2 == null) skill2 = SkillLibrary.Get("SKILL_002"); if (ultimate == null) ultimate = SkillLibrary.Get("SKILL_003");
            basics = new[] { basic1, basic2, basic3 };
            sparkB1 = basic1; sparkB2 = basic2; sparkB3 = basic3; sparkS1 = skill1; sparkS2 = skill2; sparkU = ultimate;   // 스파크 검 = 인스펙터/에셋 값
            Kit = WeaponKits.Get(WeaponKits.Spark);
            stats.Damaged += (d, p, k) => { if (stats.IsAlive) Stagger(hitStagger); };
            stats.Died += () => { executor.Interrupt(); stats.Guarding = false; if (animator) animator.CrossFadeInFixedTime("Death", 0.05f); };
            executor.SkillFinished += (s, done) => { if (!buffered) basicIndex = -1; };   // 연타가 이어지면 StartBasic 이 다시 설정
        }

        public SkillDefinition SlotSkill(int slot) => slot switch { 0 => basic1, 1 => skill1, 2 => skill2, _ => ultimate };   // HUD: 0 좌클릭, 1 Q, 2 E, 3 R

        /// <summary>무기 묶음 적용: 스킬 6개 · 방어 코인 · 방어 자세 · 고유 효과(보호막/결투 고조 + 날 회전) · 검 모델 보이기.</summary>
        public void ApplyKit(WeaponKit k)
        {
            if (k == null) return; if (Kit == k && basics != null) return; Kit = k;
            if (executor != null && executor.IsExecuting) executor.Interrupt();
            if (k.id == WeaponKits.Spark) { basic1 = sparkB1; basic2 = sparkB2; basic3 = sparkB3; skill1 = sparkS1; skill2 = sparkS2; ultimate = sparkU; }
            else { basic1 = SkillLibrary.Get(k.basics[0]); basic2 = SkillLibrary.Get(k.basics[1]); basic3 = SkillLibrary.Get(k.basics[2]); skill1 = SkillLibrary.Get(k.skill1); skill2 = SkillLibrary.Get(k.skill2); ultimate = SkillLibrary.Get(k.ultimate); }
            basics = new[] { basic1, basic2, basic3 }; basicIndex = -1; buffered = false;
            if (defense != null) { defense.guardBasePower = k.guardBase; defense.guardCoinPower = k.guardCoin; defense.guardReduction = k.guardReduction; defense.perfectReduction = k.perfectReduction; }
            guardAnim = string.IsNullOrEmpty(k.guardAnim) ? "Guard" : k.guardAnim;
            if (animator != null && !animator.HasState(0, Animator.StringToHash(guardAnim))) guardAnim = "Guard";
            SetPassive<ShieldBarrier>(k.passive == WeaponPassive.ShieldBarrier); SetPassive<DuelHeat>(k.passive == WeaponPassive.DuelHeat); SetPassive<WeaponSpin>(k.passive == WeaponPassive.DuelHeat);
            foreach (var t in GetComponentsInChildren<Transform>(true)) if (t.name == "StarSparkSword") t.gameObject.SetActive(k.showSword);
            KitChanged?.Invoke(k);
        }
        void SetPassive<T>(bool on) where T : Behaviour
        {
            if (TryGetComponent<T>(out var c)) c.enabled = on; else if (on) gameObject.AddComponent<T>();
        }

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
            if (guardHeld && !stats.Guarding && executor.CanCancel) { if (executor.IsExecuting) executor.Interrupt(); if (defense != null) defense.StartGuard(); else { stats.Guarding = true; stats.GuardStartTime = Time.time; } if (animator) animator.CrossFadeInFixedTime(guardAnim, 0.06f); }
            else if (!guardHeld && stats.Guarding) { if (defense != null) defense.StopGuard(); else stats.Guarding = false; if (animator) animator.CrossFadeInFixedTime("Locomotion", 0.12f); }

            // 일반 공격 3연타: 회복 구간에 다음 타로 이어짐
            if (InputAdapter.AttackPressed())
            {
                if (!executor.IsExecuting) StartBasic(0);
                else if (basicIndex >= 0) buffered = true;
            }
            if (buffered && executor.IsExecuting && executor.Phase == SkillPhase.Recovery && basicIndex >= 0 && basicIndex < basics.Length - 1) { buffered = false; StartBasic(basicIndex + 1); }

            // Q/E/R
            int k = InputAdapter.SkillPressed();
            if (k >= 0 && executor.CanCancel)
            {
                var s = k == 0 ? skill1 : k == 1 ? skill2 : ultimate;
                if (s != null && s.requiresDuelHeat)   // 회절원참격: 결투 고조가 가득 차야 함, 쓰면 0
                {
                    if (!TryGetComponent<DuelHeat>(out var heat) || !heat.enabled || !heat.Full) { Refused?.Invoke($"결투 고조가 가득 차야 한다 ({(heat != null ? heat.Stacks : 0)}/{DuelHeat.MaxStacks})"); return; }
                    stats.Guarding = false; pc?.FaceForAttack(); if (executor.UseSkill(s)) { basicIndex = -1; heat.Consume(); }
                    return;
                }
                stats.Guarding = false; pc?.FaceForAttack(); if (executor.UseSkill(s)) basicIndex = -1;
            }
        }

        void StartBasic(int i) { stats.Guarding = false; pc?.FaceForAttack(); if (executor.UseSkill(basics[i])) basicIndex = i; else basicIndex = -1; }
    }
}
