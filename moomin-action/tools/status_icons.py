"""
상태이상 아이콘 5종(화상 · 에너지 · 출혈 · 진동 · 침잠) — 검은 배경, 절제된 림버스풍(평면 채색 + 단단한 하이라이트 + 은은한 빛 번짐).
모양은 모두 SDF(거리장)로 그려 가장자리가 매끄럽고, 크기를 바꿔도 다시 만들 수 있음.

  python3 tools/status_icons.py            → unity/Assets/Resources/StatusIcons/*.png (투명 배경, 게임용)
                                             previews/status_icons.png (검은 배경 미리보기)
"""
import os, sys
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
N = 1024          # 작업 해상도(4배 슈퍼샘플)
OUT = 256         # 게임용 크기
SPAN = 1.3        # 캔버스 반폭(도형은 -1..1 안, 바깥은 빛 번짐 여백)
PX = 2.0 * SPAN / N

ys, xs = np.mgrid[0:N, 0:N].astype(np.float32)
X = ((xs + 0.5) / N * 2 - 1) * SPAN
Y = (1 - (ys + 0.5) / N * 2) * SPAN          # 위가 +


# ── SDF 기본 도형 ─────────────────────────────
def circle(cx, cy, r): return np.hypot(X - cx, Y - cy) - r
def ellipse(cx, cy, rx, ry):
    k = np.hypot((X - cx) / rx, (Y - cy) / ry)
    return (k - 1) * min(rx, ry)
def union(*d): return np.minimum.reduce(d)
def inter(a, b): return np.maximum(a, b)
def sub(a, b): return np.maximum(a, -b)
def smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b * (1 - h) + a * h - k * h * (1 - h)
def offset(d, r): return d - r

def bezier(p0, p1, p2, t):
    t = np.asarray(t)[:, None]
    return (1 - t) ** 2 * np.array(p0) + 2 * (1 - t) * t * np.array(p1) + t ** 2 * np.array(p2)

def tongue(p0, ctrl, p1, r0, r1=0.0, n=48, ease=1.0):
    """굵기가 r0→r1 로 줄어드는 휜 획(불꽃 혀·핏방울 꼬리)."""
    ts = np.linspace(0, 1, n); pts = bezier(p0, ctrl, p1, ts)
    d = np.full(X.shape, 9.0, np.float32)
    for t, (px, py) in zip(ts, pts):
        r = r0 + (r1 - r0) * t ** ease
        d = np.minimum(d, np.hypot(X - px, Y - py) - r)
    return d

def polygon(pts):
    """다각형 SDF(안쪽 음수)."""
    pts = np.asarray(pts, np.float32); d = np.full(X.shape, 1e9, np.float32); s = np.ones(X.shape, np.float32)
    for i in range(len(pts)):
        a, b = pts[i], pts[i - 1]; e = b - a; wx, wy = X - a[0], Y - a[1]
        h = np.clip((wx * e[0] + wy * e[1]) / (e @ e), 0, 1)
        d = np.minimum(d, (wx - e[0] * h) ** 2 + (wy - e[1] * h) ** 2)
        c1 = Y >= a[1]; c2 = Y < b[1]; c3 = e[0] * wy > e[1] * wx
        flip = (c1 & c2 & c3) | (~c1 & ~c2 & ~c3); s[flip] *= -1
    return s * np.sqrt(d)

def arc(r, w, half_angle, side):
    """좌(side=-1)/우(+1) 공명 파동 호: 양 끝으로 갈수록 가늘어짐."""
    ang = np.arctan2(Y, X * side)                 # 0 = 바깥 수평 방향
    t = np.clip(np.abs(ang) / half_angle, 0, 1.5)
    width = w * np.clip(1 - t ** 2, 0, 1)
    d = np.abs(np.hypot(X, Y) - r) - width
    return np.where((np.abs(ang) <= half_angle) & (X * side > 0), d, np.maximum(d, 0.02 + (np.abs(ang) - half_angle) * r))


# ── 채색 ─────────────────────────────────────
def cov(d): return np.clip(0.5 - d / (PX * 1.2), 0, 1)       # 경계 부드럽게(안티앨리어싱)
def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)
def vgrad(top, bottom, y0=1.0, y1=-1.0):
    t = np.clip((Y - y1) / (y0 - y1), 0, 1)[..., None]
    return hexc(bottom) * (1 - t) + hexc(top) * t

