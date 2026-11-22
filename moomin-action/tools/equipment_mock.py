"""
장비 화면 미리보기(1920×1080) — UI/EquipmentMenu.cs 와 같은 좌표(PW·SlotX·ViewX·ArtX·RightX …)·색.
가운데 모습은 blender/build_headband.py 가 EquipmentPreview 와 같은 각도로 렌더한 exports/equip_portrait.png(머리띠 착용).
  python3 tools/equipment_mock.py → previews/equipment_mock.png
"""
import math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONTS = "/usr/share/fonts/truetype/nanum/"; F_R, F_B = FONTS + "NanumMyeongjo.ttf", FONTS + "NanumMyeongjoBold.ttf"
W, H = 1920, 1080
def c255(*c): return tuple(int(round(v * 255)) for v in c)
GOLD, GOLD_B, GOLD_D, CREAM, MUTED = c255(.80, .65, .36), c255(1, .86, .5), c255(.45, .36, .2), c255(.92, .87, .76), c255(.62, .57, .47)
GREEN_FX, CP_GREEN = c255(.6, .95, .4), (0x9B, 0xE8, 0x6A)
GRADE = {"S": c255(.45, .95, 1), "A": c255(.55, .95, .45), "B": c255(.45, .65, 1), "C": c255(.75, .75, .75)}
# EquipmentMenu 상수
PW, PH, SlotX, SlotY, SlotSize, SlotStep = 1720, 930, 50, 130, 130, 150
ViewX, ViewY, ViewW, ViewH, ArtX, ArtY0, ArtStep, ArtSize, RightX, RightW = 200, 112, 560, 640, 800, 210, 168, 104, 1040, 640
PX, PY = (W - PW) // 2, (H - PH) // 2

def font(p, s): return ImageFont.truetype(p, s)
def text(img, xy, s, size, color, path=F_R, anchor="lm", alpha=255):
    lay = Image.new("RGBA", img.size); d = ImageDraw.Draw(lay); f = font(path, size)
    d.text((xy[0] + 1.5, xy[1] + 1.5), s, font=f, fill=(0, 0, 0, int(.85 * alpha)), anchor=anchor); d.text(xy, s, font=f, fill=color + (alpha,), anchor=anchor); img.alpha_composite(lay)
def runs(img, x, y, parts, anchor_y="m"):
    """색이 섞인 한 줄: [(글자, 크기, 색, 굵게)] 를 왼쪽부터 이어 씀(Unity rich text 흉내)."""
    for s, size, col, bold in parts:
        f = font(F_B if bold else F_R, size); text(img, (x, y), s, size, col, F_B if bold else F_R, "l" + anchor_y); x += f.getlength(s)
def slot_box(img, x, y, w, h, tint=(255, 255, 255), alpha=1.0, glow=False):
    if glow:
        g = Image.new("RGBA", img.size); ImageDraw.Draw(g).rectangle((x - 6, y - 6, x + w + 6, y + h + 6), outline=GOLD_B + (200,), width=8); img.alpha_composite(g.filter(ImageFilter.GaussianBlur(9)))
    d = ImageDraw.Draw(img); border = tuple(int(GOLD_D[k] * tint[k] / 255) for k in range(3))
    d.rectangle((x, y, x + w, y + h), fill=(18, 15, 13, int(235 * alpha))); d.rectangle((x, y, x + w, y + h), outline=border + (255,), width=2 if tint == (255, 255, 255) else 3)
def frame_panel(img):
    d = ImageDraw.Draw(img); d.rectangle((PX, PY, PX + PW, PY + PH), fill=(14, 12, 10, 242))
    d.rectangle((PX, PY, PX + PW, PY + PH), outline=GOLD + (255,), width=2); d.rectangle((PX + 5, PY + 5, PX + PW - 5, PY + PH - 5), outline=GOLD_D + (255,), width=1)
    for cx in (PX + 10, PX + PW - 10):
        for cy in (PY + 10, PY + PH - 10): d.polygon([(cx, cy - 5), (cx + 5, cy), (cx, cy + 5), (cx - 5, cy)], fill=GOLD_B)
def hline(img, cx, y, w):
    ln = np.zeros((2, w, 4), np.uint8); ln[..., :3] = GOLD; u = np.linspace(0, 1, w); ln[..., 3] = (255 * np.clip(np.minimum(u, 1 - u) * 5, 0, 1))[None]; img.alpha_composite(Image.fromarray(ln, "RGBA"), (int(cx - w / 2), int(y)))
