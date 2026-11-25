using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    /// <summary>HuntCatalog 의 구역마다 HuntZone 을 만듦(GameManager 가 NavMesh 를 구운 뒤 붙임). 자리 = Blender 좌표 → 게임 좌표 → 걸을 수 있는 곳(없으면 시작 위치 둘레).</summary>
    public class HuntZoneSpawner : MonoBehaviour
    {
        public List<GameObject> prefabs = new();

        void Start()
        {
            var pl = GameObject.FindGameObjectWithTag("Player"); Vector3 start = pl != null ? pl.transform.position : Vector3.zero; int k = 0;
            foreach (var h in HuntCatalog.All)
            {
                if (HuntZone.Find(h.zoneId) != null) continue;
                GameObject prefab = null; foreach (var p in prefabs) if (p != null && p.name == h.monsterKind) { prefab = p; break; }
                if (prefab == null) { Debug.LogWarning($"[토벌] {h.monsterKind} 프리팹이 GameManager.enemyPrefabs 에 없음 — {h.zoneName} 비활성"); continue; }
                Vector3 want = MapPlaces.BlenderToWorld(h.zoneBx, h.zoneBy);
                if (!Walkable(want, out var at))
                {   // 지도 자리가 걸을 수 없으면 시작 위치 둘레에 나눠 놓음
                    float a = k * Mathf.PI * 0.5f + 0.4f; if (!Walkable(start + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 28f, out at)) continue;
                }
                var go = new GameObject("HuntZone_" + h.zoneId); go.transform.position = at;
                var z = go.AddComponent<HuntZone>(); z.zoneId = h.zoneId; z.zoneName = h.zoneName; z.kind = h.monsterKind; z.monsterName = h.monsterName;
                z.prefab = prefab; z.radius = h.zoneRadius; z.maxAlive = h.maxAlive; z.minLevel = h.minLevel; k++;
            }
        }
        static bool Walkable(Vector3 p, out Vector3 at)
        {
            at = p; foreach (float r in new[] { 4f, 10f, 20f }) if (NavMesh.SamplePosition(p, out var h, r, NavMesh.AllAreas)) { at = h.position; return true; }
            return false;
        }
    }
}
