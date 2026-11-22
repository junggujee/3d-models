"""게임 맵: 사용자 3D 월드(GLB) → 정리·도색·질감 → Unity(City.fbx + 텍스처) + 미리보기.
실행: python build_map.py <3dwolrdnew.glb> [--scale 0.14] [--preview]

1. 정리   : SketchUp 변환기가 남긴 '부품 원본'(맨 위 계층 + 메시를 공유하는 메시) 삭제, 광택(거울처럼 보이던 성벽) 제거
2. 도색   : 흰색(무늬 없는) 면을 위치·면 방향으로 분류해서 칠함
            산 = 아래 잔디 → 가파른 곳·중턱 바위 → 꼭대기 눈 · 성 구역 = 밝은 석벽 + 청회색 슬레이트 지붕 + 포석
            마을 = 따뜻한 회벽 + 붉은 기와(지붕널) + 포석 · 지면(번갈아 칠해져 줄무늬였던 원판) = 잔디 · 나무·수풀의 흰 면 = 잎 초록
3. 질감   : 원본 텍스처(반목조 벽·지붕널·목재·포석·잔디·흙·돌벽·물 등) 재사용 + 색만 바꾼 변형 + 바위·눈 생성.
            바닥·벽·지붕·산은 월드 좌표 상자 투영 UV(어디서나 같은 크기로 반복), 창문 있는 반목조 벽 등은 원래 UV 유지
4. 게임용 : 단위 → 미터(기본 1단위 = 0.14m: 집 높이 약 7m), 바닥 = 0, 성벽 안 중심 = 원점,
            City(보이는 메시) · Foliage(나무·수풀, 충돌 없음) · City_Collision(작은 장식 제외한 충돌용) · PlayerSpawn(마을 광장)
"""
import sys, os, math, argparse, shutil, time
sys.path.insert(0, os.path.dirname(__file__))
import bpy, bmesh, numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

ap = argparse.ArgumentParser(); ap.add_argument("src"); ap.add_argument("--scale", type=float, default=0.14); ap.add_argument("--preview", action="store_true"); ap.add_argument("--cell", type=float, default=48.0, help="최적화 구역 크기(m)")
args = ap.parse_args([a for a in sys.argv[1:] if a != "--"])
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.join(HERE, "..")
OUT_CITY = os.path.join(ROOT, "unity", "Assets", "City"); TEX = os.path.join(OUT_CITY, "Textures"); OUT_EXP = os.path.join(ROOT, "exports"); OUT_PRE = os.path.join(ROOT, "previews")
os.makedirs(TEX, exist_ok=True); os.makedirs(OUT_EXP, exist_ok=True); os.makedirs(OUT_PRE, exist_ok=True)
t0 = time.time(); log = lambda *a: print(f"[map {time.time()-t0:6.1f}s]", *a, flush=True)

# 원본 좌표 기준 영역(위에서 본 렌더로 측정)
GROUND_Z = -51.0; CENTER = (150.0, 0.0); CASTLE = (-195.0, 211.0, 330.0); TOWN = (380.0, -153.0)
GROUND_NAMES = {"그룹#187"}; MOUNTAIN_NAMES = {"외부 쉘#21", "그룹#24", "그룹#21"}; FOLIAGE_PREFIX = ("2D 나무", "수풀")
CASTLE_WORDS = ("성벽", "기둥", "타워")

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=args.src); log("불러옴", len(bpy.data.objects))

# ── 1. 정리 ───────────────────────────────────────────
for o in [o for o in bpy.data.objects if o.type == "MESH" and o.parent is None and o.data.users > 1]: bpy.data.objects.remove(o, do_unlink=True)
meshes = [o for o in bpy.data.objects if o.type == "MESH" and o.data.materials and not (len(o.data.materials) == 1 and o.data.materials[0] and o.data.materials[0].name == "Material")]
for o in [o for o in bpy.data.objects if o.type == "MESH" and o not in meshes]: bpy.data.objects.remove(o, do_unlink=True)
log("부품 원본 삭제 후 메시", len(meshes))

# ── 2. 텍스처 준비(원본 추출 + 색 변형 + 생성) ───────────
from PIL import Image
def save_img(img_np, name):  # float 0..1 HxWx3
    p = os.path.join(TEX, name); Image.fromarray((np.clip(img_np, 0, 1) * 255).astype(np.uint8)).save(p, quality=90); return p
src_img = {}   # 원본 재질 이름 → 텍스처 파일
for m in bpy.data.materials:
    if not m.node_tree: continue
    for n in m.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image:
            key = n.image.name; fn = key + (".png" if n.image.file_format == "PNG" else ".jpg"); p = os.path.join(TEX, fn)
            if not os.path.exists(p):
                n.image.filepath_raw = p; n.image.file_format = "PNG" if fn.endswith(".png") else "JPEG"; n.image.save()
            src_img[m.name] = p
log("원본 텍스처", len(set(src_img.values())))
def load_np(p): return np.asarray(Image.open(p).convert("RGB"), dtype=np.float32) / 255.0
def tint(p, mul, add=0.0, sat=1.0, name=None):
    a = load_np(p); g = a.mean(axis=2, keepdims=True); a = g + (a - g) * sat; return save_img(a * np.array(mul) + add, name)
def tile_noise(n, octaves, seed):
    rng = np.random.default_rng(seed); x = np.arange(n) / n; X, Y = np.meshgrid(x, x); out = np.zeros((n, n))
    for o in range(octaves):
        f = 2 ** (o + 1)
        for _ in range(6):
            kx, ky = rng.integers(-f, f + 1, 2); ph = rng.random() * 6.283; out += np.cos(2 * np.pi * (kx * X + ky * Y) + ph) / (o + 1)
    return (out - out.min()) / (out.max() - out.min())