def soft(img, box, color, a):
    x0, y0, x1, y1 = box; w, h = x1 - x0, y1 - y0; yy, xx = np.mgrid[0:h, 0:w]; d = np.hypot((xx - w / 2) / (w / 2), (yy - h / 2) / (h / 2)); al = np.clip(1 - d, 0, 1) ** 2 * a
    g = np.zeros((h, w, 4), np.uint8); g[..., :3] = color; g[..., 3] = (al * 255).astype(np.uint8); img.alpha_composite(Image.fromarray(g, "RGBA"), (x0, y0))
def sword_icon(size):
    im = Image.new("RGBA", (size * 4, size * 4)); d = ImageDraw.Draw(im); s = size * 4
    d.line((s * .26, s * .76, s * .76, s * .22), fill=c255(.55, .6, .7), width=int(s * .09)); d.line((s * .3, s * .72, s * .72, s * .26), fill=(255, 255, 255), width=int(s * .025))
    d.line((s * .22, s * .6, s * .4, s * .78), fill=c255(.8, .1, .1), width=int(s * .06)); d.line((s * .2, s * .8, s * .3, s * .7), fill=c255(.2, .2, .24), width=int(s * .07))
    return im.resize((size, size), Image.LANCZOS)

def main():
    bg = Image.open(os.path.join(ROOT, "previews", "map_town.png")).convert("RGBA").resize((W, int(W * 860 / 1400)), Image.LANCZOS).crop((0, 0, W, H)).filter(ImageFilter.GaussianBlur(3))
    img = bg.copy(); img.alpha_composite(Image.new("RGBA", (W, H), (0, 0, 0, int(.72 * 255))))
    frame_panel(img); text(img, (W / 2, PY + 22 + 35), "장 비", 46, GOLD, F_B, "mm"); hline(img, W / 2, PY + 22 + 78, PW - 120)
    hb = Image.open(os.path.join(ROOT, "unity", "Assets", "Resources", "ItemIcons", "headband_blue.png")).convert("RGBA")
    # 왼쪽 칸
    names = ["무기", "머리", "가슴", "다리", "발"]
    for i, nm in enumerate(names):
        x, y = PX + SlotX, PY + SlotY + i * SlotStep; on = i == 1
        slot_box(img, x, y, SlotSize, SlotSize, tint=GOLD_B if on else (255, 255, 255), glow=on)
        icon, grade = (sword_icon(96), "B") if i == 0 else (hb.resize((96, 96), Image.LANCZOS), "B") if i == 1 else (None, "")
        if icon: img.alpha_composite(icon, (x + 17, y + 17))
        else: text(img, (x + SlotSize / 2, y + SlotSize / 2), nm, 24, MUTED, F_R, "mm", 140)
        if grade: text(img, (x + 10, y + 22), grade, 30, GRADE[grade], F_B, "lm")
        if icon: text(img, (x + 9, y + SlotSize - 16), nm, 18, GOLD_B if on else MUTED, F_R, "lm")
    # 가운데 모습
    soft(img, (PX + ViewX - 40, PY + ViewY + 40, PX + ViewX + ViewW + 40, PY + ViewY + ViewH + 40), (255, 209, 115), .22)
    soft(img, (PX + ViewX + 90, PY + ViewY + ViewH - 70, PX + ViewX + ViewW - 90, PY + ViewY + ViewH - 10), (0, 0, 0), .55)
    por = Image.open(os.path.join(ROOT, "exports", "equip_portrait.png")).convert("RGBA").resize((ViewW, ViewH), Image.LANCZOS); img.alpha_composite(por, (PX + ViewX, PY + ViewY))
    text(img, (PX + ViewX + ViewW / 2, PY + ViewY + ViewH - 16), "끌어서 돌려 보기", 20, MUTED, F_R, "mm")
    text(img, (PX + ViewX + 10, PY + ViewY + ViewH + 31), "민미", 42, CREAM, F_B, "lm")
    runs(img, PX + ViewX + 10, PY + ViewY + ViewH + 78, [("Lv.12   ", 30, CREAM, True), ("스파크 검사", 30, (0xE8, 0xC9, 0x7A), True)])
    attack, fp, mh, lv = 15.5, 1, 210, 12; cp = round(attack * 120 + fp * 320 + mh * 6 + 0 * 4000 + lv * 40)
    runs(img, PX + ViewX + 10, PY + ViewY + ViewH + 122, [("전투력  ", 30, CREAM, True), (f"{cp:,}", 30, CP_GREEN, True)])
    # 아티팩트
    text(img, (PX + ArtX + ArtSize / 2, PY + ArtY0 - 100 + 15), "마법 아티팩트", 22, GOLD_B, F_R, "mm")
    for i in range(3):
        cx, cy = PX + ArtX + ArtSize / 2, PY + ArtY0 + i * ArtStep; r = ArtSize * .78 / 2 * math.sqrt(2)
        d = ImageDraw.Draw(img); d.polygon([(cx, cy - r), (cx + r, cy), (cx, cy + r), (cx - r, cy)], fill=(18, 15, 13, 235), outline=GOLD_D + (255,))
        text(img, (cx, cy), "◇", 30, MUTED, F_R, "mm", 140)
    # 장비 묶음
    text(img, (PX + ArtX - 50 + 105, PY + ViewY + ViewH + 35), "장비 묶음", 22, MUTED, F_R, "mm")
    for i in range(3):
        x, y = PX + ArtX - 40 + i * 68, PY + ViewY + ViewH + 56; slot_box(img, x, y, 60, 60, tint=c255(.55, .75, 1) if i == 0 else (255, 255, 255))
        text(img, (x + 30, y + 30), str(i + 1), 30, (255, 255, 255) if i == 0 else MUTED, F_B, "mm")
    # 능력치
    sx, sy = PX + RightX, PY + 112; slot_box(img, sx, sy, RightW, 230, alpha=.9)
    text(img, (sx + 24, sy + 30), "능력치", 28, GOLD_B, F_B, "lm")
    labels = ["공격력", "코인 최종 위력", "무기 배율", "최대 체력", "받는 피해"]; dim = (0x9E, 0x92, 0x7A)
    rows = [[("15.5   ", 25, CREAM, False), ("기본 10 · 레벨 +5.5 · 장비 +0", 20, dim, False)], [("+1", 25, CP_GREEN, False), ("   공격마다(앞면·뒷면 모두)", 20, dim, False)],
            [("×1.00", 25, CREAM, False)], [("210", 25, CREAM, False)], [("-0%", 25, CREAM, False)]]
    for k, (lab, parts) in enumerate(zip(labels, rows)):
        y = sy + 58 + 15 + k * 31; text(img, (sx + 24, y), lab, 25, MUTED, F_R, "lm"); runs(img, sx + 210, y, parts)
    # 장비 목록
    text(img, (PX + RightX, PY + 356 + 20), "머리  —  낄 수 있는 장비 1개", 26, GOLD_B, F_B, "lm")
    for i in range(12):
        x, y = PX + RightX + (i % 6) * 106, PY + 404 + (i // 6) * 106; slot_box(img, x, y, 96, 96, tint=GOLD_B if i == 0 else (255, 255, 255), alpha=1 if i == 0 else .45)
        if i == 0: img.alpha_composite(hb.resize((72, 72), Image.LANCZOS), (x + 12, y + 12)); text(img, (x + 6, y + 14), "장착", 17, GOLD_B, F_B, "lm")
    # 자세히
    dx, dy = PX + RightX, PY + 626; slot_box(img, dx, dy, RightW, 236, alpha=.9); img.alpha_composite(hb.resize((100, 100), Image.LANCZOS), (dx + 20, dy + 20))
    runs(img, dx + 136, dy + 38, [("파란 머리띠   ", 32, GOLD_B, True), ("B", 32, GRADE["B"], True)])
    text(img, (dx + 136, dy + 76), "방어구 · 머리", 22, MUTED, F_R, "lm"); text(img, (dx + 136, dy + 114), "코인 최종 위력 +1", 25, GREEN_FX, F_B, "lm")
    text(img, (dx + 20, dy + 150), "보송보송한 하늘색 머리띠. 쓰고 있으면 마음이 차분해져", 22, CREAM, F_R, "lm"); text(img, (dx + 20, dy + 180), "코인을 한 번 더 힘껏 던진다.", 22, CREAM, F_R, "lm")
    text(img, (PX + RightX - 40 + (RightW + 80) / 2, PY + PH - 58 + 16), "W/S·A/D 이동   Enter 장착   X 해제   1·2·3 묶음   C·Esc 닫기", 20, MUTED, F_R, "mm")
    text(img, (PX + RightX + RightW, PY + 356 + 20), "파란 머리띠 장착", 24, GOLD_B, F_R, "rm")
    out = os.path.join(ROOT, "previews", "equipment_mock.png"); img.convert("RGB").save(out); print("✔", out)

if __name__ == "__main__":
    main()
