# 실시간 코인 전투 — 명세 v2.0 구현 문서

> 원칙: **코인 판정(CoinSystem) · 공격 진행(SkillExecutor) · 피해 계산(CombatResolver) 은 서로 독립된 시스템.**
> 수치 검증·클래시 승률: [`balance.md`](balance.md) (`python3 tools/coin_sim.py`) · 컴파일 검사: `tools/compile_check.sh`

## 확정 사항(명세 v2)
| 항목 | 구현 |
|---|---|
| 코인 애니메이션 | **코인 하나 = 공격 한 번**. 3코인 스킬 = 공격 애니메이션 3번, 각 타격 순간 코인 판정 |
| 공격 위력 | 공격마다 `P = 기본 + (앞면 ? 코인 위력 : 0)` (예: 연속 베기 앞·뒤·앞 = 13·10·13, 합계 36). 피해 `D = P × M × R` |
| 클래시 | 양쪽 **히트박스가 동시에 열려 있고 서로를 겨눌 때만**, 지금 공격의 위력 비교. 이미 던진 코인을 다시 던지지 않음 |
| 적 AI | 상황 분석 → 행동 점수 → 최고 점수 행동(접근·공격·방어·회피), 0.4초마다 판단, NavMesh 이동 |

## 클래스 구조(명세 2-1)
| 클래스 | 파일 | 역할 |
|---|---|---|
| PlayerController | `Player/PlayerController.cs` | 이동·Space 회피(무적 0.05~0.40초)·타깃 고정 시 바라보기 |
| CombatManager | `Core/CombatManager.cs` | 플레이어 전투 입력(좌클릭 3연타·Q/E/R·우클릭 방어/패링), 상태(Idle~Dead), 피격 경직 |
| SkillExecutor | `Combat/SkillExecutor.cs` | 스킬 실행·공격 순서·쿨타임·정신력 조건, 애니메이션 이벤트 수신(플레이어·적 공용) |
| SkillDefinition | `Combat/SkillDefinition.cs` | ScriptableObject(Create ▸ Combat ▸ Skill Definition). 기본값: `SkillLibrary.cs`, 에셋: `Assets/Data/Skills/` |
| CoinSystem | `Coin/CoinSystem.cs` | 앞면·뒷면 판정만(`TossCoin`, `CalculatePower`), 순수 수식은 `CoinMath` |
| AttackContext | `Combat/AttackContext.cs` | 공격 하나의 결과(위력·앞면·명중·클래시·히트박스 시간·적중 목록) |
| CharacterStats | `Character/CharacterStats.cs` | 체력·정신력·방어/패링·회피 무적·취약(플레이어·적 공용) |
| CombatResolver | `Combat/CombatResolver.cs` | 히트박스가 열린 동안 대상 탐색 → 클래시 또는 피해(방어·패링·회피 반영, 대상별 1회) |
| ClashManager | `Combat/ClashManager.cs` | 위력 비교와 결과 적용. `mode`: StageCompare(명세, 기본) / LimbusRounds(이전 요청의 림버스식 다회 합) |
| EnemyBrain | `Enemy/EnemyBrain.cs` | 0.4초마다 상황 수집 → 결정 → 실행. 행동 중에는 판단하지 않음 |
| EnemyDecisionSystem | `Enemy/EnemyDecisionSystem.cs` | 행동 점수 계산(아래 표) + 쓸 스킬 선택, 성격 가중치(`EnemyPersonality`) |
| EnemyCombatController | `Enemy/EnemyCombatController.cs` | NavMeshAgent 이동, 공격(바닥 예고), 방어, 회피, 경직, 보스 설정 |
| CombatHUD / CoinDisplay | `UI/CombatHUD.cs`, `UI/CoinDisplay.cs` | 정신력·스킬 슬롯·클래시·조준 대상 / 공격별 코인·위력·합계 |

## 공격 한 번의 흐름(명세 5-1)
스킬 입력 → `SkillExecutor.UseSkill` → (windup) → 공격 애니메이션 → **`OnCoinAttack`**: 코인 판정 → 위력 → `AttackContext` → `CombatResolver.ResolveAttack` → 히트박스가 열린 동안 `Tick` → 명중/빗나감/클래시 → 다음 공격 → 회복 → `OnSkillFinished`

애니메이션 이벤트(명세 6-2)는 Unity 메뉴 **Moomin ▸ 1. FBX 임포트 설정**이 Attack1~3 클립의 타격 프레임에 자동으로 넣음: `OnCoinAttack` · `OpenHitbox` · `CloseHitbox` · `OnSkillFinished`.
이벤트가 오지 않으면(적, 설정 누락) 스킬 데이터의 `hitTime` 으로 진행 — 어느 쪽이든 코인은 공격당 한 번만 던짐.

