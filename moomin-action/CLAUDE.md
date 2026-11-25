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
- **UI**(`UI/`, uGUI 를 코드로 생성, 프리팹 없음): `UITheme`(어두운 양피지 + 금박 이중 테두리, 대화 선택지식 » 막대), `UIFactory`, `HUD`, `PauseMenu`(휴식), `SettingsMenu`, `CharacterSheet`(가방·장비·스킬), `CodexPage`(도감), `InfoPage`(조작법), `UIRoot`(입력 라우팅). `GameManager.Start` 가 `UIRoot` 를 자동 생성. 메뉴가 열리면 `UIState.Paused` 로 게임이 멈춤 — **게임플레이 스크립트는 Update 맨 앞에서 `UIState.Paused` 를 확인할 것.**
- 한글 폰트: `Resources/Fonts/NanumMyeongjo.ttf`(OFL, 라이선스 같은 폴더) 우선, 없으면 OS 폰트(나눔명조 → 바탕 → 맑은 고딕 …). 타이틀 제목은 `UITheme.TitleFont`(나눔명조 ExtraBold).
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
- 지상괴물형 = `blender/build_whale.py` → **고래고래(지상 최대의 끼룩이)**(사용자 스케치: 길쭉한 몸 + 볼록한 이마에서 튀어나온 주둥이, 끝에 분홍 입): 척추 사슬 fluke←tail2←tail1←body→head(+jaw·spout·flipper.L/R 은 head 자식). 몸통·이마·주둥이·꼬리·얼굴은 **부드러운 피부**(z 위치로 이웃 척추 두 개에 무게, JOINTS/BLEND) → 휘어도 한 몸. 애니메이션은 `pose()`로 매 키 사슬 전체를 키잉 + `wave()` 로 뒤로 갈수록 늦는 물결(오버래핑 액션). 클립: Idle Walk Run Windup Attack1(박치기 11) Attack2(지느러미 11) Attack3(꼬리 한 바퀴 14) Jump(착지 24) BeamCharge(27) Beam(발사 8) BeamHold(루프 18) BeamEnd(24) Spout(16) Guard Dodge Hit Death. 프리팹 ×2 배율, 체력 220·경직 180, 2웨이브부터 웨이브당 1마리(`EnemyCombatController.minWave/maxPerWave`), 이름 옆 별칭 `CharacterStats.epithet`.
- 고래 기술(SkillLibrary WHALE_*): 고래박치기(합 가능) · 고래빔(직선 광역 `AreaShape.Line` 13m×2.4m, 합 불가, 파란 직사각형 예고, 4코인 0.5초 간격 → 약 2초 유지, `windupAnimation` BeamCharge + `WhaleChargeFx` 기 모으기, `recoveryAnimation` BeamEnd, 연출 `Effects/WhaleBeamFx`: 한 번 쏘면 스킬이 끝날 때까지 유지·코인마다 Pulse, 겹 3개(안개·색이 바뀌는 몸통·흰 심)+흐르는 결·고리·반짝이·물보라·점광원) · 점프 내려찍기(원 4m, 합 불가, 공중에서 `lungeDelay/lungeTime` 동안 날아가고 예고는 착지 자리) · 지느러미 연타 · 꼬리 휘두르기(원) · 물기둥(원, 합 불가). 연출 `SkillDefinition.vfx` → `Effects/SkillVfxPlayer`(빔·충격파·물기둥). 예고 색: 빨강 일반 · 파랑 합 불가 · 보라 방어 불가.
- 고래 기술 일러스트: `SkillDefinition.subject = Whale` → `SkillIllustration.WhaleScene/WhaleFigure`(고래 옆모습 실루엣 + 눈 흰 점 + 숨구멍 물방울, Python 쌍둥이 `illustration_preview.whale()`). 동작 Beam(빛줄기·결·고리)·Leap(내리꽂힘·충격 가시)·Spout(물기둥)·Rush·Flurry·Slash. 미리보기 `previews/whale_skill_illustrations.png`·`whale_skill_panels.png`.
- 보스바 `UI/BossBar.cs`(화면 아래 가운데, `EnemyCombatController.showBossBar` · `bossEmblem`): 문장 메달·이름+별칭·빨간 조각 체력·경직 게이지·상태이상·%·물결 장식, 등장(떠오르며 차오름)·격파 연출. 그림 `tools/boss_bar_art.py`, 미리보기 `tools/boss_bar_mock.py`. 웨이브 보스(ConfigureBoss)도 자동 표시.
- 새 적 유형 추가: build_*.py(모델·애니메이션) → SkillLibrary 에 기술 → MoominSetup 에 프리팹(BuildPrefabs) → GameManager.enemyPrefabs.

