# 하프프 왕국 · 하프물범 여왕의 성(구래성) — 성 안

- 왕국 이름: **하프프** · 성 이름: **구래성**(둘 다 사용자 지정). 여왕 이름 하프리나는 Claude 가 지음

## 설계(사용자와 정함)
- 성의 주인: **하프물범 여왕** · 역할: 이야기·NPC 거점 + 보물·퍼즐 + 최종 보스 자리 · 분위기: **웅장한 왕궁풍**(사용자 참고 그림: 고딕 왕궁 홀) · 규모: 여러 층 + 지하
- 공통 양식: 흰 대리석 + 금줄 상감 · 붉은 카펫 · 마름모 격자 기둥 · 고딕 늑골 천장 · 금 샹들리에 · 스테인드글라스 + **하프물범 얼굴 메달·장식·조각상이 곳곳에**
- 층 계획: 지하(얼음 보물고·감옥) · 1층(대현관·대연회장) · 2층(**알현실**·도서관) · 3층(여왕의 침실) · 꼭대기(별빛 탑 전망대 = 최종 보스 자리)
- **다른 방을 만들 때는 먼저 사용자에게 물어볼 것**(참고 그림을 받을 수 있음)

## 지금 있는 방: 알현실 — `blender/build_castle_hall.py` → `Resources/World/CastleHall.fbx`
- 크기 60m × 27m, 천장 꼭대기 23m(가운데) · 18m(양옆 통로). 맵 좌표 그대로, **성 위 하늘 z = 260m**(바깥과 안 만나는 닫힌 실내)
- 바닥: 가운데 흰 대리석 + 금 상감(가장자리 줄 · 8m 칸 줄 · 카펫 양옆 마름모) · 양옆 통로 육각 타일
- 붉은 카펫(금 테두리): 정문 → 왕좌 계단(6단 × 0.29m — 캐릭터가 오를 수 있는 높이) → 왕좌 앞
- 기둥 10개(크림색 + 금빛 나선 띠 12줄이 엇갈려 마름모 격자) · 기둥마다 **물범 얼굴 메달** + 3초 촛대
- 천장: 뾰족 아치 볼트 + 가로 늑골 · 대각 늑골 · 금 꼭지 · 용마루 늑골
- 샹들리에 3개(촛불 2단 14+9) · 카펫 양옆 촛대 · 받침 위 화분
- 창: 양옆 뾰족 아치 스테인드글라스 10폭(파랑 + 보라·빨강·금·흰 조각, 금 테두리) · 끝 벽 3폭 + **물범 얼굴 장미창**
- 조각상: 정문 쪽 하프물범 한 쌍(진주를 안고 작은 왕관) · 왕좌 양옆 큰 한 쌍(1.55배)
- **거대한 왕좌**(높이 약 15.5m): 금 받침·틀 · 남색 벨벳 등받이 · 붉은 방석 · 물범 머리 팔걸이 · 양옆 진주 기둥 · 꼭대기 물범 얼굴 문장 + 진주 왕관 · 양옆 물범 문장 깃발
- **맨두 문장 · 곰곰이 문장**(사용자 요청 — 하나씩): 왕좌 계단 앞(x 43m) 양옆 벽의 큰 깃발. 왼쪽 남색 깃발 = 맨두(물범 얼굴 + 빨간 근위대 모자 + 엇갈린 파란 방망이) · 오른쪽 붉은 깃발 = 곰곰이(졸린 곰 얼굴 + 사골곰뼈)
- 정문(안쪽 나무 문 + 물범 문고리) · 바깥 문 `CastleDoor`(맵 좌표 — 성 팔각 성벽 바깥면, 성 중심에서 11.75m, 물범 문장)
- 충돌 `CastleHall_Collision`(바닥·벽·천장·기둥·계단·왕좌·조각상) · 표시 Mark_Arrive/ArriveLook/ExitDoor/Throne/ThroneSeat/HallCenter/Chandelier_1..3/Window/DoorFront/DoorLook
- 미리보기 `--preview` → `previews/castle_hall_entrance/throne/aisle/emblems.png` · 사용자 참고 그림 `docs/refs/seal_castle_throne_ref.jpg`

## 런타임 — `World/CastleInterior.cs`
- `GameManager.Start` 가 NavMesh 전에 `Ensure()`: 충돌 · Glow 재질 · 점광원(샹들리에 3 — 촛불 일렁임 · 창 · 왕좌) · 실내 NavMesh
- 문 두 개는 `SeaGate` 를 그대로 씀(화면 어둡게 → 건너감). SeaGate 에 `arriveText · canUse · blockedText · onArrive · signHeight` 추가
- 바깥 문은 **맨두를 통과해야**(`CastleGate.Passed` = 꿀바른 땅콩빵) 열림
- 성 안에서는 웨이브가 멈춤 · 장소 이름 "하프물범 여왕의 성 · 알현실"

