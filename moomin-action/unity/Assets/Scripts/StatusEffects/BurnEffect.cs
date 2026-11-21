namespace MoominAction
{
    /// <summary>
    /// 화상: <b>발동 간격(기본 2초)마다</b> 강도만큼 체력 손실, 횟수 -1.
    /// 경과 시간을 누적해서 간격을 넘을 때만 발동 — 프레임 속도와 무관(한 프레임이 길면 그 사이 발동분을 모두 처리).
    /// </summary>
    public class BurnEffect : StatusEffect
    {
        public override StatusType Type => StatusType.Burn;
        float acc;
        public BurnEffect() { Interval = 2f; }
        public override void Add(StatusAmount a) { bool was = Active; base.Add(a); if (!was && Active) acc = 0f; }   // 새로 붙으면 타이머 처음부터
        public override void Clear() { base.Clear(); acc = 0f; }
        /// <summary>시간 진행. 반환: 이번에 잃을 체력 합.</summary>
        public int Advance(float dt)
        {
            if (!Active || Interval <= 0f) { acc = 0f; return 0; }
            acc += dt; int total = 0;
            while (acc >= Interval && Active) { acc -= Interval; total += Potency; Consume(); }
            return total;
        }
        public float TimeToNextTick => Active ? Interval - acc : 0f;
    }
}
