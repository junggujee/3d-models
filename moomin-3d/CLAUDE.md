# 민미(무민) — 엎드린 인형 + 서 있는 모습 — 3D 모델 인수인계 (Claude용)

three.js로 **코드만으로 만든(외부 3D 파일 없음)** 민미 인형 모델과 애니메이션입니다. `gift-3d-models`, `spin-saber-3d`와 같은 방식·같은 API 규칙입니다.

## 파일
| 경로 | 내용 |
|---|---|
| `src/moomin.js` | **핵심.** `createMoomin(opt)`, `applyEnvironment()`, `FUR` 내보냄 |
| `src/viewer360.js` | 드래그 360° 뷰어 `Viewer360` (`open(model)`) |
| `demo/index.html` | 360° 데모. URL: `?pose=standing`, `?yaw=0.7&pitch=0.45&dist=0.85` |
| `reference/` | `lying_ref_1~5.png` 엎드린 인형 사진, `face_ref.webp` 정면 얼굴 비율 기준, `standing_ref.png` 서 있는 모습 기준 |
| `screenshots/` | 렌더 결과: `lying_front`, `lying_three_quarter`, `standing_front`, `standing_three_quarter` |

## 실행·의존성
- three.js **r160** (`three@0.160.0`), addon: `RoomEnvironment`. ES 모듈이라 로컬 서버 필요: `python3 -m http.server` → `/demo/index.html`
- `npm i three@0.160.0` 후 그대로 import 가능

## API
```js
import { createMoomin, applyEnvironment } from "./src/moomin.js";
applyEnvironment(renderer, scene);                  // 환경맵(부드러운 반사)
renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 1.1;
const m = createMoomin({ pose: "lying", furLayers: 14 }); // pose: "lying"(엎드림) | "standing"(서 있음). 털 겹 수(저사양이면 8)
scene.add(m);
m.userData.update(dt, t);                           // 매 프레임
```
- 좌표: **Y 위, +Z 앞(얼굴)**, 원점 = 바닥 중앙. 바깥 Group의 위치·회전·크기는 비어 있어 자유롭게 배치.
- 바운딩 계산은 메시만 사용(털 껍질 때문에 삼각형이 많음).

## 구성·조정 포인트 (`moomin.js`)
| 요소 | 구현 | 조정 |
|---|---|---|
| 털 | 셸 퍼: 타원체를 법선 방향으로 `furLayers`겹 부풀리고 `alphaMap`+`alphaTest`로 가닥처럼 깎음, 털끝 처짐 | `FUR=0.05`(길이), 텍스처 `repeat(14,9)`(밀도), `addFur` 3번째 인자(겹 수) |
| 몸통·머리 | 타원체: 몸 `(0.44,0.35,0.6)`, 머리 `(0.54,0.33,0.58)` + 넓은 주둥이 | `ell(rx,ry,rz)` 값 |
| 눈 | 흰 테두리 + 파란 자수 평면 2장(광택 없는 천 질감, 가장자리 번짐 텍스처), 털 사이로 살짝 묻힘 | `onHead(phi, th, off)` 위치, `eyeTex`/`ringTex` 색 |
| 눈썹 | 갈색 곡선 튜브 | `0x7a4034` |
| 귀·팔·뒷다리 | 털 달린 작은 타원체 | `ear`, `arm`, `leg` |
| 꼬리 | 끝으로 갈수록 가늘어지는 튜브 + 끝 털뭉치 | `path` 점, 반지름 `0.055→0.023` |
| 애니메이션 | 숨쉬기, 머리 갸웃, 눈 깜빡(2.5~5.5초마다), 귀 쫑긋, 꼬리 살랑, 앞발 꼼지락 | `update` 안 숫자 |

## 얼굴 비율 기준 (face_ref.webp)
- 큰 머리, 넓은 주둥이, 눈 사이가 넓은 둥근 세로 타원(흰 테두리 + 파란 자수), 눈 위 가늘고 긴 눈썹(바깥쪽이 처짐), 머리 위 양끝의 둥근 귀
- 눈 위치: `EYE`/`BROW` 상수(`phi`,`th`), 크기: `PlaneGeometry`
- 서 있는 자세는 두 팔을 볼에 댄 모습(`standing_ref.png`), 팔 각도 `arm.rotation`

## 주의
1. 눈은 털 껍질 위쪽 층에 일부 가려져 부드럽게 보입니다. 눈 오프셋(`FUR * 0.9`)을 키우면 더 선명, 줄이면 더 묻힙니다.
2. 조명을 과하게 주면 흰 털이 날아갑니다. 검증된 조명: `Hemisphere(0xffffff,0x8899aa,1.5)` + `Directional(1,2,2) 2.2`.
3. 글씨·로고(태그 문구 등) 넣지 않음. 모양은 `reference/`의 실물 기준.
4. GLB 내보내기: 털(alphaMap 셸)과 절차적 애니메이션은 glTF로 그대로 옮겨지지 않음.

## 검증
`demo/index.html`을 열어 `screenshots/`와 비교. 콘솔 에러 없음(헤드리스 크롬으로 확인), 털이 가닥처럼 복슬하게 보이는지, 눈이 부드럽게 파란지 확인.
