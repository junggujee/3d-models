"""펫: 하프물범 '몽실이'. 처음 받은 하프물범 필통 3D 모델(gift-3d-models/src/models.js createSeal)의 치수·부품을 그대로 옮김.
  몸 타원 A .5 · B .43 · C .72(three 단위), 검은 자수 눈 · 회색 눈썹 · 회색 퐁퐁 코 2개 + 검은 코끝 · 등 지퍼 · 앞/뒤 지느러미(같은 관절 위치·각도).
  털: three.js 의 셸 털 대신 털결 노멀 맵(tools/fabric_normals.py Fur_Normal) + 광택(sheen).
크기: three 1 단위 = S m(몸 길이 약 0.8m — 민미 가슴 높이에 오는 펫).
뼈대: root → body(몸 가운데) → chest(앞 절반·머리) · hips(뒤 절반) + 지느러미 4개(앞 = chest, 뒤 = hips 자식). 피부는 앞뒤 위치로 chest/hips 에 부드럽게.
애니메이션(30fps): Idle61 Walk25 Run17(배로 통통 '갈럼프') Windup15 Attack1 24(몸통 박치기, 타격 11) Attack2 24(물방울 뿜기, 11)
                  Guard12 Dodge14 Hit16 Death30(기절 — 펫은 죽지 않음) Happy40(빙글 + 지느러미 짝짝) Eat30(고개 끄덕 냠냠) Sleep61(엎드려 새근)
실행: <bpy 파이썬> blender/build_seal.py [--preview]"""
import sys, os, math, shutil
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
import bmesh

HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "exports"); PRE = os.path.join(HERE, "..", "previews"); UNITY = os.path.join(HERE, "..", "unity", "Assets", "Models")
FURTEX = os.path.join(UNITY, "Textures", "Fur_Normal.png")
reset_scene()
S = 0.45                                   # three 단위 → m
def C3(v): return Vector((v[0] * S, -v[2] * S, v[1] * S))   # three(x, y위, z앞) → Blender(앞 = -Y)

FUR = mat("Seal_Fur", (0.9, 0.9, 0.93), rough=1.0, sheen=1.0)
GREY = mat("Seal_Nose", (0.42, 0.42, 0.47), rough=1.0, sheen=0.6)
BLACK = mat("Seal_Eye", (0.03, 0.03, 0.04), rough=0.2)
BROW = mat("Seal_Brow", (0.55, 0.55, 0.6), rough=0.9)
ZIP = mat("Seal_Zip", (0.8, 0.81, 0.85), rough=0.4, metal=0.3)
MATS = [FUR, GREY, BLACK, BROW, ZIP]; I_FUR, I_GREY, I_BLACK, I_BROW, I_ZIP = range(5)
if os.path.exists(FURTEX):
    for m_ in (FUR, GREY):
        nt = m_.node_tree; b = nt.nodes["Principled BSDF"]; img = nt.nodes.new("ShaderNodeTexImage"); img.image = bpy.data.images.load(FURTEX); img.image.colorspace_settings.name = "Non-Color"
        mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (8, 8, 1); tc = nt.nodes.new("ShaderNodeTexCoord"); nm = nt.nodes.new("ShaderNodeNormalMap"); nm.inputs["Strength"].default_value = 0.9
        nt.links.new(tc.outputs["UV"], mp.inputs[0]); nt.links.new(mp.outputs[0], img.inputs[0]); nt.links.new(img.outputs["Color"], nm.inputs["Color"]); nt.links.new(nm.outputs[0], b.inputs["Normal"])

A, B, C, FURL = 0.5, 0.43, 0.72, 0.045
BY = B + FURL                               # 몸 중심 높이(three)

def ellipsoid3(center, radii, mat_idx, M=None, seg=48, rings=32):
    """three 좌표에서 타원체(+선택 변환 M: three 공간 4×4) → Blender bmesh. UV 포함(털 노멀 맵용)."""
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0, calc_uvs=True)
    Mm = M if M is not None else Matrix.Identity(4)
    for v in bm.verts:
        p = Vector((v.co.x * radii[0], v.co.z * radii[1], v.co.y * radii[2]))   # 구의 z 를 three 의 y(위)로
        p = Mm @ p + Vector(center); v.co = C3(p)
    bm.normal_update()
    for f in bm.faces: f.smooth = True; f.material_index = mat_idx
    return bm

