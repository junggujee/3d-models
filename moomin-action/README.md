# 민미 액션 — 스파크 검으로 싸우는 3D 액션 게임 (Blender + Unity)

민미가 도시를 돌아다니며 **스파크 검**으로 그림자 덩어리들과 싸우는 웨이브 액션. 모델·애니메이션은 **Blender(Python, 화면 없이)** 로 만들고, 게임 로직은 **Unity(C#)** 로 작성합니다.

```
moomin-action/
├─ blender/        모델·뼈대·애니메이션 생성 스크립트 (bpy)
├─ exports/        Blender 결과물: *.fbx(Unity용) *.glb(웹/미리보기) *.blend
├─ previews/       Blender 렌더 미리보기
├─ unity/Assets/   Unity 프로젝트에 복사할 Assets (Models, Scripts, Editor)
└─ city/           도시 배경 변환 안내 (원본 .skp는 Git 제외)
```

## 1. 모델 다시 만들기 (Blender)
```bash
pip install bpy==5.3.0     # Python 3.13
python blender/build_all.py --preview     # exports/ 와 previews/ 갱신
```
| 모델 | 내용 |
|---|---|
| `Moomin` | 서 있는 민미(약 1.27m, 약 1만 정점). 뼈대 14개, 애니메이션 **Idle, Walk, Run, Attack1~3, Hit, Death**. 눈 깜빡임용 `eye.L/R`, 검 소켓 `hand.R` |
| `StarSparkSword` | 검은 금속 칼날 + 빛나는 날 + 붉은 육각 가드. 스파크 위치 `SparkPoint_*` 빈 오브젝트 |
| `ShadowBlob` | 적. 움직임은 Unity 코드(스쿼시) |

## 2. Unity에서 실행
1. Unity 6(또는 2022 LTS 이상)에서 **3D(URP 또는 Built-in) 새 프로젝트** 생성
2. 이 저장소의 `unity/Assets/*` 를 프로젝트 `Assets/` 에 복사 (`Models/` 의 FBX 3개 포함)
3. 메뉴 **Moomin ▸ 한 번에 설정 (전부 실행)** → `Assets/Scenes/Main.unity` 열고 Play
4. 도시: `city/README.md` 대로 변환한 파일을 `Assets/City/` 에 넣고 **Moomin ▸ 4. 메인 씬 만들기** 재실행

조작: WASD 이동 · Shift 달리기 · Space 점프 · 좌클릭/J 공격(3연타 콤보) · Esc 커서 해제 · 사망 후 R 재시작

## 3. 현재 상태·한계 (정직하게)
- **검증됨**: Blender 빌드·내보내기(FBX/GLB), 애니메이션 포즈 렌더, C# 문법 검사(0 오류).
- **미검증**: Unity에서의 실제 실행. Unity 에디터가 없는 환경에서 작성해서 컴파일·플레이 테스트를 못 했습니다. 첫 실행에서 오류가 나면 콘솔 메시지를 알려 주세요. 특히 확인 포인트: (a) 애니메이션 클립 이름(`MoominRig|Idle` 형태), (b) 검이 손에 붙는 각도(`MoominSetup` 의 `Quaternion.Euler(-120,0,0)`), (c) 입력 시스템 설정.
- 털은 게임용으로 **저폴리 + 노이즈 질감 + 시스(sheen)** 로 표현(셸 퍼 셰이더는 아직 없음, 추후 URP 셰이더로 추가 가능).
- 도시는 `.skp` 변환 대기 중(city/README.md).
