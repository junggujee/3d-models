"""레이저 블레이더 광선 잔상 미리보기: 민미(기본 자세) + 회전부를 여러 각도로 렌더 → tools 쪽 합성(previews/laser_trail_mock.png).
실행: <bpy 파이썬> blender/render_laser_trail.py   (build_weapons.py 다음)"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); OUT = os.path.join(ROOT, "exports", "laser_trail"); os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "exports", "Moomin.blend"))
sc = bpy.context.scene
for o in list(bpy.data.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
bpy.ops.import_scene.fbx(filepath=os.path.join(ROOT, "unity", "Assets", "Resources", "Weapons", "LaserBlader.fbx"), axis_forward="-Z", axis_up="Y")
grip = next(o for o in bpy.data.objects if o.name.startswith("Grip_R_Laser")); rotor = next(o for o in bpy.data.objects if o.name.startswith("Rotor_R_Laser"))
moomin = bpy.data.objects["Moomin"]
sc.render.engine = "CYCLES"; sc.cycles.samples = 24; sc.cycles.use_denoising = True; sc.view_settings.view_transform = "AgX"
sc.render.film_transparent = True; sc.render.resolution_x = sc.render.resolution_y = 520; sc.render.image_settings.color_mode = "RGBA"
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.0; sun.rotation_euler = (0.8, 0.15, -0.9); sc.collection.objects.link(sun)
w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 0.8; sc.world = w
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.location = Vector((-2.9, -1.2, 1.0)); cam.data.lens = 40; cam.rotation_euler = (Vector((-0.35, -0.1, 0.45)) - cam.location).to_track_quat("-Z", "Y").to_euler()
axis = (rotor.matrix_world.translation - grip.matrix_world.translation).normalized(); base = rotor.matrix_world.copy(); piv = base.translation.copy()
moomin.hide_render = True; grip.hide_render = True
for i in range(10):
    a = -i * math.radians(14)
    rotor.matrix_world = Matrix.Translation(piv) @ Matrix.Rotation(a, 4, axis) @ Matrix.Translation(-piv) @ base
    sc.render.filepath = os.path.join(OUT, f"rotor_{i}.png"); bpy.ops.render.render(write_still=True)
rotor.matrix_world = base; moomin.hide_render = False; grip.hide_render = False; rotor.hide_render = True
sc.render.filepath = os.path.join(OUT, "body.png"); bpy.ops.render.render(write_still=True)
print("✔ laser trail frames")
