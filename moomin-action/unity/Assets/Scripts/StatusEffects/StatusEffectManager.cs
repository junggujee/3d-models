using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>상태이상 이름·색·규칙 설명(UI 공용).</summary>
    public static class StatusInfo
    {
        public static string Name(StatusType t) => t switch { StatusType.Bleed => "출혈", StatusType.Burn => "화상", StatusType.Sinking => "침잠", StatusType.Tremor => "진동", StatusType.PowerUp => "힘", StatusType.PowerDown => "위력 감소", StatusType.Paralysis => "마비", _ => "" };
        public static Color ColorOf(StatusType t) => t switch
        {
            StatusType.Bleed => new Color(0.86f, 0.2f, 0.24f), StatusType.Burn => new Color(1f, 0.55f, 0.15f),
            StatusType.Sinking => new Color(0.29f, 0.39f, 0.85f), StatusType.Tremor => new Color(1f, 0.69f, 0.16f),
            StatusType.PowerUp => new Color(1f, 0.84f, 0.42f), StatusType.PowerDown => new Color(0.62f, 0.66f, 0.78f), StatusType.Paralysis => new Color(0.82f, 0.9f, 0.3f), _ => Color.white,
        };
        public static string Rule(StatusType t) => t switch
        {
            StatusType.Bleed => "공격 코인을 쓸 때마다 강도만큼 체력 손실, 횟수 -1",
            StatusType.Burn => "일정 시간(기본 2초)마다 강도만큼 체력 손실, 횟수 -1",
            StatusType.Sinking => "공격에 맞을 때 강도만큼 정신력 손실, 횟수 -1",
            StatusType.Tremor => "쌓아 두었다가 '진동 폭발' 스킬에 맞으면 경직 게이지 피해, 횟수 -1",
            StatusType.PowerUp => "코인을 던질 때 앞면 코인 위력 +강도, 횟수 -1",
            StatusType.PowerDown => "코인을 던질 때 앞면 코인 위력 -강도, 횟수 -1",
            StatusType.Paralysis => "코인을 던질 때 코인 위력 0, 횟수 -1",
            _ => "",
        };
        public static string Hex(StatusType t) => ColorUtility.ToHtmlStringRGB(ColorOf(t));
        public static readonly StatusType[] All = { StatusType.Bleed, StatusType.Burn, StatusType.Sinking, StatusType.Tremor, StatusType.PowerUp, StatusType.PowerDown, StatusType.Paralysis };
        /// <summary>코인 위력에 영향을 주는 효과(스킬 패널 위쪽 3칸).</summary>
        public static readonly StatusType[] Coin = { StatusType.PowerUp, StatusType.PowerDown, StatusType.Paralysis };
        /// <summary>스킬이 적중했을 때 상대가 아니라 '자신'에게 주는 효과.</summary>
        public static bool IsSelf(StatusType t) => t == StatusType.PowerUp;
    }

    /// <summary>
    /// 캐릭터 한 명의 상태이상(플레이어·적 공용). 발동은 모두 실제 전투 사건에서:
    ///  · 출혈 = SkillExecutor.OnCoinAttack → OnCoinUsed      · 침잠 = CombatResolver.ApplyHit → OnHitTaken
    ///  · 진동 폭발 = CombatResolver.ApplyHit(진동 폭발 스킬) → TryTremorBurst
    ///  · 화상 = 시간 — Update 는 시간만 누적하고, 간격(burnInterval)을 넘는 순간에만 피해(매 프레임 피해 없음).
    /// 같은 공격(AttackContext.id)에서는 각 효과가 한 번만 발동.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterStats))]
    public class StatusEffectManager : MonoBehaviour
    {
        [Header("Limits")] [Min(1)] public int maxPotency = 99; [Min(1)] public int maxCount = 99;
        [Header("Burn")] [Tooltip("화상 발동 간격(초)")] [Min(0.1f)] public float burnInterval = 2f;
        [Header("Tremor Burst")] [Tooltip("경직 피해 = 강도 × 이 값 + 횟수 × perCount")] public int tremorStaggerPerPotency = 4; public int tremorStaggerPerCount = 2;
        [Header("Duration (초, 0 = 횟수가 다 될 때까지)")] public float bleedDuration = 0f, burnDuration = 0f, sinkingDuration = 0f, tremorDuration = 0f;
        [Tooltip("힘·위력 감소·마비 지속시간(초). 실시간 전투라 코인을 안 던지면 영원히 남지 않도록 기본 12초")] public float coinModifierDuration = 12f;

        public readonly BleedEffect Bleed = new(); public readonly BurnEffect Burn = new(); public readonly SinkingEffect Sinking = new(); public readonly TremorEffect Tremor = new();
        public readonly PowerUpEffect PowerUp = new(); public readonly PowerDownEffect PowerDown = new(); public readonly ParalysisEffect Paralysis = new();
        /// <summary>강도·횟수가 바뀔 때마다(UI 즉시 갱신용).</summary>
        public event Action Changed;
        /// <summary>효과 발동: (종류, 수치 — 체력/정신력/경직 피해).</summary>
        public event Action<StatusType, int> Triggered;
        public static event Action<StatusEffectManager, StatusType, int> AnyTriggered;

        CharacterStats stats;
        void Awake() { stats = GetComponent<CharacterStats>(); Configure(); stats.Died += ClearAll; }
        void OnValidate() { if (Application.isPlaying) Configure(); }
        void Configure()
        {
            foreach (var e in Effects) { e.MaxPotency = maxPotency; e.MaxCount = maxCount; }
            Burn.Interval = burnInterval; Tremor.PerPotency = tremorStaggerPerPotency; Tremor.PerCount = tremorStaggerPerCount;
            Bleed.Duration = bleedDuration; Burn.Duration = burnDuration; Sinking.Duration = sinkingDuration; Tremor.Duration = tremorDuration;
            PowerUp.Duration = PowerDown.Duration = Paralysis.Duration = coinModifierDuration;
        }
        StatusEffect[] effects; public StatusEffect[] Effects => effects ??= new StatusEffect[] { Bleed, Burn, Sinking, Tremor, PowerUp, PowerDown, Paralysis };
        public StatusEffect Get(StatusType t) => t switch { StatusType.Bleed => Bleed, StatusType.Burn => Burn, StatusType.Sinking => Sinking, StatusType.Tremor => Tremor,
            StatusType.PowerUp => PowerUp, StatusType.PowerDown => PowerDown, StatusType.Paralysis => Paralysis, _ => null };
        public bool HasAny { get { foreach (var e in Effects) if (e.Active) return true; return false; } }

        // ── 부여 ────────────────────────────────
        public void Apply(StatusType t, StatusAmount a) { var e = Get(t); if (e == null || a.IsEmpty || !stats.IsAlive) return; e.Add(a); Changed?.Invoke(); }
        /// <summary>스킬 적중 시 코인 결과(앞/뒷면)에 맞는 부여량을 적용. self = true 면 '자신에게' 효과(힘)만, false 면 상대에게 주는 효과만.</summary>
        public void ApplyFromSkill(SkillDefinition s, bool heads, bool self = false)
        {
            if (s == null || !stats.IsAlive) return; bool any = false;
            foreach (var t in StatusInfo.All) { if (StatusInfo.IsSelf(t) != self) continue; var a = s.StatusOn(t, heads); if (a.IsEmpty) continue; Get(t).Add(a); any = true; }
            if (any) Changed?.Invoke();
        }

        /// <summary>코인을 던질 때(위력 계산 전): 힘·위력 감소·마비를 한 번씩 소모하고 보정값을 돌려줌.</summary>
        public CoinModifiers UseCoinModifiers(int attackId)
        {
            var m = new CoinModifiers { powerUp = PowerUp.UseOnCoin(attackId), powerDown = PowerDown.UseOnCoin(attackId), paralyzed = Paralysis.UseOnCoin(attackId) > 0 };
            if (m.Any) Changed?.Invoke(); return m;
        }
        public void ClearAll() { foreach (var e in Effects) e.Clear(); Changed?.Invoke(); }

        // ── 발동(전투 사건) ─────────────────────
        /// <summary>출혈: 이 캐릭터가 공격 코인을 사용함.</summary>
        public void OnCoinUsed(AttackContext ctx)
        {
            int dmg = Bleed.OnOwnerCoinUsed(ctx.id); if (dmg <= 0) return;
            stats.LoseHealth(dmg, "출혈"); Fire(StatusType.Bleed, dmg);
        }
        /// <summary>침잠: 이 캐릭터가 공격에 맞음.</summary>
        public void OnHitTaken(AttackContext ctx)
        {
            int p = Sinking.OnOwnerHit(ctx.id); if (p <= 0) return;
            if (stats.hasSanity) stats.ChangeSanity(-p, "침잠"); else OnSinkingWithoutSanity(p);
            Fire(StatusType.Sinking, p);
        }
        /// <summary>정신력이 없는 대상의 침잠 효과(기본: 같은 양의 체력 손실). 다른 효과가 필요하면 상속해서 바꿈.</summary>
        protected virtual void OnSinkingWithoutSanity(int potency) => stats.LoseHealth(potency, "침잠");
        /// <summary>진동 폭발: 경직 게이지 피해. 반환 = 경직이 깨졌는가.</summary>
        public bool TryTremorBurst(AttackContext ctx, out int staggerDamage)
        {
            staggerDamage = Tremor.Burst(ctx.id); if (staggerDamage <= 0) return false;
            bool broke = stats.AddStagger(staggerDamage); Fire(StatusType.Tremor, staggerDamage); return broke;
        }

        void Fire(StatusType t, int v) { Triggered?.Invoke(t, v); AnyTriggered?.Invoke(this, t, v); Changed?.Invoke(); }

        // ── 시간: 화상 간격·지속시간만(누적 후 간격을 넘을 때만 피해) ──
        void Update()
        {
            if (UIState.Paused || !stats.IsAlive) return;
            float dt = Time.deltaTime; bool changed = false;
            int burn = Burn.Advance(dt); if (burn > 0) { stats.LoseHealth(burn, "화상"); Fire(StatusType.Burn, burn); }
            foreach (var e in Effects) changed |= e.TickDuration(dt);
            if (changed) Changed?.Invoke();
        }

        /// <summary>"출혈 3/2  화상 2/1" 처럼 색 입힌 요약(UI).</summary>
        public string Summary()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var e in Effects) if (e.Active) sb.Append($"<color=#{StatusInfo.Hex(e.Type)}>{StatusInfo.Name(e.Type)} {(e.Type == StatusType.Paralysis ? e.Count.ToString() : e.Potency + "/" + e.Count)}</color>  ");
            return sb.ToString().TrimEnd();
        }
    }
}
