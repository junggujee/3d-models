"""대장간 작업대(UI/CraftBench.cs) 배치 미리보기 — 좌표는 C# 과 같음(패널 1180×760, 작업대 root = 패널 왼쪽 위 (40, 196)).
출력: previews/craft_bench_mock.png(레이저 블레이더 X 모양을 놓은 모습) · previews/craft_showcase_mock.png(특별 제작 연출 한 장면)
아이콘은 Unity 에서 코드로 그리므로 여기서는 아이템 색 동그라미 + 이름 첫 글자로 대신함."""
import os, math
from PIL import Image, ImageDraw, ImageFont, ImageFilter
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); OUT = os.path.join(ROOT, "previews")
FONT = os.path.join(ROOT, "unity", "Assets", "Resources", "Fonts", "NanumMyeongjoExtraBold.ttf")
def F(n): return ImageFont.truetype(FONT, n)
W, H = 1180, 760; OX, OY = 40, 216
INK = (14, 12, 10); GOLD = (204, 166, 92); CREAM = (235, 222, 194); MUTED = (158, 145, 120); HOT = (255, 235, 166)
ITEM = {"bat_alloy": ("파란 방망이 조각", (64, 153, 242)), "bear_bone": ("사골곰뼈", (245, 235, 209)), "rainbow_pearl": ("무지개 진주", (242, 209, 255)),
        "peanut_piece": ("땅콩빵 조각", (242, 224, 168)), "shard_star": ("별조각", (255, 230, 102)), "dust_fire": ("불씨 가루", (255, 120, 40)),
        "feather_silver": ("은빛 깃털", (200, 210, 225)), "honey": ("꿀", (255, 191, 51)), "peanut_bread": ("땅콩빵", (219, 168, 97)),
        "mandu_core": ("맨두의 진동 코어", (242, 204, 51)), "laser_blader": ("레이저 블레이더", (89, 191, 255))}
BAG = [("bat_alloy", 4), ("bear_bone", 2), ("rainbow_pearl", 1), ("peanut_piece", 14), ("shard_star", 9), ("dust_fire", 6), ("feather_silver", 2), ("honey", 1), ("mandu_core", 1)]
GRID = ["bat_alloy", None, "bear_bone", None, "rainbow_pearl", None, "bear_bone", None, "bat_alloy"]
BOOK = ["레이저 블레이더", "땅콩빵", "꿀바른 땅콩빵", "회전 쌍검", "베개 방패", "파란 머리띠", "용궁 진주 반지", "진동 부적"]

def panel():
    im = Image.new("RGB", (W + 80, H + 80), (40, 44, 50)); d = ImageDraw.Draw(im)
    d.rounded_rectangle((40, 40, 40 + W, 40 + H), 18, fill=(28, 22, 18), outline=GOLD, width=4); d.rounded_rectangle((52, 52, 28 + W, 28 + H), 14, outline=(120, 96, 52), width=2)
    d.text((40 + W / 2, 40 + 60), "마을 대장간", font=F(46), fill=GOLD, anchor="mm"); d.line((140, 40 + 106, W - 60, 40 + 106), fill=(150, 120, 60), width=3)
    d.text((40 + W / 2, 40 + 128), "풍선고래 대장장이", font=F(24), fill=MUTED, anchor="mm"); d.text((40 + W - 40, 40 + 58), "골드  1,240", font=F(26), fill=HOT, anchor="rm")
    for k, (lab, on) in enumerate((("제  작", True), ("강  화", False))):
        cx = 40 + W / 2 + (-200 + 220 * k) + 90; d.rounded_rectangle((cx - 90, 40 + 158, cx + 90, 40 + 204), 8, fill=(60, 48, 34), outline=GOLD if on else (90, 76, 50), width=2)
        d.text((cx, 40 + 181), lab, font=F(28), fill=HOT if on else MUTED, anchor="mm")
    return im, d
def P(x, y): return (40 + OX + x, 40 + OY + y)     # root 좌표(오른쪽 + · 아래 +) → 그림
def icon(d, cx, cy, iid, r, alpha=255):
    name, col = ITEM[iid]; d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=col + (alpha,), outline=(30, 24, 18, alpha), width=3)
    d.text((cx, cy), name[0], font=F(int(r * 0.9)), fill=(30, 24, 18, alpha), anchor="mm")
