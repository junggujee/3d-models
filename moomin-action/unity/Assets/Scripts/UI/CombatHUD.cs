using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 코인 전투 HUD(설계서 3-3, 10장): 정신력 게이지(-45~+45)와 상태·앞면 확률, 하단 스킬 슬롯(좌클릭/Q/E/R: 코인 수·쿨타임·정신력 비용),
    /// 현재 스킬의 코인 판정(대기/앞면/뒷면/파괴)과 위력, 합 라운드 표시, 조준 대상(이름·체력·다음 공격·적 코인), 조준 표식, 회피/거부 알림.
    /// </summary>
    public class CombatHUD
    {
        enum CoinLook { Pending, Heads, Tails, Broken }
        static Sprite[] coinSprites;
        static Sprite CoinSprite(CoinLook k)
        {
            if (coinSprites == null)
            {
                coinSprites = new Sprite[4];
                for (int s = 0; s < 4; s++)
                {
                    const int N = 48; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear }; var px = new Color[N * N];
                    for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
                    {
                        float x = (i + .5f) / N * 2 - 1, y = (j + .5f) / N * 2 - 1, r = Mathf.Sqrt(x * x + y * y), a = Mathf.Clamp01((0.95f - r) * N * 0.5f); Color c;
                        switch ((CoinLook)s)
                        {
                            case CoinLook.Heads: c = r > 0.78f ? new Color(.62f, .45f, .14f) : Color.Lerp(new Color(1f, .93f, .55f), new Color(.88f, .66f, .22f), r); if (Mathf.Abs(x) + Mathf.Abs(y) < 0.32f) c = new Color(1f, .98f, .8f); break;
                            case CoinLook.Tails: c = r > 0.78f ? new Color(.32f, .3f, .28f) : Color.Lerp(new Color(.5f, .48f, .45f), new Color(.28f, .27f, .25f), r); break;
                            case CoinLook.Broken: c = r > 0.78f ? new Color(.45f, .08f, .08f) : new Color(.22f, .05f, .05f); if (Mathf.Abs(Mathf.Abs(x) - Mathf.Abs(y)) < 0.12f && r < 0.7f) c = new Color(.9f, .2f, .2f); break;
                            default: c = r > 0.78f ? UITheme.GoldDim : new Color(.08f, .07f, .06f, .85f); break;
                        }
                        c.a *= a; px[j * N + i] = c;
                    }
                    tex.SetPixels(px); tex.Apply(); coinSprites[s] = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(.5f, .5f), 100f);
                }
            }
            return coinSprites[(int)k];
        }

        class SkillSlotUI { public SkillData skill; public SkillSlot slot; public Text name, cd, cost; public RectTransform cdFill; public Image frame; public Image[] pips; }

        UIRoot ui; SanitySystem sanity; SkillManager skills; AttackController ac; LockOnSystem lockOn; CombatManager cm;
        RectTransform sanityPos, sanityNeg, canvasRect; Text sanityLabel;
        readonly List<SkillSlotUI> slots = new();
        GameObject coinPanel; CanvasGroup coinGroup; Text coinTitle, coinInfo; Image[] coinImgs = new Image[5]; float coinFade;
        GameObject clashPanel; CanvasGroup clashGroup; Text clashTitle, clashRows, clashResult; ClashOutcome clash; float clashT;
        GameObject targetPanel; Text targetName, targetIntent, targetCoins; UIFactory.Gauge targetHp; float targetTrail = 1f; Image lockMarker; EnemyAI lastTarget;
        readonly List<string> enemyCoinLog = new(); float enemyCoinT;
        Text popup; float popupT;
        System.Action<ClashOutcome> onClash; System.Action<EnemyAI, CoinResult, int> onEnemyCoin;

        public void Build(UIRoot ui, Transform hudRoot, Transform core)
        {
            this.ui = ui; var p = ui.player.gameObject; sanity = p.GetComponent<SanitySystem>(); skills = p.GetComponent<SkillManager>(); ac = p.GetComponent<AttackController>(); lockOn = p.GetComponent<LockOnSystem>(); cm = p.GetComponent<CombatManager>();
            canvasRect = (RectTransform)hudRoot.GetComponentInParent<Canvas>().transform;
            var tl = new Vector2(0, 1);
            // 정신력 게이지(스태미나 아래)
            var sg = Img(core, "Sanity", UITheme.Bar, Color.white); Set(sg.rectTransform, tl, tl, tl, new Vector2(44, -98), new Vector2(380, 14));
            var inner = NewRect("Inner", sg.transform); Stretch(inner, 3);
            sanityNeg = Box(inner, "Neg", new Color(.35f, .5f, .95f)).rectTransform; sanityPos = Box(inner, "Pos", UITheme.Gold).rectTransform;
            foreach (var r in new[] { sanityNeg, sanityPos }) { r.offsetMin = r.offsetMax = Vector2.zero; r.anchorMin = new Vector2(.5f, 0); r.anchorMax = new Vector2(.5f, 1); }
            var mid = Box(inner, "Mid", UITheme.Cream); Set(mid.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(.5f, .5f), Vector2.zero, new Vector2(2, 4));
            sanityLabel = Txt(core, "", 22, UITheme.Cream); Set(sanityLabel.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(436, -105), new Vector2(520, 30));

            // 스킬 슬롯(하단 중앙)
            var bc = new Vector2(.5f, 0);
            var defs = new[] { (SkillSlot.Basic, "좌클릭"), (SkillSlot.Skill1, "Q"), (SkillSlot.Skill2, "E"), (SkillSlot.Ultimate, "R") };
            for (int i = 0; i < defs.Length; i++)
            {
                var u = new SkillSlotUI { slot = defs[i].Item1, skill = skills.SlotSkill(defs[i].Item1) };
                u.frame = Img(core, "Skill" + i, UITheme.Slot, Color.white, true); Set(u.frame.rectTransform, bc, bc, bc, new Vector2(-189 + i * 126, 30), new Vector2(114, 114));
                var key = Txt(u.frame.transform, defs[i].Item2, i == 0 ? 18 : 28, UITheme.Gold, TextAnchor.UpperLeft, FontStyle.Bold); Set(key.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(8, -4), Vector2.zero);
                u.name = Txt(u.frame.transform, u.skill != null ? u.skill.skillName : "", 20, UITheme.Cream, TextAnchor.LowerCenter); Set(u.name.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(0, 8), new Vector2(-8, 0));
                u.cost = Txt(u.frame.transform, u.skill != null && u.skill.sanityCost > 0 ? "-" + u.skill.sanityCost : "", 18, new Color(.6f, .7f, 1f), TextAnchor.UpperRight); Set(u.cost.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(-8, -4), Vector2.zero);
                int n = u.skill != null ? u.skill.coinCount : 0; u.pips = new Image[n];
                for (int c = 0; c < n; c++) { u.pips[c] = Img(u.frame.transform, "Pip", CoinSprite(CoinLook.Heads), Color.white, false); Set(u.pips[c].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2((c - (n - 1) * .5f) * 18, 8), new Vector2(16, 16)); }
                var ov = Box(u.frame.transform, "Cooldown", new Color(0, 0, 0, .7f)); u.cdFill = ov.rectTransform; Set(u.cdFill, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, Vector2.zero);
                u.cd = Txt(u.frame.transform, "", 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(u.cd.rectTransform);
                slots.Add(u);
            }

            // 현재 스킬 코인 판정
            var cp = Img(hudRoot, "CoinPanel", UITheme.Frame, Color.white); coinPanel = cp.gameObject; coinGroup = coinPanel.AddComponent<CanvasGroup>(); Set(cp.rectTransform, bc, bc, bc, new Vector2(0, 168), new Vector2(560, 150));
            coinTitle = Txt(cp.transform, "", 28, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); Set(coinTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(-40, 36));
            for (int i = 0; i < 5; i++) { coinImgs[i] = Img(cp.transform, "Coin" + i, CoinSprite(CoinLook.Pending), Color.white, false); Set(coinImgs[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2((i - 2) * 56, 2), new Vector2(46, 46)); }
            coinInfo = Txt(cp.transform, "", 21, UITheme.Cream, TextAnchor.MiddleCenter); Set(coinInfo.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 14), new Vector2(-30, 30));
            coinGroup.alpha = 0;

            // 합 표시
            var kp = Img(hudRoot, "ClashPanel", UITheme.Frame, Color.white); clashPanel = kp.gameObject; clashGroup = clashPanel.AddComponent<CanvasGroup>(); At(kp.rectTransform, new Vector2(.5f, .7f), Vector2.zero, new Vector2(620, 260));
            clashTitle = Txt(kp.transform, "합", 34, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold); Set(clashTitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(-40, 44));
            clashRows = Txt(kp.transform, "", 24, UITheme.Cream, TextAnchor.UpperCenter); Set(clashRows.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(.5f, .5f), new Vector2(0, -6), new Vector2(-40, -110));
            clashResult = Txt(kp.transform, "", 30, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); Set(clashResult.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 16), new Vector2(-40, 40));
            clashGroup.alpha = 0;

            // 조준 대상(상단 중앙) + 표식
            var tp = Img(hudRoot, "TargetPanel", UITheme.Slot, new Color(1, 1, 1, .92f)); targetPanel = tp.gameObject; At(tp.rectTransform, new Vector2(.5f, 1), new Vector2(0, -28), new Vector2(640, 112)); tp.rectTransform.pivot = new Vector2(.5f, 1);
            targetName = Txt(tp.transform, "", 26, UITheme.GoldBright, TextAnchor.MiddleLeft, FontStyle.Bold); Set(targetName.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(18, -6), new Vector2(-36, 32));
            targetHp = MakeGauge(tp.transform, "HP", UITheme.Red, new Vector2(600, 14), true, false); Set(targetHp.root, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -42), new Vector2(604, 14));
            targetIntent = Txt(tp.transform, "", 21, UITheme.Cream, TextAnchor.MiddleLeft); Set(targetIntent.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(18, 28), new Vector2(-36, 28));
            targetCoins = Txt(tp.transform, "", 21, UITheme.Muted, TextAnchor.MiddleRight); Set(targetCoins.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(18, 4), new Vector2(-36, 26));
            targetPanel.SetActive(false);
            lockMarker = Img(hudRoot, "LockMarker", UITheme.Slot, UITheme.GoldBright, true); At(lockMarker.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(30, 30)); lockMarker.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); lockMarker.gameObject.SetActive(false);

            popup = Txt(hudRoot, "", 34, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold, "Popup"); At(popup.rectTransform, new Vector2(.5f, .44f), Vector2.zero, new Vector2(800, 50));

            // 이벤트 연결
            skills.SkillStarted += s => { heads.Clear(); coinFade = 99f; RefreshCoins(); };
            skills.SkillEnded += (s, done) => coinFade = 1.6f;
            skills.SkillRefused += (s, why) => Popup(s.skillName + " — " + why, UITheme.Muted);
            ac.CoinResolved += (s, r, p) => { if (r.index == 0) heads.Clear(); heads.Add(r.heads); RefreshCoins(); };
            ac.CoinsDestroyed += (s, n) => RefreshCoins();
            onClash = o => { clash = o; clashT = 0f; }; ClashManager.Clashed += onClash;
            cm.Evaded += () => Popup("회피", UITheme.Cream);
            sanity.Changed += (a, b, why) => { if (why == "패링" || why.Contains("앞면")) Popup(why + (b > a ? "  +" : "  ") + (b - a), UITheme.GoldBright); };
            onEnemyCoin = (e, r, pw) => { if (lockOn == null || lockOn.Target != e) return; enemyCoinLog.Add(r.heads ? "<color=#E8C97A>앞</color>" : "<color=#888888>뒤</color>"); if (enemyCoinLog.Count > 5) enemyCoinLog.RemoveAt(0); enemyCoinT = 2.5f; };
            EnemyAI.AnyCoinTossed += onEnemyCoin;
        }

        /// <summary>정적 이벤트 구독 해제(씬 재시작 시 파괴된 UI를 건드리지 않도록).</summary>
        public void Dispose() { if (onClash != null) ClashManager.Clashed -= onClash; if (onEnemyCoin != null) EnemyAI.AnyCoinTossed -= onEnemyCoin; }

        void Popup(string t, Color c) { popup.text = t; popup.color = c; popupT = 1.2f; }

        void RefreshCoins()
        {
            var s = ac.Skill ?? skills.Current; if (s == null) return;
            coinTitle.text = s.skillName + "   <size=20><color=#9E9178>정신력 " + (sanity.Value >= 0 ? "+" : "") + sanity.Value + " · 앞면 " + Mathf.RoundToInt(sanity.HeadsProbability * 100) + "%</color></size>";
            int n = s.coinCount, resolved = ac.Tossed, lost = ac.CoinsLost;
            for (int i = 0; i < 5; i++)
            {
                coinImgs[i].gameObject.SetActive(i < n); if (i >= n) continue;
                coinImgs[i].rectTransform.anchoredPosition = new Vector2((i - (n - 1) * .5f) * 56, 2);
                CoinLook look = i < resolved ? (i < heads.Count && heads[i] ? CoinLook.Heads : CoinLook.Tails) : i >= n - lost ? CoinLook.Broken : CoinLook.Pending;
                coinImgs[i].sprite = CoinSprite(look);
            }
            coinInfo.text = $"기본 위력 {s.basePower} · 코인 위력 +{s.coinPower}    <color=#E8C97A>현재 위력 {ac.LastPower}</color>    예상 {s.ExpectedPower(sanity.Value):0.0}";
        }
        // 코인별 앞/뒤 기록(AttackController 는 앞면 수만 세므로 HUD 가 순서를 기억)
        readonly List<bool> heads = new();


        public void Update(float dt)
        {
            // 정신력
            float v = sanity.Value / 45f; sanityPos.anchorMax = new Vector2(.5f + Mathf.Max(0, v) * .5f, 1); sanityNeg.anchorMin = new Vector2(.5f + Mathf.Min(0, v) * .5f, 0);
            sanityLabel.text = $"정신력 {(sanity.Value >= 0 ? "+" : "")}{sanity.Value}  ·  {SanitySystem.Label(sanity.State)}  ·  앞면 {Mathf.RoundToInt(sanity.HeadsProbability * 100)}%";
            sanityLabel.color = sanity.State == SanityState.Focus ? UITheme.GoldBright : sanity.State == SanityState.Stable ? UITheme.Cream : sanity.State == SanityState.Unstable ? new Color(.6f, .7f, 1f) : new Color(.95f, .35f, .35f);
            // 스킬 슬롯
            foreach (var u in slots)
            {
                if (u.skill == null) continue; float cd = skills.CooldownLeft(u.skill), k = u.skill.cooldown > 0 ? cd / u.skill.cooldown : 0f;
                u.cdFill.anchorMax = new Vector2(1, k); u.cd.text = cd > 0 ? Mathf.CeilToInt(cd).ToString() : "";
                bool active = skills.Current != null && (skills.Current == u.skill || (u.slot == SkillSlot.Basic && skills.Current.slot == SkillSlot.Basic));
                u.frame.color = active ? UITheme.GoldBright : Color.white;
            }
            // 코인 패널
            if (coinFade < 90f) coinFade -= dt; coinGroup.alpha = Mathf.Clamp01(coinFade < 90f ? coinFade / 0.6f : 1f);
            // 합 패널: 라운드를 0.18초 간격으로 하나씩
            if (clash != null)
            {
                clashT += dt; int shown = Mathf.Min(clash.rounds.Count, 1 + (int)(clashT / 0.18f)); var sb = new System.Text.StringBuilder();
                int start = Mathf.Max(0, shown - 4);
                for (int i = start; i < shown; i++) { var r = clash.rounds[i]; string pc = r.winner > 0 ? "#F2D27F" : "#BBBBBB", ec = r.winner < 0 ? "#E06060" : "#BBBBBB"; sb.AppendLine($"<color={pc}>{r.playerPower}</color>  <size=18>({r.playerCoins}코인)</size>   :   <size=18>({r.enemyCoins}코인)</size>  <color={ec}>{r.enemyPower}</color>"); }
                clashTitle.text = (clash.guard ? (clash.parry ? "패링 합" : "방어 합") : "합") + $"   <size=22><color=#CFC4A8>{clash.player.name}  vs  {clash.enemy.name}</color></size>";
                clashRows.text = sb.ToString();
                bool done = shown >= clash.rounds.Count && clashT > clash.rounds.Count * 0.18f;
                clashResult.text = !done ? "" : clash.result == ClashResult.PlayerWin ? $"합 승리 — 코인 {clash.playerCoinsLeft}개 남음" : clash.result == ClashResult.EnemyWin ? $"<color=#E06060>합 패배 — 적 코인 {clash.enemyCoinsLeft}개 공격</color>" : "<color=#CFC4A8>무승부</color>";
                float end = clash.rounds.Count * 0.18f + 1.3f; clashGroup.alpha = clashT < end ? 1f : Mathf.Clamp01(1f - (clashT - end) / 0.4f); if (clashT > end + 0.4f) clash = null;
            }
            else clashGroup.alpha = 0f;
            // 조준 대상
            var t = lockOn != null ? lockOn.Target : null; targetPanel.SetActive(t != null); lockMarker.gameObject.SetActive(false);
            if (t != null)
            {
                if (t != lastTarget) { enemyCoinLog.Clear(); targetTrail = 1f; lastTarget = t; }
                float r = t.Health.Current / t.Health.maxHealth; targetHp.SetFill(r); targetTrail = r < targetTrail ? Mathf.MoveTowards(targetTrail, r, .4f * dt) : r; targetHp.SetTrail(targetTrail);
                targetName.text = t.displayName + (t.DamageTakenMultiplier > 1f ? "   <color=#E8C97A><size=20>경직 — 반격 기회</size></color>" : "");
                var a = t.State == EnemyState.Windup || t.State == EnemyState.Strike ? t.CurrentAttack : t.PlannedAttack;
                string state = t.State == EnemyState.Windup ? "<color=#E06060>예고</color> " : t.State == EnemyState.Strike ? "<color=#E06060>공격</color> " : "다음 ";
                targetIntent.text = a == null ? "" : $"{state}{a.name}  ·  기본 {Mathf.RoundToInt(a.basePower * t.powerScale)} · 코인 {a.coinCount}개 +{a.coinPower}" + (a.unblockable ? "  <color=#B070FF>방어 불가</color>" : "");
                if (enemyCoinT > 0) enemyCoinT -= dt; targetCoins.text = enemyCoinT > 0 ? "적 코인  " + string.Join(" ", enemyCoinLog) : "";
                var cam = Camera.main; if (cam != null)
                {
                    Vector3 sp = cam.WorldToScreenPoint(lockOn.TargetPoint);
                    if (sp.z > 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out var lp)) { lockMarker.gameObject.SetActive(true); lockMarker.rectTransform.anchoredPosition = lp; }
                }
            }
            if (popupT > 0) { popupT -= dt; var c = popup.color; c.a = Mathf.Clamp01(popupT / 0.4f); popup.color = c; } else popup.text = "";
        }
    }
}
