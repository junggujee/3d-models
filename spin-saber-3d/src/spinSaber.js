// 회전 쌍검 광선검 — 코드로 만든 3D 모델 (외부 3D 파일 없음, 글씨·로고 없음)
// 좌표: 칼날 축 = Y(위아래 대칭), 앞(손잡이 고리가 보이는 면) = +Z, 원점 = 고리 중심
// 회전은 Z축(정면을 향한 축) 기준 — 선풍기 날개처럼 돕니다.
import * as THREE from "three";
import { RoundedBoxGeometry } from "three/addons/geometries/RoundedBoxGeometry.js";
import { RoomEnvironment } from "three/addons/environments/RoomEnvironment.js";

// 금속 반사용 환경맵 (장면마다 한 번)
export function applyEnvironment(renderer, scene) {
  const pm = new THREE.PMREMGenerator(renderer);
  scene.environment = pm.fromScene(new RoomEnvironment(), 0.04).texture;
  pm.dispose();
}

// 24색 팔레트 (색상환 15°씩). 12번 = 시안(기본)
export const PALETTE = Array.from({ length: 24 }, (_, i) => new THREE.Color().setHSL(i / 24, 1, 0.55));

export const THEMES = {
  silver: { armor: 0xc9ced6, trim: 0x3a3f48, panel: 0xeef2f6, metal: 0.9, rough: 0.22 }, // 은색 (사진 2)
  gold:   { armor: 0xc8a24a, trim: 0x5a4a22, panel: 0xf2f4f6, metal: 1.0, rough: 0.25 }, // 금색 (사진 3)
  blue:   { armor: 0x2f6fd6, trim: 0x1c2b4a, panel: 0xcfd8e6, metal: 0.6, rough: 0.3 },  // 파랑 (사진 1)
};

const dotTex = (() => {
  const c = document.createElement("canvas"); c.width = c.height = 64;
  const x = c.getContext("2d"), g = x.createRadialGradient(32, 32, 0, 32, 32, 32);
  g.addColorStop(0, "rgba(255,255,255,1)"); g.addColorStop(0.35, "rgba(255,255,255,.7)"); g.addColorStop(1, "rgba(255,255,255,0)");
  x.fillStyle = g; x.fillRect(0, 0, 64, 64);
  return new THREE.CanvasTexture(c);
})();

const additive = { transparent: true, depthWrite: false, blending: THREE.AdditiveBlending };

/**
 * @param {object} opt
 * @param {"silver"|"gold"|"blue"} [opt.theme="silver"]
 * @param {number} [opt.color=12]        PALETTE 번호(0~23) 또는 THREE.Color/hex
 * @param {number} [opt.bladeLength=1.0] 한쪽 칼날 길이
 * @param {number} [opt.bladeRadius=0.036] 칼날 굵기(반지름)
 * @param {boolean} [opt.bladeOn=true]   처음에 칼날이 켜진 상태인지(켜질 때 펼쳐지는 애니메이션)
 * @param {boolean} [opt.spinning=false] 처음부터 회전할지
 */
