"""바다 · 남쪽 선착장(맵 좌표 그대로 — City_*.fbx 와 같은 설정으로 내보내 World 아래에 그대로 놓으면 맞음).
맵 바닥(지면)은 중심 C, 꼭짓점 반지름 586m 의 정24각형 원판(z = 0). 원판 밖 = 바다.
  · Seaside_Floor: 원판 가장자리(z=0)에서 바깥으로 모래사장 → 얕은 바다 → 깊은 바다(최대 약 -35m). 섬 전체를 둘러쌈. 재질: 마른 모래 · 젖은 모래 · 바다 밑.
    고리(원판을 중심에서 키운 다각형) × 변마다 나눔(선착장 쪽 두 변만 촘촘하게).
  · 선착장(지도 남쪽 끝 꼭짓점 T 에서 바다 쪽 u 방향): 나무 다리(널판 + 기둥 + 밧줄 난간) · 끝의 T자 마당 · 물로 내려가는 계단 · 계류 말뚝 · 등불(Glow)
  · 어부 오두막 · 그물 · 통 · 상자 · 구명 튜브 · 생선 말리는 대 · 바위 · 물속 꾸밈(해초 · 불가사리 · 조개 · 산호)
  · 물에 뜬 부표 Float_* (Unity 가 파도에 맞춰 출렁이게)
  · 바닷길 문 두 개: Gate_Town(마을 남쪽 성벽 안쪽 — 성벽에 붙은 나무 문) · Gate_Harbor(선착장 뭍 — 나무 기둥 문)
  · Seaside_Collision: 다리·마당·계단·오두막·통·문 기둥 상자(보이지 않음). 바닥 충돌은 Seaside_Floor 메시 그대로(Unity 가 MeshCollider).
  · 표시(빈 물체, 위치만 의미): Mark_SeaLevel · Mark_Harbor · Mark_Arrive(마을→선착장 도착) · Mark_Town(선착장→마을 도착) · Mark_Fisher · Mark_Boat/Mark_BoatBow · Mark_Dock
해수면 z = SEA(-0.8). 파도 높이 합은 0.7m 미만이라 뭍(z=0)을 넘지 않음(Unity Ocean 과 같게).
실행: <bpy 파이썬> blender/build_seaside.py [--preview]
출력: unity/Assets/Resources/World/Seaside.fbx · Boat.fbx · exports/Seaside.blend · previews/seaside_*.png"""
import sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bpy, bmesh
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World"); EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
os.makedirs(OUT, exist_ok=True); os.makedirs(PRE, exist_ok=True)
reset_scene()
random.seed(7)

# ── 맵 원판(build_map 결과에서 측정) · 선착장 좌표계 ─────────────────
C = Vector((-50.012, 28.328)); R = 586.0; N = 24; TIP_ANG = math.radians(-36.4136)   # 지도 남쪽 끝 꼭짓점의 각도
SEA = -0.8
POLY = [C + R * Vector((math.cos(TIP_ANG + 2 * math.pi * k / N), math.sin(TIP_ANG + 2 * math.pi * k / N))) for k in range(N)]
T2 = POLY[0]; U2 = (T2 - C).normalized(); V2 = Vector((-U2.y, U2.x))
T = Vector((T2.x, T2.y, 0.0)); U = Vector((U2.x, U2.y, 0.0)); V = Vector((V2.x, V2.y, 0.0)); Z = Vector((0, 0, 1))
def H(a, b, c=0.0): """선착장 좌표(a: 바다 쪽 +, b: 옆, c: 높이) → 맵 좌표"""; return T + U * a + V * b + Z * c
FRAME = Matrix(((U.x, V.x, 0), (U.y, V.y, 0), (0, 0, 1)))   # 열 = u, v, z

def M_(name, color, **kw): m = mat(name, color, **kw); m.diffuse_color = (*color, 1); return m
SAND = M_("Sea_Sand", (0.86, 0.76, 0.56), rough=1.0); WET = M_("Sea_WetSand", (0.62, 0.53, 0.38), rough=0.6); BED = M_("Sea_Bed", (0.42, 0.45, 0.36), rough=1.0)
WOOD = M_("Pier_Wood", (0.55, 0.38, 0.22), rough=0.9); WOOD2 = M_("Pier_WoodLight", (0.66, 0.49, 0.3), rough=0.9); DARK = M_("Pier_DarkWood", (0.3, 0.2, 0.12), rough=0.9)
ROPE = M_("Pier_Rope", (0.78, 0.68, 0.46), rough=1.0); IRON = M_("Pier_Iron", (0.16, 0.16, 0.17), rough=0.5, metal=0.6)
RED = M_("Harbor_Red", (0.78, 0.16, 0.13), rough=0.7); WHITE = M_("Harbor_White", (0.93, 0.92, 0.88), rough=0.7); BLUE = M_("Harbor_Blue", (0.2, 0.42, 0.72), rough=0.6)
ROOF = M_("Harbor_Roof", (0.55, 0.2, 0.15), rough=0.8); GLOW = M_("Harbor_LampGlow", (1.0, 0.85, 0.45), rough=0.4, emit=(1.0, 0.8, 0.4), emit_strength=4.0)
ROCK = M_("Sea_Rock", (0.45, 0.43, 0.4), rough=1.0); WEED = M_("Sea_Weed", (0.2, 0.5, 0.25), rough=0.8); STAR = M_("Sea_Star", (0.98, 0.5, 0.3), rough=0.8)
CORAL = M_("Sea_Coral", (0.95, 0.45, 0.6), rough=0.8); SHELL = M_("Sea_Shell", (0.98, 0.88, 0.8), rough=0.5); NET = M_("Harbor_Net", (0.3, 0.38, 0.32), rough=1.0)
FISH = M_("Harbor_Fish", (0.6, 0.68, 0.75), rough=0.4, metal=0.3); GLASS = M_("Harbor_Glass", (0.35, 0.55, 0.7), rough=0.15)

