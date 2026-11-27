"""하프물범 여왕의 성 — 지하감옥(여왕에게 "끼루룩 끼룩끼룩 민미민미" 하면 끌려오는 곳).
  · 돌 복도(길이 26m · 낮은 둥근 천장 5m) · 오른쪽에 쇠창살 감방 3칸 · 첫 칸이 민미가 갇히는 방(흔들면 열리는 철창 문 DungeonCellDoor)
  · 감방 안: 지푸라기 · 양동이 · 벽 사슬 · 높은 창살 창(달빛) · 왼쪽 벽 횃불(Glow) · 열쇠 꾸러미 걸린 탁자 · 하프물범 얼굴 돌판
  · 복도 끝 계단(6단) 위 나무 문 → 알현실로 돌아감
자리: 성 위 하늘(맵 좌표 z = FLOOR) — 알현실과 떨어진 닫힌 실내.
표시: Mark_CellArrive · Mark_CellLook · Mark_CellDoorFront · Mark_ExitStairs · Mark_ExitLook · Mark_DungeonCenter · Mark_Torch_1..5 · Mark_Moon
실행: <bpy 파이썬> blender/build_castle_dungeon.py [--preview]   →  unity/Assets/Resources/World/CastleDungeon.fbx"""
import sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bpy, bmesh
from mathutils import Vector, Matrix
from meshkit import Frame, Builder
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World"); EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
reset_scene(); random.seed(3)
CASTLE = Vector((-43.6, 27.3)); FLOOR = 230.0
O = Vector((CASTLE.x - 13.0, CASTLE.y + 60.0, FLOOR)); F = Frame(O, (1, 0, 0), (0, 1, 0))
def M_(n, c, rough=0.8, metal=0.0): m = mat(n, c, rough=rough, metal=metal); m.diffuse_color = (*c, 1); return m
STONE = M_("Dun_Stone", (0.42, 0.4, 0.38), 0.95); STONED = M_("Dun_StoneDark", (0.27, 0.26, 0.25), 0.95); IRON = M_("Dun_Iron", (0.16, 0.16, 0.18), 0.45, 0.8)
WOODM = M_("Dun_Wood", (0.36, 0.22, 0.11), 0.8); STRAW = M_("Dun_Straw", (0.86, 0.72, 0.36), 1.0); GOLD = M_("Dun_Gold", (1.0, 0.76, 0.3), 0.3, 1.0)
FIRE = mat("Dun_FlameGlow", (1.0, 0.55, 0.18), emit=(1.0, 0.5, 0.15), emit_strength=8.0); MOON = mat("Dun_MoonGlow", (0.6, 0.72, 1.0), emit=(0.6, 0.72, 1.0), emit_strength=3.0)
SEALM = M_("Dun_SealStone", (0.72, 0.71, 0.69), 0.7); INK = M_("Dun_Ink", (0.06, 0.06, 0.07), 0.4); MOSS = M_("Dun_Moss", (0.22, 0.32, 0.16), 1.0)
MATS = [STONE, STONED, IRON, WOODM, STRAW, GOLD, FIRE, MOON, SEALM, INK, MOSS]
b = Builder(MATS, F)
L, HW, HC = 26.0, 2.0, 5.0                     # 복도 길이 · 반폭 · 천장 높이
CELLS = [(3.0, 7.5), (9.0, 13.5), (15.0, 19.5)]; CD = 4.2   # 감방(x 범위) · 깊이(y 2 → 6.2)
# 바닥(돌판 격자) · 벽 · 둥근 천장
for i in range(int(L / 1.0)):
    for j in range(-2, 7):
        if j >= 2 and not any(x0 <= i + 0.5 <= x1 for x0, x1 in CELLS): continue
        b.box(i + 0.5, j + 0.5 if j < 2 else j + 0.5, -0.12 + random.uniform(-0.02, 0.02), 0.96, 0.96, 0.25, random.choice(["Dun_Stone", "Dun_StoneDark"]))
b.box(L / 2, -HW - 0.4, HC / 2, L, 0.8, HC, "Dun_Stone")                                         # 왼쪽 벽
b.box(-0.4, 2.0, HC / 2, 0.8, 9.0, HC, "Dun_Stone"); b.box(L + 0.4, 0, HC / 2 + 1.0, 0.8, 5.0, HC + 2, "Dun_Stone")
for x0, x1 in CELLS:
    b.box((x0 + x1) / 2, 2 + CD + 0.4, HC / 2, x1 - x0 + 1.6, 0.8, HC, "Dun_Stone")              # 감방 뒷벽
    for xe in (x0 - 0.4, x1 + 0.4): b.box(xe, 2 + CD / 2, HC / 2, 0.8, CD, HC, "Dun_Stone")      # 칸막이
    b.box((x0 + x1) / 2, 2 + CD / 2, HC + 0.2, x1 - x0 + 0.8, CD + 0.8, 0.4, "Dun_StoneDark")    # 감방 천장
