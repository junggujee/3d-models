"""바다 밑 용궁(고래고래 임금님의 궁전) — 사용자 그림 2장(용궁 앞 결투장 · 용궁 안) 바탕. 맵 좌표 그대로(Seaside 와 같은 내보내기) → Resources/World/Palace.fbx.
자리: 선착장 끝 꼭짓점 T 에서 바다 쪽(u)으로 165m, 바다 밑을 파낸 분지(z = -42, build_seaside.py PALACE_*)에. 앞(결투장·문) = 뭍 쪽(-u).
  · Palace_Dome: 반지름 36m 공기방울(가운데 = 분지 바닥) — 안은 물이 없어 걸어 다니고 싸움(Unity AirDome), 꼭대기 -6m.
  · 결투장(Palace_Arena): 반지름 13.5m 원판, 돌 타일 두 색 + 금빛 8각 별·고리·바퀴살(GoldGlow), 둘레 기둥·수정 받침(CrystalGlow), 앞은 열림
  · 계단 → 궁전 바닥(높이 4.25m) · 계단 양옆 받침 위 왕관 쓴 고래고래 석상 + 빛 구슬 · 높은 깃대 깃발 2(파랑 + 금테 + 흰 고래 + 왕관)
  · 궁전(Palace_Building): 앞벽(아치 문 + 빛나는 고래 문장 창) · 옆벽(아치 창) · 뒷벽 · 지붕(가운데 네모 구멍 + 금 테) · 탑 9개(파란 고깔 지붕 · 금 구슬)
  · 안(Palace_Hall): 바닥 금빛 별 무늬 · 기둥 10 · 조개 왕좌(단 3칸 + 부채꼴 조개 + 진주) · 안쪽 석상 2 · 수정 · 산호 · 등불 · 깃발 · 지붕 구멍 아래 큰 왕관 샹들리에
  · 바깥: 옛 기둥 줄(양옆 폐허 아치) · 산호초 · 해파리 Jelly_*(Unity 가 둥실) · 물고기 떼 FishSchool_*(원점 = 돔 가운데, Unity 가 돌림) · 바다 위 임금님 부표 Float_KingBuoy
  · Palace_Collision(보이지 않음) · 표시 Mark_*(DomeCenter · Arena · BossSpawn · Patrol_1~10 · Throne · Entrance · DomeTop)
실행: <bpy 파이썬> blender/build_palace.py [--preview]   (build_whale.py 결과 exports/Goraegorae.blend 필요 — 석상)"""
import sys, os, math, random
sys.path.insert(0, os.path.dirname(__file__))
import bpy, bmesh
from mathutils import Vector, Matrix
from lib import mat, reset_scene, to_object
from meshkit import Frame, Builder
import whale_king_parts as WK

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World"); EXP = os.path.join(ROOT, "exports"); PRE = os.path.join(ROOT, "previews")
random.seed(3)

# ── 자리(build_seaside.py 와 같은 수) ────────────────────────────
C = Vector((-50.012, 28.328)); R = 586.0; N = 24; TIP = math.radians(-36.4136); SEA = -0.8
T2 = C + R * Vector((math.cos(TIP), math.sin(TIP))); U2 = (T2 - C).normalized(); V2 = Vector((-U2.y, U2.x))
PALACE_A, PALACE_B, FLOOR_Z, DOME_R = 165.0, 0.0, -42.0, 36.0
O2 = T2 + U2 * PALACE_A + V2 * PALACE_B
F = Frame(Vector((O2.x, O2.y, FLOOR_Z)), Vector((U2.x, U2.y, 0)), Vector((V2.x, V2.y, 0)))   # 로컬 x = 바다 쪽(궁전 뒤), y = 옆, z = 위
SURF = SEA - FLOOR_Z                                                                          # 로컬 해수면 높이(41.2)

# ── 고래 석상 원본(기본 자세, 줄인 메시) ─────────────────────────
reset_scene()
with bpy.data.libraries.load(os.path.join(EXP, "Goraegorae.blend")) as (src, dst): dst.objects = [n for n in src.objects]
whale_src = None
for o in dst.objects:
    if o is None: continue
    bpy.context.collection.objects.link(o)
    if o.type == "ARMATURE": o.data.pose_position = "REST"
    if o.type == "MESH": whale_src = o
dm = whale_src.modifiers.new("dec", "DECIMATE"); dm.ratio = 0.22
dg = bpy.context.evaluated_depsgraph_get(); ev = whale_src.evaluated_get(dg); WHALE_ME = bpy.data.meshes.new_from_object(ev)
for o in list(bpy.data.objects): bpy.data.objects.remove(o)

