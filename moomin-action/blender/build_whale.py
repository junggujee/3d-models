"""지상괴물형 적: 고래고래(지상 최대의 끼룩이). 모델 + 뼈대 + 애니메이션 → exports/ 와 unity/Assets/Models/.
특징: 통통한 둥근 고래 몸(위 벨벳 파랑 · 아래·턱 흰색, 등 가운데 솔기), 양옆 둥근 지느러미로 땅을 짚고 기어 다님, 작은 등지느러미,
      굵은 꼬리 + 꼬리 지느러미, 까만 점 눈 · 넓은 미소 · 분홍 혀 · 분홍 볼, 정수리 숨구멍에서 솟는 물방울(끼룩이), 벌어지는 아래턱(고래빔).
애니메이션(30fps): Idle Walk Run Windup Attack1(고래박치기, 타격 11) Attack2(지느러미 치기, 11) Attack3(꼬리 휘두르기 한 바퀴, 14)
                  Jump(점프 내려찍기, 착지 24) Beam(몸을 세우고 입을 벌려 고래빔, 20) BeamHold(빔 유지, 맥동 20) Spout(물기둥, 16)
                  Guard Dodge Hit Death
실행: python build_whale.py [--preview]"""
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(__file__))
from lib import *

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "exports"); PRE = os.path.join(HERE, "..", "previews"); UNITY = os.path.join(HERE, "..", "unity", "Assets", "Models")
reset_scene()

BLUE = mat("Whale_Blue", (0.07, 0.17, 0.5), rough=0.75, sheen=0.8)
WHITE = mat("Whale_White", (0.9, 0.91, 0.88), rough=0.85, sheen=0.3)
DARK = mat("Whale_Dark", (0.03, 0.03, 0.05), rough=0.6)
PINK = mat("Whale_Pink", (0.95, 0.48, 0.52), rough=0.85)
MOUTH = mat("Whale_Mouth", (0.32, 0.06, 0.1), rough=0.8)
SPOUT = mat("Whale_Spout", (0.7, 0.88, 1.0), rough=0.2, emit=(0.5, 0.75, 1.0), emit_strength=0.6)
SEAM = mat("Whale_Seam", (0.045, 0.11, 0.36), rough=0.9)
MATS = [BLUE, WHITE, DARK, PINK, MOUTH, SPOUT, SEAM]; I_BLUE, I_WHITE, I_DARK, I_PINK, I_MOUTH, I_SPOUT, I_SEAM = range(7)

# ── 몸 치수(three.js 좌표: y 위, z 앞)
BC, BR = (0.0, 0.92, 0.1), (1.05, 0.88, 1.45)          # 몸 타원
def body_pt(phi, th, off=0.0):
    """몸 표면의 점과 법선(Blender). phi = 옆 각도(0 = 정면), th = 위아래 각도."""
    d = Vector((math.cos(th) * math.sin(phi), math.sin(th), math.cos(th) * math.cos(phi)))
    p = Vector(BC) + Vector((d.x * BR[0], d.y * BR[1], d.z * BR[2])); n = Vector((d.x / BR[0], d.y / BR[1], d.z / BR[2])).normalized()
    return T(*(p + n * off)), Vector((n.x, -n.z, n.y)).normalized()
def frame(n): r = Vector((0, 0, 1)).cross(n).normalized(); return r, n.cross(r)

BONES = [
    ("root", None, T(0, 0, 0), T(0, 0.3, 0)), ("body", "root", T(0, 0.55, -0.3), T(0, 1.4, -0.3)),
    ("jaw", "body", T(0, 0.55, 0.7), T(0, 0.45, 1.5)),
    ("flipper.L", "body", T(0.85, 0.55, 0.55), T(1.4, 0.3, 0.6)), ("flipper.R", "body", T(-0.85, 0.55, 0.55), T(-1.4, 0.3, 0.6)),
    ("tail", "body", T(0, 0.85, -1.25), T(0, 0.95, -1.9)), ("fluke", "tail", T(0, 0.95, -1.9), T(0, 1.05, -2.35)),
    ("spout", "body", T(0, 1.75, 0.35), T(0, 2.1, 0.35)),
]
arm = make_armature("WhaleRig", BONES)
parts = []
def add(name, bm, bone): parts.append(to_object(name, bm, MATS, bone))

