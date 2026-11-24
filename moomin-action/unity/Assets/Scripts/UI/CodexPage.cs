using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 도감(K · 캐릭터 시트의 "도감" 버튼). 사용자 참고: 탭 + 분류 아이콘 + 세로로 긴 카드 격자 | 오른쪽 큰 그림 + 이름 · 종류 · 대표 수치 · 별 · 설명 인용.
    /// 가진 장비와 스킬만 보임. 밝은 종이 + 먹칠 + 양털 테두리(tools/sheet_art.py → Resources/UI/Codex).
    /// 조작: [ ] 또는 탭 클릭 = 장비 ↔ 스킬 · 분류 칩 클릭 · ←→↑↓ 고르기 · 휠 스크롤 · Esc/K 뒤로.
    /// </summary>
    public class CodexPage : MenuPage
    {
        const float CW = 1700, CH = 940, GX = 64, GY = 224, CardW = 156, CardH = 208, StepX = 172, StepY = 222, VW = 736, VH = 640;
        const int Cols = 4, MaxCards = 40;
        static readonly string[][] Chips = { new[] { "전체", "무기", "머리", "가슴", "다리", "발", "아티팩트" }, new[] { "전체", "일반 공격", "스킬", "필살기" } };
        static readonly Color Ink = new Color(0.2f, 0.17f, 0.15f), InkSoft = new Color(0.38f, 0.34f, 0.3f), Gold = new Color(0.72f, 0.5f, 0.12f), Good = new Color(0.22f, 0.5f, 0.2f);

        class Entry { public ItemDef item; public SkillDefinition skill; public string key; }
        readonly List<Entry> entries = new();
        RectTransform board, grid; Text[] tabText = new Text[2]; Image[] tabLine = new Image[2]; Text countText;
        readonly List<(Image bg, Text label)> chips = new();
        readonly List<(GameObject go, Image frame, Image glow, Image icon, Image art, Text name, Text tag)> cards = new();
        Image bigIcon, bigArt, circle; Text name, type, keyLabel, keyValue, stars, lines, desc;
        int tab, chip, cursor; float scrollY, scrollTarget;
        CharacterStats stats; Inventory inv;

        static Vector2 TL(float x, float y) => new Vector2(x, -y);
        static RectTransform Place(RectTransform r, float x, float y, float w, float h) => Set(r, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(x, y), new Vector2(w, h));
        Text Label(Transform p, string s, int size, Color c, float x, float y, float w, float h, TextAnchor a = TextAnchor.MiddleLeft)
        {
            var t = Txt(p, s, size, c, a); t.font = UITheme.BodyFont; if (t.TryGetComponent<Shadow>(out var sh)) Object.Destroy(sh); Place(t.rectTransform, x, y, w, h); return t;
        }

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            var dim = Box(canvas, "CodexRoot", new Color(0.05f, 0.05f, 0.06f, 0.62f)); Stretch(dim.rectTransform); dim.raycastTarget = true; root = dim.gameObject;
            var bg = Img(root.transform, "Board", UITheme.Art("Codex", "Codex"), Color.white, false, true); board = bg.rectTransform; At(board, new Vector2(.5f, .5f), Vector2.zero, new Vector2(CW, CH));
            if (bg.sprite == null) { bg.sprite = UITheme.Frame; bg.type = Image.Type.Sliced; }
            var B = board.transform;
            var title = StyleHeading(Txt(B, "도감", 56, Ink, TextAnchor.MiddleLeft)); if (title.TryGetComponent<Shadow>(out var tsh)) Object.Destroy(tsh); Place(title.rectTransform, 70, 44, 300, 76);
            var sub = Label(B, "Codex · 가진 장비와 스킬", 20, InkSoft, 214, 70, 340, 40);
            var kl = Label(B, "[", 26, InkSoft, 560, 62, 40, 44, TextAnchor.MiddleCenter); var kr = Label(B, "]", 26, InkSoft, 900, 62, 40, 44, TextAnchor.MiddleCenter);
            foreach (var k in new[] { kl, kr }) { var box = Img(k.transform, "Key", UITheme.Art("Sheet", "Button", new Vector4(12, 12, 12, 12)), Color.white, true); Stretch(box.rectTransform); box.transform.SetAsFirstSibling(); }
            string[] tn = { "장비", "스킬" };
            for (int i = 0; i < 2; i++)
            {
                int k = i; var hit = Box(B, "Tab" + i, new Color(0, 0, 0, 0)); hit.raycastTarget = true; Place(hit.rectTransform, 610 + i * 140, 54, 130, 60);
                tabText[i] = StyleHeading(Txt(hit.transform, tn[i], 32, Ink, TextAnchor.MiddleCenter)); Stretch(tabText[i].rectTransform); if (tabText[i].TryGetComponent<Shadow>(out var s2)) Object.Destroy(s2);
                tabLine[i] = Box(hit.transform, "Line", Ink); Set(tabLine[i].rectTransform, new Vector2(.15f, 0), new Vector2(.85f, 0), new Vector2(.5f, 0), Vector2.zero, new Vector2(0, 3));
                OnClick(hit.gameObject, () => { SetTab(k); });
            }
            countText = Label(B, "", 20, InkSoft, 1290, 70, 360, 40, TextAnchor.MiddleRight);
            for (int i = 0; i < 7; i++)
            {
                int k = i; var c = Img(B, "Chip" + i, UITheme.Art("Codex", "Chip", new Vector4(14, 14, 14, 14)), Color.white, true, true); Place(c.rectTransform, 60 + i * 102, 166, 96, 38);
                var t = Label(c.transform, "", 17, Ink, 0, 0, 96, 38, TextAnchor.MiddleCenter); chips.Add((c, t)); OnClick(c.gameObject, () => { chip = k; cursor = 0; scrollTarget = 0; Rebuild(); });
            }
            var vp = Box(B, "Viewport", new Color(1, 1, 1, 0)); vp.raycastTarget = true; Place(vp.rectTransform, 56, 216, VW, VH); vp.gameObject.AddComponent<RectMask2D>();
            OnScroll(vp.gameObject, e => scrollTarget -= e.scrollDelta.y * StepY * 0.5f);
            grid = NewRect("Grid", vp.transform); Set(grid, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(VW, VH));
            for (int i = 0; i < MaxCards; i++)
            {
                int k = i; var f = Img(grid, "Card" + i, UITheme.Art("Codex", "Card", new Vector4(16, 16, 16, 16)), Color.white, true, true); Place(f.rectTransform, 8 + (i % Cols) * StepX, 8 + (i / Cols) * StepY, CardW, CardH);
                var glow = Img(f.transform, "Glow", UITheme.Art("Codex", "CardGlow"), Color.white, false); Stretch(glow.rectTransform, 3);
                var icon = Img(f.transform, "Icon", null, Color.white, false); icon.preserveAspect = true; Place(icon.rectTransform, 23, 26, 110, 110);
                var artBg = Box(f.transform, "ArtBg", new Color(.08f, .07f, .06f, 1)); Place(artBg.rectTransform, 8, 44, 140, 74);
                var art = Img(artBg.transform, "Art", null, Color.white, false); Stretch(art.rectTransform, 2);
                var nm = Label(f.transform, "", 17, Ink, 6, 150, 144, 46, TextAnchor.MiddleCenter);
                var tag = Label(f.transform, "", 13, Gold, 10, 6, 136, 20, TextAnchor.MiddleLeft);
                OnClick(f.gameObject, () => { cursor = k; Show(); }); OnHover(f.gameObject, () => { if (cursor != k) { cursor = k; Show(); } }); OnScroll(f.gameObject, e => scrollTarget -= e.scrollDelta.y * StepY * 0.5f);
                cards.Add((f.gameObject, f, glow, icon, art, nm, tag));
            }
            // 오른쪽 큰 그림 + 글
            circle = Img(B, "Circle", UITheme.Art("Codex", "Circle"), Color.white, false); Place(circle.rectTransform, 850, 196, 520, 520);
            bigIcon = Img(B, "BigIcon", null, Color.white, false); bigIcon.preserveAspect = true; Place(bigIcon.rectTransform, 990, 336, 240, 240);
            var artFrame = Box(B, "BigArtBg", new Color(.08f, .07f, .06f, 1)); Place(artFrame.rectTransform, 870, 340, 480, 250);
            bigArt = Img(artFrame.transform, "BigArt", null, Color.white, false); Stretch(bigArt.rectTransform, 3);
            name = StyleHeading(Txt(B, "", 38, Ink, TextAnchor.UpperLeft)); if (name.TryGetComponent<Shadow>(out var s3)) Object.Destroy(s3); Place(name.rectTransform, 1390, 196, 270, 96);
            type = Label(B, "", 20, InkSoft, 1390, 292, 270, 28); keyLabel = Label(B, "", 19, InkSoft, 1390, 334, 270, 26);
            keyValue = Label(B, "", 64, Ink, 1390, 358, 270, 84); keyValue.font = UITheme.DisplayFont;
            stars = Label(B, "", 26, Gold, 1390, 446, 270, 34); lines = Label(B, "", 18, Ink, 1390, 488, 270, 230, TextAnchor.UpperLeft); lines.lineSpacing = 1.3f;
            desc = Label(B, "", 21, InkSoft, 870, 740, 770, 110, TextAnchor.UpperLeft); desc.fontStyle = FontStyle.Italic; desc.lineSpacing = 1.4f;
            Label(B, "Esc · K  뒤로      [ ]  장비 ↔ 스킬      ←→↑↓  고르기      휠  넘기기", 18, InkSoft, 60, 874, 1580, 30, TextAnchor.MiddleCenter);
            root.SetActive(false);
        }

        public override void Open()
        {
            stats = ui != null ? ui.player : null; inv = stats != null ? stats.GetComponent<Inventory>() : null;
            base.Open(); chip = 0; cursor = 0; scrollY = scrollTarget = 0; Rebuild();
        }

        void SetTab(int t) { tab = t; chip = 0; cursor = 0; scrollY = scrollTarget = 0; Rebuild(); }

        bool ChipMatch(Entry e)
        {
            if (chip == 0) return true;
            if (e.item != null) { var s = e.item.Slot; return chip switch { 1 => s == EquipSlot.Weapon, 2 => s == EquipSlot.Head, 3 => s == EquipSlot.Chest, 4 => s == EquipSlot.Legs, 5 => s == EquipSlot.Feet, _ => s == EquipSlot.Artifact }; }
            var kind = SkillInfo.Kind(e.key); return chip switch { 1 => kind == "일반 공격", 2 => kind == "스킬", _ => kind == "필살기" };
        }

        void Rebuild()
        {
            entries.Clear(); int items = 0, skills = 0;
            var seen = new HashSet<string>();
            if (inv != null) foreach (var s in inv.slots) { if (s == null || s.Empty || s.Def == null || !s.Def.Equippable || !seen.Add(s.id)) continue; items++; var e = new Entry { item = s.Def }; if (tab == 0 && ChipMatch(e)) entries.Add(e); }
            foreach (var (sk, key) in SkillInfo.Owned(stats)) { skills++; var e = new Entry { skill = sk, key = key }; if (tab == 1 && ChipMatch(e)) entries.Add(e); }
            countText.text = $"모은 장비 {items}  ·  익힌 스킬 {skills}";
            for (int i = 0; i < 2; i++) { tabText[i].color = i == tab ? Ink : new Color(Ink.r, Ink.g, Ink.b, .45f); tabLine[i].enabled = i == tab; }
            var names = Chips[tab];
            for (int i = 0; i < chips.Count; i++) { bool on = i < names.Length; chips[i].bg.gameObject.SetActive(on); if (!on) continue; chips[i].label.text = names[i]; chips[i].bg.color = i == chip ? new Color(1f, .86f, .55f) : Color.white; }
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i]; bool has = i < entries.Count; c.go.SetActive(has); if (!has) continue; var e = entries[i];
                bool isItem = e.item != null; c.icon.gameObject.SetActive(isItem); c.art.transform.parent.gameObject.SetActive(!isItem);
                if (isItem) { c.icon.sprite = ItemIcons.Get(e.item); c.name.text = e.item.name; c.glow.color = ItemDef.GradeColor(e.item.grade) * new Color(1, 1, 1, .8f); c.tag.text = inv != null && inv.IsEquipped(e.item.id) ? "장착 중" : e.item.grade; }
                else { c.art.sprite = SkillIllustration.Get(e.skill); c.name.text = e.skill.skillName; c.glow.color = SkillAttributes.ColorOf(e.skill.attribute) * new Color(1, 1, 1, .8f); c.tag.text = e.key; }
            }
            if (cursor >= entries.Count) cursor = Mathf.Max(0, entries.Count - 1);
            Show();
        }

        void Show()
        {
            for (int i = 0; i < cards.Count; i++) if (cards[i].go.activeSelf) { cards[i].frame.color = i == cursor ? new Color(1f, .9f, .66f) : Color.white; cards[i].go.transform.localScale = Vector3.one * (i == cursor ? 1.04f : 1f); }
            bool any = cursor >= 0 && cursor < entries.Count; var e = any ? entries[cursor] : null;
            bigIcon.enabled = e != null && e.item != null; bigArt.transform.parent.gameObject.SetActive(e != null && e.skill != null); circle.color = new Color(1, 1, 1, e != null ? 1f : .4f);
            if (e == null) { name.text = tab == 0 ? "아직 모은 장비가 없다" : "아직 익힌 스킬이 없다"; type.text = keyLabel.text = keyValue.text = stars.text = lines.text = desc.text = ""; return; }
            if (e.item != null)
            {
                var d = e.item; bigIcon.sprite = ItemIcons.Get(d); name.text = d.name; type.text = d.TypeLabel + (string.IsNullOrEmpty(d.grade) ? "" : $"  ·  {d.grade} 등급");
                var fx = EquipmentMath.Describe(d.stats);
                if (d.type == ItemType.Weapon) { keyLabel.text = "공격력 배율"; keyValue.text = "×" + d.damageMultiplier.ToString("0.00"); }
                else { keyLabel.text = "대표 효과"; keyValue.text = fx.Count > 0 ? fx[0].Substring(fx[0].LastIndexOf(' ') + 1) : "—"; }
                stars.text = Stars(d.grade);
                var l = new List<string>(fx); if (inv != null && inv.IsEquipped(d.id)) l.Add("<color=#3B7A2E>지금 장착 중</color>"); l.Add($"가치 {d.value} 금화");
                lines.text = string.Join("\n", l); desc.text = string.IsNullOrEmpty(d.description) ? "" : "“" + d.description + "”";
            }
            else
            {
                var s = e.skill; var (mn, mx) = SkillInfo.Power(s, stats); bigArt.sprite = SkillIllustration.Get(s);
                name.text = s.skillName; type.text = $"{SkillInfo.Kind(e.key)}  ·  {e.key}"; keyLabel.text = "최종 위력"; keyValue.text = mn != mx ? $"{mn}~{mx}" : mx.ToString();
                stars.text = $"<color=#{ColorUtility.ToHtmlStringRGB(SkillAttributes.ColorOf(s.attribute))}>◆</color> {SkillInfo.AttrLine(s)}";
                lines.text = string.Join("\n", SkillInfo.Details(s, stats)).Replace("  ·  ", "\n");
                desc.text = string.IsNullOrEmpty(s.description) ? "" : "“" + s.description + "”";
            }
        }
        static string Stars(string g) { int n = g switch { "S" => 5, "A" => 4, "B" => 3, "C" => 2, _ => 0 }; return n == 0 ? "" : new string('★', n) + "<color=#C9BBA0>" + new string('☆', 5 - n) + "</color>"; }

        public override void Tick()
        {
            float dt = Time.unscaledDeltaTime;
            int rows = (entries.Count + Cols - 1) / Cols; float max = Mathf.Max(0, rows * StepY + 8 - VH); scrollTarget = Mathf.Clamp(scrollTarget, 0, max);
            scrollY = Mathf.Lerp(scrollY, scrollTarget, 1f - Mathf.Exp(-14f * dt)); grid.anchoredPosition = new Vector2(0, scrollY);
            if (InputAdapter.TabNext() || InputAdapter.TabPrev()) SetTab(1 - tab);
            var n = InputAdapter.NavPressed();
            if ((n.x != 0 || n.y != 0) && entries.Count > 0)
            {
                cursor = Mathf.Clamp(cursor + n.x - n.y * Cols, 0, entries.Count - 1);
                float top = (cursor / Cols) * StepY; if (top < scrollTarget) scrollTarget = top; if (top + StepY > scrollTarget + VH) scrollTarget = top + StepY - VH;
                Show();
            }
        }
    }
}
