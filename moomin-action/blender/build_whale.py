"""지상괴물형 적: 고래고래(지상 최대의 끼룩이). 모델 + 뼈대 + 애니메이션 → exports/ 와 unity/Assets/Models/.
특징(사용자 스케치 기준): 길쭉한 몸 + 볼록한 이마에서 앞으로 튀어나온 주둥이(끝에 분홍 입), 위 벨벳 파랑 · 배 흰색 · 등 솔기,
      앞쪽 위 등지느러미, 아래로 내려 땅을 짚는 가슴지느러미, 길게 가늘어지는 꼬리 + 갈라진 꼬리 지느러미,
      주둥이 뿌리 옆 까만 점 눈 · 분홍 볼 · 주둥이를 따라 웃는 입선, 숨구멍 물방울(끼룩이), 벌어지는 아래턱(고래빔).
애니메이션(30fps, 척추 사슬 물결): Idle Walk Run Windup Attack1(고래박치기, 타격 11) Attack2(지느러미 치기, 11) Attack3(꼬리 휘두르기 한 바퀴, 14)
                  Jump(점프 내려찍기, 착지 24) BeamCharge(기 모으기 27) Beam(발사 8) BeamHold(유지 루프 18) BeamEnd(마무리 24) Spout(물기둥, 16)
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

# 척추 사슬(뒤 → 앞): fluke ← tail2 ← tail1 ← body → head. 몸 피부는 앞뒤 위치(z)에 따라 이웃한 두 뼈에 부드럽게 나눠 붙임(SKIN)
BONES = [
    ("root", None, T(0, 0, 0), T(0, 0.3, 0)),
    ("body", "root", T(0, 0.85, -0.5), T(0, 0.85, 0.45)),
    ("head", "body", T(0, 0.85, 0.45), T(0, 0.8, 1.5)),
    ("jaw", "head", T(0, 0.48, 1.35), T(0, 0.42, 2.45)),
    ("spout", "head", T(0, 1.5, 0.95), T(0, 1.85, 0.95)),
    ("flipper.L", "head", T(0.62, 0.45, 0.75), T(0.85, 0.0, 0.8)), ("flipper.R", "head", T(-0.62, 0.45, 0.75), T(-0.85, 0.0, 0.8)),
    ("tail1", "body", T(0, 0.85, -0.5), T(0, 0.92, -1.5)), ("tail2", "tail1", T(0, 0.92, -1.5), T(0, 1.0, -2.45)), ("fluke", "tail2", T(0, 1.0, -2.45), T(0, 1.12, -3.1)),
]
CHAIN = ["fluke", "tail2", "tail1", "body", "head"]; JOINTS = [-2.45, -1.5, -0.5, 0.45]; BLEND = [0.25, 0.4, 0.45, 0.4]
arm = make_armature("WhaleRig", BONES)
parts = []
def add(name, bm, bone): parts.append(to_object(name, bm, MATS, bone))
def two_tone(bm, line_y):
    """면 높이로 파랑/흰 배 나누기 + 등 가운데 솔기."""
    for f in bm.faces:
        c = f.calc_center_median(); x, y = c.x / K, c.z / K
        f.material_index = I_WHITE if y < line_y else (I_SEAM if abs(x) < 0.022 and y > 1.25 else I_BLUE)
    return bm

add("body", two_tone(ellipsoid(BC, BR, seg=96, rings=60, fuzz=0.005, seed=3), 0.38), "SKIN")
add("melon", two_tone(ellipsoid(HC, HR, seg=72, rings=48, fuzz=0.005, seed=4), 0.42), "SKIN")
add("snout", ellipsoid(SC, SR, seg=48, rings=28, fuzz=0.003, seed=5, mat_idx=I_BLUE), "SKIN")
add("jaw", two_tone(ellipsoid(JC, JR, seg=48, rings=24, fuzz=0.003, seed=6), 0.43), "jaw")
add("mouthInside", ellipsoid((0, 0.52, 1.9), (0.16, 0.07, 0.52), mat_idx=I_MOUTH), "head")       # 주둥이 사이에 숨어 있다가 턱을 벌리면 보임
add("lip", ellipsoid((0, 0.45, 2.36), (0.12, 0.06, 0.12), fuzz=0.002, seed=7, mat_idx=I_PINK), "jaw")   # 주둥이 끝 분홍 입(스케치의 분홍 부분)
# 가슴지느러미: 몸 아래로 내려 땅을 짚음 · 등지느러미: 몸 앞쪽 위 · 꼬리: 길게 가늘어짐 + 갈라진 꼬리 지느러미
for s, side in ((1, "L"), (-1, "R")):
    add(f"flipper.{side}", ellipsoid((0.66 * s, 0.22, 0.85), (0.14, 0.36, 0.26), rot=(0.25, 0, 0.3 * s), fuzz=0.004, seed=8 + s, seg=32, rings=18), f"flipper.{side}")
add("dorsal", ellipsoid((0, 1.6, 0.35), (0.08, 0.34, 0.36), rot=(-0.45, 0, 0), fuzz=0.003, seed=11, seg=28, rings=18), "SKIN")
pts = [T(0, 0.84 + 0.2 * t * t, -0.7 - 2.05 * t) for t in [i / 12 for i in range(13)]]   # 몸 안쪽 깊은 곳에서 시작 → 이음매 없음
b2, _ = tube(pts, lambda t: (0.6 - 0.48 * t ** 0.8) * K, sides=24, mat_idx=I_BLUE); add("tail", b2, "SKIN")
for s in (1, -1):
    add("fluke", ellipsoid((0.34 * s, 1.18, -2.95), (0.5, 0.07, 0.22), rot=(0.25, 0.55 * s, 0.25 * s), fuzz=0.003, seed=12 + s, seg=28, rings=14), "SKIN")
# 얼굴: 주둥이 뿌리 옆의 까만 점 눈 · 분홍 볼 · 주둥이 옆을 따라 살짝 웃는 입선
for s in (1, -1):
    pe, ne = surf(HC, HR, 0.78 * s, 0.0, 0.004); add("eye", oriented_disc(pe, ne, 0.07 * K, 0.08 * K, 0.03 * K, I_DARK), "SKIN")
    r_, u_ = frame(ne); add("eyeHi", oriented_disc(pe + u_ * 0.025 * K + r_ * (-0.02 * s) * K + ne * 0.02 * K, ne, 0.02 * K, 0.02 * K, 0.012 * K, I_WHITE), "SKIN")
    pc, nc = surf(HC, HR, 0.62 * s, -0.32, 0.004); add("cheek", oriented_disc(pc, nc, 0.11 * K, 0.065 * K, 0.015 * K, I_PINK), "SKIN")
    line = []
    for k in range(17):
        z = 1.42 + 0.95 * k / 16; dz = (z - SC[2]) / SR[2]; y = 0.5 + 0.06 * (1 - k / 16) ** 2      # 뒤끝이 살짝 올라가는 미소
        x = SR[0] * math.sqrt(max(0.02, 1 - dz * dz - ((y - SC[1]) / SR[1]) ** 2)) + 0.012
        line.append(T(x * s, y, z))
    b2, _ = tube(line, lambda t: 0.012 * K, sides=6, mat_idx=I_DARK); add("smile", b2, "SKIN")
# 숨구멍 + 솟는 물방울(끼룩이)
ph, nh = surf(HC, HR, 0, 1.2, 0.002); add("blowhole", oriented_disc(ph, nh, 0.08 * K, 0.035 * K, 0.015 * K, I_DARK), "SKIN")
for dx, h, r in ((0, 0.28, 0.1), (0.12, 0.2, 0.07), (-0.12, 0.2, 0.07), (0.05, 0.42, 0.06), (-0.06, 0.38, 0.055)):
    add("drop", ellipsoid((dx, 1.5 + h, 0.95), (r, r * 1.25, r), mat_idx=I_SPOUT, seg=16, rings=10), "spout")

whale = join(parts, "Goraegorae")
# 부드러운 피부: z(앞뒤)로 이웃한 척추 뼈에 무게를 나눔 → 휘어도 이음매 없이 한 몸으로 이어짐
def smooth(a, b, x): t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
skin = whale.vertex_groups["SKIN"]; vg = {b: (whale.vertex_groups.get(b) or whale.vertex_groups.new(name=b)) for b in CHAIN}
for v in whale.data.vertices:
    if not any(g.group == skin.index for g in v.groups): continue
    z = -v.co.y / K; t = [smooth(j - w, j + w, z) for j, w in zip(JOINTS, BLEND)]
    for i, b in enumerate(CHAIN):
        w = (1.0 if i == 0 else t[i - 1]) * (1.0 if i == len(CHAIN) - 1 else 1 - t[i])
        if w > 1e-4: vg[b].add([v.index], w, "REPLACE")
whale.vertex_groups.remove(skin); bind(whale, arm)

# ── 애니메이션: 모든 키에서 사슬 전체를 함께 키잉 + 뒤로 갈수록 늦게 따라오는 물결(오버래핑 액션) → 몸이 끊김 없이 이어짐
X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)
def R(b, pitch=0.0, yaw=0.0, roll=0.0):
    """월드 축 기준 회전(pitch = X: 머리 쪽은 음수가 들림 · 꼬리 쪽은 양수가 들림, yaw = Z, roll = Y)을 로컬 오일러로."""
    M = arm.data.bones[b].matrix_local.to_3x3()
    W = Matrix.Rotation(yaw, 3, Z) @ Matrix.Rotation(pitch, 3, X) @ Matrix.Rotation(roll, 3, Y)
    return (M.inverted() @ W @ M).to_euler()
def anim(name, length, build, loop=True):
    act = new_action(arm, name, length); build(); finish_action(arm, act, loop=loop); act.frame_start, act.frame_end = 1, length; return act
def pose(f, head=(0, 0), body=(0, 0), t1=(0, 0), t2=(0, 0), fk=(0, 0), jaw=0.0, fl=(0, 0, 0, 0), sp=0.5, loc=(0, 0, 0), rot=(0, 0, 0), sq=0.0, roll=0.0):
    """한 프레임 자세. head/body/t1/t2/fk = (pitch, yaw), fl = (왼 노 젓기, 왼 벌리기, 오른 노 젓기, 오른 벌리기), sq = 몸 눌림(+) / 늘어남(-), sp = 물방울 크기."""
    for b, (p, y) in (("head", head), ("body", body), ("tail1", t1), ("tail2", t2), ("fluke", fk)):
        key(arm, b, f, rot=R(b, p, y, roll if b == "body" else 0.0), scale=Vector((1 + 0.08 * sq, 1 - 0.05 * sq, 1 - 0.1 * sq)) if b == "body" else None)
    key(arm, "jaw", f, rot=R("jaw", jaw))
    key(arm, "flipper.L", f, rot=R("flipper.L", fl[0], 0, -fl[1])); key(arm, "flipper.R", f, rot=R("flipper.R", fl[2], 0, fl[3]))
    key(arm, "spout", f, scale=Vector((1, 1, 1)) * (0.7 + 0.6 * sp))
    key(arm, "root", f, loc=Vector((loc[0] * K, loc[1] * K, loc[2] * K)), rot=R("root", rot[0], rot[1], rot[2]))
def wave(u, amp, lag=0.75, axis=0):
    """머리(0)→꼬리지느러미(4) 순서로 lag 만큼 늦는 물결. amp = 뼈별 진폭 5개. axis 0 = pitch, 1 = yaw."""
    out = []
    for i, a in enumerate(amp):
        v = a * math.sin(u - i * lag); out.append((v, 0.0) if axis == 0 else (0.0, v))
    return out
def frames(n, step): return list(range(1, n, step)) + [n]

def idle():   # 숨 쉬기 + 꼬리가 천천히 살랑(뒤로 갈수록 늦고 크게) + 물방울 퐁퐁
    for f in frames(61, 5):
        u = (f - 1) / 60 * 2 * math.pi; h, b, a, c, d = wave(u, (0.02, 0.01, 0.06, 0.11, 0.18), axis=1)
        pose(f, head=(-0.02 * math.sin(u), h[1]), body=(0, b[1]), t1=(0.02 * math.sin(u - 1), a[1]), t2=(0.04 * math.sin(u - 1.6), c[1]), fk=(0.08 * math.sin(u - 2.2), d[1]),
             fl=(0.12 * math.sin(u), 0.05, 0.12 * math.sin(u + 0.5), 0.05), sp=0.5 + 0.5 * math.sin(2 * u), sq=0.25 * math.sin(u), jaw=0.03 + 0.02 * math.sin(u))
def walk(speed):   # 배로 물결치며 앞으로: 앞에서 뒤로 흐르는 위아래 물결 + 콩콩 튐 + 지느러미로 번갈아 짚기
    def b():
        L = 31 if speed < 1.3 else 21
        for f in frames(L, 3):
            u = (f - 1) / (L - 1) * 2 * math.pi; s_ = speed
            h, bd, a, c, d = wave(u, (0.07 * s_, 0.05 * s_, 0.12 * s_, 0.2 * s_, 0.32 * s_), lag=0.8)
            pose(f, head=(h[0], 0.04 * s_ * math.sin(u)), body=bd, t1=a, t2=c, fk=d, loc=(0, 0, 0.09 * s_ * (1 - math.cos(2 * u)) / 2),
                 fl=(0.7 * s_ * math.sin(u), 0.15, -0.7 * s_ * math.sin(u), 0.15), sq=-0.35 * math.cos(2 * u), sp=0.5)
    return b
def windup():   # 예고: 몸을 뒤로 당기며 머리·꼬리를 들어 올리고 부르르
    for f, v in ((1, 0), (5, 0.6), (9, 1), (13, 0.95), (17, 1), (20, 1)):
        j = 0.03 * math.sin(f * 2.1) * (f > 9)
        pose(f, head=(-0.18 * v, j), body=(-0.08 * v, 0), t1=(0.15 * v, 0), t2=(0.25 * v, -j), fk=(0.35 * v, 0), loc=(0, 0.12 * v, 0), fl=(-0.4 * v, 0.4 * v, -0.4 * v, 0.4 * v), sq=0.3 * v, sp=0.6)
def ram():   # 고래박치기: 움츠림(6) → 머리로 들이받음(11) → 꼬리가 늦게 휘청(14) → 복귀
    for f, hp, bp, tw, push, sq in ((1, 0, 0, 0, 0, 0), (4, -0.12, -0.05, 0.1, 0.12, 0.3), (7, -0.2, -0.08, 0.18, 0.2, 0.45),
                                    (11, 0.22, 0.08, -0.1, -0.45, -0.4), (14, 0.16, 0.05, -0.32, -0.38, -0.2), (18, 0.06, 0.02, 0.2, -0.15, 0.1), (24, 0, 0, 0, 0, 0)):
        pose(f, head=(hp, 0), body=(bp, 0), t1=(tw * 0.5, 0), t2=(tw, 0), fk=(tw * 1.4, 0), loc=(0, push, 0), sq=sq, fl=(0.7 * (push < 0), 0.2, 0.7 * (push < 0), 0.2), jaw=0.05)
def flipper_slap():   # 오른 지느러미를 들어(6) 후려침(11), 몸은 비틀고 꼬리는 반대로 늦게
    for f, a, tw in ((1, 0, 0), (4, 0.5, 0.15), (7, 1.0, 0.28), (11, -0.5, -0.3), (14, -0.35, -0.22), (18, -0.1, 0.1), (24, 0, 0)):
        pose(f, head=(0, -0.18 * tw), body=(0, 0), roll=-0.4 * tw, t1=(0, 0.2 * tw), t2=(0, 0.35 * tw), fk=(0, 0.5 * tw), fl=(0.1, 0.1, 0.2, 1.2 * a), sq=0.15 * abs(a))
def tail_spin():   # 한 바퀴 돌며 꼬리 휘두르기: 돌 때 꼬리가 뒤로 처지고(늦음) 14에서 휙 펴짐
    for f, a, lagv in ((1, 0, 0), (4, -0.25, -0.2), (8, 1.2, 0.45), (12, 2.6, 0.55), (14, 3.3, 0.2), (17, 4.4, -0.25), (22, 5.9, -0.15), (26, 6.25, 0.05), (30, 2 * math.pi, 0)):
        pose(f, rot=(0, a, 0), head=(0, 0.1 * lagv), body=(0, 0), t1=(0, -0.4 * lagv), t2=(0, -0.7 * lagv), fk=(0, -1.0 * lagv), fl=(0, 0.6 * abs(lagv), 0, 0.6 * abs(lagv)), sq=-0.2 * abs(lagv))
def jump():   # 웅크림(8) → 도약(14, 바나나 휨) → 정점(17, 머리 숙이고 꼬리 듦) → 착지 내려찍기(24) → 출렁이며 가라앉음
    K_ = ((1, 0, 0, 0, 0, 0), (5, 0, 0.12, 0.1, 0.6, 0), (8, 0, 0.2, 0.18, 0.9, 0), (11, 1.0, -0.2, -0.2, -0.6, 0), (14, 1.7, -0.15, -0.3, -0.5, 0),
          (17, 2.0, 0.15, 0.25, -0.2, 0), (21, 1.0, 0.25, 0.4, -0.3, 0), (24, 0, 0.18, 0.25, 1.2, 0), (27, 0, -0.08, -0.2, -0.3, 0.6), (31, 0, 0.05, 0.12, 0.2, 0.3), (35, 0, -0.02, -0.05, -0.05, 0.1), (40, 0, 0, 0, 0, 0))
    for f, h, hp, tp, sq, wob in K_:
        pose(f, loc=(0, 0, h), head=(hp, 0), body=(hp * 0.3, 0), t1=(tp * 0.5, 0), t2=(tp, 0), fk=(tp * 1.4 + 0.2 * wob, 0), sq=sq,
             fl=(-0.9 if h > 0 else 0.3 * sq, 0.9 if h > 0 else 0.5 * max(0, sq), -0.9 if h > 0 else 0.3 * sq, 0.9 if h > 0 else 0.5 * max(0, sq)), sp=0.4 + 0.6 * (f >= 24 and f < 30), jaw=0.1 if h > 0 else 0.03)
HOLD = dict(head=(-0.3, 0), body=(-0.22, 0), t1=(0.18, 0), t2=(0.28, 0), fk=(0.35, 0))   # 빔 자세: 머리를 들고 꼬리로 버팀
def beam_charge():   # 기 모으기(Windup 대신, 0.9초): 천천히 몸을 세우고 입을 조금씩 열며 부풀고 점점 세게 떨림
    for f in frames(27, 3):
        v = min(1.0, (f - 1) / 12); k = max(0.0, (f - 12) / 15); jit = 0.012 * k * (1 if f % 2 else -1) * 3
        pose(f, head=(HOLD["head"][0] * v, jit), body=(HOLD["body"][0] * v, 0), t1=(HOLD["t1"][0] * v, -jit), t2=(HOLD["t2"][0] * v, jit), fk=(HOLD["fk"][0] * v, 0),
             jaw=0.08 + 0.3 * k, sq=-0.3 * k, fl=(-0.3 * v, 0.7 * v, -0.3 * v, 0.7 * v), sp=0.5 + 0.8 * k, loc=(0.01 * k * (1 if f % 2 else -1), 0.05 * v, 0))
def beam_fire():   # 발사(8): 턱이 탁 벌어지고 반동으로 몸이 뒤로 밀림 → 유지 자세로
    for f, jaw, rec, sq in ((1, 0.38, 0, -0.3), (4, 0.75, 0.06, -0.1), (8, 0.95, 0.22, 0.35), (12, 0.9, 0.12, 0.15), (20, 0.88, 0.05, 0.05)):
        pose(f, head=(HOLD["head"][0] - 0.12 * rec / 0.22, 0), body=(HOLD["body"][0], 0), t1=HOLD["t1"], t2=(HOLD["t2"][0] + 0.1 * rec, 0), fk=(HOLD["fk"][0] + 0.25 * rec, 0),
             jaw=jaw, loc=(0, rec, 0), sq=sq, fl=(-0.3, 0.8, -0.3, 0.8), sp=1.4)
def beam_hold():   # 유지(루프): 입을 벌린 채 맥동, 몸이 떨리고 꼬리가 버티며 출렁
    for f in frames(18, 2):
        u = (f - 1) / 17 * 2 * math.pi; jit = 0.02 * math.sin(u * 3)
        pose(f, head=(HOLD["head"][0] + 0.03 * math.sin(u), jit), body=HOLD["body"], t1=(HOLD["t1"][0], -jit), t2=(HOLD["t2"][0] + 0.05 * math.sin(u - 1), 0.08 * math.sin(u - 1)),
             fk=(HOLD["fk"][0] + 0.08 * math.sin(u - 1.8), 0.14 * math.sin(u - 1.8)), jaw=0.88 + 0.06 * math.sin(u * 2), loc=(0.012 * math.sin(u * 5), 0.08 + 0.02 * math.sin(u * 2), 0),
             sq=0.12 * math.sin(u * 2), fl=(-0.3, 0.8, -0.3, 0.8), sp=1.2 + 0.2 * math.sin(u * 2))
def beam_end():   # 마무리: 입을 닫고 숨을 내쉬며 몸을 내림(꼬리가 늦게 내려옴)
    for f, v, tv in ((1, 1, 1), (6, 0.8, 1), (12, 0.35, 0.7), (18, 0.05, 0.3), (24, 0, 0)):
        pose(f, head=(HOLD["head"][0] * v, 0), body=(HOLD["body"][0] * v, 0), t1=(HOLD["t1"][0] * tv, 0), t2=(HOLD["t2"][0] * tv, 0), fk=(HOLD["fk"][0] * tv, 0),
             jaw=0.88 * v * v, sq=0.25 * (1 - v) * v * 4 * 0.5, fl=(-0.3 * v, 0.8 * v, -0.3 * v, 0.8 * v), sp=0.4 + 0.6 * v)
def spout_attack():   # 꾹 웅크렸다가(10) 몸을 활처럼 펴며 물기둥(16) → 출렁
    for f, hp, tp, sq, sp in ((1, 0, 0, 0, 0.5), (6, 0.1, 0.12, 0.6, 0.3), (10, 0.16, 0.2, 0.95, 0.2), (16, -0.25, -0.25, -0.6, 2.6), (20, -0.18, -0.12, -0.3, 2.2),
                              (26, 0.05, 0.1, 0.2, 1.2), (31, -0.02, -0.04, -0.05, 0.7), (36, 0, 0, 0, 0.5)):
        pose(f, head=(hp, 0), body=(hp * 0.4, 0), t1=(tp * 0.6, 0), t2=(tp, 0), fk=(tp * 1.3, 0), sq=sq, sp=sp, fl=(0.4 * sq, 0.3, 0.4 * sq, 0.3))
def guard():   # 몸을 C 자로 말아 머리와 꼬리를 앞으로 모음, 지느러미로 가림
    for f, v in ((1, 0), (4, 0.6), (8, 1), (12, 0.95)):
        pose(f, head=(0.22 * v, 0), body=(0.08 * v, 0), t1=(-0.2 * v, 0.15 * v), t2=(-0.3 * v, 0.3 * v), fk=(-0.35 * v, 0.45 * v), fl=(-0.9 * v, -0.2 * v, -0.9 * v, -0.2 * v), sq=0.3 * v)
def dodge():   # 옆으로 데굴(몸 전체를 굴리고 꼬리가 늦게 따라옴)
    for f, v in ((1, 0), (4, 0.6), (7, 1), (10, 0.55), (14, 0)):
        pose(f, roll=0.9 * v, loc=(0, 0, 0.18 * math.sin(math.pi * v)), head=(0, 0.1 * v), t1=(0, -0.15 * v), t2=(0, -0.3 * v), fk=(0, -0.45 * v), sq=-0.2 * v)
def hit():   # 머리가 젖혀지고 그 충격이 꼬리까지 물결로 전해짐
    for f in frames(18, 2):
        e = math.exp(-(f - 1) / 6); u = (f - 1) / 17 * 2.4 * math.pi
        h, b, a, c, d = wave(u, (0.3 * e, 0.12 * e, 0.22 * e, 0.32 * e, 0.42 * e), lag=0.9)
        pose(f, head=(-abs(h[0]), 0), body=b, t1=a, t2=c, fk=d, sq=-0.4 * e, sp=0.5 + 1.2 * e, fl=(-0.6 * e, 0.4 * e, -0.6 * e, 0.4 * e), loc=(0, 0.12 * e, 0))
def death():   # 크게 출렁이다 옆으로 뒤집혀 배를 보임
    for f in frames(40, 3):
        v = min(1.0, max(0.0, (f - 10) / 18)); e = math.exp(-(f - 1) / 10); u = (f - 1) / 12 * 2 * math.pi
        h, b, a, c, d = wave(u, (0.2 * e, 0.1 * e, 0.25 * e, 0.35 * e, 0.45 * e))
        pose(f, roll=math.pi * 0.95 * v * v * (3 - 2 * v), loc=(0, 0, 0.45 * math.sin(math.pi * v) * 0.5 + 0.55 * v), head=h, body=b, t1=a, t2=c, fk=d,
             fl=(0.6 * v, 0.6 * v, 0.6 * v, 0.6 * v), sp=max(0.0, 0.5 - v))

A = {}
A["Idle"] = anim("Idle", 61, idle); A["Walk"] = anim("Walk", 31, walk(1)); A["Run"] = anim("Run", 21, walk(1.6))
A["Windup"] = anim("Windup", 20, windup, False); A["Attack1"] = anim("Attack1", 24, ram, False); A["Attack2"] = anim("Attack2", 24, flipper_slap, False)
A["Attack3"] = anim("Attack3", 30, tail_spin, False); A["Jump"] = anim("Jump", 40, jump, False)
A["BeamCharge"] = anim("BeamCharge", 27, beam_charge, False); A["Beam"] = anim("Beam", 20, beam_fire, False); A["BeamHold"] = anim("BeamHold", 18, beam_hold); A["BeamEnd"] = anim("BeamEnd", 24, beam_end, False)
A["Spout"] = anim("Spout", 36, spout_attack, False)
A["Guard"] = anim("Guard", 12, guard); A["Dodge"] = anim("Dodge", 14, dodge, False); A["Hit"] = anim("Hit", 18, hit, False); A["Death"] = anim("Death", 40, death, False)

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = T(0, 0.85, -0.3)
    preview(os.path.join(PRE, "whale_front.png"), T(1.6, 1.4, 6.2), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "whale_three_quarter.png"), T(6.6, 2.0, 1.2), c, arm_ob=arm, action=A["Idle"], frame=1)
    preview(os.path.join(PRE, "whale_beam.png"), T(5.6, 1.6, 4.2), c, arm_ob=arm, action=A["Beam"], frame=10)
    preview(os.path.join(PRE, "whale_jump.png"), T(7.5, 2.4, 5.0), T(0, 1.8, -0.4), arm_ob=arm, action=A["Jump"], frame=17)

glb, fbx = export_all("Goraegorae", OUT, [arm, whale])
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "Goraegorae.fbx"))
print("EXPORTED", glb, fbx, "verts:", len(whale.data.vertices))
