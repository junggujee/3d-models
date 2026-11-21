using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 전투 화면 HUD: 좌상단 체력(잔상)·스태미나 게이지, 목표 문구, 우하단 다이아몬드 퀵슬롯(1~4), 좌하단 금화·조작 안내,
    /// 우상단 획득 알림, 중앙 배너(웨이브/처치), 피격 붉은 번쩍임, 쓰러짐 화면.
    /// </summary>
    public class HUD
    {
        UIRoot ui; GameObject hudRoot, coreRoot; Health hp; Stamina st; Inventory inv;
        Gauge hpBar, stBar; float hpTrail = 1f; Text objective, scoreText, goldText, weaponText;
        Image[] qIcon = new Image[4]; Text[] qCount = new Text[4]; Image flash;
        Text banner; float bannerT, bannerDur; Color bannerCol; Image deathBand; Text deathText, deathHint; float deathT = -1;
        class Toast { public RectTransform rt; public Text text; public Image icon; public CanvasGroup cg; public float t; }
        readonly List<Toast> toasts = new(); readonly List<Toast> pool = new(); Transform toastParent;

        public void Build(UIRoot ui, Transform canvas)
        {
            this.ui = ui; hp = ui.player.GetComponent<Health>(); st = ui.player.GetComponent<Stamina>(); inv = ui.player.GetComponent<Inventory>();
            hudRoot = NewRect("HUD", canvas).gameObject; Stretch((RectTransform)hudRoot.transform);
            var core = NewRect("Core", hudRoot.transform); Stretch(core); coreRoot = core.gameObject;
            var tl = new Vector2(0, 1);
            hpBar = MakeGauge(core.transform, "HP", UITheme.Red, new Vector2(520, 26), true, true); Set(hpBar.root, tl, tl, tl, new Vector2(44, -40), new Vector2(520, 26));
            stBar = MakeGauge(core.transform, "ST", UITheme.Green, new Vector2(380, 16), false, false); Set(stBar.root, tl, tl, tl, new Vector2(44, -74), new Vector2(380, 16));
            // 목표
            var diamond = Img(core.transform, "ObjIcon", UITheme.Slot, Color.white, true); Set(diamond.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(62, -140), new Vector2(34, 34)); diamond.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            var dot = Box(diamond.transform, "Dot", UITheme.Gold); Set(dot.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(14, 14));
            objective = Txt(core.transform, "", 30, UITheme.Cream); Set(objective.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(92, -140), new Vector2(700, 44));
            scoreText = Txt(core.transform, "", 24, UITheme.Muted); Set(scoreText.rectTransform, tl, tl, new Vector2(0, .5f), new Vector2(92, -178), new Vector2(700, 36));
            // 퀵슬롯(우하단, 다이아몬드)
            var br = new Vector2(1, 0);
            weaponText = Txt(core.transform, "", 22, UITheme.GoldDim, TextAnchor.MiddleRight); Set(weaponText.rectTransform, br, br, br, new Vector2(-40, 176), new Vector2(560, 34));
            for (int i = 0; i < 4; i++)
            {
                var slot = NewRect("Quick" + i, core.transform); Set(slot, br, br, new Vector2(.5f, .5f), new Vector2(-380 + i * 112, 92), new Vector2(96, 96));
                var dia = Img(slot, "Diamond", UITheme.Slot, Color.white, true); Set(dia.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(70, 70)); dia.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                qIcon[i] = Img(slot, "Icon", null, Color.white, false); Set(qIcon[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60));
                var key = Txt(slot, (i + 1).ToString(), 26, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold); Set(key.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(0, -2), new Vector2(40, 32));
                qCount[i] = Txt(slot, "", 22, UITheme.Cream, TextAnchor.LowerRight, FontStyle.Bold); Set(qCount[i].rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(2, 2), new Vector2(50, 30));
            }
            // 좌하단
            var bl = new Vector2(0, 0);
            goldText = Txt(core.transform, "", 28, UITheme.GoldBright); Set(goldText.rectTransform, bl, bl, bl, new Vector2(44, 70), new Vector2(400, 40));
            var hint = Txt(core.transform, "Q 구르기   좌클릭 공격   I 가방   1~4 퀵슬롯   Esc 휴식", 21, UITheme.Muted); Set(hint.rectTransform, bl, bl, bl, new Vector2(44, 28), new Vector2(900, 34));
            // 획득 알림(우상단)
            var tp = NewRect("Toasts", hudRoot.transform); Stretch(tp); toastParent = tp;
            // 중앙 배너·피격 번쩍임·쓰러짐
            flash = Box(hudRoot.transform, "Flash", new Color(.6f, 0, 0, 0)); Stretch(flash.rectTransform);
            banner = Txt(hudRoot.transform, "", 72, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, "Banner"); At(banner.rectTransform, new Vector2(.5f, .62f), Vector2.zero, new Vector2(1600, 120));
            deathBand = Box(hudRoot.transform, "DeathBand", new Color(0, 0, 0, 0)); At(deathBand.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(4000, 300));
            deathText = Txt(deathBand.transform, "쓰러졌다", 130, new Color(.72f, .1f, .1f, 0), TextAnchor.MiddleCenter, FontStyle.Bold, "DeathText"); Stretch(deathText.rectTransform);
            deathHint = Txt(hudRoot.transform, "R   다시 시작", 32, new Color(.9f, .85f, .7f, 0), TextAnchor.MiddleCenter); At(deathHint.rectTransform, new Vector2(.5f, .32f), Vector2.zero, new Vector2(800, 50));
            deathBand.gameObject.SetActive(false); deathHint.gameObject.SetActive(false);

            hp.Damaged += (a, p, k) => flash.color = new Color(.6f, 0, 0, .38f);
            if (inv != null) { inv.Picked += OnPicked; inv.Message += m => Toast_Text(m); }
            var gm = GameManager.Instance; if (gm != null) { gm.WaveStarted += w => Banner("웨이브 " + w, UITheme.Gold, 2.4f); gm.WaveCleared += w => Banner("적을 쓰러뜨렸다", new Color(1f, .86f, .5f), 2.6f); }
            hp.Died += () => { deathT = 0; deathBand.gameObject.SetActive(true); deathHint.gameObject.SetActive(true); };
        }

        void OnPicked(ItemDef d, int n) { if (d == null) Toast_Item(null, "금화  +" + n); else Toast_Item(d, d.name + (n > 1 ? "  ×" + n : "")); }
        void Toast_Text(string m) => Toast_Item(null, m);
        void Banner(string t, Color c, float dur) { banner.text = t; bannerCol = c; bannerT = bannerDur = dur; }

        void Toast_Item(ItemDef d, string msg)
        {
            Toast t = pool.Count > 0 ? pool[pool.Count - 1] : null; if (t != null) pool.RemoveAt(pool.Count - 1);
            if (t == null)
            {
                t = new Toast(); var panel = Img(toastParent, "Toast", UITheme.Slot, Color.white, true); t.rt = panel.rectTransform; Set(t.rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(460, 64)); t.cg = panel.gameObject.AddComponent<CanvasGroup>();
                t.icon = Img(panel.transform, "Icon", null, Color.white, false); Set(t.icon.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(8, 0), new Vector2(48, 48));
                t.text = Txt(panel.transform, "", 25, UITheme.Cream, TextAnchor.MiddleLeft); Set(t.text.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(34, 0), new Vector2(-84, 0));
            }
            t.t = 3.2f; t.text.text = msg; t.icon.enabled = d != null; t.icon.sprite = d != null ? ItemIcons.Get(d) : null; t.rt.gameObject.SetActive(true); toasts.Insert(0, t); if (toasts.Count > 5) { var old = toasts[toasts.Count - 1]; toasts.RemoveAt(toasts.Count - 1); old.rt.gameObject.SetActive(false); pool.Add(old); }
        }

        public void Update()
        {
            float dt = Time.unscaledDeltaTime;
            coreRoot.SetActive(GameSettings.ShowHud);
            float r = hp.Current / hp.maxHealth; hpBar.SetFill(r); hpTrail = r < hpTrail ? Mathf.MoveTowards(hpTrail, r, 0.35f * dt) : r; hpBar.SetTrail(hpTrail); hpBar.label.text = Mathf.CeilToInt(hp.Current) + " / " + Mathf.CeilToInt(hp.maxHealth);
            stBar.SetFill(st.Current / st.max);
            var gm = GameManager.Instance; if (gm != null) { objective.text = gm.Wave == 0 ? "그림자들이 몰려온다" : "웨이브 " + gm.Wave + "  —  남은 그림자 " + gm.EnemiesAlive + "마리"; scoreText.text = "처치 점수  " + gm.Score; }
            if (inv != null)
            {
                goldText.text = "금화  " + inv.gold.ToString("N0"); var w = ItemDatabase.Get(inv.equippedWeapon); weaponText.text = w != null ? w.name : "";
                for (int i = 0; i < 4; i++) { var d = ItemDatabase.Get(inv.quick[i]); int c = d != null ? inv.Count(d.id) : 0; qIcon[i].enabled = d != null; qIcon[i].sprite = d != null ? ItemIcons.Get(d) : null; qIcon[i].color = c > 0 ? Color.white : new Color(1, 1, 1, .3f); qCount[i].text = d != null ? c.ToString() : ""; }
            }
            // 알림 위치·페이드
            for (int i = toasts.Count - 1; i >= 0; i--) { var t = toasts[i]; t.t -= dt; if (t.t <= 0) { t.rt.gameObject.SetActive(false); toasts.RemoveAt(i); pool.Add(t); continue; } t.cg.alpha = Mathf.Clamp01(t.t / 0.6f); }
            for (int i = 0; i < toasts.Count; i++) toasts[i].rt.anchoredPosition = Vector2.Lerp(toasts[i].rt.anchoredPosition, new Vector2(-40, -40 - i * 72), 12f * dt);
            // 배너
            if (bannerT > 0) { bannerT -= dt; float k = bannerT / bannerDur, a = Mathf.Clamp01(Mathf.Min(k * 3f, (1f - k) * 5f)); banner.color = new Color(bannerCol.r, bannerCol.g, bannerCol.b, a); } else banner.color = new Color(0, 0, 0, 0);
            flash.color = new Color(.6f, 0, 0, Mathf.MoveTowards(flash.color.a, 0, 0.9f * dt));
            if (deathT >= 0) { deathT += dt; float a = Mathf.Clamp01(deathT / 1.6f); deathBand.color = new Color(0, 0, 0, .75f * a); deathText.color = new Color(.72f, .1f, .1f, a); deathHint.color = new Color(.9f, .85f, .7f, Mathf.Clamp01((deathT - 1.6f) / 0.8f)); }
        }
    }
}
