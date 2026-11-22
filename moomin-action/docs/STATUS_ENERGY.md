# 상태이상 · 경직 게이지 · 에너지

플레이어와 적이 같은 시스템을 씁니다. 모든 발동은 **실제 전투 사건**에서 일어나고, 같은 공격(`AttackContext.id`)으로는 효과마다 한 번만 발동합니다.

| 효과 | 발동 시점 | 효과 | 소모 |
|---|---|---|---|
| 출혈 | 내가 공격 코인을 쓸 때 (`SkillExecutor.OnCoinAttack`) | 강도만큼 체력 손실 | 횟수 -1 |
| 화상 | 2초마다 (`burnInterval`) | 강도만큼 체력 손실 | 횟수 -1 |
| 침잠 | 내가 공격에 맞을 때 (`CombatResolver.ApplyHit`) | 강도만큼 정신력 손실 (정신력이 없으면 기본은 체력 손실) | 횟수 -1 |
| 진동 | 스스로는 발동하지 않음. `tremorBurstEnabled` 스킬에 맞을 때 폭발 | 경직 게이지 피해 = 강도×4 + 횟수×2 | 횟수 -1 |

- 중첩하면 강도와 횟수를 각각 더합니다(상한 99). 횟수가 0이 되면 효과가 사라집니다.
- **화상 타이밍**: `Update`는 시간만 쌓고, 간격을 넘는 순간에만 피해를 줍니다. 매 프레임 피해는 없습니다. 프레임이 길어도 밀린 발동을 모두 처리하므로 30/144fps나 불규칙한 프레임에서도 횟수가 같습니다(테스트로 확인).
- **명중 처리 순서**(한 공격·대상당 1회): ① 쌓여 있던 침잠 발동 → ② 이번 코인 결과(앞면/뒷면)에 맞는 상태이상 부여 → ③ 진동 폭발 → ④ 공격자 에너지 획득.
- **경직 게이지**(체력과 별개, `CharacterStats`): 가득 차면 `staggerDuration`초 동안 경직됩니다(이동·공격 불가, 받는 피해 ×`staggerVulnerable`). 경직이 걸리면 게이지는 0으로 돌아갑니다. 피해가 `staggerRecoverDelay`초 동안 없으면 서서히 회복됩니다.
- **에너지**(`EnergyManager`, 0~100): 명중 +3(+스킬 `energyGain`), 클래시 승리 +10, 회피 +5, 패링 +12, 경직 깨뜨림 +15. 스킬의 `energyCost`보다 적으면 사용할 수 없고, 적 AI도 그 스킬을 고르지 않습니다(`IsReady`).

## 파일

| 파일 | 역할 |
|---|---|
| `Scripts/StatusEffects/StatusEffectData.cs` | `StatusType`, `StatusAmount`(강도/횟수), 공통 기반 `StatusEffect`(중첩·소모·지속시간) — 순수 C# |
| `Scripts/StatusEffects/BleedEffect.cs` · `BurnEffect.cs` · `SinkingEffect.cs` · `TremorEffect.cs` | 효과별 발동 규칙 — 순수 C# |
| `Scripts/StatusEffects/StatusEffectManager.cs` | 캐릭터 한 명의 상태이상 컴포넌트: 부여·발동·화상 시간·이벤트(`Changed`, `Triggered`, `AnyTriggered`), `StatusInfo`(이름·색·규칙) |
| `Scripts/Character/StaggerGauge.cs` | 경직 게이지 수치 — 순수 C# |
| `Scripts/Character/CharacterStats.cs` | (수정) `hasSanity`, 경직 설정, `AddStagger()`, `IsStaggerBroken` |
| `Scripts/CombatResources/EnergyPool.cs` | 에너지 수치 — 순수 C# |
| `Scripts/CombatResources/EnergyManager.cs` | 에너지 컴포넌트: 획득량 설정, `Gain`/`CanSpend`/`Spend` |
| `Scripts/Combat/AttackContext.cs` | (수정) 공격 고유 번호 `id` — 중복 발동 방지 키 |
| `Scripts/Combat/SkillDefinition.cs` | (수정) 앞면/뒷면별 상태이상 부여량, `tremorBurstEnabled`, `energyCost`, `energyGain` |
| `Scripts/Combat/SkillExecutor.cs` | (수정) 에너지 검사·소비, 코인 사용 시 출혈 |
| `Scripts/Combat/CombatResolver.cs` | (수정) 명중 후 침잠 → 부여 → 진동 폭발 → 에너지, 패링 에너지, `TremorBurst` 이벤트 |
| `Scripts/Combat/ClashManager.cs` | (수정) 클래시 승리 에너지 |
| `Scripts/Combat/SkillLibrary.cs` | (수정) 기본 스킬의 상태이상·에너지 값 |
| `Scripts/UI/CombatHUD.cs` | (수정) 에너지 게이지, 내 상태이상·경직, 적 상태이상 아이콘(강도/횟수), 적 경직 게이지, 진동 폭발 연출 |
| `Scripts/UI/SkillPanel.cs` · `SkillPanelArt.cs` | (수정) 스킬 패널 상태이상 줄(앞/뒤), 에너지 비용, 침잠 아이콘 |
| `Editor/MoominSetup.cs` | (수정) 플레이어·적 프리팹에 `StatusEffectManager`, `EnergyManager` 추가 |
| `tools/status_tests/` | Unity 없이 핵심 로직 테스트 42개 (`cd tools && dotnet run --project status_tests`) |

