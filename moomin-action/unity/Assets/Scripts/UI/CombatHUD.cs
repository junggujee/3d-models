using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 전투 HUD(명세 v2): 정신력 게이지(-45~+45)·상태·앞면 확률, 하단 스킬 슬롯(좌클릭/Q/E/R: 코인 수·쿨타임·필요 정신력),
    /// CoinDisplay(현재 스킬의 공격별 코인·위력), 클래시 결과, 조준 대상(이름·체력·AI 행동·적 코인 결과), 조준 표식, 알림.
    /// </summary>
    public class CombatHUD
    {
        class SlotUI { public SkillDefinition skill; public Text name, cd, cost, affinity; public RectTransform cdFill; public Image frame; }
        UIRoot ui; CharacterStats stats; SkillExecutor ex; CombatManager cm; LockOnSystem lockOn;
        RectTransform sanityPos, sanityNeg, canvasRect; Text sanityLabel; readonly List<SlotUI> slots = new(); CoinDisplay coinDisplay; EnemySkillPanels enemyPanels;
        GameObject clashPanel; CanvasGroup clashGroup; Text clashTitle, clashRows, clashResult; ClashOutcome clash; float clashT;
        GameObject targetPanel; Text targetName, targetIntent, targetCoins; Gauge targetHp; float targetTrail = 1f; Image lockMarker; EnemyCombatController lastTarget;
        readonly List<string> enemyCoins = new(); float enemyCoinT; Text popup; float popupT;
        System.Action<ClashOutcome> onClash; System.Action<AttackContext> onEnemyCoin; System.Action<AttackContext, CharacterStats, int> onHit;

        public void Build(UIRoot ui, Transform hudRoot, Transform core)
        {
            this.ui = ui; var p = ui.player.gameObject; stats = ui.player; ex = p.GetComponent<SkillExecutor>(); cm = p.GetComponent<CombatManager>(); lockOn = p.GetComponent<LockOnSystem>();
            canvasRect = (RectTransform)hudRoot.GetComponentInParent<Canvas>().transform; var tl = new Vector2(0, 1);
            // 정신력
            var sg = Img(core, "Sanity", UITheme.Bar, Color.white); Set(sg.rectTransform, tl, tl, tl, new Vector2(44, -98), new Vector2(380, 14));
            var inner = NewRect("Inner", sg.transform); Stretch(inner, 3);
            sanityNeg = Box(inner, "Neg", new Color(.35f, .5f, .95f)).rectTransform; sanityPos = Box(inner, "Pos", UITheme.Gold).rectTransform;
            foreach (var r in new[] { sanityNeg, sanityPos }) { r.offsetMin = r.offsetMax = Vector2.zero; r.anchorMin = new Vector2(.5f, 0); r.anchorMax = new Vector2(.5f, 1); }
            var mid = Box(inner, "Mid", UITheme.Cream); Set(mid.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(.5f, .5f), Vector2.zero, new Vector2(2, 4));
            sanityLabel = Txt(core, "", 22, UITheme.Cream); Set(sanityLabel.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(436, -105), new Vector2(560, 30));
            // 스킬 슬롯
            var bc = new Vector2(.5f, 0); string[] keys = { "좌클릭", "Q", "E", "R" };
            for (int i = 0; i < 4; i++)
            {
                var u = new SlotUI { skill = cm != null ? cm.SlotSkill(i) : null };
                u.frame = Img(core, "Skill" + i, UITheme.Slot, Color.white, true); Set(u.frame.rectTransform, bc, bc, bc, new Vector2(-189 + i * 126, 30), new Vector2(114, 114));
                var key = Txt(u.frame.transform, keys[i], i == 0 ? 18 : 28, UITheme.Gold, TextAnchor.UpperLeft, FontStyle.Bold); Set(key.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(8, -4), Vector2.zero);
                u.name = Txt(u.frame.transform, u.skill != null ? u.skill.skillName : "", 20, UITheme.Cream, TextAnchor.LowerCenter); Set(u.name.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(0, 8), new Vector2(-8, 0));
                u.cost = Txt(u.frame.transform, u.skill != null && u.skill.sanityCost > 0 ? "정신 " + u.skill.sanityCost : "", 16, new Color(.6f, .7f, 1f), TextAnchor.UpperRight); Set(u.cost.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(-8, -6), Vector2.zero);
                int n = u.skill != null ? u.skill.coinCount : 0;
                for (int c = 0; c < n; c++) { var pip = Img(u.frame.transform, "Pip", CoinDisplay.Coin(CoinDisplay.Look.Heads), Color.white, false); Set(pip.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2((c - (n - 1) * .5f) * 18, 8), new Vector2(16, 16)); }
                var ov = Box(u.frame.transform, "Cooldown", new Color(0, 0, 0, .7f)); u.cdFill = ov.rectTransform; Set(u.cdFill, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, Vector2.zero);
                u.cd = Txt(u.frame.transform, "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(u.cd.rectTransform);
                u.affinity = Txt(u.frame.transform, "", 18, Color.white, TextAnchor.LowerCenter, FontStyle.Bold); Set(u.affinity.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 0), new Vector2(0, 4), new Vector2(0, 24)); slots.Add(u);   // 조준 대상과의 상성
            }
            enemyPanels = new EnemySkillPanels(); enemyPanels.Build(hudRoot);   // 적 스킬 패널(머리 위)
            coinDisplay = new CoinDisplay(); coinDisplay.Build(hudRoot, ex, stats);
            // 클래시
            var kp = Img(hudRoot, "ClashPanel", UITheme.Frame, Color.white); clashPanel = kp.gameObject; clashGroup = clashPanel.AddComponent<CanvasGroup>(); At(kp.rectTransform, new Vector2(.5f, .7f), Vector2.zero, new Vector2(620, 240));
            clashTitle = Txt(kp.transform, "클래시", 34, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold); Set(clashTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(-40, 44));
            clashRows = Txt(kp.transform, "", 30, UITheme.Cream, TextAnchor.UpperCenter); Set(clashRows.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(0, -8), new Vector2(-40, -110));
            clashResult = Txt(kp.transform, "", 30, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); Set(clashResult.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 16), new Vector2(-40, 40));
            clashGroup.alpha = 0;
            // 조준 대상
            var tp = Img(hudRoot, "TargetPanel", UITheme.Slot, new Color(1, 1, 1, .92f)); targetPanel = tp.gameObject; At(tp.rectTransform, new Vector2(.5f, 1), new Vector2(0, -28), new Vector2(640, 112)); tp.rectTransform.pivot = new Vector2(.5f, 1);
            targetName = Txt(tp.transform, "", 26, UITheme.GoldBright, TextAnchor.MiddleLeft, FontStyle.Bold); Set(targetName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -6), new Vector2(-36, 32));
            targetHp = MakeGauge(tp.transform, "HP", UITheme.Red, new Vector2(600, 14), true, false); Set(targetHp.root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -42), new Vector2(604, 14));
            targetIntent = Txt(tp.transform, "", 21, UITheme.Cream, TextAnchor.MiddleLeft); Set(targetIntent.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(18, 28), new Vector2(-36, 28));
            targetCoins = Txt(tp.transform, "", 21, UITheme.Muted, TextAnchor.MiddleRight); Set(targetCoins.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(18, 4), new Vector2(-36, 26));
            targetPanel.SetActive(false);
            lockMarker = Img(hudRoot, "LockMarker", UITheme.Slot, UITheme.GoldBright, true); At(lockMarker.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30)); lockMarker.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); lockMarker.gameObject.SetActive(false);
            popup = Txt(hudRoot, "", 34, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold, "Popup"); At(popup.rectTransform, new Vector2(.5f, .44f), Vector2.zero, new Vector2(800, 50));

            ex.SkillRefused += (s, why) => Popup(s.skillName + " — " + why, UITheme.Muted);
            stats.Evaded += () => Popup("회피", UITheme.Cream);
            stats.SanityChanged += (a, b, why) => { if (why == "패링" || why.Contains("앞면")) Popup(why + "  " + (b > a ? "+" : "") + (b - a), UITheme.GoldBright); };
            onClash = o => { clash = o; clashT = 0f; }; ClashManager.Clashed += onClash;
            onHit = (a, target, dmg) => { if (a.owner == ex && a.affinity != 0) Popup(a.affinity > 0 ? $"약점!  ×{SkillAttributes.AdvantageDamage:0.00}" : $"저항  ×{SkillAttributes.DisadvantageDamage:0.0}", a.affinity > 0 ? new Color(.5f, 1f, .5f) : new Color(1f, .5f, .5f)); };
            CombatResolver.DamageDealt += onHit;
            onEnemyCoin = a => { enemyCoins.Add(a.isHeads ? $"<color=#E8C97A>앞 {a.power}</color>" : $"<color=#999999>뒤 {a.power}</color>"); if (enemyCoins.Count > 5) enemyCoins.RemoveAt(0); enemyCoinT = 3f; };
        }

        /// <summary>정적 이벤트·적 구독 해제(씬 재시작 시 파괴된 UI 를 건드리지 않도록).</summary>
        public void Dispose() { if (onClash != null) ClashManager.Clashed -= onClash; if (onHit != null) CombatResolver.DamageDealt -= onHit; if (lastTarget != null) lastTarget.Executor.CoinAttacked -= onEnemyCoin; }
        void Popup(string t, Color c) { popup.text = t; popup.color = c; popupT = 1.2f; }

        public void Update(float dt)
        {
            float v = stats.currentSanity / 45f; sanityPos.anchorMax = new Vector2(.5f + Mathf.Max(0, v) * .5f, 1); sanityNeg.anchorMin = new Vector2(.5f + Mathf.Min(0, v) * .5f, 0);
            var st = stats.Sanity; sanityLabel.text = $"정신력 {(stats.currentSanity >= 0 ? "+" : "")}{stats.currentSanity}  ·  {CharacterStats.SanityLabel(st)}  ·  앞면 {Mathf.RoundToInt(stats.GetCoinProbability() * 100)}%";
            sanityLabel.color = st == SanityState.Focus ? UITheme.GoldBright : st == SanityState.Stable ? UITheme.Cream : st == SanityState.Unstable ? new Color(.6f, .7f, 1f) : new Color(.95f, .35f, .35f);
            var lockT = lockOn != null ? lockOn.Target : null; SkillAttribute? tAttr = lockT != null ? SkillAttributes.DefenderAttribute(lockT.Stats) : (SkillAttribute?)null;
            foreach (var u in slots)
            {
                if (u.skill == null) continue;
                int aff = tAttr.HasValue ? SkillAttributes.Affinity(u.skill.attribute, tAttr.Value) : 0;
                u.affinity.text = aff > 0 ? "▲ 유리" : aff < 0 ? "▼ 불리" : ""; u.affinity.color = aff > 0 ? new Color(.5f, 1f, .5f) : new Color(1f, .45f, .45f); float cd = ex.CooldownLeft(u.skill), k = u.skill.cooldown > 0 ? cd / u.skill.cooldown : 0f; bool lacking = stats.currentSanity < u.skill.sanityCost;
                u.cdFill.anchorMax = new Vector2(1, lacking ? 1 : k); u.cd.text = cd > 0 ? Mathf.CeilToInt(cd).ToString() : ""; u.cost.color = lacking ? new Color(.95f, .4f, .4f) : new Color(.6f, .7f, 1f);
                u.frame.color = ex.CurrentSkill == u.skill || (u == slots[0] && ex.CurrentSkill != null && ex.CurrentSkill.skillId.StartsWith("BASIC")) ? UITheme.GoldBright : Color.Lerp(Color.white, SkillAttributes.ColorOf(u.skill.attribute), 0.55f);
            }
            coinDisplay.Update(dt); enemyPanels.Update(dt);
            // 클래시
            if (clash != null)
            {
                clashT += dt; int shown = Mathf.Min(clash.rounds.Count, 1 + (int)(clashT / 0.2f)); var sb = new System.Text.StringBuilder();
                for (int i = Mathf.Max(0, shown - 4); i < shown; i++) { var r = clash.rounds[i]; string pc = r.player > r.enemy ? "#F2D27F" : "#BBBBBB", ec = r.enemy > r.player ? "#E06060" : "#BBBBBB"; sb.AppendLine(clash.mode == ClashMode.LimbusRounds ? $"<color={pc}>{r.player}</color> <size=18>({r.pc})</size>  :  <size=18>({r.ec})</size> <color={ec}>{r.enemy}</color>" : $"<color={pc}>{r.player}</color>   :   <color={ec}>{r.enemy}</color>"); }
                clashTitle.text = (clash.parry ? "패링" : "클래시") + $"   <size=22><color=#CFC4A8>{clash.playerName}  vs  {clash.enemyName}</color></size>" + (clash.playerAffinity > 0 ? "  <size=20><color=#80FF80>상성 유리 +2</color></size>" : clash.playerAffinity < 0 ? "  <size=20><color=#FF7070>상성 불리</color></size>" : ""); clashRows.text = clash.parry ? "" : sb.ToString();
                bool done = clashT > clash.rounds.Count * 0.2f;
                clashResult.text = !done ? "" : clash.result == ClashResult.PlayerWin ? (clash.parry ? "막아 냈다 — 반격 기회" : "승리 — 공격 유지") : clash.result == ClashResult.EnemyWin ? (clash.parry ? "<color=#E06060>막혔다 — 경직</color>" : "<color=#E06060>패배 — 공격 중단</color>") : "<color=#CFC4A8>무승부 — 양쪽 공격 종료</color>";
                float end = clash.rounds.Count * 0.2f + 1.2f; clashGroup.alpha = clashT < end ? 1f : Mathf.Clamp01(1f - (clashT - end) / 0.4f); if (clashT > end + 0.4f) clash = null;
            }
            else clashGroup.alpha = 0f;
            // 조준 대상
            var t = lockOn != null ? lockOn.Target : null; targetPanel.SetActive(t != null); lockMarker.gameObject.SetActive(false);
            if (t != lastTarget) { if (lastTarget != null) lastTarget.Executor.CoinAttacked -= onEnemyCoin; enemyCoins.Clear(); targetTrail = 1f; lastTarget = t; if (t != null) t.Executor.CoinAttacked += onEnemyCoin; }
            if (t != null)
            {
                float r = t.Stats.HealthRatio; targetHp.SetFill(r); targetTrail = r < targetTrail ? Mathf.MoveTowards(targetTrail, r, .4f * dt) : r; targetHp.SetTrail(targetTrail);
                targetName.text = t.Stats.displayName + (t.Stats.IsVulnerable ? "   <color=#E8C97A><size=20>경직 — 반격 기회</size></color>" : "") + $"   <size=18><color=#9E9178>정신력 {t.Stats.currentSanity:+0;-0;0} · {SkillAttributes.Names[(int)t.Stats.attribute]} 속성 · {SkillAttributes.WeaknessText(t.Stats.attribute)}</color></size>";
                var s = t.Executor.CurrentSkill; var brain = t.GetComponent<EnemyBrain>();
                if (s != null) targetIntent.text = (t.Executor.Phase == SkillPhase.Windup ? "<color=#E06060>예고</color> " : "<color=#E06060>공격</color> ") + $"{s.skillName} · 기본 {s.basePower} · 코인 {s.coinCount}개 +{s.coinPower}" + (s.unblockable ? "  <color=#B070FF>방어 불가</color>" : "");
                else targetIntent.text = t.IsStaggered ? "경직" : t.Stats.Guarding ? "방어" : t.IsDodging ? "회피" : brain != null ? brain.LastAction switch { EnemyAction.Approach => "접근", EnemyAction.Attack => "공격 준비", EnemyAction.Guard => "방어", EnemyAction.Dodge => "회피", _ => "관찰" } : "";
                if (enemyCoinT > 0) enemyCoinT -= dt; targetCoins.text = enemyCoinT > 0 ? "적 코인  " + string.Join("  ", enemyCoins) : "";
                var cam = Camera.main;
                if (cam != null) { Vector3 sp = cam.WorldToScreenPoint(lockOn.TargetPoint); if (sp.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var lp)) { lockMarker.gameObject.SetActive(true); lockMarker.rectTransform.anchoredPosition = lp; } }
            }
            if (popupT > 0) { popupT -= dt; var c = popup.color; c.a = Mathf.Clamp01(popupT / 0.4f); popup.color = c; } else popup.text = "";
        }
    }
}
