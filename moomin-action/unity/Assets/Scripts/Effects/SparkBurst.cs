using UnityEngine;

namespace MoominAction
{
    /// <summary>불꽃 파티클(프리팹 없이 코드로 생성). 기본은 노랑→주황→빨강, tint 를 주면 그 색으로(앞면 = 금색 섬광).</summary>
    public static class SparkBurst
    {
        public static readonly Color Gold = new Color(1f, 0.86f, 0.35f), Crimson = new Color(0.95f, 0.15f, 0.15f), Violet = new Color(0.6f, 0.35f, 1f), Yellow = new Color(1f, 0.95f, 0.42f);
        static Material mat;
        public static void Spawn(Vector3 pos, Vector3 dir, int count = 28, float speed = 5f, Color? tint = null)
        {
            var go = new GameObject("SparkBurst"); go.transform.position = pos; go.transform.rotation = dir.sqrMagnitude > 0.01f ? Quaternion.LookRotation(dir) : Quaternion.identity;
            var ps = go.AddComponent<ParticleSystem>(); ps.Stop();
            var main = ps.main; main.duration = 0.2f; main.loop = false; main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed); main.startSize = new ParticleSystem.MinMaxCurve(0.03f, tint.HasValue ? 0.12f : 0.09f);
            main.gravityModifier = 1.6f; main.simulationSpace = ParticleSystemSimulationSpace.World; main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 45f; sh.radius = 0.02f;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();
            Color a = tint ?? new Color(1f, 0.95f, 0.4f), b = tint.HasValue ? Color.Lerp(tint.Value, Color.white, 0.3f) : new Color(1f, 0.5f, 0.1f), c = tint.HasValue ? tint.Value * 0.6f : new Color(0.8f, 0.1f, 0.05f);
            g.SetKeys(new[] { new GradientColorKey(Color.Lerp(a, Color.white, 0.4f), 0f), new GradientColorKey(b, 0.5f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (mat == null) { var sh2 = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default"); mat = new Material(sh2); mat.SetColor("_BaseColor", Color.white); }
            r.material = mat; r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2.5f;
            ps.Play();
        }
    }
}