def on_surface(x, y, off=0.0):
    z = C * math.sqrt(max(0.0, 1 - (x / A) ** 2 - (y / B) ** 2)); return Vector((x, y + BY, z + off))

parts = []
def add(name, bm, group): parts.append(to_object(name, bm, MATS, group))

add("body", ellipsoid3((0, BY, 0), (A, B, C), I_FUR, seg=72, rings=48), "SKIN")
for s in (-1, 1):
    e = on_surface(0.2 * s, 0.02, FURL * 0.6); add("eye", ellipsoid3(e, (0.066, 0.066, 0.04), I_BLACK, seg=24, rings=16), "chest")
    br = on_surface(0.22 * s, 0.14, FURL * 0.8); ang = 0.25 * s   # 캡슐(가로로 누운 눈썹)
    pts = [br + Vector((math.cos(ang) * 0.044 * t, math.sin(ang) * 0.044 * t, 0)) for t in (-1, -0.5, 0, 0.5, 1)]
    tb, _ = tube([C3(p) for p in pts], lambda t: 0.013 * S, sides=8, mat_idx=I_BROW); add("brow", tb, "chest")
    n = on_surface(0.048 * s, -0.09, FURL * 0.7); add("nose", ellipsoid3(n, (0.072, 0.072, 0.072), I_GREY, seg=24, rings=16), "chest")
add("noseTip", ellipsoid3(on_surface(0, -0.035, FURL + 0.03), (0.022, 0.022, 0.022), I_BLACK, seg=12, rings=8), "chest")
zp = [C3(Vector((0, BY + B * math.sqrt(max(0, 1 - (zz / C) ** 2)) + FURL * 0.7, zz))) for zz in [(-0.56 + 0.04 * k) for k in range(22)]]   # 등 지퍼(three: z -0.56 ~ 0.3)
tb, _ = tube(zp, lambda t: 0.024 * S, sides=8, mat_idx=I_ZIP); add("zipper", tb, "SKIN")
add("zipPull", ellipsoid3((0, BY + B * math.sqrt(1 - (0.3 / C) ** 2) + FURL, 0.33), (0.025, 0.012, 0.045), I_ZIP, seg=12, rings=8), "chest")

# 지느러미: three.js makeFlipper(sx, sy, sz, px, py, pz, rx, ry, rz, offX, offZ) 그대로(관절 = 뼈 머리)
FLIP = {}
def flipper(name, sx, sy, sz, px, py, pz, rx, ry, rz, offX, offZ):
    R = Euler((rx, ry, rz), "ZYX").to_matrix().to_4x4()   # three.js 'XYZ' 순서 = Rx·Ry·Rz
    M = Matrix.Translation(Vector((px, py + BY, pz))) @ R @ Matrix.Translation(Vector((offX, 0, offZ)))
    bm = ellipsoid3((0, 0, 0), (sx, sy, sz), I_FUR, M=M, seg=24, rings=16)
    add(name, bm, name); FLIP[name] = (C3(Vector((px, py + BY, pz))), C3(M @ Vector((offX * 2.2, 0, offZ * 2.2 - (0.06 if pz < 0 else 0)))))
for s, side in ((-1, "R"), (1, "L")):
    flipper(f"flip.F{side}", 0.13, 0.045, 0.1, 0.4 * s, -0.28, 0.2, 0, -0.6 * s, -0.4 * s, 0.08 * s, 0)
    flipper(f"flip.B{side}", 0.11, 0.045, 0.14, 0.07 * s, -0.24, -0.68, 0.3, 0.6 * s, 0.15 * s, 0, -0.1)

seal = join(parts, "Seal")
# ── 뼈대
mid = C3(Vector((0, BY, 0)))
BONES = [("root", None, Vector((0, 0, 0)), Vector((0, 0, 0.1))), ("body", "root", mid, mid + Vector((0, 0, 0.12))),
         ("chest", "body", mid, C3(Vector((0, BY, C)))), ("hips", "body", mid, C3(Vector((0, BY, -C))))]