def M_(name, color, **kw): m = mat(name, color, **kw); m.diffuse_color = (*color, 1); return m
STA = M_("Palace_StoneA", (0.55, 0.6, 0.8), rough=0.85); STB = M_("Palace_StoneB", (0.44, 0.49, 0.72), rough=0.85); STD = M_("Palace_StoneDark", (0.28, 0.32, 0.52), rough=0.9)
GOLD = M_("Palace_GoldGlow", (0.95, 0.8, 0.45), rough=0.35, metal=0.6, emit=(0.9, 0.7, 0.3), emit_strength=0.6); GOLDM = M_("Palace_Gold", (0.95, 0.76, 0.35), rough=0.3, metal=0.9)
CRY = M_("Palace_CrystalGlow", (0.45, 0.85, 1.0), rough=0.1, emit=(0.4, 0.8, 1.0), emit_strength=3.0); ORB = M_("Palace_OrbGlow", (0.85, 0.92, 1.0), rough=0.2, emit=(0.8, 0.9, 1.0), emit_strength=3.0)
ROOF = M_("Palace_Roof", (0.22, 0.33, 0.82), rough=0.6); BAN = M_("Palace_Banner", (0.14, 0.18, 0.55), rough=0.9); WHT = M_("Palace_White", (0.95, 0.95, 0.98), rough=0.7)
CORP = M_("Palace_CoralPink", (0.98, 0.5, 0.75), rough=0.8); CORV = M_("Palace_CoralViolet", (0.68, 0.45, 0.95), rough=0.8)
SHELL = M_("Palace_Shell", (0.86, 0.76, 0.96), rough=0.35, metal=0.2); SHG = M_("Palace_ShellGlow", (1.0, 0.86, 0.95), rough=0.3, emit=(1.0, 0.8, 0.95), emit_strength=1.5)
EMB = M_("Palace_EmblemGlow", (0.38, 0.6, 1.0), rough=0.2, emit=(0.35, 0.55, 1.0), emit_strength=2.5); WIN = M_("Palace_WindowGlow", (0.45, 0.75, 1.0), rough=0.2, emit=(0.4, 0.7, 1.0), emit_strength=2.0)
STAT = M_("Palace_Statue", (0.5, 0.56, 0.74), rough=0.8); JELLY = M_("Jelly_Glow", (0.95, 0.72, 0.98), rough=0.3, emit=(0.9, 0.6, 1.0), emit_strength=1.6)
FISH = M_("Palace_Fish", (0.35, 0.5, 0.88), rough=0.5); BUB = M_("Palace_Bubble", (0.7, 0.88, 1.0), rough=0.05); RED = M_("Palace_Red", (0.8, 0.18, 0.2), rough=0.6)
ALL = [STA, STB, STD, GOLD, GOLDM, CRY, ORB, ROOF, BAN, WHT, CORP, CORV, SHELL, SHG, EMB, WIN, STAT, JELLY, FISH, BUB, RED]
def B(): return Builder(ALL, F)

FACE = Matrix(((0, 1, 0), (-1, 0, 0), (0, 0, 1)))                                # 모델 앞(-Y) → 로컬 -x(뭍 쪽)
def statue(b, x, y, z, scale, yaw=0.0, pitch=0.0):
    """왕관 쓴 고래고래 석상(고래 모델 공간: 앞 = -Y → 로컬 -x 쪽을 보게)."""
    bm = bmesh.new(); bm.from_mesh(WHALE_ME)
    for f in bm.faces: f.material_index = b.M("Palace_Statue"); f.smooth = True
    cr = WK.crown(1.0)
    for f in cr.faces: f.material_index = b.M("Palace_Gold")
    me = bpy.data.meshes.new("_c"); cr.to_mesh(me); cr.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)
    Lm = Matrix.Rotation(yaw, 3, "Z") @ Matrix.Rotation(pitch, 3, "Y") @ FACE
    M = Matrix.Translation(F.P(x, y, z)) @ (F.R @ Lm).to_4x4() @ Matrix.Diagonal((scale, scale, scale, 1))
    b.merge(bm, M)

def crystal(b, x, y, z, h, w, rot=0.0):
    """육각 쌍뿔 수정"""
    top = F.P(x, y, z + h); bot = F.P(x, y, z); mid = []
    for k in range(6):
        a = rot + k * math.pi / 3; mid.append(b.bm.verts.new(F.P(x + math.cos(a) * w, y + math.sin(a) * w, z + h * 0.35)))
    vt, vb = b.bm.verts.new(top), b.bm.verts.new(bot)
    for k in range(6):
        j = (k + 1) % 6; b.bm.faces.new((vt, mid[k], mid[j])).material_index = b.M("Palace_CrystalGlow"); b.bm.faces.new((vb, mid[j], mid[k])).material_index = b.M("Palace_CrystalGlow")

def coral(b, x, y, z, s, m):
    for k in range(5):
        a = random.uniform(0, 6.28); pts = [(x, y, z)]
        for t in range(1, 6): pts.append((x + math.cos(a) * 0.18 * t * s + random.uniform(-0.1, 0.1) * s, y + math.sin(a) * 0.18 * t * s, z + 0.32 * t * s))
        b.tube(pts, 0.09 * s, m, sides=5, taper=0.5)
        b.sphere(pts[-1], (0.11 * s, 0.11 * s, 0.11 * s), m, seg=8, rings=5)

