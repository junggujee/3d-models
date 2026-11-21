using UnityEngine;

namespace MoominAction
{
    /// <summary>칼날 가장자리(재질 이름에 "Sword_Edge")를 무지개색으로 순환시키고 맥동시킴. 공격 중엔 더 밝게.</summary>
    public class SwordGlow : MonoBehaviour
    {
        public float hueSpeed = 0.25f, intensity = 3.5f, attackBoost = 2f;
        public bool Attacking;
        Material edge; static readonly int Emission = Shader.PropertyToID("_EmissionColor"), Base = Shader.PropertyToID("_BaseColor");

        void Start()
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials) if (m.name.Contains("Sword_Edge")) { edge = m; m.EnableKeyword("_EMISSION"); }
        }
        void Update()
        {
            if (edge == null) return;
            var c = Color.HSVToRGB(Mathf.Repeat(Time.time * hueSpeed, 1f), 0.75f, 1f);
            float k = intensity * (1f + 0.15f * Mathf.Sin(Time.time * 7f)) * (Attacking ? attackBoost : 1f);
            edge.SetColor(Emission, c * k); if (edge.HasProperty(Base)) edge.SetColor(Base, c);
        }
    }
}