`Scripts/Resources/` 대신 `CombatResources/`를 씁니다. Unity에서 `Resources`라는 이름의 폴더는 특수 폴더라 빌드에 전부 강제로 포함되기 때문입니다.

## Unity Inspector 설정

1. 메뉴 **Moomin ▸ 3. 프리팹 만들기**를 다시 실행하면 플레이어·ShadowBlob·SleepyBear 프리팹에 두 컴포넌트가 붙습니다. 직접 만든 캐릭터에는 `StatusEffectManager`와 `EnergyManager`를 추가하세요. `StatusEffectManager`가 없는 대상이 맞으면 자동으로 추가되지만, 미리 붙여 두는 쪽이 설정을 바꿀 수 있어 좋습니다.
2. **스킬 에셋**(`Assets/Data/Skills/*.asset`, 메뉴 "스킬 데이터 에셋 만들기"로 다시 생성):
   - `Status On Hit`: `Bleed On Heads/Tails` 등 효과마다 앞면·뒷면의 **Potency(강도) / Count(횟수)**
   - `Tremor Burst Enabled`: 적중 시 진동 폭발
   - `Energy`: `Energy Cost`(사용 시 소비), `Energy Gain`(명중마다 추가 획득)
3. **CharacterStats**: `Has Sanity`(끄면 침잠이 체력 손실), `Stagger Gauge`(`Max Stagger`, `Stagger Duration`, `Stagger Vulnerable`, 회복 지연/속도)
4. **StatusEffectManager**: 상한, `Burn Interval`, 진동 폭발 계수(`Tremor Stagger Per Potency/Count`), 효과별 지속시간(0이면 횟수가 다 될 때까지)
5. **EnergyManager**: `Max Energy`, `Start Energy`, 사건별 획득량

## 기본 스킬 값

| 스킬 | 앞면 | 뒷면 | 기타 |
|---|---|---|---|
| 베기 / 되베기 / 이어 베기 | 진동 1/1 · 침잠 1/1 · 진동 2/1 | — · — · 진동 1/1 | 명중 에너지 +4 |
| 연속 베기 | 출혈 2/1 | 출혈 1/1 | |
| 불꽃 찌르기 | 화상 3/2 | 화상 2/1 | 정신력 8 |
| 별빛 난무 (강화) | 침잠 2/1 | — | **에너지 60**, 진동 폭발, 기본 위력 14 |
| 솜주먹 연타 / 베개 내려찍기 / 졸음 돌진 | 진동 1/1 · 침잠 2/2 | — · 침잠 1/1 | 졸음 돌진 진동 폭발 |
| 내려찍기 / 돌진 / 난타 / 방어 부수기 | 진동 2/1 · 출혈 3/2 · 화상 2/1 | — · 출혈 1/1 · — | 방어 부수기: 에너지 40, 진동 폭발 |

## 아이콘

`tools/status_icons.py`가 화상·에너지·출혈·진동·침잠 아이콘을 그려 `unity/Assets/Resources/StatusIcons/*.png`(256px, 투명 배경)와 미리보기 `previews/status_icons.png`를 만듭니다. 모양은 모두 코드(SDF)로 그리므로 색이나 형태를 바꾸려면 스크립트를 고친 뒤 다시 실행하면 됩니다(`python3 tools/status_icons.py [Burn Energy ...]`, numpy와 Pillow 필요).

- 진동: 황금빛 주황, 좌우 대칭 공명 파동 + 세로 가시
- 침잠: 짙은 남색. 위는 겹겹이 짙어지는 응집된 구체, 아래는 액체처럼 흘러내리는 줄기와 떨어지는 방울
- 게임에서는 `SkillPanelArt.StatusIcon` / `EnergyIcon`이 이 그림을 불러 씁니다(`Resources.Load`). 그림이 없으면 예전 흰색 아이콘에 상태이상 색을 입혀 대신 씁니다.
- `Resources` 폴더에 둔 이유: UI를 코드로 만들기 때문에 Inspector 연결 없이 이름으로 불러오고, 빌드에도 포함시키기 위해서입니다. Unity 기본 임포트 설정(Default 텍스처) 그대로 쓰면 됩니다.
