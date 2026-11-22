using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 플레이어 레벨·경험치. 적을 쓰러뜨리면 EnemyCombatController.expReward 만큼 얻음.
    /// 필요 경험치 = ExpCurve.ToNext(레벨)(100 × 레벨^1.6). 레벨이 오르면 최대 체력 +healthPerLevel, 늘어난 만큼 회복.
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class Experience : MonoBehaviour
    {
        public static Experience Instance { get; private set; }
        [Min(1)] public int level = 1; [Min(0)] public long exp = 0; [Min(1)] public int maxLevel = 99;
        public float healthPerLevel = 10f;
        public long ToNext => ExpCurve.ToNext(level);
        public float Ratio => ToNext > 0 ? Mathf.Clamp01((float)exp / ToNext) : 0f;
        /// <summary>(얻은 양)</summary>
        public event Action<long> Gained; public event Action<int> LeveledUp;
        CharacterStats stats;
        void Awake() { Instance = this; stats = GetComponent<CharacterStats>(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Gain(long amount)
        {
            if (amount <= 0 || stats == null || !stats.IsAlive) return;
            int ups = ExpCurve.Add(ref level, ref exp, amount, maxLevel); Gained?.Invoke(amount);
            for (int i = 0; i < ups; i++) { stats.maxHealth += healthPerLevel; stats.Heal(healthPerLevel); }
            if (ups > 0) LeveledUp?.Invoke(level);
        }
    }
}
