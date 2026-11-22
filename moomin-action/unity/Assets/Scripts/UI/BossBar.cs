using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 보스바(화면 아래 가운데, 엘든링풍): 왼쪽 보스 문장 메달 · 이름 + 별칭 · 넓은 쇠 틀 체력바(맞으면 빨간 조각) · 금색 경직 게이지 · 상태이상 · % · 오른쪽 물결 장식.
    /// EnemyCombatController.showBossBar 인 적이 40m 안에 있으면 가장 가까운 적을 보여 줌(고래고래 · 웨이브 보스).
    /// 등장: 아래에서 떠오르며 체력이 0 → 현재 값까지 차오름 · 격파: 하얗게 번쩍이며 "격파" 가 박히고 사라짐.
    /// 그림: tools/boss_bar_art.py → Resources/UI/Bars/Boss*.png
    /// </summary>
    public class BossBar
    {
        public float showRange = 40f;
        RectTransform root, fill, chunkRt, stag; CanvasGroup group; Image fillImg, chunkImg, stagImg, emblem, flash; Text nameText, pctText, defeat; readonly Image[] icons = new Image[StatusInfo.All.Length];
        EnemyCombatController target; Transform player; readonly DamageChunk chunk = new(); float introT = -1f, deathT = -1f, shown;
        static Sprite Load(string n, Vector4 border, float ppu = 200f)
        {
            var tex = Resources.Load<Texture2D>("UI/Bars/" + n); if (tex == null) return null; tex.wrapMode = TextureWrapMode.Clamp;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), ppu, 0, SpriteMeshType.FullRect, border);
        }

        public void Build(Transform hudRoot, Transform playerTf)
        {
            player = playerTf; var bc = new Vector2(.5f, 0);
            root = NewRect("BossBar", hudRoot); Set(root, bc, bc, new Vector2(.5f, 0), new Vector2(0, 178), new Vector2(1040, 100)); group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false;
            var bar = NewRect("Bar", root); Set(bar, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(30, 18), new Vector2(-120, 40));
            var back = Img(bar, "Back", BarArt.Back, new Color(.6f, .6f, .7f, 1f), false); Stretch(back.rectTransform, 7);
            var inner = NewRect("Inner", bar); Stretch(inner, 7);
            chunkImg = Img(inner, "Chunk", BarArt.Fill, new Color(.95f, .15f, .1f), false); chunkRt = chunkImg.rectTransform; Stretch(chunkRt);
            fillImg = Img(inner, "Fill", BarArt.Fill, Color.white, false); fill = fillImg.rectTransform; Stretch(fill);
            var frameSp = Load("BossFrame", new Vector4(26, 26, 26, 26)); var frame = Img(bar, "Frame", frameSp != null ? frameSp : UITheme.Bar, Color.white, true); Stretch(frame.rectTransform, -6);
            flash = Box(inner, "Flash", new Color(1, 1, 1, 0)); Stretch(flash.rectTransform);
            // 경직 게이지(바 아래 가는 금색 줄)
            var sb = Img(root, "StaggerBack", BarArt.Back, Color.white, false); Set(sb.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(30, 6), new Vector2(-180, 7));
            stagImg = Box(sb.transform, "Stagger", new Color(.95f, .8f, .3f)); stag = stagImg.rectTransform; Stretch(stag);
            // 오른쪽 끝 장식 · 왼쪽 문장 메달
            var capSp = Load("BossCap", Vector4.zero, 100f); if (capSp != null) { var cap = Img(root, "Cap", capSp, Color.white, false); Set(cap.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0, .5f), new Vector2(-62, 38), new Vector2(64, 80)); }
            emblem = Img(root, "Emblem", null, Color.white, false); Set(emblem.rectTransform, Vector2.zero, Vector2.zero, new Vector2(.5f, .5f), new Vector2(58, 40), new Vector2(104, 104));
            // 이름 · 별칭(위 왼쪽) · 상태이상 · %(위 오른쪽)
            nameText = Txt(root, "", 32, UITheme.Cream, TextAnchor.LowerLeft, FontStyle.Bold); Set(nameText.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(120, 60), new Vector2(-400, 44));
            nameText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            pctText = Txt(root, "", 22, UITheme.GoldBright, TextAnchor.LowerRight, FontStyle.Bold); Set(pctText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-96, 60), new Vector2(120, 34));
            pctText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            for (int i = 0; i < icons.Length; i++) { icons[i] = Img(root, "Status" + i, SkillPanelArt.StatusIcon(StatusInfo.All[i]), SkillPanelArt.StatusTint(StatusInfo.All[i]), false); Set(icons[i].rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(.5f, .5f), new Vector2(-240 - i * 32, 76), new Vector2(30, 30)); }
            defeat = Txt(root, "격파", 72, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.BoldAndItalic); Set(defeat.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 0), new Vector2(0, 8), new Vector2(600, 90));
            defeat.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3); defeat.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }

        public void Update(float dt)
        {
            if (deathT >= 0f) { Death(dt); return; }
            var want = Pick();
            if (want != target) { if (target != null && target.Stats != null && !target.Stats.IsAlive) { deathT = 0f; return; } Show(want); }
            if (target == null) { if (root.gameObject.activeSelf) { group.alpha = Mathf.MoveTowards(group.alpha, 0f, dt * 3f); if (group.alpha <= 0f) root.gameObject.SetActive(false); } return; }
            if (!target.Stats.IsAlive) { deathT = 0f; return; }
            var st = target.Stats; float r = st.HealthRatio;
            if (introT >= 0f)   // 등장: 떠오르며 0 → 현재 체력까지 차오름
            {
                introT += dt; float u = Mathf.Clamp01(introT / 0.9f); group.alpha = Mathf.Clamp01(introT / 0.3f);
                root.anchoredPosition = new Vector2(0, 178 - 40 * (1 - Mathf.Clamp01(introT / 0.35f)));
                shown = r * (1 - (1 - u) * (1 - u)); fill.anchorMax = new Vector2(shown, 1); chunkRt.anchorMin = chunkRt.anchorMax = new Vector2(shown, 1);
                if (u >= 1f) { introT = -1f; chunk.Set(r); chunk.Advance(10f); }
            }
            else
            {
                chunk.Set(r); chunk.Advance(dt); fill.anchorMax = new Vector2(r, 1);
                chunkRt.anchorMin = new Vector2(r, 0); chunkRt.anchorMax = new Vector2(Mathf.Max(r, chunk.Top), 1);
                var cc = Color.Lerp(new Color(.95f, .15f, .1f), Color.white, chunk.Flash * .6f); cc.a = chunk.Alpha; chunkImg.color = cc;
            }
            pctText.text = Mathf.CeilToInt(r * 100f) + "%";
            stag.anchorMax = new Vector2(st.IsStaggerBroken ? 1f : st.StaggerGauge.Ratio, 1); stagImg.color = st.IsStaggerBroken ? Color.Lerp(UITheme.GoldBright, Color.white, .5f + .5f * Mathf.Sin(Time.time * 14f)) : new Color(.95f, .8f, .3f);
            var se = target.GetComponent<StatusEffectManager>(); int k = 0;
            for (int i = 0; i < icons.Length; i++) { var e = se != null ? se.Get(StatusInfo.All[i]) : null; bool on = e != null && e.Active; icons[i].gameObject.SetActive(on); if (on) icons[i].rectTransform.anchoredPosition = new Vector2(-240 - k++ * 32, 76); }
        }

        EnemyCombatController Pick()
        {
            if (player == null) return null; EnemyCombatController best = null; float bd = showRange * showRange;
            if (target != null && target.Stats != null && target.Stats.IsAlive && (target.transform.position - player.position).sqrMagnitude < bd * 1.3f) return target;   // 한 번 잡으면 조금 멀어져도 유지
            foreach (var e in EnemyCombatController.All)
            {
                if (e == null || !e.showBossBar || e.Stats == null || !e.Stats.IsAlive) continue;
                float d = (e.transform.position - player.position).sqrMagnitude; if (d < bd) { bd = d; best = e; }
            }
            return best;
        }

        void Show(EnemyCombatController e)
        {
            target = e; if (e == null) return;
            root.gameObject.SetActive(true); defeat.gameObject.SetActive(false); introT = 0f; group.alpha = 0f; flash.color = new Color(1, 1, 1, 0);
            var st = e.Stats; nameText.text = st.displayName + (string.IsNullOrEmpty(st.epithet) ? "" : $"   <size=20><color=#C9B27A>{st.epithet}</color></size>");
            var em = Resources.Load<Texture2D>("UI/Bars/BossEmblem_" + (string.IsNullOrEmpty(e.bossEmblem) ? "Default" : e.bossEmblem)); if (em == null) em = Resources.Load<Texture2D>("UI/Bars/BossEmblem_Default");
            emblem.sprite = em != null ? Sprite.Create(em, new Rect(0, 0, em.width, em.height), new Vector2(.5f, .5f), 100f) : UITheme.Slot;
            fillImg.color = e.bossEmblem == "Whale" ? new Color(.22f, .58f, .98f) : new Color(.78f, .14f, .12f);   // 고래 = 바다색, 그 밖의 보스 = 핏빛
            chunk.Set(0f); chunk.Advance(10f);
        }

        /// <summary>격파: 체력이 비고 하얗게 번쩍 → "격파" 가 크게 박힘 → 2초 뒤 사라짐.</summary>
        void Death(float dt)
        {
            deathT += dt; fill.anchorMax = new Vector2(0, 1); chunkRt.anchorMin = chunkRt.anchorMax = new Vector2(0, 1); pctText.text = "0%";
            flash.color = new Color(1, 1, 1, Mathf.Clamp01(1f - deathT / 0.5f) * .8f);
            defeat.gameObject.SetActive(true); float s = deathT < .2f ? Mathf.Lerp(1.8f, .95f, deathT / .2f) : Mathf.Lerp(.95f, 1f, Mathf.Clamp01((deathT - .2f) / .1f));
            defeat.rectTransform.localScale = Vector3.one * s; defeat.color = new Color(1f, .86f, .45f, Mathf.Clamp01(deathT / .1f));
            group.alpha = deathT < 2.2f ? 1f : Mathf.Clamp01(1f - (deathT - 2.2f) / .5f);
            if (deathT > 2.7f) { deathT = -1f; target = null; root.gameObject.SetActive(false); defeat.gameObject.SetActive(false); }
        }
    }
}
