"""하프물범 여왕의 성 — 알현실(왕좌의 방). 사용자 참고 그림(고딕 왕궁 홀) 그대로 + 하프물범 얼굴·장식·조각상.
  · 흰 대리석 바닥(금줄 상감: 가장자리 줄 · 칸 줄 · 마름모) · 양옆 통로는 육각 타일
  · 가운데 붉은 카펫(금 테두리) — 문에서 왕좌 계단 꼭대기까지
  · 마름모 격자 기둥 2줄(크림색 몸 + 금빛 나선 띠가 엇갈려 격자) · 받침 · 머리 장식 · 기둥마다 하프물범 얼굴 메달 + 촛대
  · 고딕 늑골 천장(뾰족 아치 볼트 — 가운데 높고 양옆 통로 낮음, 가로 늑골 · 대각 늑골 · 금 꼭지)
  · 금빛 샹들리에(가운데 3개, 촛불 고리 2단) · 카펫 양옆 촛대 · 받침 위 화분
  · 양옆 벽 뾰족 아치 스테인드글라스(파랑 중심 + 보라·빨강·금 조각, 납 테두리) · 끝 벽 큰 창 3폭 + 하프물범 얼굴 장미창
  · 문 쪽 하프물범 조각상 한 쌍(천사상 대신) · 왕좌 양옆 큰 하프물범 조각상 · 남색 깃발(물범 문장)
  · 맨 끝 계단(6단) 위 **거대한 왕좌**(금 틀 · 남색 벨벳 등받이 · 붉은 방석 · 물범 머리 팔걸이 · 꼭대기 물범 얼굴 문장 + 진주 왕관)
  · 큰 나무 정문(성 안쪽에서 돌아 나가는 문)
자리: 성 위 하늘(맵 좌표 z = FLOOR) — 바깥과 이어지지 않는 별도 실내. 성 본채 앞 문(CastleDoor, 맵 좌표 그대로 성 팔각 성벽 앞)에서 들어옴.
표시: Mark_Arrive · Mark_ArriveLook · Mark_ExitDoor · Mark_Throne · Mark_ThroneSeat · Mark_Chandelier_1..3 · Mark_Window · Mark_HallCenter · Mark_DoorFront · Mark_DoorLook
실행: <bpy 파이썬> blender/build_castle_hall.py [--preview]   →  unity/Assets/Resources/World/CastleHall.fbx"""
import sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bpy, bmesh
from mathutils import Vector, Matrix
from meshkit import Frame, Builder

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World"); EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
reset_scene(); random.seed(7)

CASTLE = Vector((-43.6, 27.3)); PLAZA = Vector((35.3, -30.7)); DIR = (PLAZA - CASTLE).normalized()
FLOOR = 260.0                                     # 실내 바닥 높이(성 위 하늘 — 바깥과 안 만남)
L, W, NW = 60.0, 13.5, 7.0                        # 길이(문 0 → 왕좌 쪽 +x) · 반폭(벽) · 기둥 줄(가운데 통로 반폭)
COLX = [8.0, 16.0, 24.0, 32.0, 40.0]               # 기둥 자리
SPRING, APEX, AAPEX = 14.4, 23.0, 18.0             # 볼트가 시작하는 높이 · 가운데 꼭대기 · 통로 꼭대기
DAIS0, STEP_N, STEP_H, STEP_D = 46.0, 6, 0.29, 0.9 # 왕좌 계단
TOP = STEP_N * STEP_H; THRONE_X = 55.5

def M_(n, c, rough=0.5, metal=0.0): m = mat(n, c, rough=rough, metal=metal); m.diffuse_color = (*c, 1); return m
MARBLE = M_("Hall_Marble", (0.95, 0.94, 0.92), 0.18); GOLD = M_("Hall_Gold", (1.0, 0.76, 0.3), 0.25, 1.0); STONE = M_("Hall_Stone", (0.9, 0.86, 0.78), 0.8)
LAT = M_("Hall_Lattice", (0.92, 0.86, 0.74), 0.6); LATR = M_("Hall_LatticeRib", (0.78, 0.66, 0.44), 0.45, 0.4)
CARPET = M_("Hall_Carpet", (0.72, 0.06, 0.1), 0.9); HEX = M_("Hall_Hex", (0.88, 0.86, 0.83), 0.4); GROUT = M_("Hall_Grout", (0.62, 0.6, 0.58), 0.9)
VAULT = M_("Hall_Vault", (0.96, 0.93, 0.86), 0.85)
GB = mat("Hall_GlassGlow_Blue", (0.12, 0.3, 0.85), emit=(0.12, 0.3, 0.85), emit_strength=2.5); GP = mat("Hall_GlassGlow_Purple", (0.45, 0.2, 0.7), emit=(0.45, 0.2, 0.7), emit_strength=2.5)
GR = mat("Hall_GlassGlow_Red", (0.8, 0.12, 0.16), emit=(0.8, 0.12, 0.16), emit_strength=2.5); GY = mat("Hall_GlassGlow_Gold", (1.0, 0.78, 0.25), emit=(1.0, 0.78, 0.25), emit_strength=2.5)
GW = mat("Hall_GlassGlow_White", (0.92, 0.95, 1.0), emit=(0.92, 0.95, 1.0), emit_strength=2.5)
LEAD = M_("Hall_Lead", (0.16, 0.15, 0.18), 0.6, 0.5); CANDLE = M_("Hall_Candle", (0.98, 0.95, 0.88), 0.6)
FLAME = mat("Hall_FlameGlow", (1.0, 0.7, 0.3), emit=(1.0, 0.65, 0.25), emit_strength=8.0)
LEAF = M_("Hall_Leaf", (0.16, 0.38, 0.14), 0.9); POT = M_("Hall_Pot", (0.82, 0.8, 0.76), 0.5)
SEAL = M_("Hall_SealStone", (0.97, 0.97, 0.96), 0.35); INK = M_("Hall_Ink", (0.06, 0.06, 0.08), 0.3)
NAVY = M_("Hall_Velvet", (0.08, 0.12, 0.32), 0.9); RVEL = M_("Hall_RedVelvet", (0.62, 0.05, 0.1), 0.9)
WOOD = M_("Hall_Wood", (0.42, 0.24, 0.12), 0.7); PEARLM = M_("Hall_Pearl", (0.96, 0.94, 1.0), 0.1)
BATB = M_("Hall_BatBlue", (0.08, 0.45, 0.9), 0.35); BEAR = M_("Hall_BearBrown", (0.5, 0.31, 0.16), 0.8); BEARL = M_("Hall_BearLight", (0.86, 0.68, 0.48), 0.8)
MATS = [MARBLE, GOLD, STONE, LAT, LATR, CARPET, HEX, GROUT, VAULT, GB, GP, GR, GY, GW, LEAD, CANDLE, FLAME, LEAF, POT, SEAL, INK, NAVY, RVEL, WOOD, PEARLM, BATB, BEAR, BEARL]

