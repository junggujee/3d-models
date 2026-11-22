"""
타이틀 화면 미리보기용 배경: 게임 맵(exports/City.glb)의 성과 산맥을 타이틀 카메라(Unity Title/TitleCamera 와 같은 궤도)에서 렌더.
  color.png(하늘 투명) + dist.png(카메라 거리 0~1, 안개 합성용). tools/title_mock.py 가 하늘·안개·UI 를 얹음.
  Unity 에서는 이 그림을 쓰지 않음(실제 맵을 실시간으로 비춤) — 디자인 확인용.
실행:  <bpy 파이썬> blender/render_title_bg.py <출력 폴더> [궤도 각도(도)]
"""
import bpy, math, json, os, sys
from mathutils import Vector

args = [a for a in sys.argv[1:] if not a.endswith(".py")]
OUT = args[0] if args else "title_render"; ANGLE = float(args[1]) if len(args) > 1 else 0.0
os.makedirs(OUT, exist_ok=True)
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
GLB = os.path.join(ROOT, "exports", "City.glb")
META = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "WorldMap", "WorldMapData.json")
lm = {l["id"]: l["blender"] for l in json.load(open(META, encoding="utf-8"))["landmarks"]}

# TitleCamera 와 같은 값: 성과 산지 사이(성 쪽 0.35)를 중심으로, 광장 쪽에서 반경 R·높이 H 로 천천히 돎
c, h, p = Vector(lm["castle"] + [0]), Vector(lm["highland"] + [0]), Vector(lm["plaza"] + [0])
center = c.lerp(h, 0.35); R, H, LOOK_UP = 175.0, 30.0, 34.0
base = math.atan2(p.y - center.y, p.x - center.x) + math.radians(ANGLE)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=GLB)
for o in bpy.data.objects:
    if "Collision" in o.name: o.hide_render = True
sc = bpy.context.scene; sc.render.resolution_x, sc.render.resolution_y = 1600, 900
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); cam.data.lens = 26; cam.data.clip_end = 3000; cam.data.shift_x = -0.16
sc.collection.objects.link(cam); sc.camera = cam
cam.location = center + Vector((math.cos(base) * R, math.sin(base) * R, H))
look = center + Vector((0, 0, LOOK_UP)); cam.rotation_euler = (look - cam.location).to_track_quat("-Z", "Y").to_euler()

sc.render.engine = "CYCLES"; sc.cycles.samples = 28; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True
sc.view_settings.view_transform = "AgX"; sc.render.film_transparent = True
sc.world = bpy.data.worlds.new("W"); sc.world.use_nodes = True
bg = sc.world.node_tree.nodes["Background"]; bg.inputs[0].default_value = (0.55, 0.66, 0.85, 1); bg.inputs[1].default_value = 0.8
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.6; sun.data.angle = math.radians(4); sun.data.color = (1.0, 0.9, 0.78)
sun.rotation_euler = (math.radians(62), 0, base + math.radians(140)); sc.collection.objects.link(sun)   # 낮은 옆빛: 성벽·산 능선에 그늘
sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGBA"
def render(name): sc.render.filepath = os.path.join(OUT, name + ".png"); bpy.ops.render.render(write_still=True); print("✔", name)
render("color")

mat = bpy.data.materials.new("Dist"); mat.use_nodes = True; nt = mat.node_tree; nt.nodes.clear()
out = nt.nodes.new("ShaderNodeOutputMaterial"); em = nt.nodes.new("ShaderNodeEmission"); cd = nt.nodes.new("ShaderNodeCameraData"); mr = nt.nodes.new("ShaderNodeMapRange")
mr.inputs[1].default_value = 0; mr.inputs[2].default_value = 900
nt.links.new(cd.outputs["View Distance"], mr.inputs[0]); nt.links.new(mr.outputs[0], em.inputs[0]); nt.links.new(em.outputs[0], out.inputs[0])
sc.view_settings.view_transform = "Standard"; sc.cycles.samples = 2; sc.cycles.use_denoising = False; sun.hide_render = True; bg.inputs[1].default_value = 0
bpy.context.view_layer.material_override = mat; render("dist")
