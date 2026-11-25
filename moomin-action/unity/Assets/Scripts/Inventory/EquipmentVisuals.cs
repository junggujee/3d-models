using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 낀 장비의 3D 모양(ItemDef.model, Resources 경로)을 캐릭터 뼈(ItemDef.attachBone)에 붙임 — 머리띠는 머리 뼈를 따라 움직임.
    /// 장비 모델은 Blender 에서 <b>민미 모델과 같은 공간(기본 자세)</b>에 만들어 내보냄(blender/build_headband.py).
    /// 그래서 붙일 때 스킨 메시의 bindpose(기본 자세에서 '메시 공간 → 뼈 공간' 행렬)를 그대로 쓰면, 지금 어떤 자세든 정확한 자리에 붙음.
    /// </summary>
    public class EquipmentVisuals : MonoBehaviour
    {
        readonly Dictionary<string, List<GameObject>> shown = new();

        public static void Sync(Inventory inv)
        {
            if (inv == null) return;
            if (!inv.TryGetComponent<EquipmentVisuals>(out var v)) v = inv.gameObject.AddComponent<EquipmentVisuals>();
            v.Refresh(inv);
        }

        void Refresh(Inventory inv)
        {
            var want = new HashSet<string>();
            foreach (var id in inv.EquippedIds()) { var d = ItemDatabase.Get(id); if (d != null && !string.IsNullOrEmpty(d.model)) want.Add(id); }
            foreach (var id in new List<string>(shown.Keys)) if (!want.Contains(id)) { foreach (var g in shown[id]) if (g != null) Destroy(g); shown.Remove(id); }
            foreach (var id in want)
            {
                if (shown.ContainsKey(id)) continue; var d = ItemDatabase.Get(id); var list = new List<GameObject>();
                foreach (var part in d.model.Split('|'))   // "경로@뼈" 여러 개(예: 슬리퍼 왼쪽@leg.L|오른쪽@leg.R)
                {
                    var bits = part.Split('@'); var g = Attach(d, bits[0].Trim(), bits.Length > 1 ? bits[1].Trim() : d.attachBone); if (g != null) list.Add(g);
                }
                shown[id] = list;
            }
        }

        /// <summary>이름에 Glow · Core · Edge 가 든 재질은 스스로 빛나게(FBX 발광값이 안 넘어와도) — 광선검 칼날, 쌍검 날끝, LED.</summary>
        static void GlowMaterials(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.materials;
                for (int i = 0; i < ms.Length; i++)
                {
                    var m = ms[i]; if (m == null) continue; string n = m.name;
                    if (!(n.Contains("Glow") || n.Contains("Core") || n.Contains("Edge"))) continue;
                    Color c = m.HasProperty("_Color") ? m.color : Color.white; float k = n.Contains("Core") ? 3.2f : 2.2f;
                    m.EnableKeyword("_EMISSION"); if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * k); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
                r.materials = ms;
            }
        }

        GameObject Attach(ItemDef d, string path, string boneName)
        {
            var prefab = Resources.Load<GameObject>(path); if (prefab == null) { Debug.LogWarning($"[장비] 모델 없음: Resources/{path}"); return null; }
            var go = BoneAttach.Attach(gameObject, prefab, boneName, "Equip_" + d.id + "_" + prefab.name);
            if (go != null) GlowMaterials(go);
            return go;
        }
    }

    /// <summary>
    /// 모델 공간(기본 자세)에서 만든 물건을 뼈에 붙이기(장비 · NPC 모자 공용).
    /// 스킨 메시의 bindpose(기본 자세에서 '메시 공간 → 뼈 공간')를 그대로 쓰면 지금 어떤 자세든 정확한 자리.
    /// </summary>
    public static class BoneAttach
    {
        public static GameObject Attach(GameObject owner, GameObject prefab, string boneName, string name)
        {
            if (owner == null || prefab == null) return null;
            Transform bone = null; Matrix4x4 bind = Matrix4x4.identity;
            foreach (var smr in owner.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var bones = smr.bones; var binds = smr.sharedMesh != null ? smr.sharedMesh.bindposes : null; if (binds == null) continue;
                for (int i = 0; i < bones.Length && i < binds.Length; i++) if (bones[i] != null && bones[i].name == boneName) { bone = bones[i]; bind = binds[i]; break; }
                if (bone != null) break;
            }
            var go = Object.Instantiate(prefab); go.name = name;
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = owner.layer;   // 장비 화면 카메라(플레이어 레이어)에 같이 보이게
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            if (bone == null) { Debug.LogWarning($"[붙이기] 뼈 '{boneName}' 를 찾지 못해 몸에 그대로 붙임"); go.transform.SetParent(owner.transform, false); return go; }
            // 메시(자식)의 '루트 → 메시' 변환 H. 원하는 것: 메시 월드 = 뼈 월드 × bindpose  ⇒  루트(뼈 기준) = bindpose × H⁻¹
            var mf = go.GetComponentInChildren<MeshFilter>(); Matrix4x4 H = mf != null ? go.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix : Matrix4x4.identity;
            Matrix4x4 local = bind * H.inverse;
            go.transform.SetParent(bone, false);
            go.transform.localPosition = local.GetColumn(3); go.transform.localRotation = local.rotation; go.transform.localScale = local.lossyScale;
            return go;
        }
    }

    /// <summary>NPC 소품(어부 모자 등)을 뼈에 붙임.</summary>
    public static class NpcAccessory
    {
        public static GameObject Attach(GameObject npc, string resourcePath, string boneName)
        {
            var prefab = Resources.Load<GameObject>(resourcePath); if (prefab == null) { Debug.LogWarning($"[NPC] 소품 없음: Resources/{resourcePath}"); return null; }
            return BoneAttach.Attach(npc, prefab, boneName, "Accessory_" + prefab.name);
        }
    }
}
