"""용궁 생일 파티(퀘스트 「삐진 고래고래 임금님」을 끝내면 Unity Palace 가 켬) — 맵 좌표(build_palace.py 와 같은 틀) → Resources/World/Party.fbx.
결투장 가운데: 분홍 식탁보 둥근 탁자 + 3단 딸기 생크림 케이크(층마다 딸기 · 분홍 띠 · 맨 위 큰 딸기 · 초 5개(불꽃 Glow) · 작은 금관)
둘레: 컵케이크 작은 탁자 2 · 선물 상자 · 풍선 Balloon_*(Unity 가 둥실) · 기둥 사이 삼각 깃발 줄 · 바닥 색종이
표시: Mark_Guest_1~5(손님 자리) · Mark_PartyKing · Mark_Cake · Party_Collision(탁자)
실행: <bpy 파이썬> blender/build_party.py [--preview]"""
import sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
import bpy, bmesh
from mathutils import Vector, Matrix
from lib import mat, reset_scene, to_object
from meshkit import Frame, Builder
import whale_king_parts as WK

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World"); EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
random.seed(11); reset_scene()
C = Vector((-50.012, 28.328)); R = 586.0; TIP = math.radians(-36.4136)
T2 = C + R * Vector((math.cos(TIP), math.sin(TIP))); U2 = (T2 - C).normalized(); V2 = Vector((-U2.y, U2.x)); O2 = T2 + U2 * 165.0
F = Frame(Vector((O2.x, O2.y, -42.0)), Vector((U2.x, U2.y, 0)), Vector((V2.x, V2.y, 0)))
AX, AZ = -11.0, 0.25                                                             # 결투장 가운데(build_palace.py)

def M_(n, c, **kw): m = mat(n, c, **kw); m.diffuse_color = (*c, 1); return m
CREAM = M_("Cake_Cream", (0.99, 0.96, 0.93), rough=0.6); PINKC = M_("Cake_PinkCream", (1.0, 0.72, 0.8), rough=0.6); SPONGE = M_("Cake_Sponge", (0.98, 0.84, 0.55), rough=0.9)
BERRY = M_("Cake_Strawberry", (0.9, 0.1, 0.18), rough=0.35); LEAF = M_("Cake_Leaf", (0.25, 0.7, 0.25), rough=0.6); CLOTH = M_("Party_Cloth", (1.0, 0.75, 0.85), rough=0.9)
WOOD = M_("Party_Wood", (0.6, 0.42, 0.28), rough=0.8); CANDLE = M_("Party_Candle", (0.6, 0.8, 1.0), rough=0.5); FLAME = M_("Party_FlameGlow", (1.0, 0.75, 0.3), emit=(1.0, 0.7, 0.25), emit_strength=6.0)
GOLD = M_("Party_Gold", (1.0, 0.8, 0.3), rough=0.3, metal=0.9); STR = M_("Party_String", (0.95, 0.95, 0.95), rough=0.9)
BAL = [M_(f"Balloon_{n}", c, rough=0.25) for n, c in (("Pink", (1.0, 0.5, 0.7)), ("Blue", (0.4, 0.65, 1.0)), ("Yellow", (1.0, 0.85, 0.3)), ("Mint", (0.45, 0.9, 0.75)), ("Violet", (0.7, 0.5, 1.0)))]
ALL = [CREAM, PINKC, SPONGE, BERRY, LEAF, CLOTH, WOOD, CANDLE, FLAME, GOLD, STR] + BAL
def B(): return Builder(ALL, F)

def berry(b, x, y, z, s=1.0, tilt=0.0):
    b.sphere((x, y, z), (0.13 * s, 0.13 * s, 0.16 * s), "Cake_Strawberry", seg=10, rings=7, rot=Matrix.Rotation(tilt, 3, "X"))
    for k in range(3): a = k * 2.1; b.sphere((x + math.cos(a) * 0.06 * s, y + math.sin(a) * 0.06 * s, z + 0.15 * s), (0.07 * s, 0.03 * s, 0.015 * s), "Cake_Leaf", seg=6, rings=4, rot=Matrix.Rotation(a, 3, "Z"))

