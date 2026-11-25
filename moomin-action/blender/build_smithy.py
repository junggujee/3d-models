"""마을 대장간(맵 좌표 그대로 — City_*.fbx 와 같은 설정으로 내보내 World 아래에 그대로 놓으면 맞음).

마을 동쪽에 홀로 선 작은 집(Blender 약 (70, -56), 지붕 높이 13m)을 헐고 그 자리에 대장간을 세운다.
  · --carve : City_TownE.fbx · City_Collision.fbx 에서 그 집 면을 지우고 다시 내보냄(집을 실제로 치움)
  · 기본    : 대장간 모델 → Resources/World/Smithy.fbx + 자리 정보 Smithy.json

생김새(참고 사진: 마인크래프트 대장간)
  · 돌 기단 + 앞마당(자갈) · 낮은 돌 난간
  · 1층 = 한쪽이 트인 작업장: 빨간 벽돌 화덕(불빛) · 모루 · 물통 · 작업대 · 숫돌 · 연장걸이 · 석탄 더미
  · 벽돌 굴뚝이 지붕을 뚫고 올라가 꼭대기에서 연기
  · 2층 = 통나무 받침으로 앞으로 내민 층(하얀 회벽 + 짙은 나무 뼈대 + 창 + 작은 발코니·꽃상자)
  · 짙은 슬레이트 박공지붕(계단식 가장자리 · 넓은 처마) · 등불 여러 개
표시(빈 물체): Mark_Smith(풍선고래 자리) · Mark_Anvil · Mark_Forge(불빛) · Mark_Smoke · Mark_Door · Mark_Sign · Mark_Yard
Smithy_Collision: 보이지 않는 충돌 상자(앞은 트여 있어 걸어 들어갈 수 있음).
실행: <bpy 파이썬> blender/build_smithy.py [--carve] [--preview]
"""
import sys, os, math, json
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bpy, bmesh
from mathutils import Vector, Matrix
from meshkit import Frame, Builder

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World"); CITY = os.path.join(ROOT, "unity", "Assets", "City")
EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
os.makedirs(OUT, exist_ok=True); os.makedirs(PRE, exist_ok=True)

SITE = Vector((69.95, -55.95)); GZ = 0.30                      # 헐어 낸 집 자리 · 바닥 높이
PLAZA = Vector((35.3, -30.7))
CARVE = (65.3, 74.6, -60.6, -51.3, -1.0, 15.5)                 # 지울 범위(x0, x1, y0, y1, z0, z1)
FRONT = (PLAZA - SITE).normalized()                            # 앞 = 광장 쪽
YAW = math.atan2(FRONT.y, FRONT.x)

# ── --carve: 도시 파일에서 그 집을 지움 ─────────────────────────────
if "--carve" in sys.argv:
    x0, x1, y0, y1, z0, z1 = CARVE
    for fname in ("City_TownE", "City_Collision"):
        path = os.path.join(CITY, fname + ".fbx")
        if not os.path.exists(path): print("없음:", path); continue
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=path, automatic_bone_orientation=True)
        removed = 0
        for ob in [o for o in bpy.data.objects if o.type == "MESH"]:
            me = ob.data; n = len(me.polygons)
            if n == 0: continue
            M = ob.matrix_world; bm = bmesh.new(); bm.from_mesh(me)
            kill = [f for f in bm.faces if (lambda c: x0 <= c.x <= x1 and y0 <= c.y <= y1 and z0 <= c.z <= z1)(M @ f.calc_center_median())]
            if kill:
                bmesh.ops.delete(bm, geom=kill, context="FACES"); removed += len(kill)
                bm.to_mesh(me); me.update()
            bm.free()
        objs = [o for o in bpy.data.objects if o.type in ("MESH", "EMPTY")]
        bpy.ops.object.select_all(action="DESELECT")
        for o in objs: o.select_set(True)
        bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                                 path_mode="RELATIVE", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
        print(f"헐었음 {fname}: 면 {removed:,}개 제거 → {os.path.getsize(path) / 1e6:.1f} MB")
    sys.exit(0)