def M(name): return f"openskp_material_{name}"
shingle, stone, plaster, grass, paving = src_img.get(M(18)), src_img.get(M(5)), src_img.get(M(19)), src_img.get(M(8)), src_img.get(M(15))
def fbm(n, seed, octaves=6):   # 타일 되는 프랙탈 노이즈(값 노이즈 격자를 감싸서 보간)
    rng = np.random.default_rng(seed); out = np.zeros((n, n)); amp = 1.0; tot = 0.0
    for o in range(octaves):
        g = 4 * 2 ** o; grid = rng.random((g, g)); x = np.arange(n) * g / n; i0 = np.floor(x).astype(int); t = x - i0; t = t * t * (3 - 2 * t); i1 = (i0 + 1) % g
        a_ = grid[i0][:, i0] * (1 - t)[None, :] + grid[i0][:, i1] * t[None, :]; b_ = grid[i1][:, i0] * (1 - t)[None, :] + grid[i1][:, i1] * t[None, :]
        out += (a_ * (1 - t)[:, None] + b_ * t[:, None]) * amp; tot += amp; amp *= 0.5
    return out / tot
# ── 질감(기본 텍스처): 바위 = 프랙탈 + 가로 층리 + 갈라진 틈
f1, f2, f3 = fbm(512, 11), fbm(512, 12, 4), fbm(512, 13, 3)
yy = np.arange(512)[:, None] / 512.0
strata = 0.5 + 0.5 * np.sin((yy * 9 + f2 * 1.6) * 2 * np.pi)
cracks = np.clip(1 - np.abs(f3 - 0.5) * 28, 0, 1)
ROCK_BASE = np.clip(0.46 + (f1 - 0.5) * 0.4 + (strata - 0.5) * 0.06 - cracks * 0.08, 0, 1)   # 대비를 낮춰 멀리서 요란하지 않게
def rock_tint(rgb, name): return save_img(ROCK_BASE[..., None] * np.array(rgb), name)
# ── 채색(같은 질감에 색만 다르게)
NEW = {
    # 산: 높이에 따라 이끼 바위 → 회갈색 바위 → 밝은 바위 → 눈
    "Rock_Moss": rock_tint((0.78, 0.92, 0.62), "Rock_Moss.jpg"), "Rock_Gray": rock_tint((1.0, 0.95, 0.88), "Rock_Gray.jpg"),
    "Rock_Light": rock_tint((1.25, 1.22, 1.18), "Rock_Light.jpg"),
    "Mountain_Snow": save_img(np.clip(0.86 + (f1[..., None] - 0.5) * 0.12, 0, 1) * np.array([0.95, 0.97, 1.0]), "Mountain_Snow.jpg"),
    "Mountain_Grass": tint(grass, (0.85, 0.95, 0.7), 0.0, 0.85, "Mountain_Grass.jpg"),
    # 성: 흰 석재 + 청회색 지붕
    "Castle_Stone": tint(stone, (1.12, 1.1, 1.06), 0.05, 0.7, "Castle_Stone.jpg"),
    "Roof_Slate": tint(shingle, (0.75, 0.85, 1.05), 0.03, 0.25, "Roof_Slate.jpg"),
}
# 마을: 회벽 5색 · 지붕 4색(집마다 하나씩 골라 칠함)
WALLS = {"Wall_Cream": (1.04, 0.98, 0.86), "Wall_Ochre": (1.05, 0.88, 0.62), "Wall_Rose": (1.05, 0.86, 0.82), "Wall_Sage": (0.9, 0.97, 0.85), "Wall_White": (1.08, 1.06, 1.02)}
ROOFS = {"Roof_Terracotta": (1.35, 0.72, 0.55), "Roof_Brown": (1.0, 0.75, 0.6), "Roof_Red": (1.3, 0.6, 0.5), "Roof_BlueGray": (0.8, 0.88, 1.0)}
for k, c in WALLS.items(): NEW[k] = tint(plaster, c, 0.0, 1.0, k + ".jpg")
for k, c in ROOFS.items(): NEW[k] = tint(shingle, c, 0.02, 0.8 if "Blue" not in k else 0.3, k + ".jpg")

# 최종 재질 목록
FINAL = []; FIDX = {}
def make_mat(name, tex=None, color=(1, 1, 1), rough=0.9):
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = 0.0; b.inputs["Base Color"].default_value = (*color, 1)
    if "Specular IOR Level" in b.inputs: b.inputs["Specular IOR Level"].default_value = 0.25
    if tex:
        t = m.node_tree.nodes.new("ShaderNodeTexImage"); t.image = bpy.data.images.load(tex, check_existing=True); m.node_tree.links.new(t.outputs["Color"], b.inputs["Base Color"])
    FIDX[name] = len(FINAL); FINAL.append(m); return FIDX[name]
# 원본 재질(텍스처 있으면 그대로, 단색이면 그 색)
for m in list(bpy.data.materials):
    if not m.name.startswith("openskp_material_"): continue
    if m.name in src_img: make_mat("Map_" + m.name.split("_")[-1], src_img[m.name])
    else:
        b = m.node_tree.nodes.get("Principled BSDF") if m.node_tree else None; c = tuple(b.inputs["Base Color"].default_value[:3]) if b else (1, 1, 1); make_mat("Map_" + m.name.split("_")[-1], None, c)
for k, p in NEW.items(): make_mat(k, p)
LEAF = make_mat("Leaf_Green", None, (0.22, 0.42, 0.16)); DARKWOOD = FIDX["Map_13"] if "Map_13" in FIDX else FIDX["Map_17"]
GRASS = FIDX["Map_8"]; PAVE = FIDX["Map_15"]; DIRT = FIDX["Map_10"]; STONE_TEX = FIDX["Map_5"]
BOX_UV = {FIDX[k]: s for k, s in {**{w: 3.0 for w in WALLS}, **{r: 2.0 for r in ROOFS}, "Castle_Stone": 2.5, "Roof_Slate": 2.0, "Mountain_Grass": 10.0, "Rock_Moss": 16.0, "Rock_Gray": 16.0, "Rock_Light": 16.0, "Mountain_Snow": 16.0,
                                       "Map_8": 4.0, "Map_16": 4.0, "Map_10": 5.0, "Map_15": 2.5, "Map_7": 3.0, "Map_9": 3.0, "Map_5": 2.5, "Map_14": 3.0, "Map_19": 3.0}.items() if k in FIDX}   # 재질 → 반복 크기(m)
