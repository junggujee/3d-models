using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    /// <summary>
    /// AI 가 고른 행동을 실제 이동·공격으로 실행(명세 v2 8-5).
    ///  · 접근: NavMeshAgent(구운 NavMesh 가 없으면 직접 이동으로 대체)
    ///  · 공격: SkillExecutor.UseSkill — 예고(windup) 동안 바닥 경고(빨강 일반 / 보라 방어 불가), 공격마다 앞으로 돌진(lunge)
    ///  · 방어: 정해진 시간 동안 Guarding(정면 피해 감소, 막 시작했을 때는 패링)
    ///  · 회피: 옆·뒤로 짧게 물러나며 무적, 쿨타임
    ///  · 경직(클래시 패배·패링당함): 스킬 중단, 받는 피해 증가 = 플레이어의 반격 기회
    /// </summary>
    /// <summary>적 유형: 인간형(곰 인형 등) · 지상괴물형 · 바다괴물형.</summary>
    public enum EnemyArchetype { Humanoid, GroundMonster, SeaMonster }

    [RequireComponent(typeof(CharacterStats), typeof(SkillExecutor))]
    public class EnemyCombatController : MonoBehaviour, ICombatant
    {
        public static readonly List<EnemyCombatController> All = new();
        public NavMeshAgent agent; public Animator animator; public SkillExecutor skillExecutor;
        public SkillDefinition basicSkill; public List<SkillDefinition> skills = new();
        public Transform visual; public float moveSpeed = 2.4f, dodgeDistance = 2.6f, dodgeTime = 0.32f, dodgeCooldown = 2.2f, guardCooldown = 1.2f, flinchTime = 0.25f, gravity = -20f;
        public EnemyArchetype archetype = EnemyArchetype.GroundMonster; public bool isBoss; public int score = 10;
        public CharacterStats Stats => stats; public SkillExecutor Executor => skillExecutor;
        public float AimHeight => (archetype == EnemyArchetype.Humanoid ? 0.9f : 0.6f) * (isBoss ? 2.4f : 1f);
        /// <summary>애니메이터(컨트롤러 포함)가 있으면 동작을 재생, 없으면 몸을 늘였다 줄였다 하는 연출.</summary>
        public bool Animated => animator != null && animator.runtimeAnimatorController != null;
        void Play(string state, float fade = 0.08f) { if (Animated && animator.HasState(0, Animator.StringToHash(state))) animator.CrossFadeInFixedTime(state, fade); }
        public bool IsStaggered => Time.time < staggerUntil;
        public bool IsDodging => Time.time < dodgeEnd;
        /// <summary>스킬·경직·회피·방어 중이면 새 판단을 하지 않음.</summary>
        public bool Busy => !stats.IsAlive || skillExecutor.IsExecuting || IsStaggered || IsDodging || Time.time < busyUntil;

        [Tooltip("쓰러뜨리면 플레이어가 얻는 경험치(레벨 1 기준 — 레벨·레벨 차이로 바뀜)")] public int expReward = 40;
        [Tooltip("종류별 레벨 보정(곰 +1 · 그린볼런 +1 · 고래 +2)")] public int levelOffset;
        [Header("Boss Bar")] [Tooltip("화면 아래 보스바로 표시")] public bool showBossBar; [Tooltip("보스바 문장(Resources/UI/Bars/BossEmblem_이름)")] public string bossEmblem = "Default";
        [Tooltip("종류(프리팹 이름: PinkCube·GreenBlock·ShadowBlob·SleepyBear — 토벌 퀘스트가 셈). 비우면 오브젝트 이름에서")] public string kind;
        /// <summary>이 적을 만든 토벌 구역(HuntZone) — 없으면 빈 값.</summary>
        [HideInInspector] public string zoneId;
        public string Kind { get { if (string.IsNullOrEmpty(kind)) kind = name.Replace("(Clone)", "").Trim(); return kind; } }
        /// <summary>적이 쓰러질 때(토벌 퀘스트 집계 — Hunts.Hook).</summary>
        public static event System.Action<EnemyCombatController> AnyKilled;
        [Header("Wave")] [Tooltip("이 웨이브부터 등장")] public int minWave = 1; [Tooltip("한 웨이브에 최대 몇 마리(0 = 제한 없음)")] public int maxPerWave = 0;
        DefenseController defense; SkillPhase lastPhase; public Transform Target => target;
        CharacterStats stats; Transform target; Vector3 knock, dodgeDir, baseScale; float vy, staggerUntil, dodgeEnd, nextDodge, nextGuard, busyUntil, deadT = -1, nextRepath, nextWarp; bool navigating;
        TelegraphIndicator tele;

        void OnEnable() { All.Add(this); } void OnDisable() { All.Remove(this); }
        void Awake()
        {
            stats = GetComponent<CharacterStats>(); defense = GetComponent<DefenseController>(); if (!skillExecutor) skillExecutor = GetComponent<SkillExecutor>(); if (!agent) agent = GetComponent<NavMeshAgent>(); if (!animator) animator = GetComponentInChildren<Animator>();
            if (!visual) visual = transform.childCount > 0 ? transform.GetChild(0) : transform; baseScale = visual.localScale;
            if (agent) { agent.updateRotation = false; agent.speed = moveSpeed; agent.stoppingDistance = 1.0f; agent.acceleration = 20f; }
            skills.RemoveAll(s => s == null); if (skills.Count == 0) { skills.Add(basicSkill != null ? basicSkill : SkillLibrary.Get("ENEMY_BUMP")); skills.Add(SkillLibrary.Get("ENEMY_SLAM")); }
            if (basicSkill == null) basicSkill = skills[0];
            stats.Damaged += OnDamaged; stats.Died += OnDied; lastPos = transform.position;
            skillExecutor.SkillStarted += OnSkillStarted; skillExecutor.SkillFinished += (s, done) => { HideTelegraph(); };
            skillExecutor.CoinAttacked += a => { if (!Animated) visual.localScale = new Vector3(baseScale.x * 0.85f, baseScale.y * 1.2f, baseScale.z * 0.85f); };
        }

        // ── AI 가 호출하는 행동 ─────────────────────────
        public bool CanAttack() { if (Busy) return false; foreach (var s in skills) if (skillExecutor.IsReady(s)) return true; return false; }
        public void Approach(Transform t) { target = t; navigating = true; stats.Guarding = false; }
        public void Hold() { navigating = false; if (stats.Guarding && Time.time >= busyUntil) stats.Guarding = false; }
        public void Attack(SkillDefinition skill, Transform t)
        {
            target = t; navigating = false; stats.Guarding = false; FaceNow(t);
            skillExecutor.UseSkill(skill != null ? skill : basicSkill);
        }
        public void Guard(Transform t, float seconds)
        {
            if (Time.time < nextGuard) { Hold(); return; }
            target = t; navigating = false; if (defense != null) defense.StartGuard(); else { stats.Guarding = true; stats.GuardStartTime = Time.time; } busyUntil = Time.time + seconds; nextGuard = busyUntil + guardCooldown; Play("Guard");
        }
        public void Dodge(Transform t)
        {
            if (Time.time < nextDodge) { Guard(t, 0.6f); return; }
            target = t; navigating = false; stats.Guarding = false; Vector3 away = transform.position - t.position; away.y = 0; away.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, away) * (Random.value < 0.5f ? 1f : -1f);
            dodgeDir = (away * 0.6f + side * 0.8f).normalized; dodgeEnd = Time.time + dodgeTime; nextDodge = dodgeEnd + dodgeCooldown; if (defense != null) defense.StartEvade();   // 회피 코인 한 번
            Play("Dodge", 0.04f);
        }

        public void Stagger(float seconds, float vulnerable = 1f)
        {
            if (!stats.IsAlive) return; float s = isBoss ? seconds * 0.6f : seconds;
            skillExecutor.Interrupt(); HideTelegraph(); stats.Guarding = false; navigating = false; staggerUntil = Time.time + s; if (vulnerable > 1f) stats.SetVulnerable(vulnerable, s); Play("Hit", 0.04f);
        }

        void OnSkillStarted(SkillDefinition s)
        {
            if (s.windup <= 0f) return; HideTelegraph(); Play(string.IsNullOrEmpty(s.windupAnimation) ? "Windup" : s.windupAnimation, 0.1f);
            if (s.vfx == SkillVfx.WhaleBeam) WhaleChargeFx.Play(transform, s.windup);   // 고래빔 기 모으기
            Color tc = !s.canBeGuarded ? new Color(0.65f, 0.2f, 1f, 0.45f) : !s.canBeClashed ? new Color(0.2f, 0.55f, 1f, 0.45f) : new Color(1f, 0.15f, 0.1f, 0.4f);   // 보라 = 방어 불가 · 파랑 = 합 불가 · 빨강 = 일반 · 초록 직선 = 원거리
            if (s.isProjectile && s.projectileLob) return;   // 딸기 폭탄: 떨어질 자리마다 던질 때 예고(Projectile)
            if (s.isProjectile && s.projectileSpread >= 359f) { tele = TelegraphIndicator.Show(transform, Vector3.zero, 4.5f / transform.localScale.x, s.windup, new Color(0.75f, 0.35f, 1f, 0.42f)); return; }   // 회전 탄막: 몸 둘레 보라 원
            if (s.isProjectile) { tele = TelegraphIndicator.ShowLine(transform, s.range / transform.localScale.z, s.projectileRadius * 2.2f / transform.localScale.x, s.windup, new Color(0.45f, 0.95f, 0.3f, 0.42f)); return; }   // 초록 직선 = 원거리(날아오는 길)
            if (s.isAreaAttack && s.areaShape == AreaShape.Line) { tele = TelegraphIndicator.ShowLine(transform, s.areaRadius / transform.localScale.z, s.areaWidth / transform.localScale.x, s.windup, tc); return; }
            bool aoe = s.isAreaAttack || s.angle >= 360f; float r = (aoe ? s.Reach : Mathf.Max(1f, s.range * 0.6f)) / transform.localScale.x;
            float travel = s.lunge * s.lungeTime * 0.7f;   // 돌진하며 내려찍는 기술(점프)은 착지할 자리에 예고
            Vector3 off = aoe ? Vector3.forward * (travel / transform.localScale.z) : Vector3.forward * (s.range * 0.5f / transform.localScale.z);
            tele = TelegraphIndicator.Show(transform, off, r, s.windup, tc);
        }
        void HideTelegraph() { if (tele) tele.Hide(); tele = null; }

        void OnDamaged(float dmg, Vector3 p, Vector3 k)
        {
            knock = k * (isBoss ? 0.25f : 1f);
            if (!skillExecutor.IsExecuting && !IsStaggered && !stats.Guarding) { busyUntil = Mathf.Max(busyUntil, Time.time + flinchTime); if (Animated) Play("Hit", 0.04f); else visual.localScale = baseScale * 0.88f; }   // 공격 중에는 경직되지 않음(슈퍼아머)
        }

        void OnDied()
        {
            skillExecutor.Interrupt(); HideTelegraph(); stats.Guarding = false; deadT = 0f; if (agent && agent.enabled) agent.enabled = false; Play("Death", 0.05f);
            foreach (var c in GetComponents<Collider>()) c.enabled = false;
            RunStats.Kills++; if (isBoss || showBossBar) RunStats.BossKills++;
            if (GameManager.Instance != null) GameManager.Instance.AddScore(score);
            if (Experience.Instance != null) Experience.Instance.Gain(LevelMath.ExpReward(expReward, stats.level, Experience.Instance.level)); EnemyLoot.Drop(transform.position + Vector3.up * 0.5f, isBoss);
            AnyKilled?.Invoke(this);
        }

        void Update()
        {
            if (UIState.Paused) return;
            if (deadT >= 0f)   // 애니메이션이 있으면 쓰러지는 동작을 보여 준 뒤 사라짐
            {
                deadT += Time.deltaTime; float hold = Animated ? 1.6f : 0f; float k = Mathf.Max(0, 1 - Mathf.Max(0, deadT - hold) * 2.5f);
                visual.localScale = baseScale * k; if (deadT > hold + 0.4f) Destroy(gameObject); return;
            }
            if (stats.InClash) { if (agent && agent.enabled && agent.isOnNavMesh) agent.isStopped = true; UpdateAnimator(); return; }   // 합 연출 중: ClashManager 가 움직임
            knock = Vector3.Lerp(knock, Vector3.zero, 8f * Time.deltaTime);
            if (defense != null && defense.IsEvading && !IsDodging) defense.EndEvade();   // 회피 동작이 끝나면 회피 판정도 끝
            Vector3 extra = knock; bool moveNav = false; var s = skillExecutor.CurrentSkill;

            UpdateAnimator();
            // 회복 구간에 들어가면 마무리 동작(예: 고래빔 BeamEnd)
            var ph = skillExecutor.Phase; if (ph != lastPhase) { if (ph == SkillPhase.Recovery && s != null && !string.IsNullOrEmpty(s.recoveryAnimation)) Play(s.recoveryAnimation, 0.12f); lastPhase = ph; }
            if (IsStaggered) { if (!Animated) { visual.localRotation = Quaternion.Euler(0, 0, 12f * Mathf.Sin(Time.time * 18f)); visual.localScale = baseScale * (1f + 0.05f * Mathf.Sin(Time.time * 30f)); } }   // 애니메이션이 있으면 Hit 동작
            else
            {
                if (!Animated) visual.localRotation = Quaternion.identity;
                if (IsDodging) extra += dodgeDir * (dodgeDistance / dodgeTime);
                else if (skillExecutor.IsExecuting && s != null)
                {
                    if (skillExecutor.Phase == SkillPhase.Windup) { Face(4f); float w = Mathf.Clamp01(skillExecutor.PhaseTime / Mathf.Max(0.01f, s.windup)); if (!Animated) visual.localScale = Vector3.Lerp(baseScale, new Vector3(baseScale.x * 1.18f, baseScale.y * 0.78f, baseScale.z * 1.18f), w); }
                    else if (skillExecutor.Phase == SkillPhase.Attacking)
                    {
                        float lt = skillExecutor.PhaseTime - s.lungeDelay;   // 돌진 구간(점프 공격은 공중에 뜬 동안)
                        if (lt >= 0f && lt < s.lungeTime) extra += transform.forward * s.lunge * (1f - 0.6f * lt / Mathf.Max(0.01f, s.lungeTime));
                        Face(s.areaShape == AreaShape.Line && s.isAreaAttack ? 0.5f : 2f);   // 빔은 쏘는 동안 거의 안 돌아감
                    }
                    else if (!Animated) visual.localScale = Vector3.Lerp(visual.localScale, baseScale, 8f * Time.deltaTime);
                }
                else if (stats.Guarding) { Face(8f); if (!Animated) visual.localScale = Vector3.Lerp(visual.localScale, new Vector3(baseScale.x * 1.1f, baseScale.y * 0.85f, baseScale.z * 1.1f), 10f * Time.deltaTime); if (Time.time >= busyUntil) { stats.Guarding = false; Play("Locomotion", 0.15f); } }
                else
                {
                    if (!Animated) visual.localScale = Vector3.Lerp(visual.localScale, baseScale, 8f * Time.deltaTime);
                    if (navigating && target) { moveNav = true; Face(10f); float bob = Mathf.Abs(Mathf.Sin(Time.time * 8f)); if (!Animated) visual.localScale = new Vector3(baseScale.x * (1 + 0.08f * bob), baseScale.y * (1 - 0.12f * bob), baseScale.z * (1 + 0.08f * bob)); }
                    else if (target) Face(6f);
                }
            }
            Move(moveNav, extra);
        }

        Vector3 lastPos; float speedSmoothed;
        /// <summary>이동 속도를 Animator "Speed"(0 대기 · 0.5 걷기 · 1 달리기)로 전달.</summary>
        void UpdateAnimator()
        {
            if (!Animated) return;
            float v = Time.deltaTime > 0 ? Vector3.ProjectOnPlane(transform.position - lastPos, Vector3.up).magnitude / Time.deltaTime : 0f; lastPos = transform.position;
            speedSmoothed = Mathf.Lerp(speedSmoothed, Mathf.Clamp01(v / (moveSpeed * 2f)), 10f * Time.deltaTime); animator.SetFloat("Speed", skillExecutor.IsExecuting || IsDodging ? 0f : speedSmoothed);
        }

        void Move(bool moveNav, Vector3 extra)
        {
            bool nav = agent && agent.enabled && agent.isOnNavMesh;
            if (agent && agent.enabled && !agent.isOnNavMesh && Time.time >= nextWarp) { nextWarp = Time.time + 1f; if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas)) agent.Warp(hit.position); }
            if (nav)
            {
                agent.isStopped = !moveNav;
                if (moveNav && target && Time.time >= nextRepath) { nextRepath = Time.time + 0.2f; agent.SetDestination(target.position); }
                if (extra.sqrMagnitude > 0.0001f) agent.Move(extra * Time.deltaTime);
                return;
            }
            // NavMesh 가 없을 때: 직접 이동 + 바닥 붙이기
            Vector3 v = extra;
            if (moveNav && target) { Vector3 to = target.position - transform.position; to.y = 0; if (to.magnitude > 1.0f) v += to.normalized * moveSpeed; }
            Vector3 pos = transform.position + v * Time.deltaTime;
            if (GroundBelow(pos, out float gy)) { pos.y = gy; vy = 0; }
            else { vy += gravity * Time.deltaTime; pos.y += vy * Time.deltaTime; }
            transform.position = pos;
        }

        /// <summary>자기 콜라이더를 제외한 가장 가까운 바닥 높이.</summary>
        bool GroundBelow(Vector3 pos, out float y)
        {
            y = 0; float best = float.MaxValue; bool found = false;
            foreach (var h in Physics.RaycastAll(pos + Vector3.up * 1.5f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore))
                if (!h.transform.IsChildOf(transform) && h.collider.GetComponentInParent<CharacterStats>() == null && h.distance < best) { best = h.distance; y = h.point.y; found = true; }
            return found;
        }

        void Face(float speed) { if (!target) return; Vector3 to = target.position - transform.position; to.y = 0; if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), speed * Time.deltaTime); }
        void FaceNow(Transform t) { Vector3 to = t.position - transform.position; to.y = 0; if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(to); }

        /// <summary>레벨 정하기(웨이브 생성 직후): 체력 +6%/레벨 · 공격력 +0.5/레벨 · 경험치 +8%/레벨.</summary>
        public void SetLevel(int lv)
        {
            stats.level = Mathf.Max(1, lv); stats.maxHealth *= LevelMath.EnemyHealthScale(stats.level); stats.attack += LevelMath.EnemyAttackBonus(stats.level);
            expReward = Mathf.RoundToInt(expReward * (1f + 0.08f * (stats.level - 1))); stats.ResetStats();
        }

        /// <summary>보스로 전환(웨이브 3마다): 체력 ×6, 크기 ×1.8, 위력 ×1.35, 보스 기술, 신중한 성격.</summary>
        public void ConfigureBoss()
        {
            isBoss = true; score = 100; expReward *= 5; showBossBar = true; stats.displayName = "거대한 그림자"; stats.attribute = SkillAttribute.Red; stats.maxHealth *= 6f; stats.ResetStats(); stats.ChangeSanity(10, "보스");
            transform.localScale *= 1.8f; moveSpeed *= 0.85f; if (agent) agent.speed = moveSpeed;
            skills = new List<SkillDefinition>();
            foreach (var id in new[] { "ENEMY_BUMP", "ENEMY_SLAM", "BOSS_LUNGE", "BOSS_FLURRY", "BOSS_BREAK" })
            {
                var c = Instantiate(SkillLibrary.Get(id)); c.basePower = Mathf.RoundToInt(c.basePower * 1.35f); c.range *= 1.5f; c.areaRadius *= 1.5f; skills.Add(c);
            }
            basicSkill = skills[0]; var brain = GetComponent<EnemyBrain>(); if (brain) brain.personality = EnemyPersonality.Boss;
        }
    }

    /// <summary>처치 보상: 금화 항상, 별조각 40% · 붉은 약병 25% · 불꽃 가루 30% · 은빛 깃털 8% · 무기 3%(보스는 4번 굴리고 무기 25%).</summary>
    /// <summary>집에서 찾은 장비 5종(이불 로브·각티슈 방패·솜털 밀집모자·수면 바지·토끼 털 슬리퍼) — 적·땅콩빵에서 가끔.</summary>
    public static class HomeGear
    {
        public static readonly string[] Ids = { "robe_blanket", "shield_tissue", "cap_sleep", "pants_pajama", "slippers_fuzzy" };
        public static string Random() => Ids[UnityEngine.Random.Range(0, Ids.Length)];
    }

    public static class EnemyLoot
    {
        public static void Drop(Vector3 p, bool boss)
        {
            ItemPickup.Spawn("gold", Random.Range(5, 16) * (boss ? 8 : 1), p);
            for (int k = 0, n = boss ? 4 : 1; k < n; k++)
            {
                if (Random.value < 0.40f) ItemPickup.Spawn("shard_star", 1, p); if (Random.value < 0.25f) ItemPickup.Spawn("potion_red", 1, p);
                if (Random.value < 0.30f) ItemPickup.Spawn("dust_fire", Random.Range(1, 4), p); if (Random.value < 0.08f) ItemPickup.Spawn("feather_silver", 1, p);
                if (Random.value < (boss ? 0.25f : 0.03f)) ItemPickup.Spawn(Random.value < 0.5f ? "sword_ember" : "sword_frost", 1, p);
                if (Random.value < (boss ? 0.3f : 0.04f)) ItemPickup.Spawn(HomeGear.Random(), 1, p);   // 집에서 찾은 장비
            }
        }
    }
}
