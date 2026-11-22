"""
체력바 · 경험치바 그림(중세 판타지 HUD): 어두운 나무+쇠 틀 · 금색 안쪽 테 · 리벳 · 꼰 끈 장식 · 광택 채움 · 퍼센트 꼬리표 · 레벨 원판.
  python3 tools/bar_art.py   →  unity/Assets/Resources/UI/Bars/*.png (게임이 Resources.Load 로 불러 9분할 스프라이트로 씀)
Unity 쪽 9분할 테두리 값은 UI/BarArt.cs 의 BORDER 표와 같아야 함.
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Bars")

def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)
def grid(w, h): ys, xs = np.mgrid[0:h, 0:w].astype(np.float32); return xs + .5, ys + .5
def rrect(X, Y, x0, y0, x1, y1, r):
    """둥근 사각형 SDF(px, 안쪽 음수)."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2; hx, hy = (x1 - x0) / 2 - r, (y1 - y0) / 2 - r
    qx, qy = np.abs(X - cx) - hx, np.abs(Y - cy) - hy
    return np.hypot(np.maximum(qx, 0), np.maximum(qy, 0)) + np.minimum(np.maximum(qx, qy), 0) - r
def cov(d, soft=1.0): return np.clip(.5 - d / soft, 0, 1)
def hash2(x, y):
    x = x.astype(np.int64); y = y.astype(np.int64); h = (x * 374761393 + y * 668265263) & 0xffffffff
    h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff; return ((h ^ (h >> 16)) & 0xffff) / 65535.0
def vnoise(x, y):
    xi, yi = np.floor(x), np.floor(y); tx, ty = x - xi, y - yi; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty)
    a = hash2(xi, yi) + (hash2(xi + 1, yi) - hash2(xi, yi)) * tx; b = hash2(xi, yi + 1) + (hash2(xi + 1, yi + 1) - hash2(xi, yi + 1)) * tx
    return a + (b - a) * ty

class Layer:
    def __init__(self, w, h): self.w, self.h = w, h; self.rgb = np.zeros((h, w, 3), np.float32); self.a = np.zeros((h, w), np.float32)
    def put(self, c, color, alpha=1.0):
        c = np.clip(c * alpha, 0, 1); col = color if np.ndim(color) == 3 else np.broadcast_to(color, (self.h, self.w, 3))
        self.rgb = self.rgb * (1 - c[..., None]) + col * c[..., None]; self.a = self.a + c * (1 - self.a)
    def cut(self, c): self.a = self.a * (1 - np.clip(c, 0, 1))
    def save(self, name):
        img = np.dstack([np.clip(self.rgb, 0, 1), np.clip(self.a, 0, 1)]); os.makedirs(OUT, exist_ok=True)
        Image.fromarray((img * 255).astype(np.uint8), "RGBA").save(os.path.join(OUT, name + ".png")); print("✔", name)

def frame():
    """9분할 틀 192×96, 테두리 28px: 바깥 검은 선 · 나뭇결 몸체 · 위쪽 빛 · 금색 안쪽 테 · 모서리 리벳 · 가운데는 비움."""
    W, H = 192, 96; X, Y = grid(W, H); L = Layer(W, H)
    outer = rrect(X, Y, 1, 1, W - 1, H - 1, 16); body = rrect(X, Y, 3, 3, W - 3, H - 3, 14)
    L.put(cov(outer), hexc('#0E0905'))
    grain = vnoise(X * .06, Y * .9) * .6 + vnoise(X * .015, Y * .25) * .4
    wood = hexc('#2E2015')[None, None] * (1 - grain[..., None] * .35) + hexc('#5A4129')[None, None] * (grain[..., None] * .35)
    shade = np.clip(1.15 - (Y / H) * .5, .6, 1.15)[..., None]                     # 위가 밝고 아래가 어두움
    L.put(cov(body), wood * shade)
    L.put(cov(np.abs(body + 2) - .9) * (Y < H * .5), hexc('#8A6A45'), .8)      # 위쪽 테두리 빛
    trim = rrect(X, Y, 11, 11, W - 11, H - 11, 7)
    L.put(cov(np.abs(trim) - 1.6), hexc('#D8B25E'))                              # 금색 안쪽 테
    L.put(cov(np.abs(trim + 2.4) - .7), hexc('#6B4A1C'), .9)                     # 테 안쪽 그림자
    for cx, cy in ((14, 14), (W - 14, 14), (14, H - 14), (W - 14, H - 14)):    # 리벳
        d = np.hypot(X - cx, Y - cy) - 4.6
        L.put(cov(d + -1.2 * 0 + 1.2), hexc('#120C08')); L.put(cov(d), hexc('#B8924F')); L.put(cov(np.hypot(X - cx + 1.3, Y - cy + 1.3) - 1.6), hexc('#F6E2A8'))
    L.cut(cov(rrect(X, Y, 13, 13, W - 13, H - 13, 6)))                          # 가운데 창(채움이 보이도록)
    L.save("Frame")

