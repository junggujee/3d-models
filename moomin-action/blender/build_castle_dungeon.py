"""하프프 왕국 구래성 — 지하감옥 + 광산 노역장(사용자 참고 그림 2장: 2층 석조 감옥 홀 · 나무 비계 광산).
  · 감옥 동(뒤쪽 벽): 아치 감방 2층(아래 6칸 · 위 6칸) + 위층 발코니 통로 · 난간 · 돌계단 · 쇠사슬 · 왕관 문장 깃발 · 횃불
  · 감방 철창 밖 = 광산 노역장(동굴): 울퉁불퉁 바위벽 · 흙바닥 · 레일 + 광차 2(광석 가득) · 도르래 탑 + 쇠사슬 양동이
    · 나무 비계(5m 높이 통로 · 기둥 · 가새 · 계단) · 기둥 등불 · 광석 더미 · 나무 상자 · 곡괭이 문장(엇갈린 곡괭이) 깃발
  · 가운데 둥근 쇠고리 샹들리에(쇠사슬) · 천장 둥근 창살 채광창(달빛 기둥) · 바닥 배수 창살
  · 끝: 바위 아치 + 내리닫이 쇠창살 문(DungeonExitGate — 올라감) · 간수 초소(책상 · 장부 · 열쇠 꾸러미 · 등불) · 문 밖 계단
  · 캘 수 있는 바위 자리 Mark_Rock_1..16 + 본 바위 MineRock_Template(광석 박힌 바위 — Unity 가 자리마다 복제)
자리: 성 위 하늘(맵 좌표 z = FLOOR) — 알현실과 떨어진 닫힌 실내.
표시: Mark_CellArrive · Mark_CellLook · Mark_CellDoorFront · Mark_Jailer · Mark_JailerLook · Mark_ExitGate · Mark_ExitLook · Mark_DungeonCenter
      · Mark_Torch_* · Mark_Lantern_* · Mark_Moon · Mark_Rock_*
실행: <bpy 파이썬> blender/build_castle_dungeon.py [--preview]   →  unity/Assets/Resources/World/CastleDungeon.fbx"""
import sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bpy, bmesh
from mathutils import Vector, Matrix, noise
from meshkit import Frame, Builder
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World"); PRE = os.path.join(ROOT, "previews")
reset_scene(); random.seed(5)
CASTLE = Vector((-43.6, 27.3)); FLOOR = 230.0
O = Vector((CASTLE.x - 22.0, CASTLE.y + 60.0, FLOOR)); F = Frame(O, (1, 0, 0), (0, 1, 0))
def M_(n, c, rough=0.85, metal=0.0): m = mat(n, c, rough=rough, metal=metal); m.diffuse_color = (*c, 1); return m
STONE = M_("Dun_Stone", (0.46, 0.43, 0.42)); STONED = M_("Dun_StoneDark", (0.3, 0.28, 0.28)); IRON = M_("Dun_Iron", (0.16, 0.15, 0.17), 0.45, 0.8)
WOODM = M_("Dun_Wood", (0.42, 0.26, 0.13)); WOODD = M_("Dun_WoodDark", (0.28, 0.17, 0.09)); ROCK = M_("Dun_Rock", (0.56, 0.36, 0.24), 0.95); DIRT = M_("Dun_Dirt", (0.44, 0.3, 0.2), 1.0)
FIRE = mat("Dun_FlameGlow", (1.0, 0.55, 0.18), emit=(1.0, 0.5, 0.15), emit_strength=8.0); LANT = mat("Dun_LanternGlow", (1.0, 0.78, 0.4), emit=(1.0, 0.72, 0.35), emit_strength=6.0)
MOON = mat("Dun_SunsetGlow", (1.0, 0.55, 0.32), emit=(1.0, 0.5, 0.28), emit_strength=5.0)   # 쇠창살 사이로 드는 노을빛
BAN = M_("Dun_Banner", (0.5, 0.1, 0.12), 0.9); GOLD = M_("Dun_Gold", (0.9, 0.7, 0.32), 0.35, 0.9)
ORE_CU = M_("Dun_OreCopper", (0.85, 0.45, 0.2), 0.4, 0.6); ORE_AG = M_("Dun_OreSilver", (0.85, 0.87, 0.92), 0.25, 0.9); ORE_AU = M_("Dun_OreGold", (1.0, 0.78, 0.25), 0.25, 1.0)
SAPH = mat("Dun_SapphireGlow", (0.2, 0.45, 1.0), emit=(0.2, 0.45, 1.0), emit_strength=2.0); STRAW = M_("Dun_Straw", (0.86, 0.72, 0.36), 1.0)
MATS = [STONE, STONED, IRON, WOODM, WOODD, ROCK, DIRT, FIRE, LANT, MOON, BAN, GOLD, ORE_CU, ORE_AG, ORE_AU, SAPH, STRAW]
b = Builder(MATS, F)
L, YB, YF = 46.0, 12.0, -14.0        # 길이 · 감옥 동 벽(+y) · 동굴 앞쪽 벽(-y)
H1, H2 = 4.4, 9.4                    # 1층 · 2층 감방 천장 높이
def rock(c, r, mi="Dun_Rock", seed=0, flat=0.0, sub=2):
    bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0)
    for v in bm.verts:
        n = noise.noise(v.co * 1.7 + Vector((seed, seed * 0.3, 0))); v.co = v.co * (1 + 0.28 * n)
        v.co = Vector((v.co.x * r[0], v.co.y * r[1], v.co.z * r[2] * (1 - flat * (v.co.z < 0))))
    bmesh.ops.transform(bm, matrix=Matrix.Translation(F.P(*c)), verts=bm.verts[:])
    for f in bm.faces: f.material_index = b.M(mi); f.smooth = False
    b.merge(bm)