for k, (h, t) in FLIP.items():
    if (t - h).length < 0.02: t = h + Vector((0, 0, 0.05))
    BONES.append((k, "chest" if ".F" in k else "hips", h, t))
arm = make_armature("SealRig", BONES)
def smooth(a, b, x): t = max(0.0, min(1.0, (x - a) / (b - a))); return t * t * (3 - 2 * t)
skin = seal.vertex_groups["SKIN"]; gc = seal.vertex_groups.get("chest") or seal.vertex_groups.new(name="chest"); gh = seal.vertex_groups.new(name="hips")
for v in seal.data.vertices:
    if not any(g.group == skin.index for g in v.groups): continue
    zf = -v.co.y / S; w = smooth(-0.3, 0.3, zf)   # 앞(+z three) 일수록 chest
    if w > 0: gc.add([v.index], w, "REPLACE")
    if w < 1: gh.add([v.index], 1 - w, "REPLACE")
seal.vertex_groups.remove(skin); bind(seal, arm)

# ── 애니메이션(키 목록 → 부드럽게 이어 매 프레임)
def R(bn, pitch=0.0, roll=0.0, yaw=0.0):
    M = arm.data.bones[bn].matrix_local.to_3x3(); W = Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(pitch, 3, "X") @ Matrix.Rotation(roll, 3, "Y")
    return (M.inverted() @ W @ M).to_euler()
def track(keys, default=0.0):
    if not keys: return lambda f: default
    keys = sorted(keys)
    def at(f):
        if f <= keys[0][0]: return keys[0][1]
        for (f0, v0), (f1, v1) in zip(keys, keys[1:]):
            if f <= f1: u = (f - f0) / (f1 - f0); u = u * u * (3 - 2 * u); return v0 + (v1 - v0) * u
        return keys[-1][1]
    return at
CH = ("z", "pitch", "roll", "yaw", "head", "tail", "sq", "ff", "fs", "bf", "spin")
def anim(name, length, spec, loop=True, extra=None):
    """z 튐(m) · pitch 몸 앞 숙임(+ = 머리 아래) · roll · yaw · head 머리 들기(+ 위) · tail 꼬리 들기(+ 위) · sq 납작(+)/늘어남(-)
       ff 앞지느러미 노 젓기 · fs 앞지느러미 벌리기 · bf 뒷지느러미 위아래 · spin 몸 전체 회전(rad)."""
    tr = {k: track(spec.get(k, [])) for k in CH}; act = new_action(arm, name, length)
    for f in range(1, length + 1):
        ff_ = ((f - 1) % (length - 1)) + 1 if loop else f; g = {k: tr[k](ff_) for k in CH}
        if extra: extra(f, g)
        key(arm, "root", f, loc=Vector((0, g["z"], 0)), rot=R("root", 0, 0, g["yaw"] + g["spin"]))
        sq = g["sq"]; key(arm, "body", f, rot=R("body", g["pitch"], g["roll"]), scale=Vector((1 + 0.5 * sq, 1 - sq, 1 + 0.5 * sq)))
        key(arm, "chest", f, rot=R("chest", -g["head"])); key(arm, "hips", f, rot=R("hips", g["tail"]))
        for side, s in (("L", 1), ("R", -1)):
            key(arm, f"flip.F{side}", f, rot=R(f"flip.F{side}", 0.55 * g["ff"], s * 0.6 * g["fs"] * -1))   # 지느러미가 몸에서 떨어져 보이지 않게 진폭 줄임
            key(arm, f"flip.B{side}", f, rot=R(f"flip.B{side}", -g["bf"] + 0.15 * s * g["bf"]))
    finish_action(arm, act, loop=loop); act.frame_start, act.frame_end = 1, length; return act
