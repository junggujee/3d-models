using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 코인 판정 순간의 특수 연출(프리팹 없이 코드로): 고래빔(입에서 앞으로 뻗는 빛기둥 + 흰 심 + 물보라), 충격파(바닥에 퍼지는 고리), 물기둥(숨구멍에서 솟는 물 + 고리).
    /// 앞면이면 더 굵고 밝게. CombatResolver.ResolveAttack 이 SkillDefinition.vfx 를 보고 부름.
    /// </summary>
    public class SkillVfxPlayer : MonoBehaviour
    {
        static Material baseMat; static Texture2D ringTex;
        enum Kind { Beam, Ring, Column }
        Kind kind; float t, life, size, width; Transform[] parts; Material[] mats; Color color;

        public static void Play(SkillDefinition s, Transform owner, bool heads)
        {
            float sc = owner.lossyScale.y, k = heads ? 1.25f : 1f;
            switch (s.vfx)
            {
                case SkillVfx.WhaleBeam: WhaleBeamFx.FireOrPulse(s, owner, heads); break;   // 한 번 쏘면 스킬이 끝날 때까지 유지, 코인마다 불끈
                case SkillVfx.Shockwave:
                {
                    var v = Make(Kind.Ring, owner.position + Vector3.up * 0.06f, Quaternion.identity, 0.45f, new Color(1f, 0.85f, 0.45f, 0.8f)); v.size = s.Reach * 2f * k;
                    v.parts = new[] { Quad(v.transform, v.mats[0]) }; SparkBurst.Spawn(owner.position + Vector3.up * 0.3f, Vector3.up, 50, 8f, SparkBurst.Gold);
                    ThirdPersonCamera.Shake(0.14f * k, 0.35f); break;
                }
                case SkillVfx.WaterSpout:
                {
                    Vector3 top = owner.position + Vector3.up * 1.8f * sc;
                    var v = Make(Kind.Column, top, Quaternion.identity, 0.7f, new Color(0.55f, 0.85f, 1f, 0.7f)); v.size = 4.5f * k; v.width = 0.7f * k;
                    v.parts = new[] { Cylinder(v.transform, v.mats[0]) }; SparkBurst.Spawn(top, Vector3.up, 60, 10f, new Color(0.6f, 0.9f, 1f));
                    var r = Make(Kind.Ring, owner.position + Vector3.up * 0.06f, Quaternion.identity, 0.5f, new Color(0.55f, 0.85f, 1f, 0.7f)); r.size = s.Reach * 2f; r.parts = new[] { Quad(r.transform, r.mats[0]) };
                    ThirdPersonCamera.Shake(0.08f, 0.3f); break;
                }
            }
        }

        static SkillVfxPlayer Make(Kind kind, Vector3 pos, Quaternion rot, float life, Color c)
        {
            var go = new GameObject("Vfx_" + kind); go.transform.SetPositionAndRotation(pos, rot);
            var v = go.AddComponent<SkillVfxPlayer>(); v.kind = kind; v.life = life; v.color = c; v.mats = new Material[2]; v.mats[0] = Mat(c); return v;
        }
        static Material Mat(Color c)
        {
            if (baseMat == null) baseMat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
            return new Material(baseMat) { color = c, renderQueue = 3100 };
        }
        static Transform Cylinder(Transform parent, Material m)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Destroy(c.GetComponent<Collider>()); c.transform.SetParent(parent, false);
            var r = c.GetComponent<Renderer>(); r.material = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return c.transform;
        }
        static Transform Quad(Transform parent, Material m)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(q.GetComponent<Collider>()); q.transform.SetParent(parent, false); q.transform.localRotation = Quaternion.Euler(90, 0, 0);
            if (ringTex == null)
            {
                const int N = 128; ringTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var px = new Color[N * N];
                for (int j = 0; j < N; j++) for (int i = 0; i < N; i++) { float x = (i + .5f) / N * 2 - 1, y = (j + .5f) / N * 2 - 1, d = Mathf.Sqrt(x * x + y * y); px[j * N + i] = new Color(1, 1, 1, d > 1 ? 0 : Mathf.Pow(d, 6f)); }
                ringTex.SetPixels(px); ringTex.Apply();
            }
            m.mainTexture = ringTex; var r = q.GetComponent<Renderer>(); r.material = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; return q.transform;
        }

        void Update()
        {
            t += Time.deltaTime; float u = Mathf.Clamp01(t / life), fade = u < 0.7f ? 1f : 1f - (u - 0.7f) / 0.3f;
            switch (kind)
            {
                case Kind.Beam:   // 앞으로 쭉 뻗고(0.08초) 유지 → 가늘어지며 사라짐. 실린더는 Y 축이 길이 → 앞(Z)으로 눕힘
                {
                    float grow = Mathf.Clamp01(t / 0.08f), w = width * (u < 0.7f ? 1f + 0.08f * Mathf.Sin(t * 60f) : fade);
                    for (int i = 0; i < parts.Length; i++)
                    {
                        float ww = i == 0 ? w : w * 0.45f; parts[i].localRotation = Quaternion.Euler(90, 0, 0);
                        parts[i].localScale = new Vector3(ww, size * grow * 0.5f, ww); parts[i].localPosition = new Vector3(0, 0, size * grow * 0.5f);
                    }
                    break;
                }
                case Kind.Ring: parts[0].localScale = Vector3.one * Mathf.Lerp(0.2f, size, 1f - (1f - u) * (1f - u)); break;
                case Kind.Column: { float h = size * Mathf.Clamp01(t / 0.15f); parts[0].localScale = new Vector3(width * fade, h * 0.5f, width * fade); parts[0].localPosition = new Vector3(0, h * 0.5f, 0); break; }
            }
            for (int i = 0; i < mats.Length; i++) if (mats[i] != null) { var c = mats[i].color; c.a = (i == 0 ? color.a : 0.95f) * fade; mats[i].color = c; }
            if (u >= 1f) Destroy(gameObject);
        }
        void OnDestroy() { foreach (var m in mats) if (m != null) Destroy(m); }
    }
}