# ── 바닥 높이(원판 가장자리에서 바깥으로 d m) ──────────────────────
def smooth(a, b, x): t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
def profile(d):
    if d < 10: return -0.12 * d
    if d < 40: return -1.2 - (d - 10) * 0.17
    if d < 130: return -6.3 - (d - 40) * 0.1
    if d < 450: return -15.3 - (d - 130) * 0.045
    return -29.7 - (d - 450) * 0.01
def noise(x, y): return 0.45 * math.sin(x * 0.11 + y * 0.07) + 0.3 * math.sin(x * 0.05 - y * 0.13 + 1.3) + 0.2 * math.sin(x * 0.23 + y * 0.19 + 2.1)
def floor_z(p, d):
    z = profile(d) + noise(p.x, p.y) * smooth(2, 14, d) + 0.12 * math.sin(p.x * 0.9 + p.y * 0.6) * smooth(6, 20, d)
    q2 = Vector((p.x, p.y)); a = (q2 - T2).dot(U2); b = (q2 - T2).dot(V2)      # 선착장 앞은 배가 다니게 깊게(골)
    z -= 2.2 * smooth(6, 0, abs(b) - 9) * smooth(4, 24, a) * smooth(140, 90, a)
    rp = math.hypot(a - PALACE_A, b - PALACE_B)                                # 용궁 분지(build_palace.py): 가운데 평평하게 -42m
    return z + (PALACE_Z - z) * smooth(100, 46, rp)

APO = R * math.cos(math.pi / N)                                                # 변까지 거리
PALACE_A, PALACE_B, PALACE_Z = 165.0, 0.0, -42.0                               # 용궁 자리(선착장 좌표) · 분지 바닥
RINGS = sorted(set([0, 1.5, 3, 4.5, 6, 8, 10, 12.5, 15, 18, 22, 27, 33, 40, 50, 62, 320, 450, 650, 900] + list(range(66, 300, 6))))   # 용궁 분지 둘레(60~300m)는 6m 마다
def subdiv(k): return 64 if k in (0, N - 1) else (16 if k in (1, N - 2) else 6)   # 꼭짓점 T 양쪽 변이 촘촘

bm = bmesh.new(); uvl = bm.loops.layers.uv.new("UVMap"); rows = []
for d in RINGS:
    f = 1 + d / APO; row = []
    for k in range(N):
        a0 = C + (POLY[k] - C) * f; a1 = C + (POLY[(k + 1) % N] - C) * f; S = subdiv(k)
        for s in range(S):
            p = a0.lerp(a1, s / S); row.append(bm.verts.new((p.x, p.y, 0.0 if d == 0 else floor_z(p, d))))
    rows.append(row)
for i in range(len(rows) - 1):
    r0, r1 = rows[i], rows[i + 1]; n = len(r0)
    for j in range(n):
        q = (r0[j], r0[(j + 1) % n], r1[(j + 1) % n], r1[j])
        try: fc = bm.faces.new(q)
        except ValueError: continue
        zc = sum(v.co.z for v in q) / 4; fc.material_index = 0 if zc > SEA + 0.25 else (1 if zc > -2.5 else 2); fc.smooth = True
        for l in fc.loops: l[uvl].uv = (l.vert.co.x * 0.25, l.vert.co.y * 0.25)
bm.normal_update()
for fc in bm.faces:
    if fc.normal.z < 0: fc.normal_flip()
floor = to_object("Seaside_Floor", bm, [SAND, WET, BED])

