#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoominAction.EditorTools
{
    /// <summary>
    /// 메뉴 Moomin ▸ "한 번에 설정": FBX 애니메이션 루프 지정 → Animator Controller 생성 → 플레이어/적 프리팹 → 메인 씬(바닥·조명·카메라·GameManager).
    /// 도시 모델은 Assets/City/ 아래 FBX/프리팹을 넣어 두면 씬 중앙에 자동 배치됩니다.
    /// </summary>
    public static class MoominSetup
    {
        const string Models = "Assets/Models/", Prefabs = "Assets/Prefabs/", Anims = "Assets/Animations/", Scenes = "Assets/Scenes/";

        [MenuItem("Moomin/한 번에 설정 (전부 실행)")]
        public static void All() { ConfigureImports(); BuildController(); BuildPrefabs(); BuildScene(); Debug.Log("[Moomin] 설정 완료 → Assets/Scenes/Main.unity 를 열고 Play!"); }

        [MenuItem("Moomin/1. FBX 임포트 설정")]
        public static void ConfigureImports()
        {
            var imp = AssetImporter.GetAtPath(Models + "Moomin.fbx") as ModelImporter; if (imp == null) { Debug.LogError("Assets/Models/Moomin.fbx 가 없습니다."); return; }
            imp.animationType = ModelImporterAnimationType.Generic; imp.importAnimation = true; imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            var clips = imp.defaultClipAnimations;
            foreach (var c in clips) { c.name = c.name.Contains("|") ? c.name.Split('|').Last() : c.name; c.loopTime = c.name is "Idle" or "Walk" or "Run"; c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true; }
            imp.clipAnimations = clips; imp.SaveAndReimport();
            foreach (var n in new[] { "StarSparkSword.fbx", "ShadowBlob.fbx" }) { var m = AssetImporter.GetAtPath(Models + n) as ModelImporter; if (m != null) { m.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; m.SaveAndReimport(); } }
        }

        static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath(Models + "Moomin.fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview") && c.name.EndsWith(name));

        [MenuItem("Moomin/2. Animator Controller 만들기")]
        public static void BuildController()
        {
            System.IO.Directory.CreateDirectory(Anims);
            var path = Anims + "Moomin.controller"; AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path); ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.CreateBlendTreeInController("Locomotion", out BlendTree tree); tree.blendParameter = "Speed";
            tree.AddChild(Clip("Idle"), 0f); tree.AddChild(Clip("Walk"), 0.5f); tree.AddChild(Clip("Run"), 1f);
            var sm = ac.layers[0].stateMachine;
            foreach (var n in new[] { "Attack1", "Attack2", "Attack3", "Hit", "Death" }) { var s = sm.AddState(n); s.motion = Clip(n); if (n.StartsWith("Attack") || n == "Hit") { var t = s.AddExitTransition(); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.1f; } }
            AssetDatabase.SaveAssets();
        }

        static Transform Find(Transform t, string n) { if (t.name == n) return t; foreach (Transform c in t) { var r = Find(c, n); if (r) return r; } return null; }

        [MenuItem("Moomin/3. 프리팹 만들기")]
        public static void BuildPrefabs()
        {
            System.IO.Directory.CreateDirectory(Prefabs);
            // ── 플레이어
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Moomin.fbx")); player.name = "Player"; player.tag = "Player";
            var cc = player.AddComponent<CharacterController>(); cc.height = 1.3f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.65f, 0); cc.stepOffset = 0.3f;
            var anim = player.GetComponentInChildren<Animator>(); anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Anims + "Moomin.controller"); anim.applyRootMotion = false;
            player.AddComponent<Health>().maxHealth = 100f; player.AddComponent<PlayerController>(); player.AddComponent<PlayerHealth>();
            var combo = player.AddComponent<SwordCombat>();
            var hand = Find(player.transform, "hand.R");
            if (hand != null)
            {
                var sword = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "StarSparkSword.fbx"), hand);
                sword.name = "StarSparkSword"; sword.transform.localPosition = Vector3.zero; sword.transform.localRotation = Quaternion.Euler(-120f, 0, 0);   // 칼끝이 앞·위를 보도록(필요시 인스펙터에서 조정)
                combo.glow = sword.AddComponent<SwordGlow>(); var tip = Find(sword.transform, "SparkPoint_Tip"); combo.sparkOrigin = tip;
            }
            else Debug.LogWarning("hand.R 뼈를 찾지 못했습니다. 검을 손 뼈에 수동으로 붙이세요.");
            PrefabUtility.SaveAsPrefabAsset(player, Prefabs + "Player.prefab"); Object.DestroyImmediate(player);
            // ── 적
            var enemy = new GameObject("ShadowBlob"); var vis = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "ShadowBlob.fbx"), enemy.transform); vis.name = "Visual";
            var ecc = enemy.AddComponent<CharacterController>(); ecc.height = 0.9f; ecc.radius = 0.4f; ecc.center = new Vector3(0, 0.45f, 0);
            enemy.AddComponent<Health>().maxHealth = 30f; var ai = enemy.AddComponent<EnemyAI>(); ai.visual = vis.transform;
            PrefabUtility.SaveAsPrefabAsset(enemy, Prefabs + "ShadowBlob.prefab"); Object.DestroyImmediate(enemy);
        }

        [MenuItem("Moomin/4. 메인 씬 만들기")]
        public static void BuildScene()
        {
            System.IO.Directory.CreateDirectory(Scenes);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "Ground"; ground.transform.localScale = new Vector3(40, 1, 40);
            // 도시 배경(Assets/City 아래 첫 프리팹/FBX)
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/City" });
            if (guids.Length > 0) { var city = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]))); city.name = "City"; ground.GetComponent<MeshRenderer>().enabled = false; foreach (var m in city.GetComponentsInChildren<MeshRenderer>()) if (m.GetComponent<Collider>() == null) m.gameObject.AddComponent<MeshCollider>(); }
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Player.prefab")); player.transform.position = new Vector3(0, 0.1f, 0);
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<ThirdPersonCamera>(); cam.target = player.transform;
            player.GetComponent<PlayerController>().cam = camGo.transform;
            var gm = new GameObject("GameManager").AddComponent<GameManager>(); gm.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ShadowBlob.prefab"); gm.player = player.GetComponent<Health>();
            EditorSceneManager.SaveScene(scene, Scenes + "Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scenes + "Main.unity", true) };
        }
    }
}
#endif