export function createSpinSaber(opt = {}) {
  const T = THEMES[opt.theme] || THEMES.silver;
  const BL = opt.bladeLength ?? 1.0;
  const BR = opt.bladeRadius ?? 0.036; // 칼날 굵기(반지름)
  const uColor = { value: new THREE.Color() };
  const uT = { value: 0 };

  const outer = new THREE.Group();   // 고정 프레임(회전 궤적, 빛 고리)
  const spinner = new THREE.Group(); // 실제로 도는 검
  outer.add(spinner);

  // ── 재질
  const armor = new THREE.MeshPhysicalMaterial({ color: T.armor, metalness: T.metal, roughness: T.rough, clearcoat: 0.6, clearcoatRoughness: 0.2 });
  const trim = new THREE.MeshPhysicalMaterial({ color: T.trim, metalness: 0.8, roughness: 0.35 });
  const panel = new THREE.MeshPhysicalMaterial({ color: T.panel, metalness: 0.3, roughness: 0.3, clearcoat: 1 });
  const chrome = new THREE.MeshPhysicalMaterial({ color: 0xe8ebf0, metalness: 1, roughness: 0.08 });
  const rubber = new THREE.MeshStandardMaterial({ color: 0x23262d, roughness: 0.85 });
  const glowMat = new THREE.MeshStandardMaterial({ color: 0x111111, emissive: new THREE.Color(), emissiveIntensity: 2.2, roughness: 0.4 });
  const glowMats = [glowMat];

  // ── 가운데 고리 손잡이
  const R = 0.17;
  const ring = new THREE.Mesh(new THREE.TorusGeometry(R, 0.034, 20, 72), armor); spinner.add(ring);
  const ringTrim = new THREE.Mesh(new THREE.TorusGeometry(R + 0.03, 0.012, 12, 72), trim); spinner.add(ringTrim);
  const ringGlow = new THREE.Mesh(new THREE.TorusGeometry(R - 0.03, 0.008, 10, 72), glowMat); spinner.add(ringGlow);
  // 톱니 테두리
  const toothGeo = new RoundedBoxGeometry(0.03, 0.035, 0.05, 2, 0.006);
  for (let i = 0; i < 28; i++) {
    const a = (i / 28) * Math.PI * 2;
    if (Math.abs(Math.sin(a)) > 0.9) continue; // 위아래(하우징과 닿는 곳)는 비움
    const t = new THREE.Mesh(toothGeo, trim);
    t.position.set(Math.cos(a) * (R + 0.05), Math.sin(a) * (R + 0.05), 0); t.rotation.z = a;
    spinner.add(t);
  }
  // 손잡이 봉(고리 안, 칼날 축 방향) + 파란 줄무늬
  const grip = new THREE.Mesh(new THREE.CapsuleGeometry(0.032, 0.26, 6, 16), rubber); spinner.add(grip);
  for (const y of [-0.08, 0, 0.08]) {
    const band = new THREE.Mesh(new THREE.TorusGeometry(0.034, 0.005, 8, 24), glowMat);
    band.rotation.x = Math.PI / 2; band.position.y = y; spinner.add(band);
  }
  // 회전 잠금 스위치(고리 옆)
  const sw = new THREE.Mesh(new RoundedBoxGeometry(0.05, 0.08, 0.03, 2, 0.008), trim);
  sw.position.set(R + 0.075, -0.06, 0.04); spinner.add(sw);

  // ── 하우징(위·아래 대칭)
  const housingShape = () => {
    const s = new THREE.Shape(), p = [[0.16, 0.15], [0.21, 0.27], [0.15, 0.37], [0.12, 0.5], [0.065, 0.6]];
    s.moveTo(-p[0][0], p[0][1]);
    for (const [x, y] of p) s.lineTo(x, y);
    for (const [x, y] of [...p].reverse()) s.lineTo(-x, y);
    s.lineTo(-p[0][0], p[0][1]);
    return s;
  };
  const panelShape = () => {
    const s = new THREE.Shape();
    s.moveTo(-0.09, 0.22); s.lineTo(0.09, 0.22); s.lineTo(0.12, 0.29); s.lineTo(0.07, 0.4); s.lineTo(0.045, 0.52);
    s.lineTo(-0.045, 0.52); s.lineTo(-0.07, 0.4); s.lineTo(-0.12, 0.29); s.lineTo(-0.09, 0.22);
    return s;
  };
  const makeHousing = () => {
    const h = new THREE.Group();
    const body = new THREE.Mesh(new THREE.ExtrudeGeometry(housingShape(), { depth: 0.08, bevelEnabled: true, bevelSize: 0.012, bevelThickness: 0.014, bevelSegments: 2 }), armor);
    body.geometry.translate(0, 0, -0.04); h.add(body);
    for (const side of [1, -1]) {
      const pg = new THREE.ExtrudeGeometry(panelShape(), { depth: 0.012, bevelEnabled: false });
      const pnl = new THREE.Mesh(pg, panel); pnl.position.z = side * 0.056 - (side < 0 ? 0.012 : 0); h.add(pnl);
      // 빛나는 보석(다이아) + 줄 조명
      const gem = new THREE.Mesh(new THREE.OctahedronGeometry(0.035, 0), glowMat);
      gem.scale.set(0.8, 1.4, 0.35); gem.position.set(0, 0.36, side * 0.07); h.add(gem);
      for (const x of [-0.075, 0.075]) {
        const strip = new THREE.Mesh(new RoundedBoxGeometry(0.014, 0.11, 0.01, 1, 0.004), glowMat);
        strip.position.set(x, 0.29, side * 0.07); strip.rotation.z = -Math.sign(x) * 0.35; h.add(strip);
      }
    }
    // 옆 날개 지느러미
    for (const sx of [-1, 1]) {
      const fin = new THREE.Mesh(new RoundedBoxGeometry(0.14, 0.035, 0.06, 2, 0.01), trim);
      fin.position.set(sx * 0.21, 0.33, 0); fin.rotation.z = sx * 0.55; h.add(fin);
      const finGlow = new THREE.Mesh(new RoundedBoxGeometry(0.1, 0.008, 0.064, 1, 0.003), glowMat);
      finGlow.position.set(sx * 0.215, 0.315, 0); finGlow.rotation.z = sx * 0.55; h.add(finGlow);
    }
    // 칼날 발사구
    const nozzle = new THREE.Mesh(new THREE.CylinderGeometry(0.04, 0.05, 0.09, 24), chrome);
    nozzle.position.y = 0.645; h.add(nozzle);
    const nozzleGlow = new THREE.Mesh(new THREE.TorusGeometry(0.042, 0.009, 10, 32), glowMat);
    nozzleGlow.rotation.x = Math.PI / 2; nozzleGlow.position.y = 0.69; h.add(nozzleGlow);
    return h;
  };
  const top = makeHousing(); spinner.add(top);
  const bottom = makeHousing(); bottom.rotation.z = Math.PI; spinner.add(bottom);

  // ── 칼날(위·아래)
  const Y0 = 0.69;
  const bladeCoreMat = new THREE.ShaderMaterial({
    uniforms: { uColor, uT },
    vertexShader: `varying vec3 vN; varying vec3 vV; varying float vY;
      void main(){ vY = position.y; vec4 mv = modelViewMatrix*vec4(position,1.); vN = normalize(normalMatrix*normal); vV = normalize(-mv.xyz); gl_Position = projectionMatrix*mv; }`,
    fragmentShader: `uniform vec3 uColor; uniform float uT; varying vec3 vN; varying vec3 vV; varying float vY;
      void main(){
        float f = pow(1. - abs(dot(vN, vV)), 1.5);                 // 가장자리는 색, 가운데는 흰 심지
        float bands = 0.88 + 0.12 * smoothstep(0.4, 0.5, abs(fract(vY * 5.0) - 0.5)); // 마디(늘어나는 칼날)
        vec3 c = mix(vec3(1.0), uColor * 1.6, f) * bands * (0.92 + 0.08 * sin(uT * 30.0));
        gl_FragColor = vec4(c, 1.0);
      }`,
  });
  const bladeGlowMat = new THREE.ShaderMaterial({
    ...additive, side: THREE.DoubleSide, uniforms: { uColor, uT },
    vertexShader: `varying vec2 vUv; void main(){ vUv = uv; gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }`,
    fragmentShader: `uniform vec3 uColor; uniform float uT; varying vec2 vUv;
      void main(){
        float d = abs(vUv.x - 0.5) * 2.0;
        float ends = smoothstep(0.0, 0.05, vUv.y) * smoothstep(1.0, 0.9, vUv.y);
        float a = pow(max(0.0, 1.0 - d), 2.2) * ends * (0.8 + 0.2 * sin(uT * 11.0));
        gl_FragColor = vec4(uColor * 1.3, a * 0.85);
      }`,
  });
  const blades = [];
  for (const dir of [1, -1]) {
    const pivot = new THREE.Group(); pivot.position.y = dir * Y0; pivot.rotation.z = dir > 0 ? 0 : Math.PI;
    const grow = new THREE.Group(); pivot.add(grow); // scale.y 로 펼치기/접기
    const coreGeo = new THREE.CylinderGeometry(BR * 0.92, BR, BL, 24, 1); coreGeo.translate(0, BL / 2, 0);
    grow.add(new THREE.Mesh(coreGeo, bladeCoreMat));
    const tip = new THREE.Mesh(new THREE.SphereGeometry(BR * 0.92, 16, 10), bladeCoreMat); tip.position.y = BL; grow.add(tip);
    for (const ry of [0, Math.PI / 2]) {
      const halo = new THREE.Mesh(new THREE.PlaneGeometry(BR * 11, BL + 0.14), bladeGlowMat);
      halo.position.y = BL / 2; halo.rotation.y = ry; grow.add(halo);
    }
    spinner.add(pivot);
    blades.push({ pivot, grow });
  }

  // ── 번개(칼날을 감는 전기 아크): 칼날마다 2줄, 0.06초마다 모양이 바뀜
  const ARC_PTS = 26;
  const arcMat = new THREE.LineBasicMaterial({ ...additive, color: 0xffffff, opacity: 0.9 });
  const arcColorMat = new THREE.LineBasicMaterial({ ...additive, color: 0xffffff, opacity: 0.7 });
  const arcs = [];
  for (const b of blades) for (let k = 0; k < 2; k++) {
    const g = new THREE.BufferGeometry(); g.setAttribute("position", new THREE.BufferAttribute(new Float32Array(ARC_PTS * 3), 3));
    const line = new THREE.Line(g, k ? arcColorMat : arcMat); line.frustumCulled = false;
    b.pivot.add(line); arcs.push(line);
  }
  const rebuildArc = (line, len) => {
    const p = line.geometry.attributes.position.array;
    let x = 0, z = 0;
    for (let i = 0; i < ARC_PTS; i++) {
      const y = 0.03 + (i / (ARC_PTS - 1)) * len;
      x += (Math.random() - 0.5) * 0.05; z += (Math.random() - 0.5) * 0.05;
      x = THREE.MathUtils.clamp(x, -0.08, 0.08); z = THREE.MathUtils.clamp(z, -0.08, 0.08);
      p[i * 3] = x; p[i * 3 + 1] = y; p[i * 3 + 2] = z;
    }
    line.geometry.attributes.position.needsUpdate = true;
  };

  // ── 불꽃 스파크(고리 양옆에서 바깥으로)
  const N = 200, sp = new Float32Array(N * 3), sc = new Float32Array(N * 3), sv = new Float32Array(N * 3), sl = new Float32Array(N), sm = new Float32Array(N);
  const sgeo = new THREE.BufferGeometry();
  sgeo.setAttribute("position", new THREE.BufferAttribute(sp, 3)); sgeo.setAttribute("color", new THREE.BufferAttribute(sc, 3));
  const sparks = new THREE.Points(sgeo, new THREE.PointsMaterial({ ...additive, size: 0.05, map: dotTex, vertexColors: true }));
  sparks.frustumCulled = false; spinner.add(sparks);
  for (let i = 0; i < N; i++) { sm[i] = 1; sl[i] = Math.random() * 0.6; sp[i * 3 + 1] = -99; }
  const tmpC = new THREE.Color();

  // ── 회전 궤적 원판 + 고리에서 퍼지는 빛 고리 (고정 프레임)
  const uRot = { value: 0 }, uSpeed = { value: 0 };
  const trail = new THREE.Mesh(
    new THREE.RingGeometry(Y0 * 0.85, Y0 + BL + 0.04, 128, 1),
    new THREE.ShaderMaterial({
      ...additive, side: THREE.DoubleSide, uniforms: { uColor, uRot, uSpeed, uR0: { value: Y0 }, uR1: { value: Y0 + BL } },
      vertexShader: `varying vec2 vP; void main(){ vP = position.xy; gl_Position = projectionMatrix*modelViewMatrix*vec4(position,1.); }`,
      fragmentShader: `uniform vec3 uColor; uniform float uRot, uSpeed, uR0, uR1; varying vec2 vP;
        void main(){
          float r = length(vP);
          float a = atan(vP.y, vP.x) - 1.5707963 - uRot;           // 칼날(+Y) 방향 기준 각도
          float behind = fract(-a / 3.1415927);                      // 칼날 뒤쪽으로 0→1 (쌍검이라 반 바퀴 주기)
          float trailA = pow(1.0 - behind, 6.0) * 0.9 + 0.12;       // 칼날 바로 뒤가 가장 밝음
          float radial = smoothstep(uR0 * 0.9, uR0 + 0.1, r) * (0.35 + 0.65 * smoothstep(uR0, uR1, r)) * smoothstep(uR1 + 0.04, uR1 - 0.04, r);
          gl_FragColor = vec4(uColor * 1.2, trailA * radial * uSpeed * 0.55);
        }`,
    })
  );
  trail.renderOrder = -1; outer.add(trail);
  const pulses = [0, 1, 2].map((i) => {
    const m = new THREE.Mesh(new THREE.RingGeometry(0.96, 1.0, 96), new THREE.MeshBasicMaterial({ ...additive, color: 0xffffff, opacity: 0 }));
    m.userData.phase = i / 3; outer.add(m); return m;
  });

  // ── 상태
  const state = { bladeOn: opt.bladeOn ?? true, ext: 0, spinning: !!opt.spinning, speed: 0, angle: 0, color: new THREE.Color(), arcTimer: 0 };
  const setColor = (c) => {
    state.colorIndex = typeof c === "number" && c >= 0 && c < 24 && Number.isInteger(c) ? c : -1;
    state.color.copy(state.colorIndex >= 0 ? PALETTE[state.colorIndex] : new THREE.Color(c));
    uColor.value.copy(state.color);
    for (const m of glowMats) m.emissive.copy(state.color);
    arcColorMat.color.copy(state.color);
    for (const p of pulses) p.material.color.copy(state.color);
  };
  setColor(opt.color ?? 12);

  const SPIN_MAX = 16; // rad/s (약 2.5바퀴/초)
  outer.userData.api = {
    setSpinning: (on) => { state.spinning = !!on; },
    toggleSpin: () => (state.spinning = !state.spinning),
    setBlade: (on) => { state.bladeOn = !!on; },
    toggleBlade: () => (state.bladeOn = !state.bladeOn),
    setColor,
    nextColor: () => { const i = ((state.colorIndex >= 0 ? state.colorIndex : 11) + 1) % 24; setColor(i); return i; },
    get state() { return { bladeOn: state.bladeOn, spinning: state.spinning, colorIndex: state.colorIndex, extension: state.ext, speed: state.speed }; },
  };
  outer.userData.spinner = spinner;
  outer.userData.length = 2 * (Y0 + BL + 0.03);

  outer.userData.update = (dt, t) => {
    uT.value = t;
    // 칼날 펼치기/접기 (부드럽게, 끝에서 살짝 튕김)
    const target = state.bladeOn ? 1 : 0;
    state.ext += (target - state.ext) * Math.min(1, dt * (state.bladeOn ? 5 : 8));
    const e = state.ext < 0.002 ? 0.0001 : state.ext;
    for (const b of blades) { b.grow.scale.set(1, e, 1); b.grow.visible = state.ext > 0.01; }
    // 회전 (가속/감속)
    const sTarget = state.spinning ? SPIN_MAX : 0;
    state.speed += (sTarget - state.speed) * Math.min(1, dt * (state.spinning ? 1.6 : 1.1));
    state.angle += state.speed * dt;
    spinner.rotation.z = state.angle;
    const sp01 = state.speed / SPIN_MAX;
    uRot.value = state.angle; uSpeed.value = sp01 * state.ext;
    // 빛 고리: 회전 중에 고리 중심에서 퍼져 나감
    for (const p of pulses) {
      const ph = (t * 0.8 + p.userData.phase) % 1;
      p.scale.setScalar(0.25 + ph * (Y0 + BL * 0.6));
      p.material.opacity = sp01 * (1 - ph) * 0.5;
    }
    // 발광 맥동
    for (const m of glowMats) m.emissiveIntensity = 1.8 + 0.5 * Math.sin(t * 5) + sp01 * 0.8;
    // 번개
    state.arcTimer -= dt;
    if (state.arcTimer <= 0) {
      state.arcTimer = 0.05 + Math.random() * 0.05;
      for (const a of arcs) { a.visible = state.ext > 0.3 && Math.random() < 0.75; if (a.visible) rebuildArc(a, BL * state.ext * (0.6 + Math.random() * 0.4)); }
    }
    // 스파크: 고리 양옆에서 옆으로 튐 (회전 중엔 더 많이)
    const rate = 0.6 + sp01 * 1.4;
    for (let i = 0; i < N; i++) {
      sl[i] -= dt * rate;
      if (sl[i] <= 0) {
        sm[i] = sl[i] = 0.3 + Math.random() * 0.5;
        const side = Math.random() < 0.5 ? -1 : 1;
        sp.set([side * (R + 0.06), (Math.random() - 0.5) * 0.12, (Math.random() - 0.5) * 0.06], i * 3);
        sv.set([side * (0.6 + Math.random() * 0.9), (Math.random() - 0.4) * 0.9, (Math.random() - 0.5) * 0.5], i * 3);
      }
      sv[i * 3 + 1] -= 1.5 * dt;
      for (let k = 0; k < 3; k++) sp[i * 3 + k] += sv[i * 3 + k] * dt;
      const f = Math.max(0, sl[i] / sm[i]);
      tmpC.setHSL(0.04 + 0.11 * f, 1, 0.35 + 0.45 * f);
      sc[i * 3] = tmpC.r * f; sc[i * 3 + 1] = tmpC.g * f; sc[i * 3 + 2] = tmpC.b * f;
    }
    sgeo.attributes.position.needsUpdate = true; sgeo.attributes.color.needsUpdate = true;
  };
  return outer;
}
