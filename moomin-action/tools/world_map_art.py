"""
월드맵 그림 완성: blender/render_world_map.py 가 만든 color/normal/height 렌더 → 일러스트 지도.
  1) 그림체: 부드럽게 뭉갠 채색(색 단계 줄이기) + 채도·따뜻한 색감 + 법선이 꺾이는 곳에 갈색 외곽선(건물·산이 또렷하게)
  2) 주요 장소 강조: 장소 주변은 밝게 빛나고, 나머지는 살짝 어둡고 바랜 색(스포트라이트)
  3) 양피지 바탕 · 바깥으로 갈수록 종이색으로 바래는 가장자리 · 테두리 · 나침반
  4) 장소 핀 아이콘 5종(성 · 광장 · 산지 · 마을) + 접힌 종이 뒷면 + 플레이어 표시
  python3 tools/world_map_art.py <렌더 폴더>   →  unity/Assets/Resources/UI/WorldMap/*  +  previews/world_map.png
"""
import json, math, os, sys
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = sys.argv[1] if len(sys.argv) > 1 else "map_render"
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "WorldMap"); os.makedirs(OUT, exist_ok=True)
FONT = "/usr/share/fonts/truetype/nanum/NanumSquareB.ttf"

def load(n): return np.asarray(Image.open(os.path.join(SRC, n + ".png")).convert("RGBA"), np.float32) / 255
def blur(a, r): return np.asarray(Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(r)), np.float32) / 255
def hexc(h): h = h.lstrip('#'); return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)], np.float32)
def hash2(x, y):
    x = x.astype(np.int64); y = y.astype(np.int64); h = (x * 374761393 + y * 668265263) & 0xffffffff
    h = ((h ^ (h >> 13)) * 1274126177) & 0xffffffff; return ((h ^ (h >> 16)) & 0xffff) / 65535.0
def vnoise(x, y):
    xi, yi = np.floor(x), np.floor(y); tx, ty = x - xi, y - yi; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty)
    a = hash2(xi, yi) + (hash2(xi + 1, yi) - hash2(xi, yi)) * tx; b = hash2(xi, yi + 1) + (hash2(xi + 1, yi + 1) - hash2(xi, yi + 1)) * tx
    return a + (b - a) * ty
def fbm(x, y): return vnoise(x, y) * .5 + vnoise(x * 2.1, y * 2.1) * .3 + vnoise(x * 4.3, y * 4.3) * .2

meta = json.load(open(os.path.join(SRC, "map_meta.json"), encoding="utf-8"))
col, nrm, hgt = load("color"), load("normal"), load("height")
H, W = col.shape[:2]; ys, xs = np.mgrid[0:H, 0:W].astype(np.float32); U, V = xs / W, 1 - ys / H

# ── 1) 그림체 ─────────────────────────────
rgb = col[..., :3]
img = Image.fromarray((rgb * 255).astype(np.uint8)).filter(ImageFilter.MedianFilter(5)).filter(ImageFilter.SMOOTH_MORE)
rgb = np.asarray(img, np.float32) / 255
lum = rgb @ np.array([.3, .59, .11], np.float32)
rgb = lum[..., None] + (rgb - lum[..., None]) * 1.35                        # 채도 올리기
rgb = np.clip(rgb * np.array([1.06, 1.0, .9]) + np.array([.02, .01, -.01]), 0, 1)   # 따뜻하게
rgb = np.round(rgb * 14) / 14 * .5 + rgb * .5                               # 색 단계 줄이기(일러스트 느낌, 반만)
n = nrm[..., :3] * 2 - 1; edge = np.zeros((H, W), np.float32)
for dy, dx in ((0, 1), (1, 0), (1, 1), (1, -1)):
    edge = np.maximum(edge, np.linalg.norm(n - np.roll(np.roll(n, dy, 0), dx, 1), axis=2))
hh = hgt[..., 0]; edge = np.maximum(edge, np.abs(hh - np.roll(hh, 1, 0)) * 25 + np.abs(hh - np.roll(hh, 1, 1)) * 25)
line = np.clip((edge - .35) * 1.6, 0, 1) * col[..., 3]
line = blur(line, .6) * .75
rgb = rgb * (1 - line[..., None]) + hexc('#3A2614') * line[..., None]

