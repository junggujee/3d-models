"""하프물범 여왕 초상화(다른 캐릭터보다 훨씬 고풍스럽게) — portrait_art.cel() 로 셀 채색한 여왕을
금박 타원 메달 안에 넣음: 남색 벨벳 바탕(가운데 밝게) · 머리 뒤 금빛 후광 + 빛줄기 · 금 이중 테 + 진주 구슬 · 월계수 가지 ·
꼭대기 왕관 문장 · 아래 리본 · 반짝이 · 인물 둘레 금빛 번짐 + 왼쪽 위 금빛 테두리 빛 · 살짝 캔버스 결.
입력: exports/portraits_raw/queen_<표정>_flat/shade.png (blender/build_joraeng.py --queen)
출력: unity/Assets/Resources/UI/Portraits/queen_<표정>.png (600×720, 투명 배경) · previews/queen_portraits_sheet.png
실행: python3 tools/queen_portrait.py"""
import os, sys, math
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, "exports", "portraits_raw"); OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Portraits")
sys.argv = [sys.argv[0], "__none__"]                       # portrait_art 를 불러올 때 다른 이름으로 처리하지 않게
import portrait_art as pa
W, H = 600, 720; CX, CY = W / 2, 384; RX, RY = 255, 300          # 메달 타원
GOLD, GOLD_D, GOLD_L = np.array([0.93, 0.72, 0.3]), np.array([0.55, 0.36, 0.12]), np.array([1.0, 0.93, 0.66])
Y, X = np.mgrid[0:H, 0:W].astype(np.float32)
def ell(rx, ry, cx=CX, cy=CY): return np.sqrt(((X - cx) / rx) ** 2 + ((Y - cy) / ry) ** 2)
def cov(d, w=1.2): return np.clip(0.5 - d / w, 0, 1)
class Layer:
    def __init__(s): s.rgb = np.zeros((H, W, 3), np.float32); s.a = np.zeros((H, W), np.float32)
    def put(s, m, col, k=1.0):
        m = np.clip(m * k, 0, 1)[..., None]; c = np.broadcast_to(np.asarray(col, np.float32), s.rgb.shape) if np.ndim(col) == 1 else col
        s.rgb = s.rgb * (1 - m) + c * m; s.a = s.a + (1 - s.a) * m[..., 0]
def backdrop():
    L = Layer(); e = ell(RX, RY)
    inner = e < 1.0
    r0 = ell(RX * 0.95, RY * 0.95, CX, CY - 60)                                           # 벨벳: 가운데 밝은 남색 → 가장자리 짙게
    vel = np.clip(1 - r0, 0, 1)[..., None]; base = np.array([0.04, 0.07, 0.2]) + np.array([0.12, 0.17, 0.36]) * vel ** 1.4
    rng = np.random.default_rng(7); grain = (rng.random((H, W)) - 0.5)[..., None] * 0.025
    L.put(cov((e - 1.0) * RX), base + grain)
    halo = np.exp(-(ell(170, 170, CX, 215) ** 2) * 1.6)[..., None]                         # 머리 뒤 금빛 후광
    ang = np.arctan2(Y - 215, X - CX); rays = (0.5 + 0.5 * np.cos(ang * 18)) ** 6 * np.exp(-ell(300, 300, CX, 215) * 1.2)
    glow = np.clip(halo * 0.75 + rays[..., None] * 0.35, 0, 1) * (e < 1.0)[..., None]
    L.rgb = L.rgb * (1 - glow) + GOLD_L * glow
    for (sx, sy, sr) in ((150, 150, 5), (450, 175, 4), (120, 420, 3.5), (480, 470, 4.5), (175, 600, 3), (430, 620, 3.5), (300, 92, 3)):   # 반짝이
        d = np.hypot(X - sx, Y - sy); a2 = np.arctan2(Y - sy, X - sx); star = d - sr * (0.35 + 0.65 * np.abs(np.cos(2 * a2)) ** 6) * 3
        L.put(cov(star, 1.5), GOLD_L)
    return L, inner
