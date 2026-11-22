"""
월드맵 미리보기(Unity 없이): UI/WorldMapView.cs 와 같은 배치(지도 높이 900, 세 폭, 핀 아이콘 위 46px · 이름표 아래 22px)와 같은 펼치기 곡선.
  python3 tools/world_map_mock.py  →  previews/world_map.png(다 펼친 화면) · previews/world_map_unfold.png(펼치는 6장면)
"""
import json, math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); D = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "WorldMap")
FONT = "/usr/share/fonts/truetype/nanum/NanumSquareB.ttf"
MAP, BACK = Image.open(os.path.join(D, "WorldMap.png")).convert("RGBA"), Image.open(os.path.join(D, "WorldMapBack.png")).convert("RGBA")
META = json.load(open(os.path.join(D, "WorldMapData.json"), encoding="utf-8"))
H = 900; W = int(H * MAP.width / MAP.height); SW, SH = 1920, 1080
LAYOUT = {"highland": ((70, 12), True), "plaza": ((0, 0), True), "west": ((-60, 0), False), "east": ((60, 0), False)}                      # WorldMapView.Layout 과 같게

def seg(u, a, b): return min(1, max(0, (u - a) / (b - a)))
def ease(x): return 1 - (1 - x) ** 3
def text(im, xy, s, size, fill, anchor="mm"):
    ImageDraw.Draw(im).text(xy, s, font=ImageFont.truetype(FONT, size), fill=fill, anchor=anchor, stroke_width=3, stroke_fill=(0, 0, 0))

def panel_img(i, angle):
    """폭 i 를 각도만큼 접은 모습: 가로 폭 × |cos|, 90° 넘으면 뒷면(좌우 반전), 기울수록 그늘·조금 커짐."""
    w3 = W // 3; src = (BACK.resize((W, H)) if abs(angle) > 90 else MAP.resize((W, H))).crop((i * w3, 0, (i + 1) * w3, H))
    if abs(angle) > 90: src = src.transpose(Image.FLIP_LEFT_RIGHT)
    k = abs(math.sin(math.radians(angle))); cw = max(1, int(w3 * abs(math.cos(math.radians(angle))))); ch = int(H * (1 + .05 * k))
    im = src.resize((cw, ch), Image.LANCZOS); sh = Image.new("RGBA", im.size, (0, 0, 0, int(255 * .55 * k))); im.alpha_composite(sh); return im, cw, ch

def frame(u, bg):
    """u: 0 접혀 숨음 ~ 1 다 펼침(WorldMapView.Apply 와 같은 곡선)."""
    img = bg.copy(); a = seg(u, 0, .22); l, r = ease(seg(u, .2, .52)), ease(seg(u, .42, .74))
    img.alpha_composite(Image.new("RGBA", img.size, (0, 0, 0, int(255 * .65 * a))))
    s = .72 + .28 * ease(a); cy = SH / 2 + 10 + 140 * (1 - ease(a)); sheet = Image.new("RGBA", (W + 40, int(H * 1.1)))
    ox, oy = 20, int(H * .05); w3 = W // 3
    def put(i, angle):
        im, cw, ch = panel_img(i, angle)
        hinge = ox + w3 * (1 if i == 0 else 2)            # 접는 선
        if i == 1: x = ox + w3
        elif i == 0: x = hinge - cw if abs(angle) <= 90 else hinge
        else: x = hinge if abs(angle) <= 90 else hinge - cw
        sheet.alpha_composite(im, (int(x), oy - (ch - H) // 2))
    put(1, 0); put(0, 180 * (1 - l)); put(2, -180 * (1 - r))
    for idx, lm in enumerate(META["landmarks"]):          # 핀
        k = seg(u, .72 + idx * .04, .82 + idx * .04)
        if k <= 0: continue
        pop = 1.2 * k / .7 if k < .7 else 1.2 - .2 * (k - .7) / .3; (dx, dy), above = LAYOUT.get(lm["id"], ((0, 0), False))
        px, py = ox + lm["uv"][0] * W + dx, oy + (1 - lm["uv"][1]) * H - dy
        g = Image.new("RGBA", (150, 150)); ImageDraw.Draw(g).ellipse((25, 25, 125, 125), fill=(255, 215, 100, 120)); g = g.filter(ImageFilter.GaussianBlur(18))
        sheet.alpha_composite(g, (int(px - 75), int(py - 75)))
        ic = Image.open(os.path.join(D, "Pin_" + lm["id"] + ".png")).resize((max(1, int(72 * pop)),) * 2, Image.LANCZOS); sheet.alpha_composite(ic, (int(px - ic.width / 2), int(py - 46 - ic.height / 2)))
        ImageDraw.Draw(sheet).rectangle((px - 2, py - 14, px + 2, py + 2), fill=(216, 178, 90))
        ly = py - 106 if above else py + 22; lw = (40 + len(lm["name"]) * 26) * pop
        ImageDraw.Draw(sheet).rounded_rectangle((px - lw / 2, ly - 20 * pop, px + lw / 2, ly + 20 * pop), 8, fill=(30, 22, 14, 235), outline=(200, 160, 80), width=3)
        if pop > .3: text(sheet, (px, ly), lm["name"], int(26 * pop), (255, 222, 128))
    if u > .8:
        pm = Image.open(os.path.join(D, "PlayerMarker.png")).resize((46, 46)); sb = META["spawnBlender"]; g = META["ground"]
        uu, vv = g["origin"][0] + g["perX"][0] * sb[0] + g["perY"][0] * sb[1], g["origin"][1] + g["perX"][1] * sb[0] + g["perY"][1] * sb[1]
        sheet.alpha_composite(pm, (int(ox + uu * W - 23), int(oy + (1 - vv) * H - 23)))
    sheet = sheet.resize((int(sheet.width * s), int(sheet.height * s)), Image.LANCZOS)
    img.alpha_composite(sheet, (int(SW / 2 - sheet.width / 2), int(cy - sheet.height / 2)))
    if u > .7:
        text(img, (SW / 2, SH / 2 - H / 2 - 24), "세계 지도", 46, (255, 222, 128)); text(img, (SW / 2, SH / 2 + H / 2 + 48), "M · Esc   지도 접기", 24, (232, 220, 190))
    return img

bg = Image.open(os.path.join(ROOT, "previews", "map_town.png")).convert("RGB").resize((SW, SH)).filter(ImageFilter.GaussianBlur(4)).convert("RGBA")
full = frame(1.0, bg); full.convert("RGB").save(os.path.join(ROOT, "previews", "world_map.png")); print("✔ world_map.png")
steps = [(.12, "1 떠오름"), (.3, "2 왼쪽 폭"), (.46, "3 왼쪽 거의 다"), (.6, "4 오른쪽 폭"), (.8, "5 핀 등장"), (1.0, "6 완성")]
strip = Image.new("RGB", (3 * 640, 2 * 380), (20, 16, 12))
for i, (u, label) in enumerate(steps):
    f = frame(u, bg).convert("RGB").resize((640, 360)); strip.paste(f, ((i % 3) * 640, (i // 3) * 380 + 20)); text(strip, ((i % 3) * 640 + 70, (i // 3) * 380 + 14), label, 22, (255, 222, 128))
strip.save(os.path.join(ROOT, "previews", "world_map_unfold.png")); print("✔ world_map_unfold.png")