WHITE = {M(0), M(2)}
log("재질", len(FINAL))

# ── 3. 도색: 면마다 최종 재질 번호 ─────────────────────
for o in meshes:   # 공유 메시를 물체마다 복사(bpy.ops.make_single_user 는 물체 2만 개에서 매우 느림)
    if o.data.users > 1: o.data = o.data.copy()
log("단일 사용자 메시로 분리")
# 면 방향 바로잡기: SketchUp 모델은 뒤집힌 면이 많아(Unity 는 뒷면을 그리지 않아 구멍처럼 보임)
#  ① 부품마다 바깥쪽으로 다시 계산(벽)  ② 수직이 아닌 면(바닥·경사 지붕·평지붕)은 모두 위를 향하게 — 게임에서는 위에서만 보이므로.
flipped = 0
for o in meshes:
    me = o.data
    if len(me.polygons) == 0: continue
    bm = bmesh.new(); bm.from_mesh(me); bmesh.ops.recalc_face_normals(bm, faces=bm.faces); Mz = o.matrix_world.to_3x3()
    for f in bm.faces:
        if (Mz @ f.normal).z < -0.2 * (Mz @ f.normal).length: f.normal_flip(); flipped += 1
    bm.to_mesh(me); bm.free()
log("면 방향 다시 계산(아래를 보던 면", flipped, "개 위로)")

def shape_metrics(o):
    """형태 분석: 면적 가중 수직면 비율, 축 정렬 비율(건물은 벽이 수직이고 면이 x/y/z 축에 맞음, 산·바위는 둘 다 낮음)."""
    me = o.data; n = len(me.polygons)
    nr = np.zeros(n * 3, np.float32); me.polygons.foreach_get("normal", nr); nr = nr.reshape(-1, 3) @ np.array(o.matrix_world.to_3x3()).T; nr /= np.maximum(np.linalg.norm(nr, axis=1, keepdims=True), 1e-9)
    ar = np.zeros(n, np.float32); me.polygons.foreach_get("area", ar); ar = ar / max(ar.sum(), 1e-9)
    vert = float((ar * (np.abs(nr[:, 2]) < 0.15)).sum()); axis = float((ar * (np.abs(nr).max(axis=1) > 0.985)).sum()); return vert, axis, n

REPORT = {"mountain": [], "hill": [], "castle": [], "build": [], "foliage": [], "ground": []}
def category(o, lo, hi):
    base = o.name.split(".")[0]
    if base.startswith(FOLIAGE_PREFIX): return "foliage", None
    if base in GROUND_NAMES: return "ground", None
    vert, axis, n = shape_metrics(o); h = hi.z - lo.z; w = max(hi.x - lo.x, hi.y - lo.y); m = (vert, axis, n, h, w)
    if not any(k in base for k in CASTLE_WORDS) and n >= 40 and axis < 0.3 and vert < 0.35 and h > 25:   # 자연 지형: 수직 벽·축 정렬 면이 적음
        return ("mountain" if h > 250 else "hill"), m
    if any(k in base for k in CASTLE_WORDS) or math.hypot((lo.x + hi.x) / 2 - CASTLE[0], (lo.y + hi.y) / 2 - CASTLE[1]) < CASTLE[2]: return "castle", m
    return "build", m

