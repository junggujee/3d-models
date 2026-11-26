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

def whale_king(l, X, Y):
    """고래고래 임금님: 고래 옆모습 + 금관(뾰족 5 · 앞 별) + 보라 망토 자락 + 별 반짝이."""
    cape = np.maximum(np.hypot((X - 112) / 58, (Y - 134) / 34) - 1, -(Y - 112) / 30)
    l.put(cov(cape * 20 + 2), hexc('#1A0A22')); l.put(cov(cape * 20), hexc('#7A2E8C'))
    whale(l, X, Y)
    band = np.maximum(np.abs(Y - 104) - 6, np.abs(X - 166) - 22)
    spikes = np.min([np.maximum((80 - (3 if k == 2 else 0)) + np.abs(X - (148 + 9 * k)) * 2.6 - Y, Y - 99) for k in range(5)], axis=0)   # 위로 뾰족
    crown = np.minimum(band, spikes)
    l.put(cov(crown + 1.8), hexc('#2A1A05')); l.put(cov(crown), hexc('#F2C14E')); l.put(cov(crown + 1.2) * (Y < 103), hexc('#FFE9A0'), .6)
    for k in range(5): l.put(cov(np.hypot(X - (148 + 9 * k), Y - (78 if k != 2 else 76)) - 3), hexc('#FFE9A0'))
    st = np.hypot(X - 166, Y - 104); ang = np.arctan2(Y - 104, X - 166); star = st - (4.5 + 2.5 * np.cos(5 * ang)); l.put(cov(star), hexc('#E7D7FF'))
    for (cx, cy, rr) in ((60, 70, 5), (205, 90, 4), (196, 200, 4.5)):            # 반짝이 별
        d = np.hypot(X - cx, Y - cy); a2 = np.arctan2(Y - cy, X - cx); l.put(cov(d - (rr * (0.55 + 0.45 * np.cos(4 * a2)))), hexc('#FFF4C8'))

def mandu(l, X, Y):
    """맨두: 하얀 동그란 물범 얼굴(정면) + 빨간 근위대 모자(금띠 · 별 · 흰 깃털) + 콧수염."""
    face = np.hypot((X - 128) / 70, (Y - 150) / 58) - 1
    l.put(cov(face * 30 + 2), hexc('#1A1E2A')); l.put(cov(face * 30), hexc('#F4F5F8')[None, None] * np.clip(1.15 - ((Y - 100) / 120)[..., None] * .3, .8, 1.2))
    cap = np.maximum(np.hypot((X - 128) / 52, (Y - 104) / 40) - 1, Y - 108)
    l.put(cov(cap * 20 + 2), hexc('#2A0505')); l.put(cov(cap * 20), hexc('#D23A3A'))
    l.put(cov(np.maximum(np.abs(Y - 102) - 4, np.abs(X - 128) - 50)), hexc('#F2C14E'))
    l.put(cov(np.maximum(np.hypot((X - 128) / 58, (Y - 112) / 9) - 1, 108 - Y) * 10), hexc('#1A1A22'))   # 챙
    plume = np.hypot((X - 150) / 9, (Y - 50) / 20) - 1; l.put(cov(plume * 9 + 1.5), hexc('#8A8A99')); l.put(cov(plume * 9), hexc('#FFFFFF'))
    st = np.hypot(X - 128, Y - 82); ang = np.arctan2(Y - 82, X - 128); l.put(cov(st - (7 + 4 * np.cos(5 * (ang + math.pi / 2)))), hexc('#FFE9A0'))
    for ex in (100, 156):
        l.put(cov(np.hypot(X - ex, Y - 146) - 8), hexc('#0A0A10')); l.put(cov(np.hypot(X - ex + 2.5, Y - 149) - 2.6), hexc('#FFFFFF'))
        l.put(cov(np.maximum(np.abs(Y - 130 + (np.abs(X - ex)) * 0.15) - 1.8, np.abs(X - ex) - 9)), hexc('#2A2A30'))   # 눈썹
    nose = np.maximum(np.maximum(Y - 168, 158 - Y - (8 - np.abs(X - 128))), np.abs(X - 128) - 8); l.put(cov(nose), hexc('#3A3A44'))
    for sx in (-1, 1):
        for k in range(3):
            x0 = 128 + sx * 22; y0 = 172 + k * 6
            d = np.where((X - x0) * sx > 0, np.abs(Y - y0 - (X - x0) * sx * (k - 1) * 0.12) - 1.2, 9); d = np.maximum(d, (X - x0) * sx - 44)
            l.put(cov(d), hexc('#6A6A74'))

