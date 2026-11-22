using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 타이틀 배경 카메라: 성과 그 뒤 산맥(산지) 사이를 중심으로, 마을 광장 쪽에서 천천히 오가며 돎(±swing° 를 period 초에 한 번 — 맵 가장자리가 보이지 않게).
    /// 렌즈 이동(lensShift)으로 성을 화면 오른쪽에 두어 왼쪽 제목·메뉴와 겹치지 않음.
    /// 값은 blender/render_title_bg.py(미리보기 렌더)와 같음. 장소 좌표는 MapPlaces(월드맵 데이터).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class TitleCamera : MonoBehaviour
    {
        [Tooltip("성(0) ~ 산지(1) 사이 어디를 중심으로")] [Range(0, 1)] public float towardHighland = 0.35f;
        public float radius = 175f, height = 30f, lookUp = 34f;
        [Tooltip("좌우로 오가는 각도(도)")] public float swing = 22f; [Tooltip("한 번 오가는 시간(초)")] public float period = 110f;
        [Tooltip("위아래로 살짝 떠다님(m)")] public float bob = 2.5f;
        [Tooltip("화면 가로 기준 렌즈 이동(음수 = 중심이 오른쪽으로)")] public float lensShiftX = -0.16f;
        Vector3 center; float baseAngle; bool ready;

        void Start()
        {
            var cam = GetComponent<Camera>();
            cam.usePhysicalProperties = true; cam.sensorSize = new Vector2(36f, 20.25f); cam.focalLength = 26f; cam.gateFit = Camera.GateFitMode.Horizontal;
            cam.lensShift = new Vector2(lensShiftX, 0); cam.farClipPlane = 3000f; cam.nearClipPlane = 0.5f;
            bool hasCastle = MapPlaces.TryWorldPosition("castle", out var castle), hasHigh = MapPlaces.TryWorldPosition("highland", out var high), hasPlaza = MapPlaces.TryWorldPosition("plaza", out var plaza);
            if (!hasCastle)
            {   // 데이터가 없으면 "Castle" 분류의 렌더러 중심
                var c = GameObject.Find("Castle"); var b = new Bounds(Vector3.zero, Vector3.zero); bool any = false;
                if (c != null) foreach (var r in c.GetComponentsInChildren<Renderer>()) { if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
                castle = any ? b.center : Vector3.zero; high = castle; plaza = castle + Vector3.back * 100f;
            }
            if (!hasHigh) high = castle; if (!hasPlaza) plaza = castle + Vector3.back * 100f;
            center = Vector3.Lerp(castle, high, towardHighland); center.y = 0f;
            var toPlaza = plaza - center; baseAngle = Mathf.Atan2(toPlaza.z, toPlaza.x);
            ready = true; Place(0f);
        }

        void LateUpdate() { if (ready) Place(Time.unscaledTime); }

        void Place(float t)
        {
            float a = baseAngle + swing * Mathf.Deg2Rad * Mathf.Sin(t * 2f * Mathf.PI / Mathf.Max(1f, period));
            var pos = center + new Vector3(Mathf.Cos(a) * radius, height + bob * Mathf.Sin(t * 0.37f), Mathf.Sin(a) * radius);
            transform.position = pos; transform.rotation = Quaternion.LookRotation(center + Vector3.up * lookUp - pos, Vector3.up);
        }
    }
}
