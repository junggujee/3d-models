using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 카메라 기준 이동·달리기·점프 + <b>Q 구르기</b>(스태미나 소모, 무적 프레임 0.05~0.38초, 공격 후딜 취소 가능).
    /// Animator: "Speed"(0~1) 블렌드, 상태 "Roll".
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health), typeof(Stamina))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 3.2f, runSpeed = 6.0f, turnSpeed = 14f, jumpHeight = 1.1f, gravity = -22f;
        [Header("구르기")] public float rollSpeed = 7.8f, rollDuration = 0.66f, rollCost = 18f, iFrameStart = 0.05f, iFrameEnd = 0.40f, rollCooldown = 0.12f, sprintDrain = 9f;
        public Animator animator; public Transform cam;
        [HideInInspector] public float moveMultiplier = 1f; [HideInInspector] public bool locked;
        public bool Rolling { get; private set; }
        CharacterController cc; Health health; Stamina stamina; SwordCombat combat; Vector3 vel, rollDir; float speed01, rollT, nextRoll;

        void Awake()
        {
            cc = GetComponent<CharacterController>(); health = GetComponent<Health>(); stamina = GetComponent<Stamina>(); combat = GetComponent<SwordCombat>();
            if (!animator) animator = GetComponentInChildren<Animator>(); if (!cam && Camera.main) cam = Camera.main.transform;
        }

        Vector3 InputDir(Vector2 mv)
        {
            Vector3 f = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward, r = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
            return f * mv.y + r * mv.x;
        }

        void Update()
        {
            if (UIState.Paused) return;
            Vector2 mv = locked ? Vector2.zero : InputAdapter.Move(); Vector3 dir = InputDir(mv);
            if (Rolling) { UpdateRoll(); return; }
            if (!locked && InputAdapter.RollPressed() && Time.time >= nextRoll && stamina.Spend(rollCost)) { StartRoll(dir); return; }

            bool run = InputAdapter.SprintHeld() && mv.sqrMagnitude > 0.01f && stamina.Current > 1f; if (run) stamina.Drain(sprintDrain);
            float target = (run ? runSpeed : walkSpeed) * mv.magnitude * moveMultiplier;
            if (dir.sqrMagnitude > 0.001f && moveMultiplier > 0.5f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            if (cc.isGrounded && vel.y < 0) vel.y = -2f;
            if (cc.isGrounded && !locked && moveMultiplier > 0.5f && InputAdapter.JumpPressed()) vel.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            vel.y += gravity * Time.deltaTime;
            cc.Move((dir.normalized * target + new Vector3(0, vel.y, 0)) * Time.deltaTime);
            speed01 = Mathf.MoveTowards(speed01, mv.magnitude * (run ? 1f : 0.5f), 6f * Time.deltaTime);
            if (animator) animator.SetFloat("Speed", speed01);
        }

        void StartRoll(Vector3 dir)
        {
            rollDir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;    // 입력 방향, 없으면 바라보는 방향
            transform.rotation = Quaternion.LookRotation(rollDir); Rolling = true; rollT = 0f; combat?.CancelAttack();
            if (animator) animator.CrossFadeInFixedTime("Roll", 0.04f);
        }

        void UpdateRoll()
        {
            rollT += Time.deltaTime; health.Invulnerable = rollT >= iFrameStart && rollT <= iFrameEnd;
            float k = rollT / rollDuration, curve = Mathf.Lerp(1.15f, 0.35f, k * k);          // 처음에 빠르고 끝에서 감속
            if (cc.isGrounded && vel.y < 0) vel.y = -2f; vel.y += gravity * Time.deltaTime;
            cc.Move((rollDir * rollSpeed * curve + new Vector3(0, vel.y, 0)) * Time.deltaTime);
            if (rollT >= rollDuration) { Rolling = false; health.Invulnerable = false; nextRoll = Time.time + rollCooldown; if (animator) animator.CrossFadeInFixedTime("Locomotion", 0.1f); }
        }

        public void Knockback(Vector3 force) { vel.y = Mathf.Max(vel.y, force.y); cc.Move(new Vector3(force.x, 0, force.z) * 0.15f); }
        public void CancelRoll() { if (Rolling) { Rolling = false; health.Invulnerable = false; } }
    }
}
