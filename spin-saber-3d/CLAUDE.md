# 회전 쌍검 광선검 — 3D 모델 인수인계 (Claude용)

three.js로 **코드만으로 만든(외부 3D 파일 없음)** 회전 쌍검 광선검 모델과 애니메이션입니다.
같은 방식으로 만든 "하프물범 필통 · 스타 스파크 검" 패키지(`gift-3d-models`)와 짝을 이루지만, 이 폴더만으로 독립적으로 동작합니다.
이 문서만 읽고 다른 장면에 넣거나 모양·색·움직임을 고칠 수 있도록 정리했습니다.

---

## 1. 파일 구성

| 경로 | 내용 |
|---|---|
| `src/spinSaber.js` | **핵심.** `createSpinSaber(options)`, `applyEnvironment()`, `PALETTE`(24색), `THEMES` 내보냄 |
| `src/viewer360.js` | 드래그 360° 회전·핀치 확대·자동 회전 뷰어 `Viewer360` (`open(model)`) |
| `demo/index.html` | 테마(실버/골드/블루), 🌀 회전, ⚡ 칼날 펼치기/접기, 🎨 24색 바꾸기 버튼이 있는 데모. URL 옵션: `?theme=gold&spin&color=16` |
| `reference/` | 실물 상품 사진 3장(블루 회전 장면, 실버, 골드) |
| `screenshots/` | 렌더 결과: 실버 정면·회전 중·옆, 골드, 블루 회전, 손잡이 근접 |

## 2. 의존성 · 실행

- **three.js r160 (`three@0.160.0`)** 기준. addons: `three/addons/geometries/RoundedBoxGeometry.js`, `three/addons/environments/RoomEnvironment.js`
- import 경로가 표준 형태라 npm 프로젝트에서는 `npm i three@0.160.0` 후 그대로, 순수 HTML에서는 `demo/index.html`처럼 importmap으로 CDN 연결
- ES 모듈이라 로컬 서버 필요: 폴더 루트에서 `npx serve .` 또는 `python3 -m http.server` → `/demo/index.html`

## 3. API

```js
import { createSpinSaber, applyEnvironment, PALETTE, THEMES } from "./src/spinSaber.js";

applyEnvironment(renderer, scene);                       // 필수: 금속 반사용 환경맵
renderer.toneMapping = THREE.ACESFilmicToneMapping;      // 권장
renderer.toneMappingExposure = 1.1;

const saber = createSpinSaber({
  theme: "silver",     // "silver" | "gold" | "blue"   (사진 2 / 3 / 1)
  color: 12,           // PALETTE 번호 0~23 (12 = 시안) 또는 hex/THREE.Color
  bladeLength: 1.0,    // 한쪽 칼날 길이
  bladeRadius: 0.036,  // 칼날 굵기
  bladeOn: true,       // true면 처음에 칼날이 펼쳐지는 애니메이션
  spinning: false,     // 처음부터 회전할지
});
scene.add(saber);

// 매 프레임
saber.userData.update(dt, t);

// 제어
const api = saber.userData.api;
api.toggleSpin();  api.setSpinning(true);
api.toggleBlade(); api.setBlade(false);
api.nextColor();   api.setColor(5);  api.setColor(0xff3366);
api.state;         // { bladeOn, spinning, colorIndex, extension(0~1), speed(rad/s) }
```

- **좌표**: 칼날 축 = **Y**(위아래 대칭), 손잡이 고리가 보이는 정면 = **+Z**, 원점 = 고리 중심
- **회전축 = Z**(정면을 향한 축). 선풍기 날개처럼 화면 평면에서 돕니다. 최고 속도 `SPIN_MAX = 16 rad/s`(약 2.5바퀴/초), 가속·감속은 부드럽게.
- 구조: 반환되는 `outer`(고정 프레임: 회전 궤적 원판, 빛 고리) 안에 `userData.spinner`(실제로 도는 검)가 들어 있습니다. 바깥 `outer`의 위치·회전·크기는 비어 있어 자유롭게 배치하면 됩니다.
- `userData.length` = 칼날을 펼쳤을 때 전체 길이(기본 3.44). 화면 맞춤에 쓰세요. 칼날이 접혀 있을 때 `Box3`로 재면 몸통만 잡힙니다.

### 크기 (기본값)
| 부분 | 값 |
|---|---|
| 전체 길이(칼날 펼침) | 약 3.44 (Y −1.72 ~ +1.72) |
| 몸통(하우징+고리) 길이 | 약 1.38 (Y −0.69 ~ +0.69), 폭 약 0.55 |
| 고리 반지름 | 0.17 (두께 0.034) + 톱니 테두리 |
| 칼날 | 발사구 y=±0.69 에서 길이 1.0, 반지름 0.036 |

## 4. 디자인 원본(실물) 설명

- 상품: 회전 쌍검 광선검 장난감("빠른 회전 쌍검 발도", 赤焰飓风光剑 계열) — `reference/`
- 공통 특징
  - 가운데 **둥근 고리 손잡이**(바깥 톱니 테두리, 안쪽에 손으로 쥐는 봉) — 고리를 쥐고 검 전체를 돌림
  - 고리 위아래로 **대칭인 기계 장갑 하우징**: 각진 장갑판, 흰 패널, 시안 LED 줄·보석, 옆 날개
  - 양쪽으로 뻗는 **빛나는 원통형 칼날**(마디가 있는 늘어나는 칼날), 칼끝 둥글게
  - **회전 잠금 스위치**, **24색 LED 전환**, 칼날 자동 펼침
  - 광고 이미지 연출: 회전 시 원형 잔상, 고리 주변 동심원 빛, 칼날을 감는 **번개**, 고리 옆 **불꽃**