# ── 탁자 + 케이크 ─────────────────────────────────────────
K = B(); TZ = AZ + 0.95
K.cyl((AX, 0, AZ), (AX, 0, AZ + 0.85), 0.25, "Party_Wood", seg=10); K.cyl((AX, 0, AZ + 0.85), (AX, 0, TZ), 2.3, "Party_Cloth", seg=40)
for k in range(24): a = 2 * math.pi * k / 24; K.sphere((AX + math.cos(a) * 2.3, math.sin(a) * 2.3, TZ - 0.18), (0.22, 0.22, 0.24), "Party_Cloth", seg=8, rings=5)   # 식탁보 주름
z = TZ
for i, (r, h) in enumerate(((1.35, 0.6), (1.0, 0.55), (0.68, 0.5))):
    K.cyl((AX, 0, z), (AX, 0, z + h * 0.45), r, "Cake_Sponge", seg=36); K.cyl((AX, 0, z + h * 0.45), (AX, 0, z + h * 0.55), r + 0.02, "Cake_PinkCream", seg=36)
    K.cyl((AX, 0, z + h * 0.55), (AX, 0, z + h), r, "Cake_Cream", seg=36)
    K.torus((AX, 0, z + h), r - 0.05, 0.07, "Cake_Cream", seg=36, sides=6); K.torus((AX, 0, z + 0.05), r + 0.02, 0.06, "Cake_PinkCream", seg=36, sides=6)
    n = int(r * 9)
    for k in range(n): a = 2 * math.pi * (k + 0.5 * i) / n; berry(K, AX + math.cos(a) * (r - 0.12), math.sin(a) * (r - 0.12), z + h + 0.1, 0.9)
    z += h
for k in range(3): a = k * 2.1 + 0.4; berry(K, AX + math.cos(a) * 0.25, math.sin(a) * 0.25, z + 0.14, 1.8, 0.3)   # 맨 위 큰 딸기
for k in range(5):
    a = 2 * math.pi * k / 5 + 0.3; x, y = AX + math.cos(a) * 0.5, math.sin(a) * 0.5
    K.cyl((x, y, z), (x, y, z + 0.45), 0.04, "Party_Candle", seg=8); K.sphere((x, y, z + 0.52), (0.05, 0.05, 0.09), "Party_FlameGlow", seg=8, rings=5)
cr = WK.crown(1.4, at=Vector((0, 0, 0)))
for f in cr.faces: f.material_index = K.M("Party_Gold")
K.merge(cr, Matrix.Translation(F.P(AX, 0, z + 0.02)))
for sx in (-1, 1):                                                               # 컵케이크 탁자 2
    tx, ty = AX + 1.0, sx * 5.0
    K.cyl((tx, ty, AZ), (tx, ty, AZ + 0.75), 0.15, "Party_Wood", seg=8); K.cyl((tx, ty, AZ + 0.75), (tx, ty, AZ + 0.82), 1.0, "Party_Cloth", seg=24)
    for k in range(5):
        a = 2 * math.pi * k / 5; cx, cy = tx + math.cos(a) * 0.55, ty + math.sin(a) * 0.55
        K.cyl((cx, cy, AZ + 0.82), (cx, cy, AZ + 1.0), 0.14, "Cake_Sponge", seg=10, r1=0.17); K.sphere((cx, cy, AZ + 1.05), (0.17, 0.17, 0.11), "Cake_PinkCream" if k % 2 else "Cake_Cream", seg=10, rings=6)
        berry(K, cx, cy, AZ + 1.17, 0.6)
for k in range(7):                                                               # 선물 상자
    a = 2 * math.pi * k / 7 + 0.2; r = random.uniform(2.9, 3.6); x, y = AX + math.cos(a) * r, math.sin(a) * r; s = random.uniform(0.35, 0.6); m = BAL[k % 5].name
    K.box(x, y, AZ + s / 2, s, s, s, m, rot=a); K.box(x, y, AZ + s / 2 + 0.005, s * 1.02, s * 0.15, s * 1.02, "Party_Gold", rot=a); K.box(x, y, AZ + s / 2 + 0.005, s * 0.15, s * 1.02, s * 1.02, "Party_Gold", rot=a)
    K.sphere((x, y, AZ + s + 0.06), (0.1, 0.06, 0.06), "Party_Gold", seg=8, rings=5)
for k in range(140):                                                             # 바닥 색종이
    a = random.uniform(0, 6.28); r = random.uniform(2.6, 12); x, y = AX + math.cos(a) * r, math.sin(a) * r
    K.box(x, y, AZ + 0.03, 0.14, 0.08, 0.01, BAL[k % 5].name, rot=random.uniform(0, 3.14))