# 몸: 아래·앞 턱은 흰색(앞으로 갈수록 경계가 올라감), 등 가운데 솔기
bm = ellipsoid(BC, BR, seg=112, rings=72, fuzz=0.005, seed=3)
# 아래턱 자리(앞 아래)는 비워 둠 → 아래턱 조각이 그 자리를 막고, 벌리면 입 안이 보임
cut = [f for f in bm.faces if (lambda c: -c.y / K > 0.72 and c.z / K < 0.6)(f.calc_center_median())]
bmesh.ops.delete(bm, geom=cut, context="FACES")
for f in bm.faces:
    c = f.calc_center_median(); x, z, y = c.x / K, -c.y / K, c.z / K      # Blender → three
    front = max(0.0, min(1.0, (z - 0.55) / 0.95)); line = 0.52 + 0.42 * front * front
    f.material_index = I_WHITE if y < line else (I_SEAM if abs(x) < 0.025 and y > 1.2 else I_BLUE)
add("body", bm, "body")
# 아래턱(흰 턱 앞부분 위에 얹힘): 입을 벌리면 안쪽 입(짙은 붉은색)이 보임
add("jaw", ellipsoid((0, 0.52, 0.95), (0.78, 0.3, 0.62), fuzz=0.004, seed=4, mat_idx=I_WHITE), "jaw")
add("mouthInside", ellipsoid((0, 0.6, 0.92), (0.6, 0.17, 0.5), mat_idx=I_MOUTH), "body")   # 몸 안쪽에 숨어 있다가 턱을 벌리면 보임
# 지느러미: 몸 옆에서 바닥을 짚는 둥근 노
for s, side in ((1, "L"), (-1, "R")):
    add(f"flipper.{side}", ellipsoid((1.08 * s, 0.42, 0.62), (0.42, 0.12, 0.3), rot=(0.15, 0.25 * s, -0.35 * s), fuzz=0.004, seed=5 + s, seg=32, rings=18), f"flipper.{side}")
# 등지느러미(작고 둥글게 뒤로 누움)
add("dorsal", ellipsoid((0, 1.78, -0.35), (0.07, 0.26, 0.24), rot=(-0.5, 0, 0), fuzz=0.003, seed=8, seg=24, rings=16), "body")
# 꼬리: 굵게 시작해 가늘어지는 관 + 양쪽으로 펼친 꼬리 지느러미
pts = [T(0, 0.85 + 0.2 * t * t, -1.2 - 0.8 * t) for t in [i / 8 for i in range(9)]]
b2, _ = tube(pts, lambda t: (0.5 - 0.3 * t) * K, sides=20, mat_idx=I_BLUE); add("tail", b2, "tail")
for s in (1, -1):
    add("fluke", ellipsoid((0.32 * s, 1.08, -2.12), (0.42, 0.07, 0.2), rot=(0.2, 0.35 * s, 0.2 * s), fuzz=0.003, seed=9 + s, seg=28, rings=14), "fluke")
# 얼굴: 까만 점 눈, 넓은 미소 + 분홍 혀, 분홍 볼
for s in (1, -1):
    pe, ne = body_pt(0.42 * s, 0.12, 0.004); add("eye", oriented_disc(pe, ne, 0.085 * K, 0.095 * K, 0.03 * K, I_DARK), "body")
    pe2 = pe + frame(ne)[1] * 0.03 * K + frame(ne)[0] * (-0.022 * s) * K; add("eyeHi", oriented_disc(pe2 + ne * 0.02 * K, ne, 0.022 * K, 0.022 * K, 0.012 * K, I_WHITE), "body")
    pc, nc = body_pt(0.62 * s, -0.04, 0.004); add("cheek", oriented_disc(pc, nc, 0.12 * K, 0.07 * K, 0.015 * K, I_PINK), "body")
