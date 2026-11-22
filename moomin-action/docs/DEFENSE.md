# 가드 · 회피 · 광역 공격 (확정 규칙)

핵심은 **코인 판정(SkillExecutor, 공격마다 한 번 → `AttackContext`)과 대상별 피해 판정(CombatResolver)을 분리**하는 것입니다. 3코인 광역 스킬이면 코인은 3번만 굴리고, 각 코인 결과를 범위 안 모든 적에게 똑같이 적용합니다. 가드·회피·체력·상성은 적마다 따로 계산합니다.

| 규칙 | 동작 |
|---|---|
| **가드 (Reduction)** | 위력 비교 없이 피해 × (1 − `guardReduction`). 기본 0.5 → 100 피해가 50 |
| **회피 (Coin Clash)** | 회피를 시작할 때 회피 코인을 **한 번** 굴림: 위력 = `evadeBasePower` + (앞면 ? `evadeCoinPower` : 0). 공격이 들어올 때마다 회피 위력 ≥ 공격 위력이면 **피해 없음 + 회피 유지**(다음 공격도 같은 위력으로 판정), 작으면 **회피 종료 + 피해** |
| **광역 (Shared Coin)** | `isAreaAttack` 스킬은 공격자 중심 `areaRadius` 안의 모든 적에게, 같은 코인 결과·같은 위력을 적용 |
| **단일** | `isAreaAttack` 꺼짐: 사거리·각도 안에서 **한 명**(조준 대상 우선, 없으면 가장 가까운 적) |
| **중복 방지** | 한 캐릭터에 콜라이더가 여러 개여도 한 코인에 한 번(`DefenseRules.Unique` + `AttackContext.hitTargets`) |

판정 순서(대상 한 명, 코인 하나): ① 회피(`canBeEvaded`) → ② 가드(`canBeGuarded`, 가드 시작 직후 0.2초면 패링) → ③ 피해 D = P × M × R(R에 가드·상성·취약 포함) → 상태이상·에너지.

## 기존 코드와 맞춘 부분

명세의 코드를 그대로 붙여 넣으면 상태이상·에너지·합 연출·스킬 패널·적 AI가 끊어집니다. 그래서 규칙과 이름(`DefenseController`, `isAreaAttack`, `areaRadius`, `targetLayers`, `canBeClashed`, `canBeGuarded`, `canBeEvaded`, `StartGuard/StopGuard/StartEvade/TryEvade/EndEvade/GetDamageMultiplier`)은 명세대로 두고, 기존 파일에 연결했습니다.

- **회피 = 구르기**: Space(`PlayerController` 구르기 시작) → `StartEvade()`, 구르기가 끝나면 `EndEvade()`(명세의 `OnEvadeFinished`). 예전의 구르기 무적 시간은 없앴습니다. 적의 회피 동작도 같습니다.
- **가드 = 우클릭**: `CombatManager`가 `StartGuard()`/`StopGuard()` 호출. 입력은 `InputAdapter` 한 곳에서만 받으므로(새/기존 Input System 공용) 명세의 `PlayerDefenseInput`(직접 `Input` 사용)은 만들지 않았습니다.
- **패링**: 기존 기능이라 남겨 두었습니다(`DefenseController.parryEnabled`, 끄면 순수 감소율만).
- **정면 가드**: 명세대로 기본은 방향과 상관없이 감소합니다. `guardRequiresFacing`를 켜면 예전처럼 정면만 막습니다.
- **`unblockable` → `canBeGuarded`**: 반대 의미로 바뀌었습니다. `clashEnabled` → `canBeClashed`는 이름만 바뀌어 기존 에셋 값이 유지됩니다. 메뉴 **스킬 데이터 에셋 만들기**를 다시 실행하면 모든 값이 새로 채워집니다.
- **광역 스킬**: 별빛 난무(반경 3.2), 그림자 내려찍기(반경 2), 새 테스트 스킬 **충격파 `SKILL_004`**(8 / +3 / 3코인, 쿨 5초, 반경 4).

