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
    /// 메뉴 Moomin ▸ "한 번에 설정": FBX 임포트(루프·애니메이션 이벤트) → Animator Controller → 스킬 에셋(Assets/Data/Skills) → 레이어(Player·Enemy) → 플레이어/적 프리팹 → 메인 씬(바닥·조명·카메라·GameManager·NavMesh).
    /// 도시 모델은 Assets/City/ 아래 FBX/프리팹을 넣어 두면 씬 중앙에 자동 배치됩니다.
    /// </summary>
    public static class MoominSetup
    {
        const string Models = "Assets/Models/", Prefabs = "Assets/Prefabs/", Anims = "Assets/Animations/", Scenes = "Assets/Scenes/";

        [MenuItem("Moomin/한 번에 설정 (전부 실행)")]
        public static void All() { ConfigureImports(); BuildController(); BuildBearController(); BuildWhaleController(); BuildMonsterControllers(); BuildNpcController(); BuildPrefabs(); BuildScene(); BuildTitleScene(); Debug.Log("[Moomin] 설정 완료 → Assets/Scenes/Title.unity 를 열고 Play! (Main.unity 를 바로 열면 새 게임으로 시작)"); }

        [MenuItem("Moomin/1. FBX 임포트 설정")]
        public static void ConfigureImports()
        {
            var imp = AssetImporter.GetAtPath(Models + "Moomin.fbx") as ModelImporter; if (imp == null) { Debug.LogError("Assets/Models/Moomin.fbx 가 없습니다."); return; }
            imp.animationType = ModelImporterAnimationType.Generic; imp.importAnimation = true; imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            var clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.name = c.name.Contains("|") ? c.name.Split('|').Last() : c.name; c.loopTime = c.name is "Idle" or "Walk" or "Run" or "Guard" or "ShieldGuard" or "DualGuard" or "LaserGuard" or "SwimIdle" or "Swim" or "DiveIdle" or "Dive" or "BoatSit"; c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                // 공격 클립의 타격 프레임(Blender: Attack1/2 = 24프레임 중 11, Attack3 = 30프레임 중 14)에 명세 v2 6-2 의 애니메이션 이벤트 배치
                float hit = c.name switch { "Attack1" => 10f / 23f, "Attack2" => 10f / 23f, "Attack3" => 13f / 29f, _ => -1f };
                if (WeaponHitFrames.TryGetValue(c.name, out var wf)) hit = (wf.hit - 1f) / (wf.len - 1f);   // 베개 방패·회전 쌍검 클립(blender/moomin_weapon_anims.py 와 같은 프레임)
                if (hit > 0f) c.events = new[] {
                    new AnimationEvent { functionName = "OnCoinAttack", time = hit }, new AnimationEvent { functionName = "OpenHitbox", time = hit },
                    new AnimationEvent { functionName = "CloseHitbox", time = Mathf.Min(0.95f, hit + 0.15f) }, new AnimationEvent { functionName = "OnSkillFinished", time = 0.98f } };
                else c.events = new AnimationEvent[0];
            }
            imp.clipAnimations = clips; imp.SaveAndReimport();
            // 인간형 적: 잠꾸러기 곰(애니메이션 이벤트 없음 — 적은 스킬 데이터의 hitTime 으로 진행)
            var bear = AssetImporter.GetAtPath(Models + "SleepyBear.fbx") as ModelImporter;
            if (bear != null)
            {
                bear.animationType = ModelImporterAnimationType.Generic; bear.importAnimation = true; bear.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                var bc = bear.defaultClipAnimations;
                foreach (var c in bc) { c.name = c.name.Contains("|") ? c.name.Split('|').Last() : c.name; c.loopTime = c.name is "Idle" or "Walk" or "Run" or "Guard"; c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true; c.events = new AnimationEvent[0]; }
                bear.clipAnimations = bc; bear.SaveAndReimport();
            }
            // 지상괴물형 적: 고래고래(루프 = Idle·Walk·Run·Guard·BeamHold)
            var whale = AssetImporter.GetAtPath(Models + "Goraegorae.fbx") as ModelImporter;
            if (whale != null)
            {
                whale.animationType = ModelImporterAnimationType.Generic; whale.importAnimation = true; whale.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                var wc = whale.defaultClipAnimations;
                foreach (var c in wc) { c.name = c.name.Contains("|") ? c.name.Split('|').Last() : c.name; c.loopTime = c.name is "Idle" or "Walk" or "Run" or "Guard" or "BeamHold"; c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true; c.events = new AnimationEvent[0]; }
                whale.clipAnimations = wc; whale.SaveAndReimport();
            }
            // 일반 몬스터: 핑크큐브말랑이 · 그린볼런(루프 = Idle·Walk·Run·Guard)
            foreach (var n in new[] { "PinkCube", "GreenBlock" })
            {
                if (!(AssetImporter.GetAtPath(Models + n + ".fbx") is ModelImporter mi)) continue;
                mi.animationType = ModelImporterAnimationType.Generic; mi.importAnimation = true; mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                var mc = mi.defaultClipAnimations;
                foreach (var c in mc) { c.name = c.name.Contains("|") ? c.name.Split('|').Last() : c.name; c.loopTime = c.name is "Idle" or "Walk" or "Run" or "Guard"; c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true; c.events = new AnimationEvent[0]; }
                mi.clipAnimations = mc; mi.SaveAndReimport();
            }
            foreach (var t in new[] { "Pebble_Normal", "Weave_Normal", "Fur_Normal" })   // 핑크큐브 오돌도돌 · 땅콩빵 짜임 · 물범 털결
                if (AssetImporter.GetAtPath(Models + "Textures/" + t + ".png") is TextureImporter pn) { pn.textureType = TextureImporterType.NormalMap; pn.wrapMode = TextureWrapMode.Repeat; pn.SaveAndReimport(); }
            // 펫 하프물범(루프 = Idle·Walk·Run·Guard·Sleep)
            if (AssetImporter.GetAtPath(Models + "Seal.fbx") is ModelImporter si)
            {
                si.animationType = ModelImporterAnimationType.Generic; si.importAnimation = true; si.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                var sclips = si.defaultClipAnimations;
                foreach (var c in sclips) { c.name = c.name.Contains("|") ? c.name.Split('|').Last() : c.name; c.loopTime = c.name is "Idle" or "Walk" or "Run" or "Guard" or "Sleep"; c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true; c.events = new AnimationEvent[0]; }
                si.clipAnimations = sclips; si.SaveAndReimport();
            }
            // NPC 조랭하프족(루프 = Idle·Walk·Talk)
            if (AssetImporter.GetAtPath(Models + "Joraeng.fbx") is ModelImporter ji)
            {
                ji.animationType = ModelImporterAnimationType.Generic; ji.importAnimation = true; ji.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                var jclips = ji.defaultClipAnimations;
                foreach (var c in jclips) { c.name = c.name.Contains("|") ? c.name.Split('|').Last() : c.name; c.loopTime = c.name is "Idle" or "Walk" or "Talk"; c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true; c.events = new AnimationEvent[0]; }
                ji.clipAnimations = jclips; ji.SaveAndReimport();
            }
            // 대화창·장식·초상화 그림: 밉맵 없이 선명하게(판 그림은 3200px 이라 최대 크기 4096)
            foreach (var folder in new[] { "Assets/Resources/UI/Dialogue", "Assets/Resources/UI/Ornament", "Assets/Resources/UI/Portraits", "Assets/Resources/UI/Sheet", "Assets/Resources/UI/Codex", "Assets/Resources/UI/Fx" })
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
                    if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) is TextureImporter ui)
                    { ui.mipmapEnabled = false; ui.alphaIsTransparency = true; ui.npotScale = TextureImporterNPOTScale.None; ui.maxTextureSize = 4096; ui.textureCompression = TextureImporterCompression.CompressedHQ; ui.wrapMode = TextureWrapMode.Clamp; ui.SaveAndReimport(); }
            }
            // 글꼴: 빛의 계승자(완성형 2,350자)에 없는 글자는 나눔명조에서 빌려 옴
            var nanumFallback = AssetDatabase.LoadAssetAtPath<Font>("Assets/Resources/Fonts/NanumMyeongjoExtraBold.ttf");
            foreach (var fn in new[] { "HeirOfLight-Bold", "HeirOfLight-Regular", "SandollGawain" })
                if (AssetImporter.GetAtPath("Assets/Resources/Fonts/" + fn + ".ttf") is TrueTypeFontImporter fi && nanumFallback != null)
                { fi.includeFontData = true; fi.fontReferences = new[] { nanumFallback }; fi.SaveAndReimport(); }
            // 맵(City.fbx): 애니메이션·카메라·조명 없음, 충돌용으로 읽기 가능, 메시 최적화 · 텍스처: 밉맵 + 2048 + 고품질 압축 + 비스듬히 봐도 선명하게
            foreach (var cg in AssetDatabase.FindAssets("City", new[] { "Assets/City" }))
            if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(cg)) is ModelImporter cityImp)
            {
                cityImp.preserveHierarchy = true;   // 분류(Terrain·Town…) 빈 물체를 그대로 — MapTools 가 이름으로 찾음
                cityImp.importAnimation = false; cityImp.importCameras = false; cityImp.importLights = false; cityImp.isReadable = true;
                cityImp.meshOptimizationFlags = MeshOptimizationFlags.Everything; cityImp.indexFormat = ModelImporterIndexFormat.Auto; cityImp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; cityImp.SaveAndReimport();
            }
            foreach (var g in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/City" }))
                if (AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) is TextureImporter ti)
                { ti.mipmapEnabled = true; ti.maxTextureSize = 2048; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.anisoLevel = 4; ti.wrapMode = TextureWrapMode.Repeat; ti.SaveAndReimport(); }
            // 바다·선착장·통통배·어부 모자(Resources): 바닥·충돌 메시는 런타임 MeshCollider 용으로 읽기 가능
            foreach (var path in new[] { "Assets/Resources/World/Seaside.fbx", "Assets/Resources/World/Boat.fbx", "Assets/Resources/Npc/FisherHat.fbx", "Assets/Resources/World/Palace.fbx", "Assets/Resources/Boss/KingCrown.fbx", "Assets/Resources/Boss/KingCape.fbx", "Assets/Resources/World/Party.fbx" })
                if (AssetImporter.GetAtPath(path) is ModelImporter seaImp)
                {
                    seaImp.preserveHierarchy = true; seaImp.importAnimation = false; seaImp.importCameras = false; seaImp.importLights = false; seaImp.isReadable = true;
                    seaImp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; seaImp.meshOptimizationFlags = MeshOptimizationFlags.Everything; seaImp.SaveAndReimport();
                }
            foreach (var n in new[] { "StarSparkSword.fbx", "ShadowBlob.fbx" }) { var m = AssetImporter.GetAtPath(Models + n) as ModelImporter; if (m != null) { m.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; m.SaveAndReimport(); } }
        }

        static AnimationClip ClipOf(string fbx, string name)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(Models + fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            return all.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name)) ?? all.FirstOrDefault(c => c.name.EndsWith(name));
        }

        /// <summary>Idle/Walk/Run 블렌드(Speed) + 추가 상태. oneShots 는 끝나면 Locomotion 으로 돌아감.</summary>
        static RuntimeAnimatorController MakeController(string fbx, string path, string[] states, string[] oneShots)
        {
            System.IO.Directory.CreateDirectory(Anims); AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path); ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.CreateBlendTreeInController("Locomotion", out BlendTree tree); tree.blendParameter = "Speed";
            tree.AddChild(ClipOf(fbx, "Idle"), 0f); tree.AddChild(ClipOf(fbx, "Walk"), 0.5f); tree.AddChild(ClipOf(fbx, "Run"), 1f);
            var sm = ac.layers[0].stateMachine;
            foreach (var n in states) { var st = sm.AddState(n); st.motion = ClipOf(fbx, n); if (System.Array.IndexOf(oneShots, n) >= 0) { var t = st.AddExitTransition(); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.1f; } }
            AssetDatabase.SaveAssets(); return ac;
        }

        [MenuItem("Moomin/2-4. NPC(조랭하프족) Animator 만들기")]
        public static void BuildNpcController()
        {
            const string fbx = "Joraeng.fbx"; if (AssetImporter.GetAtPath(Models + fbx) == null) { Debug.LogWarning("Assets/Models/Joraeng.fbx 가 없습니다."); return; }
            var path = Anims + "Joraeng.controller"; System.IO.Directory.CreateDirectory(Anims); AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path); ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.CreateBlendTreeInController("Locomotion", out BlendTree tree); tree.blendParameter = "Speed";
            tree.AddChild(ClipOf(fbx, "Idle"), 0f); tree.AddChild(ClipOf(fbx, "Walk"), 1f);
            var sm = ac.layers[0].stateMachine;
            var talk = sm.AddState("Talk"); talk.motion = ClipOf(fbx, "Talk");   // 대화 중 반복(Npc 가 끝낼 때 Locomotion 으로)
            foreach (var n in new[] { "Wave", "Yes", "No", "Laugh", "Sad", "Surprised", "QuestAccept", "QuestComplete" })   // 표정 동작: 한 번 → 기본(Locomotion)
            { var st = sm.AddState(n); st.motion = ClipOf(fbx, n); var t = st.AddExitTransition(); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.12f; }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Moomin/2-1. 곰(인간형 적) Animator 만들기")]
        public static void BuildBearController()
        {
            if (AssetImporter.GetAtPath(Models + "SleepyBear.fbx") == null) { Debug.LogWarning("Assets/Models/SleepyBear.fbx 가 없습니다."); return; }
            MakeController("SleepyBear.fbx", Anims + "SleepyBear.controller", new[] { "Windup", "Attack1", "Attack2", "Attack3", "Guard", "Dodge", "Hit", "Death" }, new[] { "Attack1", "Attack2", "Attack3", "Dodge", "Hit" });
        }

        /// <summary>무기 클립의 (타격 프레임, 길이) — blender/moomin_weapon_anims.py 의 HIT 표와 같게.</summary>
        static readonly System.Collections.Generic.Dictionary<string, (float hit, float len)> WeaponHitFrames = new()
        {
            ["ShieldBash"] = (10, 24), ["ShieldSwing"] = (10, 24), ["ShieldSlam"] = (15, 30), ["ShieldLeap"] = (22, 36),
            ["DualSlash1"] = (10, 24), ["DualSlash2"] = (10, 24), ["DualCross"] = (10, 30), ["DualDash"] = (10, 24), ["DualWhirl"] = (10, 24), ["DualGrind"] = (10, 20), ["DualFinale"] = (26, 40),
            ["LaserSlash1"] = (10, 24), ["LaserSlash2"] = (10, 24), ["LaserCross"] = (10, 30), ["LaserDash"] = (10, 24), ["LaserWhirl"] = (10, 24), ["LaserGrind"] = (10, 20), ["LaserFinale"] = (26, 40),
        };
        /// <summary>민미 클립(이름이 정확히 같은 것 우선 — "Guard" 가 "ShieldGuard" 에 걸리지 않게).</summary>
        static AnimationClip Clip(string name)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(Models + "Moomin.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            return all.FirstOrDefault(c => c.name == name || c.name.EndsWith("|" + name)) ?? all.FirstOrDefault(c => c.name.EndsWith(name));
        }

        [MenuItem("Moomin/2-2. 고래고래(지상괴물형 적) Animator 만들기")]
        public static void BuildWhaleController()
        {
            if (AssetImporter.GetAtPath(Models + "Goraegorae.fbx") == null) { Debug.LogWarning("Assets/Models/Goraegorae.fbx 가 없습니다."); return; }
            MakeController("Goraegorae.fbx", Anims + "Goraegorae.controller", new[] { "Windup", "Attack1", "Attack2", "Attack3", "Jump", "BeamCharge", "Beam", "BeamHold", "BeamEnd", "Spout", "Guard", "Dodge", "Hit", "Death" },
                           new[] { "Attack1", "Attack2", "Attack3", "Jump", "BeamEnd", "Spout", "Dodge", "Hit" });
        }

        [MenuItem("Moomin/2-3. 일반 몬스터(핑크큐브말랑이·그린볼런) Animator 만들기")]
        public static void BuildMonsterControllers()
        {
            foreach (var n in new[] { "PinkCube", "GreenBlock" })
            {
                if (AssetImporter.GetAtPath(Models + n + ".fbx") == null) { Debug.LogWarning($"Assets/Models/{n}.fbx 가 없습니다."); continue; }
                MakeController(n + ".fbx", Anims + n + ".controller", new[] { "Windup", "Attack1", "Attack2", "Guard", "Dodge", "Hit", "Death" }, new[] { "Attack1", "Attack2", "Dodge", "Hit" });
            }
            if (AssetImporter.GetAtPath(Models + "Seal.fbx") != null)   // 펫: 전투 상태 + 먹기·기뻐하기·자기(Death = 기절)
                MakeController("Seal.fbx", Anims + "Seal.controller", new[] { "Windup", "Attack1", "Attack2", "Guard", "Dodge", "Hit", "Death", "Happy", "Eat", "Sleep" }, new[] { "Attack1", "Attack2", "Dodge", "Hit", "Happy", "Eat" });
        }

        [MenuItem("Moomin/2. Animator Controller 만들기")]
        public static void BuildController()
        {
            System.IO.Directory.CreateDirectory(Anims);
            var path = Anims + "Moomin.controller"; AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path); ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.CreateBlendTreeInController("Locomotion", out BlendTree tree); tree.blendParameter = "Speed";
            tree.AddChild(Clip("Idle"), 0f); tree.AddChild(Clip("Walk"), 0.5f); tree.AddChild(Clip("Run"), 1f);
            var sm = ac.layers[0].stateMachine;
            foreach (var n in new[] { "Roll", "Guard", "Attack1", "Attack2", "Attack3", "Hit", "Death", "ShieldGuard", "ShieldBash", "ShieldSwing", "ShieldSlam", "ShieldLeap", "DualGuard", "DualSlash1", "DualSlash2", "DualCross", "DualDash", "DualWhirl", "DualGrind", "DualFinale", "LaserGuard", "LaserSlash1", "LaserSlash2", "LaserCross", "LaserDash", "LaserWhirl", "LaserGrind", "LaserFinale", "SwimIdle", "Swim", "DiveIdle", "Dive", "BoatSit" }) { var s = sm.AddState(n); s.motion = Clip(n); if (n.StartsWith("Attack") || n == "Hit" || n == "Roll" || (WeaponHitFrames.ContainsKey(n))) { var t = s.AddExitTransition(); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.1f; } }
            AssetDatabase.SaveAssets();
        }

        static Transform Find(Transform t, string n) { if (t.name == n) return t; foreach (Transform c in t) { var r = Find(c, n); if (r) return r; } return null; }
        static void SetLayer(GameObject g, int layer) { if (layer < 0) return; foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }

        /// <summary>ProjectSettings/TagManager 에 레이어가 없으면 빈 칸(8~31)에 추가.</summary>
        static int EnsureLayer(string name)
        {
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]); var layers = tm.FindProperty("layers");
            for (int i = 8; i < 32; i++) if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            for (int i = 8; i < 32; i++) { var p = layers.GetArrayElementAtIndex(i); if (string.IsNullOrEmpty(p.stringValue)) { p.stringValue = name; tm.ApplyModifiedProperties(); return i; } }
            Debug.LogWarning("레이어 칸이 가득 차서 " + name + " 레이어를 만들지 못했습니다(팀 구분은 CharacterStats.team 으로 계속 동작)."); return -1;
        }

        const string SkillDir = "Assets/Data/Skills/";
        /// <summary>스킬 에셋(없으면 기본값으로 생성).</summary>
        static SkillDefinition SkillAsset(string id)
        {
            var path = SkillDir + id + ".asset"; var a = AssetDatabase.LoadAssetAtPath<SkillDefinition>(path); if (a != null) return a;
            System.IO.Directory.CreateDirectory(SkillDir); var src = SkillLibrary.Get(id); if (src == null) return null;
            a = Object.Instantiate(src); a.name = id; AssetDatabase.CreateAsset(a, path); return a;
        }

        [MenuItem("Moomin/스킬 데이터 에셋 만들기")]
        public static void ExportSkills() { foreach (var s in SkillLibrary.All) SkillAsset(s.skillId); AssetDatabase.SaveAssets(); Debug.Log("[Moomin] 스킬 에셋: " + SkillDir + " (Inspector 에서 수치를 바꾸면 바로 반영)"); }

        [MenuItem("Moomin/3. 프리팹 만들기")]
        public static void BuildPrefabs()
        {
            System.IO.Directory.CreateDirectory(Prefabs); ExportSkills();
            int playerLayer = EnsureLayer("Player"), enemyLayer = EnsureLayer("Enemy");
            int enemyMask = enemyLayer >= 0 ? 1 << enemyLayer : ~0, playerMask = playerLayer >= 0 ? 1 << playerLayer : ~0;
            int petLayer = EnsureLayer("Pet"); if (petLayer >= 0 && playerLayer >= 0) playerMask |= 1 << petLayer;   // 적 공격이 펫(막아서기)에게도 맞게 — 장비 화면 카메라는 Player 레이어만 그림

            // ── 플레이어: CharacterController · PlayerController · CharacterStats · CoinSystem · SkillExecutor · CombatResolver · CombatManager ...
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Moomin.fbx")); player.name = "Player"; player.tag = "Player";
            var cc = player.AddComponent<CharacterController>(); cc.height = 1.3f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.65f, 0); cc.stepOffset = 0.3f;
            var anim = player.GetComponentInChildren<Animator>(); anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Anims + "Moomin.controller"); anim.applyRootMotion = false;
            var stats = player.AddComponent<CharacterStats>(); stats.displayName = "민미"; stats.team = Team.Player; stats.attribute = SkillAttribute.Black; stats.maxHealth = stats.currentHealth = 100f;
            player.AddComponent<Stamina>(); player.AddComponent<CoinSystem>(); player.AddComponent<StatusEffectManager>(); player.AddComponent<Experience>(); player.AddComponent<DefenseController>(); player.AddComponent<EnergyManager>(); var res = player.AddComponent<CombatResolver>(); res.targetMask = enemyMask;
            var ex = player.AddComponent<SkillExecutor>(); ex.useAnimationEvents = true; ex.animator = anim;
            var cm = player.AddComponent<CombatManager>();
            cm.basic1 = SkillAsset("BASIC_1"); cm.basic2 = SkillAsset("BASIC_2"); cm.basic3 = SkillAsset("BASIC_3"); cm.skill1 = SkillAsset("SKILL_001"); cm.skill2 = SkillAsset("SKILL_002"); cm.ultimate = SkillAsset("SKILL_003"); SkillAsset("SKILL_004");   // 충격파(광역 테스트) 에셋도 생성
            player.AddComponent<Inventory>(); player.AddComponent<LockOnSystem>(); player.AddComponent<PlayerController>(); player.AddComponent<PlayerSwim>();   // 헤엄·잠수
            var hand = Find(player.transform, "hand.R");
            if (hand != null)
            {
                var sword = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "StarSparkSword.fbx"), hand);
                sword.name = "StarSparkSword"; sword.transform.localPosition = Vector3.zero; sword.transform.localRotation = Quaternion.Euler(-120f, 0, 0);   // 칼끝이 앞·위를 보도록(필요시 Inspector 에서 조정)
                sword.AddComponent<SwordGlow>(); res.sparkOrigin = Find(sword.transform, "SparkPoint_Tip");
            }
            else Debug.LogWarning("hand.R 뼈를 찾지 못했습니다. 검을 손 뼈에 수동으로 붙이세요.");
            SetLayer(player, playerLayer);
            PrefabUtility.SaveAsPrefabAsset(player, Prefabs + "Player.prefab"); Object.DestroyImmediate(player);

            // ── 적: NavMeshAgent · CharacterStats · CoinSystem · SkillExecutor · CombatResolver · EnemyBrain · EnemyCombatController
            var enemy = new GameObject("ShadowBlob"); var vis = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "ShadowBlob.fbx"), enemy.transform); vis.name = "Visual";
            var col = enemy.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 0.45f, 0); col.radius = 0.4f; col.height = 0.9f;
            var agent = enemy.AddComponent<UnityEngine.AI.NavMeshAgent>(); agent.radius = 0.4f; agent.height = 0.9f; agent.speed = 2.4f; agent.angularSpeed = 0f; agent.stoppingDistance = 1f;
            var es = enemy.AddComponent<CharacterStats>(); es.displayName = "그림자"; es.team = Team.Enemy; es.attribute = SkillAttribute.Black; es.maxHealth = es.currentHealth = 30f; es.invulnerableTime = 0.1f;
            enemy.AddComponent<CoinSystem>(); enemy.AddComponent<StatusEffectManager>(); enemy.AddComponent<DefenseController>(); enemy.AddComponent<EnergyManager>(); var eres = enemy.AddComponent<CombatResolver>(); eres.targetMask = playerMask;
            var eex = enemy.AddComponent<SkillExecutor>(); eex.useAnimationEvents = false;
            var ecc = enemy.AddComponent<EnemyCombatController>(); ecc.archetype = EnemyArchetype.GroundMonster; ecc.visual = vis.transform; ecc.agent = agent; ecc.skills = new System.Collections.Generic.List<SkillDefinition> { SkillAsset("ENEMY_BUMP"), SkillAsset("ENEMY_SLAM") }; ecc.basicSkill = ecc.skills[0];
            enemy.AddComponent<EnemyBrain>();
            foreach (var id in new[] { "BOSS_LUNGE", "BOSS_FLURRY", "BOSS_BREAK" }) SkillAsset(id);
            SetLayer(enemy, enemyLayer);
            PrefabUtility.SaveAsPrefabAsset(enemy, Prefabs + "ShadowBlob.prefab"); Object.DestroyImmediate(enemy);

            // ── 인간형 적: 잠꾸러기 곰(애니메이션·신중한 성격·주먹 기술)
            var bearModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "SleepyBear.fbx");
            if (bearModel != null)
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(bearModel); b.name = "SleepyBear";
                var bAnim = b.GetComponentInChildren<Animator>(); if (bAnim == null) bAnim = b.AddComponent<Animator>();
                bAnim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Anims + "SleepyBear.controller"); bAnim.applyRootMotion = false;
                var bc = b.AddComponent<CapsuleCollider>(); bc.center = new Vector3(0, 0.63f, 0); bc.radius = 0.35f; bc.height = 1.26f;
                var ba = b.AddComponent<UnityEngine.AI.NavMeshAgent>(); ba.radius = 0.35f; ba.height = 1.26f; ba.speed = 2.2f; ba.angularSpeed = 0f; ba.stoppingDistance = 1f;
                var bs = b.AddComponent<CharacterStats>(); bs.displayName = "잠꾸러기 곰"; bs.team = Team.Enemy; bs.attribute = SkillAttribute.Yellow; bs.maxHealth = bs.currentHealth = 45f; bs.invulnerableTime = 0.1f;
                b.AddComponent<CoinSystem>(); b.AddComponent<StatusEffectManager>(); b.AddComponent<DefenseController>(); b.AddComponent<EnergyManager>(); var br = b.AddComponent<CombatResolver>(); br.targetMask = playerMask;
                var bex = b.AddComponent<SkillExecutor>(); bex.useAnimationEvents = false; bex.animator = bAnim;
                var bcc = b.AddComponent<EnemyCombatController>(); bcc.archetype = EnemyArchetype.Humanoid; bcc.animator = bAnim; bcc.agent = ba; bcc.visual = b.transform; bcc.moveSpeed = 2.2f; bcc.score = 15;
                bcc.skills = new System.Collections.Generic.List<SkillDefinition> { SkillAsset("HUMAN_JAB"), SkillAsset("HUMAN_SLAM"), SkillAsset("HUMAN_RUSH") }; bcc.basicSkill = bcc.skills[0];
                b.AddComponent<EnemyBrain>().personality = EnemyPersonality.Humanoid;
                var becc = b.GetComponent<EnemyCombatController>(); if (becc != null) { becc.expReward = 70; becc.levelOffset = 1; }   // 인간형은 경험치 더 많이 · 레벨 +1
                SetLayer(b, enemyLayer);
                PrefabUtility.SaveAsPrefabAsset(b, Prefabs + "SleepyBear.prefab"); Object.DestroyImmediate(b);
            }

            // ── 지상괴물형 적: 고래고래(지상 최대의 끼룩이). 플레이어보다 훨씬 큼(×2), 느리지만 체력·경직 게이지가 큼. 2웨이브부터 한 웨이브에 1마리
            var whaleModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Goraegorae.fbx");
            if (whaleModel != null)
            {
                var w = (GameObject)PrefabUtility.InstantiatePrefab(whaleModel); w.name = "Goraegorae"; w.transform.localScale = Vector3.one * 2f;
                var wAnim = w.GetComponentInChildren<Animator>(); if (wAnim == null) wAnim = w.AddComponent<Animator>();
                wAnim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Anims + "Goraegorae.controller"); wAnim.applyRootMotion = false;
                var wcol = w.AddComponent<CapsuleCollider>(); wcol.direction = 2; wcol.center = new Vector3(0, 0.5f, -0.08f); wcol.radius = 0.45f; wcol.height = 3.1f;   // 주둥이 끝 ~ 꼬리 3.1m, Z 방향 캡슐(로컬, ×2 배율)
                var wa = w.AddComponent<UnityEngine.AI.NavMeshAgent>(); wa.radius = 0.6f; wa.height = 1.1f; wa.speed = 1.8f; wa.angularSpeed = 0f; wa.stoppingDistance = 2.5f;
                var ws = w.AddComponent<CharacterStats>(); ws.displayName = "고래고래"; ws.epithet = "지상 최대의 끼룩이"; ws.team = Team.Enemy; ws.attribute = SkillAttribute.Blue;
                ws.maxHealth = ws.currentHealth = 220f; ws.invulnerableTime = 0.1f; ws.maxStagger = 180f; ws.staggerDuration = 2.5f;
                w.AddComponent<CoinSystem>(); w.AddComponent<StatusEffectManager>(); w.AddComponent<DefenseController>(); w.AddComponent<EnergyManager>(); var wr = w.AddComponent<CombatResolver>(); wr.targetMask = playerMask;
                var wex = w.AddComponent<SkillExecutor>(); wex.useAnimationEvents = false; wex.animator = wAnim;
                var wcc = w.AddComponent<EnemyCombatController>(); wcc.archetype = EnemyArchetype.GroundMonster; wcc.animator = wAnim; wcc.agent = wa; wcc.visual = w.transform; wcc.moveSpeed = 1.8f; wcc.score = 40;
                wcc.expReward = 150; wcc.levelOffset = 2; wcc.minWave = 2; wcc.maxPerWave = 1; wcc.showBossBar = true; wcc.bossEmblem = "Whale";   // 전용 보스바
                wcc.skills = new System.Collections.Generic.List<SkillDefinition> { SkillAsset("WHALE_FLIPPER"), SkillAsset("WHALE_RAM"), SkillAsset("WHALE_BEAM"), SkillAsset("WHALE_JUMP"), SkillAsset("WHALE_TAIL"), SkillAsset("WHALE_SPOUT") };
                wcc.basicSkill = wcc.skills[0];
                w.AddComponent<EnemyBrain>().personality = EnemyPersonality.Grunt;
                SetLayer(w, enemyLayer);
                PrefabUtility.SaveAsPrefabAsset(w, Prefabs + "Goraegorae.prefab"); Object.DestroyImmediate(w);
            }
            // ── 일반 몬스터 ①: 핑크큐브말랑이(약하고 빠름 · 말랑 박치기 · 쭈욱 늘이기 · 통통 연속) — 1웨이브부터
            var pink = BuildMonster("PinkCube", "핑크큐브말랑이", SkillAttribute.Red, 40f, 70f, 2.8f, 0.32f, 0.62f, 35, 10, 1, 0, playerMask,
                                    new[] { "CUBE_BOUNCE", "CUBE_STRETCH", "CUBE_TRIPLE" });
            if (pink != null) { ApplyMaterial(pink, "Cube_Jelly", JellyMaterial()); Save(pink, "PinkCube"); }
            // ── 일반 몬스터 ②: 그린볼런(단단하고 느림 · 블럭 박치기 · 톱니 던지기 = 원거리) — 2웨이브부터, 한 웨이브에 2마리까지
            var green = BuildMonster("GreenBlock", "그린볼런", SkillAttribute.Green, 85f, 140f, 1.5f, 0.36f, 0.66f, 60, 18, 2, 2, playerMask,
                                     new[] { "BLOCK_GEAR", "BLOCK_RAM" });
            if (green != null) { ApplyMaterial(green, "Block_Green", PlasticMaterial("GreenBlock_Plastic", new Color(0.08f, 0.5f, 0.06f), 0.82f)); ApplyMaterial(green, "Block_Gear", PlasticMaterial("GreenBlock_Gear", new Color(0.12f, 0.58f, 0.09f), 0.85f)); Save(green, "GreenBlock"); }
            // ── 자원: 땅콩빵(부수면 땅콩빵 조각) — 적 레이어 · 팀 Enemy 라 플레이어 공격이 맞음, 조준·웨이브에는 안 들어감
            var peanutModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "PeanutBread.fbx");
            if (peanutModel != null)
            {
                var pn = new GameObject("PeanutBread"); var pm = (GameObject)PrefabUtility.InstantiatePrefab(peanutModel, pn.transform); pm.name = "Model";
                var pc = pn.AddComponent<BoxCollider>(); pc.center = new Vector3(0, 0.17f, 0); pc.size = new Vector3(1.0f, 0.34f, 0.5f);
                var ps = pn.AddComponent<CharacterStats>(); ps.displayName = "땅콩빵"; ps.team = Team.Enemy; ps.hasSanity = false; ps.maxHealth = ps.currentHealth = 30f; ps.invulnerableTime = 0.05f; ps.maxStagger = 9999f;
                pn.AddComponent<ResourceNode>();
                ApplyMaterial(pn, "Peanut_Cloth", NormalMapped("PeanutBread_Cloth", new Color(0.95f, 0.94f, 0.9f), "Weave_Normal", 6f, 0.25f, 1.2f));
                Save(pn, "PeanutBread");
            }
            // ── 펫: 하프물범(야생 → 땅콩빵 조각으로 길들임). 펫 레이어(적 공격이 맞음) · 팀 Player(민미 공격은 안 맞음)
            var sealModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Seal.fbx");
            if (sealModel != null)
            {
                var sg = (GameObject)PrefabUtility.InstantiatePrefab(sealModel); sg.name = "Seal";
                var sa = sg.GetComponentInChildren<Animator>(); if (sa == null) sa = sg.AddComponent<Animator>(); sa.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Anims + "Seal.controller"); sa.applyRootMotion = false;
                var scol = sg.AddComponent<CapsuleCollider>(); scol.direction = 2; scol.center = new Vector3(0, 0.22f, 0); scol.radius = 0.22f; scol.height = 0.8f;
                var sag = sg.AddComponent<UnityEngine.AI.NavMeshAgent>(); sag.radius = 0.3f; sag.height = 0.45f; sag.speed = 3.3f; sag.stoppingDistance = 0.5f;
                var sst = sg.AddComponent<CharacterStats>(); sst.displayName = "하프물범"; sst.team = Team.Player; sst.attribute = SkillAttribute.Blue; sst.maxHealth = sst.currentHealth = 60f; sst.invulnerableTime = 0.1f;
                sg.AddComponent<CoinSystem>(); sg.AddComponent<StatusEffectManager>(); sg.AddComponent<DefenseController>(); sg.AddComponent<EnergyManager>(); var sr = sg.AddComponent<CombatResolver>(); sr.targetMask = enemyMask;
                var sex = sg.AddComponent<SkillExecutor>(); sex.useAnimationEvents = false; sex.animator = sa;
                var pet = sg.AddComponent<PetSeal>(); pet.bump = SkillAsset("SEAL_BUMP"); pet.bubble = SkillAsset("SEAL_BUBBLE");
                var fur = NormalMapped("Seal_Fur", new Color(0.93f, 0.93f, 0.95f), "Fur_Normal", 8f, 0.15f, 0.9f); ApplyMaterial(sg, "Seal_Fur", fur);
                ApplyMaterial(sg, "Seal_Nose", NormalMapped("Seal_Nose", new Color(0.5f, 0.5f, 0.55f), "Fur_Normal", 8f, 0.1f, 0.9f));
                SetLayer(sg, petLayer >= 0 ? petLayer : playerLayer); PrefabUtility.SaveAsPrefabAsset(sg, Prefabs + "Seal.prefab"); Object.DestroyImmediate(sg);
            }
            // ── NPC: 조랭하프족(마을 주민·퀘스트). 기본 레이어(전투에 안 걸림)
            var npcModel = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Joraeng.fbx");
            if (npcModel != null)
            {
                var ng = (GameObject)PrefabUtility.InstantiatePrefab(npcModel); ng.name = "Joraeng";
                var na = ng.GetComponentInChildren<Animator>(); if (na == null) na = ng.AddComponent<Animator>(); na.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Anims + "Joraeng.controller"); na.applyRootMotion = false;
                var ncol = ng.AddComponent<CapsuleCollider>(); ncol.center = new Vector3(0, 0.48f, 0); ncol.radius = 0.34f; ncol.height = 0.96f;
                var nag = ng.AddComponent<UnityEngine.AI.NavMeshAgent>(); nag.radius = 0.32f; nag.height = 0.95f; nag.speed = 0.9f; nag.stoppingDistance = 0.1f; nag.avoidancePriority = 70;
                ng.AddComponent<Npc>();
                ApplyMaterial(ng, "Joraeng_Fur", NormalMapped("Joraeng_Fur", new Color(0.97f, 0.95f, 0.9f), "Fur_Normal", 9f, 0.12f, 1.0f));
                ApplyMaterial(ng, "Joraeng_Scarf", NormalMapped("Joraeng_Scarf", Color.white, "Fur_Normal", 6f, 0.1f, 0.7f));   // 색은 Npc 가 주민마다
                var eye = new Material(Shader.Find("Standard")) { name = "Joraeng_Eye", color = new Color(0.03f, 0.03f, 0.04f) }; eye.SetFloat("_Glossiness", 0.92f); ApplyMaterial(ng, "Joraeng_Eye", SaveMat(eye, "Joraeng_Eye"));
                var brown = new Material(Shader.Find("Standard")) { name = "Joraeng_Brown", color = new Color(0.45f, 0.27f, 0.13f) }; brown.SetFloat("_Glossiness", 0.2f); ApplyMaterial(ng, "Joraeng_Brown", SaveMat(brown, "Joraeng_Brown"));
                var seam = new Material(Shader.Find("Standard")) { name = "Joraeng_Seam", color = new Color(0.86f, 0.82f, 0.74f) }; seam.SetFloat("_Glossiness", 0.05f); ApplyMaterial(ng, "Joraeng_Seam", SaveMat(seam, "Joraeng_Seam"));
                PrefabUtility.SaveAsPrefabAsset(ng, Prefabs + "Joraeng.prefab"); Object.DestroyImmediate(ng);
            }
            AssetDatabase.SaveAssets();
            void Save(GameObject g, string n) { SetLayer(g, enemyLayer); PrefabUtility.SaveAsPrefabAsset(g, Prefabs + n + ".prefab"); Object.DestroyImmediate(g); }
        }

        /// <summary>애니메이션이 있는 일반 몬스터 프리팹(저장은 부른 쪽에서). 반환 null = 모델 없음.</summary>
        static GameObject BuildMonster(string model, string displayName, SkillAttribute attr, float hp, float stagger, float speed, float radius, float height, int exp, int score,
                                       int minWave, int maxPerWave, int playerMask, string[] skillIds)
        {
            var m = AssetDatabase.LoadAssetAtPath<GameObject>(Models + model + ".fbx"); if (m == null) { Debug.LogWarning($"Assets/Models/{model}.fbx 가 없습니다."); return null; }
            var g = (GameObject)PrefabUtility.InstantiatePrefab(m); g.name = model;
            var an = g.GetComponentInChildren<Animator>(); if (an == null) an = g.AddComponent<Animator>();
            an.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Anims + model + ".controller"); an.applyRootMotion = false;
            var col = g.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, height * 0.5f, 0); col.radius = radius; col.height = height;
            var ag = g.AddComponent<UnityEngine.AI.NavMeshAgent>(); ag.radius = radius; ag.height = height; ag.speed = speed; ag.angularSpeed = 0f; ag.stoppingDistance = 1f;
            var st = g.AddComponent<CharacterStats>(); st.displayName = displayName; st.team = Team.Enemy; st.attribute = attr; st.maxHealth = st.currentHealth = hp; st.invulnerableTime = 0.1f; st.maxStagger = stagger;
            g.AddComponent<CoinSystem>(); g.AddComponent<StatusEffectManager>(); g.AddComponent<DefenseController>(); g.AddComponent<EnergyManager>(); var rs = g.AddComponent<CombatResolver>(); rs.targetMask = playerMask;
            var ex = g.AddComponent<SkillExecutor>(); ex.useAnimationEvents = false; ex.animator = an;
            var ec = g.AddComponent<EnemyCombatController>(); ec.archetype = EnemyArchetype.GroundMonster; ec.animator = an; ec.agent = ag; ec.visual = g.transform; ec.moveSpeed = speed; ec.score = score;
            ec.expReward = exp; ec.minWave = minWave; ec.maxPerWave = maxPerWave; ec.levelOffset = exp >= 60 ? 1 : 0;   // 그린볼런(경험치 큼) 레벨 +1
            ec.skills = new System.Collections.Generic.List<SkillDefinition>(); foreach (var id in skillIds) { var sk = SkillAsset(id); if (sk != null) ec.skills.Add(sk); }
            ec.basicSkill = ec.skills.Count > 0 ? ec.skills[0] : null;
            g.AddComponent<EnemyBrain>().personality = EnemyPersonality.Grunt;
            return g;
        }

        /// <summary>이름이 key 인 재질 칸을 바꿈(FBX 가 만든 재질 대신 손질한 재질).</summary>
        static void ApplyMaterial(GameObject g, string key, Material mat)
        {
            foreach (var r in g.GetComponentsInChildren<Renderer>(true))
            {
                var ms = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < ms.Length; i++) if (ms[i] != null && ms[i].name.Replace(" (Instance)", "") == key) { ms[i] = mat; changed = true; }
                if (changed) r.sharedMaterials = ms;
            }
        }
        const string Mats = "Assets/Materials/";
        static Material SaveMat(Material m, string name) { System.IO.Directory.CreateDirectory(Mats); var path = Mats + name + ".mat"; AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(m, path); return m; }
        /// <summary>핑크큐브 젤리: 진한 분홍 + 매끈한 광택 + 오돌도돌 노멀 맵(타일 6) + 속에서 비치는 듯한 은은한 분홍 빛.</summary>
        static Material JellyMaterial()
        {
            var m = new Material(Shader.Find("Standard")) { name = "PinkCube_Jelly", color = new Color(0.98f, 0.36f, 0.52f) };
            m.SetFloat("_Glossiness", 0.72f); m.SetFloat("_Metallic", 0f);
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(Models + "Textures/Pebble_Normal.png");
            if (nm != null) { m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", 0.8f); m.SetTextureScale("_MainTex", new Vector2(6, 6)); m.EnableKeyword("_NORMALMAP"); }
            m.SetColor("_EmissionColor", new Color(0.22f, 0.03f, 0.08f)); m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return SaveMat(m, "PinkCube_Jelly");
        }
        /// <summary>노멀 맵 재질(천·털): 색 · 노멀 맵(Models/Textures) · 타일 · 광택 · 노멀 세기.</summary>
        static Material NormalMapped(string name, Color c, string normal, float tile, float gloss, float bump)
        {
            var m = new Material(Shader.Find("Standard")) { name = name, color = c }; m.SetFloat("_Glossiness", gloss); m.SetFloat("_Metallic", 0f);
            var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(Models + "Textures/" + normal + ".png");
            if (nm != null) { m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", bump); m.SetTextureScale("_MainTex", new Vector2(tile, tile)); m.EnableKeyword("_NORMALMAP"); }
            return SaveMat(m, name);
        }
        /// <summary>그린볼런: 단단하고 매끈한 플라스틱(높은 광택).</summary>
        static Material PlasticMaterial(string name, Color c, float gloss)
        {
            var m = new Material(Shader.Find("Standard")) { name = name, color = c }; m.SetFloat("_Glossiness", gloss); m.SetFloat("_Metallic", 0f); return SaveMat(m, name);
        }

        [MenuItem("Moomin/코인 확률 검증")]
        public static void CoinStats()
        {
            var rng = new System.Random(1); const int N = 200000; var sb = new System.Text.StringBuilder("[Moomin] 정신력별 앞면 비율 (설정값 / 실제)\n");
            for (int sv = -45; sv <= 45; sv += 15) { int h = 0; for (int i = 0; i < N; i++) if (CoinMath.Toss(sv, rng)) h++; sb.AppendLine($"  정신력 {sv,3}: {CoinMath.HeadsProbability(sv):P1} / {(float)h / N:P1}"); }
            var dist = new int[4]; for (int i = 0; i < N; i++) dist[CoinMath.TossMany(3, 20, rng)]++;
            sb.AppendLine("  3코인·정신력+20(70%) 앞면 수: " + string.Join("  ", Enumerable.Range(0, 4).Select(k => $"{k}개 {(float)dist[k] / N:P1}")) + "   (이론 2.7% / 18.9% / 44.1% / 34.3%)");
            var q = SkillLibrary.Get("SKILL_001"); var b = SkillLibrary.Get("ENEMY_BUMP"); int win = 0, draw = 0;
            for (int i = 0; i < N; i++) { int p = CoinMath.Power(q.basePower, q.coinPower, CoinMath.Toss(0, rng)), e = CoinMath.Power(b.basePower, b.coinPower, CoinMath.Toss(0, rng)); if (p > e) win++; else if (p == e) draw++; }
            sb.AppendLine($"  클래시(공격 1회 비교, 정신력 0): 연속 베기 vs 몸통 박치기 = 승 {(float)win / N:P1} · 무 {(float)draw / N:P1}");
            Debug.Log(sb.ToString());
        }

        /// <summary>해·바닥·맵(Assets/City 의 City_*.fbx → World, 소품 자리, MapTools 규칙)을 지금 씬에 배치. 반환: 시작 위치(PlayerSpawn).</summary>
        static Vector3 PlaceWorld(out GameObject ground)
        {
            Vector3 spawn = new Vector3(0, 0.1f, 0);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "Ground"; ground.transform.localScale = new Vector3(40, 1, 40);
            // 도시 배경(Assets/City 의 City.fbx 등): 보이는 메시 + 보이지 않는 충돌 메시(City_Collision) + 시작 위치(PlayerSpawn)
            // 맵: Assets/City 의 City_*.fbx(최적화 맵, 분류별 파일)를 World 아래에 모으고 소품 자리를 만든 뒤 MapTools 규칙 적용. 예전 한 덩어리 City.fbx 도 지원.
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/City" });
            if (guids.Length > 0)
            {
                var worldGo = new GameObject("World"); var world = worldGo.transform; bool optimized = false;
                foreach (var g in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(g); if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;
                    var part = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path)); part.transform.SetParent(world, false);
                    optimized |= System.IO.Path.GetFileNameWithoutExtension(path).StartsWith("City_");
                }
                foreach (var n in new[] { "Props", "Interiors", "Decor" }) if (Find(world, n) == null) new GameObject(n).transform.SetParent(world, false);   // 이후 소품·인테리어 자리
                ground.GetComponent<MeshRenderer>().enabled = false; ground.transform.position = new Vector3(0, -0.05f, 0);   // 맵 밖으로 떨어지지 않게 투명 바닥은 남김
                if (optimized) EditorTools.MapTools.Apply(world);
                else
                {   // 예전 한 덩어리 맵(City · City_Collision)
                    var colT = Find(world, "City_Collision");
                    if (colT != null) { var mf = colT.GetComponent<MeshFilter>(); var mc = colT.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; var mr = colT.GetComponent<MeshRenderer>(); if (mr) mr.enabled = false; }
                    else foreach (var m in world.GetComponentsInChildren<MeshRenderer>()) if (m.GetComponent<Collider>() == null) m.gameObject.AddComponent<MeshCollider>();
                    foreach (var t in world.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
                }
                var sp = Find(world, "PlayerSpawn"); if (sp != null) spawn = sp.position + Vector3.up * 0.1f;
            }
            return spawn;
        }

        [MenuItem("Moomin/5. 타이틀 씬 만들기")]
        public static void BuildTitleScene()
        {
            System.IO.Directory.CreateDirectory(Scenes);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PlaceWorld(out var ground); Object.DestroyImmediate(ground);   // 타이틀은 걸어 다니지 않음
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camGo.tag = "MainCamera"; camGo.AddComponent<TitleCamera>();
            new GameObject("Title").AddComponent<TitleScreen>();
            EditorSceneManager.SaveScene(scene, Scenes + "Title.unity"); RegisterScenes();
            Debug.Log("[Moomin] 타이틀 씬 완성 → Assets/Scenes/Title.unity");
        }

        /// <summary>빌드 설정: Title(처음) → Main. 있는 씬만.</summary>
        static void RegisterScenes()
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            foreach (var n in new[] { "Title", "Main" }) if (System.IO.File.Exists(Scenes + n + ".unity")) list.Add(new EditorBuildSettingsScene(Scenes + n + ".unity", true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        [MenuItem("Moomin/4. 메인 씬 만들기")]
        public static void BuildScene()
        {
            System.IO.Directory.CreateDirectory(Scenes);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var spawn = PlaceWorld(out _);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Player.prefab")); player.transform.position = spawn;
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<ThirdPersonCamera>(); cam.target = player.transform;
            player.GetComponent<PlayerController>().cam = camGo.transform;
            var gmGo = new GameObject("GameManager"); var gm = gmGo.AddComponent<GameManager>(); gmGo.AddComponent<NavMeshBaker>(); gmGo.AddComponent<ClashManager>();
            gm.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ShadowBlob.prefab"); gm.player = player.GetComponent<CharacterStats>();
            gm.peanutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "PeanutBread.prefab"); gm.sealPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Seal.prefab");   // 자연: 땅콩빵 · 야생 하프물범
            gm.npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Joraeng.prefab");   // 마을: 조랭이(퀘스트) · 몽글이 · 포실이
            foreach (var n in new[] { "ShadowBlob", "SleepyBear", "Goraegorae", "PinkCube", "GreenBlock" }) { var e = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + n + ".prefab"); if (e != null) gm.enemyPrefabs.Add(e); }
            EditorSceneManager.SaveScene(scene, Scenes + "Main.unity");
            RegisterScenes();
        }
    }
}
#endif
