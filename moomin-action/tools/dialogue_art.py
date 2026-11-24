"""대화창 그림(사용자 참고 이미지: 밝은 양피지 판 · 왼쪽 위 붉은 보석이 박힌 어두운 장식 모서리 · 위 가운데 제목 띠 · 왼쪽 일러스트 · 오른쪽 글상자와 스크롤 화살표).
+ 제목·장(칸토)·보스 이름 장식(사용자 참고 '스타 세일러' 로고: 크림→베이지 글자, 양끝 마름모 가는 선, 작은 네 갈래 별).
모두 2배 해상도(Unity pixelsPerUnit 200) → unity/Assets/Resources/UI/Dialogue/*.png · Resources/UI/Ornament/*.png
판·글상자·초상화 틀은 실제 크기 그대로(늘이지 않음), 띠·버튼·이름표는 9분할.
실행: python3 tools/dialogue_art.py"""
import os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Dialogue"); ORN = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Ornament")
os.makedirs(OUT, exist_ok=True); os.makedirs(ORN, exist_ok=True)
S = 2           # UI 1 단위 = 2 픽셀
SS = 4          # 매끈한 선을 위해 4배로 그려 줄임
rng = np.random.default_rng(7)

# 레이아웃(UI 단위) — UI/DialogueBox.cs 와 같아야 함
PANEL = (1560, 400); TEXTBOX = (1070, 300); PORTRAIT = (400, 340)

INK = (42, 28, 20); BROWN = (96, 64, 38); GOLD = (196, 158, 92); GOLD_HI = (240, 214, 150); RED = (178, 34, 38)

def save(img, name, folder=OUT): img.save(os.path.join(folder, name + ".png")); print("✔", name, img.size)

