using UnityEngine;

namespace MoominAction
{
    /// <summary>적 공격 하나(코인 포함). 예고(windup) 동안 바닥에 경고 표시 → 타격 순간 코인을 던져 합 또는 일방 공격.</summary>
    [System.Serializable]
    public class EnemyAttack
    {
        public string name = "몸통 박치기";
        public int basePower = 6, coinCount = 2, coinPower = 3;
        public float damageMultiplier = 0.7f, knockback = 4f;
        public float windup = 0.65f, impactDelay = 0.12f, strikeTime = 0.3f, recovery = 0.8f;
        public float range = 1.4f, lunge = 5f, radius = 1.2f;
        public bool aoe, unblockable;

        public EnemyAttack Clone() => (EnemyAttack)MemberwiseClone();
        public static EnemyAttack Bump() => new EnemyAttack();
        public static EnemyAttack Slam() => new EnemyAttack { name = "내려찍기", basePower = 9, coinCount = 2, coinPower = 4, damageMultiplier = 0.8f, knockback = 7f, windup = 1.0f, impactDelay = 0.15f, strikeTime = 0.35f, recovery = 1.1f, range = 1.9f, lunge = 1f, radius = 2.0f, aoe = true };
        // 보스 전용
        public static EnemyAttack GuardBreak() => new EnemyAttack { name = "방어 부수기", basePower = 14, coinCount = 1, coinPower = 6, damageMultiplier = 1.0f, knockback = 9f, windup = 1.2f, recovery = 1.2f, range = 2.2f, lunge = 3f, radius = 1.6f, unblockable = true };
        public static EnemyAttack Lunge() => new EnemyAttack { name = "돌진", basePower = 10, coinCount = 2, coinPower = 4, damageMultiplier = 0.8f, knockback = 8f, windup = 0.8f, strikeTime = 0.45f, recovery = 1.0f, range = 6f, lunge = 12f, radius = 1.4f };
        public static EnemyAttack Flurry() => new EnemyAttack { name = "난타", basePower = 7, coinCount = 4, coinPower = 3, damageMultiplier = 0.6f, knockback = 3f, windup = 0.7f, strikeTime = 0.6f, recovery = 0.9f, range = 1.8f, lunge = 3f, radius = 1.4f };
    }
}
