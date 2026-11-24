using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 캐릭터 시트(I 가방 · C 장비 — 같은 화면). 사용자 참고: 양피지 캐릭터 시트 → 밝은 종이 + 먹칠 + 양털처럼 뽀송한 테두리.
    ///   ┌ A 장비: 아치 틀 안에 지금 민미 3D(장비를 끼우면 바로 몸에 보임, 끌어서 돌리기) + 둘레 장비 칸 8개(머리·가슴·다리·발 | 무기·아티팩트 3) + 이름·레벨·전투력 · 장비 묶음 1·2·3
    ///   ├ B 능력치: 경험치·체력·스태미나·정신력 막대 + 공격/방어 표
    ///   ├ D 자세히: 고른 아이템(먹선 마법진 위 큰 아이콘 · 이름 · 등급 별 · 대표 수치 · 효과 · 설명) + 장착/사용/해제 버튼 · 도감(K) 버튼
    ///   ├ E 가방: 왼쪽 분류 아이콘 5개 + 10×3 칸
    ///   └ F 스킬: 가진 스킬 카드 가로 스크롤(일러스트 · 이름 띠 · 최종 위력) — 클릭하면 스킬 상세 창
    /// 그림: tools/sheet_art.py → Resources/UI/Sheet (칸 테두리·머리 먹줄은 판 그림에 들어 있으니 배치 숫자를 같이 바꿀 것).
    /// 조작: 칸 클릭 = 고르기 · 한 번 더(또는 Enter) = 장착/사용 · 장비 칸 클릭 = 그 칸 장비만 보기 · X 해제/버리기 · R 정렬 · 1~4 퀵슬롯 · [ ] 분류 · ←→↑↓ 이동
    ///       스킬 카드: 휠·끌기·화살표로 가로 스크롤, 클릭 = 상세(Esc 로 상세만 닫힘) · K 도감 · I/C/Esc 닫기
    /// </summary>
    public class CharacterSheet : MenuPage
    {
        // ── 배치(시트 왼쪽 위 기준) — tools/sheet_art.py 의 SEC 와 같게
        public const float SW = 1840, SH = 1020;
        const float Cell = 70, CellStep = 78, GridX = 124, GridY = 712, CardW = 196, CardH = 262, CardStep = 210, ViewX = 960, ViewY = 704, ViewW = 824, ViewH = 270;
        const int Cols = 10, Rows = 3, Cells = Cols * Rows;
        static readonly string[] TabArt = { "TabAll", "TabPotion", "TabGear", "TabMat", "TabKey" }, TabNames = { "전체", "소모품", "장비", "재료", "중요" };
        static readonly (EquipSlot slot, int index, float x, float y, string label)[] SlotDefs =
        {
            (EquipSlot.Head, 0, 60, 76, "머리"), (EquipSlot.Chest, 0, 60, 184, "가슴"), (EquipSlot.Legs, 0, 60, 292, "다리"), (EquipSlot.Feet, 0, 60, 400, "발"),
            (EquipSlot.Weapon, 0, 528, 76, "무기"), (EquipSlot.Artifact, 0, 528, 184, "아티팩트"), (EquipSlot.Artifact, 1, 528, 292, "아티팩트"), (EquipSlot.Artifact, 2, 528, 400, "아티팩트"),
        };
        static readonly Color Ink = new Color(0.2f, 0.17f, 0.15f), InkSoft = new Color(0.36f, 0.32f, 0.28f), Good = new Color(0.22f, 0.5f, 0.2f), Gold = new Color(0.72f, 0.5f, 0.12f), Paper = new Color(0.99f, 0.97f, 0.92f);

        Inventory inv; CharacterStats stats; Experience xp; Stamina stamina;
        RectTransform sheet, viewRt; RawImage view; EquipmentPreview preview; Image viewFlash; float equipPop;
        Text nameText, subText; Image[] presetBtn = new Image[Inventory.PresetCount]; Text[] presetTxt = new Text[Inventory.PresetCount];
        readonly Image[] slotFrame = new Image[8], slotIcon = new Image[8]; readonly Text[] slotEmpty = new Text[8], slotGrade = new Text[8];
        readonly Image[] barFill = new Image[4]; readonly Text[] barVal = new Text[4]; Text atkNames, atkVals, defNames, defVals;
        Image dIcon, dCircle; Text dName, dType, dStars, dKeyLabel, dKey, dFx, dDesc, dHint, dActionTxt; Image dAction;
        readonly Image[] tabImg = new Image[5]; readonly Image[] cellFrame = new Image[Cells], cellIcon = new Image[Cells]; readonly Text[] cellCount = new Text[Cells], cellTag = new Text[Cells];
        Text goldText, message; float messageT;
        RectTransform cardContent; readonly List<(GameObject go, Image frame)> cards = new(); List<(SkillDefinition skill, string key)> owned = new(); float scrollX, scrollTarget; Image arrowL, arrowR;
        // 스킬 상세 창
        GameObject popup; Image popArt; Text popName, popKind, popAttr, popPower, popPowerSub, popBody;

        int tab, cursor = -1, selSlot = -1, lastEquipHash; readonly int[] view_ = new int[Cells]; bool filterSlot;

        static Vector2 TL(float x, float y) => new Vector2(x, -y);
        RectTransform Place(RectTransform r, float x, float y, float w, float h) => Set(r, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(x, y), new Vector2(w, h));
        Text Label(string s, int size, Color c, float x, float y, float w, float h, TextAnchor a = TextAnchor.MiddleLeft, Transform parent = null)
        {
            var t = Txt(parent != null ? parent : sheet, s, size, c, a); t.font = UITheme.BodyFont; if (t.TryGetComponent<Shadow>(out var sh)) Object.Destroy(sh); Place(t.rectTransform, x, y, w, h); return t;
        }
        Text Heading(string s, float x, float y)
        {
            var t = StyleHeading(Txt(sheet, s, 30, Paper, TextAnchor.MiddleLeft)); Place(t.rectTransform, x + 22, y + 8, 240, 46);   // 머리 먹줄 위 밝은 글자
            if (t.TryGetComponent<Shadow>(out var sh)) { sh.effectColor = new Color(0, 0, 0, .35f); sh.effectDistance = new Vector2(1, -1); }
            return t;
        }

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            var dim = Box(canvas, "SheetRoot", new Color(0.05f, 0.05f, 0.06f, 0.62f)); Stretch(dim.rectTransform); dim.raycastTarget = true; root = dim.gameObject;
            var bg = Img(root.transform, "Sheet", UITheme.Art("Sheet", "Sheet"), Color.white, false, true); sheet = bg.rectTransform; At(sheet, new Vector2(.5f, .5f), Vector2.zero, new Vector2(SW, SH));
            if (bg.sprite == null) { bg.sprite = UITheme.Frame; bg.type = Image.Type.Sliced; }

            // ── A 장비 + 3D 모습
            var arch = Img(sheet, "Arch", UITheme.Art("Sheet", "Arch"), Color.white, false); Place(arch.rectTransform, 166, 64, 320, 440);
            viewFlash = Img(sheet, "EquipFlash", UITheme.SoftCircle, new Color(1f, .85f, .5f, 0f), false); Place(viewFlash.rectTransform, 146, 120, 360, 360);
            view = new GameObject("View", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); view.transform.SetParent(sheet, false); viewRt = view.rectTransform; Place(viewRt, 176, 84, 300, 410); view.raycastTarget = true;
            OnDrag(view.gameObject, e => { if (preview != null) preview.yaw += e.delta.x * 0.4f; });
            Label("끌어서 돌려 보기", 17, InkSoft, 176, 470, 300, 24, TextAnchor.MiddleCenter);
            for (int i = 0; i < SlotDefs.Length; i++)
            {
                var (slot, index, x, y, label) = SlotDefs[i]; int k = i;
                var f = Img(sheet, "Slot" + i, UITheme.Art("Sheet", "Slot", new Vector4(14, 14, 14, 14)), Color.white, true, true); Place(f.rectTransform, x, y, 88, 88);
                slotIcon[i] = Img(f.transform, "Icon", null, Color.white, false); Set(slotIcon[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 4), new Vector2(62, 62));
                slotEmpty[i] = Label(slot == EquipSlot.Artifact ? "◇" : "+", 30, new Color(InkSoft.r, InkSoft.g, InkSoft.b, .55f), 0, 0, 88, 88, TextAnchor.MiddleCenter, f.transform);
                var lab = Label(label, 14, InkSoft, 0, 66, 88, 20, TextAnchor.MiddleCenter, f.transform);
                slotGrade[i] = Label("", 20, Color.white, 6, 2, 30, 26, TextAnchor.MiddleLeft, f.transform);
                slotFrame[i] = f;
                OnClick(f.gameObject, () => SelectSlot(k));
            }
            nameText = StyleDisplay(Txt(sheet, "", 46, Color.white, TextAnchor.MiddleCenter)); Place(nameText.rectTransform, 166, 504, 320, 58);
            nameText.GetComponent<UIGradient>().bottom = new Color(0.55f, 0.42f, 0.28f); nameText.GetComponent<UIGradient>().top = new Color(0.25f, 0.2f, 0.17f);   // 밝은 종이 위: 먹 → 갈색
            if (nameText.TryGetComponent<Shadow>(out var nsh)) nsh.effectColor = new Color(1, 1, 1, .5f);
            subText = Label("", 21, Ink, 46, 566, 556, 32, TextAnchor.MiddleCenter);
            Label("묶음", 15, InkSoft, 520, 508, 96, 18, TextAnchor.MiddleCenter);
            for (int i = 0; i < Inventory.PresetCount; i++)
            {
                int idx = i; var b = Img(sheet, "Preset" + i, UITheme.Art("Sheet", "Button", new Vector4(12, 12, 12, 12)), Color.white, true, true); Place(b.rectTransform, 520 + i * 33, 528, 30, 30);
                presetBtn[i] = b; presetTxt[i] = Label((i + 1).ToString(), 18, Ink, 0, 0, 30, 30, TextAnchor.MiddleCenter, b.transform);
                OnClick(b.gameObject, () => { if (inv != null) inv.SwitchPreset(idx); Refresh(); });
            }

            // ── B 능력치
            Heading("능력치", 640, 40);
            string[] bars = { "경험치", "체력", "스태미나", "정신력" }; Color[] cols = { new Color(.86f, .66f, .26f), new Color(.8f, .27f, .24f), new Color(.42f, .66f, .32f), new Color(.34f, .5f, .82f) };
            for (int i = 0; i < 4; i++)
            {
                float y = 110 + i * 58; Label(bars[i], 22, Ink, 664, y, 200, 26); barVal[i] = Label("", 20, InkSoft, 900, y, 260, 26, TextAnchor.MiddleRight);
                var fr = Img(sheet, "Bar" + i, UITheme.Art("Sheet", "BarFrame", new Vector4(12, 0, 12, 0)), Color.white, true); Place(fr.rectTransform, 660, y + 28, 500, 20);
                var fill = Box(fr.transform, "Fill", cols[i]); Stretch(fill.rectTransform, 4); fill.rectTransform.anchorMax = new Vector2(0.5f, 1); barFill[i] = fill;
            }
            Label("공격", 24, Ink, 664, 350, 240, 30); Label("방어", 24, Ink, 924, 350, 240, 30);
            atkNames = Label("", 19, InkSoft, 664, 388, 150, 230, TextAnchor.UpperLeft); atkVals = Label("", 19, Ink, 780, 388, 124, 230, TextAnchor.UpperRight);
            defNames = Label("", 19, InkSoft, 924, 388, 150, 230, TextAnchor.UpperLeft); defVals = Label("", 19, Ink, 1040, 388, 124, 230, TextAnchor.UpperRight);
            foreach (var t in new[] { atkNames, atkVals, defNames, defVals }) t.lineSpacing = 1.35f;

            // ── D 자세히
            Heading("자세히", 1204, 40);
            var codexBtn = Img(sheet, "CodexBtn", UITheme.Art("Sheet", "Button", new Vector4(12, 12, 12, 12)), Color.white, true, true); Place(codexBtn.rectTransform, 1640, 56, 146, 40);
            Label("도감  (K)", 20, Ink, 0, 0, 146, 40, TextAnchor.MiddleCenter, codexBtn.transform); OnClick(codexBtn.gameObject, () => { if (ui != null) ui.OpenCodex(); });
            dCircle = Img(sheet, "Circle", UITheme.Art("Sheet", "Circle"), Color.white, false); Place(dCircle.rectTransform, 1226, 110, 250, 250);
            dIcon = Img(sheet, "BigIcon", null, Color.white, false); dIcon.preserveAspect = true; Place(dIcon.rectTransform, 1271, 155, 160, 160);
            dName = StyleHeading(Txt(sheet, "", 34, Ink, TextAnchor.MiddleLeft)); if (dName.TryGetComponent<Shadow>(out var dsh)) Object.Destroy(dsh); Place(dName.rectTransform, 1490, 110, 300, 50);
            dType = Label("", 20, InkSoft, 1490, 160, 300, 30); dStars = Label("", 24, Gold, 1490, 190, 300, 32);
            dKeyLabel = Label("", 19, InkSoft, 1490, 236, 300, 26); dKey = Label("", 54, Ink, 1490, 258, 300, 70); dKey.font = UITheme.DisplayFont;
            dFx = Label("", 17, Good, 1230, 366, 550, 100, TextAnchor.UpperLeft); dDesc = Label("", 19, InkSoft, 1230, 470, 550, 86, TextAnchor.UpperLeft); dDesc.fontStyle = FontStyle.Italic;
            dAction = Img(sheet, "Action", UITheme.Art("Sheet", "Button", new Vector4(12, 12, 12, 12)), Color.white, true, true); Place(dAction.rectTransform, 1630, 562, 156, 44);
            dActionTxt = Label("", 21, Ink, 0, 0, 156, 44, TextAnchor.MiddleCenter, dAction.transform); OnClick(dAction.gameObject, DoAction);
            dHint = Label("", 15, InkSoft, 1230, 558, 390, 50, TextAnchor.MiddleLeft);

            // ── E 가방
            Heading("가방", 36, 644); goldText = Label("", 22, Gold, 600, 656, 300, 32, TextAnchor.MiddleRight);
            for (int i = 0; i < 5; i++)
            {
                int k = i; var t = Img(sheet, "Tab" + i, UITheme.Art("Sheet", TabArt[i]), Color.white, false, true); Place(t.rectTransform, 56, 712 + i * 54, 48, 48); tabImg[i] = t;
                OnClick(t.gameObject, () => { tab = k; filterSlot = false; cursor = -1; Refresh(); }); OnHover(t.gameObject, () => Say(TabNames[k]));
            }
            for (int i = 0; i < Cells; i++)
            {
                int k = i; float x = GridX + (i % Cols) * CellStep, y = GridY + (i / Cols) * CellStep;
                var f = Img(sheet, "Cell" + i, UITheme.Art("Sheet", "Slot", new Vector4(14, 14, 14, 14)), Color.white, true, true); Place(f.rectTransform, x, y, Cell, Cell);
                cellIcon[i] = Img(f.transform, "Icon", null, Color.white, false); Set(cellIcon[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(52, 52));
                cellCount[i] = Label("", 16, Ink, 0, 46, 64, 22, TextAnchor.MiddleRight, f.transform); cellTag[i] = Label("", 13, Gold, 5, 3, 60, 18, TextAnchor.MiddleLeft, f.transform);
                cellFrame[i] = f;
                OnHover(f.gameObject, () => { if (cursor != k) { cursor = k; Refresh(); } });
                OnClick(f.gameObject, () => { if (cursor == k) DoAction(); else { cursor = k; Refresh(); } });
            }
            message = Label("", 18, Gold, 124, 950, 760, 28);

            // ── F 스킬(가로 스크롤)
            Heading("스킬", 940, 644); Label("휠 · 끌기로 넘기고, 누르면 자세히", 16, InkSoft, 1230, 656, 556, 30, TextAnchor.MiddleRight);
            var vp = Box(sheet, "SkillView", new Color(1, 1, 1, 0)); vp.raycastTarget = true; Place(vp.rectTransform, ViewX, ViewY, ViewW, ViewH); vp.gameObject.AddComponent<RectMask2D>();
            cardContent = NewRect("Cards", vp.transform); Set(cardContent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(ViewW, ViewH));
            OnScroll(vp.gameObject, e => scrollTarget -= e.scrollDelta.y * CardStep * 0.5f); OnDrag(vp.gameObject, e => { scrollTarget -= e.delta.x; scrollX = scrollTarget; });
            var arrow = UITheme.Art("Sheet", "Arrow");
            arrowR = Img(sheet, "ArrowR", arrow, Color.white, false, true); Place(arrowR.rectTransform, 1786, 812, 20, 30); OnClick(arrowR.gameObject, () => scrollTarget += CardStep);
            arrowL = Img(sheet, "ArrowL", arrow, Color.white, false, true); Place(arrowL.rectTransform, 938, 812, 20, 30); arrowL.rectTransform.localScale = new Vector3(-1, 1, 1); arrowL.rectTransform.pivot = new Vector2(.5f, 1); arrowL.rectTransform.anchoredPosition = TL(948, 812); OnClick(arrowL.gameObject, () => scrollTarget -= CardStep);

            BuildPopup();
            root.SetActive(false);
        }

        // ── 스킬 카드(가진 스킬이 바뀔 수 있어 열 때마다 다시)
        void BuildCards()
        {
            foreach (var c in cards) Object.Destroy(c.go); cards.Clear();
            owned = SkillInfo.Owned(stats);
            for (int i = 0; i < owned.Count; i++)
            {
                var (s, key) = owned[i]; int k = i; var ac = SkillAttributes.ColorOf(s.attribute);
                var f = Img(cardContent, "Card" + i, UITheme.Art("Sheet", "Card", new Vector4(16, 16, 16, 16)), Color.white, true, true); Set(f.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(i * CardStep, -4), new Vector2(CardW, CardH));
                var T = f.transform;
                var dot = Box(T, "Attr", ac); Set(dot.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -12), new Vector2(14, 14)); dot.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                Label(key, 15, InkSoft, 10, 6, 140, 24, TextAnchor.MiddleLeft, T);
                var artBg = Box(T, "ArtBg", new Color(.08f, .07f, .06f, 1f)); Place(artBg.rectTransform, 8, 32, 180, 96);
                var art = Img(artBg.transform, "Art", SkillIllustration.Get(s), Color.white, false); Stretch(art.rectTransform, 2);
                var ribbon = Img(T, "NameInk", SkillPanelArt.BannerInk, new Color(.12f, .1f, .09f, .95f), true); Place(ribbon.rectTransform, 4, 132, 188, 40);
                var nm = StyleSkill(Txt(ribbon.transform, s.skillName, 23, Color.white, TextAnchor.MiddleCenter), ac); Stretch(nm.rectTransform);
                Label(SkillInfo.Kind(key), 14, InkSoft, 0, 176, CardW, 20, TextAnchor.MiddleCenter, T);
                var band = Img(T, "PowerInk", SkillPanelArt.Fill, Color.white, true); Place(band.rectTransform, 10, 198, 176, 56);
                var glow = Img(band.transform, "Glow", SkillPanelArt.Glow, new Color(ac.r, ac.g, ac.b, .5f), false); Stretch(glow.rectTransform, -6);
                var lab = Txt(band.transform, "최종 위력", 14, new Color(.85f, .8f, .7f), TextAnchor.MiddleLeft); Place(lab.rectTransform, 10, 0, 90, 56);
                var (mn, mx) = SkillInfo.Power(s, stats);
                var pw = StyleDisplay(Txt(band.transform, mx.ToString(), 44, Color.white, TextAnchor.MiddleRight), true); Place(pw.rectTransform, 70, -2, 98, 60);
                pw.GetComponent<TextGlow>().color = new Color(ac.r, ac.g, ac.b, .6f);
                var sub = Txt(band.transform, mn != mx ? $"{mn}~" : "", 14, new Color(.85f, .8f, .7f), TextAnchor.LowerLeft); Place(sub.rectTransform, 10, 30, 70, 24);
                OnClick(f.gameObject, () => OpenPopup(k)); OnScroll(f.gameObject, e => scrollTarget -= e.scrollDelta.y * CardStep * 0.5f);
                cards.Add((f.gameObject, f));
            }
            scrollX = scrollTarget = 0;
        }

        void BuildPopup()
        {
            var dim = Box(root.transform, "SkillPopup", new Color(0.05f, 0.05f, 0.06f, 0.55f)); Stretch(dim.rectTransform); dim.raycastTarget = true; popup = dim.gameObject;
            OnClick(popup, ClosePopup);
            var p = Img(popup.transform, "Paper", UITheme.Art("Sheet", "Popup"), Color.white, false, true); At(p.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1000, 620)); var P = p.transform;
            if (p.sprite == null) { p.sprite = UITheme.Frame; p.type = Image.Type.Sliced; }
            var artBg = Box(P, "ArtBg", new Color(.08f, .07f, .06f, 1f)); Set(artBg.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(52, 52), new Vector2(548, 288));
            popArt = Img(artBg.transform, "Art", null, Color.white, false); Stretch(popArt.rectTransform, 3);
            var rib = Img(P, "NameInk", SkillPanelArt.BannerInk, new Color(.12f, .1f, .09f, .95f), true); Set(rib.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(630, 52), new Vector2(330, 58));
            popName = StyleSkill(Txt(rib.transform, "", 32, Color.white, TextAnchor.MiddleCenter), Color.white); Stretch(popName.rectTransform);
            popKind = Txt(P, "", 20, InkSoft, TextAnchor.MiddleLeft); Set(popKind.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(636, 118), new Vector2(320, 28));
            popAttr = Txt(P, "", 20, Ink, TextAnchor.MiddleLeft); Set(popAttr.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(636, 148), new Vector2(330, 28));
            var band = Img(P, "PowerInk", SkillPanelArt.Fill, Color.white, true); Set(band.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(630, 192), new Vector2(330, 148));
            var bl = Txt(band.transform, "최종 위력", 20, new Color(.9f, .85f, .74f), TextAnchor.UpperLeft); Set(bl.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(16, 12), new Vector2(200, 28));
            popPower = StyleDisplay(Txt(band.transform, "", 74, Color.white, TextAnchor.MiddleRight), true); Set(popPower.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(100, 30), new Vector2(214, 90));
            popPowerSub = Txt(band.transform, "", 18, new Color(.9f, .85f, .74f), TextAnchor.LowerLeft); Set(popPowerSub.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(16, 104), new Vector2(300, 34));
            popBody = Txt(P, "", 21, Ink, TextAnchor.UpperLeft); popBody.font = UITheme.BodyFont; popBody.lineSpacing = 1.3f; Set(popBody.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(56, 366), new Vector2(900, 220));
            foreach (var t in new[] { popKind, popAttr, popBody }) if (t.TryGetComponent<Shadow>(out var sh)) Object.Destroy(sh);
            var close = Txt(P, "클릭 · Esc  닫기", 16, InkSoft, TextAnchor.LowerRight); Set(close.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 26), new Vector2(300, 26));
            popup.SetActive(false);
        }

        void OpenPopup(int i)
        {
            if (i < 0 || i >= owned.Count) return; var (s, key) = owned[i]; var (mn, mx) = SkillInfo.Power(s, stats);
            popArt.sprite = SkillIllustration.Get(s); popName.text = s.skillName; popName.GetComponent<TextGlow>().color = SkillAttributes.ColorOf(s.attribute) * new Color(1, 1, 1, .6f);
            popKind.text = $"{SkillInfo.Kind(key)}  ·  {key}"; popAttr.text = SkillInfo.AttrLine(s);
            popPower.text = mx.ToString(); popPower.GetComponent<TextGlow>().color = SkillAttributes.ColorOf(s.attribute) * new Color(1, 1, 1, .6f);
            popPowerSub.text = $"뒷면 {mn}  ·  앞면 {mx}" + (stats != null && stats.equipFinalPower != 0 ? $"  (장비 +{stats.equipFinalPower})" : "");
            popBody.text = string.Join("\n", SkillInfo.Details(s, stats)) + (string.IsNullOrEmpty(s.description) ? "" : "\n<i>" + s.description + "</i>");
            popup.SetActive(true); popup.transform.SetAsLastSibling();
        }
        void ClosePopup() { if (popup != null) popup.SetActive(false); }
        public override bool HandleCancel() { if (popup != null && popup.activeSelf) { ClosePopup(); return true; } return false; }

        // ── 열기·닫기
        /// <summary>I = 가방부터, C = 장비 칸(머리)부터.</summary>
        public void Focus(bool equipment) { if (equipment) { selSlot = 0; filterSlot = true; tab = 2; } else { selSlot = -1; filterSlot = false; } cursor = -1; }

        public override void Open()
        {
            stats = ui != null ? ui.player : null; inv = stats != null ? stats.GetComponent<Inventory>() : null; xp = stats != null ? stats.GetComponent<Experience>() : null; stamina = stats != null ? stats.GetComponent<Stamina>() : null;
            if (inv != null) { inv.Changed -= Refresh; inv.Changed += Refresh; inv.Message -= Say; inv.Message += Say; lastEquipHash = EquipHash(); }
            base.Open(); ClosePopup(); BuildCards();
            if (stats != null) { if (preview == null) preview = new EquipmentPreview(); preview.Begin(stats.transform, 600, 820); view.texture = preview.Texture; }
            Refresh();
        }
        public override void Close() { if (inv != null) { inv.Changed -= Refresh; inv.Message -= Say; inv.Persist(); } if (preview != null) preview.End(); ClosePopup(); base.Close(); }
        void Say(string m) { if (message != null) { message.text = m; messageT = 2.5f; } }

        int EquipHash() { if (inv == null) return 0; int h = (inv.equippedWeapon ?? "").GetHashCode(); foreach (var id in inv.EquippedIds()) h = h * 31 + (id ?? "").GetHashCode(); return h; }

        void SelectSlot(int i)
        {
            if (selSlot == i && filterSlot) { filterSlot = false; selSlot = -1; tab = 0; }   // 한 번 더 누르면 가방 전체로
            else { selSlot = i; filterSlot = true; tab = 2; }
            cursor = -1; Refresh();
        }

        bool Match(ItemDef d)
        {
            if (filterSlot && selSlot >= 0) return d.Equippable && d.Slot == SlotDefs[selSlot].slot;
            return tab switch { 0 => true, 1 => d.type == ItemType.Consumable, 2 => d.Equippable, 3 => d.type == ItemType.Material, _ => d.type == ItemType.Key };
        }
        int CurSlot() => cursor >= 0 && cursor < Cells ? view_[cursor] : -1;

        /// <summary>고른 칸: 장착·해제·사용. 장비 칸을 골라 둔 상태면 그 칸(아티팩트 번호)에 낌.</summary>
        void DoAction()
        {
            if (inv == null) return; int si = CurSlot();
            if (si >= 0)
            {
                var s = inv.slots[si]; var d = s.Def; if (d == null) return;
                if (d.Equippable && filterSlot && selSlot >= 0)
                {
                    var sd = SlotDefs[selSlot];
                    if (inv.Equipped(sd.slot, sd.index) == s.id) { if (sd.slot != EquipSlot.Weapon) inv.Unequip(sd.slot, sd.index); }
                    else if (d.type == ItemType.Weapon) inv.Equip(s.id); else inv.EquipTo(s.id, sd.slot, sd.slot == EquipSlot.Artifact ? sd.index : -1);
                }
                else inv.UseSlot(si);
            }
            else if (selSlot >= 0) { var sd = SlotDefs[selSlot]; if (sd.slot != EquipSlot.Weapon) inv.Unequip(sd.slot, sd.index); else Say("무기는 다른 무기로만 바꿀 수 있다"); }
            Refresh();
        }

        void Refresh()
        {
            if (inv == null || root == null || !root.activeInHierarchy) return;
            int eh = EquipHash(); if (eh != lastEquipHash) { lastEquipHash = eh; equipPop = 0.45f; }   // 장비가 바뀜 → 3D 모습 톡 + 빛
            // A
            for (int i = 0; i < SlotDefs.Length; i++)
            {
                var d = ItemDatabase.Get(inv.Equipped(SlotDefs[i].slot, SlotDefs[i].index)); bool on = i == selSlot;
                slotIcon[i].enabled = d != null; slotIcon[i].sprite = d != null ? ItemIcons.Get(d) : null; slotEmpty[i].enabled = d == null;
                slotGrade[i].text = d != null ? d.grade : ""; slotGrade[i].color = d != null ? ItemDef.GradeColor(d.grade) * new Color(.75f, .75f, .75f, 1) : Color.white;
                slotFrame[i].sprite = UITheme.Art("Sheet", on ? "SlotSel" : "Slot", new Vector4(14, 14, 14, 14));
            }
            int lv = xp != null ? xp.level : 1;
            nameText.text = stats.displayName;
            subText.text = $"Lv.{lv}  ·  {LevelMath.Title(lv)}  ·  전투력 <color=#3B7A2E>{EquipmentMath.CombatPower(stats.Attack, stats.equipFinalPower, stats.maxHealth, stats.equipDamageReduction, lv):N0}</color>";
            for (int i = 0; i < Inventory.PresetCount; i++) { bool on = i == inv.preset; presetBtn[i].sprite = UITheme.Art("Sheet", on ? "SlotSel" : "Button", new Vector4(12, 12, 12, 12)); presetTxt[i].color = on ? Gold : Ink; }
            // B
            UpdateBars();
            var w = ItemDatabase.Get(inv.equippedWeapon); float wm = w != null ? w.damageMultiplier : 1f; float guard = 0.5f;
            if (stats.TryGetComponent<DefenseController>(out var dc)) guard = dc.guardReduction;
            atkNames.text = "공격력\n코인 최종 위력\n무기 배율\n레벨 보너스\n장비 공격력\n이동 속도";
            atkVals.text = $"{stats.Attack:0.#}\n{(stats.equipFinalPower >= 0 ? "+" : "")}{stats.equipFinalPower}\n×{wm:0.00}\n+{stats.levelAttack:0.#}\n+{stats.equipAttack:0.#}\n{100 + stats.equipMoveSpeed * 100:0}%";
            defNames.text = "최대 체력\n받는 피해\n가드 감소\n회피 코인\n경직 한계\n몸 속성";
            defVals.text = $"{stats.maxHealth:0}\n-{stats.equipDamageReduction * 100:0.#}%\n{Mathf.Clamp(guard + stats.equipGuard, 0, .9f) * 100:0}%\n+{stats.equipEvadePower}\n{stats.maxStagger:0}\n{SkillAttributes.Names[(int)stats.attribute]}";
            // E
            int n = 0; for (int i = 0; i < Cells; i++) view_[i] = -1;
            for (int i = 0; i < Inventory.Capacity && n < Cells; i++) { var s = inv.slots[i]; if (!s.Empty && s.Def != null && Match(s.Def)) view_[n++] = i; }
            for (int i = 0; i < Cells; i++)
            {
                int si = view_[i]; bool has = si >= 0; var s = has ? inv.slots[si] : null; var d = has ? s.Def : null;
                cellIcon[i].enabled = has; cellIcon[i].sprite = has ? ItemIcons.Get(d) : null; cellCount[i].text = has && s.count > 1 ? s.count.ToString() : "";
                int q = has ? System.Array.IndexOf(inv.quick, s.id) : -1;
                cellTag[i].text = has && d.Equippable && inv.IsEquipped(s.id) ? "장착" : q >= 0 ? "Q" + (q + 1) : "";
                cellFrame[i].sprite = UITheme.Art("Sheet", i == cursor ? "SlotSel" : "Slot", new Vector4(14, 14, 14, 14)); cellFrame[i].color = new Color(1, 1, 1, has || i == cursor ? 1f : .6f);
            }
            for (int i = 0; i < 5; i++) tabImg[i].color = (!filterSlot && i == tab) || (filterSlot && i == 2) ? Color.white : new Color(1, 1, 1, .5f);
            goldText.text = "금화  " + inv.gold.ToString("N0");
            // D: 가방에서 고른 것 → 아니면 고른 장비 칸에 낀 것
            int cs = CurSlot(); var cur = cs >= 0 ? inv.slots[cs].Def : selSlot >= 0 ? ItemDatabase.Get(inv.Equipped(SlotDefs[selSlot].slot, SlotDefs[selSlot].index)) : null;
            ShowDetail(cur, cs >= 0 ? inv.Count(cur.id) : 0);
            if (filterSlot && selSlot >= 0) Say($"{SlotDefs[selSlot].label} 칸 — 낄 수 있는 장비 {n}개");
        }

        void ShowDetail(ItemDef d, int count)
        {
            dIcon.enabled = d != null; dIcon.sprite = d != null ? ItemIcons.Get(d) : null; dCircle.color = new Color(1, 1, 1, d != null ? 1f : .5f);
            if (d == null)
            {
                dName.text = selSlot >= 0 ? SlotDefs[selSlot].label + " 칸 · 비어 있음" : "—"; dType.text = selSlot >= 0 ? "아래 가방에서 낄 장비를 고르세요" : "칸을 고르면 자세히 보입니다";
                dStars.text = dKeyLabel.text = dKey.text = dFx.text = dDesc.text = ""; dAction.gameObject.SetActive(false); dHint.text = HintText(); return;
            }
            dName.text = d.name; dType.text = d.TypeLabel + (count > 1 ? $"  ·  {count}개" : "");
            dStars.text = Stars(d.grade);
            if (d.type == ItemType.Weapon) { dKeyLabel.text = "공격력 배율"; dKey.text = "×" + d.damageMultiplier.ToString("0.00"); }
            else if (d.heal > 0) { dKeyLabel.text = "체력 회복"; dKey.text = "+" + d.heal; }
            else if (d.stamina > 0) { dKeyLabel.text = "스태미나 회복"; dKey.text = "+" + d.stamina; }
            else if (d.Equippable) { var f = EquipmentMath.Describe(d.stats); dKeyLabel.text = "대표 효과"; dKey.text = f.Count > 0 ? f[0].Split(' ')[f[0].Split(' ').Length - 1] : "—"; }
            else { dKeyLabel.text = "가치"; dKey.text = d.value + " G"; }
            var fx = EquipmentMath.Describe(d.stats);
            if (d.type == ItemType.Weapon) { var kit = WeaponKits.Get(d.kit); fx.Insert(0, $"방어 코인 {kit.guardBase}+{kit.guardCoin}"); if (!string.IsNullOrEmpty(kit.passiveName)) fx.Add($"<color=#8A5A1E>{kit.passiveName}</color> — {kit.passiveText}"); }
            dFx.text = string.Join("\n", fx);
            dDesc.text = string.IsNullOrEmpty(d.description) ? "" : "“" + d.description + "”";
            bool equipped = d.Equippable && inv.IsEquipped(d.id);
            if (CurSlot() >= 0)
            {
                dActionTxt.text = d.Equippable ? (equipped ? (d.type == ItemType.Weapon ? "장착 중" : "해제") : "장착") : d.type == ItemType.Consumable ? "사용" : "";
                dAction.gameObject.SetActive(dActionTxt.text.Length > 0 && dActionTxt.text != "장착 중");
            }
            else { dActionTxt.text = "해제"; dAction.gameObject.SetActive(selSlot >= 0 && SlotDefs[selSlot].slot != EquipSlot.Weapon); }   // 장비 칸에 낀 것
            dHint.text = HintText();
        }
        static string Stars(string g) { int n = g switch { "S" => 5, "A" => 4, "B" => 3, "C" => 2, _ => 0 }; return n == 0 ? "" : new string('★', n) + "<color=#C9BBA0>" + new string('☆', 5 - n) + "</color>"; }
        static string HintText() => "Enter 장착·사용   X 해제·버리기\n1~4 퀵슬롯   R 정렬   [ ] 분류";

        void UpdateBars()
        {
            float[] v = { xp != null ? xp.Ratio : 0, stats.HealthRatio, stamina != null ? stamina.Current / Mathf.Max(1, stamina.max) : 0, Mathf.InverseLerp(stats.minSanity, stats.maxSanity, stats.currentSanity) };
            string[] t = { xp != null ? $"{xp.exp:N0} / {xp.ToNext:N0}" : "", $"{stats.currentHealth:0} / {stats.maxHealth:0}", stamina != null ? $"{stamina.Current:0} / {stamina.max:0}" : "", $"{stats.currentSanity} ({stats.minSanity}~{stats.maxSanity})" };
            for (int i = 0; i < 4; i++) { barFill[i].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v[i]), 1); barVal[i].text = t[i]; }
        }

        public override void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            if (messageT > 0) { messageT -= dt; if (messageT <= 0) message.text = ""; }
            if (preview != null) preview.Update();
            // 장비가 바뀐 순간: 3D 모습이 톡 커졌다 돌아오고 뒤에 금빛
            if (equipPop > 0) equipPop = Mathf.Max(0, equipPop - dt); float k = equipPop / 0.45f;
            viewRt.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(k * Mathf.PI)); viewFlash.color = new Color(1f, .85f, .5f, .7f * k);
            // 스킬 가로 스크롤
            float max = Mathf.Max(0, owned.Count * CardStep - 14 - ViewW); scrollTarget = Mathf.Clamp(scrollTarget, 0, max);
            scrollX = Mathf.Lerp(scrollX, scrollTarget, 1f - Mathf.Exp(-14f * dt)); cardContent.anchoredPosition = new Vector2(-scrollX, 0);
            arrowL.color = new Color(1, 1, 1, scrollTarget > 1 ? 1 : .25f); arrowR.color = new Color(1, 1, 1, scrollTarget < max - 1 ? 1 : .25f);
            if (popup.activeSelf) { if (InputAdapter.ConfirmPressed()) ClosePopup(); return; }
            if (InputAdapter.CodexPressed() && ui != null) { ui.OpenCodex(); return; }
            // 가방 키보드
            var nv = InputAdapter.NavPressed();
            if (nv.x != 0 || nv.y != 0) { int c = cursor < 0 ? 0 : cursor; int cx = Mathf.Clamp(c % Cols + nv.x, 0, Cols - 1), cy = Mathf.Clamp(c / Cols - nv.y, 0, Rows - 1); cursor = cy * Cols + cx; Refresh(); }
            if (InputAdapter.TabNext()) { filterSlot = false; tab = (tab + 1) % 5; cursor = -1; Refresh(); }
            if (InputAdapter.TabPrev()) { filterSlot = false; tab = (tab + 4) % 5; cursor = -1; Refresh(); }
            if (InputAdapter.ConfirmPressed()) DoAction();
            if (InputAdapter.DropPressed())
            {
                int si = CurSlot();
                if (si >= 0 && inv.slots[si].Def != null && inv.slots[si].Def.Equippable && inv.IsEquipped(inv.slots[si].id)) inv.UnequipItem(inv.slots[si].id);
                else if (si >= 0) inv.Drop(si);
                else if (selSlot >= 0) inv.Unequip(SlotDefs[selSlot].slot, SlotDefs[selSlot].index);
                Refresh();
            }
            if (InputAdapter.SortPressed()) { inv.Sort(); Refresh(); }
            int q = InputAdapter.QuickPressed(); if (q >= 0 && CurSlot() >= 0) { inv.AssignQuick(q, inv.slots[CurSlot()].id); Refresh(); }
            if (Time.frameCount % 10 == 0) UpdateBars();
        }
    }
}
