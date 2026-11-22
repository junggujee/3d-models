#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MoominAction.EditorTools
{
    /// <summary>
    /// 맵 구조와 규칙(docs/MAP.md). City.fbx(blender/build_map.py 최적화 출력)는 이렇게 생김:
    ///   World ─ Terrain · Castle · Town · Foliage   (구역마다 *_LOD0/1/2 → Unity 가 LOD Group 자동 생성)
    ///         ├ Collision                         (구역마다 충돌 메시 — 화면에는 안 그림)
    ///         ├ Props · Interiors · Decor        (빈 자리: 이후 소품·인테리어를 여기 넣음)
    ///         └ PlayerSpawn
    /// "맵 정리" 메뉴는 이 규칙을 World 아래 전부에 다시 적용 — 새 소품을 넣은 뒤에도 한 번 누르면 정적·충돌·LOD 가 맞춰짐.
    /// </summary>
    public static class MapTools
    {
        const StaticEditorFlags Solid = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
        const StaticEditorFlags Thin = StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic;
        /// <summary>LOD 전환 화면 높이(LOD0→1, 1→2, 마지막 = 이보다 작으면 안 그림).</summary>
        static readonly Dictionary<string, float[]> LodHeights = new()
        {
            { "Town", new[] { 0.30f, 0.10f, 0.012f } }, { "Castle", new[] { 0.25f, 0.08f, 0.008f } },
            { "Terrain", new[] { 0.20f, 0.06f, 0.003f } }, { "Foliage", new[] { 0.22f, 0.05f } },
        };
        public const string NoColliderPrefix = "NoCol_";
        public const float SmallPropSize = 0.5f, CullPropSize = 2f;

        [MenuItem("Moomin/맵 정리 (새 소품·인테리어 포함)")]
        public static void ApplyMenu()
        {
            var w = GameObject.Find("World");
            if (w == null) { EditorUtility.DisplayDialog("맵 정리", "씬에 World 오브젝트가 없습니다. 메뉴 4번(메인 씬 만들기)으로 맵을 배치하세요.", "확인"); return; }
            Undo.RegisterFullObjectHierarchyUndo(w, "맵 정리"); Apply(w.transform);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(w.scene);
        }

        /// <summary>World 아래 전체에 규칙 적용. 반환: 처리한 렌더러 수.</summary>
        public static int Apply(Transform world)
        {
            int renderers = 0, colliders = 0, lods = 0, props = 0;
            // 분류 이름으로 찾음(맵이 여러 FBX 로 나뉘어 World 아래 한 단계 더 깊이 있을 수 있음: World/City_TownW/TownW). Town* = 마을.
            foreach (var cat in world.GetComponentsInChildren<Transform>(true))
            {
                string key = cat.name.StartsWith("Town") ? "Town" : cat.name;
                if (cat != world && cat.parent != world && cat.parent.parent != world) continue;
                switch (key)
                {
                    case "Collision":
                        foreach (var mf in cat.GetComponentsInChildren<MeshFilter>(true))
                        {
                            if (!mf.TryGetComponent<MeshCollider>(out var mc)) { mc = mf.gameObject.AddComponent<MeshCollider>(); colliders++; }
                            mc.sharedMesh = mf.sharedMesh; if (mf.TryGetComponent<MeshRenderer>(out var r)) r.enabled = false;
                            GameObjectUtility.SetStaticEditorFlags(mf.gameObject, 0);
                        }
                        break;
                    case "Terrain": case "Castle": case "Town": case "Foliage":
                        var flags = key == "Foliage" ? Thin : Solid;
                        foreach (var t in cat.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
                        foreach (var lg in cat.GetComponentsInChildren<LODGroup>(true)) { SetHeights(lg, LodHeights[key]); lods++; }
                        renderers += cat.GetComponentsInChildren<Renderer>(true).Length;
                        break;
                    case "Props": case "Interiors": case "Decor":
                        foreach (Transform p in cat) { ApplyProp(p.gameObject, ref colliders, ref lods); props++; }
                        renderers += cat.GetComponentsInChildren<Renderer>(true).Length;
                        break;
                }
            }
            Debug.Log($"[맵 정리] 렌더러 {renderers}개 · LOD 그룹 {lods}개 · 새 충돌체 {colliders}개 · 소품 {props}개");
            return renderers;
        }

        /// <summary>
        /// 소품·인테리어 한 덩어리: 움직이는 물체(Rigidbody)는 정적 제외. 아니면 정적 + 충돌체가 없으면 메시 충돌체(작은 것·NoCol_ 은 제외),
        /// 큰 것은 가리개로, 작은 것은 멀면 안 그림(LOD Group 이 없을 때만 1단계 LOD 추가).
        /// </summary>
        static void ApplyProp(GameObject g, ref int colliders, ref int lods)
        {
            if (g.GetComponentInChildren<Rigidbody>(true) != null) return;
            var rs = g.GetComponentsInChildren<Renderer>(true); if (rs.Length == 0) return;
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); float size = b.size.magnitude;
            var flags = size >= 4f ? Solid : Thin;
            foreach (var t in g.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);
            if (!g.name.StartsWith(NoColliderPrefix) && size >= SmallPropSize && g.GetComponentInChildren<Collider>(true) == null)
                foreach (var mf in g.GetComponentsInChildren<MeshFilter>(true)) { var mc = mf.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; colliders++; }
            if (size < CullPropSize && g.GetComponentInChildren<LODGroup>(true) == null)
            {
                var lg = g.AddComponent<LODGroup>(); lg.SetLODs(new[] { new LOD(0.015f, rs) }); lg.RecalculateBounds(); lods++;
            }
        }

        static void SetHeights(LODGroup lg, float[] h)
        {
            var lods = lg.GetLODs(); if (lods.Length == 0) return;
            for (int i = 0; i < lods.Length; i++) lods[i].screenRelativeTransitionHeight = h[Mathf.Min(i, h.Length - 1)] * (i < h.Length ? 1f : Mathf.Pow(0.5f, i - h.Length + 1));
            for (int i = 1; i < lods.Length; i++) lods[i].screenRelativeTransitionHeight = Mathf.Min(lods[i].screenRelativeTransitionHeight, lods[i - 1].screenRelativeTransitionHeight * 0.95f);
            lods[lods.Length - 1].screenRelativeTransitionHeight = h[h.Length - 1]; lg.SetLODs(lods);
        }

        [MenuItem("Moomin/맵 가림 처리 굽기 (Occlusion Culling)")]
        public static void BakeOcclusion()
        {
            StaticOcclusionCulling.smallestOccluder = 3f; StaticOcclusionCulling.smallestHole = 0.25f; StaticOcclusionCulling.backfaceThreshold = 100f;
            if (StaticOcclusionCulling.Compute()) Debug.Log("[맵] 가림 처리 굽기 완료 — 건물에 가려진 물체는 그리지 않음");
        }
    }
}
#endif