# ── 바닥: 감옥 쪽 돌판 · 노역장 흙
for i in range(int(L)):
    for j in range(int(YB) - 4, int(YB)):
        b.box(i + 0.5, j + 0.5, -0.12 + random.uniform(-0.02, 0.02), 0.96, 0.96, 0.25, random.choice(["Dun_Stone", "Dun_StoneDark"]))
b.box(L / 2, (YF + YB - 4) / 2, -0.15, L, YB - 4 - YF, 0.3, "Dun_Dirt")
for k in range(6): b.box(12 + k * 0.4, 6.0, 0.02, 0.1, 2.2, 0.05, "Dun_Iron")                     # 배수 창살
for k in range(5): b.box(13.0, 5.1 + k * 0.45, 0.02, 2.2, 0.1, 0.05, "Dun_Iron")
# ── 감옥 동: 2층 아치 감방 6 × 2 + 발코니
CW, CD = 6.0, 4.0; CELLX = [4.0 + i * 6.6 for i in range(6)]          # 감방 왼쪽 x
b.box(L / 2, YB + CD + 0.5, H2 / 2 + 1, L, 1.0, H2 + 2, "Dun_Stone")                                  # 뒤벽
for z0, z1 in ((0.0, H1), (H1 + 0.5, H2)):
    for x0 in CELLX + [CELLX[-1] + CW + 0.6]:
        b.box(x0 - 0.3, YB + CD / 2, (z0 + z1) / 2, 0.6, CD, z1 - z0, "Dun_Stone")                     # 칸막이
    b.box(L / 2, YB + CD / 2, z1 + 0.25, L, CD + 0.4, 0.5, "Dun_StoneDark")                             # 층 바닥/천장
for x0 in CELLX:
    for z0, z1 in ((0.0, H1), (H1 + 0.5, H2)):
        xm = x0 + CW / 2; spring = z1 - 1.4
        for s in (-1, 1): b.box(xm + s * (CW / 2 - 0.25), YB - 0.1, (z0 + spring) / 2, 0.5, 0.9, spring - z0, "Dun_Stone")   # 아치 기둥
        for k in range(13):                                                                              # 아치 돌
            a = math.pi * k / 12; ax, az = xm + (CW / 2 - 0.25) * math.cos(a), spring + 1.2 * math.sin(a)
            b.box(ax, YB - 0.1, az, 0.55, 0.9, 0.5, "Dun_Stone", tilt=Matrix.Rotation(-(a - math.pi / 2), 3, "Y"))
        b.box(xm, YB - 0.1, z1 - 0.1, CW, 0.9, 0.3, "Dun_Stone")
        if x0 == CELLX[1] and z0 == 0.0: continue                                                       # 민미 감방 문은 따로(움직임)
        for k in range(13):                                                                              # 철창(아치 안)
            xx = x0 + 0.6 + (CW - 1.2) * k / 12; top = spring + 1.1 * math.sin(math.acos(max(-1, min(1, (xx - xm) / (CW / 2 - 0.5))))) if abs(xx - xm) < CW / 2 - 0.5 else spring
            b.cyl((xx, YB - 0.1, z0), (xx, YB - 0.1, top), 0.04, "Dun_Iron", seg=6)
        for zz in (z0 + 0.2, z0 + 2.0): b.box(xm, YB - 0.1, zz, CW - 1.0, 0.08, 0.1, "Dun_Iron")
    b.sphere((x0 + 1.2, YB + CD - 0.6, 0.12), (0.9, 0.6, 0.15), "Dun_Straw", seg=10, rings=6)