O = Vector((CASTLE.x - 30.0, CASTLE.y, FLOOR))
F = Frame(O, (1, 0, 0), (0, 1, 0))
b = Builder(MATS, F)

def arch_z(y, w, z0, h):
    """뾰족 아치: 반폭 w, 시작 높이 z0, 솟음 h — |y| ≤ w 에서의 높이"""
    R = (w * w + h * h) / (2 * w); y = min(abs(y), w)
    return z0 + math.sqrt(max(0.0, R * R - (y - (w - R)) ** 2))
def arch_pts(w, z0, h, n=12):
    return [(w * (-1 + 2 * i / n), arch_z(w * (-1 + 2 * i / n), w, z0, h)) for i in range(n + 1)]
def arch_plate(axis, at, uc, w, zb, z0, h, thick, mat_n, n=24):
    """뾰족 아치 판 한 덩어리: 아래 zb 부터 아치 꼭대기까지. axis 'x' = 끝 벽(판이 y-z 면, 두께 x) · 'y' = 옆벽(x-z 면, 두께 y). uc = 가로 가운데."""
    prof = [(uc + u, zb) for u in (-w,)] + [(uc + u, z) for (u, z) in arch_pts(w, z0, h, n)] + [(uc + w, zb)]
    bm = bmesh.new(); vs = []
    for d_ in (-thick / 2, thick / 2):
        row = []
        for (u, z) in prof: row.append(bm.verts.new(F.P(at + d_, u, z) if axis == "x" else F.P(u, at + d_, z)))
        vs.append(row)
    bm.faces.new(vs[0]); bm.faces.new(list(reversed(vs[1])))
    for i in range(len(prof)):
        j = (i + 1) % len(prof); bm.faces.new((vs[0][i], vs[0][j], vs[1][j], vs[1][i]))
    for f_ in bm.faces: f_.material_index = b.M(mat_n)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:]); b.merge(bm)

# ── 바닥: 가운데 대리석 + 금 상감 · 양옆 육각 타일 ─────────────────────
b.box(L / 2, 0, -0.25, L, NW * 2 + 0.4, 0.5, "Hall_Marble")
def inlay(x, y, sx, sy, rot=0.0): b.box(x, y, 0.006, sx, sy, 0.014, "Hall_Gold", rot=rot)
for yy in (-NW + 0.25, NW - 0.25, -1.75, 1.75): inlay(DAIS0 / 2, yy, DAIS0, 0.07)
for xx in range(4, int(DAIS0), 8): inlay(xx, 0, 0.07, NW * 2 - 0.5)
for x0 in range(0, int(DAIS0) - 7, 8):                                       # 카펫 양옆 마름모
    for s in (1, -1):
        cx, cy = x0 + 4, s * 4.4; dlen = math.hypot(4, 2.4)
        for a in (math.atan2(2.4, 4), -math.atan2(2.4, 4)):
            for sg in (1, -1): inlay(cx + sg * 2 * math.cos(a) * 0 + sg * 2.0, cy + sg * 1.2 * (1 if a > 0 else -1), dlen, 0.06, rot=a)
for s in (1, -1):
    b.box(L / 2, s * (NW + (W - NW) / 2), -0.27, L, W - NW + 0.3, 0.5, "Hall_Grout")
    r = 0.46; dx, dy = r * 1.5, r * math.sqrt(3)
    j = 0; xx = 0.6
    while xx < L - 0.5:
        yy = NW + 0.5 + (dy / 2 if j % 2 else 0)
        while yy < W - 0.4:
            b.cyl((xx, s * yy, -0.02), (xx, s * yy, 0.012), r * 0.93, "Hall_Hex", seg=6)
            yy += dy
        xx += dx; j += 1
# ── 카펫(금 테두리) · 계단 · 왕좌 단 ──────────────────────────────────
b.box(DAIS0 / 2 + 0.5, 0, 0.03, DAIS0 - 1.0, 2.6, 0.04, "Hall_Carpet")
for s in (1, -1): b.box(DAIS0 / 2 + 0.5, s * 1.36, 0.035, DAIS0 - 1.0, 0.14, 0.045, "Hall_Gold")
for i in range(STEP_N):
    x0 = DAIS0 + i * STEP_D; z1 = (i + 1) * STEP_H; wid = 18.0 - i * 0.6
    b.box((x0 + L) / 2, 0, z1 / 2, L - x0, wid, z1, "Hall_Marble")
    b.box(x0 + 0.05, 0, z1 + 0.005, 0.1, wid, 0.02, "Hall_Gold")
    b.box(x0 + STEP_D / 2, 0, z1 + 0.02, STEP_D + 0.02, 2.6, 0.03, "Hall_Carpet")
    b.box(x0 - 0.015, 0, z1 - STEP_H / 2 + 0.02, 0.03, 2.6, STEP_H + 0.04, "Hall_Carpet")
b.box((DAIS0 + STEP_N * STEP_D + THRONE_X) / 2 + 0.5, 0, TOP + 0.02, THRONE_X - DAIS0 - STEP_N * STEP_D + 1.0, 2.6, 0.03, "Hall_Carpet")