## 클래시 결과(명세 7-3)
| 결과 | 이긴 쪽 | 진 쪽 |
|---|---|---|
| 승리/패배 | 공격 유지(그 공격이 그대로 명중), 정신력 +5 | 공격 취소·경직 1초(받는 피해 ×1.3), **남은 코인 소모 안 함**, 정신력 -5 |
| 무승부 | 양쪽 공격 종료, 짧은 경직 0.4초 | |
방어: 정면 피해 ×0.3, 방어 시작 0.2초 안이면 **패링**(피해 없음, 공격자 경직 1초·취약 ×1.5, 정신력 +5). `unblockable` 스킬(보스 방어 부수기)은 막을 수 없음.

## 적 AI 행동 점수(명세 8-3)
| 상황 | 공격 | 방어 | 회피 |
|---|---|---|---|
| 멀리 있음 | → 접근 100 | | |
| 공격 가능한 거리 | 80 | 20 | 10 |
| 플레이어 공격 중 | 20 (+50: 내 기대 위력이 더 크면 클래시를 노림) | 60 | 90 (거리 2.5 안) |
| 적 체력 20% 이하 | 20 | 90 | 70 |
| 보정 | 플레이어 체력 30% 이하 +25, 내 정신력 +15 이상 +15 / -20 이하 -20, 플레이어가 방어 중이면 방어 불가 기술 +40 | | |
점수 × 성격 가중치(공격성·신중함·회피성) + 무작위(일반 0~12, 보스 0~8). 일반 적과 보스는 같은 판단 규칙에 숫자만 다름.

## 정신력
명중 +1 · 클래시 승 +5 / 패 -5 · 강한 공격 피격(14 이상) -4 · 회피 성공 +1 · 방어 +1 · 패링 +5 · 웨이브 정리 후 0을 향해 초당 3 회복.
**스킬의 정신력 비용은 조건 겸 소모**(명세 5-2): E 는 정신력 8 이상일 때 쓸 수 있고 8 소모, R 은 15 이상 / 15 소모.
상태: 집중(+25~) 주는 피해 +10% · 안정 · 불안정(-25~-39) 전부 앞면 효과 봉인 · 붕괴(-40~) 받는 피해 +25%.

## 기본 스킬(SkillLibrary → Assets/Data/Skills)
| 키 | 스킬 | 기본 | 코인 위력 | 코인 수 | 배율 | 쿨 | 정신력 | 전부 앞면 |
|---|---|---|---|---|---|---|---|---|
| 좌클릭 | 베기 → 되베기 → 이어 베기 | 8 / 8 / 9 | +3 / +4 / +3 | 1 / 1 / 2 | 1.0 | - | - | - |
| Q | 연속 베기 | 10 | +3 | 3 | 1.0 | 5초 | - | - |
| E | 불꽃 찌르기 | 14 | +6 | 2 | 1.2 | 8초 | 8 | 적 크게 경직 |
| R | 별빛 난무(전방위) | 12 | +5 | 5 | 1.3 | 30초 | 15 | 정신력 +10 |
| 적 | 몸통 박치기 / 내려찍기 | 7 / 9 | +3 / +4 | 2 / 2 | 0.7 / 0.8 | 1.2 / 4초 | - | 예고 0.65 / 1.0초 |
| 보스 | 돌진 · 난타 · 방어 부수기(×1.35) | 13 · 9 · 18 | +4 · +3 · +6 | 2 · 4 · 1 | | | | 방어 부수기 = 방어 불가 |

## Unity 연결(명세 9장) — 메뉴 Moomin ▸ 한 번에 설정이 자동으로 해 줌
- **Player**: CharacterController, PlayerController, CharacterStats, CoinSystem, SkillExecutor(이벤트 사용), CombatResolver(대상 = Enemy 레이어), CombatManager(스킬 에셋 연결), Stamina, Inventory, LockOnSystem
- **Enemy**: CapsuleCollider, NavMeshAgent, CharacterStats, CoinSystem, SkillExecutor(타이머), CombatResolver(대상 = Player 레이어), EnemyBrain, EnemyCombatController
- 레이어 `Player`·`Enemy` 를 자동 추가. 팀 구분은 `CharacterStats.team` 으로도 하므로 레이어가 없어도 아군 피해 없음
- NavMesh: 추가 패키지 없이 실행 시 `NavMeshBaker` 가 바닥·도시 콜라이더로 구움(도시를 넣어도 그대로 동작). NavMesh 가 없으면 적은 직접 이동으로 대체

## 검증 상태
- ✔ C# 전체(에디터 스크립트 포함) **실제 컴파일 성공** — Unity 2021.3 참조 어셈블리 기준, `tools/compile_check.sh`
- ✔ 수식·예시값·클래시 확률 — `tools/coin_sim.py`
- ✘ Unity 에디터 실행·플레이 테스트 전(이 환경에 Unity 없음). 새 Input System 분기와 Unity 2023+ 전용 분기는 컴파일 검사에서 제외됨
