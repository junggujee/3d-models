"""
타이틀·저장 화면 그림(파랑 + 갈색 + 하양, 엘든링 + 중세풍). 모양은 SDF(거리장)로 그려 가장자리가 매끄러움.
  python3 tools/title_art.py  → unity/Assets/Resources/UI/Title/*.png (2배 해상도 — Unity 는 pixelsPerUnit 200)
    Crest       문장: 가죽 코인 고리(테두리 눈금 · 대각선 네 곳 푸른 보석) + 가운데 검(푸른 홈, 보석 박힌 손잡이) + 칼끝 별빛
    DividerLeft 제목 아래 구분선(왼쪽 보석에서 오른쪽으로 옅어짐) · Divider 가운데 보석 구분선(슬롯 화면)
    Marker      메뉴 선택 표시(푸른 보석) · MenuGlow 선택 막대(왼쪽에서 번지는 푸른 빛 + 하얀 밑줄)
    SlotFrame   저장 슬롯·패널 틀(9분할 20 UI = 40px: 남색 바탕, 갈색 가죽 띠, 하얀 실선, 모서리 보석)
UITheme.TitleArt(이름, 테두리) 가 불러씀. 테두리 값을 바꾸면 UITheme.TitleStyle 도 같이.
"""
import os
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Title")
SS = 3   # 슈퍼샘플

IVORY = (245, 240, 225); MIST = (174, 186, 204)
BLUE = (102, 153, 219); BLUE_B = (186, 222, 255); BLUE_D = (41, 69, 117)
BROWN = (115, 74, 41); BROWN_L = (178, 130, 82); BROWN_D = (62, 38, 20); NAVY = (13, 20, 36)


class Canvas:
    """최종 px 좌표(왼쪽 위 원점, y 아래로)로 그리고 SS 배로 슈퍼샘플."""
    def __init__(s, w, h):
        s.w, s.h = w, h; ys, xs = np.mgrid[0:h * SS, 0:w * SS].astype(np.float32)
        s.X = (xs + 0.5) / SS; s.Y = (ys + 0.5) / SS; s.px = 1.0
        s.rgb = np.zeros((h * SS, w * SS, 3), np.float32); s.a = np.zeros((h * SS, w * SS), np.float32)

    def cov(s, d, soft=0.0):
        """SDF → 덮임(0~1). soft = 부드러운 가장자리 폭(px)."""
        k = max(0.75, soft); return np.clip(0.5 - d / k, 0, 1)

    def paint(s, d, color, alpha=1.0, soft=0.0):
        a = s.cov(d, soft) * alpha
        col = np.asarray(color, np.float32) / 255 if not callable(color) else color()
        col = np.broadcast_to(col, s.rgb.shape) if col.ndim == 1 else col
        s.rgb = s.rgb * (1 - a[..., None]) + col * a[..., None]; s.a = s.a + a * (1 - s.a)
        # rgb 는 'over' 합성(앞색이 덮음) — 반투명 바탕 위 색도 자연스럽게

    def glow(s, d, color, radius, alpha):
        """도형 바깥으로 번지는 빛(거리에 따라 지수 감쇠) — 뒤에 먼저 칠할 것."""
        a = np.exp(-np.maximum(d, 0) / radius) * alpha * (d > -1e9)
        col = np.asarray(color, np.float32) / 255
        s.rgb = s.rgb * (1 - a[..., None]) + col * a[..., None]; s.a = s.a + a * (1 - s.a)

    def image(s):
        h, w = s.h, s.w; a = s.a.reshape(h, SS, w, SS).mean((1, 3)); rgb = (s.rgb * s.a[..., None]).reshape(h, SS, w, SS, 3).mean((1, 3))
        rgb = np.where(a[..., None] > 1e-4, rgb / np.maximum(a[..., None], 1e-4), 0)
        return Image.fromarray(np.dstack([np.clip(rgb, 0, 1) * 255, np.clip(a, 0, 1) * 255]).astype(np.uint8), "RGBA")


# ── SDF 도형(px) ──────────────────────────────
def circle(c, x, y, r): return np.hypot(c.X - x, c.Y - y) - r
def ring(c, x, y, r, w): return np.abs(np.hypot(c.X - x, c.Y - y) - r) - w / 2
def diamond(c, x, y, rx, ry=None): ry = ry or rx; return (np.abs(c.X - x) / rx + np.abs(c.Y - y) / ry - 1) * min(rx, ry) * 0.7
def box(c, x, y, hw, hh, r=0):
    qx, qy = np.abs(c.X - x) - hw + r, np.abs(c.Y - y) - hh + r
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
def seg(c, x0, y0, x1, y1, w):
    px, py, dx, dy = c.X - x0, c.Y - y0, x1 - x0, y1 - y0; t = np.clip((px * dx + py * dy) / (dx * dx + dy * dy), 0, 1)
    return np.hypot(px - dx * t, py - dy * t) - w / 2
