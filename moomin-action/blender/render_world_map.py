"""
게임 맵(exports/City.glb, build_map.py 결과)을 '입체적인 2D 지도'용으로 렌더링.
  - 마을의 긴 축(남서 → 북동)이 가로가 되도록 카메라를 돌려 비스듬히(고도 50°) 내려다보는 정사영 → 건물·산의 높이가 보임
  - 주요 장소 5곳(성 · 마을 광장 · 산지 · 동쪽 마을 · 서쪽 마을)을 지붕·석재·눈 면의 분포로 자동으로 찾음
  - 패스 3장: 색(color) · 법선(normal, 외곽선용) · 높이(height, 바탕 처리용)  +  map_meta.json(지면 → 지도 좌표 변환, 장소 좌표)
실행:  <bpy 파이썬> blender/render_world_map.py <출력 폴더>   (그다음 tools/world_map_art.py 가 그림 완성)
"""
import bpy, numpy as np, math, json, os, sys
from mathutils import Vector, Euler
from bpy_extras.object_utils import world_to_camera_view

OUT = sys.argv[-1] if len(sys.argv) > 1 and not sys.argv[-1].endswith(".py") else "map_render"
os.makedirs(OUT, exist_ok=True)
GLB = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "exports", "City.glb")
RES_X, RES_Y = 2048, 1280
ELEVATION = 50.0          # 카메라 고도(도) — 90 이면 바로 위
ORTHO = 440.0             # 화면 가로 폭(m)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=GLB)
city = bpy.data.objects["City"]; bpy.data.objects["City_Collision"].hide_render = True
me = city.data; names = [m.name for m in me.materials]
n = len(me.polygons); C = np.empty(n * 3, np.float32); me.polygons.foreach_get("center", C); C = C.reshape(-1, 3)
MI = np.empty(n, np.int32); me.polygons.foreach_get("material_index", MI); AR = np.empty(n, np.float32); me.polygons.foreach_get("area", AR)
Mw = np.array(city.matrix_world); C = C @ Mw[:3, :3].T + Mw[:3, 3]
def faces(pred): return np.isin(MI, [i for i, s in enumerate(names) if pred(s)])
def centroid(mask): w = AR[mask]; return (C[mask, :2] * w[:, None]).sum(0) / w.sum()

# ── 주요 장소 찾기 ─────────────────────────────
roof = faces(lambda s: s.startswith("Roof_") and s != "Roof_Slate")
pts = C[roof, :2]; w = AR[roof]
# 마을 지붕을 2개 덩어리로(간단한 k-평균) → 긴 축 방향
c0, c1 = np.percentile(pts, 15, 0), np.percentile(pts, 85, 0)
for _ in range(20):
    lab = np.linalg.norm(pts - c0, axis=1) > np.linalg.norm(pts - c1, axis=1)
    c0 = (pts[~lab] * w[~lab, None]).sum(0) / w[~lab].sum(); c1 = (pts[lab] * w[lab, None]).sum(0) / w[lab].sum()
axis = c1 - c0; axis /= np.linalg.norm(axis)
east, west = (c1, c0) if axis[0] >= 0 else (c0, c1); axis = (east - west) / np.linalg.norm(east - west)
plaza = (east + west) / 2
def densest(mask, cell, within, avoid=None, refine=None):
    """mask 면의 개수가 가장 많은 칸(cell m) → 그 주변 refine m 안의 중심. within = 광장에서 이 거리 안만.
    면적이 아니라 개수: 길고 큰 성벽 면보다 잘게 나뉜 성 건물이 잡힘."""
    p = C[mask, :2]; a = np.ones(len(p), np.float32); keep = np.linalg.norm(p - plaza, axis=1) < within
    if avoid is not None: keep &= np.linalg.norm(p - avoid, axis=1) > 60
    p, a = p[keep], a[keep]; k = np.floor(p / cell).astype(int); keys, inv = np.unique(k, axis=0, return_inverse=True)
    best = keys[np.bincount(inv.ravel(), a).argmax()] * cell + cell / 2; m = np.linalg.norm(p - best, axis=1) < (refine or cell)
    return (p[m] * a[m, None]).sum(0) / a[m].sum()
# 성: 성곽 석재가 가장 빽빽한 곳(마을을 두른 성벽은 길게 퍼져 있어 밀도가 낮음), 마을 안은 제외
castle = densest(faces(lambda s: s == "Roof_Slate"), 20, 170, avoid=plaza, refine=30)
# 산지: 성벽 안쪽에서 눈 덮인 면이 가장 많은 봉우리
highland = densest(faces(lambda s: s == "Mountain_Snow"), 20, 170, refine=45)
spawn = bpy.data.objects["PlayerSpawn"].location
LANDMARKS = [("castle", "성", castle), ("plaza", "마을 광장", plaza), ("highland", "산지", highland), ("east", "동쪽 마을", east), ("west", "서쪽 마을", west)]
for k, nm, p in LANDMARKS: print(f"{nm:6s} {p.round(1)}")

def ground_z(x, y):
    """그 지점의 가장 높은 면 높이(장소 표시를 건물·산 위에)."""
    m = np.linalg.norm(C[:, :2] - [x, y], axis=1) < 6; return float(C[m, 2].max()) if m.any() else 0.0