## 속성·상성·스킬 패널
- 속성 7색(SkillDefinition.attribute) + 몸 속성(CharacterStats.attribute). 상성 = 색상환 다음 색을 이김(빨>주>노>초>파>보>빨), 검정 중립. 수치·판정은 `SkillAttributes`(SkillDefinition.cs) 한 곳: 유리 피해 ×1.25·클래시 +2, 불리 ×0.8.
- 스킬 패널 v3 `UI/SkillPanel.cs`(적 머리 위 `EnemySkillPanels` + 플레이어 왼쪽 아래 + 클래시 때 가운데 `UI/ClashStage.cs`: 두 패널 나란히·화면 어둡게·모서리 붉은 긁힘). 위쪽 3칸 = 주인의 힘/위력 감소/마비(실시간). 거친 금박선 `UI/SkillPanelArt.cs`(Frame·BannerInk·Slashes 는 해시로 획 배치), 흑백 먹선 일러스트 `UI/SkillIllustration.cs`. **Python 쌍둥이**: `tools/illustration_preview.py`(일러스트)·`tools/skill_panel_mock.py`(패널 좌표·스프라이트) — C# 를 바꾸면 같이 고치고 `previews/skill_panel_v3_mock.png` 재생성.
- 상태이상·경직·에너지(상세: docs/STATUS_ENERGY.md): `StatusEffects/`(출혈·화상·침잠·진동, 강도/횟수 분리) + `StatusEffectManager`, 경직 게이지 `CharacterStats.AddStagger`, 에너지 `CombatResources/EnergyManager`. 발동은 전투 사건에서만, 같은 `AttackContext.id` 로는 한 번만. 화상만 시간(누적 후 간격마다). 상태이상 피해는 `CharacterStats.LoseHealth`(경직·회피 없음).
- 상태이상·에너지 아이콘: `tools/status_icons.py` → `Assets/Resources/StatusIcons/*.png`(SkillPanelArt 가 Resources.Load, 없으면 SDF 대체 아이콘). 진동=황금빛 주황, 침잠=짙은 남색.
- 가드·회피·광역(상세: docs/DEFENSE.md): `Defense/DefenseController`(가드 = 피해 × (1-감소율), 회피 = 시작할 때 코인 1번 → 회피 위력 ≥ 공격 위력이면 피해 없음·유지, 작으면 종료). 구르기/적 회피 동작이 StartEvade/EndEvade(무적 시간 없음). 단일 공격 = 한 명(조준 우선), `isAreaAttack` = 반경 안 전원에게 같은 AttackContext. `unblockable` 는 `canBeGuarded`(반대 의미), `clashEnabled` 는 `canBeClashed`.
- 합 연출: ClashManager.Sequence 코루틴(부딪힘 → 물러남 → 재충돌 … → ApplyResult → Victory). 판정은 즉시, 결과 적용은 연출 뒤. 묶기 = `CharacterStats.InClash`(PlayerController·CombatManager·EnemyCombatController 가 멈춤) + `SkillExecutor.Held`(시간·히트박스·애니메이션 이벤트 무시). HUD 는 ClashManager.Clashed/ClashExchange/ClashEnded → ClashStage. 섬광 `Effects/ClashFlash`, 흔들림 `ThirdPersonCamera.Shake`.
- 코인 위력 효과: 힘(자신)·위력 감소·마비 → `SkillExecutor.OnCoinAttack` 에서 `StatusEffectManager.UseCoinModifiers(ctx.id)` 로 코인마다 1회 소모, 위력 = 기본 + (앞면 ? CoinModifiers.CoinValue(코인 위력) : 0).
- 핵심 로직은 순수 C#(UnityEngine 없음) → `cd tools && dotnet run --project status_tests` 로 검증. 이 파일들에 UnityEngine 을 쓰지 말 것.

## 체력바·경험치바
- `UI/VitalBars.cs`(HUD 왼쪽 위): 체력 "체력 283/391" + 줄었을 때만 붉은 [72%] 꼬리표, 맞으면 줄어든 만큼 빨간 조각(0.45초 유지 → 0.35초 동안 줄며 사라짐, 연속 피해는 이어서 커짐), 체력 25% 아래면 붉게 깜빡. 경험치바 + 레벨 원판, 얻은 만큼 밝게 번쩍이며 차오름, 레벨 업이면 끝까지 찼다가 다시.
- 순수 로직 `Core/VitalsMath.cs`(DamageChunk · ExpCurve 100×레벨^1.6 · NumberFormat 2.6M/14.44M) → status_tests. 레벨·경험치 `Player/Experience.cs`(적 처치 시 `EnemyCombatController.expReward`: 그림자 40 · 곰 70 · 보스 ×5, 레벨 업 최대 체력 +12·공격력 +0.6·완전 회복 — docs/LEVEL.md).
- 그림 `tools/bar_art.py` → `Resources/UI/Bars/*.png`(BarArt 가 9분할로 로드, 테두리 값은 두 파일이 같아야 함). 미리보기 `tools/vital_bars_mock.py` → `previews/vital_bars_mock.png`.
- HUD 세로 배치: 체력 -22 · 경험치 -80 · 스태미나 -124 · 정신력 -142 · 에너지 -166 · 상태 -196 · 목표 -236.

