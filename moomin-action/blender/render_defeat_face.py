"""
패배 화면용 민미 얼굴(눈이 ×× 인 모습): exports/Moomin.blend 의 실제 모델에서 눈(흰 테·파란 자수) 면을 지우고
정면 얼굴을 렌더(× 는 tools/defeat_face.py 가 눈 자리에 2D 로 그림 — 곡면에 박히지 않고 또렷하게) → exports/DefeatFace_raw.png + .json(눈 자리).
실행:  <bpy 파이썬> blender/render_defeat_face.py
"""
import bpy, bmesh, math, os
from mathutils import Vector, Matrix

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "exports", "DefeatFace_raw.png")   # 다음: python3 tools/defeat_face.py
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "exports", "Moomin.blend"))
sc = bpy.context.scene
for o in list(bpy.data.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
rig = bpy.data.objects["MoominRig"]; rig.data.pose_position = "REST"
if rig.animation_data: rig.animation_data.action = None
body = bpy.data.objects["Moomin"]; me = body.data
names = [m.name for m in me.materials]; ring_i, blue_i, brow_i = names.index("Moomin_EyeRing"), names.index("Moomin_EyeBlue"), names.index("Moomin_Brow")

# 눈 자리(왼/오): 흰 테 면의 중심과 평균 법선
bm = bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
eyes = {}
for f in bm.faces:
    if f.material_index == ring_i:
        k = 1 if f.calc_center_median().x > 0 else -1; e = eyes.setdefault(k, [Vector(), Vector(), 0])
        e[0] += f.calc_center_median() * f.calc_area(); e[1] += f.normal * f.calc_area(); e[2] += f.calc_area()
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.material_index in (ring_i, blue_i)], context="FACES")
bm.to_mesh(me); bm.free()
mw = body.matrix_world

eye_pts = {k: mw @ (v[0] / v[2]) for k, v in eyes.items()}
eye_nrm = {k: (mw.to_3x3() @ (v[1] / v[2])).normalized() for k, v in eyes.items()}

# 카메라: 얼굴 정면(살짝 위에서, 고개 숙인 듯)
head = (eye_pts[1] + eye_pts[-1]) / 2 + Vector((0, 0, -0.06))
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); cam.data.type = "ORTHO"; cam.data.ortho_scale = 0.95
cam.location = head + Vector((0, -3, 0.35)); cam.rotation_euler = (head + Vector((0, 0, -0.02)) - cam.location).to_track_quat("-Z", "Y").to_euler()
cam.rotation_euler.y = math.radians(-6)   # 갸우뚱(기운 없이)
sc.collection.objects.link(cam); sc.camera = cam
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 4.0; sun.rotation_euler = (math.radians(50), 0, math.radians(-30)); sc.collection.objects.link(sun)
sc.world = bpy.data.worlds.new("W"); sc.world.use_nodes = True; sc.world.node_tree.nodes["Background"].inputs[1].default_value = 1.4
sc.render.engine = "CYCLES"; sc.cycles.samples = 48; sc.cycles.use_denoising = True; sc.cycles.device = "CPU"
sc.view_settings.view_transform = "AgX"; sc.view_settings.exposure = 0.4; sc.render.film_transparent = True
sc.render.resolution_x = sc.render.resolution_y = 512
sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGBA"
os.makedirs(os.path.dirname(OUT), exist_ok=True); sc.render.filepath = OUT; bpy.ops.render.render(write_still=True)
# 눈 자리 → 화면 좌표(0~1, 왼쪽 아래 원점) + 눈 크기(px): tools/defeat_face.py 가 그 자리에 × 를 그림
import json
from bpy_extras.object_utils import world_to_camera_view
bpy.context.view_layer.update()
meta = {}
for k, p in eye_pts.items():
    v = world_to_camera_view(sc, cam, p); right = Vector((0, 0, 1)).cross(eye_nrm[k]).normalized()
    v2 = world_to_camera_view(sc, cam, p + right * 0.07)
    meta["L" if k > 0 else "R"] = {"uv": [v.x, v.y], "r": abs(v2.x - v.x) * 512, "tilt": math.degrees(cam.rotation_euler.y)}
json.dump(meta, open(OUT.replace(".png", ".json"), "w"), indent=1); print("✔", OUT, meta)
