// 민미(무민) 엎드린 인형 — 코드로 만든 3D 모델 (외부 3D 파일 없음, 글씨·로고 없음)
// 좌표: Y = 위, +Z = 앞(얼굴), 원점 = 바닥 중앙. 움직임은 userData.update(dt, t) 로 모델이 스스로 처리.
import * as THREE from "three";
import { RoomEnvironment } from "three/addons/environments/RoomEnvironment.js";

export function applyEnvironment(renderer, scene) {
  const pm = new THREE.PMREMGenerator(renderer);
  scene.environment = pm.fromScene(new RoomEnvironment(), 0.04).texture;
  pm.dispose();
}

// ── 털 가닥 텍스처 (점마다 길이가 다른 털). 촘촘하고 부드러운 복슬 털
const furTex = (() => {
  const N = 256, c = document.createElement("canvas"); c.width = c.height = N;
  const x = c.getContext("2d"), img = x.createImageData(N, N);
  for (let i = 0; i < N * N; i++) {
    const v = Math.random() < 0.6 ? Math.floor(Math.pow(Math.random(), 0.55) * 255) : 0;
    img.data[i * 4] = img.data[i * 4 + 1] = img.data[i * 4 + 2] = v; img.data[i * 4 + 3] = 255;
  }
  x.putImageData(img, 0, 0);
  const t = new THREE.CanvasTexture(c);
  t.wrapS = t.wrapT = THREE.RepeatWrapping; t.repeat.set(14, 9);
  t.magFilter = THREE.NearestFilter; t.minFilter = THREE.NearestFilter; t.generateMipmaps = false;
  return t;
})();

export const FUR = 0.05; // 털 길이

// mesh 위에 털 껍질(shell)을 여러 겹 쌓아 입체적인 복슬복슬 털을 만듦
function addFur(mesh, length = FUR, layers = 14, tint = [1, 1, 1]) {
  const base = mesh.geometry, pos = base.attributes.position, nrm = base.attributes.normal;
  for (let i = 1; i <= layers; i++) {
    const h = i / layers, g = base.clone(), p = g.attributes.position;
    const droop = -0.3 * h * h * length; // 털 끝이 살짝 처짐
    for (let k = 0; k < p.count; k++) {
      p.setXYZ(k, pos.getX(k) + nrm.getX(k) * length * h, pos.getY(k) + nrm.getY(k) * length * h + droop, pos.getZ(k) + nrm.getZ(k) * length * h);
    }
    const s = 0.92 + 0.08 * h; // 털 뿌리는 살짝 어둡게(입체감)
    const m = new THREE.MeshStandardMaterial({
      color: new THREE.Color(s * tint[0], s * tint[1], s * tint[2]), roughness: 1, alphaMap: furTex, alphaTest: 0.08 + h * 0.85,
    });
    const shell = new THREE.Mesh(g, m); shell.renderOrder = i; mesh.add(shell);
  }
}

