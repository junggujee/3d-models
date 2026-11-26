"""아이템 아이콘(3D 일러스트) — 그림이 없는 아이템마다 작은 3D 소품을 만들어 투명 배경 256px 로 렌더.
출력: unity/Assets/Resources/ItemIcons/<id>.png (ItemIcons.Get 이 이 그림을 먼저 씀) · previews/item_icons_sheet.png(전체 모음)
이미 그림이 있는 것(장비·레이저 블레이더 등, 다른 build_* 가 만듦)은 건드리지 않음. --all 이면 여기 있는 것은 다시 그림.
스타일: Cycles · AgX · 3/4 위에서 · 정사영 자동 맞춤 · 해 + 부드러운 보조광 · 얇은 선(Freestyle 없이 윤곽은 재질 대비로).
실행: <bpy 파이썬> blender/render_item_icons.py [--all] [--only id,id]"""
import sys, os, math
import bpy, bmesh
from mathutils import Vector, Matrix, Euler
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
ICONS = os.path.join(ROOT, "unity", "Assets", "Resources", "ItemIcons"); PRE = os.path.join(ROOT, "previews")
os.makedirs(ICONS, exist_ok=True); os.makedirs(PRE, exist_ok=True)
ALL = "--all" in sys.argv
ONLY = None
if "--only" in sys.argv: ONLY = set(sys.argv[sys.argv.index("--only") + 1].split(","))

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = "CYCLES"; sc.cycles.samples = 48; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True
sc.view_settings.view_transform = "Standard"   # 색이 바래지 않게(AgX 는 밝은 색을 하얗게 날림)
sc.render.film_transparent = True; sc.render.resolution_x = sc.render.resolution_y = 256; sc.render.image_settings.color_mode = "RGBA"
w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.78, 0.82, 0.9, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.9; sc.world = w
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 2.4; sun.data.angle = 0.25; sun.rotation_euler = (0.75, 0.25, 0.55); sc.collection.objects.link(sun)
fill = bpy.data.objects.new("Fill", bpy.data.lights.new("Fill", "AREA")); fill.data.energy = 150; fill.data.size = 4; fill.location = (-3, -2, 2.5); sc.collection.objects.link(fill)
fill.rotation_euler = (Vector((0, 0, 0)) - fill.location).to_track_quat("-Z", "Y").to_euler()
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.type = "ORTHO"

MATS = {}
def M(name, col, rough=0.45, metal=0.0, emit=0.0, trans=0.0, coat=0.0, film=0.0, alpha=1.0):
    key = name
    if key in MATS: return MATS[key]
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*col, 1); b.inputs["Roughness"].default_value = rough; b.inputs["Metallic"].default_value = metal
    if emit > 0: b.inputs["Emission Color"].default_value = (*col, 1); b.inputs["Emission Strength"].default_value = emit
    if trans > 0: b.inputs["Transmission Weight"].default_value = trans; b.inputs["IOR"].default_value = 1.45
    if coat > 0: b.inputs["Coat Weight"].default_value = coat
    if film > 0 and "Thin Film Thickness" in b.inputs: b.inputs["Thin Film Thickness"].default_value = film; b.inputs["Thin Film IOR"].default_value = 1.6
    if alpha < 1: b.inputs["Alpha"].default_value = alpha
    MATS[key] = m; return m
def rainbow(name):
    """보는 각도에 따라 일곱 빛깔이 도는 진주(가장자리로 갈수록 색이 바뀜)."""
    m = M(name, (1, 1, 1), rough=0.1, coat=1.0); nt = m.node_tree; b = nt.nodes["Principled BSDF"]
    lw = nt.nodes.new("ShaderNodeLayerWeight"); lw.inputs["Blend"].default_value = 0.8
    ramp = nt.nodes.new("ShaderNodeValToRGB"); cr = ramp.color_ramp
    stops = [(0.0, (1.0, 0.9, 0.95)), (0.15, (1.0, 0.55, 0.7)), (0.32, (1.0, 0.85, 0.3)), (0.5, (0.4, 0.95, 0.55)), (0.68, (0.35, 0.7, 1.0)), (1.0, (0.7, 0.4, 1.0))]
    cr.elements[0].position, cr.elements[0].color = stops[0][0], (*stops[0][1], 1); cr.elements[1].position, cr.elements[1].color = stops[-1][0], (*stops[-1][1], 1)
    for pos, col in stops[1:-1]: e = cr.elements.new(pos); e.color = (*col, 1)
    nt.links.new(lw.outputs["Facing"], ramp.inputs["Fac"]); nt.links.new(ramp.outputs["Color"], b.inputs["Base Color"]); return m

