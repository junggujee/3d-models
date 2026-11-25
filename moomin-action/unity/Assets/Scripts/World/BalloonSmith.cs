using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 풍선고래 대장장이(blender/build_balloon_whale.py → Models/BalloonWhale.fbx).
    /// 풍선이라 바닥에서 조금 떠서 둥실둥실. 평소에는 모루 앞에서 망치질(Hammer), 플레이어가 오면 돌아보고 F 로 대화 → 대장간 화면.
    /// 대장간 화면에서 성공/실패에 따라 Cheer · Fail 동작을 보여 준다(UI/SmithPage 가 Play 를 부름).
    /// </summary>
    public class BalloonSmith : MonoBehaviour
    {
        public static BalloonSmith Current { get; private set; }
        Animator anim; Smithy shop; Transform player; Quaternion home;
        bool talking; float nextIdle, bob;

        public void Setup(Smithy s)
        {
            shop = s; Current = this; home = transform.rotation;
            anim = GetComponentInChildren<Animator>();
            if (!TryGetComponent<CapsuleCollider>(out var col)) { col = gameObject.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 0.75f, 0); col.radius = 0.5f; col.height = 1.4f; }
            Play("Hammer");
        }
        void OnDestroy() { if (Current == this) Current = null; }

        public void Play(string state)
        {
            if (anim == null || anim.runtimeAnimatorController == null || !anim.HasState(0, Animator.StringToHash(state))) return;
            anim.CrossFadeInFixedTime(state, 0.15f);
        }

        void Update()
        {
            if (UIState.Paused) { if (anim != null) anim.updateMode = AnimatorUpdateMode.UnscaledTime; return; }
            if (anim != null) anim.updateMode = AnimatorUpdateMode.Normal;
            if (talking) talking = false;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p == null) return; player = p.transform; }
            float d = Vector3.Distance(player.position, transform.position);
            if (d < 6f)
            {
                Vector3 to = player.position - transform.position; to.y = 0;
                if (to.sqrMagnitude > 0.1f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 3f * Time.deltaTime);
                Interaction.Offer(this, d, "F   풍선고래에게 맡기기", Talk);
                if (Time.time >= nextIdle) { nextIdle = Time.time + 6f; Play("Talk"); }
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, home, 2f * Time.deltaTime);
                if (Time.time >= nextIdle) { nextIdle = Time.time + 2.2f; Play("Hammer"); }   // 모루에 망치질
            }
        }

        void Talk()
        {
            var ui = UI.UIRoot.Instance; if (ui == null) return;
            if (ui.StartDialogue(SmithStory.Greet(this))) { talking = true; Play("Talk"); }
        }
        /// <summary>대장간 화면 열기(대화 선택지에서).</summary>
        public void OpenForge(int tab) { var ui = UI.UIRoot.Instance; if (ui != null) UI.SmithPage.Open(ui, this, tab); }
    }
}
