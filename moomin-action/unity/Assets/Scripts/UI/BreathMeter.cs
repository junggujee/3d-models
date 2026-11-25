using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 숨 방울(잠수 중 · 숨이 덜 찼을 때만): 화면 아래 가운데 위에 "숨" + 방울 10개(남은 만큼 하늘색, 빈 건 흐린 테두리).
    /// 30% 아래면 깜빡, 0 이면 빨갛게. 물 밖에서 다 차면 사라짐.
    /// </summary>
    public class BreathMeter
    {
        const int N = 10;
        RectTransform root; CanvasGroup group; readonly Image[] dots = new Image[N]; Text label; CharacterStats owner; PlayerSwim swim;
        static Sprite dot;

        static Sprite Dot()
        {
            if (dot != null) return dot; const int S = 48; var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var px = new Color32[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S * 2 - 1, dy = (y + 0.5f) / S * 2 - 1, r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((1f - r) * S * 0.5f); float rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) * 9f); float hi = Mathf.Clamp01(1f - new Vector2(dx + 0.35f, dy - 0.35f).magnitude * 4f);
                float v = Mathf.Clamp01(0.55f + rim * 0.45f + hi * 0.6f); px[y * S + x] = new Color32((byte)(v * 255), (byte)(v * 255), (byte)(v * 255), (byte)(a * 255));
            }
            t.SetPixels32(px); t.Apply(); dot = Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(.5f, .5f), 100f); return dot;
        }

        public void Build(Transform core, CharacterStats player)
        {
            owner = player; var b = new Vector2(.5f, 0);
            root = NewRect("Breath", core); At(root, b, new Vector2(0, 250), new Vector2(420, 44)); group = root.gameObject.AddComponent<CanvasGroup>(); group.alpha = 0f; group.blocksRaycasts = false;
            label = Txt(root, "숨", 26, UITheme.Cream, TextAnchor.MiddleRight); label.font = UITheme.HeadingFont; At(label.rectTransform, new Vector2(0, .5f), new Vector2(-6, 0), new Vector2(60, 40));
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .8f);
            for (int i = 0; i < N; i++) { dots[i] = Img(root, "B" + i, Dot(), Color.white, false); At(dots[i].rectTransform, new Vector2(0, .5f), new Vector2(40 + i * 36, 0), new Vector2(30, 30)); }
        }

        public void Update(float dt)
        {
            if (root == null) return;
            if (swim == null && owner != null) swim = owner.GetComponent<PlayerSwim>();
            if (swim == null) { group.alpha = 0f; return; }
            float b = swim.Breath01; bool show = swim.mode == PlayerSwim.Mode.Dive || b < 0.999f;
            group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, dt * 3f); if (group.alpha <= 0f) return;
            bool low = b < 0.3f, empty = b <= 0f; float blink = low ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;
            for (int i = 0; i < N; i++)
            {
                float fill = Mathf.Clamp01(b * N - i);
                dots[i].color = empty ? new Color(1f, 0.35f, 0.3f, 0.5f + 0.4f * blink) : Color.Lerp(new Color(0.6f, 0.75f, 0.85f, 0.25f), new Color(0.6f, 0.92f, 1f, blink), fill);
                dots[i].rectTransform.localScale = Vector3.one * (0.75f + 0.25f * fill);
            }
            label.color = empty ? new Color(1f, 0.45f, 0.4f) : UITheme.Cream;
        }
    }
}
