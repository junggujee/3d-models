using System;

namespace MoominAction
{
    /// <summary>
    /// 체력바의 '피해 조각'(순수 로직). 체력이 줄면 줄어든 만큼(이전 값 ~ 현재 값)을 빨갛게 남겨 두었다가
    /// Hold 초 뒤 Drain 초 동안 현재 값까지 줄어들며 사라짐. 연속으로 맞으면 조각이 이어서 커지고 타이머는 다시 시작.
    /// 회복하면 조각 위쪽까지 차오르는 순간 사라짐.
    /// </summary>
    public class DamageChunk
    {
        public float Hold = 0.45f, Drain = 0.35f;
        public float Top { get; private set; }        // 조각 위쪽 끝(0~1)
        public float Current { get; private set; }    // 현재 체력 비율(0~1)
        public bool Active { get; private set; }
        public float Age { get; private set; }
        float drainFrom;
        public DamageChunk(float start = 1f) { Current = Top = Clamp01(start); }
        /// <summary>체력 비율이 바뀔 때마다 호출.</summary>
        public void Set(float ratio)
        {
            ratio = Clamp01(ratio);
            if (ratio < Current) { if (!Active) Top = Current; Active = true; Age = 0f; }
            Current = ratio;
            if (Active && Current >= Top) { Active = false; Top = Current; }
        }
        public void Advance(float dt)
        {
            if (!Active) { Top = Current; return; }
            float prevAge = Age; Age += dt;
            if (Age < Hold) return;
            if (prevAge < Hold || drainFrom < Top) drainFrom = Math.Max(drainFrom, Top);
            float u = Drain <= 0f ? 1f : Clamp01((Age - Hold) / Drain);
            Top = drainFrom + (Current - drainFrom) * u;
            if (u >= 1f) { Active = false; Top = Current; drainFrom = 0f; }
        }
        /// <summary>조각 투명도: 처음엔 진하고(번쩍), 줄어들 때 옅어짐.</summary>
        public float Alpha => !Active ? 0f : Age < Hold ? 1f : 1f - 0.6f * Clamp01((Age - Hold) / Math.Max(1e-4f, Drain));
        /// <summary>맞은 직후 잠깐 흰빛이 섞이는 정도(0~1).</summary>
        public float Flash => Active && Age < 0.1f ? 1f - Age / 0.1f : 0f;
        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }

    /// <summary>경험치 곡선: 다음 레벨까지 필요한 경험치 = round(Base × level^Exponent).</summary>
    public static class ExpCurve
    {
        public const double Base = 100, Exponent = 1.6;
        public static long ToNext(int level) => (long)Math.Round(Base * Math.Pow(Math.Max(1, level), Exponent));
        /// <summary>경험치를 더하고 레벨업 처리. 반환 = 오른 레벨 수.</summary>
        public static int Add(ref int level, ref long exp, long amount, int maxLevel = 99)
        {
            if (amount <= 0) return 0; exp += amount; int ups = 0;
            while (level < maxLevel && exp >= ToNext(level)) { exp -= ToNext(level); level++; ups++; }
            if (level >= maxLevel) exp = Math.Min(exp, ToNext(level));
            return ups;
        }
    }

    /// <summary>숫자 표기: 9,999 까지는 쉼표, 그 위는 K · M · B (소수점 둘째 자리까지, 끝의 0 생략: 2.6M · 14.44M).</summary>
    public static class NumberFormat
    {
        public static string Comma(long v) => v.ToString("#,0");
        public static string Short(long v)
        {
            long a = Math.Abs(v); string s = v < 0 ? "-" : "";
            if (a < 10000) return s + Comma(a);
            if (a < 1_000_000) return s + Trim(a / 1e3) + "K";
            if (a < 1_000_000_000) return s + Trim(a / 1e6) + "M";
            return s + Trim(a / 1e9) + "B";
        }
        static string Trim(double x) => (Math.Floor(x * 100) / 100).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
