using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>기본 스킬 목록. Assets/Resources/Skills 에 같은 skillId 의 SkillData 에셋이 있으면 그 값으로 덮어씀(데이터 중심).</summary>
    public static class SkillLibrary
    {
        static Dictionary<string, SkillData> map;
        public static SkillData Get(string id) { Ensure(); return map.TryGetValue(id, out var s) ? s : null; }
        public static IEnumerable<SkillData> All { get { Ensure(); return map.Values; } }

        static SkillData Make(string id, string name, SkillSlot slot, int b, int n, int c, CoinMode mode, float dur, float[] hits, string[] anims, float[] animT, float range, float angle, float kb,
                              float mult = 1f, int sanity = 0, float cd = 0f, CoinEffect fx = CoinEffect.BonusPower, bool special = false, string desc = "")
        {
            var s = ScriptableObject.CreateInstance<SkillData>(); s.name = id;
            s.skillId = id; s.skillName = name; s.slot = slot; s.basePower = b; s.coinCount = n; s.coinPower = c; s.mode = mode; s.duration = dur; s.hitTimes = hits; s.animStates = anims; s.animTimes = animT;
            s.range = range; s.angle = angle; s.knockback = kb; s.damageMultiplier = mult; s.sanityCost = sanity; s.cooldown = cd; s.coinEffect = fx; s.special = special; s.description = desc; return s;
        }

        static void Ensure()
        {
            if (map != null) return; map = new Dictionary<string, SkillData>();
            void Add(SkillData s) => map[s.skillId] = s;
            // 일반 공격(좌클릭 3연타): 코인 1~2개, 빠르고 안정적
            Add(Make("BASIC_1", "베기", SkillSlot.Basic, 8, 1, 3, CoinMode.PerHit, 0.8f, new[] { 0.37f }, new[] { "Attack1" }, new[] { 0f }, 1.9f, 110, 4f));
            Add(Make("BASIC_2", "되베기", SkillSlot.Basic, 8, 1, 4, CoinMode.PerHit, 0.8f, new[] { 0.37f }, new[] { "Attack2" }, new[] { 0f }, 1.9f, 110, 5f));
            Add(Make("BASIC_3", "내려찍기", SkillSlot.Basic, 9, 2, 3, CoinMode.SingleMotion, 1.0f, new[] { 0.40f }, new[] { "Attack3" }, new[] { 0f }, 2.3f, 160, 8f));
            // Q 연속 베기: 3코인 = 3연속 공격(설계서 예시: 기본 10, 코인 +3, 쿨 5초)
            Add(Make("SKILL_001", "연속 베기", SkillSlot.Skill1, 10, 3, 3, CoinMode.PerHit, 1.9f, new[] { 0.37f, 0.87f, 1.37f }, new[] { "Attack1", "Attack2", "Attack3" }, new[] { 0f, 0.5f, 1.0f }, 2.1f, 120, 5f,
                     cd: 5f, desc: "세 번 연속으로 벤다. 베기마다 코인을 하나씩 던진다."));
            // E 특수: 한 번의 찌르기에 코인 2개, 정신력 -8, 모두 앞면이면 적을 경직
            Add(Make("SKILL_002", "불꽃 찌르기", SkillSlot.Skill2, 16, 2, 6, CoinMode.SingleMotion, 1.0f, new[] { 0.45f }, new[] { "Attack3" }, new[] { 0f }, 2.8f, 70, 10f,
                     mult: 1.2f, sanity: 8, cd: 8f, fx: CoinEffect.StaggerOnAllHeads, special: true, desc: "한 번의 찌르기에 코인 두 개를 건다. 모두 앞면이면 적이 크게 휘청인다. 정신력 8 소모."));
            // R 필살기: 5코인 5연속, 높은 위력, 정신력 -12, 쿨 30초, 모두 앞면이면 정신력 +10
            Add(Make("SKILL_003", "별빛 난무", SkillSlot.Ultimate, 14, 5, 5, CoinMode.PerHit, 2.8f, new[] { 0.35f, 0.75f, 1.15f, 1.55f, 2.1f }, new[] { "Attack1", "Attack2", "Attack1", "Attack2", "Attack3" }, new[] { 0f, 0.4f, 0.8f, 1.2f, 1.7f },
                     3.2f, 360, 9f, mult: 1.3f, sanity: 12, cd: 30f, fx: CoinEffect.SanityOnAllHeads, special: true, desc: "주위를 다섯 번 베어 낸다. 다섯 코인이 모두 앞면이면 정신력이 크게 오른다. 정신력 12 소모."));
            // 방어(우클릭): 합 전용 방어 코인. 방어를 누른 직후(0.2초)에 막으면 패링(기본 위력 +8)
            Add(Make("GUARD", "방어", SkillSlot.Guard, 6, 2, 4, CoinMode.SingleMotion, 0f, new float[0], new string[0], new float[0], 0f, 140, 0f, desc: "막는 순간 방어 코인으로 합을 겨룬다. 누른 직후 막으면 패링."));

            foreach (var a in Resources.LoadAll<SkillData>("Skills")) if (!string.IsNullOrEmpty(a.skillId)) map[a.skillId] = a;
        }
    }
}
