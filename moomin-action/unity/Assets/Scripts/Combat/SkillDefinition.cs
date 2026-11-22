using UnityEngine;

namespace MoominAction
{
    public enum CoinEffect { None, StaggerOnAllHeads, SanityOnAllHeads }
    /// <summary>스킬 속성(색). 스킬 패널·슬롯·이펙트 색에 쓰임.</summary>
    public enum SkillAttribute { Red, Blue, Yellow, Green, Orange, Purple, Black }
    /// <summary>일러스트를 따로 넣지 않았을 때 코드로 그리는 스케치의 동작 종류.</summary>
    public enum SkillMotif { Slash, Thrust, Smash, Rush, Flurry }

    public static class SkillAttributes
    {
        public static readonly string[] Names = { "빨강", "파랑", "노랑", "초록", "주황", "보라", "검정" };
        public static Color ColorOf(SkillAttribute a) => a switch
        {
            SkillAttribute.Red => new Color(0.86f, 0.16f, 0.2f), SkillAttribute.Blue => new Color(0.2f, 0.45f, 0.95f), SkillAttribute.Yellow => new Color(0.95f, 0.78f, 0.15f),
            SkillAttribute.Green => new Color(0.3f, 0.75f, 0.3f), SkillAttribute.Orange => new Color(0.98f, 0.5f, 0.12f), SkillAttribute.Purple => new Color(0.6f, 0.3f, 0.9f),
            _ => new Color(0.32f, 0.3f, 0.34f),
        };

        // ── 상성: 색상환 순서로 다음 색을 이김(빨강 > 주황 > 노랑 > 초록 > 파랑 > 보라 > 빨강). 검정은 상성 없음.
        public const float AdvantageDamage = 1.25f, DisadvantageDamage = 0.8f; public const int AdvantageClashBonus = 2;
        static readonly SkillAttribute[] Wheel = { SkillAttribute.Red, SkillAttribute.Orange, SkillAttribute.Yellow, SkillAttribute.Green, SkillAttribute.Blue, SkillAttribute.Purple };
        static int WheelIndex(SkillAttribute a) => System.Array.IndexOf(Wheel, a);
        /// <summary>a 가 이기는 속성(검정이면 null).</summary>
        public static SkillAttribute? Beats(SkillAttribute a) { int i = WheelIndex(a); return i < 0 ? null : Wheel[(i + 1) % Wheel.Length]; }
        /// <summary>a 에게 지는 속성 = a 의 약점(검정이면 null).</summary>
        public static SkillAttribute? WeakTo(SkillAttribute a) { int i = WheelIndex(a); return i < 0 ? null : Wheel[(i + Wheel.Length - 1) % Wheel.Length]; }
        /// <summary>공격 속성 vs 방어 속성: +1 유리, -1 불리, 0 보통.</summary>
        public static int Affinity(SkillAttribute attacker, SkillAttribute defender) => Beats(attacker) == defender ? 1 : Beats(defender) == attacker ? -1 : 0;
        public static float DamageMultiplier(int affinity) => affinity > 0 ? AdvantageDamage : affinity < 0 ? DisadvantageDamage : 1f;
        /// <summary>방어 쪽 속성: 공격 중이면 그 스킬의 속성, 아니면 몸 속성.</summary>
        public static SkillAttribute DefenderAttribute(CharacterStats target)
        {
            var ex = target.GetComponent<SkillExecutor>(); return ex != null && ex.IsExecuting && ex.CurrentSkill != null ? ex.CurrentSkill.attribute : target.attribute;
        }
        public static string WeaknessText(SkillAttribute a) { var w = WeakTo(a); return w.HasValue ? Names[(int)w.Value] + "에 약함" : "상성 없음"; }
    }

    /// <summary>
    /// 스킬 데이터(명세 v2 3-2). Project 창 ▸ Create ▸ Combat ▸ Skill Definition 으로 만들고 Inspector 에서 수정.
    /// 코인 하나 = 공격 한 번: 공격 i 는 windup + i×attackInterval 에 시작, 그 뒤 hitTime 에 코인 판정(애니메이션 이벤트 OnCoinAttack 이 오면 그 시점).
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Combat/Skill Definition")]
    public class SkillDefinition : ScriptableObject
    {
        public string skillId = "SKILL_000";
        public string skillName = "스킬";

