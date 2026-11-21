namespace MoominAction
{
    /// <summary>
    /// 침잠: 대상이 <b>공격에 맞을 때</b> 강도만큼 정신력 손실(코인 앞면 확률 하락), 횟수 -1. 같은 공격 판정에서는 한 번만.
    /// 정신력이 없는 대상은 StatusEffectManager.OnSinkingWithoutSanity 로 다른 효과를 줄 수 있음(기본: 같은 양의 체력 손실).
    /// </summary>
    public class SinkingEffect : StatusEffect
    {
        public override StatusType Type => StatusType.Sinking;
        int lastHitId = int.MinValue;
        /// <summary>반환: 잃을 정신력(발동 안 하면 0).</summary>
        public int OnOwnerHit(int attackId)
        {
            if (!Active || attackId == lastHitId) return 0;
            lastHitId = attackId; int p = Potency; Consume(); return p;
        }
    }
}