cats = {}; rng = np.random.default_rng(7)
for o in meshes:
    me = o.data; Mw = o.matrix_world; n = len(me.polygons)
    if n == 0: continue
    ws = [Mw @ Vector(c) for c in o.bound_box]; lo = Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws))); hi = Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws)))
    cat, metric = category(o, lo, hi); cats[o.name] = cat
    if metric is not None: REPORT[cat].append((o.name, metric, (lo.x + hi.x) / 2, (lo.y + hi.y) / 2))
    else: REPORT[cat].append((o.name, None, 0, 0))
    old = [m.name if m else "" for m in me.materials]
    mi = np.zeros(n, np.int32); me.polygons.foreach_get("material_index", mi)
    nr = np.zeros(n * 3, np.float32); me.polygons.foreach_get("normal", nr); nr = nr.reshape(-1, 3) @ np.array(Mw.to_3x3()).T; nr /= np.maximum(np.linalg.norm(nr, axis=1, keepdims=True), 1e-9)
    ce = np.zeros(n * 3, np.float32); me.polygons.foreach_get("center", ce); ce = ce.reshape(-1, 3) @ np.array(Mw.to_3x3()).T + np.array(Mw.translation)
    anz = np.abs(nr[:, 2]); z = ce[:, 2]   # 뒤집힌 면이 많아서 위/아래 대신 기울기(|nz|)로 판정
    out = np.array([FIDX.get("Map_" + old[i].split("_")[-1], FIDX["Map_2"]) if i < len(old) and old[i].startswith("openskp") else FIDX["Map_2"] for i in mi], np.int32)
    white = np.array([i < len(old) and old[i] in WHITE for i in mi])
    # ── 1단계 질감: 표면 종류 정하기 / 2단계 채색: 영역·높이·집마다 색
    if cat == "ground":
        out[:] = GRASS
    elif cat in ("mountain", "hill"):
        zr = (z - lo.z) / max(1.0, hi.z - lo.z) + (rng.random(n) - .5) * .1               # 상대 높이 + 경계를 흐리는 노이즈
        gentle = anz > 0.75
        if cat == "mountain":
            m = np.where(zr > 0.74, FIDX["Mountain_Snow"], np.where(zr > 0.5, FIDX["Rock_Light"], np.where(zr > 0.25, FIDX["Rock_Gray"], np.where(gentle, FIDX["Mountain_Grass"], FIDX["Rock_Moss"]))))
        else:
            m = np.where(gentle & (zr < 0.6), FIDX["Mountain_Grass"], np.where(zr < 0.5, FIDX["Rock_Moss"], FIDX["Rock_Gray"]))
        summit = (anz > 0.97) & (zr > 0.55)                                                # 평평한 꼭대기: 성 대지는 포석, 그 밖은 잔디
        in_castle_zone = math.hypot((lo.x + hi.x) / 2 - CASTLE[0], (lo.y + hi.y) / 2 - CASTLE[1]) < CASTLE[2]
        m = np.where(summit, PAVE if in_castle_zone else FIDX["Mountain_Grass"], m)
        out = np.where(white | (out == FIDX["Map_10"]) | (out == FIDX["Map_3"]) | (out == FIDX["Map_12"]), m, out)
    elif cat == "foliage":
        out = np.where(white, LEAF, out)
    else:
        castle = cat == "castle"; pick = abs(hash(o.name)) 
        wall = FIDX["Castle_Stone"] if castle else FIDX[list(WALLS)[pick % len(WALLS)]]
        roof = FIDX["Roof_Slate"] if castle else FIDX[list(ROOFS)[(pick // 7) % len(ROOFS)]]
        near_ground = z < GROUND_Z + 4; underside = (nr[:, 2] < -0.5) & (z < lo.z + 0.15 * (hi.z - lo.z)) & ~near_ground
        m = np.where(anz > 0.97, np.where(near_ground, PAVE, roof if not castle else FIDX["Castle_Stone"]), np.where(anz > 0.2, roof, wall))
        m = np.where(underside, DARKWOOD, m)
        out = np.where(white, m, out)
        out = np.where(out == FIDX["Map_18"], roof, out)   # 원본 지붕널(UV 가 한 점에 뭉쳐 검게 보이던 면)도 집 지붕색으로
        if castle: out = np.where(out == FIDX["Map_5"], FIDX["Castle_Stone"], out)
    at = me.attributes.get("fmat") or me.attributes.new("fmat", "INT", "FACE"); at.data.foreach_set("value", out)   # 최종 재질 번호는 속성에 저장(재질 슬롯은 합친 뒤 한 번만)
log("도색 완료", {c: sum(1 for v in cats.values() if v == c) for c in set(cats.values())})
def write_report():
    L = ["# 맵 분석 (자동 생성: blender/build_map.py)", "", f"원본: {os.path.basename(args.src)} · 단위 1 = {args.scale} m · 부품 원본 삭제 후 메시 {len(meshes)}개", "",
         "분류 기준(형태): **수직 벽 비율**(면적 가중, |법선 z| < 0.15)과 **축 정렬 비율**(법선이 x/y/z 축과 거의 같은 면). 산·언덕·바위는 둘 다 낮고(축 < 0.3, 수직 < 0.35), 건물은 높음.", "",
         "| 분류 | 물체 수 | 면 수 |", "|---|---|---|"]
    fc = {}
    for o in meshes: fc[cats.get(o.name)] = fc.get(cats.get(o.name), 0) + len(o.data.polygons)
    names = {"mountain": "산", "hill": "언덕·바위", "castle": "성", "build": "마을 건물", "foliage": "나무·수풀", "ground": "지면"}
    for k, v in names.items(): L.append(f"| {v} | {len(REPORT[k])} | {fc.get(k, 0):,} |")
    L += ["", "## 산 (높이 = 원본 단위 × 배율, 미터)", "", "| 물체 | 높이(m) | 폭(m) | 수직 벽 | 축 정렬 | 면 수 | 채색 |", "|---|---|---|---|---|---|---|"]
    for name, (v, a, n, h, w), x, y in sorted(REPORT["mountain"], key=lambda r: -r[1][3]): L.append(f"| {name} | {h*args.scale:.0f} | {w*args.scale:.0f} | {v:.0%} | {a:.0%} | {n:,} | 아래 잔디·이끼 바위 → 회색 바위 → 밝은 바위 → 꼭대기 눈 |")
    L += ["", "## 언덕·바위", "", "| 물체 | 높이(m) | 폭(m) | 수직 벽 | 축 정렬 | 면 수 |", "|---|---|---|---|---|---|"]
    for name, (v, a, n, h, w), x, y in sorted(REPORT["hill"], key=lambda r: -r[1][3]): L.append(f"| {name} | {h*args.scale:.0f} | {w*args.scale:.0f} | {v:.0%} | {a:.0%} | {n:,} |")
    from collections import Counter
    for key, title, paint in (("castle", "성", "흰 석재 벽 · 청회색 슬레이트 지붕 · 석재 바닥"), ("build", "마을 건물", "집마다 회벽 5색(크림·황토·장미·세이지·흰색) 중 하나 + 지붕 4색(테라코타·갈색·적색·청회색) 중 하나, 땅 높이 평면은 포석, 처마 밑은 짙은 목재")):
        cnt = Counter(r[0].split(".")[0] for r in REPORT[key]); L += ["", f"## {title} — {len(REPORT[key])}개", "", f"채색: {paint}", "", "| 부품 이름 | 배치 수 |", "|---|---|"]
        for nm, c in cnt.most_common(15): L.append(f"| {nm} | {c} |")
    p = os.path.join(ROOT, "docs", "map_analysis.md"); os.makedirs(os.path.dirname(p), exist_ok=True); open(p, "w").write("\n".join(L) + "\n"); log("분석 보고서", p)
write_report()

# ── 4. 합치기·단위·위치 ───────────────────────────────
# bpy.ops.object.join 은 물체 2만 개에서 제곱으로 느려져서, 정점·면·UV·재질 번호를 numpy 로 모아 새 메시를 직접 만든다.
GAME = bpy.data.scenes.new("Game"); S = args.scale; DUP = {}
Tm = np.array(Matrix.Diagonal((S, S, S, 1)) @ Matrix.Translation((-CENTER[0], -CENTER[1], -GROUND_Z)))
def merge(objs, name, with_mats=True, double_sided=False, coll=None):
    cos, lvs, lts, uvs, fms = [], [], [], [], []; voff = 0
    for o in objs:
        me = o.data; nv, nl, npy = len(me.vertices), len(me.loops), len(me.polygons)
        if npy == 0: continue
        Mw = Tm @ np.array(o.matrix_world)
        co = np.empty(nv * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3) @ Mw[:3, :3].T + Mw[:3, 3]
        lv = np.empty(nl, np.int32); me.loops.foreach_get("vertex_index", lv); lt = np.empty(npy, np.int32); me.polygons.foreach_get("loop_total", lt)
        uv = np.zeros(nl * 2, np.float32)
        if me.uv_layers.active: me.uv_layers.active.data.foreach_get("uv", uv)
        uv = uv.reshape(-1, 2); fm = np.zeros(npy, np.int32); me.attributes["fmat"].data.foreach_get("value", fm)
        if np.linalg.det(Mw[:3, :3]) < 0:   # 거울 배치된 사본: 면 방향 뒤집기
            ls = np.concatenate([[0], np.cumsum(lt)[:-1]]); idx = np.concatenate([np.arange(st + t - 1, st - 1, -1) for st, t in zip(ls, lt)]); lv, uv = lv[idx], uv[idx]
        cos.append(co); lvs.append(lv + voff); lts.append(lt); uvs.append(uv); fms.append(fm); voff += nv
    co, lv, lt, uv, fm = np.concatenate(cos), np.concatenate(lvs), np.concatenate(lts), np.concatenate(uvs), np.concatenate(fms)
    # 겹친 면 제거: 원본에 같은 부품이 같은 자리에 두 벌씩 놓인 곳이 있어(그룹#115·#117 등) 같은 위치의 면이 두 장 →
    # 렌더러에서는 서로 그림자를 드리워 검게, Unity 에서는 깜빡이는 줄무늬(z-fighting)로 보임. 꼭짓점 위치(5mm 단위)가 같은 면은 하나만 남김.
    ls = np.concatenate([[0], np.cumsum(lt)[:-1]]); q = np.round(co / 0.005).astype(np.int64)
    seen = set(); keep = np.ones(len(lt), bool)
    for i in range(len(lt)):
        key = tuple(sorted(map(tuple, q[lv[ls[i]:ls[i] + lt[i]]].tolist())))
        if key in seen: keep[i] = False
        else: seen.add(key)
    if not keep.all():
        lkeep = np.repeat(keep, lt); lv, uv, lt, fm = lv[lkeep], uv[lkeep], lt[keep], fm[keep]
    DUP[name] = int((~keep).sum())
    if double_sided:   # 평면 나무: 겹친 앞뒷면을 지웠으니 뒤집은 사본을 붙여 확실한 양면으로(Unity 는 뒷면을 그리지 않음)
        ls = np.concatenate([[0], np.cumsum(lt)[:-1]]); idx = np.concatenate([np.arange(st + t - 1, st - 1, -1) for st, t in zip(ls, lt)])
        lv, uv, lt, fm = np.concatenate([lv, lv[idx]]), np.concatenate([uv, uv[idx]]), np.concatenate([lt, lt]), np.concatenate([fm, fm])
    me = bpy.data.meshes.new(name); me.vertices.add(len(co)); me.vertices.foreach_set("co", co.ravel())
    me.loops.add(len(lv)); me.loops.foreach_set("vertex_index", lv); me.polygons.add(len(lt)); me.polygons.foreach_set("loop_start", np.concatenate([[0], np.cumsum(lt)[:-1]]).astype(np.int32))
    me.update(calc_edges=True); uvl = me.uv_layers.new(name="UVMap"); uvl.data.foreach_set("uv", uv.ravel())
    if with_mats:
        for m in FINAL: me.materials.append(m)
        me.polygons.foreach_set("material_index", fm)
    me.update(); ob = bpy.data.objects.new(name, me); (coll or GAME.collection).objects.link(ob); return ob
fol = [o for o in meshes if cats.get(o.name) == "foliage"]; rest = [o for o in meshes if cats.get(o.name) != "foliage"]
def diag(o):
    ws = [o.matrix_world @ Vector(c) for c in o.bound_box]; return (Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws))) - Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws)))).length
