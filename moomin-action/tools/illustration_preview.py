"""스킬 일러스트 v3 시제품(흑백 먹선풍): 인물 실루엣 + 마른 붓결 빛 획 + 먹 얼룩 바탕 + 흰 먹 튀김 + 하프톤 + 비네트, 속성색은 아주 조금.
(C# UI/SkillIllustration.cs 와 같은 알고리즘 — 한쪽을 바꾸면 다른 쪽도)"""
import math, random, numpy as np
from PIL import Image
W, H = 384, 200; ASP = W / H

def w32(a): return ((a + 2 ** 31) % 2 ** 32) - 2 ** 31
def hash2(x, y):
    """SkillPanelArt.Hash 와 같은 정수 해시(0..1)."""
    x = np.asarray(x, np.int64); y = np.asarray(y, np.int64)
    h = w32(x * 374761393 + y * 668265263); h = w32((h ^ (h >> 13)) * 1274126177); return ((h ^ (h >> 16)) & 0xffff) / 65535.0
def noise(x, y):
    xi = np.floor(x).astype(np.int64); yi = np.floor(y).astype(np.int64); tx = x - xi; ty = y - yi; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty)
    a = hash2(xi, yi) + (hash2(xi + 1, yi) - hash2(xi, yi)) * tx; b = hash2(xi, yi + 1) + (hash2(xi + 1, yi + 1) - hash2(xi, yi + 1)) * tx
    return a + (b - a) * ty

def capsule(X, Y, a, b, r):
    pax, pay = X - a[0], Y - a[1]; bax, bay = b[0] - a[0], b[1] - a[1]
    h = np.clip((pax * bax + pay * bay) / max(1e-9, bax * bax + bay * bay), 0, 1)
    return np.hypot(pax - bax * h, pay - bay * h) - r

def figure(X, Y, pose, ox, s):
    """실루엣 SDF. 좌표: x 0..ASP, y 0(아래)..1(위). pose = (몸 기울기, 앞팔 각도, 뒷팔 각도, 앞다리, 뒷다리, 무기 각도)"""
    lean, fa, ba, fl, bl, wa = [math.radians(v) for v in pose]
    hip = (ox, 0.30 * s + 0.05); up = (math.sin(lean), math.cos(lean))
    chest = (hip[0] + up[0] * 0.22 * s, hip[1] + up[1] * 0.22 * s); head = (chest[0] + up[0] * 0.09 * s, chest[1] + up[1] * 0.09 * s)
    def limb(o, ang, l1, l2, bend):
        e = (o[0] + math.cos(ang) * l1 * s, o[1] + math.sin(ang) * l1 * s); w = (e[0] + math.cos(ang + bend) * l2 * s, e[1] + math.sin(ang + bend) * l2 * s); return e, w
    fe, fw = limb(chest, fa, 0.12, 0.11, -0.3); be, bw = limb(chest, ba, 0.11, 0.1, 0.4)
    fk, ff = limb(hip, fl, 0.15, 0.15, -0.5); bk, bf = limb(hip, bl, 0.15, 0.15, 0.6)
    d = np.hypot(X - head[0], Y - head[1]) - 0.05 * s
    for a, b, r in ((hip, chest, 0.05), (chest, fe, 0.022), (fe, fw, 0.02), (chest, be, 0.022), (be, bw, 0.02), (hip, fk, 0.03), (fk, ff, 0.026), (hip, bk, 0.03), (bk, bf, 0.026)):
        d = np.minimum(d, capsule(X, Y, a, b, r * s))
    # 망토(뒤로 펄럭이는 삼각형 근사: 굵어지는 캡슐 2개)
    cape = (chest[0] - 0.18 * s, chest[1] - 0.12 * s); d = np.minimum(d, capsule(X, Y, chest, cape, 0.045 * s))
    wt = (fw[0] + math.cos(wa) * 0.34 * s, fw[1] + math.sin(wa) * 0.34 * s)
    return d, capsule(X, Y, fw, wt, 0.008 * s), wt

