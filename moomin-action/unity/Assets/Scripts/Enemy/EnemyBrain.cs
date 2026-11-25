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
        /// <summary>집(토벌 구역 가운데)과 끈 길이: 플레이어가 멀어지거나 집에서 leash 넘게 끌려가면 집으로 돌아감(home 이 없으면 없음).</summary>
        [HideInInspector] public Transform home; [HideInInspector] public float leash;
        /// <summary>싸우지 않을 때(사거리 밖) 멈추지 않음 — 다른 컴포넌트(용궁 임금님의 순찰)가 움직임.</summary>
        [HideInInspector] public bool externalIdle;
        readonly EnemyDecisionSystem decision = new(); SkillExecutor playerSkills; float nextDecisionTime;

        void Awake() { if (!stats) stats = GetComponent<CharacterStats>(); if (!combat) combat = GetComponent<EnemyCombatController>(); }
        void Start() { var p = GameObject.FindGameObjectWithTag("Player"); if (p) { player = p.transform; playerStats = p.GetComponent<CharacterStats>(); playerSkills = p.GetComponent<SkillExecutor>(); } nextDecisionTime = Time.time + Random.Range(0.2f, 0.8f); }

        void Update()
        {
            if (UIState.Paused || !stats.IsAlive || player == null || playerStats == null || !playerStats.IsAlive) { if (combat) combat.Hold(); return; }
            if (Time.time < nextDecisionTime || combat.Busy) return;
            nextDecisionTime = Time.time + decisionInterval;
            float distance = Vector3.Distance(transform.position, player.position);
            if (home != null && leash > 0f)
            {
                Vector3 ph = player.position - home.position, sh = transform.position - home.position; ph.y = 0; sh.y = 0;
                if (distance > aggroRange || ph.magnitude > leash) { if (sh.magnitude > 3f) combat.Approach(home); else combat.Hold(); return; }
            }
            if (distance > aggroRange) { if (!externalIdle) combat.Hold(); return; }

            bool playerAttacking = playerSkills != null && playerSkills.IsExecuting && playerSkills.Phase != SkillPhase.Recovery;
            var situation = new EnemySituation
            {
                distance = distance, healthRatio = stats.HealthRatio, playerHealthRatio = playerStats.HealthRatio, playerAttacking = playerAttacking, playerGuarding = playerStats.Guarding,
                attackAvailable = combat.CanAttack(), sanity = stats.currentSanity,
                playerAttackPower = playerAttacking && playerSkills.CurrentSkill != null ? playerSkills.CurrentSkill.ExpectedPower(playerStats.currentSanity) : 0f,
                playerAttribute = SkillAttributes.DefenderAttribute(playerStats)
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
