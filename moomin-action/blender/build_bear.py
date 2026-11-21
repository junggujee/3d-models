"""인간형 적: 잠자는 곰 인형(다이소 잠자는 곰 인형 바탕). 모델 + 뼈대 + 애니메이션 → exports/ 와 unity/Assets/Models/.
특징: 큰 둥근 머리, 작은 둥근 귀, 감은 눈(곡선), 크림색 주둥이 패치 + 짙은 코 + 人 모양 입, 분홍 소용돌이 볼, 줄무늬 배 패치, 짧은 팔다리, 짧은 벨벳 털.
애니메이션: Idle(꾸벅꾸벅 졸기) Walk Run Windup(공격 예고) Attack1(오른 주먹) Attack2(왼 주먹) Attack3(두 팔 내려찍기) Guard Dodge Hit Death
실행: python build_bear.py [--preview]"""
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(__file__))
from lib import *

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "exports"); PRE = os.path.join(HERE, "..", "previews"); UNITY = os.path.join(HERE, "..", "unity", "Assets", "Models")
reset_scene()

# ── 재질(벨벳 갈색 털, 크림, 짙은 갈색 자수, 분홍 자수, 줄무늬 배)
FUR = mat("Bear_Fur", (0.36, 0.21, 0.11), rough=0.8, sheen=0.7)
CREAM = mat("Bear_Cream", (0.93, 0.88, 0.77), rough=0.9, sheen=0.3)
DARK = mat("Bear_Dark", (0.13, 0.07, 0.05), rough=0.7)
PINK = mat("Bear_Pink", (0.86, 0.45, 0.42), rough=0.9)
GRAY = mat("Bear_Stripe", (0.6, 0.58, 0.54), rough=0.9, sheen=0.3)
MATS = [FUR, CREAM, DARK, PINK, GRAY]; I_FUR, I_CREAM, I_DARK, I_PINK, I_GRAY = range(5)

# ── 뼈대(앞 = -Y)
BONES = [
    ("root", None, T(0, 0, 0), T(0, 0.25, 0)), ("hips", "root", T(0, 0.42, 0), T(0, 1.05, 0)),
    ("head", "hips", T(0, 1.12, 0), T(0, 2.05, 0)),
    ("arm.L", "hips", T(0.42, 1.02, 0), T(0.54, 0.58, 0.04)), ("arm.R", "hips", T(-0.42, 1.02, 0), T(-0.54, 0.58, 0.04)),
    ("hand.R", "arm.R", T(-0.54, 0.58, 0.04), T(-0.56, 0.45, 0.06)),
    ("leg.L", "hips", T(0.2, 0.38, 0), T(0.2, 0.0, 0)), ("leg.R", "hips", T(-0.2, 0.38, 0), T(-0.2, 0.0, 0)),
]
arm = make_armature("BearRig", BONES)
parts = []
def add(name, bm, bone): parts.append(to_object(name, bm, MATS, bone))

# ── 몸
add("body", ellipsoid((0, 0.78, 0), (0.42, 0.48, 0.3), fuzz=0.006, seed=1, seg=48, rings=32), "hips")
add("hipsBulge", ellipsoid((0, 0.55, 0.01), (0.44, 0.32, 0.31), fuzz=0.006, seed=2), "hips")
for s, side in ((1, "L"), (-1, "R")):
    add(f"leg.{side}", ellipsoid((0.2 * s, 0.17, 0.02), (0.16, 0.22, 0.17), fuzz=0.005, seed=3 + s, seg=28, rings=18), f"leg.{side}")
    add(f"arm.{side}", ellipsoid((0.5 * s, 0.82, 0.02), (0.12, 0.27, 0.12), rot=(0, 0.28 * s, 0), fuzz=0.005, seed=5 + s, seg=28, rings=18), f"arm.{side}")
# 줄무늬 배 패치: 몸 표면을 따라 붙인 타원 격자(가로줄마다 크림/회색 재질 번갈아) — 텍스처 없이 FBX·Unity 에서 그대로 보임
def body_front(x, y):
    z = -1.0
    for (cx, cy, cz), (rx, ry, rz) in (((0, 0.78, 0), (0.42, 0.48, 0.3)), ((0, 0.55, 0.01), (0.44, 0.32, 0.31))):
        q = 1 - ((x - cx) / rx) ** 2 - ((y - cy) / ry) ** 2
        if q > 0: z = max(z, cz + rz * math.sqrt(q))
    return z
bm = bmesh.new(); RX, RY, CY, STRIPES, SUB, COLS = 0.26, 0.31, 0.7, 11, 3, 24
rows = []   # 가로 띠 격자: 줄 경계 = 행 경계 → 줄무늬가 깔끔하게 나뉨
for r in range(STRIPES * SUB + 1):
    t = r / (STRIPES * SUB); y = CY - RY + 2 * RY * t; w = RX * math.sqrt(max(0.0, 1 - ((y - CY) / RY) ** 2))
    rows.append([bm.verts.new(T(-w + 2 * w * c / COLS, y, body_front(-w + 2 * w * c / COLS, y) + 0.016)) for c in range(COLS + 1)])