for xs, xe in ((-0.8, CELLS[0][0] - 0.8), (CELLS[0][1] + 0.8, CELLS[1][0] - 0.8), (CELLS[1][1] + 0.8, CELLS[2][0] - 0.8), (CELLS[2][1] + 0.8, L)):
    b.box((xs + xe) / 2, HW + 0.4, HC / 2, xe - xs, 0.8, HC, "Dun_Stone")                        # 감방 사이 앞벽
for i in range(27):                                                                               # 둥근 천장(복도)
    a0, a1 = math.pi * i / 27, math.pi * (i + 1) / 27; am = (a0 + a1) / 2
    y = -HW * math.cos(am); z = HC - 1.2 + 1.4 * math.sin(am)
    b.box(L / 2, y, z + 0.15, L, HW * 2 * math.pi / 27 * 0.62 + 0.08, 0.3, "Dun_StoneDark", tilt=Matrix.Rotation(-(am - math.pi / 2), 3, "X"))
for x in range(2, int(L), 4): b.box(x, 0, HC + 0.05, 0.4, HW * 2 + 0.4, 0.35, "Dun_Stone")    # 천장 띠
# 쇠창살(감방 앞) — 첫 칸은 문 자리(가운데 1.2m)를 비움 → 따로 움직이는 문
DOOR_X0, DOOR_X1 = 4.65, 5.85
def bars(x0, x1, skip=None):
    n = int((x1 - x0) / 0.2)
    for k in range(n + 1):
        x = x0 + (x1 - x0) * k / n
        if skip and skip[0] - 0.05 < x < skip[1] + 0.05: continue
        b.cyl((x, HW, 0.0), (x, HW, HC - 0.2), 0.035, "Dun_Iron", seg=6)
    for z in (0.15, 2.2, HC - 0.3):
        if skip: b.box((x0 + skip[0]) / 2, HW, z, skip[0] - x0, 0.08, 0.1, "Dun_Iron"); b.box((skip[1] + x1) / 2, HW, z, x1 - skip[1], 0.08, 0.1, "Dun_Iron")
        else: b.box((x0 + x1) / 2, HW, z, x1 - x0, 0.08, 0.1, "Dun_Iron")
