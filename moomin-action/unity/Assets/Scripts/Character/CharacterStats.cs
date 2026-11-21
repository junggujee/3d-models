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
        public string displayName = "민미"; public Team team = Team.Player;
        [Tooltip("몸 속성(공격 중이 아닐 때 맞는 쪽 상성). 검정 = 상성 없음")] public SkillAttribute attribute = SkillAttribute.Black;
        [Header("Health")] public float maxHealth = 100f; public float currentHealth = 100f;
        [Header("Sanity")] public int maxSanity = 45; public int minSanity = -45; public int currentSanity = 0; public float sanityRecoverPerSecond = 3f;
        [Header("Defense")] public float invulnerableTime = 0.05f; public float guardDamageMultiplier = 0.3f; public float parryWindow = 0.2f; public float strongHitDamage = 14f;

        // 정신력 변화 규칙(초기 밸런스 값)
        public const int HitLanded = 1, ClashWin = 5, ClashLose = -5, StrongHitTaken = -4, DodgeSuccess = 1, GuardBlock = 1, ParrySuccess = 5;

        [NonSerialized] public bool Invulnerable;          // 회피 무적
        [NonSerialized] public bool Guarding; [NonSerialized] public float GuardStartTime;
        [NonSerialized] public bool inCombat = true;
        public bool IsAlive => currentHealth > 0; public bool IsDead => !IsAlive;
        public float HealthRatio => maxHealth > 0 ? currentHealth / maxHealth : 0f;
        public bool IsParryWindow => Guarding && Time.time - GuardStartTime <= parryWindow;

        public event Action<float, Vector3, Vector3> Damaged; public event Action<float> Healed; public event Action Died; public event Action Evaded;
        public event Action<int, int, string> SanityChanged;   // (이전, 현재, 이유)
        float lastHit = -99f, lastEvade = -99f, vulnerableUntil, vulnerableMult = 1f, recoverAcc;

        void Awake() { currentHealth = maxHealth; currentSanity = Mathf.Clamp(currentSanity, minSanity, maxSanity); }
        public void ResetStats() { currentHealth = maxHealth; Invulnerable = Guarding = false; }

        // ── 체력
        public void TakeDamage(float damage) => TakeDamage(damage, transform.position + Vector3.up, Vector3.zero);
        public void TakeDamage(float damage, Vector3 hitPoint, Vector3 knockback)
        {
            if (!IsAlive) return;
            if (Invulnerable) { if (Time.time - lastEvade > 0.3f) { lastEvade = Time.time; ChangeSanity(DodgeSuccess, "회피"); Evaded?.Invoke(); } return; }
            if (Time.time - lastHit < invulnerableTime) return;
            lastHit = Time.time; currentHealth = Mathf.Max(0, currentHealth - damage);
            if (damage >= strongHitDamage) ChangeSanity(StrongHitTaken, "강한 공격 피격");
            Damaged?.Invoke(damage, hitPoint, knockback);
            if (!IsAlive) Died?.Invoke();
        }
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
        public float DamageTakenMultiplier => (Sanity == SanityState.Breakdown ? 1.25f : 1f) * (IsVulnerable ? vulnerableMult : 1f);

        void Update()
        {
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
