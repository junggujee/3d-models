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
        class SlotUI { public SkillDefinition skill; public Text name, cd, cost, affinity; public RectTransform cdFill; public Image frame; public readonly List<Image> pips = new(); }
        System.Action<DefenseController, bool, int, int> onGuard; RectTransform heatRoot; Image heatFill; Text heatText;
        UIRoot ui; CharacterStats stats; SkillExecutor ex; CombatManager cm; LockOnSystem lockOn;
        RectTransform sanityPos, sanityNeg, canvasRect; Text sanityLabel; readonly List<SlotUI> slots = new(); EnemySkillPanels enemyPanels; SkillPanel playerPanel; Text playerStatus; RectTransform canvasForPanels;
        GameObject clashPanel; CanvasGroup clashGroup; Text clashTitle, clashRows, clashResult; ClashOutcome clash; float clashT;
        GameObject targetPanel; Text targetName, targetIntent, targetCoins; Gauge targetHp; float targetTrail = 1f; Image lockMarker; EnemyCombatController lastTarget;
        readonly List<string> enemyCoins = new(); float enemyCoinT; Text popup; float popupT;
        // 에너지 · 상태이상 · 경직
        RectTransform energyFill; Text energyLabel; EnergyManager energy; StatusEffectManager playerSe;
        Gauge targetStagger; Text targetStaggerLabel; readonly Image[] tIcons = new Image[StatusInfo.All.Length]; readonly Text[] tIconNums = new Text[StatusInfo.All.Length]; readonly float[] tFlash = new float[StatusInfo.All.Length]; float staggerFlash;
        System.Action<DefenseController, bool, int, int> onEvade; DefenseController playerDef;
        System.Action<AttackContext, CharacterStats, int, bool> onTremor; System.Action<StatusEffectManager, StatusType, int> onStatus;
        ClashStage stage; BossBar bossBar;
        System.Action<ClashOutcome> onClash, onClashEnd; System.Action<ClashOutcome, int, int> onExchange; System.Action<AttackContext> onEnemyCoin; System.Action<AttackContext, CharacterStats, int> onHit;

        public void Build(UIRoot ui, Transform hudRoot, Transform core)
        {
            this.ui = ui; var p = ui.player.gameObject; stats = ui.player; ex = p.GetComponent<SkillExecutor>(); cm = p.GetComponent<CombatManager>(); lockOn = p.GetComponent<LockOnSystem>();
            canvasRect = (RectTransform)hudRoot.GetComponentInParent<Canvas>().transform; var tl = new Vector2(0, 1);
            // 정신력
            var sg = Img(core, "Sanity", UITheme.Bar, Color.white); Set(sg.rectTransform, tl, tl, tl, new Vector2(44, -142), new Vector2(380, 14));
            var inner = NewRect("Inner", sg.transform); Stretch(inner, 3);
            sanityNeg = Box(inner, "Neg", new Color(.35f, .5f, .95f)).rectTransform; sanityPos = Box(inner, "Pos", UITheme.Gold).rectTransform;
            foreach (var r in new[] { sanityNeg, sanityPos }) { r.offsetMin = r.offsetMax = Vector2.zero; r.anchorMin = new Vector2(.5f, 0); r.anchorMax = new Vector2(.5f, 1); }
            var mid = Box(inner, "Mid", UITheme.Cream); Set(mid.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(.5f, .5f), Vector2.zero, new Vector2(2, 4));
            sanityLabel = Txt(core, "", 22, UITheme.Cream); Set(sanityLabel.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(436, -149), new Vector2(560, 30));
            // 스킬 슬롯
            var bc = new Vector2(.5f, 0); string[] keys = { "좌클릭", "Q", "E", "R" };
            for (int i = 0; i < 4; i++)
            {
                var u = new SlotUI { skill = cm != null ? cm.SlotSkill(i) : null };
                u.frame = Img(core, "Skill" + i, UITheme.Slot, Color.white, true); Set(u.frame.rectTransform, bc, bc, bc, new Vector2(-189 + i * 126, 30), new Vector2(114, 114));
                var key = Txt(u.frame.transform, keys[i], i == 0 ? 18 : 28, UITheme.Gold, TextAnchor.UpperLeft, FontStyle.Bold); Set(key.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(8, -4), Vector2.zero);
                u.name = StyleSkill(Txt(u.frame.transform, u.skill != null ? u.skill.skillName : "", 21, UITheme.Cream, TextAnchor.LowerCenter), new Color(1f, .85f, .5f)); u.name.GetComponent<TextGlow>().radius = 2.5f; Set(u.name.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(0, 8), new Vector2(-8, 0));
                u.cost = Txt(u.frame.transform, u.skill == null ? "" : u.skill.energyCost > 0 ? "에너지 " + u.skill.energyCost : u.skill.sanityCost > 0 ? "정신 " + u.skill.sanityCost : "", 16, new Color(.6f, .7f, 1f), TextAnchor.UpperRight); Set(u.cost.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(-8, -6), Vector2.zero);
                for (int c = 0; c < 6; c++) { var pip = Img(u.frame.transform, "Pip", CoinDisplay.Coin(CoinDisplay.Look.Heads), Color.white, false); Set(pip.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(16, 16)); u.pips.Add(pip); }
                var ov = Box(u.frame.transform, "Cooldown", new Color(0, 0, 0, .7f)); u.cdFill = ov.rectTransform; Set(u.cdFill, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, Vector2.zero);
                u.cd = Txt(u.frame.transform, "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(u.cd.rectTransform);
                u.affinity = Txt(u.frame.transform, "", 18, Color.white, TextAnchor.LowerCenter, FontStyle.Bold); Set(u.affinity.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 0), new Vector2(0, 4), new Vector2(0, 24)); slots.Add(u);   // 조준 대상과의 상성
            }
            RefreshSlots();
            // 결투 고조(회전 쌍검일 때만): 스킬 칸 위 붉은 막대 + 스택
            heatRoot = NewRect("DuelHeat", core); Set(heatRoot, bc, bc, bc, new Vector2(0, 156), new Vector2(500, 30));
            var hb = Img(heatRoot, "Frame", UITheme.Bar, Color.white, true); Set(hb.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 12));
            var hin = NewRect("Inner", hb.transform); Stretch(hin, 3); heatFill = Box(hin, "Fill", new Color(.95f, .3f, .25f)); Stretch(heatFill.rectTransform); heatFill.rectTransform.anchorMax = new Vector2(0, 1);
            heatText = StyleSkill(Txt(heatRoot, "", 18, Color.white, TextAnchor.LowerCenter), new Color(1f, .35f, .3f)); Set(heatText.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 14), new Vector2(0, 24));
            enemyPanels = new EnemySkillPanels(); enemyPanels.Build(hudRoot);   // 적 스킬 패널(머리 위)
            playerPanel = new SkillPanel(); playerPanel.Build(hudRoot); playerPanel.Pin(new Vector2(0, 0), new Vector2(30, 120)); playerPanel.FixedScale = 0.55f; playerPanel.Bind(ex);   // 내 스킬 패널(왼쪽 아래)
            canvasForPanels = canvasRect;
            // 에너지 게이지(정신력 아래)
            energy = p.GetComponent<EnergyManager>(); playerSe = p.GetComponent<StatusEffectManager>();
            var eg = Img(core, "Energy", UITheme.Bar, Color.white); Set(eg.rectTransform, tl, tl, tl, new Vector2(44, -166), new Vector2(380, 12));
            var ein = NewRect("Inner", eg.transform); Stretch(ein, 3); energyFill = Box(ein, "Fill", new Color(.3f, .85f, .95f)).rectTransform; Stretch(energyFill); energyFill.anchorMax = new Vector2(0, 1);
            energyLabel = Txt(core, "", 20, new Color(.55f, .9f, 1f)); Set(energyLabel.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(462, -172), new Vector2(300, 26));
            var eIcon = SkillPanelArt.EnergyIcon; if (eIcon != null) { var ei = Img(core, "EnergyIcon", eIcon, Color.white, false); Set(ei.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(446, -172), new Vector2(34, 34)); }
            else energyLabel.rectTransform.anchoredPosition = new Vector2(436, -172);
            playerStatus = Txt(core, "", 20, UITheme.Cream); Set(playerStatus.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(44, -196), new Vector2(640, 26));   // 내 상태이상·경직
            // 클래시
            var kp = Img(hudRoot, "ClashPanel", UITheme.Frame, Color.white); clashPanel = kp.gameObject; clashGroup = clashPanel.AddComponent<CanvasGroup>(); At(kp.rectTransform, new Vector2(.5f, .7f), Vector2.zero, new Vector2(620, 240));
            clashTitle = StyleHeading(Txt(kp.transform, "클래시", 34, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Normal)); Set(clashTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(-40, 44));
            clashRows = Txt(kp.transform, "", 30, UITheme.Cream, TextAnchor.UpperCenter); Set(clashRows.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(0, -8), new Vector2(-40, -110));
            clashResult = Txt(kp.transform, "", 30, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); Set(clashResult.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 16), new Vector2(-40, 40));
            clashGroup.alpha = 0;
            // 조준 대상
            var tp = Img(hudRoot, "TargetPanel", UITheme.Slot, new Color(1, 1, 1, .92f)); targetPanel = tp.gameObject; At(tp.rectTransform, new Vector2(.5f, 1), new Vector2(0, -28), new Vector2(640, 156)); tp.rectTransform.pivot = new Vector2(.5f, 1);
            targetName = StyleHeading(Txt(tp.transform, "", 27, UITheme.GoldBright, TextAnchor.MiddleLeft, FontStyle.Normal)); Set(targetName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -6), new Vector2(-36, 32));
            targetHp = MakeGauge(tp.transform, "HP", UITheme.Red, new Vector2(600, 14), true, false); Set(targetHp.root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -42), new Vector2(604, 14));
            // 경직 게이지(체력과 별개) + 상태이상 아이콘(강도/횟수)
            targetStagger = MakeGauge(tp.transform, "Stagger", new Color(.95f, .8f, .3f), new Vector2(420, 10), false, false); Set(targetStagger.root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(18, -60), new Vector2(420, 10));
            targetStaggerLabel = Txt(tp.transform, "", 16, new Color(.95f, .85f, .5f), TextAnchor.MiddleLeft); Set(targetStaggerLabel.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(446, -54), new Vector2(180, 22));
            for (int i = 0; i < tIcons.Length; i++)
            {
                tIcons[i] = Img(tp.transform, "Status" + i, SkillPanelArt.StatusIcon(StatusInfo.All[i]), SkillPanelArt.StatusTint(StatusInfo.All[i]), false); Set(tIcons[i].rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(32 + i * 110, -86), new Vector2(36, 36));
                tIconNums[i] = Txt(tp.transform, "", 19, UITheme.Cream, TextAnchor.MiddleLeft, FontStyle.Bold); Set(tIconNums[i].rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, .5f), new Vector2(50 + i * 110, -86), new Vector2(80, 26));
            }
            targetIntent = Txt(tp.transform, "", 21, UITheme.Cream, TextAnchor.MiddleLeft); Set(targetIntent.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(18, 28), new Vector2(-36, 28));
            targetCoins = Txt(tp.transform, "", 21, UITheme.Muted, TextAnchor.MiddleRight); Set(targetCoins.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(18, 4), new Vector2(-36, 26));
            targetPanel.SetActive(false);
            lockMarker = Img(hudRoot, "LockMarker", UITheme.Slot, UITheme.GoldBright, true); At(lockMarker.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30)); lockMarker.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); lockMarker.gameObject.SetActive(false);
            bossBar = new BossBar(); bossBar.Build(hudRoot, stats.transform);   // 보스바(화면 아래 가운데)
            stage = new ClashStage(); stage.Build(hudRoot);   // 클래시 때 가운데 두 패널
            popup = Txt(hudRoot, "", 34, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold, "Popup"); At(popup.rectTransform, new Vector2(.5f, .44f), Vector2.zero, new Vector2(800, 50));

            ex.SkillRefused += (s, why) => Popup(s.skillName + " — " + why, UITheme.Muted);
            // 회피 코인: 시작할 때 위력, 판정마다 성공/실패(내 회피 · 적 회피)
            playerDef = p.GetComponent<DefenseController>();
            if (playerDef != null) playerDef.EvadeStarted += (pw, heads) => Popup($"회피 코인 {(heads ? "앞면" : "뒷면")} · 위력 {pw}", heads ? UITheme.GoldBright : UITheme.Cream);
            onEvade = (d, ok, ep, inc) =>
            {
                if (d == playerDef) Popup(ok ? $"회피 성공  {ep} ≥ {inc}" : $"회피 실패  {ep} < {inc}", ok ? new Color(.7f, .9f, 1f) : new Color(1f, .5f, .45f));
                else if (lockOn != null && lockOn.Target != null && d.gameObject == lockOn.Target.gameObject) Popup(ok ? $"적 회피  {ep} ≥ {inc}" : $"적 회피 실패  {ep} < {inc}", ok ? new Color(1f, .6f, .5f) : UITheme.GoldBright);
            };
            DefenseController.AnyEvadeResolved += onEvade;
            if (playerDef != null) playerDef.GuardStarted += (pw, heads) => Popup($"방어 코인 {(heads ? "앞면" : "뒷면")} · 위력 {pw}", heads ? UITheme.GoldBright : UITheme.Cream);
            onGuard = (d, perfect, gp, ap) =>
            {
                if (d == playerDef) Popup(perfect ? $"완벽 방어!  {gp} ≥ {ap}" : $"방어  {gp} < {ap}", perfect ? new Color(.75f, .95f, 1f) : UITheme.Cream);
                else if (lockOn != null && lockOn.Target != null && d.gameObject == lockOn.Target.gameObject && perfect) Popup($"적 완벽 방어  {gp} ≥ {ap}", new Color(1f, .6f, .5f));
            };
            DefenseController.AnyGuardResolved += onGuard;
            if (cm != null) { cm.KitChanged += k => RefreshSlots(); cm.Refused += m => Popup(m, new Color(1f, .55f, .45f)); }
            stats.SanityChanged += (a, b, why) => { if (why == "패링" || why.Contains("앞면")) Popup(why + "  " + (b > a ? "+" : "") + (b - a), UITheme.GoldBright); };
            onClash = o => { if (!o.parry && o.playerAttack != null) { stage.Show(o); clash = null; } else { clash = o; clashT = 0f; } };   // 합 = 가운데 연출, 패링 = 작은 패널
            ClashManager.Clashed += onClash;
            onExchange = (o, i, r) => stage.Exchange(o, i, r); ClashManager.ClashExchange += onExchange;
            onClashEnd = o => stage.End(o); ClashManager.ClashEnded += onClashEnd;
            onHit = (a, target, dmg) => { if (a.owner == ex && a.affinity != 0) Popup(a.affinity > 0 ? $"약점!  ×{SkillAttributes.AdvantageDamage:0.00}" : $"저항  ×{SkillAttributes.DisadvantageDamage:0.0}", a.affinity > 0 ? new Color(.5f, 1f, .5f) : new Color(1f, .5f, .5f)); };
            CombatResolver.DamageDealt += onHit;
            // 진동 폭발 연출: 팝업 + 경직 게이지 번쩍임
            onTremor = (a, target, dmg, broke) =>
            {
                if (lockOn != null && lockOn.Target != null && lockOn.Target.Stats == target) staggerFlash = 0.35f;
                if (a.owner == ex) Popup(broke ? $"진동 폭발!  경직 {target.staggerDuration:0.#}초" : $"진동 폭발  경직 +{dmg}", broke ? UITheme.GoldBright : new Color(.95f, .85f, .5f));
                else if (target == stats) Popup(broke ? "경직됨!" : $"진동 폭발  경직 +{dmg}", new Color(1f, .55f, .45f));
            };
            CombatResolver.TremorBurst += onTremor;
            onStatus = (m, type, v) =>
            {
                if (lockOn != null && lockOn.Target != null && m.gameObject == lockOn.Target.gameObject) tFlash[System.Array.IndexOf(StatusInfo.All, type)] = 0.3f;
                if (m == playerSe && type != StatusType.Tremor) Popup($"{StatusInfo.Name(type)}  -{v}" + (type == StatusType.Sinking ? " 정신력" : ""), StatusInfo.ColorOf(type));
            };
            StatusEffectManager.AnyTriggered += onStatus;
            onEnemyCoin = a => { enemyCoins.Add(a.isHeads ? $"<color=#E8C97A>앞 {a.power}</color>" : $"<color=#999999>뒤 {a.power}</color>"); if (enemyCoins.Count > 5) enemyCoins.RemoveAt(0); enemyCoinT = 3f; };
        }

        /// <summary>정적 이벤트·적 구독 해제(씬 재시작 시 파괴된 UI 를 건드리지 않도록).</summary>
        public void Dispose() { if (onGuard != null) DefenseController.AnyGuardResolved -= onGuard; if (onEvade != null) DefenseController.AnyEvadeResolved -= onEvade; stage?.Dispose(); if (onExchange != null) ClashManager.ClashExchange -= onExchange; if (onClashEnd != null) ClashManager.ClashEnded -= onClashEnd; if (onTremor != null) CombatResolver.TremorBurst -= onTremor; if (onStatus != null) StatusEffectManager.AnyTriggered -= onStatus; if (onClash != null) ClashManager.Clashed -= onClash; if (onHit != null) CombatResolver.DamageDealt -= onHit; if (lastTarget != null) lastTarget.Executor.CoinAttacked -= onEnemyCoin; }
        // 조준 대상 컴포넌트는 대상이 바뀔 때만 찾음(매 프레임 GetComponent 하지 않게)
        EnemyCombatController cT; StatusEffectManager cTse; EnemyBrain cBrain; Image staggerFillImg;
        void CacheTarget(EnemyCombatController t) { if (t == cT) return; cT = t; cTse = t != null ? t.GetComponent<StatusEffectManager>() : null; cBrain = t != null ? t.GetComponent<EnemyBrain>() : null; }
        Image StaggerFillImg() { if (staggerFillImg == null) staggerFillImg = targetStagger.fill.GetComponent<Image>(); return staggerFillImg; }

        /// <summary>무기를 바꾸면 스킬 칸(이름·비용·코인 — 파괴불가면 빨간 코인)을 다시 씀.</summary>
        void RefreshSlots()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var u = slots[i]; u.skill = cm != null ? cm.SlotSkill(i) : null;
                u.name.text = u.skill != null ? u.skill.skillName : ""; u.cost.text = u.skill == null ? "" : u.skill.energyCost > 0 ? "에너지 " + u.skill.energyCost : u.skill.sanityCost > 0 ? "정신 " + u.skill.sanityCost : u.skill.requiresDuelHeat ? "고조 MAX" : "";
                int n = u.skill != null ? Mathf.Min(u.skill.coinCount, u.pips.Count) : 0; var look = u.skill != null && u.skill.unbreakableCoins ? CoinDisplay.Look.Unbreakable : CoinDisplay.Look.Heads;
                for (int c = 0; c < u.pips.Count; c++) { bool on = c < n; u.pips[c].gameObject.SetActive(on); if (!on) continue; u.pips[c].sprite = CoinDisplay.Coin(look); u.pips[c].rectTransform.anchoredPosition = new Vector2((c - (n - 1) * .5f) * 16, 8); }
            }
        }

        /// <summary>조준 대상 레벨 표시: 내가 높으면 초록, 낮으면 빨강 + 레벨 차이로 바뀌는 내 코인 위력.</summary>
        string LevelTag(CharacterStats t)
        {
            int b = LevelMath.CoinBonus(stats.level, t.level); string col = b > 0 ? "#9BE86A" : b < 0 ? "#E87060" : "#E8DDC4";
            return $"<color={col}>Lv.{t.level}</color>" + (b != 0 ? $"<size=17><color={col}> ({LevelMath.BonusLabel(b)})</color></size>" : "") + "  ";
        }
        void Popup(string t, Color c) { popup.text = t; popup.color = c; popupT = 1.2f; }

        public void Update(float dt)
        {
            var heat = DuelHeat.Player; bool heatOn = heat != null && heat.enabled; heatRoot.gameObject.SetActive(heatOn);
            if (heatOn)
            {
                heatFill.rectTransform.anchorMax = new Vector2(Mathf.MoveTowards(heatFill.rectTransform.anchorMax.x, heat.Ratio, dt * 2f), 1);
                heatFill.color = heat.Full ? Color.Lerp(new Color(1f, .3f, .25f), new Color(1f, .85f, .5f), .5f + .5f * Mathf.Sin(Time.unscaledTime * 10f)) : new Color(.95f, .3f, .25f);
                heatText.text = heat.Full ? "결투 고조 MAX  —  R  궁극 - 회절원참격" : $"결투 고조  {heat.Stacks}/{DuelHeat.MaxStacks}   피해 +{heat.Stacks * DuelHeat.PerStack * 100:0}%";
            }
            float v = stats.currentSanity / 45f; sanityPos.anchorMax = new Vector2(.5f + Mathf.Max(0, v) * .5f, 1); sanityNeg.anchorMin = new Vector2(.5f + Mathf.Min(0, v) * .5f, 0);
            var st = stats.Sanity; sanityLabel.text = $"정신력 {(stats.currentSanity >= 0 ? "+" : "")}{stats.currentSanity}  ·  {CharacterStats.SanityLabel(st)}  ·  앞면 {Mathf.RoundToInt(stats.GetCoinProbability() * 100)}%";
            sanityLabel.color = st == SanityState.Focus ? UITheme.GoldBright : st == SanityState.Stable ? UITheme.Cream : st == SanityState.Unstable ? new Color(.6f, .7f, 1f) : new Color(.95f, .35f, .35f);
            var lockT = lockOn != null ? lockOn.Target : null; SkillAttribute? tAttr = lockT != null ? SkillAttributes.DefenderAttribute(lockT.Stats) : (SkillAttribute?)null;
            foreach (var u in slots)
            {
                if (u.skill == null) continue;
                int aff = tAttr.HasValue ? SkillAttributes.Affinity(u.skill.attribute, tAttr.Value) : 0;
                u.affinity.text = aff > 0 ? "▲ 유리" : aff < 0 ? "▼ 불리" : ""; u.affinity.color = aff > 0 ? new Color(.5f, 1f, .5f) : new Color(1f, .45f, .45f); float cd = ex.CooldownLeft(u.skill), k = u.skill.cooldown > 0 ? cd / u.skill.cooldown : 0f; bool lacking = stats.currentSanity < u.skill.sanityCost || !ex.HasEnergyFor(u.skill) || (u.skill.requiresDuelHeat && (DuelHeat.Player == null || !DuelHeat.Player.Full));
                u.cdFill.anchorMax = new Vector2(1, lacking ? 1 : k); u.cd.text = cd > 0 ? Mathf.CeilToInt(cd).ToString() : ""; u.cost.color = lacking ? new Color(.95f, .4f, .4f) : u.skill.energyCost > 0 ? new Color(.55f, .9f, 1f) : new Color(.6f, .7f, 1f);
                u.frame.color = ex.CurrentSkill == u.skill || (u == slots[0] && ex.CurrentSkill != null && ex.CurrentSkill.skillId.StartsWith("BASIC")) ? UITheme.GoldBright : Color.Lerp(Color.white, SkillAttributes.ColorOf(u.skill.attribute), 0.55f);
            }
            if (ClashStage.OnStage.Contains(ex)) playerPanel.Hide(); else playerPanel.Tick(Camera.main, canvasForPanels, dt);
            enemyPanels.Update(dt); stage.Update(dt); bossBar.Update(dt);
            if (energy != null) { energyFill.anchorMax = new Vector2(energy.Ratio, 1); energyLabel.text = $"에너지 {energy.Current}/{energy.Max}"; }
            string ps = playerSe != null ? playerSe.Summary() : "";
            if (stats.IsStaggerBroken) ps = "<color=#F2D27F>경직!</color>  " + ps; else if (stats.StaggerGauge.Current > 0) ps = $"<color=#C9B27A>경직 {Mathf.RoundToInt(stats.StaggerGauge.Ratio * 100)}%</color>  " + ps;
            playerStatus.text = ps;
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
                targetName.text = LevelTag(t.Stats) + t.Stats.displayName + (string.IsNullOrEmpty(t.Stats.epithet) ? "" : $"  <size=19><color=#CFC4A8>{t.Stats.epithet}</color></size>") + (t.Stats.IsVulnerable ? "   <color=#E8C97A><size=20>경직 — 반격 기회</size></color>" : "") + $"   <size=18><color=#9E9178>정신력 {t.Stats.currentSanity:+0;-0;0} · {SkillAttributes.Names[(int)t.Stats.attribute]} 속성 · {SkillAttributes.WeaknessText(t.Stats.attribute)}</color></size>";
                // 경직 게이지
                targetStagger.SetFill(t.Stats.IsStaggerBroken ? 1f : t.Stats.StaggerGauge.Ratio); if (staggerFlash > 0) staggerFlash -= dt;
                StaggerFillImg().color = staggerFlash > 0 ? Color.white : t.Stats.IsStaggerBroken ? UITheme.GoldBright : new Color(.95f, .8f, .3f);
                targetStaggerLabel.text = t.Stats.IsStaggerBroken ? "경직!" : $"경직 {Mathf.RoundToInt(t.Stats.StaggerGauge.Current)}/{Mathf.RoundToInt(t.Stats.StaggerGauge.Max)}";
                // 상태이상 아이콘: 강도/횟수, 발동하면 잠깐 커짐
                CacheTarget(t); var tse = cTse;
                float x = 32f;   // 켜진 효과만 왼쪽부터 차례로
                for (int i = 0; i < tIcons.Length; i++)
                {
                    var e = tse != null ? tse.Get(StatusInfo.All[i]) : null; bool on = e != null && e.Active; tIcons[i].gameObject.SetActive(on); tIconNums[i].gameObject.SetActive(on);
                    if (tFlash[i] > 0) tFlash[i] -= dt; tIcons[i].rectTransform.localScale = Vector3.one * (1f + Mathf.Max(0, tFlash[i]) * 1.5f);
                    if (!on) continue;
                    var type = StatusInfo.All[i];
                    tIconNums[i].text = type == StatusType.Tremor ? $"{e.Potency}/{e.Count} <size=14>(폭발 {((TremorEffect)e).PreviewBurst})</size>" : type == StatusType.Paralysis ? $"{e.Count}" : $"{e.Potency}/{e.Count}";
                    tIcons[i].rectTransform.anchoredPosition = new Vector2(x, -86); tIconNums[i].rectTransform.anchoredPosition = new Vector2(x + 18, -86);
                    x += 18 + 6 + tIconNums[i].preferredWidth + 18;
                }
                var s = t.Executor.CurrentSkill; CacheTarget(t); var brain = cBrain;
                if (s != null) targetIntent.text = (t.Executor.Phase == SkillPhase.Windup ? "<color=#E06060>예고</color> " : "<color=#E06060>공격</color> ") + $"{s.skillName} · 기본 {s.basePower} · 코인 {s.coinCount}개 +{s.coinPower}" + (!s.canBeGuarded ? "  <color=#B070FF>방어 불가</color>" : "") + (!s.canBeEvaded ? "  <color=#B070FF>회피 불가</color>" : "") + (s.isAreaAttack ? $"  <color=#E8C97A>광역 {s.areaRadius:0.#}m</color>" : "") + (s.isProjectile ? $"  <color=#9BE07A>원거리 {s.range:0.#}m</color>" : "");
                else targetIntent.text = t.IsStaggered ? "경직" : t.Stats.Guarding ? "방어" : t.IsDodging ? "회피" : brain != null ? brain.LastAction switch { EnemyAction.Approach => "접근", EnemyAction.Attack => "공격 준비", EnemyAction.Guard => "방어", EnemyAction.Dodge => "회피", _ => "관찰" } : "";
                if (enemyCoinT > 0) enemyCoinT -= dt; targetCoins.text = enemyCoinT > 0 ? "적 코인  " + string.Join("  ", enemyCoins) : "";
                var cam = Camera.main;
                if (cam != null) { Vector3 sp = cam.WorldToScreenPoint(lockOn.TargetPoint); if (sp.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var lp)) { lockMarker.gameObject.SetActive(true); lockMarker.rectTransform.anchoredPosition = lp; } }
            }
            if (popupT > 0) { popupT -= dt; var c = popup.color; c.a = Mathf.Clamp01(popupT / 0.4f); popup.color = c; } else popup.text = "";
        }
    }
}
