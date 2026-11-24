using System.Collections.Generic;
using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>스킬 설명 글자(캐릭터 시트 스킬 칸 · 상세 창 · 도감 공용).</summary>
    public static class SkillInfo
    {
        public static readonly string[] SlotKeys = { "좌클릭 1타", "좌클릭 2타", "좌클릭 3타", "Q", "E", "R" };

        /// <summary>민미가 가진 스킬(일반 공격 3타 + Q·E·R) — 없는 칸은 빼고 (스킬, 키) 목록.</summary>
        public static List<(SkillDefinition skill, string key)> Owned(CharacterStats player)
        {
            var l = new List<(SkillDefinition, string)>(); if (player == null || !player.TryGetComponent<CombatManager>(out var cm)) return l;
            var arr = new[] { cm.basic1, cm.basic2, cm.basic3, cm.skill1, cm.skill2, cm.ultimate };
            for (int i = 0; i < arr.Length; i++) if (arr[i] != null) l.Add((arr[i], SlotKeys[i]));
            return l;
        }

        /// <summary>최종 위력 범위(뒷면만 ~ 앞면, 장비 최종 위력 포함).</summary>
        public static (int min, int max) Power(SkillDefinition s, CharacterStats st)
        {
            int bonus = st != null ? st.equipFinalPower : 0;
            return (EquipmentMath.FinalPower(s.basePower, s.coinPower, false, bonus), EquipmentMath.FinalPower(s.basePower, s.coinPower, true, bonus));
        }

        /// <summary>키 칸으로 종류: 좌클릭 = 일반 공격 · Q/E = 스킬 · R = 필살기.</summary>
        public static string Kind(string key) => key == "R" ? "필살기" : key == "Q" || key == "E" ? "스킬" : "일반 공격";
        public static string AttrLine(SkillDefinition s) => $"{SkillAttributes.Names[(int)s.attribute]} 속성 · 코인 {s.coinCount}개" + (s.isAreaAttack ? " · 광역" : "") + (s.isProjectile ? " · 원거리" : "");

        /// <summary>자세한 줄들(상세 창·도감).</summary>
        public static List<string> Details(SkillDefinition s, CharacterStats st)
        {
            var o = new List<string>(); var (mn, mx) = Power(s, st);
            o.Add($"기본 위력 {s.basePower}  ·  코인 위력 +{s.coinPower} × {s.coinCount}개  →  공격마다 최종 위력 {mn}~{mx}");
            o.Add($"피해 배율 ×{s.damageMultiplier:0.##}  ·  재사용 {s.cooldown:0.#}초" + (s.energyCost > 0 ? $"  ·  에너지 {s.energyCost}" : "") + (s.sanityCost > 0 ? $"  ·  정신력 {s.sanityCost}" : ""));
            o.Add((s.isAreaAttack ? $"광역 {(s.areaShape == AreaShape.Line ? "직선" : "원")} {s.areaRadius:0.#}m" : $"사거리 {s.range:0.#}m") + (s.MeleeClashable ? "  ·  합 가능" : s.isProjectile ? "  ·  원거리끼리만 합" : "  ·  합 불가") + (s.canBeGuarded ? "" : "  ·  방어 불가"));
            var fx = new List<string>();
            foreach (var t in StatusInfo.All)
            {
                var h = s.StatusOn(t, true); var tl = s.StatusOn(t, false);
                if (!h.IsEmpty) fx.Add($"앞면 {StatusInfo.Name(t)} {h.potency}·{h.count}"); if (!tl.IsEmpty) fx.Add($"뒷면 {StatusInfo.Name(t)} {tl.potency}·{tl.count}");
            }
            if (fx.Count > 0) o.Add("적중 시  " + string.Join("  ·  ", fx));
            return o;
        }
    }
}
