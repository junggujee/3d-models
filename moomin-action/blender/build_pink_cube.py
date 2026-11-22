"""일반 몬스터 ①: 핑크큐브말랑이(슬라임형). 모델 + 뼈대 + 애니메이션 → exports/PinkCube.* 와 unity/Assets/Models/PinkCube.fbx.
모양(사용자 장난감 사진 기준): 동그란 혹 3×3×3 개가 뭉친 정육면체(면마다 혹 9개, 모서리도 둥근 혹), 반투명 분홍 + 오돌도돌한 표면(노멀 맵),
      정면에 흰 눈알 두 개(검은 눈동자) + 크게 웃는 입(닫힌 고리 모양 입술, 연한 분홍). 크기 = 민미 배까지(한 변 0.6m).
몸 표면: 중심에서 쏜 광선이 혹 27개(구)와 만나는 거리의 부드러운 최댓값 → 혹 사이가 말랑하게 파인 홈.
뼈대: root(위아래 튐) → body(바닥 중심 기준: 눌림·늘어남·기울기) → top(위쪽 절반, 늦게 따라와 출렁임). 피부는 높이로 body/top 에 부드럽게 나눔.
애니메이션(30fps): Idle61 Walk31 Run21(통통 튀어 이동) Windup20 Attack1 24(몸통박치기, 타격 11) Attack2 24(쭈욱 늘어나 찌르기, 타격 11)
                  Guard12 Dodge14 Hit18(맞으면 길게 늘어났다 출렁) Death40(녹아 퍼짐)
실행: <bpy 파이썬> blender/build_pink_cube.py [--preview]"""
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "exports"); PRE = os.path.join(HERE, "..", "previews"); UNITY = os.path.join(HERE, "..", "unity", "Assets", "Models")
TEX = os.path.join(UNITY, "Textures", "Pebble_Normal.png")
reset_scene()

JELLY = mat("Cube_Jelly", (0.98, 0.22, 0.42), rough=0.42)
EYE = mat("Cube_EyeWhite", (0.96, 0.97, 0.95), rough=0.35)
PUPIL = mat("Cube_Pupil", (0.02, 0.02, 0.03), rough=0.3)
LIP = mat("Cube_Lip", (1.0, 0.6, 0.7), rough=0.5)            # 입술: 연한 분홍(사용자 요청 — 사진의 파랑 대신)
LIPIN = mat("Cube_LipInner", (0.86, 0.45, 0.55), rough=0.6)    # 입술 고리 안쪽 홈
EYERIM = mat("Cube_EyeRim", (0.62, 0.78, 0.95), rough=0.5)    # 눈알 아래 옅은 하늘색 테(사진)
MATS = [JELLY, EYE, PUPIL, LIP, LIPIN, EYERIM]; I_JELLY, I_EYE, I_PUPIL, I_LIP, I_LIPIN, I_RIM = range(6)
# 미리보기용: 젤리 속 비침 + 오돌도돌 노멀 맵(Unity 는 MoominSetup 이 같은 그림으로 재질을 만듦)
b = JELLY.node_tree.nodes["Principled BSDF"]
for k, v in (("Subsurface Weight", 0.35), ("Coat Weight", 0.35), ("Coat Roughness", 0.25)):
    if k in b.inputs: b.inputs[k].default_value = v
