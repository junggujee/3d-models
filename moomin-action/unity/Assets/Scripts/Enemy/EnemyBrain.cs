using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 적의 판단(명세 v2 8-4): 0.4초마다 상황을 모아 EnemyDecisionSystem 에 묻고, 고른 행동을 EnemyCombatController 로 실행.
    /// 스킬 실행·경직·회피·방어 유지 중에는 새로 판단하지 않음(행동 중복 방지).
    /// </summary>
    [RequireComponent(typeof(CharacterStats), typeof(EnemyCombatController))]
    public class EnemyBrain : MonoBehaviour
    {
        public Transform player; public CharacterStats stats, playerStats; public EnemyCombatController combat;
        public EnemyPersonality personality = EnemyPersonality.Grunt; public float decisionInterval = 0.4f, aggroRange = 25f;
        public EnemyAction LastAction { get; private set; }
        readonly EnemyDecisionSystem decision = new(); SkillExecutor playerSkills; float nextDecisionTime;

        void Awake() { if (!stats) stats = GetComponent<CharacterStats>(); if (!combat) combat = GetComponent<EnemyCombatController>(); }
        void Start() { var p = GameObject.FindGameObjectWithTag("Player"); if (p) { player = p.transform; playerStats = p.GetComponent<CharacterStats>(); playerSkills = p.GetComponent<SkillExecutor>(); } nextDecisionTime = Time.time + Random.Range(0.2f, 0.8f); }

        void Update()
        {
            if (UIState.Paused || !stats.IsAlive || player == null || playerStats == null || !playerStats.IsAlive) { if (combat) combat.Hold(); return; }
            if (Time.time < nextDecisionTime || combat.Busy) return;
            nextDecisionTime = Time.time + decisionInterval;
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > aggroRange) { combat.Hold(); return; }

            bool playerAttacking = playerSkills != null && playerSkills.IsExecuting && playerSkills.Phase != SkillPhase.Recovery;
            var situation = new EnemySituation
            {
                distance = distance, healthRatio = stats.HealthRatio, playerHealthRatio = playerStats.HealthRatio, playerAttacking = playerAttacking, playerGuarding = playerStats.Guarding,
                attackAvailable = combat.CanAttack(), sanity = stats.currentSanity,
                playerAttackPower = playerAttacking && playerSkills.CurrentSkill != null ? playerSkills.CurrentSkill.ExpectedPower(playerStats.currentSanity) : 0f
            };
            var action = decision.Decide(situation, personality, combat.skills, combat.Executor, out var skill);
            LastAction = action;
            switch (action)
            {
                case EnemyAction.Approach: combat.Approach(player); break;
                case EnemyAction.Attack: combat.Attack(skill, player); break;
                case EnemyAction.Guard: combat.Guard(player, 0.9f); break;
                case EnemyAction.Dodge: combat.Dodge(player); break;
                default: combat.Hold(); break;
            }
        }
    }
}