reset_scene()
STONE = mat("Smithy_Stone", (0.56, 0.56, 0.58), rough=0.95)
STONE_D = mat("Smithy_StoneDark", (0.38, 0.38, 0.41), rough=0.95)
BRICK = mat("Smithy_Brick", (0.66, 0.33, 0.28), rough=0.9)
WOOD = mat("Smithy_Wood", (0.56, 0.36, 0.18), rough=0.85)
WOOD_D = mat("Smithy_WoodDark", (0.33, 0.2, 0.1), rough=0.85)
PLASTER = mat("Smithy_Plaster", (0.93, 0.92, 0.88), rough=1.0)
ROOF = mat("Smithy_Roof", (0.21, 0.22, 0.26), rough=0.8)
IRON = mat("Smithy_Iron", (0.17, 0.17, 0.19), rough=0.45, metal=0.8)
GLOW = mat("Smithy_Glow", (1.0, 0.72, 0.3), rough=0.4, emit=(1.0, 0.62, 0.22), emit_strength=6.0)
EMBER = mat("Smithy_Ember", (1.0, 0.42, 0.12), rough=0.6, emit=(1.0, 0.35, 0.08), emit_strength=9.0)
COAL = mat("Smithy_Coal", (0.09, 0.09, 0.1), rough=1.0)
GREEN = mat("Smithy_Leaf", (0.3, 0.55, 0.24), rough=1.0)
MATS = [STONE, STONE_D, BRICK, WOOD, WOOD_D, PLASTER, ROOF, IRON, GLOW, EMBER, COAL, GREEN]

F = Frame((SITE.x, SITE.y, GZ), (FRONT.x, FRONT.y, 0), (-FRONT.y, FRONT.x, 0))
b = Builder(MATS, F)
HW, HD = 4.0, 3.9                       # 집 반폭(좌우) · 반깊이(앞뒤)
F1, F2, EAVE = 3.45, 7.15, 7.35         # 1층 천장 · 2층 천장 · 처마 높이

# ── 기단 · 앞마당 ──────────────────────────────────────────
b.box(0, 0, -0.14, HD * 2 + 0.9, HW * 2 + 0.9, 0.56, "Smithy_Stone")                   # 돌 기단
b.box(HD + 1.9, 0, -0.1, 3.0, HW * 2 + 0.3, 0.42, "Smithy_StoneDark")                  # 앞마당(자갈 바닥)
for s in (1, -1):                                                                       # 앞마당 낮은 난간
    for k in range(5):
        b.box(HD + 0.7 + k * 0.72, s * (HW + 0.05), 0.42, 0.66, 0.34, 0.5, "Smithy_Stone")
for k in range(3): b.box(HD + 3.3 + k * 0.3, 0, -0.1 - k * 0.16, 0.34, 3.4, 0.34, "Smithy_Stone")   # 앞 계단

# ── 1층: 한쪽이 트인 작업장 ─────────────────────────────────
def wall(x, y, sx, sy, z0, z1, m):
    b.box(x, y, (z0 + z1) / 2, sx, sy, z1 - z0, m)
wall(-HD + 0.2, 0, 0.45, HW * 2, 0.14, F1, "Smithy_Stone")                              # 뒤 벽
wall(0.4, -HW + 0.2, HD * 2 - 1.0, 0.45, 0.14, F1, "Smithy_Stone")                      # 오른쪽 벽
wall(-HD + 1.6, HW - 0.2, 2.6, 0.45, 0.14, F1, "Smithy_Stone")                          # 왼쪽 뒤 벽(화덕 쪽)
for s in (1, -1):                                                                        # 기둥 4개
    for xx in (HD - 0.35, -0.2):
        b.box(xx, s * (HW - 0.35), (0.14 + F1) / 2, 0.42, 0.42, F1 - 0.14, "Smithy_Wood")
