"""스타 스파크 검: 검은 금속 칼날 + 빛나는 날 + 붉은 육각 가드 + 금속 손잡이. 글씨·로고 없음.
원점 = 손잡이 중앙(민미 손 소켓에 그대로 붙임), 칼날 방향 = +Z(Blender) → Unity에서는 +Y.
SparkPoint_* 빈 오브젝트 = 스파크 파티클을 튀길 위치. 실행: python build_sword.py [--preview]"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *

OUT = os.path.join(os.path.dirname(__file__), "..", "exports"); PRE = os.path.join(os.path.dirname(__file__), "..", "previews")
reset_scene()
BLADE = mat("Sword_Blade", (0.03, 0.03, 0.04), rough=0.25, metal=0.9)
EDGE = mat("Sword_Edge", (1, 1, 1), rough=0.5, emit=(0.4, 0.9, 1.0), emit_strength=4.0)   # Unity에서 무지개색으로 순환시킴
GUARD = mat("Sword_Guard", (0.8, 0.05, 0.06), rough=0.4, emit=(1.0, 0.1, 0.08), emit_strength=0.8)
METAL = mat("Sword_Metal", (0.22, 0.23, 0.26), rough=0.2, metal=1.0)
CHROME = mat("Sword_Chrome", (0.9, 0.92, 0.95), rough=0.08, metal=1.0)
M = [BLADE, EDGE, GUARD, METAL, CHROME]

def box_prism(pts2d, depth, mat_idx, y0=0.0):
    """XZ 평면의 다각형을 Y 방향으로 압출"""
    bm = bmesh.new(); f = bm.faces.new([bm.verts.new((x, y0 - depth / 2, z)) for x, z in pts2d])
    r = bmesh.ops.extrude_face_region(bm, geom=[f]); verts = [v for v in r["geom"] if isinstance(v, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, depth, 0), verts=verts); bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    for fc in bm.faces: fc.material_index = mat_idx
    return bm

def cyl(r, h, z0, mat_idx, seg=20):
    bm = bmesh.new(); bmesh.ops.create_cone(bm, cap_ends=True, segments=seg, radius1=r, radius2=r, depth=h, matrix=Matrix.Translation((0, 0, z0 + h / 2)))
    for f in bm.faces: f.smooth = True; f.material_index = mat_idx
    return bm

L, W, TIP = 0.95, 0.045, 0.1   # 칼날 길이/반폭/비스듬한 칼끝
z0 = 0.16
blade = box_prism([(-W, z0), (W, z0), (W, z0 + L - TIP), (-W * 0.2, z0 + L), (-W, z0 + L - TIP * 0.4)], 0.028, 0)
edge = box_prism([(W - 0.012, z0 + 0.02), (W + 0.004, z0 + 0.02), (W + 0.004, z0 + L - TIP), (W - 0.012, z0 + L - TIP)], 0.03, 1)
hexpts = [(0.17 * math.cos(math.pi / 3 * i), 0.17 * math.sin(math.pi / 3 * i) * 0.55 + z0 - 0.02) for i in range(6)]
guard = box_prism(hexpts, 0.07, 2)
grip = cyl(0.026, 0.3, -0.16, 3)
rings = [cyl(0.032, 0.02, -0.11, 4), cyl(0.032, 0.02, 0.07, 4)]
pommel = ellipsoid((0, 0, 0), (0.04, 0.04, 0.04), seg=20, rings=14)
pb = pommel; pb.transform(Matrix.Translation((0, 0, -0.18)) @ Matrix.Diagonal((1, 1, 1, 1)))  # 폼멜
for f in pb.faces: f.material_index = 2
objs = [to_object(n, b, M) for n, b in (("Blade", blade), ("BladeEdge", edge), ("Guard", guard), ("Grip", grip), ("Pommel", pb))]
objs += [to_object(f"Ring{i}", r, M) for i, r in enumerate(rings)]
sword = join(objs, "StarSparkSword")
for n, p in (("SparkPoint_GuardL", (0.17, 0, z0)), ("SparkPoint_GuardR", (-0.17, 0, z0)), ("SparkPoint_Grip", (0, 0, 0.1)), ("SparkPoint_Tip", (0, 0, z0 + L))):
    e = bpy.data.objects.new(n, None); e.empty_display_type = "SPHERE"; e.empty_display_size = 0.02; e.location = p; e.parent = sword; bpy.context.collection.objects.link(e)
    # 자식 위치는 부모 좌표 기준 그대로 둠

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); sword.rotation_euler = (0, 0.5, 0.3)
    preview(os.path.join(PRE, "sword.png"), (0.5, -1.7, 0.5), (0, 0, 0.45), size=640, light_energy=4)
sword.rotation_euler = (0, 0, 0)
glb, fbx = export_all("StarSparkSword", OUT, [sword] + [c for c in sword.children])
print("EXPORTED", glb)