- 색 테마: 실버+시안(사진 2), 골드+흰 패널(사진 3), 블루 장갑(사진 1)
- **글씨·로고·상품 문구는 넣지 않음**(앞선 검 모델과 같은 원칙)

## 5. 구성 요소와 조정 포인트 (`src/spinSaber.js`)

| 요소 | 구현 | 조정 |
|---|---|---|
| 고리 손잡이 | `TorusGeometry` 장갑색 + 바깥 테두리 + 안쪽 발광 링, 톱니 28개(위아래 하우징 닿는 곳은 비움), 고무 손잡이 봉 + 발광 띠 3, 잠금 스위치 | `R = 0.17`, 톱니 개수 28 |
| 하우징 | 각진 실루엣 `Shape` → Extrude(베벨), 앞뒤 흰 패널, 발광 다이아 보석·LED 줄, 옆 날개 2, 크롬 발사구 + 발광 링. 위 하우징을 `rotation.z = π`로 복제해 아래 하우징 | `housingShape()`, `panelShape()` 꼭짓점 |
| 칼날 | 원통 + 둥근 칼끝, 셰이더: 가운데 흰 심지 → 가장자리 색(프레넬), 마디 무늬, 미세 깜빡임. 십자 평면 2장의 가산 혼합 후광 | `BL`, `BR`, 마디 간격 `vY * 5.0` |
| 펼치기/접기 | 칼날 그룹 `scale.y`를 0↔1로 부드럽게 보간 | 속도 `dt * 5`(펼침) / `dt * 8`(접힘) |
| 회전 | `spinner.rotation.z` 누적, 목표 속도로 지수 보간 | `SPIN_MAX = 16`, 가속 `1.6`, 감속 `1.1` |
| 회전 궤적 | 고정 프레임의 `RingGeometry` 원판 + 셰이더: 칼날 바로 뒤가 가장 밝고 반 바퀴 동안 사라짐(쌍검이라 π 주기), 속도에 비례해 보임 | `pow(1-behind, 6.0)`, 밝기 `0.55` |
| 빛 고리 | 고리 중심에서 퍼져 나가는 링 3개, 회전 중에만 보임 | 퍼지는 속도 `t * 0.8` |
| 번개 | 칼날마다 `Line` 2줄(흰색 1, 칼날색 1), 0.05~0.1초마다 지그재그 모양 재생성, 랜덤 깜빡임 | `ARC_PTS = 26`, 흔들림 폭 `±0.08` |
| 스파크 | 200개 Points: 고리 양옆에서 바깥으로 튀고 중력으로 떨어짐, 노랑→주황→빨강으로 식음, 회전 중엔 더 빨리 생성 | `N = 200`, 크기 `0.05` |
| 색 | `setColor`가 칼날·후광·궤적·LED 발광 재질·번개·빛 고리를 한 번에 바꿈 | `PALETTE`(HSL 15° 간격) |
| 테마 | 장갑/트림/패널 색과 금속감 | `THEMES` 객체에 항목 추가 가능 |

## 6. 반드시 지킬 것

1. **`applyEnvironment()` 필수**: 없으면 금속 장갑·크롬이 새까맣게 보입니다.
2. **ACES 톤 매핑 권장**: 정면의 강한 직사광은 장갑면을 하얗게 날립니다. 검증된 조명: `Hemisphere(0xffffff, 0x8899aa, 1.5)` + `Directional(1, 2, 2) 2.2` + 역광 `(-2, .5, -2) 0.9` (`viewer360.js`).
3. **가산 혼합 셰이더는 알파를 직접 출력**(`vec4(color, a)`) — 투명 배경 캔버스에서 검은 사각형이 생기지 않게.
4. **바운딩 계산 시 파티클·가산 재질 제외**: 숨겨 둔 스파크가 y=−99 근처에 있습니다.
5. 회전 궤적 원판은 **회전하지 않는 프레임**(`outer`)에 있어야 합니다. `spinner` 안에 넣으면 궤적이 같이 돌아 깨집니다.
6. 글씨·로고 금지.

## 7. 다른 환경으로 옮길 때

- **React Three Fiber**: `const s = useMemo(() => createSpinSaber({ theme: "gold" }), [])` → `<primitive object={s} />`, `useFrame((st, dt) => s.userData.update(dt, st.clock.elapsedTime))`, 버튼에서 `s.userData.api.toggleSpin()`
- **AR**: 마커 평면이 XY·위가 +Z인 앵커라면 그대로 두면 검이 종이 위에 누운 채 돌고, `rotation.x = Math.PI/2`면 세워서 돕니다.
- **GLB 내보내기**: 장갑·고리 메시는 `GLTFExporter`로 나가지만, 칼날 셰이더·후광·궤적·번개·스파크·회전 애니메이션은 코드 효과라 내보내지지 않습니다(회전은 키프레임으로 굽기 쉬움).

## 8. 검증

- `demo/index.html`에서 테마 3종, 🌀 회전, ⚡ 칼날, 🎨 색 버튼을 눌러 `screenshots/`와 비교
- 버튼 상태 확인(자동 테스트로 확인한 값): 회전 켬 → 속도 약 15.7 rad/s 도달, 칼날 끔 → `extension` 0, 색 두 번 → `colorIndex` 14
- 콘솔 에러 없음