for r in range(STRIPES * SUB):
    for c in range(COLS):
        f = bm.faces.new((rows[r][c], rows[r][c + 1], rows[r + 1][c + 1], rows[r + 1][c])); f.material_index = I_CREAM if (r // SUB) % 2 == 0 else I_GRAY
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5); bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
for f in bm.faces:
    f.smooth = True
    if f.normal.y > 0: f.normal_flip()   # 앞(-Y)을 보게
add("belly", bm, "hips")

# ── 머리(둥글고 아래쪽이 약간 넓음)
HC, HR = (0, 1.58, 0), (0.55, 0.47, 0.42)
add("head", ellipsoid(HC, HR, fuzz=0.006, seed=7, seg=56, rings=36), "head")
add("jowl", ellipsoid((0, 1.44, 0.02), (0.56, 0.32, 0.4), fuzz=0.006, seed=8), "head")
for s, side in ((1, "L"), (-1, "R")):
    add(f"ear.{side}", ellipsoid((0.43 * s, 1.95, -0.04), (0.15, 0.15, 0.08), rot=(0, 0, -0.35 * s), fuzz=0.004, seed=9 + s, seg=24, rings=16), "head")

def on_head(phi, th, off=0.0):
    d = Vector((math.cos(th) * math.sin(phi), math.sin(th), math.cos(th) * math.cos(phi)))
    p = Vector(HC) + Vector((d.x * HR[0], d.y * HR[1], d.z * HR[2])); n = Vector((d.x / HR[0], d.y / HR[1], d.z / HR[2])).normalized()
    return T(*(p + n * off)), Vector((n.x, -n.z, n.y)).normalized()
def frame(n): r = Vector((0, 0, 1)).cross(n).normalized(); return r, n.cross(r)

# 주둥이 패치·코·입
pb, nb = on_head(0, -0.18); add("muzzle", oriented_disc(pb, nb, 0.2 * K, 0.145 * K, 0.035 * K, I_CREAM), "head")
right, up = frame(nb); front = pb + nb * 0.042 * K   # 주둥이 패치 표면 위로
add("nose", oriented_disc(front + up * 0.045 * K, nb, 0.062 * K, 0.042 * K, 0.03 * K, I_DARK), "head")
for pts in ([(0, 0.0), (0, -0.055)], [(0, -0.055), (-0.05, -0.095)], [(0, -0.055), (0.05, -0.095)]):
    line = [front + right * (x * K) + up * (y * K) for x, y in pts]; line = [line[0].lerp(line[1], t / 4) for t in range(5)]
    b2, _ = tube(line, lambda t: 0.007 * K, sides=6, mat_idx=I_DARK); add("mouth", b2, "head")
for s in (1, -1):
    # 감은 눈(◡), 분홍 소용돌이 볼(동심원 두 개)
    pe, ne = on_head(0.36 * s, 0.12, 0.004); r_, u_ = frame(ne)
    arc = [pe + r_ * (x * K) + u_ * (-0.018 * (1 - (x / 0.065) ** 2) * K) for x in [(-0.065 + 0.13 * i / 10) for i in range(11)]]
    b2, _ = tube(arc, lambda t: 0.007 * K, sides=6, mat_idx=I_DARK); add("eye", b2, "head")
    pc, nc = on_head(0.68 * s, -0.12, 0.004); r_, u_ = frame(nc)
    for rad in (0.055, 0.03):
        ring = [pc + (r_ * math.cos(a) + u_ * math.sin(a)) * rad * K for a in [2 * math.pi * i / 24 for i in range(25)]]
        b2, _ = tube(ring, lambda t: 0.006 * K, sides=5, mat_idx=I_PINK, cap_end=False); add("cheek", b2, "head")

bear = join(parts, "SleepyBear"); bind(bear, arm)

# ── 애니메이션
X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
def E(b, ax, a): return rot_world(arm, b, ax, a)
def anim(name, length, build):
    act = new_action(arm, name, length); build(); finish_action(arm, act); act.frame_start, act.frame_end = 1, length; return act

def idle():   # 꾸벅꾸벅 조는 모습
    for f, nod, br in ((1, 0, 0), (25, 0.28, 1), (40, 0.32, 0.5), (44, -0.05, 0), (61, 0, 0)):
        key(arm, "head", f, rot=E("head", X, nod)); key(arm, "hips", f, scale=Vector((1, 1 + 0.02 * br, 1)))
        key(arm, "arm.L", f, rot=E("arm.L", Y, 0.05 * br)); key(arm, "arm.R", f, rot=E("arm.R", Y, -0.05 * br))