# ── 도우미: 상자·원기둥·구를 선착장 좌표로 ──────────────────────────
class Builder:
    def __init__(self, mats): self.bm = bmesh.new(); self.mats = mats
    def _mat(self, verts, mi):
        for v in verts:
            for f in v.link_faces: f.material_index = mi
    def box(self, a, b, c, la, lb, lc, mi, rot=0.0, world=None):
        """중심(a,b,c 선착장 좌표 — world 가 있으면 맵 좌표), 크기 la(바다 쪽)·lb(옆)·lc(높이), rot = 위에서 본 회전"""
        r = bmesh.ops.create_cube(self.bm, size=1.0); vs = r["verts"]
        M = Matrix.Translation(world if world is not None else H(a, b, c)) @ (FRAME @ Matrix.Rotation(rot, 3, "Z")).to_4x4() @ Matrix.Diagonal((la, lb, lc, 1))
        bmesh.ops.transform(self.bm, matrix=M, verts=vs); self._mat(vs, mi); return vs
    def cyl(self, p0, p1, r, mi, seg=10, r1=None):
        d = p1 - p0; L = d.length; r = bmesh.ops.create_cone(self.bm, cap_ends=True, segments=seg, radius1=r, radius2=r if r1 is None else r1, depth=L); vs = r["verts"]
        q = Z.rotation_difference(d.normalized()); M = Matrix.Translation((p0 + p1) / 2) @ q.to_matrix().to_4x4()
        bmesh.ops.transform(self.bm, matrix=M, verts=vs); self._mat(vs, mi); return vs
    def sphere(self, p, rx, ry, rz, mi, seg=12, rings=8, rot=None):
        r = bmesh.ops.create_uvsphere(self.bm, u_segments=seg, v_segments=rings, radius=1.0); vs = r["verts"]
        M = Matrix.Translation(p) @ ((rot or Matrix.Identity(3)).to_4x4()) @ Matrix.Diagonal((rx, ry, rz, 1))
        bmesh.ops.transform(self.bm, matrix=M, verts=vs); self._mat(vs, mi); return vs
    def torus(self, p, R_, r, mi, axis=Z, seg=20, sides=8):
        vs = []; rows_ = []
        q = Z.rotation_difference(axis.normalized()).to_matrix()
        for i in range(seg):
            a = 2 * math.pi * i / seg; ring = []
            for j in range(sides):
                b = 2 * math.pi * j / sides; loc = Vector(((R_ + r * math.cos(b)) * math.cos(a), (R_ + r * math.cos(b)) * math.sin(a), r * math.sin(b)))
                ring.append(self.bm.verts.new(p + q @ loc))
            rows_.append(ring); vs += ring
        for i in range(seg):
            for j in range(sides):
                f = self.bm.faces.new((rows_[i][j], rows_[(i + 1) % seg][j], rows_[(i + 1) % seg][(j + 1) % sides], rows_[i][(j + 1) % sides])); f.material_index = mi; f.smooth = True
        return vs
    def rope(self, pts, r, mi, sides=6):
        tb, _ = tube(pts, lambda t: r, sides=sides, mat_idx=mi)
        me = bpy.data.meshes.new("tmp"); tb.to_mesh(me); tb.free(); self.bm.from_mesh(me); bpy.data.meshes.remove(me)
    def done(self, name):
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces[:]); return to_object(name, self.bm, self.mats)

def dist_out(p):
    """원판 가장자리에서 바깥으로 거리(안쪽이면 음수) = 변 24개 평면까지 거리의 최댓값(바닥 고리를 만든 방식과 같음)"""
    q = Vector((p.x, p.y)) - C
    return max(q.dot(Vector((math.cos(TIP_ANG + (k + 0.5) * 2 * math.pi / N), math.sin(TIP_ANG + (k + 0.5) * 2 * math.pi / N)))) - APO for k in range(N))
def fz(a, b): p = H(a, b); d = dist_out(p); return 0.0 if d <= 0 else floor_z(p, d)

# ── 선착장 ─────────────────────────────────────────────────────
MI = {m.name: i for i, m in enumerate([WOOD, WOOD2, DARK, ROPE, IRON, GLOW, RED, WHITE, NET, FISH, BLUE, ROOF, GLASS])}
P = Builder([WOOD, WOOD2, DARK, ROPE, IRON, GLOW, RED, WHITE, NET, FISH, BLUE, ROOF, GLASS])
DECK = 0.45; A0, A1, W = -8.0, 40.0, 3.4; HEAD0, HEAD1, HW = 40.0, 47.0, 10.0
def deck_h(a): return DECK if a >= A0 else max(0.03, DECK * (a - (A0 - 4)) / 4)   # 시작 4m 는 뭍에서 올라오는 비탈
a = A0 - 4; k = 0
while a < A1:                                                                   # 다리 널판(가로로)
    h = deck_h(a + 0.14); P.box(a + 0.14, 0, h - 0.05, 0.28, W, 0.08, MI["Pier_Wood"] if k % 3 else MI["Pier_WoodLight"], rot=0); a += 0.32; k += 1
for side in (-1, 1): P.box((A0 + A1) / 2, side * (W / 2 - 0.25), DECK - 0.22, A1 - A0, 0.2, 0.25, MI["Pier_DarkWood"])   # 받침 보
a = HEAD0; k = 0
while a < HEAD1:                                                                # T자 마당(세로 널판)
    P.box(a + 0.14, 0, DECK - 0.05, 0.28, HW, 0.08, MI["Pier_Wood"] if k % 4 else MI["Pier_WoodLight"]); a += 0.32; k += 1