def frame(L):
    e = ell(RX, RY)
    for (r, w, c) in ((1.0, 0.03, GOLD), (1.04, 0.012, GOLD_D), (1.075, 0.02, GOLD)):       # 금 이중 테
        L.put(cov((np.abs(e - r) - w) * RX), c)
    hl = np.clip((CY - Y) / RY, 0, 1) * cov((np.abs(e - 1.0) - 0.012) * RX); L.put(hl, GOLD_L, 0.7)   # 위쪽 하이라이트
    for k in range(44):                                                                     # 진주 구슬
        t = 2 * math.pi * k / 44; px, py = CX + RX * 1.04 * math.cos(t), CY + RY * 1.04 * math.sin(t)
        d = np.hypot(X - px, Y - py); L.put(cov(d - 5.2), np.array([0.97, 0.95, 1.0])); L.put(cov(np.hypot(X - px + 1.6, Y - py + 1.6) - 1.8), np.array([1, 1, 1]))
    for side in (-1, 1):                                                                    # 월계수 가지(아래 양옆에서 위로)
        for k in range(11):
            t = 0.62 + 0.035 * k; ang = math.pi / 2 + side * (math.pi * t)
            px, py = CX + RX * 1.1 * math.cos(ang), CY + RY * 1.1 * math.sin(ang)
            tang = ang + side * math.pi / 2; lx, ly = math.cos(tang + side * 0.6), math.sin(tang + side * 0.6)
            for s2 in (-1, 1):
                qx, qy = px + s2 * 9 * (-ly), py + s2 * 9 * lx
                d = np.sqrt((((X - qx) * lx + (Y - qy) * ly) / 13) ** 2 + (((X - qx) * -ly + (Y - qy) * lx) / 5) ** 2)
                L.put(cov((d - 1) * 6), GOLD); L.put(cov((d - 0.55) * 6), GOLD_L, 0.6)
    cx, cy = CX, CY - RY * 1.08                                                             # 꼭대기 왕관 문장
    band = np.maximum(np.abs(Y - (cy + 6)) - 8, np.abs(X - cx) - 44)
    spikes = np.min([np.maximum((cy - 22 - (8 if k == 2 else 0)) + np.abs(X - (cx - 36 + 18 * k)) * 2.4 - Y, Y - cy) for k in range(5)], axis=0)
    crown = np.minimum(band, spikes); L.put(cov(crown + 2.2), GOLD_D); L.put(cov(crown), GOLD); L.put(cov(crown + 1.5) * (Y < cy + 2), GOLD_L, 0.6)
    for k in range(5): L.put(cov(np.hypot(X - (cx - 36 + 18 * k), Y - (cy - 26 - (8 if k == 2 else 0))) - 4.2), np.array([0.97, 0.95, 1.0]))
    L.put(cov(np.hypot((X - cx) / 7, (Y - (cy + 6)) / 6) - 1, 0.15), np.array([0.15, 0.4, 1.0]))     # 사파이어
    rb = np.maximum(np.abs(Y - (CY + RY * 1.02)) - 13, np.abs(X - CX) - 120 + np.abs(Y - (CY + RY * 1.02)) * 0.6)   # 아래 리본
    L.put(cov(rb + 2), np.array([0.25, 0.02, 0.06])); L.put(cov(rb), np.array([0.62, 0.06, 0.14]))
    L.put(cov(np.abs(rb + 6) - 1.2) * cov(rb), GOLD, 0.9)