city = merge(rest, "City"); foliage = merge(fol, "Foliage", double_sided=True) if fol else None
collision = merge([o for o in rest if diag(o) >= 30], "City_Collision", with_mats=False)   # 작은 장식(대각 30단위 미만)·나무 제외
log("합침", len(city.data.polygons), "면 / 나무", len(foliage.data.polygons) if foliage else 0, "/ 충돌", len(collision.data.polygons), "/ 겹친 면 제거", DUP)

# ── 5. 상자 투영 UV(월드 좌표, 미터 기준 반복 크기) ─────
def box_uv(obj):
    me = obj.data
    if not me.uv_layers: me.uv_layers.new(name="UVMap")
    uvl = me.uv_layers.active; nP = len(me.polygons); nL = len(me.loops)
    if nP == 0: return 0
    mi = np.zeros(nP, np.int32); me.polygons.foreach_get("material_index", mi)
    nr = np.zeros(nP * 3, np.float32); me.polygons.foreach_get("normal", nr); nr = nr.reshape(-1, 3)
    lt = np.zeros(nP, np.int32); me.polygons.foreach_get("loop_total", lt)
    lv = np.zeros(nL, np.int32); me.loops.foreach_get("vertex_index", lv); co = np.zeros(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
    face_of_loop = np.repeat(np.arange(nP), lt)
    uv = np.zeros(nL * 2, np.float32); uvl.data.foreach_get("uv", uv); uv = uv.reshape(-1, 2)
    tile = np.zeros(len(FINAL), np.float32)
    for k, s_ in BOX_UV.items(): tile[k] = s_
    ft = tile[mi][face_of_loop]; sel = ft > 0; ax = np.argmax(np.abs(nr), axis=1)[face_of_loop]; p = co[lv]
    u = np.where(ax == 0, p[:, 1], p[:, 0]); v = np.where(ax == 2, p[:, 1], p[:, 2])
    uv[sel, 0] = (u / np.maximum(ft, 1e-6))[sel]; uv[sel, 1] = (v / np.maximum(ft, 1e-6))[sel]
    uvl.data.foreach_set("uv", uv.ravel()); me.update(); return int(sel.sum())
log("상자 투영 UV", box_uv(city), "루프")

# ── 6. 시작 위치(마을 광장 근처의 빈 바닥) ───────────
bm = bmesh.new(); bm.from_mesh(collision.data); tree = BVHTree.FromBMesh(bm); bm.free()
tx, ty = (TOWN[0] - CENTER[0]) * S, (TOWN[1] - CENTER[1]) * S; spawn = None
def free(x, y):
    hit = tree.ray_cast(Vector((x, y, 200)), Vector((0, 0, -1)))
    if hit[0] is None or hit[1].z < 0.8 or hit[0].z > 2.0: return None          # 지붕 위 제외(땅 높이 근처만)
    g = hit[0]
    for dx, dy in ((0, 0), (1.5, 0), (-1.5, 0), (0, 1.5), (0, -1.5)):
        if tree.ray_cast(g + Vector((dx, dy, 0.3)), Vector((0, 0, 1)), 4.0)[0] is not None: return None
        dn = tree.ray_cast(Vector((x + dx, y + dy, g.z + 1)), Vector((0, 0, -1)), 2.0)
        if dn[0] is None or abs(dn[0].z - g.z) > 0.4: return None
    return g
for r in np.arange(0, 60, 1.5):
    for a in range(0, 360, 15 if r else 360):
        g = free(tx + r * math.cos(math.radians(a)), ty + r * math.sin(math.radians(a)))
        if g is not None: spawn = g; break
    if spawn is not None: break
sp = bpy.data.objects.new("PlayerSpawn", None); sp.location = spawn if spawn is not None else Vector((tx, ty, 0.5)); GAME.collection.objects.link(sp)
log("시작 위치", tuple(round(c, 1) for c in sp.location), "자동" if spawn is not None else "기본값")

# ── 7. 최적화(Unity 용): 구역 · LOD · 구역별 충돌 ─────────
# 가까이서 보이는 모습(LOD0)은 원본 그대로. 원래 부품(건물 하나·산 하나) 단위로 48m 칸에 배정해서 건물이 칸 경계에서 잘리지 않음.
#   World ─ Terrain · Castle · Town · Foliage(칸마다 *_LOD0/1/2 → Unity 가 LOD Group 자동 생성) · Collision(칸마다 충돌 메시)
#         └ Props · Interiors · Decor(빈 자리: 이후 소품·인테리어를 여기에 넣고 메뉴 "맵 정리" 를 누르면 같은 규칙 적용) · PlayerSpawn
OPT = bpy.data.scenes.new("Optimized"); CELL = args.cell; STATS = {}
def empty(name, parent=None, loc=(0, 0, 0)):
    e = bpy.data.objects.new(name, None); e.location = loc; OPT.collection.objects.link(e)
    if parent: e.parent = parent
    return e
def gcenter(o):
    ws = [o.matrix_world @ Vector(c) for c in o.bound_box]; c = sum(ws, Vector()) / 8; g = Tm @ np.array([c.x, c.y, c.z, 1.0]); return g[0], g[1]
def cell_key(o): x, y = gcenter(o); return int(math.floor(x / CELL)), int(math.floor(y / CELL))
def mesh_arrays(me):
    nv, nl, npy = len(me.vertices), len(me.loops), len(me.polygons)
    co = np.empty(nv * 3, np.float32); me.vertices.foreach_get("co", co); lv = np.empty(nl, np.int32); me.loops.foreach_get("vertex_index", lv)
    lt = np.empty(npy, np.int32); me.polygons.foreach_get("loop_total", lt); mi = np.empty(npy, np.int32); me.polygons.foreach_get("material_index", mi)
    uv = np.zeros(nl * 2, np.float32)
    if me.uv_layers.active: me.uv_layers.active.data.foreach_get("uv", uv)
    return co.reshape(-1, 3), lv, lt, uv.reshape(-1, 2), mi
def build_mesh(name, co, lv, lt, uv, mi, mats, parent):
    used, inv = np.unique(lv, return_inverse=True); co = co[used]; lv = inv.astype(np.int32).ravel()   # 안 쓰는 정점 정리
    me = bpy.data.meshes.new(name); me.vertices.add(len(co)); me.vertices.foreach_set("co", co.ravel())
    me.loops.add(len(lv)); me.loops.foreach_set("vertex_index", lv); me.polygons.add(len(lt)); me.polygons.foreach_set("loop_start", (np.cumsum(lt) - lt).astype(np.int32))   # 면이 0개여도 안전
    me.update(calc_edges=True); me.uv_layers.new(name="UVMap").data.foreach_set("uv", uv.ravel())
    for m in mats: me.materials.append(m)
    me.polygons.foreach_set("material_index", mi); me.update()
    ob = bpy.data.objects.new(name, me); OPT.collection.objects.link(ob); ob.parent = parent; return ob
def strip_small(src, name, min_diag, parent, thin_below=0.0):
    """작은 장식 덩어리(창틀·등불·울타리 조각 등, 연결된 면 묶음의 크기 < min_diag m)를 통째로 뺀 사본 — 큰 면에 구멍이 나지 않음."""
    co, lv, lt, uv, mi = mesh_arrays(src.data); nF = len(lt); ls = np.concatenate([[0], np.cumsum(lt)[:-1]])
    if nF == 0: return build_mesh(name, co, lv, lt, uv, mi, list(src.data.materials), parent)
    # 원본(SketchUp)은 면마다 꼭짓점이 따로라서, 번호가 아니라 '위치'(5mm)가 같은 꼭짓점을 같은 점으로 보고 연결 덩어리를 찾음
    _, pid = np.unique(np.round(co / 0.005).astype(np.int64), axis=0, return_inverse=True); pid = pid.ravel(); plv = pid[lv]
    lab = np.arange(pid.max() + 1)                                 # 위치 묶음 번호(최솟값 전파 = 연결 요소)
    for _ in range(400):
        fmin = np.minimum.reduceat(lab[plv], ls); new = lab.copy(); np.minimum.at(new, plv, np.repeat(fmin, lt))
        new = new[new]                                             # 경로 압축(빨리 수렴)
        if np.array_equal(new, lab): break
        lab = new
    comp = lab[plv[ls]]; uniq, ci = np.unique(comp, return_inverse=True)
    lo = np.full((len(uniq), 3), np.inf, np.float32); hi = np.full((len(uniq), 3), -np.inf, np.float32); vc = np.repeat(ci, lt)
    np.minimum.at(lo, vc, co[lv]); np.maximum.at(hi, vc, co[lv]); size = np.linalg.norm(hi - lo, axis=1); keepc = size >= min_diag
    if thin_below > 0: keepc &= (size >= thin_below) | (np.arange(len(uniq)) % 2 == 0)   # 이보다 작은 덩어리는 절반만(성기게)
    keep = keepc[ci]
    if keep.all(): keep = keep   # 그대로
    lk = np.repeat(keep, lt)
    return build_mesh(name, co, lv[lk], lt[keep], uv[lk], mi[keep], list(src.data.materials), parent)
def decimate(ob, kind, value):
    m = ob.modifiers.new("D", "DECIMATE")
    if kind == "planar": m.decimate_type = "DISSOLVE"; m.angle_limit = math.radians(value); m.delimit = {"MATERIAL", "UV"}
    else: m.decimate_type = "COLLAPSE"; m.ratio = value
    with bpy.context.temp_override(scene=OPT, view_layer=OPT.view_layers[0], object=ob, active_object=ob, selected_objects=[ob]):
        bpy.ops.object.modifier_apply(modifier=m.name)
def weld(ob, dist=0.001):
    """같은 위치(1mm)의 꼭짓점을 하나로: 보이는 모습은 같고(UV·재질은 면 모서리마다 그대로) 꼭짓점이 줄어 파일이 작고 LOD 가 깔끔해짐."""
    bm = bmesh.new(); bm.from_mesh(ob.data); n0 = len(bm.verts); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=dist); bm.to_mesh(ob.data); bm.free(); ob.data.update()
    STATS.setdefault("_weld", [0, 0]); STATS["_weld"][0] += n0; STATS["_weld"][1] += len(ob.data.vertices)
