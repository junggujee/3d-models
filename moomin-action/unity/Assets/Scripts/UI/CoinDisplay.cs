using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 현재 스킬의 코인 표시(명세 v2 4-2 예시와 같은 정보): 공격마다 코인(대기·앞면·뒷면·중단)과 그 공격의 위력, 합계, 기본 위력·코인 위력·기대값.
    /// 예) 연속 베기 — 앞 13 · 뒤 10 · 앞 13 → 합계 36
    /// </summary>
    public class CoinDisplay
    {
        public enum Look { Pending, Heads, Tails, Broken, Unbreakable, UnbreakableTails }
        static Sprite[] sprites;
        public static Sprite Coin(Look k)
        {
            if (sprites == null)
            {
                sprites = new Sprite[6];
                for (int s = 0; s < 6; s++)
                {
                    const int N = 48; var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear }; var px = new Color[N * N];
                    for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
                    {
                        float x = (i + .5f) / N * 2 - 1, y = (j + .5f) / N * 2 - 1, r = Mathf.Sqrt(x * x + y * y), a = Mathf.Clamp01((0.95f - r) * N * 0.5f); Color c;
                        switch ((Look)s)
                        {
                            case Look.Heads: c = r > 0.78f ? new Color(.62f, .45f, .14f) : Color.Lerp(new Color(1f, .93f, .55f), new Color(.88f, .66f, .22f), r); if (Mathf.Abs(x) + Mathf.Abs(y) < 0.32f) c = new Color(1f, .98f, .8f); break;
                            case Look.Tails: c = r > 0.78f ? new Color(.32f, .3f, .28f) : Color.Lerp(new Color(.5f, .48f, .45f), new Color(.28f, .27f, .25f), r); break;
                            case Look.Unbreakable: c = r > 0.78f ? new Color(.95f, .78f, .35f) : Color.Lerp(new Color(1f, .35f, .3f), new Color(.7f, .06f, .08f), r); if (Mathf.Abs(x) + Mathf.Abs(y) < 0.32f) c = new Color(1f, .9f, .85f); break;   // 파괴불가: 빨간 코인 + 금테
                            case Look.UnbreakableTails: c = r > 0.78f ? new Color(.75f, .55f, .25f) : Color.Lerp(new Color(.6f, .15f, .15f), new Color(.35f, .05f, .06f), r); break;
                            case Look.Broken: c = r > 0.78f ? new Color(.45f, .08f, .08f) : new Color(.22f, .05f, .05f); if (Mathf.Abs(Mathf.Abs(x) - Mathf.Abs(y)) < 0.12f && r < 0.7f) c = new Color(.9f, .2f, .2f); break;
                            default: c = r > 0.78f ? UITheme.GoldDim : new Color(.08f, .07f, .06f, .85f); break;
                        }
                        c.a *= a; px[j * N + i] = c;
                    }
                    tex.SetPixels(px); tex.Apply(); sprites[s] = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(.5f, .5f), 100f);
                }
            }
            return sprites[(int)k];
        }

        const int Max = 6; GameObject root; CanvasGroup group; Text title, info; Image[] coins = new Image[Max]; Text[] powers = new Text[Max]; float fade;
        SkillExecutor ex; CharacterStats stats; SkillDefinition shown; bool interrupted;

        public void Build(Transform parent, SkillExecutor executor, CharacterStats st)
        {
            ex = executor; stats = st; var bc = new Vector2(.5f, 0);
            var panel = Img(parent, "CoinDisplay", UITheme.Frame, Color.white); root = panel.gameObject; group = root.AddComponent<CanvasGroup>(); Set(panel.rectTransform, bc, bc, bc, new Vector2(0, 168), new Vector2(600, 172));
            title = Txt(panel.transform, "", 28, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); Set(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(.5f, 1), new Vector2(0, -12), new Vector2(-40, 36));
            for (int i = 0; i < Max; i++)
            {
                coins[i] = Img(panel.transform, "Coin" + i, Coin(Look.Pending), Color.white, false); Set(coins[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(46, 46));
                powers[i] = Txt(panel.transform, "", 22, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.Bold); Set(powers[i].rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(60, 26));
            }
            info = Txt(panel.transform, "", 21, UITheme.Cream, TextAnchor.MiddleCenter); Set(info.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(.5f, 0), new Vector2(0, 12), new Vector2(-30, 30));
            group.alpha = 0;
            ex.SkillStarted += s => { shown = s; interrupted = false; fade = 99f; Refresh(); };
            ex.CoinAttacked += a => Refresh();
            ex.SkillFinished += (s, done) => { interrupted = !done; fade = 1.8f; Refresh(); };
        }

        void Refresh()
        {
            var s = shown; if (s == null) return; var res = ex.results; int n = Mathf.Min(s.coinCount, Max), total = 0;
            title.color = Color.Lerp(UITheme.GoldBright, SkillAttributes.ColorOf(s.attribute), 0.5f);
            title.text = s.skillName + $"   <size=20><color=#9E9178>정신력 {(stats.currentSanity >= 0 ? "+" : "")}{stats.currentSanity} · 앞면 {Mathf.RoundToInt(stats.GetCoinProbability() * 100)}%</color></size>";
            for (int i = 0; i < Max; i++)
            {
                bool on = i < n; coins[i].gameObject.SetActive(on); powers[i].gameObject.SetActive(on); if (!on) continue;
                float x = (i - (n - 1) * .5f) * 70; coins[i].rectTransform.anchoredPosition = new Vector2(x, 12); powers[i].rectTransform.anchoredPosition = new Vector2(x, -26);
                if (i < res.Count) { var a = res[i]; coins[i].color = Color.white; coins[i].sprite = Coin(s.unbreakableCoins ? (a.isHeads ? Look.Unbreakable : Look.UnbreakableTails) : a.isHeads ? Look.Heads : Look.Tails); powers[i].text = a.power.ToString(); powers[i].color = a.isHeads ? UITheme.GoldBright : UITheme.Cream; total += a.power; }
                else { coins[i].sprite = Coin(interrupted ? Look.Broken : s.unbreakableCoins ? Look.Unbreakable : Look.Pending); if (s.unbreakableCoins && !interrupted) coins[i].color = new Color(1, 1, 1, .45f); else coins[i].color = Color.white; powers[i].text = interrupted ? "중단" : "대기"; powers[i].color = UITheme.Muted; }
            }
            info.text = $"기본 위력 {s.basePower} · 코인 위력 +{s.coinPower}    <color=#E8C97A>합계 {total}</color>    기대 {s.ExpectedPower(stats.currentSanity) * s.coinCount:0.0}";
        }

        public void Update(float dt) { if (fade < 90f) fade -= dt; group.alpha = Mathf.Clamp01(fade < 90f ? fade / 0.6f : 1f); }
    }
}
