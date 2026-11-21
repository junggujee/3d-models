using UnityEngine;

namespace MoominAction
{
    /// <summary>3인칭 궤도 카메라. 마우스/오른쪽 스틱으로 회전, 벽에 가려지면 가까이 당김.</summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        public Transform target; public float distance = 5.2f, height = 1.5f, sensitivity = 1f, minPitch = -10f, maxPitch = 60f, smooth = 14f;
        float yaw, pitch = 18f; bool captured;
        void Start() { if (target) yaw = target.eulerAngles.y; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; captured = true; }
        void LateUpdate()
        {
            if (!target) return;
            if (InputAdapter.EscapePressed()) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; captured = false; }
            if (!captured && InputAdapter.AttackPressed()) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; captured = true; }
            if (captured) { var l = InputAdapter.Look() * sensitivity; yaw += l.x; pitch = Mathf.Clamp(pitch - l.y, minPitch, maxPitch); }
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0); Vector3 pivot = target.position + Vector3.up * height;
            float d = distance;
            if (Physics.SphereCast(pivot, 0.25f, rot * Vector3.back, out var hit, distance, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(target)) d = Mathf.Max(0.8f, hit.distance);
            Vector3 pos = pivot + rot * Vector3.back * d;
            transform.position = Vector3.Lerp(transform.position, pos, 1f - Mathf.Exp(-smooth * Time.deltaTime)); transform.rotation = rot;
        }
    }
}