def ell(X, Y, c, rx, ry, rot=0.0):
    """회전한 타원 SDF(근사)."""
    ca, sa = math.cos(rot), math.sin(rot); dx, dy = X - c[0], Y - c[1]; u = dx * ca + dy * sa; v = -dx * sa + dy * ca
    return (np.hypot(u / rx, v / ry) - 1) * min(rx, ry)

def whale(X, Y, c, s, tilt, jaw=0.0, curl=0.0):
    """고래고래 옆모습 실루엣 SDF(오른쪽을 봄). c = 몸 중심, s = 크기, tilt = 기울기(도, + = 주둥이가 위), jaw = 입 벌림(라디안), curl = 꼬리 휨(+ 위).
    반환: (sdf, 주둥이 끝, 숨구멍, 지느러미 위치)"""
    t = math.radians(tilt); ct, st = math.cos(t), math.sin(t)
    P = lambda x, y: (c[0] + (x * ct - y * st) * s, c[1] + (x * st + y * ct) * s)
    d = ell(X, Y, P(0, 0), .32 * s, .13 * s, t)                                            # 몸
    d = np.minimum(d, ell(X, Y, P(.2, .03), .14 * s, .12 * s, t))                           # 볼록한 이마
    d = np.minimum(d, ell(X, Y, P(.37, -.005), .13 * s, .035 * s, t + jaw * .5))           # 위 주둥이
    d = np.minimum(d, ell(X, Y, P(.33, -.05), .11 * s, .028 * s, t - jaw))                 # 아래턱
    d = np.minimum(d, ell(X, Y, P(.02, .14), .05 * s, .07 * s, t - .5))                    # 등지느러미
    d = np.minimum(d, ell(X, Y, P(.14, -.12), .08 * s, .028 * s, t - .6))                  # 가슴지느러미
    pts = [P(-.22 - .34 * k / 8, curl * .18 * (k / 8) ** 2) for k in range(9)]
    for k in range(8): d = np.minimum(d, capsule(X, Y, pts[k], pts[k + 1], (.1 - .075 * k / 8) * s))   # 가늘어지는 꼬리
    end = pts[-1]; ft = t + math.atan2(curl * .36, -.34)
    for sg in (1, -1): d = np.minimum(d, ell(X, Y, (end[0] - .03 * s, end[1] + sg * .05 * s), .09 * s, .025 * s, ft + sg * 1.0))   # 갈라진 꼬리 지느러미
    return d, P(.5, -.02), P(.16, .15), P(.14, -.12)

def smin(a, b, k=40.0):
    h = np.clip(0.5 + 0.5 * (b - a) * k, 0, 1); return b * (1 - h) + a * h - h * (1 - h) / k

def boxd(u, v, hx, hy):
    qx, qy = np.abs(u) - hx, np.abs(v) - hy; return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0)

def cube(X, Y, c, s, sx, sy, tilt, kind):
    """일반 몬스터 실루엣 SDF(C# SkillIllustration.CubeSdf 와 같음). kind = "slime"(핑크큐브: 혹 3×3 이 뭉친 네모) | "block"(그린볼런: 계단 모서리 + 가운데 구멍 + 오른쪽 톱니).
    sx·sy = 늘어남(1 이 기본), tilt = 기울기(도, + 반시계). 반환: (sdf, 얼굴 좌표 함수 P(lx, ly))."""
    t = math.radians(tilt); ct, st = math.cos(t), math.sin(t); dx, dy = X - c[0], Y - c[1]
    u = (dx * ct + dy * st) / (s * sx); v = (-dx * st + dy * ct) / (s * sy); k = s * min(sx, sy)
    P = lambda lx, ly: (c[0] + (lx * sx * ct - ly * sy * st) * s, c[1] + (lx * sx * st + ly * sy * ct) * s)
    if kind == "slime":
        d = None
        for i in (-1, 0, 1):
            for j in (-1, 0, 1):
                e = np.hypot(u - i * .13, v - j * .13) - .085; d = e if d is None else smin(d, e)
        return d * k, P
    d = boxd(u, v, .2, .2)
    for i in (-1, 1):
        for j in (-1, 1): d = np.maximum(d, -boxd(u - i * .2, v - j * .2, .055, .055))   # 계단 모서리
    d = np.maximum(d, -boxd(u, v, .07, .07))                                              # 가운데 구멍
    return d * k, P

