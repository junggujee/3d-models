# 맨두 — 왕실근위대 엘리트 하프물범 (성문 지기)

## 모델 — `blender/build_mandu.py`
- 실행: `/home/user/bpyenv/bin/python blender/build_mandu.py [--preview] [--portrait]`
- 출력: `exports/Mandu.fbx` → `unity/Assets/Models/Mandu.fbx`, 초상화 `Resources/UI/Portraits/mandu_normal.png`, 미리보기 `previews/mandu_*.png`
- 통통한 하얀 몸(뒤로 갈수록 가늘어짐) · 작고 까만 눈 · 눈 바로 위 갈색 점 눈썹 · 세모 코 · 짧게 웃는 ω 입 · 수염 · 봉제선 · 앞지느러미 · 꼬리
- 크기 `SCALE = 1.26`: 길이 2.2m · 등높이 1.07m · 모자 끝 1.58m — 민미(키 1.3m) 의 약 1.7배, 고래고래보다는 훨씬 작음
- 무기 **파란 야구방망이**(파란 배럴 · 검은 홈 손잡이 · 은색 띠 · 파란 끝마개). 뼈 `bat`(body 의 자식)
  - 채널 `bat` 0 = 옆구리에 참(평소) / 1 = 손에 듦(전투 동작). `batp` 내려치기 · `batr` 좌우 스윙 · `batfall` 놓쳐 바닥에 떨어뜨림(패배)
- 근위대 복장: 빨간 모자(금띠 · 챙 · 금별 휘장 · 흰 깃털) · 파란 띠 · 훈장
- 뼈: root · body · head · tail · eye.L/R · flip.L/R
- 클립: Idle · Walk · Run · Talk · Yes · No · Salute · Laugh · Guard · Dodge · Hit
  - Windup(예고) · Attack1 배치기(타격 11프레임) · Attack2 · Attack3
  - **Bat1 · Bat2**(방망이 좌우 후려치기, 11) · **Bat3**(내려찍기, 13) · **HomeRun**(풀스윙, 23)
  - **Charge**(돌진, 14) · **Chomp**(거대화해 감싸 물었다 뱉기, 20, 60프레임)
  - **TonCharge**(필살기 예고 · 웅크림) · **MillionTon**(150프레임: 다섯 배 높이로 점프 → 낙하 62프레임 타격 → 납작한 채로 약 2초 → 복귀)
  - **Death = 패배 모션**(90프레임): 비틀 → 방망이를 놓쳐 바닥에 떨어뜨림 → 앞으로 폭삭 → 납작하게 뻗고 모자가 얼굴 앞으로 굴러 떨어짐, 꼬리만 두 번 파닥

## Unity
- 프리팹 `Mandu`(Moomin 메뉴 → BuildMonster, `minWave 999` 라 웨이브·토벌엔 안 나옴, `GameManager.enemyPrefabs` 에 들어 있음)
- 기술(`SkillLibrary`): `MANDU_BUMP` 근위대 배치기 · `MANDU_SLAP` 지느러미 찰싹 연타(×3) · `MANDU_ROLL` 근위대 굴러가기 — 일러스트 주제 Seal
- 보스바 문장 `BossEmblem_Mandu`(`tools/boss_bar_art.py`), 별칭 "왕실근위대 엘리트"
- `World/CastleGuard.cs`
  - `CastleGate.Ensure(gm)` — GameManager.Start 에서 NavMesh 구운 뒤. 성(MapPlaces "castle")에서 광장 쪽으로 19m, 성 둘레 단(약 8.5m) 위에 섬
  - `ManduGuard` 근무 모드: 팀 Player(공격 안 받음) · 제자리 지킴 · 가까이 오면 바라보고 경례 · F 대화
  - 전투 모드("억지로 들어간다"): 팀 Enemy · 보스바 · aggro 18 · 성문에서 25m 끈(leash) · 플레이어가 32m 넘게 멀어지면 근무로 복귀
  - 쓰러지면 "졌다맨두…" — 그래도 통과 아님, 60초 뒤 다시 근무
  - **드롭: 레이저 블레이더 제작 재료** `mandu_core` 맨두의 진동 코어 ×1 · `bat_alloy` 파란 방망이 조각 ×3~5
- `Story/ManduStory.cs` — 말끝마다 "~맨두". 메뉴: 꿀바른 땅콩빵 바치기(가지고 있을 때) · 들여보내 줘(조리법 설명) · 억지로 들어간다 · 그냥 갈게
  - 빵을 바치면 `Quests.Log` "castle_gate" 완료(저장됨) → 맨두가 옆으로 비켜서 경례
- 아이템: `peanut_bread` 땅콩빵 · `honey` 꿀 · `honey_peanut_bread` 꿀바른 땅콩빵(열쇠 아이템)
- `Dialogue.playAnim` — Npc 가 아닌 말하는 이의 몸짓 훅. 초상화는 표정 그림이 없으면 `_normal` 로 대체

## 다음 단계
- 땅콩빵 조각 9개 → 땅콩빵 조합, 땅콩빵 + 꿀 → 꿀바른 땅콩빵 조합
- 꿀을 주는 퀘스트
- 성 안(내부) — 지금은 들어갈 곳이 없음
- 재료로 레이저 블레이더를 **조합**하는 제작 창(지금은 재료만 떨어짐)
