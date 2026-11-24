using System;
using UnityEngine;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>일시정지 메뉴: 대화 선택지처럼 » 표시와 금색 선택 막대. W/S·화살표·마우스, Enter/E 선택, Esc 닫기.</summary>
    public class PauseMenu : MenuPage
    {
        struct Entry { public string label; public Action action; public Image bar; public Text chevron, text; }
        Entry[] entries; int sel;

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            var dim = UIFactory.Box(canvas, "PauseRoot", UITheme.Dim); UIFactory.Stretch(dim.rectTransform); dim.raycastTarget = true; root = dim.gameObject;
            var defs = new (string, Action)[] { ("계속하기", ui.CloseAll), ("가방", () => ui.OpenSheet(false)), ("장비", () => ui.OpenSheet(true)), ("도감", ui.OpenCodex), ("기록 남기기", () => ui.Open(ui.savePage)), ("기록 불러오기", () => ui.Open(ui.loadPage)), ("설정", () => ui.Open(ui.settingsPage)), ("조작법", () => ui.Open(ui.controlsPage)), ("타이틀로", ui.ToTitle), ("게임 종료", ui.QuitGame) };
            float step = 74f; var panel = UIFactory.Panel(root.transform, "Panel", new Vector2(820, 230 + defs.Length * step)); UIFactory.Header(panel.transform, "휴식", 820);
            entries = new Entry[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                int idx = i; var row = UIFactory.Box(panel.transform, "Row" + i, new Color(0, 0, 0, 0)); row.raycastTarget = true;
                UIFactory.Set(row.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -150 - i * step), new Vector2(700, 68));
                var bar = UIFactory.Img(row.transform, "Bar", UITheme.Highlight, Color.white); UIFactory.Stretch(bar.rectTransform);
                var chev = UIFactory.Txt(row.transform, "»", 44, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, "Chevron"); UIFactory.Set(chev.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(34, 2), new Vector2(60, 70));
                var txt = UIFactory.Txt(row.transform, defs[i].Item1, 36, UITheme.Cream, TextAnchor.MiddleLeft, FontStyle.Normal, "Label"); UIFactory.Set(txt.rectTransform, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, .5f), new Vector2(110, 0), new Vector2(-150, 70));
                UIFactory.OnHover(row.gameObject, () => { sel = idx; Refresh(); }); UIFactory.OnClick(row.gameObject, () => entries[idx].action?.Invoke());
                entries[i] = new Entry { label = defs[i].Item1, action = defs[i].Item2, bar = bar, chevron = chev, text = txt };
            }
            var hint = UIFactory.Txt(panel.transform, "W/S 이동    Enter 선택    Esc 닫기", 22, UITheme.Muted, TextAnchor.MiddleCenter); UIFactory.Set(hint.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 26), new Vector2(700, 40));
            root.SetActive(false);
        }

        public override void Open() { base.Open(); sel = 0; Refresh(); }

        void Refresh()
        {
            for (int i = 0; i < entries.Length; i++) { bool s = i == sel; entries[i].bar.enabled = s; entries[i].chevron.color = s ? UITheme.GoldBright : UITheme.GoldDim; entries[i].text.color = s ? Color.white : UITheme.Cream; }
        }

        public override void Tick()
        {
            var n = InputAdapter.NavPressed(); if (n.y != 0) { sel = (sel - n.y + entries.Length) % entries.Length; Refresh(); }
            if (InputAdapter.ConfirmPressed()) entries[sel].action?.Invoke();
        }
    }

    /// <summary>글만 보여주는 화면(조작법 등).</summary>
    public class InfoPage : MenuPage
    {
        readonly string title; readonly string[] left, right;
        public InfoPage(string title, string[] left, string[] right) { this.title = title; this.left = left; this.right = right; }
        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            var dim = UIFactory.Box(canvas, "InfoRoot_" + title, UITheme.Dim); UIFactory.Stretch(dim.rectTransform); dim.raycastTarget = true; root = dim.gameObject;
            var panel = UIFactory.Panel(root.transform, "Panel", new Vector2(1040, Mathf.Max(760, 230 + left.Length * 62))); UIFactory.Header(panel.transform, title, 1040);
            for (int i = 0; i < left.Length; i++)
            {
                var k = UIFactory.Txt(panel.transform, left[i], 32, UITheme.Gold, TextAnchor.MiddleRight); UIFactory.Set(k.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(60, -150 - i * 62), new Vector2(380, 56));
                var v = UIFactory.Txt(panel.transform, right[i], 32, UITheme.Cream); UIFactory.Set(v.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(470, -150 - i * 62), new Vector2(480, 56));
            }
            var hint = UIFactory.Txt(panel.transform, "Esc 돌아가기", 22, UITheme.Muted, TextAnchor.MiddleCenter); UIFactory.Set(hint.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 26), new Vector2(600, 40));
            root.SetActive(false);
        }
    }
}
