"""용궁 미리보기(Cycles): exports/Palace.blend + 바다 밑(Seaside_Floor). 물은 푸른 배경 + 옅은 안개로 흉내.
출력: previews/palace_arena.png · palace_hall.png · palace_outside.png"""
import os, sys, math
import bpy
from mathutils import Vector
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE); EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
bpy.ops.wm.open_mainfile(filepath=os.path.join(EXP, "Palace.blend"))
sc = bpy.context.scene
with bpy.data.libraries.load(os.path.join(EXP, "Seaside.blend")) as (src, dst): dst.objects = [n for n in src.objects if n == "Seaside_Floor"]
for o in dst.objects: sc.collection.objects.link(o)
for o in bpy.data.objects:
    if o.name.startswith(("Palace_Collision", "Float_")): o.hide_render = True
dome = bpy.data.objects["Palace_Dome"]; bub = dome.data.materials[0]; b = bub.node_tree.nodes["Principled BSDF"]
b.inputs["Alpha"].default_value = 0.12; b.inputs["Roughness"].default_value = 0.02
C = Vector((-50.012, 28.328)); R = 586.0; TIP = math.radians(-36.4136)
T = C + R * Vector((math.cos(TIP), math.sin(TIP))); U = (T - C).normalized(); V = Vector((-U.y, U.x)); O = T + U * 165
def P(x, y, z): p = O + U * x + V * y; return Vector((p.x, p.y, -42 + z))
w = bpy.data.worlds.new("Sea"); w.use_nodes = True; nt = w.node_tree; bg = nt.nodes["Background"]; bg.inputs[0].default_value = (0.05, 0.18, 0.55, 1); bg.inputs[1].default_value = 0.9; sc.world = w
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 2.6; sun.data.color = (0.75, 0.88, 1.0); sun.rotation_euler = (0.35, 0.15, 0.4); sc.collection.objects.link(sun)
for x, y, z, e, c in ((-11, 0, 6, 9000, (0.5, 0.7, 1.0)), (16.5, 0, 10.5, 5000, (1.0, 0.85, 0.6)), (3, 0, 9, 4000, (0.6, 0.8, 1.0))):
    L = bpy.data.objects.new("PL", bpy.data.lights.new("PL", "POINT")); L.data.energy = e; L.data.color = c; L.data.shadow_soft_size = 2; L.location = P(x, y, z); sc.collection.objects.link(L)
sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 48; sc.cycles.use_denoising = True; sc.render.resolution_x, sc.render.resolution_y = 1280, 720
sc.view_settings.view_transform = "AgX"; sc.render.film_transparent = False
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
def shot(name, loc, tgt, lens, hide_dome=True):
    dome.hide_render = hide_dome; cam.location = loc; cam.data.lens = lens; cam.rotation_euler = (tgt - loc).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(PRE, name + ".png"); bpy.ops.render.render(write_still=True); print("✔", name)
shot("palace_arena", P(-33, 0, 5.5), P(8, 0, 9), 22)
shot("palace_hall", P(8.0, 0, 6.6), P(25, 0, 7.6), 20)
shot("palace_outside", P(-75, -55, 30), P(0, 0, 8), 28, hide_dome=False)