// ── 눈 텍스처: 부드러운 수놓은 실 느낌(광택 없음, 가장자리 번짐)
function softDisc(draw) {
  const c = document.createElement("canvas"); c.width = c.height = 256;
  const x = c.getContext("2d"); draw(x, 256);
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4; return t;
}
const feather = (x, S, inner) => { // 가장자리를 부드럽게 지움
  x.globalCompositeOperation = "destination-in";
  const g = x.createRadialGradient(S / 2, S / 2, S * inner, S / 2, S / 2, S / 2);
  g.addColorStop(0, "#fff"); g.addColorStop(1, "rgba(255,255,255,0)");
  x.fillStyle = g; x.fillRect(0, 0, S, S); x.globalCompositeOperation = "source-over";
};
const eyeTex = softDisc((x, S) => {
  const g = x.createRadialGradient(S / 2, S / 2, 0, S / 2, S / 2, S * 0.46);
  g.addColorStop(0, "#1f5fb5"); g.addColorStop(0.75, "#2f78cc"); g.addColorStop(1, "#4b93df");
  x.fillStyle = g; x.beginPath(); x.arc(S / 2, S / 2, S * 0.46, 0, 7); x.fill();
  x.lineWidth = 1.6; // 자수 실 결
  for (let i = 0; i < 260; i++) {
    const a = (i / 260) * Math.PI * 2, r0 = S * 0.04, r1 = S * 0.46;
    x.strokeStyle = i % 2 ? "rgba(255,255,255,.10)" : "rgba(10,40,110,.12)";
    x.beginPath(); x.moveTo(S / 2 + Math.cos(a) * r0, S / 2 + Math.sin(a) * r0); x.lineTo(S / 2 + Math.cos(a) * r1, S / 2 + Math.sin(a) * r1); x.stroke();
  }
  feather(x, S, 0.42);
});
const ringTex = softDisc((x, S) => {
  const g = x.createRadialGradient(S / 2, S / 2, S * 0.25, S / 2, S / 2, S * 0.5);
  g.addColorStop(0, "#f8f6f1"); g.addColorStop(0.8, "#f3efe8"); g.addColorStop(1, "#e2ddd3");
  x.fillStyle = g; x.beginPath(); x.arc(S / 2, S / 2, S * 0.5, 0, 7); x.fill();
  x.strokeStyle = "rgba(150,140,125,.55)"; x.lineWidth = 3; x.beginPath(); x.arc(S / 2, S / 2, S * 0.455, 0, 7); x.stroke(); // 얇은 윤곽 박음선
  feather(x, S, 0.44);
});

const ell = (rx, ry, rz, ws = 56, hs = 40) => { const g = new THREE.SphereGeometry(1, ws, hs); g.scale(rx, ry, rz); g.computeVertexNormals(); return g; };

/**
 * @param {object} [opt]
 * @param {"lying"|"standing"} [opt.pose="lying"]  엎드린 인형 / 서 있는 모습(사진 2)
 * @param {number} [opt.furLayers=14]  털 겹 수(낮추면 가벼워짐)
 */