def fibers(w, h, strength):
    """양피지 섬유결: 가로로 긴 잡음 + 얼룩."""
    n = rng.normal(0, 1, (h, w)).astype(np.float32)
    a = Image.fromarray(((n * 30) + 128).clip(0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(1))
    a = a.resize((max(1, w // 6), h)).resize((w, h), Image.BILINEAR)   # 가로 결
    blot = Image.fromarray((rng.normal(0, 1, (max(2, h // 40), max(2, w // 40))) * 40 + 128).clip(0, 255).astype(np.uint8)).resize((w, h), Image.BICUBIC)
    v = (np.asarray(a).astype(np.float32) - 128) / 128 * 0.6 + (np.asarray(blot).astype(np.float32) - 128) / 128 * 0.8
    return v * strength

def parchment(w, h, base=(239, 228, 202), edge=(214, 192, 150), vign=0.9, grain=10):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    dx = np.minimum(xx, w - 1 - xx) / (w * 0.5); dy = np.minimum(yy, h - 1 - yy) / (h * 0.5)
    d = np.clip(np.minimum(dx * w / min(w, h), dy * h / min(w, h)) * 2.2, 0, 1)   # 가장자리 0 → 안쪽 1
    t = (1 - d) ** 1.6 * vign
    rgb = np.array(base, np.float32)[None, None] * (1 - t[..., None]) + np.array(edge, np.float32)[None, None] * t[..., None]
    rgb += fibers(w, h, grain)[..., None]
    return rgb.clip(0, 255)

def rounded_mask(w, h, r):
    m = Image.new("L", (w * SS, h * SS), 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, w * SS - 1, h * SS - 1], r * SS, fill=255); return m.resize((w, h), Image.LANCZOS)

def draw_ss(w, h, fn):
    """SS배로 그린 뒤 줄여서 매끈하게."""
    im = Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0)); fn(ImageDraw.Draw(im), SS); return im.resize((w, h), Image.LANCZOS)

# ── 1. 큰 양피지 판(대화창 바탕)
def panel():
    w, h = PANEL[0] * S, PANEL[1] * S
    rgb = parchment(w, h, vign=1.0, grain=9)
    img = Image.fromarray(rgb.astype(np.uint8)).convert("RGBA"); img.putalpha(rounded_mask(w, h, 14 * S))
    def lines(d, k):
        o = 6 * S * k; d.rounded_rectangle([o, o, w * k - o, h * k - o], 12 * S * k, outline=BROWN + (255,), width=3 * S * k)
        o2 = 13 * S * k; d.rounded_rectangle([o2, o2, w * k - o2, h * k - o2], 8 * S * k, outline=(150, 112, 66, 200), width=1 * S * k)
    img.alpha_composite(draw_ss(w, h, lines))
    # 그림자(아래쪽으로 살짝) — 판 밖으로
    sh = Image.new("RGBA", (w + 40 * S, h + 40 * S), (0, 0, 0, 0)); m = rounded_mask(w, h, 14 * S)
    blk = Image.new("RGBA", (w, h), (20, 12, 6, 150)); blk.putalpha(m.point(lambda v: v * 150 // 255))
    sh.alpha_composite(blk, (20 * S, 26 * S)); sh = sh.filter(ImageFilter.GaussianBlur(10 * S)); sh.alpha_composite(img, (20 * S, 20 * S))
    save(sh, "Panel")   # 바깥 여백 20 단위(그림자) — DialogueBox 가 크기 + 40 으로 배치

# ── 2. 모서리 장식: 왼쪽 위(붉은 보석) · 나머지 셋(작은 덩굴)
def scroll_curl(d, k, cx, cy, r, turns, width, col):
    pts = []
    for i in range(80):
        t = i / 79; a = turns * 2 * math.pi * t; rr = r * (1 - 0.75 * t)
        pts.append(((cx + rr * math.cos(a)) * k, (cy + rr * math.sin(a)) * k))
    d.line(pts, fill=col, width=int(width * k), joint="curve")

def corner_gem():
    n = 120; w = h = n * S
    def fn(d, k):
        u = S * k; dark = (34, 22, 16, 255); edge = (120, 86, 50, 255)
        # L 자 판(모서리를 감싸는 어두운 장식 금속)
        poly = [(0, 0), (n * 0.98, 0), (n * 0.86, 10), (n * 0.62, 12), (n * 0.5, 22), (34, 24), (24, 34), (22, n * 0.5), (12, n * 0.62), (10, n * 0.86), (0, n * 0.98)]
        d.polygon([(x * u, y * u) for x, y in poly], fill=dark, outline=edge, width=int(2 * u))
        for (cx, cy, rot) in ((n * 0.56, 9, 0), (9, n * 0.56, 1)):
            for s in (-1, 1):
                if rot == 0: scroll_curl(d, u, cx + s * 14, cy + 5, 7, 1.3 * s, 2.4, dark)
                else: scroll_curl(d, u, cx + 5, cy + s * 14, 7, 1.3 * s, 2.4, dark)
        # 마름모 끝 장식
        for (x, y) in ((n * 0.98, 0), (0, n * 0.98)):
            d.polygon([((x - 6) * u, y * u), (x * u, (y - 6) * u), ((x + 6) * u, y * u), (x * u, (y + 6) * u)], fill=dark)
        # 보석 받침(금 테두리) + 붉은 보석(위 밝은 반사)
        c = 26; d.ellipse([(c - 19) * u, (c - 19) * u, (c + 19) * u, (c + 19) * u], fill=(28, 18, 12, 255), outline=GOLD + (255,), width=int(3 * u))
        for i in range(12):
            a = i / 12 * 2 * math.pi; x, y = c + 21 * math.cos(a), c + 21 * math.sin(a); d.ellipse([(x - 2.2) * u, (y - 2.2) * u, (x + 2.2) * u, (y + 2.2) * u], fill=GOLD + (255,))
        for i, rr in enumerate(range(14, 0, -1)):
            t = i / 13; col = (int(110 + 120 * t), int(14 + 40 * t * t), int(20 + 40 * t * t), 255)
            ox, oy = -3 * t, -4 * t; d.ellipse([(c + ox - rr) * u, (c + oy - rr) * u, (c + ox + rr) * u, (c + oy + rr) * u], fill=col)
        d.ellipse([(c - 8) * u, (c - 10) * u, (c - 2) * u, (c - 5) * u], fill=(255, 220, 220, 230))
    save(draw_ss(w, h, fn), "CornerGem")

def corner_small():
    n = 64; w = h = n * S
    def fn(d, k):
        u = S * k; dark = (40, 27, 19, 255)
        d.polygon([(0, 0), (n * u, 0), (n * 0.7 * u, 6 * u), (14 * u, 8 * u), (8 * u, 14 * u), (6 * u, n * 0.7 * u), (0, n * u)], fill=dark)
        scroll_curl(d, u, 26, 8, 6, 1.2, 2.2, dark); scroll_curl(d, u, 8, 26, 6, -1.2, 2.2, dark)
        d.polygon([(7 * u, 13 * u), (13 * u, 7 * u), (19 * u, 13 * u), (13 * u, 19 * u)], fill=GOLD + (255,))
    save(draw_ss(w, h, fn), "Corner")

# ── 3. 제목 띠(어두운 판 + 금 이중 테두리 + 양끝 뾰족) — 9분할(좌우 44 단위)
def banner():
    w, h = 300 * S, 78 * S
    def fn(d, k):
        u = S * k; W, H = 300, 78; m = 6
        outer = [(0, H / 2), (30, m), (W - 30, m), (W, H / 2), (W - 30, H - m), (30, H - m)]
        d.polygon([(x * u, y * u) for x, y in outer], fill=(46, 30, 21, 248))
        inner = [(10, H / 2), (34, m + 6), (W - 34, m + 6), (W - 10, H / 2), (W - 34, H - m - 6), (34, H - m - 6)]
        d.polygon([(x * u, y * u) for x, y in outer], outline=GOLD + (255,), width=int(2.5 * u))
        d.polygon([(x * u, y * u) for x, y in inner], outline=(150, 116, 66, 255), width=int(1.2 * u))
        for x in (6, W - 6): d.polygon([((x - 4) * u, H / 2 * u), (x * u, (H / 2 - 4) * u), ((x + 4) * u, H / 2 * u), (x * u, (H / 2 + 4) * u)], fill=GOLD_HI + (255,))
    img = draw_ss(w, h, fn)
    # 안쪽 은은한 세로 빛
    a = np.asarray(img).astype(np.float32); yy = np.linspace(-1, 1, h)[:, None]; glow = (1 - yy ** 2) * 18
    a[..., :3] = np.clip(a[..., :3] + glow[..., None] * (a[..., 3:4] > 200), 0, 255); save(Image.fromarray(a.astype(np.uint8)), "TitleBanner")

# ── 4. 초상화 틀(따뜻한 빛 바탕 + 갈색 테두리) · 5. 글상자(밝은 크림 + 가는 테두리 + 위쪽 안 그림자)
def portrait_bg():
    w, h = PORTRAIT[0] * S, PORTRAIT[1] * S
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32); cx, cy = w * 0.5, h * 0.42
    r = np.sqrt(((xx - cx) / (w * 0.62)) ** 2 + ((yy - cy) / (h * 0.62)) ** 2); t = np.clip(r, 0, 1) ** 1.4
    rgb = np.array((250, 240, 214), np.float32) * (1 - t[..., None]) + np.array((205, 178, 132), np.float32) * t[..., None]
    rgb += fibers(w, h, 5)[..., None]
    img = Image.fromarray(rgb.clip(0, 255).astype(np.uint8)).convert("RGBA"); img.putalpha(rounded_mask(w, h, 10 * S))
    def fn(d, k):
        u = S * k; d.rounded_rectangle([1.5 * u, 1.5 * u, (PORTRAIT[0] - 1.5) * u, (PORTRAIT[1] - 1.5) * u], 10 * u, outline=BROWN + (255,), width=int(3 * u))
        d.rounded_rectangle([8 * u, 8 * u, (PORTRAIT[0] - 8) * u, (PORTRAIT[1] - 8) * u], 6 * u, outline=(176, 140, 88, 170), width=int(1 * u))
    img.alpha_composite(draw_ss(w, h, fn)); save(img, "PortraitBg")
    # 초상화 위에 얹는 테두리만(초상화가 틀 밖으로 튀어나올 때 아래쪽 테두리를 다시 그림)
    fr = draw_ss(w, h, fn); a = np.asarray(fr).copy(); cut = int(h * 0.38); a[:cut, :, 3] = (a[:cut, :, 3] * np.linspace(0, 1, cut)[:, None] ** 6).astype(np.uint8)
    save(Image.fromarray(a), "PortraitFrame")   # 아래·옆 테두리만(머리는 위 테두리 앞으로 튀어나옴)

def textbox():
    w, h = TEXTBOX[0] * S, TEXTBOX[1] * S
    rgb = parchment(w, h, base=(249, 243, 228), edge=(230, 214, 182), vign=0.55, grain=5)
    yy = np.arange(h, dtype=np.float32)[:, None]; rgb -= (np.exp(-yy / (10 * S)) * 22)[..., None]   # 위쪽 안 그림자
    img = Image.fromarray(rgb.clip(0, 255).astype(np.uint8)).convert("RGBA"); img.putalpha(rounded_mask(w, h, 8 * S))
    def fn(d, k):
        u = S * k; d.rounded_rectangle([1 * u, 1 * u, (TEXTBOX[0] - 1) * u, (TEXTBOX[1] - 1) * u], 8 * u, outline=(128, 92, 56, 255), width=int(2 * u))
    img.alpha_composite(draw_ss(w, h, fn)); save(img, "TextBox")

# ── 6. 선택 버튼(보통 · 강조 · 마우스 올림 빛) 9분할(좌우 30)
def button(name, fill, line, hi):
    W, H = 200, 64; w, h = W * S, H * S
    def fn(d, k):
        u = S * k; m = 3
        shape = [(0, H / 2), (18, m), (W - 18, m), (W, H / 2), (W - 18, H - m), (18, H - m)]
        d.polygon([(x * u, y * u) for x, y in shape], fill=fill + (250,), outline=line + (255,), width=int(2.4 * u))
        inner = [(9, H / 2), (22, m + 6), (W - 22, m + 6), (W - 9, H / 2), (W - 22, H - m - 6), (22, H - m - 6)]
        d.polygon([(x * u, y * u) for x, y in inner], outline=hi + (150,), width=int(1 * u))
    img = draw_ss(w, h, fn); a = np.asarray(img).astype(np.float32); yy = np.linspace(0, 1, h)[:, None]
    a[..., :3] = np.clip(a[..., :3] + ((1 - yy) * 26)[..., None] * (a[..., 3:4] > 200), 0, 255); save(Image.fromarray(a.astype(np.uint8)), name)

def arrow():
    n = 28; w = h = n * S
    def fn(d, k):
        u = S * k; d.polygon([(4 * u, 8 * u), (24 * u, 8 * u), (14 * u, 22 * u)], fill=BROWN + (255,)); d.polygon([(8 * u, 10 * u), (14 * u, 10 * u), (12 * u, 14 * u)], fill=(170, 130, 80, 255))
    save(draw_ss(w, h, fn), "Arrow")

def nameplate():
    W, H = 220, 56; w, h = W * S, H * S
    def fn(d, k):
        u = S * k; shape = [(0, H / 2), (14, 4), (W - 14, 4), (W, H / 2), (W - 14, H - 4), (14, H - 4)]
        d.polygon([(x * u, y * u) for x, y in shape], fill=(58, 38, 26, 250), outline=GOLD + (255,), width=int(2 * u))
    save(draw_ss(w, h, fn), "NamePlate")

# ── 7. 장식선(스타 세일러풍): 가는 금선 + 양끝 마름모 + 가운데 네 갈래 별 / 네 갈래 별 단독
def sparkle(d, u, cx, cy, r, col, thin=0.18):
    pts = []
    for i in range(8):
        a = i * math.pi / 4 - math.pi / 2; rr = r if i % 2 == 0 else r * thin; pts.append(((cx + rr * math.cos(a)) * u, (cy + rr * math.sin(a)) * u))
    d.polygon(pts, fill=col)

def divider():
    W, H = 600, 24; w, h = W * S, H * S
    def fn(d, k):
        u = S * k; c = H / 2
        for x0, x1 in ((14, W / 2 - 22), (W / 2 + 22, W - 14)):
            d.line([(x0 * u, c * u), (x1 * u, c * u)], fill=(214, 186, 132, 230), width=int(1.6 * u))
        for x in (8, W - 8): d.polygon([((x - 6) * u, c * u), (x * u, (c - 6) * u), ((x + 6) * u, c * u), (x * u, (c + 6) * u)], fill=(236, 214, 164, 255))
        sparkle(d, u, W / 2, c, 11, (246, 206, 110, 255)); sparkle(d, u, W / 2, c, 4.5, (255, 246, 214, 255), 0.5)
        for x in (W / 2 - 34, W / 2 + 34): d.ellipse([(x - 2) * u, (c - 2) * u, (x + 2) * u, (c + 2) * u], fill=(236, 214, 164, 255))
    save(draw_ss(w, h, fn), "Divider", ORN)

def star():
    n = 48; w = h = n * S
    def fn(d, k): u = S * k; sparkle(d, u, n / 2, n / 2, n / 2 - 1, (250, 214, 120, 255)); sparkle(d, u, n / 2, n / 2, n / 5, (255, 250, 230, 255), 0.5)
    img = draw_ss(w, h, fn); glow = img.filter(ImageFilter.GaussianBlur(4 * S)); out = Image.new("RGBA", img.size); out.alpha_composite(glow); out.alpha_composite(img); save(out, "Sparkle", ORN)

panel(); corner_gem(); corner_small(); banner(); portrait_bg(); textbox(); arrow(); nameplate()
button("Button", (54, 36, 25), (176, 140, 84), (230, 200, 140)); button("ButtonPrimary", (110, 40, 30), GOLD, GOLD_HI); button("ButtonHover", (82, 56, 36), GOLD_HI, (255, 236, 180))
divider(); star()