def banner(b, x, y, h0, h1, w):
    """깃발: 파란 천(아래 뾰족) + 금테 + 흰 고래 + 작은 왕관 — 앞(-x)을 봄"""
    b.box(x, y, (h0 + h1) / 2 + 0.4, 0.08, w, h1 - h0 - 0.8, "Palace_Banner")
    vs = [b.bm.verts.new(F.P(x + dx, y + dy, h0 + dz)) for dx in (-0.04, 0.04) for (dy, dz) in ((-w / 2, 0.8), (w / 2, 0.8), (0, 0))]
    for q in ((0, 1, 2), (5, 4, 3), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)): b.bm.faces.new([vs[i] for i in q]).material_index = b.M("Palace_Banner")
    for dy in (-w / 2, w / 2): b.box(x - 0.06, y + dy, (h0 + h1) / 2 + 0.4, 0.05, 0.12, h1 - h0 - 0.8, "Palace_GoldGlow")
    b.box(x - 0.06, y, h1 - 0.05, 0.05, w + 0.1, 0.12, "Palace_GoldGlow")
    cz = (h0 + h1) / 2 + 0.6
    b.sphere((x - 0.08, y, cz), (0.03, w * 0.26, w * 0.16), "Palace_White"); b.sphere((x - 0.08, y - w * 0.27, cz - 0.04), (0.03, w * 0.12, w * 0.05), "Palace_White")
    b.sphere((x - 0.08, y + w * 0.3, cz + 0.06), (0.03, w * 0.08, w * 0.04), "Palace_White")
    cr = WK.crown(w * 0.9, at=Vector((0, 0, 0)))
    for f in cr.faces: f.material_index = b.M("Palace_Gold")
    b.merge(cr, Matrix.Translation(F.P(x - 0.12, y, cz + w * 0.18)) @ (F.R @ FACE).to_4x4())

def star_lines(b, cx, cy, z, r_out, r_in, width, mat_):
    """바닥 금빛 8각 별(선) + 바퀴살"""
    pts = []
    for k in range(16):
        a = k * math.pi / 8; r = r_out if k % 2 == 0 else r_in; pts.append((cx + math.cos(a) * r, cy + math.sin(a) * r))
    for k in range(16):
        (x0, y0), (x1, y1) = pts[k], pts[(k + 1) % 16]; L = math.hypot(x1 - x0, y1 - y0)
        b.box((x0 + x1) / 2, (y0 + y1) / 2, z, L, width, 0.04, mat_, rot=math.atan2(y1 - y0, x1 - x0))
    for k in range(8):
        a = k * math.pi / 4; b.box(cx + math.cos(a) * r_out * 0.5, cy + math.sin(a) * r_out * 0.5, z, r_out, width * 0.6, 0.035, mat_, rot=a)

def ring_line(b, cx, cy, z, r, width, mat_, seg=48):
    for k in range(seg):
        a0, a1 = 2 * math.pi * k / seg, 2 * math.pi * (k + 1) / seg; am = (a0 + a1) / 2
        b.box(cx + math.cos(am) * r, cy + math.sin(am) * r, z, 2 * math.pi * r / seg * 1.02, width, 0.04, mat_, rot=am + math.pi / 2)

# ══ 1. 결투장 ══════════════════════════════════════════════════
AX, AR, AZ = -11.0, 13.5, 0.25
A = B()
RINGS = [(0, 3, 8), (3, 6, 16), (6, 9, 24), (9, 11.5, 32), (11.5, AR, 40)]
for ri, (r0, r1, n) in enumerate(RINGS):
    for k in range(n):
        a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 1) / n; zj = AZ + random.uniform(-0.015, 0.02)
        q = [(r0, a0), (r1, a0), (r1, a1), (r0, a1)] if r0 > 0 else [(0, a0), (r1, a0), (r1, a1)]
        vs = [A.bm.verts.new(F.P(AX + math.cos(a) * r, math.sin(a) * r, zj)) for r, a in q]
        fc = A.bm.faces.new(vs); fc.material_index = A.M("Palace_StoneA" if (ri + k) % 2 == 0 else "Palace_StoneB")