def bench(d, im, grid, result=True):
    cell, gap = 92, 8; gx, gy = P(30, 6); gw = cell * 3 + gap * 2
    d.rectangle((gx - 10, gy - 10, gx + gw + 10, gy + gw + 10), fill=(58, 42, 28))
    for i in range(9):
        r, c = divmod(i, 3); x, y = gx + c * (cell + gap), gy + r * (cell + gap)
        d.rounded_rectangle((x, y, x + cell, y + cell), 8, fill=(236, 226, 205), outline=(140, 116, 80), width=2)
        if grid[i]: icon(d, x + cell / 2, y + cell / 2, grid[i], 30)
    mid = gy + gw / 2; ax = gx + gw + 62 - 30
    d.rectangle((ax - 32, mid - 8, ax + 32, mid + 8), fill=GOLD); hx = gx + gw + 98 - 30; d.polygon([(hx - 15, mid - 20), (hx + 15, mid), (hx - 15, mid + 20)], fill=GOLD)
    rx = gx + gw + 190 - 30; d.rounded_rectangle((rx - 62, mid - 62, rx + 62, mid + 62), 10, fill=HOT if result else (236, 226, 205), outline=GOLD, width=3)
    if result:
        icon(d, rx, mid, "laser_blader", 42); d.text((rx, mid + 80), "레이저 블레이더", font=F(24), fill=CREAM, anchor="mm"); d.text((rx, mid + 108), "눌러서 만들기", font=F(22), fill=CREAM, anchor="mm")
    hx0, hy0 = P(30, gw + 18); d.multiline_text((hx0, hy0), "재료 클릭 → 칸 클릭: 놓기 · 칸 우클릭: 비우기\n재료 우클릭: 빈칸 모두 채우기", font=F(18), fill=MUTED, spacing=6)
    bx, by = P(30, gw + 66); d.text((bx, by), "제작법", font=F(26), fill=GOLD)
    for k, name in enumerate(BOOK):
        x, y = P(30 + (k % 4) * 140, gw + 104 + (k // 4) * 46); d.rounded_rectangle((x, y, x + 136, y + 40), 6, fill=(70, 58, 44), outline=(120, 98, 64), width=2)
        d.ellipse((x + 6, y + 4, x + 38, y + 36), fill=(180, 170, 150)); fs = 15 if len(name) <= 6 else 12; d.text((x + 40, y + 20), name, font=F(fs), fill=CREAM, anchor="lm")
    lx, ly = P(600, 0); d.text((lx, ly), "가방 재료", font=F(26), fill=GOLD)
    placed = {}
    for g in grid:
        if g: placed[g] = placed.get(g, 0) + 1
    for i in range(30):
        r, c = divmod(i, 6); x, y = P(600 + c * 77, 42 + r * 77)
        held = i == 0
        d.rounded_rectangle((x, y, x + 70, y + 70), 7, fill=HOT if held else (150, 138, 118), outline=(110, 92, 64), width=2)
        if i < len(BAG):
            iid, n = BAG[i]; av = n - placed.get(iid, 0); icon(d, x + 35, y + 33, iid, 22, 255 if av > 0 else 110)
            d.text((x + 64, y + 66), str(av), font=F(18), fill=(255, 255, 255), anchor="rd", stroke_width=2, stroke_fill=(30, 24, 18))
    tx, ty = P(600, 42 + 5 * 77 + 6); d.text((tx, ty), "손에 든 재료: 파란 방망이 조각  (남은 2)", font=F(22), fill=HOT)

im, d = panel(); bench(d, im, GRID); d.text((40 + W / 2, 40 + H - 28), "Esc  닫기", font=F(22), fill=MUTED, anchor="mm")
im.save(os.path.join(OUT, "craft_bench_mock.png")); print("✔ craft_bench_mock")

# 특별 제작 연출 한 장면(약 1.9초 — 빛줄기 속에서 결과가 떠오름)
im, d = panel(); bench(d, im, [None] * 9, result=False)
ov = Image.new("RGBA", im.size, (0, 0, 0, 0)); o = ImageDraw.Draw(ov)
o.rectangle(P(-30, -30) + P(1130, 550), fill=(5, 10, 20, 184))
cx, cy = P(550, 260)
for k in range(14):
    a = math.radians(k * 360 / 14 + 25); hue = k / 14
    col = tuple(int(255 * v) for v in __import__("colorsys").hsv_to_rgb(hue, 0.45, 1.0)) + (97,)
    dx, dy = math.cos(a), math.sin(a); nx, ny = -dy, dx
    o.polygon([(cx + nx * 8, cy + ny * 8), (cx - nx * 8, cy - ny * 8), (cx - nx * 8 + dx * 260, cy - ny * 8 + dy * 260), (cx + nx * 8 + dx * 260, cy + ny * 8 + dy * 260)], fill=col)
im = Image.alpha_composite(im.convert("RGBA"), ov); g = Image.new("RGBA", im.size, (0, 0, 0, 0)); gd = ImageDraw.Draw(g)
gd.ellipse((cx - 150, cy - 150, cx + 150, cy + 150), outline=(140, 230, 255, 150), width=14); g = g.filter(ImageFilter.GaussianBlur(6)); im = Image.alpha_composite(im, g)
d = ImageDraw.Draw(im); icon(d, cx, cy, "laser_blader", 110)
d.text((cx, cy + 190), "레이저 블레이더 완성!", font=F(54), fill=HOT, anchor="mm", stroke_width=3, stroke_fill=(90, 60, 20))
im.convert("RGB").save(os.path.join(OUT, "craft_showcase_mock.png")); print("✔ craft_showcase_mock")
