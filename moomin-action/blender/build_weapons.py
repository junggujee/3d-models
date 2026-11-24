"""민미 무기 2종(민미 모델 공간 — 기본 자세에서 만들고 EquipmentVisuals 가 bindpose 로 뼈에 붙임).
  ① 베개 방패(arm.L): 땅콩빵 베개 모양(가운데 잘록한 두 덩이 · 짜임 천)을 세워 몸 앞 왼쪽에 — 갈색 테두리 바인딩, 덩이마다 단추(베개 터프팅), 안쪽에 땅콩 모양 수눈 선, 뒤에 팔 끈
  ② 회전 쌍검(hand.R · arm.L): 손잡이(가죽 감개·금 가드·폼멜) 끝에서 초승달 두 날이 바람개비처럼 돎. 날 = 푸른 강철 + 빛나는 날끝
     물체 이름 Grip_* (원점 = 손) · Rotor_* (원점 = 축 중심) — Unity WeaponSpin 이 Grip→Rotor 축으로 Rotor 를 돌림
결과: unity/Assets/Resources/Weapons/PillowShield.fbx · SpinBlade_R.fbx · SpinBlade_L.fbx
      Resources/ItemIcons/pillow_shield.png · spin_blades.png · previews/weapon_*.png(무기 동작)
실행: <bpy 파이썬> blender/build_moomin.py 다음에 blender/build_weapons.py"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
WD = os.path.join(ROOT, "unity", "Assets", "Resources", "Weapons"); ICONS = os.path.join(ROOT, "unity", "Assets", "Resources", "ItemIcons"); PRE = os.path.join(ROOT, "previews")
WEAVE = os.path.join(ROOT, "unity", "Assets", "Models", "Textures", "Weave_Normal.png")
bpy.ops.wm.open_mainfile(filepath=os.path.join(ROOT, "exports", "Moomin.blend"))
sc = bpy.context.scene
for o in list(bpy.data.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
rig = bpy.data.objects["MoominRig"]; moomin = bpy.data.objects["Moomin"]
if rig.animation_data: rig.animation_data.action = None
rig.data.pose_position = "REST"

def obj(name, bm, mats, origin=Vector((0, 0, 0))):
    for v in bm.verts: v.co -= origin
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m_ in mats: me.materials.append(m_)
    o = bpy.data.objects.new(name, me); sc.collection.objects.link(o); o.location = origin
    for p in me.polygons: p.use_smooth = True
    return o
def add_bm(dst, src):
    me = bpy.data.meshes.new("t"); src.to_mesh(me); src.free(); dst.from_mesh(me); bpy.data.meshes.remove(me)
def M(name, col, **kw):
    m = mat(name, col, **kw); return m

# ══════════ ① 베개 방패
CLOTH = M("Shield_Cloth", (0.95, 0.93, 0.87), rough=0.85, sheen=0.5)
TRIM = M("Shield_Trim", (0.5, 0.31, 0.16), rough=0.7)
BTN = M("Shield_Button", (0.58, 0.38, 0.2), rough=0.45)
STITCH = M("Shield_Stitch", (0.66, 0.46, 0.26), rough=0.8)
if os.path.exists(WEAVE):
    nt = CLOTH.node_tree; b = nt.nodes["Principled BSDF"]; img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(WEAVE); img.image.colorspace_settings.name = "Non-Color"
    mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (7, 7, 1); tc = nt.nodes.new("ShaderNodeTexCoord"); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 1.0
    nt.links.new(tc.outputs["UV"], mp.inputs[0]); nt.links.new(mp.outputs[0], img.inputs[0]); nt.links.new(img.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"])
SMATS = [CLOTH, TRIM, BTN, STITCH]
AX, AY, AZ, PXY, PZ = 0.3, 0.2, 0.075, 2.8, 3.0   # 반폭 · 반높이 · 반두께(m) — 땅콩빵 베개 비율
def pinch(x): return 1 - 0.28 * math.exp(-(x / (0.42 * AX)) ** 2)
def radius(d):
    r = 0.2
    for _ in range(7):
        x = r * d.x; ay = AY * pinch(x)
        k = (abs(d.x / AX) ** PXY + abs(d.y / ay) ** PXY) ** (PZ / PXY) + abs(d.z / AZ) ** PZ; r = k ** (-1 / PZ)
    return r
SC = T(0.42, 0.82, 0.55)                         # 방패 가운데(몸 앞 왼쪽)
def to_world(p): return SC + Vector((p.x, -p.z, p.y))   # 베개 좌표(x 폭, y 높이, z 두께: +z = 앞) → Blender(앞 = -Y)
bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=96, v_segments=56, radius=1.0)
for v in bm.verts: d = v.co.normalized(); v.co = d * radius(d)
uvl = bm.loops.layers.uv.new("UVMap")
for f in bm.faces:
    f.smooth = True; f.material_index = 0
    for l in f.loops: co = l.vert.co; l[uvl].uv = (co.x * 1.0, co.y * 1.0)
for v in bm.verts: v.co = to_world(v.co)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
def front_point(x, y, out=0.0):
    """앞면(+z) 위 점: 베개 좌표 (x, y) → 표면 + 바깥으로 out."""
    z = AZ
    for _ in range(8):
        d = Vector((x, y, z)).normalized(); r = radius(d); p = d * r; z = max(1e-4, z + (AZ * 1.2 - p.length) * 0.0) if False else p.z
        d = Vector((x, y, max(z, 1e-3)));
        k = (abs(x / AX) ** PXY + abs(y / (AY * pinch(x))) ** PXY) ** (PZ / PXY)
        z = AZ * max(0.0, 1 - k) ** (1 / PZ) if k < 1 else 0.0
        break
    return to_world(Vector((x, y, z + out)))
# 테두리 바인딩(두께 가운데 둘레) — 갈색 굵은 관
ring = []
for k in range(160):
    a = 2 * math.pi * k / 160; d = Vector((math.cos(a), math.sin(a), 0)); p = d * radius(d); ring.append(to_world(p + d * 0.004))
ring += ring[:2]; tb, _ = tube(ring, lambda t: 0.016, sides=10, mat_idx=1); add_bm(bm, tb)
# 안쪽 땅콩 모양 수눈 선(앞면, 0.78배)
st = []
for k in range(120):
    a = 2 * math.pi * k / 120; d = Vector((math.cos(a), math.sin(a), 0)); p = d * radius(d) * 0.76; st.append(front_point(p.x, p.y, 0.004))
st += st[:2]; tb, _ = tube(st, lambda t: 0.0045, sides=6, mat_idx=3); add_bm(bm, tb)
# 덩이마다 단추(베개 터프팅) + 단추 둘레 살짝 들어간 주름 대신 작은 X 바늘땀
for sx in (-0.15, 0.15):
    c = front_point(sx, 0.0, 0.004); bb = ellipsoid((0, 0, 0), (0.028 / K, 0.028 / K, 0.014 / K), seg=18, rings=10, mat_idx=2)
    for v in bb.verts: v.co = Vector((v.co.x, v.co.y, v.co.z)) + c
    add_bm(bm, bb)
    for s1 in (-1, 1):
        a = front_point(sx - 0.016, -0.016 * s1, 0.02); b2 = front_point(sx + 0.016, 0.016 * s1, 0.02)
        tb, _ = tube([a, b2], lambda t: 0.003, sides=5, mat_idx=3); add_bm(bm, tb)
# 뒤쪽 팔 끈(방패 뒷면 → 왼팔 아래쪽)
back = to_world(Vector((0.05, 0.02, -AZ * 0.9))); forearm = T(0.6, 0.86, 0.12)
strap = [back.lerp(forearm, t) + Vector((0, 0, 0.02 * math.sin(math.pi * t))) for t in [i / 8 for i in range(9)]]
tb, _ = tube(strap, lambda t: 0.018, sides=8, mat_idx=1); add_bm(bm, tb)
bm.normal_update()
shield = obj("PillowShield", bm, SMATS)

# ══════════ ② 회전 쌍검
LEATHER = M("Blade_Leather", (0.22, 0.13, 0.08), rough=0.8)
GOLD = M("Blade_Gold", (0.86, 0.66, 0.28), rough=0.3, metal=1.0)
STEEL = M("Blade_Steel", (0.68, 0.76, 0.88), rough=0.22, metal=1.0)
EDGE = M("Blade_Edge", (0.55, 0.85, 1.0), rough=0.2, emit=(0.4, 0.8, 1.0), emit_strength=2.5)
BMATS = [LEATHER, GOLD, STEEL, EDGE]
GL, HUB_R, BLADE_L = 0.15, 0.045, 0.44
def blade_set(side, hand):
    s = 1 if side == "L" else -1; fwd = Vector((0, -1, 0)); g0 = hand; g1 = hand + fwd * GL; hub = g1 + fwd * 0.03
    # 손잡이: 가죽 감개(물결 굵기) · 폼멜 · 금 가드 고리
    gb = bmesh.new()
    pts = [g0 + fwd * (GL * t) for t in [i / 12 for i in range(13)]]
    tb, _ = tube(pts, lambda t: 0.021 + 0.0025 * math.sin(t * math.pi * 10), sides=12, mat_idx=0); add_bm(gb, tb)
    pm = ellipsoid((0, 0, 0), (0.03 / K, 0.03 / K, 0.03 / K), seg=16, rings=10, mat_idx=1)
    for v in pm.verts: v.co += g0 - fwd * 0.01
    add_bm(gb, pm)
    ring = [g1 + Vector((math.cos(a) * 0.045, 0, math.sin(a) * 0.045)) for a in [2 * math.pi * k / 24 for k in range(26)]]
    tb, _ = tube(ring, lambda t: 0.009, sides=8, mat_idx=1); add_bm(gb, tb)
    grip = obj(f"Grip_{side}", gb, BMATS, origin=g0)
    # 회전부: 금 축 + 초승달 날 2개(축에 수직인 면, 바람개비처럼 같은 방향으로 휨) + 빛나는 날끝
    rb = bmesh.new()
    hubp = [hub - fwd * 0.02 + fwd * (0.04 * t) for t in (0, 0.5, 1)]
    tb, _ = tube(hubp, lambda t: HUB_R, sides=16, mat_idx=1); add_bm(rb, tb)
    for k in (0, 1):
        base_ang = math.pi / 2 + k * math.pi   # 위 · 아래
        outer, inner = [], []
        for i in range(21):
            t = i / 20; r = HUB_R * 0.8 + (BLADE_L - HUB_R) * t
            ang = base_ang + s * 0.9 * t ** 1.4          # 휘어 감기(쌍검은 서로 반대로)
            w = 0.08 * (1 - t) ** 0.8 + 0.005        # 폭(끝으로 갈수록 뾰족)
            c = Vector((math.cos(ang), 0, math.sin(ang))); nrm = Vector((-math.sin(ang), 0, math.cos(ang))) * s
            outer.append(hub + c * r + nrm * w * 0.5); inner.append(hub + c * r - nrm * w * 0.5)
        th = 0.006
        vs_o = [rb.verts.new(p + fwd * th) for p in outer] + [rb.verts.new(p - fwd * th) for p in outer]
        vs_i = [rb.verts.new(p + fwd * th * 0.3) for p in inner] + [rb.verts.new(p - fwd * th * 0.3) for p in inner]
        n = len(outer)
        for i in range(n - 1):
            for (a, b_) in ((vs_o[i], vs_i[i]), ):
                pass
            f1 = rb.faces.new((vs_o[i], vs_o[i + 1], vs_i[i + 1], vs_i[i])); f2 = rb.faces.new((vs_o[n + i + 1], vs_o[n + i], vs_i[n + i], vs_i[n + i + 1]))
            f3 = rb.faces.new((vs_o[i + 1], vs_o[i], vs_o[n + i], vs_o[n + i + 1])); f4 = rb.faces.new((vs_i[i], vs_i[i + 1], vs_i[n + i + 1], vs_i[n + i]))
            for f in (f1, f2, f3, f4): f.material_index = 2; f.smooth = False
        edge = [p + (p - hub).normalized() * 0.0 for p in outer]
        tb, _ = tube(edge, lambda t: 0.007 * (1 - 0.6 * t) + 0.0015, sides=6, mat_idx=3); add_bm(rb, tb)
    bmesh.ops.recalc_face_normals(rb, faces=rb.faces)
    rotor = obj(f"Rotor_{side}", rb, BMATS, origin=hub)
    return grip, rotor
grip_r, rotor_r = blade_set("R", T(-0.62, 0.58, 0.14))
grip_l, rotor_l = blade_set("L", T(0.62, 0.72, 0.13))

# ── 내보내기
os.makedirs(WD, exist_ok=True)
def export(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(filepath=os.path.join(WD, name + ".fbx"), use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", bake_anim=False)
    print("✔", name, sum(len(o.data.vertices) for o in objs))
export([shield], "PillowShield"); export([grip_r, rotor_r], "SpinBlade_R"); export([grip_l, rotor_l], "SpinBlade_L")

# ── 렌더: 아이콘 + 무기 동작 미리보기(뼈를 따라가게 armature 결합)
for o, bone in ((shield, "arm.L"), (grip_r, "hand.R"), (rotor_r, "hand.R"), (grip_l, "arm.L"), (rotor_l, "arm.L")):
    vg = o.vertex_groups.new(name=bone); vg.add(list(range(len(o.data.vertices))), 1.0, "REPLACE")
    m = o.modifiers.new("Armature", "ARMATURE"); m.object = rig
sc.render.engine = "CYCLES"; sc.cycles.samples = 28; sc.cycles.use_denoising = True; sc.view_settings.view_transform = "AgX"
w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.75, 0.82, 0.92, 1); sc.world = w
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = 3.4; sun.rotation_euler = (0.8, 0.15, 0.7); sc.collection.objects.link(sun)
cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
SH = [shield]; DU = [grip_r, rotor_r, grip_l, rotor_l]
def shoot(path, loc, target, size, transparent, show, action=None, frame=1, lens=50):
    for o in SH + DU + [moomin]: o.hide_render = o not in show
    rig.data.pose_position = "POSE" if action else "REST"
    rig.animation_data_create(); rig.animation_data.action = bpy.data.actions.get(action) if action else None; sc.frame_set(frame)
    cam.location = loc; cam.data.lens = lens; cam.rotation_euler = (Vector(target) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
    sc.render.film_transparent = transparent; sc.render.resolution_x = sc.render.resolution_y = size; sc.render.image_settings.color_mode = "RGBA" if transparent else "RGB"
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)
def center(objs):
    pts = [o.matrix_world @ v.co for o in objs for v in o.data.vertices]; mn = Vector([min(p[k] for p in pts) for k in range(3)]); mx = Vector([max(p[k] for p in pts) for k in range(3)]); return (mn + mx) / 2, (mx - mn).length
os.makedirs(ICONS, exist_ok=True); os.makedirs(PRE, exist_ok=True)
c, s_ = center(SH); shoot(os.path.join(ICONS, "pillow_shield.png"), c + Vector((0.35, -1.0, 0.35)).normalized() * s_ * 1.9, c, 256, True, SH)
c, s_ = center([grip_r, rotor_r]); shoot(os.path.join(ICONS, "spin_blades.png"), c + Vector((0.9, -0.8, 0.5)).normalized() * s_ * 1.8, c, 256, True, [grip_r, rotor_r])
if "--preview" in sys.argv or True:
    tgt = Vector((0, -0.05, 0.62))
    for nm, act, fr, loc in (("shield_idle", None, 1, (1.3, -3.0, 1.2)), ("shield_guard", "ShieldGuard", 6, (1.2, -3.0, 1.2)), ("shield_bash", "ShieldBash", 10, (2.6, -2.2, 1.1)),
                             ("shield_slam", "ShieldSlam", 9, (2.4, -2.4, 1.3)), ("shield_leap", "ShieldLeap", 18, (2.6, -2.6, 1.6))):
        shoot(os.path.join(PRE, f"weapon_{nm}.png"), Vector(loc), tgt + (Vector((0, 0, 0.3)) if "leap" in nm else Vector()), 420, False, SH + [moomin], act, fr)
    for nm, act, fr, loc in (("dual_idle", None, 1, (1.3, -3.0, 1.2)), ("dual_guard", "DualGuard", 6, (1.2, -3.0, 1.2)), ("dual_slash", "DualSlash1", 10, (2.4, -2.4, 1.2)),
                             ("dual_whirl", "DualWhirl", 8, (2.2, -2.6, 1.3)), ("dual_grind", "DualGrind", 10, (2.6, -2.0, 1.1)), ("dual_finale", "DualFinale", 20, (2.6, -2.6, 1.7))):
        shoot(os.path.join(PRE, f"weapon_{nm}.png"), Vector(loc), tgt + (Vector((0, 0, 0.45)) if "finale" in nm else Vector()), 420, False, DU + [moomin], act, fr)
print("✔ renders")
