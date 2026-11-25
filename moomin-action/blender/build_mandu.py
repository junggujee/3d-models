"""맨두 — 왕실근위대 엘리트 하프물범(사용자 인형 사진 6장: 엎드린 하얀 하프물범 봉제 인형).
모양: 위에서 보면 앞이 넓은 달걀, 옆에서 보면 등이 둥글고 길쭉한 빵 모양(바닥 평평), 정면은 거의 동그란 얼굴.
      얼굴: 까만 구슬 눈(넓게) · 눈 위 갈색 점 눈썹 · 갈색 부채꼴 역삼각 코 · 코 밑에서 갈라져 넓게 웃는 ω 입 · 양쪽 수염 2가닥 ·
      정수리~등 가운데 솔기 + 턱 아래 가로 솔기. 옆구리 아래 앞지느러미 2 · 뒤로 V 자로 벌어진 꼬리지느러미(살짝 회녹색).
근위대 차림: 빨간 근위대 모자(금 띠 · 검은 챙 · 금 별 휘장 · 흰 깃털) · 파란 띠(어깨띠 고리) + 금 훈장.
크기: 길이 1.75m · 폭 0.95m · 높이 0.85m(모자 꼭대기 1.25m). 앞 = -Y.
뼈: root → body → head(앞쪽, 피부는 y 로 부드럽게) · eye.L/R · flip.L/R · tail
애니메이션(30fps): Idle61 Walk25 Talk31 Yes30 No36 Salute40(경례) Laugh40 Windup20 Attack1 24(배치기, 타격 11) Attack2 24(지느러미 찰싹, 11)
                   Attack3 30(근위대 굴러가기 — 몸이 한 바퀴, 14) Guard12(루프) Dodge16 Hit18 Death40
출력: unity/Assets/Models/Mandu.fbx · Resources/UI/Portraits/mandu_normal.png(--portrait) · previews/mandu_*.png(--preview)
실행: <bpy 파이썬> blender/build_mandu.py [--preview] [--portrait]"""
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh
from mathutils import Vector, Matrix, Euler, Quaternion
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews"); UNITY = os.path.join(ROOT, "unity", "Assets", "Models")
FURTEX = os.path.join(UNITY, "Textures", "Fur_Normal.png")
reset_scene()

def M_(n, c, **kw): m = mat(n, c, **kw); m.diffuse_color = (*c, 1); return m
FUR = M_("Mandu_Fur", (0.93, 0.92, 0.86), rough=1.0, sheen=1.0); FLIP = M_("Mandu_Flipper", (0.84, 0.86, 0.8), rough=1.0, sheen=0.8)
SEAM = M_("Mandu_Seam", (0.8, 0.78, 0.7), rough=1.0); EYE = M_("Mandu_Eye", (0.01, 0.01, 0.015), rough=0.08); BROWN = M_("Mandu_Brown", (0.33, 0.18, 0.08), rough=0.9)
RED = M_("Mandu_CapRed", (0.78, 0.1, 0.12), rough=0.6); GOLD = M_("Mandu_Gold", (1.0, 0.78, 0.3), rough=0.3, metal=0.9); BLACK = M_("Mandu_Visor", (0.05, 0.05, 0.06), rough=0.25)
PLUME = M_("Mandu_Plume", (0.98, 0.98, 1.0), rough=1.0, sheen=1.0); SASH = M_("Mandu_Sash", (0.15, 0.3, 0.75), rough=0.7, sheen=0.4)
MATS = [FUR, FLIP, SEAM, EYE, BROWN, RED, GOLD, BLACK, PLUME, SASH]; I = {m.name: i for i, m in enumerate(MATS)}
if os.path.exists(FURTEX):
    for m_, sc_ in ((FUR, 10), (FLIP, 7)):
        nt = m_.node_tree; b = nt.nodes["Principled BSDF"]; img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(FURTEX); img.image.colorspace_settings.name = "Non-Color"
        mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (sc_, sc_, 1); tc = nt.nodes.new("ShaderNodeTexCoord"); nm = nt.nodes.new("ShaderNodeNormalMap")
        nt.links.new(tc.outputs["UV"], mp.inputs[0]); nt.links.new(mp.outputs[0], img.inputs[0]); nt.links.new(img.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"])

def smooth(a, b, x): t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
# ── 몸: 늘인 공 → 뒤로 갈수록 가늘고 낮게, 앞은 둥글고 높게, 바닥은 평평 ─────────
BC, BR = Vector((0.0, 0.06, 0.42)), Vector((0.48, 0.84, 0.42))
bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=72, v_segments=44, radius=1.0, calc_uvs=True)
for v in bm.verts:
    x, y, z = v.co.x * BR.x, v.co.y * BR.y, v.co.z * BR.z
    back = smooth(-0.1, 0.84, y); x *= 1 - 0.34 * back; z = z * (1 - 0.3 * back) - 0.07 * back * (1 if z > 0 else 0)
    front = smooth(0.0, -0.84, y); z *= 1 + 0.06 * front; x *= 1 + 0.03 * front
    zz = BC.z + z
    if zz < 0.1: zz = 0.1 - (0.1 - zz) * 0.3                                   # 바닥 평평
    v.co = Vector((x, BC.y + y, zz))
for f in bm.faces: f.smooth = True; f.material_index = I["Mandu_Fur"]
uvl = bm.loops.layers.uv.active
if uvl:
    for f in bm.faces:
        for l in f.loops: l[uvl].uv = (l[uvl].uv.x * 4, l[uvl].uv.y * 2)
body = to_object("body", bm, MATS, "SKIN")
BVH = BVHTree.FromObject(body, bpy.context.evaluated_depsgraph_get())
def front_pt(x, z, off=0.0):
    """정면(-Y)에서 쏜 광선이 몸에 닿는 점 + 법선 방향으로 off"""
    loc, n, _, _ = BVH.ray_cast(Vector((x, -3.0, z)), Vector((0, 1, 0)), 10.0)
    if loc is None: return Vector((x, -0.8, z)), Vector((0, -1, 0))
    return loc + n * off, n
def top_pt(x, y, off=0.0):
    loc, n, _, _ = BVH.ray_cast(Vector((x, y, 3.0)), Vector((0, 0, -1)), 10.0)
    if loc is None: return Vector((x, y, 0.8)), Vector((0, 0, 1))
    return loc + n * off, n
