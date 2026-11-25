"""풍선고래 대장장이(사용자 사진: 연한 살구색 풍선에 매직으로 얼굴을 그린 것 + 말풍선 낙서).
모양: 통통하게 부푼 풍선 몸 · 아래 꼭지(묶은 자리) · 매직으로 그린 점 눈 두 개 · 눈 위 둥근 눈썹 ·
      고래처럼 길게 휘어 올라간 입 · 볼 선 · 등에 작은 분수 구멍 · 양옆 작은 지느러미 · 뒤 꼬리 지느러미 · 그려 넣은 체리 낙서
대장장이 차림: 갈색 가죽 앞치마 · 이마에 올린 보안경 · 빨간 머리띠 · 오른 지느러미에 쥔 쇠망치
풍선이라 바닥에서 조금 떠서 둥실둥실(모든 동작에 뜬 높이 + 출렁임).
뼈: root → body → tail · flip.L/R · eye.L/R · hammer(flip.R 자식)
애니메이션(30fps): Idle61 Talk31 Hammer24(루프 · 타격 11) Yes30 No36 Happy40 Think40 Fail40 Cheer40
출력: unity/Assets/Models/BalloonWhale.fbx · Resources/UI/Portraits/balloon_normal.png(--portrait) · previews/balloon_*.png(--preview)
실행: <bpy 파이썬> blender/build_balloon_whale.py [--preview] [--portrait]"""
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews"); UNITY = os.path.join(ROOT, "unity", "Assets", "Models")
reset_scene()

def M_(n, c, **kw): m = mat(n, c, **kw); m.diffuse_color = (*c, 1); return m
SKIN = M_("Balloon_Skin", (0.97, 0.63, 0.5), rough=0.55, sheen=0.3)       # 살구색 고무
INK = M_("Balloon_Ink", (0.26, 0.12, 0.09), rough=0.9)                     # 매직 선
KNOT = M_("Balloon_Knot", (0.93, 0.56, 0.44), rough=0.6)
APRON = M_("Balloon_Apron", (0.42, 0.26, 0.13), rough=0.9)
STRAP = M_("Balloon_Strap", (0.3, 0.18, 0.09), rough=0.9)
IRON = M_("Balloon_Iron", (0.2, 0.2, 0.23), rough=0.4, metal=0.85)
WOOD = M_("Balloon_Wood", (0.55, 0.35, 0.17), rough=0.85)
BAND = M_("Balloon_Band", (0.8, 0.18, 0.18), rough=0.8)
GLASS = M_("Balloon_Glass", (0.55, 0.75, 0.85), rough=0.2)
CHERRY = M_("Balloon_Cherry", (0.78, 0.22, 0.25), rough=0.6)
MATS = [SKIN, INK, KNOT, APRON, STRAP, IRON, WOOD, BAND, GLASS, CHERRY]
I = {m.name: i for i, m in enumerate(MATS)}

def smooth(a, b, x): t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
# ── 풍선 몸: 위가 넓고 아래로 갈수록 좁아져 꼭지로 ─────────────
BC, BR = Vector((0.0, 0.0, 0.72)), Vector((0.46, 0.43, 0.5))
bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=64, v_segments=40, radius=1.0, calc_uvs=True)
for v in bm.verts:
    x, y, z = v.co.x * BR.x, v.co.y * BR.y, v.co.z * BR.z
    down = smooth(0.1, -1.0, v.co.z)                                       # 아래로 갈수록 좁아짐(풍선 배)
    x *= 1 - 0.42 * down; y *= 1 - 0.42 * down; z -= 0.1 * down
    up = smooth(0.2, 1.0, v.co.z); x *= 1 + 0.05 * up; y *= 1 + 0.05 * up  # 위는 살짝 볼록
    v.co = Vector((x, BC.y + y, BC.z + z))
for f in bm.faces: f.smooth = True; f.material_index = I["Balloon_Skin"]
body = to_object("body", bm, MATS, "SKIN")
BVH = BVHTree.FromObject(body, bpy.context.evaluated_depsgraph_get())
def front_pt(x, z, off=0.0):
    loc, n, _, _ = BVH.ray_cast(Vector((x, -3.0, z)), Vector((0, 1, 0)), 10.0)
    if loc is None: return Vector((x, -0.45, z)), Vector((0, -1, 0))
    return loc + n * off, n
