using UnityEngine;

namespace MoominAction
{
    /// <summary>3인칭 궤도 카메라. 마우스/오른쪽 스틱으로 회전(설정의 감도·상하 반전), Tab 조준 중에는 대상이 화면에 들어오도록 자동 회전, 메뉴 중 정지, 벽에 가려지면 당김.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target; public float distance = 5.2f, height = 1.5f, minPitch = -10f, maxPitch = 60f, smooth = 14f, lockOnPitch = 14f;
        float yaw, pitch = 18f; LockOnSystem lockOn;
        static float shakeAmp, shakeT, shakeDur;
        /// <summary>화면 흔들림(합 충돌·승리). 실제 시간 기준이라 히트스톱 중에도 흔들림.</summary>
        public static void Shake(float amplitude, float seconds) { if (amplitude >= shakeAmp * (shakeDur > 0 ? shakeT / shakeDur : 0f)) { shakeAmp = amplitude; shakeT = shakeDur = seconds; } }
        void Start() { if (target) { yaw = target.eulerAngles.y; lockOn = target.GetComponent<LockOnSystem>(); } UIState.Reset(); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        /// <summary>순간이동 뒤: 대상 뒤로 바로 옮김(부드럽게 따라가지 않고).</summary>
        public void SnapBehind()
        {
            if (!target) return; yaw = target.eulerAngles.y;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0); transform.SetPositionAndRotation(target.position + Vector3.up * height + rot * Vector3.back * distance, rot);
        }
        Transform savedTarget; float savedDistance, savedHeight;
        /// <summary>다른 것을 따라가기(통통배) — Restore 로 되돌림.</summary>
        public void Follow(Transform t, float dist, float h)
        {
            if (savedTarget == null) { savedTarget = target; savedDistance = distance; savedHeight = height; }
            target = t; distance = dist; height = h;
        }
        public void Restore() { if (savedTarget != null) { target = savedTarget; distance = savedDistance; height = savedHeight; savedTarget = null; } }

        void LateUpdate()
        {
            if (!target) return;
            if (!UIState.Paused)
            {
                if (lockOn != null && lockOn.Target != null)
                {
                    Vector3 to = lockOn.TargetPoint - target.position; float want = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                    yaw = Mathf.LerpAngle(yaw, want, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime)); pitch = Mathf.Lerp(pitch, lockOnPitch, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
                }
                else { var l = InputAdapter.Look() * GameSettings.MouseSensitivity; yaw += l.x; pitch = Mathf.Clamp(pitch - l.y * (GameSettings.InvertY ? -1f : 1f), minPitch, maxPitch); }
            }
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0); Vector3 pivot = target.position + Vector3.up * height; float d = distance;
            if (Physics.SphereCast(pivot, 0.25f, rot * Vector3.back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(target)) d = Mathf.Max(0.8f, hit.distance);
            Vector3 pos = pivot + rot * Vector3.back * d;
            float k = Time.timeScale > 0 ? 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime) : 0f;
            transform.position = Vector3.Lerp(transform.position, pos, k); transform.rotation = rot;
            if (shakeT > 0f && GameSettings.CameraShake) { shakeT -= Time.unscaledDeltaTime; transform.position += UnityEngine.Random.insideUnitSphere * shakeAmp * Mathf.Clamp01(shakeT / Mathf.Max(0.01f, shakeDur)); }
        }
    }
}
