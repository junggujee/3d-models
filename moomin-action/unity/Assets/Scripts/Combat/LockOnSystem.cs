using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>Tab 타깃 고정: 화면 가운데에 가까운 적을 조준, 다시 누르면 해제. 대상이 죽거나 멀어지면 다음 적으로 넘어감.</summary>
    public class LockOnSystem : MonoBehaviour
    {
        public float range = 22f, breakRange = 30f, maxScreenAngle = 70f;
        public EnemyAI Target { get; private set; }
        public event Action<EnemyAI> TargetChanged;
        public Vector3 TargetPoint => Target != null ? Target.transform.position + Vector3.up * Target.AimHeight : transform.position;

        void Update()
        {
            if (UIState.Paused) return;
            if (InputAdapter.LockOnPressed()) Set(Target != null ? null : FindBest());
            if (Target != null && (Target.State == EnemyState.Dead || (Target.transform.position - transform.position).sqrMagnitude > breakRange * breakRange)) Set(FindBest());
        }

        void Set(EnemyAI t) { if (t == Target) return; Target = t; TargetChanged?.Invoke(t); }

        EnemyAI FindBest()
        {
            var cam = Camera.main; Vector3 fwd = cam ? cam.transform.forward : transform.forward; EnemyAI best = null; float bestScore = float.MaxValue;
            foreach (var e in EnemyAI.All)
            {
                if (e == null || e.State == EnemyState.Dead) continue; Vector3 to = e.transform.position - transform.position; float d = to.magnitude; if (d > range) continue;
                float ang = Vector3.Angle(Vector3.ProjectOnPlane(fwd, Vector3.up), Vector3.ProjectOnPlane(to, Vector3.up)); if (ang > maxScreenAngle && d > 4f) continue;
                float score = ang + d * 2f; if (score < bestScore) { bestScore = score; best = e; }
            }
            return best;
        }
    }
}
