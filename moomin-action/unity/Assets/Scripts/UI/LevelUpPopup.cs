using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 레벨업 화면 알림(HUD 가 만듦): 위쪽 가운데에 금빛으로 빛나는 "LEVEL UP" → "Lv.7 → Lv.8" → 장식선 → 오른 능력치가 한 줄씩(0.18초 간격)
    ///   최대 체력 +12 · 공격력 +0.6 · 체력·스태미나 모두 회복 · (새 칭호) · 레벨 차이 안내(3레벨마다 코인 위력 ±1)
    /// 4초 동안 보이고 사라짐. 바로 또 오르면 다시 처음부터.
    /// </summary>
    public class LevelUpPopup
    {
        RectTransform root; CanvasGroup group; Text big, levels; Image line; readonly List<Text> rows = new(); float t = -1; const float Dur = 4f;

        public void Build(Transform parent, Experience xp)
        {
            root = NewRect("LevelUp", parent); At(root, new Vector2(.5f, .72f), Vector2.zero, new Vector2(900, 360)); group = root.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0; group.blocksRaycasts = false;
            var halo = Img(root, "Halo", UITheme.SoftCircle, new Color(1f, .8f, .4f, .35f), false); At(halo.rectTransform, new Vector2(.5f, 1), new Vector2(0, -70), new Vector2(760, 220));
            big = StyleDisplay(Txt(root, "LEVEL UP", 96, Color.white, TextAnchor.MiddleCenter), true); At(big.rectTransform, new Vector2(.5f, 1), new Vector2(0, -70), new Vector2(900, 120));
            big.GetComponent<TextGlow>().color = new Color(1f, .78f, .35f, .55f); big.GetComponent<TextGlow>().radius = 7f;
            levels = StyleHeading(Txt(root, "", 38, UITheme.GoldBright, TextAnchor.MiddleCenter)); At(levels.rectTransform, new Vector2(.5f, 1), new Vector2(0, -150), new Vector2(900, 50));
            line = Divider(root, "Line", 560); At(line.rectTransform, new Vector2(.5f, 1), new Vector2(0, -186), line.rectTransform.sizeDelta);
            for (int i = 0; i < 5; i++) { var r = Txt(root, "", 26, UITheme.Cream, TextAnchor.MiddleCenter); At(r.rectTransform, new Vector2(.5f, 1), new Vector2(0, -222 - i * 34), new Vector2(900, 34)); rows.Add(r); }
            if (xp != null) xp.LevelJumped += Show;
        }

        public void Show(int from, int to)
        {
            int n = Mathf.Max(1, to - from); var l = new List<string>
            {
                $"최대 체력  <color=#9BE86A>+{LevelMath.HealthPerLevel * n:0}</color>      공격력  <color=#9BE86A>+{LevelMath.AttackPerLevel * n:0.#}</color>",
                "<color=#BFE3FF>체력 · 스태미나 모두 회복</color>",
            };
            for (int lv = from + 1; lv <= to; lv++) if (LevelMath.NewTitleAt(lv)) l.Add($"새 칭호  <color=#F2D27F>「{LevelMath.Title(lv)}」</color>");
            l.Add($"<size=20><color=#C9BBA0>상대보다 {LevelMath.LevelsPerCoin}레벨 높을 때마다 코인 위력 +1 (최대 +{LevelMath.MaxCoinBonus})</color></size>");
            levels.text = $"Lv.{from}  →  Lv.{to}";
            for (int i = 0; i < rows.Count; i++) rows[i].text = i < l.Count ? l[i] : "";
            t = 0f;
        }

        public void Update(float dt)
        {
            if (t < 0) return; t += dt; if (t > Dur) { t = -1; group.alpha = 0; return; }
            group.alpha = Mathf.Clamp01(Mathf.Min(t * 5f, (Dur - t) * 2.5f));
            float pop = t < 0.3f ? 1f + 0.25f * Mathf.Sin(t / 0.3f * Mathf.PI) : 1f; big.rectTransform.localScale = Vector3.one * pop;
            for (int i = 0; i < rows.Count; i++) { float a = Mathf.Clamp01((t - 0.45f - i * 0.18f) * 5f); var c = rows[i].color; rows[i].color = new Color(c.r, c.g, c.b, a); rows[i].rectTransform.anchoredPosition = new Vector2(0, -222 - i * 34 - 8f * (1f - a)); }
            line.color = new Color(1, 1, 1, Mathf.Clamp01((t - 0.3f) * 4f));
        }
    }
}