b.box(L / 2, YB - 1.4, H1 + 0.3, L - 2, 2.2, 0.35, "Dun_Wood")                                           # 2층 발코니
for k in range(int(L / 1.2)): b.cyl((1.4 + k * 1.2, YB - 2.45, H1 + 0.5), (1.4 + k * 1.2, YB - 2.45, H1 + 1.6), 0.05, "Dun_Iron", seg=6)
b.box(L / 2, YB - 2.45, H1 + 1.6, L - 2, 0.1, 0.1, "Dun_Iron")
for x in range(3, int(L), 6): b.box(x, YB - 2.3, H1 - 0.2, 0.25, 0.25, 1.0, "Dun_WoodDark", tilt=Matrix.Rotation(0.6, 3, "X"))   # 받침
for i in range(11): b.box(L - 3.0 - i * 0.45, YB - 3.2, (i + 1) * 0.4 / 2, 0.45, 1.6, (i + 1) * 0.4, "Dun_Stone")   # 2층 돌계단
for x in (8.0, 20.0, 32.0): b.box(x, YB - 0.62, H2 - 1.2, 1.4, 0.08, 3.2, "Dun_Banner"); b.box(x, YB - 0.66, H2 - 0.6, 0.6, 0.06, 0.45, "Dun_Gold")   # 왕관 문장 깃발
for x in (CELLX[i] - 0.3 for i in range(6)):
    for zz in (2.6, H1 + 2.8): b.box(x, YB - 0.62, zz, 0.12, 0.25, 0.4, "Dun_Iron"); b.sphere((x, YB - 0.85, zz + 0.35), (0.12, 0.12, 0.2), "Dun_FlameGlow", seg=10, rings=6)
for k in range(10):                                                                                     # 쇠사슬(위에서 늘어짐)
    pts = [(4 + k * 4.4, YB - 0.7 - 0.0, H2 + 1.6 - 0.3 * j + 0.15 * math.sin(j)) for j in range(6)]
    if k % 3 == 0: b.tube(pts, 0.06, "Dun_Iron", sides=5)
# ── 노역장 동굴: 바위벽 · 천장
for k in range(36):
    t = k / 35; x = t * L
    rock((x, YF - 0.5 + random.uniform(-0.8, 0.6), 4 + random.uniform(-1, 2)), (3.2, 2.2, 6 + random.uniform(0, 3)), seed=k)
for k in range(14):
    yy = YF + (YB - YF) * k / 13
    rock((-1.5 + random.uniform(-0.5, 0.5), yy, 4.5), (2.2, 2.8, 7), seed=k + 50); rock((L + 1.5, yy, 4.5), (2.2, 2.8, 7), seed=k + 80)
SKY = (23.0, -1.5); SKW, SKD = 26.0, 9.0
for k in range(60):
    rx_, ry_ = random.uniform(0, L), random.uniform(YF, YB)
    if abs(rx_ - SKY[0]) < SKW / 2 + 3.5 and abs(ry_ - SKY[1]) < SKD / 2 + 3.5: continue                   # 노을 창살 틈 자리는 비움
    rock((rx_, ry_, H2 + 3.5 + random.uniform(0, 2)), (5, 5, 2.2), seed=k + 120)
