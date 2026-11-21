# 하프물범 필통 · 스타 스파크 검 — 3D 모델 인수인계 (Claude용)

이 폴더는 three.js로 **코드만으로 만든(외부 3D 파일 없음)** 3D 모델 두 개와 애니메이션 패키지입니다.
원래 준우·정우 생일 AR 서프라이즈 웹앱(`junggujee/mandu`)에서 쓰던 것을, 다른 프로젝트에서 재사용할 수 있게 분리했습니다.
이 문서만 읽고 모델을 다른 장면에 넣거나, 모양·색·움직임을 고칠 수 있도록 정리했습니다.

---

## 1. 파일 구성

| 경로 | 내용 |
|---|---|
| `src/models.js` | **핵심.** `createSeal()`, `createSword()`, `applyEnvironment()` 내보냄 |
| `src/viewer360.js` | 드래그 360° 회전·핀치 확대·자동 회전 뷰어 클래스 `Viewer360` |
| `demo/index.html` | 두 모델을 버튼으로 바꿔 보는 360° 데모 |
| `demo/minimal-scene.html` | 일반 three.js 장면에 두 모델을 넣는 최소 예제(통합 참고용) |
| `reference/` | 물범 필통 실제 상품 사진 3장, AR 인식용 물범 선화 1장 |
| `screenshots/` | 렌더 결과(앞/뒤/옆, AR 화면) — 수정 전후 비교 기준 |

검(스타 스파크 배틀 컷) 참고 사진은 파일로 남아 있지 않아 아래 4절에 글로 정리했습니다.

## 2. 의존성 · 실행

- **three.js r160 (`three@0.160.0`)** 기준. 사용하는 addons: `three/addons/geometries/RoundedBoxGeometry.js`, `three/addons/environments/RoomEnvironment.js`
- import 경로는 표준 형태(`"three"`, `"three/addons/..."`)라서
  - **npm/Vite 등**: `npm i three@0.160.0` 후 그대로 import
  - **순수 HTML**: `demo/*.html` 처럼 importmap으로 CDN 연결
- ES 모듈이므로 `file://`로 열면 안 되고 로컬 서버가 필요합니다: 폴더 루트에서 `npx serve .` 또는 `python3 -m http.server` → `/demo/index.html`
- 이후 three 버전에서도 대부분 동작하지만, r163+ 에서는 `scene.environmentIntensity` 등 API가 늘었을 뿐 깨지는 부분은 확인하지 않았습니다(검증된 버전은 r160).

## 3. 공통 API · 좌표 규칙

```js
import { createSeal, createSword, applyEnvironment } from "./src/models.js";

applyEnvironment(renderer, scene);   // 반드시 한 번: 금속·광택 재질용 환경맵(RoomEnvironment → PMREM)
renderer.toneMapping = THREE.ACESFilmicToneMapping; renderer.toneMappingExposure = 1.1; // 권장(없으면 금속면이 하얗게 날아감)

const seal = createSeal();   // THREE.Group
const sword = createSword(); // THREE.Group
scene.add(seal, sword);

// 매 프레임 (dt: 초, t: 누적 초)
seal.userData.update(dt, t);
sword.userData.update(dt, t);
```

- **좌표**: 모델 로컬 기준 **Y = 위, +Z = 앞(얼굴/정면)**, 단위는 임의(아래 크기 참고)
- **움직임은 모델 내부에서만** 처리합니다. 바깥 Group의 position/rotation/scale은 비어 있으니 호출하는 쪽에서 자유롭게 배치·회전·크기 조절하면 됩니다.
- `userData.update`를 호출하지 않으면 정지 상태(스파크·공기방울 없음, 무지개 정지)로 보입니다.
- 크기 계산 시 주의: 두 모델 모두 파티클(Points)이 들어 있고, 대기 중인 입자는 y = -99 근처에 숨겨 둡니다. `Box3.setFromObject(model)`을 그대로 쓰면 크기가 엉망이 되니 **메시만, 그리고 가산 혼합(AdditiveBlending) 재질은 빼고** 계산하세요(`viewer360.js`의 `open()` 참고).

### 측정값 (r160, 기본 스케일)

