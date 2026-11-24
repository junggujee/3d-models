using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 플레이어 레벨·경험치. 적을 쓰러뜨리면 EnemyCombatController.expReward 만큼 얻음.
    /// 필요 경험치 = ExpCurve.ToNext(레벨)(100 × 레벨^1.6). 레벨이 오르면 최대 체력 +12 · 공격력 +0.6(LevelMath) · 체력·스태미나 모두 회복.
    /// 레벨은 CharacterStats.level 에도 넣음(레벨 차이 코인 보정). 연출: Effects/LevelUpFx(금빛 기둥·고리) + UI/LevelUpPopup(LEVEL UP · 오른 능력치).
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class Experience : MonoBehaviour
    {
        public static Experience Instance { get; private set; }
        [Min(1)] public int level = 1; [Min(0)] public long exp = 0; [Min(1)] public int maxLevel = 99;
        public float healthPerLevel = LevelMath.HealthPerLevel; [Tooltip("레벨당 공격력 +")] public float attackPerLevel = LevelMath.AttackPerLevel;
        public long ToNext => ExpCurve.ToNext(level);
        public float Ratio => ToNext > 0 ? Mathf.Clamp01((float)exp / ToNext) : 0f;
        /// <summary>(얻은 양)</summary>
        public event Action<long> Gained; public event Action<int> LeveledUp;
        /// <summary>(이전 레벨, 새 레벨) — 한 번에 여러 레벨이 오를 수 있음.</summary>
        public event Action<int, int> LevelJumped;
        CharacterStats stats;
        void Awake() { Instance = this; stats = GetComponent<CharacterStats>(); Refresh(); }
        /// <summary>레벨에 따른 공격력을 CharacterStats 에 반영(불러오기로 레벨을 바꾼 뒤에도 부름).</summary>
        public void Refresh() { if (stats != null) { stats.levelAttack = EquipmentMath.LevelAttack(level, attackPerLevel); stats.level = level; } }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Gain(long amount)
        {
            if (amount <= 0 || stats == null || !stats.IsAlive) return;
            int before = level; int ups = ExpCurve.Add(ref level, ref exp, amount, maxLevel); Gained?.Invoke(amount);
            if (ups <= 0) return;
            stats.maxHealth += healthPerLevel * ups; stats.Heal(stats.maxHealth);   // 레벨업: 체력 모두 회복
            if (TryGetComponent<Stamina>(out var st)) st.Restore(st.max);
            Refresh(); LeveledUp?.Invoke(level); LevelJumped?.Invoke(before, level);
            LevelUpFx.Play(transform, level);
        }
    }
}
