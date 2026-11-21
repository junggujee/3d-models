using UnityEngine;

namespace MoominAction
{
    public enum CoinEffect { None, StaggerOnAllHeads, SanityOnAllHeads }

    /// <summary>
    /// 스킬 데이터(명세 v2 3-2). Project 창 ▸ Create ▸ Combat ▸ Skill Definition 으로 만들고 Inspector 에서 수정.
    /// 코인 하나 = 공격 한 번: 공격 i 는 windup + i×attackInterval 에 시작, 그 뒤 hitTime 에 코인 판정(애니메이션 이벤트 OnCoinAttack 이 오면 그 시점).
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Combat/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId = "SKILL_000";
        public string skillName = "스킬";

        [Header("Coin Settings")] public int basePower = 10; public int coinPower = 3; public int coinCount = 3;
        [Header("Attack Settings")] public float damageMultiplier = 1.0f; public float cooldown = 5f; public int sanityCost = 0;
        [Header("Clash Settings")] public bool clashEnabled = true; public bool unblockable;

        [Header("Timing (초)")]
        [Tooltip("첫 공격 전 준비 시간(적의 공격 예고)")] public float windup = 0f;
        [Tooltip("공격(코인) 사이 간격")] public float attackInterval = 0.5f;
        [Tooltip("공격 시작부터 타격(코인 판정)까지")] public float hitTime = 0.35f;
        [Tooltip("히트박스가 열려 있는 시간")] public float activeWindow = 0.12f;
        [Tooltip("마지막 공격 뒤 회복(이 구간은 회피·다음 공격으로 취소 가능)")] public float recovery = 0.3f;
        [Tooltip("코인 i 에 쓸 애니메이터 상태(개수보다 적으면 반복)")] public string[] attackAnimations = { "Attack1", "Attack2", "Attack3" };

        [Header("Hit")] public float range = 2f; public float angle = 120f; public float knockback = 4f; [Tooltip("공격마다 앞으로 미끄러지는 속도")] public float lunge = 0f;
        public CoinEffect coinEffect = CoinEffect.None;
        [TextArea] public string description;

        public int MinPower => basePower; public int MaxPower => basePower + coinPower;
        public float ExpectedPower(int sanity) => CoinMath.ExpectedPower(basePower, coinPower, sanity);
        public float Duration => windup + (coinCount - 1) * attackInterval + hitTime + activeWindow + recovery;
    }
}