def top_pt(x, y, off=0.0):
    loc, n, _, _ = BVH.ray_cast(Vector((x, y, 3.0)), Vector((0, 0, -1)), 10.0)
    if loc is None: return Vector((x, y, 1.2)), Vector((0, 0, 1))
    return loc + n * off, n
def side_pt(s, y, z, off=0.0):
    loc, n, _, _ = BVH.ray_cast(Vector((3.0 * s, y, z)), Vector((-s, 0, 0)), 10.0)
    if loc is None: return Vector((0.45 * s, y, z)), Vector((s, 0, 0))
    return loc + n * off, n
parts = []
def add(name, b_, group): o = to_object(name, b_, MATS, group); parts.append(o); return o
def facing(n): return Vector((0, -1, 0)).rotation_difference(n).to_matrix()
def ell(c, r, rot=None, mi=0, seg=18, rings=11):
    b_ = bmesh.new(); bmesh.ops.create_uvsphere(b_, u_segments=seg, v_segments=rings, radius=1.0)
    R3 = rot if rot is not None else Matrix.Identity(3)
    for v in b_.verts: v.co = Vector(c) + R3 @ Vector((v.co.x * r[0], v.co.y * r[1], v.co.z * r[2]))
    for f in b_.faces: f.smooth = True; f.material_index = mi
    return b_
def stroke(pts2, r, mi, group, name, off=0.006, sides=5):
    """정면에 매직으로 그린 선: (x, z) 목록을 몸 표면에 붙임"""
    pts = [front_pt(x, z, off)[0] for x, z in pts2]
    tb, _ = tube(pts, lambda t: r, sides=sides, mat_idx=mi); return add(name, tb, group)

# ── 매직으로 그린 얼굴 ────────────────────────────────────
EX, EZ = 0.215, 0.745
for s, side in ((1, "L"), (-1, "R")):
    p, n = front_pt(EX * s, EZ, 0.008); add(f"Eye{side}", ell(p, (0.05, 0.028, 0.05), facing(n), I["Balloon_Ink"]), f"eye.{side}")
    brow = [(s * (0.11 + 0.19 * t), EZ + 0.135 + 0.065 * math.sin(math.pi * t)) for t in [i / 8 for i in range(9)]]
    stroke(brow, 0.011, I["Balloon_Ink"], "head", f"Brow{side}")           # 눈 위 둥근 눈썹
mouth = [(-0.265 + 0.53 * t, 0.675 - 0.1 * math.sin(math.pi * t) + 0.02 * t) for t in [i / 14 for i in range(15)]]
stroke(mouth, 0.016, I["Balloon_Ink"], "head", "Mouth")                    # 고래처럼 길게 휘어 올라간 입
for s, side in ((1, "L"), (-1, "R")):                                      # 볼 선
    cheek = [(s * (0.345 + 0.045 * math.sin(math.pi * t)), 0.66 - 0.16 * t) for t in [i / 6 for i in range(7)]]
    stroke(cheek, 0.010, I["Balloon_Ink"], "head", f"Cheek{side}")
cp, cn = front_pt(0.30, 1.0, 0.01)                                        # 체리 낙서(사진의 낙서)
add("Cherry", ell(cp, (0.035, 0.016, 0.035), facing(cn), I["Balloon_Cherry"]), "head")
stroke([(0.30 + 0.03 * t, 1.035 + 0.09 * t) for t in [i / 5 for i in range(6)]], 0.008, I["Balloon_Ink"], "head", "CherryStem")

# ── 고래다움: 분수 구멍 · 지느러미 · 꼬리 ────────────────────
sp, sn = top_pt(0.0, 0.14, -0.004); add("Blowhole", ell(sp, (0.045, 0.06, 0.018), None, I["Balloon_Ink"]), "body")
FLIPJ = {}
for s, side in ((1, "L"), (-1, "R")):
    root_, n = side_pt(s, 0.02, 0.62, -0.03); FLIPJ[side] = (root_, root_ + Vector((0.14 * s, 0.03, -0.06)))
    add(f"Flipper{side}", ell(root_ + Vector((0.11 * s, 0.02, -0.05)), (0.11, 0.07, 0.15),
        Matrix.Rotation(-0.55 * s, 3, "Y") @ Matrix.Rotation(0.2, 3, "X"), I["Balloon_Skin"]), f"flip.{side}")