smile = []
for i in range(25):
    u = -1 + 2 * i / 24; p, n = body_pt(0.5 * u, -0.08 - 0.07 * (1 - u * u) + 0.02 * math.cos(u * 6), 0.012); smile.append(p)
b2, _ = tube(smile, lambda t: 0.014 * K, sides=6, mat_idx=I_DARK); add("smile", b2, "body")
pt, nt = body_pt(0, -0.2, 0.006); add("tongue", oriented_disc(pt, nt, 0.13 * K, 0.07 * K, 0.02 * K, I_PINK), "body")
# 숨구멍 + 솟는 물방울(끼룩이)
ph, nh = body_pt(0, 1.15, 0.002); add("blowhole", oriented_disc(ph, nh, 0.09 * K, 0.04 * K, 0.015 * K, I_DARK), "body")
for i, (dx, h, r) in enumerate(((0, 0.28, 0.1), (0.12, 0.2, 0.07), (-0.12, 0.2, 0.07), (0.05, 0.42, 0.06), (-0.06, 0.38, 0.055))):
    add("drop", ellipsoid((dx, 1.79 + h, 0.33), (r, r * 1.25, r), mat_idx=I_SPOUT, seg=16, rings=10), "spout")

whale = join(parts, "Goraegorae"); bind(whale, arm)

# ── 애니메이션
X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
def E(b, ax, a): return rot_world(arm, b, ax, a)
def anim(name, length, build, loop=True):
    act = new_action(arm, name, length); build(); finish_action(arm, act, loop=loop); act.frame_start, act.frame_end = 1, length; return act
def fl(f, a, b=None):   # 두 지느러미(앞뒤로 노 젓기 = X축)
    key(arm, "flipper.L", f, rot=E("flipper.L", X, a)); key(arm, "flipper.R", f, rot=E("flipper.R", X, a if b is None else b))
def spout(f, v): key(arm, "spout", f, scale=Vector((1, 1, 1)) * (0.7 + 0.5 * v))

def idle():   # 숨 쉬기 · 꼬리 살랑 · 물방울 퐁퐁
    for f in (1, 16, 31, 46, 61):
        u = (f - 1) / 60 * 2 * math.pi
        key(arm, "body", f, scale=Vector((1 + 0.02 * math.sin(u), 1 + 0.02 * math.sin(u), 1 - 0.015 * math.sin(u))))
        key(arm, "tail", f, rot=E("tail", Z, 0.15 * math.sin(u))); key(arm, "fluke", f, rot=E("fluke", X, 0.2 * math.sin(u + 1)))
        fl(f, 0.1 * math.sin(u), -0.1 * math.sin(u)); spout(f, 0.5 + 0.5 * math.sin(u * 2))
def walk(speed):   # 배로 콩콩 튀며 지느러미로 짚고 나아감
    def b():
        for f in (1, 6, 11, 16, 21, 26, 31) if speed < 1.3 else (1, 6, 11, 16, 21):
            L = 31 if speed < 1.3 else 21; u = (f - 1) / (L - 1) * 2 * math.pi
            key(arm, "root", f, loc=Vector((0, 0, 0.08 * K * speed * abs(math.sin(u)))))
            key(arm, "body", f, rot=E("body", Y, 0.07 * speed * math.sin(u)), scale=Vector((1 + 0.03 * math.cos(2 * u), 1 + 0.03 * math.cos(2 * u), 1 - 0.04 * math.cos(2 * u))))
            fl(f, 0.6 * speed * math.sin(u), -0.6 * speed * math.sin(u)); key(arm, "tail", f, rot=E("tail", Z, 0.25 * speed * math.sin(u)))
            key(arm, "fluke", f, rot=E("fluke", X, 0.3 * math.cos(u)))
    return b
