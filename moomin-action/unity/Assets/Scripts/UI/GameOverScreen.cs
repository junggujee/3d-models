using System;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 쓰러졌을 때: ① 화면이 조금 어두워지며 붉은 "패배...." 띠 + 눈이 ×× 인 민미 얼굴(천천히 줄어들며 또렷해짐, 약 3초)
    /// → ② 띠가 사라지고 결과 화면이 떠오름: 여정의 기록(처치 수·합 승률·최고 위력·도달 웨이브·레벨·플레이 시간, 숫자는 0에서 차오름)
    ///    + [마지막 저장에서 다시](어느 기록·몇 분 전인지) / [타이틀로]. 저장이 없으면 [처음부터 다시].
    /// HUD 가 만들고 매 프레임 Update. 결과가 뜨면 게임을 멈추고(UIState.Paused) 커서를 보여 줌.
    /// </summary>
    public class GameOverScreen
    {
        const float BandIn = 0.9f, BandHold = 3.0f, ResultIn = 0.8f;
        RectTransform root, panel, face; Image shade, band, faceGlow; RawImage faceImg; Text bandText; CanvasGroup panelGroup;
        /// <summary>밝기(예전보다 한 단계 밝게): 화면 덮개 · 띠 · 결과 화면 덮개.</summary>
        const float ShadeBand = .40f, BandFill = .55f, ShadeResults = .58f;
        static readonly Color DefeatRed = new Color(.93f, .24f, .2f), SubRed = new Color(.96f, .45f, .38f);
        Text sub; readonly Text[] values = new Text[6]; int[] targets = new int[6]; string[] suffix = new string[6];
        struct Choice { public Image bar; public Text chev, label, detail; public Action act; }
        Choice[] choices; int sel; float t = -1; bool resultsShown;
        CharacterStats player;

        public bool Active => t >= 0;

        public void Build(Transform parent, CharacterStats player)
        {
            this.player = player; var pal = UITheme.InGame;
            root = NewRect("GameOver", parent); Stretch(root);
            shade = Box(root, "Shade", new Color(0, 0, 0, 0)); Stretch(shade.rectTransform);
            band = Img(root, "Band", UITheme.Highlight, new Color(0, 0, 0, 0)); At(band.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 20), new Vector2(2400, 260));
            var bandFill = Box(band.transform, "Fill", new Color(.08f, .05f, .05f, BandFill)); Stretch(bandFill.rectTransform, 2); bandFill.transform.SetAsFirstSibling();
            bandText = Txt(band.transform, "패배....", 140, new Color(DefeatRed.r, DefeatRed.g, DefeatRed.b, 0), TextAnchor.MiddleCenter, FontStyle.Bold, "Text"); Stretch(bandText.rectTransform);
            bandText.gameObject.AddComponent<Outline>().effectColor = new Color(.2f, 0, 0, .8f);
            // 눈이 ×× 인 민미 얼굴(Resources/UI/GameOver/DefeatFace.png — blender/render_defeat_face.py + tools/defeat_face.py): 띠 위로 떠오름, 결과 화면에서는 왼쪽 위로
            face = NewRect("Face", root); At(face, new Vector2(.5f, .5f), new Vector2(0, 300), new Vector2(300, 300));
            faceGlow = Img(face, "Glow", UITheme.SoftCircle, new Color(1f, .55f, .45f, 0), false); Stretch(faceGlow.rectTransform, -40);
            faceImg = new GameObject("Img", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>(); faceImg.transform.SetParent(face, false); Stretch(faceImg.rectTransform); faceImg.raycastTarget = false;
            faceImg.texture = Resources.Load<Texture2D>("UI/GameOver/DefeatFace"); face.gameObject.SetActive(faceImg.texture != null);

            panel = Img(root, "Results", pal.frame, Color.white, true, true).rectTransform; At(panel, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1060, 820));
            panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
            var lift = Box(panel, "Lift", new Color(.45f, .36f, .25f, .28f)); Stretch(lift.rectTransform, 10);   // 바탕을 조금 밝게(따뜻한 양피지 빛)
            var title = Txt(panel, "여정의 기록", 50, pal.bright, TextAnchor.MiddleCenter, FontStyle.Bold); Set(title.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -30), new Vector2(900, 66));
            sub = Txt(panel, "", 26, SubRed, TextAnchor.MiddleCenter); Set(sub.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -96), new Vector2(900, 38));
            var line = Img(panel, "Divider", UITheme.Highlight, Color.white); Set(line.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -140), new Vector2(920, 12));
            string[] labels = { "처치한 적", "합 승률", "최고 위력", "도달 웨이브", "레벨", "플레이 시간" };
            for (int i = 0; i < 6; i++)
            {
                int col = i % 2, row = i / 2; float x = col == 0 ? -240 : 240, y = -200 - row * 128;
                var cell = Img(panel, "Cell" + i, UITheme.Slot, new Color(1, 1, 1, .85f)); var cellLift = Box(cell.transform, "Lift", new Color(.5f, .42f, .3f, .22f)); Stretch(cellLift.rectTransform, 2); Set(cell.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(x, y), new Vector2(440, 112));
                var lab = Txt(cell.transform, labels[i], 24, UITheme.Cream, TextAnchor.MiddleCenter); Set(lab.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -8), new Vector2(420, 34));
                values[i] = Txt(cell.transform, "", 46, pal.bright, TextAnchor.MiddleCenter, FontStyle.Bold); Set(values[i].rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 8), new Vector2(420, 62));
            }
            choices = new Choice[2];
            for (int i = 0; i < 2; i++)
            {
                int idx = i; float y = -604 - i * 92;
                var row = Box(panel, "Choice" + i, new Color(0, 0, 0, 0)); row.raycastTarget = true; Set(row.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, y), new Vector2(820, 80));
                var bar = Img(row.transform, "Bar", UITheme.Highlight, Color.white); Stretch(bar.rectTransform);
                var chev = Txt(row.transform, "»", 42, pal.accent, TextAnchor.MiddleCenter, FontStyle.Bold); Set(chev.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(30, 2), new Vector2(60, 70));
                var lab = Txt(row.transform, "", 34, pal.text); Set(lab.rectTransform, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, .5f), new Vector2(100, 10), new Vector2(-120, 44));
                var det = Txt(row.transform, "", 21, new Color(.8f, .75f, .64f)); Set(det.rectTransform, new Vector2(0, .5f), new Vector2(1, .5f), new Vector2(0, .5f), new Vector2(102, -22), new Vector2(-120, 30));
                OnHover(row.gameObject, () => { sel = idx; Refresh(); }); OnClick(row.gameObject, () => { sel = idx; choices[idx].act?.Invoke(); });
                choices[i] = new Choice { bar = bar, chev = chev, label = lab, detail = det };
            }
            root.gameObject.SetActive(false);
        }

        /// <summary>쓰러진 순간 부름.</summary>
        public void Show()
        {
            if (Active) return; t = 0; resultsShown = false; root.gameObject.SetActive(true); panel.gameObject.SetActive(false);
            var gm = GameManager.Instance; int wave = gm != null ? gm.Wave : 0;
            int level = player != null && player.TryGetComponent<Experience>(out var xp) ? xp.level : 1;
            targets = new[] { RunStats.Kills, 0, RunStats.BestPower, wave, level, 0 };
            suffix = new[] { "", "", "", "", "", "" };
            values[1].text = SaveFormat.WinRate(RunStats.ClashWins, RunStats.ClashLosses) + (RunStats.ClashWins + RunStats.ClashLosses > 0 ? $"   <size=24>{RunStats.ClashWins}승 {RunStats.ClashLosses}패</size>" : "");
            values[5].text = SaveFormat.PlayTime(SaveSystem.PlayTime);
            sub.text = wave > 0 ? $"웨이브 {wave}에서 패배했다" : "첫 웨이브를 넘기지 못했다";
            int latest = SaveSystem.LatestSlot();
            if (latest >= 0)
            {
                var d = SaveSystem.Peek(latest);
                choices[0].label.text = "마지막 저장에서 다시"; choices[0].detail.text = $"{SaveFormat.SlotName(latest)}  ·  {SaveFormat.Summary(d.level, d.place, d.wave)}  ·  {SaveFormat.Ago(d.savedAt, DateTime.UtcNow)}";
                choices[0].act = () => SaveSystem.Load(latest);
            }
            else { choices[0].label.text = "처음부터 다시"; choices[0].detail.text = "저장된 기록이 없다 — 새 여정을 시작한다"; choices[0].act = SaveSystem.StartNewGame; }
            choices[1].label.text = "타이틀로"; choices[1].detail.text = "기록은 저장된 곳까지 남아 있다"; choices[1].act = SaveSystem.ToTitle;
            sel = 0; Refresh();
        }

        void Refresh()
        {
            for (int i = 0; i < choices.Length; i++)
            {
                bool s = i == sel; choices[i].bar.enabled = s; choices[i].chev.color = s ? UITheme.GoldBright : UITheme.GoldDim;
                choices[i].label.color = s ? Color.white : UITheme.Cream;
            }
        }

        /// <summary>
        /// 얼굴: ① 띠 위로 톡 떨어지며 나타나 기운 없이 좌우로 흔들림 → ② 결과 화면이 뜨면 패널 왼쪽 위로 작아지며 옮겨 감(패널 모서리에 걸침).
        /// </summary>
        void UpdateFace()
        {
            if (!face.gameObject.activeSelf) return;
            float appear = Mathf.Clamp01((t - .25f) / .7f), ea = 1f - (1f - appear) * (1f - appear) * (1f - appear);
            float r = Mathf.Clamp01((t - BandHold - .2f) / ResultIn), er = r * r * (3 - 2 * r);
            var bandPos = new Vector2(0, 300 + 40 * (1 - ea)); var panelPos = new Vector2(-445, 330);
            face.anchoredPosition = Vector2.Lerp(bandPos, panelPos + panel.anchoredPosition, er);
            float size = Mathf.Lerp(300f, 210f, er); face.sizeDelta = new Vector2(size, size);
            face.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(Mathf.Sin(t * 1.6f) * 4f, -8f, er));
            faceImg.color = new Color(1, 1, 1, ea); faceGlow.color = new Color(1f, .55f, .45f, .35f * ea * (1 - .5f * er));
            if (r > 0f) face.SetAsLastSibling();   // 결과 패널 위에 걸침
        }

        public void Update(float dt)
        {
            if (!Active) return; t += dt;
            // ① 쓰러짐 띠
            float a = Mathf.Clamp01(t / BandIn), fadeBand = 1f - Mathf.Clamp01((t - BandHold) / 0.6f);
            shade.color = new Color(0, 0, 0, Mathf.Lerp(0, ShadeBand, Mathf.Clamp01(t / 2f)));
            band.color = new Color(1, 1, 1, .9f * a * fadeBand); band.transform.GetChild(0).GetComponent<Image>().color = new Color(.08f, .05f, .05f, BandFill * a * fadeBand);
            float s = Mathf.Lerp(1.12f, 1f, 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 2.6f), 3f)); bandText.rectTransform.localScale = new Vector3(s, s, 1);
            bandText.color = new Color(DefeatRed.r, DefeatRed.g, DefeatRed.b, a * fadeBand);
            UpdateFace();
            // ② 결과
            float r = t - BandHold - 0.2f; if (r < 0) return;
            if (!resultsShown) { resultsShown = true; panel.gameObject.SetActive(true); UIState.SetPaused(true); }
            float k = Mathf.Clamp01(r / ResultIn), e = 1f - (1f - k) * (1f - k) * (1f - k);
            panelGroup.alpha = k; panel.anchoredPosition = new Vector2(0, -50 * (1 - e)); shade.color = new Color(0, 0, 0, ShadeBand + (ShadeResults - ShadeBand) * k);
            float count = Mathf.Clamp01((r - .3f) / 1.1f); count = 1f - (1f - count) * (1f - count);   // 숫자 차오름
            foreach (int i in new[] { 0, 2, 3, 4 }) values[i].text = Mathf.RoundToInt(targets[i] * count) + suffix[i];
            if (k < 1f) return;
            var nav = InputAdapter.NavPressed(); if (nav.y != 0) { sel = (sel - nav.y + choices.Length) % choices.Length; Refresh(); }
            if (InputAdapter.ConfirmPressed() && !SceneFader.Busy) choices[sel].act?.Invoke();
        }
    }
}
