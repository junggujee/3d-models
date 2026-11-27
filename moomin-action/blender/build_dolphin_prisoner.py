"""구래성 지하 노역장의 돌고래 죄수(억울하게 잡혀 온 돌고래들) — 고래고래 모델(exports/Goraegorae.blend)을 그대로 쓰고 소품만 더함.
  · DolphinPickaxe: 오른 지느러미(flipper.R) 끝에 쥔 작은 곡괭이(나무 자루 + 쇠 머리) — 고래 모델 공간 그대로 → Unity NpcAccessory 가 뼈 bindpose 로 붙임
  · DolphinStripes: 몸통에 두른 흑백 줄무늬 죄수 띠(body 뼈) + 앞 번호표
  · 초상화 dolphin_normal.png(줄인 크기 · 곡괭이를 든 모습, Cycles + Freestyle 선 + 셀 채색) · previews/dolphin_prisoner.png
Unity 에서 프리팹 DolphinPrisoner = Goraegorae.fbx × 0.4(길이 약 1.3m, MoominSetup) + 이 소품 + World/DolphinPrisoner.cs(통통 뛰어다님 · 곡괭이질 · 감방에 박혀 있음 · F 대화).
실행: <bpy 파이썬> blender/build_dolphin_prisoner.py"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
import bpy, bmesh
from mathutils import Vector, Matrix
from lib import mat, tube, to_object, bind
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
NPC = os.path.join(ROOT, "unity", "Assets", "Resources", "Npc"); PORT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Portraits"); PRE = os.path.join(ROOT, "previews")
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "exports", "Goraegorae.blend"))
arm = next(o for o in bpy.data.objects if o.type == "ARMATURE"); body = next(o for o in bpy.data.objects if o.type == "MESH" and o.name == "Goraegorae")
WOOD = mat("Dolphin_PickWood", (0.55, 0.34, 0.16), rough=0.7); IRON = mat("Dolphin_PickIron", (0.42, 0.42, 0.47), rough=0.35, metal=0.9)
BLK = mat("Dolphin_StripeBlack", (0.06, 0.06, 0.08), rough=0.8); WHT = mat("Dolphin_StripeWhite", (0.95, 0.95, 0.93), rough=0.8); TAG = mat("Dolphin_Tag", (0.95, 0.85, 0.5), rough=0.6)
MATS = [WOOD, IRON, BLK, WHT, TAG]
def put(bm, other):
    me = bpy.data.meshes.new("t"); other.to_mesh(me); other.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)
# ── 곡괭이: 지느러미 끝에서 위·앞으로 뻗은 자루 + 쇠 머리
tip = arm.data.bones["flipper.R"].tail_local.copy(); grip = tip + Vector((-0.02, -0.02, 0.04))
up = Vector((-0.15, -0.35, 1.0)).normalized(); top = grip + up * 0.62
pk = bmesh.new()
put(pk, tube([grip - up * 0.12, top], lambda t: 0.028, sides=8, mat_idx=0)[0])
side = up.cross(Vector((1, 0, 0))).normalized()
head = [top + side * (0.26 * s) + up * (-0.08 * abs(s) ** 2) for s in [i / 8 * 2 - 1 for i in range(9)]]
put(pk, tube(head, lambda t: 0.04 * (1 - 0.6 * abs(2 * t - 1)), sides=8, mat_idx=1)[0])
hb = bmesh.new(); bmesh.ops.create_cube(hb, size=1.0); bmesh.ops.transform(hb, matrix=Matrix.Translation(top) @ Matrix.Diagonal((0.07, 0.07, 0.08, 1)), verts=hb.verts[:])
for f in hb.faces: f.material_index = 1
put(pk, hb); pick = to_object("DolphinPickaxe", pk, MATS)
# ── 죄수 띠: 몸통(y 0.05)을 감는 흑백 줄무늬 + 앞 번호표
BC, BR = Vector((0.0, 0.0, 0.51)), Vector((0.47, 0.93, 0.43))
st = bmesh.new()
for k in range(3):
    yy = -0.08 + k * 0.1; sc = math.sqrt(max(0.05, 1 - (yy / BR.y) ** 2)) * 1.06
    pts = [Vector((BR.x * sc * math.cos(2 * math.pi * j / 40), yy, BC.z + BR.z * sc * math.sin(2 * math.pi * j / 40))) for j in range(41)]
    put(st, tube(pts, lambda t: 0.045, sides=6, mat_idx=2 if k % 2 == 0 else 3)[0])
tb = bmesh.new(); bmesh.ops.create_cube(tb, size=1.0); bmesh.ops.transform(tb, matrix=Matrix.Translation(Vector((0.0, -0.02, BC.z - BR.z * 1.0))) @ Matrix.Diagonal((0.2, 0.12, 0.03, 1)), verts=tb.verts[:])
for f in tb.faces: f.material_index = 4
put(st, tb); bmesh.ops.recalc_face_normals(st, faces=st.faces[:]); stripes = to_object("DolphinStripes", st, MATS)
os.makedirs(NPC, exist_ok=True)
for ob in (pick, stripes):
    bpy.ops.object.select_all(action="DESELECT"); ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(NPC, ob.name + ".fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y")
    print("EXPORTED", ob.name)
for ob, bn in ((pick, "flipper.R"), (stripes, "body")):
    vg = ob.vertex_groups.new(name=bn); vg.add(list(range(len(ob.data.vertices))), 1.0, "REPLACE"); bind(ob, arm)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(ROOT, "exports", "DolphinPrisoner.blend"))   # 미리보기(render_mine_dolphins.py)용
# ── 초상화 + 미리보기
sc = bpy.context.scene
for o in list(sc.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
act = bpy.data.actions.get("Idle")
if act: arm.animation_data_create(); arm.animation_data.action = act; sc.frame_set(1)
w = bpy.data.worlds.new("PW"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 0.7; sc.world = w
for loc, e in ((Vector((-2.2, -3.0, 2.4)), 260), (Vector((2.4, 1.0, 1.8)), 140)):
    L = bpy.data.objects.new("L", bpy.data.lights.new("L", "AREA")); L.data.energy = e; L.data.size = 2.0; L.location = loc; sc.collection.objects.link(L)
    L.rotation_euler = (Vector((0, -0.6, 0.6)) - loc).to_track_quat("-Z", "Y").to_euler()
cam = bpy.data.objects.new("PC", bpy.data.cameras.new("PC")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 50
sc.render.engine = "CYCLES"; sc.cycles.samples = 40; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True; sc.view_settings.view_transform = "Standard"
def shot(path, tgt, off, w_, h_, line):
    cam.location = tgt + off; cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.render.film_transparent = True; sc.render.resolution_x, sc.render.resolution_y = w_, h_; sc.render.image_settings.color_mode = "RGBA"
    sc.render.use_freestyle = line; vl = sc.view_layers[0]; vl.use_freestyle = line
    if line:
        fs = vl.freestyle_settings; ls = fs.linesets[0] if len(fs.linesets) else fs.linesets.new("L"); ls.select_silhouette = True; ls.select_border = True; ls.select_crease = True
        ls.linestyle = ls.linestyle or bpy.data.linestyles.new("Ink"); ls.linestyle.color = (0.08, 0.1, 0.22); ls.linestyle.thickness = 2.4
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)
os.makedirs(PORT, exist_ok=True); os.makedirs(PRE, exist_ok=True)
shot(os.path.join(PORT, "dolphin_normal.png"), Vector((-0.1, -0.7, 0.62)), Vector((-1.6, -2.9, 0.55)), 600, 720, True)
shot(os.path.join(PRE, "dolphin_prisoner.png"), Vector((0, -0.2, 0.6)), Vector((-2.6, -3.6, 1.6)), 700, 560, False)
print("✔ dolphin portrait + preview")