b.box(L / 2, (YF + YB) / 2, H2 + 6.6, L + 6, YB - YF + 6, 1.0, "Dun_StoneDark")
# 노역장 위 긴 창살 틈(노을빛) — SKY · SKW · SKD 는 천장 바위 앞에서 정함
for s_ in (-1, 1):
    b.box(SKY[0], SKY[1] + s_ * (SKD / 2 + 0.6), H2 + 4.6, SKW + 2.4, 1.2, 1.2, "Dun_Stone"); b.box(SKY[0] + s_ * (SKW / 2 + 0.6), SKY[1], H2 + 4.6, 1.2, SKD + 2.4, 1.2, "Dun_Stone")
for k in range(int(SKW / 0.9) + 1): b.box(SKY[0] - SKW / 2 + k * 0.9, SKY[1], H2 + 4.5, 0.12, SKD, 0.16, "Dun_Iron")
for k in range(int(SKD / 1.5) + 1): b.box(SKY[0], SKY[1] - SKD / 2 + k * 1.5, H2 + 4.45, SKW, 0.12, 0.16, "Dun_Iron")
b.box(SKY[0], SKY[1], H2 + 6.0, SKW, SKD, 0.1, "Dun_SunsetGlow")
for k in range(12):                                                                                         # 창살 틈 둘레 바위 턱
    a_ = 2 * math.pi * k / 12; rock((SKY[0] + (SKW / 2 + 2.5) * math.cos(a_), SKY[1] + (SKD / 2 + 2.5) * math.sin(a_), H2 + 4.8), (3.2, 2.6, 1.4), seed=k + 300)
# 가운데 쇠고리 샹들리에
CH = (22.0, 2.0, 7.2)
for k in range(4):
    a = k * math.pi / 2; b.tube([(CH[0] + 1.8 * math.cos(a), CH[1] + 1.8 * math.sin(a), CH[2]), (CH[0], CH[1], CH[2] + 2.0)], 0.04, "Dun_Iron", sides=4)
b.tube([(CH[0], CH[1], CH[2] + 2.0), (CH[0], CH[1], H2 + 4.0)], 0.06, "Dun_Iron", sides=5)
b.torus(CH, 1.8, 0.12, "Dun_Iron", seg=28, sides=6)
for k in range(8):
    a = 2 * math.pi * k / 8; px, py = CH[0] + 1.8 * math.cos(a), CH[1] + 1.8 * math.sin(a)
    b.cyl((px, py, CH[2]), (px, py, CH[2] + 0.35), 0.09, "Dun_Iron", seg=8); b.sphere((px, py, CH[2] + 0.5), (0.09, 0.09, 0.18), "Dun_FlameGlow", seg=8, rings=5)