def side_pt(s, y, z, off=0.0):
    loc, n, _, _ = BVH.ray_cast(Vector((3.0 * s, y, z)), Vector((-s, 0, 0)), 10.0)
    if loc is None: return Vector((0.45 * s, y, z)), Vector((s, 0, 0))
    return loc + n * off, n
parts = []
def add(name, b_, group): o = to_object(name, b_, MATS, group); parts.append(o); return o
def facing(n): return Vector((0, -1, 0)).rotation_difference(n).to_matrix()
def ell(c, r, rot=None, mi=0, seg=20, rings=12):
    b_ = bmesh.new(); bmesh.ops.create_uvsphere(b_, u_segments=seg, v_segments=rings, radius=1.0)
    R3 = rot if rot is not None else Matrix.Identity(3)
    for v in b_.verts: v.co = Vector(c) + R3 @ Vector((v.co.x * r[0], v.co.y * r[1], v.co.z * r[2]))
    for f in b_.faces: f.smooth = True; f.material_index = mi
    return b_
def tube_on(pts, r, mi, group, name, sides=6):
    tb, _ = tube(pts, lambda t: r, sides=sides, mat_idx=mi); return add(name, tb, group)

# ── 얼굴 ─────────────────────────────────────────────────
EX, EZ = 0.205, 0.5
for s, side in ((1, "L"), (-1, "R")):
    p, n = front_pt(EX * s, EZ, 0.012); add(f"Eye{side}", ell(p, (0.037, 0.023, 0.039), facing(n), I["Mandu_Eye"]), f"eye.{side}")
    p, n = front_pt(0.222 * s, 0.598, 0.004); add(f"Brow{side}", ell(p, (0.021, 0.008, 0.011), facing(n), I["Mandu_Brown"]), "head")
    for k, (z0, z1, x1) in enumerate(((0.44, 0.47, 0.37), (0.39, 0.395, 0.38))):   # 수염 2가닥
        pts = [front_pt(s * (0.28 + (x1 - 0.28) * t), z0 + (z1 - z0) * t, 0.006)[0] for t in [i / 6 for i in range(7)]]
        tube_on(pts, 0.007, I["Mandu_Brown"], "head", f"Whisker{side}{k}", 5)
# 코(부채꼴 역삼각) — 얇은 세모 기둥
nc, nn = front_pt(0.0, 0.44, 0.004); up = Vector((0, 0, 1)); sx = Vector((1, 0, 0))
tri = [nc + sx * -0.055 + up * 0.03, nc + sx * 0.055 + up * 0.03, nc + up * -0.04]
nb = bmesh.new(); A_ = [nb.verts.new(p) for p in tri]; B_ = [nb.verts.new(p + nn * 0.012) for p in tri]
for q in ([A_[0], A_[1], A_[2]], [B_[2], B_[1], B_[0]], [A_[0], B_[0], B_[1], A_[1]], [A_[1], B_[1], B_[2], A_[2]], [A_[2], B_[2], B_[0], A_[0]]): nb.faces.new(q).material_index = I["Mandu_Brown"]
add("Nose", nb, "head")
for s, side in ((1, "L"), (-1, "R")):                                              # ω 입: 코 밑에서 갈라져 옆으로 넓게 웃음
    ctrl = [(0.0, 0.405), (0.015 * s, 0.380), (0.048 * s, 0.364), (0.088 * s, 0.360), (0.124 * s, 0.372), (0.152 * s, 0.390)]
    pts = [front_pt(x, z, 0.006)[0] for x, z in ctrl]; tube_on(pts, 0.009, I["Mandu_Brown"], "head", f"Mouth{side}", 5)
# 솔기: 정수리~등 가운데 · 턱 아래 가로
pts = [front_pt(0, z, 0.003)[0] for z in (0.3, 0.55, 0.7)] + [top_pt(0, y, 0.003)[0] for y in [-0.6 + 1.3 * t / 10 for t in range(11)]]
tube_on(pts, 0.006, I["Mandu_Seam"], "SKIN", "SeamTop", 4)
pts = [front_pt(x, 0.3 - 0.02 * (x / 0.45) ** 2, 0.003)[0] for x in [-0.42 + 0.84 * t / 16 for t in range(17)]]
tube_on(pts, 0.006, I["Mandu_Seam"], "head", "SeamChin", 4)
# 지느러미: 앞(옆구리 아래, 바깥·뒤로) · 꼬리(뒤로 V)
FLIPJ = {}
for s, side in ((1, "L"), (-1, "R")):
    root_, n = side_pt(s, -0.22, 0.14, -0.03); FLIPJ[side] = (root_, root_ + Vector((0.12 * s, 0.12, -0.02)))
    add(f"Flipper{side}", ell(root_ + Vector((0.1 * s, 0.1, -0.02)), (0.1, 0.17, 0.065), Matrix.Rotation(0.5 * s, 3, "Z") @ Matrix.Rotation(-0.25 * s, 3, "Y"), I["Mandu_Flipper"]), f"flip.{side}")
TJ = Vector((0, 0.86, 0.22))
for s in (1, -1):
    add(f"Tail{'L' if s > 0 else 'R'}", ell(TJ + Vector((0.1 * s, 0.17, 0.05)), (0.08, 0.2, 0.05), Matrix.Rotation(-0.45 * s, 3, "Z") @ Matrix.Rotation(-0.35, 3, "X"), I["Mandu_Flipper"]), "tail")
# ── 근위대 차림 ───────────────────────────────────────────
hp_, _ = top_pt(0, -0.28, 0.0)                                                     # 모자: 이마 위(앞쪽 정수리)
cap = bmesh.new()
def cyl(b_, p0, p1, r0, r1, mi, seg=24):
    d = p1 - p0; vs = bmesh.ops.create_cone(b_, cap_ends=True, segments=seg, radius1=r0, radius2=r1, depth=d.length)["verts"]
    bmesh.ops.transform(b_, matrix=Matrix.Translation((p0 + p1) / 2) @ Vector((0, 0, 1)).rotation_difference(d.normalized()).to_matrix().to_4x4(), verts=vs)
    for v in vs:
        for f in v.link_faces: f.material_index = mi
