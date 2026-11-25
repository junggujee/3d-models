using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    /// <summary>
    /// 성문 지기 「맨두」(왕실근위대 엘리트 하프물범, blender/build_mandu.py → Models/Mandu.fbx, 프리팹 Mandu — GameManager.enemyPrefabs, 웨이브엔 안 나옴).
    /// 자리: 성 둘레 높은 단(8.5m) 위, 성 본채 앞에서 마을 쪽(월드맵 남동)을 바라봄. GameManager 가 NavMesh 뒤에 CastleGate.Ensure().
    ///  · 평소(근무): 플레이어 편(공격·조준 안 됨) · 제자리를 지킴(밀리면 돌아감) · 가까이 오면 바라보고 처음엔 경례 · F 대화(Story/ManduStory)
    ///  · 꿀바른 땅콩빵을 바치면 통과(옆으로 비켜서서 경례 — 저장: Quests.Log "castle_gate" 완료)
    ///  · "억지로 들어간다" → 전투(보스바 · 문장 Mandu · 노랑/진동 기술) — 성문에서 25m 넘게 끌려가거나 플레이어가 멀어지면 근무로 돌아감
    ///  · 이기면 "졌다맨두…" + 레이저 블레이더 재료를 떨군다. 그래도 빵 없이는 통과 아님. 60초 뒤 다시 근무
    /// </summary>
    public class CastleGate : MonoBehaviour
    {
        public static CastleGate Instance { get; private set; }
        public const string PassId = "castle_gate";
        public static bool Passed => Quests.Log.IsDone(PassId);
        public Vector3 Post { get; private set; }
        public Vector3 Aside { get; private set; }
        public Quaternion Facing { get; private set; }
        GameObject prefab; float respawnAt = -1f; Transform postT;

        public static void Ensure(GameManager gm)
        {
            if (Instance != null || gm == null) return;
            GameObject pf = null; foreach (var e in gm.enemyPrefabs) if (e != null && e.name == "Mandu") { pf = e; break; }
            if (pf == null) { Debug.Log("[성문] Mandu 프리팹이 GameManager.enemyPrefabs 에 없음 — blender/build_mandu.py + Moomin 메뉴"); return; }
            var c = gm.gameObject.AddComponent<CastleGate>(); c.prefab = pf; c.Place(); c.Spawn();
        }
        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>성(지도 장소 castle)에서 광장 쪽으로 19m — 성 둘레 단 위(높이 약 8.5m).</summary>
        void Place()
        {
            Vector3 castle = MapPlaces.TryWorldPosition("castle", out var cw) ? cw : Vector3.zero, plaza = MapPlaces.TryWorldPosition("plaza", out var pw) ? pw : castle + Vector3.back * 60f;
            Vector3 dir = plaza - castle; dir.y = 0; dir = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.back;
            Vector3 want = castle + dir * 19f;
            if (Physics.Raycast(want + Vector3.up * 200f, Vector3.down, out var hit, 400f, ~0, QueryTriggerInteraction.Ignore)) want = hit.point;
            if (NavMesh.SamplePosition(want, out var nh, 6f, NavMesh.AllAreas)) want = nh.position;
            Post = want; Facing = Quaternion.LookRotation(dir); Aside = want + Vector3.Cross(Vector3.up, dir) * 2.8f;
            if (NavMesh.SamplePosition(Aside, out var ah, 3f, NavMesh.AllAreas)) Aside = ah.position;
            postT = new GameObject("CastleGatePost").transform; postT.SetPositionAndRotation(Post, Facing);
        }
        void Spawn()
        {
            var go = Instantiate(prefab, Passed ? Aside : Post, Facing); go.name = "Mandu";
            go.AddComponent<ManduGuard>().Setup(this, postT);
        }
        public void Defeated() { respawnAt = Time.time + 60f; }
        void Update()
        {
            if (UIState.Paused || respawnAt < 0f || Time.time < respawnAt) return;
            respawnAt = -1f; Spawn();
        }
    }

    /// <summary>
    /// 맨두 본체(평소 근무 ↔ 전투). 속성 노랑 · 타입 진동 · 무기 파란 야구방망이(평소엔 옆에 차고, 전투 동작에서 손에 듦 — 클립이 들고 있음).
    ///  · 보통 기술: 몽둥이 찜질 · 홈런!! · 맨두돌진 · 냠냠쫍쫍(거대화해서 감싸 물었다가 뱉음 — 그동안 못 움직임)
    ///  · 맨두맨두: 플레이어가 때리러 오면 방망이를 세워 받음. 플레이어 공격 위력이 맨두의 방어 위력보다 낮으면 바로 근위대 배치기로 반격
    ///  · 몽둥이 찜질로 합에서 이기면 코인 3개를 더 씀(마지막 타격 반복 — 진동이 쌓임)
    ///  · 필살기 100만톤 맨두: 체력 25% 아래 · 잠깐 뜸 들인 뒤 다섯 배 높이로 뛰어올라 플레이어 자리(빨간 원)로 떨어짐
    /// </summary>
    [RequireComponent(typeof(EnemyCombatController))]
    public class ManduGuard : MonoBehaviour
    {
        EnemyCombatController ec; EnemyBrain brain; CharacterStats stats; Animator anim; SkillExecutor exec; DefenseController def; NavMeshAgent agent;
        CastleGate gate; Transform post, player; CharacterStats playerStats;
        SkillDefinition beat, homerun, bump, charge, chomp, million;
        bool fighting, talking, defeated, pendingFight, usedMillion, counterArmed;
        float nextLine, nextBack, nextGuard, millionAt = -1f;
        bool leaping; Vector3 leapFrom, leapTo; float leapStart, leapHit; TelegraphIndicator leapTele;
        public bool Fighting => fighting;

        public void Setup(CastleGate g, Transform postT)
        {
            gate = g; post = postT; ec = GetComponent<EnemyCombatController>(); brain = GetComponent<EnemyBrain>(); stats = GetComponent<CharacterStats>();
            anim = GetComponentInChildren<Animator>(); exec = GetComponent<SkillExecutor>(); def = GetComponent<DefenseController>(); agent = GetComponent<NavMeshAgent>();
            stats.displayName = "맨두"; stats.epithet = "왕실근위대 엘리트"; stats.attribute = SkillAttribute.Yellow;
            ec.isBoss = true; ec.bossEmblem = "Mandu"; ec.kind = "Mandu"; ec.customTelegraph = CustomTelegraph;
            int pl = Experience.Instance != null ? Experience.Instance.level : 1; ec.SetLevel(Mathf.Max(6, pl + 1));
            beat = SkillLibrary.Get("MANDU_BEAT"); homerun = SkillLibrary.Get("MANDU_HOMERUN"); bump = SkillLibrary.Get("MANDU_BUMP");
            charge = SkillLibrary.Get("MANDU_CHARGE"); chomp = SkillLibrary.Get("MANDU_CHOMP"); million = SkillLibrary.Get("MANDU_MILLION");
            ec.skills = new System.Collections.Generic.List<SkillDefinition>(); foreach (var k in new[] { beat, homerun, charge, chomp }) if (k != null) ec.skills.Add(k);
            ec.basicSkill = beat;
            stats.Died += OnDefeated;
            if (exec != null) { exec.SkillStarted += OnSkillStarted; exec.CoinAttacked += OnCoin; }
            Duty();
        }
        void OnEnable() { ClashManager.ClashEnded += OnClashEnded; DefenseController.AnyGuardResolved += OnGuardResolved; }
        void OnDisable() { ClashManager.ClashEnded -= OnClashEnded; DefenseController.AnyGuardResolved -= OnGuardResolved; if (leapTele) leapTele.Hide(); }

        void OnDefeated()
        {
            defeated = true; gate.Defeated(); if (leapTele) { leapTele.Hide(); leapTele = null; }
            Toast.Show("맨두: 졌다맨두… 하지만 근위대는 포기하지 않는다맨두! 빵 없이는 못 지나간다맨두!");
            Vector3 p = transform.position + Vector3.up * 0.6f;                      // 레이저 블레이더 제작 재료
            ItemPickup.Spawn("mandu_core", 1, p); ItemPickup.Spawn("bat_alloy", Random.Range(3, 6), p);
            Quests.AnnounceNow("재료를 얻었다", "맨두의 진동 코어 · 파란 방망이 조각");
        }

        /// <summary>근무: 플레이어 편(맞지 않음) · 보스바 끔 · 싸우지 않음.</summary>
        void Duty()
        {
            fighting = false; usedMillion = false; millionAt = -1f; counterArmed = false; stats.team = Team.Player; ec.showBossBar = false; stats.ResetStats();
            if (brain != null) { brain.aggroRange = 0f; brain.externalIdle = true; brain.home = null; }
        }
        /// <summary>"억지로 들어간다" — 대화창이 닫히면 전투.</summary>
        public void RequestFight() { pendingFight = true; }
        void StartFight()
        {
            pendingFight = false; fighting = true; stats.team = Team.Enemy; ec.showBossBar = true;
            if (brain != null) { brain.personality = EnemyPersonality.Boss; brain.aggroRange = 18f; brain.externalIdle = false; brain.home = post; brain.leash = 25f; }
            Quests.AnnounceNow("성문 지기", "왕실근위대 엘리트 맨두"); Toast.Show("맨두: 근위대의 이름으로 막겠다맨두! 각오해라맨두!"); nextLine = Time.time + 8f;
        }

        void Update()
        {
            if (defeated || !stats.IsAlive) return;
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return;
                player = p.transform; p.TryGetComponent(out playerStats);
            }
            if (leaping) Leap();
            if (talking) { if (!UIState.Paused) talking = false; else return; }
            if (UIState.Paused) return;
            if (pendingFight) { StartFight(); return; }
            float d = Vector3.Distance(player.position, transform.position);
            if (fighting) { Fight(d); return; }
            Vector3 home = CastleGate.Passed ? gate.Aside : gate.Post; Vector3 off = transform.position - home; off.y = 0;
            if (off.magnitude > 0.8f && !ec.Busy) { if (Time.time >= nextBack) { nextBack = Time.time + 1f; Back(home); } return; }
            ec.Hold();
            if (d < 7f)
            {
                Vector3 to = player.position - transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 3f * Time.deltaTime);
                if (!saluted) { saluted = true; Play("Salute"); }
                Interaction.Offer(this, d, "F   맨두와 이야기", Talk);
            }
            else { saluted = false; transform.rotation = Quaternion.Slerp(transform.rotation, gate.Facing, 2f * Time.deltaTime); }
        }
        bool saluted;

        void Fight(float d)
        {
            if (d > 32f) { Toast.Show("맨두: 도망갔다맨두? 근무로 돌아간다맨두."); Duty(); return; }
            // 필살기 100만톤 맨두: 체력 25% 아래에서 한 박자 뜸 들인 뒤
            if (!usedMillion && million != null && stats.currentHealth <= stats.maxHealth * 0.25f)
            {
                if (millionAt < 0f) { millionAt = Time.time + 1.6f; Toast.Show("맨두: 이걸로 끝이다맨두… 100만톤 맨두!!"); ThirdPersonCamera.Shake(0.25f, 1.4f); }
                else if (Time.time >= millionAt && !ec.Busy && !stats.InClash) { ec.Attack(million, player); usedMillion = true; }
            }
            // 맨두맨두: 플레이어가 때리러 들어오면 방망이를 세워 받음(합 방어)
            if (!ec.Busy && !stats.Guarding && Time.time >= nextGuard && d < 4.2f && PlayerSwinging())
            {
                nextGuard = Time.time + 7f; counterArmed = true; ec.Guard(player, 1.1f); Toast.Show("맨두: 맨두맨두!");
            }
            if (Time.time >= nextLine) { nextLine = Time.time + Random.Range(10f, 16f); Toast.Show(Lines[Random.Range(0, Lines.Length)]); }
        }
        bool PlayerSwinging()
        {
            if (playerStats == null || !playerStats.TryGetComponent<SkillExecutor>(out var px)) return false;
            return px.IsExecuting && px.Phase != SkillPhase.Recovery;
        }

        // ── 맨두맨두로 받아냈을 때: 플레이어 공격 위력이 맨두의 방어 위력보다 낮으면 1코인 배치기로 반격
        void OnGuardResolved(DefenseController who, bool perfect, int guardPower, int attackPower)
        {
            if (who != def || !counterArmed || !fighting || !stats.IsAlive) return;
            counterArmed = false;
            if (!perfect) { Toast.Show("맨두: 으으… 세다맨두!"); return; }
            Toast.Show($"맨두: 어림없다맨두! (방어 {guardPower} > 공격 {attackPower}) 근위대 배치기다맨두!");
            if (bump != null && player != null) { stats.Guarding = false; ec.Attack(bump, player); }
        }

        // ── 몽둥이 찜질로 합에서 이기면 코인 3개를 더(마지막 타격 반복 · 진동 누적)
        void OnClashEnded(ClashOutcome o)
        {
            if (!fighting || o == null || exec == null || o.result != ClashResult.EnemyWin) return;
            if (o.enemyAttack == null || o.enemyAttack.owner != exec) return;
            if (exec.CurrentSkill == null || exec.CurrentSkill.skillId != "MANDU_BEAT") return;
            exec.GrantExtraCoins(3); Toast.Show("맨두: 신난다맨두! 찜질 삼 세판이다맨두!"); ThirdPersonCamera.Shake(0.12f, 0.3f);
        }

        // ── 스킬 시작: 100만톤은 뛰어올라 플레이어 자리로 / 냠냠쫍쫍은 큰소리로 예고
        void OnSkillStarted(SkillDefinition s)
        {
            if (s == null) return;
            if (s.skillId == "MANDU_CHOMP") Toast.Show("맨두: 냠냠쫍쫍이다맨두!");
            else if (s.skillId == "MANDU_HOMERUN") Toast.Show("맨두: 받아라맨두! 호오옴런!!");
            else if (s.skillId == "MANDU_MILLION" && player != null) BeginLeap(s);
        }
        /// <summary>100만톤 맨두는 바닥 예고를 플레이어 자리(월드 고정)에 직접 그림.</summary>
        bool CustomTelegraph(SkillDefinition s) => s != null && s.skillId == "MANDU_MILLION";

        void BeginLeap(SkillDefinition s)
        {
            leapFrom = transform.position; leapTo = player.position;
            if (NavMesh.SamplePosition(leapTo, out var nh, 4f, NavMesh.AllAreas)) leapTo = nh.position;
            leapStart = Time.time; leapHit = s.windup + s.hitTime; leaping = true;
            if (agent && agent.enabled) agent.enabled = false;
            if (leapTele) leapTele.Hide();
            leapTele = TelegraphIndicator.Show(null, leapTo + Vector3.up * 0.05f, s.areaRadius, leapHit, new Color(1f, 0.12f, 0.12f, 0.5f));
            Toast.Show("맨두: 피해라맨두… 아니, 못 피한다맨두!");
        }
        void Leap()
        {
            float t = Time.time - leapStart;
            float u = Mathf.Clamp01((t - (leapHit - 1.0f)) / 0.8f);                      // 떨어지기 전 0.8초 동안 플레이어 자리 위로 이동
            Vector3 p = Vector3.Lerp(leapFrom, leapTo, u * u * (3 - 2 * u)); p.y = Mathf.Lerp(leapFrom.y, leapTo.y, u);
            transform.position = p;
            if (t < leapHit) return;
            leaping = false; transform.position = leapTo;
            if (leapTele) { leapTele.Hide(); leapTele = null; }
            if (agent) { agent.enabled = true; if (agent.isOnNavMesh) agent.Warp(leapTo); }
            ThirdPersonCamera.Shake(0.75f, 0.9f);                                        // 지면이 부서지는 연출
            SparkBurst.Spawn(leapTo + Vector3.up * 0.2f, Vector3.up, 60, 9f, new Color(0.85f, 0.7f, 0.35f));
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 0.25f; Vector3 dir = new Vector3(Mathf.Cos(a), 0.25f, Mathf.Sin(a));
                SparkBurst.Spawn(leapTo + dir * 2.4f, dir, 14, 7f, new Color(0.7f, 0.6f, 0.45f));
            }
        }

        // ── 냠냠쫍쫍이 맞으면 한동안 입 안에서 꼼짝 못 함(뱉어낼 때까지)
        void OnCoin(AttackContext ctx)
        {
            if (ctx == null || ctx.skill == null || ctx.skill.skillId != "MANDU_CHOMP" || player == null) return;
            if (Vector3.Distance(player.position, transform.position) > ctx.skill.range + 1.2f) return;
            if (player.TryGetComponent<CombatManager>(out var cm)) cm.Stagger(0.9f);
            Toast.Show("맨두에게 꿀꺽 삼켜졌다! 오물오물…");
        }

        void Back(Vector3 home) { if (backT == null) backT = new GameObject("ManduHome").transform; backT.position = home; ec.Approach(backT); }
        Transform backT;

        void Talk()
        {
            var ui = UI.UIRoot.Instance; if (ui == null) return;
            if (ui.StartDialogue(ManduStory.Gate(this))) { talking = true; ec.Hold(); Play("Talk"); }
        }
        /// <summary>꿀바른 땅콩빵 바치기 → 통과(옆으로 비켜섬).</summary>
        public bool GiveBread()
        {
            var gm = GameManager.Instance; if (gm == null || gm.player == null || !gm.player.TryGetComponent<Inventory>(out var inv)) return false;
            if (!inv.Remove("honey_peanut_bread", 1)) return false;
            Quests.Log.Accept(CastleGate.PassId); Quests.Log.Complete(CastleGate.PassId);
            Quests.AnnounceNow("성문 통과!", "맨두가 길을 비켜 주었다"); Play("Laugh"); return true;
        }
        public void Play(string state)
        {
            if (anim == null || anim.runtimeAnimatorController == null || !anim.HasState(0, Animator.StringToHash(state))) return;
            anim.CrossFadeInFixedTime(state, 0.15f);
        }
        static readonly string[] Lines = { "맨두: 몽둥이 찜질이다맨두! 찰싹찰싹!", "맨두: 방망이는 근위대 지급품이다맨두. 아깝다맨두.", "맨두: 꿀바른 땅콩빵만 있었어도 이럴 일 없었다맨두!", "맨두: 엘리트는 지치지 않는다맨두… 헥헥." };
    }
}