# 레일 + 광차
RY = -5.0
for s in (-0.55, 0.55): b.box(L / 2 - 2, RY + s, 0.08, L - 6, 0.1, 0.12, "Dun_Iron")
for x in range(2, int(L) - 4): b.box(x, RY, 0.03, 0.3, 1.6, 0.08, "Dun_WoodDark")
def cart(x):
    b.box(x, RY, 0.75, 1.8, 1.2, 0.9, "Dun_Wood"); b.box(x, RY, 1.22, 1.9, 1.3, 0.08, "Dun_WoodDark")
    for dx in (-0.6, 0.6):
        for s in (-1, 1): b.cyl((x + dx, RY + s * 0.62, 0.28), (x + dx, RY + s * 0.7, 0.28), 0.25, "Dun_Iron", seg=12)
    for k in range(9): rock((x - 0.6 + 0.4 * (k % 4), RY - 0.3 + 0.3 * (k // 4), 1.3), (0.3, 0.3, 0.22), "Dun_OreCopper" if k % 4 == 1 else "Dun_Rock", seed=x + k, sub=1)
cart(14.0); cart(31.0)
# 나무 비계(앞쪽 바위벽을 따라) + 계단 + 등불 기둥
SY = YF + 3.0
for x in range(4, int(L) - 2, 4):
    for y in (SY - 1.2, SY + 1.2): b.box(x, y, 2.6, 0.35, 0.35, 5.2, "Dun_Wood")
    b.box(x, SY, 2.0, 0.2, 2.6, 0.2, "Dun_WoodDark", tilt=Matrix.Rotation(0.75, 3, "X"))
b.box(L / 2 + 1, SY, 5.2, L - 6, 2.8, 0.25, "Dun_Wood")
for x in range(4, int(L) - 2, 1): b.box(x, SY + 1.45, 5.75, 0.08, 0.08, 1.0, "Dun_WoodDark")
b.box(L / 2 + 1, SY + 1.45, 6.25, L - 6, 0.12, 0.12, "Dun_Wood")
for i in range(12): b.box(5.0 + i * 0.45, SY + 2.2, (i + 1) * 0.43 / 2, 0.45, 1.4, (i + 1) * 0.43, "Dun_WoodDark")
LANTS = [(6.0, -3.0), (16.0, 1.5), (26.0, -2.5), (36.0, 1.5), (42.0, -6.0), (10.0, SY + 1.5), (24.0, SY + 1.5), (38.0, SY + 1.5)]
for (lx, ly) in LANTS:
    top = 6.8 if ly < SY + 2 else 3.0
    if ly >= SY + 2 or ly > SY + 1.6: b.box(lx, ly, top / 2, 0.22, 0.22, top, "Dun_Wood")
    b.box(lx + 0.35, ly, top - 0.2, 0.7, 0.1, 0.1, "Dun_Wood"); b.box(lx + 0.6, ly, top - 0.55, 0.3, 0.3, 0.42, "Dun_LanternGlow"); b.box(lx + 0.6, ly, top - 0.3, 0.36, 0.36, 0.06, "Dun_Iron")
# 도르래 탑 + 양동이
HX, HY = 38.0, -9.0
for dx in (-1.2, 1.2):
    for dy in (-1.2, 1.2): b.box(HX + dx, HY + dy, 4.5, 0.35, 0.35, 9.0, "Dun_WoodDark")
for zz in (3.0, 6.0, 9.0):
    b.box(HX, HY - 1.2, zz, 2.6, 0.2, 0.2, "Dun_Wood"); b.box(HX, HY + 1.2, zz, 2.6, 0.2, 0.2, "Dun_Wood"); b.box(HX - 1.2, HY, zz, 0.2, 2.6, 0.2, "Dun_Wood"); b.box(HX + 1.2, HY, zz, 0.2, 2.6, 0.2, "Dun_Wood")
b.box(HX - 2.6, HY, 9.2, 5.5, 0.3, 0.3, "Dun_Wood"); b.cyl((HX, HY - 0.3, 9.2), (HX, HY + 0.3, 9.2), 0.5, "Dun_Iron", seg=16)
b.tube([(HX - 5.0, HY, 9.0), (HX - 5.0, HY, 5.2)], 0.05, "Dun_Iron", sides=5)
b.cyl((HX - 5.0, HY, 4.0), (HX - 5.0, HY, 5.0), 0.6, "Dun_Wood", r1=0.75, seg=14); rock((HX - 5.0, HY, 5.0), (0.55, 0.55, 0.3), "Dun_OreCopper", seed=7, sub=1)
# 광석 더미 · 상자 · 곡괭이 깃발
for (px, py, sc) in ((3.0, -10.0, 1.6), (44.0, 6.5, 1.2), (28.0, -11.0, 1.4)): rock((px, py, 0.3), (sc, sc, sc * 0.6), seed=int(px), flat=0.6)
for (cx, cy) in ((8.0, -10.5), (9.2, -10.6), (8.5, -10.3), (34.0, 5.0), (35.1, 5.2)): b.box(cx, cy, 0.45, 0.9, 0.9, 0.9, "Dun_Wood"); b.box(cx, cy, 0.92, 0.95, 0.95, 0.06, "Dun_WoodDark")
for x in (12.0, 30.0):
    b.box(x, SY - 1.25, 4.0, 1.3, 0.06, 2.6, "Dun_Banner")
    for s in (-1, 1): b.box(x, SY - 1.3, 4.3, 0.12, 0.05, 1.4, "Dun_Gold", tilt=Matrix.Rotation(s * 0.7, 3, "Y")); b.box(x + s * 0.35, SY - 1.3, 4.85, 0.45, 0.05, 0.14, "Dun_Gold", tilt=Matrix.Rotation(s * 0.7, 3, "Y"))
# ── 출구: 바위 아치 + 간수 초소 + 문 밖 계단
EX = L - 0.6
for s in (-1, 1): b.box(EX, s * 2.6, 2.8, 1.2, 1.2, 5.6, "Dun_Stone")
b.box(EX, 0, 5.9, 1.2, 6.4, 0.8, "Dun_Stone"); b.box(EX + 3.0, 0, 3.0, 0.6, 4.0, 6.0, "Dun_StoneDark")
for i in range(5): b.box(EX + 0.9 + i * 0.4, 0, (i + 1) * 0.35 / 2, 0.4, 4.0, (i + 1) * 0.35, "Dun_Stone")
JX, JY = L - 4.5, -4.0
b.box(JX, JY, 0.8, 2.0, 1.0, 0.1, "Dun_Wood")
for dx in (-0.9, 0.9):
    for dy in (-0.4, 0.4): b.box(JX + dx, JY + dy, 0.4, 0.1, 0.1, 0.8, "Dun_WoodDark")
b.box(JX - 0.4, JY, 0.88, 0.6, 0.45, 0.06, "Dun_Straw"); b.box(JX + 0.5, JY, 0.95, 0.25, 0.25, 0.3, "Dun_LanternGlow"); b.torus((JX + 0.1, JY + 0.2, 0.87), 0.15, 0.02, "Dun_Gold", seg=12, sides=4)
hall = b.done("CastleDungeon")
# ── 움직이는 것: 민미 감방 문 · 출구 내리닫이 쇠창살
x0 = CELLX[1]; DOOR_X0, DOOR_X1 = x0 + 0.6, x0 + CW - 0.6
d = Builder(MATS, F)
for k in range(11): xx = DOOR_X0 + (DOOR_X1 - DOOR_X0) * k / 10; d.cyl((xx, YB - 0.1, 0.05), (xx, YB - 0.1, H1 - 1.5), 0.04, "Dun_Iron", seg=6)
for zz in (0.2, 1.4, H1 - 1.55): d.box((DOOR_X0 + DOOR_X1) / 2, YB - 0.1, zz, DOOR_X1 - DOOR_X0, 0.08, 0.1, "Dun_Iron")
door = d.done("DungeonCellDoor", origin=F.P(DOOR_X0, YB - 0.1, 0.0))
d2 = Builder(MATS, F); d2.box((DOOR_X0 + DOOR_X1) / 2, YB - 0.1, 1.5, DOOR_X1 - DOOR_X0, 0.15, 3.0, "Dun_Iron"); dc = d2.done("DungeonCellDoor_Collision", origin=F.P(DOOR_X0, YB - 0.1, 0.0))
dc.parent = door; dc.matrix_parent_inverse = door.matrix_world.inverted()
g = Builder(MATS, F)
for zz in (0.5, 2.5, 4.5): g.box(EX, 0, zz, 0.1, 4.2, 0.12, "Dun_Iron")
gate = g.done("DungeonExitGate", origin=F.P(EX, 0, 0))
g2 = Builder(MATS, F); g2.box(EX, 0, 2.8, 0.3, 4.2, 5.6, "Dun_Iron"); gc = g2.done("DungeonExitGate_Collision", origin=F.P(EX, 0, 0))
gc.parent = gate; gc.matrix_parent_inverse = gate.matrix_world.inverted()
# 캘 바위 본(원점 = 바닥 가운데 · Unity 가 Mark_Rock_* 자리마다 복제)
r_ = Builder(MATS, Frame(Vector((0, 0, 0)), (1, 0, 0), (0, 1, 0))); rb = r_
bm = bmesh.new(); bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
for v in bm.verts:
    n = noise.noise(v.co * 1.6 + Vector((3, 1, 0))); v.co = v.co * (1 + 0.3 * n); v.co = Vector((v.co.x * 0.85, v.co.y * 0.75, v.co.z * 0.6 + 0.45))
for f in bm.faces: f.material_index = r_.M("Dun_Rock")
r_.merge(bm)
for k, mi in enumerate(["Dun_OreCopper", "Dun_OreCopper", "Dun_OreSilver", "Dun_OreGold", "Dun_SapphireGlow", "Dun_OreCopper"]):
    a = k * 1.1; r_.sphere((0.62 * math.cos(a), 0.52 * math.sin(a), 0.5 + 0.2 * math.sin(k * 2.3)), (0.13, 0.13, 0.1), mi, seg=6, rings=4)
template = r_.done("MineRock_Template"); template.location = F.P(-30, 0, -20)
# ── 충돌
c = Builder(MATS, F)
c.box(L / 2, (YF + YB) / 2 + 2, -0.15, L + 4, YB - YF + 12, 0.3, "Dun_Stone")
c.box(L / 2, YB + CD + 0.5, H2 / 2, L + 2, 1.0, H2 + 4, "Dun_Stone"); c.box(L / 2, YF - 1.0, 6, L + 2, 1.0, 14, "Dun_Stone")
c.box(-1.0, (YF + YB) / 2 + 2, 6, 1.0, YB - YF + 12, 14, "Dun_Stone"); c.box(L + 4.0, 0, 6, 1.0, 10, 14, "Dun_Stone")
for s in (-1, 1): c.box(EX, s * 4.5, 6, 1.2, 5.0, 14, "Dun_Stone")
c.box(L / 2, (YF + YB) / 2, H2 + 5, L + 4, YB - YF + 4, 1.0, "Dun_Stone")
for x0 in CELLX + [CELLX[-1] + CW + 0.6]: c.box(x0 - 0.3, YB + CD / 2, H2 / 2, 0.6, CD, H2, "Dun_Stone")
for x0 in CELLX:
    if x0 == CELLX[1]:
        for xs, xe in ((x0, DOOR_X0), (DOOR_X1, x0 + CW)): c.box((xs + xe) / 2, YB - 0.1, H1 / 2, xe - xs, 0.4, H1, "Dun_Stone")
    else: c.box(x0 + CW / 2, YB - 0.1, H1 / 2, CW, 0.4, H1, "Dun_Stone")
c.box(L / 2, YB + CD / 2, H1 + 0.25, L, CD + 0.4, 0.5, "Dun_Stone"); c.box(L / 2, YB - 1.4, H1 + 0.3, L - 2, 2.2, 0.35, "Dun_Stone")
for i in range(11): c.box(L - 3.0 - i * 0.45, YB - 3.2, (i + 1) * 0.4 / 2, 0.45, 1.6, (i + 1) * 0.4, "Dun_Stone")
c.box(L / 2 + 1, SY, 5.2, L - 6, 2.8, 0.25, "Dun_Stone")
for i in range(12): c.box(5.0 + i * 0.45, SY + 2.2, (i + 1) * 0.43 / 2, 0.45, 1.4, (i + 1) * 0.43, "Dun_Stone")
for x in (14.0, 31.0): c.box(x, RY, 0.7, 1.9, 1.3, 1.4, "Dun_Stone")
c.box(HX - 1.2 + 1.2, HY, 4.5, 2.8, 2.8, 9.0, "Dun_Stone"); c.box(JX, JY, 0.5, 2.0, 1.0, 1.0, "Dun_Stone")
col = c.done("CastleDungeon_Collision")
# ── 표시
marks = []
def mark(n, p): e = bpy.data.objects.new(n, None); e.location = p; bpy.context.collection.objects.link(e); marks.append(e)
xm = CELLX[1] + CW / 2
mark("Mark_CellArrive", F.P(xm, YB + 2.0, 0.06)); mark("Mark_CellLook", F.P(xm, YB - 6, 0.06)); mark("Mark_CellDoorFront", F.P(xm, YB - 1.2, 0.06))
mark("Mark_Jailer", F.P(JX - 0.2, JY - 1.3, 0.06)); mark("Mark_JailerLook", F.P(JX - 8, JY - 1.3, 0.06))
mark("Mark_ExitGate", F.P(EX - 1.6, 0, 0.06)); mark("Mark_ExitLook", F.P(EX + 4, 0, 0.06)); mark("Mark_DungeonCenter", F.P(L / 2, 0, 0.06)); mark("Mark_Moon", F.P(SKY[0], SKY[1], H2 + 3.0))
for i, sx in enumerate((-8.0, 0.0, 8.0)): mark(f"Mark_Sunset_{i + 1}", F.P(SKY[0] + sx, SKY[1], H2 + 4.0))
ci = 0
for x0 in CELLX:
    for zf in (0.0, H1 + 0.5):
        if x0 == CELLX[1] and zf == 0.0: continue
        ci += 1; mark(f"Mark_Cell_{ci}", F.P(x0 + CW / 2, YB + CD / 2, zf + 0.06))
mark("Mark_Chandelier", F.P(CH[0], CH[1], CH[2] + 0.4))
for i, x in enumerate(CELLX): mark(f"Mark_Torch_{i + 1}", F.P(x - 0.3, YB - 1.2, 3.0))
for i, (lx, ly) in enumerate(LANTS): mark(f"Mark_Lantern_{i + 1}", F.P(lx + 0.6, ly, (6.8 if ly < SY + 2 else 3.0) - 0.6))
ROCKS = [(5, -8), (9, -1), (11, 3), (17, -9), (19, 2.5), (23, -8.5), (25, 4), (28, -7), (29, 0.5), (33, -2), (34, 3), (40, -2), (41, 3.5), (7, 3.5), (21, -1.5), (36, -11)]
for i, (rx, ry) in enumerate(ROCKS): mark(f"Mark_Rock_{i + 1}", F.P(rx, ry, 0.02))
allobj = [hall, door, dc, gate, gc, template, col] + marks
bpy.ops.object.select_all(action="DESELECT")
for o in allobj: o.select_set(True)
fbx = os.path.join(OUT, "CastleDungeon.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("EXPORTED", fbx, f"{os.path.getsize(fbx) / 1e6:.2f} MB", "faces", len(hall.data.polygons))
if "--preview" in sys.argv:
    for o in (col, dc, gc): o.hide_render = True
    for (rx, ry) in ROCKS:                                                                                      # 미리보기용 바위 복제
        cp_ = template.copy(); cp_.location = F.P(rx, ry, 0.02); bpy.context.collection.objects.link(cp_)
    sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.samples = 32; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True; sc.view_settings.view_transform = "AgX"
    sc.render.resolution_x = 1100; sc.render.resolution_y = 620
    w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 0.04; sc.world = w
    def pl(p, e, c, r=0.2):
        Lp = bpy.data.objects.new("L", bpy.data.lights.new("L", "POINT")); Lp.data.energy = e; Lp.data.color = c; Lp.data.shadow_soft_size = r; Lp.location = p; sc.collection.objects.link(Lp)
    for x in CELLX: pl(F.P(x - 0.3, YB - 1.2, 3.0), 260, (1, 0.6, 0.3))
    for (lx, ly) in LANTS: pl(F.P(lx + 0.6, ly - 0.4, (6.8 if ly < SY + 2 else 3.0) - 0.6), 380, (1, 0.72, 0.4))
    pl(F.P(CH[0], CH[1], CH[2] + 0.3), 900, (1, 0.65, 0.35), 1.0)
    for sx in (-8.0, 0.0, 8.0):
        sp = bpy.data.objects.new("S", bpy.data.lights.new("S", "SPOT")); sp.data.energy = 14000; sp.data.color = (1.0, 0.55, 0.3); sp.data.spot_size = 0.9; sp.data.spot_blend = 0.6
        sp.location = F.P(SKY[0] + sx, SKY[1], H2 + 5.2); sp.rotation_euler = (0.35, 0.0, 0.0); sc.collection.objects.link(sp)
    sc.render.use_motion_blur = False
    cam = bpy.data.objects.new("C", bpy.data.cameras.new("C")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 18
    def shot(n, loc, tgt): cam.location = loc; cam.rotation_euler = (tgt - loc).to_track_quat("-Z", "Y").to_euler(); sc.render.filepath = os.path.join(PRE, n); bpy.ops.render.render(write_still=True)
    shot("dungeon_sunset.png", F.P(12, 2.5, 1.7), F.P(25, -2, H2 + 4.5)); shot("dungeon_prison.png", F.P(22, -8, 2.8), F.P(20, YB, 4.5)); shot("dungeon_mine.png", F.P(2.0, 6.0, 3.2), F.P(30, -6, 2.5)); shot("dungeon_cell.png", F.P(xm, YB + 3.2, 2.0), F.P(xm, YB - 6, 1.6))
