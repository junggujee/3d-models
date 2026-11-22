using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 가드와 회피(플레이어·적 공용). CombatResolver.ApplyHit 이 공격 코인 하나마다 대상별로 호출:
    ///   ① 회피 중이고 스킬이 회피 가능 → 회피 코인 위력과 공격 위력 비교(성공 = 피해 없음·회피 유지, 실패 = 회피 종료 후 피해)
    ///   ② 가드 중이고 스킬이 가드 가능 → 피해 × (1 - guardReduction). 위력 비교 없음.
    /// 입력: 플레이어 = CombatManager(우클릭 가드) · PlayerController(Space 구르기 = 회피), 적 = EnemyCombatController.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(CharacterStats))]
    public class DefenseController : MonoBehaviour
    {
        [Header("References")] public CharacterStats stats; public CoinSystem coinSystem;
        [Header("Guard")] [Range(0f, 0.9f)] [Tooltip("가드 시 피해 감소율(0.5 = 절반)")] public float guardReduction = 0.5f;
        [Tooltip("켜면 정면(±70°)에서 온 공격만 가드")] public bool guardRequiresFacing = false;
        [Tooltip("가드 시작 직후(parryWindow 초) 막으면 패링: 피해 없음 + 공격자 경직(기존 기능)")] public bool parryEnabled = true;
        [Header("Evade")] public int evadeBasePower = 8; public int evadeCoinPower = 3;

        readonly EvadeState evade = new();
        public bool isGuarding => stats != null && stats.Guarding;
        public bool IsEvading => evade.Active;
        public int CurrentEvadePower => evade.Power;
        public bool EvadeHeads => evade.Heads;

        /// <summary>회피 시작(코인 판정 결과): (위력, 앞면).</summary>
        public event Action<int, bool> EvadeStarted;
        /// <summary>회피 판정: (성공, 회피 위력, 공격 위력).</summary>
        public event Action<bool, int, int> EvadeResolved;
        public static event Action<DefenseController, bool, int, int> AnyEvadeResolved;

        void Awake() { if (!stats) stats = GetComponent<CharacterStats>(); if (!coinSystem) coinSystem = GetComponent<CoinSystem>(); }

        // ── 가드
        public void StartGuard() { if (stats == null || stats.Guarding) return; stats.Guarding = true; stats.GuardStartTime = Time.time; }
        public void StopGuard() { if (stats != null) stats.Guarding = false; }
        public float GetDamageMultiplier() => DefenseRules.GuardMultiplier(isGuarding, Mathf.Clamp(guardReduction + (stats != null ? stats.equipGuard : 0f), 0f, 0.9f));   // 장비(각티슈 방패 등) 가드 보너스

        // ── 회피: 시작할 때 코인 한 번
        public void StartEvade()
        {
            if (stats == null) return;
            var r = coinSystem != null ? coinSystem.Toss(stats, 0) : new CoinResult { heads = CoinMath.Toss(stats.currentSanity) };
            int eb = evadeBasePower + (stats != null ? stats.equipEvadePower : 0);   // 장비 회피 코인 위력
            int power = coinSystem != null ? coinSystem.CalculatePower(eb, evadeCoinPower, r.heads) : CoinMath.Power(eb, evadeCoinPower, r.heads);
            evade.Start(power, r.heads); EvadeStarted?.Invoke(power, r.heads);
        }
        /// <summary>공격 위력과 비교. true = 회피 성공(피해 없음, 회피 유지).</summary>
        public bool TryEvade(int incomingPower)
        {
            if (!evade.Active) return false;
            bool ok = evade.TryEvade(incomingPower);
            EvadeResolved?.Invoke(ok, evade.Power, incomingPower); AnyEvadeResolved?.Invoke(this, ok, evade.Power, incomingPower);
            if (ok) stats.NotifyEvaded();
            return ok;
        }
        public void EndEvade() => evade.End();
    }
}