def fill():
    """채움(흰색 → Image.color 로 색): 위쪽 광택 띠 · 가운데 · 아래로 어두워짐."""
    W, H = 16, 64; X, Y = grid(W, H); t = Y / H; L = Layer(W, H)
    lum = np.where(t < .12, 1.0, np.where(t < .45, .93 - (t - .12) * .1, .9 - (t - .45) * .45))
    lum = lum + np.exp(-((t - .2) / .035) ** 2) * .12
    L.put(np.ones((H, W), np.float32), np.repeat(lum[..., None], 3, 2)); L.save("Fill")

def back():
    """빈 칸 바탕(어두운 홈)."""
    W, H = 16, 64; X, Y = grid(W, H); t = Y / H; L = Layer(W, H)
    L.put(np.ones((H, W), np.float32), hexc('#0B0F0A')[None, None] * (1.25 - t[..., None] * .5)); L.save("Back")

def binding():
    """꼰 끈 감개 48×120: 비스듬한 가닥이 엇갈리고 원통처럼 양옆이 어두움, 위아래 끝은 묶어 맨 매듭."""
    W, H = 48, 120; X, Y = grid(W, H); L = Layer(W, H)
    body = rrect(X, Y, 6, 8, W - 6, H - 8, 9)
    u = (X - 6) / (W - 12); cyl = np.clip(np.sin(np.pi * np.clip(u, 0, 1)), 0, 1) ** .6
    phase = (Y * .085 + X * .11) % 1.0; strand = np.where(phase < .5, 0, 1)
    gap = np.exp(-((np.minimum(phase, 1 - phase) * 2 - 0) / .12) ** 2) + np.exp(-(((phase - .5) * 2) / .12) ** 2)
    rope = np.where(strand[..., None] == 0, hexc('#D2A869')[None, None], hexc('#8A6436')[None, None])
    rope = rope * (.5 + .6 * cyl[..., None]) * (1 - np.clip(gap, 0, 1)[..., None] * .8)
    L.put(cov(body + 1.5), hexc('#160E07')); L.put(cov(body), rope)
    for yy in (10, H - 10):                                                     # 위아래 매듭 띠
        k = rrect(X, Y, 4, yy - 4, W - 4, yy + 4, 4); L.put(cov(k + 1.2), hexc('#160E07')); L.put(cov(k), hexc('#6E4F2B') * (.7 + .5 * cyl[..., None]))
    L.save("Binding")

def tag():
    """퍼센트 꼬리표(9분할 64×48, 테두리 14px): 붉은 칠 + 검은 테두리 + 위쪽 광택."""
    W, H = 64, 48; X, Y = grid(W, H); L = Layer(W, H)
    o = rrect(X, Y, 1, 1, W - 1, H - 1, 7); b = rrect(X, Y, 3, 3, W - 3, H - 3, 5)
    L.put(cov(o), hexc('#1A0606')); L.put(cov(b), hexc('#B3202A')[None, None] * np.clip(1.2 - (Y / H)[..., None] * .5, .6, 1.2))
    L.put(cov(rrect(X, Y, 5, 5, W - 5, 15, 4)), np.array([1, .75, .75], np.float32), .25); L.save("Tag")

def badge():
    """레벨 원판 96×96: 어두운 바탕 + 금테 두 줄 + 위쪽 빛."""
    W = H = 96; X, Y = grid(W, H); L = Layer(W, H); r = np.hypot(X - 48, Y - 48)
    L.put(cov(r - 46), hexc('#0E0905')); L.put(cov(r - 43), hexc('#2E2015')[None, None] * np.clip(1.25 - (Y / H)[..., None] * .6, .6, 1.25))
    L.put(cov(np.abs(r - 39) - 2.2), hexc('#D8B25E')); L.put(cov(np.abs(r - 33.5) - .8), hexc('#8A6A35'))
    L.put(cov(np.abs(r - 43) - 1.2) * (Y < 40), hexc('#8A6A45'), .8); L.save("Badge")

if __name__ == "__main__":
    for f in (frame, fill, back, binding, tag, badge): f()