def copy_obj(src, name, parent):
    ob = bpy.data.objects.new(name, src.data.copy()); OPT.collection.objects.link(ob); ob.parent = parent; return ob
# LOD 규칙: (LOD1, LOD2) — 건물 = 작은 덩어리 빼기 + 같은 평면 합치기, 산·언덕 = 형태를 유지하며 줄이기, 나무 = 작은 수풀 빼기(LOD2 없음 → 멀면 사라짐)
RULES = {
    "Town":    (lambda b, n, p: (decimate(o := strip_small(b, n, 0.6, p), "planar", 4) or o), lambda b, n, p: (decimate(o := strip_small(b, n, 2.5, p), "planar", 10) or o)),
    "Castle":  (lambda b, n, p: (decimate(o := strip_small(b, n, 0.6, p), "planar", 4) or o), lambda b, n, p: (decimate(o := strip_small(b, n, 2.5, p), "planar", 10) or o)),
    "Terrain": (lambda b, n, p: (decimate(o := copy_obj(b, n, p), "collapse", 0.45) or o), lambda b, n, p: (decimate(o := copy_obj(b, n, p), "collapse", 0.15) or o)),
    "Foliage": (lambda b, n, p: strip_small(b, n, 1.2, p, thin_below=6.0), None),   # 작은 수풀 빼고, 6m 미만 나무는 절반만
}
world = empty("World"); groups = {k: empty(k, world) for k in ("Terrain", "Castle", "Town", "Foliage", "Collision", "Props", "Interiors", "Decor")}
def gcenter_obj(ob):   # 구역 부모(빈 물체)의 자식 메시 중심 x
    for c in ob.children:
        if c.type == "MESH" and len(c.data.vertices): co = np.empty(len(c.data.vertices) * 3, np.float32); c.data.vertices.foreach_get("co", co); return float(co.reshape(-1, 3)[:, 0].mean())
    return 0.0