tilt = Vector((0, 0.12, 1)).normalized(); base = hp_ - tilt * 0.035
cyl(cap, base, base + tilt * 0.3, 0.17, 0.155, I["Mandu_CapRed"]); cyl(cap, base + tilt * 0.02, base + tilt * 0.08, 0.176, 0.172, I["Mandu_Gold"])
cyl(cap, base + tilt * 0.295, base + tilt * 0.315, 0.165, 0.16, I["Mandu_Gold"])
vis = ell(base + tilt * 0.02 + Vector((0, -0.13, -0.01)), (0.17, 0.11, 0.018), Matrix.Rotation(0.2, 3, "X"), I["Mandu_Visor"])
me_ = bpy.data.meshes.new("v"); vis.to_mesh(me_); vis.free(); cap.from_mesh(me_); bpy.data.meshes.remove(me_)
sp = base + tilt * 0.17 + Vector((0, -0.165, 0))                                    # 금 별 휘장
st = [];
for k in range(10):
    a = math.pi / 2 + k * math.pi / 5; r = 0.055 if k % 2 == 0 else 0.024; st.append(sp + Vector((math.cos(a) * r, 0, math.sin(a) * r)))
F_ = [cap.verts.new(p) for p in st]; B2 = [cap.verts.new(p + Vector((0, 0.012, 0))) for p in st]; c0 = cap.verts.new(sp + Vector((0, -0.012, 0))); c1 = cap.verts.new(sp + Vector((0, 0.012, 0)))
for k in range(10):
    j = (k + 1) % 10; cap.faces.new((c0, F_[j], F_[k])).material_index = I["Mandu_Gold"]; cap.faces.new((c1, B2[k], B2[j])).material_index = I["Mandu_Gold"]
    cap.faces.new((F_[k], F_[j], B2[j], B2[k])).material_index = I["Mandu_Gold"]
pl = ell(base + tilt * 0.36 + Vector((0.07, 0.02, 0.06)), (0.05, 0.05, 0.13), Matrix.Rotation(-0.35, 3, "Y"), I["Mandu_Plume"])
me_ = bpy.data.meshes.new("p"); pl.to_mesh(me_); pl.free(); cap.from_mesh(me_); bpy.data.meshes.remove(me_)
add("GuardCap", cap, "head")
ring = []                                                                          # 파란 어깨띠(목 둘레 고리, 살짝 기울게)
for k in range(41):
    a = 2 * math.pi * k / 40; yv = -0.08 + 0.06 * math.sin(a)
    if math.sin(a) > -0.95:
        loc, n, _, _ = BVH.ray_cast(Vector((math.cos(a) * 2.0, yv, 0.42 + math.sin(a) * 2.0)), -Vector((math.cos(a), 0, math.sin(a))), 5.0)
        if loc is not None and loc.z > 0.12: ring.append(loc + n * 0.012)
tube_on(ring, 0.03, I["Mandu_Sash"], "SKIN", "Sash", 6)
mp_, mn = side_pt(1, -0.08, 0.45, 0.035); add("Medal", ell(mp_, (0.012, 0.045, 0.045), None, I["Mandu_Gold"], 16, 10), "SKIN")
mp2, _ = side_pt(1, -0.08, 0.56, 0.03); add("MedalRibbon", ell(mp2, (0.01, 0.03, 0.04), None, I["Mandu_CapRed"], 10, 6), "SKIN")


# ── 파란 야구방망이(맨두의 무기 — 사진: 파란 배럴 · 검은 홈 손잡이 · 은색 띠 · 파란 끝마개) ──
BATBLUE = M_("Mandu_BatBlue", (0.07, 0.48, 0.9), rough=0.35); BATGRIP = M_("Mandu_BatGrip", (0.035, 0.035, 0.045), rough=0.25)
BATRING = M_("Mandu_BatRing", (0.86, 0.88, 0.92), rough=0.12, metal=0.95)
MATS.extend([BATBLUE, BATGRIP, BATRING]); I = {m.name: i for i, m in enumerate(MATS)}
GRIP = Vector((-0.58, -0.44, 0.47))                        # 오른 지느러미가 쥐는 자리(몸 바깥·앞쪽 — 휘둘러도 몸을 안 뚫게)
BATDIR = Vector((-0.22, -0.30, 1.0)).normalized()          # 들었을 때 방망이가 뻗는 쪽(위·앞·바깥)
HOLDIR = Vector((0.27, 1.0, -0.03)).normalized()            # 찼을 때 — 몸 오른쪽 바깥에 뒤로 눕힘
HOLOFF = Vector((0.03, 0.16, -0.06))                      # 찼을 때 옆구리 바깥을 타고 뒤로
FALLDIR = Vector((-0.92, 0.36, 0.06)).normalized()         # 패배 — 바닥에 뒹굶
FALLOFF = Vector((-0.16, -0.22, -0.40))
bat = bmesh.new()
def bseg(z0, z1, r0, r1, mi, seg=16): cyl(bat, Vector((0, 0, z0)), Vector((0, 0, z1)), r0, r1, mi, seg)
bseg(-0.022, 0.012, 0.050, 0.047, I["Mandu_BatBlue"]); bseg(0.012, 0.042, 0.035, 0.030, I["Mandu_BatBlue"])   # 끝마개(둥근 손잡이 혹)
bseg(0.042, 0.400, 0.031, 0.031, I["Mandu_BatGrip"])                                                          # 검은 손잡이
for k in range(14): zc = 0.058 + k * 0.0245; bseg(zc, zc + 0.014, 0.0365, 0.0365, I["Mandu_BatGrip"], 14)     # 손가락 홈(도톰한 고리)
bseg(0.400, 0.426, 0.0375, 0.0375, I["Mandu_BatRing"])                                                        # 은색 띠
bseg(0.426, 0.610, 0.033, 0.054, I["Mandu_BatBlue"]); bseg(0.610, 0.900, 0.058, 0.063, I["Mandu_BatBlue"])    # 목 → 배럴
bseg(0.900, 0.936, 0.063, 0.058, I["Mandu_BatBlue"]); bseg(0.936, 0.952, 0.058, 0.030, I["Mandu_BatBlue"])    # 끝 둥글게
bmesh.ops.transform(bat, matrix=Matrix.Translation(GRIP) @ Vector((0, 0, 1)).rotation_difference(BATDIR).to_matrix().to_4x4(), verts=bat.verts[:])
for f in bat.faces: f.smooth = True
add("Bat", bat, "bat")

