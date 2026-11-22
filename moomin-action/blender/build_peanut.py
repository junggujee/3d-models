"""자원 오브젝트: 땅콩빵(사용자 사진 — 하얀 메모리폼 땅콩 모양 베개). 가운데가 잘록한 둥근 직육면체 + 엇갈린 바구니 짜임 천(노멀 맵) + 아래쪽 1/3 높이의 둘레 솔기.
부수면 두 쪽(LobeL · LobeR)이 갈라져 튀어 나가도록 가운데에서 잘라 두 물체로 내보냄(자른 면 = 노르스름한 메모리폼 속).
  → unity/Assets/Models/PeanutBread.fbx (뼈 없음, 물체 2개 — 원점은 각 쪽 가운데)
  → unity/Assets/Resources/ItemIcons/peanut_piece.png (땅콩빵 조각 아이콘) · previews/peanut_*.png
크기 1.0 × 0.48 × 0.3 m. 실행: <bpy 파이썬> blender/build_peanut.py"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
UNITY = os.path.join(ROOT, "unity", "Assets", "Models"); ICON = os.path.join(ROOT, "unity", "Assets", "Resources", "ItemIcons", "peanut_piece.png"); PRE = os.path.join(ROOT, "previews")
WEAVE = os.path.join(UNITY, "Textures", "Weave_Normal.png")
reset_scene()
CLOTH = mat("Peanut_Cloth", (0.93, 0.92, 0.88), rough=0.85, sheen=0.4)
FOAM = mat("Peanut_Foam", (0.95, 0.88, 0.66), rough=1.0)
SEAM = mat("Peanut_Seam", (0.86, 0.85, 0.8), rough=0.9)
MATS = [CLOTH, FOAM, SEAM]; I_CLOTH, I_FOAM, I_SEAM = range(3)
if os.path.exists(WEAVE):
    nt = CLOTH.node_tree; b = nt.nodes["Principled BSDF"]; img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(WEAVE); img.image.colorspace_settings.name = "Non-Color"
    mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (6, 6, 1); tc = nt.nodes.new("ShaderNodeTexCoord"); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 1.0
    nt.links.new(tc.outputs["UV"], mp.inputs[0]); nt.links.new(mp.outputs[0], img.inputs[0]); nt.links.new(img.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"])

AX, AY, AZ, PXY, PZ = 0.5, 0.25, 0.17, 2.8, 3.0
def pinch(x): return 1 - 0.3 * math.exp(-(x / (0.42 * AX)) ** 2)           # 가운데 잘록(폭)
def pinch_h(x): return 1 - 0.12 * math.exp(-(x / (0.45 * AX)) ** 2)         # 가운데 살짝 낮음(높이)
def radius(d):
    """중심에서 방향 d 로 표면까지(초타원체 + 잘록함, 몇 번 반복으로 맞춤)."""
    r = 0.3
    for _ in range(6):
        x = r * d.x; ay, az = AY * pinch(x), AZ * pinch_h(x)
        k = (abs(d.x / AX) ** PXY + abs(d.y / ay) ** PXY) ** (PZ / PXY) + abs(d.z / az) ** PZ; r = k ** (-1 / PZ)
    return r
bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=96, v_segments=56, radius=1.0)
for v in bm.verts: d = v.co.normalized(); v.co = d * radius(d) + Vector((0, 0, AZ))
bm.normal_update()
uvl = bm.loops.layers.uv.new("UVMap")
for f in bm.faces:
    f.smooth = True; f.material_index = I_CLOTH; n = f.normal; ax = max(range(3), key=lambda k: abs(n[k]))
    for l in f.loops: co = l.vert.co; o = [co[(ax + 1) % 3], co[(ax + 2) % 3]]; l[uvl].uv = (o[0], o[1])   # 1 UV = 1m (재질 타일 6 → 짜임 칸 약 1cm)
# 솔기: 아래쪽 1/3 높이에서 둘레를 따라 도는 가는 관
zs = -AZ * 0.35; ring = []
for k in range(160):
    a = 2 * math.pi * k / 160; d = Vector((math.cos(a), math.sin(a), zs / 0.5)).normalized()
    for _ in range(8):   # 높이 zs 인 표면점을 찾음
        r = radius(d); p = d * r
        if abs(p.z - zs) < 1e-4: break
        d = Vector((p.x, p.y, zs)).normalized()
    p = d * radius(d); ring.append(p + Vector((p.x, p.y, 0)).normalized() * 0.006 + Vector((0, 0, AZ)))
ring += ring[:2]
tb, _ = tube(ring, lambda t: 0.008, sides=8, mat_idx=I_SEAM)
me = bpy.data.meshes.new("t"); tb.to_mesh(me); bm.from_mesh(me); bpy.data.meshes.remove(me)

def half(side):
    """x=0 에서 잘라 한쪽만 남기고 자른 면을 메폼 속으로 채움."""
    h = bm.copy(); geom = h.verts[:] + h.edges[:] + h.faces[:]
    res = bmesh.ops.bisect_plane(h, geom=geom, plane_co=(0, 0, 0), plane_no=(1, 0, 0), clear_inner=side > 0, clear_outer=side < 0)
    # 자른 자리의 열린 고리(몸통 1개 + 솔기 관 2개)를 가운데 점으로 부채꼴 채움
    edges = [e for e in h.edges if e.is_boundary and all(abs(v.co.x) < 1e-4 for v in e.verts)]
    left = set(edges)
    while left:
        e0 = left.pop(); loop = [e0.verts[0], e0.verts[1]]
        while True:
            nxt = next((e for e in left if loop[-1] in e.verts), None)
            if nxt is None: break
            left.discard(nxt); v = nxt.other_vert(loop[-1])
            if v == loop[0]: break
            loop.append(v)
        if len(loop) < 3: continue
        c = h.verts.new(sum((v.co for v in loop), Vector()) / len(loop))
        for a, b in zip(loop, loop[1:] + loop[:1]):
            try: f = h.faces.new((a, b, c)); f.material_index = I_FOAM; f.smooth = False
            except ValueError: pass
    bmesh.ops.recalc_face_normals(h, faces=h.faces)
    ob = to_object("Lobe" + ("R" if side > 0 else "L"), h, MATS)
    # 원점 = 그 쪽 가운데(갈라질 때 자연스럽게 돌도록)
    c = sum((v.co for v in ob.data.vertices), Vector()) / len(ob.data.vertices); c.z = AZ
    ob.data.transform(Matrix.Translation(-c)); ob.location = c; return ob
L, Rr = half(-1), half(1)
root = bpy.data.objects.new("PeanutBread", None); bpy.context.collection.objects.link(root)
for o in (L, Rr): o.parent = root

os.makedirs(UNITY, exist_ok=True)
bpy.ops.object.select_all(action="DESELECT")
for o in (root, L, Rr): o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(UNITY, "PeanutBread.fbx"), use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_anim=False)
print("✔ PeanutBread.fbx", len(L.data.vertices) + len(Rr.data.vertices))

sc = bpy.context.scene; sc.render.engine = "CYCLES"; sc.cycles.samples = 40; sc.cycles.use_denoising = True; sc.view_settings.view_transform = "AgX"
w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.72, 0.8, 0.9, 1); sc.world = w
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.2; sun.rotation_euler = (0.85, 0.2, 0.9); sc.collection.objects.link(sun)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
def shoot(path, loc, target, size, transparent=False, lens=50):
    cam.location = loc; cam.data.lens = lens; cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    sc.render.film_transparent = transparent; sc.render.resolution_x = sc.render.resolution_y = size; sc.render.image_settings.color_mode = "RGBA" if transparent else "RGB"
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)
os.makedirs(PRE, exist_ok=True); os.makedirs(os.path.dirname(ICON), exist_ok=True)
shoot(os.path.join(PRE, "peanut_whole.png"), Vector((1.2, -1.9, 1.1)), Vector((0, 0, 0.12)), 640)
L.location.x -= 0.22; L.rotation_euler = (0.2, -0.5, 0.3); Rr.location.x += 0.22; Rr.rotation_euler = (-0.15, 0.6, -0.2); Rr.location.z += 0.08
shoot(os.path.join(PRE, "peanut_broken.png"), Vector((1.2, -1.9, 1.1)), Vector((0, 0, 0.12)), 640)
L.hide_render = True; Rr.location = (0.2, 0, 0.12); Rr.rotation_euler = (0.3, 0.9, 0.4)
shoot(ICON, Vector((0.2 + 0.55, -0.8, 0.75)), Vector((0.2, 0, 0.12)), 256, transparent=True, lens=55)
print("✔ renders")