b.box(HD - 0.35, 0, F1 - 0.2, 0.42, HW * 2 - 0.3, 0.34, "Smithy_WoodDark")              # 앞 들보
b.box(-HD + 0.4, 0, F1 - 0.2, 0.42, HW * 2 - 0.3, 0.34, "Smithy_WoodDark")
b.box(HD - 1.4, HW - 1.25, 0.9, 0.5, 2.0, 1.1, "Smithy_Wood")                            # 앞 카운터(왼쪽)
b.box(HD - 1.4, HW - 1.25, 1.52, 0.66, 2.2, 0.16, "Smithy_WoodDark")

# 벽돌 화덕 + 굴뚝(지붕을 뚫고 위로)
CX, CY = -HD + 1.3, HW + 0.95          # 굴뚝은 왼쪽 바깥 벽에 붙임(밖에서 보임)
HX, HY = -HD + 1.3, HW - 0.9          # 화덕(불구멍)은 집 안쪽
b.box(CX, CY - 0.75, 1.15, 2.3, 3.4, 2.3, "Smithy_Brick")                                # 화덕 덩이(벽을 물고 밖으로)
b.box(HX + 0.2, HY - 0.25, 1.05, 1.3, 1.5, 1.5, "Smithy_Coal")                           # 불구멍(안쪽 어둡게)
b.box(HX + 0.25, HY - 0.3, 0.62, 1.5, 1.7, 0.42, "Smithy_Ember")                         # 잉걸불
b.box(HX + 0.25, HY - 0.3, 0.46, 1.7, 1.9, 0.26, "Smithy_Coal")
b.box(HX + 0.2, HY - 1.15, 2.15, 1.7, 0.3, 0.9, "Smithy_Brick")                          # 후드 앞턱
for zz in (3.4, 5.6, 7.8, 10.0):
    b.box(CX, CY, zz, 1.7, 1.7, 2.2, "Smithy_Brick")                                     # 굴뚝 기둥
b.box(CX, CY, 11.35, 2.1, 2.1, 0.5, "Smithy_Brick")                                      # 굴뚝 갓
b.box(CX, CY, 11.75, 1.7, 1.7, 0.35, "Smithy_StoneDark")
for s in (1, -1): b.box(CX + s * 1.0, CY, 10.6, 0.5, 2.1, 0.4, "Smithy_Brick")           # 굴뚝 턱

# 모루 · 물통 · 작업대 · 숫돌 · 석탄
AX, AY = HD - 2.3, HW - 2.9
b.cyl((AX, AY, 0.14), (AX, AY, 0.62), 0.42, "Smithy_Wood", seg=10)                       # 나무 그루터기
b.box(AX, AY, 0.74, 1.1, 0.5, 0.24, "Smithy_Iron")                                       # 모루 몸
b.box(AX, AY, 0.93, 0.7, 0.36, 0.16, "Smithy_Iron")
b.box(AX + 0.62, AY, 0.86, 0.5, 0.3, 0.2, "Smithy_Iron")
b.cyl((AX - 1.5, AY - 1.2, 0.14), (AX - 1.5, AY - 1.2, 0.86), 0.46, "Smithy_Wood", seg=12)   # 물통
b.cyl((AX - 1.5, AY - 1.2, 0.8), (AX - 1.5, AY - 1.2, 0.84), 0.42, "Smithy_StoneDark", seg=12)
b.box(-HD + 0.75, -HW + 1.5, 0.9, 0.9, 2.6, 0.12, "Smithy_Wood")                         # 뒤쪽 작업대
for yy in (-HW + 0.55, -HW + 2.45): b.box(-HD + 0.75, yy, 0.52, 0.7, 0.14, 0.76, "Smithy_Wood")
for k, yy in enumerate((-HW + 0.7, -HW + 1.2, -HW + 1.7, -HW + 2.2)):                    # 연장걸이
    b.box(-HD + 0.33, yy, 1.9 + 0.04 * k, 0.1, 0.1, 0.62, "Smithy_Iron")