def windup():   # 예고: 몸을 뒤로 젖히고 지느러미를 들어 올림
    for f, v in ((1, 0), (8, 1), (20, 1)):
        key(arm, "body", f, rot=E("body", X, 0.25 * v), loc=Vector((0, 0.15 * K * v, 0))); fl(f, -0.5 * v); key(arm, "tail", f, rot=E("tail", X, -0.4 * v))
def ram():   # 고래박치기: 움츠렸다가 머리로 들이받음, 타격 11
    for f, lean, push in ((1, 0, 0), (6, 0.3, 0.15), (11, -0.35, -0.45), (16, -0.25, -0.35), (24, 0, 0)):
        key(arm, "body", f, rot=E("body", X, lean), loc=Vector((0, push * K, 0))); fl(f, 0.6 * (lean < 0), 0.6 * (lean < 0)); key(arm, "tail", f, rot=E("tail", X, -lean))
def flipper_slap():   # 오른 지느러미로 후려치기, 타격 11
    for f, a, tw in ((1, 0, 0), (6, -1.2, 0.25), (11, 1.0, -0.3), (16, 0.8, -0.25), (24, 0, 0)):
        key(arm, "flipper.R", f, rot=E("flipper.R", Z, a)); key(arm, "body", f, rot=E("body", Z, tw))
def tail_spin():   # 제자리 한 바퀴 돌며 꼬리 휘두르기, 타격 14
    for f, a in ((1, 0), (6, 0.4), (14, math.pi), (22, 2 * math.pi - 0.2), (30, 2 * math.pi)):
        key(arm, "root", f, rot=E("root", Z, a)); key(arm, "tail", f, rot=E("tail", Z, -0.6 * math.sin(a / 2)))
def jump():   # 웅크림 → 높이 도약 → 착지 내려찍기(24) → 출렁
    for f, h, sq, lean in ((1, 0, 0, 0), (8, 0, 1, 0.15), (14, 1.6, -0.6, -0.15), (19, 1.9, -0.3, 0.1), (24, 0, 1.2, 0.05), (30, 0, -0.3, 0), (40, 0, 0, 0)):
        key(arm, "root", f, loc=Vector((0, 0, h * K)))
        key(arm, "body", f, scale=Vector((1 + 0.12 * sq, 1 + 0.12 * sq, 1 - 0.18 * sq)), rot=E("body", X, lean)); fl(f, -1.0 if h > 0 else 0.3 * sq)
        key(arm, "tail", f, rot=E("tail", X, -0.5 if h > 0 else 0.2 * sq))
def beam():   # 몸을 세우고(머리 위로) 입을 크게 벌려 고래빔 발사, 타격 20
    for f, up, jaw, sh in ((1, 0, 0, 0), (10, -0.45, 0.1, 0), (16, -0.5, 0.75, 0), (20, -0.42, 0.85, 1), (32, -0.42, 0.85, 1), (42, -0.4, 0.5, 0), (50, 0, 0, 0)):
        key(arm, "body", f, rot=E("body", X, up)); key(arm, "jaw", f, rot=E("jaw", X, jaw)); fl(f, -0.6 * (up < 0)); key(arm, "tail", f, rot=E("tail", X, 0.5 * (up < 0)))
        key(arm, "root", f, loc=Vector((0, 0.03 * K * sh * (f % 2), 0)))
def beam_hold():   # 빔 유지: 입 벌린 채 맥동(맥동 정점 20)
    for f, k in ((1, 0), (10, 0.5), (20, 1), (24, 0.6)):
        key(arm, "body", f, rot=E("body", X, -0.42 - 0.04 * k)); key(arm, "jaw", f, rot=E("jaw", X, 0.8 + 0.1 * k)); fl(f, -0.6)
        key(arm, "root", f, loc=Vector((0, (0.06 * k) * K, 0)))
