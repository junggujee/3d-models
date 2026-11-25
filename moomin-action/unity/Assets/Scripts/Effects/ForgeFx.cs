using UnityEngine;

namespace MoominAction
{
    /// <summary>대장간 연출: 망치가 내려칠 때 불똥 + 화면 흔들림, 끝났을 때 금빛(성공) / 잿빛(실패) 터짐.</summary>
    public static class ForgeFx
    {
        static readonly Color Spark = new Color(1f, 0.72f, 0.25f), Win = new Color(1f, 0.9f, 0.5f), Lose = new Color(0.55f, 0.5f, 0.45f);

        /// <summary>망치질 한 번: 모루에서 불똥이 튀고 화면이 짧게 흔들림.</summary>
        public static void Strike(Vector3 at)
        {
            SparkBurst.Spawn(at, Vector3.up, 26, 6.5f, Spark);
            ThirdPersonCamera.Shake(0.18f, 0.16f);
        }

        /// <summary>마무리: 성공이면 금빛이 크게 퍼지고, 실패면 잿빛 연기처럼 작게 흩어짐.</summary>
        public static void Burst(Vector3 at, bool success)
        {
            if (success)
            {
                SparkBurst.Spawn(at, Vector3.up, 70, 9f, Win);
                for (int k = 0; k < 6; k++)
                {
                    float a = k * Mathf.PI / 3f; var dir = new Vector3(Mathf.Cos(a), 0.5f, Mathf.Sin(a));
                    SparkBurst.Spawn(at + dir * 0.7f, dir, 14, 7f, Win);
                }
                ThirdPersonCamera.Shake(0.3f, 0.35f);
            }
            else
            {
                SparkBurst.Spawn(at, Vector3.down, 22, 2.2f, Lose);
                ThirdPersonCamera.Shake(0.1f, 0.25f);
            }
        }
    }
}