## 하프물범 여왕 하프리나
- 모델: 조랭하프(`Joraeng.fbx`) × **1.8배** + 장신구 3개 — `blender/build_joraeng.py --queen [--preview]` → `Resources/Npc/QueenCrown.fbx`(head) · `QueenCape.fbx`(body) · `QueenScepter.fbx`(flip.R)
  - 왕관: 금 띠 · 뾰족 장식 8(진주) · 앞 큰 사파이어 · 띠 위 사파이어/루비 · 빨간 벨벳 모자 · 금 방울
  - 망토 무늬: **여왕이 좋아하는 무늬 = 사용자 사진(장미빛 바탕 + 흰 다마스크·격자·꽃)** → `tools/queen_pattern.py`(밝기 평탄화 + 두 색) → `Resources/Npc/QueenPattern.png` · Blender 는 Queen_Velvet 에 이미지 텍스처, Unity 는 SpawnQueen 이 Queen_Velvet 재질에 넣음 · 원본 `docs/refs/queen_pattern_photo.jpg`
  - 망토: 어깨(0.545m)에서 바닥까지 몸 뒤를 감싸며 퍼지고 뒤로 끌림 · 진홍 벨벳 + 금빛 안감 · 흰 담비털 깃·앞자락·단(검은 점) · 단 근처 금 수 두 줄 · 등 금 문장 + 사파이어 · 앞 금 걸쇠 + 루비
  - 보석 지팡이: 금 막대 + 고리 · 세 방향 금 테두리 안 큰 사파이어(Glow) · 진주 6 · 꼭대기 금별
- 초상화(다른 캐릭터보다 훨씬 고풍스럽게): `build_joraeng.py --queen` 원본 → **`tools/queen_portrait.py`** → `Resources/UI/Portraits/queen_<표정>.png`
  - 셀 채색(portrait_art.cel) + 금박 타원 메달: 남색 벨벳 바탕 · 머리 뒤 금빛 후광 · 빛줄기 · 금 이중 테 + 진주 구슬 · 월계수 가지 · 꼭대기 왕관 문장(사파이어) · 아래 진홍 리본 · 반짝이
  - 인물은 메달 안 흉상(0.8배), 왕관·지팡이 머리만 테 앞으로 튀어나옴 · 둘레 금빛 번짐 · 왼쪽 위 금빛 테두리 빛 · 그늘 밝게 · 캔버스 결. 모음 `previews/queen_portraits_sheet.png`
  - 놀람·곤란 표정은 지팡이가 얼굴을 가리지 않는 자세로(Surprised 3 · Sad 14)
- 런타임: `CastleInterior.SpawnQueen`(GameManager.npcPrefab + `Npc` — story `QueenStory.Build` · questId 여왕의 등불 · 목도리 끔 · 움직이지 않음 · 대화 거리 4.2m) — 왕좌 계단 위 `Mark_Throne`
- 대화 `Story/QueenStory.cs`(말투: 짐 · ~니라 · ~구나)
  - 처음: 여왕 소개 → **민미 소개(선택지 "민미라고 해요! … 민미")** → 왕국 소개(하프 왕국 · 구래성 · 맨두 · 조랭하프 마을) → 등불이 꺼져 감 → 퀘스트 제시(수락/나중에). 만남 기록 `Quests.QueenMet`
  - 그 뒤 선택지: ① 이 왕국에 대해 자세히 소개해줘 민미(건국 전설 · 고래족 용궁 · 끼룩 전쟁 · 보물고와 지하감옥 복선) ② 장비에 축복을 부탁해 민미(**퀘스트 전엔 "(잠김)"** → 거절 대사) ③ 끼루룩 끼룩끼룩 민미민미 → "-? 뭐야 하프 너 고래족이었나." → "-어쩔 수 없군." → "-넌 지하감옥으로 가라" → 대화가 닫히면 지하감옥 · (곰기름을 다 모았으면) 바치기 · 물러나기
- 퀘스트 `Quests.QueenLamps` 「여왕의 등불」: 곰기름(`bear_oil`, 곰곰이 드롭) 2병 → 경험치 1800 · 금화 800 · 붉은 약병 3 · 별조각 4 + **축복 열림**
- 여왕의 축복 `Inventory.BlessEquipped()`: 지금 낀 장비 전부(장비마다 한 번, 무료) — 무기 공격 ×1.06 · 최종 위력 +1 / 방어구·아티팩트 받는 피해 -2% · 최대 체력 +10. 저장 `Inventory.State.blessed`