def spout_attack():   # 꾹 눌렀다가 숨구멍으로 물기둥(16)
    for f, sq, sp in ((1, 0, 0), (10, 1, 0.2), (16, -0.6, 2.4), (24, -0.2, 1.6), (36, 0, 0.5)):
        key(arm, "body", f, scale=Vector((1 + 0.1 * sq, 1 + 0.1 * sq, 1 - 0.15 * sq))); key(arm, "spout", f, scale=Vector((1, 1, 1)) * (0.7 + sp)); fl(f, 0.4 * sq)
def guard():   # 지느러미를 앞으로 모으고 웅크림
    for f, v in ((1, 0), (6, 1), (12, 0.95)):
        key(arm, "flipper.L", f, rot=E("flipper.L", Z, 0.9 * v)); key(arm, "flipper.R", f, rot=E("flipper.R", Z, -0.9 * v)); key(arm, "body", f, rot=E("body", X, 0.15 * v), scale=Vector((1, 1, 1 - 0.08 * v)))
def dodge():   # 옆으로 데굴
    for f, v in ((1, 0), (5, 1), (10, 0.6), (14, 0)):
        key(arm, "body", f, rot=E("body", Y, 0.6 * v)); key(arm, "root", f, loc=Vector((0, 0, 0.15 * K * v)))
def hit():
    for f, v in ((1, 0), (4, 1), (10, 0.4), (18, 0)):
        key(arm, "body", f, rot=E("body", X, 0.2 * v), scale=Vector((1 - 0.05 * v, 1 - 0.05 * v, 1 + 0.06 * v))); fl(f, -0.5 * v); key(arm, "spout", f, scale=Vector((1, 1, 1)) * (0.7 + 1.0 * v))
def death():   # 옆으로 발라당 → 배를 보이며 뒤집힘
    for f, v in ((1, 0), (12, 0.5), (26, 1), (40, 1)):
        key(arm, "root", f, rot=E("root", Y, math.pi * 0.95 * v), loc=Vector((0, 0, 0.6 * K * math.sin(math.pi * min(v, 1) * 0.5) * (1 - v) + 0.95 * K * v)))
        fl(f, 0.8 * v, -0.8 * v); key(arm, "spout", f, scale=Vector((1, 1, 1)) * max(0.01, 1 - v))

A = {}
A["Idle"] = anim("Idle", 61, idle); A["Walk"] = anim("Walk", 31, walk(1)); A["Run"] = anim("Run", 21, walk(1.6))
A["Windup"] = anim("Windup", 20, windup); A["Attack1"] = anim("Attack1", 24, ram, False); A["Attack2"] = anim("Attack2", 24, flipper_slap, False)
A["Attack3"] = anim("Attack3", 30, tail_spin, False); A["Jump"] = anim("Jump", 40, jump, False); A["Beam"] = anim("Beam", 50, beam, False)
A["BeamHold"] = anim("BeamHold", 24, beam_hold); A["Spout"] = anim("Spout", 36, spout_attack, False)
A["Guard"] = anim("Guard", 12, guard); A["Dodge"] = anim("Dodge", 14, dodge, False); A["Hit"] = anim("Hit", 18, hit, False); A["Death"] = anim("Death", 40, death, False)

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = T(0, 0.9, -0.2)
    preview(os.path.join(PRE, "whale_front.png"), T(1.2, 1.6, 5.6), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "whale_three_quarter.png"), T(4.4, 2.6, 3.6), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "whale_beam.png"), T(4.6, 1.8, 3.2), c, arm_ob=arm, action=A["Beam"], frame=22)
    preview(os.path.join(PRE, "whale_jump.png"), T(7.5, 2.4, 5.0), T(0, 1.8, -0.4), arm_ob=arm, action=A["Jump"], frame=17)

glb, fbx = export_all("Goraegorae", OUT, [arm, whale])
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "Goraegorae.fbx"))
print("EXPORTED", glb, fbx, "verts:", len(whale.data.vertices))
