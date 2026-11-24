using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 불꽃 파티클(프리팹 없이 코드로). 기본은 노랑→주황→빨강, tint 를 주면 그 색으로(앞면 = 금색 섬광).
    /// 최적화: 예전에는 부를 때마다 GameObject + ParticleSystem 을 새로 만들고 지웠음(타격·광선 잔상 빛 가루처럼 초당 수십 번 → 쓰레기·끊김).
    /// 이제 장면마다 ParticleSystem 하나(SparkPool)에 Emit 으로 낱개 입자를 쏨: 위치·방향(원뿔 45°)·속도·크기·색을 입자마다 지정.
    /// 장면이 바뀌면 자동으로 다시 만듦. 동시에 최대 4000개.
    /// </summary>
    public static class SparkBurst
    {
        public static readonly Color Gold = new Color(1f, 0.86f, 0.35f), Crimson = new Color(0.95f, 0.15f, 0.15f), Violet = new Color(0.6f, 0.35f, 1f), Yellow = new Color(1f, 0.95f, 0.42f);
        static readonly Color DefaultHot = new Color(1f, 0.78f, 0.3f);
        static ParticleSystem ps; static Material mat;

        static ParticleSystem Pool()
        {
            if (ps != null) return ps;
            var go = new GameObject("SparkPool"); ps = go.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.loop = true; main.playOnAwake = false; main.maxParticles = 4000; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.6f; main.startLifetime = 0.6f; main.startSpeed = 0f; main.startSize = 0.06f;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true; var g = new Gradient();   // 입자 색(startColor) × 수명 동안 밝음 → 조금 어둡게 → 사라짐
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.85f, 0.7f), 0.5f), new GradientColorKey(new Color(0.7f, 0.45f, 0.35f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            if (mat == null)
            {
                var sh2 = Shader.Find("Universal Render Pipeline/Particles/Unlit"); if (sh2 == null) sh2 = Shader.Find("Particles/Standard Unlit"); if (sh2 == null) sh2 = Shader.Find("Sprites/Default");
                mat = new Material(sh2); if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            }
            r.material = mat; r.renderMode = ParticleSystemRenderMode.Stretch; r.lengthScale = 2.5f; r.velocityScale = 0.02f;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
            return ps;
        }

        public static void Spawn(Vector3 pos, Vector3 dir, int count = 28, float speed = 5f, Color? tint = null)
        {
            var p = Pool(); if (count <= 0) return;
            if (!p.isPlaying) p.Play();
            var fwd = dir.sqrMagnitude > 0.01f ? dir.normalized : Vector3.up; var rot = Quaternion.LookRotation(fwd);
            float maxSize = tint.HasValue ? 0.12f : 0.09f; var ep = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                // 원뿔(반각 45°) 안의 무작위 방향
                float a = Random.value * Mathf.PI * 2f, t = Mathf.Lerp(Mathf.Cos(45f * Mathf.Deg2Rad), 1f, Random.value), st = Mathf.Sqrt(1f - t * t);
                var d = rot * new Vector3(Mathf.Cos(a) * st, Mathf.Sin(a) * st, t);
                ep.position = pos + Random.insideUnitSphere * 0.02f; ep.velocity = d * Random.Range(speed * 0.4f, speed);
                ep.startLifetime = Random.Range(0.3f, 0.8f); ep.startSize = Random.Range(0.03f, maxSize);
                var c = tint.HasValue ? tint.Value : Color.Lerp(DefaultHot, new Color(1f, 0.45f, 0.12f), Random.value);   // 기본: 노랑~주황 불꽃
                ep.startColor = Color.Lerp(c, Color.white, tint.HasValue ? 0.25f : 0.15f);
                p.Emit(ep, 1);
            }
        }
    }
}
