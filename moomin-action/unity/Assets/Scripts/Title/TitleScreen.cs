using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MoominAction.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction
{
    /// <summary>
    /// 타이틀 화면(Title 씬): 뒤는 실제 맵(TitleCamera 가 성·산맥을 천천히 비춤), 왼쪽에 파랑·갈색·하양의 제목 "민미의 모험"과 메뉴.
    ///   이어하기(가장 최근 저장, 없으면 흐리게) · 새 게임 · 불러오기(자동 저장 + 기록 1~3) · 설정 · 종료.
    /// 그림: Resources/UI/Title/(Crest·Divider·Marker·MenuGlow·SlotFrame, tools/title_art.py). 없으면 글자·기본 스프라이트로 대신함.
    /// 등장: 검은 화면에서 밝아지며 문장 → 제목 → 메뉴 순서로 떠오름. 미리보기 tools/title_mock.py → previews/title_mock.png(좌표가 같음).
    /// </summary>
    public class TitleScreen : MonoBehaviour, IMenuHost
    {
        public string gameTitle = "민미의 모험", subtitle = "THE  ADVENTURE  OF  MINMI", version = "Ver 0.4";
        [Tooltip("안개(멀리 있는 산이 푸르게 흐려짐)")] public bool fog = true; public Color fogColor = new Color(0.63f, 0.71f, 0.83f); public float fogDensity = 0.0012f;

        class Entry { public string label; public System.Action act; public bool enabled = true; public RectTransform row; public Image glow, marker; public Text text, detail; }
        readonly List<Entry> entries = new(); int sel; float t;
        Canvas canvas; CanvasGroup crestG, titleG, menuG, footG; RectTransform mainRoot;
        SettingsMenu settings; SaveSlotPage load; readonly Stack<MenuPage> stack = new();
        MenuPage Current => stack.Count > 0 ? stack.Peek() : null;

        // 배치(1920×1080 기준, 왼쪽 위 기준 px) — tools/title_mock.py 의 같은 이름 값과 같게
        const float Left = 150f, CrestY = 200f, CrestSize = 176f, TitleY = 352f, SubY = 444f, DividerY = 492f, MenuY = 572f, MenuStep = 76f;

        void Awake()
        {
            UIState.Reset(); GameSettings.Load(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            if (fog) { RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = fogColor; RenderSettings.fogDensity = fogDensity; }
            canvas = MakeCanvas(transform, "TitleCanvas", 40);
        }

        void Start()
        {
            Build(); SceneFader.FadeIn(2.0f);
            settings = new SettingsMenu(); settings.BuildFor(this, canvas.transform);
            load = new SaveSlotPage(SaveSlotPage.Mode.Load, UITheme.TitleStyle, false); load.BuildFor(this, canvas.transform);
        }

        static Vector2 TL(float x, float y) => new Vector2(x, -y);

        void Build()
        {
            var pal = UITheme.TitleStyle; var tl = new Vector2(0, 1);
            mainRoot = NewRect("Title", canvas.transform); Stretch(mainRoot);
            var shade = Img(mainRoot, "LeftShade", UITheme.LeftShade, Color.white, false); Set(shade.rectTransform, Vector2.zero, new Vector2(0, 1), new Vector2(0, .5f), Vector2.zero, new Vector2(1150, 0));
            var bottom = Img(mainRoot, "BottomShade", UITheme.BottomShade, Color.white, false); Set(bottom.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 160));

            crestG = Group("Crest"); titleG = Group("TitleText"); menuG = Group("Menu"); footG = Group("Foot");
            var crestSprite = UITheme.TitleArt("Crest", 0, null);
            if (crestSprite != null) { var c = Img(crestG.transform, "Crest", crestSprite, Color.white, false); Set(c.rectTransform, tl, tl, new Vector2(.5f, .5f), TL(Left + CrestSize / 2 - 14, CrestY), new Vector2(CrestSize, CrestSize)); }

            var title = Txt(titleG.transform, gameTitle, 132, UITheme.Ivory, TextAnchor.MiddleLeft, FontStyle.Normal, "Title"); title.font = UITheme.TitleFont; Set(title.rectTransform, tl, tl, new Vector2(0, .5f), TL(Left - 6, TitleY), new Vector2(1300, 160));
            var o = title.gameObject.AddComponent<Outline>(); o.effectColor = new Color(UITheme.BlueDeep.r, UITheme.BlueDeep.g, UITheme.BlueDeep.b, .9f); o.effectDistance = new Vector2(3, -3);
            if (title.TryGetComponent<Shadow>(out var sh)) { sh.effectColor = new Color(0, 0, 0, .7f); sh.effectDistance = new Vector2(6, -8); }
            var sub = Txt(titleG.transform, subtitle, 30, UITheme.BlueBright, TextAnchor.MiddleLeft, FontStyle.Normal, "Subtitle"); Set(sub.rectTransform, tl, tl, new Vector2(0, .5f), TL(Left, SubY), new Vector2(1000, 44));
            var div = Img(titleG.transform, "Divider", UITheme.TitleArt("DividerLeft", 0, UITheme.Highlight), Color.white, false); Set(div.rectTransform, tl, tl, new Vector2(0, .5f), TL(Left - 20, DividerY), new Vector2(640, 26));

            int latest = SaveSystem.LatestSlot(); bool any = SaveSystem.AnyExists();
            var cont = new Entry { label = "이어하기", enabled = latest >= 0, act = () => { if (latest >= 0) SaveSystem.Load(latest); } };
            if (latest >= 0) { var d = SaveSystem.Peek(latest); cont.detail = null; contDetail = SaveFormat.Summary(d.level, d.place, d.wave) + "   ·   " + SaveFormat.Ago(d.savedAt, System.DateTime.UtcNow); }
            entries.Add(cont);
            entries.Add(new Entry { label = "새 게임", act = SaveSystem.StartNewGame });
            entries.Add(new Entry { label = "불러오기", enabled = any, act = () => Open(load) });
            entries.Add(new Entry { label = "설정", act = () => Open(settings) });
            entries.Add(new Entry { label = "종료", act = Quit });
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i]; int idx = i;
                var row = Box(menuG.transform, "Item" + i, new Color(0, 0, 0, 0)); row.raycastTarget = true; e.row = row.rectTransform;
                Set(e.row, tl, tl, new Vector2(0, .5f), TL(Left - 40, MenuY + i * MenuStep), new Vector2(760, 64));
                e.glow = Img(row.transform, "Glow", pal.highlight, Color.white, false); Stretch(e.glow.rectTransform);
                if (pal.marker != null) { e.marker = Img(row.transform, "Marker", pal.marker, Color.white, false); Set(e.marker.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(40, 0), new Vector2(30, 30)); }
                e.text = Txt(row.transform, e.label, 40, pal.text, TextAnchor.MiddleLeft, FontStyle.Normal, "Label"); Set(e.text.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(76, 0), new Vector2(240, 60));
                if (i == 0) { e.detail = Txt(row.transform, contDetail ?? "저장된 기록이 없다", 22, pal.muted, TextAnchor.MiddleLeft, FontStyle.Normal, "Detail"); Set(e.detail.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(270, -2), new Vector2(480, 40)); }
                OnHover(row.gameObject, () => { if (Current == null && entries[idx].enabled) { sel = idx; Refresh(); } });
                OnClick(row.gameObject, () => { if (Current == null && entries[idx].enabled && !SceneFader.Busy) { sel = idx; entries[idx].act?.Invoke(); } });
            }
            var hint = Txt(footG.transform, "W / S  ·  마우스   이동         Enter   선택", 22, pal.muted); Set(hint.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(Left, 40), new Vector2(900, 36));
            var ver = Txt(footG.transform, version, 20, new Color(pal.muted.r, pal.muted.g, pal.muted.b, .7f), TextAnchor.MiddleRight); Set(ver.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 40), new Vector2(400, 32));
            sel = latest >= 0 ? 0 : 1; Refresh(); Animate(0f);
        }
        string contDetail;

        CanvasGroup Group(string name) { var r = NewRect(name, mainRoot); Stretch(r); return r.gameObject.AddComponent<CanvasGroup>(); }

        void Refresh()
        {
            var pal = UITheme.TitleStyle;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i]; bool s = i == sel && Current == null;
                e.glow.enabled = s; if (e.marker != null) e.marker.enabled = s;
                e.text.color = !e.enabled ? new Color(pal.muted.r, pal.muted.g, pal.muted.b, .35f) : s ? UITheme.Ivory : pal.text * new Color(1, 1, 1, .85f);
                e.text.fontStyle = s ? FontStyle.Bold : FontStyle.Normal;
                if (e.detail != null) e.detail.color = new Color(pal.muted.r, pal.muted.g, pal.muted.b, e.enabled ? (s ? 1f : .6f) : .35f);
            }
        }

        /// <summary>등장: 문장(0.4~1.4초) → 제목(0.9~2.1초, 살짝 떠오름) → 메뉴(1.7~2.7초) → 아래 안내.</summary>
        void Animate(float time)
        {
            float k(float a, float b) { float u = Mathf.Clamp01((time - a) / (b - a)); return u * u * (3 - 2 * u); }
            crestG.alpha = k(.4f, 1.4f); titleG.alpha = k(.9f, 2.1f); menuG.alpha = k(1.7f, 2.7f); footG.alpha = k(2.4f, 3.2f);
            ((RectTransform)titleG.transform).anchoredPosition = new Vector2(0, -18 * (1 - k(.9f, 2.1f)));
            ((RectTransform)menuG.transform).anchoredPosition = new Vector2(-24 * (1 - k(1.7f, 2.7f)), 0);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime; if (t < 3.5f) Animate(t);
            if (SceneFader.Busy && t > 2.5f) return;
            if (Current != null)
            {
                Current.Tick(); if (InputAdapter.CancelPressed()) Back(); return;
            }
            if (t < 1.7f) return;
            var nav = InputAdapter.NavPressed();
            if (nav.y != 0) { for (int n = 0; n < entries.Count; n++) { sel = (sel - nav.y + entries.Count) % entries.Count; if (entries[sel].enabled) break; } Refresh(); }
            if (InputAdapter.ConfirmPressed() && entries[sel].enabled) entries[sel].act?.Invoke();
            // 선택 막대가 숨 쉬듯
            var g = entries[sel].glow; if (g.enabled) g.color = new Color(1, 1, 1, .8f + .2f * Mathf.Sin(Time.unscaledTime * 2.4f));
        }

        public void Open(MenuPage page) { if (Current != null) Current.Close(); stack.Push(page); page.Open(); mainRoot.gameObject.SetActive(false); }
        public void Back()
        {
            if (stack.Count == 0) return; stack.Pop().Close(); GameSettings.Save();
            if (stack.Count > 0) stack.Peek().Open(); else { mainRoot.gameObject.SetActive(true); Refresh(); }
        }

        void Quit()
        {
            GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
