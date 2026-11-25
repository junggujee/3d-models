using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// NPC 대화창(사용자 참고 이미지 구성) — 그림: tools/dialogue_art.py → Resources/UI/Dialogue, 미리보기: tools/dialogue_mock.py → previews/dialogue_mock.png
    ///  · 화면 아래 가운데 밝은 양피지 판(1560×400), 왼쪽 위 붉은 보석이 박힌 어두운 장식 모서리 + 나머지 모서리 덩굴
    ///  · 판 위 가운데 제목 띠(퀘스트 제목·장소 — 빛의 계승자 Bold, 크림→베이지)
    ///  · 왼쪽: 캐릭터 2D 초상화(표정마다 — Resources/UI/Portraits/<이름>_<표정>.png), 머리가 틀 위로 살짝 튀어나옴, 표정이 바뀌면 톡 튐
    ///  · 오른쪽: 글상자 + 이름표 + 한 글자씩 나오는 대사(나눔명조) + 위·아래 화살표(아래 화살표가 까딱이면 '다음')
    ///  · 선택지(수락/거절 등): 오른쪽 글상자 위, 오른쪽 정렬 버튼 — ←→/A D/1 2/마우스로 고르고 Space·Enter·F·클릭
    /// 게임은 멈춤(휴식 메뉴와 같은 MenuPage). Esc 로 닫으면 대화 끝(선택하지 않은 부탁은 다음에 다시).
    /// 레이아웃 숫자는 tools/dialogue_art.py · dialogue_mock.py 와 같아야 함.
    /// </summary>
    public class DialogueBox : MenuPage
    {
        const float PW = 1560, PH = 400, Bottom = 36, TBX = 460, TBW = 1070, TBH = 300, BtnW = 340, BtnH = 68, BtnGap = 18;
        const float CharsPerSecond = 42f;
        static readonly Color BodyColor = new Color(0.24f, 0.155f, 0.09f);

        RectTransform panel, portraitRt; Image portrait, arrowDown, arrowUp; Text title, nameText, body, hint; Image banner;
        readonly List<(RectTransform rt, Image bg, Text label, Image star)> buttons = new();
        readonly Dictionary<string, Sprite> faces = new();
        Dialogue dlg; DialogueLine line; int index = -1, sel; float typed, popT, openT; string full = ""; int visibleLen; bool typing, choosing, finished;

        public override void Build(UIRoot ui, Transform canvas)
        {
            base.Build(ui, canvas);
            var r = NewRect("Dialogue", canvas); Stretch(r); root = r.gameObject;
            var shade = Img(r, "Shade", UITheme.BottomShade, Color.white, false); Set(shade.rectTransform, Vector2.zero, new Vector2(1, .55f), new Vector2(.5f, 0), Vector2.zero, Vector2.zero);

            panel = NewRect("Panel", r); Set(panel, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, Bottom), new Vector2(PW, PH));
            var bgSprite = UITheme.Art("Dialogue", "Panel");
            var bg = Img(panel, "Paper", bgSprite != null ? bgSprite : UITheme.Frame, Color.white, bgSprite == null, true);
            Stretch(bg.rectTransform); if (bgSprite != null) { bg.rectTransform.offsetMin = new Vector2(-20, -20); bg.rectTransform.offsetMax = new Vector2(20, 20); }   // 그림 바깥 20 = 그림자
            OnClick(bg.gameObject, Advance);

            // 왼쪽: 초상화 틀 + 초상화(위로 튀어나옴) + 아래·옆 테두리
            var bl = Vector2.zero;
            var pbg = Img(panel, "PortraitBg", UITheme.Art("Dialogue", "PortraitBg"), Color.white, false); Set(pbg.rectTransform, bl, bl, bl, new Vector2(30, 30), new Vector2(400, 340));
            portrait = Img(panel, "Portrait", null, Color.white, false); portrait.preserveAspect = true; portraitRt = portrait.rectTransform; Set(portraitRt, bl, bl, new Vector2(.5f, 0), new Vector2(230, 30), new Vector2(358, 430));
            var pfr = Img(panel, "PortraitFrame", UITheme.Art("Dialogue", "PortraitFrame"), Color.white, false); Set(pfr.rectTransform, bl, bl, bl, new Vector2(30, 30), new Vector2(400, 340));

            // 모서리 장식
            var gem = Img(panel, "CornerGem", UITheme.Art("Dialogue", "CornerGem"), Color.white, false); Set(gem.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(46, -46), new Vector2(120, 120));
            var corner = UITheme.Art("Dialogue", "Corner");
            foreach (var (anchor, pos, scale) in new[] { (new Vector2(1, 1), new Vector2(-24, -24), new Vector3(-1, 1, 1)), (new Vector2(0, 0), new Vector2(24, 24), new Vector3(1, -1, 1)), (new Vector2(1, 0), new Vector2(-24, 24), new Vector3(-1, -1, 1)) })
            { var c = Img(panel, "Corner", corner, Color.white, false); Set(c.rectTransform, anchor, anchor, new Vector2(.5f, .5f), pos, new Vector2(64, 64)); c.rectTransform.localScale = scale; c.enabled = corner != null; }

            // 오른쪽: 글상자 + 이름표 + 대사 + 화살표
            var tb = Img(panel, "TextBox", UITheme.Art("Dialogue", "TextBox"), Color.white, false); Set(tb.rectTransform, bl, bl, bl, new Vector2(TBX, 30), new Vector2(TBW, TBH));
            if (tb.sprite == null) { tb.sprite = UITheme.Slot; tb.type = Image.Type.Sliced; tb.color = new Color(1, 1, 1, .9f); }
            var plate = Img(panel, "NamePlate", UITheme.Art("Dialogue", "NamePlate", new Vector4(30, 0, 30, 0)), Color.white, true); Set(plate.rectTransform, bl, bl, new Vector2(0, .5f), new Vector2(TBX + 24, 30 + TBH), new Vector2(250, 56));
            nameText = StyleHeading(Txt(plate.transform, "", 30, Color.white, TextAnchor.MiddleCenter)); Stretch(nameText.rectTransform); nameText.rectTransform.offsetMin = new Vector2(20, 0); nameText.rectTransform.offsetMax = new Vector2(-20, 0);
            var ng = nameText.gameObject.AddComponent<UIGradient>(); ng.top = new Color(1f, 0.96f, 0.86f); ng.bottom = new Color(0.89f, 0.77f, 0.57f);
            body = Txt(tb.transform, "", 34, BodyColor, TextAnchor.UpperLeft, FontStyle.Normal, "Body"); body.font = UITheme.BodyFont; body.lineSpacing = 1.22f;
            Stretch(body.rectTransform); body.rectTransform.offsetMin = new Vector2(44, 30); body.rectTransform.offsetMax = new Vector2(-90, -46);
            if (body.TryGetComponent<Shadow>(out var bsh)) { bsh.effectColor = new Color(1, 1, 1, 0.35f); bsh.effectDistance = new Vector2(1, -1); }   // 밝은 종이 위 어두운 글씨: 옅은 양각
            var arrow = UITheme.Art("Dialogue", "Arrow");
            arrowUp = Img(tb.transform, "ArrowUp", arrow, new Color(1, 1, 1, .55f), false); Set(arrowUp.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(.5f, .5f), new Vector2(-34, -34), new Vector2(28, 28)); arrowUp.rectTransform.localScale = new Vector3(1, -1, 1);
            arrowDown = Img(tb.transform, "ArrowDown", arrow, Color.white, false); Set(arrowDown.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(.5f, .5f), new Vector2(-34, 32), new Vector2(28, 28));
            arrowUp.enabled = arrowDown.enabled = arrow != null;

            // 제목 띠(판 위 가운데)
            banner = Img(panel, "TitleBanner", UITheme.Art("Dialogue", "TitleBanner", new Vector4(44, 0, 44, 0)), Color.white, true); Set(banner.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(.5f, .5f), Vector2.zero, new Vector2(640, 78));
            title = StyleDisplay(Txt(banner.transform, "", 34, Color.white, TextAnchor.MiddleCenter)); Stretch(title.rectTransform); title.rectTransform.offsetMin = new Vector2(50, 0); title.rectTransform.offsetMax = new Vector2(-50, 0);

            // 선택지(글상자 위 오른쪽, 오른쪽부터)
            for (int i = 0; i < 3; i++)
            {
                var b = Img(panel, "Choice" + i, UITheme.Art("Dialogue", "Button", new Vector4(30, 0, 30, 0)), Color.white, true, true);
                Set(b.rectTransform, bl, bl, new Vector2(1, .5f), new Vector2(TBX + TBW - i * (BtnW + BtnGap), PH + 39 + 18 + BtnH / 2), new Vector2(BtnW, BtnH));
                var label = StyleHeading(Txt(b.transform, "", 30, Color.white, TextAnchor.MiddleCenter)); Stretch(label.rectTransform); label.rectTransform.offsetMin = new Vector2(44, 0); label.rectTransform.offsetMax = new Vector2(-30, 0);
                var lg = label.gameObject.AddComponent<UIGradient>(); lg.top = new Color(1f, 0.96f, 0.88f); lg.bottom = new Color(0.84f, 0.74f, 0.58f);
                var star = Img(b.transform, "Star", UITheme.Art("Ornament", "Sparkle"), Color.white, false); Set(star.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(.5f, .5f), new Vector2(36, 0), new Vector2(26, 26));
                int k = i; OnHover(b.gameObject, () => { if (choosing) { sel = SlotToChoice(k); Highlight(); } }); OnClick(b.gameObject, () => { if (choosing) Pick(SlotToChoice(k)); });
                buttons.Add((b.rectTransform, b, label, star)); b.gameObject.SetActive(false);
            }

            hint = Txt(panel, "Space · F · 클릭  다음      ← →  고르기      Esc  닫기", 20, new Color(0.92f, 0.88f, 0.8f), TextAnchor.UpperRight); Set(hint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(-30, -6), new Vector2(900, 28));
            root.SetActive(false);
        }

        /// <summary>대화 시작(UIRoot.StartDialogue 가 Open 전에 부름).</summary>
        public void Begin(Dialogue d) { dlg = d; index = -1; finished = false; }

        public override void Open()
        {
            base.Open(); openT = 0f;
            if (dlg == null) { host.Back(); return; }
            title.text = string.IsNullOrEmpty(dlg.title) ? "" : dlg.title; banner.gameObject.SetActive(title.text.Length > 0);
            if (index < 0) Show(0);
        }

        public override void Close()
        {
            base.Close(); if (dlg == null) return;
            var d = dlg; dlg = null; line = null; choosing = false;
            if (d.npc != null) d.npc.EndTalk();
            d.onEnd?.Invoke();
        }

        void Show(int i)
        {
            if (dlg == null || i < 0 || i >= dlg.lines.Count) { Finish(); return; }
            index = i; line = dlg.lines[i]; choosing = false; foreach (var b in buttons) b.rt.gameObject.SetActive(false);
            nameText.text = string.IsNullOrEmpty(line.speaker) ? dlg.speaker : line.speaker;
            SetFace(line.face);
            full = line.text ?? ""; visibleLen = VisibleLength(full); typed = 0f; typing = visibleLen > 0; body.text = typing ? "" : full;
            if (dlg.npc != null && !string.IsNullOrEmpty(line.anim)) dlg.npc.Play(line.anim);
            if (dlg.playAnim != null && !string.IsNullOrEmpty(line.anim)) dlg.playAnim(line.anim);
            line.onShow?.Invoke();
            if (!typing) OnTyped();
        }

        void SetFace(string face)
        {
            var sp = Face(dlg.portrait, string.IsNullOrEmpty(face) ? "normal" : face); if (sp == null) sp = Face(dlg.portrait, "normal");
            if (portrait.sprite != sp) popT = 0.18f;
            portrait.sprite = sp; portrait.enabled = sp != null;
        }
        Sprite Face(string who, string face)
        {
            string key = who + "_" + face; if (faces.TryGetValue(key, out var s)) return s;
            var tex = Resources.Load<Texture2D>("UI/Portraits/" + key);
            if (tex != null) { tex.wrapMode = TextureWrapMode.Clamp; s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, 0), 100f); }
            if (s == null && face != "normal") s = Face(who, "normal");   // 그 표정 그림이 없으면 기본 얼굴
            faces[key] = s; return s;
        }

        void OnTyped()
        {
            typing = false; body.text = full;
            if (line != null && line.choices != null && line.choices.Length > 0)
            {
                choosing = true; sel = 0;
                for (int c = 0; c < line.choices.Length; c++) if (line.choices[c].primary) { sel = c; break; }
                int n = Mathf.Min(line.choices.Length, buttons.Count);
                for (int s = 0; s < buttons.Count; s++)
                {
                    bool on = s < n; buttons[s].rt.gameObject.SetActive(on); if (!on) continue;
                    buttons[s].label.text = line.choices[SlotToChoice(s)].text;
                }
                Highlight();
            }
        }

        /// <summary>버튼 칸(오른쪽부터 0,1,2) ↔ 선택지 번호(왼쪽부터 0,1,2): 화면에서 왼→오 순서가 선택지 순서가 되게.</summary>
        int SlotToChoice(int slot) { int n = line != null && line.choices != null ? Mathf.Min(line.choices.Length, buttons.Count) : 0; return n - 1 - slot; }

        void Highlight()
        {
            if (line == null || line.choices == null) return; int n = Mathf.Min(line.choices.Length, buttons.Count);
            for (int s = 0; s < n; s++)
            {
                int c = SlotToChoice(s); var ch = line.choices[c]; bool on = c == sel;
                string art = on ? (ch.primary ? "ButtonPrimary" : "ButtonHover") : (ch.primary ? "ButtonPrimary" : "Button");
                var sp = UITheme.Art("Dialogue", art, new Vector4(30, 0, 30, 0)); if (sp != null) buttons[s].bg.sprite = sp;
                buttons[s].bg.color = on || ch.primary ? Color.white : new Color(1, 1, 1, .88f);
                buttons[s].star.enabled = on && buttons[s].star.sprite != null; buttons[s].rt.localScale = Vector3.one * (on ? 1.04f : 1f);
            }
        }

        void Pick(int c)
        {
            if (!choosing || line == null || line.choices == null || c < 0 || c >= line.choices.Length) return;
            var ch = line.choices[c]; choosing = false; foreach (var b in buttons) b.rt.gameObject.SetActive(false);
            bool ok = ch.act == null || ch.act();
            string next = ok || string.IsNullOrEmpty(ch.failNext) ? ch.next : ch.failNext;
            int to = dlg != null ? dlg.IndexOf(next) : -1; Show(to >= 0 ? to : index + 1);
        }

        void Advance()
        {
            if (dlg == null || line == null || openT < 0.15f) return;
            if (typing) { OnTyped(); return; }
            if (choosing) { Pick(sel); return; }
            if (line.end) { Finish(); return; }
            int to = dlg.IndexOf(line.next); Show(to >= 0 ? to : index + 1);
        }

        void Finish() { if (finished) return; finished = true; host.Back(); }

        public override void Tick()
        {
            float dt = Time.unscaledDeltaTime; openT += dt;
            if (typing)
            {
                typed += dt * CharsPerSecond; int n = Mathf.Min(visibleLen, Mathf.FloorToInt(typed)); body.text = Visible(full, n);
                if (n >= visibleLen) OnTyped();
            }
            // 초상화: 표정이 바뀌면 톡(살짝 커졌다 돌아옴) + 숨쉬기
            popT = Mathf.Max(0, popT - dt); float pop = 1f + 0.05f * Mathf.Sin(popT / 0.18f * Mathf.PI);
            portraitRt.localScale = new Vector3(pop, pop * (1f + 0.008f * Mathf.Sin(Time.unscaledTime * 2.2f)), 1);
            arrowDown.rectTransform.anchoredPosition = new Vector2(-34, 32 + (typing || choosing ? 0 : Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 6f));
            arrowDown.color = new Color(1, 1, 1, typing || choosing ? .3f : 1f);

            if (choosing && line != null && line.choices != null)
            {
                int n = line.choices.Length; var nav = InputAdapter.NavPressed(); int dx = nav.x != 0 ? nav.x : -nav.y;
                if (dx != 0) { sel = (sel + dx + n) % n; Highlight(); }
                int q = InputAdapter.QuickPressed(); if (q >= 0 && q < n) { sel = q; Highlight(); Pick(q); return; }
            }
            if (InputAdapter.ConfirmPressed() || InputAdapter.InteractPressed()) Advance();
        }

        // ── 서식 글자(<color> 등)를 깨지 않고 n 글자만 보이기 ──
        static int VisibleLength(string s) { int n = 0; bool tag = false; foreach (char c in s) { if (c == '<') tag = true; else if (c == '>' && tag) tag = false; else if (!tag) n++; } return n; }
        static readonly Stack<string> open = new();
        static string Visible(string s, int n)
        {
            var sb = new StringBuilder(); open.Clear(); int shown = 0;
            for (int i = 0; i < s.Length && shown < n; i++)
            {
                if (s[i] == '<')
                {
                    int e = s.IndexOf('>', i); if (e < 0) break; string t = s.Substring(i, e - i + 1); sb.Append(t);
                    if (t.StartsWith("</")) { if (open.Count > 0) open.Pop(); } else { int sp = t.IndexOfAny(new[] { '=', '>', ' ' }, 1); open.Push(t.Substring(1, sp - 1)); }
                    i = e; continue;
                }
                sb.Append(s[i]); shown++;
            }
            while (open.Count > 0) sb.Append("</").Append(open.Pop()).Append('>');
            return sb.ToString();
        }
    }
}
