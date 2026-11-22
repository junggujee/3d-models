"""일반 몬스터 ②: 그린볼런(딱딱한 블럭형). 모델 + 뼈대 + 애니메이션 → exports/GreenBlock.* 와 unity/Assets/Models/GreenBlock.fbx.
모양(사용자 장난감 사진 기준): 단단하고 매끈한 초록 판 6개가 맞물린 정육면체. 판마다 3×3 칸 — 가운데는 구멍,
      변 가운데 칸 4개는 두껍게 튀어나온 십자, 모서리 칸은 한 단 낮게(계단처럼 각진 맞물림). 칸 사이 가는 홈, 모서리는 살짝 깎음.
      오른쪽(+X) 면에 톱니바퀴(던지는 원거리 기술). 한 변 0.66m(핑크큐브보다 조금 큼).
뼈대: root → body → plate.px/nx/py/ny/top/bottom(판마다 100%) + gear. 판이 따로라서 쓰러지면 6개가 사방으로 흩어짐.
애니메이션(30fps): Idle61 Walk31 Run21(묵직하게 기울며 쿵쿵) Windup20(판이 벌어지며 힘 모음) Attack1 24(블럭 박치기, 타격 11)
                  Attack2 24(톱니 던지기, 11 에 톱니가 손을 떠남 → 18~24 새 톱니가 돋아남) Guard12(판을 꽉 조임) Dodge14 Hit18(판이 덜컹) Death40(판 6개가 흩어져 떨어짐)
실행: <bpy 파이썬> blender/build_green_block.py [--preview]"""
import sys, os, math, random, shutil
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "exports"); PRE = os.path.join(HERE, "..", "previews"); UNITY = os.path.join(HERE, "..", "unity", "Assets", "Models")
reset_scene()
GREEN = mat("Block_Green", (0.025, 0.3, 0.02), rough=0.3)        # 매끈한 플라스틱
GREEN_IN = mat("Block_GreenInner", (0.012, 0.12, 0.01), rough=0.5)  # 안쪽(구멍으로 보이는 면) — 어둡게
GEAR = mat("Block_Gear", (0.04, 0.36, 0.03), rough=0.28)
MATS = [GREEN, GREEN_IN, GEAR]; I_G, I_IN, I_GEAR = range(3)

S, T_, CZ = 0.33, 0.085, 0.33          # 반 변 · 판 두께 · 중심 높이
STEP, GAP, BEV = 0.045, 0.008, 0.016    # 모서리 칸이 낮은 정도 · 칸 사이 홈 · 모서리 깎기

def box(bm, lo, hi, mat_idx):
    """축 정렬 상자(lo~hi, 판 좌표) + 모서리 깎기."""
    tmp = bmesh.new(); bmesh.ops.create_cube(tmp, size=1.0)
    for v in tmp.verts: v.co = Vector(((lo[k] + hi[k]) / 2 + v.co[k] * (hi[k] - lo[k]) for k in range(3)))
    bmesh.ops.bevel(tmp, geom=list(tmp.edges), offset=min(BEV, min(hi[k] - lo[k] for k in range(3)) * 0.3), segments=2, affect="EDGES", profile=0.6)
    for f in tmp.faces: f.material_index = mat_idx; f.smooth = False
    me = bpy.data.meshes.new("t"); tmp.to_mesh(me); tmp.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)

# 판: 면 방향 axis(0 x,1 y,2 z) · 부호 sgn. 겹치지 않게 x 판은 면 전체, y 판은 x 를 판 두께만큼 안쪽, z 판은 x·y 모두 안쪽
PLATES = [("plate.px", 0, 1), ("plate.nx", 0, -1), ("plate.py", 1, 1), ("plate.ny", 1, -1), ("plate.top", 2, 1), ("plate.bottom", 2, -1)]
FR = (0.0, 0.29, 0.71, 1.0)   # 칸 나눔: 가운데 구멍을 크게(사진의 큰 네모 구멍)
def plate_mesh(axis, sgn):
    bm = bmesh.new(); u_ax, v_ax = [k for k in range(3) if k != axis]
    def extent(k): return (-S + T_, S - T_) if (k == 0 and axis in (1, 2)) or (k == 1 and axis == 2) else (-S, S)
    (u0, u1), (v0, v1) = extent(u_ax), extent(v_ax)
    us = [u0 + (u1 - u0) * f for f in FR]; vs = [v0 + (v1 - v0) * f for f in FR]
    for i in range(3):
        for j in range(3):
            if i == 1 and j == 1: continue                     # 가운데 구멍
            corner = i != 1 and j != 1
            outer = S - (STEP if corner else 0.0); inner = S - T_
            lo, hi = [0, 0, 0], [0, 0, 0]
            g = GAP if corner else GAP * 0.4
            lo[u_ax], hi[u_ax] = us[i] + g / 2, us[i + 1] - g / 2; lo[v_ax], hi[v_ax] = vs[j] + g / 2, vs[j + 1] - g / 2
            lo[axis], hi[axis] = (inner, outer) if sgn > 0 else (-outer, -inner)
            box(bm, lo, hi, I_G)
    # 안쪽 면(구멍으로 보이는 곳)을 어둡게
    for f in bm.faces:
        if f.normal[axis] * sgn < -0.9: f.material_index = I_IN
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector((0, 0, CZ)))
    return bm