if "Subsurface Radius" in b.inputs: b.inputs["Subsurface Radius"].default_value = (0.25, 0.08, 0.1)
if os.path.exists(TEX):
    nt = JELLY.node_tree; img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(TEX); img.image.colorspace_settings.name = "Non-Color"
    uvm = nt.nodes.new("ShaderNodeMapping"); uvm.inputs["Scale"].default_value = (6, 6, 1); tc = nt.nodes.new("ShaderNodeTexCoord")
    nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 0.8
    nt.links.new(tc.outputs["UV"], uvm.inputs[0]); nt.links.new(uvm.outputs[0], img.inputs[0]); nt.links.new(img.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"])

# ── 몸: 혹 27개(간격 A, 반지름 RB)를 중심 높이 CZ 에 → 한 변 ≈ 2(A+RB) = 0.6m
A, RB, CZ, SOFT = 0.17, 0.13, 0.30, 70.0
SPH = [Vector((i * A, j * A, k * A)) for i in (-1, 0, 1) for j in (-1, 0, 1) for k in (-1, 0, 1)]
def ray_t(d):
    """중심에서 방향 d 로 나간 광선이 혹 덩어리 표면과 만나는 거리(부드러운 최댓값 → 혹 사이 홈이 둥글게)."""
    ts = []
    for c in SPH:
        dc = d.dot(c); disc = RB * RB - c.length_squared + dc * dc
        if disc > 0: ts.append(dc + math.sqrt(disc))
    m = max(ts); return m + math.log(sum(math.exp(SOFT * (t - m)) for t in ts)) / SOFT - math.log(len(ts)) / SOFT * 0.15
def front_y(x, z):
    """정면(-Y)에서 본 표면 위치(얼굴 부품 붙이기): 각 혹의 앞쪽 끝 중 가장 앞(부드러운 최솟값)."""
    ys = []; z = z - CZ   # 혹 중심은 몸 가운데 기준
    for c in SPH:
        q = RB * RB - (x - c.x) ** 2 - (z - c.z) ** 2
        if q > 0: ys.append(c.y - math.sqrt(q))
    if not ys: return -A
    m = min(ys); return m - math.log(sum(math.exp(-SOFT * (y - m)) for y in ys)) / SOFT

RES = 40
bm = bmesh.new(); uvl = bm.loops.layers.uv.new("UVMap")
for axis in range(3):
    for sgn in (-1, 1):
        grid = {}
        for i in range(RES + 1):
            for j in range(RES + 1):
                u, v = i / RES * 2 - 1, j / RES * 2 - 1
                p = [0, 0, 0]; p[axis] = sgn; p[(axis + 1) % 3] = u; p[(axis + 2) % 3] = v
                d = Vector(p).normalized(); grid[i, j] = bm.verts.new(d * ray_t(d) + Vector((0, 0, CZ)))
        for i in range(RES):
            for j in range(RES):
                q = [grid[i, j], grid[i + 1, j], grid[i + 1, j + 1], grid[i, j + 1]]
                f = bm.faces.new(q if sgn > 0 else q[::-1]); f.smooth = True; f.material_index = I_JELLY
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5); bm.normal_update()
for f in bm.faces:   # 상자 투영 UV(면이 향한 축으로) — 노멀 맵 돌기가 고르게
    n = f.normal; ax = max(range(3), key=lambda k: abs(n[k]))
    for l in f.loops:
        co = l.vert.co - Vector((0, 0, CZ)); o = [co[(ax + 1) % 3], co[(ax + 2) % 3]]; l[uvl].uv = (o[0] / 0.6 + 0.5, o[1] / 0.6 + 0.5)
parts = [to_object("body", bm, MATS)]

# ── 얼굴(정면 -Y): 위쪽 가운데 맞닿은 눈알 두 개 + 아래 넓게 웃는 입(닫힌 고리 입술 + 안쪽 홈)
def proud(x, z, r):
    """반지름 r 원판이 혹 위로 묻히지 않게: 그 범위에서 가장 앞(가장 작은 y)."""
    return min(front_y(x + r * math.cos(a), z + r * math.sin(a)) for a in [k * math.pi / 4 for k in range(8)] + [0]) if r > 0 else front_y(x, z)
def add(name, bm_, idx=None):
    if idx is not None:
        for f in bm_.faces: f.material_index = idx
    parts.append(to_object(name, bm_, MATS))