# ── 뼈대 · 피부 ───────────────────────────────────────────
BONES = [("root", None, Vector((0, 0, 0)), Vector((0, 0, 0.1))), ("body", "root", Vector((0, 0.4, 0.4)), Vector((0, -0.05, 0.4))),
         ("head", "body", Vector((0, -0.05, 0.42)), Vector((0, -0.7, 0.46))), ("tail", "body", TJ, TJ + Vector((0, 0.3, 0.05))), ("bat", "body", GRIP, GRIP + BATDIR * 0.3)]
for s, side in ((1, "L"), (-1, "R")):
    e, _ = front_pt(EX * s, EZ, -0.01); BONES.append((f"eye.{side}", "head", e, e + Vector((0, 0, 0.06))))
    BONES.append((f"flip.{side}", "body", *FLIPJ[side]))
arm = make_armature("ManduRig", BONES)
for ob in [body] + parts:
    if "SKIN" not in ob.vertex_groups: bind(ob, arm); continue
    skin = ob.vertex_groups["SKIN"]; gb = ob.vertex_groups.new(name="body"); gh = ob.vertex_groups.new(name="head")
    for v in ob.data.vertices:
        w = smooth(0.12, -0.32, v.co.y)
        if w > 0: gh.add([v.index], w, "REPLACE")
        if w < 1: gb.add([v.index], 1 - w, "REPLACE")
    ob.vertex_groups.remove(skin); bind(ob, arm)

# ── 애니메이션(채널 → 매 프레임 키) ─────────────────────────
def R(bn, pitch=0.0, roll=0.0, yaw=0.0):
    M = arm.data.bones[bn].matrix_local.to_3x3(); W = Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(pitch, 3, "X") @ Matrix.Rotation(-roll, 3, "Y")
    return M.inverted() @ W @ M
def track(keys, default=0.0):
    if not keys: return lambda f: default
    keys = sorted(keys)
    def at(f):
        if f <= keys[0][0]: return keys[0][1]
        for (f0, v0), (f1, v1) in zip(keys, keys[1:]):
            if f <= f1: u = (f - f0) / (f1 - f0); u = u * u * (3 - 2 * u); return v0 + (v1 - v0) * u
        return keys[-1][1]
    return at
CH = ("z", "fwd", "side", "lean", "roll", "yaw", "hp", "hy", "hr", "sq", "fl", "fr", "fls", "frs", "eye", "tail", "bat", "batp", "baty", "batr", "batfall", "grow")
DEF = {"eye": 1.0, "grow": 1.0}
HOLQ = BATDIR.rotation_difference(HOLDIR); FALLQ = BATDIR.rotation_difference(FALLDIR); MBAT = None
def anim(name, length, spec, loop=True):
    """z 튐 · fwd 앞으로(+) · side 옆 · lean 숙임(+ 앞) · roll 옆 구르기(한 바퀴 = 2π, 연속) · yaw · hp/hy/hr 머리 · sq 눌림(+)/늘어남(-)
       fl/fr 앞지느러미 들기(+ 바깥 위) · fls/frs 앞뒤 휘젓기(+ 앞) · eye 눈 세로(0.1 감음 ~ 1.35 놀람) · tail 꼬리 들기
       bat 방망이 0 = 옆에 참 / 1 = 손에 듦 · batp 내려치기(+ 앞아래) · baty 옆으로 휘두르기 · batr 비틀기 · batfall 놓쳐 바닥에 떨어뜨림 · grow 몸 전체 배율."""
    global MBAT
    if MBAT is None: MBAT = arm.data.bones["bat"].matrix_local.to_3x3()
    tr = {k: track(spec.get(k, []), DEF.get(k, 0.0)) for k in CH}; act = new_action(arm, name, length); prev = {}
    def K(bone, f, Mx, loc=None, scale=None):
        e = Mx.to_euler("XYZ", prev[bone]) if bone in prev else Mx.to_euler("XYZ"); prev[bone] = e; key(arm, bone, f, rot=e, loc=loc, scale=scale)
    for f in range(1, length + 1):
        g = {k: tr[k](f) for k in CH}
        gr = g["grow"]; K("root", f, R("root", 0, 0, g["yaw"]), loc=Vector((g["side"], g["z"], -g["fwd"])), scale=Vector((gr, gr, gr)))     # root 로컬: y = 위, z = 뒤(+Y 월드)
        sq = g["sq"]; K("body", f, R("body", g["lean"], g["roll"]), scale=Vector((1 + 0.5 * sq, 1 + 0.3 * sq, 1 - sq)))
        K("head", f, R("head", g["hp"], g["hr"], g["hy"]))
        for side, s in (("L", 1), ("R", -1)):
            raise_ = g["fl"] if side == "L" else g["fr"]; sw = g["fls"] if side == "L" else g["frs"]
            K(f"flip.{side}", f, R(f"flip.{side}", sw, raise_ * s))
            key(arm, f"eye.{side}", f, scale=Vector((1, max(0.08, g["eye"]), 1)))
        K("tail", f, R("tail", -g["tail"]))
        bb = max(0.0, min(1.0, g["bat"])); bf = max(0.0, min(1.0, g["batfall"]))
        q = HOLQ.slerp(Quaternion(), bb).slerp(FALLQ, bf)
        W = Matrix.Rotation(g["baty"], 3, "Z") @ Matrix.Rotation(g["batp"], 3, "X") @ Matrix.Rotation(-g["batr"], 3, "Y") @ q.to_matrix()
        K("bat", f, MBAT.inverted() @ W @ MBAT, loc=MBAT.inverted() @ (HOLOFF * (1 - bb) + FALLOFF * bf))
    finish_action(arm, act, loop=loop); act.frame_start, act.frame_end = 1, length; return act
