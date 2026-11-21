using UnityEngine;

namespace MoominAction
{
    /// <summary>카메라 기준 이동·달리기·점프. 공격 중에는 이동이 느려짐. Animator 파라미터 "Speed"(0~1)로 Idle/Walk/Run 블렌딩.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 3.2f, runSpeed = 6.0f, turnSpeed = 14f, jumpHeight = 1.1f, gravity = -22f;
        public Animator animator; public Transform cam;
        [HideInInspector] public float moveMultiplier = 1f;   // 공격 중 SwordCombat 이 낮춤
        [HideInInspector] public bool locked;                  // 피격/사망 중 입력 잠금
        CharacterController cc; Vector3 vel; float speed01;

        void Awake() { cc = GetComponent<CharacterController>(); if (!animator) animator = GetComponentInChildren<Animator>(); if (!cam && Camera.main) cam = Camera.main.transform; }

        void Update()
        {
            Vector2 mv = locked ? Vector2.zero : InputAdapter.Move();
            Vector3 f = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward, r = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
            Vector3 dir = f * mv.y + r * mv.x;
            bool run = InputAdapter.SprintHeld(); float target = (run ? runSpeed : walkSpeed) * mv.magnitude * moveMultiplier;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            if (cc.isGrounded && vel.y < 0) vel.y = -2f;
            if (cc.isGrounded && !locked && InputAdapter.JumpPressed()) vel.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            vel.y += gravity * Time.deltaTime;
            Vector3 horizontal = dir.normalized * target;
            cc.Move((horizontal + new Vector3(0, vel.y, 0)) * Time.deltaTime);
            speed01 = Mathf.MoveTowards(speed01, mv.magnitude * (run ? 1f : 0.5f) * (moveMultiplier > 0.5f ? 1f : 0.3f), 6f * Time.deltaTime);
            if (animator) animator.SetFloat("Speed", speed01);
        }
        public void Knockback(Vector3 force) { vel.y = Mathf.Max(vel.y, force.y); cc.Move(new Vector3(force.x, 0, force.z) * 0.15f); }
    }
}
