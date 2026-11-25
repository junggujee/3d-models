using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace MoominAction
{
    /// <summary>
    /// 마을 NPC — 조랭하프족(blender/build_joraeng.py). 가까이 가면 "F  조랭이와 대화" → 대화창(UI/DialogueBox).
    ///  · 평소: 집 둘레를 천천히 거닐다 멈춰 두리번(Idle·Walk), 민미가 다가오면 바라보고 가끔 손 흔들기(Wave)
    ///  · 대화 중: 민미를 바라보고 Talk, 대사마다 표정 동작(긍정 Yes · 부정 No · 웃음 Laugh · 슬픔 Sad · 당황 Surprised · 수락 QuestAccept · 완료 QuestComplete)
    ///    게임이 멈춘 동안에도 움직이도록 애니메이터를 실제 시간으로
    ///  · 머리 위 표시: 줄 퀘스트가 있으면 금색 "!", 진행 중 "?"(다 모았으면 밝게), 끝나면 없음
    ///  · 목도리 색·표시(scarfColor/scarf) — 주민마다 다르게. 털 색 살짝(furTint)
    /// 대화 내용: questId 가 있으면 Story/JoraengStory 의 퀘스트 대화, 없으면 chatter(주민 잡담) 중 하나.
    /// </summary>
    public class Npc : MonoBehaviour
    {
        public static readonly List<Npc> All = new();
        public string npcName = "조랭이", portrait = "joraeng", place = "조랭하프 마을";
        public string questId;
        /// <summary>정해진 대화(항구 어부 등) — 있으면 퀘스트·잡담 대신 이것.</summary>
        public System.Func<Npc, Dialogue> story;
        [Tooltip("잡담: \"표정|동작|대사\" (표정 normal·happy·sad·surprised·troubled, 동작 Talk·Yes·No·Laugh·Sad·Surprised·Wave)")]
        public string[] chatter = new string[0];
        public bool scarf = true; public Color scarfColor = new Color(0.86f, 0.3f, 0.27f); public Color furTint = Color.white;
        public float interactRange = 2.8f, wanderRadius = 3f, walkSpeed = 0.9f;

        Animator anim; NavMeshAgent agent; Transform player; Vector3 home; float nextWander, nextWave, resumeTalkAt = -1; bool talking; int chatIndex;
        Transform marker; Text markerText;
        static readonly HashSet<string> OneShots = new() { "Wave", "Yes", "No", "Laugh", "Sad", "Surprised", "QuestAccept", "QuestComplete" };
        static readonly Dictionary<string, float> Length = new() { { "Wave", 1.33f }, { "Yes", 1f }, { "No", 1.2f }, { "Laugh", 1.5f }, { "Sad", 1.66f }, { "Surprised", 1.2f }, { "QuestAccept", 1.33f }, { "QuestComplete", 1.66f } };

        public bool Talking => talking;
        public QuestDef Quest => Quests.Get(questId);
        /// <summary>토벌 퀘스트(반복) — questId 가 HuntCatalog 의 id 일 때.</summary>
        public HuntDef Hunt => HuntCatalog.Get(questId);

        void Awake()
        {
            anim = GetComponentInChildren<Animator>(); TryGetComponent<NavMeshAgent>(out agent); home = transform.position;
            if (agent != null) { agent.speed = walkSpeed; agent.angularSpeed = 240f; agent.stoppingDistance = 0.1f; agent.radius = 0.32f; agent.height = 0.95f; }
            ApplyLook(); BuildMarker();
        }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void Start() { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) player = p.transform; nextWander = Time.time + Random.Range(2f, 6f); }

        /// <summary>목도리 색·표시, 털 색(재질 복사 없이 MaterialPropertyBlock).</summary>
        public void ApplyLook()
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                bool isScarf = r.name.Contains("Scarf");
                if (isScarf) { r.enabled = scarf; var mb = new MaterialPropertyBlock(); r.GetPropertyBlock(mb); mb.SetColor("_Color", scarfColor); r.SetPropertyBlock(mb); }
                else if (furTint != Color.white)
                {
                    var mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        if (mats[i] == null || !mats[i].name.Contains("Fur")) continue;
                        var mb = new MaterialPropertyBlock(); r.GetPropertyBlock(mb, i); mb.SetColor("_Color", mats[i].color * furTint); r.SetPropertyBlock(mb, i);
                    }
                }
            }
        }

        void BuildMarker()
        {
            var go = new GameObject("QuestMarker", typeof(RectTransform)); go.transform.SetParent(transform, false); go.transform.localPosition = new Vector3(0, 1.32f, 0);
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.WorldSpace; var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(100, 100); rt.localScale = Vector3.one * 0.006f;
            var tgo = new GameObject("Mark", typeof(RectTransform)); tgo.transform.SetParent(go.transform, false); var t = tgo.AddComponent<Text>(); ((RectTransform)tgo.transform).sizeDelta = new Vector2(100, 100);
            t.font = UI.UITheme.DisplayFont; t.fontSize = 84; t.alignment = TextAnchor.MiddleCenter; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            var o = tgo.AddComponent<Outline>(); o.effectColor = new Color(0.2f, 0.11f, 0.04f, 0.9f); o.effectDistance = new Vector2(3, -3);
            marker = go.transform; markerText = t;
        }

        void Update()
        {
            if (anim != null) anim.updateMode = talking ? AnimatorUpdateMode.UnscaledTime : AnimatorUpdateMode.Normal;
            if (talking) { TalkingTick(); return; }
            if (UIState.Paused) return;
            if (player == null) { Start(); if (player == null) return; }
            float d = Vector3.Distance(transform.position, player.position);
            if (d <= interactRange) Interaction.Offer(this, d, $"F   {PetSeal.Josa(npcName, "과", "와")} 대화", Talk);
            if (d < 5f)
            {
                StopWalk(); Face(player.position, 3f);
                if (Time.time >= nextWave && d > 2.2f) { nextWave = Time.time + 14f; Play("Wave"); }
            }
            else Wander();
            if (anim != null) anim.SetFloat("Speed", agent != null && agent.enabled ? Mathf.Clamp01(agent.velocity.magnitude / Mathf.Max(0.1f, walkSpeed)) : 0f);
        }

        void LateUpdate()
        {
            if (marker == null) return;
            var q = Quest; var h = Hunt; string m = ""; Color c = UI.UITheme.GoldBright;
            if (h != null)
            {   // 토벌: 처음 = 금색 "!", 다시 받을 수 있음 = 하늘색 "!", 진행 중 "?"(다 잡으면 밝게)
                if (!Hunts.Log.IsActive(h.id)) { m = "!"; if (Hunts.Log.Clears(h.id) > 0) c = new Color(0.55f, 0.85f, 1f); }
                else { m = "?"; c = Hunts.Ready(h) ? UI.UITheme.GoldBright : new Color(0.75f, 0.72f, 0.66f); }
            }
            else if (q != null)
            {
                var st = Quests.Log.State(q.id);
                if (st == QuestState.NotStarted) m = "!";
                else if (st == QuestState.Active) { m = "?"; c = Quests.CanTurnIn(q) ? UI.UITheme.GoldBright : new Color(0.75f, 0.72f, 0.66f); }
            }
            bool show = m.Length > 0 && !talking; markerText.enabled = show; if (!show) return;
            markerText.text = m; markerText.color = c;
            marker.localPosition = new Vector3(0, 1.32f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.05f, 0);
            var cam = Camera.main; if (cam != null) marker.rotation = Quaternion.LookRotation(marker.position - cam.transform.position);
        }

        void Wander()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
            if (Time.time < nextWander) return; nextWander = Time.time + Random.Range(5f, 10f);
            var p = home + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * wanderRadius;
            if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas)) { agent.isStopped = false; agent.SetDestination(hit.position); }
        }
        void StopWalk() { if (agent != null && agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); } }
        void Face(Vector3 target, float speed)
        {
            var dir = target - transform.position; dir.y = 0; if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
        }

        // ── 대화 ───────────────────────────
        public void Talk()
        {
            var ui = UI.UIRoot.Instance; if (ui == null) return;
            var hunt = Hunt;
            var dlg = story != null ? story(this) : hunt != null ? HuntStory.Build(this, hunt) : questId != null && questId.Length > 0 ? JoraengStory.Quest(this) : JoraengStory.Chatter(this, chatIndex++);
            if (dlg == null) return;
            talking = true; StopWalk(); Play("Talk");
            if (!ui.StartDialogue(dlg)) { talking = false; resumeTalkAt = -1; Play("Idle"); }
        }
        public void EndTalk() { talking = false; resumeTalkAt = -1; Play("Idle"); nextWave = Time.time + 20f; }

        void TalkingTick()
        {
            if (player != null) Face(player.position, 6f);
            if (resumeTalkAt > 0 && Time.unscaledTime >= resumeTalkAt) { resumeTalkAt = -1; Play("Talk"); }
        }

        /// <summary>표정 동작(한 번) — 끝나면 대화 중이면 Talk 로 돌아감. Talk·Idle 은 그대로 반복.</summary>
        public void Play(string state)
        {
            if (anim == null || string.IsNullOrEmpty(state) || anim.runtimeAnimatorController == null) return;
            int h = Animator.StringToHash(state);
            if (state == "Idle") { if (anim.HasState(0, Animator.StringToHash("Locomotion"))) anim.CrossFadeInFixedTime("Locomotion", 0.2f); return; }
            if (!anim.HasState(0, h)) return;
            anim.CrossFadeInFixedTime(h, 0.15f);
            resumeTalkAt = talking && OneShots.Contains(state) ? Time.unscaledTime + (Length.TryGetValue(state, out var len) ? len : 1f) : -1;
        }
    }
}