def held(n): return [(1, 1), (n, 1)]
def blink(n, at): return [(1, 1), (at - 2, 1), (at, 0.1), (at + 2, 1), (n, 1)]
def osc(n, amp, cyc=1, ph=0.0, base=0.0): return [(1 + round(k * (n - 1) / 24), base + amp * math.sin(2 * math.pi * cyc * k / 24 + ph)) for k in range(25)]
A = {}
A["Idle"] = anim("Idle", 61, {"sq": osc(61, 0.025), "hr": osc(61, 0.04, 1, 0.5), "fl": osc(61, 0.06, 2), "fr": osc(61, 0.06, 2, 1), "eye": blink(61, 44), "tail": osc(61, 0.12)})
A["Walk"] = anim("Walk", 25, {"z": [(1, 0), (7, 0.06), (13, 0), (19, 0.06), (25, 0)], "sq": [(1, 0.08), (7, -0.06), (13, 0.08), (19, -0.06), (25, 0.08)], "lean": osc(25, 0.05, 2),
                              "fl": osc(25, 0.25, 2, 0, 0.1), "fr": osc(25, 0.25, 2, 0, 0.1), "fls": osc(25, 0.4, 2), "frs": osc(25, 0.4, 2), "tail": osc(25, 0.35, 2, 1.2), "roll": osc(25, 0.05)})
A["Run"] = anim("Run", 17, {"z": [(1, 0), (5, 0.09), (9, 0), (13, 0.09), (17, 0)], "sq": [(1, 0.1), (5, -0.08), (9, 0.1), (13, -0.08), (17, 0.1)], "lean": [(1, 0.1), (17, 0.1)],
                             "fl": osc(17, 0.35, 2, 0, 0.15), "fr": osc(17, 0.35, 2, 0, 0.15), "fls": osc(17, 0.6, 2), "frs": osc(17, 0.6, 2), "tail": osc(17, 0.5, 2, 1.2)})
A["Talk"] = anim("Talk", 31, {"hp": osc(31, 0.07, 2), "hr": osc(31, 0.05), "fl": [(1, 0.1), (10, 0.5), (20, 0.2), (31, 0.1)], "fls": osc(31, 0.3), "sq": osc(31, 0.03, 2), "eye": blink(31, 22)})
A["Yes"] = anim("Yes", 30, {"hp": [(1, 0), (6, 0.3), (11, -0.04), (16, 0.28), (21, -0.02), (30, 0)], "sq": [(1, 0), (6, 0.06), (11, 0), (16, 0.06), (30, 0)], "eye": [(1, 1), (6, 0.4), (21, 0.4), (30, 1)]}, loop=False)
A["No"] = anim("No", 36, {"hy": [(1, 0), (6, 0.32), (12, -0.32), (18, 0.3), (24, -0.28), (30, 0.08), (36, 0)], "fl": [(1, 0), (8, 0.8), (30, 0.8), (36, 0)], "fr": [(1, 0), (8, 0.8), (30, 0.8), (36, 0)],
                          "fls": [(1, 0), (8, 0.6), (14, -0.2), (20, 0.6), (26, -0.2), (36, 0)], "frs": [(1, 0), (8, -0.2), (14, 0.6), (20, -0.2), (26, 0.6), (36, 0)], "eye": [(1, 1), (6, 0.6), (30, 0.6), (36, 1)]}, loop=False)
A["Salute"] = anim("Salute", 40, {"fr": [(1, 0), (8, 2.3), (32, 2.3), (40, 0)], "frs": [(1, 0), (8, 0.9), (32, 0.9), (40, 0)], "lean": [(1, 0), (6, -0.12), (32, -0.12), (40, 0)],
                                  "hp": [(1, 0), (6, -0.1), (32, -0.1), (40, 0)], "z": [(1, 0), (5, 0.04), (9, 0), (40, 0)], "sq": [(1, 0), (5, -0.08), (9, 0.03), (13, 0), (40, 0)], "tail": [(1, 0), (6, 0.5), (32, 0.5), (40, 0)]}, loop=False)
A["Laugh"] = anim("Laugh", 40, {"lean": [(1, 0), (8, -0.18), (34, -0.16), (40, 0)], "eye": [(1, 1), (6, 0.1), (36, 0.1), (40, 1)], "sq": osc(40, 0.06, 4), "fl": osc(40, 0.4, 4, 0, 0.4), "fr": osc(40, 0.4, 4, 0, 0.4)}, loop=False)
A["Windup"] = anim("Windup", 20, {"bat": held(20), "batr": [(1, 0.2), (12, 0.95), (20, 0.9)], "batp": [(1, 0.1), (12, -0.5), (20, -0.45)], "lean": [(1, 0), (12, -0.22), (20, -0.2)], "sq": [(1, 0), (12, -0.1), (20, -0.08)], "fl": [(1, 0), (12, 0.7), (20, 0.65)], "fr": [(1, 0), (12, 0.7), (20, 0.65)],
                                  "eye": [(1, 1), (10, 0.6), (20, 0.6)], "tail": [(1, 0), (12, 0.6), (20, 0.6)]})
A["Attack1"] = anim("Attack1", 24, {"bat": held(24), "lean": [(1, -0.15), (8, -0.25), (11, 0.3), (16, 0.22), (24, 0)], "fwd": [(1, 0), (8, -0.05), (11, 0.35), (16, 0.3), (24, 0)], "z": [(1, 0), (8, 0.15), (11, 0.02), (24, 0)],
                                    "sq": [(1, 0), (8, -0.12), (11, 0.18), (15, 0), (24, 0)], "fl": [(1, 0.4), (11, -0.3), (24, 0)], "fr": [(1, 0.4), (11, -0.3), (24, 0)], "eye": [(1, 0.7), (11, 0.4), (24, 1)]}, loop=False)
A["Attack2"] = anim("Attack2", 24, {"bat": held(24), "fr": [(1, 0), (7, 2.0), (11, 0.4), (16, 0.5), (24, 0)], "frs": [(1, 0), (7, -0.4), (11, 1.2), (16, 1.0), (24, 0)], "yaw": [(1, 0), (7, 0.25), (11, -0.3), (24, 0)],
                                    "roll": [(1, 0), (7, -0.12), (11, 0.15), (24, 0)], "hy": [(1, 0), (7, 0.15), (11, -0.2), (24, 0)], "eye": [(1, 1), (9, 0.5), (24, 1)]}, loop=False)
