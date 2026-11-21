using System;
using UnityEngine;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>설정창: 음량·마우스 감도(막대), 시점 반전·화면 모드·품질·수직 동기화·인터페이스 표시(선택). ←/→ 또는 ◀▶ 클릭으로 조절, 즉시 적용·저장.</summary>
    public class SettingsMenu : MenuPage
    {
        class Row { public string label; public Func<string> value; public Action<int> adjust; public Func<float> bar; public Action activate; public Image highlight; public Text valueText, labelText; public RectTransform fill; }
        Row[] rows; int sel;

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            var dim = UIFactory.Box(canvas, "SettingsRoot", UITheme.Dim); UIFactory.Stretch(dim.rectTransform); dim.raycastTarget = true; root = dim.gameObject;
            var panel = UIFactory.Panel(root.transform, "Panel", new Vector2(1100, 860)); UIFactory.Header(panel.transform, "설정", 1100);
            string[] q = QualitySettings.names;
            rows = new[] {
                new Row { label = "전체 음량", value = () => Mathf.RoundToInt(GameSettings.MasterVolume * 100) + "%", bar = () => GameSettings.MasterVolume, adjust = d => { GameSettings.MasterVolume = Mathf.Clamp01(Mathf.Round((GameSettings.MasterVolume + d * 0.05f) * 20f) / 20f); } },
                new Row { label = "마우스 감도", value = () => GameSettings.MouseSensitivity.ToString("0.0"), bar = () => (GameSettings.MouseSensitivity - 0.2f) / 2.3f, adjust = d => { GameSettings.MouseSensitivity = Mathf.Clamp(Mathf.Round((GameSettings.MouseSensitivity + d * 0.1f) * 10f) / 10f, 0.2f, 2.5f); } },
                new Row { label = "시점 상하 반전", value = () => GameSettings.InvertY ? "켜짐" : "꺼짐", adjust = d => GameSettings.InvertY = !GameSettings.InvertY },
                new Row { label = "화면 모드", value = () => GameSettings.Fullscreen ? "전체 화면" : "창 모드", adjust = d => GameSettings.Fullscreen = !GameSettings.Fullscreen },
                new Row { label = "그래픽 품질", value = () => q.Length > 0 ? q[Mathf.Clamp(GameSettings.Quality, 0, q.Length - 1)] : "-", adjust = d => { if (q.Length > 0) GameSettings.Quality = (GameSettings.Quality + d + q.Length) % q.Length; } },
                new Row { label = "수직 동기화", value = () => GameSettings.VSync ? "켜짐" : "꺼짐", adjust = d => GameSettings.VSync = !GameSettings.VSync },
                new Row { label = "인터페이스 표시", value = () => GameSettings.ShowHud ? "켜짐" : "꺼짐", adjust = d => GameSettings.ShowHud = !GameSettings.ShowHud },
                new Row { label = "기본값으로 되돌리기", activate = () => GameSettings.Reset() },
                new Row { label = "닫기", activate = () => ui.Back() },
            };
            for (int i = 0; i < rows.Length; i++)
            {
                int idx = i; var r = rows[i]; float y = -150 - i * 78;
                var row = UIFactory.Box(panel.transform, "Row" + i, new Color(0, 0, 0, 0)); row.raycastTarget = true; UIFactory.Set(row.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(960, 70));
                r.highlight = UIFactory.Img(row.transform, "Bar", UITheme.Highlight, Color.white); UIFactory.Stretch(r.highlight.rectTransform);
                bool button = r.adjust == null;
                r.labelText = UIFactory.Txt(row.transform, "» " + r.label, 32, UITheme.Cream, button ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft); UIFactory.Set(r.labelText.rectTransform, new Vector2(0, 0), new Vector2(button ? 1 : .5f, 1), new Vector2(0, .5f), new Vector2(button ? 0 : 40, 0), new Vector2(button ? 0 : -40, 0));
                if (!button)
                {
                    float vx = 250;   // 값 영역 중심(오른쪽)
                    var left = UIFactory.Txt(row.transform, "◀", 30, UITheme.Gold, TextAnchor.MiddleCenter); left.raycastTarget = true; UIFactory.Set(left.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(vx - 190, 0), new Vector2(50, 60)); UIFactory.OnClick(left.gameObject, () => { sel = idx; Change(-1); });
                    var rt = UIFactory.Txt(row.transform, "▶", 30, UITheme.Gold, TextAnchor.MiddleCenter); rt.raycastTarget = true; UIFactory.Set(rt.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(vx + 190, 0), new Vector2(50, 60)); UIFactory.OnClick(rt.gameObject, () => { sel = idx; Change(1); });
                    r.valueText = UIFactory.Txt(row.transform, "", 30, UITheme.GoldBright, TextAnchor.MiddleCenter); UIFactory.Set(r.valueText.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(vx, r.bar != null ? 14 : 0), new Vector2(320, 44));
                    if (r.bar != null)
                    {
                        var track = UIFactory.Img(row.transform, "Track", UITheme.Bar, Color.white, true, true); UIFactory.Set(track.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(vx, -16), new Vector2(300, 14));
                        var f = UIFactory.Box(track.transform, "Fill", UITheme.Gold); UIFactory.Stretch(f.rectTransform, 3); r.fill = f.rectTransform; f.rectTransform.anchorMax = new Vector2(0.5f, 1);
                        var trackRect = track.rectTransform; var rr = r;
                        UIFactory.OnClick(track.gameObject, e => { RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRect, e.position, e.pressEventCamera, out var lp); float u = Mathf.Clamp01((lp.x + trackRect.rect.width * .5f) / trackRect.rect.width); sel = idx; SetBar(rr, u); });
                    }
                }
                UIFactory.OnHover(row.gameObject, () => { sel = idx; Refresh(); }); if (button) UIFactory.OnClick(row.gameObject, () => { sel = idx; rows[idx].activate?.Invoke(); Refresh(); });
            }
            var hint = UIFactory.Txt(panel.transform, "W/S 이동    A/D 조절    Enter 선택    Esc 돌아가기", 22, UITheme.Muted, TextAnchor.MiddleCenter); UIFactory.Set(hint.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 22), new Vector2(900, 40));
            root.SetActive(false);
        }

        void SetBar(Row r, float u) { if (r.label == "전체 음량") GameSettings.MasterVolume = Mathf.Round(u * 20f) / 20f; else GameSettings.MouseSensitivity = Mathf.Round((0.2f + u * 2.3f) * 10f) / 10f; Apply(); }
        void Change(int d) { rows[sel].adjust?.Invoke(d); Apply(); }
        void Apply() { GameSettings.Apply(); GameSettings.Save(); Refresh(); }

        public override void Open() { base.Open(); sel = 0; Refresh(); }
        public override void Close() { base.Close(); GameSettings.Save(); }

        void Refresh()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i]; bool s = i == sel; r.highlight.enabled = s; r.labelText.color = s ? Color.white : UITheme.Cream;
                if (r.valueText != null) r.valueText.text = r.value(); if (r.fill != null) r.fill.anchorMax = new Vector2(Mathf.Clamp01(r.bar()), 1);
            }
        }

        public override void Tick()
        {
            var n = InputAdapter.NavPressed();
            if (n.y != 0) { sel = (sel - n.y + rows.Length) % rows.Length; Refresh(); }
            if (n.x != 0 && rows[sel].adjust != null) Change(n.x);
            if (InputAdapter.ConfirmPressed() && rows[sel].activate != null) { rows[sel].activate(); Refresh(); }
        }
    }
}
