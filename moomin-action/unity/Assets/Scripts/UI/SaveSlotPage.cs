using System;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 저장 슬롯 화면(남기기 / 불러오기). 한 줄 = 화면 사진 · 기록 이름 · "Lv.12 · 마을 광장 · 웨이브 7" · 플레이 시간 · 날짜(몇 분 전).
    /// 불러오기는 자동 저장 + 기록 1~3, 남기기는 기록 1~3(자동 저장은 웨이브를 끝낼 때 저절로).
    /// 덮어쓰기·지우기·(게임 중) 불러오기는 한 번 더 눌러야 실행. 테마는 여는 곳에 맞춤(게임 안 금박 / 타이틀 파랑·갈색).
    /// </summary>
    public class SaveSlotPage : MenuPage
    {
        public enum Mode { Save, Load }
        readonly Mode mode; readonly UITheme.Palette pal; readonly bool confirmLoad;
        class Row { public int slot; public Image bg, glow, marker; public Text markerText, name, summary, time, date, empty; public RawImage thumb; public Text thumbEmpty; }
        Row[] rows; int sel; int confirmSlot = -1; string confirmWhat; Text message; float messageT;

        /// <param name="confirmLoad">게임 중 불러오기면 true(지금 진행이 사라지므로 한 번 더 확인).</param>
        public SaveSlotPage(Mode mode, UITheme.Palette palette, bool confirmLoad) { this.mode = mode; pal = palette; this.confirmLoad = confirmLoad; }

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            var dim = Box(canvas, "SaveSlots_" + mode, pal.dimBg); Stretch(dim.rectTransform); dim.raycastTarget = true; root = dim.gameObject;
            int n = mode == Mode.Load ? SaveFormat.SlotCount : SaveFormat.ManualSlots; float rowH = 168, gap = 186, top = -158;
            float h = -top + n * gap + 110;
            var panel = Img(root.transform, "Panel", pal.frame, Color.white, true, true); At(panel.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1280, h));
            var title = Txt(panel.transform, mode == Mode.Save ? "기록 남기기" : "기록 불러오기", 46, pal.bright, TextAnchor.MiddleCenter, FontStyle.Bold, "Title");
            Set(title.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(1100, 64));
            bool inGame = pal.accent == UITheme.Gold;
            var line = Img(panel.transform, "Divider", inGame ? UITheme.Highlight : UITheme.TitleArt("Divider", 0, UITheme.Highlight), Color.white, true);
            Set(line.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -100), new Vector2(900, inGame ? 12 : 22));
            rows = new Row[n];
            for (int i = 0; i < n; i++)
            {
                int slot = mode == Mode.Load ? (i == 0 ? SaveFormat.AutoSlot : i - 1) : i; int idx = i;
                var r = new Row { slot = slot };
                r.bg = Img(panel.transform, "Slot" + i, pal.row, Color.white, true, true); Set(r.bg.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, top - i * gap), new Vector2(1160, rowH));
                r.glow = Img(r.bg.transform, "Glow", pal.highlight, Color.white); Stretch(r.glow.rectTransform, 2);
                var tl = new Vector2(0, 1); var trn = new Vector2(1, 1);
                var frame = Box(r.bg.transform, "ThumbFrame", new Color(pal.dim.r, pal.dim.g, pal.dim.b, .9f)); Set(frame.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(46, 0), new Vector2(252, 144));
                var thumbBack = Box(frame.transform, "ThumbBack", new Color(0.02f, 0.03f, 0.05f, 1f)); Stretch(thumbBack.rectTransform, 3);
                r.thumb = new GameObject("Thumb", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); r.thumb.transform.SetParent(frame.transform, false); Stretch(r.thumb.rectTransform, 3); r.thumb.raycastTarget = false;
                r.thumbEmpty = Txt(frame.transform, "—", 40, pal.muted, TextAnchor.MiddleCenter); Stretch(r.thumbEmpty.rectTransform);
                if (pal.marker != null) { r.marker = Img(r.bg.transform, "Marker", pal.marker, Color.white, false); Set(r.marker.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(22, 0), new Vector2(30, 30)); }
                else { r.markerText = Txt(r.bg.transform, pal.markerGlyph, 40, pal.accent, TextAnchor.MiddleCenter, FontStyle.Bold); Set(r.markerText.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(22, 2), new Vector2(40, 60)); }
                r.name = Txt(r.bg.transform, SaveFormat.SlotName(slot), 34, pal.bright, TextAnchor.MiddleLeft, FontStyle.Bold); Set(r.name.rectTransform, tl, tl, tl, new Vector2(330, -18), new Vector2(460, 48));
                r.summary = Txt(r.bg.transform, "", 29, pal.text); Set(r.summary.rectTransform, tl, tl, tl, new Vector2(330, -72), new Vector2(560, 44));
                r.empty = Txt(r.bg.transform, mode == Mode.Save ? "비어 있음 — 여기에 남길 수 있다" : "비어 있음", 28, pal.muted); Set(r.empty.rectTransform, tl, tl, tl, new Vector2(330, -72), new Vector2(560, 44));
                r.time = Txt(r.bg.transform, "", 26, pal.accent, TextAnchor.MiddleRight); Set(r.time.rectTransform, trn, trn, trn, new Vector2(-34, -22), new Vector2(320, 40));
                r.date = Txt(r.bg.transform, "", 23, pal.muted, TextAnchor.MiddleRight); Set(r.date.rectTransform, trn, trn, trn, new Vector2(-34, -112), new Vector2(420, 36));
                OnHover(r.bg.gameObject, () => { if (sel != idx) { sel = idx; confirmSlot = -1; Refresh(); } });
                OnClick(r.bg.gameObject, () => { sel = idx; Activate(); });
                rows[i] = r;
            }
            message = Txt(panel.transform, "", 26, pal.accent, TextAnchor.MiddleCenter, FontStyle.Bold, "Message"); Set(message.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 62), new Vector2(1100, 40));
            var hint = Txt(panel.transform, "W/S 이동    Enter 선택    X 지우기    Esc 돌아가기", 22, pal.muted, TextAnchor.MiddleCenter); Set(hint.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 22), new Vector2(1000, 36));
            root.SetActive(false);
        }

        public override void Open()
        {
            base.Open(); confirmSlot = -1; Say("", 0);
            sel = 0; if (mode == Mode.Load) { int latest = SaveSystem.LatestSlot(); for (int i = 0; i < rows.Length; i++) if (rows[i].slot == latest) sel = i; }
            Refresh();
        }

        void Say(string m, float seconds) { message.text = m; messageT = seconds; }

        void Refresh()
        {
            var now = DateTime.UtcNow;
            for (int i = 0; i < rows.Length; i++)
            {
                var r = rows[i]; var d = SaveSystem.Peek(r.slot); bool s = i == sel, has = d != null;
                r.glow.enabled = s; r.bg.color = s ? Color.white : new Color(1, 1, 1, .82f);
                if (r.marker != null) r.marker.enabled = s; if (r.markerText != null) r.markerText.enabled = s;
                r.name.color = s ? pal.bright : pal.text;
                r.summary.gameObject.SetActive(has); r.time.gameObject.SetActive(has); r.date.gameObject.SetActive(has); r.empty.gameObject.SetActive(!has);
                var tex = has ? SaveSystem.Thumbnail(r.slot) : null; r.thumb.texture = tex; r.thumb.enabled = tex != null; r.thumbEmpty.enabled = tex == null;
                if (!has) continue;
                r.summary.text = SaveFormat.Summary(d.level, d.place, d.wave);
                r.time.text = "플레이  " + SaveFormat.PlayTime(d.playSeconds);
                string ago = SaveFormat.Ago(d.savedAt, now), date = SaveFormat.Date(d.savedAt);
                r.date.text = ago == date || string.IsNullOrEmpty(ago) ? date : date + "   ·   " + ago;
            }
        }

        void Activate()
        {
            var r = rows[sel]; bool has = SaveSystem.Exists(r.slot); string name = SaveFormat.SlotName(r.slot);
            if (mode == Mode.Save)
            {
                if (!SaveSystem.CanSave(out var why)) { Say(why, 2.5f); return; }
                if (has && !Confirmed(r.slot, "save")) { Say($"{name}을 덮어씁니다 — 한 번 더 누르면 저장", 4f); return; }
                if (SaveSystem.Save(r.slot)) Say($"{name}에 남겼다", 2.5f); else Say("저장하지 못했다", 2.5f);
                confirmSlot = -1; Refresh();
            }
            else
            {
                if (!has) { Say("비어 있는 기록이다", 2f); return; }
                if (confirmLoad && !Confirmed(r.slot, "load")) { Say("지금 진행은 사라집니다 — 한 번 더 누르면 불러오기", 4f); return; }
                SaveSystem.Load(r.slot);
            }
        }

        /// <summary>같은 슬롯·같은 행동을 두 번 연속 눌렀는가(첫 번째는 확인 문구만).</summary>
        bool Confirmed(int slot, string what)
        {
            if (confirmSlot == slot && confirmWhat == what) { confirmSlot = -1; return true; }
            confirmSlot = slot; confirmWhat = what; return false;
        }

        void Delete()
        {
            var r = rows[sel]; if (!SaveSystem.Exists(r.slot)) return;
            if (!Confirmed(r.slot, "delete")) { Say($"{SaveFormat.SlotName(r.slot)}을 지웁니다 — X 를 한 번 더 누르면 삭제", 4f); return; }
            SaveSystem.Delete(r.slot); Say($"{SaveFormat.SlotName(r.slot)}을 지웠다", 2f); Refresh();
        }

        public override void Tick()
        {
            if (messageT > 0) { messageT -= Time.unscaledDeltaTime; if (messageT <= 0) { message.text = ""; confirmSlot = -1; } }
            var nav = InputAdapter.NavPressed(); if (nav.y != 0) { sel = (sel - nav.y + rows.Length) % rows.Length; confirmSlot = -1; Say("", 0); Refresh(); }
            if (InputAdapter.ConfirmPressed()) Activate();
            else if (InputAdapter.DropPressed()) Delete();
        }
    }
}
