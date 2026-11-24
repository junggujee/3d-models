using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 왼쪽 위 체력바 + 경험치바(중세 판타지 틀 · 꼰 끈 장식).
    ///  · 체력: "체력 28,347/39,100" + 줄었을 때만 붉은 [72%] 꼬리표. 맞으면 줄어든 만큼 <b>빨간 조각</b>이 남았다가(0.45초) 줄어들며 사라짐(DamageChunk).
    ///  · 경험치: 레벨 원판 + "경험치 2.6M/14.44M". 얻은 만큼 밝게 번쩍이며 차오르고, 레벨 업이면 끝까지 찼다가 새 레벨에서 다시.
    /// 비율·조각 계산은 순수 로직(Core/VitalsMath.cs) → tools/status_tests 로 검증.
    /// </summary>
    public class VitalBars
    {
        public static readonly Color HpColor = new Color(.36f, .86f, .22f), ChunkColor = new Color(.95f, .12f, .1f), ExpColor = new Color(1f, .8f, .2f);
        CharacterStats hp; Experience xp;
        RectTransform hpFill, hpChunk, expFill, expGain, barrierFill; Image hpChunkImg, hpFillImg, expGainImg; Text hpLabel, hpPct, expLabel, lvText; RectTransform hpTag, hpRoot;
        readonly DamageChunk chunk = new(); float expShown, expGainT; int shownLevel;

        public void Build(Transform parent, CharacterStats stats, Experience exp)
        {
            hp = stats; xp = exp; var tl = new Vector2(0, 1);
            // ── 체력
            hpRoot = Bar(parent, "HP", new Vector2(40, -22), new Vector2(600, 52), HpColor, out hpFill, out hpFillImg, out hpChunk, out hpChunkImg);
            var bar = Box(hpFill.parent, "Barrier", new Color(1f, 0.99f, 0.95f, 0.55f)); barrierFill = bar.rectTransform; barrierFill.anchorMin = Vector2.zero; barrierFill.anchorMax = new Vector2(0, 1); barrierFill.offsetMin = barrierFill.offsetMax = Vector2.zero;   // 보호막(하얀 솜털 막)
            var fluff = Img(bar.transform, "Fluff", UITheme.SoftCircle, new Color(1, 1, 1, .5f), false); Stretch(fluff.rectTransform, -4);
            hpLabel = Txt(hpRoot, "", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(hpLabel.rectTransform); hpLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            var tag = Img(hpRoot, "Tag", BarArt.Tag, Color.white, true); hpTag = tag.rectTransform; Set(hpTag, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, .5f), Vector2.zero, new Vector2(74, 30));
            hpPct = Txt(tag.transform, "", 21, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(hpPct.rectTransform); hpPct.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .8f);
            Bindings(hpRoot, 66f);
            // ── 경험치(왼쪽에 레벨 원판)
            var expRoot = Bar(parent, "EXP", new Vector2(92, -80), new Vector2(548, 30), ExpColor, out expFill, out _, out expGain, out expGainImg);
            expGainImg.color = new Color(1f, .98f, .8f, 0f);
            expLabel = Txt(expRoot, "", 19, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(expLabel.rectTransform); expLabel.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            Bindings(expRoot, 42f);
            var badge = Img(parent, "LevelBadge", BarArt.Badge, Color.white, false); Set(badge.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(64, -99), new Vector2(54, 54));
            lvText = Txt(badge.transform, "", 22, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(lvText.rectTransform); lvText.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            chunk.Set(hp.HealthRatio); chunk.Advance(10f); expShown = xp != null ? xp.Ratio : 0f; shownLevel = xp != null ? xp.level : 1;
            if (xp != null) xp.Gained += _ => expGainT = 0.6f;
        }

        /// <summary>틀 하나: 어두운 홈 → (조각) → 채움 → 틀. 채움·조각은 Inner 안에서 anchor 로 길이 조절.</summary>
        static RectTransform Bar(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor, out RectTransform fill, out Image fillImg, out RectTransform chunk, out Image chunkImg)
        {
            var tl = new Vector2(0, 1); var root = NewRect(name, parent); Set(root, tl, tl, tl, pos, size);
            float inset = size.y > 40 ? 7f : 6f;
            var back = Img(root, "Back", BarArt.Back, Color.white, false); Stretch(back.rectTransform, inset);
            var inner = NewRect("Inner", root); Stretch(inner, inset);
            chunkImg = Img(inner, "Chunk", BarArt.Fill, ChunkColor, false); chunk = chunkImg.rectTransform; chunk.anchorMin = Vector2.zero; chunk.anchorMax = Vector2.one; chunk.offsetMin = chunk.offsetMax = Vector2.zero;
            fillImg = Img(inner, "Fill", BarArt.Fill, fillColor, false); fill = fillImg.rectTransform; fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            var frame = Img(root, "Frame", BarArt.Frame, Color.white, true); Stretch(frame.rectTransform);
            if (!BarArt.HasArt) frame.type = Image.Type.Sliced;
            return root;
        }
        /// <summary>꼰 끈 감개 두 개(틀 위에 감은 장식).</summary>
        static void Bindings(RectTransform root, float h)
        {
            var sp = BarArt.Binding; if (sp == null) return;
            foreach (float x in new[] { .16f, .86f })
            { var b = Img(root, "Binding", sp, Color.white, false); Set(b.rectTransform, new Vector2(x, .5f), new Vector2(x, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(h * .5f, h)); b.transform.SetSiblingIndex(root.childCount - 1); }
        }

        public void Update(float dt)
        {
            // 체력: 비율 → 조각
            float r = hp.HealthRatio; chunk.Set(r); chunk.Advance(dt);
            hpFill.anchorMax = new Vector2(r, 1);
            hpChunk.anchorMin = new Vector2(r, 0); hpChunk.anchorMax = new Vector2(Mathf.Max(r, chunk.Top), 1);
            var cc = Color.Lerp(ChunkColor, Color.white, chunk.Flash * .6f); cc.a = chunk.Alpha; hpChunkImg.color = cc;
            hpFillImg.color = Color.Lerp(HpColor, new Color(.95f, .3f, .2f), r < .25f ? (.5f + .5f * Mathf.Sin(Time.unscaledTime * 8f)) * .5f : 0f);   // 위험하면 붉게 깜빡
            long cur = (long)Mathf.Ceil(hp.currentHealth), max = (long)Mathf.Ceil(hp.maxHealth);
            hpLabel.text = $"체력 {NumberFormat.Comma(cur)}/{NumberFormat.Comma(max)}" + (hp.barrier > 0.5f ? $"  <color=#FFF6DA>+ 보호막 {Mathf.CeilToInt(hp.barrier)}</color>" : "");
            barrierFill.anchorMax = new Vector2(Mathf.Clamp01(hp.maxHealth > 0 ? hp.barrier / hp.maxHealth : 0f), 1);
            bool hurt = cur < max; hpTag.gameObject.SetActive(hurt);
            if (hurt) { hpPct.text = Mathf.FloorToInt(r * 100f) + "%"; hpLabel.rectTransform.anchoredPosition = new Vector2(-44, 0); hpTag.anchoredPosition = new Vector2(hpLabel.preferredWidth * .5f - 44 + 10, 0); }
            else hpLabel.rectTransform.anchoredPosition = Vector2.zero;
            // 경험치: 부드럽게 차오름, 레벨 업이면 끝까지 → 0 → 새 값
            if (xp == null) return;
            float want = xp.Ratio;
            if (xp.level != shownLevel) { expShown = Mathf.MoveTowards(expShown, 1f, dt * 2.5f); if (expShown >= 1f) { expShown = 0f; shownLevel = xp.level; } }
            else expShown = Mathf.MoveTowards(expShown, want, dt * Mathf.Max(.6f, Mathf.Abs(want - expShown) * 4f));
            float gainTop = xp.level != shownLevel ? 1f : want;
            expFill.anchorMax = new Vector2(expShown, 1); expGain.anchorMin = new Vector2(expShown, 0); expGain.anchorMax = new Vector2(Mathf.Max(expShown, gainTop), 1);
            if (expGainT > 0) expGainT -= dt; expGainImg.color = new Color(1f, .98f, .8f, Mathf.Clamp01(expGainT / .6f) * .9f + (gainTop > expShown ? .35f : 0f));
            expLabel.text = $"경험치 {NumberFormat.Short(xp.exp)}/{NumberFormat.Short(xp.ToNext)}"; lvText.text = xp.level.ToString();
        }
    }
}