b.box(-HD + 0.33, -HW + 1.45, 2.32, 0.12, 2.0, 0.1, "Smithy_WoodDark")
b.cyl((-0.1, -HW + 1.0, 0.72), (-0.1, -HW + 1.0, 0.86), 0.5, "Smithy_StoneDark", seg=14) # 숫돌
for s in (1, -1): b.box(-0.1, -HW + 1.0 + s * 0.55, 0.5, 0.16, 0.16, 0.72, "Smithy_Wood")
for k in range(7):                                                                        # 석탄 더미
    import random as _r
    b.box(-HD + 1.1 - 0.12 * k % 0.6, -HW + 3.2, 0.2 + 0.08 * (k % 3), 0.5, 0.5, 0.34, "Smithy_Coal", rot=0.4 * k)

# ── 2층: 앞으로 내민 층(회벽 + 나무 뼈대) ─────────────────────
JX, JW, JD = 0.55, HW + 0.5, HD + 0.55                                                   # 내민 폭
for s in (1, -1):                                                                         # 통나무 받침
    for xx in (HD - 0.4, 0.2, -HD + 0.8):
        b.cyl((xx, s * (HW - 0.2), F1 - 0.15), (xx, s * (HW + 0.45), F1 + 0.5), 0.17, "Smithy_Wood", seg=8)
for yy in (HW - 1.6, 0.0, -HW + 1.6):
    b.cyl((HD - 0.2, yy, F1 - 0.15), (HD + 0.5, yy, F1 + 0.5), 0.17, "Smithy_Wood", seg=8)
b.box(0.25, 0, F1 + 0.28, JD * 2, JW * 2, 0.4, "Smithy_Wood")                            # 2층 바닥
wall(0.25, 0, JD * 2, JW * 2, F1 + 0.48, F2, "Smithy_Plaster")                           # 회벽 덩이(겉면)
for s in (1, -1):                                                                         # 짙은 나무 뼈대(벽 바깥면에 덧댐)
    for yy in (JW - 0.3, JW - 2.7, 0.0):
        b.box(0.25 + s * (JD + 0.07), yy, (F1 + 0.5 + F2) / 2, 0.2, 0.36, F2 - F1 - 0.5, "Smithy_WoodDark")
    for xx in (JD - 0.4, 0.5, -JD + 1.0):
        b.box(xx + 0.25, s * (JW + 0.07), (F1 + 0.5 + F2) / 2, 0.36, 0.2, F2 - F1 - 0.5, "Smithy_WoodDark")
for s in (1, -1):                                                                         # 2층 허리띠 · 처마 밑 띠
    b.box(0.25, s * (JW + 0.07), F1 + 0.62, JD * 2 + 0.3, 0.24, 0.3, "Smithy_WoodDark")
    b.box(0.25, s * (JW + 0.07), F2 - 0.22, JD * 2 + 0.3, 0.24, 0.34, "Smithy_WoodDark")
    b.box(0.25 + s * (JD + 0.07), 0, F1 + 0.62, 0.24, JW * 2 + 0.3, 0.3, "Smithy_WoodDark")
    b.box(0.25 + s * (JD + 0.07), 0, F2 - 0.22, 0.24, JW * 2 + 0.3, 0.34, "Smithy_WoodDark")
for s in (1, -1):                                                                         # 2층 창(짙은 틀 + 어두운 유리)
    b.box(0.25 + JD + 0.06, s * 1.9, F1 + 1.75, 0.16, 1.3, 1.3, "Smithy_WoodDark")
    b.box(0.25 + JD + 0.14, s * 1.9, F1 + 1.75, 0.06, 1.05, 1.05, "Smithy_StoneDark")
    b.box(-1.4, s * (JW + 0.08), F1 + 1.75, 1.3, 0.16, 1.3, "Smithy_WoodDark")
    b.box(-1.4, s * (JW + 0.16), F1 + 1.75, 1.05, 0.06, 1.05, "Smithy_StoneDark")