def gear(X, Y, c, r):
    """톱니바퀴 SDF(이 8개 + 구멍) — 던진 톱니(밝게)."""
    dx, dy = X - c[0], Y - c[1]; rr = np.hypot(dx, dy); th = np.arctan2(dy, dx)
    R_ = np.where(np.cos(8 * th) > 0.3, r, r * .74); return np.maximum(rr - R_, -(rr - r * .3))

def render(motif, seed, accent, subject="human"):
    rng = np.random.default_rng(seed); R = lambda a, b: a + rng.random() * (b - a)
    xs = (np.arange(W) + .5) / W * ASP; ys = (np.arange(H) + .5) / H; X, Y = np.meshgrid(xs, ys[::-1])
    # 바탕: 어두운 종이 + 먹 얼룩(낮은 주파수 노이즈) + 거친 입자
    lum = 0.13 + 0.08 * Y + rng.random((H, W)) * 0.05 + (noise(X * 4 + seed, Y * 4) - .5) * 0.2 + (noise(X * 13, Y * 13 + seed) - .5) * 0.06; glow = np.zeros_like(X); acc = np.zeros_like(X)
    def stroke(a, b, w, l=1.0, ac=0.0, taper=True):
        nonlocal lum, glow, acc
        pax, pay = X - a[0], Y - a[1]; bax, bay = b[0] - a[0], b[1] - a[1]
        L = max(1e-9, math.hypot(bax, bay)); h = np.clip((pax * bax + pay * bay) / (L * L), 0, 1); t = np.sin(np.pi * np.clip(h, .02, .98)) if taper else 1
        d = np.hypot(pax - bax * h, pay - bay * h) - w * t; core = np.clip(0.5 - d * H * 0.8, 0, 1)
        across = (pax * -bay + pay * bax) / L                                   # 획을 가로지르는 좌표 → 마른 붓결(획 방향 줄무늬)
        core = core * np.clip(0.45 + 0.75 * noise(h * L * 8, across * 260 + 17), 0, 1)
        lum = np.maximum(lum, core * l); glow += np.exp(-np.maximum(d, 0) * 40) * w * 6 * l; acc = np.maximum(acc, core * ac)
    def arc(c, rx, ry, a0, a1, w, l=1, ac=0, n=24):
        P = lambda t: (c[0] + math.cos(math.radians(a0 + (a1 - a0) * t)) * rx, c[1] + math.sin(math.radians(a0 + (a1 - a0) * t)) * ry)
        for k in range(n): stroke(P(k / n), P((k + 1) / n), w * max(.12, math.sin(math.pi * (k + .5) / n)), l, ac, taper=False)
    fx = R(0.45, 0.7)
    lum = lum + 0.22 * np.exp(-((X - fx - 0.35) ** 2 / 0.25 + (Y - 0.5) ** 2 / 0.35)) * (0.6 + 0.8 * noise(X * 6, Y * 6 + 3))   # 연기 같은 밝은 배경(실루엣이 보이게)
    W_ = None; C_ = None; G_ = None   # 고래 · 일반 몬스터 실루엣 설정
    if subject == "whale":
        if motif == "Beam":     # 주둥이를 들고 앞으로 굵은 빛줄기 + 결 + 고리
            W_ = ((fx - .25, .4), 1.0, 14, .45, -.4); _, tip, _, _ = whale(X, Y, *W_[:3], W_[3], W_[4])
            stroke(tip, (ASP + .1, tip[1] + .1), .075, 1, .3, taper=False); stroke(tip, (ASP + .1, tip[1] + .1), .03, 1, 0, taper=False)
            for k in range(22): o = R(-.12, .12); stroke((tip[0] + R(0, .3), tip[1] + o), (ASP + .1, tip[1] + .1 + o * 1.6), R(.002, .008), R(.5, .95), R(0, 1) > .6)
            for k in range(5): cx = tip[0] + .15 + k * (ASP - tip[0]) / 5; arc((cx, tip[1] + .1 * (cx - tip[0]) / (ASP - tip[0])), .025, .13, 0, 360, .006, .8, .4, n=16)
        elif motif == "Leap":   # 하늘에서 내리꽂힘 + 땅에서 솟는 충격 가시 + 낮게 퍼지는 충격파
            W_ = ((fx + .3, .6), .8, -28, .1, .7); c = (fx + .45, .04)
            for k in range(24): a = math.radians(R(10, 170)); l = R(.15, .55); stroke(c, (c[0] + math.cos(a) * l * 1.6, c[1] + math.sin(a) * l), R(.008, .03), R(.6, 1), R(0, 1) > .6)
            for k in range(3): arc(c, .3 + k * .18, .05 + k * .025, 0, 180, .01, .8, .3, n=20)
            for k in range(14): x0 = fx + R(-.1, .5); stroke((x0, R(.75, 1)), (x0 + .1, R(.45, .6)), R(.002, .006), R(.4, .8))
        elif motif == "Spout":  # 숨구멍에서 하늘로 치솟는 물기둥 + 흩날리는 물방울
            W_ = ((fx + .15, .26), .95, 4, .05, .2); _, _, hole, _ = whale(X, Y, *W_[:3], W_[3], W_[4])
            for k in range(10): o = R(-.035, .035); stroke((hole[0] + o, hole[1]), (hole[0] + o * 2.5, 1.05), R(.01, .03), R(.7, 1), R(0, 1) > .5, taper=False)
            for k in range(40): a = math.radians(R(20, 160)); l = R(.15, .5); p = (hole[0] + math.cos(a) * l * 1.3, .9 + math.sin(a) * l * .2 - l * .4); stroke(p, (p[0] + .01, p[1] - .02), R(.004, .012), R(.6, 1))
        elif motif == "Rush":   # 들이받음: 뒤로 흐르는 속도선 + 주둥이 끝 충격
            W_ = ((fx + .32, .45), .9, -6, .1, .1); _, tip, _, _ = whale(X, Y, *W_[:3], W_[3], W_[4])
            for k in range(34): y = R(.1, .9); x0 = R(0, fx); stroke((x0, y), (x0 - R(.3, .8), y), R(.002, .012), R(.3, .9), R(0, 1) > .75)
            for k in range(16): a = math.radians(R(-80, 80)); stroke(tip, (tip[0] + math.cos(a) * R(.1, .3), tip[1] + math.sin(a) * R(.1, .3)), R(.006, .02), R(.7, 1), R(0, 1) > .6)
        elif motif == "Flurry": # 지느러미 주변에 엇갈린 궤적
            W_ = ((fx + .3, .5), .9, 6, .05, -.3); _, _, _, fp = whale(X, Y, *W_[:3], W_[3], W_[4])
            for k in range(7): arc((fp[0] + R(-.05, .25), fp[1] + R(-.1, .05)), R(.12, .3), R(.1, .22), R(0, 360), R(0, 360), R(.006, .022), R(.6, 1), R(0, 1) > .5, n=14)
        else:                   # 꼬리 휘두르기: 꼬리를 휘감는 큰 호
            W_ = ((fx + .25, .42), .9, 0, .05, .9)
            arc((fx + .05, .35), .62, .42, 200, 40, .045, 1, .3, n=26)
            for k in range(7): arc((fx + .05 + R(-.05, .05), .35 + R(-.05, .05)), R(.5, .7), R(.32, .48), R(190, 215), R(30, 60), R(.002, .009), R(.5, .9), R(0, 1) > .6)
        pose = None
    elif subject in ("slime", "block"):   # 일반 몬스터: 핑크큐브말랑이 · 그린볼런
        kind = subject; G_ = None
        if motif == "Thrust":    # 쭈욱 늘어나 찌르기: 오른쪽으로 길게
            C_ = ((fx + .28, .36), 1.0, 2.1, .68, 0); tip = (fx + .28 + .21 * 2.1, .36)
            for k in range(18): y = R(.15, .75); stroke((R(tip[0] - .1, ASP), y), (ASP + .2, .36 + (y - .36) * .3), R(.002, .008), R(.4, .9), R(0, 1) > .7)
            for k in range(12): a = math.radians(R(-70, 70)); stroke(tip, (tip[0] + math.cos(a) * R(.08, .22), tip[1] + math.sin(a) * R(.08, .22)), R(.005, .015), R(.7, 1), R(0, 1) > .6)
        elif motif == "Flurry":  # 통통 연속: 땅 위로 튀어 온 궤적 세 번
            C_ = ((fx + .38, .5), .85, .88, 1.18, -6)
            for k in range(3): arc((fx - .45 + k * .3, .22), .15, .2 + k * .03, 180, 0, .012, .9, .4, n=16)
            for k in range(3): arc((fx - .3 + k * .3, .12), .07, .02, 0, 360, .006, .6, .2, n=12)
        elif motif == "Smash":   # 블럭 박치기: 앞으로 기울며 쿵 + 충격
            C_ = ((fx + .25, .4), 1.0, 1, 1, -22); c = (fx + .55, .2)
            for k in range(22): a = math.radians(R(-60, 120)); l = R(.15, .5); stroke(c, (c[0] + math.cos(a) * l, c[1] + math.sin(a) * l), R(.008, .03), R(.6, 1), R(0, 1) > .6)
        elif motif == "Throw":   # 톱니 던지기: 블럭이 비틀어 던진 톱니가 빙글 날아감
            C_ = ((fx - .1, .38), .9, 1, 1, 8); G_ = ((fx + .62, .55), .09)
            for k in range(16): y = R(.42, .68); x0 = R(fx + .05, fx + .5); stroke((x0, y), (x0 - R(.15, .45), y - .04), R(.002, .01), R(.4, .9), R(0, 1) > .6)
            for k in range(3): arc(G_[0], .13 + k * .03, .13 + k * .03, R(0, 90) + 120 * k, R(0, 90) + 120 * k + 70, .006, .9, .5, n=10)
        else:                    # 말랑 박치기(Rush): 옆으로 눌리며 들이받음 + 속도선
            C_ = ((fx + .3, .4), 1.0, 1.25, .84, -8)
            for k in range(34): y = R(.1, .9); x0 = R(0, fx); stroke((x0, y), (x0 - R(.3, .8), y), R(.002, .012), R(.3, .9), R(0, 1) > .75)
        pose = None
    elif motif == "Slash":
        pose = (15, 70, 200, -60, -120, 30); arc((fx + 0.5, 0.2), 1.0, 0.75, 180, 75, 0.05, 1, 0.3)
        for k in range(8): arc((fx + 0.5 + R(-.1, .1), 0.2 + R(-.1, .1)), R(.85, 1.1), R(.6, .85), R(170, 190), R(70, 90), R(.002, .01), R(.5, .9), R(0, 1) > .6)
    elif motif == "Thrust":
        pose = (35, 5, 190, -30, -150, 2); stroke((fx + .2, .47), (ASP, .5), .05, 1, .2)
        for k in range(18): y = R(.1, .9); stroke((R(fx + .3, ASP), y), (ASP + .2, .5 + (y - .5) * .3), R(.002, .008), R(.4, .9), R(0, 1) > .7)
    elif motif == "Smash":
        pose = (5, 100, 80, -70, -110, 95); c = (fx + .35, .05)
        for k in range(26): a = math.radians(R(15, 165)); l = R(.3, 1); stroke(c, (c[0] + math.cos(a) * l, c[1] + math.sin(a) * l), R(.01, .04), R(.6, 1), R(0, 1) > .6)
    elif motif == "Rush":
        pose = (45, -10, 160, -20, -170, -5)
        for k in range(36): y = R(.05, .95); x0 = R(0, ASP); stroke((x0, y), (x0 - R(.3, .9), y), R(.002, .012), R(.3, .9), R(0, 1) > .75)
    else:
        pose = (20, 40, 210, -55, -125, 60)
        for k in range(6): c = (fx + R(-.2, .6), R(.2, .8)); arc(c, R(.3, .6), R(.3, .5), R(0, 360), R(0, 360), R(.006, .025), R(.6, 1), R(0, 1) > .5, n=14)
    # 먹 얼룩(방울 + 주변 작은 튀김)
    for k in range(18):
        cx, cy, r = R(0, ASP), R(0, 1), R(.006, .03)
        for m in range(6):
            ox, oy, rr = (cx, cy, r) if m == 0 else (cx + R(-.08, .08), cy + R(-.06, .06), r * R(.12, .35))
            lum = np.where(np.hypot(X - ox, Y - oy) < rr, np.minimum(lum, .03), lum)
    # 흰 먹 튀김(연출 쪽에 몰림)
    for k in range(70):
        cx, cy, r = fx + R(-.3, 1.0), R(.05, .95), R(.0015, .006)
        glow = np.where(np.hypot(X - cx, Y - cy) < r, 2.5, glow)
    lum = np.clip(lum + glow * 0.35, 0, 1)
    lum = lum * lum * (3 - 2 * lum)                                              # 대비 올리기
    # 하프톤(중간 밝기 영역에 점무늬)
    cell = 6; gx = (np.arange(W) % cell - cell / 2) ; gy = (np.arange(H) % cell - cell / 2); GX, GY = np.meshgrid(gx, gy)
    dots = np.hypot(GX, GY) < (lum * cell * 0.75); mid = (lum > .15) & (lum < .6); lum = np.where(mid, np.where(dots, lum + .15, lum - .08), lum)
    # 실루엣(검정) + 가장자리 빛
    if W_ is not None: d, _, hole, _ = whale(X, Y, *W_[:3], W_[3], W_[4]); wd = np.full_like(X, 1.0)
    elif C_ is not None:
        d, P = cube(X, Y, C_[0], C_[1], C_[2], C_[3], C_[4], subject); wd = np.full_like(X, 1.0)
        if subject == "block" and G_ is None: d = np.minimum(d, gear(X, Y, P(.24, 0), .085 * C_[1]))   # 옆에 붙은 톱니
    else: d, wd, tip = figure(X, Y, pose, fx, 1.0)
    rim = np.clip(1 - np.abs(d) * H * 0.35, 0, 1) * np.clip(glow, 0, 1)
    lum = np.where(d < 0, 0.02 + 0.15 * rim, lum); lum = np.maximum(lum, rim * 0.8)
    lum = np.where(wd < 0, 0.9, lum)   # 무기(밝은 칼날)
    if W_ is not None:   # 고래: 눈의 흰 점 + 숨구멍 물방울(밝게) — 실루엣이어도 고래고래로 알아보게
        c0, s0, t0 = W_[:3]; tt = math.radians(t0); ex, ey = c0[0] + (.27 * math.cos(tt) - .02 * math.sin(tt)) * s0, c0[1] + (.27 * math.sin(tt) + .02 * math.cos(tt)) * s0
        lum = np.where(np.hypot(X - ex, Y - ey) < .012 * s0, 0.95, lum)
        for dx, dy, rr in ((0, .05, .018), (.03, .075, .012), (-.03, .07, .012)): lum = np.where(np.hypot(X - hole[0] - dx * s0, Y - hole[1] - dy * s0) < rr * s0, 0.9, lum)
    if C_ is not None and subject == "slime":   # 핑크큐브: 흰 눈알 + 검은 눈동자 + 흰 미소
        for sg in (-1, 1):
            e = P(sg * .052, .06); lum = np.where(np.hypot(X - e[0], Y - e[1]) < .036 * C_[1], 0.92, lum); lum = np.where(np.hypot(X - e[0] - .004, Y - e[1]) < .015 * C_[1], 0.03, lum)
        for k in range(14):
            t0, t1 = -1 + 2 * k / 14, -1 + 2 * (k + 1) / 14; a, b = P(.11 * t0, -.045 + .035 * t0 * t0), P(.11 * t1, -.045 + .035 * t1 * t1)
            pax, pay = X - a[0], Y - a[1]; bax, bay = b[0] - a[0], b[1] - a[1]; h = np.clip((pax * bax + pay * bay) / max(1e-9, bax * bax + bay * bay), 0, 1)
            lum = np.where(np.hypot(pax - bax * h, pay - bay * h) < .012 * C_[1], 0.85, lum)
    if G_ is not None:   # 날아가는 톱니(밝게, 가운데 구멍은 어둡게)
        gd = gear(X, Y, G_[0], G_[1]); lum = np.where(gd < 0, 0.9, lum); lum = np.where(np.hypot(X - G_[0][0], Y - G_[0][1]) < G_[1] * .3, 0.05, lum)
    vig = np.clip(1 - (np.hypot((X / ASP - .5) * 1.6, (Y - .5) * 1.6) - .5) * 0.9, .5, 1); lum = lum * vig   # 비네트
    lum = np.clip(lum, 0, 1); acc = np.clip(acc, 0, 1) * (lum > .3) * 0.45      # 속성색은 아주 조금
    rgb = np.stack([lum] * 3, -1) * (1 - acc[..., None] * .8) + np.array(accent)[None, None, :] / 255 * acc[..., None] * np.maximum(lum, .6)[..., None]
    return Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8))

