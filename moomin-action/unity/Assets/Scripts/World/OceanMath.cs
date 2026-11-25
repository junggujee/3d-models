using System;

namespace MoominAction
{
    /// <summary>파도 하나(게르스트너): 진행 방향(dirX, dirZ 단위벡터) · 파수 k = 2π/파장 · 각속도 ω = √(g·k) · 진폭(m) = 가파름 / k.</summary>
    public struct OceanWave
    {
        public float dirX, dirZ, k, omega, amp;
    }

    /// <summary>
    /// 바다 물결 계산(순수 C# — UnityEngine 없음, status_tests 로 검증).
    /// 셰이더(Resources/Shaders/MoominOcean.shader)와 <b>같은 식</b>: 점 (x, z) 가 시간 t 에
    ///   x += dx·A·cos f,  z += dz·A·cos f,  y += A·sin f   (f = k·(d·(x,z)) − ω·t)
    /// 로 옮겨짐(물마루 쪽으로 모이는 둥근 물결). 높이를 물을 때는 '어느 점이 (x, z) 로 왔는가'를 몇 번 되풀이해 거꾸로 찾음(HeightAt).
    /// 얕은 곳은 물결이 작아짐(AmpScale: 깊이 0m → 35%, 6m 이상 → 100%) — 셰이더도 같은 깊이 그림을 씀.
    /// 파도 4개의 진폭 합 ≈ 0.69m &lt; 해수면 아래 뭍까지 거리(0.8m) → 물이 뭍 위로 넘치지 않음.
    /// </summary>
    public static class OceanMath
    {
        public const float G = 9.81f;
        /// <summary>파도 4개: (기본 방향에서 돌린 각°, 파장 m, 가파름).</summary>
        public static readonly (float angle, float length, float steep)[] Spec = { (0f, 22f, 0.1f), (28f, 13f, 0.09f), (-35f, 8.5f, 0.08f), (70f, 5f, 0.06f) };

        public static OceanWave Make(float angleDeg, float wavelength, float steepness)
        {
            double a = angleDeg * Math.PI / 180.0; float k = (float)(2 * Math.PI / Math.Max(0.5f, wavelength));
            return new OceanWave { dirX = (float)Math.Sin(a), dirZ = (float)Math.Cos(a), k = k, omega = (float)Math.Sqrt(G * k), amp = steepness / k };
        }
        /// <summary>기본 진행 방향(도, +Z 에서 시계 방향 = Unity 의 yaw)에 맞춘 파도 4개.</summary>
        public static OceanWave[] Waves(float baseAngleDeg)
        {
            var w = new OceanWave[Spec.Length];
            for (int i = 0; i < w.Length; i++) w[i] = Make(baseAngleDeg + Spec[i].angle, Spec[i].length, Spec[i].steep);
            return w;
        }
        public static float MaxAmplitude(OceanWave[] w) { float s = 0f; foreach (var x in w) s += x.amp; return s; }

        /// <summary>깊이(m)에 따른 물결 크기 배율.</summary>
        public static float AmpScale(float depth) { float t = Math.Max(0f, Math.Min(1f, depth / 6f)); return 0.35f + 0.65f * t; }

        /// <summary>(x, z) 에 있던 물 알갱이가 시간 t 에 옮겨 간 양.</summary>
        public static void Displace(OceanWave[] w, float x, float z, float t, float ampScale, out float dx, out float dy, out float dz)
        {
            dx = dy = dz = 0f;
            for (int i = 0; i < w.Length; i++)
            {
                var v = w[i]; float a = v.amp * ampScale, f = v.k * (v.dirX * x + v.dirZ * z) - v.omega * t;
                float c = (float)Math.Cos(f), s = (float)Math.Sin(f);
                dx += v.dirX * a * c; dz += v.dirZ * a * c; dy += a * s;
            }
        }

        /// <summary>
        /// 수면 높이(해수면 기준, m): 옮겨 와서 (x, z) 에 닿은 알갱이의 높이. 3번 되풀이(오차 수 mm).
        /// depthAt: 깊이(얕으면 물결이 작음) — null 이면 깊은 바다.
        /// </summary>
        public static float HeightAt(OceanWave[] w, float x, float z, float t, Func<float, float, float> depthAt = null, int iterations = 3)
        {
            float px = x, pz = z, dx = 0f, dy = 0f, dz = 0f;
            for (int i = 0; i <= iterations; i++)
            {
                float sc = depthAt != null ? AmpScale(depthAt(px, pz)) : 1f;
                Displace(w, px, pz, t, sc, out dx, out dy, out dz);
                if (i < iterations) { px = x - dx; pz = z - dz; }
            }
            return dy;
        }
    }
}
