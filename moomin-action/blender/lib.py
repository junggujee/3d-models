"""Blender(bpy) 공용 도우미. 좌표 규칙: three.js 모델 좌표(Y 위, +Z 앞)를 Blender(Z 위, 앞 = -Y)로 변환해 씁니다."""
import bpy, bmesh, math
from mathutils import Vector, Matrix, Euler, noise

K = 0.6  # three.js 단위 → 미터 (서 있는 민미 키 ≈ 1.27m)


def T(x, y, z):
    """three.js (x,y위,z앞) → Blender (X, Y=-앞, Z=위), 미터"""
    return Vector((x * K, -z * K, y * K))


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.render.fps = 30


# ── 재질 ─────────────────────────────────────────────
def mat(name, color, rough=0.9, metal=0.0, sheen=0.0, emit=None, emit_strength=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*color, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    if sheen and "Sheen Weight" in b.inputs:
        b.inputs["Sheen Weight"].default_value = sheen; b.inputs["Sheen Roughness"].default_value = 0.5
    if emit is not None:
        b.inputs["Emission Color"].default_value = (*emit, 1); b.inputs["Emission Strength"].default_value = emit_strength
    return m


# ── 메시 조각 ────────────────────────────────────────
def ellipsoid(c, r, rot=(0, 0, 0), seg=40, rings=26, fuzz=0.0, mat_idx=0, seed=0.0):
    """c,r: three.js 좌표계 중심/반지름. 털 느낌을 위해 fuzz만큼 잔물결 노이즈를 입힘."""
    bm = bmesh.new()
    S = Matrix.Diagonal((r[0] * K, r[2] * K, r[1] * K, 1.0))
    M = Matrix.Translation(T(*c)) @ Euler(rot).to_matrix().to_4x4() @ S
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1.0, matrix=M)
    bm.normal_update()
    if fuzz:
        for v in bm.verts:
            v.co += v.normal * (noise.noise(v.co * 22 + Vector((seed, seed, seed))) * fuzz * K)
    for f in bm.faces:
        f.smooth = True; f.material_index = mat_idx
    return bm


def oriented_disc(pos_b, normal_b, r_side, r_up, depth, mat_idx):
    """표면 위에 붙이는 납작한 타원 원판(눈). pos_b/normal_b 는 Blender 좌표."""
    n = normal_b.normalized()
    q = Vector((0, -1, 0)).rotation_difference(n)
    bm = bmesh.new()
    M = Matrix.Translation(pos_b) @ q.to_matrix().to_4x4() @ Matrix.Diagonal((r_side, depth, r_up, 1.0))
    bmesh.ops.create_uvsphere(bm, u_segments=28, v_segments=18, radius=1.0, matrix=M)
    for f in bm.faces:
        f.smooth = True; f.material_index = mat_idx
    return bm


def tube(points, radius_fn, sides=8, mat_idx=0, cap_end=True):
    """점들을 따라가는 가변 반지름 관(눈썹, 꼬리). 반환: (bmesh, [점마다 정점 인덱스 범위])"""
    bm = bmesh.new(); rings = []
    t0 = (points[1] - points[0]).normalized()
    ref = Vector((0, 0, 1)) if abs(t0.z) < 0.9 else Vector((1, 0, 0))
    nrm = t0.cross(ref).normalized()
    for i, p in enumerate(points):
        t = ((points[min(i + 1, len(points) - 1)] - points[max(i - 1, 0)])).normalized()
        nrm = (nrm - t * nrm.dot(t)).normalized(); bi = t.cross(nrm)
        r = radius_fn(i / (len(points) - 1))
        ring = [bm.verts.new(p + (nrm * math.cos(a) + bi * math.sin(a)) * r) for a in (2 * math.pi * k / sides for k in range(sides))]
        rings.append(ring)
    for i in range(len(rings) - 1):
        for k in range(sides):
            bm.faces.new((rings[i][k], rings[i][(k + 1) % sides], rings[i + 1][(k + 1) % sides], rings[i + 1][k]))
    if cap_end:
        bm.faces.new(rings[-1][::-1]); bm.faces.new(rings[0])
    for f in bm.faces:
        f.smooth = True; f.material_index = mat_idx
    return bm, rings


def to_object(name, bm, materials, bone=None, collection=None):
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for m in materials: me.materials.append(m)
    ob = bpy.data.objects.new(name, me); (collection or bpy.context.collection).objects.link(ob)
    if bone:
        vg = ob.vertex_groups.new(name=bone); vg.add(list(range(len(me.vertices))), 1.0, "REPLACE")
    return ob


