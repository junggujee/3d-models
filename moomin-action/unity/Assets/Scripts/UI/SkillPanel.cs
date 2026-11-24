using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 스킬 패널 v3(보내 주신 컨셉 이미지 기준: 거친 금박선 · 어두운 바탕 · 흑백 먹선 일러스트). 적은 머리 위, 플레이어는 화면 왼쪽 아래, 클래시 때는 화면 가운데(ClashStage).
    ///   위: 큰 <b>+코인 위력</b> · 아이콘 3개 = 이 캐릭터의 <b>힘 / 위력 감소 / 마비</b>(코인 위력에 영향, 실시간)
    ///   가운데: 속성색 거친 육각형 안의 <b>기본 위력</b> · 붓으로 끈 이름 띠 · 거친 테두리 안의 <b>스킬 일러스트</b>
    ///   아래: 팔각 코인 결과 · "적중 시 ○○ n 부여" · 빛나는 <b>최종 위력</b>
    ///   오른쪽: <b>개별 코인 결과</b>(앞면/뒷면 목록)
    /// </summary>
    public class SkillPanel
    {
        public SkillExecutor Owner { get; private set; }
        public bool FollowOwner = true; public float FixedScale = 0.55f;
        /// <summary>이 시각까지는 스킬이 끝났어도 그대로 보여 줌(클래시 연출).</summary>
        public float HoldUntil = -1f;
        /// <summary>투명도 배율(합에서 진 쪽을 흐리게).</summary>
        public float AlphaMul = 1f;
        public void Punch(float amount) { punch = Mathf.Max(punch, amount); }
        RectTransform root; CanvasGroup group;
        Image frame, hexRing, hexGlow, banner, bannerInk, art, artFrame, totalGlow, statusIcon, sideFrame;
        Text plus, skillName, baseText, totalLabel, total, status, attrLine, sideTitle; Text[] statNum = new Text[3]; Image[] statIcon = new Image[3];
        readonly List<Image> coins = new(); readonly List<Image> sideCoins = new(); readonly List<Text> sideText = new(); RectTransform side;
        SkillDefinition shown; float punch;

        public void Build(Transform parent)
        {
            root = NewRect("SkillPanel", parent); root.anchorMin = root.anchorMax = new Vector2(.5f, .5f); root.pivot = new Vector2(.5f, 0); root.sizeDelta = new Vector2(760, 460);
            group = root.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts = false; var tl = new Vector2(0, 1);
            // 본체(어두운 바탕 + 거친 테두리)
            var fill = Img(root, "Fill", SkillPanelArt.Fill, Color.white, true); Set(fill.rectTransform, tl, tl, tl, new Vector2(120, -150), new Vector2(460, 300));
            frame = Img(fill.transform, "Frame", SkillPanelArt.Frame, Color.white, true); Stretch(frame.rectTransform, -6);
            // 일러스트
            artFrame = Img(fill.transform, "ArtFrame", SkillPanelArt.Frame, Color.white, true); Set(artFrame.rectTransform, tl, tl, tl, new Vector2(22, -16), new Vector2(416, 158));
            art = Img(artFrame.transform, "Art", null, Color.white, false); Stretch(art.rectTransform, 8);
            // 이름 띠
            banner = Img(root, "Banner", SkillPanelArt.Banner, new Color(.07f, .06f, .05f, .92f), true); Set(banner.rectTransform, tl, tl, tl, new Vector2(128, -78), new Vector2(470, 74));
            bannerInk = Img(banner.transform, "Ink", SkillPanelArt.BannerInk, Color.white, true); Stretch(bannerInk.rectTransform);
            skillName = StyleSkill(Txt(banner.transform, "", 42, new Color(.98f, .95f, .88f), TextAnchor.MiddleLeft, FontStyle.Normal), new Color(1f, .86f, .55f)); Set(skillName.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(40, 0), new Vector2(-110, 0));
            skillName.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .8f);
            // 육각형(기본 위력)
            hexGlow = Img(root, "HexGlow", SkillPanelArt.Glow, Color.white, false); Set(hexGlow.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(88, -138), new Vector2(230, 230));
            var hex = Img(root, "Hex", SkillPanelArt.HexFill, new Color(.05f, .045f, .04f, .97f), false); Set(hex.rectTransform, tl, tl, tl, new Vector2(10, -60), new Vector2(156, 156));
            hexRing = Img(hex.transform, "Ring", SkillPanelArt.HexRing, Color.white, false); Stretch(hexRing.rectTransform);
            baseText = Txt(hex.transform, "", 74, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(baseText.rectTransform); baseText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3);
            // 위: +코인 위력, 아이콘 3개
            plus = Txt(root, "", 62, Color.white, TextAnchor.LowerLeft, FontStyle.BoldAndItalic); Set(plus.rectTransform, tl, tl, tl, new Vector2(20, 2), new Vector2(140, 72)); plus.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2, -2);
            for (int i = 0; i < 3; i++)
            {
                var ct = StatusInfo.Coin[i];   // 힘 · 위력 감소 · 마비
                statIcon[i] = Img(root, "StatIcon" + i, SkillPanelArt.StatusIcon(ct), SkillPanelArt.StatusTint(ct), false); Set(statIcon[i].rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(184 + i * 60, -12), new Vector2(40, 40));
                statNum[i] = Txt(root, "", 24, Color.white, TextAnchor.UpperCenter, FontStyle.Bold); Set(statNum[i].rectTransform, tl, tl, new Vector2(.5f, 1), new Vector2(184 + i * 60, -32), new Vector2(64, 30));
                statNum[i].gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .8f);
            }
            attrLine = Txt(root, "", 18, UITheme.Cream, TextAnchor.LowerRight); Set(attrLine.rectTransform, tl, tl, new Vector2(1, 1), new Vector2(590, -56), new Vector2(330, 24));
            // 아래: 코인 · 적중 효과 · 최종 위력
            for (int i = 0; i < 5; i++) { var c = Img(root, "Coin" + i, SkillPanelArt.Coin(0), Color.white, false); Set(c.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(166 + i * 50, -380), new Vector2(44, 44)); coins.Add(c); }
            statusIcon = Img(root, "StatusIcon", SkillPanelArt.StatusIcon(StatusType.Bleed), Color.white, false); Set(statusIcon.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(232, -424), new Vector2(34, 34));
            status = Txt(root, "", 22, UITheme.Cream, TextAnchor.MiddleLeft); Set(status.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(148, -424), new Vector2(300, 30)); status.horizontalOverflow = HorizontalWrapMode.Wrap;
            totalGlow = Img(root, "TotalGlow", SkillPanelArt.Glow, Color.white, false); Set(totalGlow.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(500, -395), new Vector2(240, 150));
            totalLabel = Txt(root, "최종 위력", 24, Color.white, TextAnchor.LowerRight, FontStyle.Bold); Set(totalLabel.rectTransform, tl, tl, new Vector2(1, 1), new Vector2(572, -340), new Vector2(200, 30));
            total = Txt(root, "", 84, Color.white, TextAnchor.UpperRight, FontStyle.BoldAndItalic); Set(total.rectTransform, tl, tl, new Vector2(1, 1), new Vector2(580, -366), new Vector2(150, 96));
            total.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3, -3); total.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(5, -5);
            // 오른쪽: 개별 코인 결과
            var sf = Img(root, "Side", SkillPanelArt.Fill, Color.white, true); side = sf.rectTransform; Set(side, tl, tl, tl, new Vector2(592, -186), new Vector2(160, 250));
            sideFrame = Img(sf.transform, "Frame", SkillPanelArt.Frame, Color.white, true); Stretch(sideFrame.rectTransform, -4);
            sideTitle = Txt(sf.transform, "개별 코인 결과", 18, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.Bold); Set(sideTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -8), new Vector2(-10, 28));
            for (int i = 0; i < 5; i++)
            {
                var c = Img(sf.transform, "SC" + i, SkillPanelArt.Coin(1), Color.white, false); Set(c.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(36, -60 - i * 40), new Vector2(30, 30)); sideCoins.Add(c);
                var t = Txt(sf.transform, "", 22, UITheme.Cream, TextAnchor.MiddleLeft); Set(t.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(60, -60 - i * 40), new Vector2(96, 30)); sideText.Add(t);
            }
            group.alpha = 0; root.gameObject.SetActive(false);
        }

        public void Bind(SkillExecutor ex) { Owner = ex; shown = null; }
        public void Release() { Owner = null; shown = null; HoldUntil = -1f; AlphaMul = 1f; root.gameObject.SetActive(false); }
        public void Hide() { root.gameObject.SetActive(false); }
        /// <summary>고정 위치(플레이어용).</summary>
        public void Pin(Vector2 anchor, Vector2 pos) { FollowOwner = false; root.anchorMin = root.anchorMax = anchor; root.pivot = new Vector2(0, 0); root.anchoredPosition = pos; }

        /// <summary>매 프레임: 위치·내용·투명도. 보여 줄 게 없으면 false.</summary>
        public bool Tick(Camera cam, RectTransform canvas, float dt)
        {
            if (Owner == null) return false;
            var s = Owner.IsExecuting ? Owner.CurrentSkill : Owner.LastSkill; float since = Time.time - Owner.LastFinishTime;
            bool held = Time.time < HoldUntil; bool visible = s != null && (Owner.IsExecuting || since < 1.6f || held);
            if (!visible) { root.gameObject.SetActive(false); return false; }
            float scale = FixedScale;
            if (FollowOwner)
            {
                if (cam == null) return false;
                var ecc = Owner.GetComponent<EnemyCombatController>(); float h = ecc != null ? ecc.AimHeight * 2f + 0.5f : 2f;
                Vector3 sp = cam.WorldToScreenPoint(Owner.transform.position + Vector3.up * h); float dist = Vector3.Distance(cam.transform.position, Owner.transform.position);
                if (sp.z <= 0 || dist > 32f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, sp, null, out var lp)) { root.gameObject.SetActive(false); return true; }
                root.anchoredPosition = lp; scale = Mathf.Clamp(6f / Mathf.Max(1f, dist), 0.32f, 0.62f);
            }
            root.gameObject.SetActive(true); root.localScale = Vector3.one * scale * (1f + punch); punch = Mathf.MoveTowards(punch, 0f, dt * 1.5f);
            if (s != shown) Show(s);
            var res = Owner.results; int sum = 0; for (int i = 0; i < res.Count; i++) sum += res[i].power;
            bool broken = !Owner.IsExecuting && !Owner.LastCompleted;
            for (int i = 0; i < coins.Count; i++)
            {
                bool on = i < s.coinCount; coins[i].gameObject.SetActive(on); sideCoins[i].gameObject.SetActive(on && i < res.Count); sideText[i].gameObject.SetActive(on && i < res.Count); if (!on) continue;
                int look = i < res.Count ? (res[i].isHeads ? 1 : 2) : broken ? 3 : 0; coins[i].sprite = SkillPanelArt.Coin(look);
                if (i < res.Count) { sideCoins[i].sprite = SkillPanelArt.Coin(look); sideText[i].text = (res[i].isHeads ? "앞면" : "뒷면") + $"  <size=18>{res[i].power}</size>" + ModTag(res[i].modifiers); }
            }
            side.gameObject.SetActive(res.Count > 0); side.sizeDelta = new Vector2(176, 52 + 40 * Mathf.Max(1, res.Count));
            UpdateModifierRow();
            string t = res.Count == 0 ? "?" : sum.ToString(); if (total.text != t) { if (res.Count > 0) punch = 0.1f; total.text = t; }
            totalLabel.text = !Owner.IsExecuting ? (Owner.LastCompleted ? "최종 위력" : "중단") : Owner.Phase == SkillPhase.Windup ? "공격 예고" : $"위력 {res.Count}/{s.coinCount}";
            group.alpha = (Owner.IsExecuting || held ? 1f : Mathf.Clamp01((1.6f - since) / 0.5f)) * AlphaMul;
            return true;
        }

        /// <summary>위쪽 3칸: 주인의 힘 / 위력 감소 / 마비 현재값(없으면 흐리게 0).</summary>
        void UpdateModifierRow()
        {
            var se = Owner != null ? Owner.status : null;
            for (int i = 0; i < 3; i++)
            {
                var t = StatusInfo.Coin[i]; var e = se != null ? se.Get(t) : null; bool on = e != null && e.Active;
                statNum[i].text = !on ? "0" : t == StatusType.Paralysis ? e.Count.ToString() : (t == StatusType.PowerUp ? "+" : "-") + e.Potency + $"<size=15>×{e.Count}</size>";
                var tint = SkillPanelArt.StatusTint(t); tint.a = on ? 1f : .3f; statIcon[i].color = tint;
                var nc = Color.Lerp(Color.white, StatusInfo.ColorOf(t), .35f); nc.a = on ? 1f : .35f; statNum[i].color = nc;
            }
        }
        static string ModTag(CoinModifiers m)
        {
            if (m.paralyzed) return $" <size=15><color=#{StatusInfo.Hex(StatusType.Paralysis)}>마비</color></size>";
            int d = m.powerUp - m.powerDown; return d == 0 ? "" : $" <size=15><color=#{StatusInfo.Hex(d > 0 ? StatusType.PowerUp : StatusType.PowerDown)}>{(d > 0 ? "+" : "")}{d}</color></size>";
        }

        /// <summary>"출혈 앞 2/1 · 뒤 1/1  진동 폭발" — 강도/횟수, 앞면·뒷면이 같으면 하나만.</summary>
        public static string StatusLine(SkillDefinition s, out StatusType first)
        {
            first = StatusType.None; var sb = new System.Text.StringBuilder(); int n = 0;
            foreach (var t in StatusInfo.All)
            {
                if (!s.InflictsStatus(t)) continue; if (first == StatusType.None) first = t; n++;
                StatusAmount h = s.StatusOn(t, true), l = s.StatusOn(t, false);
                string F(StatusAmount a) => t == StatusType.Paralysis ? a.count.ToString() : a.ToString();
                string amt = h.potency == l.potency && h.count == l.count ? F(h) : (h.IsEmpty ? "" : $"앞 {F(h)}") + (!h.IsEmpty && !l.IsEmpty ? " · " : "") + (l.IsEmpty ? "" : $"뒤 {F(l)}");
                sb.Append((StatusInfo.IsSelf(t) ? "자신 " : "") + $"<color=#{StatusInfo.Hex(t)}>{StatusInfo.Name(t)}</color> {amt}  ");
            }
            if (s.tremorBurstEnabled) { if (first == StatusType.None) first = StatusType.Tremor; sb.Append($"<color=#{StatusInfo.Hex(StatusType.Tremor)}>진동 폭발</color>"); n++; }
            string r = sb.ToString().TrimEnd(); return n > 1 ? "<size=18>" + r + "</size>" : r;
        }

        void Show(SkillDefinition s)
        {
            shown = s; var c = SkillAttributes.ColorOf(s.attribute); var light = Color.Lerp(c, Color.white, 0.35f); var deep = c * 0.55f; deep.a = 1f;
            frame.color = artFrame.color = sideFrame.color = light; hexRing.color = light; hexGlow.color = new Color(c.r, c.g, c.b, .4f); totalGlow.color = new Color(c.r, c.g, c.b, .45f);
            bannerInk.color = light; skillName.text = s.skillName;
            baseText.text = s.basePower.ToString(); baseText.color = new Color(.97f, .94f, .86f); baseText.GetComponent<Outline>().effectColor = deep;
            plus.text = "+" + s.coinPower; plus.color = light; plus.GetComponent<Outline>().effectColor = deep;
            UpdateModifierRow();
            attrLine.text = SkillAttributes.Names[(int)s.attribute] + " 속성 · " + SkillAttributes.WeaknessText(s.attribute) + (s.isAreaAttack ? $" · <color=#E8C97A>광역</color>" : "") + (s.isProjectile ? " · <color=#9BE07A>원거리</color>" : "") + (!s.canBeGuarded ? " · <color=#C080FF>방어 불가</color>" : "") + (!s.canBeEvaded ? " · <color=#C080FF>회피 불가</color>" : "") + (s.energyCost > 0 ? $" · <color=#6FE0F0>에너지 {s.energyCost}</color>" : "");
            art.sprite = SkillIllustration.Get(s);
            string line = StatusLine(s, out var first); bool hasStatus = line.Length > 0; statusIcon.gameObject.SetActive(hasStatus);
            if (hasStatus) { statusIcon.sprite = SkillPanelArt.StatusIcon(first); statusIcon.color = SkillPanelArt.StatusTint(first); status.text = "적중 시      " + line; }
            else status.text = s.description != null && s.description.Length > 0 ? "<size=18>" + s.description + "</size>" : "";
            totalLabel.color = light; total.color = Color.Lerp(Color.white, c, 0.55f); total.GetComponent<Outline>().effectColor = deep; total.text = "?"; punch = 0.15f;
        }
    }

    /// <summary>스킬을 쓰는 적마다 SkillPanel 을 머리 위에 붙여 줌(풀링).</summary>
    public class EnemySkillPanels
    {
        readonly List<SkillPanel> active = new(), pool = new(); Transform parent; RectTransform canvas;
        public void Build(Transform hudRoot) { parent = hudRoot; canvas = (RectTransform)hudRoot.GetComponentInParent<Canvas>().transform; }

        public void Update(float dt)
        {
            var cam = Camera.main;
            foreach (var e in EnemyCombatController.All)
            {
                if (e == null || e.Executor == null || ClashStage.OnStage.Contains(e.Executor)) continue; var ex = e.Executor;
                bool want = ex.IsExecuting || (ex.LastSkill != null && Time.time - ex.LastFinishTime < 1.6f); if (!want) continue;
                bool has = false; foreach (var p in active) if (p.Owner == ex) { has = true; break; }
                if (!has) { var p = pool.Count > 0 ? pool[pool.Count - 1] : null; if (p != null) pool.RemoveAt(pool.Count - 1); else { p = new SkillPanel(); p.Build(parent); } p.Bind(ex); active.Add(p); }
            }
            for (int i = active.Count - 1; i >= 0; i--) { var p = active[i]; if (p.Owner == null || ClashStage.OnStage.Contains(p.Owner) || !p.Tick(cam, canvas, dt)) { p.Release(); active.RemoveAt(i); pool.Add(p); } }
        }
    }
}