def chunks(objs, cat):
    out = {}
    for o in objs:
        c = cats.get(o.name)
        if cat == "Terrain" and c == "mountain": key = "M" + o.name.replace("#", "").replace(".", "_")     # 큰 산은 한 덩어리씩
        elif cat == "Terrain" and c == "ground": key = "Ground"
        else: kx, ky = cell_key(o); key = f"c{kx:+d}_{ky:+d}".replace("+", "p").replace("-", "m")
        out.setdefault(key, []).append(o)
    return out
CAT_OF = {"mountain": "Terrain", "hill": "Terrain", "ground": "Terrain", "castle": "Castle", "build": "Town", "foliage": "Foliage"}
for cat in ("Terrain", "Castle", "Town", "Foliage"):
    objs = [o for o in meshes if CAT_OF.get(cats.get(o.name)) == cat]; f0 = f1 = f2 = 0; n = 0
    for key, part in sorted(chunks(objs, cat).items()):
        name = f"{cat}_{key}"; parent = empty(name, groups[cat])
        lod0 = merge(part, name + ("" if key == "Ground" else "_LOD0"), double_sided=(cat == "Foliage"), coll=OPT.collection); lod0.parent = parent
        if len(lod0.data.polygons) == 0: continue
        box_uv(lod0); weld(lod0); n += 1; f0 += len(lod0.data.polygons)
        if key == "Ground": continue                                     # 평평한 바닥은 LOD 없음
        r1, r2 = RULES[cat]
        l1 = r1(lod0, name + "_LOD1", parent); f1 += len(l1.data.polygons)
        if r2: l2 = r2(lod0, name + "_LOD2", parent); f2 += len(l2.data.polygons)
    STATS[cat] = (n, f0, f1, f2); log("최적화", cat, f"구역 {n}개 · LOD0 {f0:,} · LOD1 {f1:,} · LOD2 {f2:,} 면")
