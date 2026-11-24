# 최적화 · 디버깅 기록

Unity 에서 실제로 돌려 보지는 못했다. 아래는 정적 점검(컴파일 · `tools/unity_audit.py` · `status_tests`)과 코드 읽기로 찾아 고친 것이다.

## 한 번에 확인하기

```bash
bash tools/check_all.sh
```

① C# 컴파일(Unity 참조) → ② Unity 정적 점검(✘ 가 하나라도 있으면 실패) → ③ 순수 로직 테스트. 수정·업데이트 뒤엔 이것부터 돌린다.

## 성능(쓰레기 · 끊김 줄이기)

| 곳 | 예전 | 지금 |
|---|---|---|
| `Effects/SparkBurst` | 부를 때마다 GameObject + ParticleSystem + 재질을 새로 만들고 지움(타격·광선 빛 가루로 초당 수십 번) | 장면에 ParticleSystem 하나(`SparkPool`)를 두고 `Emit` 으로 낱개 입자(위치·방향·속도·크기·색) — 최대 4000개, 장면이 바뀌면 다시 만듦 |
| `Combat/Projectile` | 투사체마다 재질 새로 | 종류별 정적 재질(`gearMat`·`bubbleMat`·`discMat`…) + 고리 메시 캐시(`RingMesh`) |
| `Inventory/ItemPickup` | 구슬마다 재질 새로(지워지지 않고 쌓임) | 색마다 재질 하나(`MatFor`), 그림자 끔 |
| `UI/CombatHUD` · `UI/BossBar` | 매 프레임 대상의 `GetComponent` 여러 번 | 대상이 바뀔 때만 다시 찾음(`CacheTarget`) |
| `SkillExecutor.LevelOpponent` | 코인마다 `Physics.OverlapSphere`(배열 할당) | `OverlapSphereNonAlloc` + 정적 버퍼 32 |
| `World/PetSeal` 물어 오기 | 매 프레임 `FindObjectsOfType<ItemPickup>` | `ItemPickup.All` 목록(OnEnable/OnDisable) + 0.5초마다 |

남아 있는 프레임마다 생성: `SkillVfxPlayer` · `TelegraphIndicator` · `WhaleBeamFx` 는 연출마다 재질을 만들지만 색이 따로 사라져야 해서 그대로 두었다(OnDestroy 에서 지움 — 새는 것 없음).

## 고친 버그

- **펫이 아이템을 복제/삭제:** `Inventory.Add` 는 "못 넣고 남은 개수"를 돌려주는데 `PetSeal.TryFetch` 가 반대로 읽었다 → 다 넣어도 아이템이 남아 다시 물어 옴(무한 복제), 가방이 차면 아이템이 사라짐.
- **가방이 가득 찰 때 매 프레임 "가방이 가득 찼다":** 줍기 구슬이 붙어서 계속 Add → 이제 남은 수만 남기고 2초 뒤 다시 시도. 일부만 들어가면 나머지가 사라지던 것도 고침.
- **퀘스트 보상이 가방이 차면 사라짐:** 남은 보상은 발밑에 떨어뜨림(`Quests.TurnIn`).
- 저장 항목이 주석에 삼켜지던 것(`SaveData.sanity`)·Art 경로 오타는 `unity_audit` 규칙 4b·1b 로 계속 잡는다.

## 새 코드를 쓸 때 (나중에 수정·업데이트할 때)

- 자주 부르는 연출은 `SparkBurst.Spawn(pos, dir, 개수, 속도, 색)` 을 쓴다 — 새 ParticleSystem 을 만들지 말 것.
- 같은 모양의 재질은 정적 캐시에(`static Material x; if (x == null) x = new Material(...)`), 색을 따로 바꿔야 할 때만 인스턴스 + OnDestroy 에서 Destroy.
- `Update` 안에서 `Find*`·`GetComponent` 는 대상이 바뀔 때만. 물리 검색은 `*NonAlloc`.
- `Inventory.Add` 반환값 = 남은 개수(0 이면 다 들어감).
- Unity 객체에 `?.`·`??` 쓰지 말 것(파괴된 객체가 null 로 안 보임). static event 는 OnDisable/OnDestroy 에서 꼭 빼기.
