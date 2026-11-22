"""
체력바·경험치바 미리보기(Unity 없이). UI/VitalBars.cs 의 좌표·그림(Resources/UI/Bars/*.png)·색을 그대로 써서
피해 연출 4단계(맞은 직후 → 유지 → 줄어드는 중 → 사라짐)를 위에서 아래로 그림.
  python3 tools/vital_bars_mock.py  →  previews/vital_bars_mock.png
"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Bars")
FONT = "/usr/share/fonts/truetype/nanum/NanumSquareB.ttf"
S = 2                                  # 2배로 그림
HP, CHUNK, EXP = (92, 219, 56), (242, 31, 26), (255, 204, 51)

def art(n): return Image.open(os.path.join(ART, n + ".png")).convert("RGBA")
def tint(img, rgb, a=1.0):
    r, g, b, al = img.split(); w = Image.merge("RGB", (r, g, b))
    out = Image.composite(Image.new("RGB", img.size, rgb), Image.new("RGB", img.size, (0, 0, 0)), Image.new("L", img.size, 255))
    out = Image.blend(Image.new("RGB", img.size, (0, 0, 0)), Image.merge("RGB", [Image.eval(c, lambda v, k=k: v * k // 255) for c, k in zip(w.split(), rgb)]), 1)
    res = out.convert("RGBA"); res.putalpha(al.point(lambda v: int(v * a))); return res
def nine(img, w, h, b, scale):
    """9분할: 그림 테두리 b(px) → 화면 b*scale."""
    W, H = img.size; d = int(b * scale); out = Image.new("RGBA", (w, h))
    xs = [(0, b, 0, d), (b, W - b, d, w - d), (W - b, W, w - d, w)]; ys = [(0, b, 0, d), (b, H - b, d, h - d), (H - b, H, h - d, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0: out.alpha_composite(img.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out
def text(im, xy, s, size, color, anchor="mm"):
    ImageDraw.Draw(im).text(xy, s, font=ImageFont.truetype(FONT, size * S), fill=color, anchor=anchor, stroke_width=2 * S // 2 + 1, stroke_fill=(0, 0, 0))

FRAME, FILL, BACK, BIND, TAG, BADGE = art("Frame"), art("Fill"), art("Back"), art("Binding"), art("Tag"), art("Badge")

def bar(canvas, x, y, w, h, ratio, color, chunk=None, chunk_rgba=None, label="", tag=None, bind_h=None):
    X, Y, W, H = x * S, y * S, w * S, h * S; inset = (7 if h > 40 else 6) * S; iw, ih = W - 2 * inset, H - 2 * inset
    canvas.alpha_composite(BACK.resize((iw, ih)), (X + inset, Y + inset))
    if chunk and chunk > ratio:
        c = tint(FILL.resize((max(1, int(iw * (chunk - ratio))), ih)), chunk_rgba[:3], chunk_rgba[3]); canvas.alpha_composite(c, (X + inset + int(iw * ratio), Y + inset))
    if ratio > 0: canvas.alpha_composite(tint(FILL.resize((max(1, int(iw * ratio)), ih)), color), (X + inset, Y + inset))
    canvas.alpha_composite(nine(FRAME, W, H, 28, S * 0.5), (X, Y))
    if bind_h:
        for fx in (.16, .86):
            bw, bh = int(bind_h * .5 * S), int(bind_h * S); canvas.alpha_composite(BIND.resize((bw, bh), Image.LANCZOS), (X + int(W * fx) - bw // 2, Y + H // 2 - bh // 2))
    if tag is None: text(canvas, (X + W // 2, Y + H // 2), label, 26 if h > 40 else 19, (255, 255, 255))
    else:
        f = ImageFont.truetype(FONT, 26 * S); lw = ImageDraw.Draw(canvas).textlength(label, font=f)
        cx = X + W // 2 - 44 * S; text(canvas, (cx, Y + H // 2), label, 26, (255, 255, 255))
        tx = int(cx + lw / 2 + 10 * S); canvas.alpha_composite(nine(TAG, 74 * S, 30 * S, 14, S * .5), (tx, Y + H // 2 - 15 * S)); text(canvas, (tx + 37 * S, Y + H // 2), tag, 21, (255, 255, 255))

def hud(canvas, oy, hp_ratio, chunk, chunk_rgba, cur, mx, exp_ratio, exp_label, level):
    pct = None if cur >= mx else f"{int(hp_ratio * 100)}%"
    bar(canvas, 40, oy + 22, 600, 52, hp_ratio, HP, chunk, chunk_rgba, f"체력 {cur:,}/{mx:,}", pct, 66)
    bar(canvas, 92, oy + 80, 548, 30, exp_ratio, EXP, None, None, exp_label, None, 42)
    b = BADGE.resize((54 * S, 54 * S), Image.LANCZOS); canvas.alpha_composite(b, ((64 - 27) * S, (oy + 99 - 27) * S)); text(canvas, (64 * S, (oy + 99) * S), str(level), 22, (255, 222, 128))

def main():
    rows = [("평소(체력 가득)", 1.0, None, None, 391, 391),
            ("맞은 직후 — 줄어든 만큼 빨갛게(흰빛 번쩍)", .72, 1.0, (250, 120, 110, 1.0), 283, 391),
            ("0.45초 유지", .72, 1.0, (242, 31, 26, 1.0), 283, 391),
            ("줄어드는 중", .72, .86, (242, 31, 26, .7), 283, 391),
            ("사라짐", .72, None, None, 283, 391)]
    H = 150; img = Image.new("RGBA", (1060 * S, (H * len(rows) + 20) * S), (34, 40, 30, 255))
    for i, (title, r, ch, rgba, cur, mx) in enumerate(rows):
        oy = 20 + i * H; text(img, (660 * S, (oy + 48) * S), title, 17, (232, 220, 190), anchor="lm")
        hud(img, oy, r, ch, rgba, cur, mx, .18, "경험치 2.6M/14.44M", 12)
    out = os.path.join(ROOT, "previews", "vital_bars_mock.png"); img.convert("RGB").save(out); print("✔", out)

if __name__ == "__main__":
    main()
