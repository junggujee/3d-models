"""
펫 HUD 미리보기 — UI/PetHUD.cs 와 같은 좌표: 카드(왼쪽 위 44,-300 · 380×96), 안내(화면 아래 24% 가운데), 알림(오른쪽 위).
배경은 장면 대신 물범·땅콩빵 렌더를 얹은 흐린 마을. python3 tools/pet_mock.py → previews/pet_hud_mock.png
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); W, H = 1920, 1080
F = "/usr/share/fonts/truetype/nanum/"; FR, FB = F + "NanumMyeongjo.ttf", F + "NanumMyeongjoBold.ttf"
def t(img, xy, s, size, col, path=FR, anchor="lm"):
    l = Image.new("RGBA", img.size); d = ImageDraw.Draw(l); f = ImageFont.truetype(path, size)
    for dx, dy in ((-2, 0), (2, 0), (0, -2), (0, 2), (1.5, 1.5)): d.text((xy[0] + dx, xy[1] + dy), s, font=f, fill=(0, 0, 0, 200), anchor=anchor)
    d.text(xy, s, font=f, fill=col + (255,), anchor=anchor); img.alpha_composite(l)
bg = Image.open(os.path.join(ROOT, "previews", "map_town.png")).convert("RGBA").resize((W, int(W * 860 / 1400))).crop((0, 0, W, H)).filter(ImageFilter.GaussianBlur(2))
img = bg.copy(); img.alpha_composite(Image.new("RGBA", (W, H), (0, 0, 0, 70)))
seal = Image.open(os.path.join(ROOT, "previews", "seal_front.png")).convert("RGBA"); a = np.asarray(seal).copy()
bgc = a[5, 5, :3].astype(int); mask = np.abs(a[..., :3].astype(int) - bgc).sum(2) < 18; a[mask, 3] = 0; seal = Image.fromarray(a).resize((420, 420))
img.alpha_composite(seal, (900, 560))
pea = Image.open(os.path.join(ROOT, "previews", "peanut_whole.png")).convert("RGBA"); b = np.asarray(pea).copy(); bgc = b[5, 5, :3].astype(int); m2 = np.abs(b[..., :3].astype(int) - bgc).sum(2) < 18; b[m2, 3] = 0
img.alpha_composite(Image.fromarray(b).resize((420, 420)), (1300, 520))
d = ImageDraw.Draw(img)
# 카드
x0, y0 = 44, 300; d.rectangle((x0, y0, x0 + 380, y0 + 96), fill=(18, 15, 13, 235), outline=(115, 92, 51, 255), width=2)
ic = Image.open(os.path.join(ROOT, "unity", "Assets", "Resources", "ItemIcons", "pet_seal.png")).convert("RGBA").resize((76, 76)); img.alpha_composite(ic, (x0 + 10, y0 + 10))
t(img, (x0 + 96, y0 + 21), "몽실이", 24, (255, 219, 128), FB); t(img, (x0 + 180, y0 + 22), "Lv.3", 20, (207, 198, 176))
t(img, (x0 + 370, y0 + 21), "따라오는 중 (G)", 18, (158, 145, 120), FR, "rm")
d.rectangle((x0 + 96, y0 + 42, x0 + 366, y0 + 56), fill=(8, 6, 5, 240), outline=(204, 166, 92)); d.rectangle((x0 + 99, y0 + 45, x0 + 99 + int(264 * .78), y0 + 53), fill=(140, 217, 255))
t(img, (x0 + 96, y0 + 75), "♥♥♥", 22, (255, 128, 173)); t(img, (x0 + 150, y0 + 75), "♥♥", 22, (106, 85, 96))
d.rectangle((x0 + 96, y0 + 85, x0 + 96 + int(270 * .4), y0 + 88), fill=(255, 219, 128))
# 안내 · 알림 · 목표(위치 감)
t(img, (W / 2, H * (1 - .24)), "F   땅콩빵 조각 주기  (2/3)", 28, (235, 222, 194), FB, "mm")
d.rectangle((W - 500, 40, W - 40, 104), fill=(18, 15, 13, 235), outline=(115, 92, 51, 255)); t(img, (W - 470, 72), "몽실이: 킁킁! 땅콩빵 냄새 — 오른쪽 18m", 23, (235, 222, 194))
t(img, (92, 236), "웨이브 2  —  남은 그림자 3마리", 30, (235, 222, 194)); t(img, (92, 274), "처치 점수  120", 24, (158, 145, 120))
t(img, (44, H - 28), "Space 회피   좌클릭 공격   우클릭 방어   Q·E·R 스킬   Tab 조준   I 가방   C 장비   F 상호작용   G 펫   M 지도   1~4   Esc 휴식", 21, (158, 145, 120))
img.convert("RGB").save(os.path.join(ROOT, "previews", "pet_hud_mock.png")); print("✔ pet_hud_mock.png")