## 월드맵(M)
- 그림 만들기: `blender/render_world_map.py <폴더>`(exports/City.glb 를 마을 축이 가로가 되게 돌려 고도 50° 정사영으로 color·normal·height 렌더 + map_meta.json) → `python3 tools/world_map_art.py <폴더>`(뭉갠 채색·외곽선·주요 장소 스포트라이트·양피지 가장자리·나침반, 핀 아이콘, 접힌 종이 뒷면) → `Resources/UI/WorldMap/*`. 미리보기 `tools/world_map_mock.py` → `previews/world_map.png`·`world_map_unfold.png`.
- 주요 장소 자동 탐지(면 **개수** 밀도: 면적으로 하면 긴 성벽에 끌림): 성 = Roof_Slate, 산지 = Mountain_Snow(성벽 안), 동·서쪽 마을 = 지붕 2-평균, 광장 = 두 마을 사이.
- `UI/WorldMapView.cs`: 세 폭(가운데 고정, 왼쪽/오른쪽이 접는 선 기준 Y 회전, 90° 넘으면 뒷면) 펼치기 1.15초 · 접기 0.75초(같은 곡선 역재생, 도중에 반대로 눌러도 그 자리에서), 열린 동안 UIState.Paused + 실제 시간. 핀 겹침 방지는 `Layout(id)`(mock 의 LAYOUT 과 같게). 내 위치는 PlayerSpawn 으로 Unity↔Blender 축(바꿈·부호·배율 8가지)을 자동 보정.
- bpy 는 컨테이너가 새로 뜨면 사라질 수 있음: `python3 -m venv <폴더> && <폴더>/bin/pip install bpy`.

