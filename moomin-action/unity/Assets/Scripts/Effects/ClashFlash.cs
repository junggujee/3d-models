using UnityEngine;

namespace MoominAction
{
    /// <summary>순간 섬광(점광원이 짧게 밝았다가 꺼짐). 합 부딪힘·승리 연출용, 프리팹 없음.</summary>
    public class ClashFlash : MonoBehaviour
    {
        Light l; float t, life, peak;
        public static void Spawn(Vector3 pos, Color color, float intensity, float range, float seconds)
        {
            var go = new GameObject("ClashFlash"); go.transform.position = pos;
            var f = go.AddComponent<ClashFlash>(); f.l = go.AddComponent<Light>(); f.l.type = LightType.Point; f.l.color = color; f.l.range = range; f.l.intensity = intensity; f.peak = intensity; f.life = seconds;
        }
        void Update()
        {
            t += Time.unscaledDeltaTime; float u = t / Mathf.Max(0.01f, life);   // 실제 시간: 히트스톱 중에도 번쩍
            l.intensity = peak * (u < 0.15f ? u / 0.15f : Mathf.Clamp01(1f - (u - 0.15f) / 0.85f));
            if (u >= 1f) Destroy(gameObject);
        }
    }
}
