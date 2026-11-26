using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>
    /// 대장간 작업대(3 × 3) — 대장간 화면(SmithPage)의 "제작" 쪽.
    ///  · 오른쪽 가방의 재료를 누르면 손에 들고, 작업대 칸을 누르면 1개씩 놓임(칸 하나 = 재료 1개). 같은 칸을 다시 누르거나 우클릭 = 비우기.
    ///  · 가방 재료를 우클릭 = 빈칸을 그 재료로 모두 채움(땅콩빵 조각 9개 같은 것).
    ///  · 모양이 제작법과 맞으면 화살표 오른쪽에 결과가 뜸 → 누르면 제작. 남은 재료로 한 번 더 만들 수 있으면 배치가 그대로 남음.
    ///  · 아래 제작법 책: 누르면 재료가 있으면 자동으로 놓고, 없으면 흐린 그림으로 모양만 보여 줌.
    ///  · 레이저 블레이더처럼 큰 물건(Recipe.showcase)은 특별 제작 연출: 재료가 가운데로 빨려 들어감 → 망치 두 번 → 푸른 빛 고리 → 빛줄기 속에서 결과가 떠오름.
    /// 놓은 재료는 제작 전까지 가방에서 빠지지 않음(남은 개수만 줄어 보임). 판정은 ForgeMath.Match(순수 C#).
    /// </summary>
    public class CraftBench
    {
        const float Cell = 92f, Gap = 8f, BagCell = 70f, BagGap = 7f; const int BagCols = 6, BagRows = 5;
        static readonly Color CellCol = new Color(1f, 1f, 1f, 0.8f), CellHot = new Color(1f, 0.92f, 0.65f), Glow = new Color(0.45f, 0.85f, 1f);
        readonly Inventory inv; readonly BalloonSmith smith; readonly System.Action changed;
        public RectTransform root;
        readonly string[] grid = new string[ForgeMath.GridCells]; string held; ForgeMath.Recipe ghost;
        readonly Image[] cellBg = new Image[9], cellIcon = new Image[9];
        Image resultBg, resultIcon; Text resultName, heldText; RectTransform gridRoot;
        readonly List<(Image bg, Image icon, Text count)> bag = new(); readonly List<string> bagIds = new();
        // 연출
        float animT = -1f, popT = -1f; ForgeMath.Recipe animRc; Image overlay, bigIcon, ring; Text banner; readonly List<Image> rays = new(); readonly List<(Image img, Vector2 from)> fly = new(); int strikes;
        public bool Busy => animT >= 0f;

        public CraftBench(Inventory inv, BalloonSmith smith, System.Action changed) { this.inv = inv; this.smith = smith; this.changed = changed; }

        public void Build(Transform parent)
        {
            root = UIFactory.NewRect("CraftBench", parent); UIFactory.At(root, new Vector2(0, 1), new Vector2(40, -216), new Vector2(1100, 500)); root.pivot = new Vector2(0, 1);
            // 작업대 3 × 3
            gridRoot = UIFactory.NewRect("Grid", root); UIFactory.At(gridRoot, new Vector2(0, 1), new Vector2(30, -6), new Vector2(Cell * 3 + Gap * 2, Cell * 3 + Gap * 2)); gridRoot.pivot = new Vector2(0, 1);
            var plate = UIFactory.Box(gridRoot, "Plate", new Color(0.2f, 0.14f, 0.09f, 0.55f)); UIFactory.Stretch(plate.rectTransform, -10f);
            for (int i = 0; i < 9; i++)
            {
                int r = i / 3, c = i % 3, idx = i;
                var bg = UIFactory.Img(gridRoot, "Cell" + i, UITheme.Slot, CellCol, true, true);
                UIFactory.At(bg.rectTransform, new Vector2(0, 1), new Vector2(c * (Cell + Gap), -r * (Cell + Gap)), new Vector2(Cell, Cell)); bg.rectTransform.pivot = new Vector2(0, 1);
                var ic = UIFactory.Img(bg.transform, "Icon", null, Color.white, false); UIFactory.Stretch(ic.rectTransform, 12f); ic.preserveAspect = true;
                cellBg[i] = bg; cellIcon[i] = ic;
                UIFactory.OnClick(bg.gameObject, (PointerEventData e) => ClickCell(idx, e.button == PointerEventData.InputButton.Right));
            }
            // 화살표 + 결과 칸
            float gw = Cell * 3 + Gap * 2, midY = -gw / 2f;
            var bar = UIFactory.Box(root, "ArrowBar", UITheme.Gold); UIFactory.At(bar.rectTransform, new Vector2(0, 1), new Vector2(30 + gw + 62, midY), new Vector2(64, 16)); bar.rectTransform.pivot = new Vector2(.5f, .5f);   // 위치 = 가운데
            var head = UIFactory.Box(root, "ArrowHead", UITheme.Gold); UIFactory.At(head.rectTransform, new Vector2(0, 1), new Vector2(30 + gw + 98, midY), new Vector2(30, 30)); head.rectTransform.pivot = new Vector2(.5f, .5f); head.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            resultBg = UIFactory.Img(root, "Result", UITheme.Slot, CellCol, true, true); UIFactory.At(resultBg.rectTransform, new Vector2(0, 1), new Vector2(30 + gw + 190, midY), new Vector2(124, 124)); resultBg.rectTransform.pivot = new Vector2(.5f, .5f);
            resultIcon = UIFactory.Img(resultBg.transform, "Icon", null, Color.white, false); UIFactory.Stretch(resultIcon.rectTransform, 14f); resultIcon.preserveAspect = true;
            UIFactory.OnClick(resultBg.gameObject, () => TryCraft());
            resultName = UIFactory.Txt(root, "", 24, UITheme.Cream, TextAnchor.UpperCenter); UIFactory.At(resultName.rectTransform, new Vector2(0, 1), new Vector2(30 + gw + 190, midY - 72), new Vector2(240, 64)); resultName.rectTransform.pivot = new Vector2(.5f, 1f);
            resultName.horizontalOverflow = HorizontalWrapMode.Wrap;
            var hint = UIFactory.Txt(root, "재료 클릭 → 칸 클릭: 놓기 · 칸 우클릭: 비우기\n재료 우클릭: 빈칸 모두 채우기", 18, UITheme.Muted, TextAnchor.UpperLeft);
            UIFactory.At(hint.rectTransform, new Vector2(0, 1), new Vector2(30, -gw - 18), new Vector2(540, 48)); hint.rectTransform.pivot = new Vector2(0, 1);
            // 제작법 책
            var bookLabel = UIFactory.StyleHeading(UIFactory.Txt(root, "제작법", 26, UITheme.Gold, TextAnchor.MiddleLeft));
            UIFactory.At(bookLabel.rectTransform, new Vector2(0, 1), new Vector2(30, -gw - 66), new Vector2(200, 34)); bookLabel.rectTransform.pivot = new Vector2(0, 1);
            for (int k = 0; k < ForgeMath.Recipes.Count; k++)
            {
                var rc = ForgeMath.Recipes[k]; var def = ItemDatabase.Get(rc.result); if (def == null) continue;
                var b = UIFactory.Img(root, "Book" + k, UITheme.Slot, new Color(1, 1, 1, 0.6f), true, true);
                UIFactory.At(b.rectTransform, new Vector2(0, 1), new Vector2(30 + (k % 4) * 140, -gw - 104 - (k / 4) * 46), new Vector2(136, 40)); b.rectTransform.pivot = new Vector2(0, 1);
                var bi = UIFactory.Img(b.transform, "Icon", ItemIcons.Get(def), Color.white, false); UIFactory.At(bi.rectTransform, new Vector2(0, .5f), new Vector2(6, 0), new Vector2(32, 32));
                var bt = UIFactory.Txt(b.transform, def.name, 15, UITheme.Cream, TextAnchor.MiddleLeft); UIFactory.At(bt.rectTransform, new Vector2(0, .5f), new Vector2(40, 0), new Vector2(94, 40)); bt.resizeTextForBestFit = true; bt.resizeTextMinSize = 11; bt.resizeTextMaxSize = 15; bt.rectTransform.pivot = new Vector2(0, .5f);
                var r2 = rc; UIFactory.OnClick(b.gameObject, () => FromBook(r2));
            }
            // 가방(재료)
            var bagLabel = UIFactory.StyleHeading(UIFactory.Txt(root, "가방 재료", 26, UITheme.Gold, TextAnchor.MiddleLeft));
            UIFactory.At(bagLabel.rectTransform, new Vector2(0, 1), new Vector2(600, 0), new Vector2(300, 34)); bagLabel.rectTransform.pivot = new Vector2(0, 1);
            for (int i = 0; i < BagCols * BagRows; i++)
            {
                int r = i / BagCols, c = i % BagCols, idx = i;
                var bg = UIFactory.Img(root, "Bag" + i, UITheme.Slot, new Color(1, 1, 1, 0.55f), true, true);
                UIFactory.At(bg.rectTransform, new Vector2(0, 1), new Vector2(600 + c * (BagCell + BagGap), -42 - r * (BagCell + BagGap)), new Vector2(BagCell, BagCell)); bg.rectTransform.pivot = new Vector2(0, 1);
                var ic = UIFactory.Img(bg.transform, "Icon", null, Color.white, false); UIFactory.Stretch(ic.rectTransform, 9f); ic.preserveAspect = true;
                var ct = UIFactory.Txt(bg.transform, "", 18, UITheme.Cream, TextAnchor.LowerRight); UIFactory.Stretch(ct.rectTransform, 5f);
                bag.Add((bg, ic, ct));
                UIFactory.OnClick(bg.gameObject, (PointerEventData e) => ClickBag(idx, e.button == PointerEventData.InputButton.Right));
            }
            heldText = UIFactory.Txt(root, "", 22, UITheme.GoldBright, TextAnchor.MiddleLeft);
            UIFactory.At(heldText.rectTransform, new Vector2(0, 1), new Vector2(600, -42 - BagRows * (BagCell + BagGap) - 6), new Vector2(470, 30)); heldText.rectTransform.pivot = new Vector2(0, 1);
            BuildShowcase();
            Refresh();
        }

        // ── 조작
        int Placed(string id) { int n = 0; foreach (var g in grid) if (g == id) n++; return n; }
        int Available(string id) => string.IsNullOrEmpty(id) ? 0 : inv.Count(id) - Placed(id);
        void ClickCell(int i, bool right)
        {
            if (Busy) return; ghost = null;
            if (right || string.IsNullOrEmpty(held) || grid[i] == held) grid[i] = null;
            else if (Available(held) > 0) grid[i] = held;
            else Toast.Show(PetSeal.Josa("남은 " + ItemDatabase.Get(held).name, "이", "가") + " 없어요");
            Refresh();
        }
        void ClickBag(int i, bool right)
        {
            if (Busy || i >= bagIds.Count) return; string id = bagIds[i];
            if (right) { ghost = null; for (int k = 0; k < 9; k++) if (string.IsNullOrEmpty(grid[k]) && Available(id) > 0) grid[k] = id; held = id; }
            else held = held == id ? null : id;
            Refresh();
        }
        void FromBook(ForgeMath.Recipe rc)
        {
            if (Busy) return;
            for (int k = 0; k < 9; k++) grid[k] = null;
            var lack = new List<string>();
            foreach (var kv in rc.Needs()) { int have = inv.Count(kv.Key); if (have < kv.Value) { var d = ItemDatabase.Get(kv.Key); lack.Add($"{(d != null ? d.name : kv.Key)} {kv.Value - have}개"); } }
            if (lack.Count == 0) { var lay = ForgeMath.Layout(rc); for (int k = 0; k < 9; k++) grid[k] = lay[k]; ghost = null; }
            else { ghost = rc; Toast.Show("풍선고래: 재료가 모자라~ " + string.Join(", ", lack)); }
            Refresh();
        }

        void TryCraft()
        {
            if (Busy) return; var rc = ForgeMath.Match(grid); if (rc == null) return;
            var def = ItemDatabase.Get(rc.result); if (def == null) return;
            if (!def.Stackable && inv.Contains(rc.result)) { Toast.Show("풍선고래: 그건 이미 갖고 있잖아~"); return; }
            for (int k = 0; k < 9; k++) if (!string.IsNullOrEmpty(grid[k])) inv.Remove(grid[k], 1);
            if (rc.showcase) { StartShowcase(rc); return; }
            Give(rc); popT = 0f;
            if (smith != null) smith.Play("Happy");
            ForgeFx.Strike(smith != null ? smith.transform.position + Vector3.up * 0.8f : Vector3.zero);
        }
        void Give(ForgeMath.Recipe rc)
        {
            var def = ItemDatabase.Get(rc.result); int left = inv.Add(rc.result, rc.count, silent: true);
            if (left > 0) ItemPickup.Spawn(rc.result, left, (smith != null ? smith.transform.position : Vector3.zero) + Vector3.up * 0.8f);
            Toast.Show($"{def.name} {(rc.count > 1 ? rc.count + "개 " : "")}완성!");
            Trim(); changed?.Invoke(); Refresh();
        }
        /// <summary>제작 뒤: 가진 수보다 많이 놓인 재료는 작업대에서 뺌(남은 걸로 한 번 더 만들 수 있으면 그대로).</summary>
        void Trim()
        {
            for (int k = 8; k >= 0; k--) { var id = grid[k]; if (!string.IsNullOrEmpty(id) && Available(id) < 0) grid[k] = null; }
        }

        public void Refresh()
        {
            bagIds.Clear();
            foreach (var s in inv.slots)
            {
                if (s == null || s.Empty) continue; var d = s.Def; if (d == null || d.type != ItemType.Material) continue;
                if (!bagIds.Contains(s.id)) bagIds.Add(s.id);
            }
            for (int i = 0; i < bag.Count; i++)
            {
                var (bg, ic, ct) = bag[i]; bool has = i < bagIds.Count;
                ic.enabled = has; ic.sprite = has ? ItemIcons.Get(ItemDatabase.Get(bagIds[i])) : null;
                int av = has ? Available(bagIds[i]) : 0; ct.text = has ? av.ToString() : "";
                ic.color = has && av <= 0 ? new Color(1, 1, 1, 0.35f) : Color.white;
                bg.color = has && bagIds[i] == held ? CellHot : new Color(1, 1, 1, 0.55f);
            }
            var ghostLay = ghost != null ? ForgeMath.Layout(ghost) : null;
            for (int k = 0; k < 9; k++)
            {
                string id = !string.IsNullOrEmpty(grid[k]) ? grid[k] : ghostLay != null ? ghostLay[k] : null;
                var d = ItemDatabase.Get(id); cellIcon[k].enabled = d != null; cellIcon[k].sprite = d != null ? ItemIcons.Get(d) : null;
                cellIcon[k].color = !string.IsNullOrEmpty(grid[k]) ? Color.white : new Color(1, 1, 1, 0.3f);   // 흐린 그림 = 제작법 안내
                cellBg[k].color = CellCol;
            }
            var rc = ForgeMath.Match(grid); var rd = rc != null ? ItemDatabase.Get(rc.result) : null;
            resultIcon.enabled = rd != null; resultIcon.sprite = rd != null ? ItemIcons.Get(rd) : null;
            bool owned = rd != null && !rd.Stackable && inv.Contains(rc.result);
            resultBg.color = rd == null ? CellCol : owned ? new Color(0.8f, 0.5f, 0.5f) : CellHot;
            resultName.text = rd == null ? (ghost != null ? "흐린 모양대로 놓아 봐요" : "") : owned ? rd.name + "\n(이미 있어요)" : rd.name + (rc.count > 1 ? " ×" + rc.count : "") + "\n눌러서 만들기";
            resultName.color = owned ? UITheme.Red : UITheme.Cream;
            var hd = ItemDatabase.Get(held); heldText.text = hd != null ? $"손에 든 재료: {hd.name}  (남은 {Available(held)})" : "재료를 눌러 손에 드세요";
        }

        // ── 특별 제작 연출(레이저 블레이더)
        static Sprite ringSprite;
        static Sprite Ring()
        {
            if (ringSprite != null) return ringSprite; const int N = 128; var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var px = new Color[N * N];
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) { float x = (i + .5f) / N * 2 - 1, y = (j + .5f) / N * 2 - 1, r = Mathf.Sqrt(x * x + y * y); float a = Mathf.Clamp01(1f - Mathf.Abs(r - 0.82f) / 0.12f); px[j * N + i] = new Color(1, 1, 1, a * a); }
            t.SetPixels(px); t.Apply(); ringSprite = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(.5f, .5f)); return ringSprite;
        }
        void BuildShowcase()
        {
            overlay = UIFactory.Box(root, "ShowcaseDim", new Color(0.02f, 0.04f, 0.08f, 0f)); UIFactory.Stretch(overlay.rectTransform, -30f); overlay.raycastTarget = false;
            for (int k = 0; k < 14; k++)
            {
                var ray = UIFactory.Box(root, "Ray" + k, new Color(0.5f, 0.85f, 1f, 0f)); ray.raycastTarget = false;
                UIFactory.At(ray.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(16f, 520f)); rays.Add(ray);
            }
            ring = UIFactory.Img(root, "Ring", Ring(), new Color(0.55f, 0.9f, 1f, 0f), false); ring.raycastTarget = false; UIFactory.At(ring.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(80, 80));
            bigIcon = UIFactory.Img(root, "BigIcon", null, Color.white, false); bigIcon.raycastTarget = false; bigIcon.preserveAspect = true; UIFactory.At(bigIcon.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(240, 240));
            banner = UIFactory.StyleDisplay(UIFactory.Txt(root, "", 54, UITheme.GoldBright, TextAnchor.MiddleCenter), true);
            UIFactory.At(banner.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -190), new Vector2(1000, 80));
            HideShowcase();
        }
        void HideShowcase()
        {
            overlay.color = new Color(overlay.color.r, overlay.color.g, overlay.color.b, 0f); ring.color = new Color(ring.color.r, ring.color.g, ring.color.b, 0f);
            bigIcon.enabled = false; banner.text = ""; foreach (var r in rays) r.color = new Color(r.color.r, r.color.g, r.color.b, 0f);
            foreach (var f in fly) if (f.img != null) Object.Destroy(f.img.gameObject); fly.Clear();
        }
        void StartShowcase(ForgeMath.Recipe rc)
        {
            animRc = rc; animT = 0f; strikes = 0; HideShowcase();
            for (int k = 0; k < 9; k++)                                                 // 재료 아이콘을 하나씩 떼어 내 가운데로 날려 보낼 준비
            {
                if (!cellIcon[k].enabled || string.IsNullOrEmpty(grid[k])) continue;
                var im = UIFactory.Img(root, "Fly" + k, cellIcon[k].sprite, Color.white, false); im.raycastTarget = false; im.preserveAspect = true;
                Vector2 p = (Vector2)root.InverseTransformPoint(cellIcon[k].rectTransform.position);
                im.rectTransform.anchorMin = im.rectTransform.anchorMax = new Vector2(0, 1); im.rectTransform.pivot = new Vector2(.5f, .5f);
                Vector2 local = p; im.rectTransform.anchoredPosition = local; im.rectTransform.sizeDelta = new Vector2(Cell - 24, Cell - 24);   // root 기준점 = 왼쪽 위
                fly.Add((im, local)); cellIcon[k].enabled = false;
            }
            for (int k = 0; k < 9; k++) grid[k] = null;
            if (smith != null) smith.Play("Think");
        }

        public void Tick(float dt)
        {
            if (popT >= 0f) { popT += dt; float u = Mathf.Clamp01(popT / 0.4f); resultBg.rectTransform.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(u * Mathf.PI)); if (u >= 1f) { popT = -1f; resultBg.rectTransform.localScale = Vector3.one; } }
            if (animT < 0f) return;
            animT += dt; float t = animT;
            Vector2 center = (Vector2)root.InverseTransformPoint(gridRoot.TransformPoint(new Vector3(gridRoot.rect.center.x, gridRoot.rect.center.y, 0)));
            // ① 0~0.7초: 재료가 가운데로 빨려 들어감, 칸이 푸르게 달아오름
            float a = Mathf.Clamp01(t / 0.7f), ea = a * a * (3 - 2 * a);
            foreach (var (img, from) in fly)
            {
                if (img == null) continue; img.rectTransform.anchoredPosition = Vector2.Lerp(from, center, ea);
                img.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, ea); img.color = new Color(1, 1, 1, 1f - Mathf.Clamp01((t - 0.55f) / 0.2f));
            }
            for (int k = 0; k < 9; k++) cellBg[k].color = Color.Lerp(CellCol, Glow, Mathf.Sin(Mathf.Clamp01(t / 1.3f) * Mathf.PI));
            // ② 0.7초 · 1.05초: 망치 두 번(불똥 · 화면 흔들림)
            if (strikes == 0 && t >= 0.7f) { strikes = 1; Hit(); }
            if (strikes == 1 && t >= 1.05f) { strikes = 2; Hit(); }
            // ③ 0.7~1.4초: 가운데에서 푸른 빛 고리가 퍼짐
            if (t >= 0.7f)
            {
                float r = Mathf.Clamp01((t - 0.7f) / 0.7f); ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = new Vector2(0, 1);
                ring.rectTransform.anchoredPosition = center; ring.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(60f, 560f, r); ring.color = new Color(0.55f, 0.9f, 1f, (1f - r) * 0.9f);
            }
            // ④ 1.4초~: 화면이 어두워지고 빛줄기 속에서 결과가 떠오름
            if (t >= 1.4f)
            {
                if (!bigIcon.enabled)
                {
                    var def = ItemDatabase.Get(animRc.result); bigIcon.enabled = true; bigIcon.sprite = ItemIcons.Get(def); banner.text = def.name + " 완성!";
                    if (smith != null) smith.Play("Cheer");
                    Vector3 wp = smith != null ? smith.transform.position + Vector3.up * 1.0f : Vector3.zero;
                    ForgeFx.Burst(wp, true); SparkBurst.Spawn(wp, Vector3.up, 80, 10f, new Color(0.45f, 0.9f, 1f)); ThirdPersonCamera.Shake(0.45f, 0.5f);
                }
                float u = Mathf.Clamp01((t - 1.4f) / 0.45f), over = 1f + 0.25f * Mathf.Sin(u * Mathf.PI);   // 튀어 오르며 커짐
                overlay.color = new Color(0.02f, 0.04f, 0.08f, 0.72f * Mathf.Clamp01((t - 1.4f) / 0.3f));
                bigIcon.rectTransform.localScale = Vector3.one * (u * over); bigIcon.color = Color.HSVToRGB(Mathf.Repeat(0.52f + 0.05f * Mathf.Sin(t * 4f), 1f), 0.15f, 1f);
                for (int k = 0; k < rays.Count; k++)
                {
                    var ry = rays[k]; ry.rectTransform.localRotation = Quaternion.Euler(0, 0, k * (360f / rays.Count) + t * 40f);
                    ry.color = Color.HSVToRGB(Mathf.Repeat(k / (float)rays.Count + t * 0.25f, 1f), 0.45f, 1f) * new Color(1, 1, 1, 0.38f * u);
                }
                banner.rectTransform.localScale = Vector3.one * (0.8f + 0.2f * Mathf.Clamp01((t - 1.5f) / 0.3f)); banner.color = new Color(UITheme.GoldBright.r, UITheme.GoldBright.g, UITheme.GoldBright.b, Mathf.Clamp01((t - 1.5f) / 0.3f));
            }
            // ⑤ 3.4초: 가방에 넣고 끝
            if (t >= 3.4f)
            {
                var rc = animRc; animT = -1f; HideShowcase();
                Give(rc); Quests.AnnounceNow("특별 제작!", ItemDatabase.Get(rc.result).name);
            }
        }
        void Hit()
        {
            if (smith != null) smith.Play("Hammer");
            ForgeFx.Strike(smith != null ? smith.transform.position + Vector3.up * 0.7f : Vector3.zero);
            SparkBurst.Spawn(smith != null ? smith.transform.position + Vector3.up * 0.7f : Vector3.zero, Vector3.up, 24, 6f, new Color(0.5f, 0.88f, 1f));
        }
    }
}
