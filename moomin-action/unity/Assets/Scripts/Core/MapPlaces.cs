using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 맵의 주요 장소(성·마을 광장·산지·동쪽/서쪽 마을). 월드맵 데이터(Resources/UI/WorldMap/WorldMapData.json)의 Blender 좌표를 게임 좌표와 맞춤.
    /// 쓰는 곳: 지도(내 위치), 저장 슬롯의 "장소" 이름, 타이틀 화면 카메라(성·산맥을 비춤).
    /// 좌표 맞추기: FBX 축 변환이 설정마다 달라 PlayerSpawn 위치 ↔ 데이터의 spawnBlender 로 8가지(축 바꿈 × 부호) 중 가장 맞는 것을 고름.
    /// 씬이 바뀌면 다시 맞춤(PlayerSpawn 이 다른 물체일 수 있음).
    /// </summary>
    public static class MapPlaces
    {
        [Serializable] class Landmark { public string id, name; public float[] blender; }
        [Serializable] class Ground { public float[] origin, perX, perY; }
        [Serializable] class MapData { public float[] spawnBlender; public Landmark[] landmarks; public Ground ground; public int[] image; }
        static MapData data; static bool loaded; static Matrix4x4 w2b = Matrix4x4.identity, b2w = Matrix4x4.identity; static int calibratedScene = -1;

        static MapData Data
        {
            get
            {
                if (loaded) return data; loaded = true;
                var json = Resources.Load<TextAsset>("UI/WorldMap/WorldMapData");
                if (json != null) { try { data = JsonUtility.FromJson<MapData>(json.text); } catch (Exception e) { Debug.LogWarning("[MapPlaces] 지도 데이터를 읽지 못함: " + e.Message); } }
                return data;
            }
        }

        /// <summary>게임 좌표 → 지도를 만든 Blender 좌표(x, y 만 의미 있음).</summary>
        public static Matrix4x4 WorldToBlender { get { Calibrate(); return w2b; } }

        static void Calibrate()
        {
            int scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
            if (calibratedScene == scene) return; calibratedScene = scene;
            var sp = GameObject.Find("PlayerSpawn"); var d = Data;
            w2b = Matrix4x4.identity; w2b.SetRow(1, new Vector4(0, 0, 1, 0)); w2b.SetRow(2, new Vector4(0, 1, 0, 0));
            if (sp == null || d == null || d.spawnBlender == null) { b2w = w2b.inverse; return; }
            Vector3 u = sp.transform.position; var want = new Vector2(d.spawnBlender[0], d.spawnBlender[1]); float best = float.MaxValue;
            for (int swap = 0; swap < 2; swap++) for (int sx = -1; sx <= 1; sx += 2) for (int sy = -1; sy <= 1; sy += 2)
            {
                var m = Matrix4x4.zero; m.m33 = 1; m.m21 = 1;
                if (swap == 0) { m.m00 = sx; m.m12 = sy; } else { m.m02 = sx; m.m10 = sy; }
                Vector3 p = m.MultiplyPoint3x4(u); float len = new Vector2(p.x, p.y).magnitude; float s = len > 0.01f ? want.magnitude / len : 1f;
                var ms = Matrix4x4.Scale(new Vector3(s, s, 1)) * m; Vector3 q = ms.MultiplyPoint3x4(u); float err = (new Vector2(q.x, q.y) - want).sqrMagnitude;
                if (err < best) { best = err; w2b = ms; }
            }
            b2w = w2b.inverse;
        }

        /// <summary>Blender 좌표(x, y) → 게임 좌표(높이는 위에서 쏜 레이, 맞지 않으면 0).</summary>
        public static Vector3 BlenderToWorld(float bx, float by)
        {
            Calibrate(); var pos = b2w.MultiplyPoint3x4(new Vector3(bx, by, 0)); pos.y = 0;
            if (Physics.Raycast(pos + Vector3.up * 400f, Vector3.down, out var hit, 800f)) pos.y = hit.point.y;
            return pos;
        }

        /// <summary>장소 id(castle·plaza·highland·east·west)의 게임 좌표. 높이는 위에서 아래로 쏜 레이로(맞지 않으면 0). 없으면 false.</summary>
        public static bool TryWorldPosition(string id, out Vector3 pos)
        {
            pos = Vector3.zero; var d = Data; if (d == null || d.landmarks == null) return false;
            foreach (var lm in d.landmarks)
            {
                if (lm.id != id || lm.blender == null || lm.blender.Length < 2) continue;
                Calibrate(); pos = b2w.MultiplyPoint3x4(new Vector3(lm.blender[0], lm.blender[1], 0)); pos.y = 0;
                if (Physics.Raycast(pos + Vector3.up * 400f, Vector3.down, out var hit, 800f)) pos.y = hit.point.y;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 두 게임 좌표 사이 방향을 <b>월드맵 그림 기준</b>(오른쪽 = 동, 위 = 북)으로: (동쪽 성분, 북쪽 성분) 픽셀 단위.
        /// 맵 그림은 마을 축이 가로가 되게 돌려 그렸으므로 Blender +Y 가 북쪽이 아님 — 방향 글자(토벌 구역 등)는 이것으로.
        /// </summary>
        public static Vector2 MapDelta(Vector3 fromWorld, Vector3 toWorld)
        {
            Calibrate(); var a = w2b.MultiplyPoint3x4(fromWorld); var b = w2b.MultiplyPoint3x4(toWorld); float dx = b.x - a.x, dy = b.y - a.y; var d = Data;
            if (d == null || d.ground == null || d.ground.perX == null || d.ground.perY == null) return new Vector2(dx, dy);
            float w = d.image != null && d.image.Length >= 2 ? d.image[0] : 2048f, h = d.image != null && d.image.Length >= 2 ? d.image[1] : 1280f;
            return new Vector2((d.ground.perX[0] * dx + d.ground.perY[0] * dy) * w, (d.ground.perX[1] * dx + d.ground.perY[1] * dy) * h);
        }

        /// <summary>가장 가까운 주요 장소 이름(저장 슬롯 표시용). 데이터가 없으면 "도시".</summary>
        public static string NearestName(Vector3 world)
        {
            if (Palace.Near(world)) return "바다 밑 용궁";
            if (Seaside.InHarbor(world)) return "남쪽 선착장";
            var d = Data; if (d == null || d.landmarks == null || d.landmarks.Length == 0) return "도시";
            Calibrate(); var p = w2b.MultiplyPoint3x4(world); string best = "도시"; float bd = float.MaxValue;
            foreach (var lm in d.landmarks)
            {
                if (lm.blender == null || lm.blender.Length < 2) continue;
                float dd = (new Vector2(p.x, p.y) - new Vector2(lm.blender[0], lm.blender[1])).sqrMagnitude;
                if (dd < bd) { bd = dd; best = lm.name; }
            }
            return best;
        }
    }
}
