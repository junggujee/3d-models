using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    public enum EnemyAction { Approach, Attack, Guard, Dodge, Wait }

    /// <summary>AI 가 매 판단마다 보는 상황(명세 v2 8-1).</summary>
    public struct EnemySituation
    {
        public float distance, healthRatio, playerHealthRatio; public bool playerAttacking, playerGuarding, attackAvailable; public int sanity;
        public float playerAttackPower;   // 플레이어가 지금 쓰는 스킬의 기대 위력(공격 중일 때)
        public SkillAttribute playerAttribute;   // 플레이어의 방어 속성(공격 중이면 그 스킬 속성)
    }

    /// <summary>적의 성격(가중치). 같은 판단 규칙에 숫자만 바꿔 다른 적을 만든다.</summary>
    [System.Serializable]
    public struct EnemyPersonality
    {
        [Range(0, 2)] public float aggression, caution, evasion; [Range(0, 30)] public float randomness;
        public static EnemyPersonality Grunt => new EnemyPersonality { aggression = 1f, caution = 0.6f, evasion = 0.5f, randomness = 12f };
        public static EnemyPersonality Boss => new EnemyPersonality { aggression = 1.2f, caution = 1f, evasion = 0.8f, randomness = 8f };
        /// <summary>인간형: 덜 들이대고 더 막고 피함.</summary>
        public static EnemyPersonality Humanoid => new EnemyPersonality { aggression = 0.9f, caution = 1.25f, evasion = 1.2f, randomness = 10f };
    }

    /// <summary>
    /// 상황별 행동 점수 계산(명세 v2 8-3). 상황 분석 → 행동마다 점수 → 가장 높은 행동(약간의 무작위성).
    ///   멀리 있음          → 접근
    ///   공격 가능한 거리    → 공격 80 · 방어 20 · 회피 10
    ///   플레이어 공격 중    → 공격 20 · 방어 60 · 회피 90  (내 스킬 기대 위력이 더 크면 공격 점수↑ = 클래시를 노림)
    ///   적 체력 20% 이하   → 공격 20 · 방어 90 · 회피 70
    /// 공격을 고르면 함께 쓸 스킬(사거리 안·쿨타임 끝·정신력 충분한 것 중 기대 위력이 가장 큰 것)도 돌려준다.
    /// </summary>
    public class EnemyDecisionSystem
    {
        public readonly Dictionary<EnemyAction, float> lastScores = new();

        public EnemyAction Decide(EnemySituation s, EnemyPersonality w, IList<SkillDefinition> skills, SkillExecutor ex, out SkillDefinition chosen)
        {
            chosen = null; float bestExp = float.MinValue, maxRange = 0f;
            foreach (var sk in skills)
            {
                if (sk == null) continue; maxRange = Mathf.Max(maxRange, sk.Reach);
                if (!ex.IsReady(sk) || s.distance > sk.Reach * 0.95f) continue;
                float exp = sk.ExpectedPower(s.sanity) * sk.coinCount * (s.playerGuarding && !sk.canBeGuarded ? 2f : 1f)   // 막고 있으면 방어 불가 기술 선호
                          * SkillAttributes.DamageMultiplier(SkillAttributes.Affinity(sk.attribute, s.playerAttribute));            // 상성 유리한 기술 선호
                if (exp > bestExp) { bestExp = exp; chosen = sk; }
            }
            bool canAttack = chosen != null && s.attackAvailable;
            float approach = 0, attack = 0, guard = 0, dodge = 0, wait = 5;
            if (s.distance > maxRange * 0.9f || (!canAttack && s.distance > 2.5f)) approach = 100;
            else
            {
                attack = canAttack ? 80 : 0; guard = 20; dodge = 10;
                if (s.playerAttacking)
                {
                    attack = canAttack ? 20 : 0; guard = 60; dodge = s.distance < 2.5f ? 90 : 30;
                    int aff = canAttack ? SkillAttributes.Affinity(chosen.attribute, s.playerAttribute) : 0;
                    if (canAttack && chosen.ExpectedPower(s.sanity) + (aff > 0 ? SkillAttributes.AdvantageClashBonus : 0) >= s.playerAttackPower + (aff < 0 ? SkillAttributes.AdvantageClashBonus : 0) + 2f) attack += 50;   // 이길 만하면(상성 포함) 맞받아쳐 클래시
                }
                if (s.healthRatio < 0.2f) { attack = Mathf.Min(attack, 20); guard = 90; dodge = 70; }
                if (s.playerHealthRatio < 0.3f && canAttack) attack += 25;      // 마무리
                if (s.sanity >= 15 && canAttack) attack += 15; if (s.sanity <= -20) { attack -= 20; guard += 15; }
                if (s.playerGuarding && canAttack && !chosen.canBeGuarded) attack += 40;
                if (!canAttack && !s.playerAttacking) { approach = s.distance > 1.2f ? 40 : 0; wait = 30; }
            }
            attack *= w.aggression; guard *= w.caution; dodge *= w.evasion;
            lastScores[EnemyAction.Approach] = approach + Random.Range(0, w.randomness); lastScores[EnemyAction.Attack] = attack + Random.Range(0, w.randomness);
            lastScores[EnemyAction.Guard] = guard + Random.Range(0, w.randomness); lastScores[EnemyAction.Dodge] = dodge + Random.Range(0, w.randomness); lastScores[EnemyAction.Wait] = wait;
            EnemyAction best = EnemyAction.Wait; float bestScore = float.MinValue;
            foreach (var kv in lastScores) if (kv.Value > bestScore) { bestScore = kv.Value; best = kv.Key; }
            if (best != EnemyAction.Attack) chosen = null;
            return best;
        }
    }
}
