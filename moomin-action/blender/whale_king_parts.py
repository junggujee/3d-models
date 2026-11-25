"""고래고래 임금님 소품(고래 모델 공간, exports/Goraegorae.blend 와 같은 좌표 — 앞 = -Y, 위 = +Z):
  · crown(): 금관 — 띠 + 뾰족 5개(끝에 구슬) + 앞 별 보석 · 이마(멜론) 꼭대기, 숨구멍 바로 뒤
  · cape(): 망토 — 등을 덮는 보라 겉감 + 흰 안감 + 목둘레 흰 털 깃 + 별 브로치(몸통 타원을 8% 키운 면)
모두 bmesh 를 돌려줌(재질 번호: 아래 MATS 순서). build_whale_king.py(보스 소품 FBX) · build_palace.py(석상 · 왕관 샹들리에)가 씀."""
import math
import bmesh
from mathutils import Vector, Matrix

# 재질 번호(호출하는 쪽이 이 순서로 재질 목록을 줌)
GOLD, GEM, PURPLE, LINING, FUR, STAR = range(6)
BODY_C, BODY_R = Vector((0.0, 0.0, 0.51)), Vector((0.468, 0.93, 0.43))     # 몸통 타원(build_whale.py BC/BR 을 Blender 좌표로)
CROWN_AT = Vector((0.0, -0.5, 0.86))                                        # 이마 꼭대기(멜론 위, 숨구멍 뒤)


def _star(bm, c, r_out, r_in, th, normal, up, mi):
    """두께 th 인 오각별(중심 c, 앞 = normal)."""
    n = normal.normalized(); u = up.normalized(); v = n.cross(u)
    top, bot = [], []
    for k in range(10):
        a = math.pi / 2 + k * math.pi / 5; r = r_out if k % 2 == 0 else r_in
        p = c + (u * math.sin(a) + v * math.cos(a)) * r
        top.append(bm.verts.new(p + n * th * 0.5)); bot.append(bm.verts.new(p - n * th * 0.5))
    ct = bm.verts.new(c + n * th * 0.9); cb = bm.verts.new(c - n * th * 0.5)
    for k in range(10):
        j = (k + 1) % 10
        bm.faces.new((ct, top[k], top[j])).material_index = mi; bm.faces.new((cb, bot[j], bot[k])).material_index = mi
        bm.faces.new((top[k], bot[k], bot[j], top[j])).material_index = mi