        [Header("Look")] public SkillAttribute attribute = SkillAttribute.Red; public SkillMotif motif = SkillMotif.Slash;
        [Tooltip("스킬 일러스트(비우면 motif 로 스케치를 자동 생성)")] public Sprite illustration;
        [Header("Coin Settings")] public int basePower = 10; public int coinPower = 3; public int coinCount = 3;
        [Header("Attack Settings")] public float damageMultiplier = 1.0f; public float cooldown = 5f; public int sanityCost = 0;
        [Header("Clash Settings")] public bool clashEnabled = true; public bool unblockable;

        [Header("Timing (초)")]
        [Tooltip("첫 공격 전 준비 시간(적의 공격 예고)")] public float windup = 0f;
        [Tooltip("공격(코인) 사이 간격")] public float attackInterval = 0.5f;
        [Tooltip("공격 시작부터 타격(코인 판정)까지")] public float hitTime = 0.35f;
        [Tooltip("히트박스가 열려 있는 시간")] public float activeWindow = 0.12f;
        [Tooltip("마지막 공격 뒤 회복(이 구간은 회피·다음 공격으로 취소 가능)")] public float recovery = 0.3f;
        [Tooltip("코인 i 에 쓸 애니메이터 상태(개수보다 적으면 반복)")] public string[] attackAnimations = { "Attack1", "Attack2", "Attack3" };

        [Header("Status On Hit (강도/횟수 — 코인 앞면·뒷면 따로)")]
        public StatusAmount bleedOnHeads, bleedOnTails;
        public StatusAmount burnOnHeads, burnOnTails;
        public StatusAmount sinkingOnHeads, sinkingOnTails;
        public StatusAmount tremorOnHeads, tremorOnTails;
        [Tooltip("적중 시 대상의 진동을 폭발시켜 경직 게이지 피해")] public bool tremorBurstEnabled;
        [Header("Coin Power Effects (강도/횟수 — 횟수 = 영향받는 코인 수)")]
        [Tooltip("적중 시 '자신'에게: 앞면 코인 위력 +강도")] public StatusAmount powerUpOnHeads, powerUpOnTails;
        [Tooltip("적중 시 상대에게: 앞면 코인 위력 -강도")] public StatusAmount powerDownOnHeads, powerDownOnTails;
        [Tooltip("적중 시 상대에게: 코인 위력 0 (강도 무시, 횟수만)")] public StatusAmount paralysisOnHeads, paralysisOnTails;
        [Header("Energy")] [Tooltip("사용에 필요한(소비하는) 에너지")] [Min(0)] public int energyCost; [Tooltip("공격이 명중할 때마다 얻는 에너지(기본 명중 보너스에 더해짐)")] [Min(0)] public int energyGain;
        [Header("Hit")] public float range = 2f; public float angle = 120f; public float knockback = 4f; [Tooltip("공격마다 앞으로 미끄러지는 속도")] public float lunge = 0f;
        public CoinEffect coinEffect = CoinEffect.None;
        [TextArea] public string description;

        public StatusAmount StatusOn(StatusType t, bool heads) => t switch
        {
            StatusType.Bleed => heads ? bleedOnHeads : bleedOnTails, StatusType.Burn => heads ? burnOnHeads : burnOnTails,
            StatusType.Sinking => heads ? sinkingOnHeads : sinkingOnTails, StatusType.Tremor => heads ? tremorOnHeads : tremorOnTails,
            StatusType.PowerUp => heads ? powerUpOnHeads : powerUpOnTails, StatusType.PowerDown => heads ? powerDownOnHeads : powerDownOnTails,
            StatusType.Paralysis => heads ? paralysisOnHeads : paralysisOnTails, _ => default,
        };
        public bool InflictsStatus(StatusType t) => !StatusOn(t, true).IsEmpty || !StatusOn(t, false).IsEmpty;
        public bool HasAnyStatus { get { foreach (var t in StatusInfo.All) if (InflictsStatus(t)) return true; return false; } }
        public int MinPower => basePower; public int MaxPower => basePower + coinPower;
        public float ExpectedPower(int sanity) => CoinMath.ExpectedPower(basePower, coinPower, sanity);
        public float Duration => windup + (coinCount - 1) * attackInterval + hitTime + activeWindow + recovery;
    }
}
