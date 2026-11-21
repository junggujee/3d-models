"""민미(서 있는 모습) 모델 + 뼈대 + 애니메이션(Idle, Walk, Run, Attack1~3, Hit, Death)을 만들어 exports/ 에 FBX·GLB·blend 로 내보냄.
실행: python build_moomin.py [--preview]
"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *

OUT = os.path.join(os.path.dirname(__file__), "..", "exports")
PRE = os.path.join(os.path.dirname(__file__), "..", "previews")
reset_scene()

FUR = mat("Moomin_Fur", (0.96, 0.95, 0.92), rough=1.0, sheen=1.0)
RING = mat("Moomin_EyeRing", (0.96, 0.94, 0.9), rough=1.0)
BLUE = mat("Moomin_EyeBlue", (0.10, 0.34, 0.70), rough=1.0)  # 광택 없는 부드러운 자수 느낌
BROW = mat("Moomin_Brow", (0.35, 0.15, 0.11), rough=1.0)
MATS = [FUR, RING, BLUE, BROW]
I_FUR, I_RING, I_BLUE, I_BROW = 0, 1, 2, 3

# ── 뼈대 (Blender 좌표, 앞 = -Y) ─────────────────────
def bn(*a): return T(*a)
BONES = [
    ("root",    None,    bn(0, 0, 0),        bn(0, 0.25, 0)),
    ("hips",    "root",  bn(0, 0.5, 0),      bn(0, 0.95, 0)),
    ("head",    "hips",  bn(0, 1.2, 0),      bn(0, 2.15, 0)),
    ("ear.L",   "head",  bn(0.38, 1.95, 0),  bn(0.5, 2.2, 0)),
    ("ear.R",   "head",  bn(-0.38, 1.95, 0), bn(-0.5, 2.2, 0)),
    ("eye.L",   "head",  bn(0.2, 1.62, 0.45), bn(0.2, 1.75, 0.45)),
    ("eye.R",   "head",  bn(-0.2, 1.62, 0.45), bn(-0.2, 1.75, 0.45)),
    ("arm.L",   "hips",  bn(0.55, 1.25, 0.05), bn(0.62, 0.7, 0.12)),
    ("arm.R",   "hips",  bn(-0.55, 1.25, 0.05), bn(-0.62, 0.7, 0.12)),
    ("hand.R",  "arm.R", bn(-0.62, 0.7, 0.12), bn(-0.62, 0.55, 0.14)),  # 검 소켓
    ("leg.L",   "hips",  bn(0.27, 0.32, 0.2), bn(0.27, 0.0, 0.2)),
    ("leg.R",   "hips",  bn(-0.27, 0.32, 0.2), bn(-0.27, 0.0, 0.2)),
    ("tail1",   "hips",  bn(0, 0.4, -0.42),  bn(0, 0.45, -0.58)),
    ("tail2",   "tail1", bn(0, 0.45, -0.58), bn(0, 0.62, -0.68)),
]
arm = make_armature("MoominRig", BONES)

# ── 몸 조각들 (각 조각은 한 뼈에 100% 결합 → 인형처럼 단단하게 움직임) ──
parts = []
def add(name, bm, bone): parts.append(to_object(name, bm, MATS, bone))

HC, HR = (0, 1.62, 0), (0.56, 0.5, 0.5)
add("body", ellipsoid((0, 0.8, -0.02), (0.52, 0.78, 0.46), fuzz=0.02, seed=1, seg=48, rings=32), "hips")
add("head", ellipsoid(HC, HR, fuzz=0.02, seed=2, seg=56, rings=36), "head")
add("snout", ellipsoid((0, 1.42, 0.3), (0.44, 0.27, 0.3), fuzz=0.015, seed=3), "head")
for s, side in ((1, "L"), (-1, "R")):
    add(f"leg.{side}", ellipsoid((0.27 * s, 0.14, 0.2), (0.17, 0.14, 0.26), fuzz=0.012, seed=4 + s, seg=32, rings=20), f"leg.{side}")
    add(f"arm.{side}", ellipsoid((0.6 * s, 0.95, 0.1), (0.15, 0.34, 0.15), rot=(0, 0, 0.12 * -s), fuzz=0.012, seed=6 + s, seg=32, rings=22), f"arm.{side}")
    # 귀: 머리 위 양끝
    ex, ey, ez = 0.36 * s, 2.04, -0.02
    add(f"ear.{side}", ellipsoid((ex, ey, ez), (0.085, 0.12, 0.07), rot=(0.1, 0, -0.4 * s * -1), fuzz=0.008, seed=8 + s, seg=24, rings=16), f"ear.{side}")

def on_head(phi, th, off=0.0):
    d = Vector((math.cos(th) * math.sin(phi), math.sin(th), math.cos(th) * math.cos(phi)))
    p = Vector(HC) + Vector((d.x * HR[0], d.y * HR[1], d.z * HR[2]))
    n = Vector((d.x / HR[0], d.y / HR[1], d.z / HR[2])).normalized()
    return p + n * off, n

for s, side in ((1, "L"), (-1, "R")):
    # 눈: 흰 테두리 + 파란 자수 (눈 사이 넓고 둥근 세로 타원)
    p, n = on_head(0.4 * s, 0.1, 0.0); pb, nb = T(*p), Vector((n.x, -n.z, n.y)).normalized()
    add(f"eyering.{side}", oriented_disc(pb, nb, 0.118 * K * 1.0, 0.14 * K, 0.012 * K, I_RING), f"eye.{side}")
    add(f"eyeblue.{side}", oriented_disc(pb + nb * 0.004, nb, 0.083 * K, 0.093 * K, 0.012 * K, I_BLUE), f"eye.{side}")
    # 눈썹: 가늘고 긴 곡선, 바깥쪽이 처짐
    p, n = on_head(0.42 * s, 0.55, 0.0); pb, nb = T(*p), Vector((n.x, -n.z, n.y)).normalized()
    right = Vector((0, 0, 1)).cross(nb).normalized(); up = nb.cross(right)
    pts = [pb + right * (-0.065 * s * K) + up * (0.012 * K), pb + up * (0.02 * K), pb + right * (0.07 * s * K) + up * (-0.025 * K)]
    # 곡선을 부드럽게 보간
    smooth = [(1 - t) ** 2 * pts[0] + 2 * (1 - t) * t * pts[1] + t * t * pts[2] for t in [i / 8 for i in range(9)]]
    bm, _ = tube(smooth, lambda t: 0.0055 * K * 1.3, sides=6, mat_idx=I_BROW)
    add(f"brow.{side}", bm, "head")

# 꼬리: 가늘게 뒤로 뻗었다가 위로 말림(끝이 가늘어짐) + 끝 털뭉치. 앞 절반 tail1, 뒤 절반 tail2
tp = [T(*a) for a in [(0, 0.4, -0.42), (0, 0.42, -0.52), (0, 0.5, -0.62), (0, 0.62, -0.68), (0, 0.8, -0.66), (0, 0.95, -0.6)]]
samples = tp
bm, rings = tube(samples, lambda t: (0.055 - 0.032 * t) * K, sides=8, mat_idx=I_FUR)
tail_ob = to_object("tail", bm, MATS)
n = len(tail_ob.data.vertices); half = n // 2
tail_ob.vertex_groups.new(name="tail1").add(list(range(0, half)), 1.0, "REPLACE")
tail_ob.vertex_groups.new(name="tail2").add(list(range(half, n)), 1.0, "REPLACE")
parts.append(tail_ob)
add("tailtip", ellipsoid((0, 0.97, -0.58), (0.035, 0.045, 0.035), seg=16, rings=10, fuzz=0.005), "tail2")

body = join(parts, "Moomin")
bind(body, arm)

# ── 애니메이션 ───────────────────────────────────────
X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
def E(b, ax, a): return rot_world(arm, b, ax, a)

def anim(name, length, build, loop=True):
    act = new_action(arm, name, length); build(act); finish_action(arm, act, loop)
    act.frame_start, act.frame_end = 1, length
    return act

def idle(act):  # 숨쉬기 · 머리 갸웃 · 꼬리 살랑 · 눈 깜빡 (60f)
    for f in (1, 31, 61):
        k = (f - 1) / 30
        br = 1 + 0.02 * (math.sin(k * math.pi) if f == 31 else 0)
        key(arm, "hips", f, scale=Vector((1, br, 1)))
        key(arm, "head", f, rot=E("head", Z, (0.08 if f == 31 else -0.04)))
        key(arm, "tail1", f, rot=E("tail1", Z, (0.35 if f == 31 else -0.2)))
        key(arm, "arm.L", f, rot=E("arm.L", X, 0.04 if f == 31 else 0)); key(arm, "arm.R", f, rot=E("arm.R", X, -0.04 if f == 31 else 0))
        key(arm, "ear.L", f, rot=E("ear.L", Y, 0.1 if f == 31 else 0)); key(arm, "ear.R", f, rot=E("ear.R", Y, -0.1 if f == 31 else 0))
    for f, s in ((1, 1), (44, 1), (47, 0.08), (50, 1), (61, 1)):
        for e in ("eye.L", "eye.R"): key(arm, e, f, scale=Vector((1, s, 1)))

def walk(speed):
    def b(act):  # 뒤뚱뒤뚱 걷기(몸 좌우 흔들림) (30f)
        a = 0.55 * speed
        for f in (1, 8, 16, 23, 31):
            ph = (f - 1) / 30 * 2 * math.pi
            sw = math.sin(ph)
            key(arm, "leg.L", f, rot=E("leg.L", X, a * sw)); key(arm, "leg.R", f, rot=E("leg.R", X, -a * sw))
            key(arm, "arm.L", f, rot=E("arm.L", X, -0.5 * speed * sw)); key(arm, "arm.R", f, rot=E("arm.R", X, 0.5 * speed * sw))
            key(arm, "hips", f, rot=E("hips", Z, 0.12 * sw) , loc=Vector((0, 0, 0.03 * abs(math.cos(ph)) * speed)))
            key(arm, "head", f, rot=E("head", Z, -0.1 * sw))
            key(arm, "tail1", f, rot=E("tail1", Z, 0.4 * math.sin(ph + 1)))
    return b

def attack(a_up, a_hit, tw, length=24):  # 검을 올렸다가 내려침
    def b(act):
        t = length / 24
        f = lambda x: max(1, round(x * t))
        key(arm, "arm.R", 1, rot=E("arm.R", X, 0)); key(arm, "hips", 1, rot=E("hips", Z, 0))
        key(arm, "arm.R", f(7), rot=E("arm.R", X, -a_up)); key(arm, "hips", f(7), rot=E("hips", Z, -tw * 0.6), loc=Vector((0, 0.04, 0)))
        key(arm, "arm.R", f(11), rot=E("arm.R", X, -a_hit)); key(arm, "hips", f(11), rot=E("hips", Z, tw), loc=Vector((0, -0.1, 0)))
        key(arm, "head", f(11), rot=E("head", X, 0.08))
        key(arm, "arm.R", f(16), rot=E("arm.R", X, -a_hit * 0.9)); key(arm, "hips", f(16), rot=E("hips", Z, tw * 0.6), loc=Vector((0, -0.06, 0)))
        key(arm, "arm.R", length, rot=E("arm.R", X, 0)); key(arm, "hips", length, rot=E("hips", Z, 0))
        key(arm, "arm.L", f(11), rot=E("arm.L", X, 0.5)); key(arm, "arm.L", length, rot=E("arm.L", X, 0)); key(arm, "arm.L", 1, rot=E("arm.L", X, 0))
        key(arm, "tail1", f(11), rot=E("tail1", Z, 0.6)); key(arm, "tail1", length, rot=E("tail1", Z, 0)); key(arm, "tail1", 1, rot=E("tail1", Z, 0))
    return b

def hit(act):  # 맞고 뒤로 휘청 (18f)
    for f, v in ((1, 0), (4, 1), (10, 0.5), (18, 0)):
        key(arm, "hips", f, rot=E("hips", X, 0.35 * v), loc=Vector((0, 0.08 * v, 0)))
        key(arm, "head", f, rot=E("head", X, 0.3 * v))
        key(arm, "arm.L", f, rot=E("arm.L", X, 0.8 * v)); key(arm, "arm.R", f, rot=E("arm.R", X, 0.8 * v))
        key(arm, "ear.L", f, rot=E("ear.L", Y, 0.5 * v)); key(arm, "ear.R", f, rot=E("ear.R", Y, -0.5 * v))

def death(act):  # 뒤로 벌러덩 (40f, 루프 아님)
    for f, v in ((1, 0), (10, 0.5), (24, 1), (40, 1)):
        key(arm, "root", f, rot=E("root", X, 1.5 * v), loc=Vector((0, 0.4 * v, -0.2 * v)))
        key(arm, "arm.L", f, rot=E("arm.L", Y, -0.8 * v)); key(arm, "arm.R", f, rot=E("arm.R", Y, 0.8 * v))
    for f, s in ((1, 1), (14, 1), (18, 0.1), (40, 0.1)):
        for e in ("eye.L", "eye.R"): key(arm, e, f, scale=Vector((1, s, 1)))

ACTS = {}
ACTS["Idle"] = anim("Idle", 61, idle)
ACTS["Walk"] = anim("Walk", 31, walk(1.0))
ACTS["Run"] = anim("Run", 21, walk(1.6))
ACTS["Attack1"] = anim("Attack1", 24, attack(2.6, 1.2, 0.5), loop=False)
ACTS["Attack2"] = anim("Attack2", 24, attack(2.0, 0.9, -0.6), loop=False)
ACTS["Attack3"] = anim("Attack3", 30, attack(3.0, 1.3, 0.9, 30), loop=False)
ACTS["Hit"] = anim("Hit", 18, hit, loop=False)
ACTS["Death"] = anim("Death", 40, death, loop=False)

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True)
    c = T(0, 1.05, 0)
    preview(os.path.join(PRE, "moomin_front.png"), T(1.2, 1.2, 5.6), c, arm_ob=arm, action=ACTS["Idle"], frame=1)
    for nm, fr in (("moomin_attack", 11), ("moomin_walk", 8)):
        preview(os.path.join(PRE, nm + ".png"), T(3.4, 1.4, 4.8), c, arm_ob=arm, action=ACTS[nm.split("_")[1].capitalize() if nm != "moomin_attack" else "Attack1"], frame=fr)

glb, fbx = export_all("Moomin", OUT, [arm, body])
print("EXPORTED", glb, fbx, "tris:", sum(len(p.vertices) for p in [body.data]))
