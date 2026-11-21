using UnityEngine;

namespace MoominAction
{
    public enum SkillSlot { Basic, Skill1, Skill2, Ultimate, Guard }
    /// <summary>PerHit = 코인 하나당 공격 한 번(3코인 = 3연속 공격), SingleMotion = 한 번의 공격 모션에서 코인 여러 개를 판정.</summary>
    public enum CoinMode { PerHit, SingleMotion }
    public enum CoinEffect { BonusPower, StaggerOnAllHeads, SanityOnAllHeads }

    /// <summary>
    /// 스킬 데이터(설계서 6장). 수치만 바꾸면 게임에 반영됨. 에셋으로 만들려면 Project 창 ▸ Create ▸ Moomin ▸ Skill,
    /// 또는 메뉴 Moomin ▸ "스킬 데이터 에셋 만들기" 로 기본 스킬을 Assets/Resources/Skills 에 생성(그 파일을 고치면 덮어씀).
    /// </summary>
    [CreateAssetMenu(menuName = "Moomin/Skill", fileName = "Skill")]
    public class SkillData : ScriptableObject
    {
        public string skillId = "SKILL_000", skillName = "스킬";
        public SkillSlot slot;
        [Header("코인")] public int basePower = 10; public int coinCount = 3; public int coinPower = 3; public CoinMode mode = CoinMode.PerHit; public CoinEffect coinEffect = CoinEffect.BonusPower;
        [Header("피해·비용")] public float damageMultiplier = 1f; public int sanityCost = 0; public float cooldown = 5f;
        [Header("동작(초)")] public float duration = 1.2f; public float[] hitTimes = { 0.37f }; public string[] animStates = { "Attack1" }; public float[] animTimes = { 0f };
        [Header("판정")] public float range = 2f, angle = 120f, knockback = 4f; public bool clashEnabled = true; public bool special;
        [TextArea] public string description;

        public int MaxPower => basePower + coinCount * coinPower;
        public float ExpectedPower(int sanity) => CoinMath.ExpectedPower(basePower, coinCount, coinPower, sanity);
    }
}