cake = K.done("Party_Cake")
# ── 깃발 줄(결투장 둘레 기둥 사이 · 깃대 사이) ───────────────────────
G = B()
def bunting(p0, p1, sag=0.9, n=14):
    pts = [(p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t, p0[2] + (p1[2] - p0[2]) * t - sag * math.sin(math.pi * t)) for t in [i / n for i in range(n + 1)]]
    G.tube(pts, 0.02, "Party_String", sides=4)
    for i in range(n):
        (x0, y0, z0), (x1, y1, z1) = pts[i], pts[i + 1]; m = BAL[i % 5].name; mid = ((x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2 - 0.42)
        vs = [G.bm.verts.new(F.P(*p)) for p in ((x0, y0, z0), (x1, y1, z1), mid)]
        G.bm.faces.new(vs).material_index = G.M(m)
        vs2 = [G.bm.verts.new(v.co.copy()) for v in vs]; G.bm.faces.new(vs2[::-1]).material_index = G.M(m)   # 뒷면(양면으로 보이게)
for k in range(8):
    a0, a1 = 2 * math.pi * k / 8 + 0.4, 2 * math.pi * (k + 1) / 8 + 0.4
    bunting((AX + math.cos(a0) * 14.6, math.sin(a0) * 14.6, 3.2), (AX + math.cos(a1) * 14.6, math.sin(a1) * 14.6, 3.2), 0.8)
for k in range(6):                                                               # 가운데 탁자 위로 별 모양 줄
    a = 2 * math.pi * k / 6; bunting((AX, 0, 7.5), (AX + math.cos(a) * 14.6, math.sin(a) * 14.6, 3.2), 1.2, 16)
bunt = G.done("Party_Bunting")
# ── 풍선(하나씩 물체 — Unity 가 둥실) ─────────────────────────────
balloons = []
spots = [(AX + 2.3 * math.cos(a), 2.3 * math.sin(a)) for a in (0.5, 2.0, 3.6, 5.0)] + [(AX + 14.6 * math.cos(a), 14.6 * math.sin(a)) for a in (0.9, 1.7, 2.6, 3.6, 4.6, 5.4)] + [(4.0, 6.8), (4.0, -6.8)]
for i, (x, y) in enumerate(spots):
    for j in range(3):
        b = B(); h = 4.2 + random.uniform(0, 1.6) + (0 if i < 4 else 0.5); ox, oy = random.uniform(-0.5, 0.5), random.uniform(-0.5, 0.5); m = BAL[(i + j) % 5].name
        b.sphere((x + ox, y + oy, AZ + h), (0.38, 0.38, 0.46), m, seg=14, rings=10); b.cyl((x + ox, y + oy, AZ + h - 0.5), (x + ox, y + oy, AZ + h - 0.42), 0.06, m, seg=6, r1=0.02)
        b.tube([(x + ox * (1 - t), y + oy * (1 - t), AZ + h - 0.5 - (h - 1.4) * t) for t in [k / 6 for k in range(7)]], 0.008, "Party_String", sides=3)
        balloons.append(b.done(f"Balloon_{i + 1}_{j + 1}", origin=F.P(x + ox, y + oy, AZ + h)))
# ── 충돌 · 표시 ─────────────────────────────────────────────────
Q = B(); Q.cyl((AX, 0, AZ), (AX, 0, TZ + 0.5), 2.4, "Party_Wood", seg=16)
for sx in (-1, 1): Q.cyl((AX + 1.0, sx * 5.0, AZ), (AX + 1.0, sx * 5.0, AZ + 1.1), 1.05, "Party_Wood", seg=12)
coll = Q.done("Party_Collision"); coll.hide_render = True
marks = []
def mark(n, x, y, z):
    e = bpy.data.objects.new(n, None); e.location = F.P(x, y, z); bpy.context.collection.objects.link(e); marks.append(e)
mark("Mark_Cake", AX, 0, AZ); mark("Mark_PartyKing", AX + 5.5, 0, AZ + 0.05)
for k in range(5): a = 2 * math.pi * k / 5 + 2.6; mark(f"Mark_Guest_{k + 1}", AX + math.cos(a) * 4.2, math.sin(a) * 4.2, AZ + 0.05)
objs = [cake, bunt, coll] + balloons + marks
bpy.ops.object.select_all(action="DESELECT")
for o in objs: o.select_set(True)
fbx = os.path.join(OUT, "Party.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("EXPORTED", fbx, len(cake.data.vertices), len(balloons))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(EXP, "Party.blend"))
if "--preview" in sys.argv:
    with bpy.data.libraries.load(os.path.join(EXP, "Palace.blend")) as (src, dst): dst.objects = [n for n in src.objects if n.startswith(("Palace_Arena", "Palace_Building", "Palace_Decor"))]
    sc = bpy.context.scene
    for o in dst.objects: sc.collection.objects.link(o)
    coll.hide_render = True
    w = bpy.data.worlds.new("Sea"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.05, 0.18, 0.55, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.9; sc.world = w
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 2.6; sun.rotation_euler = (0.35, 0.15, 0.4); sc.collection.objects.link(sun)
    L = bpy.data.objects.new("PL", bpy.data.lights.new("PL", "POINT")); L.data.energy = 6000; L.data.color = (1, 0.85, 0.7); L.location = F.P(AX - 3, -2, 7); sc.collection.objects.link(L)
    sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 40; sc.cycles.use_denoising = True; sc.render.resolution_x, sc.render.resolution_y = 1280, 720; sc.view_settings.view_transform = "AgX"
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 24
    cam.location = F.P(AX - 11, -5, 4.5); tgt = F.P(AX + 2, 0, 2.0); cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(PRE, "palace_party.png"); bpy.ops.render.render(write_still=True); print("✔ palace_party")
