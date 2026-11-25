using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 용궁의 보스 「고래고래 임금님」(고래고래 프리팹 + 금관 · 망토 — Resources/Boss/KingCrown · KingCape, blender/build_whale_king.py).
    ///  · 평소: 결투장 둘레 순찰 지점 8곳과 계단 위 · 왕좌의 방(Mark_Patrol_1~10)을 천천히 돌아다님(EnemyBrain.externalIdle — 싸우지 않을 땐 이 컴포넌트가 움직임)
    ///  · 민미가 공기방울 돔 안에 들어와 26m 안이면 싸움(보스 성격). 처음 만나면 인사 대사 + 큰 알림, 보스바(문장 WhaleKing)
    ///  · 1페이지: 고래 기술(박치기 · 지느러미 · 꼬리 · 점프 · 물기둥) — 위력 ×1.15
    ///  · 체력 50% 아래 → <b>각성</b>(2페이지): 포효(잠깐 경직 · 무지개 불꽃) · 별칭 "각성한 무지갯빛 임금님" · 조금 빨라짐 ·
    ///    기술 = 사용자 그림의 5종(KING_RAM 박치기 · KING_ORBIT 회전 탄막 · KING_STRAWBERRY 딸기 폭탄 · KING_BUBBLE 버블 감옥 · KING_RAINBOW 각성 고래빔) + 꼬리
    ///  · 쓰러지면: 화해 대사 · "임금님의 무지개 진주"(퀘스트 「삐진 고래고래 임금님」, 없을 때만) · 5분 뒤 다시 나타남(Palace)
    /// </summary>
    [RequireComponent(typeof(EnemyCombatController))]
    public class WhaleKing : MonoBehaviour
    {
        public static WhaleKing Current { get; private set; }
        public static readonly string[] Phase1 = { "WHALE_RAM", "WHALE_FLIPPER", "WHALE_TAIL", "WHALE_JUMP", "WHALE_SPOUT" };
        public static readonly string[] Phase2 = { "KING_RAM", "KING_ORBIT", "KING_STRAWBERRY", "KING_BUBBLE", "KING_RAINBOW", "WHALE_TAIL" };
        public const float AggroRange = 26f;

        EnemyCombatController ec; EnemyBrain brain; CharacterStats stats; Palace palace; Transform player; readonly List<Renderer> crownGlow = new();
        int patrolIndex; float nextPatrol, nextLine; bool awakened, introduced, defeated;

        void Awake() { Current = this; ec = GetComponent<EnemyCombatController>(); brain = GetComponent<EnemyBrain>(); stats = GetComponent<CharacterStats>(); }
        void OnDestroy() { if (Current == this) Current = null; }

        public void Setup(Palace p, int level)
        {
            palace = p;
            ec.isBoss = true; ec.showBossBar = true; ec.bossEmblem = "WhaleKing"; ec.score = 300; ec.expReward *= 6; ec.kind = "WhaleKing";
            stats.displayName = "고래고래 임금님"; stats.epithet = "용궁의 임금님"; stats.attribute = SkillAttribute.Blue;
            stats.maxHealth *= 7f; ec.SetLevel(level); stats.ChangeSanity(10, "보스");
            ec.skills = Build(Phase1, 1.15f); ec.basicSkill = ec.skills[0];
            if (brain != null) { brain.personality = EnemyPersonality.Boss; brain.externalIdle = true; brain.home = null; brain.aggroRange = 0f; }
            Attach("Boss/KingCrown", "head"); Attach("Boss/KingCape", "body");
            stats.Died += OnDied;
        }

        void Attach(string path, string bone)
        {
            var prefab = Resources.Load<GameObject>(path); if (prefab == null) { Debug.LogWarning("[임금님] 소품 없음: Resources/" + path); return; }
            var go = BoneAttach.Attach(gameObject, prefab, bone, "King_" + prefab.name); if (go == null) return;
            Seaside.GlowLamps(go); foreach (var r in go.GetComponentsInChildren<Renderer>()) crownGlow.Add(r);
        }

        static List<SkillDefinition> Build(string[] ids, float power)
        {
            var l = new List<SkillDefinition>();
            foreach (var id in ids)
            {
                var src = SkillLibrary.Get(id); if (src == null) continue; var c = Object.Instantiate(src); c.name = id;
                c.basePower = Mathf.RoundToInt(c.basePower * power); l.Add(c);
            }
            return l;
        }

        void Update()
        {
            if (UIState.Paused || defeated || !stats.IsAlive) return;
            if (player == null) { var pl = GameObject.FindGameObjectWithTag("Player"); if (pl == null) return; player = pl.transform; }
            bool inside = AirDome.Inside(player.position); float d = Vector3.Distance(player.position, transform.position);
            bool engage = inside && d < AggroRange;
            if (brain != null) brain.aggroRange = engage ? AggroRange + 4f : 0f;
            if (engage && !introduced) Intro();
            if (!engage) Patrol();
            if (!awakened && stats.HealthRatio <= 0.5f) Awaken();
            if (engage && Time.time >= nextLine) { nextLine = Time.time + Random.Range(14f, 22f); Say(awakened ? AwakeLines : FightLines); }
        }

        float nextApproach;
        void Patrol()
        {
            var pts = palace != null ? palace.Patrol : null; if (pts == null || pts.Length == 0 || ec.Busy) return;
            if (patrolIndex >= pts.Length) patrolIndex = 0;
            Vector3 to = pts[patrolIndex].position - transform.position; to.y = 0;
            if (to.magnitude < 2.6f || Time.time >= nextPatrol) { patrolIndex = NextPoint(patrolIndex, pts.Length); nextPatrol = Time.time + 14f; nextApproach = 0f; }
            if (Time.time >= nextApproach) { nextApproach = Time.time + 1f; ec.Approach(pts[patrolIndex]); }
        }
        /// <summary>결투장 둘레 8곳을 차례로, 가끔(20%) 계단 위(8) → 왕좌의 방(9) 까지 산책했다가 다시 결투장으로.</summary>
        static int NextPoint(int i, int n)
        {
            if (n < 10) return (i + 1) % n;
            if (i == 8) return 9; if (i == 9) return 0;
            return Random.value < 0.2f ? 8 : (i + 1) % 8;
        }

        void Intro()
        {
            introduced = true; Quests.AnnounceNow("용궁의 임금님", "고래고래 임금님");
            Toast.Show("고래고래 임금님: 끼룩! 누구냐! 내 생일 케이크 딸기를 먹은 범인이냐?!");
            nextLine = Time.time + 4f;
        }

        void Awaken()
        {
            awakened = true; stats.epithet = "각성한 무지갯빛 임금님";
            ec.skills = Build(Phase2, 1.2f); ec.basicSkill = ec.skills[0]; ec.moveSpeed *= 1.15f; if (ec.agent) ec.agent.speed = ec.moveSpeed;
            ec.Stagger(1.6f);                                                          // 포효(보스는 경직 ×0.6 → 약 1초)
            Quests.AnnounceNow("각성!", "고래고래 임금님 — 2페이지");
            Toast.Show("고래고래 임금님: 끼룩끼룩!! 이제부터 진짜야! 딸기 맛 좀 봐라!");
            Vector3 c = transform.position + Vector3.up * 2f;
            for (int i = 0; i < 6; i++) SparkBurst.Spawn(c, Quaternion.Euler(0, i * 60f, 0) * (Vector3.forward + Vector3.up), 30, 7f, Color.HSVToRGB(i / 6f, 0.7f, 1f));
            ThirdPersonCamera.Shake(0.25f, 0.8f);
            foreach (var r in crownGlow)
            {
                if (r == null) continue; var ms = r.materials;
                foreach (var m in ms) { m.EnableKeyword("_EMISSION"); var col = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color; m.SetColor("_EmissionColor", col * 1.4f); }
            }
        }

        static readonly string[] FightLines = { "고래고래 임금님: 내 딸기! 내 생일! 끼룩!", "고래고래 임금님: 흥, 제법이구나! 꼬리 맛을 보여 주마!", "고래고래 임금님: 용궁에선 내가 제일 세다고!" };
        static readonly string[] AwakeLines = { "고래고래 임금님: 버블 감옥에 가둬 주마! 뽀글뽀글!", "고래고래 임금님: 딸기 폭탄이다! 피할 수 있으면 피해 봐!", "고래고래 임금님: 무지개 고래빔이다아아!" };
        static void Say(string[] lines) => Toast.Show(lines[Random.Range(0, lines.Length)]);

        void OnDied()
        {
            defeated = true; if (palace != null) palace.KingDefeated();
            Toast.Show("고래고래 임금님: 끼룩… 졌다… 너 정말 세구나!");
            var gm = GameManager.Instance; Inventory inv = null; if (gm != null && gm.player != null) gm.player.TryGetComponent(out inv);
            bool needPearl = inv != null && inv.Count("royal_pearl") == 0 && Quests.Log.State(Quests.GrumpyKing) != QuestState.Completed;
            if (needPearl) ItemPickup.Spawn("royal_pearl", 1, transform.position + Vector3.up * 1.2f, 1.2f);
            Quests.AnnounceNow("결투 승리!", needPearl ? "임금님의 무지개 진주를 받았다" : "고래고래 임금님과 한판 더!");
            StartCoroutineSafe();
        }
        void StartCoroutineSafe() { var h = new GameObject("KingLines").AddComponent<KingAfterLines>(); }
    }

    /// <summary>임금님이 쓰러진 뒤 화해 대사(차례로 몇 초 간격) — 보스가 사라져도 남도록 따로.</summary>
    public class KingAfterLines : MonoBehaviour
    {
        static readonly string[] Lines = { "고래고래 임금님: 사실… 생일 잔치에 아무도 안 와서 심술이 났었어. 끼룩.", "고래고래 임금님: 이건 용궁의 무지개 진주야. 친구가 된 기념으로 줄게!", "고래고래 임금님: 다음 생일엔 꼭 와야 해! 딸기 케이크 같이 먹자!" };
        int i; float next;
        void Start() { next = Time.time + 2.6f; }
        void Update()
        {
            if (UIState.Paused || Time.time < next) return;
            if (i >= Lines.Length) { Destroy(gameObject); return; }
            Toast.Show(Lines[i++]); next = Time.time + 3.2f;
        }
    }
}
