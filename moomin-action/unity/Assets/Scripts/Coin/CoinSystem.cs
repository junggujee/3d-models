using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>코인 하나의 판정 결과.</summary>
    public struct CoinResult { public int index; public bool heads; public float probability; public int sanity; }

    /// <summary>코인 수식(순수 계산). 앞면 확률 = Clamp(0.5 + S/100, 0.05, 0.95), 공격 하나의 위력 = 기본 + (앞면 ? 코인 위력 : 0).</summary>
    public static class CoinMath
    {
        public const int MinSanity = -45, MaxSanity = 45;
        public static float HeadsProbability(int sanity) => Mathf.Clamp(0.5f + sanity / 100f, 0.05f, 0.95f);
        public static bool Toss(int sanity, System.Random rng = null) { float r = rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value; return r < HeadsProbability(sanity); }
        public static int Power(int basePower, int coinPower, bool heads) => basePower + (heads ? coinPower : 0);
        public static float ExpectedPower(int basePower, int coinPower, int sanity) => basePower + HeadsProbability(sanity) * coinPower;
        public static int TossMany(int coins, int sanity, System.Random rng = null) { int h = 0; for (int i = 0; i < coins; i++) if (Toss(sanity, rng)) h++; return h; }
    }

    /// <summary>
    /// 앞면·뒷면 판정만 담당(명세 v2 4-1). 언제 던질지는 SkillExecutor 가 정함.
    /// 결과는 공격 시점에 한 번만 결정 — 같은 공격의 피해·클래시 계산에서 다시 던지지 않음(AttackContext 에 저장).
    /// </summary>
    public class CoinSystem : MonoBehaviour
    {
        public event Action<CoinResult> Tossed;
        public bool TossCoin(CharacterStats stats) => Toss(stats, 0).heads;
        public CoinResult Toss(CharacterStats stats, int index)
        {
            int s = stats != null ? stats.currentSanity : 0;
            var r = new CoinResult { index = index, sanity = s, probability = CoinMath.HeadsProbability(s), heads = CoinMath.Toss(s) };
            Tossed?.Invoke(r); return r;
        }
        public int CalculatePower(int basePower, int coinPower, bool isHeads) => CoinMath.Power(basePower, coinPower, isHeads);
    }
}