def wave(n, amp, cycles=1, phase=0.0): return [(1 + round(k * (n - 1) / 16), amp * math.sin(2 * math.pi * cycles * k / 16 + phase)) for k in range(17)]
def galumph(n, h, push):
    k = lambda u: 1 + round(u * (n - 1))
    return {"z": [(1, 0), (k(.25), h), (k(.5), 0), (n, 0)], "head": [(1, .05), (k(.2), .22), (k(.5), -.05), (n, .05)],
            "tail": [(1, .05), (k(.35), -.05), (k(.6), .25), (n, .05)], "pitch": [(1, 0), (k(.2), -.1), (k(.5), .12), (n, 0)],
            "sq": [(1, .05), (k(.2), -.1), (k(.5), .14), (k(.65), 0), (n, .05)], "ff": [(1, .3), (k(.45), -push), (k(.75), .3), (n, .3)],
            "bf": wave(n, .35, 1, 1.2), "fs": [(1, .1), (n, .1)]}
A_ = {}
A_["Idle"] = anim("Idle", 61, {"sq": wave(61, .03), "head": wave(61, .06, 1, .5), "bf": wave(61, .25, 2), "ff": [(1, 0), (22, 0), (28, .35), (34, -.1), (40, .3), (46, 0), (61, 0)], "fs": [(1, .05), (61, .05)]})
A_["Walk"] = anim("Walk", 25, galumph(25, 0.035, 0.5))
A_["Run"] = anim("Run", 17, galumph(17, 0.06, 0.7))
A_["Windup"] = anim("Windup", 15, {"head": [(1, 0), (8, .35), (15, .38)], "tail": [(1, 0), (8, .2), (15, .22)], "sq": [(1, 0), (8, .15), (15, .17)], "fs": [(1, 0), (8, .5), (15, .5)]}, loop=False,
                    extra=lambda f, g: g.__setitem__("roll", 0.03 * math.sin(f * 2.2) if f > 7 else 0))
A_["Attack1"] = anim("Attack1", 24, {"head": [(1, .38), (6, .45), (10, -.25), (11, -.3), (15, 0), (24, 0)], "pitch": [(1, -.1), (6, -.18), (10, .3), (11, .32), (16, .02), (24, 0)],
                                    "z": [(1, 0), (6, 0), (9, .1), (11, .02), (14, 0), (24, 0)], "sq": [(1, .17), (6, .2), (9, -.18), (11, .2), (14, -.05), (24, 0)],
                                    "tail": [(1, .22), (11, .35), (16, 0), (24, 0)], "fs": [(1, .5), (11, .9), (18, .1), (24, 0)], "ff": [(1, 0), (11, -.6), (18, .2), (24, 0)]}, loop=False)
A_["Attack2"] = anim("Attack2", 24, {"head": [(1, 0), (7, .5), (10, .1), (11, -.15), (14, .05), (24, 0)], "sq": [(1, 0), (7, -.12), (11, .15), (15, 0), (24, 0)],
                                    "ff": [(1, 0), (6, .6), (9, -.5), (12, .6), (15, -.4), (24, 0)], "fs": [(1, .1), (9, .6), (15, .3), (24, .1)]}, loop=False)
A_["Guard"] = anim("Guard", 12, {"head": [(1, -.35), (12, -.35)], "sq": [(1, .22), (6, .25), (12, .22)], "ff": [(1, .9), (12, .9)], "fs": [(1, -.3), (12, -.3)], "tail": [(1, .2), (12, .2)]})
A_["Dodge"] = anim("Dodge", 14, {"roll": [(1, 0), (5, -.8), (9, -.3), (14, 0)], "z": [(1, 0), (5, .1), (10, 0), (14, 0)], "sq": [(1, .15), (5, -.1), (10, .15), (14, 0)]}, loop=False)
A_["Hit"] = anim("Hit", 16, {"head": [(1, 0), (3, .4), (7, -.1), (11, .05), (16, 0)], "sq": [(1, 0), (3, .25), (6, -.12), (10, .05), (16, 0)], "pitch": [(1, 0), (3, -.2), (8, .05), (16, 0)]}, loop=False)
A_["Death"] = anim("Death", 30, {"roll": [(1, 0), (8, -.3), (16, 1.5), (30, 1.45)], "z": [(1, 0), (8, .08), (16, 0), (30, 0)], "head": [(1, 0), (16, -.2), (30, -.25)],
                                 "ff": [(1, 0), (16, .8), (30, .7)], "sq": [(1, 0), (16, .15), (20, .08), (30, .1)]}, loop=False)   # 기절(배를 보이며 벌러덩)