BX = 0.25 + JD + 0.45
b.box(BX, 0, F1 + 0.95, 0.9, 2.2, 0.16, "Smithy_Wood")                                   # 작은 발코니
for k in range(8): b.box(BX + 0.36, -1.05 + k * 0.3, F1 + 1.3, 0.1, 0.1, 0.6, "Smithy_Wood")
b.box(BX + 0.36, 0, F1 + 1.62, 0.14, 2.2, 0.12, "Smithy_Wood")
b.box(BX + 0.1, 0.85, F1 + 1.12, 0.45, 0.6, 0.2, "Smithy_WoodDark")                      # 꽃상자
b.box(BX + 0.1, 0.85, F1 + 1.28, 0.38, 0.5, 0.16, "Smithy_Leaf")

# ── 박공지붕(계단식 가장자리 + 넓은 처마) ─────────────────────
PEAK = EAVE + 3.3
STEPS = 9
for i in range(STEPS):
    t = i / (STEPS - 1.0)
    hw = (JW + 0.75) * (1 - t) + 0.22                      # 좌우로 좁아짐
    z = EAVE + (PEAK - EAVE) * t
    b.box(0.25, 0, z + 0.2, JD * 2 + 0.9, hw * 2, 0.46, "Smithy_Roof")
    if i == 0:                                              # 처마 끝에만 나무 테두리
        for s in (1, -1): b.box(0.25, s * hw, z + 0.14, JD * 2 + 1.0, 0.16, 0.34, "Smithy_Wood")
        b.box(0.25 + JD + 0.48, 0, z + 0.14, 0.16, hw * 2 + 0.3, 0.34, "Smithy_Wood")
b.box(0.25, 0, PEAK + 0.42, JD * 2 + 1.0, 0.5, 0.3, "Smithy_WoodDark")                   # 용마루
for i in range(STEPS - 1):                                                                # 박공(앞뒤) 끝 나무 벽 — 지붕 모양대로 계단식
    t = i / (STEPS - 1.0); hw = (JW + 0.75) * (1 - t) + 0.22; z = EAVE + (PEAK - EAVE) * t
    dz = (PEAK - EAVE) / (STEPS - 1.0)
    for s in (1, -1):
        b.box(0.25 + s * (JD + 0.3), 0, z + 0.17 + dz / 2, 0.3, hw * 2 - 0.45, dz + 0.36, "Smithy_Wood")

# ── 등불 · 간판 ────────────────────────────────────────────
LAMPS = [(HD + 0.1, HW - 0.3, 2.65), (HD + 0.1, -HW + 0.3, 2.65), (HX + 1.6, HY - 0.3, 2.75), (HD + 1.1, HW + 0.1, 1.15), (-HD + 0.55, -HW + 0.4, 2.6)]
for (lx, ly, lz) in LAMPS:
    b.box(lx, ly, lz + 0.26, 0.1, 0.1, 0.5, "Smithy_Iron")
    b.box(lx, ly, lz, 0.3, 0.3, 0.34, "Smithy_Glow"); b.box(lx, ly, lz + 0.2, 0.22, 0.22, 0.1, "Smithy_Iron")
b.box(HD + 0.35, -HW - 0.1, 2.5, 0.12, 1.5, 0.9, "Smithy_WoodDark")                      # 간판 판
b.box(HD + 0.42, -HW - 0.1, 2.5, 0.06, 1.2, 0.6, "Smithy_Wood")
b.box(HD + 0.5, -HW - 0.1, 2.5, 0.08, 0.5, 0.18, "Smithy_Iron")                          # 간판 그림(망치)
b.box(HD + 0.5, -HW - 0.32, 2.42, 0.08, 0.12, 0.42, "Smithy_Iron")
smithy = b.done("Smithy")