class Canvas:
    def __init__(self): self.rgb = np.zeros((N, N, 3), np.float32); self.a = np.zeros((N, N), np.float32)
    def fill(self, d, color, alpha=1.0):
        c = cov(d) * alpha
        col = color if np.ndim(color) == 3 else np.broadcast_to(hexc(color) if isinstance(color, str) else color, (N, N, 3))
        self.rgb = self.rgb * (1 - c[..., None]) + col * c[..., None]; self.a = self.a + c * (1 - self.a)
    def image(self, glow, glow_strength=0.8, glow_radius=34):
        """투명 배경 RGBA(빛 번짐 포함)."""
        alpha = Image.fromarray((self.a * 255).astype(np.uint8))
        g1 = np.asarray(alpha.filter(ImageFilter.GaussianBlur(glow_radius)), np.float32) / 255
        g2 = np.asarray(alpha.filter(ImageFilter.GaussianBlur(glow_radius * 0.35)), np.float32) / 255
        ga = np.clip(g1 * glow_strength * 1.3 + g2 * glow_strength * 0.6, 0, 1) * (1 - self.a)
        gc = hexc(glow)
        a = self.a + ga
        rgb = (self.rgb * self.a[..., None] + gc * ga[..., None]) / np.maximum(a, 1e-6)[..., None]
        img = np.dstack([np.clip(rgb, 0, 1), np.clip(a, 0, 1)])
        return Image.fromarray((img * 255).astype(np.uint8), 'RGBA')


# ── 아이콘 ───────────────────────────────────
def burn():
    c = Canvas()
    outer = union(ellipse(0, -0.42, 0.5, 0.42),
                  tongue((-0.08, -0.35), (-0.22, 0.42), (0.24, 0.98), 0.36, 0.0, ease=0.8),
                  tongue((-0.28, -0.35), (-0.55, -0.05), (-0.58, 0.28), 0.24, 0.0),
                  tongue((0.28, -0.3), (0.55, -0.15), (0.6, 0.12), 0.22, 0.0),
                  tongue((-0.38, -0.55), (-0.66, -0.45), (-0.74, -0.18), 0.16, 0.0),
                  tongue((0.0, -0.6), (0.0, -0.8), (0.02, -0.97), 0.3, 0.0))
    outer = sub(outer, tongue((0.12, 0.05), (0.3, 0.2), (0.42, 0.45), 0.07, 0.0))      # 혀 사이 칼집
    outer = sub(outer, tongue((-0.22, -0.05), (-0.35, 0.1), (-0.38, 0.32), 0.06, 0.0))
    c.fill(outer, vgrad('#FF5A3C', '#E8321E'))
    c.fill(sub(offset(outer, -0.06), ellipse(0.45, 0.6, 0.5, 0.9)), '#F2442A', 0.6)      # 오른쪽 그늘 대신 밝은 면
    mid = union(ellipse(0, -0.5, 0.33, 0.3), tongue((0, -0.4), (-0.06, -0.05), (0.06, 0.22), 0.24, 0.0),
                tongue((-0.18, -0.45), (-0.36, -0.3), (-0.38, -0.08), 0.13, 0.0), tongue((0.18, -0.45), (0.34, -0.3), (0.36, -0.12), 0.12, 0.0),
                tongue((0, -0.65), (0, -0.8), (0.0, -0.9), 0.2, 0.0))
    c.fill(mid, vgrad('#FF9A3A', '#FF7A2A'))
    core = union(ellipse(0, -0.58, 0.18, 0.16), tongue((0, -0.5), (-0.04, -0.32), (0.03, -0.16), 0.13, 0.0), tongue((0, -0.7), (0, -0.78), (0, -0.85), 0.11, 0.0))
    c.fill(core, vgrad('#FFF0B8', '#FFD27A'))
    return c.image('#FF3B1F', 0.85)

def energy():
    c = Canvas()
    bolt = polygon([(0.38, 0.98), (-0.62, -0.04), (-0.02, -0.06), (-0.46, -0.98), (0.66, 0.2), (0.06, 0.2)])
    c.fill(offset(bolt, 0.012), '#22B8C8')
    c.fill(bolt, vgrad('#7FF4FF', '#3FD6E8'))
    c.fill(offset(bolt, -0.07), vgrad('#E8FEFF', '#A6F7FF'), 0.9)                  # 안쪽 밝은 심
    return c.image('#18C8E0', 0.9)

def bleed():
    c = Canvas()
    big = tongue((-0.12, -0.3), (-0.1, 0.3), (-0.06, 0.95), 0.46, 0.0, ease=0.75)
    big = union(big, circle(-0.12, -0.3, 0.46), tongue((-0.12, -0.6), (-0.14, -0.8), (-0.12, -0.92), 0.2, 0.0))
    small = union(tongue((0.56, -0.58), (0.56, -0.35), (0.6, -0.12), 0.16, 0.0, ease=0.75), circle(0.56, -0.58, 0.16))
    for d in (big, small):
        c.fill(offset(d, 0.015), '#7A0A12')
        c.fill(d, vgrad('#E0242C', '#A8101A'))
    c.fill(sub(offset(big, -0.03), tongue((-0.22, -0.2), (-0.2, 0.3), (-0.12, 0.9), 0.46, 0.0, ease=0.75)), '#8A0C15', 0.9)   # 오른쪽 아래 그늘(초승달)
    c.fill(sub(offset(small, -0.02), circle(0.5, -0.52, 0.15)), '#8A0C15', 0.9)
    c.fill(tongue((-0.36, -0.12), (-0.38, 0.12), (-0.24, 0.36), 0.06, 0.012), '#FFB0B0', 0.9)   # 하이라이트
    c.fill(ellipse(-0.36, -0.36, 0.045, 0.07), '#FFE3E3')
    c.fill(ellipse(0.48, -0.5, 0.028, 0.05), '#FFD0D0')
    return c.image('#E01020', 0.8)

