"""도시 배경(변환된 OBJ / FBX / glTF·GLB)을 게임용 맵으로 정리해서 Unity 로 내보냄.
사용: python build_city.py <변환한 도시 파일> [--scale 미터당배율] [--target-tris 400000] [--preview]

하는 일
 1. 불러오기 → 빈 오브젝트·카메라·조명 제거, 변환(위치·회전·크기) 적용
 2. 크기를 미터로 맞춤(SketchUp 은 인치로 내보내는 경우가 많아 자동 감지: 건물 높이가 비정상적으로 크면 0.0254 배)
 3. 바닥 높이 = 0, 가운데 = 원점으로 이동
 4. 같은 재질끼리 합쳐서 드로우콜 감소, 삼각형이 많으면 줄임(Decimate)
 5. 충돌용 메시(City_Collision): 단순화한 사본. Unity 에서 MeshCollider 로만 쓰고 보이지 않음
 6. PlayerSpawn: 도시 중앙 근처의 가장 넓은 빈 바닥을 찾아 빈 오브젝트로 표시
 7. unity/Assets/City/City.fbx + exports/City.glb + previews/city_*.png
"""
import sys, os, math, argparse
sys.path.insert(0, os.path.dirname(__file__))
import bpy, bmesh
from mathutils import Vector, Matrix
from lib import reset_scene

ap = argparse.ArgumentParser(); ap.add_argument("src"); ap.add_argument("--scale", type=float, default=0); ap.add_argument("--target-tris", type=int, default=400000)
ap.add_argument("--collision-ratio", type=float, default=0.25); ap.add_argument("--preview", action="store_true")
args = ap.parse_args()
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.join(HERE, "..")
OUT_UNITY = os.path.join(ROOT, "unity", "Assets", "City"); OUT_EXP = os.path.join(ROOT, "exports"); OUT_PRE = os.path.join(ROOT, "previews")

reset_scene()
ext = os.path.splitext(args.src)[1].lower()
if ext == ".obj": bpy.ops.wm.obj_import(filepath=args.src)
elif ext == ".fbx": bpy.ops.import_scene.fbx(filepath=args.src)
elif ext in (".glb", ".gltf"): bpy.ops.import_scene.gltf(filepath=args.src)
else: sys.exit(f"지원하지 않는 형식: {ext} (OBJ, FBX, glTF/GLB 로 변환해 주세요. Blender 5 는 COLLADA(.dae)를 읽지 못합니다)")

# 1. 정리
for o in list(bpy.data.objects):
    if o.type != "MESH": bpy.data.objects.remove(o, do_unlink=True)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
