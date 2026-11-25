# 맨두 — 왕실근위대 엘리트 하프물범 (성문 지기)

## 모델 — `blender/build_mandu.py`
- 실행: `/home/user/bpyenv/bin/python blender/build_mandu.py [--preview] [--portrait]`
- 출력: `exports/Mandu.fbx` → `unity/Assets/Models/Mandu.fbx`, 초상화 `Resources/UI/Portraits/mandu_normal.png`, 미리보기 `previews/mandu_*.png`
- 통통한 하얀 몸(뒤로 갈수록 가늘어짐) · 까만 눈/눈썹 · 세모 코 · ω 입 · 수염 · 봉제선 · 앞지느러미 · 꼬리
- 근위대 복장: 빨간 모자(금띠 · 챙 · 금별 휘장 · 흰 깃털) · 파란 띠 · 훈장
- 뼈: root · body · head · tail · eye.L/R · flip.L/R
- 클립: Idle · Walk · Run · Talk · Yes · No · Salute · Laugh · Windup · Attack1(배치기, 11프레임 타격) · Attack2(찰싹, 11) · Attack3(굴러가기, 14) · Guard · Dodge · Hit · Death

## Unity
- 프리팹 `Mandu`(Moomin 메뉴 → BuildMonster, `minWave 999` 라 웨이브·토벌엔 안 나옴, `GameManager.enemyPrefabs` 에 들어 있음)
- 기술(`SkillLibrary`): `MANDU_BUMP` 근위대 배치기 · `MANDU_SLAP` 지느러미 찰싹 연타(×3) · `MANDU_ROLL` 근위대 굴러가기 — 일러스트 주제 Seal
- 보스바 문장 `BossEmblem_Mandu`(`tools/boss_bar_art.py`), 별칭 "왕실근위대 엘리트"
- `World/CastleGuard.cs`
  - `CastleGate.Ensure(gm)` — GameManager.Start 에서 NavMesh 구운 뒤. 성(MapPlaces "castle")에서 광장 쪽으로 19m, 성 둘레 단(약 8.5m) 위에 섬
  - `ManduGuard` 근무 모드: 팀 Player(공격 안 받음) · 제자리 지킴 · 가까이 오면 바라보고 경례 · F 대화
  - 전투 모드("억지로 들어간다"): 팀 Enemy · 보스바 · aggro 18 · 성문에서 25m 끈(leash) · 플레이어가 32m 넘게 멀어지면 근무로 복귀
  - 쓰러지면 "졌다맨두…" — 그래도 통과 아님, 60초 뒤 다시 근무
- `Story/ManduStory.cs` — 말끝마다 "~맨두". 메뉴: 꿀바른 땅콩빵 바치기(가지고 있을 때) · 들여보내 줘(조리법 설명) · 억지로 들어간다 · 그냥 갈게
  - 빵을 바치면 `Quests.Log` "castle_gate" 완료(저장됨) → 맨두가 옆으로 비켜서 경례
- 아이템: `peanut_bread` 땅콩빵 · `honey` 꿀 · `honey_peanut_bread` 꿀바른 땅콩빵(열쇠 아이템)
- `Dialogue.playAnim` — Npc 가 아닌 말하는 이의 몸짓 훅. 초상화는 표정 그림이 없으면 `_normal` 로 대체

## 다음 단계
- 땅콩빵 조각 9개 → 땅콩빵 조합, 땅콩빵 + 꿀 → 꿀바른 땅콩빵 조합
- 꿀을 주는 퀘스트
- 성 안(내부) — 지금은 들어갈 곳이 없음