TJ = Vector((0, 0.42, 0.85))
for s in (1, -1):
    add(f"Tail{'L' if s > 0 else 'R'}", ell(TJ + Vector((0.11 * s, 0.1, 0.06)), (0.1, 0.16, 0.045),
        Matrix.Rotation(-0.5 * s, 3, "Z") @ Matrix.Rotation(-0.3, 3, "X"), I["Balloon_Skin"]), "tail")
kb = bmesh.new()                                                            # 아래 꼭지(묶은 자리)
bmesh.ops.create_cone(kb, cap_ends=True, segments=10, radius1=0.055, radius2=0.025, depth=0.12)
bmesh.ops.translate(kb, vec=Vector((0, 0, 0.14)), verts=kb.verts[:])
for f in kb.faces: f.smooth = True; f.material_index = I["Balloon_Knot"]
add("Knot", kb, "body")
add("KnotLip", ell(Vector((0, 0, 0.21)), (0.072, 0.072, 0.03), None, I["Balloon_Knot"], 12, 7), "body")

# ── 대장장이 차림 ─────────────────────────────────────────
apc, apn = front_pt(0.0, 0.23, 0.02)                                        # 가죽 앞치마(가슴받이)
add("Apron", ell(apc, (0.27, 0.058, 0.21), facing(apn), I["Balloon_Apron"], 20, 12), "body")
apc2, apn2 = front_pt(0.0, 0.13, 0.02)                                      # 아래 자락
add("ApronSkirt", ell(apc2, (0.23, 0.052, 0.15), facing(apn2), I["Balloon_Apron"], 18, 10), "body")
bp, bn = front_pt(0.0, 0.36, 0.022)                                         # 허리끈
add("ApronBelt", ell(bp, (0.26, 0.045, 0.032), facing(bn), I["Balloon_Strap"], 18, 8), "body")
for s in (1, -1):                                                            # 가는 어깨끈(얼굴을 비켜 바깥으로)
    pts = [front_pt(s * (0.3 + 0.05 * t), 0.40 + 0.5 * t, 0.015)[0] for t in [i / 6 for i in range(7)]]
    pts += [top_pt(s * (0.3 - 0.12 * t), 0.03 + 0.15 * t, 0.013)[0] for t in [i / 4 for i in range(5)]]
    tb, _ = tube(pts, lambda t: 0.016, sides=5, mat_idx=I["Balloon_Strap"]); add(f"Strap{'L' if s > 0 else 'R'}", tb, "body")
band = []                                                                    # 빨간 머리띠
for k in range(33):
    a = 2 * math.pi * k / 32
    loc, n, _, _ = BVH.ray_cast(Vector((math.cos(a) * 2.0, math.sin(a) * 2.0, 0.96)), -Vector((math.cos(a), math.sin(a), 0)), 5.0)
    if loc is not None: band.append(loc + n * 0.012)
if len(band) > 3:
    band.append(band[0]); tb, _ = tube(band, lambda t: 0.028, sides=5, mat_idx=I["Balloon_Band"]); add("Headband", tb, "body")
gp, gn = front_pt(0.0, 1.055, 0.02)                                            # 이마에 올린 보안경
add("Goggles", ell(gp, (0.2, 0.05, 0.06), facing(gn), I["Balloon_Strap"], 16, 8), "body")
for s in (1, -1):
    p2, n2 = front_pt(s * 0.09, 1.055, 0.055); add(f"Lens{'L' if s > 0 else 'R'}", ell(p2, (0.07, 0.03, 0.055), facing(n2), I["Balloon_Glass"], 14, 8), "body")
HJ = FLIPJ["R"][1] + Vector((-0.06, -0.06, -0.05))                           # 쇠망치(오른 지느러미)
hb = bmesh.new()
hs = bmesh.ops.create_cone(hb, cap_ends=True, segments=10, radius1=0.016, radius2=0.016, depth=0.3)["verts"]
bmesh.ops.transform(hb, matrix=Matrix.Translation(Vector((0, 0, 0.15))), verts=hs)
for f in hb.faces: f.material_index = I["Balloon_Wood"]
hh = bmesh.ops.create_cube(hb, size=1.0)["verts"]
bmesh.ops.transform(hb, matrix=Matrix.Translation(Vector((0, 0, 0.33))) @ Matrix.Diagonal((0.075, 0.075, 0.125, 1)), verts=hh)
for v in hh:
    for f in v.link_faces: f.material_index = I["Balloon_Iron"]