N_ = 64; top = [A.bm.verts.new(F.P(AX + math.cos(2 * math.pi * k / N_) * AR, math.sin(2 * math.pi * k / N_) * AR, AZ)) for k in range(N_)]
bot = [A.bm.verts.new(F.P(AX + math.cos(2 * math.pi * k / N_) * AR, math.sin(2 * math.pi * k / N_) * AR, -0.6)) for k in range(N_)]
for k in range(N_): A.bm.faces.new((top[k], bot[k], bot[(k + 1) % N_], top[(k + 1) % N_])).material_index = A.M("Palace_StoneDark")
star_lines(A, AX, 0, AZ + 0.02, 10.5, 4.2, 0.22, "Palace_GoldGlow"); ring_line(A, AX, 0, AZ + 0.02, 6.2, 0.16, "Palace_GoldGlow"); ring_line(A, AX, 0, AZ + 0.02, 12.8, 0.2, "Palace_GoldGlow", 64)
A.cyl((AX, 0, AZ), (AX, 0, AZ + 0.05), 1.3, "Palace_CrystalGlow", seg=24)
for k in range(14):                                                               # 둘레 짧은 기둥(앞 입구 · 뒤 계단 쪽은 비움)
    a = 2 * math.pi * k / 14 + 0.11
    if abs(math.atan2(math.sin(a), math.cos(a))) < 0.42 or abs(math.atan2(math.sin(a), math.cos(a))) > math.pi - 0.42: continue
    x, y = AX + math.cos(a) * 14.6, math.sin(a) * 14.6
    A.cyl((x, y, 0), (x, y, 2.2 if k % 2 else 1.4), 0.45, "Palace_StoneB", seg=10); A.cyl((x, y, 2.2 if k % 2 else 1.4), (x, y, 2.45 if k % 2 else 1.65), 0.6, "Palace_StoneDark", seg=10)
for a in (0.9, 1.5, 2.2, -0.9, -1.5, -2.2):                                     # 수정 받침
    x, y = AX + math.cos(a) * 16.6, math.sin(a) * 16.6
    A.box(x, y, 0.6, 1.3, 1.3, 1.2, "Palace_StoneA", rot=a); A.box(x, y, 1.25, 1.5, 1.5, 0.14, "Palace_GoldGlow", rot=a)
    crystal(A, x, y, 1.3, 2.0, 0.42, a); crystal(A, x + 0.35, y + 0.3, 1.3, 1.1, 0.25, a + 0.5); crystal(A, x - 0.3, y - 0.35, 1.3, 1.2, 0.22, a - 0.4)
arena = A.done("Palace_Arena")

# ══ 2. 궁전 건물(바닥 단 · 계단 · 벽 · 탑 · 지붕) ═══════════════════
G = B(); HZ = 4.25; X0, X1, YW = 6.5, 27.0, 12.0
G.box((X0 + X1) / 2, 0, HZ / 2, X1 - X0, 2 * YW, HZ, "Palace_StoneB")                 # 바닥 단
for k in range(int((X1 - X0) / 2)): G.box(X0 + 1 + 2 * k, -YW - 0.05, HZ / 2, 1.8, 0.1, HZ - 0.4, "Palace_StoneA"); G.box(X0 + 1 + 2 * k, YW + 0.05, HZ / 2, 1.8, 0.1, HZ - 0.4, "Palace_StoneA")
G.box(X0 - 0.05, 0, HZ - 0.2, 0.2, 2 * YW + 0.2, 0.4, "Palace_GoldGlow")
STEPS = 17
for i in range(STEPS):                                                            # 계단(17칸 · 높이 0.25)
    z1 = 0.25 * (i + 1); x = 2.3 + 0.25 * i
    G.box(x + 0.125, 0, z1 / 2, 0.25, 7.0, z1, "Palace_StoneA" if i % 2 else "Palace_StoneB")
for s in (-1, 1):
    G.box(4.4, s * 3.8, 1.4, 4.4, 0.6, 2.8, "Palace_StoneDark"); G.sphere((2.4, s * 3.8, 1.2), (0.35, 0.35, 0.35), "Palace_OrbGlow")
    G.box(3.5, s * 6.8, 1.3, 3.2, 3.2, 2.6, "Palace_StoneA"); G.box(3.5, s * 6.8, 2.66, 3.5, 3.5, 0.15, "Palace_GoldGlow")   # 석상 받침
    statue(G, 3.5, s * 6.8, 2.7, 1.5, yaw=s * 0.25, pitch=-0.22); G.sphere((1.75, s * 6.8, 3.25), (0.5, 0.5, 0.5), "Palace_OrbGlow")