## 파일

| 파일 | 역할 |
|---|---|
| `Scripts/Defense/DefenseRules.cs` | `EvadeState`(회피 코인 위력·연속 회피), `GuardMultiplier`, `Unique`(대상 중복 제거) — 순수 C# |
| `Scripts/Defense/DefenseController.cs` | 가드·회피 컴포넌트(플레이어·적 공용), 회피 이벤트 |
| `Scripts/Combat/SkillDefinition.cs` | (수정) `isAreaAttack`, `areaRadius`, `targetLayers`, `canBeClashed`, `canBeGuarded`, `canBeEvaded`, `Reach` |
| `Scripts/Combat/CombatResolver.cs` | (수정) 단일(한 명) / 광역(반경 안 전원) 대상 탐색, 회피 → 가드 → 피해 순서 |
| `Scripts/Character/CharacterStats.cs` | (수정) `NotifyEvaded()`(회피 성공 시 정신력 +1·에너지), 무적은 특수 용도로만 |
| `Scripts/Player/PlayerController.cs` · `Core/CombatManager.cs` · `Enemy/EnemyCombatController.cs` · `Enemy/EnemyDecisionSystem.cs` | (수정) 구르기·회피 동작 = 회피 코인, 가드 호출, AI 접근 거리 = `Reach` |
| `Scripts/UI/CombatHUD.cs` · `SkillPanel.cs` | (수정) "회피 코인 앞면 · 위력 11", "회피 성공 11 ≥ 10 / 실패 11 < 12", 광역·방어 불가·회피 불가 표시 |
| `Editor/MoominSetup.cs` | (수정) 플레이어·적 프리팹에 `DefenseController`, 충격파 에셋 생성 |

## Unity Inspector 설정

1. 메뉴 **Moomin ▸ 3. 프리팹 만들기**와 **스킬 데이터 에셋 만들기**를 다시 실행하세요. `DefenseController`가 붙고, `stats`·`coinSystem`은 비어 있어도 같은 오브젝트에서 자동으로 찾습니다.
2. **DefenseController**: `Guard Reduction` 0.5 · `Evade Base Power` 8 · `Evade Coin Power` 3 (명세 테스트 값), `Parry Enabled`, `Guard Requires Facing`
3. **스킬 에셋**: `Is Area Attack`, `Area Radius`, `Target Layers`(비우면 CombatResolver의 `Target Mask` = 플레이어는 Enemy, 적은 Player 레이어), `Can Be Clashed / Guarded / Evaded`
4. 충격파를 써 보려면 플레이어 `CombatManager`의 `Ultimate`(R) 칸에 `Assets/Data/Skills/SKILL_004.asset`을 넣으세요.
5. 애니메이션 이벤트(`OnCoinAttack` ×코인 수, `OnSkillFinished`)는 메뉴 1번이 민미 공격 클립에 이미 넣습니다. 적은 이벤트 없이 `hitTime` 타이머로 같은 함수를 부릅니다.

## 테스트 체크리스트

| 항목 | 확인 방법 | 상태 |
|---|---|---|
| 가드 피해 감소(100 → 50) | `tools/status_tests` | ✔ |
| 회피 코인 판정(11 vs 10 성공, 11 vs 12 실패) | `tools/status_tests` | ✔ |
| 회피 연속 성공(유지) | `tools/status_tests` | ✔ |
| 회피 실패 시 종료 | `tools/status_tests` | ✔ |
| 광역 공통 코인(같은 위력, 대상별 방어) | `tools/status_tests`(규칙) + Unity 플레이 | 규칙 ✔ / 플레이 미확인 |
| 중복 피해 방지(콜라이더 여러 개) | `tools/status_tests`(Unique) + Unity 플레이 | 규칙 ✔ / 플레이 미확인 |
| 3코인 연속 공격(판정·피해 3번) | Unity 플레이(Console·스킬 패널 개별 코인 결과) | 미확인 |

`cd tools && dotnet run --project status_tests`로 실행합니다(69개).