# ── 벽 · 끝 벽 · 정문 ──────────────────────────────────────────────
for s in (1, -1): b.box(L / 2, s * (W + 0.5), 9.8, L, 1.0, 19.6, "Hall_Stone")   # 통로 볼트 꼭대기(18m + 두께)보다 높게 — 이음매 틈 방지
for xe, nx in ((-0.5, 0), (L + 0.5, 0)):
    b.box(xe, 0, 9.8, 1.0, W * 2 + 2, 19.6, "Hall_Stone")
    arch_plate("x", xe + (0.02 if xe > 0 else -0.02), 0.0, NW + 0.3, SPRING, SPRING, APEX - SPRING + 0.6, 0.96, "Hall_Stone", 32)   # 가운데 박공(뾰족 아치까지 막음, 벽 면보다 살짝 뒤 — 같은 면 겹침 방지)
def lancet(cx_wall, axis, u, z0, wdt, hgt, pane=True, mats=None, depth=0.25):
    """뾰족 아치 창: 벽(axis='y' → 옆벽 x=u 자리, 'x' → 끝벽 y=u 자리), 아래 z0, 폭 wdt, 높이 hgt"""
    rows, cols = 10, 3; mats = mats or ["Hall_GlassGlow_Blue"] * 6 + ["Hall_GlassGlow_Purple", "Hall_GlassGlow_Red", "Hall_GlassGlow_Gold", "Hall_GlassGlow_White"]
    top = z0 + hgt; spring = top - wdt * 0.9
    for r in range(rows):
        for c in range(cols):
            uu = -wdt / 2 + wdt * (c + 0.5) / cols; zz = z0 + (top - z0) * (r + 0.5) / rows
            m = random.choice(mats); ph, pw = (top - z0) / rows - 0.08, wdt / cols - 0.08
            if zz > spring:                                                    # 뾰족한 윗부분: 아치 안쪽만큼 좁힘
                half = 0.0
                for yy in [i * wdt / 40 for i in range(21)]:
                    if arch_z(yy, wdt / 2, spring, top - spring) >= zz + ph / 2: half = yy
                lo, hi = max(-half, -wdt / 2 + c * wdt / cols + 0.04), min(half, -wdt / 2 + (c + 1) * wdt / cols - 0.04)
                if hi - lo < 0.08: continue
                uu, pw = (lo + hi) / 2, hi - lo
            if axis == "y": b.box(u + uu, cx_wall, zz, pw, depth, ph, m)
            else: b.box(cx_wall, u + uu, zz, depth, pw, ph, m)
    for (pu, pz) in arch_pts(wdt / 2 + 0.18, spring, top - spring + 0.18, 14):   # 금빛 테두리
        if axis == "y": b.box(u + pu, cx_wall, pz, 0.22, depth + 0.1, 0.22, "Hall_Gold")
        else: b.box(cx_wall, u + pu, pz, depth + 0.1, 0.22, 0.22, "Hall_Gold")
    for s in (-1, 1):
        if axis == "y": b.box(u + s * (wdt / 2 + 0.18), cx_wall, (z0 + spring) / 2, 0.22, depth + 0.1, spring - z0, "Hall_Gold")
        else: b.box(cx_wall, u + s * (wdt / 2 + 0.18), (z0 + spring) / 2, depth + 0.1, 0.22, spring - z0, "Hall_Gold")
    arch_plate(axis, cx_wall + (0.04 if cx_wall > 0 else -0.04), u, wdt / 2, z0, spring, top - spring, depth - 0.1, "Hall_Lead", 16)   # 납 뒤판(한 판)
for s in (1, -1):
    for xw in (4.0, 12.0, 20.0, 28.0, 36.0):
        lancet(s * (W - 0.02), "y", xw, 3.0, 2.4, 10.5)
for yw in (-3.4, 0.0, 3.4): lancet(L - 0.02, "x", yw, 6.0 if yw else 9.0, 2.6, 12.5 if yw else 11.0, mats=["Hall_GlassGlow_Blue"] * 4 + ["Hall_GlassGlow_Purple", "Hall_GlassGlow_Gold", "Hall_GlassGlow_White"])

# 하프물범 얼굴(메달 · 장미창 · 장식에 두루): 흰 얼굴 + 점 눈 + 세모 코 + ω 입 + 수염, 둘레 금테
def seal_face(cx, cy, cz, r, facing, glass=False):
    """facing: 얼굴이 보는 쪽 로컬 방향 (1,0,0) 등"""
    f = Vector(facing).normalized(); up = Vector((0, 0, 1)); side = f.cross(up).normalized()
    def P(a, bb, out=0.0): return Vector((cx, cy, cz)) + side * a + up * bb + f * out
    face = "Hall_GlassGlow_White" if glass else "Hall_SealStone"
    p0 = P(0, 0); b.cyl(tuple(p0 - f * 0.05), tuple(p0 + f * 0.05), r, face, seg=28)
    b.torus(tuple(p0 + f * 0.04), r, r * 0.08, "Hall_Gold", axis=tuple(f), seg=28, sides=6)
    for s in (-1, 1):
        b.sphere(tuple(P(s * r * 0.36, r * 0.08, 0.06)), (r * 0.09, r * 0.09, r * 0.09), "Hall_Ink", seg=10, rings=6)
        for k in (0, 1):                                                       # 수염
            a0, a1 = P(s * r * 0.5, -r * (0.1 + 0.12 * k), 0.06), P(s * r * 0.85, -r * (0.04 + 0.2 * k), 0.06)
            b.tube([tuple(a0), tuple(a1)], r * 0.025, "Hall_Ink", sides=4)
    b.sphere(tuple(P(0, -r * 0.12, 0.07)), (r * 0.1, r * 0.06, r * 0.07), "Hall_Ink", seg=10, rings=6)
    for s in (-1, 1): b.tube([tuple(P(0, -r * 0.2, 0.06)), tuple(P(s * r * 0.12, -r * 0.3, 0.06)), tuple(P(s * r * 0.24, -r * 0.24, 0.06))], r * 0.025, "Hall_Ink", sides=4)
