"""첫 장신구: 파란 머리띠(사용자 사진 — 보송보송한 하늘색 곱창 고리). 민미 머리에 맞춰 민미 모델과 같은 공간(기본 자세)에서 만듦.
  → unity/Assets/Resources/Equipment/Headband_Blue.fbx  (EquipmentVisuals 가 bindpose 로 head 뼈에 붙임)
  → unity/Assets/Resources/ItemIcons/headband_blue.png  (가방·장비 화면 아이콘, 투명 배경)
  → previews/headband_worn.png · headband_icon_preview.png
모양: 머리 둘레를 이마 위(앞이 높고 뒤가 낮게) 감는 고리. 굵은 관에 주름(곱창처럼 오므라든 결) + 보송한 잔털 노이즈. 귀 아래를 지나감.
실행: <bpy 파이썬> blender/build_headband.py"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT_FBX = os.path.join(ROOT, "unity", "Assets", "Resources", "Equipment", "Headband_Blue.fbx")
OUT_ICON = os.path.join(ROOT, "unity", "Assets", "Resources", "ItemIcons", "headband_blue.png")
PRE = os.path.join(ROOT, "previews")

bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "exports", "Moomin.blend"))
sc = bpy.context.scene
for o in list(bpy.data.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
rig = bpy.data.objects["MoominRig"]; rig.data.pose_position = "REST"
if rig.animation_data: rig.animation_data.action = None
moomin = bpy.data.objects["Moomin"]
assert moomin.matrix_world == Matrix.Identity(4), "민미 메시가 원점·기본 변환이어야 장비가 맞음"

# 민미 머리 타원(build_moomin.py 와 같은 값, three.js 단위)
HC, HR = Vector((0, 1.62, 0)), Vector((0.56, 0.5, 0.5))
def head_pt(phi, th, off):
    d = Vector((math.cos(th) * math.sin(phi), math.sin(th), math.cos(th) * math.cos(phi)))
    p = HC + Vector((d.x * HR.x, d.y * HR.y, d.z * HR.z)); n = Vector((d.x / HR.x, d.y / HR.y, d.z / HR.z)).normalized()
    return p + n * off, n

R0, NS, NR = 0.072, 320, 22          # 관 굵기(three 단위) · 둘레 조각 · 관 단면 조각
bm = bmesh.new(); rings = []
for i in range(NS):
    phi = 2 * math.pi * i / NS
    th = 0.56 + 0.16 * math.cos(phi)                                  # 앞(이마) 높고 뒤 낮게
    c, n = head_pt(phi, th, R0 * 0.62)
    c2, _ = head_pt(phi + 0.01, 0.56 + 0.16 * math.cos(phi + 0.01), R0 * 0.62); t = (c2 - c).normalized()
    b = t.cross(n).normalized(); n = b.cross(t).normalized()
    fold = 0.5 + 0.5 * math.sin(30 * phi + 0.8 * math.sin(5 * phi) + 0.9 * noise.noise(Vector((math.cos(phi) * 3, math.sin(phi) * 3, 0.5))))    # 곱창처럼 오므라든 주름(둘레를 따라)
    ring = []
    for j in range(NR):
        psi = 2 * math.pi * j / NR
        r = R0 * (0.86 + 0.26 * fold ** 1.5 + 0.05 * math.sin(3 * psi + 9 * phi))
        r += R0 * 0.06 * noise.noise(Vector((math.cos(phi) * 9, math.sin(phi) * 9, psi * 2.0)))   # 보송한 결
        off = n * math.cos(psi) * r * 0.82 + b * math.sin(psi) * r * 1.12   # 머리 쪽으로 살짝 납작
        ring.append(bm.verts.new(T(*(c + off))))
    rings.append(ring)
for i in range(NS):
    a, b2 = rings[i], rings[(i + 1) % NS]
    for j in range(NR): f = bm.faces.new((a[j], a[(j + 1) % NR], b2[(j + 1) % NR], b2[j])); f.smooth = True
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
# 보송보송: 아주 잔 노이즈로 표면을 흔듦
for v in bm.verts: v.co += v.normal * noise.noise(v.co * 160) * 0.0022
FUZZ = mat("Headband_Fuzz", (0.36, 0.64, 0.9), rough=0.95, sheen=1.0)
hb = to_object("Headband_Blue", bm, [FUZZ])

# ── 내보내기(머리띠만)
os.makedirs(os.path.dirname(OUT_FBX), exist_ok=True)
bpy.ops.object.select_all(action="DESELECT"); hb.select_set(True); bpy.context.view_layer.objects.active = hb
bpy.ops.export_scene.fbx(filepath=OUT_FBX, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_anim=False, path_mode="AUTO")
print("✔", OUT_FBX, len(hb.data.vertices))

# ── 렌더: 아이콘(머리띠만, 투명) · 착용 모습
sc.render.engine = "CYCLES"; sc.cycles.samples = 48; sc.cycles.use_denoising = True; sc.cycles.device = "CPU"; sc.view_settings.view_transform = "AgX"
w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.75, 0.82, 0.92, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 1.0; sc.world = w
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.4; sun.rotation_euler = (0.8, 0.15, 0.7); sc.collection.objects.link(sun)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
def shoot(path, loc, target, size, lens=55, transparent=False, hide=()):
    for o in hide: o.hide_render = True
    cam.location = loc; cam.data.lens = lens; cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    sc.render.film_transparent = transparent; sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGBA" if transparent else "RGB"
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)
    for o in hide: o.hide_render = False
os.makedirs(os.path.dirname(OUT_ICON), exist_ok=True); os.makedirs(PRE, exist_ok=True)
hc = T(*HC) + Vector((0, 0, 0.12))
shoot(OUT_ICON, hc + Vector((0.6, -0.85, 0.85)), hc + Vector((0, 0, 0.0)), 256, lens=58, transparent=True, hide=[moomin])
shoot(os.path.join(PRE, "headband_worn.png"), hc + Vector((0.9, -1.6, 0.35)), hc + Vector((0, 0, -0.08)), 640, lens=60)
shoot(os.path.join(PRE, "headband_worn_back.png"), hc + Vector((-1.0, 1.5, 0.6)), hc + Vector((0, 0, -0.05)), 640, lens=60)
print("✔ renders")

# ── 장비 화면 가운데 모습(EquipmentPreview 와 같은 각도: 앞에서 18° 돈 곳, 거리 3.3m, 시야 26°, 560×640) → tools/equipment_mock.py
c = Vector((0, 0, 0.62)); yaw = math.radians(18); d = Vector((math.sin(yaw), -math.cos(yaw), 0))   # 민미 정면 = -Y
cam.data.sensor_fit = "VERTICAL"; cam.data.angle_y = math.radians(26)
cam.location = c + d * 3.3 + Vector((0, 0, 0.25)); cam.rotation_euler = (c - cam.location).to_track_quat("-Z", "Y").to_euler()
sc.render.film_transparent = True; sc.render.resolution_x, sc.render.resolution_y = 560, 640; sc.render.image_settings.color_mode = "RGBA"
sc.render.filepath = os.path.join(ROOT, "exports", "equip_portrait.png"); bpy.ops.render.render(write_still=True); print("✔ portrait")
