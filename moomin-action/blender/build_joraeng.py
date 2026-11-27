"""NPC 종족: 조랭하프족(사용자 인형 사진 — 서 있는 하얀 하프물범 인형). 마을 주민·퀘스트 NPC 공용 모델 + 상호작용 애니메이션 + 대화창 2D 초상화.
모양: 머리와 몸이 한 덩어리(조랭이떡처럼 목에서 살짝 잘록, 배가 머리보다 약간 넓음), 앞뒤로 약간 납작한 단면, 등 가운데 세로 솔기.
      얼굴: 까만 단추 눈 · 갈색 둥근 역삼각 코 · 코 밑에서 갈라지는 ω 입 · 양쪽 갈색 수염 2가닥 · 눈 위 갈색 점 눈썹.
      옆구리 작은 앞지느러미, 바닥 앞쪽에 벌어진 꼬리지느러미 두 쪽. 털: 테리 천처럼 복슬복슬(Fur_Normal + 광택).
      주민마다 바꿔 쓰는 목도리(Scarf — 색·표시는 Unity Npc).
키 0.95m(민미 1.27m). 뼈대: root → body(바닥~목) → head(목~정수리, 피부는 높이로 body 와 부드럽게) · eye.L/R(세로 스케일 = 감기·크게 뜨기)
      · flip.L/R(앞지느러미) · fin.L/R(꼬리지느러미).
애니메이션(30fps): Idle61 Talk31 Walk25 Wave40 Yes30(긍정) No36(부정) Laugh45(웃음) Sad50(슬픔) Surprised36(당황) QuestAccept40(퀘스트 수락) QuestComplete50(퀘스트 완료)
출력: unity/Assets/Models/Joraeng.fbx · exports/portraits_raw/joraeng_<표정>.png(normal·happy·sad·surprised·troubled → tools/portrait_art.py 가 마무리)
      · previews/joraeng_*.png(--preview)
실행: <bpy 파이썬> blender/build_joraeng.py [--preview]"""
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews"); UNITY = os.path.join(ROOT, "unity", "Assets", "Models")
RAW = os.path.join(ROOT, "exports", "portraits_raw")
FURTEX = os.path.join(UNITY, "Textures", "Fur_Normal.png")
reset_scene()

def M_(name, color, **kw):
    m = mat(name, color, **kw); m.diffuse_color = (*color, 1); return m   # diffuse_color = Workbench(초상화) 색