seal_face(L - 0.4, 0.0, 19.6, 2.1, (-1, 0, 0), glass=True)                     # 끝 벽 장미창 = 물범 얼굴

# 정문(안쪽): 큰 나무 문 + 금 테두리 + 물범 문고리
for s in (-1, 1): b.box(0.05, s * 1.3, 3.6, 0.3, 2.5, 7.2, "Hall_Wood")
for (pu, pz) in arch_pts(2.8, 7.2, 2.2, 12): b.box(0.12, pu, pz, 0.36, 0.3, 0.3, "Hall_Gold")
for s in (-1, 1): b.box(0.12, s * 2.8, 3.6, 0.36, 0.3, 7.2, "Hall_Gold"); seal_face(0.3, s * 0.7, 3.4, 0.32, (1, 0, 0))

# ── 기둥(마름모 격자) + 물범 얼굴 메달 + 촛대 ──────────────────────────
def column(x, y):
    b.cyl((x, y, 0.0), (x, y, 1.0), 1.35, "Hall_Stone", seg=8); b.cyl((x, y, 1.0), (x, y, 1.6), 1.15, "Hall_Gold", r1=1.0, seg=8)
    b.cyl((x, y, 1.6), (x, y, 13.4), 0.85, "Hall_Lattice", seg=20)
    H0, H1, n = 1.7, 13.3, 26
    for k in range(6):
        for tw in (1, -1):
            a0 = k * math.pi / 3
            pts = [(x + math.cos(a0 + tw * 2.6 * t) * 0.88, y + math.sin(a0 + tw * 2.6 * t) * 0.88, H0 + (H1 - H0) * t) for t in [i / n for i in range(n + 1)]]
            b.tube(pts, 0.055, "Hall_LatticeRib", sides=5)
    b.cyl((x, y, 13.4), (x, y, 14.0), 0.95, "Hall_Gold", r1=1.25, seg=16); b.cyl((x, y, 14.0), (x, y, 14.45), 1.35, "Hall_Stone", seg=8)
    face_dir = (0, -1 if y > 0 else 1, 0)
    seal_face(x, y + face_dir[1] * 0.95, 11.6, 0.55, face_dir)
    sx = y + face_dir[1] * 1.0                                                   # 촛대(얼굴 아래)
    b.box(x, sx, 7.0, 0.14, 0.5, 0.14, "Hall_Gold")
    for dx in (-0.5, 0.0, 0.5):
        b.tube([(x, sx, 7.0), (x + dx, sx + face_dir[1] * 0.35, 7.25), (x + dx, sx + face_dir[1] * 0.4, 7.5)], 0.04, "Hall_Gold", sides=5)
        b.cyl((x + dx, sx + face_dir[1] * 0.4, 7.5), (x + dx, sx + face_dir[1] * 0.4, 7.85), 0.06, "Hall_Candle", seg=8)
        b.sphere((x + dx, sx + face_dir[1] * 0.4, 7.96), (0.05, 0.05, 0.09), "Hall_FlameGlow", seg=8, rings=5)
for xc in COLX:
    for s in (1, -1): column(xc, s * NW)

# ── 천장: 뾰족 아치 볼트(가운데 높게 · 양옆 통로 낮게) + 늑골 ───────────────
def vault(y0, y1, z0, apex, mat_n="Hall_Vault", n=20):
    w = (y1 - y0) / 2; yc = (y0 + y1) / 2
    prof = arch_pts(w, z0, apex - z0, n)
    bm = bmesh.new(); th = 0.4
    inner = [(yc + py, pz) for py, pz in prof]; outer = [(yc + py * (1 + th / w), pz + th) for py, pz in prof]
    vi = [[bm.verts.new(F.P(x, py, pz)) for (py, pz) in inner] for x in (-0.6, L + 0.6)]   # 끝 벽 속으로 파묻음(같은 면 겹침 → 검은 줄 방지)
    vo = [[bm.verts.new(F.P(x, py, pz)) for (py, pz) in outer] for x in (-0.6, L + 0.6)]
    for i in range(len(prof) - 1):
        bm.faces.new((vi[0][i], vi[1][i], vi[1][i + 1], vi[0][i + 1])); bm.faces.new((vo[0][i + 1], vo[1][i + 1], vo[1][i], vo[0][i]))
        bm.faces.new((vi[0][i + 1], vo[0][i + 1], vo[0][i], vi[0][i])); bm.faces.new((vi[1][i], vo[1][i], vo[1][i + 1], vi[1][i + 1]))
    for k in (0, 1):
        bm.faces.new((vi[k][0], vo[k][0], vo[k][-1], vi[k][-1]) if k == 0 else (vi[k][-1], vo[k][-1], vo[k][0], vi[k][0]))
    for f_ in bm.faces: f_.material_index = b.M(mat_n)
    b.merge(bm)
vault(-NW, NW, SPRING, APEX)
for s in (1, -1): vault(min(s * NW, s * W), max(s * NW, s * W), SPRING - 0.4, AAPEX)
for xr in [0.6] + COLX + [48.0, L - 0.6]:                                         # 가로 늑골(아치)
    b.tube([(xr, py, pz - 0.05) for py, pz in arch_pts(NW, SPRING, APEX - SPRING, 18)], 0.18, "Hall_LatticeRib", sides=6)
    for s in (1, -1): b.tube([(xr, s * (NW + (W - NW) / 2) + py, pz - 0.05) for py, pz in arch_pts((W - NW) / 2, SPRING - 0.4, AAPEX - SPRING + 0.4, 12)], 0.14, "Hall_LatticeRib", sides=6)