# ── 2) 주요 장소 강조(스포트라이트) ──────────
spot = np.zeros((H, W), np.float32); aspect = W / H
for lm in meta["landmarks"]:
    u, v = lm["uv"]; r = .11 if lm["id"] in ("east", "west") else .085
    d = np.hypot((U - u) * aspect, V - v); spot = np.maximum(spot, np.exp(-(d / r) ** 2))
dim = .84 + .16 * spot
gray = (rgb @ np.array([.3, .59, .11], np.float32))[..., None]
rgb = (rgb * (.72 + .28 * spot[..., None]) + (gray * .9 + hexc('#C9B48A') * .1) * (.28 - .28 * spot[..., None])) * dim[..., None]
rgb = rgb + hexc('#FFE2A0') * (blur(spot, 30) * .12)[..., None]              # 장소 주변 금빛

# ── 3) 양피지 · 가장자리 바램 · 테두리 · 나침반 ──
paper = hexc('#E8D6AE')[None, None] * (.9 + .12 * fbm(xs / 140, ys / 140))[..., None] - hexc('#4A3010')[None, None] * (fbm(xs / 30 + 9, ys / 30) * .08)[..., None]
edge_d = np.minimum(np.minimum(U, 1 - U) * W / H, np.minimum(V, 1 - V))           # 가장자리까지 거리(세로 기준)
fade = np.clip((.11 - edge_d) / .08, 0, 1) + (fbm(xs / 70, ys / 70) - .5) * .5 * np.clip((.16 - edge_d) / .08, 0, 1); fade = np.clip(fade, 0, 1)
rgb = rgb * (1 - fade[..., None] * .85) + paper * (fade[..., None] * .85)
rgb = rgb * (1 - .05 * fbm(xs / 25, ys / 25))[..., None]                     # 종이 결
m = 26; inside = (xs > m) & (xs < W - m) & (ys > m) & (ys < H - m)
rgb[~inside] = hexc('#4A2F18')
for t, c in ((m + 6, '#C8A25A'), (m + 12, '#6B4A26')):
    band = (np.abs(np.minimum(np.minimum(xs - t, W - t - xs), np.minimum(ys - t, H - t - ys))) < 1.6); rgb[band] = hexc(c)
out = Image.fromarray((np.clip(rgb, 0, 1) * 255).astype(np.uint8)).convert("RGBA")
d = ImageDraw.Draw(out)
def compass(d, x, y, r):
    for k in range(8):
        a = k * math.pi / 4; L = r if k % 2 == 0 else r * .55; w = r * .16
        p = [(x + math.sin(a) * L, y - math.cos(a) * L), (x + math.sin(a + math.pi / 2) * w, y - math.cos(a + math.pi / 2) * w), (x, y), (x + math.sin(a - math.pi / 2) * w, y - math.cos(a - math.pi / 2) * w)]
        d.polygon(p[:3], fill=(122, 82, 40)); d.polygon([p[0], p[2], p[3]], fill=(214, 178, 100) if k else (190, 40, 40))
    d.ellipse((x - r * .18, y - r * .18, x + r * .18, y + r * .18), fill=(240, 220, 170), outline=(90, 60, 30), width=3)
    f = ImageFont.truetype(FONT, int(r * .32)); d.text((x, y - r - r * .25), "N", font=f, fill=(90, 50, 25), anchor="mm")
compass(d, W - 150, H - 170, 92)
out.save(os.path.join(OUT, "WorldMap.png"), optimize=True); print("✔ WorldMap", out.size)

# 접힌 종이 뒷면(접는 동안 보이는 면)
back = paper * (.82 + .08 * fbm(xs / 60 + 3, ys / 60))[..., None]; back[~inside] = hexc('#4A2F18')
Image.fromarray((np.clip(back, 0, 1) * 255).astype(np.uint8)).resize((512, 320), Image.LANCZOS).save(os.path.join(OUT, "WorldMapBack.png")); print("✔ WorldMapBack")

