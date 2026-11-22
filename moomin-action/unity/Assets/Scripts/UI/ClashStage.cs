using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 클래시 연출: 화면을 살짝 어둡게 하고 가운데에 내 스킬 패널(왼쪽)과 적 스킬 패널(오른쪽)을 나란히 크게 띄움.
    /// 아래에 위력 비교와 결과, 화면 모서리에 붉은 긁힌 자국. 그동안 같은 캐릭터의 다른 패널(머리 위·왼쪽 아래)은 숨김(OnStage).
    /// 게임은 멈추지 않음(실시간) — 연출 시간 동안만 보였다가 사라짐.
    /// </summary>
    public class ClashStage
    {
        /// <summary>지금 가운데에 나와 있는 캐릭터(다른 패널은 이들을 건너뜀).</summary>
        public static readonly HashSet<SkillExecutor> OnStage = new();
        public float duration = 1.9f, panelScale = 0.74f;
        RectTransform root, canvas; CanvasGroup group; Image dim, slashTL, slashBR; Text vs, result;
        readonly SkillPanel left = new(), right = new(); float t = -1f; ClashOutcome shown;

        public void Build(Transform hudRoot)
        {
            canvas = (RectTransform)hudRoot.GetComponentInParent<Canvas>().transform;
            root = NewRect("ClashStage", hudRoot); Stretch(root); group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false;
            dim = Box(root, "Dim", new Color(0, 0, 0, .45f)); Stretch(dim.rectTransform);
            var red = new Color(.72f, .05f, .07f, .9f);
            slashTL = Img(root, "SlashTL", SkillPanelArt.Slashes, red, false); Set(slashTL.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(640, 640));
            slashBR = Img(root, "SlashBR", SkillPanelArt.Slashes, red, false); Set(slashBR.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 1), Vector2.zero, new Vector2(640, 640)); slashBR.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
            left.Build(root); right.Build(root);
            left.Pin(new Vector2(.5f, .5f), new Vector2(-640, -110)); right.Pin(new Vector2(.5f, .5f), new Vector2(60, -110));
            left.FixedScale = right.FixedScale = panelScale;
            vs = Txt(root, "", 40, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic); At(vs.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -150), new Vector2(900, 54)); vs.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            result = Txt(root, "", 34, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); At(result.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -200), new Vector2(900, 48)); result.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            root.gameObject.SetActive(false);
        }

        public bool Active => t >= 0f;

        public void Show(ClashOutcome o)
        {
            if (o == null || o.playerAttack == null || o.enemyAttack == null) return;
            Clear(); shown = o; t = 0f; root.gameObject.SetActive(true);
            var pe = o.playerAttack.owner; var ee = o.enemyAttack.owner;
            left.Bind(pe); right.Bind(ee); left.HoldUntil = right.HoldUntil = Time.time + duration; OnStage.Add(pe); OnStage.Add(ee);
            string pc = o.result == ClashResult.PlayerWin ? "#F2D27F" : "#BBBBBB", ec = o.result == ClashResult.EnemyWin ? "#E06060" : "#BBBBBB";
            vs.text = $"<color={pc}>{o.playerPower}</color>   <size=28>VS</size>   <color={ec}>{o.enemyPower}</color>"
                    + (o.playerAffinity > 0 ? "   <size=22><color=#80FF80>상성 유리 +2</color></size>" : o.playerAffinity < 0 ? "   <size=22><color=#FF7070>상성 불리</color></size>" : "")
                    + (o.mode == ClashMode.LimbusRounds ? $"   <size=22>{o.rounds.Count}라운드</size>" : "");
            result.text = o.result == ClashResult.PlayerWin ? "합 승리 — 공격 유지" : o.result == ClashResult.EnemyWin ? "<color=#E06060>합 패배 — 공격 중단</color>" : "<color=#CFC4A8>무승부 — 양쪽 공격 종료</color>";
        }

        public void Update(float dt)
        {
            if (t < 0f) return;
            t += dt; float a = t < .15f ? t / .15f : t > duration - .35f ? Mathf.Clamp01((duration - t) / .35f) : 1f;
            group.alpha = a; dim.color = new Color(0, 0, 0, .45f * a);
            float slide = Mathf.Clamp01(t / .2f); slashTL.rectTransform.anchoredPosition = new Vector2(-60 * (1 - slide), 60 * (1 - slide)); slashBR.rectTransform.anchoredPosition = new Vector2(60 * (1 - slide), -60 * (1 - slide));
            result.gameObject.SetActive(t > .35f);   // 결과는 패널이 뜬 뒤에
            left.Tick(null, canvas, dt); right.Tick(null, canvas, dt);
            if (t >= duration) Clear();
        }

        void Clear()
        {
            if (left.Owner != null) OnStage.Remove(left.Owner); if (right.Owner != null) OnStage.Remove(right.Owner);
            left.Release(); right.Release(); t = -1f; shown = null; if (root != null) root.gameObject.SetActive(false);
        }
        public void Dispose() { Clear(); OnStage.Clear(); }
    }
}