WT, WH = 1.0, 11.0                                                                # 벽 두께 · 높이(바닥 단 위)
DOOR = 2.4; DH = 6.8
for s in (-1, 1): G.box(X0 + 0.5, s * (DOOR + (YW - DOOR) / 2), HZ + WH / 2, WT, YW - DOOR, WH, "Palace_StoneA")   # 앞벽(문 양옆)
G.box(X0 + 0.5, 0, HZ + DH + (WH - DH) / 2, WT, 2 * DOOR, WH - DH, "Palace_StoneA")          # 문 위
G.tube([(X0 - 0.08, (DOOR + 0.2) * math.cos(math.pi * k / 20), HZ + DH - DOOR + (DOOR + 0.2) * math.sin(math.pi * k / 20)) for k in range(21)], 0.22, "Palace_GoldGlow", sides=6)   # 아치 금테
for s in (-1, 1): G.box(X0 - 0.05, s * (DOOR + 0.2), HZ + (DH - DOOR) / 2, 0.3, 0.44, DH - DOOR, "Palace_GoldGlow")
G.cyl((X0 - 0.1, 0, HZ + 9.0), (X0 - 0.35, 0, HZ + 9.0), 2.2, "Palace_EmblemGlow", seg=32); G.torus((X0 - 0.3, 0, HZ + 9.0), 2.25, 0.18, "Palace_GoldGlow", axis=(1, 0, 0), seg=32, sides=6)   # 고래 문장 창
G.sphere((X0 - 0.42, 0.15, HZ + 8.9), (0.06, 1.1, 0.62), "Palace_White"); G.sphere((X0 - 0.42, -1.1, HZ + 8.75), (0.06, 0.55, 0.22), "Palace_White"); G.sphere((X0 - 0.42, 1.25, HZ + 9.1), (0.06, 0.35, 0.18), "Palace_White")
cr = WK.crown(4.0, at=Vector((0, 0, 0)))
for fc in cr.faces: fc.material_index = G.M("Palace_Gold")
G.merge(cr, Matrix.Translation(F.P(X0 - 0.2, 0, HZ + 11.0)) @ (F.R @ FACE).to_4x4())
for s in (-1, 1):                                                                 # 옆벽 + 아치 창
    G.box((X0 + 1 + X1) / 2, s * (YW - 0.5), HZ + WH / 2, X1 - X0 - 1, WT, WH, "Palace_StoneA")
    for wx in (10.5, 14.5, 18.5, 22.5):
        for side in (-1, 1):
            yy = s * (YW - 0.5) + side * 0.52
            G.box(wx, yy, HZ + 4.5, 2.0, 0.05, 4.0, "Palace_WindowGlow"); G.cyl((wx, yy - side * 0.02, HZ + 6.5), (wx, yy + side * 0.03, HZ + 6.5), 1.0, "Palace_WindowGlow", seg=16)
            G.box(wx, yy + side * 0.04, HZ + 2.4, 2.4, 0.08, 0.2, "Palace_GoldGlow")
G.box(X1 - 0.5, 0, HZ + WH / 2, WT, 2 * YW, WH, "Palace_StoneA")                   # 뒷벽
RZ = HZ + WH; HC = 16.5; HOLE = 3.6                                               # 지붕(가운데 네모 구멍)
G.box((X0 + HC - HOLE) / 2, 0, RZ + 0.35, HC - HOLE - X0, 2 * YW, 0.7, "Palace_StoneDark"); G.box((HC + HOLE + X1) / 2, 0, RZ + 0.35, X1 - HC - HOLE, 2 * YW, 0.7, "Palace_StoneDark")
for s in (-1, 1): G.box(HC, s * (HOLE + (YW - HOLE) / 2), RZ + 0.35, 2 * HOLE, YW - HOLE, 0.7, "Palace_StoneDark")
for k in range(8):
    a = k * math.pi / 4 + math.pi / 8; G.box(HC + math.cos(a) * (HOLE + 0.4), math.sin(a) * (HOLE + 0.4), RZ + 0.9, 3.2, 0.4, 0.5, "Palace_GoldGlow", rot=a + math.pi / 2)
G.box(X0 + 0.5, 0, RZ + 0.2, 1.4, 2 * YW + 0.6, 0.4, "Palace_GoldGlow")
# 탑(x, y, 반지름, 높이, 고깔, 밑동) — 앞 탑은 앞벽 바깥에 붙고, 가운데 탑은 지붕 위에 섬(안쪽 방을 막지 않게)
TOWERS = [(6.0, -12.5, 1.8, 17, 6, HZ), (6.0, 12.5, 1.8, 17, 6, HZ), (6.0, -6.2, 1.3, 18.5, 6, HZ), (6.0, 6.2, 1.3, 18.5, 6, HZ), (10.5, 0, 2.0, 21, 7, RZ + 0.7),
          (16.5, -12.6, 1.5, 16.5, 5.5, HZ), (16.5, 12.6, 1.5, 16.5, 5.5, HZ), (27.2, -12.2, 1.5, 15.5, 5, HZ), (27.2, 12.2, 1.5, 15.5, 5, HZ)]
for x, y, r, h, sp, z0 in TOWERS:
    G.cyl((x, y, z0), (x, y, h), r, "Palace_StoneA", seg=12); G.cyl((x, y, h), (x, y, h + 0.4), r + 0.3, "Palace_GoldGlow", seg=12)
    G.cyl((x, y, h + 0.4), (x, y, h + 0.4 + sp), r + 0.35, "Palace_Roof", seg=12, r1=0.05); G.sphere((x, y, h + 0.6 + sp), (0.22, 0.22, 0.22), "Palace_Gold")
    for k in range(4):
        a = k * math.pi / 2 + math.pi / 4; G.box(x + math.cos(a) * (r - 0.05), y + math.sin(a) * (r - 0.05), h - 2.5, 0.12, 0.35, 1.4, "Palace_WindowGlow", rot=a)
building = G.done("Palace_Building")