# ── 카메라: 마을 축이 화면 가로가 되도록 ─────────
yaw = math.atan2(axis[1], axis[0]); tilt = math.radians(90 - ELEVATION)
sc = bpy.context.scene; sc.render.resolution_x, sc.render.resolution_y = RES_X, RES_Y
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); cam.data.type = "ORTHO"; cam.data.ortho_scale = ORTHO; cam.data.clip_end = 4000
cam.rotation_euler = Euler((tilt, 0, yaw), "XYZ"); sc.collection.objects.link(cam); sc.camera = cam   # 화면 오른쪽 = 마을 축(서 → 동)
up = np.array([-axis[1], axis[0]]); castle_side = 1 if (castle - plaza) @ up > 0 else -1
target = Vector((plaza[0], plaza[1], 0)) + Vector((up[0], up[1], 0)) * 35 * castle_side   # 성이 들어오도록 성 쪽으로 조금    # 성이 위쪽에 들어오도록 조금 북쪽
fwd = cam.rotation_euler.to_matrix() @ Vector((0, 0, -1)); cam.location = target - fwd * 1500
bpy.context.view_layer.update()

# ── 렌더 설정: 부드러운 햇빛(화면 왼쪽 위) + 하늘빛 ──
sc.render.engine = "CYCLES"; sc.cycles.samples = 24; sc.cycles.device = "CPU"; sc.cycles.use_denoising = True
sc.view_settings.view_transform = "Standard"; sc.render.film_transparent = True
sc.world = bpy.data.worlds.new("W"); sc.world.use_nodes = True; sc.world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.7, 0.8, 1); sc.world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.2; sun.data.angle = math.radians(6)
sun.rotation_euler = Euler((math.radians(48), 0, yaw + math.radians(35)), "XYZ"); sc.collection.objects.link(sun)
sc.render.image_settings.file_format = "PNG"; sc.render.image_settings.color_mode = "RGBA"

def render(name):
    sc.render.filepath = os.path.join(OUT, name + ".png"); bpy.ops.render.render(write_still=True); print("✔", name)

render("color")
# 법선 패스(외곽선): 화면 공간 법선을 색으로
def emission_override(build):
    mat = bpy.data.materials.new("Override"); mat.use_nodes = True; nt = mat.node_tree; nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial"); em = nt.nodes.new("ShaderNodeEmission"); nt.links.new(em.outputs[0], out.inputs[0]); build(nt, em); return mat
def normal_nodes(nt, em):
    geo = nt.nodes.new("ShaderNodeNewGeometry"); vt = nt.nodes.new("ShaderNodeVectorTransform"); vt.vector_type = "NORMAL"; vt.convert_from = "WORLD"; vt.convert_to = "CAMERA"
    mad = nt.nodes.new("ShaderNodeVectorMath"); mad.operation = "MULTIPLY_ADD"; mad.inputs[1].default_value = (.5, .5, .5); mad.inputs[2].default_value = (.5, .5, .5)
    nt.links.new(geo.outputs["Normal"], vt.inputs[0]); nt.links.new(vt.outputs[0], mad.inputs[0]); nt.links.new(mad.outputs[0], em.inputs[0])
def height_nodes(nt, em):
    geo = nt.nodes.new("ShaderNodeNewGeometry"); sep = nt.nodes.new("ShaderNodeSeparateXYZ"); mr = nt.nodes.new("ShaderNodeMapRange")
    mr.inputs[1].default_value = -2; mr.inputs[2].default_value = 120
    nt.links.new(geo.outputs["Position"], sep.inputs[0]); nt.links.new(sep.outputs[2], mr.inputs[0]); nt.links.new(mr.outputs[0], em.inputs[0])
sc.cycles.samples = 4; sc.cycles.use_denoising = False; sun.hide_render = True; sc.world.node_tree.nodes["Background"].inputs[1].default_value = 0
vl = bpy.context.view_layer
vl.material_override = emission_override(normal_nodes); render("normal")
vl.material_override = emission_override(height_nodes); render("height")

# ── 지면 → 지도 좌표(0~1, 왼쪽 아래 원점) 변환 + 장소 ──
def uv(x, y, z=0.0): v = world_to_camera_view(sc, cam, Vector((x, y, z))); return [round(v.x, 5), round(v.y, 5)]
o, ex, ey = uv(0, 0), uv(1, 0), uv(0, 1)
meta = {
    "image": [RES_X, RES_Y], "elevation": ELEVATION, "yawDeg": math.degrees(yaw),
    "ground": {"origin": o, "perX": [ex[0] - o[0], ex[1] - o[1]], "perY": [ey[0] - o[0], ey[1] - o[1]]},   # Blender 지면(x, y) → 지도 uv
    "spawnBlender": [round(spawn.x, 2), round(spawn.y, 2)],
    "landmarks": [{"id": k, "name": nm, "blender": [round(float(p[0]), 1), round(float(p[1]), 1)], "uv": uv(p[0], p[1], ground_z(p[0], p[1]))} for k, nm, p in LANDMARKS],
}
with open(os.path.join(OUT, "map_meta.json"), "w", encoding="utf-8") as f: json.dump(meta, f, ensure_ascii=False, indent=1)
print(json.dumps(meta, ensure_ascii=False))
