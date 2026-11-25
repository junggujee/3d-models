"""선착장 미리보기(Cycles): exports/Seaside.blend + 원판 땅(잔디) + 물(물결 노멀 · 투명) + 통통배(Boat.blend) + 하늘.
실행: <bpy 파이썬> blender/render_seaside_preview.py   출력: previews/seaside_harbor.png · seaside_pier.png · seaside_underwater.png"""
import os, sys, math
import bpy, bmesh
from mathutils import Vector, Matrix
HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE); EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
bpy.ops.wm.open_mainfile(filepath=os.path.join(EXP, "Seaside.blend"))
sc = bpy.context.scene
for o in bpy.data.objects:
    if o.name.startswith(("Seaside_Collision", "Seaside_GateCollision")): o.hide_render = True
C = Vector((-50.012, 28.328)); R = 586.0; N = 24; TIP = math.radians(-36.4136); SEA = -0.8
def tip_frame():
    T = C + R * Vector((math.cos(TIP), math.sin(TIP))); U = (T - C).normalized(); V = Vector((-U.y, U.x)); return T, U, V
T, U, V = tip_frame()
def H(a, b, c=0.0): p = T + U * a + V * b; return Vector((p.x, p.y, c))
# 땅(원판 일부 — 선착장 둘레 300m)
bm = bmesh.new(); pts = [C + R * Vector((math.cos(TIP + 2 * math.pi * k / N), math.sin(TIP + 2 * math.pi * k / N))) for k in range(N)]
bm.faces.new([bm.verts.new((p.x, p.y, 0.0)) for p in pts]); land = bpy.data.objects.new("Land", bpy.data.meshes.new("Land")); bm.to_mesh(land.data); sc.collection.objects.link(land)
g = bpy.data.materials.new("Grass"); g.use_nodes = True; g.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.32, 0.55, 0.22, 1); land.data.materials.append(g)
# 물: 넓은 판 + 물결(노이즈 → 범프) + 투과
bm = bmesh.new(); bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=900); sea = bpy.data.objects.new("Sea", bpy.data.meshes.new("Sea")); bm.to_mesh(sea.data); sc.collection.objects.link(sea)
sea.location = (T.x, T.y, SEA)
w = bpy.data.materials.new("Water"); w.use_nodes = True; nt = w.node_tree; b = nt.nodes["Principled BSDF"]
b.inputs["Base Color"].default_value = (0.05, 0.32, 0.42, 1); b.inputs["Roughness"].default_value = 0.04; b.inputs["Transmission Weight"].default_value = 0.85; b.inputs["IOR"].default_value = 1.33
nz = nt.nodes.new("ShaderNodeTexNoise"); nz.inputs["Scale"].default_value = 0.35; nz.inputs["Detail"].default_value = 6
bp = nt.nodes.new("ShaderNodeBump"); bp.inputs["Strength"].default_value = 0.25; nt.links.new(nz.outputs["Fac"], bp.inputs["Height"]); nt.links.new(bp.outputs["Normal"], b.inputs["Normal"])
vol = nt.nodes.new("ShaderNodeVolumeAbsorption"); vol.inputs["Color"].default_value = (0.25, 0.65, 0.7, 1); vol.inputs["Density"].default_value = 0.12
nt.links.new(vol.outputs[0], nt.nodes["Material Output"].inputs["Volume"]); sea.data.materials.append(w)
sol = bpy.data.objects.new("SeaBottomCap", None)
# 통통배
with bpy.data.libraries.load(os.path.join(EXP, "Boat.blend")) as (src, dst): dst.objects = [n for n in src.objects]
boat_root = None
for o in dst.objects:
    if o is None: continue
    sc.collection.objects.link(o)
    if o.name == "Boat": boat_root = o
bpos = H(36.5, 3.25, SEA); boat_root.location = bpos; boat_root.rotation_euler = (0, 0, math.atan2(U.y, U.x) + math.pi / 2)   # 배 앞(-Y) = 바다 쪽(u)
# 빛 · 하늘
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.2; sun.data.angle = 0.05; sc.collection.objects.link(sun)
sun.rotation_euler = (math.radians(50), 0, math.atan2(U.y, U.x) + 2.4)
wd = bpy.data.worlds.new("Sky"); wd.use_nodes = True; sky = wd.node_tree.nodes.new("ShaderNodeTexSky")
bg = wd.node_tree.nodes["Background"]; bg.inputs[1].default_value = 0.6; sc.world = wd
try: sky.sky_type = "MULTIPLE_SCATTERING"
except Exception: pass
wd.node_tree.links.new(sky.outputs[0], bg.inputs[0])
sc.render.engine = "CYCLES"; sc.cycles.device = "CPU"; sc.cycles.samples = 64; sc.cycles.use_denoising = True; sc.render.resolution_x, sc.render.resolution_y = 1280, 720
sc.cycles.max_bounces = 6; sc.cycles.transmission_bounces = 6; sc.cycles.volume_bounces = 1
sc.view_settings.view_transform = "AgX"; sc.view_settings.look = "AgX - Punchy" if "AgX - Punchy" in [x for x in ["AgX - Punchy"]] else "None"
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
def shot(name, loc, tgt, lens=28):
    cam.location = loc; cam.data.lens = lens; cam.rotation_euler = (tgt - loc).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(PRE, name + ".png"); bpy.ops.render.render(write_still=True); print("✔", name)
shot("seaside_harbor", H(-34, -26, 14), H(18, 2, 0), 24)
shot("seaside_pier", H(52, 18, 4.5), H(28, -2, 0.5), 30)
shot("seaside_underwater", H(30, -12, -4.2), H(46, 4, -6.5), 22)