def place(fig):
    """인물을 메달 안 흉상으로: 0.8배 · 아래를 타원 아래쪽에 맞춤."""
    k = 0.8; fw, fh = int(W * k), int(H * k); sm = fig.resize((fw, fh), Image.LANCZOS)
    can = Image.new("RGBA", (W, H), (0, 0, 0, 0)); can.alpha_composite(sm, ((W - fw) // 2 + 6, int(CY + RY * 0.99) - fh)); return can
def compose(fig):
    L, inner = backdrop()
    f = np.asarray(place(fig)).astype(np.float32) / 255; frgb, fa = f[..., :3], f[..., 3]
    frgb = np.clip(frgb, 0, 1) ** 0.82                                                         # 그늘을 조금 밝게(여왕은 환하게)
    e_ = ell(RX, RY); above = Y < CY - RY * 0.32
    fa = fa * np.where(above, 1.0, np.clip((0.975 - e_) * 40, 0, 1))                           # 메달 밖은 잘라 냄(왕관·지팡이 머리만 테 밖으로)
    fa_img = Image.fromarray((fa * 255).astype(np.uint8))
    glow = np.asarray(fa_img.filter(ImageFilter.GaussianBlur(14))).astype(np.float32) / 255     # 인물 둘레 금빛 번짐
    L.put(np.clip(glow - fa, 0, 1) * 0.85 * (ell(RX, RY) < 1.0), GOLD_L)
    sh = np.asarray(fa_img.filter(ImageFilter.GaussianBlur(3)).transform((W, H), Image.AFFINE, (1, 0, -5, 0, 1, -5))).astype(np.float32) / 255
    rim = np.clip(fa - sh, 0, 1)[..., None]                                                    # 왼쪽 위 금빛 테두리 빛
    frgb = frgb * (1 - rim * 0.55) + GOLD_L * rim * 0.55
    lum = frgb @ np.array([0.3, 0.55, 0.15]); frgb = np.clip(lum[..., None] + (frgb - lum[..., None]) * 1.12, 0, 1)   # 채도 조금 ↑
    rng = np.random.default_rng(3); canvas = 1 + (rng.random((H, W, 1)) - 0.5) * 0.04; frgb = np.clip(frgb * canvas, 0, 1)   # 캔버스 결
    m = fa[..., None]; L.rgb = L.rgb * (1 - m) + frgb * m; L.a = L.a + (1 - L.a) * fa
    frame(L)                                                                                   # 테는 맨 위(지팡이·왕관이 테 앞으로 나오도록 인물 위쪽만 남김)
    top = (fa > 0.5) & above; L.rgb[top] = frgb[top]; L.a[top] = 1.0                                 # 왕관·지팡이 머리는 테 앞으로(입체감)
    return Image.fromarray((np.concatenate([np.clip(L.rgb, 0, 1), L.a[..., None]], -1) * 255).astype(np.uint8), "RGBA")
os.makedirs(OUT, exist_ok=True); tiles = []
for e in pa.EXPR:
    fp, sp = os.path.join(RAW, f"queen_{e}_flat.png"), os.path.join(RAW, f"queen_{e}_shade.png")
    if not os.path.exists(fp): print("없음:", fp); continue
    fig = pa.cel(fp, sp).resize((W, H), Image.LANCZOS)
    img = compose(fig); img.save(os.path.join(OUT, f"queen_{e}.png")); tiles.append((e, img)); print("✔ queen", e)
if tiles:
    tw, th = 300, 360; sheet = Image.new("RGB", (tw * len(tiles), th + 34), (226, 214, 188)); d = ImageDraw.Draw(sheet)
    try: font = ImageFont.truetype(os.path.join(ROOT, "unity", "Assets", "Resources", "Fonts", "NanumMyeongjoExtraBold.ttf"), 20)
    except OSError: font = ImageFont.load_default()
    for i, (e, img) in enumerate(tiles):
        sm = img.resize((tw, th), Image.LANCZOS); sheet.paste(sm, (i * tw, 0), sm); d.text((i * tw + tw // 2, th + 17), e, fill=(60, 40, 25), font=font, anchor="mm")
    sheet.save(os.path.join(ROOT, "previews", "queen_portraits_sheet.png")); print("✔ sheet")