FUR = M_("Joraeng_Fur", (0.93, 0.91, 0.84), rough=1.0, sheen=1.0)
EYE = M_("Joraeng_Eye", (0.012, 0.012, 0.016), rough=0.12)
BROWN = M_("Joraeng_Brown", (0.25, 0.11, 0.035), rough=0.85)
SEAM = M_("Joraeng_Seam", (0.78, 0.74, 0.64), rough=1.0)
SCARF = M_("Joraeng_Scarf", (0.75, 0.16, 0.13), rough=0.9, sheen=0.5)
MATS = [FUR, EYE, BROWN, SEAM, SCARF]; I_FUR, I_EYE, I_BROWN, I_SEAM, I_SCARF = range(5)
if os.path.exists(FURTEX):
    for m_, sc_ in ((FUR, 9), (SCARF, 6)):
        nt = m_.node_tree; b = nt.nodes["Principled BSDF"]; img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(FURTEX); img.image.colorspace_settings.name = "Non-Color"
        mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (sc_, sc_, 1); tc = nt.nodes.new("ShaderNodeTexCoord"); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 1.0
        nt.links.new(tc.outputs["UV"], mp.inputs[0]); nt.links.new(mp.outputs[0], img.inputs[0]); nt.links.new(img.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"])

# ── 몸 단면 반지름 r(z) (m): 둥근 바닥(초타원) → 통통한 배 → 목 잘록 → 머리 → 넓적한 둥근 정수리(초타원)
H = 0.95; DEPTH = 0.86                                   # 앞뒤 납작함
MID = [(0.2, 0.34), (0.32, 0.342), (0.43, 0.324), (0.55, 0.298), (0.63, 0.306), (0.7, 0.312)]
def r_at(z):
    z = max(0.0, min(H, z))
    if z < 0.2: t = z / 0.2; return 0.34 * max(0.0, 1 - (1 - t) ** 2.2) ** (1 / 2.2)
    if z > 0.7: t = (z - 0.7) / (H - 0.7); return 0.312 * max(0.0, 1 - t ** 2.4) ** (1 / 2.4)
    for (z0, r0), (z1, r1) in zip(MID, MID[1:]):
        if z <= z1: u = (z - z0) / (z1 - z0); u = u * u * (3 - 2 * u); return r0 + (r1 - r0) * u
    return MID[-1][1]
def front_y(x, z):
    r = r_at(z); return -r * DEPTH * math.sqrt(max(0.0, 1 - (x / r) ** 2)) if r > 1e-4 else 0.0
def surf(x, z, off=0.0):
    """정면 표면 위 점 + 바깥 법선 방향으로 off 만큼."""
    e = 0.004; p = Vector((x, front_y(x, z), z)); px = Vector((x + e, front_y(x + e, z), z)); pz = Vector((x, front_y(x, z + e), z + e))
    n = (pz - p).cross(px - p).normalized(); n = n if n.y < 0 else -n
    return p + n * off, n

NZ, NA = 76, 64
bm = bmesh.new(); uvl = bm.loops.layers.uv.new("UVMap"); rows = []
for i in range(NZ + 1):
    z = H * (1 - math.cos(math.pi * i / NZ)) / 2   # 위아래 끝을 촘촘하게
    r = max(r_at(z), 0.0005); rows.append([bm.verts.new((r * math.cos(2 * math.pi * k / NA), r * DEPTH * math.sin(2 * math.pi * k / NA), z)) for k in range(NA)])
for i in range(NZ):
    for k in range(NA):
        f = bm.faces.new((rows[i][k], rows[i][(k + 1) % NA], rows[i + 1][(k + 1) % NA], rows[i + 1][k])); f.smooth = True; f.material_index = I_FUR
        for l, (uu, vv) in zip(f.loops, ((k / NA, i / NZ), ((k + 1) / NA, i / NZ), ((k + 1) / NA, (i + 1) / NZ), (k / NA, (i + 1) / NZ))): l[uvl].uv = (uu * 4, vv * 2.4)
bm.faces.new(rows[0][::-1]).material_index = I_FUR; bm.faces.new(rows[-1]).material_index = I_FUR   # 아주 작은 바닥·정수리 마개
bm.normal_update()
parts = []
def add(name, bm_, group): parts.append(to_object(name, bm_, MATS, group))
add("body", bm, "SKIN")

def ell(center, radii, rot=None, mat_idx=I_FUR, seg=24, rings=14):
    """미터 단위 타원체(UV 포함). rot: 3×3 회전."""
    b_ = bmesh.new(); bmesh.ops.create_uvsphere(b_, u_segments=seg, v_segments=rings, radius=1.0, calc_uvs=True)
    R3 = rot if rot is not None else Matrix.Identity(3)
    for v in b_.verts: v.co = Vector(center) + R3 @ Vector((v.co.x * radii[0], v.co.y * radii[1], v.co.z * radii[2]))
    b_.normal_update()
    for f in b_.faces: f.smooth = True; f.material_index = mat_idx
    return b_
def facing(n):
    return Vector((0, -1, 0)).rotation_difference(n).to_matrix()

# ── 얼굴
EZ, EX = 0.70, 0.118
for s, side in ((1, "L"), (-1, "R")):
    p, n = surf(EX * s, EZ, 0.004); add(f"eye.{side}", ell(p, (0.031, 0.022, 0.034), facing(n), I_EYE, 28, 18), f"eye.{side}")
    p, n = surf(0.128 * s, 0.818, 0.001); add("brow", ell(p, (0.012, 0.006, 0.0095), facing(n), I_BROWN, 12, 8), "head")
    for z0, z1, x0, x1 in ((0.646, 0.656, 0.2, 0.282), (0.612, 0.598, 0.2, 0.28)):   # 수염 2가닥(바깥으로 살짝 벌어짐)
        pts = [surf(s * (x0 + (x1 - x0) * t), z0 + (z1 - z0) * t, 0.002)[0] for t in [j / 7 for j in range(8)]]
        tb, _ = tube(pts, lambda t: 0.0042 * (1 - 0.25 * t), sides=6, mat_idx=I_BROWN); add("whisker", tb, "head")
# 코: 둥근 역삼각형 돔(아래 꼭짓점)
NZC = 0.657; p, n = surf(0, NZC, -0.004); Rn = facing(n); nb = bmesh.new(); bmesh.ops.create_uvsphere(nb, u_segments=36, v_segments=16, radius=1.0)
for v in nb.verts:
    a = math.atan2(v.co.z, v.co.x); rr = 1.0 / (1 - 0.15 * math.cos(3 * (a + math.pi / 2))) * 0.9
    v.co = p + Rn @ Vector((v.co.x * 0.034 * rr, v.co.y * 0.016, v.co.z * 0.027 * rr))
for f in nb.faces: f.smooth = True; f.material_index = I_BROWN
add("nose", nb, "head")
# 입: 코 밑에서 짧게 내려와 양쪽으로 웃는 곡선(ω)
for s in (1, -1):
    pts = [surf(x * s, z, 0.002)[0] for x, z in ((0, 0.634), (0.003, 0.62), (0.016, 0.606), (0.036, 0.6), (0.058, 0.604), (0.078, 0.616), (0.09, 0.628))]
    tb, _ = tube(pts, lambda t: 0.0052, sides=6, mat_idx=I_BROWN); add("mouth", tb, "head")
# 등 솔기(정수리 앞쪽에서 등 아래까지)
seam = [Vector((0, 0.05, H + 0.0012))]
for j in range(40):
    z = 0.93 - 0.68 * j / 39; seam.append(Vector((0, r_at(z) * DEPTH + 0.0015, z)))
tb, _ = tube(seam, lambda t: 0.0045, sides=6, mat_idx=I_SEAM); add("seam", tb, "SKIN")
# 앞지느러미(옆구리, 아래로 처져 살짝 벌어짐)
FLIPJ = {}
for s, side in ((1, "L"), (-1, "R")):
    rot = Matrix.Rotation(-0.32 * s, 3, "Y") @ Matrix.Rotation(-0.18, 3, "X"); top = Vector((0.305 * s, -0.04, 0.47))
    c = top + rot @ Vector((0, 0, -0.12)); add(f"flip.{side}", ell(c, (0.042, 0.068, 0.13), rot), f"flip.{side}")
    FLIPJ[side] = (top, top + rot @ Vector((0, 0, -0.24)))
# 꼬리지느러미 두 쪽(바닥 앞, 바깥으로 벌어진 납작한 잎)
FINJ = {}
for s, side in ((1, "L"), (-1, "R")):
    rot = Matrix.Rotation(0.42 * s, 3, "Z") @ Matrix.Rotation(0.12, 3, "X"); base = Vector((0.035 * s, -0.17, 0.03))
    add(f"fin.{side}", ell(base + rot @ Vector((0, -0.095, 0)), (0.068, 0.115, 0.026), rot), f"fin.{side}")
    FINJ[side] = (base, base + rot @ Vector((0, -0.2, 0)))
npc = join(parts, "Joraeng")

# 목도리(주민별 색 · 표시 여부는 Unity Npc)
RZ = 0.54; ring = []
for k in range(49):
    a = 2 * math.pi * k / 48; r = r_at(RZ) + 0.018; ring.append(Vector((r * math.cos(a), r * DEPTH * math.sin(a), RZ + 0.008 * math.sin(a * 5))))
tb, _ = tube(ring, lambda t: 0.034, sides=10, mat_idx=I_SCARF); sc1 = to_object("ScarfRing", tb, MATS, "SKIN")
tail = [surf(0.13 + 0.02 * t, RZ - 0.03 - 0.17 * t, 0.03)[0] for t in [j / 7 for j in range(8)]]
tb2, _ = tube(tail, lambda t: 0.03 * (1 - 0.25 * t), sides=8, mat_idx=I_SCARF)
sc2 = to_object("ScarfTail", tb2, MATS, "SKIN"); scarf = join([sc1, sc2], "Scarf")
for o in (npc, scarf):
    for p in o.data.polygons: p.use_smooth = True

# ── 뼈대
BONES = [("root", None, Vector((0, 0, 0)), Vector((0, 0, 0.1))), ("body", "root", Vector((0, 0, 0.04)), Vector((0, 0, 0.5))),
         ("head", "body", Vector((0, 0, 0.5)), Vector((0, 0, H)))]
for s, side in ((1, "L"), (-1, "R")):
    e, _ = surf(EX * s, EZ, -0.01); BONES.append((f"eye.{side}", "head", e, e + Vector((0, 0, 0.06))))
    BONES.append((f"flip.{side}", "body", *FLIPJ[side])); BONES.append((f"fin.{side}", "root", *FINJ[side]))
arm = make_armature("JoraengRig", BONES)
def smooth(a, b, x): t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
for ob in (npc, scarf):
    skin = ob.vertex_groups["SKIN"]; gb = ob.vertex_groups.get("body") or ob.vertex_groups.new(name="body"); gh = ob.vertex_groups.get("head") or ob.vertex_groups.new(name="head")
    for v in ob.data.vertices:
        if not any(g.group == skin.index for g in v.groups): continue
        w = smooth(0.42, 0.64, v.co.z)
        if w > 0: gh.add([v.index], w, "REPLACE")
        if w < 1: gb.add([v.index], 1 - w, "REPLACE")
    ob.vertex_groups.remove(skin); bind(ob, arm)

# ── 애니메이션
def R(bn, pitch=0.0, roll=0.0, yaw=0.0):
    """월드 축 회전 → 뼈 로컬 오일러. pitch +: 앞(-Y)으로 숙임 · roll +: 오른쪽(+X)으로 기울기 · yaw: Z."""
    M = arm.data.bones[bn].matrix_local.to_3x3(); W = Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(pitch, 3, "X") @ Matrix.Rotation(-roll, 3, "Y")
    return (M.inverted() @ W @ M).to_euler()
def track(keys, default=0.0):
    if not keys: return lambda f: default
    keys = sorted(keys)
    def at(f):
        if f <= keys[0][0]: return keys[0][1]
        for (f0, v0), (f1, v1) in zip(keys, keys[1:]):
            if f <= f1: u = (f - f0) / (f1 - f0); u = u * u * (3 - 2 * u); return v0 + (v1 - v0) * u
        return keys[-1][1]
    return at
CH = ("z", "lean", "roll", "yaw", "hp", "hy", "hr", "sq", "fl", "fr", "fls", "frs", "eye", "fin")
DEF = {"eye": 1.0}
def anim(name, length, spec, loop=True, extra=None):
    """z 튐 · lean 몸 숙임(+ 앞) · roll 기울기 · yaw 돌기 · hp/hy/hr 머리 숙임(+ 아래)·도리·갸웃 · sq 눌림(+)/늘어남(-)
       fl/fr 왼·오른 앞지느러미 들기(+ 바깥 위, 1.57 = 수평, 2.4 = 머리 위) · fls/frs 지느러미 앞뒤 · eye 눈 세로 크기(1 기본, 0.12 감음, 1.35 놀람) · fin 꼬리지느러미 들썩."""
    tr = {k: track(spec.get(k, []), DEF.get(k, 0.0)) for k in CH}; act = new_action(arm, name, length)
    for f in range(1, length + 1):
        ff = ((f - 1) % (length - 1)) + 1 if loop else f; g = {k: tr[k](ff) for k in CH}
        if extra: extra(f, g)
        key(arm, "root", f, loc=Vector((0, g["z"], 0)), rot=R("root", 0, 0, g["yaw"]))
        sq = g["sq"]; key(arm, "body", f, rot=R("body", g["lean"], g["roll"]), scale=Vector((1 + 0.5 * sq, 1 - sq, 1 + 0.5 * sq)))
        key(arm, "head", f, rot=R("head", g["hp"], g["hr"], g["hy"]))
        for side, s in (("L", 1), ("R", -1)):
            raise_ = g["fl"] if side == "L" else g["fr"]; sw = g["fls"] if side == "L" else g["frs"]
            key(arm, f"flip.{side}", f, rot=R(f"flip.{side}", sw, raise_ * s))
            key(arm, f"eye.{side}", f, scale=Vector((1 + 0.12 * (g["eye"] - 1), max(0.08, g["eye"]), 1)))
            key(arm, f"fin.{side}", f, rot=R(f"fin.{side}", -g["fin"] * 0.6))
    finish_action(arm, act, loop=loop); act.frame_start, act.frame_end = 1, length; return act
def blink(n, at): return [(1, 1), (at - 2, 1), (at, 0.12), (at + 2, 1), (n, 1)]
def osc(n, amp, cyc=1, ph=0.0, base=0.0): return [(1 + round(k * (n - 1) / 24), base + amp * math.sin(2 * math.pi * cyc * k / 24 + ph)) for k in range(25)]
def beats(frames, a, b): return [(f, a if i % 2 == 0 else b) for i, f in enumerate(frames)]
BEAT8 = [10, 14, 18, 22, 26, 30, 34, 38]
A = {}
A["Idle"] = anim("Idle", 61, {"sq": osc(61, 0.025), "hr": osc(61, 0.05, 1, 0.5), "fl": osc(61, 0.06, 2), "fr": osc(61, 0.06, 2, 1), "eye": blink(61, 42), "fin": osc(61, 0.1, 1)})
A["Talk"] = anim("Talk", 31, {"z": [(1, 0), (8, 0.012), (16, 0), (23, 0.01), (31, 0)], "hp": osc(31, 0.07, 2), "hr": osc(31, 0.05, 1),
                              "fl": [(1, 0.15), (10, 0.55), (20, 0.25), (31, 0.15)], "fls": osc(31, 0.3, 1), "sq": osc(31, 0.03, 2), "eye": blink(31, 20)})
A["Walk"] = anim("Walk", 25, {"z": [(1, 0), (7, 0.035), (13, 0), (19, 0.035), (25, 0)], "roll": osc(25, 0.12), "lean": [(1, 0.06), (25, 0.06)],
                              "fl": osc(25, 0.22, 1, 0, 0.15), "fr": osc(25, -0.22, 1, 0, 0.15), "fin": osc(25, 0.4, 2), "sq": osc(25, 0.04, 2, 1.6)})
A["Wave"] = anim("Wave", 40, {"fr": [(1, 0)] + beats([9, 13, 17, 21, 25, 29, 33], 2.2, 1.7) + [(40, 0)],
                              "hr": [(1, 0), (10, -0.12), (34, -0.12), (40, 0)], "eye": [(1, 1), (10, 0.35), (34, 0.35), (40, 1)], "sq": osc(40, 0.03, 2)}, loop=False)
A["Yes"] = anim("Yes", 30, {"hp": [(1, 0), (6, 0.32), (11, -0.05), (16, 0.3), (21, -0.03), (30, 0)], "sq": [(1, 0), (6, 0.06), (11, 0), (16, 0.06), (21, 0), (30, 0)],
                            "eye": [(1, 1), (6, 0.4), (21, 0.4), (30, 1)], "fl": [(1, 0), (8, 0.3), (24, 0.3), (30, 0)], "fr": [(1, 0), (8, 0.3), (24, 0.3), (30, 0)]}, loop=False)
A["No"] = anim("No", 36, {"hy": [(1, 0), (6, 0.35), (12, -0.35), (18, 0.33), (24, -0.3), (30, 0.1), (36, 0)], "fl": [(1, 0), (8, 0.9), (30, 0.9), (36, 0)],
                          "fr": [(1, 0), (8, 0.9), (30, 0.9), (36, 0)], "fls": [(1, 0)] + beats([8, 14, 20, 26], 0.5, -0.3) + [(36, 0)],
                          "frs": [(1, 0)] + beats([8, 14, 20, 26], -0.3, 0.5) + [(36, 0)], "eye": [(1, 1), (6, 0.6), (30, 0.6), (36, 1)]}, loop=False)
A["Laugh"] = anim("Laugh", 45, {"lean": [(1, 0), (8, -0.2), (38, -0.18), (45, 0)], "z": [(1, 0)] + beats(BEAT8, 0.02, 0) + [(45, 0)],
                                "eye": [(1, 1), (6, 0.12), (40, 0.12), (45, 1)], "fl": [(1, 0)] + beats(BEAT8, 0.75, 0.3) + [(45, 0)], "fr": [(1, 0)] + beats(BEAT8, 0.75, 0.3) + [(45, 0)],
                                "fls": [(1, 0), (10, -0.5), (38, -0.5), (45, 0)], "frs": [(1, 0), (10, -0.5), (38, -0.5), (45, 0)], "sq": osc(45, 0.05, 4), "hp": [(1, 0), (8, -0.12), (38, -0.1), (45, 0)]}, loop=False)
A["Sad"] = anim("Sad", 50, {"hp": [(1, 0), (12, 0.22), (44, 0.2), (50, 0)], "lean": [(1, 0), (12, 0.06), (44, 0.06), (50, 0)], "sq": [(1, 0), (12, 0.1), (44, 0.1), (50, 0)],
                            "hr": [(1, 0), (14, 0.1), (24, -0.06), (34, 0.1), (44, 0), (50, 0)], "fl": [(1, 0), (12, -0.12), (44, -0.12), (50, 0)], "fr": [(1, 0), (12, -0.12), (44, -0.12), (50, 0)],
                            "eye": [(1, 1), (10, 0.55), (44, 0.55), (50, 1)], "fin": [(1, 0), (12, -0.2), (44, -0.2), (50, 0)]}, loop=False)
A["Surprised"] = anim("Surprised", 36, {"z": [(1, 0), (4, 0.08), (9, 0), (36, 0)], "lean": [(1, 0), (4, -0.22), (12, -0.12), (30, -0.1), (36, 0)], "sq": [(1, 0), (3, -0.1), (9, 0.08), (13, 0), (36, 0)],
                                        "eye": [(1, 1), (4, 1.38), (30, 1.32), (36, 1)], "fl": [(1, 0), (4, 1.3), (30, 1.2), (36, 0)], "fr": [(1, 0), (4, 1.3), (30, 1.2), (36, 0)]}, loop=False,
                    extra=lambda f, g: g.__setitem__("roll", 0.03 * math.sin(f * 2.5) if 8 < f < 30 else 0.0))
A["QuestAccept"] = anim("QuestAccept", 40, {"z": [(1, 0), (6, 0.06), (11, 0), (40, 0)], "sq": [(1, 0), (4, 0.08), (7, -0.08), (11, 0.06), (15, 0), (40, 0)],
                                            "fr": [(1, 0), (10, 2.4), (32, 2.4), (40, 0)], "frs": [(1, 0), (10, -0.3), (32, -0.3), (40, 0)], "fl": [(1, 0), (10, 0.3), (32, 0.3), (40, 0)],
                                            "hp": [(1, 0), (14, 0.25), (19, -0.05), (32, 0), (40, 0)], "eye": [(1, 1), (8, 0.35), (32, 0.35), (40, 1)], "fin": [(1, 0), (6, 0.5), (11, 0), (40, 0)]}, loop=False)
A["QuestComplete"] = anim("QuestComplete", 50, {"yaw": [(1, 0), (24, 2 * math.pi), (50, 2 * math.pi)], "z": [(1, 0), (6, 0.1), (12, 0), (18, 0.1), (24, 0), (30, 0.12), (37, 0), (50, 0)],
                                                "fl": [(1, 0), (6, 1.4)] + beats([24, 30, 34, 38, 42], 1.4, 2.2) + [(50, 0)], "fr": [(1, 0), (6, 1.4)] + beats([24, 30, 34, 38, 42], 1.4, 2.2) + [(50, 0)],
                                                "eye": [(1, 1), (5, 0.12), (45, 0.12), (50, 1)], "hp": [(1, 0), (30, -0.15), (45, -0.1), (50, 0)], "sq": osc(50, 0.06, 4), "fin": osc(50, 0.5, 3)}, loop=False)

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = Vector((0, 0, 0.5))
    preview(os.path.join(PRE, "joraeng_front.png"), Vector((0.5, -2.6, 0.8)), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "joraeng_back.png"), Vector((-1.4, 2.2, 1.0)), c, arm_ob=arm, action=A["Idle"], frame=1)
    for nm, fr in (("Laugh", 20), ("Sad", 30), ("Surprised", 8), ("QuestAccept", 20), ("No", 12), ("QuestComplete", 34), ("Wave", 17)):
        preview(os.path.join(PRE, f"joraeng_{nm.lower()}.png"), Vector((0.9, -2.5, 0.85)), c, arm_ob=arm, action=A[nm], frame=fr, size=480)