for b in (-HW / 2 + 0.3, HW / 2 - 0.3): P.box((HEAD0 + HEAD1) / 2, b, DECK - 0.22, HEAD1 - HEAD0, 0.2, 0.25, MI["Pier_DarkWood"])
posts = []
for a in [A0 + 3 * i for i in range(int((A1 - A0) / 3) + 1)]:
    for side in (-1, 1): posts.append((a, side * (W / 2 - 0.12)))
for a in (HEAD0, HEAD0 + 3.5, HEAD1 - 0.2):
    for b in (-HW / 2 + 0.15, -2.5, 2.5, HW / 2 - 0.15): posts.append((a, b))
TOP = {}
for a, b in posts:
    bot = fz(a, b) - 0.6; top = DECK + (0.9 if abs(b) > 1.5 else 0.0)
    P.cyl(H(a, b, bot), H(a, b, top), 0.15, MI["Pier_DarkWood"], seg=8); TOP[(a, b)] = top
# 밧줄 난간: 다리 양옆(오른쪽 계류 자리 34~40 는 비움) · 마당 바깥
def sag(p0, p1, s=0.22, n=8): return [p0.lerp(p1, t / n) - Z * s * math.sin(math.pi * t / n) for t in range(n + 1)]
for side in (-1, 1):
    run = [p for p in posts if p[1] * side > 1.5 and p[0] <= A1 and abs(p[1]) < 2]
    run.sort()
    for (a0, b0), (a1, b1) in zip(run, run[1:]):
        if side > 0 and a0 >= 31: continue                                     # 배 타는 자리
        P.rope(sag(H(a0, b0, TOP[(a0, b0)] - 0.08), H(a1, b1, TOP[(a1, b1)] - 0.08)), 0.03, MI["Pier_Rope"])
for b0, b1 in ((-HW / 2 + 0.15, -2.5), (2.5, HW / 2 - 0.15)):
    P.rope(sag(H(HEAD1 - 0.2, b0, DECK + 0.82), H(HEAD1 - 0.2, b1, DECK + 0.82)), 0.03, MI["Pier_Rope"])
# 계단(마당 왼쪽 끝에서 물로)
STEPS = []
for i in range(9):
    zt = DECK - 0.25 * (i + 1); bb = -HW / 2 - 0.2 - 0.36 * i
    P.box(HEAD0 + 2.0, bb, zt - 0.06, 1.6, 0.4, 0.12, MI["Pier_WoodLight"]); STEPS.append((bb, zt))
P.cyl(H(HEAD0 + 1.1, -HW / 2 - 0.1, DECK), H(HEAD0 + 1.1, -HW / 2 - 3.4, DECK - 2.3), 0.06, MI["Pier_DarkWood"])
P.cyl(H(HEAD0 + 2.9, -HW / 2 - 0.1, DECK), H(HEAD0 + 2.9, -HW / 2 - 3.4, DECK - 2.3), 0.06, MI["Pier_DarkWood"])
# 계류 말뚝 · 등불
for a, b in ((HEAD1 - 0.7, -HW / 2 + 0.7), (HEAD1 - 0.7, HW / 2 - 0.7), (33.5, W / 2 - 0.4), (38.5, W / 2 - 0.4)):
    P.cyl(H(a, b, DECK - 0.05), H(a, b, DECK + 0.38), 0.16, MI["Pier_Iron"], seg=12); P.cyl(H(a, b, DECK + 0.38), H(a, b, DECK + 0.48), 0.22, MI["Pier_Iron"], seg=12)
for a in (2.0, 18.0, 32.0, HEAD1 - 1.0):
    b = -W / 2 + 0.15 if a < 40 else 0.0
    P.cyl(H(a, b, DECK), H(a, b, DECK + 2.4), 0.06, MI["Pier_DarkWood"], seg=8)
    P.box(a, b, DECK + 2.55, 0.32, 0.32, 0.36, MI["Harbor_LampGlow"]); P.box(a, b, DECK + 2.78, 0.42, 0.42, 0.08, MI["Pier_Iron"])
# 밧줄 사리 · 통 · 상자(마당)
def barrel(a, b, base, mi=MI["Pier_WoodLight"]):
    P.cyl(H(a, b, base), H(a, b, base + 0.9), 0.32, mi, seg=14, r1=0.32)
    for hz in (0.15, 0.75): P.torus(H(a, b, base + hz), 0.325, 0.025, MI["Pier_Iron"], seg=16, sides=4)
barrel(HEAD0 + 1.2, 3.8, DECK); barrel(HEAD0 + 2.0, 4.3, DECK); barrel(-12, 6.5, 0.0); barrel(-11.2, 7.3, 0.0)
for a, b, base, s in ((HEAD1 - 1.5, 3.5, DECK, 0.8), (-13.5, 5.2, 0.0, 0.9), (-13.5, 5.2, 0.9, 0.7), (-12.5, 4.4, 0.0, 0.8)):
    P.box(a, b, base + s / 2, s, s, s, MI["Pier_Wood"], rot=0.2 * b)
    P.box(a, b, base + s / 2, s * 1.02, s * 0.2, s * 1.02, MI["Pier_DarkWood"], rot=0.2 * b)
