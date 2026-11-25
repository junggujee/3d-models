using System.Collections.Generic;

namespace MoominAction
{
    /// <summary>
    /// 대장간 계산(순수 C# — UnityEngine 없음, tools/status_tests 로 검증).
    ///
    /// ■ 강화: +0 ~ +5. 돈만 내면 누구나, 다만 올라갈수록 비싸지고 잘 실패한다.
    ///   성공률  +1 95% · +2 80% · +3 60% · +4 40% · +5 25%
    ///   비용    기준값 × (1·2·4·7·12)  (기준값 = 무기 가격의 1/4, 최소 40)
    ///   실패    (풍선고래는 무기를 부수지 않는다 — "내 손에서 부서진 적은 없다구!")
    ///            +3 미만: 그대로 (돈만 날림)
    ///            +3 이상: 한 단계 내려감
    ///   효과    단계마다 공격 배율 +8%, 두 단계마다 최종 위력 +1, 받는 피해 -1.5%
    /// </summary>
    public static class ForgeMath
    {
        public const int MaxLevel = 5;
        static readonly float[] Chance = { 0.95f, 0.80f, 0.60f, 0.40f, 0.25f };
        static readonly int[] CostMul = { 1, 2, 4, 7, 12 };

        /// <summary>+level → +(level+1) 성공 확률(0~1). 범위 밖이면 0.</summary>
        public static float SuccessChance(int level) => level < 0 || level >= MaxLevel ? 0f : Chance[level];
        /// <summary>강화 비용(골드). value = 무기의 기본 가격.</summary>
        public static int Cost(int level, int value)
        {
            if (level < 0 || level >= MaxLevel) return 0;
            int baseCost = value / 4; if (baseCost < 40) baseCost = 40;
            return baseCost * CostMul[level];
        }
        /// <summary>실패했을 때 떨어지는 단계(+3 이상에서만 하락).</summary>
        public static int OnFail(int level) => level >= 3 ? level - 1 : level;
        /// <summary>강화 단계로 붙는 공격 배율(1.0 = 그대로).</summary>
        public static float DamageBonus(int level) => 1f + 0.08f * Clamp(level);
        /// <summary>강화 단계로 붙는 최종 위력(두 단계마다 +1).</summary>
        public static int PowerBonus(int level) => Clamp(level) / 2;
        /// <summary>강화 단계로 붙는 받는 피해 감소(0.015 = 1.5%).</summary>
        public static float GuardBonus(int level) => 0.015f * Clamp(level);
        public static int Clamp(int level) => level < 0 ? 0 : level > MaxLevel ? MaxLevel : level;
        /// <summary>이름 앞에 붙일 글자("+3 " · +0 이면 빈 글자).</summary>
        public static string Prefix(int level) => Clamp(level) > 0 ? "+" + Clamp(level) + " " : "";

        /// <summary>제작법 한 줄: 결과 1개 = 재료들 + 수고비.</summary>
        public class Recipe
        {
            public string id, result, note; public int gold; public string[] mats; public int[] counts;
            /// <summary>재료 i 가 몇 개 필요한가.</summary>
            public int Need(int i) => counts != null && i < counts.Length ? counts[i] : 1;
        }

        /// <summary>풍선고래가 만들 수 있는 것들(재료 + 수고비). 재료는 ItemDatabase 의 id.</summary>
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe { id = "craft_laser", result = "laser_blader", gold = 600, note = "맨두를 쓰러뜨려 얻은 진동 코어로 벼려 내는 푸른 광선검.",
                         mats = new[] { "mandu_core", "bat_alloy", "shard_star", "dust_fire" }, counts = new[] { 1, 3, 6, 4 } },
            new Recipe { id = "craft_spin", result = "spin_blades", gold = 320, note = "바람개비처럼 도는 쌍검. 별 조각을 날로 쓴다.",
                         mats = new[] { "shard_star", "dust_fire", "feather_silver" }, counts = new[] { 8, 5, 1 } },
            new Recipe { id = "craft_pillow", result = "pillow_shield", gold = 180, note = "푹신한 베개 방패. 은빛 깃털을 속에 넣는다.",
                         mats = new[] { "feather_silver", "shard_star" }, counts = new[] { 2, 4 } },
            new Recipe { id = "craft_headband", result = "headband_blue", gold = 90, note = "파란 머리띠. 코인 위력이 조금 오른다.",
                         mats = new[] { "shard_star", "feather_silver" }, counts = new[] { 3, 1 } },
            new Recipe { id = "craft_pearl_ring", result = "ring_pearl", gold = 260, note = "용궁의 진주를 박은 반지. 정신력이 단단해진다.",
                         mats = new[] { "royal_pearl", "shard_star" }, counts = new[] { 1, 5 } },
            new Recipe { id = "craft_core_charm", result = "charm_tremor", gold = 240, note = "맨두의 진동 코어를 깎아 만든 부적. 진동이 더 잘 쌓인다.",
                         mats = new[] { "mandu_core", "dust_fire" }, counts = new[] { 1, 6 } },
        };
        public static Recipe GetRecipe(string id) { foreach (var r in Recipes) if (r.id == id) return r; return null; }
    }
}