# ── 4) 장소 핀 아이콘(금테 원 + 그림) ────────
S = 192
def pin(name, draw_fn, ring='#D8B25E', fill='#2E2015'):
    im = Image.new("RGBA", (S, S)); dr = ImageDraw.Draw(im); c = S / 2
    dr.ellipse((8, 8, S - 8, S - 8), fill=(20, 12, 6, 255)); dr.ellipse((14, 14, S - 14, S - 14), fill=hexc_t(fill), outline=hexc_t(ring), width=9)
    dr.ellipse((26, 26, S - 26, S - 26), outline=(140, 100, 50, 255), width=2); draw_fn(dr, c)
    im.filter(ImageFilter.SMOOTH).save(os.path.join(OUT, "Pin_" + name + ".png")); print("✔ Pin", name)
def hexc_t(h): return tuple(int(v * 255) for v in hexc(h)) + (255,)
GOLD, CREAM, STONE = (240, 206, 120, 255), (246, 232, 200, 255), (200, 196, 210, 255)
def castle_ic(d, c):
    d.rectangle((c - 44, c - 6, c + 44, c + 48), fill=STONE); d.rectangle((c - 16, c - 40, c + 16, c + 48), fill=CREAM)
    for x in (c - 44, c + 28): d.rectangle((x, c - 26, x + 16, c + 48), fill=CREAM); d.polygon([(x - 4, c - 26), (x + 8, c - 50), (x + 20, c - 26)], fill=(90, 110, 170, 255))
    d.polygon([(c - 20, c - 40), (c, c - 72), (c + 20, c - 40)], fill=(90, 110, 170, 255)); d.rectangle((c - 10, c + 20, c + 10, c + 48), fill=(60, 40, 25, 255))
def plaza_ic(d, c):
    d.ellipse((c - 52, c + 8, c + 52, c + 50), fill=STONE); d.ellipse((c - 38, c + 14, c + 38, c + 42), fill=(110, 170, 230, 255))
    d.rectangle((c - 8, c - 30, c + 8, c + 28), fill=CREAM); d.ellipse((c - 24, c - 40, c + 24, c - 22), fill=(110, 170, 230, 255))
    for a in (-1, 1): d.arc((c - 40 + a * 10, c - 60, c + 40 + a * 10, c + 20), 200 if a < 0 else 270, 270 if a < 0 else 340, fill=(160, 210, 250, 255), width=5)
def mountain_ic(d, c):
    d.polygon([(c - 62, c + 44), (c - 14, c - 46), (c + 34, c + 44)], fill=(120, 116, 112, 255)); d.polygon([(c - 10, c + 44), (c + 26, c - 22), (c + 64, c + 44)], fill=(96, 92, 90, 255))
    d.polygon([(c - 14, c - 46), (c - 30, c - 16), (c - 18, c - 22), (c - 8, c - 12), (c + 2, c - 20)], fill=(250, 250, 255, 255)); d.polygon([(c + 26, c - 22), (c + 16, c - 4), (c + 32, c - 8)], fill=(250, 250, 255, 255))
def village_ic(roof):
    def f(d, c):
        for dx, s in ((-26, .8), (22, 1.0)):
            w = 30 * s; x = c + dx; d.rectangle((x - w, c, x + w, c + 46), fill=CREAM); d.polygon([(x - w - 8, c + 2), (x, c - 40 * s), (x + w + 8, c + 2)], fill=roof)
            d.rectangle((x - 7, c + 22, x + 7, c + 46), fill=(90, 60, 35, 255))
    return f
pin("castle", castle_ic); pin("plaza", plaza_ic); pin("highland", mountain_ic)
pin("east", village_ic((196, 72, 52, 255))); pin("west", village_ic((150, 90, 60, 255)))
# 플레이어 표시(앞을 가리키는 화살 + 고리), 위쪽 = 바라보는 방향
pl = Image.new("RGBA", (128, 128)); dp = ImageDraw.Draw(pl)
dp.ellipse((10, 10, 118, 118), outline=(255, 240, 180, 200), width=6); dp.polygon([(64, 8), (100, 104), (64, 82), (28, 104)], fill=(255, 214, 90, 255), outline=(60, 30, 10, 255))
pl.save(os.path.join(OUT, "PlayerMarker.png")); print("✔ PlayerMarker")
json.dump(meta, open(os.path.join(OUT, "WorldMapData.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1); print("✔ WorldMapData.json")
