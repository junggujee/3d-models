using System;

namespace MoominAction
{
    /// <summary>상태이상 종류.</summary>
    public enum StatusType { None, Bleed, Burn, Sinking, Tremor }

    /// <summary>부여량: 강도(Potency)와 횟수(Count). Inspector 에서 스킬마다 앞면/뒷면 따로 설정.</summary>
    [Serializable]
    public struct StatusAmount
    {
        public int potency, count;
        public StatusAmount(int potency, int count) { this.potency = potency; this.count = count; }
        public bool IsEmpty => potency <= 0 && count <= 0;
        public override string ToString() => $"{potency}/{count}";
    }

    /// <summary>
    /// 상태이상 공통 데이터: 종류 · 강도 · 횟수 · 지속시간 · 발동 간격 · 활성 여부.
    /// 발동 조건은 효과마다 다르므로(출혈 = 공격할 때, 화상 = 시간, 침잠 = 맞을 때, 진동 = 폭발 스킬) 각 클래스가 자기 메서드로 가짐.
    /// Unity 에 의존하지 않는 순수 C# — tools/status_tests 에서 그대로 실행해 검증.
    /// </summary>
    [Serializable]
    public abstract class StatusEffect
    {
        public abstract StatusType Type { get; }
        public int Potency, Count;
        /// <summary>지속시간(초). 0 이면 횟수가 다 될 때까지 유지.</summary>
        public float Duration;
        /// <summary>발동 간격(초). 시간으로 발동하는 효과(화상)만 사용.</summary>
        public float Interval;
        public float Elapsed;
        public int MaxPotency = 99, MaxCount = 99;
        public bool Active => Potency > 0 && Count > 0;

        /// <summary>중첩: 강도와 횟수를 각각 더함(상한 있음). 한쪽만 들어오면 다른 쪽은 최소 1 로 맞춰 효과가 살아 있게.</summary>
        public virtual void Add(StatusAmount a)
        {
            if (a.IsEmpty) return;
            Potency = Clamp(Potency + Math.Max(0, a.potency), 0, MaxPotency); Count = Clamp(Count + Math.Max(0, a.count), 0, MaxCount);
            if (Potency > 0 && Count == 0) Count = 1; if (Count > 0 && Potency == 0) Potency = 1;
            Elapsed = 0f;
        }
        /// <summary>발동 1회 소모: 횟수 -1, 0 이 되면 제거.</summary>
        protected void Consume() { Count--; if (Count <= 0) Clear(); }
        public virtual void Clear() { Potency = 0; Count = 0; Elapsed = 0f; }
        /// <summary>지속시간 경과(Duration > 0 일 때만). 만료되면 true.</summary>
        public bool TickDuration(float dt)
        {
            if (Duration <= 0f || !Active) return false;
            Elapsed += dt; if (Elapsed < Duration) return false; Clear(); return true;
        }
        static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
