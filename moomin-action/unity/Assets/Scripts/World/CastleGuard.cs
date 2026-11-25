using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    /// <summary>
    /// 성문 지기 「맨두」(왕실근위대 엘리트 하프물범, blender/build_mandu.py → Models/Mandu.fbx, 프리팹 Mandu — GameManager.enemyPrefabs, 웨이브엔 안 나옴).
    /// 자리: 성 둘레 높은 단(8.5m) 위, 성 본채 앞에서 마을 쪽(월드맵 남동)을 바라봄. GameManager 가 NavMesh 뒤에 CastleGate.Ensure().
    ///  · 평소(근무): 플레이어 편(공격·조준 안 됨) · 제자리를 지킴(밀리면 돌아감) · 가까이 오면 바라보고 처음엔 경례 · F 대화(Story/ManduStory)
    ///  · 꿀바른 땅콩빵을 바치면 통과(옆으로 비켜서서 경례 — 저장: Quests.Log "castle_gate" 완료)
    ///  · "억지로 들어간다" → 전투(보스바 · 문장 Mandu · 근위대 기술 3종) — 성문에서 25m 넘게 끌려가거나 플레이어가 멀어지면 근무로 돌아감
    ///  · 이기면 "졌다맨두…" — 그래도 빵 없이는 통과 아님. 60초 뒤 다시 근무
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

    /// <summary>맨두 본체(평소 근무 ↔ 전투).</summary>
    [RequireComponent(typeof(EnemyCombatController))]
    public class ManduGuard : MonoBehaviour
    {
        EnemyCombatController ec; EnemyBrain brain; CharacterStats stats; Animator anim; CastleGate gate; Transform post, player;
        bool fighting, talking, saluted, defeated; float nextLine, nextBack;
        public bool Fighting => fighting;

        public void Setup(CastleGate g, Transform postT)
        {
            gate = g; post = postT; ec = GetComponent<EnemyCombatController>(); brain = GetComponent<EnemyBrain>(); stats = GetComponent<CharacterStats>(); anim = GetComponentInChildren<Animator>();
            stats.displayName = "맨두"; stats.epithet = "왕실근위대 엘리트"; ec.isBoss = true; ec.bossEmblem = "Mandu"; ec.kind = "Mandu";
            int pl = Experience.Instance != null ? Experience.Instance.level : 1; ec.SetLevel(Mathf.Max(6, pl + 1));
            stats.Died += () => { defeated = true; gate.Defeated(); Toast.Show("맨두: 졌다맨두… 하지만 근위대는 포기하지 않는다맨두! 빵 없이는 못 지나간다맨두!"); };
            Duty();
        }

        /// <summary>근무: 플레이어 편(맞지 않음) · 보스바 끔 · 싸우지 않음.</summary>
        void Duty()
        {
            fighting = false; stats.team = Team.Player; ec.showBossBar = false; stats.ResetStats();
            if (brain != null) { brain.aggroRange = 0f; brain.externalIdle = true; brain.home = null; }
        }
        /// <summary>"억지로 들어간다" — 대화창이 닫히면 전투.</summary>
        public void RequestFight() { pendingFight = true; }
        bool pendingFight;
        void StartFight()
        {
            pendingFight = false; fighting = true; stats.team = Team.Enemy; ec.showBossBar = true;
            if (brain != null) { brain.personality = EnemyPersonality.Boss; brain.aggroRange = 18f; brain.externalIdle = false; brain.home = post; brain.leash = 25f; }
            Quests.AnnounceNow("성문 지기", "왕실근위대 엘리트 맨두"); Toast.Show("맨두: 근위대의 이름으로 막겠다맨두! 각오해라맨두!"); nextLine = Time.time + 8f;
        }

        void Update()
        {
            if (defeated || !stats.IsAlive) return;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return; player = p.transform; }
            if (talking) { if (!UIState.Paused) talking = false; else return; }
            if (UIState.Paused) return;
            if (pendingFight) { StartFight(); return; }
            float d = Vector3.Distance(player.position, transform.position);
            if (fighting)
            {
                if (d > 32f) { Toast.Show("맨두: 도망갔다맨두? 근무로 돌아간다맨두."); Duty(); return; }
                if (Time.time >= nextLine) { nextLine = Time.time + Random.Range(10f, 16f); Toast.Show(Lines[Random.Range(0, Lines.Length)]); }
                return;
            }
            // 근무: 제자리(통과했으면 옆자리)로 돌아가 바라봄
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
        Transform backT;
        void Back(Vector3 home) { if (backT == null) backT = new GameObject("ManduHome").transform; backT.position = home; ec.Approach(backT); }

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
        static readonly string[] Lines = { "맨두: 근위대 굴러가기다맨두! 데굴데굴~", "맨두: 찰싹찰싹! 아프냐맨두?", "맨두: 꿀바른 땅콩빵만 있었어도 이럴 일 없었다맨두!", "맨두: 엘리트는 지치지 않는다맨두… 헥헥." };
    }
}
