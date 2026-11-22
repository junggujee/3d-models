# 타이틀 · 저장/불러오기 · 게임 오버

미리보기: `previews/title_mock.png`, `previews/save_slots_mock.png`, `previews/game_over_mock.png` (`tools/title_mock.py`가 C#와 같은 좌표로 그림)

## 장면 흐름

```
Title.unity ──새 게임──────────────▶ Main.unity (레벨 1, 처음 위치, 처음 가방)
     │      ──이어하기 / 불러오기──▶ Main.unity (저장 적용)
     ▲                                  │
     └──── 휴식 메뉴 "타이틀로" ◀────────┤
     └──── 게임 오버 "타이틀로" ◀────────┘  게임 오버 "마지막 저장에서 다시" → Main.unity (가장 최근 저장)
```

- 장면을 바꿀 때마다 `SceneFader`가 검게 어두워졌다가 밝아진다.
- `Main.unity`를 직접 열고 Play하면 새 게임으로 시작한다.
- 메뉴 **Moomin ▸ 5. 타이틀 씬 만들기**(한 번에 설정에도 포함)가 `Title.unity`를 만들고 빌드 설정을 Title → Main 순서로 맞춘다.

## 타이틀 (`Title/TitleScreen.cs`, `Title/TitleCamera.cs`)

- **배경:** 실제 맵이다.
  - 카메라가 성과 산지 사이를 중심으로, 마을 광장 쪽에서 좌우 ±22°를 110초에 한 번 천천히 오간다.
  - 렌즈 이동으로 성을 화면 오른쪽에 둔다.
  - 엷은 푸른 안개를 깐다(지수², 0.0012).
- **왼쪽:** 남색 그늘 위에 차례로 놓는다.
  - 문장(가죽 코인 고리 + 검)
  - 제목 **민미의 모험**(나눔명조 ExtraBold, 상아색 + 짙은 파랑 외곽선)
  - 영문 부제
  - 구분선
  - 메뉴
- **메뉴:**
  - 이어하기: 가장 최근 저장을 연다. 오른쪽에 "Lv · 장소 · 웨이브 · 몇 분 전"을 표시한다.
  - 새 게임
  - 불러오기
  - 설정
  - 종료
  - 저장이 없으면 이어하기와 불러오기가 흐려지고 선택되지 않는다.
- **등장:** 검은 화면에서 밝아진 뒤 문장 → 제목 → 메뉴 순서로 떠오른다.
- **색:** 파랑·갈색·하양. `UITheme.TitleStyle`(Navy, Blue, BlueBright, Brown, BrownLight, Ivory, Mist).
- **그림:** `tools/title_art.py` → `Resources/UI/Title/`(Crest, DividerLeft, Divider, Marker, MenuGlow, SlotFrame).
- **글꼴:** `Resources/Fonts/NanumMyeongjo*.ttf`(OFL, 라이선스 파일이 같은 폴더에 있음). 어느 PC에서나 같은 모양으로 보인다.

## 저장 (`Core/SaveSystem.cs`, `Core/SaveFormat.cs`)

| 슬롯 | 언제 |
|---|---|
| 자동 저장 | 웨이브를 끝낼 때마다(`GameManager.autoSave`) |
| 기록 1~3 | 휴식 메뉴 ▸ 기록 남기기. 웨이브 사이(적이 없을 때)에만 저장할 수 있다 |

- **파일 위치:** `Application.persistentDataPath/saves/`
  - `slot1~3.json` 또는 `auto.json`
  - 같은 이름의 `.jpg`: 384×216 화면 사진. 메인 카메라를 한 번 더 그린 것이라 메뉴 UI가 찍히지 않는다.
- **저장 내용:**
  - 레벨, 경험치, 최대·현재 체력, 정신력
  - 위치, 방향
  - 웨이브, 점수
  - 여정 기록(처치 수, 보스 처치, 합 승패, 최고 위력)
  - 가방(금화, 칸, 퀵슬롯, 장착 무기)
  - 플레이 시간, 장소 이름(가장 가까운 주요 장소, `MapPlaces`)
- **쓰기:** 임시 파일에 다 쓴 다음 바꿔 끼운다. 쓰는 도중에 꺼져도 예전 저장이 남는다.
- **불러오기:** `SaveSystem.Pending`에 담고 Main을 연다.
  - `Inventory.Awake`가 가방을 가져간다.
  - `GameManager.Start`(실행 순서 -50, 카메라보다 먼저)가 나머지를 적용한다.
  - 저장된 웨이브를 끝낸 상태로 시작하고, 다음 웨이브는 잠시 뒤에 온다.
- **확인 절차:** 덮어쓰기, 지우기(X), 게임 중 불러오기는 한 번 더 눌러야 실행된다.
- **호환성:** 필드를 추가할 때는 기본값을 두고 `SaveData.version`을 올린다. 예전 파일도 읽힌다.
- **가방 저장 방식 변경:** 가방은 이제 PlayerPrefs가 아니라 저장 슬롯에 들어간다. `Inventory.Export/Import`가 담당한다.

## 게임 오버 (`UI/GameOverScreen.cs`)

1. 화면이 조금 어두워지며 붉은 **패배....** 띠가 약 3초 동안 나온다. 글씨가 1.12배에서 1배로 줄며 또렷해진다.
   - 띠 위로 눈이 ×× 인 민미 얼굴이 톡 떨어지며 나타나 기운 없이 흔들린다. 결과 화면이 뜨면 패널 왼쪽 위 모서리로 작아지며 옮겨 간다.
   - 얼굴 그림: `blender/render_defeat_face.py`(실제 민미 모델에서 눈을 지우고 정면 렌더) → `tools/defeat_face.py`(×× 를 그림) → `Resources/UI/GameOver/DefeatFace.png`.
   - 밝기: 화면 덮개 0.40(띠) → 0.58(결과), 띠 바탕 0.55, 결과 패널에 따뜻한 밝은 막을 한 겹 더함.
2. 결과 화면 **여정의 기록**이 떠오른다.
   - 처치한 적
   - 합 승률(○승 ○패)
   - 최고 위력
   - 도달 웨이브
   - 레벨
   - 플레이 시간
   - 숫자는 0부터 차오른다.
3. 버튼:
   - **마지막 저장에서 다시**: 어느 기록인지와 몇 분 전인지 표시한다. 저장이 없으면 **처음부터 다시**로 바뀐다.
   - **타이틀로**

## 검증

- `tools/status_tests`: `SaveFormat` 표시 규칙을 검사한다(시간, 날짜와 시간대, 몇 분 전, 승률, 최근 슬롯, 요약).
- `tools/compile_check.sh`, `tools/unity_audit.py`
