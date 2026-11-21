using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 캐릭터별 에너지(플레이어·적 공용, 0~maxEnergy). 실제 전투 사건에서만 증가:
    /// 명중(CombatResolver) · 클래시 승리(ClashManager) · 회피 성공 · 패링 성공 · 경직 깨뜨림 · 스킬의 energyGain.
    /// 스킬의 energyCost 가 남은 에너지보다 크면 SkillExecutor 가 사용을 거절.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterStats))]
    public class EnergyManager : MonoBehaviour
    {
        [Min(1)] public int maxEnergy = 100; [Min(0)] public int startEnergy = 0;
        [Header("Gain")] public int onHitGain = 3; public int onClashWinGain = 10; public int onDodgeGain = 5; public int onParryGain = 12; public int onStaggerBreakGain = 15;

        readonly EnergyPool pool = new();
        public int Current => pool.Current; public int Max => pool.Max; public float Ratio => pool.Max > 0 ? (float)pool.Current / pool.Max : 0f;
        /// <summary>(이전, 현재, 이유)</summary>
        public event Action<int, int, string> Changed;
        CharacterStats stats;

        void Awake() { stats = GetComponent<CharacterStats>(); pool.Max = maxEnergy; pool.Set(startEnergy); stats.Evaded += OnEvaded; }
        void OnDestroy() { if (stats != null) stats.Evaded -= OnEvaded; }
        void OnValidate() { if (Application.isPlaying) { pool.Max = maxEnergy; pool.Set(pool.Current); } }
        void OnEvaded() => Gain(onDodgeGain, "회피");

        public void Gain(int amount, string reason = "") { int b = pool.Current; if (pool.Gain(amount) > 0) Changed?.Invoke(b, pool.Current, reason); }
        public bool CanSpend(int amount) => pool.CanSpend(amount);
        public bool Spend(int amount, string reason = "") { int b = pool.Current; if (!pool.Spend(amount)) return false; if (amount > 0) Changed?.Invoke(b, pool.Current, reason); return true; }
        public void ResetEnergy() { int b = pool.Current; pool.Set(startEnergy); Changed?.Invoke(b, pool.Current, "초기화"); }
    }
}