# ══ 3. 궁전 안(왕좌의 방) ═════════════════════════════════════════
I = B(); FZ = HZ + 0.02
star_lines(I, HC, 0, FZ, 5.0, 2.0, 0.16, "Palace_GoldGlow"); ring_line(I, HC, 0, FZ, 5.6, 0.14, "Palace_GoldGlow"); I.cyl((HC, 0, FZ), (HC, 0, FZ + 0.04), 0.9, "Palace_CrystalGlow", seg=20)
for s in (-1, 1):
    for cx in (9.5, 13.0, 16.5, 20.0, 23.5):
        I.cyl((cx, s * 9.0, HZ), (cx, s * 9.0, RZ), 0.45, "Palace_StoneB", seg=12); I.box(cx, s * 9.0, HZ + 0.25, 1.2, 1.2, 0.5, "Palace_StoneDark"); I.box(cx, s * 9.0, RZ - 0.3, 1.2, 1.2, 0.6, "Palace_GoldGlow")
for i in range(3): I.box(23.3 + 0.0 * i, 0, HZ + 0.15 + 0.3 * i, 4.4 - 1.0 * i, 8.0 - 1.6 * i, 0.3, "Palace_StoneA" if i % 2 == 0 else "Palace_StoneB")   # 왕좌 단
for k in range(9):                                                                # 부채꼴 조개(갈비 9개)
    a = math.radians(-80 + 20 * k); r_ = 2.6
    I.sphere((25.0, math.sin(a) * r_ * 0.55, HZ + 1.05 + 2.4 + math.cos(a) * r_ * 0.9), (0.25, 0.62, 1.7), "Palace_Shell", seg=12, rings=8, rot=Matrix.Rotation(-a, 3, "X"))
I.sphere((24.9, 0, HZ + 1.0 + 2.2), (0.2, 2.2, 2.4), "Palace_ShellGlow", seg=20, rings=10)
I.box(24.2, 0, HZ + 1.35, 1.6, 2.6, 0.5, "Palace_Shell"); I.sphere((24.2, 0, HZ + 1.7), (0.75, 1.2, 0.25), "Palace_ShellGlow")   # 자리 · 방석
I.sphere((25.0, 0, HZ + 6.1), (0.55, 0.55, 0.55), "Palace_OrbGlow")              # 진주
for s in (-1, 1):
    I.box(19.0, s * 6.8, HZ + 0.8, 2.4, 2.4, 1.6, "Palace_StoneA"); I.box(19.0, s * 6.8, HZ + 1.65, 2.7, 2.7, 0.12, "Palace_GoldGlow")
    statue(I, 19.0, s * 6.8, HZ + 1.7, 0.95, yaw=s * 0.6, pitch=-0.15); I.sphere((17.8, s * 6.8, HZ + 2.3), (0.4, 0.4, 0.4), "Palace_OrbGlow")
    for cx in (10.0, 14.0, 22.0):
        crystal(I, cx, s * 10.8, HZ, 1.8, 0.4, cx); crystal(I, cx + 0.5, s * 10.6, HZ, 1.0, 0.25, cx + 1)
    coral(I, 8.5, s * 10.5, HZ, 1.3, "Palace_CoralPink"); coral(I, 25.5, s * 10.3, HZ, 1.2, "Palace_CoralViolet")
    for cx in (11.0, 15.0, 18.0):                                                 # 가운데 길 등불
        I.cyl((cx, s * 3.4, HZ), (cx, s * 3.4, HZ + 1.3), 0.12, "Palace_StoneDark", seg=8); I.sphere((cx, s * 3.4, HZ + 1.5), (0.26, 0.26, 0.3), "Palace_OrbGlow")
    banner(I, 26.35, s * 6.0, HZ + 3.0, HZ + 9.5, 2.2)
chand = WK.crown(9.0, at=Vector((0, 0, 0)))                                       # 지붕 구멍 아래 왕관 샹들리에
for fc in chand.faces: fc.material_index = I.M("Palace_GoldGlow")
I.merge(chand, Matrix.Translation(F.P(HC, 0, HZ + 6.8)) @ (F.R @ FACE).to_4x4())
I.sphere((HC, 0, HZ + 7.6), (0.9, 0.9, 0.9), "Palace_OrbGlow")
for k in range(4): a = k * math.pi / 2 + math.pi / 4; I.cyl((HC + math.cos(a) * 1.9, math.sin(a) * 1.9, HZ + 8.6), (HC + math.cos(a) * (HOLE + 0.3), math.sin(a) * (HOLE + 0.3), RZ + 0.6), 0.05, "Palace_Gold", seg=5)
hall = I.done("Palace_Hall")

