using System;

namespace MoominAction
{
    /// <summary>
    /// 여러 발 투사체 모양(순수 C# — status_tests). 회전 탄막 · 부채꼴 물방울 · 딸기 포물선.
    ///  · Angles: 한 코인에 n 발의 방향(도). spread ≥ 359 → 둘레 고리(같은 간격), 아니면 가운데 기준 부채꼴. 코인마다 spinPerCoin 만큼 돌려 엇갈리게(사이로 피할 틈).
    ///  · LobHeight: 포물선 높이(0 → 꼭대기 → 0), 꼭대기 = 2.5 + 거리 × 0.25.
    /// </summary>
    public static class ProjectilePattern
    {
        public static float[] Angles(int count, float spreadDeg, float baseDeg, int coinIndex = 0, float spinPerCoin = 0f)
        {
            int n = Math.Max(1, count); var a = new float[n]; float b = baseDeg + spinPerCoin * coinIndex;
            if (n == 1) { a[0] = b; return a; }
            if (spreadDeg >= 359f) { for (int i = 0; i < n; i++) a[i] = b + 360f * i / n; return a; }
            for (int i = 0; i < n; i++) a[i] = b - spreadDeg * 0.5f + spreadDeg * i / (n - 1);
            return a;
        }
        public static float LobPeak(float distance) => 2.5f + Math.Max(0f, distance) * 0.25f;
        /// <summary>t = 0~1 에서 높이(시작·끝 0).</summary>
        public static float LobHeight(float t, float distance) { float u = Math.Max(0f, Math.Min(1f, t)); return 4f * LobPeak(distance) * u * (1f - u); }
    }
}