export function createMoomin(opt = {}) {
  const pose = opt.pose === "standing" ? "standing" : "lying";
  const ST = pose === "standing";
  const L = opt.furLayers ?? 14;
  const root = new THREE.Group();
  const skin = new THREE.MeshPhysicalMaterial({ color: 0xf6f4ef, roughness: 1, sheen: 1, sheenColor: new THREE.Color(0xffffff), sheenRoughness: 0.6 });
  const fuzzy = (geo, layers = L, len = FUR) => { const m = new THREE.Mesh(geo, skin); addFur(m, len, layers); return m; };

  // ── 자세별 치수 (머리 중심 HC, 머리 반지름 HR)
  const HC = ST ? new THREE.Vector3(0, 1.62, 0) : new THREE.Vector3(0, 0.33, 0.5);
  const HR = ST ? new THREE.Vector3(0.56, 0.5, 0.5) : new THREE.Vector3(0.54, 0.33, 0.58);

  // ── 몸통
  const torso = new THREE.Group(); root.add(torso);
  const arms = [];
  if (ST) {
    const body = fuzzy(ell(0.52, 0.78, 0.46)); body.position.set(0, 0.8, -0.02); torso.add(body);
    for (const s of [-1, 1]) { // 짧은 다리
      const leg = fuzzy(ell(0.17, 0.14, 0.26, 36, 24), 10); leg.position.set(0.27 * s, 0.14, 0.2); torso.add(leg);
      // 두 팔을 들어 볼에 대고 있는 모습(사진 2)
      const arm = new THREE.Group(); arm.position.set(0.52 * s, 1.1, 0.05);
      const ag = ell(0.15, 0.34, 0.15, 32, 24); ag.translate(0, 0.3, 0);
      arm.add(fuzzy(ag, 12)); arm.rotation.set(0.45, 0, -0.22 * s); arm.userData.base = arm.rotation.clone();
      root.add(arm); arms.push(arm);
    }
  } else {
    const body = fuzzy(ell(0.44, 0.35, 0.6)); body.position.set(0, 0.35, -0.38); torso.add(body);
    for (const s of [-1, 1]) {
      const leg = fuzzy(ell(0.15, 0.17, 0.26, 36, 24), 10); leg.position.set(0.33 * s, 0.2, -0.62); leg.rotation.set(0, 0.35 * s, 0); torso.add(leg);
      const arm = new THREE.Group(); arm.position.set(0.46 * s, 0.2, 0.25);
      const a = fuzzy(ell(0.13, 0.15, 0.27, 36, 24), 12); a.position.z = 0.12; arm.add(a);
      arm.rotation.set(0, -0.2 * s, 0); arm.userData.base = arm.rotation.clone(); root.add(arm); arms.push(arm);
    }
  }

  // ── 머리: 큼직한 타원체 + 넓은 주둥이. 목 pivot에서 끄덕/갸웃
  const head = new THREE.Group(); head.position.set(0, ST ? 1.2 : 0.3, ST ? 0 : 0.3); root.add(head);
  const skull = fuzzy(ell(HR.x, HR.y, HR.z, 64, 44), L + 2); skull.position.copy(HC).sub(head.position); head.add(skull);
  const snout = fuzzy(ell(ST ? 0.44 : 0.4, ST ? 0.27 : 0.22, ST ? 0.3 : 0.3, 48, 32));
  snout.position.set(0, ST ? 1.42 : 0.22, ST ? 0.3 : 0.95).sub(head.position); head.add(snout);

  // 머리 표면의 점/법선 (방위각 phi: +Z에서 +X 쪽, 고도 th)
  const onHead = (phi, th, off = 0.0) => {
    const d = new THREE.Vector3(Math.cos(th) * Math.sin(phi), Math.sin(th), Math.cos(th) * Math.cos(phi));
    const p = HC.clone().add(d.clone().multiply(HR));
    const n = new THREE.Vector3(d.x / HR.x, d.y / HR.y, d.z / HR.z).normalize();
    return { p: p.add(n.clone().multiplyScalar(off)), n };
  };
  const place = (obj, { p, n }) => { obj.position.copy(p).sub(head.position); obj.lookAt(obj.position.clone().add(n)); };

  // ── 눈: 흰 테두리 + 파란 자수(광택 없이 부드럽게). 눈 사이가 넓고 둥근 세로 타원 (사진 1·2)
  const EYE = ST ? { phi: 0.4, th: 0.1 } : { phi: 0.66, th: 0.6 };
  const BROW = ST ? { phi: 0.42, th: 0.55 } : { phi: 0.68, th: 1.0 };
  const eyes = [];
  for (const s of [-1, 1]) {
    const g = new THREE.Group(); place(g, onHead(EYE.phi * s, EYE.th, FUR * 0.9));
    const ring = new THREE.Mesh(new THREE.PlaneGeometry(0.235, 0.28), new THREE.MeshStandardMaterial({ map: ringTex, transparent: true, roughness: 1, depthWrite: false }));
    const blue = new THREE.Mesh(new THREE.PlaneGeometry(0.165, 0.185), new THREE.MeshStandardMaterial({ map: eyeTex, transparent: true, roughness: 1, depthWrite: false }));
    blue.position.z = 0.004; ring.renderOrder = 40; blue.renderOrder = 41;
    g.add(ring, blue); head.add(g); eyes.push(g);
    // 눈썹: 가늘고 긴 갈색 곡선, 바깥쪽이 아래로 처짐
    const bg = new THREE.Group(); place(bg, onHead(BROW.phi * s, BROW.th, FUR * 0.95));
    const curve = new THREE.CatmullRomCurve3([new THREE.Vector3(-0.065 * s, 0.012, 0), new THREE.Vector3(0, 0.02, 0), new THREE.Vector3(0.07 * s, -0.025, 0)]);
    bg.add(new THREE.Mesh(new THREE.TubeGeometry(curve, 14, 0.0055, 6), new THREE.MeshStandardMaterial({ color: 0x7a4034, roughness: 1 })));
    head.add(bg);
  }

  // ── 귀: 머리 위 양끝에 달린 둥근 뿔 모양 (털 포함)
  const ears = [];
  for (const s of [-1, 1]) {
    const eg = ell(0.085, 0.12, 0.07, 28, 20); eg.translate(0, 0.08, 0); eg.computeVertexNormals();
    const ear = fuzzy(eg, 10, 0.03);
    const pt = ST ? onHead(1.2 * s, 1.0, -0.02) : onHead(1.1 * s, 0.95, -0.02);
    ear.position.copy(pt.p).sub(head.position);
    ear.userData.base = new THREE.Euler(ST ? -0.1 : -0.25, 0, -0.4 * s); ear.rotation.copy(ear.userData.base); head.add(ear); ears.push(ear);
  }

  // ── 꼬리: 가늘게 뻗다가 위로 말림 (끝이 점점 가늘어지고 끝에 털뭉치)
  const tail = new THREE.Group(); tail.position.set(0, ST ? 0.35 : 0.34, ST ? -0.42 : -0.9); root.add(tail);
  {
    const pts = ST ? [[0, 0, 0], [0, 0.02, -0.12], [0, 0.1, -0.22], [0, 0.22, -0.26]] : [[0, 0, 0], [0, 0.0, -0.2], [0.02, 0.06, -0.4], [0.04, 0.22, -0.52], [0.0, 0.42, -0.5], [-0.05, 0.56, -0.4]];
    const path = new THREE.CatmullRomCurve3(pts.map((a) => new THREE.Vector3(...a)));
    const TS = 36, RS = 10, tg = new THREE.TubeGeometry(path, TS, 1, RS, false), p = tg.attributes.position;
    for (let i = 0; i <= TS; i++) {
      const c = path.getPointAt(i / TS), r = 0.055 - 0.032 * (i / TS);
      for (let j = 0; j <= RS; j++) { const k = i * (RS + 1) + j; p.setXYZ(k, c.x + (p.getX(k) - c.x) * r, c.y + (p.getY(k) - c.y) * r, c.z + (p.getZ(k) - c.z) * r); }
    }
    tg.computeVertexNormals();
    const t = new THREE.Mesh(tg, skin); addFur(t, 0.022, 9); tail.add(t);
    const tip = fuzzy(ell(0.03, 0.04, 0.03, 20, 14), 6, 0.02); tip.position.copy(path.getPointAt(1)); tail.add(tip);
  }

  // ── 애니메이션: 숨쉬기 · 머리 갸웃 · 눈 깜빡 · 귀 쫑긋 · 꼬리 살랑 · 팔 꼼지락
  let nextBlink = 2.2;
  root.userData.pose = pose;
  root.userData.update = (dt, t) => {
    const br = Math.sin(t * 1.8);
    torso.scale.set(1 + 0.008 * br, 1 + 0.02 * br, 1 + 0.006 * br); torso.position.y = -0.007 * br;
    head.rotation.set(0.03 * Math.sin(t * 1.8 + 0.5), 0.1 * Math.sin(t * 0.7), 0.05 * Math.sin(t * 0.9 + 1));
    if (t > nextBlink) { // 깜빡임 0.18초
      const k = (t - nextBlink) / 0.18, v = k < 1 ? Math.abs(1 - 2 * k) : 1;
      eyes.forEach((e) => (e.scale.y = Math.max(0.08, v)));
      if (k >= 1) { eyes.forEach((e) => (e.scale.y = 1)); nextBlink = t + 2.5 + Math.random() * 3; }
    }
    ears.forEach((e, i) => (e.rotation.x = e.userData.base.x + 0.05 * Math.sin(t * 2.4 + i * 2)));
    tail.rotation.y = 0.35 * Math.sin(t * 2.2); tail.rotation.z = 0.06 * Math.sin(t * 2.2 + 1);
    arms.forEach((a, i) => {
      if (ST) a.rotation.x = a.userData.base.x + 0.08 * Math.sin(t * 1.5 + i * 1.7);
      else a.rotation.y = a.userData.base.y + 0.08 * Math.sin(t * 1.5 + i * 1.7);
    });
  };
  return root;
}
