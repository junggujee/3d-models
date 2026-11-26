using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>
    /// 대장간 화면(풍선고래에게 F → 선택지). 두 쪽:
    ///  ① 제작 — 3 × 3 작업대(UI/CraftBench): 가방 재료를 모양에 맞게 놓으면 결과가 나옴(ForgeMath.Match).
    ///  ② 강화 — 가진 무기·장비를 돈으로 +5 까지. 성공률·비용·실패했을 때 어떻게 되는지 미리 보여 준다.
    ///     두들기는 연출(화덕이 번쩍 · 망치 소리에 맞춰 화면이 흔들림 · 불똥) 뒤에 성공/실패가 뜬다.
    /// 수치는 모두 ForgeMath(순수 C#)에 있고 여기서는 보여 주기만 한다.
    /// </summary>
    public class SmithPage : MenuPage
    {
        const float W = 1180f, H = 760f;
        BalloonSmith smith; Inventory inv; int tab; string pickItem; CraftBench bench;
        Image panel; Transform listRoot, infoRoot; Text goldText, tabCraft, tabEnh;
        readonly List<GameObject> rows = new();
        float anvilT = -1f; int anvilStep; bool anvilSuccess; string anvilItem; int anvilFrom;
        Text resultText; Image flash;

        public static void Open(UIRoot ui, BalloonSmith smith, int tab)
        {
            var p = new SmithPage { smith = smith, tab = Mathf.Clamp(tab, 0, 1) };
            p.Build(ui, ui.Canvas); ui.Open(p);
        }

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            inv = ui != null && ui.Player != null ? ui.Player.GetComponent<Inventory>() : null;
            root = UIFactory.NewRect("SmithPage", canvas).gameObject;
            UIFactory.Stretch((RectTransform)root.transform);
            var dim = UIFactory.Box(root.transform, "Dim", UITheme.Dim); UIFactory.Stretch(dim.rectTransform); dim.raycastTarget = true;
            panel = UIFactory.Panel(root.transform, "Panel", new Vector2(W, H));
            UIFactory.Header(panel.transform, "마을 대장간", W);
            var sub = UIFactory.Txt(panel.transform, "풍선고래 대장장이", 24, UITheme.Muted, TextAnchor.MiddleCenter);
            UIFactory.At(sub.rectTransform, new Vector2(.5f, 1), new Vector2(0, -112), new Vector2(W - 120, 32));
            goldText = UIFactory.Txt(panel.transform, "", 26, UITheme.GoldBright, TextAnchor.MiddleRight);
            UIFactory.At(goldText.rectTransform, new Vector2(1, 1), new Vector2(-40, -40), new Vector2(300, 36));

            tabCraft = TabButton("제  작", -200f, 0); tabEnh = TabButton("강  화", 20f, 1);
            listRoot = UIFactory.NewRect("List", panel.transform); UIFactory.At((RectTransform)listRoot, new Vector2(0, 1), new Vector2(40, -200), new Vector2(470, H - 260));
            infoRoot = UIFactory.NewRect("Info", panel.transform); UIFactory.At((RectTransform)infoRoot, new Vector2(1, 1), new Vector2(-40, -200), new Vector2(600, H - 260));
            ((RectTransform)listRoot).pivot = new Vector2(0, 1); ((RectTransform)infoRoot).pivot = new Vector2(1, 1);
            if (inv != null) { bench = new CraftBench(inv, smith, () => { if (goldText != null) goldText.text = "골드  " + inv.gold.ToString("N0"); }); bench.Build(panel.transform); }
            flash = UIFactory.Box(panel.transform, "Flash", new Color(1f, 0.8f, 0.4f, 0f)); UIFactory.Stretch(flash.rectTransform); flash.raycastTarget = false;
            var close = UIFactory.Txt(panel.transform, "Esc  닫기", 22, UITheme.Muted, TextAnchor.MiddleCenter);
            UIFactory.At(close.rectTransform, new Vector2(.5f, 0), new Vector2(0, 28), new Vector2(300, 32));
            Refresh();
        }

        Text TabButton(string label, float x, int which)
        {
            var im = UIFactory.Img(panel.transform, "Tab" + which, UITheme.Slot, Color.white, true, true);
            UIFactory.At(im.rectTransform, new Vector2(.5f, 1), new Vector2(x + 90f, -158), new Vector2(180, 46));
            var t = UIFactory.StyleHeading(UIFactory.Txt(im.transform, label, 28, UITheme.Cream, TextAnchor.MiddleCenter));
            UIFactory.Stretch(t.rectTransform);
            UIFactory.OnClick(im.gameObject, () => { if (anvilT >= 0f || (bench != null && bench.Busy)) return; tab = which; pickItem = null; Refresh(); });
            return t;
        }

        void Clear() { foreach (var g in rows) if (g != null) Object.Destroy(g); rows.Clear(); }

        void Refresh()
        {
            if (inv == null) return;
            goldText.text = "골드  " + inv.gold.ToString("N0");
            tabCraft.color = tab == 0 ? UITheme.GoldBright : UITheme.Muted; tabEnh.color = tab == 1 ? UITheme.GoldBright : UITheme.Muted;
            Clear();
            if (bench != null) { bench.root.gameObject.SetActive(tab == 0); if (tab == 0) bench.Refresh(); }
            ((RectTransform)listRoot).gameObject.SetActive(tab == 1); ((RectTransform)infoRoot).gameObject.SetActive(tab == 1);
            if (tab == 1) { BuildEnhanceList(); BuildInfo(); }
        }

        // ── ② 강화
        void BuildEnhanceList()
        {
            float y = 0; var seen = new HashSet<string>();
            foreach (var id in Enhanceable())
            {
                if (!seen.Add(id)) continue;
                var def = ItemDatabase.Get(id); if (def == null) continue;
                int lv = inv.Enhance(id);
                var row = Row(listRoot, y, def, ForgeMath.Prefix(lv) + def.name, lv >= ForgeMath.MaxLevel ? UITheme.Gold : UITheme.Cream, id == pickItem);
                string s = id; UIFactory.OnClick(row, () => { pickItem = s; Refresh(); });
                y -= 74f;
            }
            if (y == 0) UIFactory.Txt(listRoot, "강화할 무기나 장비가 없어요.", 24, UITheme.Muted, TextAnchor.UpperLeft);
        }
        /// <summary>가방에 있는 무기·방어구·아티팩트(강화할 수 있는 것).</summary>
        IEnumerable<string> Enhanceable()
        {
            foreach (var s in inv.slots)
            {
                if (s == null || s.Empty) continue; var d = s.Def;
                if (d != null && d.Equippable) yield return s.id;
            }
        }

        GameObject Row(Transform parent, float y, ItemDef def, string label, Color col, bool selected)
        {
            var im = UIFactory.Img(parent, "Row", UITheme.Slot, selected ? new Color(1f, 0.92f, 0.7f) : new Color(1, 1, 1, 0.55f), true, true);
            UIFactory.At(im.rectTransform, new Vector2(0, 1), new Vector2(0, y), new Vector2(460, 66)); im.rectTransform.pivot = new Vector2(0, 1);
            var icon = UIFactory.Img(im.transform, "Icon", ItemIcons.Get(def), Color.white, false);
            UIFactory.At(icon.rectTransform, new Vector2(0, .5f), new Vector2(42, 0), new Vector2(52, 52));
            var t = UIFactory.Txt(im.transform, label, 26, col, TextAnchor.MiddleLeft);
            UIFactory.At(t.rectTransform, new Vector2(0, .5f), new Vector2(80, 0), new Vector2(360, 40)); t.rectTransform.pivot = new Vector2(0, .5f);
            rows.Add(im.gameObject); return im.gameObject;
        }

        // ── 오른쪽: 고른 것의 설명 + 단추
        void BuildInfo()
        {
            float y = 0;
            void Line(string s, int size, Color c) { var t = UIFactory.Txt(infoRoot, s, size, c, TextAnchor.UpperLeft); UIFactory.At(t.rectTransform, new Vector2(1, 1), new Vector2(0, y), new Vector2(580, size + 16)); t.rectTransform.pivot = new Vector2(1, 1); t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; rows.Add(t.gameObject); y -= size + 18; }
            if (anvilT >= 0f) { Line(anvilStep < 3 ? "두드리는 중…" : (anvilSuccess ? "성공!" : "실패…"), 40, anvilSuccess || anvilStep < 3 ? UITheme.GoldBright : UITheme.Red); return; }
            {
                if (string.IsNullOrEmpty(pickItem)) { Line("왼쪽에서 강화할 것을 고르세요.", 26, UITheme.Muted); return; }
                var def = ItemDatabase.Get(pickItem); if (def == null) return;
                int lv = inv.Enhance(pickItem);
                Line(ForgeMath.Prefix(lv) + def.name, 36, UITheme.GoldBright);
                Line($"지금  공격 배율 ×{ForgeMath.DamageBonus(lv):0.00}  ·  최종 위력 +{ForgeMath.PowerBonus(lv)}  ·  받는 피해 -{ForgeMath.GuardBonus(lv) * 100:0.#}%", 24, UITheme.Cream);
                Line("", 10, UITheme.Cream);
                if (lv >= ForgeMath.MaxLevel) { Line("더 올릴 수 없어요. 이게 내 한계야!", 28, UITheme.Gold); return; }
                int cost = ForgeMath.Cost(lv, def.value); float ch = ForgeMath.SuccessChance(lv);
                Line($"+{lv}  →  +{lv + 1}", 34, UITheme.Gold);
                Line($"다음  공격 배율 ×{ForgeMath.DamageBonus(lv + 1):0.00}  ·  최종 위력 +{ForgeMath.PowerBonus(lv + 1)}  ·  받는 피해 -{ForgeMath.GuardBonus(lv + 1) * 100:0.#}%", 24, UITheme.Cream);
                Line($"성공률  {ch * 100:0}%", 28, ch >= 0.6f ? UITheme.Green : UITheme.Red);
                Line($"비용  {cost:N0} 골드   (가진 돈 {inv.gold:N0})", 25, inv.gold >= cost ? UITheme.Cream : UITheme.Red);
                Line(ForgeMath.OnFail(lv) < lv ? $"실패하면  +{ForgeMath.OnFail(lv)} 로 내려가요(부서지지는 않아요)" : "실패해도 단계는 그대로예요(돈만 날아가요)", 24, UITheme.Muted);
                Button("두들기기", inv.gold >= cost, () => Enhance(pickItem, lv, cost, ch));
            }
        }

        void Button(string label, bool on, System.Action act)
        {
            var im = UIFactory.Img(infoRoot, "Btn", UITheme.Slot, on ? new Color(1f, 0.9f, 0.62f) : new Color(1, 1, 1, 0.35f), true, true);
            UIFactory.At(im.rectTransform, new Vector2(1, 0), new Vector2(0, 8), new Vector2(240, 60)); im.rectTransform.pivot = new Vector2(1, 0);
            var t = UIFactory.StyleHeading(UIFactory.Txt(im.transform, label, 30, on ? UITheme.Ink : UITheme.Muted, TextAnchor.MiddleCenter)); UIFactory.Stretch(t.rectTransform);
            if (on) UIFactory.OnClick(im.gameObject, () => act());
            rows.Add(im.gameObject);
        }

        void Enhance(string id, int lv, int cost, float chance)
        {
            if (inv.gold < cost) return;
            inv.gold -= cost; anvilItem = id; anvilFrom = lv;
            anvilSuccess = Random.value < chance;                 // 판정은 지금, 보여 주기는 연출 뒤
            anvilT = 0f; anvilStep = 0; Refresh();
        }

        public override void Tick()
        {
            if (bench != null && tab == 0) bench.Tick(Time.unscaledDeltaTime);
            if (anvilT < 0f) return;
            anvilT += Time.unscaledDeltaTime;
            int step = anvilT < 0.55f ? 0 : anvilT < 1.1f ? 1 : anvilT < 1.65f ? 2 : 3;
            if (step != anvilStep && step < 3)                     // 세 번 두드림
            {
                anvilStep = step;
                if (smith != null) smith.Play("Hammer");
                ForgeFx.Strike(smith != null ? smith.transform.position + Vector3.up * 0.7f : Vector3.zero);
                flash.color = new Color(1f, 0.78f, 0.38f, 0.22f);
                Refresh();
            }
            else if (step == 3 && anvilStep != 3)
            {
                anvilStep = 3;
                int to = anvilSuccess ? anvilFrom + 1 : ForgeMath.OnFail(anvilFrom);
                inv.SetEnhance(anvilItem, to);
                var def = ItemDatabase.Get(anvilItem);
                if (smith != null) smith.Play(anvilSuccess ? "Cheer" : "Fail");
                ForgeFx.Burst(smith != null ? smith.transform.position + Vector3.up * 0.9f : Vector3.zero, anvilSuccess);
                flash.color = anvilSuccess ? new Color(1f, 0.85f, 0.45f, 0.5f) : new Color(0.5f, 0.2f, 0.2f, 0.35f);
                if (anvilSuccess) { Toast.Show($"풍선고래: 됐다! {def.name} +{to} 야~"); Quests.AnnounceNow("강화 성공!", $"{def.name}  +{to}"); }
                else Toast.Show(to < anvilFrom ? $"풍선고래: 으앗… 미안, +{to} 로 내려갔어." : "풍선고래: 아깝다~ 이번엔 안 붙었네. 무기는 멀쩡해!");
                Refresh();
            }
            if (flash != null) flash.color = new Color(flash.color.r, flash.color.g, flash.color.b, Mathf.Max(0f, flash.color.a - Time.unscaledDeltaTime * 1.4f));
            if (anvilT > 2.6f) { anvilT = -1f; Refresh(); }
        }

        public override bool HandleCancel() => anvilT >= 0f || (bench != null && bench.Busy);       // 두드리는 중 · 특별 제작 중엔 못 닫음
        public override void Close() { base.Close(); if (root != null) Object.Destroy(root); }
    }
}