def poly(c, pts):
    """볼록 다각형(시계 방향 아무거나) — 각 변의 반평면 최대값(근사 SDF)."""
    d = None; n = len(pts); cx = sum(p[0] for p in pts) / n; cy = sum(p[1] for p in pts) / n
    for i in range(n):
        (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % n]; nx, ny = y1 - y0, -(x1 - x0); L = np.hypot(nx, ny); nx, ny = nx / L, ny / L
        if (cx - x0) * nx + (cy - y0) * ny > 0: nx, ny = -nx, -ny
        e = (c.X - x0) * nx + (c.Y - y0) * ny; d = e if d is None else np.maximum(d, e)
    return d
def star4(c, x, y, r, w):
    a = np.abs(c.X - x); b = np.abs(c.Y - y); return np.minimum(a / w + b / r, a / r + b / w) * w - w


def gem(c, x, y, r, rim=IVORY):
    """푸른 보석: 하얀 테 + 위쪽 밝고 아래쪽 짙은 두 면 + 반짝임."""
    c.paint(diamond(c, x, y, r + 2.2), rim)
    c.paint(diamond(c, x, y, r), BLUE_D)
    c.paint(np.maximum(diamond(c, x, y, r), c.Y - y), BLUE)
    c.paint(np.maximum(diamond(c, x, y, r * 0.55), c.Y - y + r * 0.1), BLUE_B, 0.9)
    c.paint(circle(c, x - r * 0.25, y - r * 0.35, max(1.0, r * 0.12)), (255, 255, 255), 0.9)


def crest():
    S = 352; c = Canvas(S, S); cx = cy = S / 2
    c.glow(circle(c, cx, cy, 150), BLUE, 14, 0.35)
    # 코인 고리: 갈색 가죽(위가 밝음) + 하얀 실선 + 눈금 + 네 방향 보석
    shade = lambda: np.dstack([np.interp(c.Y, [20, 330], [BROWN_L[k] / 255, BROWN[k] / 255]) for k in range(3)])
    c.paint(circle(c, cx, cy, 150), shade)
    c.paint(circle(c, cx, cy, 118), NAVY)
    c.paint(circle(c, cx, cy, 118), lambda: np.dstack([np.interp(np.hypot(c.X - cx, c.Y - cy), [0, 118], [BLUE_D[k] / 255 * 0.8, NAVY[k] / 255]) for k in range(3)]))
    for r, w, a in ((148.5, 2.4, 1), (121, 2.0, 1), (113, 1.0, .5), (140, 1.0, .45)): c.paint(ring(c, cx, cy, r, w), IVORY, a)
    for i in range(48):
        if i % 12 == 6: continue
        t = i / 48 * 2 * np.pi; c.paint(seg(c, cx + np.sin(t) * 126, cy - np.cos(t) * 126, cx + np.sin(t) * 135, cy - np.cos(t) * 135, 1.8), IVORY, 0.6)
    for i in range(4):   # 대각선 네 곳(위·아래는 칼끝·손잡이와 겹치지 않게)
        t = (i + 0.5) / 4 * 2 * np.pi; gem(c, cx + np.sin(t) * 131, cy - np.cos(t) * 131, 9)
    # 검: 칼날(하얀 강철, 왼쪽 면 밝게) + 푸른 홈 + 손잡이·보석
    tip, base = 52, 246
    blade = poly(c, [(cx, tip), (cx + 13, tip + 34), (cx + 12, base), (cx - 12, base), (cx - 13, tip + 34)])
    c.glow(blade, BLUE_B, 6, 0.35)
    c.paint(blade + 2.2, BROWN_D); c.paint(blade, (214, 220, 230))
    c.paint(np.maximum(blade, c.X - cx), IVORY)
    c.paint(seg(c, cx, tip + 40, cx, base - 6, 4.2), BLUE, 0.95); c.paint(seg(c, cx, tip + 44, cx, base - 10, 1.4), BLUE_B, 0.9)
    guard = box(c, cx, 252, 62, 7.5, 6)
    c.paint(guard + 2.2, IVORY); c.paint(guard, BROWN_L); c.paint(np.maximum(guard, c.Y - 250), (205, 160, 110))
    for sx in (-1, 1): c.paint(ring(c, cx + sx * 66, 246, 7, 3), IVORY); c.paint(circle(c, cx + sx * 66, 246, 4), BLUE)
    grip = box(c, cx, 282, 8, 22, 3); c.paint(grip + 2, IVORY); c.paint(grip, BROWN)
    for y in range(266, 300, 6): c.paint(np.maximum(seg(c, cx - 8, y, cx + 8, y + 4, 1.6), grip), BROWN_L, 0.9)
    c.paint(circle(c, cx, 310, 11.5), IVORY); c.paint(circle(c, cx, 310, 8), BLUE_D); c.paint(circle(c, cx, 308, 4.5), BLUE_B)
    gem(c, cx, 252, 12)
    # 칼끝 별빛
    c.glow(circle(c, cx, tip, 3), BLUE_B, 9, 0.7); c.paint(star4(c, cx, tip, 22, 2.4), (255, 255, 255)); c.paint(star4(c, cx, tip, 11, 2.0), BLUE_B, .8)
    return c.image()


