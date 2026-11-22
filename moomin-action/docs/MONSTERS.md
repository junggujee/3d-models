# 일반 몬스터

| | 핑크큐브말랑이 | 그린볼런 |
|---|---|---|
| 모델 | `blender/build_pink_cube.py` → `Models/PinkCube.fbx` | `blender/build_green_block.py` → `Models/GreenBlock.fbx` |
| 모습 | 혹 3×3×3 이 뭉친 정육면체, 오돌도돌한 진분홍 젤리, 흰 눈알 + 연분홍 고리 입술 | 단단하고 매끈한 초록 판 6개(가운데 구멍 · 십자 팔 · 계단 모서리), 옆에 톱니 |
| 크기 | 한 변 0.6m(민미 배까지) | 한 변 0.66m |
| 체력 · 경직 | 40 · 70 | 85 · 140 |
| 속도 | 2.8 | 1.5 |
| 속성 | 빨강 | 초록 |
| 등장 | 1웨이브부터(수 제한 없음) | 2웨이브부터, 웨이브당 2마리까지 |
| 경험치 · 점수 | 35 · 10 | 60 · 18 |
| 기술 | 말랑 박치기, 쭈욱 늘이기, 통통 연속 박치기 | 톱니 던지기(원거리), 블럭 박치기 |
| 쓰러짐 | 위로 쭉 늘어났다가 웅덩이처럼 녹아 퍼짐 | 판 6개와 톱니가 사방으로 튀어 나가 땅에 납작하게 떨어짐 |

## 기술

| ID | 이름 | 코인 | 사거리 | 특징 |
|---|---|---|---|---|
| CUBE_BOUNCE | 말랑 박치기 | 6+3 ×1 | 1.5m | 튀어 올라 날아가 철퍽(공중 4~11프레임 돌진) |
| CUBE_STRETCH | 쭈욱 늘이기 | 9+4 ×1 | 2.4m | 몸을 길게 늘여 찌름. 맞으면 위력 감소 1 |
| CUBE_TRIPLE | 통통 연속 박치기 | 5+2 ×3 | 1.6m | 세 번 연달아. 진동 1 |
| BLOCK_RAM | 블럭 박치기 | 9+4 ×1 | 1.7m | 기울며 쿵. 진동 2(뒷면 1) |
| BLOCK_GEAR | 톱니 던지기 | 7+3 ×2 | 9m | **원거리 투사체**. 출혈 1/2(뒷면 1/1) |

## 원거리 투사체 (`Combat/Projectile.cs`)

- `SkillDefinition.isProjectile`: 코인이 판정되는 순간, 그 AttackContext(위력·앞뒷면)를 실은 투사체를 날린다.
  - 관련 값: `projectileSpeed`, `projectileRadius`, `projectileHoming`(초당 꺾는 각도), `projectileKind`
- **맞힘:** 상대 팀 캐릭터에 닿으면 쏜 쪽 `CombatResolver.ApplyHit`으로 처리한다. 근접 공격과 같은 순서다: 회피 코인 → 가드/패링 → 피해 → 상태이상.
- **합 규칙:**
  - 근접 공격과는 합하지 않는다(일방 공격). 판정은 `SkillDefinition.MeleeClashable`.
  - 다른 팀 투사체와 공중에서 만나면 원거리끼리 합한다. 위력에 상성 보너스(+2)를 더해 비교한다. 이긴 쪽은 계속 날아가고, 진 쪽은 부서진다. 같으면 둘 다 부서진다.
- **사라지는 조건:** 벽이나 땅에 닿을 때, 사거리를 넘었을 때.
- **예고:** 초록 직선. 날아오는 길을 보여 준다.
- **패널 표시:** 스킬 패널과 적 의도 표시에 "원거리"가 붙는다.
- **모양:** 톱니는 코드로 만든 메시다(이 8개 + 구멍). 바퀴처럼 세로로 돌며 날아가고, 앞면이면 금빛 점광원과 꼬리를 남긴다.

## 일러스트

- `IllustrationSubject.Slime` / `Block`: `SkillIllustration.CubeScene`·`CubeFigure`가 그린다.
- 같은 모양을 그리는 Python 파일은 `tools/illustration_preview.py`의 `cube()`·`gear()`다. 미리보기는 `previews/monster_skill_illustrations.png`.
- 동작:
  - Rush: 옆으로 눌림
  - Thrust: 길게 늘어남
  - Flurry: 튄 궤적
  - Smash: 기울며 충격
  - Throw: 날아가는 톱니(새 SkillMotif)

## 미리보기

- `previews/pinkcube_sheet.png`: 정면, 3/4, 통통 튐, 쭈욱 늘이기, 맞아서 늘어남
- `previews/greenblock_sheet.png`: 기본, 톱니 던지기 준비, 박치기, 흩어지는 중, 흩어진 뒤