def bear(l, X, Y):
    """곰곰이: 졸린 갈색 곰 얼굴(둥근 귀 · 감긴 눈 · 밝은 주둥이 · 코) + 머리 위 Zzz + 사골곰뼈."""
    head = np.hypot((X - 128) / 64, (Y - 140) / 58) - 1
    ears = np.minimum(np.hypot(X - 78, Y - 86) - 24, np.hypot(X - 178, Y - 86) - 24)
    s = np.minimum(head * 40, ears)
    l.put(cov(s + 2), hexc('#1E1008')); l.put(cov(s), hexc('#8A5A33'))
    for ex in (78, 178): l.put(cov(np.hypot(X - ex, Y - 88) - 12), hexc('#D9A877'))
    muzzle = np.hypot((X - 128) / 34, (Y - 166) / 24) - 1; l.put(cov(muzzle * 20), hexc('#E8C9A0'))
    l.put(cov(np.hypot((X - 128) / 13, (Y - 154) / 9) - 1), hexc('#2A1608'))
    for ex in (100, 156):                                                        # 감긴 눈(아래로 둥근 선)
        d = np.abs(np.hypot(X - ex, Y - 122) - 13) - 2.4; l.put(cov(np.maximum(d, 122 - Y)), hexc('#2A1608'))
    l.put(cov(np.maximum(np.abs(np.hypot(X - 128, Y - 168) - 14) - 2.2, 168 - Y)), hexc('#2A1608'))   # 입
    bone = np.minimum(np.maximum(np.abs(Y - 214) - 6, np.abs(X - 128) - 44), np.minimum.reduce([np.hypot(X - xx, Y - yy) - 9 for xx in (84, 172) for yy in (206, 222)]))
    l.put(cov(bone + 2), hexc('#3A2A18')); l.put(cov(bone), hexc('#F4EBD6'))
    for (zx, zy, zs) in ((186, 52, 1.0), (206, 34, 0.75)):                      # Zzz
        z = np.maximum.reduce([np.abs(X - zx) - 9 * zs, np.abs(Y - zy) - 9 * zs])
        zi = np.maximum(np.abs((Y - zy) + (X - zx)) - 2.5, np.abs(X - zx) - 7 * zs)
        zz = np.minimum.reduce([np.maximum(np.abs(Y - (zy - 8 * zs)) - 2.2, np.abs(X - zx) - 8 * zs), np.maximum(np.abs(Y - (zy + 8 * zs)) - 2.2, np.abs(X - zx) - 8 * zs), zi])
        l.put(cov(np.maximum(zz, z)), hexc('#CFE3FF'))

def default(l, X, Y):
    """일반 보스: 금빛 뿔 달린 가면."""
    mask = np.hypot((X - 128) / 46, (Y - 140) / 54) - 1
    horns = np.minimum(np.hypot((X - 92) / 12, (Y - 92) / 34) - 1, np.hypot((X - 164) / 12, (Y - 92) / 34) - 1)
    s = np.minimum(mask * 40, horns * 14); l.put(cov(s + 2), hexc('#0A0705')); l.put(cov(s), hexc('#C9A35A'))
    for ex in (108, 148): l.put(cov(np.hypot((X - ex) / 12, (Y - 132) / 7) - 1), hexc('#4A0E0E'))

if __name__ == "__main__":
    frame(); cap(); emblem("Whale", whale); emblem("WhaleKing", whale_king); emblem("Mandu", mandu); emblem("Bear", bear); emblem("Default", default)