ITEM_OBJS = {}; cur = None
def start(iid):
    global cur; cur = iid; ITEM_OBJS[iid] = []
def _fin(o, m, smooth=True):
    o.data.materials.append(m)
    if smooth:
        for p in o.data.polygons: p.use_smooth = True
    ITEM_OBJS[cur].append(o); return o
def _new(bm, name):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free(); o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); return o
def sph(c, r, m, seg=32, rings=20, rot=(0, 0, 0)):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0)
    r3 = r if isinstance(r, (tuple, list)) else (r, r, r)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(c)) @ Euler(rot).to_matrix().to_4x4() @ Matrix.Diagonal((*r3, 1)), verts=bm.verts[:])
    return _fin(_new(bm, cur + "_s"), m)
def cyl(p0, p1, r0, r1, m, seg=24, smooth=True):
    a, b = Vector(p0), Vector(p1); d = b - a; bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r0, radius2=r1, depth=d.length)
    bmesh.ops.transform(bm, matrix=Matrix.Translation((a + b) / 2) @ Vector((0, 0, 1)).rotation_difference(d.normalized()).to_matrix().to_4x4(), verts=bm.verts[:])
    return _fin(_new(bm, cur + "_c"), m, smooth)
def box(c, s, m, rot=(0, 0, 0), bevel=0.0):
    bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(c)) @ Euler(rot).to_matrix().to_4x4() @ Matrix.Diagonal((*s, 1)), verts=bm.verts[:])
    o = _fin(_new(bm, cur + "_b"), m, False)
    if bevel > 0: md = o.modifiers.new("bv", "BEVEL"); md.width = bevel; md.segments = 3
    return o
def torus(c, R, r, m, rot=(0, 0, 0), seg=40, sides=14):
    bm = bmesh.new(); rows = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        rows.append([bm.verts.new(Vector(((R + r * math.cos(t)) * math.cos(a), (R + r * math.cos(t)) * math.sin(a), r * math.sin(t)))) for t in [2 * math.pi * j / sides for j in range(sides)]])
    for i in range(seg):
        for j in range(sides): bm.faces.new((rows[i][j], rows[(i + 1) % seg][j], rows[(i + 1) % seg][(j + 1) % sides], rows[i][(j + 1) % sides]))
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(c)) @ Euler(rot).to_matrix().to_4x4(), verts=bm.verts[:])
    return _fin(_new(bm, cur + "_t"), m)
def star(c, R, r, depth, m, rot=(0, 0, 0), pts=5):
    bm = bmesh.new(); top = bm.verts.new(Vector((0, 0, depth))); bot = bm.verts.new(Vector((0, 0, -depth))); ring = []
    for k in range(pts * 2):
        a = math.pi / 2 + k * math.pi / pts; rr = R if k % 2 == 0 else r; ring.append(bm.verts.new(Vector((math.cos(a) * rr, math.sin(a) * rr, 0))))
    for k in range(pts * 2):
        n = (k + 1) % (pts * 2); bm.faces.new((top, ring[k], ring[n])); bm.faces.new((bot, ring[n], ring[k]))
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(c)) @ Euler(rot).to_matrix().to_4x4(), verts=bm.verts[:])
    return _fin(_new(bm, cur + "_st"), m, False)
def gem(c, r, m, rot=(0, 0, 0), sub=1):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=r)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(c)) @ Euler(rot).to_matrix().to_4x4() @ Matrix.Diagonal((1, 1, 1.35, 1)), verts=bm.verts[:])
    return _fin(_new(bm, cur + "_g"), m, False)

