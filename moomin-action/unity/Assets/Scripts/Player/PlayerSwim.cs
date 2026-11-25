using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 헤엄 · 잠수(바다 = Ocean). PlayerController 는 이 컴포넌트가 Active 인 동안 멈추고, 이동은 여기서.
    /// <b>물 위(Surface)</b>: 발이 수면 아래 0.95m 보다 깊어지면(물에 걸어 들어가거나 다리에서 떨어지면) 첨벙 → 헤엄.
    ///   몸은 파도를 따라 출렁(발 = 수면 − 0.85m, 앞으로 헤엄칠 때는 눕혀서 − 0.35m), 카메라 기준 WASD, Shift = 빨리(기력 소모).
    ///   바닥이 높아져 물이 0.7m 보다 얕아지면 걸어 나감(모래밭 · 다리 끝 계단).
    /// <b>잠수(Dive)</b>: <b>Z</b> 로 들어가고 Z 로 다시 물 위로. 물속은 바라보는 방향(위아래 포함)으로 헤엄, Space = 위, 왼쪽 Ctrl = 아래.
    ///   숨 25초(물 위에서 다시 참) — 다 떨어지면 초당 최대 체력 4% 씩 줄어듦. 입에서 방울 뽀글. 수면까지 올라오면 저절로 물 위 헤엄.
    ///   몸은 움직이는 방향으로 기울임(모델만, 캐릭터 컨트롤러는 그대로).
    /// 헤엄·잠수·배 타기 중에는 공격·방어·구르기 없음(CombatManager · PlayerController).
    /// 애니메이션: SwimIdle · Swim · DiveIdle · Dive · BoatSit(blender/moomin_swim_anims.py) — 없으면 Locomotion 그대로.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerSwim : MonoBehaviour
    {
        public enum Mode { None, Surface, Dive }
        [Header("물에 들어가고 나오기(발이 수면 아래 몇 m)")] public float enterDepth = 0.95f, exitDepth = 0.7f;
        [Header("물 위")] public float treadOffset = 0.85f, proneOffset = 0.35f, swimSpeed = 2.6f, fastSpeed = 4.3f, fastDrain = 7f;
        [Header("잠수")] public float diveSpeed = 3.0f, diveFastSpeed = 4.6f, verticalSpeed = 2.2f, breathMax = 25f, breathRegen = 8f, drownPerSecond = 0.04f;

        public Mode mode { get; private set; }
        public bool InWater => mode != Mode.None;
        /// <summary>통통배에 탄 동안(Boat 가 켜고 끔).</summary>
        public bool OnBoat { get; set; }
        public bool Active => InWater || OnBoat;
        public float Breath { get; private set; }
        public float Breath01 => breathMax > 0 ? Breath / breathMax : 1f;

        CharacterController cc; PlayerController pc; CharacterStats stats; Stamina stamina; Animator anim; Transform model; Transform cam;
        Vector3 vel; float pitch, nextBubble, nextFoam, nextWarn, enterCooldown; string playing = ""; Quaternion modelRot; Vector3 modelPos; bool hasClips;
        static readonly int H_Swim = Animator.StringToHash("Swim");

        void Awake()
        {
            cc = GetComponent<CharacterController>(); pc = GetComponent<PlayerController>(); stats = GetComponent<CharacterStats>(); stamina = GetComponent<Stamina>();
            anim = GetComponentInChildren<Animator>(); if (pc != null) pc.swim = this; Breath = breathMax;
            if (anim != null) { model = anim.transform; modelRot = model.localRotation; modelPos = model.localPosition; }
        }
        void Start() { if (Camera.main != null) cam = Camera.main.transform; hasClips = anim != null && anim.runtimeAnimatorController != null && anim.HasState(0, H_Swim); }

        void Update()
        {
            if (UIState.Paused || OnBoat) return;
            if (stats != null && !stats.IsAlive) { if (InWater) Exit(false); return; }
            if (!Ocean.Exists) { if (InWater) Exit(false); return; }
            if (AirDome.Inside(transform.position + Vector3.up * 0.6f)) { if (InWater) { Exit(false); Toast.Show("공기방울 안이다! 숨을 쉴 수 있어"); } Regen(); return; }   // 용궁 돔 안 = 물 없음
            Vector3 p = transform.position; float surf = Ocean.SurfaceAt(p), submerge = surf - p.y;
            if (mode == Mode.None)
            {
                Regen();
                if (Time.time < enterCooldown || submerge < enterDepth || Ocean.WaterDepth(p.x, p.z) < enterDepth) return;
                Enter(surf);
            }
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            if (InputAdapter.DiveTogglePressed()) { if (mode == Mode.Surface) StartDive(); else if (mode == Mode.Dive) { mode = Mode.Surface; Toast.Show("물 위로!"); } }
            if (mode == Mode.Surface) SurfaceSwim(surf); else DiveSwim(surf);
        }

        // ── 물 위 ─────────────────────────────
        void SurfaceSwim(float surf)
        {
            Regen();
            Vector2 mv = InputAdapter.Move(); Vector3 dir = Flat(mv);
            bool fast = InputAdapter.SprintHeld() && mv.sqrMagnitude > 0.01f && stamina != null && stamina.Current > 1f; if (fast) stamina.Drain(fastDrain);
            Vector3 want = dir * (fast ? fastSpeed : swimSpeed) * Mathf.Clamp01(mv.magnitude);
            vel.x = Mathf.MoveTowards(vel.x, want.x, 6f * Time.deltaTime); vel.z = Mathf.MoveTowards(vel.z, want.z, 6f * Time.deltaTime);
            float moving = Mathf.Clamp01(new Vector2(vel.x, vel.z).magnitude / swimSpeed);
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
            // 바닥: 얕아지면 그 위에 서고, 아주 얕으면 걸어 나감
            float floorY = FloorBelow(out bool hasFloor);
            float targetY = surf - Mathf.Lerp(treadOffset, proneOffset, moving);
            if (hasFloor && floorY > targetY) targetY = floorY;
            if (hasFloor && floorY > surf - exitDepth) { Exit(true); return; }
            float dy = (targetY - transform.position.y) * Mathf.Min(1f, 6f * Time.deltaTime);
            cc.Move(new Vector3(vel.x * Time.deltaTime, dy, vel.z * Time.deltaTime));
            Play(moving > 0.25f ? "Swim" : "SwimIdle", fast ? 1.45f : 1f);
            SetPitch(0f);
            if (moving > 0.3f && Time.time >= nextFoam) { nextFoam = Time.time + 0.18f; WaterFx.Foam(new Vector3(transform.position.x, surf, transform.position.z) - transform.forward * 0.5f, -vel * 0.15f, 1, 0.45f); }
        }

        // ── 물속 ─────────────────────────────
        void StartDive()
        {
            mode = Mode.Dive; vel.y = -1.6f; Toast.Show("잠수! (Space 위 · Ctrl 아래 · Z 물 위로)");
            WaterFx.Splash(new Vector3(transform.position.x, Ocean.SurfaceAt(transform.position), transform.position.z), 10, 2f, 0.8f);
        }
        void DiveSwim(float surf)
        {
            Breath = Mathf.Max(0f, Breath - Time.deltaTime);
            var pal = Palace.Instance;                                                  // 용궁 위 공기방울 기둥: 그 안에서는 숨이 다시 참(길잡이)
            if (pal != null) { Vector3 o = transform.position - pal.Center; o.y = 0f; if (o.magnitude < 3.5f && transform.position.y > pal.Center.y + pal.DomeRadius - 2f) Breath = Mathf.Min(breathMax, Breath + breathRegen * 1.5f * Time.deltaTime); }
            if (Breath <= 0f && stats != null)
            {
                stats.LoseHealth(stats.maxHealth * drownPerSecond * Time.deltaTime, "숨");
                if (Time.time >= nextWarn) { nextWarn = Time.time + 2.5f; Toast.Show("숨이 막혀요! Z 로 물 위로!"); }
            }
            Vector2 mv = InputAdapter.Move();
            Vector3 f = cam != null ? cam.forward : transform.forward, r = cam != null ? cam.right : transform.right; r.y = 0; r.Normalize();
            Vector3 dir = f * mv.y + r * mv.x; float upDown = (InputAdapter.SwimUpHeld() ? 1f : 0f) - (InputAdapter.SwimDownHeld() ? 1f : 0f);
            bool fast = InputAdapter.SprintHeld() && mv.sqrMagnitude > 0.01f && stamina != null && stamina.Current > 1f; if (fast) stamina.Drain(fastDrain);
            Vector3 want = Vector3.ClampMagnitude(dir, 1f) * (fast ? diveFastSpeed : diveSpeed) + Vector3.up * upDown * verticalSpeed;
            vel = Vector3.MoveTowards(vel, want, 5f * Time.deltaTime);
            float top = surf - treadOffset - 0.15f;                                    // 머리가 물 밖으로 나가기 전까지
            if (transform.position.y + vel.y * Time.deltaTime > top)
            {
                if (vel.y > 0.3f && (upDown > 0f || dir.y > 0.3f)) { mode = Mode.Surface; return; }   // 위로 올라오면 저절로 물 위
                vel.y = Mathf.Min(vel.y, (top - transform.position.y) / Mathf.Max(Time.deltaTime, 0.001f));
            }
            cc.Move(vel * Time.deltaTime);
            Vector3 flat = new Vector3(vel.x, 0, vel.z); if (flat.sqrMagnitude > 0.05f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 6f * Time.deltaTime);
            float sp = vel.magnitude; Play(sp > 0.4f ? "Dive" : "DiveIdle", fast ? 1.4f : 1f);
            SetPitch(sp > 0.4f ? Mathf.Clamp(-Mathf.Atan2(vel.y, Mathf.Max(0.3f, flat.magnitude)) * Mathf.Rad2Deg, -55f, 55f) : 0f);
            if (Time.time >= nextBubble) { nextBubble = Time.time + Random.Range(0.6f, 1.3f) * (Breath <= 0f ? 0.4f : 1f); WaterFx.Bubble(transform.position + Vector3.up * 0.95f + transform.forward * 0.35f, Random.Range(2, 5)); }
        }

        // ── 들어가고 나오기 ─────────────────────
        void Enter(float surf)
        {
            mode = surf - transform.position.y > 3f ? Mode.Dive : Mode.Surface;   // 깊은 곳(용궁 돔에서 걸어 나옴 등)이면 바로 물속 헤엄 — 수면까지 순간이동하지 않게
            vel = cc.velocity; vel.y = 0; if (pc != null) pc.ResetFall();
            if (pc != null) pc.CancelRoll();
            WaterFx.Splash(new Vector3(transform.position.x, surf, transform.position.z), 26, 3.5f, 1.6f);
            if (cc.velocity.y < -3f) ThirdPersonCamera.Shake(0.05f, 0.2f);
        }
        void Exit(bool walkOut)
        {
            mode = Mode.None; vel = Vector3.zero; enterCooldown = Time.time + 0.6f; SetPitch(0f, true); if (pc != null) pc.ResetFall();
            if (anim != null && anim.runtimeAnimatorController != null && anim.HasState(0, Animator.StringToHash("Locomotion"))) anim.CrossFadeInFixedTime("Locomotion", 0.25f);
            if (anim != null) anim.speed = 1f; playing = "";
            if (walkOut) Toast.Show("물 밖으로 나왔다");
        }
        /// <summary>배에서 내릴 때 바로 물에 빠짐 등 — 지금 상태를 지움.</summary>
        public void ForceSurface() { if (Ocean.Exists) { mode = Mode.Surface; vel = Vector3.zero; } }
        public void ForceExit() { if (InWater) Exit(false); }

        void Regen() { Breath = Mathf.Min(breathMax, Breath + breathRegen * Time.deltaTime); }
        Vector3 Flat(Vector2 mv)
        {
            Vector3 f = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward, r = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
            return f * mv.y + r * mv.x;
        }
        float FloorBelow(out bool hit)
        {
            Vector3 o = transform.position + Vector3.up * 1.2f;
            hit = Physics.SphereCast(o, 0.25f, Vector3.down, out var h, 4f, ~0, QueryTriggerInteraction.Ignore) && !h.transform.IsChildOf(transform) && h.collider.GetComponentInParent<Boat>() == null;
            return hit ? h.point.y : float.NegativeInfinity;
        }
        void Play(string state, float speed)
        {
            if (anim == null) return;
            anim.speed = speed;
            if (!hasClips || playing == state) return;
            playing = state; anim.CrossFadeInFixedTime(state, 0.3f);
        }
        /// <summary>모델만 앞뒤로 기울임(가슴 높이 기준).</summary>
        void SetPitch(float deg, bool instant = false)
        {
            if (model == null) return;
            pitch = instant ? deg : Mathf.LerpAngle(pitch, deg, 1f - Mathf.Exp(-6f * Time.deltaTime));
            var q = Quaternion.AngleAxis(pitch, Vector3.right); Vector3 pivot = new Vector3(0, 0.45f, 0);
            model.localRotation = q * modelRot; model.localPosition = modelPos + pivot - q * pivot;
        }
    }
}