EYE_R, EYE_Z = 0.062, CZ + 0.085
for s in (1, -1):
    x = 0.064 * s; y = proud(x, EYE_Z, EYE_R * 0.8)
    add("eyerim", oriented_disc(Vector((x, y + 0.004, EYE_Z - 0.006)), Vector((0, -1, 0)), EYE_R * 1.04, EYE_R * 1.02, 0.03, I_RIM))
    add("eye", oriented_disc(Vector((x, y - 0.004, EYE_Z + 0.004)), Vector((0, -1, 0)), EYE_R, EYE_R, 0.034, I_EYE))
    add("pupil", oriented_disc(Vector((x + 0.004 * s, y - 0.036, EYE_Z + 0.002)), Vector((0, -1, 0)), 0.021, 0.024, 0.008, I_PUPIL))
# 입: 웃는 곡선 위 입술(가운데 아래로 처짐 없이 양끝이 올라감) + 아래 입술 → 둥근 끝으로 이어진 닫힌 고리
MW, MZ = 0.15, CZ - 0.065
def lip_curve(t, upper):   # t: -1~1
    base = MZ + 0.045 * t * t; return base + (0.024 if upper else -0.03 * (1 - 0.35 * t * t))
loop = []
for k in range(25): t = -1 + 2 * k / 24; loop.append((MW * t, lip_curve(t, True)))
for k in range(1, 8):   # 오른쪽 끝 반원
    a = math.pi / 2 - math.pi * k / 8; zc = (lip_curve(1, True) + lip_curve(1, False)) / 2; rr = (lip_curve(1, True) - lip_curve(1, False)) / 2
    loop.append((MW + rr * 0.9 * math.cos(a), zc + rr * math.sin(a)))
for k in range(25): t = 1 - 2 * k / 24; loop.append((MW * t, lip_curve(t, False)))
for k in range(1, 8):   # 왼쪽 끝 반원
    a = -math.pi / 2 - math.pi * k / 8; zc = (lip_curve(-1, True) + lip_curve(-1, False)) / 2; rr = (lip_curve(-1, True) - lip_curve(-1, False)) / 2
    loop.append((-MW + rr * 0.9 * math.cos(a), zc + rr * math.sin(a)))
loop.append(loop[0]); loop.append(loop[1])
pts = [Vector((x, proud(x, z, 0.02) - 0.006, z)) for x, z in loop]
b2, _ = tube(pts, lambda t: 0.021, sides=12, mat_idx=I_LIP); add("lips", b2)
# 고리 안쪽: 살짝 들어간 진한 분홍 홈(가운데 띠)
inner = [Vector((MW * 0.92 * t, proud(MW * 0.92 * t, MZ + 0.045 * t * t - 0.003, 0.02) + 0.004, MZ + 0.045 * t * t - 0.003)) for t in [-1 + 2 * k / 20 for k in range(21)]]
b3, _ = tube(inner, lambda t: 0.014, sides=8, mat_idx=I_LIPIN); add("lipInner", b3)

cube = join(parts, "PinkCube")

# ── 뼈대: root → body(바닥 중심) → top(가운데 높이부터)
H = 2 * CZ
arm = make_armature("PinkCubeRig", [("root", None, Vector((0, 0, 0)), Vector((0, 0, 0.12))), ("body", "root", Vector((0, 0, 0)), Vector((0, 0, CZ))), ("top", "body", Vector((0, 0, CZ)), Vector((0, 0, H)))])
def smooth(a, b, x): t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
gb, gt = cube.vertex_groups.new(name="body"), cube.vertex_groups.new(name="top")
for v in cube.data.vertices:
    w = smooth(0.16, 0.46, v.co.z); gt.add([v.index], w, "REPLACE")
    if w < 1: gb.add([v.index], 1 - w, "REPLACE")
bind(cube, arm)