A["Attack3"] = anim("Attack3", 30, {"bat": held(30), "batp": [(1, 0.2), (6, -0.4), (20, 0.5), (30, 0.2)], "roll": [(1, 0), (6, -0.3), (20, 2 * math.pi + 0.0), (30, 2 * math.pi)], "fwd": [(1, 0), (6, -0.05), (14, 0.5), (20, 0.6), (30, 0)], "z": [(1, 0), (6, 0.05), (14, 0.15), (20, 0), (30, 0)],
                                    "fl": [(1, 0), (6, -0.2), (20, -0.2), (30, 0)], "fr": [(1, 0), (6, -0.2), (20, -0.2), (30, 0)], "eye": [(1, 1), (6, 0.1), (22, 0.1), (30, 1)], "sq": [(1, 0), (6, 0.1), (14, -0.05), (22, 0.1), (30, 0)]}, loop=False)
A["Bat1"] = anim("Bat1", 24, {"bat": held(24), "batr": [(1, 0.5), (7, 1.15), (11, -1.0), (16, -1.5), (24, 0.2)], "batp": [(1, 0.2), (7, -0.6), (11, 1.5), (16, 1.7), (24, 0.2)],
                              "yaw": [(1, 0.1), (7, 0.3), (11, -0.35), (24, 0)], "fr": [(1, 1.5), (7, 1.8), (11, 1.3), (24, 1.4)], "frs": [(1, 0.3), (7, -0.3), (11, 0.9), (24, 0.3)],
                              "fl": [(1, 1.1), (24, 1.1)], "lean": [(1, 0), (7, -0.12), (11, 0.2), (24, 0.05)], "fwd": [(1, 0), (11, 0.12), (24, 0)], "eye": [(1, 1), (9, 0.5), (24, 1)]}, loop=False)
A["Bat2"] = anim("Bat2", 24, {"bat": held(24), "batr": [(1, -0.6), (7, -1.35), (11, 1.2), (16, 1.6), (24, 0.2)], "batp": [(1, 0.6), (7, 0.3), (11, 1.4), (16, 1.5), (24, 0.2)],
                              "yaw": [(1, -0.1), (7, -0.3), (11, 0.35), (24, 0)], "fr": [(1, 1.4), (7, 1.75), (11, 1.3), (24, 1.4)], "frs": [(1, 0.3), (7, 0.9), (11, -0.2), (24, 0.3)],
                              "fl": [(1, 1.1), (24, 1.1)], "lean": [(1, 0), (7, -0.1), (11, 0.22), (24, 0.05)], "fwd": [(1, 0), (11, 0.12), (24, 0)], "eye": [(1, 1), (9, 0.5), (24, 1)]}, loop=False)
A["Bat3"] = anim("Bat3", 28, {"bat": held(28), "batp": [(1, 0.2), (8, -1.4), (13, 1.85), (18, 1.7), (28, 0.2)], "batr": [(1, 0.25), (8, 0.35), (13, 0.1), (28, 0.25)],
                              "fr": [(1, 1.4), (8, 2.1), (13, 0.9), (28, 1.4)], "frs": [(1, 0.3), (8, -0.5), (13, 1.1), (28, 0.3)], "fl": [(1, 1.0), (8, 1.5), (13, 0.8), (28, 1.0)],
                              "lean": [(1, 0), (8, -0.3), (13, 0.35), (20, 0.25), (28, 0.05)], "z": [(1, 0), (8, 0.14), (13, 0), (28, 0)], "sq": [(1, 0), (8, -0.1), (14, 0.2), (20, 0), (28, 0)],
                              "fwd": [(1, 0), (8, -0.06), (13, 0.18), (28, 0)], "eye": [(1, 1), (8, 1.25), (13, 0.3), (28, 1)]}, loop=False)
A["HomeRun"] = anim("HomeRun", 40, {"bat": held(40), "batr": [(1, 0.4), (14, 1.5), (20, 1.45), (23, -1.2), (30, -2.3), (40, 0.3)], "batp": [(1, 0.15), (14, -1.0), (20, -0.95), (23, 1.55), (30, 1.8), (40, 0.15)],
                                    "baty": [(1, 0), (14, 0.3), (23, -0.3), (40, 0)], "yaw": [(1, 0.15), (14, 0.55), (20, 0.5), (23, -0.5), (30, -0.75), (40, 0)],
                                    "fr": [(1, 1.4), (14, 2.0), (23, 1.2), (30, 1.0), (40, 1.4)], "frs": [(1, 0.3), (14, -0.7), (23, 1.2), (40, 0.3)], "fl": [(1, 1.1), (14, 1.6), (23, 0.9), (40, 1.1)],
                                    "lean": [(1, 0), (14, -0.35), (20, -0.32), (23, 0.3), (30, 0.2), (40, 0.05)], "z": [(1, 0), (14, 0.05), (20, 0.2), (23, 0.05), (40, 0)],
                                    "sq": [(1, 0), (14, -0.16), (22, 0.22), (30, 0), (40, 0)], "fwd": [(1, 0), (14, -0.12), (23, 0.3), (30, 0.25), (40, 0)],
                                    "eye": [(1, 1), (10, 0.35), (22, 1.3), (30, 0.6), (40, 1)], "tail": [(1, 0), (14, 0.7), (23, -0.2), (40, 0)]}, loop=False)
A["Charge"] = anim("Charge", 30, {"bat": held(30), "batp": [(1, 0.3), (8, -0.3), (14, 1.25), (24, 1.2), (30, 0.4)], "batr": [(1, 0.3), (8, 0.7), (14, 0.15), (30, 0.3)],
                                  "lean": [(1, 0), (8, -0.2), (13, 0.42), (24, 0.38), (30, 0.1)], "fwd": [(1, 0), (8, -0.1), (14, 0.5), (24, 0.95), (30, 0.8)],
                                  "z": [(1, 0), (8, 0.06), (14, 0.12), (24, 0.03), (30, 0)], "sq": [(1, 0), (8, 0.12), (14, -0.12), (24, 0.08), (30, 0)],
                                  "fr": [(1, 1.3), (14, 1.0), (30, 1.2)], "fl": [(1, 0.4), (8, 0.9), (14, 0.5), (30, 0.6)], "eye": [(1, 1), (8, 0.4), (14, 1.3), (30, 1)],
                                  "tail": [(1, 0), (10, 0.8), (24, 0.5), (30, 0)]}, loop=False)
