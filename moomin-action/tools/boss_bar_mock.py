"""보스바 미리보기(Unity 없이): UI/BossBar.cs 와 같은 좌표(화면 아래 가운데 1040×100, y 178)·그림으로 평소 / 맞은 직후 / 격파 3장면.
  python3 tools/boss_bar_mock.py  →  previews/boss_bar_mock.png"""
import os
from PIL import Image, ImageDraw, ImageFont, ImageFilter
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); B = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Bars"); S = os.path.join(ROOT, "unity", "Assets", "Resources", "StatusIcons")
FONT = "/usr/share/fonts/truetype/nanum/NanumSquareB.ttf"
def art(n, d=B): return Image.open(os.path.join(d, n + ".png")).convert("RGBA")
def tint(im, rgb, a=1.0):
    r, g, b, al = im.split(); out = Image.merge("RGB", [Image.eval(c, lambda v, k=k: v * k // 255) for c, k in zip((r, g, b), rgb)]).convert("RGBA"); out.putalpha(al.point(lambda v: int(v * a))); return out
def nine(img, w, h, sb, db):
    W, H = img.size; out = Image.new("RGBA", (w, h))
    xs = [(0, sb, 0, db), (sb, W - sb, db, w - db), (W - sb, W, w - db, w)]; ys = [(0, sb, 0, db), (sb, H - sb, db, h - db), (H - sb, H, h - db, h)]
    for a, b, c, d in xs:
        for e, f, g, h2 in ys:
            if d > c and h2 > g: out.alpha_composite(img.crop((a, e, b, f)).resize((d - c, h2 - g), Image.LANCZOS), (c, g))
    return out
def text(im, xy, s, size, fill, anchor="ls", stroke=3):
    ImageDraw.Draw(im).text(xy, s, font=ImageFont.truetype(FONT, size), fill=fill, anchor=anchor, stroke_width=stroke, stroke_fill=(0, 0, 0))
FRAME, CAP, EMB, FILL, BACK = art("BossFrame"), art("BossCap"), art("BossEmblem_Whale"), art("Fill"), art("Back")
def bossbar(img, ox, oy, ratio, chunk=None, stag=.35, defeat=False, icons=("Sinking", "Tremor")):
    """ox, oy = 보스바 왼쪽 아래(화면 좌표, 아래가 +)."""
    W, H = 1040, 100; X0 = ox; Y = lambda y: oy - y                  # Unity 아래 기준 y → 화면
    bx, bw, by, bh = X0 + 30 + 60, W - 120, 18, 40                    # Bar: 좌우 60 안쪽, y 18~58
    ix, iw, ih = bx + 7, bw - 14, bh - 14
    img.alpha_composite(tint(BACK.resize((iw, ih)), (150, 150, 180)), (ix, Y(by + bh - 7)))
    if chunk and chunk > ratio: img.alpha_composite(tint(FILL.resize((int(iw * (chunk - ratio)), ih)), (242, 38, 26)), (ix + int(iw * ratio), Y(by + bh - 7)))
    if ratio > 0: img.alpha_composite(tint(FILL.resize((max(1, int(iw * ratio)), ih)), (56, 148, 250)), (ix, Y(by + bh - 7)))
    if defeat: img.alpha_composite(Image.new("RGBA", (iw, ih), (255, 255, 255, 120)), (ix, Y(by + bh - 7)))
    img.alpha_composite(nine(FRAME, bw + 12, bh + 12, 26, 13), (bx - 6, Y(by + bh + 6)))
    sx, sw = X0 + 30 + 90, W - 180                                     # 경직 게이지 y 6~13
    img.alpha_composite(BACK.resize((sw, 7)), (sx, Y(13))); img.alpha_composite(Image.new("RGBA", (int(sw * stag), 7), (242, 204, 77, 255)), (sx, Y(13)))
    img.alpha_composite(CAP.resize((64, 80), Image.LANCZOS), (X0 + W - 62, Y(38 + 40)))
    img.alpha_composite(EMB.resize((104, 104), Image.LANCZOS), (X0 + 58 - 52, Y(40 + 52)))
    text(img, (X0 + 120, Y(64)), "고래고래", 32, (240, 228, 200)); text(img, (X0 + 120 + 150, Y(64)), "지상 최대의 끼룩이", 20, (201, 178, 122))
    text(img, (X0 + W - 96, Y(64)), f"{int(ratio * 100 + .99)}%", 22, (255, 222, 128), anchor="rs")
    for i, n in enumerate(icons): img.alpha_composite(art(n, S).resize((30, 30), Image.LANCZOS), (X0 + W - 240 - i * 32 - 15, Y(76 + 15)))
    if defeat: text(img, (X0 + W // 2, Y(100 + 8)), "격파", 72, (255, 220, 115), anchor="mb", stroke=4)
bg = Image.open(os.path.join(ROOT, "previews", "map_town.png")).convert("RGB").resize((1920, 1080)).filter(ImageFilter.GaussianBlur(3)).convert("RGBA")
out = Image.new("RGBA", (1100, 3 * 190 + 20), (0, 0, 0, 255))
for i, (title, r, ch, st, dfe) in enumerate((("평소", .72, None, .35, False), ("맞은 직후 — 빨간 조각", .58, .72, .6, False), ("격파", 0, None, 0, True))):
    scene = bg.crop((410, 800, 1510, 1080)).copy(); bossbar(scene, 30, 250, r, ch, st, dfe); scene = scene.crop((0, 60, 1100, 250))
    out.alpha_composite(scene, (0, 10 + i * 190)); text(out, (12, 36 + i * 190), title, 20, (255, 222, 128), anchor="ls", stroke=2)
out.convert("RGB").save(os.path.join(ROOT, "previews", "boss_bar_mock.png")); print("✔ boss_bar_mock.png")
