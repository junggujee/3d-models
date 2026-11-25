"""통통배(작은 고기잡이배) — 민미가 타는 배. 선착장 옆에 매여 있음(Unity World/Boat.cs).
크기: 길이 4.6m · 폭 1.8m. 물높이 = z 0(배 원점), 앞 = -Y(Unity +Z).
  · Boat_Hull: 둥근 U자 단면을 이어 붙인 선체(바깥 + 안쪽 판, 위 테두리 나무) — 물 아래 빨강 · 흰 몸 · 파란 띠
  · Boat_Deck(바닥 널판) · Boat_Cabin(작은 조타실: 흰 벽 · 파란 지붕 · 창 · 키 바퀴) · Boat_Chimney(검은 굴뚝 + 빨간 띠) · Boat_Seat(뒤쪽 의자)
  · Boat_Fenders(옆에 매단 타이어) · Boat_Lamp(Glow 등) · Boat_Mast + Boat_Flag(원점 = 깃대 끝, Unity 가 펄럭이게)
  · Boat_Prop(원점 = 프로펠러 축, Unity 가 돌림)
  · 표시: Mark_Seat(민미 발 위치) · Mark_Smoke(굴뚝 끝) · Mark_Wake(배 뒤 물 위) · Mark_Bow
실행: <bpy 파이썬> blender/build_boat.py [--preview]   출력: unity/Assets/Resources/World/Boat.fbx · previews/boat.png"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bpy, bmesh
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World"); EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
reset_scene()
def M_(name, color, **kw): m = mat(name, color, **kw); m.diffuse_color = (*color, 1); return m
RED = M_("Boat_Red", (0.72, 0.15, 0.12), rough=0.6); WHITE = M_("Boat_White", (0.95, 0.94, 0.9), rough=0.5); BLUE = M_("Boat_Blue", (0.16, 0.4, 0.78), rough=0.5)
WOOD = M_("Boat_Wood", (0.6, 0.42, 0.25), rough=0.8); DARK = M_("Boat_DarkWood", (0.32, 0.21, 0.13), rough=0.8); BLACK = M_("Boat_Black", (0.08, 0.08, 0.09), rough=0.6)
GLASS = M_("Boat_Glass", (0.3, 0.52, 0.68), rough=0.1); TIRE = M_("Boat_Tire", (0.12, 0.12, 0.12), rough=0.9); YEL = M_("Boat_Yellow", (0.98, 0.78, 0.15), rough=0.6)
GLOW = M_("Boat_LampGlow", (1.0, 0.9, 0.55), emit=(1.0, 0.85, 0.5), emit_strength=4.0); BRASS = M_("Boat_Brass", (0.8, 0.62, 0.25), rough=0.35, metal=0.8)
MATS = [RED, WHITE, BLUE, WOOD, DARK, BLACK, GLASS, TIRE, YEL, GLOW, BRASS]
I = {m.name: i for i, m in enumerate(MATS)}
Z = Vector((0, 0, 1))

# ── 선체: 앞(-Y) → 뒤(+Y) 단면 ─────────────────────────────────────
L0, L1 = -2.3, 2.3; NS, NA = 40, 24
def smoothstep(a, b, x): u = max(0.0, min(1.0, (x - a) / (b - a))); return u * u * (3 - 2 * u)
def station(t):
    """t = 0(뱃머리) ~ 1(고물): 반폭 w · 바닥(용골) 높이 zb · 뱃전 높이 zg"""
    w = 0.88 * math.sin(min(1.0, t / 0.5) * math.pi / 2) ** 0.75 * (1 - 0.15 * smoothstep(0.8, 1.0, t))
    zb = -0.42 + 0.5 * (1 - smoothstep(0.0, 0.4, t)) ** 2
    zg = 0.55 + 0.3 * (1 - smoothstep(0.0, 0.55, t)) ** 2
    return max(0.04, w), zb, zg
def section(t, inset=0.0):
    w, zb, zg = station(t); y = L0 + (L1 - L0) * t; ww = max(0.01, w - inset); pts = []
    for k in range(NA + 1):
        th = -math.pi / 2 + math.pi * k / NA
        pts.append(Vector((ww * math.sin(th), y, (zb + inset) + (zg - zb - inset) * (1 - math.cos(th)))))
    return pts
bm = bmesh.new()
def loft(inset, flip):
    rows = [[bm.verts.new(p) for p in section(i / NS, inset)] for i in range(NS + 1)]
    for i in range(NS):
        for k in range(NA):
            q = (rows[i][k], rows[i + 1][k], rows[i + 1][k + 1], rows[i][k + 1])
            f = bm.faces.new(q[::-1] if flip else q); f.smooth = True
            zc = sum(v.co.z for v in q) / 4; zg = station((i + 0.5) / NS)[2]          # 파란 띠 = 뱃전에서 일정한 아래(배 모양을 따라)
            f.material_index = I["Boat_Red"] if zc < 0.02 else (I["Boat_Blue"] if zg - 0.24 < zc < zg - 0.1 else I["Boat_White"])
            if inset > 0: f.material_index = I["Boat_Wood"]
    return rows
outer = loft(0.0, False); inner = loft(0.06, True)
for i in range(NS + 1):                                                          # 뱃전: 바깥 끝 ↔ 안쪽 끝 잇기(양쪽)
    for k in (0, NA):
        if i < NS:
            q = (outer[i][k], inner[i][k], inner[i + 1][k], outer[i + 1][k]); f = bm.faces.new(q if k == 0 else q[::-1]); f.material_index = I["Boat_DarkWood"]
for k in range(NA):                                                              # 고물(뒤판) 막기: 바깥·안쪽 마지막 단면 사이
    q = (outer[NS][k], outer[NS][k + 1], inner[NS][k + 1], inner[NS][k]); f = bm.faces.new(q); f.material_index = I["Boat_White"]
tf = bm.faces.new(inner[NS]); tf.material_index = I["Boat_White"]                 # 고물 안쪽 판(U 모양 막기)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
hull = to_object("Boat_Hull", bm, MATS)

class B:
    def __init__(self): self.bm = bmesh.new()
    def mat(self, vs, mi):
        for v in vs:
            for f in v.link_faces: f.material_index = mi
    def box(self, c, s, mi, rot=None):
        vs = bmesh.ops.create_cube(self.bm, size=1.0)["verts"]; M = Matrix.Translation(c) @ ((rot or Matrix.Identity(3)).to_4x4()) @ Matrix.Diagonal((*s, 1))
        bmesh.ops.transform(self.bm, matrix=M, verts=vs); self.mat(vs, mi); return vs
    def cyl(self, p0, p1, r, mi, seg=12, r1=None):
        d = p1 - p0; vs = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=seg, radius1=r, radius2=r if r1 is None else r1, depth=d.length)["verts"]
        bmesh.ops.transform(self.bm, matrix=Matrix.Translation((p0 + p1) / 2) @ Z.rotation_difference(d.normalized()).to_matrix().to_4x4(), verts=vs); self.mat(vs, mi); return vs
    def torus(self, c, R_, r, mi, axis, seg=16, sides=8):
        q = Z.rotation_difference(axis.normalized()).to_matrix(); rows = []
        for i in range(seg):
            a = 2 * math.pi * i / seg; rows.append([self.bm.verts.new(c + q @ Vector(((R_ + r * math.cos(b)) * math.cos(a), (R_ + r * math.cos(b)) * math.sin(a), r * math.sin(b)))) for b in [2 * math.pi * j / sides for j in range(sides)]])
        for i in range(seg):
            for j in range(sides):
                f = self.bm.faces.new((rows[i][j], rows[(i + 1) % seg][j], rows[(i + 1) % seg][(j + 1) % sides], rows[i][(j + 1) % sides])); f.material_index = mi; f.smooth = True
    def sphere(self, c, r, mi):
        vs = bmesh.ops.create_uvsphere(self.bm, u_segments=12, v_segments=8, radius=1)["verts"]; bmesh.ops.transform(self.bm, matrix=Matrix.Translation(c) @ Matrix.Diagonal((*r, 1)), verts=vs); self.mat(vs, mi)
    def done(self, name, origin=None):
        if origin is not None: bmesh.ops.translate(self.bm, vec=-origin, verts=self.bm.verts[:])
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces[:]); o = to_object(name, self.bm, MATS)
        if origin is not None: o.location = origin
        return o

# 바닥 널판
d = B()
for i in range(16):
    y = -1.5 + i * 0.25; w, zb, zg = station((y - L0) / (L1 - L0))
    if 0.06 <= zb + 0.05: continue
    hw = w * math.sin(math.acos(max(-1.0, min(1.0, 1 - (0.06 - zb - 0.06) / (zg - zb - 0.06)))))   # 그 높이의 안쪽 반폭
    d.box(Vector((0, y, 0.06)), (2 * max(0.05, hw - 0.1), 0.22, 0.05), I["Boat_Wood"] if i % 2 else I["Boat_DarkWood"])
deck = d.done("Boat_Deck")
# 조타실(가운데) · 지붕 · 창 · 키 바퀴
c = B(); CY0, CY1, CW, CH = -0.55, 0.55, 1.2, 1.75
c.box(Vector((0, (CY0 + CY1) / 2, (0.08 + CH) / 2)), (CW, CY1 - CY0, CH - 0.08), I["Boat_White"])
c.box(Vector((0, (CY0 + CY1) / 2, CH + 0.06)), (CW + 0.3, CY1 - CY0 + 0.4, 0.12), I["Boat_Blue"])
c.box(Vector((0, CY0 - 0.01, 1.3)), (0.9, 0.04, 0.48), I["Boat_Glass"])                   # 앞 창
for s in (-1, 1):
    c.box(Vector((s * (CW / 2 + 0.01), (CY0 + CY1) / 2, 1.3)), (0.04, 0.7, 0.42), I["Boat_Glass"])   # 옆 창
    c.box(Vector((s * (CW / 2 + 0.02), (CY0 + CY1) / 2, 1.3)), (0.05, 0.06, 0.46), I["Boat_White"])
c.box(Vector((0, CY0 - 0.02, 1.3)), (0.05, 0.05, 0.5), I["Boat_White"])
c.box(Vector((0, CY1 + 0.01, 0.95)), (0.6, 0.04, 1.4), I["Boat_DarkWood"])              # 뒷문
c.sphere(Vector((0.2, CY1 + 0.05, 0.95)), (0.04, 0.04, 0.04), I["Boat_Brass"])
c.torus(Vector((0, CY1 + 0.25, 1.05)), 0.18, 0.025, I["Boat_DarkWood"], Vector((0, 1, 0)), seg=16, sides=6)   # 키 바퀴(뒤에서 잡음)
for k in range(6):
    a = k * math.pi / 3; c.cyl(Vector((0, CY1 + 0.25, 1.05)), Vector((0.24 * math.cos(a), CY1 + 0.25, 1.05 + 0.24 * math.sin(a))), 0.015, I["Boat_DarkWood"], seg=5)
c.cyl(Vector((0, CY1 + 0.25, 1.05)), Vector((0, CY1 + 0.01, 1.05)), 0.03, I["Boat_Brass"], seg=8)
c.torus(Vector((CW / 2 + 0.06, -0.05, 0.85)), 0.2, 0.06, I["Boat_Red"], Vector((1, 0, 0)), seg=16, sides=6)   # 구명 튜브
cabin = c.done("Boat_Cabin")
ch = B(); ch.cyl(Vector((0.3, 0.3, CH + 0.1)), Vector((0.3, 0.3, CH + 0.75)), 0.09, I["Boat_Black"]); ch.cyl(Vector((0.3, 0.3, CH + 0.5)), Vector((0.3, 0.3, CH + 0.6)), 0.1, I["Boat_Red"])
ch.cyl(Vector((0.3, 0.3, CH + 0.75)), Vector((0.3, 0.3, CH + 0.8)), 0.12, I["Boat_Black"]); chimney = ch.done("Boat_Chimney")
lp = B(); lp.box(Vector((-0.35, CY0 + 0.1, CH + 0.22)), (0.14, 0.14, 0.18), I["Boat_LampGlow"]); lp.box(Vector((-0.35, CY0 + 0.1, CH + 0.33)), (0.2, 0.2, 0.05), I["Boat_Black"]); lamp = lp.done("Boat_Lamp")
# 뒤쪽 의자 · 옆 타이어 · 밧줄
st = B(); st.box(Vector((0, 1.55, 0.42)), (1.1, 0.42, 0.08), I["Boat_Wood"]); st.box(Vector((0, 1.55, 0.25)), (0.12, 0.3, 0.35), I["Boat_DarkWood"])
for s in (-1, 1): st.box(Vector((s * 0.5, 1.55, 0.25)), (0.08, 0.3, 0.35), I["Boat_DarkWood"])
st.cyl(Vector((0, 2.25, 0.55)), Vector((0, 1.85, 0.75)), 0.03, I["Boat_DarkWood"], seg=6)          # 키 손잡이(꼬리 키)
seat = st.done("Boat_Seat")
fd = B()
for s in (-1, 1):
    for y in (-0.9, 0.2, 1.3):
        w, zb, zg = station((y - L0) / (L1 - L0)); fd.torus(Vector((s * (w + 0.06), y, 0.25)), 0.16, 0.07, I["Boat_Tire"], Vector((1, 0, 0)), seg=14, sides=6)
        fd.cyl(Vector((s * (w + 0.06), y, 0.42)), Vector((s * (w - 0.02), y, zg - 0.02)), 0.012, I["Boat_Wood"], seg=4)
fenders = fd.done("Boat_Fenders")
# 돛대 + 깃발 · 프로펠러
ms = B(); ms.cyl(Vector((0, -1.55, 0.3)), Vector((0, -1.55, 2.45)), 0.04, I["Boat_DarkWood"], seg=8); ms.sphere(Vector((0, -1.55, 2.47)), (0.06, 0.06, 0.06), I["Boat_Brass"]); mast = ms.done("Boat_Mast")
fl = B(); FO = Vector((0, -1.55, 2.35))
tri = [Vector((0, 0, 0)), Vector((0, 0.7, -0.17)), Vector((0, 0, -0.36))]            # 얇은 세모 기둥(양면으로 보이게)
A_ = [fl.bm.verts.new(FO + p + Vector((-0.006, 0, 0))) for p in tri]; B_ = [fl.bm.verts.new(FO + p + Vector((0.006, 0, 0))) for p in tri]
for q in ([A_[0], A_[1], A_[2]], [B_[2], B_[1], B_[0]], [A_[0], B_[0], B_[1], A_[1]], [A_[1], B_[1], B_[2], A_[2]], [A_[2], B_[2], B_[0], A_[0]]): fl.bm.faces.new(q).material_index = I["Boat_Yellow"]
flag = fl.done("Boat_Flag", origin=FO)
pr = B(); PO = Vector((0, 2.22, -0.28)); pr.cyl(PO + Vector((0, -0.3, 0.05)), PO, 0.035, I["Boat_Brass"], seg=8); pr.sphere(PO, (0.06, 0.08, 0.06), I["Boat_Brass"])
for k in range(3):
    a = 2 * math.pi * k / 3; pr.box(PO + Vector((0.11 * math.cos(a), 0.02, 0.11 * math.sin(a))), (0.2, 0.02, 0.09), I["Boat_Brass"], rot=Matrix.Rotation(-a, 3, "Y") @ Matrix.Rotation(0.5, 3, "X"))
prop = pr.done("Boat_Prop", origin=PO)
pr2 = B(); pr2.box(Vector((0, 2.28, -0.12)), (0.05, 0.12, 0.42), I["Boat_DarkWood"]); rudder = pr2.done("Boat_Rudder")

marks = []
for nm, p in (("Mark_Seat", Vector((0, 1.55, 0.46))), ("Mark_Smoke", Vector((0.3, 0.3, CH + 0.82))), ("Mark_Wake", Vector((0, 2.45, 0.0))), ("Mark_Bow", Vector((0, -2.35, 0.3)))):
    e = bpy.data.objects.new(nm, None); e.location = p; bpy.context.collection.objects.link(e); marks.append(e)
objs = [hull, deck, cabin, chimney, lamp, seat, fenders, mast, flag, prop, rudder] + marks
root = bpy.data.objects.new("Boat", None); bpy.context.collection.objects.link(root)
for o in objs: o.parent = root
bpy.ops.object.select_all(action="DESELECT")
for o in objs + [root]: o.select_set(True)
fbx = os.path.join(OUT, "Boat.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("EXPORTED", fbx, "hull verts", len(hull.data.vertices))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(EXP, "Boat.blend"))

if "--preview" in sys.argv:
    sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.samples = 48; sc.cycles.device = "CPU"; sc.render.resolution_x, sc.render.resolution_y = 900, 600
    w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.78, 0.95, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.9; sc.world = w
    sea = bpy.data.objects.new("Sea", bpy.data.meshes.new("Sea")); sc.collection.objects.link(sea); sbm = bmesh.new(); bmesh.ops.create_grid(sbm, x_segments=2, y_segments=2, size=20); sbm.to_mesh(sea.data)
    sm = mat("SeaPrev", (0.1, 0.35, 0.5), rough=0.1); sm.node_tree.nodes["Principled BSDF"].inputs["Transmission Weight"].default_value = 0.6; sea.data.materials.append(sm)
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.5; sun.rotation_euler = (0.8, 0.2, 0.9); sc.collection.objects.link(sun)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 40
    for nm, loc in (("boat", Vector((5.2, -4.6, 2.8))), ("boat_back", Vector((-4.0, 5.4, 3.2)))):
        cam.location = loc; tgt = Vector((0, 0, 0.7)); cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(PRE, nm + ".png"); bpy.ops.render.render(write_still=True); print("✔", nm)