## 지하감옥 + 광산 노역장 — `blender/build_castle_dungeon.py` → `Resources/World/CastleDungeon.fbx`
- 사용자 참고 그림 2장(`docs/refs/dungeon_prison_ref.png` · `dungeon_mine_ref.png`): **감방 철창 밖 = 광산 노역장**
- 성 위 하늘 z 230m 닫힌 실내 46m × 30m. 감옥 동(뒤쪽): 아치 감방 2층(6+6) · 위층 발코니 + 난간 + 돌계단 · 왕관 문장 깃발 · 횃불 · 쇠사슬
- 노역장 동굴: 울퉁불퉁 바위벽·천장 · 흙바닥 · 레일 + 광차 2 · 도르래 탑 + 쇠사슬 양동이 · 나무 비계(5m 통로 · 계단) · 기둥 등불 8 · 광석 더미 · 상자 · 곡괭이 문장 깃발
- 가운데 쇠고리 샹들리에 · 천장 둥근 창살 채광창(달빛) · 바닥 배수 창살 · 끝 바위 아치 + 내리닫이 쇠창살(DungeonExitGate) · 간수 초소 · 문 밖 계단
- 캘 바위 `MineRock_Template` → Unity 가 `Mark_Rock_1..16` 자리마다 복제(`World/MineRock.cs`)
- **노을빛**: 노역장 위 천장에 긴 쇠창살 틈(26m × 9m, 그 자리 천장 바위는 비움) + 뒤에 주황 노을 발광판(Dun_SunsetGlow) · Unity 는 주황 스포트라이트 3(Mark_Sunset_*) + 비스듬한 반투명 빛줄기 6장(카메라 쪽으로 돎)
- 미리보기 `--preview` → `previews/dungeon_prison.png` · `dungeon_mine.png` · `dungeon_cell.png` · `dungeon_sunset.png`

### 돌고래 죄수(억울하게 잡혀 온 돌고래들) — 20마리
- 모델: 고래고래 모델 그대로 × 0.4(길이 1.3m, 프리팹 Resources/Npc/DolphinPrisoner — MoominSetup, GameManager.dolphinPrefab) + `blender/build_dolphin_prisoner.py` 소품(곡괭이 flipper.R · 흑백 죄수 띠 + 번호표 body) + 초상화 dolphin_normal
- `World/DolphinPrisoner.cs` — 헤엄치지 않고 **짧게 통통통**(포물선 0.32초 · 위로 늘어남 · 착지 찌부): 곡괭이질 7(바위 앞 Attack1 + 돌가루) · 분주히 돌아다님 6 · 감방에 박혀 있음 7(Mark_Cell_*, 제자리 콩콩)
- F 대화 `Story/DolphinStory.cs`(말끝 "끼룩"): 억울파 5 · 광석파 5("광석 얻을 거야 끼룩!!!") · 감방파 3 무작위. 이름 "돌고래 죄수 101번…"
- 미리보기 `blender/render_mine_dolphins.py` → `previews/mine_dolphins_yard/close/cells.png`

### 노역 흐름(`CastleInterior` · `World/MineRock.cs` · `Story/JailerStory.cs` · 규칙 `Crafting/MineMath.cs` 순수 C#)
1. 여왕에게 "끼루룩 끼룩끼룩 민미민미" → 감방(2번째 칸) 안으로 → 3.5초 뒤 간수가 감방 문을 열어 줌
2. 간수 하프(조랭하프 + 쇠 투구 + 가죽 띠·열쇠 꾸러미 — `build_joraeng.py --jailer`, 초상화 jailer_*)에게 말 걸면 **노역용 곡괭이**(`prison_pickaxe`)
3. 바위 앞 F 곡괭이질 3번 → 부서짐(25초 뒤 다시 솟음): 돌덩이(`stone_rubble`) 1~2(30% 2개) + 광석 하나 확률
   - **바위마다 반드시 하나**(사용자 지정): 구리 광석 C 56% · 철 광석 B 20% · 은 광석 A 16% · **꿀** S 6%(금 대신 — 맨두의 꿀바른 땅콩빵 재료 `honey`) · **돌고래스톤** SS 2%(사파이어 대신, 희귀 재료 `dolphin_stone`) — S·SS 는 큰 알림
4. 돌덩이 **12개**(할당량) → 간수에게 "할당량 채웠어요 민미!" → 돌덩이 12·곡괭이 회수, 광석은 가짐 → 출구 쇠창살이 올라감 → 계단(F)으로 알현실
- 노역 중엔 출구가 막힘(`CastleInterior.Imprisoned`). 장소 이름 "하프물범 여왕의 성 · 지하감옥", 웨이브 멈춤
- 아이템 아이콘 7개(돌덩이·광석 5·곡괭이) = `render_item_icons.py`

## 다음
- 대현관 · 연회장 · 도서관 · 침실 · 지하 · 전망대(각각 만들기 전에 사용자에게 확인)