bmesh.ops.transform(hb, matrix=Matrix.Translation(HJ) @ Matrix.Rotation(-0.5, 3, "Y").to_4x4(), verts=hb.verts[:])
add("Hammer", hb, "hammer")

# ── 뼈대 · 피부 ───────────────────────────────────────────
BONES = [("root", None, Vector((0, 0, 0)), Vector((0, 0, 0.12))), ("body", "root", Vector((0, 0, 0.18)), Vector((0, 0, 1.0))),
         ("tail", "body", TJ, TJ + Vector((0, 0.3, 0.05)))]
for s, side in ((1, "L"), (-1, "R")):
    e, _ = front_pt(EX * s, EZ, -0.01); BONES.append((f"eye.{side}", "body", e, e + Vector((0, 0, 0.05))))
    BONES.append((f"flip.{side}", "body", *FLIPJ[side]))
BONES.append(("hammer", "flip.R", HJ, HJ + Vector((0, 0, 0.33))))
arm = make_armature("BalloonRig", BONES)
for ob in [body] + parts:
    if "SKIN" in ob.vertex_groups:
        skin = ob.vertex_groups["SKIN"]; gb = ob.vertex_groups.new(name="body")
        gb.add([v.index for v in ob.data.vertices], 1.0, "REPLACE"); ob.vertex_groups.remove(skin)
    bind(ob, arm)

# ── 애니메이션 ────────────────────────────────────────────
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
CH = ("z", "fwd", "side", "lean", "roll", "yaw", "sq", "fl", "fr", "fls", "frs", "eye", "tail", "ham")
DEF = {"eye": 1.0, "z": 0.34}                                                # 기본으로 0.34m 떠 있음(풍선)
def anim(name, length, spec, loop=True):
    """z 뜬 높이 · fwd 앞 · side 옆 · lean 숙임 · roll 좌우 기울기 · yaw · sq 눌림(+)/늘어남(-)
       fl/fr 지느러미 들기 · fls/frs 앞뒤 휘젓기 · eye 눈 세로 · tail 꼬리 · ham 망치 치기(+ 내려침)"""
    tr = {k: track(spec.get(k, []), DEF.get(k, 0.0)) for k in CH}; act = new_action(arm, name, length); prev = {}
    def K(bone, f, Mx, loc=None, scale=None):
        e = Mx.to_euler("XYZ", prev[bone]) if bone in prev else Mx.to_euler("XYZ"); prev[bone] = e; key(arm, bone, f, rot=e, loc=loc, scale=scale)
    for f in range(1, length + 1):
        g = {k: tr[k](f) for k in CH}
        K("root", f, R("root", 0, 0, g["yaw"]), loc=Vector((g["side"], g["z"], -g["fwd"])))
        sq = g["sq"]; K("body", f, R("body", g["lean"], g["roll"]), scale=Vector((1 + 0.45 * sq, 1 + 0.45 * sq, 1 - sq)))
        for side, s in (("L", 1), ("R", -1)):
            raise_ = g["fl"] if side == "L" else g["fr"]; sw = g["fls"] if side == "L" else g["frs"]
            K(f"flip.{side}", f, R(f"flip.{side}", sw, raise_ * s))
            key(arm, f"eye.{side}", f, scale=Vector((1, max(0.08, g["eye"]), 1)))
        K("tail", f, R("tail", -g["tail"])); K("hammer", f, R("hammer", g["ham"]))
    finish_action(arm, act, loop=loop); act.frame_start, act.frame_end = 1, length; return act