# ── 재질 ────────────────────────────────────────────────
GLASS = M("Glass", (0.92, 0.96, 1.0), rough=0.05, trans=0.95, alpha=0.35); CORK = M("Cork", (0.62, 0.42, 0.24), rough=0.8)
GOLD = M("Gold", (1.0, 0.78, 0.32), rough=0.25, metal=1.0); BRONZE = M("Bronze", (0.72, 0.48, 0.26), rough=0.35, metal=1.0)
STEEL = M("Steel", (0.82, 0.85, 0.9), rough=0.2, metal=1.0); LEATHER = M("Leather", (0.36, 0.2, 0.1), rough=0.7); WOOD = M("Wood", (0.6, 0.38, 0.18), rough=0.7)
PEARL = rainbow("Pearl"); IVORY = M("Ivory", (0.95, 0.9, 0.78), rough=0.45)
BREAD = M("BreadCrust", (0.66, 0.36, 0.12), rough=0.6); CRUMB = M("Crumb", (0.9, 0.7, 0.4), rough=0.8)
HONEY = M("Honey", (0.95, 0.52, 0.04), rough=0.08, trans=0.25, coat=1.0); OIL = M("Oil", (0.95, 0.66, 0.12), rough=0.05, trans=0.3)
PAPER = M("Paper", (0.93, 0.88, 0.76), rough=0.9); STRING = M("String", (0.75, 0.25, 0.2), rough=0.8); BEAR = M("BearBrown", (0.45, 0.27, 0.13), rough=0.8)
CORE = M("Core", (1.0, 0.72, 0.08), rough=0.15, emit=0.9); RING_Y = M("RingYellow", (1.0, 0.8, 0.25), rough=0.2, emit=0.6)
BATB = M("BatBlue", (0.08, 0.5, 0.92), rough=0.35); BATG = M("BatGrip", (0.04, 0.04, 0.05), rough=0.25); SILVER = M("Silver", (0.88, 0.9, 0.94), rough=0.12, metal=1.0)
STARM = M("Star", (1.0, 0.78, 0.12), rough=0.25, emit=0.35); FIRE = M("Fire", (1.0, 0.36, 0.04), rough=0.4, emit=1.6); EMBER = M("Ember", (1.0, 0.62, 0.12), rough=0.4, emit=2.5)
DISH = M("Dish", (0.32, 0.3, 0.34), rough=0.5, metal=0.3); FEATHER = M("Feather", (0.86, 0.9, 0.96), rough=0.35, metal=0.4, coat=0.5)
RED = M("RedPotion", (0.85, 0.05, 0.08), rough=0.1, emit=0.25); BLUE = M("BluePotion", (0.1, 0.5, 0.95), rough=0.1, emit=0.25)
CORD = M("Cord", (0.82, 0.15, 0.2), rough=0.7); TASSEL = M("Tassel", (0.95, 0.75, 0.2), rough=0.7)

# ── 아이템마다 소품 ──────────────────────────────────────
def potion(liquid):
    sph((0, 0, 0.42), 0.42, GLASS); sph((0, 0, 0.41), 0.36, liquid); cyl((0, 0, 0.75), (0, 0, 1.08), 0.13, 0.13, GLASS)
    cyl((0, 0, 1.02), (0, 0, 1.22), 0.15, 0.17, CORK); sph((-0.15, -0.3, 0.6), 0.06, M("Shine", (1, 1, 1), rough=0.0, emit=2.0))
start("potion_red"); potion(RED)
start("potion_blue"); potion(BLUE)
start("bread")
sph((0, 0, 0.32), (0.75, 0.42, 0.34), BREAD); 
for k in range(3): box((-0.38 + 0.38 * k, -0.02, 0.62), (0.08, 0.36, 0.06), CRUMB, rot=(0, 0, 0.5), bevel=0.02)
start("dust_fire")
cyl((0, 0, 0), (0, 0, 0.12), 0.62, 0.7, DISH, seg=32)
for k in range(9):
    a = k * 2.4; r = 0.12 + 0.05 * (k % 4); sph((math.cos(a) * r * 1.6, math.sin(a) * r * 1.6, 0.2 + 0.04 * (k % 3)), 0.17 - 0.008 * k, FIRE, seg=12, rings=8)
for k in range(6): a = k * 1.05; sph((math.cos(a) * 0.3, math.sin(a) * 0.3, 0.55 + 0.12 * (k % 3)), 0.04, EMBER, seg=8, rings=6)
start("shard_star")
star((0, 0, 0.5), 0.62, 0.27, 0.2, STARM, rot=(1.25, 0.0, 0.25)); sph((0.25, -0.2, 0.95), 0.05, EMBER, seg=8, rings=6)
start("feather_silver")
v = sph((0, 0, 0.5), (0.2, 0.05, 0.62), FEATHER, rot=(0.0, 0.5, 0.0)); cyl((-0.38, 0, -0.18), (0.3, 0, 1.12), 0.025, 0.012, SILVER)
start("rainbow_pearl")
sph((0, 0, 0.55), 0.48, PEARL, seg=48, rings=32)
for k in range(6): a = k * math.pi / 3; sph((math.cos(a) * 0.55, math.sin(a) * 0.55, 0.05), (0.22, 0.12, 0.04), M("Shell", (0.95, 0.5, 0.68), rough=0.4), rot=(0, 0, a))
start("bear_bone")
cyl((-0.62, 0, 0.3), (0.62, 0, 0.3), 0.15, 0.15, IVORY)
for x in (-0.68, 0.68):
    for y in (-0.15, 0.15): sph((x, y, 0.3), 0.2, IVORY)
