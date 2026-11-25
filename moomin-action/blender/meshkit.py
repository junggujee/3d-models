"""bmesh 로 상자·원기둥·구·고리·관을 '틀(origin + 축 3개)' 기준으로 쌓는 작은 도구(build_palace.py 가 씀).
Frame(o, ax, ay): 로컬 (x, y, z) → 월드 o + ax·x + ay·y + Z·z.  Builder(재질 목록).box/cyl/sphere/... → done(이름) = 물체."""
import math
import bmesh
from mathutils import Vector, Matrix
from lib import to_object, tube

Z = Vector((0, 0, 1))


class Frame:
    def __init__(self, o, ax, ay):
        self.o = Vector(o); self.ax = Vector(ax).normalized(); self.ay = Vector(ay).normalized()
        self.R = Matrix(((self.ax.x, self.ay.x, 0), (self.ax.y, self.ay.y, 0), (0, 0, 1)))
    def P(self, x, y, z=0.0): return self.o + self.ax * x + self.ay * y + Z * z
    def D(self, x, y, z=0.0): return self.ax * x + self.ay * y + Z * z


class Builder:
    def __init__(self, mats, frame):
        self.bm = bmesh.new(); self.mats = mats; self.f = frame; self.mi = {m.name: i for i, m in enumerate(mats)}
    def M(self, name): return self.mi[name]
    def _mat(self, verts, mi):
        for v in verts:
            for f in v.link_faces: f.material_index = mi
    def _place(self, verts, M):
        bmesh.ops.transform(self.bm, matrix=M, verts=verts)
    def box(self, x, y, z, sx, sy, sz, mat, rot=0.0, tilt=None):
        """중심(x, y, z) 크기(sx, sy, sz) — rot: 위에서 본 회전(라디안), tilt: 추가 3×3 회전(로컬)"""
        vs = bmesh.ops.create_cube(self.bm, size=1.0)["verts"]
        L = Matrix.Rotation(rot, 3, "Z") @ (tilt if tilt is not None else Matrix.Identity(3))
        self._place(vs, Matrix.Translation(self.f.P(x, y, z)) @ (self.f.R @ L).to_4x4() @ Matrix.Diagonal((sx, sy, sz, 1))); self._mat(vs, self.M(mat)); return vs
    def cyl(self, p0, p1, r, mat, seg=12, r1=None, caps=True):
        """p0, p1: 로컬 좌표 튜플"""
        a, b = self.f.P(*p0), self.f.P(*p1); d = b - a
        vs = bmesh.ops.create_cone(self.bm, cap_ends=caps, segments=seg, radius1=r, radius2=r if r1 is None else r1, depth=d.length)["verts"]
        self._place(vs, Matrix.Translation((a + b) / 2) @ Z.rotation_difference(d.normalized()).to_matrix().to_4x4()); self._mat(vs, self.M(mat)); return vs
    def sphere(self, p, r, mat, seg=14, rings=9, rot=None):
        """r: 반지름 (rx, ry, rz) 로컬 축 기준"""
        vs = bmesh.ops.create_uvsphere(self.bm, u_segments=seg, v_segments=rings, radius=1.0)["verts"]
        L = self.f.R @ (rot if rot is not None else Matrix.Identity(3))
        self._place(vs, Matrix.Translation(self.f.P(*p)) @ L.to_4x4() @ Matrix.Diagonal((r[0], r[1], r[2], 1))); self._mat(vs, self.M(mat))
        for v in vs:
            for fc in v.link_faces: fc.smooth = True
        return vs
    def torus(self, c, R_, r, mat, axis=(0, 0, 1), seg=24, sides=8, arc=2 * math.pi):
        q = Z.rotation_difference(self.f.D(*axis).normalized()).to_matrix(); cw = self.f.P(*c); rows = []
        n = seg + (0 if arc >= 2 * math.pi - 1e-4 else 1)
        for i in range(n):
            a = arc * i / seg
            rows.append([self.bm.verts.new(cw + q @ Vector(((R_ + r * math.cos(b)) * math.cos(a), (R_ + r * math.cos(b)) * math.sin(a), r * math.sin(b)))) for b in [2 * math.pi * j / sides for j in range(sides)]])
        closed = arc >= 2 * math.pi - 1e-4
        for i in range(seg if closed else seg):
            i2 = (i + 1) % n if closed else i + 1
            for j in range(sides):
                fc = self.bm.faces.new((rows[i][j], rows[i2][j], rows[i2][(j + 1) % sides], rows[i][(j + 1) % sides])); fc.material_index = self.M(mat); fc.smooth = True
    def tube(self, pts, r, mat, sides=6, taper=0.0):
        tb, _ = tube([self.f.P(*p) for p in pts], lambda t: r * (1 - taper * t), sides=sides, mat_idx=self.M(mat))
        self.merge(tb)
    def merge(self, other_bm, M=None):
        """다른 bmesh 를 (M 로 옮겨) 합침 — 재질 번호는 그대로"""
        import bpy
        me = bpy.data.meshes.new("_tmp"); other_bm.to_mesh(me); other_bm.free()
        if M is not None: me.transform(M)
        self.bm.from_mesh(me); bpy.data.meshes.remove(me)
    def prism(self, x, y, z0, z1, r, mat, seg=8):
        return self.cyl((x, y, z0), (x, y, z1), r, mat, seg=seg)
    def done(self, name, origin=None):
        if origin is not None: bmesh.ops.translate(self.bm, vec=-Vector(origin), verts=self.bm.verts[:])
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces[:])
        o = to_object(name, self.bm, self.mats)
        if origin is not None: o.location = origin
        return o
