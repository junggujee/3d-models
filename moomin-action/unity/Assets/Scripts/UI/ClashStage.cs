using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 합 연출 HUD: 화면을 살짝 어둡게 하고 위쪽 가운데에 내 스킬 패널(왼쪽)과 적 스킬 패널(오른쪽)을 나란히 띄움(캐릭터가 싸우는 화면 가운데는 비워 둠).
    /// 부딪힐 때마다 위력 숫자가 튀어나오고(ClashExchange), 끝나면 "합 승리 / 합 패배" 배너가 크게 박힘(ClashEnded). 모서리에 붉은 긁힌 자국.
    /// 그동안 같은 캐릭터의 다른 패널(머리 위·왼쪽 아래)은 숨김(OnStage). 게임은 멈추지 않음.
    /// </summary>
    public class ClashStage
    {
        /// <summary>지금 가운데에 나와 있는 캐릭터(다른 패널은 이들을 건너뜀).</summary>
        public static readonly HashSet<SkillExecutor> OnStage = new();
        public float panelScale = 0.62f, afterEnd = 1.6f, dimAlpha = 0.28f;
        RectTransform root, canvas, bannerRoot; CanvasGroup group, bannerGroup; Image dim, slashTL, slashBR, bannerGlow, bannerStreak; Text vs, banner, bannerSub;
        readonly SkillPanel left = new(), right = new(); float t = -1f, endT = -1f, vsPunch, bannerT = -1f; ClashOutcome shown;

        public void Build(Transform hudRoot)
        {
            canvas = (RectTransform)hudRoot.GetComponentInParent<Canvas>().transform;
            root = NewRect("ClashStage", hudRoot); Stretch(root); group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false;
            dim = Box(root, "Dim", new Color(0, 0, 0, dimAlpha)); Stretch(dim.rectTransform);
            var red = new Color(.72f, .05f, .07f, .9f);
            slashTL = Img(root, "SlashTL", SkillPanelArt.Slashes, red, false); Set(slashTL.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(640, 640));
            slashBR = Img(root, "SlashBR", SkillPanelArt.Slashes, red, false); Set(slashBR.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, 1), Vector2.zero, new Vector2(640, 640)); slashBR.rectTransform.localRotation = Quaternion.Euler(0, 0, 180);
            left.Build(root); right.Build(root);
            left.Pin(new Vector2(.5f, .5f), new Vector2(-560, 70)); right.Pin(new Vector2(.5f, .5f), new Vector2(90, 70));
            left.FixedScale = right.FixedScale = panelScale;
            vs = Txt(root, "", 44, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic); At(vs.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 40), new Vector2(900, 60)); vs.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            // 결과 배너(아래쪽 1/3): 빛 번짐 + 가로 빛줄기 + 큰 글자 + 작은 설명
            bannerRoot = NewRect("Banner", root); At(bannerRoot, new Vector2(.5f, .5f), new Vector2(0, -250), new Vector2(900, 160)); bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
            bannerGlow = Img(bannerRoot, "Glow", SkillPanelArt.Glow, new Color(1f, .8f, .3f, .55f), false); At(bannerGlow.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(760, 220));
            bannerStreak = Box(bannerRoot, "Streak", new Color(1f, .9f, .55f, .85f)); At(bannerStreak.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -6), new Vector2(900, 4));
            banner = StyleDisplay(Txt(bannerRoot, "", 88, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Normal), true); Stretch(banner.rectTransform); banner.rectTransform.offsetMax = new Vector2(0, 20);
            banner.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3); banner.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5, -6);
            bannerSub = Txt(bannerRoot, "", 26, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.Bold); At(bannerSub.rectTransform, new Vector2(.5f, 0), new Vector2(0, -6), new Vector2(900, 34)); bannerSub.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            root.gameObject.SetActive(false);
        }

        public bool Active => t >= 0f;

        /// <summary>합 시작: 패널 두 개를 띄우고 위력은 아직 "?".</summary>
        public void Show(ClashOutcome o)
        {
            if (o == null || o.playerAttack == null || o.enemyAttack == null) return;
            Clear(); shown = o; t = 0f; endT = -1f; bannerT = -1f; root.gameObject.SetActive(true); bannerRoot.gameObject.SetActive(false);
            var pe = o.playerAttack.owner; var ee = o.enemyAttack.owner;
            left.Bind(pe); right.Bind(ee); left.HoldUntil = right.HoldUntil = float.MaxValue; OnStage.Add(pe); OnStage.Add(ee);
            vs.text = "<color=#BBBBBB>?</color>   <size=30>VS</size>   <color=#BBBBBB>?</color>" + Affinity(o);
        }

        /// <summary>부딪힘: 라운드 결과가 있으면 숫자 공개, 숫자·패널이 튀어나옴.</summary>
        public void Exchange(ClashOutcome o, int index, int round)
        {
            if (o != shown) return;
            vsPunch = 0.45f; left.Punch(0.08f); right.Punch(0.08f);
            if (round < 0 || round >= o.rounds.Count) return;
            var r = o.rounds[round]; string pc = r.player > r.enemy ? "#F2D27F" : "#BBBBBB", ec = r.enemy > r.player ? "#E06060" : "#BBBBBB";
            string coins = o.mode == ClashMode.LimbusRounds ? $"<size=22> ({r.pc})</size>" : ""; string ecoins = o.mode == ClashMode.LimbusRounds ? $"<size=22>({r.ec}) </size>" : "";
            vs.text = $"<color={pc}>{r.player}</color>{coins}   <size=30>VS</size>   {ecoins}<color={ec}>{r.enemy}</color>" + Affinity(o);
        }

        /// <summary>결과: 승리/패배 배너가 크게 박히고, 진 쪽 패널은 흐려짐.</summary>
        public void End(ClashOutcome o)
        {
            if (o != shown) return;
            endT = t; bannerT = 0f; bannerRoot.gameObject.SetActive(true); left.HoldUntil = right.HoldUntil = Time.time + afterEnd;
            bool win = o.result == ClashResult.PlayerWin, lose = o.result == ClashResult.EnemyWin;
            banner.text = win ? "합 승리" : lose ? "합 패배" : "무승부";
            bannerSub.text = win ? "공격 유지 — 그대로 명중" : lose ? "공격 중단 — 경직" : "양쪽 공격 종료";
            Color c = win ? new Color(1f, .84f, .35f) : lose ? new Color(.95f, .3f, .28f) : new Color(.8f, .78f, .72f);
            banner.color = Color.Lerp(c, Color.white, .25f); banner.GetComponent<Outline>().effectColor = c * .35f + new Color(0, 0, 0, .65f);
            bannerGlow.color = new Color(c.r, c.g, c.b, .55f); bannerStreak.color = new Color(c.r, c.g, c.b, .9f);
            if (win) { left.Punch(0.18f); right.AlphaMul = .45f; } else if (lose) { right.Punch(0.18f); left.AlphaMul = .45f; }
        }

        static string Affinity(ClashOutcome o) => o.playerAffinity > 0 ? "\n<size=22><color=#80FF80>상성 유리 +2</color></size>" : o.playerAffinity < 0 ? "\n<size=22><color=#FF7070>상성 불리</color></size>" : "";

        public void Update(float dt)
        {
            if (t < 0f) return;
            t += dt;
            float fadeIn = Mathf.Clamp01(t / .15f), fadeOut = endT < 0f ? 1f : Mathf.Clamp01((afterEnd - (t - endT)) / .35f), a = fadeIn * fadeOut;
            group.alpha = a; dim.color = new Color(0, 0, 0, dimAlpha * a);
            float slide = Mathf.Clamp01(t / .2f); slashTL.rectTransform.anchoredPosition = new Vector2(-60 * (1 - slide), 60 * (1 - slide)); slashBR.rectTransform.anchoredPosition = new Vector2(60 * (1 - slide), -60 * (1 - slide));
            vs.rectTransform.localScale = Vector3.one * (1f + vsPunch); vsPunch = Mathf.MoveTowards(vsPunch, 0f, dt * 2.5f);
            if (bannerT >= 0f)
            {
                bannerT += dt;   // 크게 → 살짝 작게 튕김 → 1 (박히는 느낌)
                float u = Mathf.Clamp01(bannerT / .22f), s = bannerT < .22f ? Mathf.Lerp(1.9f, .92f, u * u) : Mathf.Lerp(.92f, 1f, Mathf.Clamp01((bannerT - .22f) / .12f));
                banner.rectTransform.localScale = Vector3.one * s; bannerGroup.alpha = Mathf.Clamp01(bannerT / .08f);
                bannerStreak.rectTransform.sizeDelta = new Vector2(900 * Mathf.Clamp01(bannerT / .3f), 4); bannerGlow.rectTransform.localScale = Vector3.one * (1f + .25f * Mathf.Sin(Mathf.Min(bannerT, 1f) * 9f) * Mathf.Max(0, 1 - bannerT));
            }
            left.Tick(null, canvas, dt); right.Tick(null, canvas, dt);
            if (endT >= 0f && t - endT >= afterEnd) Clear();
            else if (endT < 0f && t > 8f) Clear();   // 안전장치: 결과가 오지 않으면(캐릭터 사망 등) 정리
        }

        void Clear()
        {
            if (left.Owner != null) OnStage.Remove(left.Owner); if (right.Owner != null) OnStage.Remove(right.Owner);
            left.Release(); right.Release(); t = -1f; endT = -1f; bannerT = -1f; shown = null; if (root != null) root.gameObject.SetActive(false);
        }
        public void Dispose() { Clear(); OnStage.Clear(); }
    }
}