nc = 0
for key, part in sorted(chunks([o for o in rest if diag(o) >= 30], "Collision").items()):
    c = merge(part, f"Collision_{key}", with_mats=False, coll=OPT.collection); c.parent = groups["Collision"]; nc += len(c.data.polygons)
STATS["Collision"] = (len(groups["Collision"].children), nc, 0, 0); log("최적화 충돌", STATS["Collision"])
sp2 = empty("PlayerSpawn", world, sp.location)

# ── 8. 내보내기: Unity = 최적화 장면을 분류별 FBX 여러 개로(GitHub 파일 한도 100MB · 일부만 다시 만들기 쉬움) · 도구(월드맵 등) = 합친 장면(GLB) ──
# City_Terrain · City_Castle · City_TownW/E(마을은 커서 서·동 반으로) · City_Foliage · City_Collision(+PlayerSpawn). Unity 메뉴 4번이 World 아래에 모아 배치.
old = os.path.join(OUT_CITY, "City.fbx")
if os.path.exists(old): os.remove(old)
tw, te = empty("TownW"), empty("TownE")                                   # 마을: 서·동 반(이름이 Town 으로 시작 → MapTools 가 마을 규칙)
for ch in list(groups["Town"].children): ch.parent = tw if gcenter_obj(ch) < 0 else te
sp2.parent = groups["Collision"]                                          # 시작 위치는 충돌 파일에
FILES = {"City_Terrain": groups["Terrain"], "City_Castle": groups["Castle"], "City_TownW": tw, "City_TownE": te, "City_Foliage": groups["Foliage"], "City_Collision": groups["Collision"]}
def subtree(o): out = [o]; [out.extend(subtree(c)) for c in o.children]; return out
for fname, root in FILES.items():
    objs = subtree(root); root_parent = root.parent; root.parent = None
    for o in OPT.objects: o.select_set(o in objs)
    with bpy.context.temp_override(scene=OPT, view_layer=OPT.view_layers[0], selected_objects=objs):
        bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_CITY, fname + ".fbx"), use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", path_mode="RELATIVE",
                                 axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
    root.parent = root_parent; log("내보냄", fname, f"{os.path.getsize(os.path.join(OUT_CITY, fname + '.fbx')) / 1e6:.1f} MB")
ctx = dict(scene=GAME, view_layer=GAME.view_layers[0])
with bpy.context.temp_override(**ctx):
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT_EXP, "City.glb"), export_format="GLB", use_active_scene=True)
with open(os.path.join(ROOT, "docs", "map_optimization.md"), "w") as f:
    f.write("# 맵 최적화 결과 (자동 생성: blender/build_map.py)\n\n| 분류 | 구역 | LOD0 면 | LOD1 면 | LOD2 면 |\n|---|---|---|---|---|\n")
    for k, v in STATS.items():
        if k.startswith("_"): continue
        n_, a, b_, c_ = v; f.write(f"| {k} | {n_} | {a:,} | {b_:,} | {c_:,} |\n")
    if "_weld" in STATS: f.write(f"\n꼭짓점 합치기(1mm, LOD0): {STATS['_weld'][0]:,} → {STATS['_weld'][1]:,}\n")
co = np.empty(len(city.data.vertices) * 3, np.float32); city.data.vertices.foreach_get("co", co); co = co.reshape(-1, 3); lo, hi = co.min(0), co.max(0)
log("내보냄", f"크기 {hi[0]-lo[0]:.0f} x {hi[1]-lo[1]:.0f} x {hi[2]-lo[2]:.0f} m")

# ── 9. 미리보기 ───────────────────────────────────────
if args.preview:
    sc = GAME; sc.render.engine = "CYCLES"; sc.cycles.samples = 16; sc.cycles.device = "CPU"; sc.render.resolution_x = 1400; sc.render.resolution_y = 860
    collision.hide_render = True
    w = bpy.data.worlds.new("Sky"); w.use_nodes = True; bg = w.node_tree.nodes["Background"]; sky = w.node_tree.nodes.new("ShaderNodeTexSky")
    try: sky.sky_type = "NISHITA"
    except Exception: pass
    w.node_tree.links.new(sky.outputs["Color"], bg.inputs["Color"]); bg.inputs["Strength"].default_value = 0.35; sc.world = w
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.5; sun.rotation_euler = (0.9, 0.15, 0.8); sc.collection.objects.link(sun)
    sc.view_settings.view_transform = "AgX" if "AgX" in [v.identifier for v in sc.view_settings.bl_rna.properties["view_transform"].enum_items] else "Filmic"
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.clip_end = 5000
    def shot(name, loc, tgt, lens):
        cam.location = loc; cam.rotation_euler = (Vector(tgt) - Vector(loc)).to_track_quat("-Z", "Y").to_euler(); cam.data.lens = lens; sc.render.filepath = os.path.join(OUT_PRE, name); bpy.ops.render.render(write_still=True, scene=sc.name)
    shot("map_overview.png", (-250, -380, 230), (20, 10, 0), 26)
    shot("map_town.png", (sp.location.x - 40, sp.location.y - 55, 32), (sp.location.x + 10, sp.location.y + 10, 3), 24)
    shot("map_castle.png", ((CASTLE[0] - CENTER[0]) * S + 70, (CASTLE[1] - CENTER[1]) * S - 90, 40), ((CASTLE[0] - CENTER[0]) * S, (CASTLE[1] - CENTER[1]) * S, 20), 30)
    log("미리보기 완료")
