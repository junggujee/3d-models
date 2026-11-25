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
        /// <summary>성벽 밖 따로 구울 곳(선착장 — Seaside 가 넣음).</summary>
        public static readonly List<Bounds> Extra = new List<Bounds>();
        readonly List<NavMeshDataInstance> extras = new List<NavMeshDataInstance>();

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
            foreach (var b in Extra)
            {   // 따로 굽는 곳(선착장): 같은 방식, 그 상자 안만
                var src = new List<NavMeshBuildSource>(); NavMeshBuilder.CollectSources(b, mask, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), src);
                src.RemoveAll(s => s.component != null && (s.component.GetComponentInParent<CharacterStats>() != null || s.component.GetComponentInParent<ItemPickup>() != null || s.component.GetComponentInParent<Boat>() != null));
                var d2 = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(agentTypeID), src, b, Vector3.zero, Quaternion.identity);
                if (d2 != null) extras.Add(NavMesh.AddNavMeshData(d2));
            }
            Debug.Log($"[NavMeshBaker] NavMesh 생성 {(Built ? "완료" : "실패")} — 소스 {sources.Count}개");
        }
        void OnDestroy() { if (instance.valid) instance.Remove(); foreach (var e in extras) if (e.valid) e.Remove(); extras.Clear(); Extra.Clear(); }
    }
}
