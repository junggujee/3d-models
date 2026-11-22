"""지상괴물형 적: 고래고래(지상 최대의 끼룩이). 모델 + 뼈대 + 애니메이션 → exports/ 와 unity/Assets/Models/.
특징(사용자 스케치 기준): 길쭉한 몸 + 볼록한 이마에서 앞으로 튀어나온 주둥이(끝에 분홍 입), 위 벨벳 파랑 · 배 흰색 · 등 솔기,
      앞쪽 위 등지느러미, 아래로 내려 땅을 짚는 가슴지느러미, 길게 가늘어지는 꼬리 + 갈라진 꼬리 지느러미,
      주둥이 뿌리 옆 까만 점 눈 · 분홍 볼 · 주둥이를 따라 웃는 입선, 숨구멍 물방울(끼룩이), 벌어지는 아래턱(고래빔).
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

# ── 몸 치수(three.js 좌표: y 위, z 앞) — 길쭉한 몸 + 볼록한 이마 + 앞으로 튀어나온 주둥이(스케치 기준)
BC, BR = (0.0, 0.85, 0.0), (0.78, 0.72, 1.55)          # 몸통 타원(앞뒤로 김)
HC, HR = (0.0, 0.9, 1.05), (0.66, 0.62, 0.72)          # 이마(멜론): 몸 앞쪽 위로 볼록
SC, SR = (0.0, 0.6, 1.95), (0.21, 0.16, 0.62)           # 위 주둥이
JC, JR = (0.0, 0.46, 1.88), (0.18, 0.1, 0.58)           # 아래턱(벌어짐)
def surf(C, R, phi, th, off=0.0):
    """타원 C,R 표면의 점과 법선(Blender). phi = 옆 각도(0 = 정면), th = 위아래 각도."""
    d = Vector((math.cos(th) * math.sin(phi), math.sin(th), math.cos(th) * math.cos(phi)))
    p = Vector(C) + Vector((d.x * R[0], d.y * R[1], d.z * R[2])); n = Vector((d.x / R[0], d.y / R[1], d.z / R[2])).normalized()
    return T(*(p + n * off)), Vector((n.x, -n.z, n.y)).normalized()
def frame(n): r = Vector((0, 0, 1)).cross(n).normalized(); return r, n.cross(r)

BONES = [
    ("root", None, T(0, 0, 0), T(0, 0.3, 0)), ("body", "root", T(0, 0.55, -0.3), T(0, 1.4, -0.3)),
    ("jaw", "body", T(0, 0.48, 1.35), T(0, 0.42, 2.5)),
    ("flipper.L", "body", T(0.62, 0.45, 0.75), T(0.85, 0.0, 0.8)), ("flipper.R", "body", T(-0.62, 0.45, 0.75), T(-0.85, 0.0, 0.8)),
    ("tail", "body", T(0, 0.85, -1.35), T(0, 1.05, -2.6)), ("fluke", "tail", T(0, 1.05, -2.6), T(0, 1.15, -3.2)),
    ("spout", "body", T(0, 1.5, 0.95), T(0, 1.85, 0.95)),
]
arm = make_armature("WhaleRig", BONES)
parts = []
def add(name, bm, bone): parts.append(to_object(name, bm, MATS, bone))
def two_tone(bm, line_y):
    """면 높이로 파랑/흰 배 나누기 + 등 가운데 솔기."""
    for f in bm.faces:
        c = f.calc_center_median(); x, y = c.x / K, c.z / K
        f.material_index = I_WHITE if y < line_y else (I_SEAM if abs(x) < 0.022 and y > 1.25 else I_BLUE)
    return bm

add("body", two_tone(ellipsoid(BC, BR, seg=96, rings=60, fuzz=0.005, seed=3), 0.38), "body")
add("melon", two_tone(ellipsoid(HC, HR, seg=72, rings=48, fuzz=0.005, seed=4), 0.42), "body")
add("snout", ellipsoid(SC, SR, seg=48, rings=28, fuzz=0.003, seed=5, mat_idx=I_BLUE), "body")
add("jaw", two_tone(ellipsoid(JC, JR, seg=48, rings=24, fuzz=0.003, seed=6), 0.43), "jaw")
add("mouthInside", ellipsoid((0, 0.52, 1.9), (0.16, 0.07, 0.52), mat_idx=I_MOUTH), "body")       # 주둥이 사이에 숨어 있다가 턱을 벌리면 보임
add("lip", ellipsoid((0, 0.45, 2.36), (0.12, 0.06, 0.12), fuzz=0.002, seed=7, mat_idx=I_PINK), "jaw")   # 주둥이 끝 분홍 입(스케치의 분홍 부분)
# 가슴지느러미: 몸 아래로 내려 땅을 짚음 · 등지느러미: 몸 앞쪽 위 · 꼬리: 길게 가늘어짐 + 갈라진 꼬리 지느러미
for s, side in ((1, "L"), (-1, "R")):
    add(f"flipper.{side}", ellipsoid((0.66 * s, 0.22, 0.85), (0.14, 0.36, 0.26), rot=(0.25, 0, 0.3 * s), fuzz=0.004, seed=8 + s, seg=32, rings=18), f"flipper.{side}")
add("dorsal", ellipsoid((0, 1.6, 0.35), (0.08, 0.34, 0.36), rot=(-0.45, 0, 0), fuzz=0.003, seed=11, seg=28, rings=18), "body")
pts = [T(0, 0.84 + 0.2 * t * t, -0.7 - 2.05 * t) for t in [i / 12 for i in range(13)]]   # 몸 안쪽 깊은 곳에서 시작 → 이음매 없음
b2, _ = tube(pts, lambda t: (0.6 - 0.48 * t ** 0.8) * K, sides=24, mat_idx=I_BLUE); add("tail", b2, "tail")
for s in (1, -1):
    add("fluke", ellipsoid((0.34 * s, 1.18, -2.95), (0.5, 0.07, 0.22), rot=(0.25, 0.55 * s, 0.25 * s), fuzz=0.003, seed=12 + s, seg=28, rings=14), "fluke")
# 얼굴: 주둥이 뿌리 옆의 까만 점 눈 · 분홍 볼 · 주둥이 옆을 따라 살짝 웃는 입선
for s in (1, -1):
    pe, ne = surf(HC, HR, 0.78 * s, 0.0, 0.004); add("eye", oriented_disc(pe, ne, 0.07 * K, 0.08 * K, 0.03 * K, I_DARK), "body")
    r_, u_ = frame(ne); add("eyeHi", oriented_disc(pe + u_ * 0.025 * K + r_ * (-0.02 * s) * K + ne * 0.02 * K, ne, 0.02 * K, 0.02 * K, 0.012 * K, I_WHITE), "body")
    pc, nc = surf(HC, HR, 0.62 * s, -0.32, 0.004); add("cheek", oriented_disc(pc, nc, 0.11 * K, 0.065 * K, 0.015 * K, I_PINK), "body")
    line = []
    for k in range(17):
        z = 1.42 + 0.95 * k / 16; dz = (z - SC[2]) / SR[2]; y = 0.5 + 0.06 * (1 - k / 16) ** 2      # 뒤끝이 살짝 올라가는 미소
        x = SR[0] * math.sqrt(max(0.02, 1 - dz * dz - ((y - SC[1]) / SR[1]) ** 2)) + 0.012
        line.append(T(x * s, y, z))
    b2, _ = tube(line, lambda t: 0.012 * K, sides=6, mat_idx=I_DARK); add("smile", b2, "body")
# 숨구멍 + 솟는 물방울(끼룩이)
ph, nh = surf(HC, HR, 0, 1.2, 0.002); add("blowhole", oriented_disc(ph, nh, 0.08 * K, 0.035 * K, 0.015 * K, I_DARK), "body")
for dx, h, r in ((0, 0.28, 0.1), (0.12, 0.2, 0.07), (-0.12, 0.2, 0.07), (0.05, 0.42, 0.06), (-0.06, 0.38, 0.055)):
    add("drop", ellipsoid((dx, 1.5 + h, 0.95), (r, r * 1.25, r), mat_idx=I_SPOUT, seg=16, rings=10), "spout")

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
    os.makedirs(PRE, exist_ok=True); c = T(0, 0.85, -0.3)
    preview(os.path.join(PRE, "whale_front.png"), T(1.6, 1.4, 6.2), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "whale_three_quarter.png"), T(6.6, 2.0, 1.2), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "whale_beam.png"), T(5.6, 1.6, 4.2), c, arm_ob=arm, action=A["Beam"], frame=22)
    preview(os.path.join(PRE, "whale_jump.png"), T(7.5, 2.4, 5.0), T(0, 1.8, -0.4), arm_ob=arm, action=A["Jump"], frame=17)

glb, fbx = export_all("Goraegorae", OUT, [arm, whale])
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "Goraegorae.fbx"))
print("EXPORTED", glb, fbx, "verts:", len(whale.data.vertices))