| | 하프물범 필통 | 스타 스파크 검 |
|---|---|---|
| 바운딩 크기 (x, y, z) | 1.32 × 0.97 × 1.79 | 0.42 × 2.12 × 0.09 |
| 원점 | **바닥 중앙**(바닥이 y≈0) | **검 길이의 중앙**(칼끝 +Y, 폼멜 −Y) |
| 삼각형 수 | 약 140,600 (털 껍질 때문) | 약 2,600 |
| 메시 / 파티클 | 72 메시 / 공기방울 18개 | 12 메시 / 스파크 220개 |

## 4. 디자인 원본(실물) 설명

### 하프물범 필통 (준우 선물)
- 실제 상품: "몽실 물개 봉제 펜파우치(아기 물개 인형 필통)" — `reference/seal_product_*.webp`
- 특징: 통통한 달걀형 흰 털 몸통, 등에 회색 지퍼, 동그란 검은 자수 눈, 회색 퐁퐁 코 두 개, 작은 회색 눈썹, 옆 앞지느러미 2개, 꼬리 쪽 뒷지느러미 2개
- 요청 사항이었던 것: **복슬복슬한 입체 털**, 모양은 그대로 + **헤엄치는 모습**

### 스타 스파크 배틀 컷 (정우 선물)
- 실제 상품: LED 광선검 장난감 "스타 스파크 배틀 컷"(불꽃·사운드·분무 기능)
- 실물 특징: 길고 곧은 **검은 사각 칼날**, 칼날 한쪽 날을 따라 **무지개/붉은 LED**가 빛남, 끝이 비스듬히 잘린 칼끝, **붉은 육각형 가드**, 검정·붉은색 손잡이, 가드 부근에서 **불꽃이 튀는** 연출
- 요청 사항이었던 것:
  - **글씨·로고는 모두 빼고 검 모양만** (실물에 있는 "PIAO BO ZHE" 각인, 상품 문구 등 절대 넣지 않기)
  - 칼날은 손잡이의 약 3배 → 거기서 **25% 더 길게** (현재 `L = 1.55`)
  - 손잡이는 **광택 나는 금속**, 색 있는 부분(무지개 날, 붉은 가드)은 **빛나게**
  - **손잡이·가드에서 스파크**가 튀는 애니메이션

## 5. 모델 구조와 조정 포인트

### `createSeal()`
| 요소 | 구현 | 조정 변수(`models.js`) |
|---|---|---|
| 몸통 | 타원체 `SphereGeometry(1,72,48)` scale(A,B,C) | `A = 0.5`(폭/2), `B = 0.43`(높이/2), `C = 0.72`(길이/2) |
| 털 | **셸 퍼(shell fur)**: 몸통 지오메트리를 법선 방향으로 14겹 부풀려 겹침, 각 겹은 랜덤 점 텍스처를 `alphaMap` + 점점 높아지는 `alphaTest`로 깎아 털 가닥처럼 보이게. 뿌리 쪽을 살짝 어둡게, 털끝은 아래로 처짐 | `FUR = 0.045`(털 길이), `addFur(mesh, length, layers=14)`, `furTex.repeat(10,7)`(털 밀도) |
| 눈·눈썹·코 | 클리어코트 검은 구, 회색 캡슐, 털 있는 회색 구 2개 + 검은 코끝 | `onSurface(x, y, off)`로 몸통 표면 위 위치 지정 |
| 지퍼 | 등선 따라 `TubeGeometry` + 금속 손잡이 | z 범위 −0.56 ~ 0.3 |
| 지느러미 | 관절(pivot) Group에 달린 털 타원체(앞 2, 뒤 2) | `makeFlipper(...)` 인자 |
| 헤엄 애니메이션 | 몸 앞뒤 기울임·들썩임 `sin(t*3.2)`, 앞지느러미 노 젓기 `sin(t*4.5)`(진폭 0.5/0.35rad), 뒷지느러미 위아래 `sin(t*3.2)`(0.45rad) | `wrap.userData.update` 안 숫자 |
| 공기방울 | 18개 Points, 코 위에서 생겨 위로 떠오르며 좌우로 흔들림 | `NB`, 상승 속도 `0.35` |

- AR 앱에서는 바깥에서 추가로 **원을 그리며 헤엄**시켰습니다: 회전하는 부모 Group 안에서 `model.position.x = 0.38`, `model.rotation.y = Math.PI`(진행 방향을 보게), 부모를 `rotation.y += 0.7 rad/s`로 돌림.
- 성능: 털 때문에 삼각형이 많습니다. 저사양이면 `addFur(body, FUR, 8)`처럼 겹 수를 줄이거나 `SphereGeometry(1, 48, 32)`로 낮추세요.

