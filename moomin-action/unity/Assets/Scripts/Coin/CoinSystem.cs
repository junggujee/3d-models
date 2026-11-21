using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>코인 한 개의 판정 결과.</summary>
    public struct CoinResult
    {
        public int index;          // 스킬 안에서 몇 번째 코인인지(0부터)
        public bool heads;         // 앞면 = 코인 위력 추가
        public float probability;  // 던질 때의 앞면 확률
        public int sanity;         // 던질 때의 정신력(판정 후 정신력이 바뀌어도 결과는 그대로)
    }

    /// <summary>
    /// 코인 수식(순수 계산, 상태 없음). 설계서 3장.
    ///   앞면 확률  Pr(H) = Clamp(0.5 + S/100, 0.05, 0.95)   (S = 정신력 -45~+45)
    ///   위력       P = B + H×C                              (B 기본 위력, H 앞면 수, C 코인 위력)
    ///   기대 위력  E(P) = B + n×p×C
    /// </summary>
    public static class CoinMath
    {
        public const int MinSanity = -45, MaxSanity = 45;
        public static float HeadsProbability(int sanity) => Mathf.Clamp(0.5f + sanity / 100f, 0.05f, 0.95f);
        public static bool Toss(int sanity, System.Random rng = null) { float r = rng != null ? (float)rng.NextDouble() : UnityEngine.Random.value; return r < HeadsProbability(sanity); }
        public static int Power(int basePower, int coinPower, int heads) => basePower + heads * coinPower;
        public static float ExpectedPower(int basePower, int coins, int coinPower, int sanity) => basePower + coins * HeadsProbability(sanity) * coinPower;
        /// <summary>코인 n개를 한꺼번에 던져 앞면 수를 반환.</summary>
        public static int TossMany(int coins, int sanity, System.Random rng = null) { int h = 0; for (int i = 0; i < coins; i++) if (Toss(sanity, rng)) h++; return h; }
    }

    /// <summary>플레이어용 코인 던지기(이벤트로 HUD에 결과를 알림). 설계서의 CoinSystem.</summary>
    public class CoinSystem : MonoBehaviour
    {
        public event Action<CoinResult> Tossed;
        public CoinResult Toss(int sanity, int index = 0)
        {
            var r = new CoinResult { index = index, sanity = sanity, probability = CoinMath.HeadsProbability(sanity) };
            r.heads = CoinMath.Toss(sanity); Tossed?.Invoke(r); return r;
        }
        public bool TossCoin(int sanity, int index = 0) => Toss(sanity, index).heads;
        public int CalculatePower(int basePower, int coinPower, int coinCount, int sanity) { int h = 0; for (int i = 0; i < coinCount; i++) if (TossCoin(sanity, i)) h++; return CoinMath.Power(basePower, coinPower, h); }
    }
}