def crown(scale=1.0, at=None):
    """금관(띠 반지름 0.22 · 높이 0.2 · 뾰족 5 · 구슬 · 앞 별). at: 중심(기본 = 고래 이마)."""
    bm = bmesh.new(); c0 = (at if at is not None else CROWN_AT); R, Hb, N = 0.22 * scale, 0.07 * scale, 40
    def P(a, r, z): return c0 + Vector((math.cos(a) * r, math.sin(a) * r * 0.92, z))
    # 띠(안·밖 두 겹 원기둥)
    rows = []
    for (r, z) in ((R, 0.0), (R, Hb), (R * 0.93, Hb), (R * 0.93, 0.0)):
        rows.append([bm.verts.new(P(2 * math.pi * k / N, r, z)) for k in range(N)])
    for i in range(4):
        for k in range(N):
            f = bm.faces.new((rows[i][k], rows[i][(k + 1) % N], rows[(i + 1) % 4][(k + 1) % N], rows[(i + 1) % 4][k])); f.material_index = GOLD; f.smooth = True
    # 뾰족 5개(띠 위 삼각 판, 두께) + 끝 구슬
    for k in range(5):
        a = -math.pi / 2 + 2 * math.pi * k / 5; w = 0.42; hp = 0.14 * scale * (1.15 if k == 0 else 1.0)
        base = [P(a - w, R * 0.97, Hb), P(a + w, R * 0.97, Hb)]; tip = P(a, R * 1.02, Hb + hp)
        out = Vector((math.cos(a), math.sin(a), 0)) * 0.012 * scale
        vs = [bm.verts.new(p + out) for p in (base[0], base[1], tip)] + [bm.verts.new(p - out) for p in (base[0], base[1], tip)]
        for q in ((0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)): bm.faces.new([vs[i] for i in q]).material_index = GOLD
        r_ = bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=6, radius=0.025 * scale)
        bmesh.ops.translate(bm, vec=tip + Vector((0, 0, 0.018 * scale)), verts=r_["verts"])
        for v in r_["verts"]:
            for f in v.link_faces: f.material_index = GOLD
    # 앞 별 보석(띠 앞면)
    front = Vector((0, -1, 0)); _star(bm, P(-math.pi / 2, R * 1.01, Hb * 0.5) + front * 0.01, 0.05 * scale, 0.022 * scale, 0.02 * scale, front, Vector((0, 0, 1)), GEM)
    for k in (1, 4):                                                              # 옆 보석(둥근)
        a = -math.pi / 2 + 2 * math.pi * k / 5; r_ = bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=0.02 * scale)
        bmesh.ops.translate(bm, vec=P(a, R * 1.01, Hb * 0.5), verts=r_["verts"])
        for v in r_["verts"]:
            for f in v.link_faces: f.material_index = GEM
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def cape():
    """망토: 몸통 타원 위쪽을 φ -105°~105°(0 = 위), y -0.32(목 뒤) ~ 0.62(허리 뒤)로 덮는 면. 뒤로 갈수록 넓게 퍼지고 아래 끝은 물결."""
    bm = bmesh.new(); NY, NP = 18, 30; Y0, Y1 = -0.32, 0.62; PH = math.radians(105)
    def pt(t, s, off):
        y = Y0 + (Y1 - Y0) * t; ph = -PH + 2 * PH * s
        w = math.sqrt(max(0.05, 1 - (y / BODY_R.y) ** 2)); flare = 1.08 + 0.12 * t * t
        sag = 0.05 * t * math.sin(ph * 4 + t * 3) * abs(math.sin(ph))                # 아래 끝 물결
        r = flare + off
        return Vector((BODY_C.x + BODY_R.x * r * w * math.sin(ph), y, BODY_C.z + BODY_R.z * r * w * math.cos(ph) - sag - 0.04 * t * (1 - math.cos(ph))))
    outer = [[bm.verts.new(pt(i / NY, j / NP, 0.0)) for j in range(NP + 1)] for i in range(NY + 1)]
    inner = [[bm.verts.new(pt(i / NY, j / NP, -0.025)) for j in range(NP + 1)] for i in range(NY + 1)]
    for i in range(NY):
        for j in range(NP):
            f = bm.faces.new((outer[i][j], outer[i][j + 1], outer[i + 1][j + 1], outer[i + 1][j])); f.material_index = PURPLE; f.smooth = True
            g = bm.faces.new((inner[i][j], inner[i + 1][j], inner[i + 1][j + 1], inner[i][j + 1])); g.material_index = LINING; g.smooth = True
    for i in range(NY):                                                           # 양 옆 가장자리 · 뒤 끝 막기(두께)
        for j in (0, NP):
            q = (outer[i][j], outer[i + 1][j], inner[i + 1][j], inner[i][j]); bm.faces.new(q if j == 0 else q[::-1]).material_index = LINING
    for j in range(NP):
        bm.faces.new((outer[NY][j], outer[NY][j + 1], inner[NY][j + 1], inner[NY][j])).material_index = LINING
    # 목둘레 흰 털 깃(관 모양, 앞 끝)
    pts = [pt(0.0, s / 24, 0.03) for s in range(25)]
    rows = []
    for k, p in enumerate(pts):
        nxt = pts[min(k + 1, 24)] - pts[max(k - 1, 0)]; nxt.normalize(); side = nxt.cross(Vector((0, 1, 0))).normalized(); up = nxt.cross(side).normalized()
        rows.append([bm.verts.new(p + (side * math.cos(a) + up * math.sin(a)) * 0.045 * (1 + 0.15 * math.sin(a * 3 + k))) for a in [2 * math.pi * m / 8 for m in range(8)]])
    for k in range(24):
        for m in range(8):
            f = bm.faces.new((rows[k][m], rows[k + 1][m], rows[k + 1][(m + 1) % 8], rows[k][(m + 1) % 8])); f.material_index = FUR; f.smooth = True
    for cap_ in (rows[0], rows[-1]):
        f = bm.faces.new(cap_); f.material_index = FUR
    # 별 브로치: 깃 양 옆(왼쪽 · 오른쪽 어깨)
    for s in (0.06, 0.94):
        p = pt(0.0, s, 0.06); n = (p - Vector((BODY_C.x, p.y, BODY_C.z))).normalized()
        _star(bm, p + n * 0.02, 0.075, 0.034, 0.03, n, Vector((0, -1, 0)), STAR)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm
