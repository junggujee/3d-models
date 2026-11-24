using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    public enum SkillPhase { None, Windup, Attacking, Recovery }

    /// <summary>
    /// 스킬 실행과 공격 순서(명세 v2 5장). <b>코인 하나 = 공격 한 번</b>: 3코인 스킬은 공격 애니메이션 3번, 각 타격 순간에 코인 판정.
    /// 언제 공격할지만 정하고, 결과는 CoinSystem, 충돌·피해는 CombatResolver 가 맡음. 플레이어·적이 같은 컴포넌트를 씀.
    ///
    /// 애니메이션 이벤트(플레이어 클립에 MoominSetup 이 자동 배치): OnCoinAttack · OpenHitbox · CloseHitbox · OnSkillFinished.
    /// 이벤트가 오지 않으면(적처럼 애니메이터가 없거나 설정 누락) 스킬 데이터의 hitTime 으로 대신 진행.
    /// </summary>
    [RequireComponent(typeof(CharacterStats), typeof(CoinSystem), typeof(CombatResolver))]
    public class SkillExecutor : MonoBehaviour
    {
        public CoinSystem coinSystem; public CharacterStats stats; public EnergyManager energy; public StatusEffectManager status; public Animator animator; public CombatResolver resolver;
        public bool useAnimationEvents = false; public float eventGrace = 0.15f; public float weaponMultiplier = 1f;

        public SkillDefinition CurrentSkill { get; private set; }
        /// <summary>마지막으로 쓴 스킬(끝난 뒤에도 남음 — 스킬 패널이 결과를 잠시 보여 줄 때 사용).</summary>
        public SkillDefinition LastSkill { get; private set; } public float LastFinishTime { get; private set; } = -99f; public bool LastCompleted { get; private set; }
        public AttackContext CurrentAttack { get; private set; }
        public SkillPhase Phase { get; private set; }
        public bool IsExecuting => isExecuting;
        /// <summary>합 연출 중 스킬 진행 정지(시간·히트박스·다음 코인 모두 멈춤).</summary>
        public bool Held;
        public bool CanCancel => !isExecuting || Phase == SkillPhase.Recovery;   // 회복 구간은 회피·다음 공격으로 취소 가능
        public int CurrentCoin => currentCoin;
        public float PhaseTime => t - stageStart;
        /// <summary>첫 타격까지 남은 시간(예고 중이면 남은 예고 + hitTime, 아니면 0) — 펫이 맞받아칠 때 맞춤.</summary>
        public float TimeToFirstHit => isExecuting && Phase == SkillPhase.Windup && CurrentSkill != null ? Mathf.Max(0, CurrentSkill.windup - t) + CurrentSkill.hitTime : 0f;
        public readonly List<AttackContext> results = new();   // 이번 스킬의 공격별 결과(HUD)

        public event Action<SkillDefinition> SkillStarted; public event Action<AttackContext> CoinAttacked; public event Action<SkillDefinition, bool> SkillFinished; public event Action<SkillDefinition, string> SkillRefused;

        readonly Dictionary<SkillDefinition, float> readyAt = new();
        bool isExecuting, coinDoneThisStage, hitboxHeld; int currentCoin; float t, stageStart, recoveryEnd;

        void Awake()
        {
            if (!coinSystem) coinSystem = GetComponent<CoinSystem>(); if (!stats) stats = GetComponent<CharacterStats>(); if (!resolver) resolver = GetComponent<CombatResolver>();
            if (!energy) energy = GetComponent<EnergyManager>(); if (!status) status = GetComponent<StatusEffectManager>();
            if (!animator && useAnimationEvents) animator = GetComponentInChildren<Animator>();   // 적(애니메이터 없음)은 타이머로만 진행
        }

        public float CooldownLeft(SkillDefinition s) => s != null && readyAt.TryGetValue(s, out var r) ? Mathf.Max(0, r - Time.time) : 0f;
        public bool IsReady(SkillDefinition s) => s != null && CooldownLeft(s) <= 0f && stats.currentSanity >= s.sanityCost && HasEnergyFor(s);
        /// <summary>에너지가 충분한가(EnergyManager 가 없으면 비용 0 인 스킬만).</summary>
        public bool HasEnergyFor(SkillDefinition s) => s == null || s.energyCost <= 0 || (energy != null && energy.CanSpend(s.energyCost));

        /// <summary>스킬 사용. 실행 중이면 회복 구간에서만 끊고 새로 시작(중복 실행 방지).</summary>
        public bool UseSkill(SkillDefinition skill)
        {
            if (skill == null || !stats.IsAlive) return false;
            if (isExecuting && Phase != SkillPhase.Recovery) return false;
            float cd = CooldownLeft(skill); if (cd > 0) { SkillRefused?.Invoke(skill, $"재사용 대기 {cd:0.0}초"); return false; }
            if (stats.currentSanity < skill.sanityCost) { SkillRefused?.Invoke(skill, $"정신력 {skill.sanityCost} 필요"); return false; }
            if (!HasEnergyFor(skill)) { SkillRefused?.Invoke(skill, $"에너지 {skill.energyCost} 필요"); return false; }
            if (isExecuting) Finish(false);
            if (skill.energyCost > 0) energy.Spend(skill.energyCost, skill.skillName);
            stats.ChangeSanity(-skill.sanityCost, skill.skillName);
            CurrentSkill = LastSkill = skill; currentCoin = 0; t = 0; results.Clear(); CurrentAttack = null; isExecuting = true; hitboxHeld = false;
            if (skill.cooldown > 0) readyAt[skill] = Time.time + skill.cooldown;
            Phase = skill.windup > 0 ? SkillPhase.Windup : SkillPhase.Attacking; if (Phase == SkillPhase.Attacking) BeginStage();
            SkillStarted?.Invoke(skill); return true;
        }

        void BeginStage()
        {
            stageStart = t; coinDoneThisStage = false;
            var anims = CurrentSkill.attackAnimations;
            if (animator && anims != null && anims.Length > 0) { string st = anims[currentCoin % anims.Length]; if (animator.HasState(0, Animator.StringToHash(st))) animator.CrossFadeInFixedTime(st, 0.06f); }
        }

        void Update()
        {
            if (!isExecuting || UIState.Paused || Held) return;
            t += Time.deltaTime; var s = CurrentSkill;
            if (Phase == SkillPhase.Windup) { if (t >= s.windup) { Phase = SkillPhase.Attacking; BeginStage(); } return; }
            if (Phase == SkillPhase.Attacking)
            {
                float local = t - stageStart;
                if (!coinDoneThisStage && local >= s.hitTime + (useAnimationEvents ? eventGrace : 0f)) OnCoinAttack();     // 이벤트 대체(타이머)
                if (!isExecuting) return;                                                                                  // 클래시 패배로 중단됐을 수 있음
                if (CurrentAttack != null && CurrentAttack.HitboxOpen) resolver.Tick(CurrentAttack, s);                     // 히트박스가 열린 동안 계속 판정
                if (!isExecuting || Held) return;                                                                          // 합이 시작됐으면 여기서 멈춤
                if (coinDoneThisStage && local >= Mathf.Max(s.attackInterval, s.hitTime + s.activeWindow))
                {
                    if (currentCoin < s.coinCount) BeginStage();
                    else { Phase = SkillPhase.Recovery; recoveryEnd = t + s.recovery; }
                }
                return;
            }
            if (Phase == SkillPhase.Recovery && t >= recoveryEnd) Finish(true);
        }

        // ── 애니메이션 이벤트 ─────────────────────────────
        /// <summary>타격 프레임: 코인 판정 → 위력 계산 → AttackContext 생성 → CombatResolver 로 충돌 검사.</summary>
        public void OnCoinAttack()
        {
            if (Held || !isExecuting || Phase != SkillPhase.Attacking || coinDoneThisStage || currentCoin >= CurrentSkill.coinCount) return;
            if (t - stageStart < CurrentSkill.hitTime - eventGrace) return;   // 이전 클립의 늦은 이벤트 무시
            var s = CurrentSkill; var r = coinSystem.Toss(stats, currentCoin);
            var ctx = new AttackContext(0, r.heads) { skill = s, owner = this, coinIndex = currentCoin, activeUntil = Time.time + s.activeWindow };
            // 코인 위력 보정(힘·위력 감소·마비)을 이 코인에 한 번 적용 → 최종 위력 = 기본 + (앞면 ? 보정된 코인 위력 : 0) + 장비 보너스(예: 파란 머리띠 +1)
            ctx.modifiers = status != null ? status.UseCoinModifiers(ctx.id) : default;
            var opp = LevelOpponent(); ctx.levelBonus = opp != null ? LevelMath.CoinBonus(stats.level, opp.level) : 0;   // 레벨 차이: 3레벨마다 코인 위력 ±1(최대 ±3)
            ctx.power = EquipmentMath.FinalPower(s.basePower, ctx.modifiers.CoinValue(LevelMath.CoinPower(s.coinPower, ctx.levelBonus)), r.heads, stats.equipFinalPower);
            CurrentAttack = ctx; results.Add(ctx); currentCoin++; coinDoneThisStage = true; if (hitboxHeld) ctx.activeUntil = float.MaxValue;
            CoinAttacked?.Invoke(ctx);
            if (status != null) status.OnCoinUsed(ctx);   // 출혈: 공격 코인을 쓸 때(같은 ctx.id 로는 한 번만)
            if (!stats.IsAlive) { Interrupt(); return; }   // 출혈로 쓰러짐
            resolver.ResolveAttack(ctx, s);
            if (isExecuting && currentCoin >= s.coinCount) ApplyAllHeadsEffect(s);
        }
        /// <summary>레벨 비교 상대: 적은 쫓는 대상, 플레이어는 조준 대상, 없으면 12m 안 가장 가까운 다른 팀.</summary>
        public CharacterStats LevelOpponent()
        {
            if (TryGetComponent<EnemyCombatController>(out var ec) && ec.Target != null && ec.Target.TryGetComponent<CharacterStats>(out var ts)) return ts;
            if (TryGetComponent<LockOnSystem>(out var lo) && lo.Target != null && lo.Target.TryGetComponent<CharacterStats>(out var ls)) return ls;
            CharacterStats best = null; float bd = 144f; int mask = resolver != null && resolver.targetMask != 0 ? (int)resolver.targetMask : ~0;
            foreach (var c in Physics.OverlapSphere(transform.position, 12f, mask))
            {
                var cs = c.GetComponentInParent<CharacterStats>(); if (cs == null || cs == stats || !cs.IsAlive || cs.team == stats.team) continue;
                float d = (cs.transform.position - transform.position).sqrMagnitude; if (d < bd) { bd = d; best = cs; }
            }
            return best;
        }
        public void OpenHitbox() { hitboxHeld = true; if (CurrentAttack != null && !CurrentAttack.cancelled) CurrentAttack.activeUntil = Time.time + CurrentSkill.activeWindow * 3f; }
        public void CloseHitbox() { hitboxHeld = false; if (CurrentAttack != null) CurrentAttack.activeUntil = Mathf.Min(CurrentAttack.activeUntil, Time.time); }
        /// <summary>마지막 공격 뒤 이벤트. 코인이 남아 있으면 무시(각 공격 클립 끝에 있어도 안전).</summary>
        public void OnSkillFinished() { if (!Held && isExecuting && currentCoin >= CurrentSkill.coinCount && Phase != SkillPhase.Windup) Finish(true); }

        void ApplyAllHeadsEffect(SkillDefinition s)
        {
            if (s.coinEffect == CoinEffect.None || !stats.ExtraEffectsEnabled) return;
            foreach (var r in results) if (!r.isHeads) return;
            if (s.coinEffect == CoinEffect.SanityOnAllHeads) stats.ChangeSanity(10, s.skillName + " 전부 앞면");
            else if (s.coinEffect == CoinEffect.StaggerOnAllHeads) foreach (var target in CurrentAttack.hitTargets) { if (target.TryGetComponent<ICombatant>(out var tc)) tc.Stagger(1.6f, 1.3f); }
        }

        /// <summary>클래시 패배·피격 경직·회피 등으로 중단. 남은 코인은 소모하지 않음.</summary>
        public void Interrupt() { if (isExecuting) { if (CurrentAttack != null) CurrentAttack.cancelled = true; Finish(false); } }

        void Finish(bool completed)
        {
            var s = CurrentSkill; isExecuting = false; Phase = SkillPhase.None; CurrentSkill = null; hitboxHeld = false; LastFinishTime = Time.time; LastCompleted = completed;
            if (CurrentAttack != null) CurrentAttack.activeUntil = Mathf.Min(CurrentAttack.activeUntil, Time.time);
            if (animator && animator.HasState(0, Animator.StringToHash("Locomotion"))) animator.CrossFadeInFixedTime("Locomotion", 0.15f);
            SkillFinished?.Invoke(s, completed);
        }
    }
}
