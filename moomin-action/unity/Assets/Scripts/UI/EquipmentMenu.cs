using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 장비 화면(C 키 · 휴식 메뉴 "장비" · 가방에서 C). 사용자가 준 참고 화면의 구조를 우리 금박 테마로:
    ///   왼쪽 세로 칸 = 무기(기본 스파크 검) · 머리 · 가슴 · 다리 · 발  — 칸마다 아이콘 · 등급 글자(S/A/B/C) · 칸 이름
    ///   가운데 = 지금 민미의 3D 모습(장비 모양까지, 끌어서 돌려 보기) + 이름 · 레벨 · 전투력
    ///   그 오른쪽 = 마법 아티팩트 3칸(마름모) + 장비 묶음 1 · 2 · 3
    ///   오른쪽 = 능력치(공격력 · 코인 최종 위력 · 무기 배율 · 최대 체력 · 받는 피해) · 고른 칸에 낄 수 있는 장비 목록 · 자세한 설명
    /// 조작: W/S 칸 이동, A/D 왼쪽 칸 ↔ 아티팩트 ↔ 장비 목록, Enter 장착(칸에서는 목록으로), X 해제, 1·2·3 장비 묶음, C·Esc 닫기. 마우스 클릭도 됨.
    /// 미리보기: tools/equipment_mock.py → previews/equipment_mock.png (좌표가 같음).
    /// </summary>
    public class EquipmentMenu : MenuPage
    {
        // 배치(패널 왼쪽 위 기준, px) — tools/equipment_mock.py 와 같게
        public const float PW = 1720, PH = 930, SlotX = 50, SlotY = 130, SlotSize = 130, SlotStep = 150;
        public const float ViewX = 200, ViewY = 112, ViewW = 560, ViewH = 640, ArtX = 800, ArtY0 = 210, ArtStep = 168, ArtSize = 104, RightX = 1040, RightW = 640;
        static readonly EquipSlot[] LeftSlots = { EquipSlot.Weapon, EquipSlot.Head, EquipSlot.Chest, EquipSlot.Legs, EquipSlot.Feet };
        const int ArtCount = Inventory.ArtifactCount, SlotCount = 5 + ArtCount, CandCols = 6, CandMax = 12;

        class SlotUI { public EquipSlot slot; public int index; public Image frame, icon, glow; public Text grade, label, empty, plus; }
        readonly SlotUI[] slots = new SlotUI[SlotCount];
        Image[] candFrame = new Image[CandMax], candIcon = new Image[CandMax]; Text[] candTag = new Text[CandMax]; readonly List<string> cands = new();
        Image detailIcon; Text detailName, detailType, detailFx, detailDesc, statText, statNums, candTitle, nameText, levelText, cpText, message; float messageT;
        Image[] presetBtn = new Image[Inventory.PresetCount]; Text[] presetTxt = new Text[Inventory.PresetCount];
        RawImage view; EquipmentPreview preview;
        Inventory inv; CharacterStats stats; Experience xp;
        int sel, cand = -1; bool inList;

        static Vector2 TL(float x, float y) => new Vector2(x, -y);
        static RectTransform Place(RectTransform r, float x, float y, float w, float h) => Set(r, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), TL(x, y), new Vector2(w, h));

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            var dim = Box(canvas, "EquipmentRoot", UITheme.Dim); Stretch(dim.rectTransform); dim.raycastTarget = true; root = dim.gameObject;
            var panel = Panel(root.transform, "Panel", new Vector2(PW, PH)); Header(panel.transform, "장 비", PW, -22); var P = panel.transform;

            // ── 왼쪽: 무기·머리·가슴·다리·발
            for (int i = 0; i < 5; i++) slots[i] = MakeSlot(P, LeftSlots[i], 0, SlotX, SlotY + i * SlotStep, SlotSize, false, i);
            // ── 가운데: 3D 모습 + 받침 빛 + 이름·레벨·전투력
            var glow = Img(P, "ViewGlow", UITheme.SoftCircle, new Color(1f, .82f, .45f, .22f), false); Place(glow.rectTransform, ViewX - 40, ViewY + 40, ViewW + 80, ViewH);
            var floor = Img(P, "Floor", UITheme.SoftCircle, new Color(0, 0, 0, .55f), false); Place(floor.rectTransform, ViewX + 90, ViewY + ViewH - 70, ViewW - 180, 60);
            view = new GameObject("View", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); view.transform.SetParent(P, false); Place(view.rectTransform, ViewX, ViewY, ViewW, ViewH); view.raycastTarget = true;
            OnDrag(view.gameObject, e => { if (preview != null) preview.yaw += e.delta.x * 0.4f; });
            var vh = Txt(P, "끌어서 돌려 보기", 20, UITheme.Muted, TextAnchor.MiddleCenter); Place(vh.rectTransform, ViewX, ViewY + ViewH - 30, ViewW, 28);
            nameText = Txt(P, "", 42, UITheme.Cream, TextAnchor.MiddleLeft, FontStyle.Bold); Place(nameText.rectTransform, ViewX + 10, ViewY + ViewH + 4, 520, 54);
            levelText = Txt(P, "", 30, UITheme.Cream, TextAnchor.MiddleLeft, FontStyle.Bold); Place(levelText.rectTransform, ViewX + 10, ViewY + ViewH + 58, 520, 40);
            cpText = Txt(P, "", 30, UITheme.Cream, TextAnchor.MiddleLeft, FontStyle.Bold); Place(cpText.rectTransform, ViewX + 10, ViewY + ViewH + 102, 520, 40);
            // ── 마법 아티팩트 3칸(마름모)
            var al = Txt(P, "마법 아티팩트", 22, UITheme.GoldBright, TextAnchor.MiddleCenter); Place(al.rectTransform, ArtX - 30, ArtY0 - 100, ArtSize + 60, 30);
            for (int i = 0; i < ArtCount; i++) slots[5 + i] = MakeSlot(P, EquipSlot.Artifact, i, ArtX, ArtY0 + i * ArtStep - ArtSize / 2, ArtSize, true, 5 + i);
            // ── 장비 묶음 1 · 2 · 3
            var pl = Txt(P, "장비 묶음", 22, UITheme.Muted, TextAnchor.MiddleCenter); Place(pl.rectTransform, ArtX - 50, ViewY + ViewH + 20, 210, 30);
            for (int i = 0; i < Inventory.PresetCount; i++)
            {
                int idx = i; var b = Img(P, "Preset" + i, UITheme.Slot, Color.white, true, true); Place(b.rectTransform, ArtX - 40 + i * 68, ViewY + ViewH + 56, 60, 60);
                presetBtn[i] = b; presetTxt[i] = Txt(b.transform, (i + 1).ToString(), 30, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(presetTxt[i].rectTransform);
                OnClick(b.gameObject, () => { if (inv != null) inv.SwitchPreset(idx); Refresh(); });
            }
            // ── 오른쪽: 능력치
            var sp = Img(P, "Stats", UITheme.Slot, new Color(1, 1, 1, .9f), true); Place(sp.rectTransform, RightX, 112, RightW, 230);
            var st = Txt(sp.transform, "능력치", 28, UITheme.GoldBright, TextAnchor.MiddleLeft, FontStyle.Bold); Place(st.rectTransform, 24, 10, 300, 40);
            statText = Txt(sp.transform, "", 25, UITheme.Muted, TextAnchor.UpperLeft); Place(statText.rectTransform, 24, 58, 260, 170); statText.lineSpacing = 1.12f;
            statNums = Txt(sp.transform, "", 25, UITheme.Cream, TextAnchor.UpperLeft); Place(statNums.rectTransform, 210, 58, RightW - 230, 170); statNums.lineSpacing = 1.12f;
            // ── 장비 목록(고른 칸에 낄 수 있는 것)
            candTitle = Txt(P, "", 26, UITheme.GoldBright, TextAnchor.MiddleLeft, FontStyle.Bold); Place(candTitle.rectTransform, RightX, 356, RightW, 40);
            for (int i = 0; i < CandMax; i++)
            {
                int idx = i; float x = RightX + (i % CandCols) * 106, y = 404 + (i / CandCols) * 106;
                var f = Img(P, "Cand" + i, UITheme.Slot, Color.white, true, true); Place(f.rectTransform, x, y, 96, 96);
                var ic = Img(f.transform, "Icon", null, Color.white, false); Set(ic.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(72, 72));
                var tg = Txt(f.transform, "", 17, UITheme.GoldBright, TextAnchor.UpperLeft, FontStyle.Bold); Stretch(tg.rectTransform, 5);
                candFrame[i] = f; candIcon[i] = ic; candTag[i] = tg;
                OnHover(f.gameObject, () => { if (idx < cands.Count) { inList = true; cand = idx; Refresh(); } });
                OnClick(f.gameObject, () => { if (idx < cands.Count) { inList = true; cand = idx; EquipCandidate(); } });
            }
            // ── 자세히
            var dp = Img(P, "Detail", UITheme.Slot, new Color(1, 1, 1, .9f), true); Place(dp.rectTransform, RightX, 626, RightW, 236);
            detailIcon = Img(dp.transform, "Icon", null, Color.white, false); Place(detailIcon.rectTransform, 20, 20, 100, 100);
            detailName = Txt(dp.transform, "", 32, UITheme.GoldBright, TextAnchor.MiddleLeft, FontStyle.Bold); Place(detailName.rectTransform, 136, 16, RightW - 150, 44);
            detailType = Txt(dp.transform, "", 22, UITheme.Muted, TextAnchor.MiddleLeft); Place(detailType.rectTransform, 136, 60, RightW - 150, 32);
            detailFx = Txt(dp.transform, "", 25, new Color(.6f, .95f, .4f), TextAnchor.UpperLeft, FontStyle.Bold); Place(detailFx.rectTransform, 136, 94, RightW - 150, 40);
            detailDesc = Txt(dp.transform, "", 22, UITheme.Cream, TextAnchor.UpperLeft); Place(detailDesc.rectTransform, 20, 136, RightW - 40, 96);
            message = Txt(P, "", 24, UITheme.GoldBright, TextAnchor.MiddleRight); Place(message.rectTransform, RightX, 356, RightW, 40);   // 목록 제목 오른쪽
            var hint = Txt(panel.transform, "W/S·A/D 이동   Enter 장착   X 해제   1·2·3 묶음   C·Esc 닫기", 20, UITheme.Muted, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, RightX - 40, PH - 58, RightW + 80, 32);   // 오른쪽 아래(이름·전투력과 겹치지 않게)
            root.SetActive(false);
        }

        SlotUI MakeSlot(Transform P, EquipSlot slot, int index, float x, float y, float size, bool diamond, int order)
        {
            var s = new SlotUI { slot = slot, index = index };
            s.glow = Img(P, "SlotGlow" + order, UITheme.SoftCircle, new Color(1f, .82f, .4f, .5f), false); Place(s.glow.rectTransform, x - 22, y - 22, size + 44, size + 44);
            s.frame = Img(P, "Slot" + order, UITheme.Slot, Color.white, true, true); Place(s.frame.rectTransform, x, y, size, size);
            if (diamond) { s.frame.rectTransform.pivot = new Vector2(.5f, .5f); s.frame.rectTransform.anchoredPosition = TL(x + size / 2, y + size / 2); s.frame.rectTransform.localRotation = Quaternion.Euler(0, 0, 45); s.frame.rectTransform.sizeDelta = new Vector2(size * .78f, size * .78f); }
            var holder = NewRect("Content" + order, P); Place(holder, x, y, size, size);   // 회전하지 않는 내용(아이콘·글자)
            s.empty = Txt(holder, diamond ? "◇" : EquipmentMath.SlotName(slot), diamond ? 30 : 24, new Color(UITheme.Muted.r, UITheme.Muted.g, UITheme.Muted.b, .55f), TextAnchor.MiddleCenter); Stretch(s.empty.rectTransform);
            s.icon = Img(holder, "Icon", null, Color.white, false); Set(s.icon.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(size * (diamond ? .62f : .74f), size * (diamond ? .62f : .74f)));
            s.grade = Txt(holder, "", diamond ? 22 : 30, Color.white, TextAnchor.UpperLeft, FontStyle.BoldAndItalic); Set(s.grade.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(diamond ? 6 : 8, diamond ? -2 : -2), new Vector2(-12, -8));
            s.label = Txt(holder, diamond ? "" : EquipmentMath.SlotName(slot), 18, UITheme.Muted, TextAnchor.LowerLeft); Set(s.label.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(8, 4), new Vector2(-14, -8));
            s.plus = Txt(holder, "", 18, UITheme.GoldBright, TextAnchor.LowerRight, FontStyle.Bold); Set(s.plus.rectTransform, Vector2.zero, Vector2.one, new Vector2(.5f, .5f), new Vector2(-8, 4), new Vector2(-14, -8));
            var hit = Box(holder, "Hit", new Color(0, 0, 0, 0)); Stretch(hit.rectTransform); hit.raycastTarget = true;
            OnHover(hit.gameObject, () => { sel = order; inList = false; cand = -1; Refresh(); });
            OnClick(hit.gameObject, () => { sel = order; inList = false; cand = -1; Refresh(); });
            return s;
        }

        public override void Open()
        {
            stats = ui != null ? ui.player : null; inv = stats != null ? stats.GetComponent<Inventory>() : null; xp = stats != null ? stats.GetComponent<Experience>() : null;
            if (inv != null) { inv.Changed -= Refresh; inv.Changed += Refresh; inv.Message -= Say; inv.Message += Say; }
            base.Open(); sel = 1; inList = false; cand = -1;
            if (stats != null) { if (preview == null) preview = new EquipmentPreview(); preview.Begin(stats.transform, (int)ViewW * 2, (int)ViewH * 2); view.texture = preview.Texture; }
            Refresh();
        }
        public override void Close() { if (inv != null) { inv.Changed -= Refresh; inv.Message -= Say; } preview?.End(); base.Close(); }
        void Say(string m) { message.text = m; messageT = 2.5f; }

        SlotUI Sel => slots[Mathf.Clamp(sel, 0, SlotCount - 1)];

        void Refresh()
        {
            if (inv == null || root == null || !root.activeInHierarchy) return;
            // 칸
            for (int i = 0; i < SlotCount; i++)
            {
                var s = slots[i]; var d = ItemDatabase.Get(inv.Equipped(s.slot, s.index)); bool on = i == sel;
                s.icon.enabled = d != null; s.icon.sprite = d != null ? ItemIcons.Get(d) : null; s.empty.enabled = d == null;
                s.grade.text = d != null ? d.grade : ""; s.grade.color = d != null ? ItemDef.GradeColor(d.grade) : Color.white;
                s.frame.color = on ? new Color(1f, .86f, .5f) : new Color(1, 1, 1, d != null ? 1f : .75f); s.glow.enabled = on && !inList;
                s.label.color = on ? UITheme.GoldBright : UITheme.Muted;
            }
            // 이름·레벨·전투력
            int lv = xp != null ? xp.level : 1; var eq = inv.TotalStats;
            nameText.text = stats.displayName; levelText.text = $"Lv.{lv}   <color=#E8C97A>스파크 검사</color>";
            cpText.text = "전투력  <color=#9BE86A>" + EquipmentMath.CombatPower(stats.Attack, stats.equipFinalPower, stats.maxHealth, stats.equipDamageReduction, lv).ToString("N0") + "</color>";
            // 능력치
            var w = ItemDatabase.Get(inv.equippedWeapon); float wm = w != null ? w.damageMultiplier : 1f;
            statText.text = "공격력\n코인 최종 위력\n무기 배율\n최대 체력\n받는 피해";
            statNums.text = $"{stats.Attack:0.#}   <size=20><color=#9E927A>기본 {stats.attack:0.#} · 레벨 +{stats.levelAttack:0.#} · 장비 +{stats.equipAttack:0.#}</color></size>\n"
                          + (stats.equipFinalPower != 0 ? $"<color=#9BE86A>{(stats.equipFinalPower > 0 ? "+" : "")}{stats.equipFinalPower}</color>" : "+0") + "   <size=20><color=#9E927A>공격마다(앞면·뒷면 모두)</color></size>\n"
                          + $"×{wm:0.00}\n{stats.maxHealth:0}" + (stats.equipHealth != 0 ? $"   <size=20><color=#9BE86A>장비 +{stats.equipHealth:0}</color></size>" : "") + "\n"
                          + (stats.equipDamageReduction > 0 ? $"<color=#9BE86A>-{stats.equipDamageReduction * 100:0.#}%</color>" : "-0%");
            // 장비 목록
            var slot = Sel.slot; cands.Clear();
            foreach (var s in inv.slots) if (!s.Empty && s.Def != null && s.Def.Equippable && s.Def.Slot == slot && !cands.Contains(s.id)) cands.Add(s.id);
            candTitle.text = (slot == EquipSlot.Artifact ? $"마법 아티팩트 {Sel.index + 1}" : EquipmentMath.SlotName(slot)) + "  —  낄 수 있는 장비 " + cands.Count + "개";
            if (cand >= cands.Count) cand = cands.Count - 1;
            for (int i = 0; i < CandMax; i++)
            {
                bool has = i < cands.Count; var d = has ? ItemDatabase.Get(cands[i]) : null;
                candIcon[i].enabled = has; candIcon[i].sprite = has ? ItemIcons.Get(d) : null;
                bool here = has && inv.Equipped(slot, Sel.index) == cands[i], elsewhere = has && !here && inv.IsEquipped(cands[i]);
                candTag[i].text = here ? "장착" : elsewhere ? "<color=#9E927A>다른 칸</color>" : "";
                candFrame[i].color = inList && i == cand ? new Color(1f, .86f, .5f) : new Color(1, 1, 1, has ? 1f : .4f);
            }
            // 자세히: 목록에서 고른 것 → 아니면 그 칸에 낀 것
            var cur = inList && cand >= 0 && cand < cands.Count ? ItemDatabase.Get(cands[cand]) : ItemDatabase.Get(inv.Equipped(slot, Sel.index));
            detailIcon.enabled = cur != null; detailIcon.sprite = cur != null ? ItemIcons.Get(cur) : null;
            detailName.text = cur != null ? cur.name + (string.IsNullOrEmpty(cur.grade) ? "" : $"   <color=#{ColorUtility.ToHtmlStringRGB(ItemDef.GradeColor(cur.grade))}>{cur.grade}</color>") : (slot == EquipSlot.Artifact ? "빈 아티팩트 칸" : "비어 있음");
            detailType.text = cur != null ? cur.TypeLabel : (cands.Count > 0 ? "→ 오른쪽 목록에서 고르면 낄 수 있다" : "아직 이 칸에 맞는 장비가 없다");
            var fx = cur != null ? EquipmentMath.Describe(cur.stats) : new List<string>();
            if (cur != null && cur.type == ItemType.Weapon) fx.Insert(0, "공격력 ×" + cur.damageMultiplier.ToString("0.00"));
            detailFx.text = string.Join("   ·   ", fx); detailDesc.text = cur != null ? cur.description : "";
            for (int i = 0; i < Inventory.PresetCount; i++) { bool on = i == inv.preset; presetBtn[i].color = on ? new Color(.55f, .75f, 1f) : Color.white; presetTxt[i].color = on ? Color.white : UITheme.Muted; }
        }

        void EquipCandidate()
        {
            if (inv == null || cand < 0 || cand >= cands.Count) return;
            var id = cands[cand]; var s = Sel;
            if (inv.Equipped(s.slot, s.index) == id) { if (s.slot != EquipSlot.Weapon) inv.Unequip(s.slot, s.index); else Say("이미 낀 무기다"); }
            else inv.EquipTo(id, s.slot, s.slot == EquipSlot.Artifact ? s.index : -1);
            Refresh();
        }

        public override void Tick()
        {
            if (messageT > 0) { messageT -= Time.unscaledDeltaTime; if (messageT <= 0) message.text = ""; }
            preview?.Update();
            var n = InputAdapter.NavPressed();
            if (!inList)
            {
                if (n.y != 0)
                {
                    if (sel < 5) sel = Mathf.Clamp(sel - n.y, 0, 4); else sel = 5 + Mathf.Clamp(sel - 5 - n.y, 0, ArtCount - 1);
                    cand = -1; Refresh();
                }
                if (n.x > 0) { if (sel < 5) sel = 5 + Mathf.Clamp(sel - 1, 0, ArtCount - 1); else if (cands.Count > 0) { inList = true; cand = 0; } Refresh(); }
                if (n.x < 0 && sel >= 5) { sel = Mathf.Clamp(sel - 5 + 1, 0, 4); Refresh(); }
                if (InputAdapter.ConfirmPressed() && cands.Count > 0) { inList = true; cand = Mathf.Max(0, cands.IndexOf(inv.Equipped(Sel.slot, Sel.index))); Refresh(); }
                if (InputAdapter.DropPressed()) { inv.Unequip(Sel.slot, Sel.index); Refresh(); }
            }
            else
            {
                if (n.x != 0 || n.y != 0)
                {
                    int cx = cand % CandCols + n.x, cy = cand / CandCols - n.y;
                    if (cx < 0) { inList = false; cand = -1; Refresh(); return; }
                    cx = Mathf.Clamp(cx, 0, CandCols - 1); cy = Mathf.Clamp(cy, 0, (CandMax - 1) / CandCols); cand = Mathf.Clamp(cy * CandCols + cx, 0, Mathf.Max(0, cands.Count - 1)); Refresh();
                }
                if (InputAdapter.ConfirmPressed()) EquipCandidate();
                if (InputAdapter.DropPressed()) { inv.Unequip(Sel.slot, Sel.index); Refresh(); }
            }
            int q = InputAdapter.QuickPressed(); if (q >= 0 && q < Inventory.PresetCount && inv != null) { inv.SwitchPreset(q); Refresh(); }
        }
    }

    /// <summary>
    /// 장비 화면 가운데의 3D 모습: 멈춘 게임 속 실제 민미(장비 모양 포함)를 앞에서 비추는 카메라 → RenderTexture.
    /// 플레이어 레이어만 그려 배경은 투명. 전용 조명(플레이어 레이어에만). 화면을 끌면 yaw 로 돌려 봄.
    /// </summary>
    public class EquipmentPreview
    {
        Camera cam; Light key, rim; RenderTexture rt; Transform target; public float yaw = 18f;
        public RenderTexture Texture => rt;

        public void Begin(Transform player, int w, int h)
        {
            target = player;
            if (rt == null) { rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "EquipmentPreview", antiAliasing = 4 }; }
            if (cam == null)
            {
                var go = new GameObject("EquipmentPreviewCamera"); Object.DontDestroyOnLoad(go); go.hideFlags = HideFlags.DontSave;
                cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0); cam.fieldOfView = 26f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 20f;
                key = new GameObject("Key").AddComponent<Light>(); key.transform.SetParent(go.transform, false); key.type = LightType.Directional; key.intensity = 1.1f; key.color = new Color(1f, .95f, .88f);
                rim = new GameObject("Rim").AddComponent<Light>(); rim.transform.SetParent(go.transform, false); rim.type = LightType.Directional; rim.intensity = 0.7f; rim.color = new Color(.65f, .8f, 1f);
            }
            int layer = player.gameObject.layer, mask = layer != 0 ? 1 << layer : ~0;   // 플레이어 레이어가 있으면 플레이어만(배경 투명)
            cam.cullingMask = mask; key.cullingMask = mask; rim.cullingMask = mask; cam.targetTexture = rt; cam.gameObject.SetActive(true); Update();
        }

        public void Update()
        {
            if (cam == null || target == null) return;
            Vector3 c = target.position + Vector3.up * 0.62f;
            var dir = Quaternion.Euler(0, yaw, 0) * target.forward;
            cam.transform.position = c + dir * 3.3f + Vector3.up * 0.25f; cam.transform.LookAt(c);
            key.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(30, -40, 0) * (c - cam.transform.position).normalized);
            rim.transform.rotation = Quaternion.LookRotation(-(c - cam.transform.position).normalized + Vector3.down * 0.4f);
        }

        public void End() { if (cam != null) cam.gameObject.SetActive(false); }
    }
}
