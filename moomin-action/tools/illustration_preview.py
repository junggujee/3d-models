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

def render(motif, seed, accent):
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
    if motif == "Slash":
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
    # 인물 실루엣(검정) + 가장자리 빛
    d, wd, tip = figure(X, Y, pose, fx, 1.0); rim = np.clip(1 - np.abs(d) * H * 0.35, 0, 1) * np.clip(glow, 0, 1)
    lum = np.where(d < 0, 0.02 + 0.15 * rim, lum); lum = np.maximum(lum, rim * 0.8)
    lum = np.where(wd < 0, 0.9, lum)   # 무기(밝은 칼날)
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
