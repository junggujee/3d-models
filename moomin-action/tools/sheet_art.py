"""캐릭터 시트 · 도감 · 기절 효과 그림(사용자 참고: 양피지 캐릭터 시트 / 회색 종이 도감 / '기절' 주황 폭발 글자).
분위기: 밝은 종이 + 먹칠(먹물 번짐·붓질) + 양털처럼 뽀송한 흰 테두리.
배치(UI 단위)는 UI/CharacterSheet.cs · UI/CodexPage.cs 의 상수와 같아야 함 — 판 그림에 칸 테두리·머리 먹줄을 미리 그려 둠.
결과(1배 = pixelsPerUnit 100, 작은 것은 2배):
  unity/Assets/Resources/UI/Sheet/  Sheet.png Arch.png Slot.png SlotSel.png Card.png CardSel.png Popup.png Circle.png BarFrame.png Tab*.png Arrow.png Button.png
  unity/Assets/Resources/UI/Codex/  Codex.png Card.png CardGlow.png Chip.png
  unity/Assets/Resources/UI/Fx/     StunBurst.png StunText.png
실행: python3 tools/sheet_art.py"""
import os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RES = os.path.join(ROOT, "unity", "Assets", "Resources", "UI")
SHEET, CODEX, FX = (os.path.join(RES, d) for d in ("Sheet", "Codex", "Fx"))
for d in (SHEET, CODEX, FX): os.makedirs(d, exist_ok=True)
rng = np.random.default_rng(11)
NANUM = "/usr/share/fonts/truetype/nanum/"

# ── 배치(UI 단위, 왼쪽 위 기준) — CharacterSheet.cs 와 같게
SW, SH = 1840, 1020
SEC = {"A": (36, 40, 580, 580, ""), "B": (640, 40, 540, 580, "능력치"), "D": (1204, 40, 600, 580, "자세히"),
       "E": (36, 644, 880, 340, "가방"), "F": (940, 644, 864, 340, "스킬")}
CW, CH = 1700, 940   # 도감
CSEC = {"L": (40, 150, 760, 720, ""), "R": (824, 150, 836, 720, "")}

PAPER = (247, 240, 224); PAPER_EDGE = (226, 210, 180); INK = (52, 44, 40)

def save(img, folder, name): img.save(os.path.join(folder, name + ".png")); print("✔", os.path.basename(folder) + "/" + name, img.size)

