using System;

namespace MoominAction
{
    /// <summary>
    /// 레벨 규칙(순수 C# — tools/status_tests 로 검증).
    ///  · 레벨업: 최대 체력 +12, 공격력 +0.6, 체력·스태미나 모두 회복
    ///  · 레벨 차이 → 코인 위력 보정: 3레벨 차이마다 ±1, 최대 ±3(높으면 +, 낮으면 −). 적에게도 똑같이. 피해 자체는 보정하지 않음
    ///  · 적 레벨: 웨이브 + (−1~+1) + 종류 보정(곰 +1 · 그린볼런 +1 · 고래 +2) + 보스 +3. 레벨마다 체력 +6% · 공격력 +0.5
    ///  · 경험치: 적이 높을수록 더(레벨 차이당 ±10%, 50%~150%)
    ///  · 칭호: 10레벨마다(초보 → 새싹 → 숙련 → 노련 → 전설의 검사)
    /// </summary>
    public static class LevelMath
    {
        public const float HealthPerLevel = 12f, AttackPerLevel = 0.6f;
        public const int LevelsPerCoin = 3, MaxCoinBonus = 3, BossLevelBonus = 3;
        public const float EnemyHealthPerLevel = 0.06f, EnemyAttackPerLevel = 0.5f, ExpPerLevelDiff = 0.1f;

        /// <summary>내 레벨 − 상대 레벨 → 코인 위력 보정(0 쪽으로 버림: 차이 2 → 0, 3 → +1, −5 → −1, 10 → +3).</summary>
        public static int CoinBonus(int myLevel, int otherLevel)
        {
            if (myLevel <= 0 || otherLevel <= 0) return 0;
            int b = (myLevel - otherLevel) / LevelsPerCoin; return Math.Max(-MaxCoinBonus, Math.Min(MaxCoinBonus, b));
        }

        /// <summary>코인 위력 + 레벨 보정(0 아래로는 안 내려감).</summary>
        public static int CoinPower(int coinPower, int bonus) => Math.Max(0, coinPower + bonus);

        /// <summary>적 레벨(variance: −1~+1 난수, typeBonus: 종류 보정).</summary>
        public static int EnemyLevel(int wave, int variance, int typeBonus, bool boss) => Math.Max(1, Math.Max(1, wave) + variance + typeBonus + (boss ? BossLevelBonus : 0));

        public static float EnemyHealthScale(int level) => 1f + EnemyHealthPerLevel * Math.Max(0, level - 1);
        public static float EnemyAttackBonus(int level) => EnemyAttackPerLevel * Math.Max(0, level - 1);

        /// <summary>처치 경험치: 적이 나보다 높으면 더, 낮으면 덜(레벨 차이당 10%, 50%~150%).</summary>
        public static long ExpReward(long baseExp, int enemyLevel, int playerLevel)
        {
            double k = 1.0 + ExpPerLevelDiff * (enemyLevel - playerLevel); k = Math.Max(0.5, Math.Min(1.5, k));
            return Math.Max(1, (long)Math.Round(baseExp * k, MidpointRounding.AwayFromZero));
        }

        /// <summary>레벨 차이 표시: "코인 위력 +1" / "코인 위력 −2" / ""(보정 없음).</summary>
        public static string BonusLabel(int bonus) => bonus == 0 ? "" : bonus > 0 ? $"코인 위력 +{bonus}" : $"코인 위력 -{-bonus}";

        /// <summary>칭호(10레벨마다).</summary>
        public static string Title(int level) => level >= 40 ? "전설의 검사" : level >= 30 ? "노련한 검사" : level >= 20 ? "숙련 검사" : level >= 10 ? "새싹 검사" : "초보 검사";
        /// <summary>이 레벨에서 칭호가 바뀌는가.</summary>
        public static bool NewTitleAt(int level) => level >= 10 && level % 10 == 0 && level <= 40;
    }
}
