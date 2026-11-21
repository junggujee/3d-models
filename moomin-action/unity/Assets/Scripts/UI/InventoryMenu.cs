using UnityEngine;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>
    /// 가방: 6×5 칸 격자 + 탭(전체/소모품/무기/재료/중요) + 상세 설명 + 장착 무기 + 퀵슬롯 1~4.
    /// 조작: 화살표/WASD 이동, Enter/E(또는 칸을 한 번 더 클릭) 사용·장착, X 버리기, 1~4 퀵슬롯 지정, R 정렬, [ ] 또는 PageUp/Down 탭 전환, I/Tab/Esc 닫기.
    /// </summary>
    public class InventoryMenu : MenuPage
    {
        const int Cols = 6, Rows = 5, Cells = Cols * Rows; const float CellSize = 100f, Gap = 8f;
        static readonly string[] TabNames = { "전체", "소모품", "무기", "재료", "중요" };
        Inventory inv; int tab, cursor; int[] view = new int[Cells];
        Image[] cellFrame = new Image[Cells], cellIcon = new Image[Cells]; Text[] cellCount = new Text[Cells], cellTag = new Text[Cells];
        Image[] tabBar = new Image[5]; Text[] tabText = new Text[5];
        Image detailIcon; Text detailName, detailType, detailDesc, detailStats, goldText, weaponText, messageText; Image[] quickIcon = new Image[Inventory.QuickCount]; Text[] quickCount = new Text[Inventory.QuickCount];
        float messageT;

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas); inv = ui.player ? ui.player.GetComponent<Inventory>() : null;
            var dim = UIFactory.Box(canvas, "InventoryRoot", UITheme.Dim); UIFactory.Stretch(dim.rectTransform); dim.raycastTarget = true; root = dim.gameObject;
            var panel = UIFactory.Panel(root.transform, "Panel", new Vector2(1500, 880)); UIFactory.Header(panel.transform, "가 방", 1500, -22);
            goldText = UIFactory.Txt(panel.transform, "", 30, UITheme.GoldBright, TextAnchor.MiddleRight); UIFactory.Set(goldText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -40), new Vector2(360, 50));

            // 탭
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i; var b = UIFactory.Img(panel.transform, "Tab" + i, UITheme.Slot, Color.white, true, true); UIFactory.Set(b.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(46 + i * 128, -138), new Vector2(120, 46));
                tabBar[i] = b; tabText[i] = UIFactory.Txt(b.transform, TabNames[i], 26, UITheme.Cream, TextAnchor.MiddleCenter); UIFactory.Stretch(tabText[i].rectTransform);
                UIFactory.OnClick(b.gameObject, () => { tab = idx; cursor = 0; Refresh(); });
            }
            // 격자
            float gx = 46, gy = -200;
            for (int i = 0; i < Cells; i++)
            {
                int idx = i; int cx = i % Cols, cy = i / Cols;
                var f = UIFactory.Img(panel.transform, "Cell" + i, UITheme.Slot, Color.white, true, true); UIFactory.Set(f.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(gx + cx * (CellSize + Gap), gy - cy * (CellSize + Gap)), new Vector2(CellSize, CellSize));
                var ic = UIFactory.Img(f.transform, "Icon", null, Color.white, false); UIFactory.Set(ic.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(74, 74));
                var ct = UIFactory.Txt(f.transform, "", 24, UITheme.Cream, TextAnchor.LowerRight, FontStyle.Bold); UIFactory.Set(ct.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(-8, 4), Vector2.zero);
                var tg = UIFactory.Txt(f.transform, "장착", 18, UITheme.GoldBright, TextAnchor.UpperLeft, FontStyle.Bold); UIFactory.Set(tg.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(8, -4), Vector2.zero);
                cellFrame[i] = f; cellIcon[i] = ic; cellCount[i] = ct; cellTag[i] = tg;
                UIFactory.OnHover(f.gameObject, () => { cursor = idx; Refresh(); });
                UIFactory.OnClick(f.gameObject, () => { if (cursor == idx) UseCurrent(); else { cursor = idx; Refresh(); } });
            }
            // 상세
            var dp = UIFactory.Img(panel.transform, "Detail", UITheme.Slot, new Color(1, 1, 1, 0.9f), true); UIFactory.Set(dp.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(720, -138), new Vector2(734, 520));
            detailIcon = UIFactory.Img(dp.transform, "BigIcon", null, Color.white, false); UIFactory.Set(detailIcon.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(26, -26), new Vector2(130, 130));
            detailName = UIFactory.Txt(dp.transform, "", 40, UITheme.GoldBright, TextAnchor.MiddleLeft, FontStyle.Bold); UIFactory.Set(detailName.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(180, -30), new Vector2(530, 60));
            detailType = UIFactory.Txt(dp.transform, "", 26, UITheme.Muted, TextAnchor.MiddleLeft); UIFactory.Set(detailType.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(180, -96), new Vector2(530, 44));
            var div = UIFactory.Img(dp.transform, "Div", UITheme.Highlight, Color.white); UIFactory.Set(div.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -174), new Vector2(-60, 12));
            detailDesc = UIFactory.Txt(dp.transform, "", 28, UITheme.Cream, TextAnchor.UpperLeft); UIFactory.Set(detailDesc.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(34, -200), new Vector2(-68, 150));
            detailStats = UIFactory.Txt(dp.transform, "", 28, UITheme.GoldBright, TextAnchor.UpperLeft); UIFactory.Set(detailStats.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(34, -360), new Vector2(-68, 140));
            // 장비·퀵슬롯
            weaponText = UIFactory.Txt(panel.transform, "", 28, UITheme.Cream, TextAnchor.MiddleLeft); UIFactory.Set(weaponText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(730, -675), new Vector2(720, 44));
            var ql = UIFactory.Txt(panel.transform, "퀵슬롯", 26, UITheme.Muted, TextAnchor.MiddleLeft); UIFactory.Set(ql.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(730, -722), new Vector2(120, 80));
            for (int i = 0; i < Inventory.QuickCount; i++)
            {
                var q = UIFactory.Img(panel.transform, "Quick" + i, UITheme.Slot, Color.white, true); UIFactory.Set(q.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(860 + i * 96, -712), new Vector2(84, 84));
                quickIcon[i] = UIFactory.Img(q.transform, "Icon", null, Color.white, false); UIFactory.Set(quickIcon[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 60));
                var k = UIFactory.Txt(q.transform, (i + 1).ToString(), 22, UITheme.Gold, TextAnchor.UpperLeft, FontStyle.Bold); UIFactory.Set(k.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(6, -2), Vector2.zero);
                quickCount[i] = UIFactory.Txt(q.transform, "", 22, UITheme.Cream, TextAnchor.LowerRight, FontStyle.Bold); UIFactory.Set(quickCount[i].rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(-6, 2), Vector2.zero);
            }
            messageText = UIFactory.Txt(panel.transform, "", 26, UITheme.GoldBright, TextAnchor.MiddleLeft); UIFactory.Set(messageText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(46, 70), new Vector2(700, 40));
            var hint = UIFactory.Txt(panel.transform, "Enter 사용·장착    X 버리기    1~4 퀵슬롯 지정    R 정렬    [ ] 탭    Esc 닫기", 22, UITheme.Muted, TextAnchor.MiddleCenter); UIFactory.Set(hint.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 22), new Vector2(1400, 40));
            root.SetActive(false);
        }

        bool Match(ItemDef d) => tab switch { 0 => true, 1 => d.type == ItemType.Consumable, 2 => d.type == ItemType.Weapon, 3 => d.type == ItemType.Material, _ => d.type == ItemType.Key };
        int Slot() => cursor >= 0 && cursor < Cells ? view[cursor] : -1;

        public override void Open() { inv = ui.player ? ui.player.GetComponent<Inventory>() : inv; if (inv != null) { inv.Changed -= Refresh; inv.Changed += Refresh; inv.Message -= Say; inv.Message += Say; } cursor = 0; base.Open(); Refresh(); }
        public override void Close() { if (inv != null) { inv.Changed -= Refresh; inv.Message -= Say; inv.Persist(); } base.Close(); }
        void Say(string m) { messageText.text = m; messageT = 2.5f; }

        void UseCurrent() { int s = Slot(); if (s >= 0 && inv != null) inv.UseSlot(s); Refresh(); }

        void Refresh()
        {
            if (inv == null || root == null || !root.activeInHierarchy) return;
            int n = 0; for (int i = 0; i < Cells; i++) view[i] = -1;
            for (int i = 0; i < Inventory.Capacity && n < Cells; i++) { var s = inv.slots[i]; if (!s.Empty && Match(s.Def)) view[n++] = i; }
            for (int i = 0; i < Cells; i++)
            {
                int si = view[i]; bool has = si >= 0; var s = has ? inv.slots[si] : null; var d = has ? s.Def : null;
                cellIcon[i].enabled = has; cellIcon[i].sprite = has ? ItemIcons.Get(d) : null; cellCount[i].text = has && s.count > 1 ? s.count.ToString() : "";
                cellTag[i].text = has && d.type == ItemType.Weapon && s.id == inv.equippedWeapon ? "장착" : "";
                cellFrame[i].color = i == cursor ? new Color(1f, .86f, .5f, 1f) : new Color(1, 1, 1, has ? 1f : .55f);
            }
            for (int i = 0; i < 5; i++) { bool on = i == tab; tabBar[i].color = on ? new Color(1f, .86f, .5f) : Color.white; tabText[i].color = on ? Color.white : UITheme.Cream; }
            goldText.text = "금화  " + inv.gold.ToString("N0");
            var cur = Slot() >= 0 ? inv.slots[Slot()].Def : null;
            detailIcon.enabled = cur != null; detailIcon.sprite = cur != null ? ItemIcons.Get(cur) : null;
            detailName.text = cur != null ? cur.name : "—"; detailType.text = cur != null ? cur.TypeLabel + (cur.Stackable ? "  ·  보유 " + inv.Count(cur.id) : "") : "";
            detailDesc.text = cur != null ? cur.description : "칸을 선택하면 설명이 보입니다."; detailStats.text = cur != null ? Stats(cur) : "";
            var w = ItemDatabase.Get(inv.equippedWeapon); weaponText.text = "장착 무기   <color=#E8C97A>" + (w != null ? w.name + (w.damageMultiplier != 1f ? "  (공격력 ×" + w.damageMultiplier.ToString("0.00") + ")" : "") : "없음") + "</color>";
            for (int i = 0; i < Inventory.QuickCount; i++) { var qd = ItemDatabase.Get(inv.quick[i]); int c = qd != null ? inv.Count(qd.id) : 0; quickIcon[i].enabled = qd != null; quickIcon[i].sprite = qd != null ? ItemIcons.Get(qd) : null; quickIcon[i].color = c > 0 ? Color.white : new Color(1, 1, 1, .3f); quickCount[i].text = qd != null ? c.ToString() : ""; }
        }

        static string Stats(ItemDef d)
        {
            var sb = new System.Text.StringBuilder();
            if (d.heal > 0) sb.AppendLine("체력 회복  +" + d.heal); if (d.stamina > 0) sb.AppendLine("스태미나 회복  +" + d.stamina);
            if (d.type == ItemType.Weapon) sb.AppendLine("공격력  ×" + d.damageMultiplier.ToString("0.00")); if (d.value > 0) sb.AppendLine("가치  " + d.value + " 금화");
            return sb.ToString();
        }

        public override void Tick()
        {
            if (messageT > 0) { messageT -= Time.unscaledDeltaTime; if (messageT <= 0) messageText.text = ""; }
            var n = InputAdapter.NavPressed();
            if (n.x != 0 || n.y != 0) { int cx = cursor % Cols, cy = cursor / Cols; cx = Mathf.Clamp(cx + n.x, 0, Cols - 1); cy = Mathf.Clamp(cy - n.y, 0, Rows - 1); cursor = cy * Cols + cx; Refresh(); }
            if (InputAdapter.TabNext()) { tab = (tab + 1) % 5; cursor = 0; Refresh(); } if (InputAdapter.TabPrev()) { tab = (tab + 4) % 5; cursor = 0; Refresh(); }
            if (InputAdapter.ConfirmPressed()) UseCurrent();
            if (InputAdapter.DropPressed() && Slot() >= 0) { inv.Drop(Slot()); Refresh(); }
            if (InputAdapter.SortPressed()) { inv.Sort(); Refresh(); }
            int q = InputAdapter.QuickPressed(); if (q >= 0 && Slot() >= 0) { inv.AssignQuick(q, inv.slots[Slot()].id); Refresh(); }
        }
    }
}
