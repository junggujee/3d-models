# moomin-action 인수인계 (Claude용)

Blender(bpy 5.3, headless) → FBX/GLB → Unity(C#) 파이프라인. 민미로 도시를 돌며 스파크 검으로 싸우는 웨이브 액션.

- `blender/lib.py`: 공용 도우미. **좌표 규칙**: three.js 좌표(Y 위, +Z 앞)를 `T(x,y,z)`로 Blender(Z 위, 앞 = -Y)로 변환, `K=0.6`배(미터). 캐릭터는 Blender에서 -Y를 바라봄 → FBX(`axis_forward=-Z, axis_up=Y`)/GLB 에서 Unity +Z 전방.
- Blender 5.x 는 **레이어드 액션**이라 `action.fcurves` 가 없음(쓰지 말 것). 키는 `pose_bone.keyframe_insert`.
- 뼈 회전은 `rot_world(arm, bone, 월드축, 각도)` 로 월드 축 기준 값을 로컬 오일러로 변환해서 넣음(뼈 롤 신경 안 써도 됨).
- 조각마다 뼈 1개에 100% 결합(인형처럼 단단하게). 눈은 `eye.L/R` 뼈 Y 스케일로 깜빡임.
- 애니메이션 길이: Idle 61, Walk 31, Run 21, **Roll 20, Guard 12(루프)**, Attack1/2 24, Attack3 30, Hit 18, Death 40 (30fps). 타격 프레임 ≈ 11 → `SwordCombat` 의 `hitTime`(≈0.37s) 과 맞춤. 클립 길이를 바꾸면 `SwordCombat.combo` 도 같이 수정.
- Unity 쪽은 애니메이션 이벤트 없이 타이머로 타격 판정. `Animator` 상태명 = 클립명(`Attack1` 등), 이동은 `Speed` 파라미터 블렌드 트리.
- `Editor/MoominSetup.cs` 가 임포트 설정·컨트롤러·프리팹·씬을 자동 생성(`Assets/City/` 의 모델은 씬에 자동 배치).
- 입력은 `InputAdapter` 한 곳에서만(새/기존 Input System 모두 지원). 다른 곳에서 `UnityEngine.Input` 직접 사용 금지.
- **Unity 미검증**. Unity 에서 오류가 보고되면 그 메시지부터 고칠 것.
- 도시 `.skp` 는 변환 전. 변환 파일을 받으면 Blender 로 폴리곤·재질 정리 + 충돌용 단순 메시 생성 예정.

## 구르기·인벤토리·UI (추가)
- **회피(Space, 예전 Q)**: `PlayerController` 가 처리. 스태미나 18 소모, 무적 0.05~0.40초(`Health.Invulnerable`), 입력 방향(없으면 바라보는 방향)으로 이동, 공격 후딜 취소. 애니메이션 `Roll`(20f, 몸이 한 바퀴 구름). Blender `build_moomin.py` 의 `roll()` — hips 뼈의 오일러 X 값을 그대로 키잉(월드 X축과 일치함을 assert 로 확인; `rot_world` 는 ±π 로 감겨서 한 바퀴 회전에 못 씀).
- **스태미나**(`Stamina`): 회피·달리기(초당 9) 소모(공격은 이제 스태미나 대신 쿨타임·정신력), 0.7초 후 초당 32 회복.
- **인벤토리**(`Inventory/`): `ItemDef`/`ItemDatabase`(새 아이템은 목록에 한 줄 추가), 30칸·스택·금화·장착 무기(공격력 배율·칼날 색)·퀵슬롯 4칸·사용/버리기/정렬, PlayerPrefs 저장. 적이 죽으면 `EnemyAI.DropLoot()` → `ItemPickup`(자석처럼 끌려와 자동 획득). 아이콘은 `ItemIcons` 가 코드로 그림(이미지 파일 없음).
- **UI**(`UI/`, uGUI 를 코드로 생성, 프리팹 없음): `UITheme`(어두운 양피지 + 금박 이중 테두리, 대화 선택지식 » 막대), `UIFactory`, `HUD`, `PauseMenu`(휴식), `SettingsMenu`, `InventoryMenu`, `InfoPage`(조작법), `UIRoot`(입력 라우팅). `GameManager.Start` 가 `UIRoot` 를 자동 생성. 메뉴가 열리면 `UIState.Paused` 로 게임이 멈춤 — **게임플레이 스크립트는 Update 맨 앞에서 `UIState.Paused` 를 확인할 것.**
- 한글 폰트: `Font.CreateDynamicFontFromOSFont`(나눔명조 → 바탕 → 맑은 고딕 …) 로 OS 폰트 사용. 모든 PC 에서 같은 모양을 원하면 TTF 를 프로젝트에 넣고 `UITheme.Font` 를 바꿀 것.
- Unity 6(또는 2023.1+) 권장(`FindFirstObjectByType`; 이전 버전은 `#if` 로 대체됨). 새/기존 Input System 모두 지원, UI 는 `com.unity.ugui`(기본 포함) 사용.

## 코인 전투 — 명세 v2.0 (상세: docs/COMBAT.md)
- 구조: CombatManager(플레이어 입력) → SkillExecutor(공격 순서, 플레이어·적 공용) → CoinSystem(판정) → AttackContext(결과) → CombatResolver(히트박스·피해) → ClashManager(위력 비교). 적: EnemyBrain → EnemyDecisionSystem(점수) → EnemyCombatController(NavMesh 이동·행동).
- 코인 하나 = 공격 한 번, 공격 위력 = 기본 + (앞면 ? 코인 위력 : 0) (누적 아님). 클래시는 양쪽 히트박스가 동시에 열렸을 때만, 이미 던진 코인으로 비교. 진 쪽은 남은 코인 소모 없이 중단.
- 체력·정신력은 CharacterStats 하나(예전 Health·SanitySystem 은 삭제). 팀은 CharacterStats.team.
- 스킬 수치: SkillLibrary(기본) → 메뉴가 Assets/Data/Skills/*.asset 생성·연결. 정신력 비용은 "이 이상일 때 사용 가능 + 소모".
- **수정 후 반드시 `tools/compile_check.sh` 로 컴파일 확인**(dotnet-sdk-8.0 필요, Unity 참조 어셈블리는 NuGet 에서 자동 다운로드). 수식을 바꾸면 `tools/coin_sim.py` 도 같이 고쳐 balance.md 재생성.
- Unity 함정: 직렬화 필드·GetComponent 결과에 `??`/`??=` 금지(에디터의 '가짜 null'), `using System;` 파일에서는 `UnityEngine.Random`/`[UnityEngine.Range]`, 정적 이벤트는 구독 해제(CombatHUD.Dispose).
- 키: Space 회피, 좌클릭 공격, 우클릭 방어(0.2초 패링), Q/E/R 스킬, Tab 조준, I 가방.

## 적 유형(인간형 · 지상괴물형 · 바다괴물형)
- `EnemyCombatController.archetype`. 인간형 = `blender/build_bear.py` → SleepyBear(뼈 8개, Windup·Attack1~3·Guard·Dodge·Hit·Death 클립). 적 스킬의 hitTime 은 클립 타격 프레임(Attack1/2 0.35초, Attack3 0.43초)에 맞춤 — 적은 애니메이션 이벤트를 쓰지 않음.
- 애니메이션 있는 적: SkillExecutor.animator 를 setup 에서 지정, EnemyCombatController 가 Speed·상태를 재생(Animated). 없는 적: visual 스케일 연출.
- 줄무늬처럼 무늬가 필요한 부분은 텍스처 대신 **면마다 재질을 나눠** 칠함(FBX→Unity 에서 확실히 보이게). 표면에 붙는 패치는 몸 타원식으로 z 를 계산해 띄움(body_front).
- 새 적 유형 추가: build_*.py(모델·애니메이션) → SkillLibrary 에 기술 → MoominSetup 에 프리팹(BuildPrefabs) → GameManager.enemyPrefabs.

## 속성·상성·스킬 패널
- 속성 7색(SkillDefinition.attribute) + 몸 속성(CharacterStats.attribute). 상성 = 색상환 다음 색을 이김(빨>주>노>초>파>보>빨), 검정 중립. 수치·판정은 `SkillAttributes`(SkillDefinition.cs) 한 곳: 유리 피해 ×1.25·클래시 +2, 불리 ×0.8.
- 스킬 패널 v2 `UI/SkillPanel.cs`(적 머리 위 `EnemySkillPanels` + 플레이어 왼쪽 아래), 붓 질감 `UI/SkillPanelArt.cs`, 일러스트 `UI/SkillIllustration.cs`(실루엣+빛 연출; Python 시제품 `tools/illustration_preview.py` 와 같은 알고리즘 — 한쪽을 바꾸면 다른 쪽도). 목업 `previews/skill_panel_v2_mock.png`.
- 상태이상·경직·에너지(상세: docs/STATUS_ENERGY.md): `StatusEffects/`(출혈·화상·침잠·진동, 강도/횟수 분리) + `StatusEffectManager`, 경직 게이지 `CharacterStats.AddStagger`, 에너지 `CombatResources/EnergyManager`. 발동은 전투 사건에서만, 같은 `AttackContext.id` 로는 한 번만. 화상만 시간(누적 후 간격마다). 상태이상 피해는 `CharacterStats.LoseHealth`(경직·회피 없음).
- 핵심 로직은 순수 C#(UnityEngine 없음) → `cd tools && dotnet run --project status_tests` 로 검증. 이 파일들에 UnityEngine 을 쓰지 말 것.

## 맵(blender/build_map.py)
- 원본 GLB(SketchUp→openskp→Blender): 맨 위 계층 + 메시 공유 = 부품 원본 → 삭제. 1단위 = 0.14m, 바닥 z=-51 → 0.
- 순서: 분석(형태: 수직 벽 비율·축 정렬 비율로 산/언덕 ↔ 건물) → 질감(면 종류·텍스처·상자 투영 UV) → 채색(산 높이별, 집마다 벽 5색·지붕 4색, 성은 석재+슬레이트).
- 겹친 면: 원본에 같은 부품이 같은 자리에 두 벌(예: 그룹#115·#117)씩 있어 같은 위치 면이 2장 → 검게(렌더)/깜빡임(Unity). merge() 에서 꼭짓점 위치(5mm) 같은 면 하나만 남김(City 약 23만 장 제거). 평면 나무는 제거 후 뒤집은 사본을 붙여 양면.
- 면 방향: 수직이 아닌 면은 모두 위로(Unity 뒷면 컬링 대비).
- 성능 함정: bpy.ops.make_single_user·object.join·mesh.materials.append 를 물체 2만 개에 쓰면 제곱으로 느림 → data.copy(), numpy 직접 합치기(merge), 재질 번호는 face 속성 "fmat" 에 저장 후 한 번에.
- 면 방향: 수직이 아닌 면은 모두 위로(Unity 뒷면 컬링 대비). 남은 문제: 일부 평지붕이 어둡게 보임(원본 어두운 재질 추정, 미확인).
