namespace MoominAction
{
    /// <summary>코인 하나에 적용된 위력 보정(AttackContext 에 기록 → 스킬 패널 표시).</summary>
    public struct CoinModifiers
    {
        public int powerUp, powerDown; public bool paralyzed;
        public bool Any => powerUp > 0 || powerDown > 0 || paralyzed;
        /// <summary>앞면일 때 더해지는 코인 위력: 마비면 0, 아니면 max(0, 코인 위력 + 힘 - 위력 감소).</summary>
        public int CoinValue(int coinPower) { if (paralyzed) return 0; int v = coinPower + powerUp - powerDown; return v < 0 ? 0 : v; }
    }

    /// <summary>코인 위력 보정 효과 공통: 이 캐릭터가 코인을 던질 때마다 횟수 -1(같은 공격 id 로는 한 번만).</summary>
    public abstract class CoinModifierEffect : StatusEffect
    {
        int lastId = int.MinValue;
        /// <summary>이번 코인에 적용할 강도(활성이 아니거나 이미 적용했으면 0).</summary>
        public int UseOnCoin(int attackId)
        {
            if (!Active || attackId == lastId) return 0;
            lastId = attackId; int p = Potency; Consume(); return p;
        }
    }

    /// <summary>힘: 앞면 코인의 위력 +강도.</summary>
    public class PowerUpEffect : CoinModifierEffect { public override StatusType Type => StatusType.PowerUp; }
    /// <summary>위력 감소: 앞면 코인의 위력 -강도(최소 0).</summary>
    public class PowerDownEffect : CoinModifierEffect { public override StatusType Type => StatusType.PowerDown; }
    /// <summary>마비: 앞면이어도 코인 위력 0. 강도는 쓰지 않고 횟수(= 영향받는 코인 수)만 의미 있음.</summary>
    public class ParalysisEffect : CoinModifierEffect { public override StatusType Type => StatusType.Paralysis; }
}