## 맵(blender/build_map.py)
- 원본 GLB(SketchUp→openskp→Blender): 맨 위 계층 + 메시 공유 = 부품 원본 → 삭제. 1단위 = 0.14m, 바닥 z=-51 → 0.
- 순서: 분석(형태: 수직 벽 비율·축 정렬 비율로 산/언덕 ↔ 건물) → 질감(면 종류·텍스처·상자 투영 UV) → 채색(산 높이별, 집마다 벽 5색·지붕 4색, 성은 석재+슬레이트).
- 겹친 면: 원본에 같은 부품이 같은 자리에 두 벌(예: 그룹#115·#117)씩 있어 같은 위치 면이 2장 → 검게(렌더)/깜빡임(Unity). merge() 에서 꼭짓점 위치(5mm) 같은 면 하나만 남김(City 약 23만 장 제거). 평면 나무는 제거 후 뒤집은 사본을 붙여 양면.
- 면 방향: 수직이 아닌 면은 모두 위로(Unity 뒷면 컬링 대비).
- 성능 함정: bpy.ops.make_single_user·object.join·mesh.materials.append 를 물체 2만 개에 쓰면 제곱으로 느림 → data.copy(), numpy 직접 합치기(merge), 재질 번호는 face 속성 "fmat" 에 저장 후 한 번에.
- 면 방향: 수직이 아닌 면은 모두 위로(Unity 뒷면 컬링 대비). 남은 문제: 일부 평지붕이 어둡게 보임(원본 어두운 재질 추정, 미확인).

## 타이틀 · 저장 · 게임 오버 (상세: docs/TITLE_SAVE.md)
- 장면: `Title.unity`(메뉴 5. 타이틀 씬 만들기) → `Main.unity`. 장면 이동은 항상 `SceneFader.Load`(빌드 설정에 없으면 지금 장면 다시 열기).
- 타이틀 `Title/TitleScreen.cs`(IMenuHost, 메뉴 스택) + `Title/TitleCamera.cs`(성·산지 중심 궤도, 값은 `blender/render_title_bg.py` 와 같음). 그림 `tools/title_art.py` → `Resources/UI/Title/*`, 색 `UITheme.TitleStyle`(Palette). 미리보기 `tools/title_mock.py <렌더 폴더>` → previews/title_mock·save_slots_mock·game_over_mock.png — **TitleScreen·SaveSlotPage·GameOverScreen 배치를 바꾸면 mock 도 같이**.
- 메뉴 페이지는 `MenuPage.host`(IMenuHost)로 열고 닫음 → 같은 페이지를 게임(UIRoot)·타이틀에서 공용(`BuildFor`). 게임 전용 페이지만 `ui`(UIRoot) 사용.
- 저장 `Core/SaveSystem.cs`(SaveData JSON + jpg, 슬롯 0~2 수동 · 3 자동), 표시 규칙 `Core/SaveFormat.cs`(순수 C#, status_tests). 불러오기 = `SaveSystem.Pending` → `Inventory.Awake`(가방) + `GameManager.Start`(실행 순서 -50, 나머지). 가방은 PlayerPrefs 대신 슬롯에. 저장은 웨이브 사이에만(`SaveSystem.CanSave`).
- 여정 기록 `Core/RunStats.cs`(처치·합 승패·최고 위력, 저장 파일에 포함). 장소 이름·좌표 `Core/MapPlaces.cs`(월드맵 데이터 + PlayerSpawn 으로 축 맞춤 — WorldMapView 도 이것을 씀).
- 게임 오버 `UI/GameOverScreen.cs`(HUD 가 만듦): "패배...." 띠 + 눈이 ×× 인 민미 얼굴 → 결과(얼굴은 패널 왼쪽 위로) + 마지막 저장에서 다시/타이틀로. 밝기 상수 ShadeBand·BandFill·ShadeResults(mock 의 SHADE_* 와 같게). 얼굴 그림 = `blender/render_defeat_face.py`(실제 모델, 눈 면 삭제 + 눈 자리 json) → `tools/defeat_face.py`(×× 2D 붓선·아래 흐리게) → `Resources/UI/GameOver/DefeatFace.png`. R 재시작은 없어짐.

## 일반 몬스터 (상세: docs/MONSTERS.md)
- 핑크큐브말랑이 `blender/build_pink_cube.py`(혹 27개 구에 중심에서 쏜 광선의 부드러운 최댓값 = 몸 표면, 얼굴은 front_y(혹 앞끝 soft-min, **z 는 CZ 기준으로 빼서**)에 붙임, 뼈 root→body(바닥 중심: 눌림·늘어남·숙임)→top(높이로 부드럽게 나눈 위쪽, lag 프레임 늦게 출렁)). 키 목록 → `track()` 부드럽게 이어 매 프레임 키잉. 오돌도돌 = `tools/pebble_normal.py` 노멀 맵(Unity 재질 `MoominSetup.JellyMaterial`).
- 그린볼런 `blender/build_green_block.py`(판 6개 = 뼈 6개 100%, 겹치지 않게 x 판 전체·y 판 x 안쪽·z 판 x·y 안쪽, 칸 FR 비율, 모서리 칸 STEP 낮춤, 톱니 gear 뼈). Death = 판마다 포물선 + 기울어 납작 착지(death_frame). 재질은 MoominSetup.PlasticMaterial(이름 정확히 일치로 교체 — Block_GreenInner 는 그대로).
- 원거리 = `SkillDefinition.isProjectile` → `Combat/Projectile.cs`(근접과 합 없음 = `MeleeClashable`, 투사체끼리만 공중 합). 새 원거리 기술은 SkillLibrary 에서 isProjectile + projectile* 값만 넣으면 됨.
- 프리팹 `MoominSetup.BuildMonster`(애니메이션 있는 일반 몬스터 공용) · 컨트롤러 `BuildMonsterControllers`. 일러스트 subject Slime/Block(C# CubeScene ↔ Python cube()).

## 장비 · 공격력 (상세: docs/EQUIPMENT.md)
- 피해 D = 공격력 × (최종 위력 ÷ 10) × M × R (`Core/EquipmentMath.Damage`, 순수 C# · status_tests). 최종 위력 = 기본 + (앞면 ? 코인 위력 : 0) + `CharacterStats.equipFinalPower`. 공격력 `CharacterStats.Attack` = attack(10) + levelAttack(Experience, 레벨당 0.5) + equipAttack.
- 장비 = `Inventory.armor[4]`(머리·가슴·다리·발) · `artifacts[3]` · `equippedWeapon` · `presets[3]`. 새 장비는 ItemDatabase 한 줄(type Armor/Artifact, slot, grade, stats, model, attachBone). `Inventory.ApplyEquipment` 가 능력치·최대 체력(equipHealth 는 저장 시 뺌)·`EquipmentVisuals` 동기화.
- 장비 3D 모양은 **민미 모델 공간(기본 자세)** 에서 만들어 내보내고, EquipmentVisuals 가 스킨 메시 bindpose 로 뼈에 붙임(blender/build_headband.py 참고). 아이콘은 Resources/ItemIcons/<id>.png 우선.
- 장비 화면은 캐릭터 시트에 합침(아래 '캐릭터 시트' 절). 3D 모습 `UI/EquipmentPreview.cs`(플레이어 레이어만 그리는 카메라 → RenderTexture).
- **한 줄에 `코드; // 설명 코드;` 쓰지 말 것** — 주석 뒤 코드가 사라짐(실제로 4군데 있었음). unity_audit 4b 가 잡음.

## 자원 · 펫 · 집 장비 (상세: docs/PET_RESOURCE.md)
- 땅콩빵 `blender/build_peanut.py`(초타원체 + 잘록 + 짜임 노멀 `tools/fabric_normals.py`, LobeL·LobeR 두 물체) → `World/ResourceNode.cs`(적 레이어·팀 Enemy CharacterStats 로 플레이어 공격이 맞음, EnemyCombatController 가 없어 조준·웨이브 밖). 조각 `peanut_piece`.
- 생성 자리 `blender/build_spawn_points.py` → `Resources/World/SpawnPoints.json`(peaks/nature, Blender 좌표) → `World/NatureSpawner.cs`(GameManager.peanutPrefab/sealPrefab 이 있으면 붙음, MapPlaces.BlenderToWorld + NavMesh 로 맞춤, testNearStart 로 시작 근처에도 1개씩).
- 펫 `blender/build_seal.py`(gift-3d-models createSeal 치수 그대로, 뼈 body→chest/hips + 지느러미 4) → `World/PetSeal.cs`(야생 → F 로 조각 3개 → 길들임, G 따라오기/기다려, 보조 공격·막아서기·응원·물어 오기·냄새 맡기·기절·친밀도·레벨). 막아서기 = `CombatResolver.Interceptor` + `SkillExecutor.TimeToFirstHit` 에 맞춘 박치기. Pet 레이어(적 targetMask 에 포함). HUD `UI/PetHUD.cs`, 저장 `SaveData.pet` → `SaveSystem.LoadedPet`.
- 한국어 조사는 `PetSeal.Josa(단어, 받침 있을 때, 없을 때)` — "(이)가" 같은 괄호 쓰지 말 것.
- 집 장비 `blender/build_home_gear.py`(민미 공간, 부품마다 FBX) · `ItemDef.model = "경로@뼈|경로@뼈"`(EquipmentVisuals 가 부품마다 붙임). EquipStats 에 guard·evadePower·moveSpeed. 얻는 법 `HomeGear.Random`(적 4% · 땅콩빵 6%).

## NPC · 대화 · 퀘스트 · 글꼴 (상세: docs/NPC_QUEST.md, docs/FONTS.md)
- 조랭하프족 `blender/build_joraeng.py`(회전체 몸 r(z) + 앞뒤 납작 DEPTH, 얼굴은 `surf(x, z, off)` 로 표면에 붙임, 뼈 root→body→head(높이로 부드럽게)·eye(세로 스케일 = 눈 감기)·flip·fin, 상호작용 애니메이션 11개). 초상화 = 같은 스크립트가 Cycles 2패스(평면색 + Freestyle 선 / 음영) → `tools/portrait_art.py` 셀 채색 → `Resources/UI/Portraits/<이름>_<표정>.png`. **Workbench 는 이 컨테이너에서 GPU 가 없어 못 씀.**
- F 상호작용은 `World/Interaction.cs` 하나로(대상은 매 프레임 `Interaction.Offer`, 가장 가까운 것만, `InteractionRunner` 가 LateUpdate 에서 결정). 펫·NPC 공용 — 새 상호작용 대상도 이것으로.
- NPC `World/Npc.cs`(대화 중엔 애니메이터 UnscaledTime, 머리 위 !/?), 배치 `World/VillageSpawner.cs`(GameManager.npcPrefab). 대본 `Story/JoraengStory.cs`(Dialogue/DialogueLine/DialogueChoice — `Say(표정, 동작, 글).Label().Choose().End()`). 퀘스트 `Story/QuestLog.cs`(순수 C#, status_tests) + `Story/Quests.cs`(정의·건네기·보상·Announce) → 저장 `SaveData.quests`, HUD `UI/QuestTracker.cs`.
- 대화창 `UI/DialogueBox.cs`(MenuPage, `UIRoot.StartDialogue`). 그림 `tools/dialogue_art.py` → `Resources/UI/Dialogue`, 미리보기 `tools/dialogue_mock.py` — **배치 숫자(PW·PH·TBX… )를 바꾸면 세 곳 모두**.
- 글꼴 역할: `UITheme.DisplayFont`(빛의 계승자 Bold — 제목·장·보스·큰 알림) · `HeadingFont`(가웨인 파일이 있으면 그것, 없으면 빛의 계승자 Regular — 이름·퀘스트 제목·머리글) · `SkillFont`(빛의 계승자 Regular + 빛) · `BodyFont`(나눔명조 — 대사·설명·숫자). 꾸밈은 `UIFactory.StyleDisplay/StyleHeading/StyleSkill/Divider`, 효과 `UI/TextFx.cs`(UIGradient·TextGlow). FontStyle.Bold 로 가짜 굵게 하지 말 것.
- 주석이 객체 초기화 항목(`이름 = 값,`)을 삼키는 경우도 unity_audit 4b 가 잡음(SaveData.sanity 가 저장 안 되던 버그).

## 캐릭터 시트 · 도감 · 기절 효과 (상세: docs/CHARACTER_SHEET.md)
- `UI/CharacterSheet.cs`(I = 가방부터, C = 장비 칸부터 — `UIRoot.OpenSheet`): 밝은 종이 + 먹칠 + 양털 테두리. A 아치 틀 3D 민미(장비 즉시 반영, 바뀌면 톡 + 금빛) + 장비 칸 8 · B 막대·공격/방어 · D 자세히(+ 도감 버튼) · E 분류 5 + 10×3 · F 스킬 카드 가로 스크롤(일러스트 · 이름 먹띠 · 최종 위력) → 클릭 시 상세 창(`HandleCancel` 로 Esc 가 창만 닫음).
- `UI/CodexPage.cs`(K): 가진 장비·스킬만. [ ] 탭, 분류 칩, 세로 카드(등급·속성 색 번짐), 오른쪽 큰 그림 + 대표 수치. 스킬 글자는 `UI/SkillInfo.cs`(종류는 키 칸으로: 좌클릭/Q·E/R).
- 그림 `tools/sheet_art.py` → `Resources/UI/Sheet|Codex|Fx` — **판 그림에 칸 테두리·머리 먹줄이 들어 있으니 SEC/CSEC 좌표와 C# 상수를 같이**. 미리보기 `tools/sheet_mock.py`(previews/sheet_mock·sheet_skill_popup_mock·codex_mock·stun_fx_mock).
- 경직이 깨지면 `World/StunFx.cs`(GameManager 가 `CharacterStats.AnyStaggerBroken` 에 Hook): 몸 위 주황 폭발 + "기절" 글자(카메라 쪽으로 당겨 가려지지 않게), 경직이 끝나면 사라짐.

## 레벨 (상세: docs/LEVEL.md)
- 규칙 `Core/LevelMath.cs`(순수 C#, status_tests): 3레벨 차이마다 코인 위력 ±1(최대 ±3, 앞면 코인 위력에만) · 적 레벨 = 웨이브 ± 1 + levelOffset + 보스 3(체력 +6%/공격 +0.5 per Lv) · 경험치 레벨 차이 ±10%(50~150%) · 10레벨마다 칭호.
- `CharacterStats.level`(플레이어 = Experience.Refresh, 적 = GameManager.SpawnWave → `EnemyCombatController.SetLevel`, 펫 = ApplyLevel). 보정은 `SkillExecutor.OnCoinAttack` → `LevelOpponent()` → `AttackContext.levelBonus`.
- 연출 `Effects/LevelUpFx.cs`(금빛 기둥·고리) + `UI/LevelUpPopup.cs`(Experience.LevelJumped). 빛의 계승자 글꼴엔 "−"(U+2212)가 없으니 화면 글자는 ASCII "-".

## 무기 3종 (상세: docs/WEAPONS.md)
- `Combat/WeaponKit.cs`(spark · shield · dual: 평타 3·Q·E·R·방어 코인·방어 자세·고유 효과) ← `ItemDef.kit` ← `Inventory.ApplyWeapon` → `CombatManager.ApplyKit`(KitChanged → HUD 스킬 칸). 스파크 검은 인스펙터/에셋 스킬을 그대로.
- 방어 코인(`DefenseController.StartGuard` 에서 한 번, `ResolveGuard`: 방어 위력 ≥ 공격 위력 → 완벽 방어) · 파괴불가 코인(`unbreakableCoins`, ClashManager.ApplyResult 에서 끊지 않음) · 코인별 타격 시점 `hitTimes` · `vfxLastCoinOnly`.
- 고유 효과 `Weapons/WeaponPassives.cs`: ShieldBarrier(CharacterStats.barrier) · DuelHeat(bonusMultiplier, requiresDuelHeat 스킬 게이트) · WeaponSpin(Grip_*→Rotor_* 축 회전).
- 모델 `blender/build_weapons.py`(민미 공간, Moomin.blend 를 열어 만듦 — build_moomin.py 다음에), 클립 `blender/moomin_weapon_anims.py`(HIT 표 = MoominSetup.WeaponHitFrames = SkillLibrary hitTime). 클립 이름은 정확 일치로 찾음(`Clip`/`ClipOf` — "Guard" 가 "ShieldGuard" 에 걸리지 않게).
- 레이저 블레이더(kit laser, 회전 쌍검 상위): `build_weapons.py` ③(사용자 spin-saber-3d 를 옮김, Grip_R_Laser/Rotor_R_Laser, 몸 오른쪽 옆면에서 바퀴처럼 돎) · 양손 클립 Laser*(`two()`) · `Weapons/SaberTrail.cs`(TrailRenderer 4 + 빛 가루) · 발광 재질은 이름(Glow·Core·Edge)으로 `EquipmentVisuals.GlowMaterials`.

## 최적화 · 디버깅 (상세: docs/OPTIMIZATION.md)
- 수정 뒤 `bash tools/check_all.sh`(컴파일 + unity_audit ✘0 + status_tests) 하나로 확인.
- 불꽃·빛 가루는 `SparkBurst.Spawn` 하나(장면당 ParticleSystem 하나에 Emit) — 연출마다 ParticleSystem 만들지 말 것. 투사체·줍기 구슬 재질은 정적 캐시.
- `Inventory.Add` 는 **남은 개수**를 돌려줌(0 = 다 들어감). 가방이 차면 남은 건 `ItemPickup.Spawn` 으로 떨어뜨림. 장면의 줍기 아이템은 `ItemPickup.All`.
- 레이저 스킬(LASER_*) 일러스트는 네온 덧칠(`SkillIllustration.LaserScene`/`neon` = `illustration_preview.laser_patterns`) — 두 곳 숫자를 같이. LASER_Q = 광륜 투척(`ProjectileKind.LaserDisc`).

## 토벌 퀘스트 (반복, 상세: docs/NPC_QUEST.md)
- 정의·기록·수식 `Story/HuntLog.cs`(순수 C# — HuntDef/HuntCatalog/HuntLog/HuntMath, status_tests) · 집계/보상 `Story/Hunts.cs`(`EnemyCombatController.AnyKilled`, GameManager Hook/Unhook) · 대화 `Story/HuntStory.cs` · 구역 `World/HuntZone.cs` + `HuntZoneSpawner.cs`(enemyPrefabs 에서 이름으로) · 저장 `SaveData.hunts`.
- 4개: 몽글이 말랑이×5 · 꼬물이 그림자×6 · 포실이 곰×3 · 똘똘이 그린볼런×4. 보상 레벨 비례 + 첫 완료 ×1.5. NPC 는 `questId` 가 HuntCatalog id 면 토벌 대화(Npc.Hunt).
- 구역 몬스터 = `zoneId`·`kind` 가 붙고 `EnemyBrain.home/leash` 로 구역에 묶임. 새 퀘스트는 HuntCatalog 에 한 덩어리 + 주민 questId.

## 바다 · 선착장 · 헤엄 · 통통배 (상세: docs/SEA.md)
- 맵은 성벽 + 산맥에 둘러싸인 정24각형 원판(C(-50,28.3), 꼭짓점 586m) → 바다는 원판 밖 전체, 선착장은 지도 남쪽 끝 꼭짓점. 마을 ↔ 선착장은 바닷길 문(`World/SeaGate.cs`, `SceneFader.Cover`)으로만. 해수면 z = -0.8.
- 모델: `blender/build_seaside.py`(맵 좌표, City 와 같은 FBX 설정 → Resources/World/Seaside.fbx) · `build_boat.py`(Boat.fbx) · `build_joraeng.py --fisher`(FisherHat.fbx + fisher_ 초상화) · 민미 물놀이 클립 `moomin_swim_anims.py`(SwimIdle·Swim·DiveIdle·Dive·BoatSit).
- 런타임: `World/Seaside.cs`(GameManager 가 NavMesh 전에 Ensure — 충돌·Ocean·선착장 NavMesh(`NavMeshBaker.Extra`)·문·배·어부) · `World/Ocean.cs` + `World/OceanMath.cs`(게르스트너 4개, 순수 C# — **셰이더 `Resources/Shaders/MoominOcean.shader` 와 같은 식**, 바꾸면 둘 다) · `Player/PlayerSwim.cs`(Z 잠수, 숨 25초) · `World/Boat.cs` · `Effects/WaterFx.cs` · `UI/BreathMeter.cs` · `Story/HarborStory.cs`.
- 헤엄·배 중엔 PlayerController·CombatManager 가 멈춤(`PlayerSwim.Active`). 알림은 `Toast.Show`. 월드맵 방향 글자는 `MapPlaces.MapDelta`(그림 기준 북 — Blender +Y 가 북이 아님).

## 바다 밑 용궁 · 고래고래 임금님 (상세: docs/PALACE_BOSS.md)
- 용궁 `blender/build_palace.py`(맵 좌표 — 선착장 끝 u 165m, 분지 -42m(`build_seaside.py` PALACE_*), `meshkit.py` 틀 도구, 석상 = Goraegorae.blend 줄인 메시) → Resources/World/Palace.fbx → `World/Palace.cs`(Ensure — GameManager 가 Seaside 다음에: 충돌 · 돔 · NavMesh · 빛 · 해파리/물고기 · 임금님 생성/5분 뒤 다시).
- 공기방울 돔 `World/AirDome.cs`(안 = 물 없음: PlayerSwim 끝, Ocean 물속 안개 끔, PlayerController 천천히 내려옴) · 막 셰이더 `Resources/Shaders/MoominBubble.shader`(버블 감옥 `BubbleTrap` 도).
- 보스 `Enemy/WhaleKing.cs`(금관·망토 = `whale_king_parts.py`/`build_whale_king.py` → Resources/Boss, BoneAttach) — 순찰(`EnemyBrain.externalIdle`), 50% 각성 → KING_* 5종(합 불가 탄막: 회전 탄막 고리 · 딸기 포물선 · 버블 감옥, 박치기만 합 가능). 투사체 여러 발/포물선/감옥 = `SkillDefinition.projectileCount/Spread/Spin/Lob/trapSeconds` + `Combat/ProjectilePattern.cs`(순수 C#).
- 이야기 퀘스트 `Quests.GrumpyKing`(철썩이, `HarborStory.KingQuest`) — 아이템 royal_pearl. 일러스트 동작 Barrage·Bomb·Bubbles(C# ↔ Python 같이).
- 퀘스트 완료 뒤 = 생일 파티: `blender/build_party.py`(Party.fbx: 3단 딸기 케이크 · 풍선 Balloon_* · 깃발 · 선물) → `Palace.SetParty`(손님 5 + `PartyGuest`) · `WhaleKing.EnterParty`(팀 Player, 보스바 끔, 케이크 둘레 순찰, F 대화 `Story/KingStory.cs` → "재대결!" = `StartRematch`, 이기면 8초 뒤 다시 파티). 임금님 초상화 `build_whale_king.py --portrait` → king_normal.

## 성문 지기 맨두 (상세: docs/MANDU.md)
- 모델 `blender/build_mandu.py` → `Models/Mandu.fbx`(+ `--portrait` 초상화). 프리팹 Mandu 는 minWave 999 라 웨이브엔 안 나옴
- `World/CastleGuard.cs`(CastleGate + ManduGuard): 근무(팀 Player, F 대화) ↔ 전투(보스바, "억지로 들어간다"). 통과 = Quests.Log "castle_gate"
- `Story/ManduStory.cs` 대사는 말끝마다 "~맨두". 꿀바른 땅콩빵(`honey_peanut_bread`)을 바치면 통과
- 전투: 속성 노랑 · 타입 진동 · 파란 야구방망이(평소 옆구리, 전투 때 손에). 몽둥이 찜질/홈런!!/맨두맨두(합 방어 → 배치기 반격)/냠냠쫍쫍/맨두돌진 + 필살기 100만톤 맨두(체력 25%)
- 쓰러뜨리면 레이저 블레이더 재료(`mandu_core`·`bat_alloy`) 드롭. 패배 모션 = 방망이를 놓치고 납작하게 뻗음

## 마을 대장간 · 풍선고래 대장장이 (상세: docs/SMITHY.md)
- 건물 `blender/build_smithy.py`(→ Resources/World/Smithy.fbx). `--carve` 는 마을 동쪽 집 하나를 City_TownE·City_Collision 에서 실제로 지움(이미 적용됨 — 다시 돌리지 말 것)
- 대장장이 `blender/build_balloon_whale.py` → Models/BalloonWhale.fbx(풍선이라 떠 있음). 프리팹은 Resources/Npc/BalloonWhale, GameManager.balloonSmithPrefab
- 런타임 `World/Smithy.cs`(화덕 불빛·굴뚝 연기·NavMesh) · `World/BalloonSmith.cs`(F 대화) · `Story/SmithStory.cs` · `UI/SmithPage.cs` · `Effects/ForgeFx.cs`
- 수치는 전부 `Crafting/ForgeMath.cs`(순수 C#, status_tests): 제작법 6개 · 강화 +0~+5(성공률 95/80/60/40/25%, 비용 ×1/2/4/7/12, +4 부터 실패 시 한 단계 하락, 무기는 안 부서짐)
- 강화 단계는 `Inventory.enhance`(아이디 → +N)에 저장 — 무기·장비는 하나씩만 가지므로 아이디로 충분