A["Chomp"] = anim("Chomp", 60, {"bat": [(1, 1), (8, 1), (14, 0), (46, 0), (54, 1), (60, 1)], "grow": [(1, 1), (8, 1.1), (16, 2.35), (36, 2.3), (46, 1.5), (54, 1), (60, 1)],
                                "sq": [(1, 0), (8, -0.2), (14, 0.12), (20, -0.06), (30, 0.1), (38, -0.22), (44, 0.1), (60, 0)], "lean": [(1, 0), (10, -0.25), (18, 0.35), (34, 0.3), (40, -0.3), (48, 0), (60, 0)],
                                "fwd": [(1, 0), (10, -0.1), (18, 0.4), (34, 0.36), (42, -0.15), (60, 0)], "z": [(1, 0), (12, 0.18), (18, 0), (60, 0)],
                                "fl": [(1, 0.3), (12, 1.9), (20, 0.2), (36, 0.25), (42, 1.6), (60, 0.3)], "fr": [(1, 0.3), (12, 1.9), (20, 0.2), (36, 0.25), (42, 1.6), (60, 0.3)],
                                "fls": [(1, 0), (12, 0.9), (20, -0.5), (42, 0.8), (60, 0)], "frs": [(1, 0), (12, 0.9), (20, -0.5), (42, 0.8), (60, 0)],
                                "eye": [(1, 1), (10, 1.3), (18, 0.15), (36, 0.15), (44, 1.25), (60, 1)], "hp": [(1, 0), (14, -0.3), (20, 0.25), (38, 0.2), (44, -0.35), (60, 0)],
                                "tail": [(1, 0), (14, 0.7), (36, 0.3), (60, 0)]}, loop=False)
A["TonCharge"] = anim("TonCharge", 30, {"bat": held(30), "sq": [(1, 0), (12, 0.3), (30, 0.28)], "z": [(1, 0), (12, -0.03), (30, -0.03)], "batp": [(1, 0.2), (12, -0.9), (30, -0.85)], "batr": [(1, 0.2), (12, 0.5), (30, 0.5)],
                                        "fl": [(1, 0), (12, 1.6), (30, 1.55)], "fr": [(1, 0), (12, 1.6), (30, 1.55)], "lean": [(1, 0), (12, -0.15), (30, -0.12)],
                                        "eye": [(1, 1), (10, 1.35), (30, 1.3)], "tail": [(1, 0), (12, 0.9), (30, 0.85)]})
A["MillionTon"] = anim("MillionTon", 150, {"bat": held(150),
    "z": [(1, 0), (12, -0.05), (18, 0.1), (34, 3.6), (46, 4.3), (56, 4.1), (62, 0), (150, 0)],
    "sq": [(1, 0.25), (12, 0.34), (20, -0.3), (34, -0.22), (46, 0), (56, -0.12), (62, 0.85), (70, 0.78), (125, 0.8), (138, 0.1), (150, 0)],
    "lean": [(1, -0.1), (20, -0.5), (40, -0.2), (50, 0.3), (58, 0.1), (62, 0), (150, 0)],
    "roll": [(1, 0), (34, 2.2), (46, 4.4), (56, 6.283185), (62, 6.283185), (150, 6.283185)],
    "batp": [(1, 1.2), (20, -0.4), (46, 0.6), (62, 1.5), (125, 1.5), (140, 0.4), (150, 0.3)],
    "fl": [(1, 1.5), (20, 2.2), (46, 1.8), (62, 0.15), (125, 0.15), (150, 0.4)], "fr": [(1, 1.5), (20, 2.2), (46, 1.8), (62, 0.15), (125, 0.15), (150, 0.4)],
    "fls": [(1, 0), (20, -0.6), (46, 0.4), (62, 1.0), (125, 0.95), (150, 0)], "frs": [(1, 0), (20, -0.6), (46, 0.4), (62, 1.0), (125, 0.95), (150, 0)],
    "eye": [(1, 1.3), (20, 0.5), (44, 1.35), (60, 1.35), (64, 0.1), (120, 0.1), (134, 0.7), (150, 1)],
    "tail": [(1, 0.9), (20, -0.3), (46, 0.5), (62, 0.1), (125, 0.1), (150, 0)],
    "hp": [(1, -0.1), (46, 0.3), (62, 0.05), (150, 0)]}, loop=False)
A["Guard"] = anim("Guard", 12, {"bat": held(12), "batp": [(1, 0.3), (6, 1.45), (12, 1.4)], "batr": [(1, 0.2), (6, -0.15), (12, -0.12)], "fl": [(1, 0), (6, 1.0), (12, 0.95)], "fr": [(1, 0), (6, 1.0), (12, 0.95)], "fls": [(1, 0), (6, 1.3), (12, 1.25)], "frs": [(1, 0), (6, 1.3), (12, 1.25)],
                                "hp": [(1, 0), (6, 0.2), (12, 0.18)], "sq": [(1, 0), (6, 0.1), (12, 0.09)], "eye": [(1, 1), (6, 0.5), (12, 0.5)]})
