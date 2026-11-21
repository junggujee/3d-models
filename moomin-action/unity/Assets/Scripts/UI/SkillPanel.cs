using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 적이 스킬을 쓸 때 머리 위에 뜨는 스킬 패널(림버스 스타일).
    ///   위:  +코인 위력 · 위력 범위(최소~최대)
    ///   가운데: 속성색 육각형 안의 기본 위력 · 속성색 이름 띠 · 스킬 일러스트
    ///   맨 아래(일러스트 밑): 코인별 결과와 <b>최종 합계 위력</b>(코인이 판정될 때마다 갱신)
    /// </summary>
    public class SkillPanel
    {
        public SkillExecutor Owner { get; private set; }
        RectTransform root; CanvasGroup group; Image hexFill, hexRing, banner, art, frame; Text basePower, plus, range, skillName, total, totalLabel, attr;
        readonly List<Image> coins = new(); SkillDefinition shown; float punch;

        public void Build(Transform parent)
        {
            root = NewRect("SkillPanel", parent); root.anchorMin = root.anchorMax = new Vector2(.5f, .5f); root.pivot = new Vector2(.5f, 0); root.sizeDelta = new Vector2(380, 300);
            group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false;
            var tl = new Vector2(0, 1);
            // 이름 띠(육각형 뒤)
            banner = Img(root, "Banner", SkillIllustration.Banner, Color.white, true); Set(banner.rectTransform, tl, tl, tl, new Vector2(70, -42), new Vector2(300, 46));
            skillName = Txt(banner.transform, "", 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold); Set(skillName.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(18, 0), new Vector2(-40, 0));
            attr = Txt(root, "", 16, UITheme.Cream, TextAnchor.MiddleRight); Set(attr.rectTransform, tl, tl, tl, new Vector2(150, -18), new Vector2(220, 24));
            // 일러스트
            frame = Img(root, "ArtFrame", UITheme.Slot, Color.white, true); Set(frame.rectTransform, tl, tl, tl, new Vector2(96, -94), new Vector2(270, 112));
            art = Img(frame.transform, "Art", null, Color.white, false); Stretch(art.rectTransform, 4); art.preserveAspect = false;
            // 육각형(기본 위력)
            hexFill = Img(root, "Hex", SkillIllustration.Hex, new Color(0.08f, 0.07f, 0.07f, 0.95f), false); Set(hexFill.rectTransform, tl, tl, tl, new Vector2(0, -16), new Vector2(104, 104));
            hexRing = Img(hexFill.transform, "Ring", SkillIllustration.HexRing, Color.white, false); Stretch(hexRing.rectTransform);
            basePower = Txt(hexFill.transform, "", 50, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(basePower.rectTransform); basePower.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2, -2);
            // 위: +코인 위력, 범위
            plus = Txt(root, "", 30, Color.white, TextAnchor.LowerLeft, FontStyle.Bold); Set(plus.rectTransform, tl, tl, tl, new Vector2(4, 22), new Vector2(80, 36));
            range = Txt(root, "", 20, Color.white, TextAnchor.LowerLeft, FontStyle.Bold); Set(range.rectTransform, tl, tl, tl, new Vector2(70, 18), new Vector2(160, 30));
            // 맨 아래(일러스트 밑): 코인 결과 + 최종 합계 위력
            for (int i = 0; i < 5; i++) { var c = Img(root, "Coin" + i, CoinDisplay.Coin(CoinDisplay.Look.Pending), Color.white, false); Set(c.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(112 + i * 30, -228), new Vector2(26, 26)); coins.Add(c); }
            totalLabel = Txt(root, "최종 위력", 16, UITheme.Muted, TextAnchor.LowerRight); Set(totalLabel.rectTransform, tl, tl, new Vector2(1, 1), new Vector2(366, -210), new Vector2(140, 22));
            total = Txt(root, "", 54, Color.white, TextAnchor.UpperRight, FontStyle.Bold); Set(total.rectTransform, tl, tl, new Vector2(1, 1), new Vector2(368, -236), new Vector2(200, 66)); total.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2, -2);
            group.alpha = 0; root.gameObject.SetActive(false);
        }

        public void Bind(SkillExecutor ex) { Owner = ex; shown = null; }
        public void Release() { Owner = null; shown = null; root.gameObject.SetActive(false); }

        /// <summary>매 프레임: 위치(머리 위)·내용·투명도. 보여 줄 게 없으면 false.</summary>
        public bool Tick(Camera cam, RectTransform canvas, float dt)
        {
            if (Owner == null || cam == null) return false;
            var s = Owner.IsExecuting ? Owner.CurrentSkill : Owner.LastSkill; float since = Time.time - Owner.LastFinishTime;
            bool visible = s != null && (Owner.IsExecuting || since < 1.6f) && Owner.stats != null;
            if (!visible) { root.gameObject.SetActive(false); return false; }
            var ecc = Owner.GetComponent<EnemyCombatController>(); float h = ecc != null ? ecc.AimHeight * 2f + 0.5f : 2f;
            Vector3 sp = cam.WorldToScreenPoint(Owner.transform.position + Vector3.up * h); float dist = Vector3.Distance(cam.transform.position, Owner.transform.position);
            if (sp.z <= 0 || dist > 32f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, sp, null, out var lp)) { root.gameObject.SetActive(false); return true; }
            root.gameObject.SetActive(true); root.anchoredPosition = lp + new Vector2(-150, 0); root.localScale = Vector3.one * Mathf.Clamp(9f / Mathf.Max(1f, dist), 0.55f, 1f) * (1f + punch);
            punch = Mathf.MoveTowards(punch, 0f, dt * 1.5f);
            if (s != shown) Show(s);
            // 코인 결과·최종 합계 위력
            var res = Owner.results; int sum = 0; for (int i = 0; i < res.Count; i++) sum += res[i].power;
            for (int i = 0; i < coins.Count; i++)
            {
                bool on = i < s.coinCount; coins[i].gameObject.SetActive(on); if (!on) continue;
                coins[i].sprite = CoinDisplay.Coin(i < res.Count ? (res[i].isHeads ? CoinDisplay.Look.Heads : CoinDisplay.Look.Tails) : (!Owner.IsExecuting && !Owner.LastCompleted ? CoinDisplay.Look.Broken : CoinDisplay.Look.Pending));
            }
            string t = res.Count == 0 ? "?" : sum.ToString(); if (total.text != t) { if (res.Count > 0) punch = 0.12f; total.text = t; }
            totalLabel.text = !Owner.IsExecuting ? (Owner.LastCompleted ? "최종 위력" : "중단") : Owner.Phase == SkillPhase.Windup ? "공격 예고" : $"위력 {res.Count}/{s.coinCount}";
            group.alpha = Owner.IsExecuting ? 1f : Mathf.Clamp01((1.6f - since) / 0.5f);
            return true;
        }

        void Show(SkillDefinition s)
        {
            shown = s; var c = SkillAttributes.ColorOf(s.attribute); var light = Color.Lerp(c, Color.white, 0.35f);
            banner.color = new Color(c.r * 0.8f, c.g * 0.8f, c.b * 0.8f, 0.95f); hexRing.color = light; frame.color = Color.Lerp(Color.white, c, 0.5f);
            basePower.text = s.basePower.ToString(); basePower.GetComponent<Outline>().effectColor = c * 0.6f;
            plus.text = "+" + s.coinPower; plus.color = light; range.text = $"{s.basePower * s.coinCount}  {(s.basePower + s.coinPower) * s.coinCount}"; range.color = light;
            skillName.text = s.skillName; attr.text = SkillAttributes.Names[(int)s.attribute] + " 속성 · " + SkillAttributes.WeaknessText(s.attribute) + (s.unblockable ? " · 방어 불가" : "");
            art.sprite = SkillIllustration.Get(s); art.color = Color.Lerp(Color.white, c, 0.15f);
            total.color = light; total.GetComponent<Outline>().effectColor = c * 0.5f; total.text = "?"; punch = 0.2f;
        }
    }

    /// <summary>스킬을 쓰는 적마다 SkillPanel 을 붙여 줌(풀링).</summary>
    public class EnemySkillPanels
    {
        readonly List<SkillPanel> active = new(), pool = new(); Transform parent; RectTransform canvas;
        public void Build(Transform hudRoot) { parent = hudRoot; canvas = (RectTransform)hudRoot.GetComponentInParent<Canvas>().transform; }

        public void Update(float dt)
        {
            var cam = Camera.main;
            foreach (var e in EnemyCombatController.All)
            {
                if (e == null || e.Executor == null) continue; var ex = e.Executor;
                bool want = ex.IsExecuting || (ex.LastSkill != null && Time.time - ex.LastFinishTime < 1.6f); if (!want) continue;
                bool has = false; foreach (var p in active) if (p.Owner == ex) { has = true; break; }
                if (!has) { var p = pool.Count > 0 ? pool[pool.Count - 1] : null; if (p != null) pool.RemoveAt(pool.Count - 1); else { p = new SkillPanel(); p.Build(parent); } p.Bind(ex); active.Add(p); }
            }
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                if (p.Owner == null || !p.Tick(cam, canvas, dt)) { p.Release(); active.RemoveAt(i); pool.Add(p); }
            }
        }
    }
}