# ══ 4. 바깥 꾸밈(깃발 · 폐허 · 산호초) ═════════════════════════════
D = B()
for s in (-1, 1):
    D.cyl((-3.0, s * 13.8, 0), (-3.0, s * 13.8, 10.5), 0.22, "Palace_StoneDark", seg=10); D.sphere((-3.0, s * 13.8, 10.7), (0.35, 0.35, 0.35), "Palace_Gold")
    D.cyl((-3.0, s * 13.8 - 1.5, 9.6), (-3.0, s * 13.8 + 1.5, 9.6), 0.09, "Palace_Gold", seg=8)
    banner(D, -3.25, s * 13.8, 3.6, 9.5, 2.6)
    for k in range(6):                                                            # 옛 기둥 줄(폐허 아치)
        x = -22 + 6.5 * k; y = s * 24.0
        if k == 3: continue
        h = 9.0 if k % 2 == 0 else 6.5
        D.cyl((x, y, 0), (x, y, h), 0.8, "Palace_StoneB", seg=10); D.box(x, y, h + 0.3, 2.2, 2.2, 0.6, "Palace_StoneDark")
        if k < 5 and k != 2: D.box(x + 3.25, y, 9.6, 6.5, 1.4, 0.9, "Palace_StoneA")
    for k in range(10):
        a = random.uniform(-1.3, 1.3) + (0 if s > 0 else math.pi); r = random.uniform(19, 33)
        x, y = math.cos(a) * r - 4, math.sin(a) * r
        coral(D, x, y, 0, random.uniform(0.9, 1.8), "Palace_CoralPink" if k % 2 else "Palace_CoralViolet")
    for k in range(4): crystal(D, random.uniform(-30, 20), s * random.uniform(18, 30), 0, random.uniform(1.0, 2.6), random.uniform(0.3, 0.6), random.uniform(0, 6))
decor = D.done("Palace_Decor")

# ══ 5. 공기방울 돔 · 해파리 · 물고기 떼 · 임금님 부표 ═══════════════
db = bmesh.new(); bmesh.ops.create_uvsphere(db, u_segments=56, v_segments=28, radius=DOME_R)
for f in db.faces: f.smooth = True
bmesh.ops.translate(db, vec=F.P(0, 0, 0), verts=db.verts[:]); dome = to_object("Palace_Dome", db, [BUB])
jellies = []
for i in range(12):
    a = 2 * math.pi * i / 12 + random.uniform(-0.2, 0.2); r = random.uniform(39, 52); z = random.uniform(6, 30)
    J = B(); c = (math.cos(a) * r, math.sin(a) * r, z); s = random.uniform(0.7, 1.3)
    J.sphere(c, (0.9 * s, 0.9 * s, 0.7 * s), "Jelly_Glow", seg=14, rings=7)
    for k in range(6):
        t = k * math.pi / 3; J.tube([(c[0] + math.cos(t) * 0.5 * s, c[1] + math.sin(t) * 0.5 * s, c[2] - 0.2 * s - 0.6 * s * j + 0.15 * s * math.sin(j * 2 + k)) for j in range(6)], 0.05 * s, "Jelly_Glow", sides=4, taper=0.6)
    jellies.append(J.done(f"Jelly_{i + 1}", origin=F.P(*c)))
schools = []
for i in range(3):
    S = B(); base = 2 * math.pi * i / 3; r0 = 40 + 6 * i; z0 = 8 + 7 * i
    for k in range(9):
        a = base + random.uniform(-0.15, 0.15); r = r0 + random.uniform(-2, 2); z = z0 + random.uniform(-1.5, 1.5)
        p = (math.cos(a) * r, math.sin(a) * r, z); yaw = a + math.pi / 2                                  # 원을 따라 앞으로
        S.sphere(p, (0.35, 0.12, 0.15), "Palace_Fish", seg=10, rings=6, rot=Matrix.Rotation(yaw, 3, "Z"))
        tl = (p[0] - math.cos(yaw) * 0.4, p[1] - math.sin(yaw) * 0.4, p[2]); S.sphere(tl, (0.12, 0.03, 0.12), "Palace_Fish", seg=6, rings=4, rot=Matrix.Rotation(yaw, 3, "Z"))
    schools.append(S.done(f"FishSchool_{i + 1}", origin=F.P(0, 0, 0)))
KB = B(); kb0 = (0.0, 0.0, 0.0)
KB.sphere((0, 0, 0.15), (0.6, 0.6, 0.62), "Palace_Red", seg=16, rings=10); KB.torus((0, 0, 0.2), 0.62, 0.08, "Palace_White", seg=20, sides=6)
KB.cyl((0, 0, 0.7), (0, 0, 1.6), 0.06, "Palace_StoneDark", seg=6)
crb = WK.crown(1.6, at=Vector((0, 0, 0)))
for fc in crb.faces: fc.material_index = KB.M("Palace_Gold")
KB.merge(crb, Matrix.Translation(F.P(0, 0, 1.55)) @ (F.R @ FACE).to_4x4())
king_buoy = KB.done("Float_KingBuoy", origin=None)
me = king_buoy.data; me.transform(Matrix.Translation(-F.P(0, 0, 0))); king_buoy.location = F.P(-14, 6, SURF)

