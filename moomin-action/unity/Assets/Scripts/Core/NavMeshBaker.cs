using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    /// <summary>
    /// 실행 시 NavMesh 를 구움(추가 패키지 없이 UnityEngine.AI 만 사용). 바닥·도시의 콜라이더를 모아 적이 걸어 다닐 길을 만든다.
    /// 캐릭터(Player·Enemy 레이어)는 제외. GameManager 가 첫 웨이브 전에 호출.
    /// </summary>
    public class NavMeshBaker : MonoBehaviour
    {
        public float size = 420f; public int agentTypeID = 0;   // size: 성벽 안 전체(약 360m)
        NavMeshDataInstance instance; public bool Built { get; private set; }

        public void Build()
        {
            if (Built) return;
            int mask = ~0; int pl = LayerMask.NameToLayer("Player"), en = LayerMask.NameToLayer("Enemy"); if (pl >= 0) mask &= ~(1 << pl); if (en >= 0) mask &= ~(1 << en);
            var bounds = new Bounds(transform.position, new Vector3(size, 80f, size));
            var sources = new List<NavMeshBuildSource>(); var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(bounds, mask, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            sources.RemoveAll(s => s.component != null && (s.component.GetComponentInParent<CharacterStats>() != null || s.component.GetComponentInParent<ItemPickup>() != null));
            var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(agentTypeID), sources, bounds, Vector3.zero, Quaternion.identity);
            if (data != null) { instance = NavMesh.AddNavMeshData(data); Built = true; }
            Debug.Log($"[NavMeshBaker] NavMesh 생성 {(Built ? "완료" : "실패")} — 소스 {sources.Count}개");
        }
        void OnDestroy() { if (instance.valid) instance.Remove(); }
    }
}