# ── 애니메이션: 키 목록(프레임, 값)을 부드럽게 이어 매 프레임 키잉. top 은 body 를 늦게 따라와 출렁(오버래핑 액션)
X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
def R(bn, pitch=0.0, roll=0.0, yaw=0.0):
    """월드 축 회전 → 뼈 로컬 오일러. pitch + = 위가 앞(-Y)으로 숙음, roll + = 위가 오른쪽(+X)으로."""
    M = arm.data.bones[bn].matrix_local.to_3x3()
    W = Matrix.Rotation(yaw, 3, Z) @ Matrix.Rotation(pitch, 3, X) @ Matrix.Rotation(-roll, 3, Y)
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
def anim(name, length, spec, loop=True, lag=3, jiggle=1.0, extra=None):
    """spec: {"z": 튐 높이(m), "lean": 앞 숙임, "roll": 옆 기울기, "st": 늘어남(뼈 방향 배율), "sq": 눌림(+)/솟음(-), "yaw": 돌기}."""
    tr = {k: track(spec.get(k, []), 1.0 if k == "st" else 0.0) for k in ("z", "lean", "roll", "st", "sq", "yaw")}
    act = new_action(arm, name, length)
    for f in range(1, length + 1):
        g = lambda k, ff=f: tr[k](((ff - 1) % (length - 1)) + 1 if loop else max(1, ff))
        lean, roll, st, sq = g("lean"), g("roll"), g("st"), g("sq")
        # 늦게 따라오는 위쪽: 지금과 lag 프레임 전 값의 차이만큼 반대로 출렁 + 눌림 변화에 따른 탄성
        pl, rl, sql = g("lean", f - lag), g("roll", f - lag), g("sq", f - 2)
        if extra: lean, roll, st, sq = extra(f, lean, roll, st, sq)
        sx = (1 / math.sqrt(st)) * (1 + 0.55 * sq); sy = st * (1 - sq)
        key(arm, "root", f, loc=Vector((0, g("z"), 0)), rot=R("root", 0, 0, g("yaw")))
        key(arm, "body", f, rot=R("body", lean, roll), scale=Vector((sx, sy, sx)))
        tp, trl = jiggle * 0.7 * (pl - lean), jiggle * 0.7 * (rl - roll); tsq = jiggle * 0.5 * (sql - sq)
        key(arm, "top", f, rot=R("top", tp, trl), scale=Vector((1 + 0.5 * tsq, 1 - tsq, 1 + 0.5 * tsq)))
    finish_action(arm, act, loop=loop); act.frame_start, act.frame_end = 1, length; return act

def hop(n, h, fwd=0.12):
    """통통 튀며 한 걸음(n 프레임): 움츠림 → 솟구침(늘어남) → 공중 → 떨어지며 늘어남 → 착지 납작 → 되튐."""
    k = lambda u: 1 + round(u * (n - 1))
    return {"z": [(1, 0), (k(.12), 0), (k(.28), h * .3), (k(.5), h), (k(.72), h * .25), (k(.8), 0), (n, 0)],
            "sq": [(1, 0), (k(.12), .22), (k(.26), -.22), (k(.5), -.04), (k(.72), -.14), (k(.8), .3), (k(.9), -.08), (n, 0)],
            "lean": [(1, 0), (k(.12), -.05), (k(.3), fwd), (k(.6), fwd * .4), (k(.8), -.06), (n, 0)]}
A = {}
A["Idle"] = anim("Idle", 61, {"sq": [(1, 0), (16, .05), (31, -.03), (46, .04), (61, 0)], "roll": [(1, 0), (20, .03), (41, -.03), (61, 0)], "lean": [(1, 0), (31, .02), (61, 0)]})
A["Walk"] = anim("Walk", 31, hop(31, 0.26))
A["Run"] = anim("Run", 21, hop(21, 0.18, fwd=0.2))
A["Windup"] = anim("Windup", 20, {"sq": [(1, 0), (8, .3), (20, .34)], "lean": [(1, 0), (8, -.22), (20, -.26)]}, loop=False,
                   extra=lambda f, l, r, s, q: (l, r + (0.025 * math.sin(f * 2.3) if f > 8 else 0), s, q))   # 움츠리고 부르르
