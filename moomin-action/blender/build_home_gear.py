"""집에서 볼 수 있는 물건으로 만든 장비 5종 — 민미 모델 공간(기본 자세)에서 만들어 뼈마다 따로 내보냄(EquipmentVisuals 가 bindpose 로 붙임).
  이불 로브(가슴)  : 누빔 이불(마름모 누빔 두 색)이 등·옆을 덮는 망토 + 머리 뒤 두건            → Robe_Body@hips · Robe_Hood@head
  각티슈 방패(아티팩트): 꽃무늬 띠 각티슈 상자를 왼팔 바깥에 — 위로 휴지 한 장이 삐죽          → TissueShield@arm.L
  수면 모자(머리)  : 남색 고깔 + 크림색 접단 + 흰 방울, 뒤로 처짐(귀는 모자 위로 쏙)           → NightCap@head
  수면 바지(다리)  : 하늘색 체크 반바지(몸 아래쪽을 감쌈) + 허리 고무줄                         → PajamaShorts@hips
  털 슬리퍼(발)    : 분홍 털 슬리퍼 + 발등 방울                                                  → Slipper_L@leg.L · Slipper_R@leg.R
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

# ── 수면 모자: 머리 꼭대기 고깔(뒤로 처짐) + 접단 + 방울
NAVY, CUFF, POM = mat("Cap_Navy", (0.12, 0.17, 0.42), rough=0.9, sheen=0.5), mat("Cap_Cuff", (0.95, 0.91, 0.8), rough=1.0, sheen=0.8), mat("Cap_Pom", (1, 1, 1), rough=1.0, sheen=1.0)
TH0 = 0.78
def cap(u, v):
    phi = 2 * math.pi * u; base = surf(HEAD_C, HEAD_R, phi, TH0, 0.02)
    tip = Vector((0, 2.75, -0.55)); mid = Vector((0, 2.45, 0.05))
    b = (1 - v) ** 2 * base + 2 * (1 - v) * v * (mid + (base - Vector((0, base.y, 0))) * 0.35) + v * v * tip   # 굽은 고깔
    return b
capm = obj("NightCap", sheet(cap, 28, 14, lambda i, j: 0, close_u=True), [NAVY, CUFF, POM])
ring = [T(*surf(HEAD_C, HEAD_R, 2 * math.pi * k / 40, TH0 - 0.05, 0.05)) for k in range(41)] + [T(*surf(HEAD_C, HEAD_R, 2 * math.pi / 40, TH0 - 0.05, 0.05))]
tb, _ = tube(ring, lambda t: 0.055 * K, sides=10, mat_idx=1); cuff = obj("Cuff", tb, [NAVY, CUFF, POM])
pom = obj("Pom", ball((0, 2.75, -0.55), (0.1, 0.1, 0.1), 2, fuzz=0.01), [NAVY, CUFF, POM])
capm = merge([capm, cuff, pom], "NightCap"); PARTS["cap_sleep"] = [(capm, "head")]

# ── 수면 바지: 몸 아래쪽 체크 반바지 + 허리 고무줄
PJ1, PJ2, PJW = mat("Pajama_Blue", (0.42, 0.62, 0.9), rough=0.95, sheen=0.5), mat("Pajama_Light", (0.78, 0.87, 0.98), rough=0.95, sheen=0.5), mat("Pajama_Band", (0.95, 0.95, 0.97), rough=0.9)
shorts = obj("PajamaShorts", sheet(lambda u, v: surf(BODY_C, BODY_R, 2 * math.pi * u, -0.98 + 0.62 * v, 0.045), 40, 10, lambda i, j: (i // 2 + j // 2) % 2, close_u=True), [PJ1, PJ2, PJW]); solid(shorts, 0.025)
band = [T(*surf(BODY_C, BODY_R, 2 * math.pi * k / 48, -0.36, 0.06)) for k in range(49)] + [T(*surf(BODY_C, BODY_R, 2 * math.pi / 48, -0.36, 0.06))]
tb, _ = tube(band, lambda t: 0.03 * K, sides=8, mat_idx=2); bandm = obj("Band", tb, [PJ1, PJ2, PJW])
shorts = merge([shorts, bandm], "PajamaShorts"); PARTS["pants_pajama"] = [(shorts, "hips")]

# ── 털 슬리퍼: 발 조각 아래쪽을 감싸는 털 껍데기 + 털 테두리 + 발등 방울
FUZ, FPOM = mat("Slipper_Pink", (0.98, 0.66, 0.76), rough=1.0, sheen=1.0), mat("Slipper_Pom", (1, 0.92, 0.95), rough=1.0, sheen=1.0)
sl = []
for s, side in ((1, "L"), (-1, "R")):
    Cc, Rr = LEG[s]
    shell = obj(f"Slipper_{side}", sheet(lambda u, v, Cc=Cc, Rr=Rr: surf(Cc, Rr, 2 * math.pi * u, -1.3 + 1.3 * v, 0.04), 28, 10, lambda i, j: 0, close_u=True), [FUZ, FPOM]); solid(shell, 0.03)
    rim = [T(*surf(Cc, Rr, 2 * math.pi * k / 32, 0.0, 0.06)) for k in range(33)] + [T(*surf(Cc, Rr, 2 * math.pi / 32, 0.0, 0.06))]
    tb, _ = tube(rim, lambda t: 0.04 * K, sides=10, mat_idx=0); rimo = obj("Rim", tb, [FUZ, FPOM])
    pm = obj("Pom", ball(tuple(surf(Cc, Rr, 0.0, 0.35, 0.09)), (0.07, 0.07, 0.07), 1, fuzz=0.008), [FUZ, FPOM])
    sl.append((merge([shell, rimo, pm], f"Slipper_{side}"), f"leg.{side}"))
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
