using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>
    /// 세로 그라데이션(위 top → 아래 bottom). 정점 색에 곱하므로 Shadow·Outline 의 검은 정점은 그대로 검음(컴포넌트 순서와 무관).
    /// 제목·장(칸토)·보스 이름의 크림→베이지 글자(사용자 참고: 스타 세일러 로고).
    /// </summary>
    [AddComponentMenu("UI/Effects/Moomin Gradient")]
    public class UIGradient : BaseMeshEffect
    {
        public Color top = Color.white, bottom = new Color(0.85f, 0.74f, 0.56f);
        static readonly List<UIVertex> verts = new();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            verts.Clear(); vh.GetUIVertexStream(verts);
            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in verts) { if (v.position.y < min) min = v.position.y; if (v.position.y > max) max = v.position.y; }
            float h = Mathf.Max(1e-3f, max - min);
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i]; Color g = Color.Lerp(bottom, top, (v.position.y - min) / h); Color c = v.color;
                v.color = new Color(c.r * g.r, c.g * g.g, c.b * g.b, c.a * g.a); verts[i] = v;
            }
            vh.Clear(); vh.AddUIVertexTriangleStream(verts);
        }
    }

    /// <summary>
    /// 은은한 빛(글자 둘레에 옅은 색 복사본을 여러 겹) — 스킬 이름("조금 빛나고 깔끔하게"), 보스 이름. radius 픽셀, 겹 rings × 8 방향.
    /// </summary>
    [AddComponentMenu("UI/Effects/Moomin Glow")]
    public class TextGlow : BaseMeshEffect
    {
        public Color color = new Color(1f, 0.85f, 0.5f, 0.35f); public float radius = 4f; [Range(1, 3)] public int rings = 2;
        static readonly List<UIVertex> src = new(), dst = new();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0 || color.a <= 0f) return;
            src.Clear(); dst.Clear(); vh.GetUIVertexStream(src);
            for (int r = rings; r >= 1; r--)
            {
                float rad = radius * r / rings; float a = color.a * (1f - (r - 1f) / (rings + 0.5f)) / 3f;
                for (int k = 0; k < 8; k++)
                {
                    float ang = k * Mathf.PI / 4f; var off = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0) * rad;
                    foreach (var v0 in src) { var v = v0; v.position += off; Color c = color; c.a = a * (v0.color.a / 255f); v.color = c; dst.Add(v); }
                }
            }
            dst.AddRange(src); vh.Clear(); vh.AddUIVertexTriangleStream(dst);
        }
    }
}