def tremor():
    c = Canvas()
    for side in (-1, 1):
        for r, w, a in ((0.34, 0.05, 1.0), (0.56, 0.048, 0.95), (0.78, 0.044, 0.85)):
            d = arc(r, w, np.radians(52 - r * 12), side)
            c.fill(offset(d, 0.012), '#D9660A', a * 0.9)
            c.fill(d, vgrad('#FFD36A', '#FFA21E'), a)
    spike = (np.abs(X) / 0.075 + np.abs(Y) / 0.98 - 1) * 0.07
    c.fill(offset(spike, 0.012), '#D9660A'); c.fill(spike, vgrad('#FFE7A6', '#FFB52E'))
    orb = circle(0, 0, 0.15); c.fill(offset(orb, 0.012), '#D9660A'); c.fill(orb, '#FFB52E')
    c.fill(circle(0, 0, 0.09), '#FFE9B0')
    bar = (np.abs(X) / 0.36 + np.abs(Y) / 0.05 - 1) * 0.05; c.fill(bar, '#FFD06A', 0.9)
    c.fill((np.abs(X) / 0.03 + np.abs(Y) / 0.55 - 1) * 0.03, '#FFF6D8', 0.9)          # 심 하이라이트
    return c.image('#FF8C1A', 0.9)

def sinking():
    """위: 한데 뭉친 무거운 구체(겹겹이 짙어지는 핵) · 아래: 액체처럼 흘러내리는 줄기와 떨어지는 방울. 좌우 대칭의 독립 상징."""
    c = Canvas()
    mass = ellipse(0, 0.5, 0.56, 0.44)
    drips = [(0.0, -0.74, 0.11), (-0.3, -0.46, 0.09), (0.3, -0.46, 0.09), (-0.5, -0.14, 0.07), (0.5, -0.14, 0.07)]
    body = mass
    for x, yend, r in drips:
        stem = tongue((x, 0.3), (x, (0.3 + yend) / 2), (x, yend), r * 1.15, r * 0.45, ease=0.6)   # 위는 굵고 아래로 가늘어짐
        bulb = circle(x, yend - r * 0.25, r)                                                      # 끝에 맺힌 방울
        body = smin(body, union(stem, bulb), 0.14)
    drop = union(circle(0, -1.04, 0.065), tongue((0, -1.04), (0, -0.97), (0, -0.9), 0.065, 0.0))
    c.fill(offset(body, 0.016), '#060B26'); c.fill(offset(drop, 0.014), '#060B26')
    c.fill(body, vgrad('#3047B4', '#182472', 0.9, -0.75)); c.fill(drop, '#22339A')
    # 응집: 안쪽으로 갈수록 짙어지는 겹(눈동자처럼 보이지 않게 대비는 낮게)
    for k, col in ((0.78, '#22339A'), (0.56, '#1A2880'), (0.34, '#121C62')):
        c.fill(ellipse(0, 0.47, 0.56 * k, 0.44 * k), col, 0.9)
    # 위쪽 가장자리 빛(위로 갈수록 강하게) + 하이라이트
    band = np.abs(offset(mass, -0.035)) - 0.018
    c.fill(inter(band, -mass), '#4F6BE0', np.clip((Y - 0.35) / 0.5, 0, 1) * 0.95)
    c.fill(tongue((-0.4, 0.6), (-0.34, 0.8), (-0.14, 0.86), 0.035, 0.012), '#A9B8FF', 0.9)
    for x, yend, r in drips[:3]:
        c.fill(ellipse(x - r * 0.38, yend - r * 0.1, r * 0.2, r * 0.3), '#5E77EE', 0.85)
    c.fill(ellipse(-0.022, -1.05, 0.02, 0.03), '#6E86F5', 0.85)
    return c.image('#2A40D0', 1.0)


ICONS = [("Burn", burn), ("Energy", energy), ("Bleed", bleed), ("Tremor", tremor), ("Sinking", sinking)]

def main():
    out_dir = os.path.join(ROOT, "unity", "Assets", "Resources", "StatusIcons"); os.makedirs(out_dir, exist_ok=True)
    sheet = Image.new("RGB", (OUT * len(ICONS) + 40 * (len(ICONS) + 1), OUT + 80), (0, 0, 0))
    only = set(sys.argv[1:])
    for i, (name, fn) in enumerate(ICONS):
        if only and name not in only: continue
        img = fn().resize((OUT, OUT), Image.LANCZOS)
        img.save(os.path.join(out_dir, name + ".png"))
        sheet.paste(img, (40 + i * (OUT + 40), 40), img)
        print("✔", name)
    os.makedirs(os.path.join(ROOT, "previews"), exist_ok=True)
    sheet.save(os.path.join(ROOT, "previews", "status_icons.png"))

if __name__ == "__main__":
    main()