# ══ 6. 충돌(보이지 않음) ═════════════════════════════════════════
K = B()
K.cyl((AX, 0, -0.6), (AX, 0, AZ), AR, "Palace_StoneDark", seg=24)
for i in range(STEPS): z1 = 0.25 * (i + 1); K.box(2.3 + 0.25 * i + 0.125, 0, z1 / 2, 0.25, 7.0, z1, "Palace_StoneDark")
K.box((X0 + X1) / 2, 0, HZ / 2, X1 - X0, 2 * YW, HZ, "Palace_StoneDark")
for s in (-1, 1):
    K.box(4.4, s * 3.8, 1.4, 4.4, 0.6, 2.8, "Palace_StoneDark"); K.box(3.5, s * 6.8, 2.2, 3.2, 3.2, 4.4, "Palace_StoneDark")
    K.box(X0 + 0.5, s * (DOOR + (YW - DOOR) / 2), HZ + WH / 2, WT, YW - DOOR, WH, "Palace_StoneDark")
    K.box((X0 + 1 + X1) / 2, s * (YW - 0.5), HZ + WH / 2, X1 - X0 - 1, WT, WH, "Palace_StoneDark")
    for cx in (9.5, 13.0, 16.5, 20.0, 23.5): K.prism(cx, s * 9.0, HZ, RZ, 0.5, "Palace_StoneDark")
    K.box(19.0, s * 6.8, HZ + 1.8, 2.4, 2.4, 3.6, "Palace_StoneDark")
    K.prism(-3.0, s * 13.8, 0, 10.5, 0.3, "Palace_StoneDark")
    for k in range(6):
        if k != 3: K.prism(-22 + 6.5 * k, s * 24.0, 0, 9.0, 0.9, "Palace_StoneDark")
K.box(X0 + 0.5, 0, HZ + DH + (WH - DH) / 2, WT, 2 * DOOR, WH - DH, "Palace_StoneDark"); K.box(X1 - 0.5, 0, HZ + WH / 2, WT, 2 * YW, WH, "Palace_StoneDark")
for i in range(3): K.box(23.3, 0, HZ + 0.15 + 0.3 * i, 4.4 - 1.0 * i, 8.0 - 1.6 * i, 0.3, "Palace_StoneDark")
K.box(25.0, 0, HZ + 3.0, 0.8, 5.0, 6.0, "Palace_StoneDark")
for x, y, r, h, sp, z0 in TOWERS:
    if z0 <= HZ: K.prism(x, y, 0, h, r, "Palace_StoneDark", seg=10)
for k in range(14):
    a = 2 * math.pi * k / 14 + 0.11
    if abs(math.atan2(math.sin(a), math.cos(a))) < 0.42 or abs(math.atan2(math.sin(a), math.cos(a))) > math.pi - 0.42: continue
    K.prism(AX + math.cos(a) * 14.6, math.sin(a) * 14.6, 0, 2.4, 0.55, "Palace_StoneDark")
for a in (0.9, 1.5, 2.2, -0.9, -1.5, -2.2): K.box(AX + math.cos(a) * 16.6, math.sin(a) * 16.6, 1.0, 1.5, 1.5, 2.0, "Palace_StoneDark", rot=a)
coll = K.done("Palace_Collision"); coll.hide_render = True

# ══ 7. 표시 ══════════════════════════════════════════════════════
marks = []
def mark(name, x, y, z):
    e = bpy.data.objects.new(name, None); e.location = F.P(x, y, z); bpy.context.collection.objects.link(e); marks.append(e)
mark("Mark_DomeCenter", 0, 0, 0); mark("Mark_Arena", AX, 0, AZ + 0.05); mark("Mark_BossSpawn", AX + 5, 0, AZ + 0.05); mark("Mark_BossLook", AX - 5, 0, AZ + 0.05)
for k in range(8): a = 2 * math.pi * k / 8; mark(f"Mark_Patrol_{k + 1}", AX + math.cos(a) * 8.5, math.sin(a) * 8.5, AZ + 0.05)
mark("Mark_Patrol_9", 8.5, 0, HZ + 0.05); mark("Mark_Patrol_10", HC, 0, HZ + 0.05)
mark("Mark_Throne", 23.3, 0, HZ + 0.95); mark("Mark_Entrance", -31.0, 0, 0.1); mark("Mark_DomeTop", 0, 0, DOME_R); mark("Mark_HallLight", HC, 0, HZ + 6.5)
mark("Mark_KingBuoy", -14, 6, SURF)

allobj = [arena, building, hall, decor, dome, coll, king_buoy] + jellies + schools + marks
bpy.ops.object.select_all(action="DESELECT")
for o in allobj: o.select_set(True)
fbx = os.path.join(OUT, "Palace.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"MESH", "EMPTY"}, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", mesh_smooth_type="FACE")
print("EXPORTED", fbx, f"{os.path.getsize(fbx) / 1e6:.1f} MB", {o.name: len(o.data.vertices) for o in (arena, building, hall, decor)})
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(EXP, "Palace.blend"))