# 몸통박치기: 움츠림에서 튀어 올라 앞으로 숙이며 늘어남 → 11 에 철퍽(앞으로 납작) → 되튐 → 출렁이며 제자리
A["Attack1"] = anim("Attack1", 24, {"z": [(1, 0), (4, 0), (7, .2), (10, .1), (11, 0), (24, 0)],
                                    "lean": [(1, -.22), (4, -.1), (7, .55), (10, .95), (11, 1.0), (14, .55), (18, -.12), (21, .05), (24, 0)],
                                    "st": [(1, 1), (4, 1), (7, 1.35), (10, 1.25), (11, .9), (14, 1.1), (24, 1)],
                                    "sq": [(1, .3), (4, .32), (7, -.1), (10, 0), (11, .32), (14, .05), (18, -.06), (24, 0)]}, loop=False)
# 쭈욱 늘어나 찌르기: 뒤로 웅크림 → 앞으로 길게 쭉(최대 ≈ 1.2m 앞까지) → 잠깐 버팀 → 탄성으로 휙 돌아와 출렁
A["Attack2"] = anim("Attack2", 24, {"lean": [(1, 0), (5, -.3), (9, .85), (11, 1.0), (15, .95), (19, -.2), (22, .06), (24, 0)],
                                    "st": [(1, 1), (5, .85), (9, 1.8), (11, 2.15), (15, 2.0), (19, .82), (22, 1.06), (24, 1)],
                                    "sq": [(1, 0), (5, .25), (9, 0), (19, .18), (22, -.05), (24, 0)]}, loop=False, lag=4)
A["Guard"] = anim("Guard", 12, {"sq": [(1, .34), (6, .37), (12, .34)], "lean": [(1, -.08), (12, -.08)]}, jiggle=0.6)
A["Dodge"] = anim("Dodge", 14, {"z": [(1, 0), (5, .16), (10, 0), (14, 0)], "roll": [(1, 0), (5, -.45), (10, -.15), (12, .1), (14, 0)],
                                "sq": [(1, .2), (4, -.15), (10, .25), (14, 0)]}, loop=False)
# 맞으면: 납작 눌렸다가 → 위로 길게 쭉 늘어나며 뒤로 휘청 → 출렁이며 돌아옴
A["Hit"] = anim("Hit", 18, {"sq": [(1, 0), (3, .35), (6, 0), (10, .15), (13, -.05), (18, 0)], "st": [(1, 1), (3, 1), (6, 1.6), (10, .95), (13, 1.1), (18, 1)],
                            "lean": [(1, 0), (3, -.3), (6, -.22), (10, .1), (14, -.04), (18, 0)]}, loop=False, lag=4)
# 쓰러짐: 부르르 → 위로 쭉 늘어났다가 → 털썩 녹아 웅덩이처럼 퍼짐
A["Death"] = anim("Death", 40, {"st": [(1, 1), (8, 1.45), (14, .9), (40, 1)], "sq": [(1, 0), (8, -.1), (16, .55), (26, .8), (40, .86)],
                                "lean": [(1, 0), (8, -.15), (16, .12), (40, 0)]}, loop=False, lag=4,
                  extra=lambda f, l, r, s, q: (l, r + (0.04 * math.sin(f * 2.1) if f < 8 else 0), s, q))

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = Vector((0, 0, 0.3))
    preview(os.path.join(PRE, "pinkcube_front.png"), Vector((0.35, -2.4, 0.75)), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "pinkcube_three_quarter.png"), Vector((1.7, -1.8, 1.0)), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "pinkcube_attack2.png"), Vector((2.6, -1.2, 0.8)), Vector((0, -0.4, 0.3)), arm_ob=arm, action=A["Attack2"], frame=11)
    preview(os.path.join(PRE, "pinkcube_hit.png"), Vector((1.8, -2.0, 0.8)), Vector((0, 0, 0.45)), arm_ob=arm, action=A["Hit"], frame=6)
    preview(os.path.join(PRE, "pinkcube_hop.png"), Vector((1.8, -2.0, 0.8)), Vector((0, 0, 0.45)), arm_ob=arm, action=A["Walk"], frame=9)

glb, fbx = export_all("PinkCube", OUT, [arm, cube])
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "PinkCube.fbx"))
print("EXPORTED", glb, fbx, "verts:", len(cube.data.vertices))