def blink(n, at): return [(1, 1), (at - 2, 1), (at, 0.1), (at + 2, 1), (n, 1)]
def osc(n, amp, cyc=1, ph=0.0, base=0.0): return [(1 + round(k * (n - 1) / 24), base + amp * math.sin(2 * math.pi * cyc * k / 24 + ph)) for k in range(25)]
def bob(n, amp=0.05, cyc=1): return [(1 + round(k * (n - 1) / 24), 0.34 + amp * math.sin(2 * math.pi * cyc * k / 24)) for k in range(25)]
A = {}
A["Idle"] = anim("Idle", 61, {"z": bob(61, 0.055), "sq": osc(61, 0.035, 2), "roll": osc(61, 0.05), "fl": osc(61, 0.12, 2, 0, 0.15), "fr": osc(61, 0.12, 2, 1, 0.15),
                              "eye": blink(61, 46), "tail": osc(61, 0.16), "lean": osc(61, 0.04, 2)})
A["Talk"] = anim("Talk", 31, {"z": bob(31, 0.04, 2), "sq": osc(31, 0.05, 3), "fl": [(1, 0.2), (10, 0.8), (20, 0.35), (31, 0.2)], "fls": osc(31, 0.4),
                              "lean": osc(31, 0.08, 2), "eye": blink(31, 24), "roll": osc(31, 0.06, 2)})
A["Hammer"] = anim("Hammer", 24, {"z": [(1, 0.36), (8, 0.40), (11, 0.27), (16, 0.33), (24, 0.36)], "ham": [(1, -0.9), (8, -1.5), (11, 0.95), (16, 0.6), (24, -0.9)],
                                  "fr": [(1, 0.9), (8, 1.4), (11, 0.35), (24, 0.9)], "frs": [(1, 0.2), (8, -0.5), (11, 0.9), (24, 0.2)], "fl": [(1, 0.5), (24, 0.5)],
                                  "lean": [(1, 0.12), (8, -0.05), (11, 0.3), (16, 0.22), (24, 0.12)], "sq": [(1, 0), (8, -0.1), (12, 0.16), (18, 0), (24, 0)],
                                  "eye": [(1, 0.7), (11, 0.35), (24, 0.7)], "tail": osc(24, 0.2, 2)})
A["Yes"] = anim("Yes", 30, {"z": bob(30, 0.05, 2), "lean": [(1, 0), (6, 0.32), (11, -0.05), (16, 0.3), (21, -0.02), (30, 0)], "sq": [(1, 0), (6, 0.1), (11, 0), (16, 0.1), (30, 0)],
                            "eye": [(1, 1), (6, 0.4), (21, 0.4), (30, 1)], "fl": [(1, 0.2), (8, 0.7), (30, 0.2)], "fr": [(1, 0.2), (8, 0.7), (30, 0.2)]}, loop=False)
A["No"] = anim("No", 36, {"yaw": [(1, 0), (6, 0.3), (12, -0.3), (18, 0.28), (24, -0.26), (30, 0.07), (36, 0)], "fl": [(1, 0), (8, 0.9), (30, 0.9), (36, 0)],
                          "fr": [(1, 0), (8, 0.9), (30, 0.9), (36, 0)], "eye": [(1, 1), (6, 0.5), (30, 0.5), (36, 1)], "z": bob(36, 0.03, 2)}, loop=False)
A["Happy"] = anim("Happy", 40, {"z": [(1, 0.34), (8, 0.72), (14, 0.42), (20, 0.68), (26, 0.4), (40, 0.34)], "sq": [(1, 0), (6, -0.18), (12, 0.14), (18, -0.14), (26, 0.1), (40, 0)],
                                "fl": osc(40, 0.6, 4, 0, 0.7), "fr": osc(40, 0.6, 4, 1, 0.7), "eye": [(1, 1), (6, 0.12), (34, 0.12), (40, 1)],
                                "roll": osc(40, 0.14, 3), "tail": osc(40, 0.4, 4)}, loop=False)
A["Think"] = anim("Think", 40, {"z": bob(40, 0.03), "lean": [(1, 0), (10, -0.2), (30, -0.18), (40, 0)], "yaw": [(1, 0), (12, 0.25), (26, -0.2), (40, 0)],
                                "fr": [(1, 0.2), (10, 1.6), (32, 1.5), (40, 0.2)], "frs": [(1, 0), (10, 1.0), (32, 0.95), (40, 0)],
                                "eye": [(1, 1), (8, 0.45), (32, 0.45), (40, 1)], "sq": osc(40, 0.03, 2)}, loop=False)
