using UnityEngine;

namespace MoominAction
{
    /// <summary>3인칭 궤도 카메라. 마우스/오른쪽 스틱으로 회전(설정의 감도·상하 반전 적용), 메뉴가 열리면 멈춤, 벽에 가려지면 가까이 당김.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target; public float distance = 5.2f, height = 1.5f, minPitch = -10f, maxPitch = 60f, smooth = 14f;
        float yaw, pitch = 18f;
        void Start() { if (target) yaw = target.eulerAngles.y; UIState.Reset(); Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        void LateUpdate()
        {
            if (!target) return;
            if (!UIState.Paused) { var l = InputAdapter.Look() * GameSettings.MouseSensitivity; yaw += l.x; pitch = Mathf.Clamp(pitch - l.y * (GameSettings.InvertY ? -1f : 1f), minPitch, maxPitch); }
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0); Vector3 pivot = target.position + Vector3.up * height; float d = distance;
            if (Physics.SphereCast(pivot, 0.25f, rot * Vector3.back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(target)) d = Mathf.Max(0.8f, hit.distance);
            Vector3 pos = pivot + rot * Vector3.back * d;
            float k = Time.timeScale > 0 ? 1f - Mathf.Exp(-smooth * Time.deltaTime) : 0f;
            transform.position = Vector3.Lerp(transform.position, pos, k); transform.rotation = rot;
        }
    }
}