bays = [0.6] + COLX + [48.0, L - 0.6]
for xa, xb in zip(bays, bays[1:]):                                              # 대각 늑골 + 금 꼭지
    for s in (1, -1):
        pts = []
        for i in range(17):
            t = i / 16; yy = -NW + 2 * NW * t; xx = xa + (xb - xa) * t if s > 0 else xb - (xb - xa) * t
            pts.append((xx, yy, arch_z(yy, NW, SPRING, APEX - SPRING) - 0.12))
        b.tube(pts, 0.11, "Hall_LatticeRib", sides=5)
    b.sphere(((xa + xb) / 2, 0, APEX - 0.35), (0.45, 0.45, 0.3), "Hall_Gold", seg=14, rings=8)
b.tube([(x, 0, APEX - 0.1) for x in (0.6, L - 0.6)], 0.16, "Hall_LatticeRib", sides=6)  # 용마루 늑골

# ── 샹들리에(가운데 3) · 카펫 옆 촛대 · 화분 ─────────────────────────────
CHAND = [12.0, 28.0, 44.0]
for xc in CHAND:
    b.tube([(xc, 0, APEX - 0.4), (xc, 0, 15.6)], 0.06, "Hall_Gold", sides=6)
    for zz, rr, n in ((12.6, 2.4, 14), (13.8, 1.5, 9)):
        b.torus((xc, 0, zz), rr, 0.1, "Hall_Gold", seg=36, sides=6)
        for k in range(n):
            a = 2 * math.pi * k / n; px, py = xc + math.cos(a) * rr, math.sin(a) * rr
            b.tube([(xc, 0, zz + 1.6), (px, py, zz + 0.25), (px, py, zz)], 0.04, "Hall_Gold", sides=4)
            b.cyl((px, py, zz + 0.05), (px, py, zz + 0.45), 0.07, "Hall_Candle", seg=8); b.sphere((px, py, zz + 0.56), (0.06, 0.06, 0.11), "Hall_FlameGlow", seg=8, rings=5)
    b.sphere((xc, 0, 12.2), (0.5, 0.5, 0.7), "Hall_Gold", seg=16, rings=10); b.sphere((xc, 0, 11.4), (0.22, 0.22, 0.22), "Hall_Pearl", seg=12, rings=8)
for xx in range(6, int(DAIS0), 8):
    for s in (1, -1):
        y = s * 3.1
        b.cyl((xx, y, 0), (xx, y, 0.12), 0.38, "Hall_Gold", seg=12); b.cyl((xx, y, 0.12), (xx, y, 2.2), 0.06, "Hall_Gold", seg=8)
        for dy in (-0.45, 0.0, 0.45):
            b.tube([(xx, y, 2.0), (xx, y + dy, 2.2), (xx, y + dy, 2.35)], 0.035, "Hall_Gold", sides=4)
            b.cyl((xx, y + dy, 2.35), (xx, y + dy, 2.7), 0.055, "Hall_Candle", seg=8); b.sphere((xx, y + dy, 2.8), (0.05, 0.05, 0.09), "Hall_FlameGlow", seg=8, rings=5)
    for s in (1, -1):
        y = s * 5.2; xp = xx + 4
        if xp >= DAIS0 - 1: continue
        b.cyl((xp, y, 0), (xp, y, 0.9), 0.38, "Hall_Pot", seg=10); b.cyl((xp, y, 0.9), (xp, y, 1.4), 0.32, "Hall_Gold", r1=0.5, seg=12)
        for k in range(4): b.sphere((xp + 0.18 * math.cos(k * 1.6), y + 0.18 * math.sin(k * 1.6), 1.6 + 0.12 * (k % 2)), (0.36, 0.36, 0.3), "Hall_Leaf", seg=10, rings=7)

# ── 하프물범 조각상(문 쪽 한 쌍 · 왕좌 옆 큰 한 쌍) ───────────────────────
def seal_statue(x, y, sc, face_x):
    """앉은 하프물범(인형처럼 통통) — 몸에 붙은 큰 머리 · 볼록한 주둥이 · 점 눈 · 수염 · 앞지느러미로 진주를 안음 · 뒤 꼬리 · 작은 왕관"""
    b.box(x, y, 0.6 * sc, 2.1 * sc, 2.1 * sc, 1.2 * sc, "Hall_Stone"); b.box(x, y, 1.25 * sc, 2.3 * sc, 2.3 * sc, 0.12 * sc, "Hall_Gold")
    z0 = 1.31 * sc; f = 1 if face_x > 0 else -1
    def P(fw, sd, up): return (x + f * fw * sc, y + sd * sc, z0 + up * sc)
    b.sphere(P(-0.1, 0, 0.6), (0.95 * sc, 0.85 * sc, 0.7 * sc), "Hall_SealStone", seg=28, rings=18)        # 엉덩이(넓게)
    b.sphere(P(0.0, 0, 1.25), (0.78 * sc, 0.74 * sc, 0.75 * sc), "Hall_SealStone", seg=28, rings=18)       # 몸통
    b.sphere(P(0.12, 0, 1.95), (0.72 * sc, 0.7 * sc, 0.62 * sc), "Hall_SealStone", seg=28, rings=18)       # 머리(몸에 붙음)
    b.sphere(P(0.68, 0, 1.78), (0.24 * sc, 0.34 * sc, 0.2 * sc), "Hall_SealStone", seg=18, rings=12)       # 주둥이
    for s_ in (-1, 1):
        b.sphere(P(0.7, s_ * 0.3, 2.1), (0.06 * sc, 0.075 * sc, 0.075 * sc), "Hall_Ink", seg=10, rings=6)
        b.sphere(P(0.6, s_ * 0.36, 2.25), (0.03 * sc, 0.06 * sc, 0.02 * sc), "Hall_Ink", seg=8, rings=5)  # 점 눈썹
        for k in (0, 1): b.tube([P(0.86, s_ * 0.12, 1.78 - 0.06 * k), P(0.88, s_ * 0.42, 1.8 - 0.1 * k)], 0.018 * sc, "Hall_Ink", sides=4)
        b.sphere(P(0.55, s_ * 0.55, 1.05), (0.42 * sc, 0.14 * sc, 0.2 * sc), "Hall_SealStone", seg=14, rings=9, rot=Matrix.Rotation(-0.6 * s_, 3, "Z"))   # 앞지느러미
        b.sphere(P(-0.95, s_ * 0.28, 0.15), (0.35 * sc, 0.16 * sc, 0.08 * sc), "Hall_SealStone", seg=12, rings=7)   # 꼬리
    b.sphere(P(0.9, 0, 1.86), (0.07 * sc, 0.1 * sc, 0.06 * sc), "Hall_Ink", seg=10, rings=6)                 # 코
    b.sphere(P(0.95, 0, 1.12), (0.28 * sc, 0.28 * sc, 0.28 * sc), "Hall_Pearl", seg=16, rings=10)              # 품에 안은 진주
    b.cyl(P(0.12, 0, 2.48), P(0.12, 0, 2.8), 0.32 * sc, "Hall_Gold", r1=0.4 * sc, seg=10)                     # 작은 왕관
    for k in range(5): a = k * 2 * math.pi / 5; b.sphere(P(0.12 + 0.36 * math.cos(a), 0.36 * math.sin(a), 2.84), (0.06 * sc,) * 3, "Hall_Pearl", seg=8, rings=5)