if not meshes: sys.exit("메시가 없습니다.")
bpy.ops.object.select_all(action="DESELECT")
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.make_single_user(object=True, obdata=True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

def bounds(objs):
    lo = Vector((1e18,) * 3); hi = Vector((-1e18,) * 3)
    for o in objs:
        for v in o.data.vertices:
            p = o.matrix_world @ v.co; lo = Vector(map(min, lo, p)); hi = Vector(map(max, hi, p))
    return lo, hi

# 2. 단위 → 미터
lo, hi = bounds(meshes); size = hi - lo
scale = args.scale or (0.0254 if size.z > 400 else 1.0)    # 높이 400 이 넘으면 인치로 판단
print(f"[city] 원본 크기 {size.x:.1f} x {size.y:.1f} x {size.z:.1f}, 배율 {scale}")

# 3. 바닥 0, 가운데 원점
center = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
for o in meshes:
    o.data.transform(Matrix.Translation(-center)); o.data.transform(Matrix.Diagonal((scale, scale, scale, 1))); o.matrix_world = Matrix.Identity(4)

# 4. 재질별로 합치기
bpy.ops.object.select_all(action="SELECT"); bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join(); city = bpy.context.view_layer.objects.active; city.name = city.data.name = "City"
bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT"); bpy.ops.mesh.remove_doubles(threshold=0.001); bpy.ops.object.mode_set(mode="OBJECT")
tris = sum(len(p.vertices) - 2 for p in city.data.polygons)
print(f"[city] 삼각형 {tris:,}, 재질 {len(city.data.materials)}")
if tris > args.target_tris:
    m = city.modifiers.new("Decimate", "DECIMATE"); m.ratio = args.target_tris / tris; bpy.ops.object.modifier_apply(modifier=m.name)
    print(f"[city] 단순화 → {sum(len(p.vertices) - 2 for p in city.data.polygons):,}")
bpy.ops.mesh.customdata_custom_splitnormals_clear() if city.data.has_custom_normals else None
for p in city.data.polygons: p.use_smooth = False

# 5. 충돌 메시
col = city.copy(); col.data = city.data.copy(); col.name = col.data.name = "City_Collision"; bpy.context.collection.objects.link(col)
col.data.materials.clear()
m = col.modifiers.new("Decimate", "DECIMATE"); m.ratio = args.collision_ratio
bpy.ops.object.select_all(action="DESELECT"); col.select_set(True); bpy.context.view_layer.objects.active = col; bpy.ops.object.modifier_apply(modifier=m.name)

# 6. 시작 위치: 가운데에서 바깥으로 격자 탐색 → 위쪽 3m 안에 아무것도 없고 아래에 바닥이 있는 곳
from mathutils.bvhtree import BVHTree
bm = bmesh.new(); bm.from_mesh(city.data); tree = BVHTree.FromBMesh(bm); bm.free()
lo, hi = bounds([city]); spawn = None
def free(x, y):
    hit = tree.ray_cast(Vector((x, y, hi.z + 5)), Vector((0, 0, -1)))
    if hit[0] is None: return None
    ground = hit[0]
    if hit[1].z < 0.7: return None                                   # 경사가 심하면 제외
    for dx, dy in ((0, 0), (1.5, 0), (-1.5, 0), (0, 1.5), (0, -1.5)):  # 사방 1.5m 가 비어 있어야 함
        up = tree.ray_cast(ground + Vector((dx, dy, 0.3)), Vector((0, 0, 1)), 3.0)
        if up[0] is not None: return None
        down = tree.ray_cast(Vector((x + dx, y + dy, ground.z + 1)), Vector((0, 0, -1)), 2.0)
        if down[0] is None or abs(down[0].z - ground.z) > 0.4: return None
    return ground
for r in range(0, 200, 2):
    for a in range(0, 360, 20 if r else 360):
        x, y = r * math.cos(math.radians(a)), r * math.sin(math.radians(a))
        g = free(x, y)
        if g is not None: spawn = g; break
    if spawn is not None: break
sp = bpy.data.objects.new("PlayerSpawn", None); sp.location = spawn if spawn is not None else Vector((0, 0, 1)); bpy.context.collection.objects.link(sp)
print(f"[city] 시작 위치 {tuple(round(c, 2) for c in sp.location)} ({'자동' if spawn else '찾지 못해 원점'})")
lo, hi = bounds([city]); print(f"[city] 최종 크기 {hi.x-lo.x:.1f} x {hi.y-lo.y:.1f} x {hi.z-lo.z:.1f} m")

# 7. 내보내기
os.makedirs(OUT_UNITY, exist_ok=True); os.makedirs(OUT_EXP, exist_ok=True)
bpy.ops.object.select_all(action="SELECT")
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT_UNITY, "City.fbx"), use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=True, axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
bpy.ops.export_scene.gltf(filepath=os.path.join(OUT_EXP, "City.glb"), export_format="GLB", use_selection=True)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_EXP, "City.blend"))
if args.preview:
    from lib import preview
    col.hide_render = True; os.makedirs(OUT_PRE, exist_ok=True); r = max(hi.x - lo.x, hi.y - lo.y)
    preview(os.path.join(OUT_PRE, "city_overview.png"), (r * 0.7, -r * 0.9, r * 0.7), (0, 0, 0), size=900, light_energy=4)
print("[city] 완료")