# ── 충돌(앞은 트여 있음) ────────────────────────────────────
c = Builder(MATS, F)
c.box(0, 0, -0.14, HD * 2 + 0.9, HW * 2 + 0.9, 0.56, "Smithy_Stone")
c.box(HD + 1.9, 0, -0.1, 3.0, HW * 2 + 0.3, 0.42, "Smithy_Stone")
c.box(-HD + 0.2, 0, (0.14 + F1) / 2, 0.45, HW * 2, F1, "Smithy_Stone")
c.box(0.4, -HW + 0.2, (0.14 + F1) / 2, HD * 2 - 1.0, 0.45, F1, "Smithy_Stone")
c.box(-HD + 1.6, HW - 0.2, (0.14 + F1) / 2, 2.6, 0.45, F1, "Smithy_Stone")
c.box(CX, CY - 0.75, 1.6, 2.3, 3.4, 2.9, "Smithy_Stone")                                 # 화덕 · 굴뚝 덩이
c.box(HD - 1.4, HW - 1.25, 0.95, 0.66, 2.2, 1.3, "Smithy_Stone")                         # 카운터
c.box(AX, AY, 0.6, 1.1, 0.6, 1.0, "Smithy_Stone")                                        # 모루
c.box(0.25, 0, (F1 + 0.3 + PEAK) / 2, JD * 2 + 1.0, JW * 2 + 0.5, PEAK - F1 - 0.3, "Smithy_Stone")   # 2층 + 지붕 덩이
for s in (1, -1):
    for k in range(5): c.box(HD + 0.7 + k * 0.72, s * (HW + 0.05), 0.42, 0.66, 0.34, 0.5, "Smithy_Stone")
col = c.done("Smithy_Collision")

# ── 표시 ──────────────────────────────────────────────────
marks = []
def mark(name, p):
    e = bpy.data.objects.new(name, None); e.empty_display_size = 0.4; e.location = p; bpy.context.collection.objects.link(e); marks.append(e); return e
mark("Mark_Anvil", F.P(AX, AY, 1.05))
mark("Mark_Smith", F.P(AX - 1.25, AY + 0.35, 0.16))
mark("Mark_SmithLook", F.P(AX + 1.2, AY - 0.2, 0.16))
mark("Mark_Forge", F.P(HX + 0.25, HY - 0.3, 0.95))
mark("Mark_Smoke", F.P(CX - 0.1, CY, 12.1))
mark("Mark_Door", F.P(HD + 1.0, 0.2, 0.16))
mark("Mark_Yard", F.P(HD + 2.2, 0, 0.14))
mark("Mark_Sign", F.P(HD + 0.5, -HW - 0.1, 2.5))
for i, (lx, ly, lz) in enumerate(LAMPS): mark(f"Mark_Lamp_{i + 1}", F.P(lx, ly, lz))

allobj = [smithy, col] + marks
bpy.ops.object.select_all(action="DESELECT")
for o in allobj: o.select_set(True)
fbx = os.path.join(OUT, "Smithy.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
json.dump({"site": [SITE.x, SITE.y, GZ], "yaw": YAW, "carve": list(CARVE)}, open(os.path.join(OUT, "Smithy.json"), "w"))
print("EXPORTED", fbx, f"{os.path.getsize(fbx) / 1e6:.2f} MB", "verts", len(smithy.data.vertices))
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(EXP, "Smithy.blend"))

if "--preview" in sys.argv:
    col.hide_render = True                      # 충돌 상자는 그림에 안 나오게
    c3 = F.P(0.3, 0, 4.2)
    preview(os.path.join(PRE, "smithy_front.png"), F.P(30.0, -7.0, 13.0), c3, size=820)
    preview(os.path.join(PRE, "smithy_side.png"), F.P(9.0, 28.0, 11.0), c3, size=820)
    preview(os.path.join(PRE, "smithy_forge.png"), F.P(5.2, 1.2, 2.2), F.P(-2.2, 2.4, 1.6), size=820)