def blur_noise(h, w, scale, sigma):
    n = rng.normal(0, 1, (max(2, h // scale), max(2, w // scale))).astype(np.float32)
    im = Image.fromarray(((n * 40) + 128).clip(0, 255).astype(np.uint8)).resize((w, h), Image.BICUBIC).filter(ImageFilter.GaussianBlur(sigma))
    return (np.asarray(im).astype(np.float32) - 128) / 128

def paper(w, h, base=PAPER, edge=PAPER_EDGE, grain=7):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.minimum(np.minimum(xx, w - 1 - xx), np.minimum(yy, h - 1 - yy)) / (0.18 * min(w, h)); t = np.clip(1 - d, 0, 1) ** 2 * 0.8
    rgb = np.array(base, np.float32) * (1 - t[..., None]) + np.array(edge, np.float32) * t[..., None]
    fib = rng.normal(0, 1, (h, w)).astype(np.float32); fib = np.asarray(Image.fromarray(((fib * 30) + 128).clip(0, 255).astype(np.uint8)).resize((max(1, w // 5), h)).resize((w, h), Image.BILINEAR)).astype(np.float32) / 128 - 1
    rgb += (fib * 0.5 + blur_noise(h, w, 30, 6) * 0.9)[..., None] * grain
    return rgb

def ink_wash(rgb, mask, strength):
    """먹물 번짐: mask(0~1) 만큼 먹색을 얇게 — 가장자리는 진하고(번진 테) 안은 옅게."""
    m = np.clip(mask, 0, 1); ring = np.clip(m - np.asarray(Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6))).astype(np.float32) / 255, 0, 1) * 2.5
    a = np.clip(m * strength + ring * strength * 0.8, 0, 0.9)[..., None]
    return rgb * (1 - a) + np.array(INK, np.float32) * a

def brush_stroke_mask(w, h, pts, width, rough=0.35, taper=True, seed=0):
    """붓 한 획: 획 폭 안에 가는 붓털 선 여러 가닥(들쭉날쭉한 시작·끝, 마른 붓 갈라짐) → 살짝 번짐."""
    S = 2; im = Image.new("L", (w * S, h * S), 0); d = ImageDraw.Draw(im); r = np.random.default_rng(seed)
    P = np.array(pts, np.float32); seg = np.diff(P, axis=0); L = np.concatenate([[0], np.cumsum(np.hypot(seg[:, 0], seg[:, 1]))]); total = max(L[-1], 1)
    n = max(24, int(total / 2)); ts = np.linspace(0, total, n)
    xs = np.interp(ts, L, P[:, 0]); ys = np.interp(ts, L, P[:, 1]); dx = np.gradient(xs); dy = np.gradient(ys); nn = np.hypot(dx, dy) + 1e-6; nx, ny = -dy / nn, dx / nn
    u = ts / total; prof = (np.sin(np.pi * np.clip(u, 0, 1)) ** 0.3) if taper else np.ones_like(u)
    hairs = max(8, int(width * 1.2))
    for k in range(hairs):
        off = (k / (hairs - 1) - 0.5) * width; a0 = r.uniform(0, 0.12 * rough + 0.02); a1 = 1 - r.uniform(0, 0.25 * rough + 0.02)
        gap_c, gap_w = r.uniform(0.2, 0.9), (r.uniform(0.05, 0.3) if r.random() < rough else 0)
        val = int(r.uniform(150, 255)); lw = max(1, int(S * r.uniform(1.2, 2.6)))
        line = []
        for i in range(n):
            if u[i] < a0 or u[i] > a1 or abs(u[i] - gap_c) < gap_w / 2:
                if len(line) > 1: d.line(line, fill=val, width=lw)
                line = []; continue
            o = off * prof[i] + r.normal(0, 0.15)
            line.append(((xs[i] + nx[i] * o) * S, (ys[i] + ny[i] * o) * S))
        if len(line) > 1: d.line(line, fill=val, width=lw)
    m = np.asarray(im.resize((w, h), Image.LANCZOS).filter(ImageFilter.GaussianBlur(0.6))).astype(np.float32) / 255
    return np.clip(m * 1.3, 0, 1)

def rough_rect_mask(w, h, x, y, rw, rh, width, seed):
    """붓으로 그은 네모 테두리(모서리가 살짝 넘치고 굵기가 들쭉날쭉)."""
    r = np.random.default_rng(seed); m = np.zeros((h, w), np.float32); j = lambda: r.uniform(-3, 3)
    for (a, b) in (((x - 6, y + j()), (x + rw + 8, y + j())), ((x + rw + j(), y - 6), (x + rw + j(), y + rh + 8)), ((x + rw + 6, y + rh + j()), (x - 8, y + rh + j())), ((x + j(), y + rh + 6), (x + j(), y - 8))):
        pts = [(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t + r.uniform(-1.2, 1.2)) for t in np.linspace(0, 1, 9)]
        m = np.maximum(m, brush_stroke_mask(w, h, pts, width * r.uniform(0.8, 1.2), 0.45, True, int(r.integers(1 << 30))))
    return m

def wool_border(w, h, inset, puff=26, seed=3):
    """양털 테두리: 가장자리를 따라 동글동글한 흰 털뭉치(아래쪽 옅은 그늘 + 잔 컬)."""
    r = np.random.default_rng(seed); S = 2
    shape = Image.new("L", (w * S, h * S), 0); d = ImageDraw.Draw(shape)
    shade = Image.new("L", (w * S, h * S), 0); ds = ImageDraw.Draw(shade)
    hi = Image.new("L", (w * S, h * S), 0); dh = ImageDraw.Draw(hi)
    per = 2 * (w + h - 4 * inset); n = int(per / (puff * 0.85))
    def pt(u):
        L1, L2 = w - 2 * inset, h - 2 * inset; s = u * per
        if s < L1: return inset + s, inset
        s -= L1
        if s < L2: return w - inset, inset + s
        s -= L2
        if s < L1: return w - inset - s, h - inset
        s -= L1; return inset, h - inset - s
    for i in range(n):
        x, y = pt((i + r.uniform(-.2, .2)) / n); rr = puff * r.uniform(0.75, 1.2)
        for k in range(3):   # 털뭉치 하나 = 작은 원 셋
            ox, oy, q = r.uniform(-.4, .4) * rr, r.uniform(-.4, .4) * rr, rr * r.uniform(.55, .8)
            d.ellipse([(x + ox - q) * S, (y + oy - q) * S, (x + ox + q) * S, (y + oy + q) * S], fill=255)
            ds.ellipse([(x + ox - q * .8) * S, (y + oy - q * .2) * S, (x + ox + q * .9) * S, (y + oy + q * 1.05) * S], fill=255)
            dh.ellipse([(x + ox - q * .6) * S, (y + oy - q * .8) * S, (x + ox + q * .3) * S, (y + oy - q * .1) * S], fill=255)
    shape, shade, hi = (np.asarray(im.resize((w, h), Image.LANCZOS)).astype(np.float32) / 255 for im in (shape, shade, hi))
    curl = blur_noise(h, w, 3, 0.6)   # 잔 컬
    shade_b = np.asarray(Image.fromarray((shade * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5))).astype(np.float32) / 255
    hi_b = np.asarray(Image.fromarray((hi * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(4))).astype(np.float32) / 255
    col = np.ones((h, w, 3), np.float32) * np.array([238, 234, 226], np.float32)
    col += (hi_b * 16 - shade_b * 22 + curl * 10)[..., None]
    col = np.clip(col, 0, 255)
    edge_shadow = np.asarray(Image.fromarray((shape * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(7))).astype(np.float32) / 255
    return col, shape, edge_shadow

def compose_board(w, h, inset, sections, wash_seed, title_marks=True):
    """종이 판: 양털 테두리 + 밝은 종이 + 먹 번짐 + 칸 테두리(붓) + 머리 먹줄."""
    rgb = paper(w, h)
    # 먹칠: 모서리 번짐 몇 개 + 가로 붓질
    wash = np.zeros((h, w), np.float32); r = np.random.default_rng(wash_seed)
    for cx, cy, rad in ((0.03, 0.05, 0.22), (0.97, 0.95, 0.25), (0.98, 0.06, 0.14), (0.02, 0.96, 0.15), (0.5, 0.99, 0.18)):
        yy, xx = np.mgrid[0:h, 0:w].astype(np.float32); dd = np.sqrt(((xx / w - cx) / rad) ** 2 + ((yy / h - cy) / (rad * w / h)) ** 2)
        wash = np.maximum(wash, np.clip(1 - dd, 0, 1) ** 1.5 * (0.6 + 0.4 * blur_noise(h, w, 40, 10)))
    rgb = ink_wash(rgb, wash, 0.34)
    # 칸 테두리 + 머리 먹줄(제목 뒤 붓질)
    for key, (x, y, sw, sh, title) in sections.items():
        rgb = ink_wash(rgb, rough_rect_mask(w, h, x, y, sw, sh, 5, (sum(map(ord, key)) * 37) % 997), 0.7)
        if title and title_marks:
            m = brush_stroke_mask(w, h, [(x + 8 + 250 * t, y + 30 + 4 * math.sin(t * 4 + 1)) for t in np.linspace(0, 1, 12)], 46, 0.6, True, (sum(map(ord, key)) * 53) % 991)
            rgb = ink_wash(rgb, m, 0.72)   # 머리 먹줄(제목 글자는 그 위에 밝은 색)
    # 양털 테두리
    col, shape, sh = wool_border(w, h, inset)
    out = rgb * (1 - shape[..., None]) + col * shape[..., None]
    inner = np.zeros((h, w), np.float32); inner[inset:h - inset, inset:w - inset] = 1
    alpha = np.clip(np.maximum(shape, inner), 0, 1)
    shadow = np.clip(sh * 0.0 + 0, 0, 1)
    img = Image.fromarray(np.concatenate([out.clip(0, 255), alpha[..., None] * 255], -1).astype(np.uint8), "RGBA")
    return img

def small(w, h, fn, scale=2):
    im = Image.new("RGBA", (w * scale * 2, h * scale * 2), (0, 0, 0, 0)); fn(ImageDraw.Draw(im), scale * 2); return im.resize((w * scale, h * scale), Image.LANCZOS)

def paper_tile(w, h, base, border, bw, radius, ink_alpha=200, scale=2):
    """작은 종이 칸(9분할용, 2배 해상도)."""
    W, H = w * scale, h * scale; rgb = paper(W, H, base, tuple(int(c * .95) for c in base), 4)
    img = Image.fromarray(rgb.clip(0, 255).astype(np.uint8)).convert("RGBA")
    m = Image.new("L", (W * 4, H * 4), 0); ImageDraw.Draw(m).rounded_rectangle([0, 0, W * 4 - 1, H * 4 - 1], radius * scale * 4, fill=255); img.putalpha(m.resize((W, H), Image.LANCZOS))
    ol = small(w, h, lambda d, k: d.rounded_rectangle([bw * k / 2, bw * k / 2, w * k - bw * k / 2, h * k - bw * k / 2], radius * k, outline=border + (ink_alpha,), width=int(bw * k)), scale)
    img.alpha_composite(ol); return img

# ════════ 1. 캐릭터 시트 판
sheet = compose_board(SW, SH, 22, SEC, 5); save(sheet, SHEET, "Sheet")

# 아치 초상 틀(먹선 아치 + 안쪽 옅은 하늘빛 번짐) 320×440
def arch():
    w, h = 320, 440; S = 2; W, H = w * S, h * S
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32); cx = W / 2; r0 = W / 2 - 10 * S
    inside = ((yy > r0 + 10 * S) & (np.abs(xx - cx) < r0)) | (((xx - cx) ** 2 + (yy - (r0 + 10 * S)) ** 2) < r0 ** 2)
    inside &= yy < H - 10 * S
    glow = np.exp(-(((xx - cx) / (W * .45)) ** 2 + ((yy - H * .45) / (H * .5)) ** 2))
    rgb = np.ones((H, W, 3), np.float32) * np.array([250, 246, 236], np.float32); rgb = rgb * (1 - .25 * glow[..., None]) + np.array([214, 228, 240], np.float32) * (.25 * glow[..., None])
    rgb += blur_noise(H, W, 20, 4)[..., None] * 6
    a = inside.astype(np.float32) * 0.92
    img = Image.fromarray(np.concatenate([rgb.clip(0, 255), a[..., None] * 255], -1).astype(np.uint8), "RGBA")
    def fn(d, k):
        for off, wd, al in ((6, 4.5, 235), (14, 1.5, 150)):
            o = off; R = w / 2 - o
            d.arc([o * k, o * k, (w - o) * k, (w - 2 * o + 2 * o) * k - (w - 2 * R) * k + 0], 180, 360, fill=INK + (al,), width=int(wd * k))
            d.line([(o * k, (R + o) * k), (o * k, (h - o) * k)], fill=INK + (al,), width=int(wd * k)); d.line([((w - o) * k, (R + o) * k), ((w - o) * k, (h - o) * k)], fill=INK + (al,), width=int(wd * k))
            d.line([(o * k, (h - o) * k), ((w - o) * k, (h - o) * k)], fill=INK + (al,), width=int(wd * k))
        d.polygon([(w / 2 * k, 2 * k), ((w / 2 + 9) * k, 11 * k), (w / 2 * k, 20 * k), ((w / 2 - 9) * k, 11 * k)], fill=(190, 150, 80, 255))
    img.alpha_composite(small(w, h, fn)); save(img, SHEET, "Arch")
arch()

save(paper_tile(48, 48, (250, 245, 233), INK, 2.4, 7), SHEET, "Slot")
save(paper_tile(48, 48, (255, 243, 212), (176, 120, 40), 3.6, 7, 255), SHEET, "SlotSel")
save(paper_tile(100, 132, (251, 247, 238), INK, 2.6, 10), SHEET, "Card")
save(paper_tile(100, 132, (255, 246, 222), (176, 120, 40), 4, 10, 255), SHEET, "CardSel")
save(paper_tile(80, 40, (240, 232, 214), INK, 2.2, 8), SHEET, "Button")

def barframe():   # 먹 붓 테두리 막대(9분할 좌우 14)
    w, h = 120, 24; save(small(w, h, lambda d, k: (d.rounded_rectangle([2 * k, 3 * k, (w - 2) * k, (h - 3) * k], 9 * k, fill=(232, 224, 206, 255), outline=INK + (220,), width=int(2.2 * k)))), SHEET, "BarFrame")
barframe()

def circle(folder, name, size=300, alpha=110):   # 먹선 마법진
    def fn(d, k):
        c = size / 2; col = INK + (alpha,)
        for rr, wd in ((c - 4, 2.4), (c - 16, 1.2), (c * .62, 1.6), (c * .55, .8)): d.ellipse([(c - rr) * k, (c - rr) * k, (c + rr) * k, (c + rr) * k], outline=col, width=int(wd * k))
        for i in range(2):
            pts = [((c + (c - 18) * math.cos(a)) * k, (c + (c - 18) * math.sin(a)) * k) for a in (i * math.pi / 4 + j * math.pi / 2 for j in range(4))]; d.polygon(pts, outline=col, width=int(1.2 * k))
        for j in range(24):
            a = j * math.pi / 12; r1, r2 = c - 16, c - 4 - (6 if j % 2 else 0)
            d.line([((c + r1 * math.cos(a)) * k, (c + r1 * math.sin(a)) * k), ((c + r2 * math.cos(a)) * k, (c + r2 * math.sin(a)) * k)], fill=col, width=int(1 * k))
    save(small(size, size, fn, 2), folder, name)
circle(SHEET, "Circle"); circle(CODEX, "Circle", 520, 70)

def arrow():
    save(small(32, 48, lambda d, k: d.polygon([(8 * k, 6 * k), (26 * k, 24 * k), (8 * k, 42 * k)], fill=INK + (230,))), SHEET, "Arrow")
arrow()

# 가방 분류 아이콘(먹 그림): 전체 · 소모품 · 장비 · 재료 · 중요
def tabicon(name, draw):
    def fn(d, k):
        d.rounded_rectangle([2 * k, 2 * k, 50 * k, 50 * k], 9 * k, fill=(244, 237, 222, 255), outline=INK + (210,), width=int(2 * k)); draw(d, k, INK + (235,))
    save(small(52, 52, fn), SHEET, "Tab" + name)
tabicon("All", lambda d, k, c: [d.rectangle([(14 + (i % 2) * 13) * k, (14 + (i // 2) * 13) * k, (24 + (i % 2) * 13) * k, (24 + (i // 2) * 13) * k], outline=c, width=int(2 * k)) for i in range(4)])
tabicon("Potion", lambda d, k, c: (d.ellipse([15 * k, 22 * k, 37 * k, 42 * k], outline=c, width=int(2.4 * k)), d.rectangle([22 * k, 11 * k, 30 * k, 23 * k], outline=c, width=int(2.4 * k))))
tabicon("Gear", lambda d, k, c: (d.line([(15 * k, 38 * k), (36 * k, 14 * k)], fill=c, width=int(3 * k)), d.line([(14 * k, 28 * k), (24 * k, 38 * k)], fill=c, width=int(3 * k)), d.polygon([(36 * k, 12 * k), (40 * k, 12 * k), (40 * k, 16 * k)], fill=c)))
tabicon("Mat", lambda d, k, c: (d.ellipse([12 * k, 18 * k, 26 * k, 38 * k], outline=c, width=int(2.4 * k)), d.ellipse([24 * k, 16 * k, 40 * k, 36 * k], outline=c, width=int(2.4 * k))))
tabicon("Key", lambda d, k, c: (d.ellipse([12 * k, 14 * k, 26 * k, 28 * k], outline=c, width=int(2.6 * k)), d.line([(24 * k, 26 * k), (39 * k, 40 * k)], fill=c, width=int(3 * k)), d.line([(33 * k, 35 * k), (37 * k, 31 * k)], fill=c, width=int(3 * k))))

# 스킬 상세 창(양털 테두리 종이) 1000×620
pop = compose_board(1000, 620, 20, {"I": (46, 46, 560, 300, "")}, 9, False); save(pop, SHEET, "Popup")

# ════════ 2. 도감
codex = compose_board(CW, CH, 22, CSEC, 13, False); save(codex, CODEX, "Codex")
save(paper_tile(75, 100, (252, 250, 245), (90, 84, 80), 2, 16, 210), CODEX, "Card")
def cardglow():   # 아래쪽 색 번짐(등급·속성 색으로 칠함)
    w, h = 150, 200; yy = np.linspace(0, 1, h)[:, None] * np.ones((1, w)); xx = np.linspace(-1, 1, w)[None, :] * np.ones((h, 1))
    a = np.clip((yy - 0.35) / 0.65, 0, 1) ** 1.6 * (1 - 0.35 * xx ** 2); a *= 0.85 + 0.3 * blur_noise(h, w, 10, 3)
    img = Image.fromarray(np.dstack([np.full((h, w), 255), np.full((h, w), 255), np.full((h, w), 255), np.clip(a, 0, 1) * 255]).astype(np.uint8), "RGBA")
    m = Image.new("L", (w * 4, h * 4), 0); ImageDraw.Draw(m).rounded_rectangle([8, 8, w * 4 - 8, h * 4 - 8], 64, fill=255); img.putalpha(Image.fromarray(np.minimum(np.asarray(img.getchannel("A")), np.asarray(m.resize((w, h), Image.LANCZOS)))))
    save(img, CODEX, "CardGlow")
cardglow()
save(paper_tile(80, 28, (236, 230, 218), (90, 84, 80), 1.6, 14, 200), CODEX, "Chip")

# ════════ 3. 기절(경직) 효과: 주황 폭발 + 둥근 굵은 글자
def stun_burst():
    n = 512; img = Image.new("RGBA", (n, n), (0, 0, 0, 0)); c = n / 2; r = np.random.default_rng(4)
    glow = Image.new("L", (n, n), 0); ImageDraw.Draw(glow).ellipse([c - 150, c - 110, c + 150, c + 110], fill=255); glow = glow.filter(ImageFilter.GaussianBlur(50))
    base = Image.new("RGBA", (n, n), (255, 190, 90, 0)); base.putalpha(glow.point(lambda v: int(v * .8))); img.alpha_composite(base)
    rays = Image.new("RGBA", (n * 2, n * 2), (0, 0, 0, 0)); d = ImageDraw.Draw(rays)
    for i in range(60):
        a = r.uniform(0, 2 * math.pi); L = r.uniform(.45, 1.0) * c * 2 * .95; w0 = r.uniform(6, 16)
        p0 = (c * 2 + math.cos(a) * 70, c * 2 + math.sin(a) * 50); p1 = (c * 2 + math.cos(a) * L, c * 2 + math.sin(a) * L * .78)
        nx, ny = -math.sin(a), math.cos(a); col = (255, int(r.uniform(140, 190)), 40, int(r.uniform(150, 240)))
        d.polygon([(p0[0] + nx * w0, p0[1] + ny * w0), (p1[0], p1[1]), (p0[0] - nx * w0, p0[1] - ny * w0)], fill=col)
        if r.random() < .5:   # 갈라진 잔가지
            t = r.uniform(.4, .8); q = (p0[0] + (p1[0] - p0[0]) * t, p0[1] + (p1[1] - p0[1]) * t); b = a + r.uniform(-.5, .5)
            d.line([q, (q[0] + math.cos(b) * L * .25, q[1] + math.sin(b) * L * .2)], fill=col, width=3)
    rays = rays.resize((n, n), Image.LANCZOS); grit = Image.fromarray((np.clip(blur_noise(n, n, 2, .5) * 2 + .4, 0, 1) * 255).astype(np.uint8))   # 거친 붓 자국
    rays.putalpha(Image.fromarray((np.asarray(rays.getchannel("A")).astype(np.float32) * np.asarray(grit).astype(np.float32) / 255).astype(np.uint8)))
    img.alpha_composite(rays); save(img, FX, "StunBurst")
stun_burst()

def stun_text():
    w, h = 520, 220; f = ImageFont.truetype(NANUM + "NanumSquareRoundEB.ttf", 150); txt = "기절"
    m = Image.new("L", (w, h), 0); ImageDraw.Draw(m).text((w / 2, h / 2), txt, font=f, fill=255, anchor="mm")
    out_m = m.filter(ImageFilter.MaxFilter(15)); glow = out_m.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(10))
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    g = Image.new("RGBA", (w, h), (255, 170, 60, 0)); g.putalpha(glow.point(lambda v: int(v * .75))); img.alpha_composite(g)
    o = Image.new("RGBA", (w, h), (196, 92, 22, 255)); o.putalpha(out_m); img.alpha_composite(o)                  # 진한 주황 테두리
    o2 = Image.new("RGBA", (w, h), (255, 228, 170, 255)); o2.putalpha(m.filter(ImageFilter.MaxFilter(5))); img.alpha_composite(o2)   # 밝은 안쪽 테
    yy = np.linspace(0, 1, h)[:, None]; fill = np.ones((h, w, 3), np.float32) * (np.array([255, 248, 230]) * (1 - yy[..., None]) + np.array([250, 196, 120]) * yy[..., None])
    fl = Image.fromarray(np.dstack([fill.clip(0, 255), np.asarray(m.filter(ImageFilter.MinFilter(3)))]).astype(np.uint8), "RGBA"); img.alpha_composite(fl)
    hl = Image.new("RGBA", (w, h), (255, 255, 255, 0)); hm = np.asarray(m).astype(np.float32) * (yy < .42); hl.putalpha(Image.fromarray((hm * .45).astype(np.uint8))); img.alpha_composite(hl)
    save(img, FX, "StunText")
stun_text()