def gear_mesh(r_out=0.15, r_in=0.11, teeth=8, thick=0.045, hole=0.04):
    """톱니바퀴(X 축을 향함): 각진 이 8개 + 가운데 구멍. 판 오른쪽(+X) 면 가운데에 붙음."""
    bm = bmesh.new(); prof = []
    for k in range(teeth):
        a0 = 2 * math.pi * k / teeth
        for da, r in ((-0.16, r_in), (-0.13, r_out), (0.13, r_out), (0.16, r_in)): prof.append((a0 + da * 2 * math.pi / teeth * 2.2, r))
    outer = [(r * math.cos(a), r * math.sin(a)) for a, r in prof]
    n = len(outer); inner = [(hole * math.cos(2 * math.pi * k / n), hole * math.sin(2 * math.pi * k / n)) for k in range(n)]
    x0, x1 = S + 0.005, S + 0.005 + thick
    def ring(pts, x): return [bm.verts.new((x, p[0], p[1] + CZ)) for p in pts]
    o0, o1, i0, i1 = ring(outer, x0), ring(outer, x1), ring(inner, x0), ring(inner, x1)
    for k in range(n):
        m = (k + 1) % n
        bm.faces.new((o0[k], o0[m], o1[m], o1[k])); bm.faces.new((i0[m], i0[k], i1[k], i1[m]))
        bm.faces.new((o1[k], o1[m], i1[m], i1[k])); bm.faces.new((o0[m], o0[k], i0[k], i0[m]))
    bm.normal_update()
    for f in bm.faces: f.material_index = I_GEAR
    return bm

parts = []
for name, axis, sgn in PLATES: parts.append(to_object(name, plate_mesh(axis, sgn), MATS, name))
parts.append(to_object("gear", gear_mesh(), MATS, "gear"))
block = join(parts, "GreenBlock")
for p in block.data.polygons: p.use_smooth = False

# 뼈대: 판 뼈는 판 가운데에서 바깥(법선) 방향으로
bones = [("root", None, Vector((0, 0, 0)), Vector((0, 0, 0.12))), ("body", "root", Vector((0, 0, 0)), Vector((0, 0, CZ)))]
NORM = {}
for name, axis, sgn in PLATES:
    n = Vector((0, 0, 0)); n[axis] = sgn; NORM[name] = n; c = Vector((0, 0, CZ)) + n * (S - T_ / 2)
    bones.append((name, "body", c, c + n * 0.15))
bones.append(("gear", "body", Vector((S + 0.03, 0, CZ)), Vector((S + 0.18, 0, CZ))))
arm = make_armature("GreenBlockRig", bones); bind(block, arm)

# ── 애니메이션 도우미: 월드 방향 이동·회전을 뼈 로컬로
def L(bn, v):
    return arm.data.bones[bn].matrix_local.to_3x3().inverted() @ Vector(v)
def Rw(bn, axis, ang):
    M = arm.data.bones[bn].matrix_local.to_3x3(); return (M.inverted() @ Matrix.Rotation(ang, 3, Vector(axis)) @ M).to_euler()
