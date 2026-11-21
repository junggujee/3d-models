namespace MoominAction
{
    /// <summary>
    /// 경직 게이지(체력과 별개). 피해가 쌓여 최대치에 닿으면 '깨짐'(true 반환) → 게이지 0 으로 되돌림.
    /// 일정 시간 피해가 없으면 서서히 회복. 경직 지속시간은 CharacterStats.staggerDuration 이 따로 관리.
    /// </summary>
    public class StaggerGauge
    {
        public float Max = 100f, RecoverDelay = 4f, RecoverPerSecond = 10f;
        public float Current { get; private set; }
        float sinceHit;
        public float Ratio => Max > 0 ? Current / Max : 0f;
        public bool Add(float amount)
        {
            if (amount <= 0f) return false; Current += amount; sinceHit = 0f;
            if (Current < Max) return false; Current = 0f; return true;
        }
        public void Advance(float dt)
        {
            if (Current <= 0f) return; float before = sinceHit; sinceHit += dt; if (sinceHit <= RecoverDelay) return;
            float t = sinceHit - (before > RecoverDelay ? before : RecoverDelay);   // 지연을 넘긴 시간만 회복(프레임 속도 무관)
            Current -= RecoverPerSecond * t; if (Current < 0f) Current = 0f;
        }
        public void Reset() { Current = 0f; sinceHit = 0f; }
    }
}