seal_statue(2.8, 4.2, 1.0, 1); seal_statue(2.8, -4.2, 1.0, 1)
seal_statue(DAIS0 + 2.5, 7.0, 1.55, -1); seal_statue(DAIS0 + 2.5, -7.0, 1.55, -1)

# ── 거대한 왕좌 ─────────────────────────────────────────────────────
TX, TZ = THRONE_X, TOP
b.box(TX, 0, TZ + 0.35, 3.8, 6.0, 0.7, "Hall_Gold")                               # 받침
b.box(TX, 0, TZ + 1.35, 3.2, 4.6, 1.3, "Hall_Gold")                                # 앉는 곳
b.box(TX - 0.1, 0, TZ + 2.12, 3.0, 4.2, 0.35, "Hall_RedVelvet")                    # 붉은 방석
b.box(TX + 1.6, 0, TZ + 6.6, 0.9, 5.4, 10.0, "Hall_Gold")                           # 등받이
b.box(TX + 1.12, 0, TZ + 6.8, 0.12, 4.2, 8.2, "Hall_Velvet")                        # 남색 벨벳
arch_plate("x", TX + 1.6, 0.0, 2.7, TZ + 11.5, TZ + 11.6, 3.4, 0.9, "Hall_Gold", 24)
for s in (-1, 1):
    b.box(TX + 0.2, s * 2.55, TZ + 2.6, 3.2, 0.5, 0.6, "Hall_Gold"); b.box(TX + 0.2, s * 2.55, TZ + 1.6, 2.6, 0.45, 1.4, "Hall_Gold")
    b.sphere((TX - 1.45, s * 2.55, TZ + 3.15), (0.45, 0.42, 0.4), "Hall_SealStone", seg=18, rings=12)   # 팔걸이 물범 머리
    for e in (-1, 1): b.sphere((TX - 1.83, s * 2.55 + e * 0.17, TZ + 3.22), (0.05, 0.05, 0.055), "Hall_Ink", seg=8, rings=5)
    b.sphere((TX - 1.88, s * 2.55, TZ + 3.1), (0.045, 0.07, 0.05), "Hall_Ink", seg=8, rings=5)
    b.cyl((TX + 1.6, s * 2.9, TZ), (TX + 1.6, s * 2.9, TZ + 13.2), 0.32, "Hall_Gold", seg=12)   # 양옆 기둥
    b.sphere((TX + 1.6, s * 2.9, TZ + 13.5), (0.4, 0.4, 0.4), "Hall_Pearl", seg=14, rings=9)
seal_face(TX + 1.0, 0, TZ + 12.4, 1.4, (-1, 0, 0))                                   # 꼭대기 물범 문장
for k in range(5):                                                                  # 진주 왕관
    yy = -1.0 + 0.5 * k; hh = 0.9 if k == 2 else 0.6
    b.cyl((TX + 0.95, yy, TZ + 13.7), (TX + 0.95, yy, TZ + 13.7 + hh), 0.16, "Hall_Gold", r1=0.03, seg=8)
    b.sphere((TX + 0.95, yy, TZ + 13.8 + hh), (0.13, 0.13, 0.13), "Hall_Pearl", seg=10, rings=6)
b.box(TX + 0.95, 0, TZ + 13.65, 0.3, 2.6, 0.3, "Hall_Gold")
for s in (-1, 1):                                                                   # 깃발(물범 문장)
    b.box(L - 0.25, s * 8.8, 10.5, 0.1, 2.6, 9.0, "Hall_Velvet"); b.box(L - 0.22, s * 8.8, 15.1, 0.15, 3.0, 0.25, "Hall_Gold")
    for k in range(3): b.box(L - 0.22, s * 8.8 - 0.9 + 0.9 * k, 5.8, 0.12, 0.6, 0.6, "Hall_Velvet", rot=0.0, tilt=Matrix.Rotation(math.pi / 4, 3, "X"))
    seal_face(L - 0.35, s * 8.8, 12.0, 0.8, (-1, 0, 0))

# ── 맨두 문장 · 곰곰이 문장(사용자 요청: 알현실 장식으로 하나씩) — 왕좌 계단 앞 양옆 벽의 큰 깃발 ──────
def wall_banner(x, side, cloth):
    """옆벽(y = side·W)에 걸린 깃발 — 금 막대 + 천 + 아래 술. 메달을 걸 자리(벽 앞 0.35m)를 돌려줌."""
    yw = side * (W - 0.05)
    b.box(x, yw, 10.2, 3.4, 0.12, 9.0, cloth); b.box(x, yw - side * 0.04, 14.8, 3.9, 0.2, 0.25, "Hall_Gold")
    for k in range(3): b.box(x - 1.1 + 1.1 * k, yw, 5.5, 0.75, 0.1, 0.75, cloth, tilt=Matrix.Rotation(math.pi / 4, 3, "Y"))
    for e in (-1, 1): b.sphere((x + e * 1.95, yw - side * 0.04, 14.8), (0.18, 0.18, 0.18), "Hall_Pearl", seg=10, rings=6)
    return yw - side * 0.3