def Rxyz(bn, pitch=0.0, roll=0.0, yaw=0.0):
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
def anim(name, length, spec, loop=True, per_frame=None):
    """spec: z(튐) lean(앞 숙임 +) roll yaw open(판이 바깥으로 벌어짐 m) spin(톱니 회전 rad) gs(톱니 크기) + per_frame(f) → {뼈: (loc, rot)} 덮어쓰기."""
    tr = {k: track(spec.get(k, []), 1.0 if k == "gs" else 0.0) for k in ("z", "lean", "roll", "yaw", "open", "spin", "gs", "fwd")}
    act = new_action(arm, name, length)
    for f in range(1, length + 1):
        ff = ((f - 1) % (length - 1)) + 1 if loop else f; g = lambda k: tr[k](ff)
        over = per_frame(f) if per_frame else {}
        key(arm, "root", f, loc=Vector((0, g("z"), -g("fwd"))), rot=Rxyz("root", 0, 0, g("yaw")))
        key(arm, "body", f, rot=Rxyz("body", g("lean"), g("roll")))
        for name, _, _ in PLATES:
            if name in over: lo, ro = over[name]; key(arm, name, f, loc=lo, rot=ro); continue
            jit = 0.004 * math.sin(f * 0.9 + hash(name) % 7) if g("open") > 0.02 else 0.0
            key(arm, name, f, loc=L(name, NORM[name] * (g("open") + jit)))
        if "gear" in over: lo, ro = over["gear"]; key(arm, "gear", f, loc=lo, rot=ro, scale=Vector((1, 1, 1)) * g("gs"))
        else: key(arm, "gear", f, loc=L("gear", NORM["plate.px"] * g("open")), rot=Rw("gear", (1, 0, 0), g("spin")), scale=Vector((1, 1, 1)) * max(0.001, g("gs")))
    finish_action(arm, act, loop=loop); act.frame_start, act.frame_end = 1, length; return act

def stomp(n, h, tilt):
    k = lambda u: 1 + round(u * (n - 1))
    return {"z": [(1, 0), (k(.15), 0), (k(.4), h), (k(.62), 0), (n, 0)], "lean": [(1, 0), (k(.15), -tilt * .3), (k(.4), tilt), (k(.62), -tilt * .25), (k(.75), tilt * .1), (n, 0)],
            "spin": [(1, 0), (n, math.pi / 2)], "open": [(1, 0), (k(.62), .0), (k(.66), .012), (k(.74), 0), (n, 0)]}
A = {}
A["Idle"] = anim("Idle", 61, {"open": [(1, 0), (15, .006), (31, 0), (46, .006), (61, 0)], "spin": [(1, 0), (61, math.pi / 4)], "z": [(1, 0), (31, .01), (61, 0)]})
A["Walk"] = anim("Walk", 31, stomp(31, 0.07, 0.16))
A["Run"] = anim("Run", 21, stomp(21, 0.09, 0.22))
A["Windup"] = anim("Windup", 20, {"open": [(1, 0), (10, .05), (20, .06)], "lean": [(1, 0), (10, -.18), (20, -.2)], "spin": [(1, 0), (20, 6 * math.pi)]}, loop=False)
# 블럭 박치기: 판을 벌려 힘 모음 → 11 에 앞으로 기울며 들이받음(판이 꽉 닫힘) → 쿵 되돌아옴
A["Attack1"] = anim("Attack1", 24, {"open": [(1, .05), (6, .06), (10, 0), (11, -.004), (14, .02), (24, 0)], "lean": [(1, -.2), (6, -.3), (10, .5), (11, .55), (15, .1), (19, -.06), (24, 0)],
                                    "z": [(1, 0), (6, 0), (9, .12), (11, .02), (13, 0), (24, 0)], "fwd": [(1, 0), (6, -.05), (11, .3), (16, .1), (24, 0)]}, loop=False)
# 톱니 던지기: 몸을 왼쪽으로 비틀며 톱니가 빨라짐 → 11 에 오른쪽으로 휙 돌며 던짐(톱니가 사라짐) → 18~24 새 톱니가 돋아남
A["Attack2"] = anim("Attack2", 24, {"yaw": [(1, 0), (7, .55), (11, -.5), (16, -.25), (24, 0)], "spin": [(1, 0), (10, 9 * math.pi), (24, 10 * math.pi)],
                                    "gs": [(1, 1), (10, 1.15), (11, .001), (17, .001), (21, 1.1), (24, 1)], "open": [(1, 0), (7, .02), (11, 0), (24, 0)]}, loop=False)
A["Guard"] = anim("Guard", 12, {"open": [(1, -.006), (12, -.006)], "z": [(1, -.01), (12, -.01)], "lean": [(1, -.06), (12, -.06)]})
A["Dodge"] = anim("Dodge", 14, {"z": [(1, 0), (5, .14), (10, 0), (14, 0)], "roll": [(1, 0), (5, -.35), (10, -.1), (12, .05), (14, 0)]}, loop=False)
A["Hit"] = anim("Hit", 18, {"lean": [(1, 0), (3, -.22), (7, .06), (11, -.03), (18, 0)], "open": [(1, 0), (3, .045), (6, .005), (9, .02), (12, 0), (18, 0)]}, loop=False,
                per_frame=None)

