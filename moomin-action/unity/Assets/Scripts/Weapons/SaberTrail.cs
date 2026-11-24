using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 레이저 블레이더의 푸른 광선 잔상 + 빛 파티클.
    ///  · 칼날 끝·중간 4곳에 TrailRenderer(시안 → 파랑 → 투명) — 돌수록 원을 그리며 잔상이 겹쳐 검 전체가 빛나는 원판처럼 보임
    ///  · 잔상 길이는 결투 고조·공격 중일수록 길게, 굵기도 조금씩
    ///  · 공격 중(또는 고조가 높을 때) 칼날 위 아무 곳에서 푸른 빛 가루(SparkBurst)를 흩뿌림
    /// 칼날 위치는 장착 모델 "Rotor_R_Laser" 의 메시 경계(가장 긴 축의 양 끝)에서 계산 — 메시 읽기 권한 필요 없음.
    /// </summary>
    public class SaberTrail : MonoBehaviour
    {
        public static readonly Color Cyan = new Color(0.45f, 0.88f, 1f), Blue = new Color(0.15f, 0.4f, 1f);
        static Material mat;
        Transform rotor; readonly List<(Transform t, TrailRenderer tr, float k)> points = new(); Vector3 axisLocal, centerLocal; float extent, scanT, sparkT;
        DuelHeat heat; SkillExecutor ex;

        void Awake() { heat = GetComponent<DuelHeat>(); ex = GetComponent<SkillExecutor>(); }
        void OnDisable() { Clear(); }

        void Clear() { foreach (var p in points) if (p.t != null) Destroy(p.t.gameObject); points.Clear(); rotor = null; }

        bool Find()
        {
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                if (!t.name.StartsWith("Rotor_R_Laser") || !t.TryGetComponent<MeshFilter>(out var mf) || mf.sharedMesh == null) continue;
                var b = mf.sharedMesh.bounds; var e = b.extents; centerLocal = b.center;
                axisLocal = e.x >= e.y && e.x >= e.z ? Vector3.right : e.y >= e.z ? Vector3.up : Vector3.forward; extent = Mathf.Max(e.x, Mathf.Max(e.y, e.z));
                rotor = t; Build(); return true;
            }
            return false;
        }

        void Build()
        {
            if (mat == null) mat = new Material(Shader.Find("Sprites/Default"));
            foreach (var (sign, k) in new[] { (1f, 1f), (-1f, 1f), (1f, 0.62f), (-1f, 0.62f) })
            {
                var go = new GameObject("SaberTrail"); go.transform.SetParent(rotor, false); go.transform.localPosition = centerLocal + axisLocal * extent * k * sign;
                go.layer = gameObject.layer;
                var tr = go.AddComponent<TrailRenderer>(); tr.material = mat; tr.time = 0.18f; tr.minVertexDistance = 0.03f; tr.numCapVertices = 2; tr.alignment = LineAlignment.View;
                tr.widthCurve = new AnimationCurve(new Keyframe(0, 1f), new Keyframe(1, 0f)); tr.widthMultiplier = k >= 1f ? 0.14f : 0.1f;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Cyan, 0.25f), new GradientColorKey(Blue, 1f) }, new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.4f), new GradientAlphaKey(0f, 1f) });
                tr.colorGradient = g; tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tr.receiveShadows = false;
                points.Add((go.transform, tr, k));
            }
        }

        void LateUpdate()
        {
            if (rotor == null) { if ((scanT -= Time.unscaledDeltaTime) > 0f) return; scanT = 0.5f; if (!Find()) return; }
            float h = heat != null && heat.enabled ? heat.Ratio : 0f; bool attacking = ex != null && ex.IsExecuting;
            float time = 0.16f + 0.22f * h + (attacking ? 0.1f : 0f);
            foreach (var p in points) { if (p.tr == null) continue; p.tr.time = time * (p.k >= 1f ? 1f : 0.8f); p.tr.widthMultiplier = (p.k >= 1f ? 0.14f : 0.1f) * (1f + 0.5f * h); p.tr.emitting = Time.timeScale > 0f; }
            // 빛 가루: 공격 중이거나 고조가 높을수록 자주
            float rate = attacking ? 0.05f : h > 0.5f ? 0.15f : 0.4f; sparkT -= Time.deltaTime;
            if (Time.timeScale > 0f && sparkT <= 0f)
            {
                sparkT = rate; float u = Random.Range(-1f, 1f);
                var pos = rotor.TransformPoint(centerLocal + axisLocal * extent * u);
                SparkBurst.Spawn(pos, Random.onUnitSphere, attacking ? 5 : 2, 1.6f, Random.value < 0.5f ? Cyan : Blue);
            }
        }
    }
}