def join(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join(); o = bpy.context.view_layer.objects.active; o.name = name; o.data.name = name
    return o


# ── 뼈대 ─────────────────────────────────────────────
def make_armature(name, bones):
    """bones: [(이름, 부모|None, head_b, tail_b)]  (head/tail 은 Blender 좌표)"""
    arm = bpy.data.armatures.new(name); ob = bpy.data.objects.new(name, arm); bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for n, parent, h, t in bones:
        b = arm.edit_bones.new(n); b.head, b.tail = h, t
        if parent: b.parent = eb[parent]
        eb[n] = b
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def bind(mesh_ob, arm_ob):
    mod = mesh_ob.modifiers.new("Armature", "ARMATURE"); mod.object = arm_ob
    mesh_ob.parent = arm_ob


# ── 애니메이션 ───────────────────────────────────────
def rot_world(arm_ob, bone, axis, angle):
    """뼈를 '월드 축' 기준으로 angle(rad) 돌리는 로컬 오일러 값을 반환(부모 무포즈 기준)."""
    M = arm_ob.data.bones[bone].matrix_local.to_3x3()
    W = Matrix.Rotation(angle, 3, axis)
    return (M.inverted() @ W @ M).to_euler()


def new_action(arm_ob, name, length):
    act = bpy.data.actions.new(name); act.use_fake_user = True
    arm_ob.animation_data_create(); arm_ob.animation_data.action = act
    for pb in arm_ob.pose.bones: pb.rotation_mode = "XYZ"
    return act


def key(arm_ob, bone, frame, rot=None, loc=None, scale=None):
    pb = arm_ob.pose.bones[bone]
    pb.rotation_euler = rot if rot is not None else Euler((0, 0, 0))
    pb.location = loc if loc is not None else Vector((0, 0, 0))
    pb.scale = scale if scale is not None else Vector((1, 1, 1))
    pb.keyframe_insert("rotation_euler", frame=frame); pb.keyframe_insert("location", frame=frame); pb.keyframe_insert("scale", frame=frame)


def finish_action(arm_ob, act, loop=True):
    """키 입력을 마치고 뼈를 기본 자세로 되돌림. 루프 여부는 Unity 임포트 설정(MoominSetup)에서 이름으로 지정."""
    for pb in arm_ob.pose.bones:
        pb.rotation_euler = (0, 0, 0); pb.location = (0, 0, 0); pb.scale = (1, 1, 1)
    arm_ob.animation_data.action = None


# ── 내보내기·미리보기 ────────────────────────────────
def export_all(basename, outdir, objects):
    import os; os.makedirs(outdir, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for o in objects: o.select_set(True)
    glb = os.path.join(outdir, basename + ".glb"); fbx = os.path.join(outdir, basename + ".fbx")
    bpy.ops.export_scene.gltf(filepath=glb, export_format="GLB", use_selection=True, export_animations=True,
                              export_animation_mode="ACTIONS", export_force_sampling=True, export_apply=False)
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"ARMATURE", "MESH", "EMPTY"}, add_leaf_bones=False,
                             bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
                             bake_anim_simplify_factor=0.0, apply_scale_options="FBX_SCALE_ALL", path_mode="COPY", embed_textures=False, axis_forward="-Z", axis_up="Y")
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(outdir, basename + ".blend"))
    return glb, fbx


def preview(path, cam_loc, target, size=720, frame=None, arm_ob=None, action=None, light_energy=3.0):
    sc = bpy.context.scene
    if arm_ob and action:
        arm_ob.animation_data_create(); arm_ob.animation_data.action = action
        sc.frame_set(frame if frame is not None else int(action.frame_start))
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam)
    cam.location = cam_loc; cam.data.lens = 55; sc.camera = cam
    d = (Vector(target) - Vector(cam_loc)); cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sun.data.energy = light_energy; sun.rotation_euler = (0.9, 0.2, 0.6); sc.collection.objects.link(sun)
    w = bpy.data.worlds.new("W"); w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (0.72, 0.82, 0.93, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 1.0; sc.world = w
    sc.render.engine = "CYCLES"; sc.cycles.samples = 24; sc.cycles.device = "CPU"; sc.render.resolution_x = sc.render.resolution_y = size
    sc.render.filepath = path; bpy.ops.render.render(write_still=True)
