using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 카메라 기준 이동·달리기, <b>Space 회피</b>(구르기: 스태미나 소모, 무적 0.05~0.40초, 공격 회복 구간에서 취소 가능), 타깃 고정 시 적을 바라봄.
    /// 이동 속도는 CombatManager 상태(공격·방어·경직 중 감속)를 따름.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health), typeof(Stamina))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 3.2f, runSpeed = 6.0f, turnSpeed = 14f, gravity = -22f;
        [Header("회피(구르기)")] public float rollSpeed = 7.8f, rollDuration = 0.66f, rollCost = 18f, iFrameStart = 0.05f, iFrameEnd = 0.40f, rollCooldown = 0.12f, sprintDrain = 9f;
        public Animator animator; public Transform cam;
        public bool Rolling { get; private set; }
        CharacterController cc; Health health; Stamina stamina; CombatManager cm; SkillManager skills; LockOnSystem lockOn; Vector3 vel, rollDir; float speed01, rollT, nextRoll; Vector2 lastInput;

        void Awake()
        {
            cc = GetComponent<CharacterController>(); health = GetComponent<Health>(); stamina = GetComponent<Stamina>(); cm = GetComponent<CombatManager>(); skills = GetComponent<SkillManager>(); lockOn = GetComponent<LockOnSystem>();
            health.invulnerableTime = 0.05f;   // 적의 코인 연타가 각각 들어가도록 짧게
            if (!animator) animator = GetComponentInChildren<Animator>(); if (!cam && Camera.main) cam = Camera.main.transform;
        }

        Vector3 InputDir(Vector2 mv)
        {
            Vector3 f = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward, r = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
            return f * mv.y + r * mv.x;
        }
        bool Locked => cm != null && (cm.State == CombatState.Stagger || cm.State == CombatState.Dead || cm.State == CombatState.Clash);

        void Update()
        {
            if (UIState.Paused) return;
            Vector2 mv = Locked ? Vector2.zero : InputAdapter.Move(); lastInput = mv; Vector3 dir = InputDir(mv);
            if (Rolling) { UpdateRoll(); return; }
            if (!Locked && InputAdapter.DodgePressed() && (cm == null || cm.CanDodge) && Time.time >= nextRoll && stamina.Spend(rollCost)) { StartRoll(dir); return; }

            float mult = cm != null ? cm.MoveMultiplier : 1f;
            bool run = InputAdapter.SprintHeld() && mv.sqrMagnitude > 0.01f && stamina.Current > 1f && mult >= 1f; if (run) stamina.Drain(sprintDrain);
            float target = (run ? runSpeed : walkSpeed) * mv.magnitude * mult;
            bool locked = lockOn != null && lockOn.Target != null;
            if (locked && mult > 0f) { Vector3 to = lockOn.Target.transform.position - transform.position; to.y = 0; if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), turnSpeed * Time.deltaTime); }
            else if (dir.sqrMagnitude > 0.001f && mult > 0.4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            if (cc.isGrounded && vel.y < 0) vel.y = -2f; vel.y += gravity * Time.deltaTime;
            cc.Move((dir.normalized * target + new Vector3(0, vel.y, 0)) * Time.deltaTime);
            cm?.SetLocomotion(target > 0.1f);
            speed01 = Mathf.MoveTowards(speed01, mv.magnitude * mult * (run ? 1f : 0.5f), 6f * Time.deltaTime);
            if (animator) animator.SetFloat("Speed", speed01);
        }

        /// <summary>공격 시작 시 방향: 조준 중이면 대상, 아니면 입력 방향.</summary>
        public void FaceForAttack()
        {
            Vector3 d = lockOn != null && lockOn.Target != null ? lockOn.Target.transform.position - transform.position : InputDir(lastInput); d.y = 0;
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        }

        void StartRoll(Vector3 dir)
        {
            skills?.Cancel(); cm?.SetState(CombatState.Dodge);
            rollDir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward; transform.rotation = Quaternion.LookRotation(rollDir); Rolling = true; rollT = 0f;
            if (animator) animator.CrossFadeInFixedTime("Roll", 0.04f);
        }

        void UpdateRoll()
        {
            rollT += Time.deltaTime; health.Invulnerable = rollT >= iFrameStart && rollT <= iFrameEnd;
            float k = rollT / rollDuration, curve = Mathf.Lerp(1.15f, 0.35f, k * k);
            if (cc.isGrounded && vel.y < 0) vel.y = -2f; vel.y += gravity * Time.deltaTime;
            cc.Move((rollDir * rollSpeed * curve + new Vector3(0, vel.y, 0)) * Time.deltaTime);
            if (rollT >= rollDuration) { EndRoll(); if (animator) animator.CrossFadeInFixedTime("Locomotion", 0.1f); }
        }
        void EndRoll() { Rolling = false; health.Invulnerable = false; nextRoll = Time.time + rollCooldown; if (cm != null && cm.State == CombatState.Dodge) cm.SetState(CombatState.Idle); }

        public void Knockback(Vector3 force) { cc.Move(new Vector3(force.x, 0, force.z) * 0.15f); }
        public void CancelRoll() { if (Rolling) EndRoll(); }
    }
}
