# 장비 · 공격력

## 피해 공식

```
최종 위력 P = 스킬 기본 위력 + (앞면 ? 코인 위력(힘·위력 감소·마비 반영) : 0) + 장비 보너스(코인 최종 위력)
피해     D = 공격력 × (P ÷ 10) × M(스킬 배율 × 무기 배율 × 정신력) × R(가드·상성·취약 · 장비 피해 감소)
공격력     = 기본 10 + 레벨 보너스(레벨당 0.5) + 장비 공격력
```

- **공격력과 위력의 역할:** 기본 공격력이 피해의 크기를 정한다. 그 위에서 코인 결과(앞면·뒷면, 힘, 마비, 장비 보너스)가 피해를 직접 움직인다.
  - 예: 공격력 15에서 최종 위력 10이면 15, 13이면 19.5 → 20.
- **이전 공식과의 관계:** 공격력이 10이면 예전 공식 `D = P × M × R`과 같다. 적은 모두 공격력 10이라 기존 밸런스가 그대로다.
- **합:** 최종 위력 P끼리 비교한다. 장비 보너스도 합에 들어간다.
- **관련 코드:**
  - 순수 로직: `Core/EquipmentMath.cs` (`Damage`, `FinalPower`, `CombatPower`, `Describe`). `tools/status_tests`로 검사한다.
  - 공격력 값: `CharacterStats.attack`(기본)·`levelAttack`(Experience)·`equipAttack`/`equipFinalPower`/`equipHealth`/`equipDamageReduction`(Inventory). 최종값은 `Attack` 속성.

## 장비 칸

| 칸 | 수 | 비고 |
|---|---|---|
| 무기 | 1 | 기본 스파크 검. 빼기 없음, 다른 검으로 바꿔 낌. 무기 배율(×) |
| 머리 · 가슴 · 다리 · 발 | 각 1 | 방어구·장신구 (`ItemType.Armor` + `slot`) |
| 마법 아티팩트 | 3 | `ItemType.Artifact`. 같은 아티팩트는 한 칸에만 낄 수 있다 |
| 장비 묶음 | 3 | 1·2·3 키나 버튼으로 바꾼다. 처음 쓰는 묶음에는 지금 장비가 복사된다 |

- **가방과의 관계:** 장비는 가방 칸에 그대로 있고 '장착' 표시만 붙는다. 장착 중인 장비는 버릴 수 없다.
- **저장:** 장비·묶음 상태는 저장 슬롯(`Inventory.State`)에 들어간다.
- **장비 체력:** 최대 체력 보너스는 저장 파일에서 빼고 저장한다. 불러올 때 다시 더해 두 번 더해지지 않는다.

## 새 장비 넣기

`Inventory/ItemDef.cs`의 `ItemDatabase`에 한 줄 추가한다.

```csharp
Add(new ItemDef { id = "headband_blue", name = "파란 머리띠", type = ItemType.Armor, slot = EquipSlot.Head, grade = "B", icon = IconKind.Headband,
                  stats = new EquipStats { finalPower = 1 }, model = "Equipment/Headband_Blue", attachBone = "head", description = "…" });
```

- **능력치(`stats`):** `finalPower`(코인 최종 위력), `attack`, `health`, `damageReduction`(0.1 = 10%, 여러 개면 곱으로 겹침).
- **등급(`grade`):** S·A·B·C. 아이콘 왼쪽 위에 색으로 표시한다.
- **아이콘:** `Resources/ItemIcons/<id>.png`가 있으면 그 그림을 쓴다. 없으면 `IconKind`로 코드가 그린다.
- **3D 모양(`model`):** Resources 경로. 민미 모델과 같은 공간(기본 자세)에서 만들어 내보낸다.
  - `EquipmentVisuals`가 스킨 메시의 **bindpose**로 `attachBone`에 붙인다. 그래서 지금 어떤 자세든 정확한 자리에 붙는다.
  - 예시: `blender/build_headband.py`

## 첫 장신구: 파란 머리띠

- **모양:** 사용자 사진(보송보송한 하늘색 곱창 고리)을 바탕으로, 민미 머리 둘레 이마 위에 맞췄다(앞이 높고 뒤가 낮음, 귀 아래를 지남).
- **효과:** 코인 최종 위력 +1. 공격마다 붙고, 앞면·뒷면 모두, 합에도 들어간다.
- **얻는 법:** 새 게임 가방에 1개 들어 있다. 장비 화면이나 가방에서 장착한다.
- **파일:**
  - `blender/build_headband.py` → `Resources/Equipment/Headband_Blue.fbx`, `Resources/ItemIcons/headband_blue.png`
  - 미리보기: `previews/headband_sheet.png`

## 장비 화면 (`UI/EquipmentMenu.cs`)

- **여는 법:** C 키 / 휴식 메뉴 "장비" / 가방에서 C.
- **배치:** 참고 화면의 구조를 우리 금박 테마로 옮겼다.
  - 왼쪽 세로 칸: 무기·머리·가슴·다리·발. 칸마다 아이콘, 등급 글자, 칸 이름.
  - 가운데: 실제 민미 3D 모습(`EquipmentPreview`). 멈춘 게임 속 플레이어를 플레이어 레이어만 비추는 카메라로 그려, 장비 모양까지 보인다. 끌어서 돌려 볼 수 있다. 아래에 이름, Lv, 전투력.
  - 그 오른쪽: 마법 아티팩트 3칸(마름모) + 장비 묶음 1·2·3.
  - 오른쪽: 능력치(공격력 내역, 코인 최종 위력, 무기 배율, 최대 체력, 받는 피해) → 고른 칸에 낄 수 있는 장비 → 자세한 설명.
- **조작:**

| 키 | 동작 |
|---|---|
| W/S | 칸 이동 |
| A/D | 왼쪽 칸 ↔ 아티팩트 ↔ 목록 |
| Enter | 장착 |
| X | 해제 |
| 1·2·3 | 장비 묶음 |
| C·Esc | 닫기 |
| 마우스 | 클릭 |

- **미리보기:** `tools/equipment_mock.py` → `previews/equipment_mock.png`. 좌표는 C# 상수와 같다.

## 함께 고친 오류: 주석이 코드를 삼킴

한 줄 안에 `코드;   // 설명 다음코드;` 형태가 있어서, 주석 뒤의 코드가 실행되지 않던 곳 4군데를 고쳤다.

| 위치 | 증상 |
|---|---|
| `UIFactory.AddTrigger` | 모든 메뉴의 마우스 올리기·클릭이 동작하지 않았다 |
| `SkillIllustration` 붓 획 | 스킬 일러스트의 빛 획이 그려지지 않았다 |
| `CombatHUD` | `ClashManager.Clashed` 구독이 빠져 있었다 → 합 가운데 연출이 뜨지 않았다 |
| `EnemyCombatController` | 적 회피 애니메이션이 재생되지 않았다 |

`tools/unity_audit.py`에 이 유형을 찾는 검사(4b)를 추가했다.
