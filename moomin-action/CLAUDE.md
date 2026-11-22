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
- 순수 로직 `Core/VitalsMath.cs`(DamageChunk · ExpCurve 100×레벨^1.6 · NumberFormat 2.6M/14.44M) → status_tests. 레벨·경험치 `Player/Experience.cs`(적 처치 시 `EnemyCombatController.expReward`: 그림자 40 · 곰 70 · 보스 ×5, 레벨 업 최대 체력 +10).
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