def mandu_emblem(x, yw, side):
    """맨두 문장: 하얀 물범 얼굴 + 빨간 근위대 모자(금띠·별) + 뒤로 엇갈린 파란 야구방망이 두 개"""
    f = Vector((0, -side, 0)); up = Vector((0, 0, 1)); sd = f.cross(up).normalized(); c = Vector((x, yw, 10.4))
    def P(a, bb, out=0.0): return c + sd * a + up * bb + f * out
    for e in (-1, 1): b.tube([tuple(P(-e * 1.9, -1.9, -0.12)), tuple(P(e * 1.9, 1.9, -0.12))], 0.17, "Hall_BatBlue", sides=8)   # 엇갈린 방망이
    seal_face(c.x, c.y, c.z, 1.3, tuple(f))
    cap = P(0, 1.35, 0.12); b.cyl(tuple(cap - up * 0.35), tuple(cap + up * 0.45), 0.62, "Hall_Carpet", seg=16)   # 모자
    b.torus(tuple(cap - up * 0.25), 0.63, 0.06, "Hall_Gold", seg=20, sides=6); b.torus(tuple(cap + up * 0.4), 0.63, 0.05, "Hall_Gold", seg=20, sides=6)
    b.sphere(tuple(cap + f * 0.62), (0.16, 0.16, 0.16), "Hall_Gold", seg=10, rings=6)                                  # 금별(작은 금 방울)
    b.sphere(tuple(cap + up * 0.75 + sd * 0.2), (0.12, 0.12, 0.3), "Hall_Pearl", seg=10, rings=6)                       # 흰 깃털
def bear_emblem(x, yw, side):
    """곰곰이 문장: 졸린 갈색 곰 얼굴(둥근 귀 · 감긴 눈 · 밝은 주둥이 · 코) + 아래 사골곰뼈"""
    f = Vector((0, -side, 0)); up = Vector((0, 0, 1)); sd = f.cross(up).normalized(); c = Vector((x, yw, 10.4))
    def P(a, bb, out=0.0): return c + sd * a + up * bb + f * out
    b.cyl(tuple(P(0, 0, -0.05)), tuple(P(0, 0, 0.12)), 1.3, "Hall_BearBrown", seg=28); b.torus(tuple(P(0, 0, 0.1)), 1.3, 0.1, "Hall_Gold", axis=tuple(f), seg=28, sides=6)
    for e in (-1, 1):
        b.sphere(tuple(P(e * 1.0, 1.0, 0.0)), (0.45, 0.45, 0.2), "Hall_BearBrown", seg=14, rings=9); b.sphere(tuple(P(e * 1.0, 1.0, 0.12)), (0.25, 0.25, 0.1), "Hall_BearLight", seg=12, rings=8)
        b.tube([tuple(P(e * 0.62 + k * 0.1 - 0.2, 0.28 - 0.08 * math.sin(math.pi * k / 4) * 1.0, 0.2)) for k in range(5)], 0.045, "Hall_Ink", sides=4)   # 감긴 눈
    b.sphere(tuple(P(0, -0.35, 0.18)), (0.6, 0.45, 0.2), "Hall_BearLight", seg=16, rings=10); b.sphere(tuple(P(0, -0.2, 0.36)), (0.2, 0.14, 0.1), "Hall_Ink", seg=10, rings=6)
    b.tube([tuple(P(-0.2, -0.55, 0.35)), tuple(P(0, -0.65, 0.36)), tuple(P(0.2, -0.55, 0.35))], 0.035, "Hall_Ink", sides=4)
    b.tube([tuple(P(-1.0, -1.65, 0.1)), tuple(P(1.0, -1.65, 0.1))], 0.13, "Hall_SealStone", sides=8)                   # 사골곰뼈
    for e in (-1, 1):
        for g in (-1, 1): b.sphere(tuple(P(e * 1.1, -1.65 + g * 0.14, 0.1)), (0.18, 0.18, 0.18), "Hall_SealStone", seg=10, rings=6)
mandu_emblem(43.0, wall_banner(43.0, 1, "Hall_Velvet"), 1)
bear_emblem(43.0, wall_banner(43.0, -1, "Hall_RedVelvet"), -1)
hall = b.done("CastleHall")

# ── 충돌(보이지 않음) ──────────────────────────────────────────────
c = Builder(MATS, F)
c.box(L / 2, 0, -0.25, L + 2, W * 2 + 2, 0.5, "Hall_Stone")
for s in (1, -1): c.box(L / 2, s * (W + 0.5), 9.0, L + 2, 1.0, 18.0, "Hall_Stone")
for xe in (-0.5, L + 0.5): c.box(xe, 0, 9.0, 1.0, W * 2 + 2, 18.0, "Hall_Stone")
c.box(L / 2, 0, APEX + 0.6, L + 2, W * 2 + 2, 1.0, "Hall_Stone")
for xc in COLX:
    for s in (1, -1): c.box(xc, s * NW, 7.0, 2.0, 2.0, 14.0, "Hall_Stone")
for i in range(STEP_N):
    x0 = DAIS0 + i * STEP_D; z1 = (i + 1) * STEP_H; wid = 18.0 - i * 0.6; c.box((x0 + L) / 2, 0, z1 / 2, L - x0, wid, z1, "Hall_Stone")
c.box(TX + 0.4, 0, TZ + 1.2, 4.2, 6.0, 2.4, "Hall_Stone"); c.box(TX + 1.6, 0, TZ + 7.0, 1.0, 6.0, 14.0, "Hall_Stone")
for (x, y, sc) in ((2.8, 4.2, 1.0), (2.8, -4.2, 1.0), (DAIS0 + 2.5, 7.0, 1.55), (DAIS0 + 2.5, -7.0, 1.55)): c.box(x, y, 2.0 * sc, 2.0 * sc, 2.0 * sc, 4.0 * sc, "Hall_Stone")
col = c.done("CastleHall_Collision")

