using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 구래성 노역장의 돌고래 죄수(억울하게 잡혀 온 돌고래들). 고래고래 모델 × 0.4(GameManager.dolphinPrefab — MoominSetup) + 곡괭이 + 흑백 죄수 띠.
    /// 헤엄치지 않음 — 짧게 "통통통" 뛰어서 옮겨 다님(포물선 콩콩 + 착지 때 찌부).
    ///  · Busy: 노역장을 분주히 통통 돌아다니다 멈춰서 두리번
    ///  · Mine: 가까운 바위 앞에서 곡괭이질(박치기 동작 Attack1 + 돌가루) 반복, 가끔 자리 옮김
    ///  · Caged: 감방 안에 박혀 있음 — 제자리 콩콩 · 철창 쪽을 보며 한숨
    /// F 대화: 억울하게 잡혀 온 하소연 또는 "광석 얻을 거야 끼룩!!!" — 귀여운 감옥 죄수 대사(Story/DolphinStory)
    /// </summary>
    public class DolphinPrisoner : MonoBehaviour
    {
        public enum Mode { Busy, Mine, Caged }
        public Mode mode; public int number; public Vector3 home; public float roam = 6f; public Transform rock;
        Animator anim; Transform player; Vector3 hopFrom, hopTo; float hopT = -1f, wait, nextSwing, hopsLeft, bounceSquash; Vector3 baseScale;
        const float HopTime = 0.32f, HopHeight = 0.32f, HopLen = 0.7f;

        public void Setup(Mode m, int no, Vector3 at, Transform nearRock)
        {
            mode = m; number = no; home = at; rock = nearRock; baseScale = transform.localScale;
            anim = GetComponentInChildren<Animator>(); wait = Random.Range(0.2f, 2f);
            NpcAccessory.Attach(gameObject, "Npc/DolphinPickaxe", "flipper.R"); NpcAccessory.Attach(gameObject, "Npc/DolphinStripes", "body");
        }
        void Play(string s) { if (anim != null && anim.runtimeAnimatorController != null && anim.HasState(0, Animator.StringToHash(s))) anim.CrossFadeInFixedTime(s, 0.1f); }

        void Update()
        {
            if (UIState.Paused) { if (anim != null) anim.updateMode = AnimatorUpdateMode.UnscaledTime; return; }
            if (anim != null) anim.updateMode = AnimatorUpdateMode.Normal;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) player = p.transform; }
            if (player != null)
            {
                Vector3 d = player.position - transform.position; d.y = 0;
                if (d.magnitude < 2.4f && Mathf.Abs(player.position.y - transform.position.y) < 2f) Interaction.Offer(this, d.magnitude, $"F   돌고래 죄수 {number}번과 이야기", Talk);
            }
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 10f * Time.deltaTime);
            if (hopT >= 0f) { HopTick(); return; }
            wait -= Time.deltaTime; if (wait > 0f) { if (mode == Mode.Mine) MineTick(); return; }
            switch (mode)
            {
                case Mode.Busy:
                    if (hopsLeft <= 0) { hopsLeft = Random.Range(3, 7); var tgt = home + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) * roam; StartHops(tgt); }
                    else NextHop();
                    break;
                case Mode.Mine:
                    if (rock != null && FlatDist(transform.position, rock.position) > 1.7f) { hopsLeft = 99; StartHops(rock.position + (transform.position - rock.position).normalized * 1.3f); }
                    else { wait = Random.Range(4f, 9f); FaceTo(rock != null ? rock.position : transform.position + transform.forward); }
                    break;
                case Mode.Caged:
                    hopsLeft = Random.Range(1, 3); StartHops(home + new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-0.6f, 0.6f)) * 1.2f);
                    break;
            }
        }
        Vector3 goal;
        void StartHops(Vector3 tgt) { goal = tgt; NextHop(); }
        void NextHop()
        {
            Vector3 d = goal - transform.position; d.y = 0;
            if (d.magnitude < 0.25f || hopsLeft <= 0) { hopsLeft = 0; wait = mode == Mode.Caged ? Random.Range(2f, 5f) : Random.Range(0.8f, 2.5f); return; }
            hopsLeft--; hopFrom = transform.position; hopTo = transform.position + d.normalized * Mathf.Min(HopLen, d.magnitude);
            if (Physics.Raycast(hopTo + Vector3.up * 2f, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore)) hopTo.y = hit.point.y;
            FaceTo(hopTo); hopT = 0f;
        }
        void HopTick()
        {
            hopT += Time.deltaTime / HopTime; float t = Mathf.Clamp01(hopT);
            var p = Vector3.Lerp(hopFrom, hopTo, t); p.y += Mathf.Sin(t * Mathf.PI) * HopHeight; transform.position = p;
            transform.localScale = Vector3.Scale(baseScale, new Vector3(1f - 0.08f * Mathf.Sin(t * Mathf.PI), 1f + 0.12f * Mathf.Sin(t * Mathf.PI), 1f));   // 위로 늘어남
            if (t >= 1f)
            {
                hopT = -1f; transform.localScale = Vector3.Scale(baseScale, new Vector3(1.12f, 0.82f, 1.05f));                // 착지 찌부
                if (hopsLeft > 0) wait = 0.06f; else wait = Random.Range(0.6f, 1.8f);
            }
        }
        void MineTick()
        {
            if (rock == null || Time.time < nextSwing) return;
            nextSwing = Time.time + Random.Range(1.6f, 2.4f); FaceTo(rock.position); Play("Attack1");
            SparkBurst.Spawn(rock.position + Vector3.up * 0.6f, Vector3.up, 8, 3f, new Color(0.72f, 0.58f, 0.44f));
        }
        void FaceTo(Vector3 at) { Vector3 f = at - transform.position; f.y = 0; if (f.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(f); }
        static float FlatDist(Vector3 a, Vector3 b) { a.y = b.y = 0; return Vector3.Distance(a, b); }

        void Talk()
        {
            var ui = UI.UIRoot.Instance; if (ui == null) return;
            if (player != null) FaceTo(player.position);
            ui.StartDialogue(DolphinStory.Build(this));
        }
    }
}