def walk(speed):
    def b():
        for f in (1, 8, 16, 23, 31):
            sw = math.sin((f - 1) / 30 * 2 * math.pi); a = 0.5 * speed
            key(arm, "leg.L", f, rot=E("leg.L", X, a * sw)); key(arm, "leg.R", f, rot=E("leg.R", X, -a * sw))
            key(arm, "arm.L", f, rot=E("arm.L", X, -0.4 * speed * sw)); key(arm, "arm.R", f, rot=E("arm.R", X, 0.4 * speed * sw))
            key(arm, "hips", f, rot=E("hips", Y, 0.1 * sw), loc=Vector((0, 0.025 * abs(math.cos((f - 1) / 30 * 2 * math.pi)) * speed, 0)))
            key(arm, "head", f, rot=E("head", Y, -0.08 * sw))
    return b
def windup():  # 예고: 오른팔을 뒤로 젖히고 몸을 뒤로
    for f, v in ((1, 0), (8, 1), (20, 1)):
        key(arm, "arm.R", f, rot=E("arm.R", X, 0.9 * v)); key(arm, "arm.L", f, rot=E("arm.L", X, -0.3 * v))
        key(arm, "hips", f, rot=E("hips", Z, 0.35 * v)); key(arm, "head", f, rot=E("head", X, -0.15 * v))
def punch(bone, other, twist):
    def b():  # 타격 프레임 11(24f) — Unity 스킬 hitTime 0.35s 와 맞춤
        for f, a, t in ((1, 0.6, 0.3), (6, 0.9, 0.4), (11, -1.6, -0.5), (16, -1.4, -0.4), (24, 0, 0)):
            key(arm, bone, f, rot=E(bone, X, a)); key(arm, other, f, rot=E(other, X, -a * 0.25)); key(arm, "hips", f, rot=E("hips", Z, t * twist), loc=Vector((0, -0.04 * (a < 0), 0)))
    return b
def slam():  # 두 팔 내려찍기, 타격 프레임 14(30f)
    for f, a, lean in ((1, 0, 0), (8, -2.8, 0.2), (14, -0.7, -0.35), (20, -0.6, -0.3), (30, 0, 0)):
        for bn in ("arm.L", "arm.R"): key(arm, bn, f, rot=E(bn, X, a))
        key(arm, "hips", f, rot=E("hips", X, lean)); key(arm, "head", f, rot=E("head", X, -lean * 0.5))
def guard():
    for f, v in ((1, 0), (6, 1), (12, 0.95)):
        key(arm, "arm.L", f, rot=E("arm.L", X, -1.7 * v)); key(arm, "arm.R", f, rot=E("arm.R", X, -1.7 * v)); key(arm, "head", f, rot=E("head", X, 0.2 * v)); key(arm, "hips", f, rot=E("hips", X, 0.1 * v))
def dodge():   # 옆으로 폴짝
    for f, v in ((1, 0), (5, 1), (10, 0.6), (14, 0)):
        key(arm, "hips", f, rot=E("hips", Y, 0.3 * v), loc=Vector((0, 0.12 * v * K * 2, 0))); key(arm, "leg.L", f, rot=E("leg.L", X, -0.6 * v)); key(arm, "leg.R", f, rot=E("leg.R", X, -0.6 * v))
def hit():
    for f, v in ((1, 0), (4, 1), (10, 0.5), (18, 0)):
        key(arm, "hips", f, rot=E("hips", X, 0.3 * v)); key(arm, "head", f, rot=E("head", X, 0.35 * v)); key(arm, "arm.L", f, rot=E("arm.L", Y, -0.6 * v)); key(arm, "arm.R", f, rot=E("arm.R", Y, 0.6 * v))
def death():   # 뒤로 벌러덩
    for f, v in ((1, 0), (10, 0.4), (24, 1), (40, 1)):
        key(arm, "root", f, rot=E("root", X, 1.5 * v), loc=Vector((0, 0.4 * v, -0.2 * v))); key(arm, "arm.L", f, rot=E("arm.L", Y, -0.9 * v)); key(arm, "arm.R", f, rot=E("arm.R", Y, 0.9 * v))

A = {}
A["Idle"] = anim("Idle", 61, idle); A["Walk"] = anim("Walk", 31, walk(1)); A["Run"] = anim("Run", 21, walk(1.6))
A["Windup"] = anim("Windup", 20, windup); A["Attack1"] = anim("Attack1", 24, punch("arm.R", "arm.L", 1)); A["Attack2"] = anim("Attack2", 24, punch("arm.L", "arm.R", -1))
A["Attack3"] = anim("Attack3", 30, slam); A["Guard"] = anim("Guard", 12, guard); A["Dodge"] = anim("Dodge", 14, dodge); A["Hit"] = anim("Hit", 18, hit); A["Death"] = anim("Death", 40, death)

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = T(0, 1.05, 0)
    preview(os.path.join(PRE, "bear_front.png"), T(0.8, 1.3, 5.2), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "bear_three_quarter.png"), T(3.2, 1.6, 4.2), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "bear_attack.png"), T(3.6, 1.3, 3.6), c, arm_ob=arm, action=A["Attack1"], frame=11)

glb, fbx = export_all("SleepyBear", OUT, [arm, bear])
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "SleepyBear.fbx"))
print("EXPORTED", glb, fbx, "verts:", len(bear.data.vertices))
