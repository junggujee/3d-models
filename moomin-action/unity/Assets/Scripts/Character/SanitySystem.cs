using System;
using UnityEngine;

namespace MoominAction
{
    public enum SanityState { Focus, Stable, Unstable, Breakdown }

    /// <summary>
    /// 정신력(-45~+45). 코인 앞면 확률을 정하고 전투 흐름을 바꾸는 자원. 설계서 5장.
    /// 집중(+25~+45): 주는 피해 +10% · 안정(-24~+24) · 불안정(-25~-39): 스킬 부가 효과 봉인 · 붕괴(-40~-45): 받는 피해 +25%(조작은 유지).
    /// 전투가 끝나면(웨이브 정리) 0을 향해 천천히 회복.
    /// </summary>
    public class SanitySystem : MonoBehaviour
    {
        // 변화 규칙(초기 밸런스 값)
        public const int HitLanded = 1, ClashWin = 5, ClashLose = -5, StrongHitTaken = -4, DodgeSuccess = 1, GuardBlock = 1;
        public int startValue = 0; public float recoverPerSecond = 3f;
        public bool inCombat = true;
        public int Value { get; private set; }
        public event Action<int, int, string> Changed;   // (이전, 현재, 이유)
        float recoverAcc;

        void Awake() { Value = Mathf.Clamp(startValue, CoinMath.MinSanity, CoinMath.MaxSanity); }

        public SanityState State => Value >= 25 ? SanityState.Focus : Value >= -24 ? SanityState.Stable : Value >= -39 ? SanityState.Unstable : SanityState.Breakdown;
        public float HeadsProbability => CoinMath.HeadsProbability(Value);
        public float DamageDealtMultiplier => State == SanityState.Focus ? 1.1f : 1f;
        public float DamageTakenMultiplier => State == SanityState.Breakdown ? 1.25f : 1f;
        public bool ExtraEffectsEnabled => State == SanityState.Focus || State == SanityState.Stable;
        public static string Label(SanityState s) => s switch { SanityState.Focus => "집중", SanityState.Stable => "안정", SanityState.Unstable => "불안정", _ => "붕괴" };

        public void Add(int delta, string reason = "")
        {
            if (delta == 0) return; int old = Value; Value = Mathf.Clamp(Value + delta, CoinMath.MinSanity, CoinMath.MaxSanity);
            if (Value != old) Changed?.Invoke(old, Value, reason);
        }
        public void Set(int v, string reason = "") { Add(Mathf.Clamp(v, CoinMath.MinSanity, CoinMath.MaxSanity) - Value, reason); }

        void Update()
        {
            if (inCombat || Value == 0 || UIState.Paused) return;
            recoverAcc += recoverPerSecond * Time.deltaTime; if (recoverAcc < 1f) return;
            int step = Mathf.FloorToInt(recoverAcc); recoverAcc -= step; Add(Value > 0 ? -Mathf.Min(step, Value) : Mathf.Min(step, -Value), "전투 종료");
        }
    }
}
