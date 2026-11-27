"""미리보기: 구래성 노역장 + 돌고래 죄수(고래고래 × 0.4 + 곡괭이 + 죄수 띠) — 노을빛.
출력: previews/mine_dolphins_yard.png · mine_dolphins_cells.png
실행: <bpy 파이썬> blender/render_mine_dolphins.py"""
import os, math, random
import bpy
from mathutils import Vector, Matrix
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); PRE = os.path.join(ROOT, "previews"); random.seed(4)
bpy.ops.wm.read_factory_settings(use_empty=True)
imp = lambda p: bpy.ops.import_scene.fbx(filepath=p)
imp(os.path.join(ROOT, "unity/Assets/Resources/World/CastleDungeon.fbx"))
dun = set(bpy.data.objects)
for o in list(bpy.data.objects):
    if "Collision" in o.name: o.hide_render = True
marks = {o.name: o.matrix_world.translation.copy() for o in bpy.data.objects if o.name.startswith("Mark_")}
tmpl = next(o for o in bpy.data.objects if o.name.startswith("MineRock_Template"))
rock_pos = [p for n, p in marks.items() if n.startswith("Mark_Rock_")]
for p in rock_pos:
    c = tmpl.copy(); c.matrix_world = Matrix.Translation(p) @ Matrix.Rotation(random.uniform(0, 6.28), 4, "Z") @ Matrix.Diagonal((1, 1, 1, 1)) @ Matrix.Rotation(math.pi / 2, 4, "X"); bpy.context.collection.objects.link(c)
tmpl.hide_render = True
with bpy.data.libraries.load(os.path.join(ROOT, "exports/DolphinPrisoner.blend")) as (src, dst):
    dst.objects = [n for n in src.objects if n in ("Goraegorae", "DolphinPickaxe", "DolphinStripes")]
parts = []
for o in dst.objects:
    for m in list(o.modifiers): o.modifiers.remove(m)
    o.parent = None; parts.append(o)
def dolphin(at, yaw, hop=0.0):
    for o in parts:
        c = o.copy(); bpy.context.collection.objects.link(c); c.hide_render = False
        c.matrix_world = Matrix.Translation(at + Vector((0, 0, hop))) @ Matrix.Rotation(yaw, 4, "Z") @ Matrix.Diagonal((0.4, 0.4, 0.4, 1))
for i, p in enumerate(rock_pos[:8]):
    a = i * 2.1; at = p + Vector((math.cos(a), math.sin(a), 0)) * 1.3; d = p - at; dolphin(at, math.atan2(-d.x, d.y), 0)
for i in range(6): dolphin(marks["Mark_DungeonCenter"] + Vector((random.uniform(-12, 12), random.uniform(-5, 3), 0)), random.uniform(0, 6.28), random.choice((0, 0.25, 0.3)))
for n, p in marks.items():
    if n.startswith("Mark_Cell_") and int(n.split("_")[-1]) <= 7: dolphin(p, random.uniform(-0.8, 0.8) + math.pi, 0)
sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.samples = 28; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True; sc.view_settings.view_transform = "AgX"
sc.render.resolution_x, sc.render.resolution_y = 1100, 620
w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 0.05; sc.world = w
def pl(p, e, c): L = bpy.data.objects.new("L", bpy.data.lights.new("L", "POINT")); L.data.energy = e; L.data.color = c; L.location = p; sc.collection.objects.link(L)
for n, p in marks.items():
    if n.startswith("Mark_Torch_"): pl(p, 220, (1, 0.6, 0.3))
    if n.startswith("Mark_Lantern_"): pl(p, 320, (1, 0.72, 0.4))
for n, p in marks.items():
    if n.startswith("Mark_Sunset_"):
        sp = bpy.data.objects.new("S", bpy.data.lights.new("S", "SPOT")); sp.data.energy = 20000; sp.data.color = (1.0, 0.5, 0.26); sp.data.spot_size = 1.0; sp.data.spot_blend = 0.7
        sp.location = p + Vector((0, 0, 1.2)); sp.rotation_euler = (0.4, 0.15, 0.4); sc.collection.objects.link(sp)
cam = bpy.data.objects.new("C", bpy.data.cameras.new("C")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 20
c0 = marks["Mark_DungeonCenter"]
def shot(n, loc, tgt): cam.location = loc; cam.rotation_euler = (tgt - loc).to_track_quat("-Z", "Y").to_euler(); sc.render.filepath = os.path.join(PRE, n); bpy.ops.render.render(write_still=True)
shot("mine_dolphins_yard.png", c0 + Vector((-20, -2, 4.5)), c0 + Vector((4, -1, 0.5)))
shot("mine_dolphins_close.png", c0 + Vector((-15, -6, 1.6)), c0 + Vector((-9, -3, 0.5)))
shot("mine_dolphins_cells.png", c0 + Vector((-8, -6, 2.2)), c0 + Vector((-8, 14, 1.8)))