if __name__ == "__main__":
    acc = [(242, 199, 38), (51, 115, 242), (153, 77, 230), (219, 41, 51), (77, 191, 77)]
    out = Image.new("RGB", (W * 3 + 16, H * 2 + 12), (30, 30, 30))
    for i, m in enumerate(["Slash", "Thrust", "Smash", "Rush", "Flurry"]):
        out.paste(render(m, 7 + i, acc[i]), (4 + (i % 3) * (W + 4), 4 + (i // 3) * (H + 4)))
    import os; out.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "previews", "skill_illustrations.png"))
    # 고래고래 기술 6종(SkillLibrary WHALE_*: 박치기 Rush · 빔 Beam · 점프 Leap · 지느러미 Flurry · 꼬리 Slash · 물기둥 Spout)
    WH = [("Rush", "고래박치기", (51, 115, 242)), ("Beam", "고래빔", (51, 115, 242)), ("Leap", "점프 내려찍기", (153, 77, 230)),
          ("Flurry", "지느러미 연타", (77, 191, 77)), ("Slash", "꼬리 휘두르기", (51, 115, 242)), ("Spout", "물기둥", (51, 115, 242))]
    out = Image.new("RGB", (W * 3 + 16, H * 2 + 12), (30, 30, 30))
    for i, (m, nm, ac) in enumerate(WH): out.paste(render(m, 21 + i, ac, "whale"), (4 + (i % 3) * (W + 4), 4 + (i // 3) * (H + 4)))
    out.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "previews", "whale_skill_illustrations.png"))

    # 일반 몬스터 기술 5종: 핑크큐브말랑이(말랑 박치기 Rush · 쭈욱 늘이기 Thrust · 통통 연속 Flurry) · 그린볼런(블럭 박치기 Smash · 톱니 던지기 Throw)
    MON = [("Rush", "slime", (219, 41, 51)), ("Thrust", "slime", (219, 41, 51)), ("Flurry", "slime", (242, 140, 38)), ("Smash", "block", (77, 191, 77)), ("Throw", "block", (77, 191, 77))]
    out = Image.new("RGB", (W * 3 + 16, H * 2 + 12), (30, 30, 30))
    for i, (m, sub, ac) in enumerate(MON): out.paste(render(m, 41 + i, ac, sub), (4 + (i % 3) * (W + 4), 4 + (i // 3) * (H + 4)))
    out.save(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "previews", "monster_skill_illustrations.png"))
