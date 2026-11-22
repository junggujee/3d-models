"""
보스바 그림: 넓은 쇠 틀(금 이중선 · 리벳 · 위아래 작은 물결 눈금) · 오른쪽 끝 물결 장식 · 보스 문장(고래고래 / 기본).
  python3 tools/boss_bar_art.py  →  unity/Assets/Resources/UI/Bars/Boss*.png  (UI/BossBar.cs 가 불러 씀, 틀 9분할 테두리 26px)
"""
import os, math
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Bars")
def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)
def grid(w, h): ys, xs = np.mgrid[0:h, 0:w].astype(np.float32); return xs + .5, ys + .5
def rrect(X, Y, x0, y0, x1, y1, r):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2; hx, hy = (x1 - x0) / 2 - r, (y1 - y0) / 2 - r
    qx, qy = np.abs(X - cx) - hx, np.abs(Y - cy) - hy
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
def cov(d, s=1.0): return np.clip(.5 - d / s, 0, 1)
class L:
    def __init__(s, w, h): s.w, s.h = w, h; s.rgb = np.zeros((h, w, 3), np.float32); s.a = np.zeros((h, w), np.float32)
    def put(s, c, col, al=1.0):
        c = np.clip(c * al, 0, 1); col = col if np.ndim(col) == 3 else np.broadcast_to(col, (s.h, s.w, 3)); s.rgb = s.rgb * (1 - c[..., None]) + col * c[..., None]; s.a = s.a + c * (1 - s.a)
    def cut(s, c): s.a = s.a * (1 - np.clip(c, 0, 1))
    def save(s, n): os.makedirs(OUT, exist_ok=True); Image.fromarray((np.dstack([np.clip(s.rgb, 0, 1), np.clip(s.a, 0, 1)]) * 255).astype(np.uint8), "RGBA").save(os.path.join(OUT, n + ".png")); print("✔", n)

def frame():
    W, H = 256, 72; X, Y = grid(W, H); l = L(W, H)
    o = rrect(X, Y, 1, 1, W - 1, H - 1, 10); b = rrect(X, Y, 3, 3, W - 3, H - 3, 8)
    l.put(cov(o), hexc('#0A0705')); l.put(cov(b), hexc('#24201C')[None, None] * np.clip(1.25 - (Y / H)[..., None] * .6, .6, 1.25))
    for inset, c, w in ((9, '#D8B25E', 1.5), (14, '#7A5A26', .8)):
        l.put(cov(np.abs(rrect(X, Y, inset, inset, W - inset, H - inset, 5)) - w), hexc(c))
    for x in range(30, W - 30, 16):                                            # 위아래 작은 물결 눈금(가운데 늘어나도 반복 무늬처럼 보이게 가늘게)
        l.put(cov(np.hypot(X - x, Y - 5.2) - 1.2), hexc('#B8924F'), .8); l.put(cov(np.hypot(X - x, Y - (H - 5.2)) - 1.2), hexc('#B8924F'), .8)
    for cx, cy in ((12, 12), (W - 12, 12), (12, H - 12), (W - 12, H - 12)):
        d = np.hypot(X - cx, Y - cy) - 3.6; l.put(cov(d - 1.2), hexc('#0A0705')); l.put(cov(d), hexc('#C9A35A')); l.put(cov(np.hypot(X - cx + 1, Y - cy + 1) - 1.2), hexc('#F6E2A8'))
    l.cut(cov(rrect(X, Y, 16, 16, W - 16, H - 16, 4))); l.save("BossFrame")

def cap():
    """오른쪽 끝 장식: 금빛 물결 곡선이 말려 올라가 지느러미 촉으로 끝나고, 물방울 셋."""
    W, H = 128, 160; X, Y = grid(W, H); l = L(W, H)
    t = np.clip((X - 4) / 90, 0, 1); cy = 80 - 26 * np.sin(t * math.pi * 1.5) * t     # 오른쪽으로 갈수록 크게 출렁
    wave = np.abs(Y - cy) - (3 + 3.5 * t)
    spiral_c = (34, 80); r = np.hypot(X - spiral_c[0], Y - spiral_c[1]); a = np.arctan2(Y - spiral_c[1], X - spiral_c[0])
    spiral = np.abs(r - (6 + 8 * ((a + math.pi) / (2 * math.pi)))) - 2.4
    tip = np.maximum(np.abs(Y - 106) - (16 - (X - 92) * 0.5), np.maximum(90 - X, X - 124))  # 물결 끝(y 106)에서 오른쪽으로 뾰족한 지느러미 촉
    drops = np.minimum.reduce([np.hypot(X - 84, Y - 78) - 6, np.hypot(X - 96, Y - 62) - 4.5, np.hypot(X - 104, Y - 49) - 3.4])   # 위로 올라가는 물방울
    body = np.minimum.reduce([wave, spiral, tip, drops])
    l.put(cov(body - 2.2), hexc('#0A0705')); l.put(cov(body), hexc('#D8B25E')); l.put(cov(body + 1.4) * (Y < cy), hexc('#F6E2A8'), .55)
    l.save("BossCap")

