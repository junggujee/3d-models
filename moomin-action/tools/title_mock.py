"""
타이틀 · 저장 슬롯 · 게임 오버 화면 미리보기(1920×1080). 좌표·크기·색은 C# 와 같게:
  Title/TitleScreen.cs(Left·CrestY·TitleY·SubY·DividerY·MenuY·MenuStep) · UI/SaveSlotPage.cs · UI/GameOverScreen.cs · UI/UITheme.cs(색)
배경: blender/render_title_bg.py 렌더(color.png + dist.png) 에 하늘·안개(Unity RenderSettings 와 같은 지수² 안개)를 합성.
  python3 tools/title_mock.py <렌더 폴더>  → previews/title_mock.png · save_slots_mock.png · game_over_mock.png
"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Title")
PREV = os.path.join(ROOT, "previews")
FONTS = "/usr/share/fonts/truetype/nanum/"
F_R, F_B, F_X = FONTS + "NanumMyeongjo.ttf", FONTS + "NanumMyeongjoBold.ttf", FONTS + "NanumMyeongjoExtraBold.ttf"
F_T = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "unity", "Assets", "Resources", "Fonts", "HeirOfLight-Bold.ttf")   # 제목: 빛의 계승자 Bold(UITheme.DisplayFont)
W, H = 1920, 1080

# UITheme 색(0~1)
def c255(*c): return tuple(int(round(v * 255)) for v in c)
IVORY, BLUE, BLUE_B, BLUE_D = c255(.96, .94, .88), c255(.40, .60, .86), c255(.70, .86, 1), c255(.16, .27, .46)
MIST, TITLE_TEXT = c255(.68, .73, .80), c255(.88, .86, .80)
GOLD, GOLD_B, GOLD_D, CREAM, MUTED, RED = c255(.80, .65, .36), c255(1, .86, .5), c255(.45, .36, .2), c255(.92, .87, .76), c255(.62, .57, .47), c255(.66, .09, .09)
SHADE_BAND, BAND_FILL, SHADE_RESULTS = .40, .55, .58   # GameOverScreen 밝기
DEFEAT_RED, SUB_RED = c255(.93, .24, .2), c255(.96, .45, .38)
FOG = np.array([.63, .71, .83]); FOG_DENSITY = 0.0012

# TitleScreen 배치
LEFT, CREST_Y, CREST, TITLE_Y, SUB_Y, DIV_Y, MENU_Y, MENU_STEP = 150, 200, 176, 352, 444, 492, 572, 76


def art(name): return Image.open(os.path.join(ART, name + ".png")).convert("RGBA")
def font(path, size): return ImageFont.truetype(path, size)


def text(img, xy, s, size, color, path=F_R, anchor="lm", alpha=255, shadow=(1.5, 1.5), outline=None, outline_w=0, big_shadow=None):
    """Unity Text 흉내: 그림자(Shadow 1.5,-1.5) + 선택적 외곽선(Outline)."""
    layer = Image.new("RGBA", img.size); d = ImageDraw.Draw(layer); f = font(path, size)
    if big_shadow: d.text((xy[0] + big_shadow[0], xy[1] + big_shadow[1]), s, font=f, fill=(0, 0, 0, int(.7 * alpha)), anchor=anchor)
    if outline:
        for dx in (-outline_w, outline_w):
            for dy in (-outline_w, outline_w): d.text((xy[0] + dx, xy[1] + dy), s, font=f, fill=outline + (int(.9 * alpha),), anchor=anchor)
    if shadow: d.text((xy[0] + shadow[0], xy[1] + shadow[1]), s, font=f, fill=(0, 0, 0, int(.85 * alpha)), anchor=anchor)
    d.text(xy, s, font=f, fill=color + (alpha,), anchor=anchor)
    img.alpha_composite(layer)


def paste(img, im, box_center=None, left_mid=None, size=None):
    if size: im = im.resize((int(size[0]), int(size[1])), Image.LANCZOS)
    if box_center: x, y = int(box_center[0] - im.width / 2), int(box_center[1] - im.height / 2)
    else: x, y = int(left_mid[0]), int(left_mid[1] - im.height / 2)
    img.alpha_composite(im, (x, y))


def nine(im, w, h, border, ppu_scale=2):
    """9분할 늘리기(그림 px border = UI border × 2, 결과는 UI px)."""
    b = border * ppu_scale; sw, sh = im.size; out = Image.new("RGBA", (int(w), int(h))); bu = border
    xs_src = [0, b, sw - b, sw]; ys_src = [0, b, sh - b, sh]; xs_dst = [0, bu, w - bu, w]; ys_dst = [0, bu, h - bu, h]
    for i in range(3):
        for j in range(3):
            tile = im.crop((xs_src[i], ys_src[j], xs_src[i + 1], ys_src[j + 1])); dw, dh = int(xs_dst[i + 1] - xs_dst[i]), int(ys_dst[j + 1] - ys_dst[j])
            if dw > 0 and dh > 0: out.alpha_composite(tile.resize((dw, dh), Image.LANCZOS), (int(xs_dst[i]), int(ys_dst[j])))
    return out


def background(render_dir):
    col = Image.open(os.path.join(render_dir, "color.png")).convert("RGBA").resize((W, H), Image.LANCZOS)
    dist = np.asarray(Image.open(os.path.join(render_dir, "dist.png")).convert("L").resize((W, H), Image.BILINEAR), np.float32) / 255 * 900
    c = np.asarray(col, np.float32) / 255; rgb, a = c[..., :3], c[..., 3:]
    y = np.linspace(0, 1, H)[:, None, None]
    sky = np.array([.30, .45, .68]) * (1 - y) ** 1.2 + FOG * (1 - (1 - y) ** 1.2)
    rng = np.random.default_rng(3); cloud = Image.fromarray((rng.random((H // 24, W // 24)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC).filter(ImageFilter.GaussianBlur(40))
    cl = np.asarray(cloud, np.float32)[..., None] / 255; cl = np.clip((cl - .5) * 3, 0, 1) * np.clip(1 - y * 1.6, 0, 1) * .35
    sky = sky * (1 - cl) + np.array([.95, .95, .97]) * cl
    f = 1 - np.exp(-(FOG_DENSITY * dist) ** 2); f = f[..., None]
    rgb = rgb * (1 - f) + FOG * f
    out = rgb * a + sky * (1 - a)
    return Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)).convert("RGBA")


def left_shade(img):
    u = np.linspace(0, 1, 1150); s = np.clip((u - .05) / .95, 0, 1); s = s * s * (3 - 2 * s); a = .92 * (1 - s)
    sh = np.zeros((H, 1150, 4), np.uint8); sh[..., 0], sh[..., 1], sh[..., 2] = 8, 13, 26; sh[..., 3] = (a * 255).astype(np.uint8)[None, :]
    img.alpha_composite(Image.fromarray(sh, "RGBA"), (0, 0))
    v = np.linspace(1, 0, 160); bs = np.zeros((160, W, 4), np.uint8); bs[..., 0], bs[..., 1], bs[..., 2] = 8, 13, 26; bs[..., 3] = (.55 * (1 - v) ** 2 * 255).astype(np.uint8)[:, None]
    img.alpha_composite(Image.fromarray(bs, "RGBA"), (0, H - 160))   # 아래(1-v 큼)가 진함


def title_screen(bg):
    img = bg.copy(); left_shade(img)
    paste(img, art("Crest"), box_center=(LEFT + CREST / 2 - 14, CREST_Y), size=(CREST, CREST))
    text(img, (LEFT - 6, TITLE_Y), "민미의 모험", 132, IVORY, F_T, outline=BLUE_D, outline_w=3, shadow=None, big_shadow=(6, 8))
    text(img, (LEFT, SUB_Y), "THE  ADVENTURE  OF  MINMI", 30, BLUE_B, F_R)
    paste(img, art("DividerLeft"), left_mid=(LEFT - 20, DIV_Y), size=(640, 26))
    items = ["이어하기", "새 게임", "불러오기", "설정", "종료"]; sel = 0
    for i, s in enumerate(items):
        y = MENU_Y + i * MENU_STEP; x = LEFT - 40
        if i == sel:
            paste(img, art("MenuGlow"), left_mid=(x, y), size=(760, 64)); paste(img, art("Marker"), box_center=(x + 40, y), size=(30, 30))
        text(img, (x + 76, y), s, 40, IVORY if i == sel else TITLE_TEXT, F_B if i == sel else F_R, alpha=255 if i == sel else 217)
        if i == 0: text(img, (x + 270, y - 2), "Lv.12  ·  마을 광장  ·  웨이브 7   ·   3분 전", 22, MIST, F_R)
    text(img, (LEFT, H - 40), "W / S  ·  마우스   이동         Enter   선택", 22, MIST, F_R)
    text(img, (W - 40, H - 40), "Ver 0.4", 20, MIST, F_R, anchor="rm", alpha=180)
    return img


SLOTS = [("자동 저장", "Lv.12  ·  마을 광장  ·  웨이브 7", "3:12:05", "2026.11.22 14:03   ·   3분 전", 0),
         ("기록 1", "Lv.11  ·  성  ·  웨이브 6", "2:58:40", "2026.11.22 13:40   ·   26분 전", 1),
         ("기록 2", "Lv.4  ·  서쪽 마을  ·  웨이브 2", "0:41:12", "2026.11.20 21:15   ·   2일 전", 2),
         ("기록 3", None, None, None, None)]


def save_slots(bg, thumbs, title_style=True, mode="Load"):
    img = bg.copy(); img.alpha_composite(Image.new("RGBA", (W, H), (5, 8, 15, int(.78 * 255)) if title_style else (0, 0, 0, int(.72 * 255))))
    rows = SLOTS if mode == "Load" else SLOTS[1:]; n = len(rows); top, gap, row_h = -158, 186, 168
    ph = -top + n * gap + 110; pw = 1280; px, py = (W - pw) / 2, (H - ph) / 2
    accent, bright, text_c, muted = (BLUE, IVORY, TITLE_TEXT, MIST) if title_style else (GOLD, GOLD_B, CREAM, MUTED)
    frame = art("SlotFrame") if title_style else None
    if frame: img.alpha_composite(nine(frame, pw, ph, 20), (int(px), int(py)))
    text(img, (W / 2, py + 30 + 32), "기록 불러오기" if mode == "Load" else "기록 남기기", 46, bright, F_B, anchor="mm")
    paste(img, art("Divider"), box_center=(W / 2, py + 100 + 11), size=(900, 22))
    slot_bg = (14, 12, 10, 235)
    for i, (name, summ, t, date, th) in enumerate(rows):
        s = i == 0; rx, ry = (W - 1160) / 2, py - (top - i * gap)
        box = Image.new("RGBA", (1160, row_h)); d = ImageDraw.Draw(box)
        k = 1 if s else .82
        if title_style:
            d.rectangle((0, 0, 1159, row_h - 1), fill=(15, 23, 38, int(235 * k)), outline=(178, 130, 82, int(255 * k))); d.rectangle((2, 2, 1157, row_h - 3), outline=(245, 240, 225, int(56 * k)))
        else: d.rectangle((0, 0, 1159, row_h - 1), fill=(18, 15, 13, int(235 * k)), outline=(115, 92, 51, 255))
        img.alpha_composite(box, (int(rx), int(ry)))
        if s: paste(img, art("MenuGlow"), left_mid=(rx + 2, ry + row_h / 2), size=(1156, row_h - 4))
        fr = Image.new("RGBA", (252, 144), (115, 74, 41, 230) if title_style else (115, 92, 51, 230)); img.alpha_composite(fr, (int(rx + 46), int(ry + row_h / 2 - 72)))
        img.alpha_composite(Image.new("RGBA", (246, 138), (5, 8, 13, 255)), (int(rx + 49), int(ry + row_h / 2 - 69)))
        if th is not None: img.alpha_composite(thumbs[th].resize((246, 138), Image.LANCZOS), (int(rx + 49), int(ry + row_h / 2 - 69)))
        else: text(img, (rx + 46 + 126, ry + row_h / 2), "—", 40, muted, F_R, anchor="mm")
        if s: paste(img, art("Marker"), box_center=(rx + 22, ry + row_h / 2), size=(30, 30))
        text(img, (rx + 330, ry + 18 + 24), name, 34, bright if s else text_c, F_B)
        if summ:
            text(img, (rx + 330, ry + 72 + 22), summ, 29, text_c, F_R)
            text(img, (rx + 1160 - 34, ry + 22 + 20), "플레이  " + t, 26, accent, F_R, anchor="rm")
            text(img, (rx + 1160 - 34, ry + 112 + 18), date, 23, muted, F_R, anchor="rm")
        else: text(img, (rx + 330, ry + 72 + 22), "비어 있음", 28, muted, F_R)
    text(img, (W / 2, py + ph - 62 - 20), "", 26, accent, F_B, anchor="mm")
    text(img, (W / 2, py + ph - 22 - 18), "W/S 이동    Enter 선택    X 지우기    Esc 돌아가기", 22, muted, F_R, anchor="mm")
    return img


def game_over(bg, phase):
    img = bg.copy()
    if phase == "band":
        img.alpha_composite(Image.new("RGBA", (W, H), (0, 0, 0, int(SHADE_BAND * 255))))
        band = Image.new("RGBA", (W, 260), (20, 13, 13, int(BAND_FILL * 255))); img.alpha_composite(band, (0, H // 2 - 20 - 130))
        for yy in (H // 2 - 20 - 130, H // 2 - 20 + 128):
            ln = np.zeros((2, W, 4), np.uint8); ln[..., :3] = GOLD; u = np.linspace(0, 1, W); ln[..., 3] = (255 * np.clip(np.minimum(u, 1 - u) * 5, 0, 1))[None]; img.alpha_composite(Image.fromarray(ln, "RGBA"), (0, yy))
        face_at(img, (W / 2, H / 2 - 20 - 300), 300, 0, glow=.35)
        text(img, (W / 2, H / 2 - 20), "패배....", 140, DEFEAT_RED, F_B, anchor="mm", outline=(51, 0, 0), outline_w=1)
        return img
    img.alpha_composite(Image.new("RGBA", (W, H), (0, 0, 0, int(SHADE_RESULTS * 255))))
    pw, ph = 1060, 820; px, py = (W - pw) / 2, (H - ph) / 2
    d = ImageDraw.Draw(img); d.rectangle((px, py, px + pw, py + ph), fill=(14, 12, 10, 242), outline=GOLD, width=2)
    lift = Image.new("RGBA", (pw - 20, ph - 20), (115, 92, 64, int(.28 * 255))); img.alpha_composite(lift, (int(px + 10), int(py + 10))); d = ImageDraw.Draw(img); d.rectangle((px + 5, py + 5, px + pw - 5, py + ph - 5), outline=GOLD_D, width=1)
    text(img, (W / 2, py + 30 + 33), "여정의 기록", 50, GOLD_B, F_B, anchor="mm")
    text(img, (W / 2, py + 96 + 19), "웨이브 7에서 패배했다", 26, SUB_RED, F_R, anchor="mm")
    ln = np.zeros((2, 920, 4), np.uint8); ln[..., :3] = GOLD; u = np.linspace(0, 1, 920); ln[..., 3] = (255 * np.clip(np.minimum(u, 1 - u) * 5, 0, 1))[None]; img.alpha_composite(Image.fromarray(ln, "RGBA"), (int(W / 2 - 460), int(py + 146)))
    cells = [("처치한 적", "48"), ("합 승률", "62%"), ("최고 위력", "27"), ("도달 웨이브", "7"), ("레벨", "12"), ("플레이 시간", "3:12:05")]
    for i, (lab, val) in enumerate(cells):
        col, row = i % 2, i // 2; cx = W / 2 + (-240 if col == 0 else 240); cy = py + 200 + row * 128
        d.rectangle((cx - 220, cy, cx + 220, cy + 112), fill=(40, 33, 25, 225), outline=GOLD_D)
        text(img, (cx, cy + 8 + 17), lab, 24, CREAM, F_R, anchor="mm")
        if i == 1: text(img, (cx - 40, cy + 112 - 8 - 31), val, 46, GOLD_B, F_B, anchor="mm"); text(img, (cx + 70, cy + 112 - 8 - 27), "8승 5패", 24, GOLD_B, F_B, anchor="mm")
        else: text(img, (cx, cy + 112 - 8 - 31), val, 46, GOLD_B, F_B, anchor="mm")
    for i, (lab, det) in enumerate([("마지막 저장에서 다시", "자동 저장  ·  Lv.12  ·  마을 광장  ·  웨이브 7  ·  3분 전"), ("타이틀로", "기록은 저장된 곳까지 남아 있다")]):
        ry = py + 604 + i * 92; rx = W / 2 - 410
        if i == 0:
            hl = np.zeros((80, 820, 4), np.uint8); u = np.linspace(0, 1, 820); f = np.clip(np.minimum(u, 1 - u) * 5, 0, 1); hl[..., 0], hl[..., 1], hl[..., 2] = 217, 166, 77; hl[..., 3] = (f * .16 * 255)[None]; hl[:2, :, :3] = GOLD; hl[-2:, :, :3] = GOLD; hl[:2, :, 3] = hl[-2:, :, 3] = (f * 255)
            img.alpha_composite(Image.fromarray(hl, "RGBA"), (int(rx), int(ry)))
        text(img, (rx + 30 + 30, ry + 40), "»", 42, GOLD_B if i == 0 else GOLD_D, F_B, anchor="mm")
        text(img, (rx + 100, ry + 40 - 10), lab, 34, (255, 255, 255) if i == 0 else CREAM, F_R)
        text(img, (rx + 102, ry + 40 + 22), det, 21, (204, 191, 163), F_R)
    face_at(img, (W / 2 - 445, H / 2 - 330), 210, -8, glow=.17)
    return img


def face_at(img, center, size, angle, glow):
    """GameOverScreen 얼굴(DefeatFace.png): center(화면 px) · 크기 · 기울기(도, 반시계 +) · 뒤의 붉은 후광."""
    g = int(size + 80); yy, xx = np.mgrid[0:g, 0:g]; dd = np.hypot(xx - g / 2, yy - g / 2) / (g / 2); a = np.clip(1 - dd, 0, 1) ** 2 * glow
    halo = np.zeros((g, g, 4), np.uint8); halo[..., 0], halo[..., 1], halo[..., 2] = 255, 140, 115; halo[..., 3] = (a * 255).astype(np.uint8)
    img.alpha_composite(Image.fromarray(halo, "RGBA"), (int(center[0] - g / 2), int(center[1] - g / 2)))
    f = Image.open(os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "GameOver", "DefeatFace.png")).convert("RGBA").resize((size, size), Image.LANCZOS).rotate(angle, Image.BICUBIC, expand=True)
    img.alpha_composite(f, (int(center[0] - f.width / 2), int(center[1] - f.height / 2)))


if __name__ == "__main__":
    rd = sys.argv[1] if len(sys.argv) > 1 else "title_render"
    bg = background(rd); os.makedirs(PREV, exist_ok=True)
    title_screen(bg).convert("RGB").save(os.path.join(PREV, "title_mock.png")); print("✔ title_mock.png")
    thumbs = [bg.crop((560, 120, 1920, 885)), Image.open(os.path.join(PREV, "map_castle.png")).convert("RGBA").crop((0, 47, 1400, 834)), Image.open(os.path.join(PREV, "map_town.png")).convert("RGBA").crop((0, 47, 1400, 834))]
    save_slots(bg, thumbs).convert("RGB").save(os.path.join(PREV, "save_slots_mock.png")); print("✔ save_slots_mock.png")
    game_bg = Image.open(os.path.join(PREV, "map_town.png")).convert("RGBA").resize((W, int(W * 860 / 1400)), Image.LANCZOS).crop((0, 0, W, H))
    a, b = game_over(game_bg, "band"), game_over(game_bg, "results")
    sheet = Image.new("RGB", (W, H // 2)); sheet.paste(a.convert("RGB").resize((W // 2, H // 2)), (0, 0)); sheet.paste(b.convert("RGB").resize((W // 2, H // 2)), (W // 2, 0))
    sheet.save(os.path.join(PREV, "game_over_mock.png")); print("✔ game_over_mock.png")