for a, b in ((HEAD0 + 4.5, -3.2), (6.0, -1.0)): P.torus(H(a, b, DECK + 0.06), 0.28, 0.06, MI["Pier_Rope"], seg=18, sides=6); P.torus(H(a, b, DECK + 0.14), 0.2, 0.055, MI["Pier_Rope"], seg=16, sides=6)
pier = P.done("Seaside_Pier")

# ── 어부 오두막 · 그물 · 생선 말리는 대 ────────────────────────────
Q = Builder([WOOD, WOOD2, DARK, ROPE, IRON, GLOW, RED, WHITE, NET, FISH, BLUE, ROOF, GLASS])
HA, HB, HL, HWd, HH = -17.0, 9.0, 4.4, 3.4, 2.4                                  # 오두막 중심(a, b) · 길이 · 폭 · 벽 높이
for i in range(12):                                                             # 세로 널판 벽(바다 쪽 · 뭍 쪽)
    t = -HWd / 2 + (i + 0.5) * HWd / 12
    for a_ in (HA - HL / 2, HA + HL / 2): Q.box(a_, HB + t, HH / 2, 0.12, HWd / 12 - 0.02, HH, MI["Pier_Wood"] if i % 2 else MI["Pier_WoodLight"])
for i in range(15):
    t = -HL / 2 + (i + 0.5) * HL / 15
    for b_ in (HB - HWd / 2, HB + HWd / 2): Q.box(HA + t, b_, HH / 2, HL / 15 - 0.02, 0.12, HH, MI["Pier_Wood"] if i % 2 else MI["Pier_WoodLight"])
for s_ in (-1, 1):                                                              # 박공 지붕(옆으로 기운 두 장)
    Q.box(HA, HB + s_ * HWd * 0.28, HH + 0.55, HL + 0.8, HWd * 0.62, 0.14, MI["Harbor_Roof"], world=None)
    vs = Q.bm.verts[-8:]; bmesh.ops.rotate(Q.bm, verts=vs, cent=H(HA, HB + s_ * HWd * 0.28, HH + 0.55), matrix=Matrix.Rotation(-s_ * 0.55, 3, U))
for s_ in (-1, 1): Q.box(HA + s_ * HL / 2, HB, HH + 0.55, 0.12, HWd * 0.9, 1.0, MI["Pier_Wood"])   # 박공 벽(대충 메움)
Q.box(HA + HL / 2 + 0.07, HB, 0.95, 0.06, 1.0, 1.9, MI["Pier_DarkWood"])          # 문(바다 쪽)
Q.box(HA + HL / 2 + 0.12, HB + 0.3, 0.95, 0.06, 0.1, 0.1, MI["Pier_Iron"])
for b_ in (HB - HWd / 2 - 0.07, HB + HWd / 2 + 0.07):                           # 창문(양옆)
    Q.box(HA, b_, 1.45, 0.9, 0.05, 0.7, MI["Harbor_Glass"]); Q.box(HA, b_ + (0.03 if b_ > HB else -0.03), 1.45, 1.0, 0.05, 0.08, MI["Pier_DarkWood"])
    Q.box(HA, b_ + (0.03 if b_ > HB else -0.03), 1.45, 0.08, 0.05, 0.8, MI["Pier_DarkWood"])
Q.box(HA + HL / 2 + 0.1, HB, HH + 0.25, 0.08, 1.6, 0.45, MI["Harbor_White"])       # 간판(물고기)
Q.sphere(H(HA + HL / 2 + 0.16, HB - 0.1, HH + 0.25), 0.03, 0.42, 0.13, MI["Harbor_Blue"]); Q.sphere(H(HA + HL / 2 + 0.16, HB + 0.42, HH + 0.25), 0.03, 0.12, 0.12, MI["Harbor_Blue"])
Q.torus(H(HA + HL / 2 + 0.12, HB + 1.15, 1.5), 0.32, 0.09, MI["Harbor_Red"], axis=U, seg=16, sides=8)   # 구명 튜브(빨강 + 흰 띠)
for k_ in range(4): Q.box(HA + HL / 2 + 0.14, HB + 1.15 + 0.32 * math.cos(k_ * math.pi / 2 + 0.78), 1.5 + 0.32 * math.sin(k_ * math.pi / 2 + 0.78), 0.2, 0.12, 0.12, MI["Harbor_White"])
for a_, b_ in ((HA + 4.0, HB - 3.0), (HA + 4.0, HB + 3.4)): Q.cyl(H(a_, b_, 0), H(a_, b_, 2.4), 0.07, MI["Pier_DarkWood"], seg=8)   # 그물 기둥 + 그물
for i in range(9):
    t = i / 8; Q.rope(sag(H(HA + 4.0, HB - 3.0 + 0.0, 2.25 - 0.22 * i), H(HA + 4.0, HB + 3.4, 2.25 - 0.22 * i), 0.25 + 0.05 * i, 6), 0.015, MI["Harbor_Net"], sides=4)