A["Dodge"] = anim("Dodge", 16, {"bat": held(16), "side": [(1, 0), (8, 0.15), (16, 0)], "roll": [(1, 0), (8, 0.6), (16, 0)], "z": [(1, 0), (6, 0.1), (12, 0), (16, 0)], "sq": [(1, 0), (4, -0.08), (12, 0.06), (16, 0)]}, loop=False)
A["Hit"] = anim("Hit", 18, {"bat": held(18), "lean": [(1, 0), (4, -0.25), (10, -0.1), (18, 0)], "sq": [(1, 0), (4, 0.15), (10, 0), (18, 0)], "eye": [(1, 1), (3, 0.1), (10, 0.4), (18, 1)], "fl": [(1, 0), (4, 0.6), (18, 0)], "fr": [(1, 0), (4, 0.6), (18, 0)]}, loop=False)
A["Death"] = anim("Death", 90, {"bat": [(1, 1), (90, 1)], "batfall": [(1, 0), (16, 0), (26, 1), (90, 1)],
    "lean": [(1, 0), (6, -0.35), (16, -0.2), (24, 0.15), (34, 0.42), (44, 0.46), (90, 0.44)],
    "yaw": [(1, 0), (10, 0.22), (20, -0.26), (30, 0.12), (40, 0), (90, 0)], "roll": [(1, 0), (10, 0.18), (22, -0.2), (34, 0.08), (44, 0), (90, 0)],
    "z": [(1, 0), (8, 0.08), (16, 0.02), (26, 0.1), (36, 0), (90, 0)],
    "sq": [(1, 0), (8, -0.12), (20, 0.1), (34, -0.05), (44, 0.66), (52, 0.6), (90, 0.62)],
    "hp": [(1, 0), (8, -0.25), (22, 0.1), (40, 0.5), (90, 0.5)], "hr": [(1, 0), (12, 0.25), (24, -0.2), (40, 0.08), (90, 0.08)],
    "fl": [(1, 0.6), (10, 1.3), (24, 0.5), (44, 1.5), (90, 1.5)], "fr": [(1, 1.3), (10, 1.6), (24, 0.4), (44, 1.5), (90, 1.5)],
    "fls": [(1, 0), (10, 0.5), (24, -0.4), (44, 0.9), (90, 0.9)], "frs": [(1, 0.3), (10, -0.5), (24, 0.6), (44, 0.9), (90, 0.9)],
    "eye": [(1, 1), (6, 1.3), (14, 0.5), (26, 0.12), (90, 0.1)],
    "tail": [(1, 0), (10, 0.8), (26, 0.2), (44, 0.1), (56, 0.45), (62, 0.1), (72, 0.35), (78, 0.08), (90, 0.05)]}, loop=False)

# ── 최종 크기: 민미(키 1.3m) 의 약 1.7배 — 길이 2.2m · 등높이 1.07m · 모자 끝 1.58m (고래고래보다는 현저히 작음)
SCALE = 1.26
arm.scale = (SCALE, SCALE, SCALE); bpy.context.view_layer.update()

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = Vector((0, 0, 0.5)) * SCALE
    preview(os.path.join(PRE, "mandu_front.png"), Vector((0.0, -3.2, 0.9)) * SCALE, c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "mandu_side.png"), Vector((3.4, -0.6, 0.9)) * SCALE, Vector((0, 0.1, 0.45)) * SCALE, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "mandu_threequarter.png"), Vector((-2.3, -2.4, 1.3)) * SCALE, c, arm_ob=arm, action=A["Salute"], frame=20)
    preview(os.path.join(PRE, "mandu_attack3.png"), Vector((2.6, -2.6, 1.2)) * SCALE, c, arm_ob=arm, action=A["Attack3"], frame=12)
    preview(os.path.join(PRE, "mandu_homerun.png"), Vector((-2.6, -3.0, 1.5)) * SCALE, c, arm_ob=arm, action=A["HomeRun"], frame=23)
    preview(os.path.join(PRE, "mandu_bat3.png"), Vector((2.9, -2.4, 1.6)) * SCALE, c, arm_ob=arm, action=A["Bat3"], frame=9)
    preview(os.path.join(PRE, "mandu_chomp.png"), Vector((0.5, -4.6, 1.8)) * SCALE, c, arm_ob=arm, action=A["Chomp"], frame=26)
    preview(os.path.join(PRE, "mandu_millionton.png"), Vector((3.4, -3.4, 1.0)) * SCALE, Vector((0, 0, 0.3)) * SCALE, arm_ob=arm, action=A["MillionTon"], frame=70)
    preview(os.path.join(PRE, "mandu_defeat.png"), Vector((2.6, -2.8, 1.1)) * SCALE, Vector((0, 0, 0.25)) * SCALE, arm_ob=arm, action=A["Death"], frame=72)

glb, fbx = export_all("Mandu", OUT, [arm, body] + parts)
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "Mandu.fbx"))
print("EXPORTED", fbx, "verts:", sum(len(o.data.vertices) for o in [body] + parts))

if "--portrait" in sys.argv:
    sc = bpy.context.scene
    for o in list(sc.objects):
        if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
    arm.animation_data_create(); arm.animation_data.action = A["Idle"]; sc.frame_set(1)
    w = bpy.data.worlds.new("PW"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 0.7; sc.world = w
    for loc, e in ((Vector((-1.5, -2.4, 2.0)) * SCALE, 150 * SCALE * SCALE), (Vector((1.8, 0.6, 1.6)) * SCALE, 80 * SCALE * SCALE)):
        L = bpy.data.objects.new("L", bpy.data.lights.new("L", "AREA")); L.data.energy = e; L.data.size = 1.6; L.location = loc; sc.collection.objects.link(L)
        L.rotation_euler = (Vector((0, -0.4, 0.5)) * SCALE - loc).to_track_quat("-Z", "Y").to_euler()
    cam = bpy.data.objects.new("PC", bpy.data.cameras.new("PC")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 50
    tgt = Vector((0, -0.4, 0.62)) * SCALE; cam.location = tgt + Vector((-0.55, -2.4, 0.25)) * SCALE; cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.engine = "CYCLES"; sc.cycles.samples = 48; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True; sc.render.film_transparent = True
    sc.render.resolution_x, sc.render.resolution_y = 600, 720; sc.render.image_settings.color_mode = "RGBA"; sc.view_settings.view_transform = "Standard"
    sc.render.use_freestyle = True; sc.render.line_thickness_mode = "ABSOLUTE"; sc.render.line_thickness = 2.2
    fs = bpy.context.view_layer.freestyle_settings; ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("L")
    ls.select_by_visibility = True; ls.select_silhouette = True; ls.select_border = True; ls.select_crease = False
    ls.linestyle = ls.linestyle or bpy.data.linestyles.new("Ink"); ls.linestyle.color = (0.18, 0.1, 0.05); ls.linestyle.thickness = 2.2
    PDIR = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Portraits"); os.makedirs(PDIR, exist_ok=True)
    sc.render.filepath = os.path.join(PDIR, "mandu_normal.png"); bpy.ops.render.render(write_still=True); print("✔ mandu portrait")