def divider(left):
    W, H = (1280, 52); c = Canvas(W, H); y = H / 2
    if left:
        x0 = 40; fade = lambda a0: np.clip(1 - (c.X - x0) / (W - x0), 0, 1) ** 1.3
        c.paint(seg(c, x0 + 26, y, W - 4, y, 2.6), IVORY, 1)
        # 선을 오른쪽으로 옅게(알파를 곱함)
        m = c.X > x0 + 20; c.a = np.where(m, c.a * fade(1), c.a)
        c.paint(seg(c, x0 + 30, y + 7, x0 + 300, y + 7, 1.4), BROWN_L, 1.0); m = (c.Y > y + 4); c.a = np.where(m & (c.X > x0 + 30), c.a * np.clip(1 - (c.X - x0 - 30) / 270, 0, 1), c.a)
        for k, dx in enumerate((22, 36)): c.paint(diamond(c, x0 + 20 + dx + 14 * k, y, 3), BROWN_L)
        gem(c, x0, y, 15)
    else:
        cx = W / 2
        c.paint(seg(c, 30, y, W - 30, y, 2.4), IVORY); c.a *= np.clip(1 - np.abs(c.X - cx) / (cx - 20), 0, 1) ** 0.8
        for sx in (-1, 1):
            c.paint(seg(c, cx + sx * 30, y + 7, cx + sx * 260, y + 7, 1.3), BROWN_L, .9); c.paint(seg(c, cx + sx * 30, y - 7, cx + sx * 260, y - 7, 1.3), BROWN_L, .9)
            c.paint(diamond(c, cx + sx * 40, y, 4), BROWN_L); c.paint(diamond(c, cx + sx * 54, y, 3), IVORY)
        gem(c, cx, y, 15)
    return c.image()


def marker():
    c = Canvas(60, 60); c.glow(diamond(c, 30, 30, 18), BLUE, 5, .55); gem(c, 30, 30, 18); return c.image()


def menu_glow():
    W, H = 1520, 128; c = Canvas(W, H)
    u = c.X / W; v = np.abs(c.Y - H / 2) / (H / 2)
    a = np.clip(1 - u, 0, 1) ** 1.6 * np.clip(1 - v, 0, 1) ** 1.2 * 0.42
    col = np.dstack([np.interp(u, [0, 1], [BLUE[k] / 255 * 1.1, BLUE_D[k] / 255]) for k in range(3)])
    c.rgb = col; c.a = a
    c.paint(seg(c, 10, H - 10, W * 0.8, H - 10, 2.6), IVORY, 1.0); c.a = np.where(c.Y > H - 16, np.maximum(a, c.a * np.clip(1 - u / 0.8, 0, 1) ** 1.4), c.a)
    c.paint(box(c, 4, H / 2, 3, H / 2 - 14, 2), BROWN_L, 0.9)
    return c.image()


def slot_frame():
    S = 192; c = Canvas(S, S)
    c.paint(box(c, S / 2, S / 2, S / 2 - 1, S / 2 - 1, 10), NAVY, 0.92)
    band = np.maximum(box(c, S / 2, S / 2, S / 2 - 3, S / 2 - 3, 9), -box(c, S / 2, S / 2, S / 2 - 15, S / 2 - 15, 4))
    c.paint(band, lambda: np.dstack([np.interp(c.Y, [0, S], [BROWN_L[k] / 255, BROWN[k] / 255]) for k in range(3)]))
    for inset, w, a in ((3.5, 2.0, 1), (15, 1.6, 1), (21, 1.0, .45)): c.paint(np.abs(box(c, S / 2, S / 2, S / 2 - inset, S / 2 - inset, max(2, 10 - inset * .5))) - w / 2, IVORY, a)
    # 가죽 띠 바느질 점
    for t in range(24, S - 24, 8):
        for (x, y) in ((t, 9), (t, S - 9), (9, t), (S - 9, t)): c.paint(circle(c, x, y, 1.1), IVORY, .55)
    for x in (20, S - 20):
        for y in (20, S - 20): gem(c, x, y, 8)
    return c.image()


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    arts = {"Crest": crest(), "DividerLeft": divider(True), "Divider": divider(False), "Marker": marker(), "MenuGlow": menu_glow(), "SlotFrame": slot_frame()}
    for k, im in arts.items(): im.save(os.path.join(OUT, k + ".png")); print("✔", k, im.size)
