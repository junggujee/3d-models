"""고래고래 임금님(용궁의 보스) 소품: 금관 · 망토 → unity/Assets/Resources/Boss/KingCrown.fbx · KingCape.fbx(고래 모델 공간 — Unity BoneAttach 가 head · body 뼈에 붙임).
exports/Goraegorae.blend(build_whale.py 결과)를 열어 기본 자세로 맞춰 만듦. --preview: previews/whale_king.png · whale_king_back.png
실행: <bpy 파이썬> blender/build_whale_king.py [--preview]"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
import bpy
from mathutils import Vector
from lib import mat, to_object
import whale_king_parts as K

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "Boss"); PRE = os.path.join(ROOT, "previews"); os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "exports", "Goraegorae.blend"))
for o in bpy.data.objects:
    if o.type == "ARMATURE": o.data.pose_position = "REST"
GOLD = mat("King_Gold", (1.0, 0.78, 0.28), rough=0.3, metal=0.9); GEM = mat("King_GemGlow", (0.85, 0.75, 1.0), rough=0.2, emit=(0.8, 0.7, 1.0), emit_strength=1.5)
PUR = mat("King_Cape", (0.42, 0.12, 0.42), rough=0.7, sheen=0.6); LIN = mat("King_CapeLining", (0.95, 0.92, 0.96), rough=0.8); FUR = mat("King_Fur", (0.98, 0.97, 0.95), rough=1.0, sheen=1.0)
STAR = mat("King_StarGlow", (1.0, 0.88, 0.45), rough=0.3, metal=0.5, emit=(1.0, 0.85, 0.4), emit_strength=1.2)
M6 = [GOLD, GEM, PUR, LIN, FUR, STAR]
crown = to_object("KingCrown", K.crown(), M6); cape = to_object("KingCape", K.cape(), M6)
for o in (crown, cape):
    for p in o.data.polygons: p.use_smooth = True
for o, name in ((crown, "KingCrown"), (cape, "KingCape")):
    bpy.ops.object.select_all(action="DESELECT"); o.select_set(True)
    bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, name + ".fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y")
    print("EXPORTED", name, len(o.data.vertices))
if "--preview" in sys.argv:
    sc = bpy.context.scene
    for o in list(sc.objects):
        if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
    sc.render.engine = "CYCLES"; sc.cycles.samples = 40; sc.cycles.device = "CPU"; sc.render.resolution_x, sc.render.resolution_y = 900, 700
    w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.35, 0.45, 0.85, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 0.8; sc.world = w
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.5; sun.rotation_euler = (0.7, 0.2, 0.6); sc.collection.objects.link(sun)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam; cam.data.lens = 50
    for nm, loc in (("whale_king", Vector((-3.6, -3.4, 1.9))), ("whale_king_back", Vector((3.4, 3.2, 2.6)))):
        cam.location = loc; tgt = Vector((0, -0.4, 0.55)); cam.rotation_euler = (tgt - loc).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(PRE, nm + ".png"); bpy.ops.render.render(write_still=True); print("✔", nm)
