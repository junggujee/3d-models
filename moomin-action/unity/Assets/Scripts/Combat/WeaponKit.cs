using System.Collections.Generic;

namespace MoominAction
{
    public enum WeaponPassive { None, ShieldBarrier, DuelHeat }

    /// <summary>
    /// 무기 한 종류의 전투 묶음: 좌클릭 3타 · Q · E · R 스킬 id · 방어 코인(기본·코인 위력, 보통/완벽 감소율) · 방어 자세 애니메이션 · 고유 효과.
    /// 무기(ItemDef.kit)를 끼우면 CombatManager.ApplyKit 이 스킬·방어·고유 효과를 바꿈. 캐릭터 시트·도감의 스킬 칸도 따라 바뀜.
    /// </summary>
    public class WeaponKit
    {
        public string id, name, title; public string[] basics; public string skill1, skill2, ultimate;
        public int guardBase, guardCoin; public float guardReduction, perfectReduction; public string guardAnim = "Guard";
        public WeaponPassive passive; public bool showSword;
        public string passiveName, passiveText;
    }

    public static class WeaponKits
    {
        public const string Spark = "spark", Shield = "shield", Dual = "dual";
        static readonly Dictionary<string, WeaponKit> map = new()
        {
            [Spark] = new WeaponKit { id = Spark, name = "스파크 검", title = "검", basics = new[] { "BASIC_1", "BASIC_2", "BASIC_3" }, skill1 = "SKILL_001", skill2 = "SKILL_002", ultimate = "SKILL_003",
                guardBase = 8, guardCoin = 3, guardReduction = 0.5f, perfectReduction = 0.8f, showSword = true, passiveName = "", passiveText = "" },
            [Shield] = new WeaponKit { id = Shield, name = "베개 방패", title = "방패", basics = new[] { "SHIELD_1", "SHIELD_2", "SHIELD_3" }, skill1 = "SHIELD_Q", skill2 = "SHIELD_E", ultimate = "SHIELD_R",
                guardBase = 14, guardCoin = 5, guardReduction = 0.65f, perfectReduction = 0.9f, guardAnim = "ShieldGuard", passive = WeaponPassive.ShieldBarrier,
                passiveName = "포근한 보호막", passiveText = "합에서 이기면 최대 체력의 8% 보호막(최대 25%, 8초). 방어 위력이 높아 공격을 잘 막는다." },
            [Dual] = new WeaponKit { id = Dual, name = "회전 쌍검", title = "쌍검", basics = new[] { "DUAL_1", "DUAL_2", "DUAL_3" }, skill1 = "DUAL_Q", skill2 = "DUAL_E", ultimate = "DUAL_R",
                guardBase = 12, guardCoin = 4, guardReduction = 0.6f, perfectReduction = 0.85f, guardAnim = "DualGuard", passive = WeaponPassive.DuelHeat,
                passiveName = "결투 고조", passiveText = "명중·합 승리·완벽 방어마다 1스택(최대 10). 스택마다 쌍검이 빨리 돌고 피해 +2%. 최대에서 궁극 - 회절원참격." },
        };
        public static WeaponKit Get(string id) => id != null && map.TryGetValue(id, out var k) ? k : map[Spark];
        public static IEnumerable<WeaponKit> All => map.Values;
    }
}