start("bear_oil")
cyl((0, 0, 0), (0, 0, 0.8), 0.42, 0.42, GLASS, seg=32); cyl((0, 0, 0.03), (0, 0, 0.66), 0.38, 0.38, OIL, seg=32)
cyl((0, 0, 0.8), (0, 0, 0.92), 0.36, 0.36, GLASS); sph((0, 0, 0.98), (0.5, 0.5, 0.12), PAPER, seg=24, rings=10); torus((0, 0, 0.9), 0.38, 0.025, STRING)
sph((0, -0.43, 0.38), (0.12, 0.02, 0.1), BEAR)
for k in range(3): sph((-0.1 + 0.1 * k, -0.43, 0.52), (0.035, 0.015, 0.035), BEAR, seg=10, rings=6)
start("ring_pearl")
torus((0, 0, 0.35), 0.48, 0.07, GOLD, rot=(1.25, 0, 0)); cyl((0, -0.12, 0.83), (0, -0.12, 0.92), 0.12, 0.16, GOLD); sph((0, -0.12, 1.02), 0.14, PEARL)
start("charm_tremor")
gem((0, 0, 0.45), 0.28, CORE, rot=(0.2, 0.3, 0.4)); torus((0, 0, 0.45), 0.42, 0.03, CORD, rot=(1.3, 0, 0.2))
cyl((0, 0, 0.0), (0, 0, -0.45), 0.08, 0.13, TASSEL); torus((0, 0, 0.0), 0.09, 0.03, CORD)
for k in range(3): torus((0, 0, 0.45), 0.55 + 0.1 * k, 0.012, RING_Y, rot=(1.3 + 0.2 * k, 0.4 * k, 0))
start("mandu_core")
gem((0, 0, 0.5), 0.36, CORE, rot=(0.3, 0.2, 0.5), sub=1)
for k in range(3): torus((0, 0, 0.5), 0.55 + 0.08 * k, 0.018, RING_Y, rot=(1.1 + 0.35 * k, 0.3 * k, 0.5 * k))
start("bat_alloy")
o = cyl((-0.1, 0, 0.0), (0.0, 0, 0.9), 0.22, 0.25, BATB, seg=24)
for p in o.data.vertices:                                                       # 부러진 끝을 들쭉날쭉하게
    if p.co.z > 0.4: p.co.z += 0.12 * math.sin(p.co.x * 37 + p.co.y * 23)
cyl((-0.2, 0, -0.35), (-0.1, 0, 0.0), 0.14, 0.2, BATB); cyl((-0.32, 0, -0.85), (-0.2, 0, -0.35), 0.12, 0.13, BATG)
for k in range(4): torus((-0.32 + 0.03 * k, 0, -0.75 + 0.12 * k), 0.13, 0.025, BATG, rot=(0, -0.24, 0))
torus((-0.2, 0, -0.33), 0.135, 0.025, SILVER, rot=(0, -0.24, 0))
start("peanut_bread")
sph((-0.32, 0, 0.32), (0.42, 0.38, 0.32), CRUMB); sph((0.32, 0, 0.32), (0.42, 0.38, 0.32), CRUMB); sph((0, 0, 0.3), (0.3, 0.3, 0.26), CRUMB)
for k in range(5): box((-0.55 + 0.27 * k, -0.06, 0.62), (0.04, 0.3, 0.03), BREAD, rot=(0, 0, 0.6), bevel=0.01)
start("honey")
sph((0, 0, 0.45), (0.5, 0.5, 0.48), HONEY); cyl((0, 0, 0.82), (0, 0, 1.0), 0.32, 0.36, M("HoneyLid", (0.95, 0.85, 0.55), rough=0.7)); torus((0, 0, 0.84), 0.34, 0.03, STRING)
cyl((0.1, 0, 0.9), (0.55, -0.1, 1.45), 0.035, 0.035, WOOD); 
for k in range(3): torus((0.08 - 0.0 * k, 0, 0.82 - 0.07 * k), 0.09, 0.03, WOOD)
sph((0.32, -0.38, 0.42), (0.06, 0.06, 0.12), HONEY)
start("honey_peanut_bread")
sph((-0.32, 0, 0.32), (0.42, 0.38, 0.32), CRUMB); sph((0.32, 0, 0.32), (0.42, 0.38, 0.32), CRUMB); sph((0, 0, 0.3), (0.3, 0.3, 0.26), CRUMB)
sph((-0.3, 0, 0.56), (0.36, 0.32, 0.12), HONEY); sph((0.3, 0, 0.56), (0.36, 0.32, 0.12), HONEY); sph((0.0, 0, 0.5), (0.24, 0.24, 0.1), HONEY)
for x in (-0.45, 0.2, 0.52): sph((x, -0.34, 0.36), (0.05, 0.05, 0.12), HONEY)
sph((0.15, -0.2, 0.75), 0.05, M("Spark", (1, 0.95, 0.7), emit=4.0))
start("key_old")
torus((-0.55, 0, 0.3), 0.28, 0.07, BRONZE, rot=(math.pi / 2, 0, 0)); cyl((-0.27, 0, 0.3), (0.72, 0, 0.3), 0.06, 0.06, BRONZE)
for x in (0.45, 0.62): box((x, 0, 0.12), (0.08, 0.1, 0.3), BRONZE, bevel=0.01)
def sword(edge):
    box((0, 0, 1.0), (0.18, 0.05, 1.5), STEEL, bevel=0.02); star((0, 0, 1.78), 0.09, 0.09, 0.025, STEEL, pts=2, rot=(math.pi / 2, 0, 0))
    for sx in (-1, 1): box((sx * 0.085, 0, 1.0), (0.05, 0.065, 1.42), edge)   # 양쪽 날 빛깔
    box((0, 0, 0.2), (0.62, 0.12, 0.1), GOLD, bevel=0.02)
    cyl((0, 0, 0.15), (0, 0, -0.32), 0.06, 0.06, LEATHER); sph((0, 0, -0.38), 0.1, GOLD)