FISHER = "--fisher" in sys.argv   # 항구 어부(노란 어부 모자) — 모자 FBX + fisher_<표정> 초상화만 만듦(Joraeng.fbx 는 그대로)
QUEEN = "--queen" in sys.argv     # 하프물범 여왕(왕관 · 망토 · 보석 지팡이) — 장신구 FBX 3개 + queen_<표정> 초상화 원본만 만듦
if not FISHER and not QUEEN:
    glb, fbx = export_all("Joraeng", OUT, [arm, npc, scarf])
    os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "Joraeng.fbx"))
elif FISHER:
    # 노란 어부 모자(사우스웨스터): 정수리를 덮는 둥근 머리 부분 + 뒤가 길게 처지는 챙 + 갈색 띠 + 앞에 흰 물고기 표시.
    # 모델 공간(기본 자세) 그대로 내보냄 → Unity NpcAccessory 가 head 뼈 bindpose 로 붙임(민미 장비와 같은 방식).
    HAT = M_("Fisher_HatYellow", (0.98, 0.76, 0.12), rough=0.55); BAND = M_("Fisher_HatBand", (0.36, 0.2, 0.09), rough=0.8); FISHM = M_("Fisher_HatFish", (0.97, 0.97, 0.95), rough=0.5)
    hb = bmesh.new(); CZ, CR = 0.9, (0.305, 0.275, 0.14)
    bmesh.ops.create_uvsphere(hb, u_segments=32, v_segments=16, radius=1.0)
    for v in hb.verts: v.co = Vector((v.co.x * CR[0], v.co.y * CR[1], CZ + max(v.co.z, -0.5) * CR[2]))   # 아래쪽은 잘라 평평하게(머리 안)
    for f in hb.faces: f.smooth = True; f.material_index = 0
    rows = []; NB = 40                                                                  # 챙: 안쪽 타원(z 0.84) → 바깥(앞 0.38 · 옆 0.42 · 뒤 0.47, 뒤로 갈수록 처짐)
    for k in range(NB):
        a_ = 2 * math.pi * k / NB; cx, cy = math.cos(a_), math.sin(a_); back = max(0.0, cy)          # +Y = 뒤
        ro = 0.42 + 0.05 * back - 0.04 * max(0.0, -cy); zo = 0.8 - 0.08 * back
        inner = Vector((0.285 * cx, 0.255 * cy, 0.84)); outer = Vector((ro * cx, ro * 0.95 * cy, zo))
        rows.append([hb.verts.new(inner), hb.verts.new(outer), hb.verts.new(outer - Vector((0, 0, 0.022))), hb.verts.new(inner - Vector((0, 0, 0.022)))])
    for k in range(NB):
        r0, r1 = rows[k], rows[(k + 1) % NB]
        for j in range(4):
            f = hb.faces.new((r0[j], r1[j], r1[(j + 1) % 4], r0[(j + 1) % 4])); f.material_index = 0; f.smooth = True
    band, _ = tube([Vector((0.297 * math.cos(2 * math.pi * k / 40), 0.268 * math.sin(2 * math.pi * k / 40), 0.862)) for k in range(41)], lambda t: 0.018, sides=6, mat_idx=1)
    me_ = bpy.data.meshes.new("b"); band.to_mesh(me_); band.free(); hb.from_mesh(me_); bpy.data.meshes.remove(me_)
    for c_, r_ in ((Vector((0.0, -0.276, 0.915)), (0.06, 0.012, 0.028)), (Vector((0.07, -0.265, 0.915)), (0.022, 0.012, 0.028))):   # 흰 물고기(몸 + 꼬리)
        fb = ell(c_, r_, None, 2, 12, 8); me_ = bpy.data.meshes.new("f"); fb.to_mesh(me_); fb.free(); hb.from_mesh(me_); bpy.data.meshes.remove(me_)
    bmesh.ops.recalc_face_normals(hb, faces=hb.faces[:])
    hat = to_object("FisherHat", hb, [HAT, BAND, FISHM])
    bpy.ops.object.select_all(action="DESELECT"); hat.select_set(True)
    NPCDIR = os.path.join(ROOT, "unity", "Assets", "Resources", "Npc"); os.makedirs(NPCDIR, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(NPCDIR, "FisherHat.fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y")
    print("EXPORTED hat", os.path.join(NPCDIR, "FisherHat.fbx"))
    hv = hat.vertex_groups.new(name="head"); hv.add(list(range(len(hat.data.vertices))), 1.0, "REPLACE"); bind(hat, arm)   # 초상화에서는 머리를 따라 움직이게
if QUEEN:
    # 하프물범 여왕의 장신구 — 모델 공간(기본 자세) 그대로 내보냄 → Unity NpcAccessory 가 뼈 bindpose 로 붙임.
    # 왕관(head) · 망토(body: 어깨에서 등 뒤로 바닥까지 퍼지는 진홍 벨벳 + 흰 담비털 깃·단 + 금 수 + 보석 걸쇠) · 보석 지팡이(flip.R)
    QG = M_("Queen_Gold", (1.0, 0.76, 0.28), rough=0.22, metal=1.0); QV = M_("Queen_Velvet", (0.46, 0.03, 0.1), rough=0.85, sheen=0.8)
    QL = M_("Queen_Lining", (0.86, 0.66, 0.22), rough=0.5, metal=0.3); QE = M_("Queen_Ermine", (0.98, 0.97, 0.94), rough=1.0, sheen=1.0)
    QD = M_("Queen_ErmineDot", (0.05, 0.05, 0.06), rough=0.8); QS = mat("Queen_SapphireGlow", (0.12, 0.35, 1.0), rough=0.05, emit=(0.15, 0.4, 1.0), emit_strength=1.5)
    QR = M_("Queen_Ruby", (0.85, 0.05, 0.12), rough=0.08); QP = M_("Queen_Pearl", (0.97, 0.95, 1.0), rough=0.12)
    QM = [QG, QV, QL, QE, QD, QS, QR, QP]; G_, V_, L_, E_, D_, S_, R_, P_ = range(8)
    def put(bm_, other):
        me_ = bpy.data.meshes.new("t"); other.to_mesh(me_); other.free(); bm_.from_mesh(me_); bpy.data.meshes.remove(me_)
    def sph(c_, r3, mi, seg=16, rings=10):
        b_ = bmesh.new(); bmesh.ops.create_uvsphere(b_, u_segments=seg, v_segments=rings, radius=1.0)
        for v in b_.verts: v.co = Vector((c_[0] + v.co.x * r3[0], c_[1] + v.co.y * r3[1], c_[2] + v.co.z * r3[2]))
        for f in b_.faces: f.smooth = True; f.material_index = mi
        return b_
    def ring_tube(z, rx, ry, rad, mi, n=48, wob=0.0):
        pts = [Vector((rx * math.cos(2 * math.pi * k / n), ry * math.sin(2 * math.pi * k / n), z + wob * math.sin(6 * 2 * math.pi * k / n))) for k in range(n + 1)]
        tb_, _ = tube(pts, lambda t: rad, sides=8, mat_idx=mi); return tb_
    # ── 왕관: 금 띠 + 뾰족 장식 8개(진주) + 앞 큰 사파이어 + 빨간 벨벳 모자 + 띠 위 보석
    cr = bmesh.new(); CZ0, CZ1 = 0.895, 0.965; RX, RY = 0.205, 0.205 * DEPTH + 0.01
    for z_ in [CZ0 + (CZ1 - CZ0) * t for t in (0.0, 0.5, 1.0)]: put(cr, ring_tube(z_, RX, RY, 0.012, G_))
    rows_ = []
    for z_ in (CZ0, CZ1):
        rows_.append([cr.verts.new(Vector((RX * math.cos(2 * math.pi * k / 48), RY * math.sin(2 * math.pi * k / 48), z_))) for k in range(48)])
    for k in range(48):
        j = (k + 1) % 48; f = cr.faces.new((rows_[0][k], rows_[0][j], rows_[1][j], rows_[1][k])); f.material_index = G_; f.smooth = True
    for k in range(8):
        a_ = -math.pi / 2 + 2 * math.pi * k / 8; tall = 0.115 if k == 0 else 0.08 if k % 2 == 0 else 0.06
        base = Vector((RX * math.cos(a_), RY * math.sin(a_), CZ1)); tip = Vector((RX * 0.92 * math.cos(a_), RY * 0.92 * math.sin(a_), CZ1 + tall))
        sp_ = bmesh.new(); bmesh.ops.create_cone(sp_, cap_ends=True, segments=4, radius1=0.035, radius2=0.004, depth=tall)
        bmesh.ops.transform(sp_, matrix=Matrix.Translation((base + tip) / 2) @ Vector((0, 0, 1)).rotation_difference((tip - base).normalized()).to_matrix().to_4x4(), verts=sp_.verts[:])
        for f in sp_.faces: f.material_index = G_
        put(cr, sp_); put(cr, sph(tip + Vector((0, 0, 0.012)), (0.014, 0.014, 0.014), P_, 10, 6))
        jm = S_ if k % 2 == 0 else R_; jp = Vector(((RX + 0.008) * math.cos(a_ + math.pi / 8), (RY + 0.008) * math.sin(a_ + math.pi / 8), (CZ0 + CZ1) / 2))
        put(cr, sph(jp, (0.011, 0.011, 0.014), jm, 10, 6))
    put(cr, sph(Vector((0, -RY - 0.012, (CZ0 + CZ1) / 2 + 0.01)), (0.026, 0.014, 0.03), S_, 14, 8))                 # 앞 큰 사파이어
    put(cr, sph(Vector((0, 0.0, 0.955)), (RX * 0.95, RY * 0.95, 0.05), V_, 20, 10))                               # 벨벳 모자
    put(cr, sph(Vector((0, 0.0, 1.03)), (0.022, 0.022, 0.022), G_, 10, 6))                                         # 꼭대기 금 방울
    bmesh.ops.recalc_face_normals(cr, faces=cr.faces[:]); crown = to_object("QueenCrown", cr, QM)
    # ── 망토: 어깨(0.58)에서 바닥까지, 몸 뒤 반 바퀴 + 옆을 감쌈(앞은 열림), 아래로 갈수록 퍼지고 뒤로 끌림
    cp = bmesh.new(); NA_, NZ_ = 40, 26; A0, A1 = math.radians(-28), math.radians(208); Z0 = 0.545
    def cape_pt(i, j, off):
        a_ = A0 + (A1 - A0) * i / NA_; z_ = Z0 * (1 - j / NZ_) ** 1.0; drop = Z0 - z_
        rr = r_at(min(z_, 0.6)) + 0.03 + 0.22 * drop ** 1.3 + off; back = max(0.0, math.sin(a_))
        return Vector((rr * math.cos(a_), rr * DEPTH * math.sin(a_) + 0.18 * drop * back ** 2, max(0.006, z_ - 0.0)))
    outer = [[cp.verts.new(cape_pt(i, j, 0.012)) for i in range(NA_ + 1)] for j in range(NZ_ + 1)]
    inner = [[cp.verts.new(cape_pt(i, j, 0.0)) for i in range(NA_ + 1)] for j in range(NZ_ + 1)]
    for j in range(NZ_):
        for i in range(NA_):
            f = cp.faces.new((outer[j][i], outer[j][i + 1], outer[j + 1][i + 1], outer[j + 1][i])); f.material_index = V_; f.smooth = True
            f = cp.faces.new((inner[j + 1][i], inner[j + 1][i + 1], inner[j][i + 1], inner[j][i])); f.material_index = L_; f.smooth = True
    for j in range(NZ_):
        for i in (0, NA_):
            f = cp.faces.new((outer[j][i], outer[j + 1][i], inner[j + 1][i], inner[j][i]) if i == 0 else (inner[j][i], inner[j + 1][i], outer[j + 1][i], outer[j][i])); f.material_index = V_
    for i in range(NA_):
        f = cp.faces.new((outer[NZ_][i], outer[NZ_][i + 1], inner[NZ_][i + 1], inner[NZ_][i])); f.material_index = V_
    def ermine(pts, rad):
        tb_, _ = tube(pts, lambda t: rad, sides=10, mat_idx=E_); put(cp, tb_)
        for k in range(2, len(pts) - 1, 3):                                                    # 담비털 검은 점
            put(cp, sph(pts[k] + (pts[k] - Vector((0, 0, pts[k].z))).normalized() * rad * 0.95 * 0 + Vector((0, 0, rad * 0.9)), (rad * 0.28, rad * 0.28, rad * 0.45), D_, 8, 5))
    ermine([cape_pt(i, NZ_, 0.006) + Vector((0, 0, 0.02)) for i in range(NA_ + 1)], 0.028)                                   # 단(바닥)
    for i in (0, NA_): ermine([cape_pt(i, j, 0.006) for j in range(NZ_ + 1)], 0.024)                                         # 앞 자락 두 줄
    CR_ = [Vector(((r_at(0.53) + 0.035) * math.cos(2 * math.pi * k / 40), (r_at(0.53) + 0.035) * DEPTH * math.sin(2 * math.pi * k / 40), 0.535 + 0.014 * math.sin(2 * math.pi * k / 40))) for k in range(41)]
    ermine(CR_, 0.038)                                                                                                        # 담비털 깃
    bc_ = cape_pt(NA_ // 2 + 2, 9, 0.02); put(cp, sph(bc_, (0.075, 0.02, 0.085), G_, 20, 10)); put(cp, sph(bc_ + Vector((0, 0.012, 0)), (0.05, 0.015, 0.058), S_, 16, 8))   # 등 금 문장 + 사파이어
    for j in (NZ_ - 4, NZ_ - 2):                                                                                             # 금 수(단 근처 두 줄)
        put(cp, tube([cape_pt(i, j, 0.016) for i in range(2, NA_ - 1)], lambda t: 0.0055, sides=5, mat_idx=G_)[0])
    cz_ = Vector((0, -(r_at(0.53) + 0.068) * DEPTH, 0.53))                                                                 # 앞 걸쇠(금 + 루비)
    put(cp, sph(cz_, (0.04, 0.02, 0.04), G_, 14, 8)); put(cp, sph(cz_ + Vector((0, -0.018, 0)), (0.022, 0.012, 0.022), R_, 12, 7))
    bmesh.ops.recalc_face_normals(cp, faces=cp.faces[:]); cape = to_object("QueenCape", cp, QM)
    # ── 보석 지팡이: 오른 지느러미 끝에 세로로 — 금 막대 + 고리 · 위에 금 테두리 안 큰 사파이어 · 꼭대기 별
    tipR = FLIPJ["R"][1]; gp = Vector((tipR.x - 0.04, tipR.y - 0.07, tipR.z + 0.02))
    sc_ = bmesh.new(); z_lo, z_hi = 0.02, gp.z + 0.68
    put(sc_, tube([Vector((gp.x, gp.y, z_lo)), Vector((gp.x, gp.y, z_hi))], lambda t: 0.015, sides=10, mat_idx=G_)[0])
    for zz_ in (z_lo + 0.02, gp.z - 0.06, gp.z + 0.08, z_hi - 0.05):
        rt_ = [Vector((gp.x + 0.024 * math.cos(2 * math.pi * k / 20), gp.y + 0.024 * math.sin(2 * math.pi * k / 20), zz_)) for k in range(21)]
        put(sc_, tube(rt_, lambda t: 0.008, sides=6, mat_idx=G_)[0])
    top_c = Vector((gp.x, gp.y, z_hi + 0.075))
    put(sc_, sph(top_c, (0.055, 0.055, 0.065), S_, 20, 12))                                                                   # 큰 사파이어
    for k in range(3):                                                                                                       # 금 테두리(세 방향 고리)
        a_ = k * math.pi / 3; pts = [top_c + Matrix.Rotation(a_, 3, "Z") @ Vector((0.072 * math.cos(t), 0, 0.082 * math.sin(t))) for t in [2 * math.pi * q / 32 for q in range(33)]]
        put(sc_, tube(pts, lambda t: 0.006, sides=6, mat_idx=G_)[0])
    put(sc_, sph(top_c + Vector((0, 0, -0.085)), (0.03, 0.03, 0.02), G_, 12, 6))
    star_c = top_c + Vector((0, 0, 0.11)); sb = bmesh.new(); ctr0 = sb.verts.new(star_c + Vector((0, -0.01, 0))); ctr1 = sb.verts.new(star_c + Vector((0, 0.01, 0))); ring_ = []
    for k in range(10):
        a_ = math.pi / 2 + k * math.pi / 5; rr_ = 0.04 if k % 2 == 0 else 0.017; ring_.append(sb.verts.new(star_c + Vector((math.cos(a_) * rr_, 0, math.sin(a_) * rr_))))
    for k in range(10):
        f = sb.faces.new((ctr0, ring_[k], ring_[(k + 1) % 10])); f.material_index = G_; f = sb.faces.new((ctr1, ring_[(k + 1) % 10], ring_[k])); f.material_index = G_
    put(sc_, sb)
    for k in range(6): a_ = k * math.pi / 3; put(sc_, sph(top_c + Vector((0.075 * math.cos(a_), 0.075 * math.sin(a_), -0.06)), (0.01, 0.01, 0.01), P_, 8, 5))
    bmesh.ops.recalc_face_normals(sc_, faces=sc_.faces[:]); scepter = to_object("QueenScepter", sc_, QM)
    NPCDIR = os.path.join(ROOT, "unity", "Assets", "Resources", "Npc"); os.makedirs(NPCDIR, exist_ok=True)
    for ob_ in (crown, cape, scepter):
        bpy.ops.object.select_all(action="DESELECT"); ob_.select_set(True)
        bpy.ops.export_scene.fbx(filepath=os.path.join(NPCDIR, ob_.name + ".fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y")
        print("EXPORTED", ob_.name)
    for ob_, bn_ in ((crown, "head"), (cape, "body"), (scepter, "flip.R")):                                                # 초상화에서는 뼈를 따라 움직이게
        vg_ = ob_.vertex_groups.new(name=bn_); vg_.add(list(range(len(ob_.data.vertices))), 1.0, "REPLACE"); bind(ob_, arm)
    scarf.hide_render = True                                                                                                  # 여왕은 목도리 대신 담비털 깃
    if "--preview" in sys.argv:
        os.makedirs(PRE, exist_ok=True); c_ = Vector((0, 0, 0.6))
        preview(os.path.join(PRE, "queen_front.png"), Vector((0.7, -2.9, 1.0)), c_, arm_ob=arm, action=A["Idle"], frame=1)
        preview(os.path.join(PRE, "queen_back.png"), Vector((-1.6, 2.6, 1.2)), c_, arm_ob=arm, action=A["Idle"], frame=1)
if not FISHER and not QUEEN: print("EXPORTED", fbx, "verts:", len(npc.data.vertices), len(scarf.data.vertices))

# ── 대화창 초상화 원본: Workbench(평면 채색 + 외곽선 + 굴곡 음영) → tools/portrait_art.py 가 일러스트풍으로 마무리
sc = bpy.context.scene
for o in list(sc.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
extras = {}
def deco(name, center, radii, color, bone, rot=None):
    m = M_(name + "_m", color, rough=0.4); o = to_object(name, ell(center, radii, rot, 0, 20, 12), [m], bone); bind(o, arm); extras[name] = o; return o
for s, side in ((1, "L"), (-1, "R")):
    p, n = surf(EX * s - 0.01 * s, EZ + 0.014, 0.026); deco(f"shine{side}", p, (0.008, 0.004, 0.01), (1, 1, 1), f"eye.{side}", facing(n))
    p, n = surf(0.2 * s, 0.665, 0.004); deco(f"blush{side}", p, (0.05, 0.008, 0.026), (1.0, 0.42, 0.45), "head", facing(n))
p, n = surf(-0.15, 0.655, 0.012); deco("tear", p, (0.016, 0.01, 0.026), (0.35, 0.65, 1.0), "head", facing(n))
p, n = surf(0.23, 0.86, 0.02); deco("sweat", p, (0.018, 0.01, 0.03), (0.5, 0.8, 1.0), "head", facing(n) @ Matrix.Rotation(-0.3, 3, "Y"))
cam = bpy.data.objects.new("PCam", bpy.data.cameras.new("PCam")); sc.collection.objects.link(cam); sc.camera = cam
tgt = Vector((0, 0, 0.6)); cam.location = tgt + Vector((0.55, -1.9, 0.2)); cam.data.lens = 50
if QUEEN: tgt = Vector((-0.04, 0, 0.64)); cam.location = tgt + Vector((0.62, -2.35, 0.3)); cam.data.sensor_fit = "VERTICAL"; cam.data.sensor_height = 24
cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
key_l = bpy.data.objects.new("Key", bpy.data.lights.new("Key", "AREA")); key_l.data.energy = 110; key_l.data.size = 1.6; sc.collection.objects.link(key_l)
key_l.location = Vector((-1.4, -1.8, 1.9)); key_l.rotation_euler = (Vector((0, 0, 0.6)) - key_l.location).to_track_quat("-Z", "Y").to_euler()
rim = bpy.data.objects.new("Rim", bpy.data.lights.new("Rim", "AREA")); rim.data.energy = 70; rim.data.size = 1.0; sc.collection.objects.link(rim)
rim.location = Vector((1.6, 1.2, 1.4)); rim.rotation_euler = (Vector((0, 0, 0.6)) - rim.location).to_track_quat("-Z", "Y").to_euler()
w = bpy.data.worlds.new("PW"); w.use_nodes = True; sc.world = w; bg = w.node_tree.nodes["Background"]
sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True
sc.render.film_transparent = True; sc.render.resolution_x = 600; sc.render.resolution_y = 720; sc.render.image_settings.color_mode = "RGBA"
sc.view_settings.view_transform = "Standard"
sc.render.use_freestyle = True; sc.render.line_thickness_mode = "ABSOLUTE"; sc.render.line_thickness = 2.2
fs = bpy.context.view_layer.freestyle_settings; ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("L")
ls.select_by_visibility = True; ls.select_silhouette = True; ls.select_border = True; ls.select_crease = True; fs.crease_angle = math.radians(128)
ls.linestyle = ls.linestyle or bpy.data.linestyles.new("Ink"); ls.linestyle.color = (0.16, 0.085, 0.045); ls.linestyle.thickness = 2.2
ALL = [m for m in bpy.data.materials]
SHEEN = {m.name: m.node_tree.nodes["Principled BSDF"].inputs["Sheen Weight"].default_value for m in ALL if m.use_nodes}
BASE = {m.name: tuple(m.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value) for m in ALL if m.use_nodes}
def mode(flat):
    """flat: 빛 없이 자체 발광 = 평면 고유색(+ 외곽선) / 아니면: 흰 재질 + 조명 = 음영(털 노멀 포함)."""
    for m in ALL:
        if not m.use_nodes: continue
        b = m.node_tree.nodes["Principled BSDF"]; col = BASE[m.name]
        b.inputs["Base Color"].default_value = (0, 0, 0, 1) if flat else (0.8, 0.8, 0.8, 1)
        b.inputs["Emission Color"].default_value = col; b.inputs["Emission Strength"].default_value = 1.0 if flat else 0.0
        if "Specular IOR Level" in b.inputs: b.inputs["Specular IOR Level"].default_value = 0.0 if flat else 0.5
        if "Sheen Weight" in b.inputs: b.inputs["Sheen Weight"].default_value = 0.0 if flat else SHEEN.get(m.name, 0.0)
        b.inputs["Metallic"].default_value = 0.0                                              # 음영 패스는 금속도 무광(크롬처럼 까맣게 안 나오게)
    key_l.hide_render = rim.hide_render = flat; bg.inputs[1].default_value = 0.0 if flat else 0.22; sc.render.use_freestyle = flat
    sc.cycles.samples = 12 if flat else 28; sc.cycles.use_denoising = not flat
    sc.cycles.filter_width = 1.2
os.makedirs(RAW, exist_ok=True)
EXPR = {"normal": ("Idle", 1, []), "happy": ("Laugh", 20, ["blushL", "blushR"]), "sad": ("Sad", 30, ["tear"]),
        "surprised": ("Surprised", 10, ["sweat"]), "troubled": ("No", 10, ["sweat", "blushL", "blushR"])}
if QUEEN: EXPR["surprised"] = ("Surprised", 3, ["sweat"]); EXPR["troubled"] = ("Sad", 14, ["sweat"])   # 여왕: 지팡이가 얼굴을 가리지 않는 자세
arm.animation_data_create()
only = [a_ for a_ in sys.argv if a_.startswith("--only=")]
for key_, (act_name, fr, show) in EXPR.items():
    if only and key_ not in only[0][7:].split(","): continue
    arm.animation_data.action = A[act_name]; sc.frame_set(fr)
    for nm, o in extras.items(): o.hide_render = not ((nm.startswith("shine") and key_ != "happy") or nm in show)
    for flat, tag in ((True, "flat"), (False, "shade")):
        mode(flat); sc.render.filepath = os.path.join(RAW, f"{'fisher' if FISHER else 'queen' if QUEEN else 'joraeng'}_{key_}_{tag}.png"); bpy.ops.render.render(write_still=True)
    print("✔ portrait raw", key_)
