"""집에서 볼 수 있는 물건으로 만든 장비 5종 — 민미 모델 공간(기본 자세)에서 만들어 뼈마다 따로 내보냄(EquipmentVisuals 가 bindpose 로 붙임).
  이불 로브(가슴)  : 누빔 이불(마름모 누빔 두 색)이 등·옆을 덮는 망토 + 머리 뒤 두건            → Robe_Body@hips · Robe_Hood@head
  각티슈 방패(아티팩트): 꽃무늬 띠 각티슈 상자를 왼팔 바깥에 — 위로 휴지 한 장이 삐죽          → TissueShield@arm.L
  솜털 밀집모자(머리): 두 색 밀짚 챙 + 둥근 꼭지 + 꼭지 둘레 솜털 뭉치 띠(귀는 꼭지 옆으로 쏙) → StrawHat@head
  수면 바지(다리)  : 하늘색 체크 반바지(몸 아래쪽을 감쌈) + 허리 고무줄                         → PajamaShorts@hips
  토끼 털 슬리퍼(발): 하얀 토끼털 + 발등 토끼 얼굴(긴 귀·분홍 속귀·구슬 눈·코) + 솜꼬리          → Slipper_L@leg.L · Slipper_R@leg.R
출력: unity/Assets/Resources/Equipment/*.fbx · Resources/ItemIcons/<id>.png · previews/home_gear_*.png
실행: <bpy 파이썬> blender/build_home_gear.py"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
EQ = os.path.join(ROOT, "unity", "Assets", "Resources", "Equipment"); ICONS = os.path.join(ROOT, "unity", "Assets", "Resources", "ItemIcons"); PRE = os.path.join(ROOT, "previews")
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "exports", "Moomin.blend"))
sc = bpy.context.scene
for o in list(bpy.data.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
rig = bpy.data.objects["MoominRig"]; rig.data.pose_position = "REST"
if rig.animation_data: rig.animation_data.action = None
moomin = bpy.data.objects["Moomin"]

def surf(Cc, Rr, phi, th, off):
    """three 좌표 타원체 표면점(phi 0 = 정면 +z, th 위아래) + 법선 방향 off."""
    d = Vector((math.cos(th) * math.sin(phi), math.sin(th), math.cos(th) * math.cos(phi)))
    p = Vector(Cc) + Vector((d.x * Rr[0], d.y * Rr[1], d.z * Rr[2])); n = Vector((d.x / Rr[0], d.y / Rr[1], d.z / Rr[2])).normalized()
    return p + n * off
def sheet(fn, nu, nv, mat_fn, close_u=False):
    """(u, v) 격자 면 — fn(u, v) 는 three 좌표 점, mat_fn(i, j) 는 재질 번호."""
    bm = bmesh.new(); vs = [[bm.verts.new(T(*fn(i / nu, j / nv))) for j in range(nv + 1)] for i in range(nu + (0 if close_u else 1))]
    nI = nu if close_u else nu
    for i in range(nI):
        a, b = vs[i], vs[(i + 1) % len(vs)]
        for j in range(nv):
            f = bm.faces.new((a[j], b[j], b[j + 1], a[j + 1])); f.material_index = mat_fn(i, j); f.smooth = True
    return bm
def solid(ob, t):
    m = ob.modifiers.new("S", "SOLIDIFY"); m.thickness = t; m.offset = 0; m.use_rim = True
    dg = bpy.context.evaluated_depsgraph_get(); me = bpy.data.meshes.new_from_object(ob.evaluated_get(dg)); ob.modifiers.clear(); ob.data = me; return ob
def obj(name, bm, mats):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m_ in mats: me.materials.append(m_)
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); return o
def merge(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]; bpy.ops.object.join(); o = bpy.context.view_layer.objects.active; o.name = name; o.data.name = name; return o
def ball(c, r, mat_idx, seg=20, rings=12, fuzz=0.0):
    bm = ellipsoid(c, r, seg=seg, rings=rings, fuzz=fuzz, mat_idx=mat_idx, seed=hash(str(c)) % 97); return bm

BODY_C, BODY_R = (0, 0.8, -0.02), (0.52, 0.78, 0.46)
HEAD_C, HEAD_R = (0, 1.62, 0), (0.56, 0.5, 0.5)
LEG = {1: ((0.27, 0.14, 0.2), (0.17, 0.14, 0.26)), -1: ((-0.27, 0.14, 0.2), (0.17, 0.14, 0.26))}
PARTS = {}   # 아이템 id → [(물체, 뼈)]

# ── 이불 로브: 등·옆을 덮는 누빔 망토(앞은 트임) + 머리 뒤 두건
QA, QB = mat("Robe_Cream", (0.93, 0.89, 0.8), rough=0.95, sheen=0.6), mat("Robe_Lilac", (0.66, 0.6, 0.85), rough=0.95, sheen=0.6)
quilt = lambda i, j: (i // 3 + j // 3) % 2
robe = obj("Robe_Body", sheet(lambda u, v: surf(BODY_C, BODY_R, math.radians(48 + 264 * u), -0.95 + 1.75 * v, 0.07 + 0.03 * math.sin(math.pi * v)), 36, 18, quilt), [QA, QB]); solid(robe, 0.03)
hood = obj("Robe_Hood", sheet(lambda u, v: surf(HEAD_C, HEAD_R, math.radians(115 + 130 * u), -0.55 + 1.35 * v, 0.07 + 0.05 * math.sin(math.pi * u)), 18, 12, quilt), [QA, QB]); solid(hood, 0.03)
PARTS["robe_blanket"] = [(robe, "hips"), (hood, "head")]

# ── 각티슈 방패: 왼팔 바깥(+x)에 세운 상자, 휴지가 위로
WHITE, BAND, TISSUE = mat("Tissue_Box", (0.96, 0.95, 0.92), rough=0.6), mat("Tissue_Band", (0.95, 0.55, 0.62), rough=0.6), mat("Tissue_Paper", (1, 1, 1), rough=0.9)
bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
cx, cy, cz, sx, sy, sz = 0.84, 0.92, 0.14, 0.18, 0.5, 0.32
for v in bm.verts: v.co = T(cx + v.co.x * sx, cy + v.co.y * sy, cz + v.co.z * sz)
bmesh.ops.bevel(bm, geom=list(bm.edges), offset=0.012, segments=2, affect="EDGES")
for f in bm.faces:
    c3 = f.calc_center_median(); yy = c3.z / K; f.material_index = 1 if abs(yy - cy) < 0.07 else 0; f.smooth = False
box = obj("TissueShield", bm, [WHITE, BAND, TISSUE])
tis = obj("Tissue", sheet(lambda u, v: (cx + 0.05 * math.sin(u * 6) * v, cy + sy / 2 + 0.16 * v, cz - 0.1 + 0.2 * u + 0.04 * math.sin(v * 5)), 8, 6, lambda i, j: 2), [WHITE, BAND, TISSUE]); solid(tis, 0.004)
box = merge([box, tis], "TissueShield"); PARTS["shield_tissue"] = [(box, "arm.L")]

# ── 솜털 밀집모자: 밀짚 챙(두 색 밀짚 고리가 번갈아, 앞이 살짝 들림) + 둥근 꼭지 + 꼭지 둘레 솜털 뭉치 띠 + 챙 위 솜털 몇 개(귀는 꼭지 옆으로 쏙)
STRAW_A, STRAW_B, COTTON = mat("Straw_Light", (0.88, 0.74, 0.44), rough=0.85), mat("Straw_Dark", (0.74, 0.58, 0.3), rough=0.9), mat("Hat_Cotton", (1, 0.99, 0.97), rough=1.0, sheen=1.0)
HY, TILT = 2.0, -0.12                                   # 챙 높이(three) · 앞이 들리게 기울임(라디안)
def tilt(p):
    q = Vector(p) - Vector((0, HY, 0)); c, s_ = math.cos(TILT), math.sin(TILT)
    return Vector((q.x, q.y * c - q.z * s_, q.y * s_ + q.z * c)) + Vector((0, HY, 0))
def brim(u, v):
    a = 2 * math.pi * u; r = 0.3 + 0.52 * v
    y = HY + 0.03 * (1 - v) - 0.05 * v * v + 0.015 * math.sin(a * 7) * v   # 바깥으로 갈수록 살짝 처지고 물결
    return tilt((math.sin(a) * r * 1.05, y, math.cos(a) * r))
def crown(u, v):
    a = 2 * math.pi * u; r = 0.32 * math.cos(v * math.pi / 2) ** 0.45 + 0.01
    return tilt((math.sin(a) * r * 1.05, HY + 0.24 * math.sin(v * math.pi / 2), math.cos(a) * r))
hb = obj("StrawHat", sheet(brim, 40, 12, lambda i, j: j % 2, close_u=True), [STRAW_A, STRAW_B, COTTON]); solid(hb, 0.02)
hc = obj("Crown", sheet(crown, 32, 10, lambda i, j: (j + 1) % 2, close_u=True), [STRAW_A, STRAW_B, COTTON]); solid(hc, 0.02)
puffs = []
for k in range(14):   # 꼭지 둘레 솜털 띠
    a = 2 * math.pi * k / 14; c = tilt((math.sin(a) * 0.335, HY + 0.05, math.cos(a) * 0.32))
    puffs.append(obj("Puff", ball(tuple(c), (0.065, 0.06, 0.065), 2, fuzz=0.012), [STRAW_A, STRAW_B, COTTON]))
for a, r in ((0.5, 0.62), (2.3, 0.7), (4.1, 0.58), (5.4, 0.66)):   # 챙 위에 몇 개
    c = tilt((math.sin(a) * r, HY + 0.03, math.cos(a) * r)); puffs.append(obj("Puff", ball(tuple(c), (0.055, 0.045, 0.055), 2, fuzz=0.012), [STRAW_A, STRAW_B, COTTON]))
hat = merge([hb, hc] + puffs, "StrawHat"); PARTS["cap_sleep"] = [(hat, "head")]   # 아이템 id 는 예전 그대로(저장 호환)

# ── 수면 바지: 몸 아래쪽 체크 반바지 + 허리 고무줄
PJ1, PJ2, PJW = mat("Pajama_Blue", (0.42, 0.62, 0.9), rough=0.95, sheen=0.5), mat("Pajama_Light", (0.78, 0.87, 0.98), rough=0.95, sheen=0.5), mat("Pajama_Band", (0.95, 0.95, 0.97), rough=0.9)
shorts = obj("PajamaShorts", sheet(lambda u, v: surf(BODY_C, BODY_R, 2 * math.pi * u, -0.98 + 0.62 * v, 0.045), 40, 10, lambda i, j: (i // 2 + j // 2) % 2, close_u=True), [PJ1, PJ2, PJW]); solid(shorts, 0.025)
band = [T(*surf(BODY_C, BODY_R, 2 * math.pi * k / 48, -0.36, 0.06)) for k in range(49)] + [T(*surf(BODY_C, BODY_R, 2 * math.pi / 48, -0.36, 0.06))]
tb, _ = tube(band, lambda t: 0.03 * K, sides=8, mat_idx=2); bandm = obj("Band", tb, [PJ1, PJ2, PJW])
shorts = merge([shorts, bandm], "PajamaShorts"); PARTS["pants_pajama"] = [(shorts, "hips")]

# ── 토끼 털 슬리퍼: 하얀 토끼털 껍데기 + 털 테두리, 발등에 토끼 얼굴(긴 귀 두 개·분홍 속귀·까만 구슬 눈·분홍 코) + 뒤꿈치 솜꼬리
FUZ, PINK, BEAD = mat("Bunny_Fur", (0.97, 0.96, 0.95), rough=1.0, sheen=1.0), mat("Bunny_Pink", (0.98, 0.62, 0.72), rough=0.9, sheen=0.6), mat("Bunny_Eye", (0.03, 0.03, 0.04), rough=0.2)
BM = [FUZ, PINK, BEAD]
sl = []
for s, side in ((1, "L"), (-1, "R")):
    Cc, Rr = LEG[s]
    shell = obj(f"Slipper_{side}", sheet(lambda u, v, Cc=Cc, Rr=Rr: surf(Cc, Rr, 2 * math.pi * u, -1.3 + 1.3 * v, 0.045), 28, 10, lambda i, j: 0, close_u=True), BM); solid(shell, 0.035)
    rim = [T(*surf(Cc, Rr, 2 * math.pi * k / 32, 0.0, 0.065)) for k in range(33)] + [T(*surf(Cc, Rr, 2 * math.pi / 32, 0.0, 0.065))]
    tb, _ = tube(rim, lambda t: 0.045 * K, sides=10, mat_idx=0); rimo = obj("Rim", tb, BM)
    parts_ = [shell, rimo]
    face = surf(Cc, Rr, 0.0, 0.3, 0.1)   # 발등 앞(토끼 얼굴 자리)
    parts_.append(obj("Head", ball(tuple(face), (0.11, 0.09, 0.09), 0, fuzz=0.012), BM))
    for e in (-1, 1):   # 긴 귀: 뒤로 눕고 바깥으로 벌어짐(분홍 속귀)
        base = Vector(face) + Vector((e * 0.05, 0.08, -0.02)); tip = base + Vector((e * 0.06, 0.22, -0.12))
        mid = (base + tip) / 2; L = (tip - base).length; d = (tip - base).normalized()
        rx = -math.atan2(d.z, d.y); rz = -math.atan2(d.x, d.y)
        bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=1.0)
        for vv in bm.verts: vv.co = Vector((vv.co.x * 0.045, vv.co.y * 0.022, vv.co.z * L / 2))   # 납작한 긴 타원(z = 귀 길이)
        Mr = Matrix.Rotation(rx, 3, "X") @ Matrix.Rotation(rz, 3, "Z")
        for vv in bm.verts: q = Vector((vv.co.x, vv.co.z, vv.co.y)); q = Mr @ q; vv.co = T(*(mid + q))
        for f in bm.faces: f.material_index = 0; f.smooth = True
        parts_.append(obj("Ear", bm, BM))
        bi = bmesh.new(); bmesh.ops.create_uvsphere(bi, u_segments=12, v_segments=8, radius=1.0)
        for vv in bi.verts: vv.co = Vector((vv.co.x * 0.025, vv.co.y * 0.012, vv.co.z * L / 2 * 0.75))
        for vv in bi.verts: q = Vector((vv.co.x, vv.co.z, vv.co.y)); q = Mr @ q; vv.co = T(*(mid + q + Vector((0, 0, 0.018))))
        for f in bi.faces: f.material_index = 1; f.smooth = True
        parts_.append(obj("EarIn", bi, BM))
        parts_.append(obj("Eye", ball(tuple(Vector(face) + Vector((e * 0.045, 0.02, 0.08))), (0.018, 0.018, 0.012), 2), BM))
    parts_.append(obj("Nose", ball(tuple(Vector(face) + Vector((0, -0.02, 0.095))), (0.022, 0.016, 0.015), 1), BM))
    parts_.append(obj("Tail", ball(tuple(surf(Cc, Rr, math.pi, 0.1, 0.09)), (0.065, 0.065, 0.065), 0, fuzz=0.014), BM))
    sl.append((merge(parts_, f"Slipper_{side}"), f"leg.{side}"))
PARTS["slippers_fuzzy"] = sl

# ── 내보내기(부품마다 FBX)
os.makedirs(EQ, exist_ok=True)
for item, parts in PARTS.items():
    for o, bone in parts:
        for p in o.data.polygons: p.use_smooth = True
        bpy.ops.object.select_all(action="DESELECT"); o.select_set(True); bpy.context.view_layer.objects.active = o
        bpy.ops.export_scene.fbx(filepath=os.path.join(EQ, o.name + ".fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_anim=False)
        print("✔", o.name, "@", bone, len(o.data.vertices))

# ── 렌더: 아이콘(아이템만) · 착용 모습(앞·뒤)
sc.render.engine = "CYCLES"; sc.cycles.samples = 40; sc.cycles.use_denoising = True; sc.view_settings.view_transform = "AgX"
w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.75, 0.82, 0.92, 1); sc.world = w
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.4; sun.rotation_euler = (0.8, 0.15, 0.7); sc.collection.objects.link(sun)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
headband = None
def shoot(path, loc, target, size, transparent, show):
    allp = [o for parts in PARTS.values() for o, _ in parts]
    for o in allp + [moomin]: o.hide_render = o not in show
    cam.location = loc; cam.data.lens = 50; cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    sc.render.film_transparent = transparent; sc.render.resolution_x = sc.render.resolution_y = size; sc.render.image_settings.color_mode = "RGBA" if transparent else "RGB"
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)
def center(objs):
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]; mn = Vector([min(p[k] for p in pts) for k in range(3)]); mx = Vector([max(p[k] for p in pts) for k in range(3)]); return (mn + mx) / 2, (mx - mn).length
os.makedirs(ICONS, exist_ok=True); os.makedirs(PRE, exist_ok=True)
for item, parts in PARTS.items():
    objs = [o for o, _ in parts]; c, size = center(objs); d = Vector((0.75, -1.0, 0.55)).normalized()
    if item == "robe_blanket": d = Vector((-0.8, 1.0, 0.45)).normalized()   # 로브는 등 쪽에서
    shoot(os.path.join(ICONS, item + ".png"), c + d * size * 1.9, c, 256, True, objs)
allp = [o for parts in PARTS.values() for o, _ in parts]
shoot(os.path.join(PRE, "home_gear_front.png"), Vector((1.3, -3.2, 1.3)), Vector((0, 0, 0.72)), 640, False, allp + [moomin])
shoot(os.path.join(PRE, "home_gear_back.png"), Vector((-1.5, 3.0, 1.4)), Vector((0, 0, 0.72)), 640, False, allp + [moomin])
print("✔ renders")
