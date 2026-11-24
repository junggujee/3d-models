using System;
using UnityEngine;

namespace MoominAction
{
    public interface IDamageable { void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockback); bool IsDead { get; } }
    public enum Team { Player, Enemy }
    public enum SanityState { Focus, Stable, Unstable, Breakdown }

    /// <summary>
    /// 모든 캐릭터(플레이어·적)가 공통으로 갖는 체력·정신력·방어 상태(명세 v2 3-1).
    /// 정신력 -45~+45 → 코인 앞면 확률 Clamp(0.5 + S/100, 0.05, 0.95).
    /// 정신력 상태: 집중(+25~) 주는 피해 +10% · 안정 · 불안정(-25~-39) 스킬 부가 효과 봉인 · 붕괴(-40~) 받는 피해 +25%.
    /// </summary>
    public class CharacterStats : MonoBehaviour, IDamageable
    {
        public string displayName = "민미"; [Tooltip("이름 옆 별칭(예: 지상 최대의 끼룩이)")] public string epithet = ""; public Team team = Team.Player;
        [Tooltip("몸 속성(공격 중이 아닐 때 맞는 쪽 상성). 검정 = 상성 없음")] public SkillAttribute attribute = SkillAttribute.Black;
        [Tooltip("레벨(플레이어 = Experience, 적 = 웨이브로 정함, 펫 = 펫 레벨). 상대와 3레벨 차이마다 코인 위력 ±1(최대 ±3) — LevelMath")] [Min(1)] public int level = 1;
        [Header("Health")] public float maxHealth = 100f; public float currentHealth = 100f;
        [Header("Attack")] [Tooltip("기본 공격력. 피해 = 공격력 × (최종 위력 ÷ 10) × 배율 (EquipmentMath.Damage) — 10 이면 예전 공식과 같음")] public float attack = 10f;
        /// <summary>레벨(Experience)·장비(Inventory)가 더하는 값. 장비 체력은 maxHealth 에 이미 더해져 있고 equipHealth 로 기억(저장 시 뺌).</summary>
        [NonSerialized] public float levelAttack, equipAttack, equipHealth, equipDamageReduction, equipGuard, equipMoveSpeed; [NonSerialized] public int equipFinalPower, equipEvadePower;
        /// <summary>최종 공격력 = 기본 + 레벨 + 장비.</summary>
        public float Attack => attack + levelAttack + equipAttack;
        [Header("Sanity")] [Tooltip("끄면 침잠이 정신력 대신 StatusEffectManager.OnSinkingWithoutSanity 효과를 줌")] public bool hasSanity = true; public int maxSanity = 45; public int minSanity = -45; public int currentSanity = 0; public float sanityRecoverPerSecond = 3f;
        [Header("Defense")] public float invulnerableTime = 0.05f; [Tooltip("DefenseController 가 없을 때만 쓰는 가드 피해 배율")] public float guardDamageMultiplier = 0.5f; public float parryWindow = 0.2f; public float strongHitDamage = 14f;

        [Header("Stagger Gauge (체력과 별개)")]
        [Min(1)] public float maxStagger = 100f; [Tooltip("게이지가 가득 차면 이 시간 동안 경직(이동·공격 불가)")] public float staggerDuration = 3f;
        [Tooltip("경직 중 받는 피해 배율")] public float staggerVulnerable = 1.5f; [Tooltip("마지막 경직 피해 후 회복 시작까지(초)")] public float staggerRecoverDelay = 4f; public float staggerRecoverPerSecond = 10f;
        public readonly StaggerGauge StaggerGauge = new();
        /// <summary>(경직 피해량, 깨졌는가)</summary>
        public event Action<float, bool> StaggerDamaged;
        public static event Action<CharacterStats> AnyStaggerBroken;
        public bool IsStaggerBroken => Time.time < staggerBrokenUntil;
        float staggerBrokenUntil = -99f;

        // 정신력 변화 규칙(초기 밸런스 값)
        public const int HitLanded = 1, ClashWin = 5, ClashLose = -5, StrongHitTaken = -4, DodgeSuccess = 1, GuardBlock = 1, ParrySuccess = 5;

        [NonSerialized] public bool Invulnerable;          // 회피 무적
        [NonSerialized] public bool Guarding; [NonSerialized] public float GuardStartTime;
        [NonSerialized] public bool inCombat = true;
        /// <summary>합 연출 중: 이동·입력·AI 정지(ClashManager 가 직접 움직임).</summary>
        [NonSerialized] public bool InClash;
        public bool IsAlive => currentHealth > 0; public bool IsDead => !IsAlive;
        public float HealthRatio => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsParryWindow => Guarding && Time.time - GuardStartTime <= parryWindow;

        public event Action<float, Vector3, Vector3> Damaged; public event Action<float> Healed; public event Action Died; public event Action Evaded;
        public event Action<int, int, string> SanityChanged;   // (이전, 현재, 이유)
        float lastHit = -99f, lastEvade = -99f, vulnerableUntil, vulnerableMult = 1f, recoverAcc;

        void Awake() { ConfigureStagger(); currentHealth = maxHealth; currentSanity = Mathf.Clamp(currentSanity, minSanity, maxSanity); }
        public void ResetStats() { currentHealth = maxHealth; Invulnerable = Guarding = false; StaggerGauge.Reset(); }
        void ConfigureStagger() { StaggerGauge.Max = maxStagger; StaggerGauge.RecoverDelay = staggerRecoverDelay; StaggerGauge.RecoverPerSecond = staggerRecoverPerSecond; }
        void OnValidate() { ConfigureStagger(); }

        // ── 경직 게이지
        /// <summary>경직 게이지 피해. 가득 차면 staggerDuration 동안 경직(ICombatant.Stagger) → true. 이미 경직 중이면 쌓지 않음.</summary>
        public bool AddStagger(float amount)
        {
            if (!IsAlive || amount <= 0f || IsStaggerBroken) return false;
            bool broke = StaggerGauge.Add(amount);
            if (broke) { staggerBrokenUntil = Time.time + staggerDuration; if (TryGetComponent<ICombatant>(out var cb)) cb.Stagger(staggerDuration, staggerVulnerable); }
            StaggerDamaged?.Invoke(amount, broke); if (broke) AnyStaggerBroken?.Invoke(this);
            return broke;
        }

        // ── 체력
        public void TakeDamage(float damage) => TakeDamage(damage, transform.position + Vector3.up, Vector3.zero);
        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 knockback)
        {
            if (!IsAlive) return;
            if (Invulnerable) return;   // 연출 등 특수 무적(회피는 DefenseController 의 코인 비교로 처리)
            if (Time.time - lastHit < invulnerableTime) return;
            lastHit = Time.time; currentHealth = Mathf.Max(0, currentHealth - damage);
            if (damage >= strongHitDamage) ChangeSanity(StrongHitTaken, "강한 공격 피격");
            Damaged?.Invoke(damage, hitPoint, knockback);
            if (!IsAlive) Died?.Invoke();
        }
        /// <summary>회피 성공 알림(DefenseController): 정신력 +1, Evaded 이벤트(에너지·HUD). 같은 순간 여러 번이면 한 번만.</summary>
        public void NotifyEvaded() { if (Time.time - lastEvade < 0.05f) return; lastEvade = Time.time; ChangeSanity(DodgeSuccess, "회피"); Evaded?.Invoke(); }
        /// <summary>상태이상 등으로 체력 손실: 회피·무적 시간·경직과 무관(Damaged 이벤트 없이 HealthLost).</summary>
        public void LoseHealth(float amount, string reason = "")
        {
            if (!IsAlive || amount <= 0) return; currentHealth = Mathf.Max(0, currentHealth - amount); HealthLost?.Invoke(amount, reason);
            if (!IsAlive) Died?.Invoke();
        }
        public event Action<float, string> HealthLost;
        public void Heal(float amount) { if (!IsAlive || amount <= 0) return; float b = currentHealth; currentHealth = Mathf.Min(maxHealth, currentHealth + amount); if (currentHealth > b) Healed?.Invoke(currentHealth - b); }

        // ── 정신력
        public void ChangeSanity(int amount, string reason = "")
        {
            if (amount == 0) return; int old = currentSanity; currentSanity = Mathf.Clamp(currentSanity + amount, minSanity, maxSanity);
            if (old != currentSanity) SanityChanged?.Invoke(old, currentSanity, reason);
        }
        public float GetCoinProbability() => CoinMath.HeadsProbability(currentSanity);
        public SanityState Sanity => currentSanity >= 25 ? SanityState.Focus : currentSanity >= -24 ? SanityState.Stable : currentSanity >= -39 ? SanityState.Unstable : SanityState.Breakdown;
        public static string SanityLabel(SanityState s) => s switch { SanityState.Focus => "집중", SanityState.Stable => "안정", SanityState.Unstable => "불안정", _ => "붕괴" };
        public bool ExtraEffectsEnabled => Sanity == SanityState.Focus || Sanity == SanityState.Stable;
        public float DamageDealtMultiplier => Sanity == SanityState.Focus ? 1.1f : 1f;

        // ── 받는 피해 보정(R): 붕괴 ×1.25, 경직 중 취약 ×(설정값)
        public void SetVulnerable(float multiplier, float seconds) { vulnerableMult = multiplier; vulnerableUntil = Time.time + seconds; }
        public bool IsVulnerable => Time.time < vulnerableUntil && vulnerableMult > 1f;
        public float DamageTakenMultiplier => (Sanity == SanityState.Breakdown ? 1.25f : 1f) * (IsVulnerable ? vulnerableMult : 1f) * (1f - equipDamageReduction);

        void Update()
        {
            if (!UIState.Paused && !IsStaggerBroken) StaggerGauge.Advance(Time.deltaTime);   // 게이지 회복(피해 아님)
            if (inCombat || currentSanity == 0 || UIState.Paused || sanityRecoverPerSecond <= 0) return;   // 전투가 끝나면 0을 향해 회복
            recoverAcc += sanityRecoverPerSecond * Time.deltaTime; if (recoverAcc < 1f) return;
            int step = Mathf.FloorToInt(recoverAcc); recoverAcc -= step; ChangeSanity(currentSanity > 0 ? -Mathf.Min(step, currentSanity) : Mathf.Min(step, -currentSanity), "전투 종료");
        }
    }

    /// <summary>플레이어(CombatManager)와 적(EnemyCombatController)이 공통으로 제공 — 클래시·패링 결과를 적용할 때 사용.</summary>
    public interface ICombatant
    {
        CharacterStats Stats { get; } SkillExecutor Executor { get; } Transform transform { get; }
        void Stagger(float seconds, float vulnerable = 1f);
    }
}