def emblem(name, draw):
    W = H = 256; X, Y = grid(W, H); l = L(W, H); r = np.hypot(X - 128, Y - 128)
    l.put(cov(r - 124), hexc('#0A0705')); l.put(cov(r - 118), hexc('#13213F')[None, None] * np.clip(1.3 - (Y / H)[..., None] * .7, .6, 1.3))
    l.put(cov(np.abs(r - 110) - 4.5), hexc('#D8B25E')); l.put(cov(np.abs(r - 98) - 1.5), hexc('#8A6A35'))
    for k in range(12):                                                           # 금테 바깥 톱니
        ang = k * math.pi / 6; px, py = 128 + math.cos(ang) * 118, 128 + math.sin(ang) * 118
        l.put(cov(np.hypot(X - px, Y - py) - 5), hexc('#D8B25E'))
    draw(l, X, Y); l.save("BossEmblem_" + name)

def whale(l, X, Y):
    """옆모습 고래고래: 길쭉한 몸 · 볼록한 이마 · 튀어나온 주둥이(분홍 끝) · 등지느러미 · 갈라진 꼬리 · 숨구멍 물방울."""
    body = np.hypot((X - 120) / 62, (Y - 140) / 30) - 1
    melon = np.hypot((X - 165) / 30, (Y - 132) / 27) - 1
    snout = np.hypot((X - 200) / 26, (Y - 146) / 10) - 1
    tail = np.where((X > 50) & (X < 90), np.abs(Y - 140 + (90 - X) * 0.35) - (6 + (X - 50) * 0.45), 9)
    fl1 = np.hypot((X - 42) / 18, (Y - 112) / 8) - 1; fl2 = np.hypot((X - 42) / 18, (Y - 128) / 7) - 1
    dorsal = np.hypot((X - 128) / 12, (Y - 106) / 14) - 1
    s = np.minimum.reduce([body * 20, melon * 20, snout * 8, tail, fl1 * 8, fl2 * 8, dorsal * 10])
    l.put(cov(s + 2), hexc('#0A1430')); l.put(cov(s), hexc('#3B6FD6')[None, None] * np.clip(1.25 - ((Y - 100) / 80)[..., None] * .5, .7, 1.3))
    belly = np.maximum(s, -(Y - 150)); l.put(cov(belly), hexc('#E8EEF5'))
    l.put(cov(np.hypot((X - 222) / 7, (Y - 150) / 5) - 1), hexc('#F28A9A'))                # 분홍 주둥이 끝
    l.put(cov(np.hypot(X - 176, Y - 136) - 3.6), hexc('#0A0A10')); l.put(cov(np.hypot(X - 177, Y - 135) - 1.2), hexc('#FFFFFF'))
    for dx, dy, rr in ((0, 0, 7), (10, 8, 5), (-9, 7, 5), (4, -12, 4)):                  # 물방울
        l.put(cov(np.hypot(X - 160 - dx, Y - 88 - dy) - rr), hexc('#A8DAFF'))

def default(l, X, Y):
    """일반 보스: 금빛 뿔 달린 가면."""
    mask = np.hypot((X - 128) / 46, (Y - 140) / 54) - 1
    horns = np.minimum(np.hypot((X - 92) / 12, (Y - 92) / 34) - 1, np.hypot((X - 164) / 12, (Y - 92) / 34) - 1)
    s = np.minimum(mask * 40, horns * 14); l.put(cov(s + 2), hexc('#0A0705')); l.put(cov(s), hexc('#C9A35A'))
    for ex in (108, 148): l.put(cov(np.hypot((X - ex) / 12, (Y - 132) / 7) - 1), hexc('#4A0E0E'))

if __name__ == "__main__":
    frame(); cap(); emblem("Whale", whale); emblem("Default", default)
