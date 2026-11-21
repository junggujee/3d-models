namespace MoominAction
{
    /// <summary>
    /// 진동: 쌓아 두기만 하고 스스로는 발동하지 않음(일반 공격으로 자동 폭발 없음).
    /// <b>진동 폭발</b>이 설정된 스킬이 적중하면 경직 게이지 피해 = 강도 × PerPotency + 횟수 × PerCount, 횟수 -1. 같은 공격 판정에서 한 번만.
    /// </summary>
    public class TremorEffect : StatusEffect
    {
        public override StatusType Type => StatusType.Tremor;
        public int PerPotency = 4, PerCount = 2;
        int lastBurstId = int.MinValue;
        /// <summary>반환: 경직 게이지 피해(발동 안 하면 0).</summary>
        public int Burst(int attackId)
        {
            if (!Active || attackId == lastBurstId) return 0;
            lastBurstId = attackId; int dmg = Potency * PerPotency + Count * PerCount; Consume(); return dmg;
        }
        public int PreviewBurst => Active ? Potency * PerPotency + Count * PerCount : 0;
    }
}