start("sword_spark"); sword(M("EdgeGold", (1.0, 0.75, 0.1), emit=1.0))
start("sword_frost"); sword(M("EdgeIce", (0.25, 0.7, 1.0), emit=1.0))
start("sword_ember"); sword(M("EdgeEmber", (0.95, 0.16, 0.03), emit=0.5))
TILT = {"sword_spark": (0, 0.7, 0), "sword_frost": (0, 0.7, 0), "sword_ember": (0, 0.7, 0), "bat_alloy": (0, 0.5, 0), "key_old": (0, 0, 0.4), "feather_silver": (0, 0.2, 0)}

# ── 렌더 ────────────────────────────────────────────────
done = []
for iid, objs in ITEM_OBJS.items():
    out = os.path.join(ICONS, iid + ".png")
    if ONLY is not None and iid not in ONLY: continue
    if os.path.exists(out) and not ALL and ONLY is None: print("건너뜀(이미 있음)", iid); continue
    for o in sc.objects:
        if o.type == "MESH": o.hide_render = o not in objs
    if iid in TILT:                                                             # 길쭉한 것은 대각선으로 눕힘
        for o in objs: o.matrix_world = Euler(TILT[iid]).to_matrix().to_4x4() @ o.matrix_world
    bpy.context.view_layer.update()
    d = Vector((0.62, -1.0, 0.62)).normalized()
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]
    c = sum(pts, Vector()) / len(pts); mn = Vector([min(p[k] for p in pts) for k in range(3)]); mx = Vector([max(p[k] for p in pts) for k in range(3)]); c = (mn + mx) / 2
    cam.location = c + d * 10; cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler(); bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted(); loc = [inv @ p for p in pts]
    ext = max(max(p.x for p in loc) - min(p.x for p in loc), max(p.y for p in loc) - min(p.y for p in loc))
    cx = (max(p.x for p in loc) + min(p.x for p in loc)) / 2; cy = (max(p.y for p in loc) + min(p.y for p in loc)) / 2
    cam.location = cam.matrix_world @ Vector((cx, cy, 0)); cam.data.ortho_scale = ext * 1.18
    sc.render.filepath = out; bpy.ops.render.render(write_still=True); done.append(iid); print("✔ 아이콘", iid)

# 전체 모음(가방 칸처럼)
try:
    from PIL import Image, ImageDraw
    files = sorted(f for f in os.listdir(ICONS) if f.endswith(".png"))
    cols = 8; rows = (len(files) + cols - 1) // cols; S = 140
    sheet = Image.new("RGBA", (cols * S + 20, rows * S + 20), (40, 34, 28, 255)); dr = ImageDraw.Draw(sheet)
    for k, f in enumerate(files):
        x, y = 10 + (k % cols) * S, 10 + (k // cols) * S
        dr.rounded_rectangle((x + 4, y + 4, x + S - 4, y + S - 4), 10, fill=(150, 138, 118, 255), outline=(110, 92, 64, 255), width=3)
        im = Image.open(os.path.join(ICONS, f)).convert("RGBA").resize((S - 22, S - 22)); sheet.alpha_composite(im, (x + 11, y + 11))
    sheet.save(os.path.join(PRE, "item_icons_sheet.png")); print("✔ sheet", len(files))
except Exception as e:
    print("모음 생략:", e)