A["Fail"] = anim("Fail", 40, {"z": [(1, 0.34), (7, 0.44), (14, 0.12), (24, 0.16), (40, 0.3)], "sq": [(1, 0), (7, -0.2), (14, 0.3), (24, 0.26), (40, 0.04)],
                              "lean": [(1, 0), (8, -0.25), (16, 0.4), (28, 0.35), (40, 0.05)], "fl": [(1, 0.2), (8, 1.5), (16, -0.1), (40, 0.2)],
                              "fr": [(1, 0.2), (8, 1.5), (16, -0.1), (40, 0.2)], "eye": [(1, 1), (6, 1.35), (14, 0.1), (32, 0.1), (40, 0.8)],
                              "tail": [(1, 0), (8, 0.7), (16, -0.2), (40, 0)]}, loop=False)
A["Cheer"] = anim("Cheer", 40, {"z": [(1, 0.34), (10, 0.95), (18, 0.5), (26, 0.85), (34, 0.45), (40, 0.36)], "sq": [(1, 0), (8, -0.24), (14, 0.18), (22, -0.18), (30, 0.12), (40, 0)],
                                "fl": [(1, 0.3), (10, 2.1), (26, 1.9), (40, 0.4)], "fr": [(1, 0.3), (10, 2.1), (26, 1.9), (40, 0.4)], "ham": [(1, -0.9), (10, 0.3), (26, 0.2), (40, -0.9)],
                                "eye": [(1, 1), (8, 0.15), (32, 0.15), (40, 1)], "roll": osc(40, 0.18, 3), "tail": osc(40, 0.5, 4)}, loop=False)

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = Vector((0, 0, 0.7))
    preview(os.path.join(PRE, "balloon_front.png"), Vector((0.0, -3.6, 1.1)), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "balloon_side.png"), Vector((3.4, -1.1, 1.1)), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "balloon_hammer.png"), Vector((-2.5, -2.7, 1.3)), c, arm_ob=arm, action=A["Hammer"], frame=11)
    preview(os.path.join(PRE, "balloon_cheer.png"), Vector((2.2, -3.0, 1.6)), c, arm_ob=arm, action=A["Cheer"], frame=12)

glb, fbx = export_all("BalloonWhale", OUT, [arm, body] + parts)
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "BalloonWhale.fbx"))
print("EXPORTED", fbx, "verts:", sum(len(o.data.vertices) for o in [body] + parts))

if "--portrait" in sys.argv:
    sc = bpy.context.scene
    for o in list(sc.objects):
        if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
    arm.animation_data_create(); arm.animation_data.action = A["Idle"]; sc.frame_set(1)
    w = bpy.data.worlds.new("PW"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 0.7; sc.world = w
    for loc, e in ((Vector((-1.2, -1.9, 1.8)), 120), (Vector((1.5, 0.6, 1.4)), 70)):
        L = bpy.data.objects.new("L", bpy.data.lights.new("L", "AREA")); L.data.energy = e; L.data.size = 1.4; L.location = loc; sc.collection.objects.link(L)
        L.rotation_euler = (Vector((0, -0.3, 0.8)) - loc).to_track_quat("-Z", "Y").to_euler()
    cam = bpy.data.objects.new("PC", bpy.data.cameras.new("PC")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 50
    tgt = Vector((0, -0.3, 0.76)); cam.location = tgt + Vector((-0.4, -2.1, 0.14)); cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.engine = "CYCLES"; sc.cycles.samples = 48; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True; sc.render.film_transparent = True
    sc.render.resolution_x, sc.render.resolution_y = 600, 720; sc.render.image_settings.color_mode = "RGBA"; sc.view_settings.view_transform = "Standard"
    sc.render.use_freestyle = True; sc.render.line_thickness_mode = "ABSOLUTE"; sc.render.line_thickness = 2.2
    vl = sc.view_layers[0]; vl.use_freestyle = True
    fs = vl.freestyle_settings
    if not fs.linesets: fs.linesets.new("ls")
    pdir = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Portraits"); os.makedirs(pdir, exist_ok=True)
    sc.render.filepath = os.path.join(pdir, "balloon_normal.png"); bpy.ops.render.render(write_still=True)
    print("✔ balloon portrait")
