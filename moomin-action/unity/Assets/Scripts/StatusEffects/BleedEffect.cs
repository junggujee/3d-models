namespace MoominAction
{
    /// <summary>
    /// 출혈: 대상이 <b>공격 코인을 실제로 사용할 때</b> 강도만큼 체력 손실, 횟수 -1. 같은 공격 판정(attackId)에서는 한 번만.
    /// </summary>
    public class BleedEffect : StatusEffect
    {
        public override StatusType Type => StatusType.Bleed;
        int lastAttackId = int.MinValue;
        /// <summary>반환: 잃을 체력(발동 안 하면 0).</summary>
        public int OnOwnerCoinUsed(int attackId)
        {
            if (!Active || attackId == lastAttackId) return 0;
            lastAttackId = attackId; int dmg = Potency; Consume(); return dmg;
        }
    }
}
