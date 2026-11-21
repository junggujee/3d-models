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

## 코인 전투 (림버스 컴퍼니 재해석) — 상세는 docs/COMBAT.md
- 키: Space 회피, 좌클릭 일반 공격, 우클릭 방어(패링 0.2초), Q/E/R 스킬, Tab 타깃 고정, I 가방(Tab 아님). 점프는 없음.
- 흐름: `SkillManager`(입력·타이밍·쿨타임) → `AttackController.ExecuteHit`(합 → 코인 → 피해) → `ClashManager`(다회 합, 결과 적용). 적이 맞히는 순간 `EnemyAI.Impact` → `CombatManager.ReceiveEnemyAttack`(회피 → 합 → 방어 합 → 일방 공격).
- 수식은 `CoinMath` 한 곳. 바꾸면 `tools/coin_sim.py` 도 같이 고쳐서 `docs/balance.md` 를 다시 생성할 것(두 구현이 같은 규칙이어야 함).
- 스킬 수치는 `SkillLibrary`(기본값) 또는 `Assets/Resources/Skills/*.asset`(덮어쓰기). 적 공격은 `EnemyAttack` 팩토리 + 프리팹 목록.
- 정적 이벤트(`ClashManager.Clashed`, `EnemyAI.AnyCoinTossed`)는 구독한 쪽이 해제해야 함(`CombatHUD.Dispose`).
- `using System;` 이 있는 파일에서 `[Range]` 대신 `[UnityEngine.Range]`, `Random` 대신 `UnityEngine.Random` 을 쓸 것(모호성 오류).
