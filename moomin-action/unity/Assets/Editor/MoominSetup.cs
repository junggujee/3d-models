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
        public static void All() { ConfigureImports(); BuildController(); BuildBearController(); BuildWhaleController(); BuildPrefabs(); BuildScene(); Debug.Log("[Moomin] 설정 완료 → Assets/Scenes/Main.unity 를 열고 Play!"); }

        [MenuItem("Moomin/1. FBX 임포트 설정")]
        public static void ConfigureImports()
        {
            var imp = AssetImporter.GetAtPath(Models + "Moomin.fbx") as ModelImporter; if (imp == null) { Debug.LogError("Assets/Models/Moomin.fbx 가 없습니다."); return; }
            imp.animationType = ModelImporterAnimationType.Generic; imp.importAnimation = true; imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            var clips = imp.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.name = c.name.Contains("|") ? c.name.Split('|').Last() : c.name; c.loopTime = c.name is "Idle" or "Walk" or "Run" or "Guard"; c.lockRootRotation = c.lockRootHeightY = c.lockRootPositionXZ = true;
                // 공격 클립의 타격 프레임(Blender: Attack1/2 = 24프레임 중 11, Attack3 = 30프레임 중 14)에 명세 v2 6-2 의 애니메이션 이벤트 배치
                float hit = c.name switch { "Attack1" => 10f / 23f, "Attack2" => 10f / 23f, "Attack3" => 13f / 29f, _ => -1f };
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
            foreach (var n in new[] { "StarSparkSword.fbx", "ShadowBlob.fbx" }) { var m = AssetImporter.GetAtPath(Models + n) as ModelImporter; if (m != null) { m.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; m.SaveAndReimport(); } }
        }

        static AnimationClip ClipOf(string fbx, string name) => AssetDatabase.LoadAllAssetsAtPath(Models + fbx).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview") && c.name.EndsWith(name));

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

        [MenuItem("Moomin/2-1. 곰(인간형 적) Animator 만들기")]
        public static void BuildBearController()
        {
            if (AssetImporter.GetAtPath(Models + "SleepyBear.fbx") == null) { Debug.LogWarning("Assets/Models/SleepyBear.fbx 가 없습니다."); return; }
            MakeController("SleepyBear.fbx", Anims + "SleepyBear.controller", new[] { "Windup", "Attack1", "Attack2", "Attack3", "Guard", "Dodge", "Hit", "Death" }, new[] { "Attack1", "Attack2", "Attack3", "Dodge", "Hit" });
        }

        static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath(Models + "Moomin.fbx").OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview") && c.name.EndsWith(name));

        [MenuItem("Moomin/2-2. 고래고래(지상괴물형 적) Animator 만들기")]
        public static void BuildWhaleController()
        {
            if (AssetImporter.GetAtPath(Models + "Goraegorae.fbx") == null) { Debug.LogWarning("Assets/Models/Goraegorae.fbx 가 없습니다."); return; }
            MakeController("Goraegorae.fbx", Anims + "Goraegorae.controller", new[] { "Windup", "Attack1", "Attack2", "Attack3", "Jump", "BeamCharge", "Beam", "BeamHold", "BeamEnd", "Spout", "Guard", "Dodge", "Hit", "Death" },
                           new[] { "Attack1", "Attack2", "Attack3", "Jump", "BeamEnd", "Spout", "Dodge", "Hit" });
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
            foreach (var n in new[] { "Roll", "Guard", "Attack1", "Attack2", "Attack3", "Hit", "Death" }) { var s = sm.AddState(n); s.motion = Clip(n); if (n.StartsWith("Attack") || n == "Hit" || n == "Roll") { var t = s.AddExitTransition(); t.hasExitTime = true; t.exitTime = 0.95f; t.duration = 0.1f; } }
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

            // ── 플레이어: CharacterController · PlayerController · CharacterStats · CoinSystem · SkillExecutor · CombatResolver · CombatManager ...
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Moomin.fbx")); player.name = "Player"; player.tag = "Player";
            var cc = player.AddComponent<CharacterController>(); cc.height = 1.3f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.65f, 0); cc.stepOffset = 0.3f;
            var anim = player.GetComponentInChildren<Animator>(); anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Anims + "Moomin.controller"); anim.applyRootMotion = false;
            var stats = player.AddComponent<CharacterStats>(); stats.displayName = "민미"; stats.team = Team.Player; stats.attribute = SkillAttribute.Black; stats.maxHealth = stats.currentHealth = 100f;
            player.AddComponent<Stamina>(); player.AddComponent<CoinSystem>(); player.AddComponent<StatusEffectManager>(); player.AddComponent<Experience>(); player.AddComponent<DefenseController>(); player.AddComponent<EnergyManager>(); var res = player.AddComponent<CombatResolver>(); res.targetMask = enemyMask;
            var ex = player.AddComponent<SkillExecutor>(); ex.useAnimationEvents = true; ex.animator = anim;
            var cm = player.AddComponent<CombatManager>();
            cm.basic1 = SkillAsset("BASIC_1"); cm.basic2 = SkillAsset("BASIC_2"); cm.basic3 = SkillAsset("BASIC_3"); cm.skill1 = SkillAsset("SKILL_001"); cm.skill2 = SkillAsset("SKILL_002"); cm.ultimate = SkillAsset("SKILL_003"); SkillAsset("SKILL_004");   // 충격파(광역 테스트) 에셋도 생성
            player.AddComponent<Inventory>(); player.AddComponent<LockOnSystem>(); player.AddComponent<PlayerController>();
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
                var becc = b.GetComponent<EnemyCombatController>(); if (becc != null) becc.expReward = 70;   // 인간형은 경험치 더 많이
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
                wcc.expReward = 150; wcc.minWave = 2; wcc.maxPerWave = 1; wcc.showBossBar = true; wcc.bossEmblem = "Whale";   // 전용 보스바
                wcc.skills = new System.Collections.Generic.List<SkillDefinition> { SkillAsset("WHALE_FLIPPER"), SkillAsset("WHALE_RAM"), SkillAsset("WHALE_BEAM"), SkillAsset("WHALE_JUMP"), SkillAsset("WHALE_TAIL"), SkillAsset("WHALE_SPOUT") };
                wcc.basicSkill = wcc.skills[0];
                w.AddComponent<EnemyBrain>().personality = EnemyPersonality.Grunt;
                SetLayer(w, enemyLayer);
                PrefabUtility.SaveAsPrefabAsset(w, Prefabs + "Goraegorae.prefab"); Object.DestroyImmediate(w);
            }
            AssetDatabase.SaveAssets();
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

        [MenuItem("Moomin/4. 메인 씬 만들기")]
        public static void BuildScene()
        {
            System.IO.Directory.CreateDirectory(Scenes);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Sun").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f; sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane); ground.name = "Ground"; ground.transform.localScale = new Vector3(40, 1, 40);
            // 도시 배경(Assets/City 의 City.fbx 등): 보이는 메시 + 보이지 않는 충돌 메시(City_Collision) + 시작 위치(PlayerSpawn)
            Vector3 spawn = new Vector3(0, 0.1f, 0);
            var guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/City" });
            if (guids.Length > 0)
            {
                var city = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]))); city.name = "City";
                ground.GetComponent<MeshRenderer>().enabled = false; ground.transform.position = new Vector3(0, -0.05f, 0);   // 도시 밖으로 떨어지지 않게 투명 바닥은 남김
                var colT = Find(city.transform, "City_Collision");
                if (colT != null) { var mf = colT.GetComponent<MeshFilter>(); var mc = colT.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; var mr = colT.GetComponent<MeshRenderer>(); if (mr) mr.enabled = false; }
                else foreach (var m in city.GetComponentsInChildren<MeshRenderer>()) if (m.GetComponent<Collider>() == null) m.gameObject.AddComponent<MeshCollider>();
                foreach (var t in city.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
                var sp = Find(city.transform, "PlayerSpawn"); if (sp != null) spawn = sp.position + Vector3.up * 0.1f;
            }
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Player.prefab")); player.transform.position = spawn;
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)); camGo.tag = "MainCamera"; var cam = camGo.AddComponent<ThirdPersonCamera>(); cam.target = player.transform;
            player.GetComponent<PlayerController>().cam = camGo.transform;
            var gmGo = new GameObject("GameManager"); var gm = gmGo.AddComponent<GameManager>(); gmGo.AddComponent<NavMeshBaker>(); gmGo.AddComponent<ClashManager>();
            gm.enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "ShadowBlob.prefab"); gm.player = player.GetComponent<CharacterStats>();
            foreach (var n in new[] { "ShadowBlob", "SleepyBear", "Goraegorae" }) { var e = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + n + ".prefab"); if (e != null) gm.enemyPrefabs.Add(e); }
            EditorSceneManager.SaveScene(scene, Scenes + "Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scenes + "Main.unity", true) };
        }
    }
}
#endif
