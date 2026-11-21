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

ap = argparse.ArgumentParser(); ap.add_argument("src"); ap.add_argument("--scale", type=float, default=0.14); ap.add_argument("--preview", action="store_true")
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
GAME = bpy.data.scenes.new("Game"); S = args.scale
Tm = np.array(Matrix.Diagonal((S, S, S, 1)) @ Matrix.Translation((-CENTER[0], -CENTER[1], -GROUND_Z)))
def merge(objs, name, with_mats=True):
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
    me = bpy.data.meshes.new(name); me.vertices.add(len(co)); me.vertices.foreach_set("co", co.ravel())
    me.loops.add(len(lv)); me.loops.foreach_set("vertex_index", lv); me.polygons.add(len(lt)); me.polygons.foreach_set("loop_start", np.concatenate([[0], np.cumsum(lt)[:-1]]).astype(np.int32))
    me.update(calc_edges=True); uvl = me.uv_layers.new(name="UVMap"); uvl.data.foreach_set("uv", uv.ravel())
    if with_mats:
        for m in FINAL: me.materials.append(m)
        me.polygons.foreach_set("material_index", fm)
    me.update(); ob = bpy.data.objects.new(name, me); GAME.collection.objects.link(ob); return ob
fol = [o for o in meshes if cats.get(o.name) == "foliage"]; rest = [o for o in meshes if cats.get(o.name) != "foliage"]
def diag(o):
    ws = [o.matrix_world @ Vector(c) for c in o.bound_box]; return (Vector((max(w.x for w in ws), max(w.y for w in ws), max(w.z for w in ws))) - Vector((min(w.x for w in ws), min(w.y for w in ws), min(w.z for w in ws)))).length
city = merge(rest, "City"); foliage = merge(fol, "Foliage") if fol else None
collision = merge([o for o in rest if diag(o) >= 30], "City_Collision", with_mats=False)   # 작은 장식(대각 30단위 미만)·나무 제외
log("합침", len(city.data.polygons), "면 / 나무", len(foliage.data.polygons) if foliage else 0, "/ 충돌", len(collision.data.polygons))

# ── 5. 상자 투영 UV(월드 좌표, 미터 기준 반복 크기) ─────
me = city.data
if not me.uv_layers: me.uv_layers.new(name="UVMap")
uvl = me.uv_layers.active; nP = len(me.polygons); nL = len(me.loops)
mi = np.zeros(nP, np.int32); me.polygons.foreach_get("material_index", mi)
nr = np.zeros(nP * 3, np.float32); me.polygons.foreach_get("normal", nr); nr = nr.reshape(-1, 3)
ls = np.zeros(nP, np.int32); me.polygons.foreach_get("loop_start", ls); lt = np.zeros(nP, np.int32); me.polygons.foreach_get("loop_total", lt)
lv = np.zeros(nL, np.int32); me.loops.foreach_get("vertex_index", lv); co = np.zeros(len(me.vertices) * 3, np.float32); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)
face_of_loop = np.repeat(np.arange(nP), lt)
uv = np.zeros(nL * 2, np.float32); uvl.data.foreach_get("uv", uv); uv = uv.reshape(-1, 2)
tile = np.zeros(len(FINAL), np.float32)
for k, s in BOX_UV.items(): tile[k] = s
ft = tile[mi][face_of_loop]; sel = ft > 0; ax = np.argmax(np.abs(nr), axis=1)[face_of_loop]; p = co[lv]
u = np.where(ax == 0, p[:, 1], p[:, 0]); v = np.where(ax == 2, p[:, 1], p[:, 2])
uv[sel, 0] = (u / np.maximum(ft, 1e-6))[sel]; uv[sel, 1] = (v / np.maximum(ft, 1e-6))[sel]
uvl.data.foreach_set("uv", uv.ravel()); me.update(); log("상자 투영 UV", int(sel.sum()), "루프")

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

# ── 7. 내보내기(Game 장면만) ───────────────────────────
ctx = dict(scene=GAME, view_layer=GAME.view_layers[0])
with bpy.context.temp_override(**ctx):
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_CITY, "City.fbx"), use_selection=False, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", path_mode="RELATIVE",
                             axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
    bpy.ops.export_scene.gltf(filepath=os.path.join(OUT_EXP, "City.glb"), export_format="GLB", use_active_scene=True)
co = np.empty(len(city.data.vertices) * 3, np.float32); city.data.vertices.foreach_get("co", co); co = co.reshape(-1, 3); lo, hi = co.min(0), co.max(0)
log("내보냄", f"크기 {hi[0]-lo[0]:.0f} x {hi[1]-lo[1]:.0f} x {hi[2]-lo[2]:.0f} m")

# ── 8. 미리보기 ───────────────────────────────────────
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