# ── 성 본채 앞 바깥 문(맵 좌표 — 팔각 성벽 바깥면) ─────────────────────
DOORT = 11.75; dp = CASTLE + DIR * DOORT; DZ = 8.54
Fd = Frame((dp.x, dp.y, DZ), (DIR.x, DIR.y, 0), (-DIR.y, DIR.x, 0)); d = Builder(MATS, Fd)
for s in (-1, 1): d.box(0.15, s * 1.0, 2.7, 0.3, 1.9, 5.4, "Hall_Wood")
for (pu, pz) in arch_pts(2.15, 5.4, 1.6, 12): d.box(0.2, pu, pz, 0.4, 0.3, 0.3, "Hall_Gold")
for s in (-1, 1): d.box(0.2, s * 2.15, 2.7, 0.4, 0.3, 5.4, "Hall_Gold")
d.box(0.25, 0, 7.6, 0.2, 0.2, 0.2, "Hall_Gold")
# 문 위 물범 얼굴 문장(바깥에서 보이게, 바깥쪽 = +x)
def face_on(builder, cx, cy, cz, r):
    p = Vector((cx, cy, cz)); builder.cyl((cx - 0.05, cy, cz), (cx + 0.05, cy, cz), r, "Hall_SealStone", seg=24); builder.torus((cx + 0.04, cy, cz), r, r * 0.08, "Hall_Gold", axis=(1, 0, 0), seg=24, sides=6)
    for s in (-1, 1): builder.sphere((cx + 0.06, cy + s * r * 0.36, cz + r * 0.08), (r * 0.09,) * 3, "Hall_Ink", seg=8, rings=5)
    builder.sphere((cx + 0.07, cy, cz - r * 0.12), (r * 0.07, r * 0.1, r * 0.07), "Hall_Ink", seg=8, rings=5)
face_on(d, 0.35, 0, 7.0, 0.55)
door = d.done("CastleDoor")

# ── 표시 ────────────────────────────────────────────────────────
marks = []
def mark(name, p):
    e = bpy.data.objects.new(name, None); e.empty_display_size = 0.5; e.location = p; bpy.context.collection.objects.link(e); marks.append(e); return e
mark("Mark_Arrive", F.P(4.5, 0, 0.06)); mark("Mark_ArriveLook", F.P(20, 0, 0.06)); mark("Mark_ExitDoor", F.P(1.4, 0, 0.06))
mark("Mark_Throne", F.P(THRONE_X - 3.0, 0, TOP + 0.06)); mark("Mark_ThroneSeat", F.P(THRONE_X - 0.6, 0, TOP + 2.3)); mark("Mark_HallCenter", F.P(L / 2, 0, 0.06))
for i, xc in enumerate(CHAND): mark(f"Mark_Chandelier_{i + 1}", F.P(xc, 0, 12.0))
mark("Mark_Window", F.P(L - 3.0, 0, 14.0))
mark("Mark_DoorFront", Fd.P(2.6, 0, 0.06)); mark("Mark_DoorLook", Fd.P(10.0, 0, 0.06))

allobj = [hall, col, door] + marks
bpy.ops.object.select_all(action="DESELECT")
for o in allobj: o.select_set(True)
os.makedirs(OUT, exist_ok=True); fbx = os.path.join(OUT, "CastleHall.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("EXPORTED", fbx, f"{os.path.getsize(fbx) / 1e6:.2f} MB", "hall verts", len(hall.data.vertices), "faces", len(hall.data.polygons))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(EXP, "CastleHall.blend"))

if "--preview" in sys.argv:
    col.hide_render = True; door.hide_render = True
    sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.samples = 40; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True
    sc.view_settings.view_transform = "AgX"; sc.render.resolution_x = 1000; sc.render.resolution_y = 1000
    w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.9, 0.88, 0.84, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.25; sc.world = w
    for xc in CHAND:
        Lp = bpy.data.objects.new("L", bpy.data.lights.new("L", "POINT")); Lp.data.energy = 2600; Lp.data.color = (1, 0.85, 0.65); Lp.data.shadow_soft_size = 2; Lp.location = F.P(xc, 0, 11.0); sc.collection.objects.link(Lp)
    for s in (1, -1):
        for xw in (12.0, 28.0, 44.0):
            Lw = bpy.data.objects.new("W", bpy.data.lights.new("W", "AREA")); Lw.data.energy = 600; Lw.data.size = 5; Lw.data.color = (0.75, 0.82, 1.0); Lw.location = F.P(xw, s * (W - 1.5), 9.0)
            Lw.rotation_euler = (F.P(xw, 0, 0) - Lw.location).to_track_quat("-Z", "Y").to_euler(); sc.collection.objects.link(Lw)
    Lt = bpy.data.objects.new("T", bpy.data.lights.new("T", "AREA")); Lt.data.energy = 1800; Lt.data.size = 6; Lt.data.color = (0.8, 0.85, 1.0); Lt.location = F.P(L - 4, 0, 16); Lt.rotation_euler = (F.P(THRONE_X - 4, 0, 2) - Lt.location).to_track_quat("-Z", "Y").to_euler(); sc.collection.objects.link(Lt)
    cam = bpy.data.objects.new("C", bpy.data.cameras.new("C")); sc.collection.objects.link(cam); sc.camera = cam
    def shot(name, loc, tgt, lens):
        cam.location = loc; cam.data.lens = lens; cam.rotation_euler = (tgt - loc).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(PRE, name); bpy.ops.render.render(write_still=True)
    shot("castle_hall_entrance.png", F.P(1.5, 0, 7.5), F.P(L, 0, 6.0), 22)
    shot("castle_hall_throne.png", F.P(38, -3.5, 4.0), F.P(THRONE_X, 0, 7.0), 28)
    shot("castle_hall_aisle.png", F.P(10, -11.5, 2.2), F.P(30, 2, 9.0), 20)
    shot("castle_hall_emblems.png", F.P(36, 0, 5.0), F.P(43.0, 0, 9.5), 16)