bars(CELLS[0][0], CELLS[0][1], (DOOR_X0, DOOR_X1)); bars(*CELLS[1]); bars(*CELLS[2])
b.box(DOOR_X0 - 0.04, HW, 2.4, 0.1, 0.12, 4.8, "Dun_Iron"); b.box(DOOR_X1 + 0.04, HW, 2.4, 0.1, 0.12, 4.8, "Dun_Iron")
# 감방 안(첫 칸): 지푸라기 · 양동이 · 사슬 · 창살 창(달빛)
cx0, cx1 = CELLS[0]
for k in range(7): b.sphere((cx0 + 0.9 + 0.35 * (k % 4), 5.2 + 0.3 * (k // 4), 0.08), (0.55, 0.4, 0.12), "Dun_Straw", seg=10, rings=6)
b.cyl((cx1 - 0.7, 3.0, 0.0), (cx1 - 0.7, 3.0, 0.5), 0.26, "Dun_Wood", r1=0.3, seg=12); b.torus((cx1 - 0.7, 3.0, 0.45), 0.3, 0.02, "Dun_Iron", seg=16, sides=5)
for s in (-1, 1):
    pts = [(cx0 + 2.2 + s * 0.6, 2 + CD - 0.02, 3.2 - 0.12 * k) for k in range(8)]
    b.tube(pts, 0.03, "Dun_Iron", sides=5); b.torus((cx0 + 2.2 + s * 0.6, 2 + CD - 0.1, 2.25), 0.12, 0.025, "Dun_Iron", axis=(0, 1, 0), seg=12, sides=5)
b.box((cx0 + cx1) / 2, 2 + CD - 0.02, 3.9, 1.2, 0.1, 0.7, "Dun_MoonGlow")
for k in range(5): b.cyl(((cx0 + cx1) / 2 - 0.5 + 0.25 * k, 2 + CD - 0.08, 3.55), ((cx0 + cx1) / 2 - 0.5 + 0.25 * k, 2 + CD - 0.08, 4.25), 0.025, "Dun_Iron", seg=5)
for k in range(4): b.sphere((random.uniform(1, L - 3), -HW + 0.05, random.uniform(0.2, 1.2)), (0.4, 0.06, 0.25), "Dun_Moss", seg=8, rings=5)
# 왼쪽 벽 횃불 · 하프물범 얼굴 돌판 · 열쇠 탁자
TORCH = [2.0, 8.0, 14.0, 20.0, 24.5]
for x in TORCH:
    b.box(x, -HW + 0.08, 2.4, 0.1, 0.16, 0.5, "Dun_Iron"); b.cyl((x, -HW + 0.18, 2.55), (x, -HW + 0.38, 3.0), 0.05, "Dun_Wood", r1=0.07, seg=8)
    b.sphere((x, -HW + 0.4, 3.12), (0.1, 0.1, 0.18), "Dun_FlameGlow", seg=10, rings=6)
fc = Vector((11.0, -HW + 0.1, 3.2)); f_ = Vector((0, 1, 0)); up = Vector((0, 0, 1)); sd = Vector((1, 0, 0))
b.cyl(tuple(fc - f_ * 0.05), tuple(fc + f_ * 0.06), 0.75, "Dun_SealStone", seg=24); b.torus(tuple(fc + f_ * 0.06), 0.75, 0.06, "Dun_Gold", axis=(0, 1, 0), seg=24, sides=6)
for s in (-1, 1):
    b.sphere(tuple(fc + sd * s * 0.27 + up * 0.06 + f_ * 0.1), (0.07, 0.07, 0.07), "Dun_Ink", seg=10, rings=6)
    b.tube([tuple(fc + sd * s * 0.35 - up * 0.1 + f_ * 0.1), tuple(fc + sd * s * 0.62 - up * 0.05 + f_ * 0.1)], 0.018, "Dun_Ink", sides=4)
b.sphere(tuple(fc - up * 0.08 + f_ * 0.11), (0.07, 0.05, 0.05), "Dun_Ink", seg=10, rings=6)
b.box(17.0, -HW + 0.6, 0.75, 1.6, 0.9, 0.08, "Dun_Wood")
for xx in (16.35, 17.65):
    for yy in (-HW + 0.25, -HW + 0.95): b.box(xx, yy, 0.37, 0.08, 0.08, 0.74, "Dun_Wood")
b.torus((17.2, -HW + 0.55, 0.81), 0.12, 0.015, "Dun_Gold", seg=14, sides=4)
for k in range(3): b.box(17.2 + 0.1 * math.cos(k * 2.1), -HW + 0.55 + 0.1 * math.sin(k * 2.1), 0.81, 0.16, 0.03, 0.015, "Dun_Gold", rot=k * 2.1)
# 끝 계단 + 나무 문(위로)
for i in range(6): b.box(21.5 + i * 0.75, 0, (0.3 + i * 0.3) / 2, 0.75, HW * 2, 0.3 + i * 0.3, "Dun_Stone")
b.box(L - 0.02, 0, 1.8 + 3.0 / 2 + 0.0, 0.1, 1.8, 2.6, "Dun_Wood"); b.box(L - 0.08, 0.5, 3.2, 0.06, 0.1, 0.1, "Dun_Gold")
hall = b.done("CastleDungeon")
# 움직이는 철창 문(경첩 = DOOR_X0 쪽)
d = Builder(MATS, F)
for k in range(7): x = DOOR_X0 + 0.08 + (DOOR_X1 - DOOR_X0 - 0.16) * k / 6; d.cyl((x, HW, 0.05), (x, HW, 2.9), 0.035, "Dun_Iron", seg=6)
for z in (0.2, 1.4, 2.8): d.box((DOOR_X0 + DOOR_X1) / 2, HW, z, DOOR_X1 - DOOR_X0, 0.08, 0.1, "Dun_Iron")
d.box(DOOR_X1 - 0.15, HW - 0.06, 1.5, 0.14, 0.06, 0.2, "Dun_Gold")
door = d.done("DungeonCellDoor", origin=F.P(DOOR_X0, HW, 0.0))
d2 = Builder(MATS, F); d2.box(DOOR_X0 + 0.6, HW, 1.5, 1.2, 0.12, 3.0, "Dun_Iron"); door_col = d2.done("DungeonCellDoor_Collision", origin=F.P(DOOR_X0, HW, 0.0))
door_col.parent = door; door_col.matrix_parent_inverse = door.matrix_world.inverted()
# 충돌
c = Builder(MATS, F)
c.box(L / 2, 2.0, -0.15, L + 2, 10, 0.3, "Dun_Stone"); c.box(L / 2, -HW - 0.4, HC / 2, L + 2, 0.8, HC, "Dun_Stone"); c.box(-0.4, 2.0, HC / 2, 0.8, 10, HC, "Dun_Stone")
c.box(L + 0.4, 0, HC / 2 + 1, 0.8, 5, HC + 2, "Dun_Stone"); c.box(L / 2, 2.0, HC + 0.4, L + 2, 10, 0.4, "Dun_Stone")
for x0, x1 in CELLS:
    c.box((x0 + x1) / 2, 2 + CD + 0.4, HC / 2, x1 - x0 + 1.6, 0.8, HC, "Dun_Stone")
    for xe in (x0 - 0.4, x1 + 0.4): c.box(xe, 2 + CD / 2, HC / 2, 0.8, CD, HC, "Dun_Stone")
for xs, xe in ((-0.8, CELLS[0][0] - 0.8), (CELLS[0][1] + 0.8, CELLS[1][0] - 0.8), (CELLS[1][1] + 0.8, CELLS[2][0] - 0.8), (CELLS[2][1] + 0.8, L)):
    c.box((xs + xe) / 2, HW + 0.4, HC / 2, xe - xs, 0.8, HC, "Dun_Stone")
c.box((CELLS[0][0] + DOOR_X0) / 2, HW, HC / 2, DOOR_X0 - CELLS[0][0], 0.2, HC, "Dun_Stone"); c.box((DOOR_X1 + CELLS[0][1]) / 2, HW, HC / 2, CELLS[0][1] - DOOR_X1, 0.2, HC, "Dun_Stone")
for x0, x1 in CELLS[1:]: c.box((x0 + x1) / 2, HW, HC / 2, x1 - x0, 0.2, HC, "Dun_Stone")
for i in range(6): c.box(21.5 + i * 0.75, 0, (0.3 + i * 0.3) / 2, 0.75, HW * 2, 0.3 + i * 0.3, "Dun_Stone")
col = c.done("CastleDungeon_Collision")
marks = []
def mark(n, p): e = bpy.data.objects.new(n, None); e.location = p; bpy.context.collection.objects.link(e); marks.append(e)
mark("Mark_CellArrive", F.P((cx0 + cx1) / 2, 4.6, 0.06)); mark("Mark_CellLook", F.P((cx0 + cx1) / 2, 0.0, 0.06)); mark("Mark_CellDoorFront", F.P((DOOR_X0 + DOOR_X1) / 2, HW + 0.9, 0.06))
mark("Mark_ExitStairs", F.P(21.0, 0, 0.06)); mark("Mark_ExitLook", F.P(L, 0, 0.06)); mark("Mark_DungeonCenter", F.P(L / 2, 1.0, 0.06)); mark("Mark_Moon", F.P((cx0 + cx1) / 2, 2 + CD - 0.6, 3.9))
for i, x in enumerate(TORCH): mark(f"Mark_Torch_{i + 1}", F.P(x, -HW + 0.6, 3.1))
allobj = [hall, door, door_col, col] + marks
bpy.ops.object.select_all(action="DESELECT")
for o in allobj: o.select_set(True)
fbx = os.path.join(OUT, "CastleDungeon.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("EXPORTED", fbx, f"{os.path.getsize(fbx) / 1e6:.2f} MB")
if "--preview" in sys.argv:
    col.hide_render = True; door_col.hide_render = True
    sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.samples = 32; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True; sc.view_settings.view_transform = "AgX"
    sc.render.resolution_x = 900; sc.render.resolution_y = 600
    w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 0.05; sc.world = w
    for x in TORCH:
        Lp = bpy.data.objects.new("L", bpy.data.lights.new("L", "POINT")); Lp.data.energy = 220; Lp.data.color = (1, 0.6, 0.3); Lp.location = F.P(x, -HW + 0.7, 3.1); sc.collection.objects.link(Lp)
    Lm = bpy.data.objects.new("M", bpy.data.lights.new("M", "POINT")); Lm.data.energy = 120; Lm.data.color = (0.6, 0.72, 1.0); Lm.location = F.P((cx0 + cx1) / 2, 5.4, 3.6); sc.collection.objects.link(Lm)
    cam = bpy.data.objects.new("C", bpy.data.cameras.new("C")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 20
    def shot(n, loc, tgt): cam.location = loc; cam.rotation_euler = (tgt - loc).to_track_quat("-Z", "Y").to_euler(); sc.render.filepath = os.path.join(PRE, n); bpy.ops.render.render(write_still=True)
    shot("dungeon_corridor.png", F.P(0.6, -1.2, 1.8), F.P(14, 2, 1.6)); shot("dungeon_cell.png", F.P(5.2, 5.9, 2.2), F.P(5.2, 0, 1.3))