# 쓰러짐: 부르르 떨다가(1~12) 판 6개가 사방으로 튀어 나가 빙글빙글 돌며 땅에 납작하게 떨어짐(+ 작게 튐), 톱니도 굴러감
rng = random.Random(4)
SCAT = {}
for name, axis, sgn in PLATES:
    n = NORM[name]; side = Vector((n.x, n.y, 0)); d = side.normalized() if side.length > 0 else Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized()
    dist = rng.uniform(0.75, 1.15) if axis != 2 else rng.uniform(0.35, 0.7); up = 1.6 if axis == 2 and sgn > 0 else 1.0
    tilt_axis = d.cross(Vector((0, 0, 1))).normalized() if axis != 2 else Vector((-d.y, d.x, 0))
    final_tilt = (math.pi / 2 if axis != 2 else (0.0 if sgn > 0 else 0.0)) + 2 * math.pi * rng.choice((0, 1))
    SCAT[name] = (d, dist, up, tilt_axis, final_tilt * (-1 if axis != 2 else 1), rng.uniform(-1.2, 1.2))
def death_frame(f):
    out = {}; shake = 0.012 * math.sin(f * 2.7) * (f < 12)
    for name, axis, sgn in PLATES:
        d, dist, up, tax, tilt, spin_z = SCAT[name]; c0 = arm.data.bones[name].head_local.copy()
        if f < 12: out[name] = (L(name, NORM[name] * (0.01 + shake)), Euler((0, 0, 0))); continue
        u = min(1.0, (f - 12) / 16); land = 1.0 - (1 - u) ** 1.0
        # 수평으로 dist 만큼, 위로 포물선(정점 up×0.45m) → 바닥(판 두께 절반 높이)
        z_end = T_ / 2 + 0.005 - c0.z; hz = 4 * 0.45 * up * u * (1 - u) + z_end * u
        bounce = 0.05 * math.sin(math.pi * min(1, max(0, (f - 28) / 5))) if f >= 28 else 0.0
        wv = d * dist * (1 - (1 - u) ** 2) + Vector((0, 0, hz + bounce))
        rot = (Matrix.Rotation(spin_z * u, 3, "Z") @ Matrix.Rotation(tilt * (1 - (1 - u) ** 2), 3, tax))
        M = arm.data.bones[name].matrix_local.to_3x3(); out[name] = (L(name, wv), (M.inverted() @ rot @ M).to_euler())
    # 톱니: 옆으로 굴러가 쓰러짐
    if f >= 12:
        u = min(1.0, (f - 12) / 20); g0 = arm.data.bones["gear"].head_local
        wv = Vector((0.9 * (1 - (1 - u) ** 2), -0.3 * u, (0.05 - g0.z) * u + 0.3 * 4 * u * (1 - u)))
        M = arm.data.bones["gear"].matrix_local.to_3x3(); rot = Matrix.Rotation(-math.pi / 2 * u, 3, "Y") @ Matrix.Rotation(12 * u, 3, "X")
        out["gear"] = (L("gear", wv), (M.inverted() @ rot @ M).to_euler())
    return out
A["Death"] = anim("Death", 40, {"lean": [(1, 0), (6, -.1), (12, 0)]}, loop=False, per_frame=death_frame)

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = Vector((0, 0, 0.33))
    preview(os.path.join(PRE, "greenblock_front.png"), Vector((1.6, -2.0, 1.2)), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "greenblock_throw.png"), Vector((1.9, -1.9, 1.0)), c, arm_ob=arm, action=A["Attack2"], frame=7)
    preview(os.path.join(PRE, "greenblock_ram.png"), Vector((2.2, -1.2, 0.9)), Vector((0, -0.2, 0.33)), arm_ob=arm, action=A["Attack1"], frame=11)
    preview(os.path.join(PRE, "greenblock_death_mid.png"), Vector((2.6, -2.6, 1.8)), Vector((0, 0, 0.3)), arm_ob=arm, action=A["Death"], frame=19)
    preview(os.path.join(PRE, "greenblock_death_end.png"), Vector((2.6, -2.6, 1.8)), Vector((0, 0, 0.1)), arm_ob=arm, action=A["Death"], frame=40)

glb, fbx = export_all("GreenBlock", OUT, [arm, block])
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "GreenBlock.fbx"))
print("EXPORTED", glb, fbx, "verts:", len(block.data.vertices))
