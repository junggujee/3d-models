using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 기본 스킬 값(코드). 메뉴 Moomin ▸ "스킬 데이터 에셋 만들기" 가 이 값으로 Assets/Data/Skills/*.asset 을 만들고 플레이어·적에 연결함.
    /// 에셋이 연결돼 있으면 에셋 값을, 없으면 여기 값을 씀.
    /// </summary>
    public static class SkillLibrary
    {
        static Dictionary<string, SkillDefinition> map;
        public static SkillDefinition Get(string id) { Ensure(); return id != null && map.TryGetValue(id, out var s) ? s : null; }
        public static IEnumerable<SkillDefinition> All { get { Ensure(); return map.Values; } }

        static SkillDefinition S(string id, string name, int b, int c, int n, float mult, float cd, int sanity, float interval, float hit, float recovery, string[] anims,
                                 float range, float angle, float kb, float windup = 0f, float lunge = 0f, CoinEffect fx = CoinEffect.None, bool unblockable = false, string desc = "")
        {
            var s = ScriptableObject.CreateInstance<SkillDefinition>(); s.name = id;
            s.skillId = id; s.skillName = name; s.basePower = b; s.coinPower = c; s.coinCount = n; s.damageMultiplier = mult; s.cooldown = cd; s.sanityCost = sanity;
            s.attackInterval = interval; s.hitTime = hit; s.recovery = recovery; s.attackAnimations = anims; s.range = range; s.angle = angle; s.knockback = kb;
            s.windup = windup; s.lunge = lunge; s.coinEffect = fx; s.unblockable = unblockable; s.description = desc; return s;
        }

        static void Ensure()
        {
            if (map != null) return; map = new Dictionary<string, SkillDefinition>();
            void Look(string id, SkillAttribute a, SkillMotif m) { if (map.TryGetValue(id, out var s)) { s.attribute = a; s.motif = m; } }
            void OnHit(string id, StatusType t, int n) { if (map.TryGetValue(id, out var s)) { s.onHitStatus = t; s.onHitStacks = n; } }
            void Add(SkillDefinition s) => map[s.skillId] = s;
            string[] A1 = { "Attack1" }, A2 = { "Attack2" }, A23 = { "Attack2", "Attack3" }, A123 = { "Attack1", "Attack2", "Attack3" }, A5 = { "Attack1", "Attack2", "Attack1", "Attack2", "Attack3" }, A33 = { "Attack3", "Attack3" };
            // ── 플레이어: 일반 공격(좌클릭 3연타)
            Add(S("BASIC_1", "베기", 8, 3, 1, 1f, 0f, 0, 0.8f, 0.35f, 0.25f, A1, 1.9f, 110, 4f));
            Add(S("BASIC_2", "되베기", 8, 4, 1, 1f, 0f, 0, 0.8f, 0.35f, 0.25f, A2, 1.9f, 110, 5f));
            Add(S("BASIC_3", "이어 베기", 9, 3, 2, 1f, 0f, 0, 0.45f, 0.35f, 0.35f, A23, 2.2f, 140, 7f));
            // ── 플레이어: 스킬(명세 예시: 연속 베기 기본 10, 코인 +3, 3코인, 쿨 5초)
            Add(S("SKILL_001", "연속 베기", 10, 3, 3, 1.0f, 5f, 0, 0.5f, 0.35f, 0.35f, A123, 2.1f, 120, 5f, desc: "세 번 연속으로 벤다. 베기마다 코인을 하나씩 던진다."));
            Add(S("SKILL_002", "불꽃 찌르기", 14, 6, 2, 1.2f, 8f, 8, 0.55f, 0.4f, 0.4f, A33, 2.8f, 70, 10f, lunge: 4f, fx: CoinEffect.StaggerOnAllHeads,
                  desc: "두 번 깊게 찌른다. 둘 다 앞면이면 적이 크게 휘청인다. 정신력 8 이상일 때 사용, 8 소모."));
            Add(S("SKILL_003", "별빛 난무", 12, 5, 5, 1.3f, 30f, 15, 0.42f, 0.33f, 0.5f, A5, 3.2f, 360, 8f, fx: CoinEffect.SanityOnAllHeads,
                  desc: "주위를 다섯 번 베어 낸다. 다섯 번 모두 앞면이면 정신력 +10. 정신력 15 이상일 때 사용, 15 소모."));
            // ── 인간형 적: 잠꾸러기 곰(애니메이션 있음 — hitTime 을 클립 타격 프레임에 맞춤: Attack1/2 0.35초, Attack3 0.43초)
            Add(S("HUMAN_JAB", "솜주먹 연타", 6, 3, 3, 0.6f, 2.5f, 0, 0.4f, 0.35f, 0.7f, new[] { "Attack1", "Attack2", "Attack1" }, 1.7f, 90, 3f, windup: 0.45f, lunge: 2f));
            Add(S("HUMAN_SLAM", "베개 내려찍기", 10, 4, 2, 0.8f, 5f, 0, 0.6f, 0.43f, 1.0f, new[] { "Attack3" }, 2.0f, 140, 7f, windup: 0.85f, lunge: 1.5f));
            Add(S("HUMAN_RUSH", "졸음 돌진", 9, 4, 1, 0.8f, 7f, 0, 0.5f, 0.35f, 1.1f, new[] { "Attack1" }, 5.0f, 60, 8f, windup: 0.75f, lunge: 10f));
            // ── 적(같은 SkillExecutor 사용). windup = 바닥 경고 시간
            Add(S("ENEMY_BUMP", "몸통 박치기", 7, 3, 2, 0.7f, 1.2f, 0, 0.35f, 0.12f, 0.8f, A1, 1.6f, 100, 4f, windup: 0.65f, lunge: 5f));
            Add(S("ENEMY_SLAM", "내려찍기", 9, 4, 2, 0.8f, 4f, 0, 0.45f, 0.15f, 1.1f, A1, 2.0f, 360, 7f, windup: 1.0f, lunge: 1f));
            Add(S("BOSS_LUNGE", "돌진", 13, 4, 2, 0.8f, 5f, 0, 0.45f, 0.15f, 1.0f, A1, 6f, 70, 8f, windup: 0.8f, lunge: 12f));
            Add(S("BOSS_FLURRY", "난타", 9, 3, 4, 0.6f, 6f, 0, 0.3f, 0.12f, 0.9f, A1, 2.4f, 120, 3f, windup: 0.7f, lunge: 3f));
            Add(S("BOSS_BREAK", "방어 부수기", 18, 6, 1, 1.0f, 8f, 0, 0.5f, 0.15f, 1.2f, A1, 3.2f, 120, 9f, windup: 1.2f, lunge: 3f, unblockable: true));
            // 속성(색)·일러스트 동작
            Look("BASIC_1", SkillAttribute.Yellow, SkillMotif.Slash); Look("BASIC_2", SkillAttribute.Green, SkillMotif.Slash); Look("BASIC_3", SkillAttribute.Blue, SkillMotif.Flurry);
            Look("SKILL_001", SkillAttribute.Red, SkillMotif.Flurry); Look("SKILL_002", SkillAttribute.Orange, SkillMotif.Thrust); Look("SKILL_003", SkillAttribute.Purple, SkillMotif.Slash);
            Look("HUMAN_JAB", SkillAttribute.Yellow, SkillMotif.Flurry); Look("HUMAN_SLAM", SkillAttribute.Blue, SkillMotif.Smash); Look("HUMAN_RUSH", SkillAttribute.Green, SkillMotif.Rush);
            Look("ENEMY_BUMP", SkillAttribute.Black, SkillMotif.Rush); Look("ENEMY_SLAM", SkillAttribute.Purple, SkillMotif.Smash);
            // 적중 시 상태이상
            OnHit("SKILL_001", StatusType.Bleed, 1); OnHit("SKILL_002", StatusType.Burn, 2); OnHit("SKILL_003", StatusType.Tremor, 1);
            OnHit("HUMAN_SLAM", StatusType.Tremor, 2); OnHit("ENEMY_SLAM", StatusType.Tremor, 1); OnHit("BOSS_LUNGE", StatusType.Bleed, 2); OnHit("BOSS_FLURRY", StatusType.Burn, 1);
            Look("BOSS_LUNGE", SkillAttribute.Red, SkillMotif.Rush); Look("BOSS_FLURRY", SkillAttribute.Orange, SkillMotif.Flurry); Look("BOSS_BREAK", SkillAttribute.Black, SkillMotif.Smash);
        }
    }
}