### `createSword()`
| 요소 | 구현 | 조정 변수 |
|---|---|---|
| 칼날 | 비스듬한 칼끝 Shape를 Extrude, 검은 금속(Physical, metalness 0.9, clearcoat) | `L = 1.55`(칼날 길이), `TIP = 0.1`, 폭 `0.05*2` |
| 무지개 날 | 칼날 한쪽 가장자리 띠, ShaderMaterial로 HSV 무지개가 칼끝 쪽으로 흐름 | 셰이더의 `vY*0.55`(색 간격), `uT*0.35`(흐름 속도) |
| 발광 후광 | 십자로 엇갈린 평면 2장, 가산 혼합 셰이더(어느 각도에서도 빛남, 맥동 `sin(uT*7)`) | 평면 폭 `0.42` |
| 가드 | 붉은 육각판 Extrude, 은은한 자체발광(emissive) 맥동 | `redGlow` 재질, `emissiveIntensity 0.3±0.15` |
| 손잡이 | 건메탈 금속(metalness 1, roughness 0.2) + 크롬 링 2 + 붉은 띠 2 + 붉은 폼멜 | `metal`, `chrome` 재질 |
| 스파크 | 220개 Points(가산 혼합): 70%는 가드 양끝, 30%는 손잡이 윗부분에서 생성 → 옆·위로 튀고 중력으로 떨어짐, 색은 노랑→주황→빨강으로 식음. `sin(t*2.3) > 0.85`일 때 한꺼번에 '파박' | `N`, 수명 `0.35~0.95s`, 중력 `1.6`, 입자 크기 `0.07` |
| 플레어 | 가드 중앙의 주황 Sprite, 불규칙 깜빡임 | `flare.scale` |

## 6. 반드시 지킬 것 (이 조건에서 문제가 났던 부분)

1. **`applyEnvironment()` 필수** — 없으면 금속 재질(손잡이·크롬·칼날)이 새까맣게 보입니다.
2. **ACES 톤 매핑 권장 + 조명 과하지 않게** — 정면에서 강한 직사광을 주면 검 넓은 면과 붉은 가드가 하얗게 날아갔습니다. 검증된 조명: `Hemisphere(0xffffff, 0x8899aa, 1.0)` + `Directional(0xffffff, 1.6)` at `(1, 1.5, 1.2)`.
3. **투명 배경 캔버스(`alpha:true`)** 위에서는 가산 혼합 셰이더가 알파를 직접 출력해야 합니다(현재 `gl_FragColor = vec4(c*0.9, a)`). 알파를 1로 쓰면 후광 판이 검은 사각형으로 보입니다.
4. **글씨·로고 금지**(검), 하프물범은 **실제 상품 모양 유지**가 원 요청입니다.
5. 바운딩 계산 시 파티클 제외(3절 참고).

## 7. 다른 환경으로 옮길 때

- **React Three Fiber**: `const obj = useMemo(() => createSeal(), [])` → `<primitive object={obj} />`, `useFrame((s, dt) => obj.userData.update(dt, s.clock.elapsedTime))`. 환경맵은 `applyEnvironment(gl, scene)` 한 번 또는 drei `<Environment preset="apartment" />`.
- **AR (MindAR 등)**: 마커 평면이 XY, 위가 +Z인 앵커라면 모델을 `rotation.x = Math.PI/2`로 세워서 넣습니다(원래 앱에서 물범은 `π/2 − 0.85`로 살짝 뒤로 기울여 얼굴이 카메라를 보게, 검은 `0.55`로 비스듬히 띄움).
- **GLB 파일이 필요할 때**(Blender·Unity 등): `GLTFExporter`로 메시는 내보낼 수 있지만, ① 털은 `alphaMap`+`alphaTest` 방식이라 glTF에서 그대로 재현되지 않고 ② 셰이더 발광·파티클·절차적 애니메이션은 내보내지지 않습니다. 필요하면 털을 텍스처 기반으로 바꾸고, 지느러미 움직임을 키프레임으로 구워(bake) 내보내는 작업이 별도로 필요합니다.

## 8. 검증 방법

- `demo/index.html`에서 두 모델을 각각 드래그로 360° 돌려 보고 `screenshots/`와 비교
- 콘솔 에러 없음, 물범 털이 덩어리지지 않고 가닥처럼 보이는지, 검 손잡이가 금속 반사를 보이는지, 스파크가 가드·손잡이에서 나오는지 확인
