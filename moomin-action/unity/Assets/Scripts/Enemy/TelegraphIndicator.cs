using UnityEngine;

namespace MoominAction
{
    /// <summary>적 공격 예고: 바닥에 원형 경고(가장자리 고리 + 안쪽이 시간에 따라 차오름). 빨강 = 일반, 보라 = 방어 불가.</summary>
    public class TelegraphIndicator : MonoBehaviour
    {
        static Material baseMat; static Texture2D ring;
        Material mat; Color color; float duration, t; Transform fill;

        static Texture2D Ring()
        {
            if (ring != null) return ring; const int N = 128; ring = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var c = new Color[N * N];
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) { float x = (i + .5f) / N * 2 - 1, y = (j + .5f) / N * 2 - 1, r = Mathf.Sqrt(x * x + y * y); float a = r > 1 ? 0 : r > 0.92f ? 1 : 0.25f + 0.25f * r; c[j * N + i] = new Color(1, 1, 1, a); }
            ring.SetPixels(c); ring.Apply(); return ring;
        }

        public static TelegraphIndicator Show(Transform owner, Vector3 localOffset, float radius, float duration, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.name = "Telegraph"; Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(owner, false); go.transform.localPosition = localOffset + Vector3.up * 0.04f; go.transform.localRotation = Quaternion.Euler(90, 0, 0); go.transform.localScale = Vector3.one * radius * 2f;
            var t = go.AddComponent<TelegraphIndicator>(); t.color = color; t.duration = duration;
            if (baseMat == null) baseMat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            t.mat = new Material(baseMat) { mainTexture = Ring(), color = color, renderQueue = 3100 }; go.GetComponent<Renderer>().material = t.mat;
            go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var f = GameObject.CreatePrimitive(PrimitiveType.Quad); Object.Destroy(f.GetComponent<Collider>()); f.transform.SetParent(go.transform, false); f.transform.localPosition = new Vector3(0, 0, -0.001f); f.transform.localScale = Vector3.zero;
            f.GetComponent<Renderer>().material = t.mat; t.fill = f.transform; return t;
        }

        void Update()
        {
            t += Time.deltaTime; float k = Mathf.Clamp01(t / duration);
            if (fill) fill.localScale = Vector3.one * k;                                   // 안쪽 원이 차오르면 곧 공격
            if (mat) mat.color = new Color(color.r, color.g, color.b, color.a * (0.6f + 0.4f * Mathf.Sin(t * 18f) * k));
            if (t > duration + 0.5f) Destroy(gameObject);
        }
        public void Hide() { if (this && gameObject) Destroy(gameObject); }
        void OnDestroy() { if (mat) Destroy(mat); }
    }
}