for j in range(10):
    bj = HB - 3.0 + 6.4 * j / 9; Q.rope([H(HA + 4.0, bj, 2.25 - 0.25 * math.sin(math.pi * j / 9)), H(HA + 4.0, bj, 0.5)], 0.015, MI["Harbor_Net"], sides=4)
for a_ in (HA - 3.5, HA - 0.5):                                                  # 생선 말리는 대
    for b_ in (HB - 4.5, HB - 7.5): Q.cyl(H(a_, b_, 0), H(a_, b_, 1.8), 0.05, MI["Pier_DarkWood"], seg=6)
for a_ in (HA - 3.5, HA - 0.5): Q.cyl(H(a_, HB - 4.5, 1.8), H(a_, HB - 7.5, 1.8), 0.04, MI["Pier_DarkWood"], seg=6)
for k_ in range(6):
    a_ = HA - 3.5 + (k_ % 2) * 3.0; b_ = HB - 5.0 - (k_ // 2) * 1.0
    Q.sphere(H(a_, b_, 1.45), 0.08, 0.05, 0.28, MI["Harbor_Fish"]); Q.sphere(H(a_, b_, 1.13), 0.09, 0.02, 0.07, MI["Harbor_Fish"])
    Q.cyl(H(a_, b_, 1.8), H(a_, b_, 1.7), 0.01, MI["Pier_Rope"], seg=4)
hut = Q.done("Seaside_Hut")

# ── 바위 · 물속 꾸밈 ──────────────────────────────────────────────
D = Builder([ROCK, WEED, STAR, CORAL, SHELL])
def rock(a, b, s, sink=0.4):
    p = H(a, b); z = fz(a, b); r = bmesh.ops.create_icosphere(D.bm, subdivisions=2, radius=s); vs = r["verts"]
    for v in vs: v.co = Vector((v.co.x * random.uniform(0.85, 1.3), v.co.y * random.uniform(0.85, 1.2), v.co.z * random.uniform(0.6, 0.9))) * random.uniform(0.92, 1.08)
    bmesh.ops.transform(D.bm, matrix=Matrix.Translation(Vector((p.x, p.y, z - sink * s))) @ Matrix.Rotation(random.uniform(0, 6.28), 4, "Z"), verts=vs); D._mat(vs, 0)
for a_, b_, s_ in ((-2, -14, 1.6), (1, -18, 1.1), (3.5, -15.5, 0.8), (-3, 16, 1.4), (2, 19, 1.0), (6, -24, 2.2), (9, 22, 1.8), (-1, -30, 1.3), (14, -11, 1.0), (16, 14, 1.2)): rock(a_, b_, s_)
for _ in range(26):                                                              # 해초 다발
    a_ = random.uniform(10, 70); b_ = random.choice([-1, 1]) * random.uniform(4, 30); z0 = fz(a_, b_)
    if z0 > -2.2: continue
    for k_ in range(random.randint(3, 6)):
        p0 = H(a_ + random.uniform(-0.6, 0.6), b_ + random.uniform(-0.6, 0.6), z0 - 0.1); hgt = random.uniform(1.2, min(3.2, -z0 - 1.0)); ph = random.uniform(0, 6)
        pts = [p0 + Z * hgt * t + V * 0.18 * math.sin(ph + t * 5) + U * 0.12 * math.cos(ph + t * 4) for t in [i / 7 for i in range(8)]]
        tb, _ = tube(pts, lambda t: 0.07 * (1 - 0.7 * t), sides=5, mat_idx=1); me = bpy.data.meshes.new("t"); tb.to_mesh(me); tb.free(); D.bm.from_mesh(me); bpy.data.meshes.remove(me)
def starfish(a, b, s):
    z = fz(a, b) + 0.04; c0 = H(a, b, z); rot = random.uniform(0, 6.28); vs = []
    for k_ in range(5):
        ang = rot + 2 * math.pi * k_ / 5; d_ = Vector((math.cos(ang), math.sin(ang), 0))
        vs += D.sphere(c0 + d_ * s * 0.45, s * 0.5, s * 0.14, s * 0.07, 2, seg=8, rings=5, rot=Matrix.Rotation(ang, 3, "Z"))
    vs += D.sphere(c0, s * 0.22, s * 0.22, s * 0.09, 2, seg=10, rings=5)
for _ in range(14): starfish(random.uniform(8, 50), random.choice([-1, 1]) * random.uniform(2, 20), random.uniform(0.25, 0.4))
for _ in range(18):
    a_ = random.uniform(6, 45); b_ = random.uniform(-18, 18); z = fz(a_, b_) + 0.03
    D.sphere(H(a_, b_, z), 0.16, 0.13, 0.07, 4, seg=10, rings=6, rot=Matrix.Rotation(random.uniform(0, 6.28), 3, "Z"))
for _ in range(10):                                                              # 산호(울퉁불퉁 공 묶음)
    a_ = random.uniform(25, 75); b_ = random.choice([-1, 1]) * random.uniform(8, 26); z = fz(a_, b_)
    for k_ in range(5): D.sphere(H(a_ + random.uniform(-0.5, 0.5), b_ + random.uniform(-0.5, 0.5), z + random.uniform(0.1, 0.6)), 0.3, 0.3, 0.35, 3, seg=8, rings=6)
decor = D.done("Seaside_Decor")

# ── 물에 뜬 부표(Float_*) ─────────────────────────────────────────
floats = []
for i, (a_, b_) in enumerate(((52, -6), (55, 7), (64, -14), (70, 10), (24, -9))):
    B = Builder([RED, WHITE, IRON]); c0 = H(a_, b_, SEA)
    B.sphere(Vector((0, 0, 0.15)), 0.4, 0.4, 0.42, 0, seg=14, rings=10); B.cyl(Vector((0, 0, 0.5)), Vector((0, 0, 1.1)), 0.05, 2, seg=6); B.sphere(Vector((0, 0, 1.15)), 0.1, 0.1, 0.1, 1)
    B.torus(Vector((0, 0, 0.2)), 0.41, 0.06, 1, seg=16, sides=6)
    o = B.done(f"Float_Buoy{i + 1}"); o.location = c0; floats.append(o)

# ── 바닷길 문 ─────────────────────────────────────────────────────
def gate(name, pos2, face2, door):
    """pos2: 맵 xy, face2: 문 앞(사람이 서는 쪽)을 향한 단위 벡터. door=True 면 성벽에 붙은 나무 문, False 면 기둥 문"""
    G = Builder([WOOD, DARK, ROPE, IRON, GLOW, WHITE, BLUE]); f3 = Vector((face2.x, face2.y, 0)); s3 = Vector((-face2.y, face2.x, 0)); o3 = Vector((pos2.x, pos2.y, 0))
    def P_(f, s, z): return o3 + f3 * f + s3 * s + Z * z
    for s_ in (-1.5, 1.5): G.cyl(P_(0, s_, 0), P_(0, s_, 3.2), 0.2, 1, seg=10)
    G.cyl(P_(0, -2.1, 3.3), P_(0, 2.1, 3.3), 0.16, 0, seg=10); G.cyl(P_(0, -1.8, 2.8), P_(0, 1.8, 2.8), 0.1, 0, seg=8)
    r_ = bmesh.ops.create_cube(G.bm, size=1.0); vs = r_["verts"]; M = Matrix.Translation(P_(0.14, 0, 3.05)) @ Matrix(((s3.x, f3.x, 0), (s3.y, f3.y, 0), (0, 0, 1))).to_4x4() @ Matrix.Diagonal((1.7, 0.08, 0.42, 1))
    bmesh.ops.transform(G.bm, matrix=M, verts=vs); G._mat(vs, 5)                                    # 간판(흰 판 + 파도 무늬)
    for k_ in range(3): G.sphere(P_(0.2, -0.5 + 0.5 * k_, 3.05), 0.02, 0.2, 0.06, 6, seg=8, rings=4, rot=Matrix(((s3.x, f3.x, 0), (s3.y, f3.y, 0), (0, 0, 1))))
    for s_ in (-1.5, 1.5): G.sphere(P_(0.28, s_, 2.45), 0.16, 0.16, 0.2, 4)   # 등불
    G.rope(sag(P_(0.05, -1.5, 2.55), P_(0.05, 1.5, 2.55), 0.3), 0.04, 2)
    if door:                                                                     # 성벽에 붙은 두 짝 나무 문
        for s_ in (-0.66, 0.66):
            r_ = bmesh.ops.create_cube(G.bm, size=1.0); vs = r_["verts"]; M = Matrix.Translation(P_(-0.15, s_, 1.25)) @ Matrix(((s3.x, f3.x, 0), (s3.y, f3.y, 0), (0, 0, 1))).to_4x4() @ Matrix.Diagonal((1.28, 0.14, 2.5, 1))
            bmesh.ops.transform(G.bm, matrix=M, verts=vs); G._mat(vs, 1)
            G.torus(P_(0.0, s_ * 0.35, 1.2), 0.1, 0.02, 3, axis=f3, seg=12, sides=4)
    return G.done(name)
NORTH = Vector((-0.788, 0.616)).normalized(); EAST = Vector((0.614, 0.79)).normalized()
TOWN_POS = 40 * EAST - 123.0 * NORTH                                             # 남쪽 성벽 안쪽(e=40, s=123 — 성벽 면은 s≈125.5)
TOWN_FACE = -(0.148 * EAST + 0.989 * (-NORTH)).normalized()                        # 성벽에서 마을 쪽
gate_town = gate("Gate_Town", TOWN_POS, TOWN_FACE, True)
HG = H(-30, -3); gate_harbor = gate("Gate_Harbor", Vector((HG.x, HG.y)), -V2 * 0 + U2, False)

# ── 충돌(보이지 않음) ──────────────────────────────────────────────
K_ = Builder([DARK])
K_.box((A0 + A1) / 2, 0, DECK - 0.15, A1 - A0, W, 0.3, 0)                        # 다리
vs = K_.box(A0 - 2, 0, 0.2, 4.0, W, 0.2, 0)                                      # 비탈: 윗면 = deck_h, 아랫면 0.15 아래
for v in vs:
    a_ = (v.co - T).dot(U); top = v.co.z > 0.2; v.co.z = deck_h(a_) - (0.0 if top else 0.15)
K_.box((HEAD0 + HEAD1) / 2, 0, DECK - 0.15, HEAD1 - HEAD0, HW, 0.3, 0)            # 마당
for bb, zt in STEPS: K_.box(HEAD0 + 2.0, bb, zt - 0.1, 1.6, 0.4, 0.2, 0)
K_.box(HA, HB, HH / 2, HL + 0.2, HWd + 0.2, HH, 0)                                # 오두막
for a_, b_ in ((HEAD0 + 1.2, 3.8), (HEAD0 + 2.0, 4.3), (-12, 6.5), (-11.2, 7.3)): K_.box(a_, b_, DECK + 0.45 if a_ > 0 else 0.45, 0.64, 0.64, 0.9, 0)
for a_, b_, s_ in ((HEAD1 - 1.5, 3.5, 0.8), (-13.5, 5.2, 0.9), (-12.5, 4.4, 0.8)): K_.box(a_, b_, (DECK if a_ > 0 else 0) + s_ / 2 + (0.4 if b_ == 5.2 else 0), s_, s_, s_ * (2 if b_ == 5.2 else 1), 0)
coll = K_.done("Seaside_Collision"); coll.hide_render = True
def gate_coll(pos2, face2):
    f3 = Vector((face2.x, face2.y, 0)); s3 = Vector((-face2.y, face2.x, 0))
    for s_ in (-1.5, 1.5):
        r_ = bmesh.ops.create_cube(K2.bm, size=1.0); vs = r_["verts"]; p = Vector((pos2.x, pos2.y, 1.6)) + s3 * s_
        bmesh.ops.transform(K2.bm, matrix=Matrix.Translation(p) @ Matrix.Diagonal((0.45, 0.45, 3.2, 1)), verts=vs)
K2 = Builder([DARK]); gate_coll(TOWN_POS, TOWN_FACE); gate_coll(Vector((HG.x, HG.y)), U2); coll2 = K2.done("Seaside_GateCollision"); coll2.hide_render = True

# ── 표시 ─────────────────────────────────────────────────────────
marks = []
def mark(name, p):
    e = bpy.data.objects.new(name, None); e.empty_display_size = 0.5; e.location = p; bpy.context.collection.objects.link(e); marks.append(e); return e
mark("Mark_SeaLevel", H(0, 0, SEA)); mark("Mark_Harbor", H(0, 0, 0)); mark("Mark_HarborSea", H(60, 0, SEA)); mark("Mark_IslandCenter", Vector((C.x, C.y, 0.0)))
mark("Mark_Arrive", H(-24, -3, 0.05)); mark("Mark_ArriveLook", H(0, 0, 0.05))
mark("Mark_Town", Vector((TOWN_POS.x, TOWN_POS.y, 0.05)) + Vector((TOWN_FACE.x, TOWN_FACE.y, 0)) * 3.0); mark("Mark_TownLook", Vector((TOWN_POS.x, TOWN_POS.y, 0.05)) + Vector((TOWN_FACE.x, TOWN_FACE.y, 0)) * 8.0)
mark("Mark_GateTown", Vector((TOWN_POS.x, TOWN_POS.y, 0.05)) + Vector((TOWN_FACE.x, TOWN_FACE.y, 0)) * 1.2); mark("Mark_GateHarbor", H(-30 + 1.2, -3, 0.05))
mark("Mark_Fisher", H(-3.0, -3.6, 0.05)); mark("Mark_Boat", H(36.5, W / 2 + 1.55, SEA)); mark("Mark_BoatBow", H(41.5, W / 2 + 1.55, SEA)); mark("Mark_Dock", H(36.5, W / 2 - 0.6, DECK + 0.05))

# ── 내보내기(City 와 같은 설정) ────────────────────────────────────
allobj = [floor, pier, hut, decor, gate_town, gate_harbor, coll, coll2] + floats + marks
bpy.ops.object.select_all(action="DESELECT")
for o in allobj: o.select_set(True)
fbx = os.path.join(OUT, "Seaside.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("EXPORTED", fbx, f"{os.path.getsize(fbx) / 1e6:.1f} MB", "floor verts", len(floor.data.vertices), "pier", len(pier.data.vertices), "hut", len(hut.data.vertices), "decor", len(decor.data.vertices))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(EXP, "Seaside.blend"))
