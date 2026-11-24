using UnityEngine;

namespace MoominAction.UI
{
    /// <summary>
    /// 캐릭터 시트의 3D 모습(아치 틀 안): 멈춘 게임 속 실제 민미(장비 모양 포함)를 앞에서 비추는 카메라 → RenderTexture.
    /// 플레이어 레이어만 그려 배경은 투명. 전용 조명(플레이어 레이어에만). 화면을 끌면 yaw 로 돌려 봄.
    /// </summary>
    public class EquipmentPreview
    {
        Camera cam; Light key, rim; RenderTexture rt; Transform target; public float yaw = 18f;
        public RenderTexture Texture => rt;

        public void Begin(Transform player, int w, int h)
        {
            target = player;
            if (rt == null) { rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { name = "EquipmentPreview", antiAliasing = 4 }; }
            if (cam == null)
            {
                var go = new GameObject("EquipmentPreviewCamera"); Object.DontDestroyOnLoad(go); go.hideFlags = HideFlags.DontSave;
                cam = go.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0, 0, 0, 0); cam.fieldOfView = 26f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 20f;
                key = new GameObject("Key").AddComponent<Light>(); key.transform.SetParent(go.transform, false); key.type = LightType.Directional; key.intensity = 1.1f; key.color = new Color(1f, .95f, .88f);
                rim = new GameObject("Rim").AddComponent<Light>(); rim.transform.SetParent(go.transform, false); rim.type = LightType.Directional; rim.intensity = 0.7f; rim.color = new Color(.65f, .8f, 1f);
            }
            int layer = player.gameObject.layer, mask = layer != 0 ? 1 << layer : ~0;   // 플레이어 레이어가 있으면 플레이어만(배경 투명)
            cam.cullingMask = mask; key.cullingMask = mask; rim.cullingMask = mask; cam.targetTexture = rt; cam.gameObject.SetActive(true); Update();
        }

        public void Update()
        {
            if (cam == null || target == null) return;
            Vector3 c = target.position + Vector3.up * 0.62f;
            var dir = Quaternion.Euler(0, yaw, 0) * target.forward;
            cam.transform.position = c + dir * 3.3f + Vector3.up * 0.25f; cam.transform.LookAt(c);
            key.transform.rotation = Quaternion.LookRotation(Quaternion.Euler(30, -40, 0) * (c - cam.transform.position).normalized);
            rim.transform.rotation = Quaternion.LookRotation(-(c - cam.transform.position).normalized + Vector3.down * 0.4f);
        }

        public void End() { if (cam != null) cam.gameObject.SetActive(false); }
    }
}