A_["Happy"] = anim("Happy", 40, {"spin": [(1, 0), (20, 2 * math.pi), (40, 2 * math.pi)], "z": [(1, 0), (5, .08), (10, 0), (15, .08), (20, 0), (26, .1), (32, 0), (40, 0)],
                                 "head": [(1, .1), (20, .3), (30, .35), (40, .1)], "ff": [(1, 0), (24, 0), (27, .8), (30, -.2), (33, .8), (36, -.2), (40, 0)],
                                 "fs": [(1, .2), (24, .2), (27, .9), (30, .2), (33, .9), (36, .2), (40, .2)], "sq": wave(40, .08, 3)}, loop=False)
A_["Eat"] = anim("Eat", 30, {"head": [(1, 0), (6, -.35), (10, .05), (15, -.35), (19, .05), (24, .25), (30, 0)], "sq": [(1, 0), (6, .08), (10, 0), (15, .08), (19, 0), (30, 0)],
                             "bf": wave(30, .4, 2)}, loop=False)
A_["Sleep"] = anim("Sleep", 61, {"sq": [(1, .12), (30, .18), (61, .12)], "head": [(1, -.3), (30, -.32), (61, -.3)], "tail": [(1, -.05), (61, -.05)], "ff": [(1, .5), (61, .5)], "fs": [(1, -.2), (61, -.2)]})

if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); c = Vector((0, 0, 0.24))
    preview(os.path.join(PRE, "seal_front.png"), Vector((0.7, -1.7, 0.6)), c, arm_ob=arm, action=A_["Idle"], frame=1)
    preview(os.path.join(PRE, "seal_side.png"), Vector((1.9, -0.3, 0.55)), c, arm_ob=arm, action=A_["Walk"], frame=7)
    preview(os.path.join(PRE, "seal_happy.png"), Vector((1.0, -1.6, 0.8)), c, arm_ob=arm, action=A_["Happy"], frame=29)
    preview(os.path.join(PRE, "seal_attack.png"), Vector((1.6, -1.2, 0.6)), c, arm_ob=arm, action=A_["Attack1"], frame=11)
    preview(os.path.join(PRE, "seal_faint.png"), Vector((1.0, -1.6, 0.9)), c, arm_ob=arm, action=A_["Death"], frame=30)

# 펫 카드 얼굴 아이콘(정면, 투명) → Resources/ItemIcons/pet_seal.png
ICON = os.path.join(HERE, "..", "unity", "Assets", "Resources", "ItemIcons", "pet_seal.png"); os.makedirs(os.path.dirname(ICON), exist_ok=True)
sc = bpy.context.scene; arm.animation_data_create(); arm.animation_data.action = A_["Idle"]; sc.frame_set(1)
for o in list(sc.objects):
    if o.type in ("CAMERA", "LIGHT"): bpy.data.objects.remove(o)
cam = bpy.data.objects.new("IconCam", bpy.data.cameras.new("IconCam")); sc.collection.objects.link(cam); sc.camera = cam
fc = C3(Vector((0, BY + 0.02, 0.2))); cam.location = fc + Vector((0.35, -1.25, 0.25)); cam.data.lens = 70; cam.rotation_euler = (fc - cam.location).to_track_quat("-Z", "Y").to_euler()
sun = bpy.data.objects.new("IconSun", bpy.data.lights.new("IconSun", "SUN")); sun.data.energy = 3.0; sun.rotation_euler = (0.7, 0.2, 0.5); sc.collection.objects.link(sun)
w = bpy.data.worlds.new("IW"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[1].default_value = 0.9; sc.world = w
sc.render.engine = "CYCLES"; sc.cycles.samples = 32; sc.cycles.use_denoising = True; sc.render.film_transparent = True; sc.render.resolution_x = sc.render.resolution_y = 256
sc.render.image_settings.color_mode = "RGBA"; sc.render.filepath = ICON; bpy.ops.render.render(write_still=True); arm.animation_data.action = None
glb, fbx = export_all("Seal", OUT, [arm, seal])
os.makedirs(UNITY, exist_ok=True); shutil.copy(fbx, os.path.join(UNITY, "Seal.fbx"))
print("EXPORTED", glb, fbx, "verts:", len(seal.data.vertices))
