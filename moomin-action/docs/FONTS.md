# 글꼴 (2026-11-24)

목표: **세계관의 깊이와 전투의 무게감을 전하면서, 기존 UI의 가독성은 지킨다.**

## 역할

| 역할 | 쓰는 곳 | 글꼴 | 꾸밈 |
|---|---|---|---|
| Display | 게임 제목, 장(칸토) 제목, 보스 이름, 큰 알림(웨이브·퀘스트 수락/완료), 합 승패 띠, 패배 띠, 대화창 제목 띠 | **빛의 계승자 Bold** | 크림→베이지 세로 그라데이션 + 부드러운 그림자, 필요하면 은은한 빛 + 스타 세일러풍 장식선 |
| Heading | 캐릭터·NPC·펫 이름, 퀘스트 제목, 메뉴 머리글, 선택지 버튼, 타이틀 메뉴 | **Sandoll 가웨인**(파일이 있으면) → 없으면 **빛의 계승자 Regular** | 그대로(가짜 굵게 없음) |
| Skill | 스킬 이름(스킬 패널 띠, 스킬 칸) | **빛의 계승자 Regular** | 흰색→옅은 크림 + 금빛 은은한 빛(`TextGlow`) |
| Body | 대사, 설명, 숫자, 조작 안내 | **나눔명조**(기존) | 그대로 |

## 고른 이유

- **빛의 계승자**를 Display·Skill에 썼다.
  - 획이 굵고 끝이 날카로워서, 보스 이름과 큰 제목에서 전투의 무게감이 산다.
  - 사용자 설명대로 "전통 판타지·고전적인 분위기"이고, 장식이 과하지 않아 스킬 이름처럼 짧은 글에 잘 맞는다.
  - 무료 글꼴이라 게임에 넣을 수 있다.
- **Sandoll 가웨인**(1순위 후보)은 유료 상용 글꼴이라 프로젝트에 넣지 않았다.
  - 라이선스가 있는 파일을 `unity/Assets/Resources/Fonts/SandollGawain.ttf`로 넣으면 Heading 역할에 자동으로 쓰인다(`UITheme.HeadingFont`).
  - 가웨인은 기사도풍의 차분한 세리프라 이름·장 제목·메뉴에 더 어울린다. 보스·제목처럼 강하게 보여야 하는 곳은 빛의 계승자를 그대로 둔다.
- **나눔명조**는 긴 대사와 숫자를 쉽게 읽게 하려고 그대로 쓴다.
  - 판타지 글꼴은 긴 글에서 눈이 피곤하다.
- 사용자가 보낸 **스타 세일러 로고**는 글꼴이 아니라 로고 디자인이다. 다음 세 가지만 흉내 냈다.
  - 크림→베이지 그라데이션
  - 양 끝이 마름모인 가는 금선과 가운데 네 갈래 별(`Resources/UI/Ornament/Divider.png`, `Sparkle.png`)
  - 부드러운 그림자

## 코드

- `UITheme.DisplayFont / HeadingFont / SkillFont / BodyFont`
- `UIFactory.StyleDisplay(text, glow)`, `StyleHeading(text)`, `StyleSkill(text, glowColor)`, `Divider(parent, name, width)`
- `UI/TextFx.cs`
  - `UIGradient`: 정점 색에 곱하는 세로 그라데이션이다. 그림자 정점은 검은색 그대로 남는다.
  - `TextGlow`: 글자 둘레에 옅은 복사본을 8방향×2겹으로 깐다.

## 글자 수

- 빛의 계승자는 완성형 2,350자만 있다.
- `MoominSetup`이 글꼴 임포터의 `fontReferences`에 나눔명조 ExtraBold를 넣는다. 그래서 없는 글자(예: 똠, 꽥)도 빈칸으로 나오지 않는다.

## 라이선스

- 빛의 계승자: `unity/Assets/Resources/Fonts/HeirOfLight_LICENSE.txt`
  - 산돌구름 무료 글꼴 안내 https://www.sandollcloud.com/free-font/274/Sandoll-HeirofLight
- 나눔명조: OFL, `NanumFont_LICENSE.txt`
- 미리보기: `previews/font_roles.png` (`tools/dialogue_mock.py`)
