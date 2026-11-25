using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 통통배(blender/build_boat.py → Resources/World/Boat). 선착장 끝 오른쪽에 매여 있음(Seaside 가 만듦).
    ///  · 타기: 가까이(헤엄 중이어도) "F  통통배 타기" → 민미가 뒤 의자에 앉고(BoatSit) 카메라가 배를 따라감
    ///  · 몰기: W/S = 앞/뒤(앞 최고 9m/s · 뒤 3m/s, Shift = 힘껏 12m/s), A/D = 방향(빠를수록 잘 돎)
    ///  · 내리기: F — 선착장 옆(Mark_Dock 9m 안)이면 다리 위로, 아니면 배 옆 물에 풍덩(헤엄)
    ///  · 물에 뜨기: 앞·뒤·좌·우 네 점의 수면 높이(Ocean, 셰이더와 같은 파도) → 높이 · 앞뒤 기울기 · 좌우 기울기(부드럽게), 빠르면 뱃머리가 살짝 들림
    ///  · 부딪힘: 앞으로 구(반지름 1.1m) 쏘아 다리·바위에 닿으면 튕김, 바닥이 0.9m 보다 얕으면(모래밭) 멈춤, 선착장에서 900m 넘게 가면 되돌림
    ///  · 연출: 굴뚝 연기 "통…통…"(빠를수록 자주) · 배 뒤 하얀 물결 · 빠르면 뱃머리 물보라 · 프로펠러 회전 · 깃발 펄럭
    /// 물리(Rigidbody) 없이 위치를 직접 움직임 — 가볍고 흔들림이 없음.
    /// </summary>
    public class Boat : MonoBehaviour
    {
        public static Boat Current { get; private set; }
        public float maxSpeed = 9f, boostSpeed = 12f, reverseSpeed = 3f, accel = 3.2f, turnRate = 50f, range = 900f;
        public bool Driving { get; private set; }

        Transform seat, smoke, wake, bow, prop, flag; Quaternion propBase, flagBase; float speed, yaw, bob, puffT, foamT, spray, propAngle, heightV;
        Transform player; PlayerSwim swim; CharacterController cc; Animator anim; Vector3 home; float stuckMsg;

        void Awake() { Current = this; }
        void OnDestroy() { if (Current == this) Current = null; }

        void Start()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "Mark_Seat": seat = t; break; case "Mark_Smoke": smoke = t; break; case "Mark_Wake": wake = t; break; case "Mark_Bow": bow = t; break;
                    case "Boat_Prop": prop = t; break; case "Boat_Flag": flag = t; break;
                }
            }
            if (prop != null) propBase = prop.localRotation; if (flag != null) flagBase = flag.localRotation;
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true)) { var r = mf.GetComponent<MeshRenderer>(); if (r != null) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            var bc = gameObject.GetComponent<BoxCollider>(); if (bc == null) bc = gameObject.AddComponent<BoxCollider>();   // 헤엄칠 때 부딪히는 몸통
            bc.center = new Vector3(0, 0.3f, 0); bc.size = new Vector3(1.7f, 1.2f, 4.4f);
            Seaside.GlowLamps(gameObject);
            yaw = transform.eulerAngles.y; home = transform.position;
            var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) { player = p.transform; swim = p.GetComponent<PlayerSwim>(); cc = p.GetComponent<CharacterController>(); anim = p.GetComponentInChildren<Animator>(); }
        }

        void Update()
        {
            if (UIState.Paused) return;
            float dt = Time.deltaTime;
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) Start(); }
            if (Driving) Drive(dt);
            else
            {
                speed = Mathf.MoveTowards(speed, 0f, 2f * dt);
                transform.position += transform.forward * speed * dt;
                if (player != null)
                {
                    Vector3 d = player.position - transform.position; d.y = 0; float dist = d.magnitude;
                    if (dist < 3.6f && Mathf.Abs(player.position.y - transform.position.y) < 3f && (swim == null || !swim.OnBoat)) Interaction.Offer(this, dist, "F   통통배 타기", Board);
                }
            }
            Float(dt); Effects(dt);
        }

        // ── 몰기 ─────────────────────────────
        void Drive(float dt)
        {
            Vector2 mv = InputAdapter.Move(); bool boost = InputAdapter.SprintHeld() && mv.y > 0.1f;
            float target = mv.y > 0 ? mv.y * (boost ? boostSpeed : maxSpeed) : mv.y * reverseSpeed;
            speed = Mathf.MoveTowards(speed, target, (Mathf.Abs(target) < Mathf.Abs(speed) ? accel * 1.6f : accel) * dt);
            float turnK = Mathf.Clamp(Mathf.Abs(speed) / 3f, 0.25f, 1f) * (speed < -0.1f ? -1f : 1f);
            yaw += mv.x * turnRate * turnK * dt;
            Vector3 fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward, step = fwd * speed * dt;
            Vector3 probe = transform.position + Vector3.up * 0.6f; float look = Mathf.Abs(step.magnitude) + 1.4f;
            if (Mathf.Abs(speed) > 0.05f && Physics.SphereCast(probe, 1.0f, fwd * Mathf.Sign(speed), out var hit, look, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(transform) && (player == null || !hit.transform.IsChildOf(player)))
            {
                if (Mathf.Abs(speed) > 2.5f) { ThirdPersonCamera.Shake(0.08f, 0.25f); WaterFx.Splash(hit.point, 12, 2f, 1f); }
                speed = -speed * 0.3f; step = Vector3.zero;
            }
            Vector3 next = transform.position + step + fwd * Mathf.Sign(speed) * 2.3f;
            if (Ocean.WaterDepth(next.x, next.z) < 0.9f) { if (speed * Vector3.Dot(step, fwd) > 0) { speed = 0f; step = Vector3.zero; Msg("너무 얕아서 더 못 가요"); } }
            var sea = Seaside.Instance; Vector3 from = sea != null ? sea.HarborSea : home; Vector3 off = transform.position + step - from; off.y = 0;
            if (off.magnitude > range && Vector3.Dot(off, step) > 0) { step = Vector3.zero; speed *= 0.5f; Msg("더 멀리 가면 위험해요! 돌아가요"); }
            transform.position += step;
            Interaction.Offer(this, 0.01f, "F   내리기  ·  W/S 앞뒤 · A/D 방향 · Shift 힘껏", Leave);
        }
        void Msg(string m) { if (Time.time >= stuckMsg) { stuckMsg = Time.time + 2.5f; Toast.Show(m); } }

        // ── 물에 뜨기 ───────────────────────────
        void Float(float dt)
        {
            if (!Ocean.Exists) return;
            Vector3 p = transform.position, f = Quaternion.Euler(0, yaw, 0) * Vector3.forward, r = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            float hb = Ocean.SurfaceAt(p + f * 1.9f), hs = Ocean.SurfaceAt(p - f * 1.9f), hl = Ocean.SurfaceAt(p - r * 0.8f), hr = Ocean.SurfaceAt(p + r * 0.8f);
            float h = (hb + hs + hl + hr) * 0.25f;
            float y = Mathf.SmoothDamp(p.y, h + 0.02f, ref heightV, 0.25f);
            float pitchDeg = Mathf.Atan2(hs - hb, 3.8f) * Mathf.Rad2Deg - Mathf.Clamp01(Mathf.Abs(speed) / boostSpeed) * 4f * Mathf.Sign(speed);
            float rollDeg = Mathf.Atan2(hl - hr, 1.6f) * Mathf.Rad2Deg * 0.7f;
            var want = Quaternion.Euler(pitchDeg, yaw, rollDeg);
            transform.SetPositionAndRotation(new Vector3(p.x, y, p.z), Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-4f * dt)));
        }

        // ── 연출 ─────────────────────────────
        void Effects(float dt)
        {
            float k = Mathf.Clamp01(Mathf.Abs(speed) / maxSpeed);
            if (prop != null) { propAngle += (Driving ? 400f + 1600f * k : 0f) * dt; prop.localRotation = Quaternion.AngleAxis(propAngle, prop.parent.InverseTransformDirection(transform.forward)) * propBase; }
            if (flag != null) { float a = Mathf.Sin(Time.time * (6f + 8f * k)) * (8f + 18f * k); flag.localRotation = Quaternion.AngleAxis(a, flag.parent.InverseTransformDirection(transform.up)) * flagBase; }
            if (!Driving) return;
            puffT -= dt;
            if (smoke != null && puffT <= 0f) { puffT = Mathf.Lerp(0.55f, 0.2f, k); WaterFx.Smoke(smoke.position, -transform.forward * speed * 0.25f + Vector3.right * 0.1f, 0.8f + 0.5f * k); }   // 통…통…
            if (k > 0.08f && wake != null)
            {
                foamT -= dt; float sy = Ocean.SurfaceAt(wake.position);
                if (foamT <= 0f) { foamT = Mathf.Lerp(0.12f, 0.035f, k); WaterFx.Foam(new Vector3(wake.position.x, sy, wake.position.z), -transform.forward * speed * 0.15f, 2, 0.6f + 0.6f * k); }
                spray -= dt;
                if (k > 0.55f && bow != null && spray <= 0f) { spray = 0.12f; var bp = bow.position; WaterFx.Splash(new Vector3(bp.x, Ocean.SurfaceAt(bp), bp.z), 6, 1.6f + 2f * k, 1.2f); }
            }
        }

        // ── 타고 내리기 ─────────────────────────
        void Board()
        {
            if (player == null || Driving) return;
            if (swim != null) { swim.ForceExit(); swim.OnBoat = true; }
            if (cc != null) cc.enabled = false;
            player.SetParent(seat != null ? seat : transform, false); player.localPosition = new Vector3(0, -0.2f, 0); player.localRotation = Quaternion.identity;
            if (anim != null && anim.runtimeAnimatorController != null && anim.HasState(0, Animator.StringToHash("BoatSit"))) anim.CrossFadeInFixedTime("BoatSit", 0.3f);
            var cam = Camera.main; if (cam != null && cam.TryGetComponent<ThirdPersonCamera>(out var tpc)) tpc.Follow(transform, 9.5f, 2.4f);
            Driving = true; Toast.Show("통통배 출발! 통… 통… 통…");
        }
        void Leave()
        {
            if (!Driving || player == null) return;
            Driving = false; var sea = Seaside.Instance; Vector3 dock = sea != null ? sea.Dock : transform.position;
            player.SetParent(null, true);
            Vector3 side = transform.position + transform.right * 1.6f; bool atDock = sea != null && Vector3.Distance(new Vector3(dock.x, 0, dock.z), new Vector3(transform.position.x, 0, transform.position.z)) < 9f;
            Vector3 dest = atDock ? dock : new Vector3(side.x, Ocean.SurfaceAt(side) - 0.85f, side.z);
            Vector3 f = transform.forward; f.y = 0; player.SetPositionAndRotation(dest, f.sqrMagnitude > 0.01f ? Quaternion.LookRotation(f) : Quaternion.identity);
            if (cc != null) cc.enabled = true; Physics.SyncTransforms();
            if (swim != null) { swim.OnBoat = false; if (!atDock) swim.ForceSurface(); }
            if (player.TryGetComponent<PlayerController>(out var pc)) pc.ResetFall();
            if (anim != null && anim.runtimeAnimatorController != null && anim.HasState(0, Animator.StringToHash(atDock ? "Locomotion" : "SwimIdle"))) anim.CrossFadeInFixedTime(atDock ? "Locomotion" : "SwimIdle", 0.25f);
            var cam = Camera.main; if (cam != null && cam.TryGetComponent<ThirdPersonCamera>(out var tpc)) tpc.Restore();
            if (!atDock) WaterFx.Splash(new Vector3(dest.x, Ocean.SurfaceAt(dest), dest.z), 20, 3f, 1.4f);
            Toast.Show(atDock ? "선착장에 내렸다" : "풍덩!");
        }
    }
}
