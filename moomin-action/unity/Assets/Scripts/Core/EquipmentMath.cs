using System;
using System.Collections.Generic;

namespace MoominAction
{
    /// <summary>장비 칸. 무기 1 · 방어구 4(머리·가슴·다리·발) · 마법 아티팩트 3.</summary>
    public enum EquipSlot { None, Weapon, Head, Chest, Legs, Feet, Artifact }

    /// <summary>장비 하나가 주는 능력치(합칠 수 있음).</summary>
    [Serializable]
    public struct EquipStats
    {
        /// <summary>코인 최종 위력 +(앞면·뒷면 상관없이 공격마다, 합에도 들어감)</summary>
        public int finalPower;
        /// <summary>기본 공격력 +</summary>
        public float attack;
        /// <summary>최대 체력 +</summary>
        public float health;
        /// <summary>받는 피해 감소(0.1 = 10%). 여러 개면 곱으로 겹침.</summary>
        public float damageReduction;
        /// <summary>가드 피해 감소율 +(0.15 → 기본 50% 가 65%)</summary>
        public float guard;
        /// <summary>회피 코인 위력 +(구르기 판정이 더 잘 성공)</summary>
        public int evadePower;
        /// <summary>이동 속도 +(0.1 = 10%)</summary>
        public float moveSpeed;
        public static EquipStats operator +(EquipStats a, EquipStats b) => new EquipStats
        {
            finalPower = a.finalPower + b.finalPower, attack = a.attack + b.attack, health = a.health + b.health,
            damageReduction = 1f - (1f - a.damageReduction) * (1f - b.damageReduction),
            guard = a.guard + b.guard, evadePower = a.evadePower + b.evadePower, moveSpeed = a.moveSpeed + b.moveSpeed,
        };
        public bool IsEmpty => finalPower == 0 && attack == 0 && health == 0 && damageReduction == 0 && guard == 0 && evadePower == 0 && moveSpeed == 0;
    }

    /// <summary>
    /// 공격력·피해·전투력 공식(순수 C# — tools/status_tests 로 검증).
    /// 피해 D = 공격력 × (최종 위력 ÷ 기준 위력 10) × 배율 × 받는 쪽 보정.
    ///   기본 공격력이 피해의 크기를 정하고, 코인 결과(앞면·뒷면·힘·마비·장비 보너스)가 그 위에서 피해를 직접 움직임.
    ///   공격력 10 이면 예전 공식 D = P × M × R 과 같음(밸런스 유지).
    /// </summary>
    public static class EquipmentMath
    {
        public const float ReferencePower = 10f, BaseAttack = 10f;

        public static int Damage(int finalPower, float attack, float multiplier, float defense)
        {
            double d = Math.Max(0, attack) * (Math.Max(0, finalPower) / (double)ReferencePower) * multiplier * defense;
            return Math.Max(1, (int)Math.Round(d, MidpointRounding.AwayFromZero));
        }

        /// <summary>코인 하나의 최종 위력 = 기본 + (앞면 ? 코인 위력 : 0) + 장비 보너스(0 아래로는 안 내려감).</summary>
        public static int FinalPower(int basePower, int coinValue, bool heads, int bonus) => Math.Max(0, basePower + (heads ? coinValue : 0) + bonus);

        /// <summary>레벨에 따른 공격력(레벨당 +perLevel).</summary>
        public static float LevelAttack(int level, float perLevel) => Math.Max(0, level - 1) * perLevel;

        /// <summary>전투력(화면 표시용 한 숫자): 공격력·최종 위력 보너스·최대 체력·피해 감소를 섞음.</summary>
        public static int CombatPower(float attack, int finalPowerBonus, float maxHealth, float damageReduction, int level)
        {
            double cp = attack * 120 + finalPowerBonus * 320 + maxHealth * 6 + damageReduction * 4000 + level * 40;
            return (int)Math.Round(cp);
        }

        public static EquipStats Sum(IEnumerable<EquipStats> items) { var t = default(EquipStats); foreach (var s in items) t += s; return t; }

        /// <summary>능력치 한 줄 설명들("코인 최종 위력 +1" …). 0 인 값은 빼고.</summary>
        public static List<string> Describe(EquipStats s)
        {
            var o = new List<string>();
            if (s.finalPower != 0) o.Add($"코인 최종 위력 {(s.finalPower > 0 ? "+" : "")}{s.finalPower}");
            if (s.attack != 0) o.Add($"공격력 {(s.attack > 0 ? "+" : "")}{s.attack:0.#}");
            if (s.health != 0) o.Add($"최대 체력 {(s.health > 0 ? "+" : "")}{s.health:0.#}");
            if (s.damageReduction != 0) o.Add($"받는 피해 -{s.damageReduction * 100:0.#}%");
            if (s.guard != 0) o.Add($"가드 피해 감소 +{s.guard * 100:0.#}%");
            if (s.evadePower != 0) o.Add($"회피 코인 위력 {(s.evadePower > 0 ? "+" : "")}{s.evadePower}");
            if (s.moveSpeed != 0) o.Add($"이동 속도 +{s.moveSpeed * 100:0.#}%");
            return o;
        }

        public static string SlotName(EquipSlot s) => s switch
        {
            EquipSlot.Weapon => "무기", EquipSlot.Head => "머리", EquipSlot.Chest => "가슴", EquipSlot.Legs => "다리", EquipSlot.Feet => "발", EquipSlot.Artifact => "마법 아티팩트", _ => "",
        };
    }
}
