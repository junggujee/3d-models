using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 스킬 입력·실행·쿨타임(설계서 6장). 좌클릭 = 일반 공격 3연타(BASIC_1~3), Q/E/R = 스킬 1·특수·필살기.
    /// 각 스킬의 hitTimes 마다 AttackController.ExecuteHit 를 호출(애니메이션 이벤트 대신 타이머, 클립 길이와 맞춤).
    /// 공격 시작 직후는 취소 불가, 마지막 타격 이후(회복 구간)는 회피·다음 공격으로 취소 가능.
    /// </summary>
    [RequireComponent(typeof(AttackController), typeof(CombatManager))]
    public class SkillManager : MonoBehaviour
    {
        public string[] basicChain = { "BASIC_1", "BASIC_2", "BASIC_3" };
        public string skill1 = "SKILL_001", skill2 = "SKILL_002", ultimate = "SKILL_003";
        public float clashWindow = 0.2f;
        public Animator animator; public SwordGlow glow;
        public SkillData Current { get; private set; } public float T { get; private set; }
        public event Action<SkillData> SkillStarted; public event Action<SkillData, bool> SkillEnded; public event Action<SkillData, string> SkillRefused;
        readonly Dictionary<string, float> readyAt = new();
        AttackController ac; CombatManager cm; SanitySystem sanity; PlayerController pc;
        int hitIdx, animIdx, basicIdx = -1; bool buffered; float lastHitT;

        void Awake() { ac = GetComponent<AttackController>(); cm = GetComponent<CombatManager>(); sanity = GetComponent<SanitySystem>(); pc = GetComponent<PlayerController>(); if (!animator) animator = GetComponentInChildren<Animator>(); }
        void Start() { if (!glow) glow = GetComponentInChildren<SwordGlow>(); }

        public SkillData SlotSkill(SkillSlot s) => s switch { SkillSlot.Skill1 => SkillLibrary.Get(skill1), SkillSlot.Skill2 => SkillLibrary.Get(skill2), SkillSlot.Ultimate => SkillLibrary.Get(ultimate), SkillSlot.Guard => SkillLibrary.Get("GUARD"), _ => SkillLibrary.Get(basicChain[0]) };
        public float CooldownLeft(SkillData s) => s != null && readyAt.TryGetValue(s.skillId, out var r) ? Mathf.Max(0, r - Time.time) : 0f;
        /// <summary>다음 타격이 0.2초 안에 나갈 예정 → 적 공격이 지금 닿으면 그 타격으로 합.</summary>
        public bool InHitWindow => Current != null && Current.clashEnabled && hitIdx < Current.hitTimes.Length && ac.CoinsLeft > 0 && Current.hitTimes[hitIdx] - T <= clashWindow;

        public bool TryClashNow(EnemyAI e)
        {
            if (!InHitWindow) return false;
            int i = hitIdx++; bool clashed = ac.ExecuteHit(i, e); if (Current != null) lastHitT = T; return clashed;
        }

        void Update()
        {
            if (UIState.Paused) return;
            if (cm.State == CombatState.Stagger || cm.State == CombatState.Dead || cm.State == CombatState.Dodge) { if (Current != null) Cancel(); return; }
            bool canStart = Current == null || cm.State == CombatState.Recovery;
            if (InputAdapter.AttackPressed())
            {
                if (Current == null || (Current.slot != SkillSlot.Basic && cm.State == CombatState.Recovery)) Begin(SkillLibrary.Get(basicChain[0]), 0);
                else if (Current.slot == SkillSlot.Basic) buffered = true;
            }
            int k = InputAdapter.SkillPressed();
            if (k >= 0 && canStart) Begin(SlotSkill(k == 0 ? SkillSlot.Skill1 : k == 1 ? SkillSlot.Skill2 : SkillSlot.Ultimate), -1);
            if (Current == null) return;

            T += Time.deltaTime; var s = Current;
            while (animIdx < s.animStates.Length && T >= s.animTimes[animIdx]) { if (animator) animator.CrossFadeInFixedTime(s.animStates[animIdx], 0.06f); animIdx++; }
            while (hitIdx < s.hitTimes.Length && T >= s.hitTimes[hitIdx]) { int i = hitIdx++; ac.ExecuteHit(i); if (Current == null) return; lastHitT = T; }
            bool hitsDone = hitIdx >= s.hitTimes.Length || ac.CoinsLeft <= 0;
            if (hitsDone && cm.State == CombatState.Attack) cm.SetState(CombatState.Recovery);
            if (s.slot == SkillSlot.Basic && buffered && hitsDone && T >= lastHitT + 0.1f && basicIdx >= 0 && basicIdx < basicChain.Length - 1) { Begin(SkillLibrary.Get(basicChain[basicIdx + 1]), basicIdx + 1); return; }
            if (T >= s.duration) Finish(true);
        }

        bool Begin(SkillData s, int basicIndex)
        {
            if (s == null) return false;
            float cd = CooldownLeft(s); if (cd > 0) { SkillRefused?.Invoke(s, $"재사용 대기 {cd:0.0}초"); return false; }
            if (Current != null) End(false);
            if (cm.State == CombatState.Guard) cm.SetState(CombatState.Idle);
            if (s.sanityCost > 0) sanity.Add(-s.sanityCost, s.skillName);
            Current = s; T = 0; hitIdx = 0; animIdx = 0; basicIdx = basicIndex; buffered = false; lastHitT = 0;
            ac.BeginSkill(s); if (s.cooldown > 0) readyAt[s.skillId] = Time.time + s.cooldown;
            pc?.FaceForAttack(); cm.SetState(CombatState.Attack); if (glow) glow.Attacking = true; SkillStarted?.Invoke(s); return true;
        }

        void End(bool completed)
        {
            var s = Current; if (s == null) return; Current = null; ac.EndSkill(); if (glow) glow.Attacking = false;
            if (cm.State == CombatState.Attack || cm.State == CombatState.Recovery) { cm.SetState(CombatState.Idle); if (animator) animator.CrossFadeInFixedTime("Locomotion", 0.15f); }
            SkillEnded?.Invoke(s, completed);
        }
        void Finish(bool completed) => End(completed);
        /// <summary>피격·합 패배·회피 등으로 스킬 중단.</summary>
        public void Cancel() => End(false);
    }
}
