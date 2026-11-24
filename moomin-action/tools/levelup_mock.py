"""레벨업 연출 미리보기(UI/LevelUpPopup.cs + Effects/LevelUpFx.cs 느낌) → previews/levelup_mock.png"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sheet_mock import text, F, DISP, HEAD, BODY, R, PRE, W, H

img = Image.open(os.path.join(PRE, "map_town.png")).convert("RGBA").resize((W, H)) if os.path.exists(os.path.join(PRE, "map_town.png")) else Image.new("RGBA", (W, H), (90, 110, 130, 255))
# 월드: 금빛 기둥 + 고리(민미 자리)
mx, my = W // 2, 860; pil = Image.new("RGBA", (W, H)); d = ImageDraw.Draw(pil)
for i in range(60):
    a = int(200 * (1 - i / 60)); d.rectangle([mx - 70 + i, my - 520, mx + 70 - i, my], fill=(255, 214, 120, a // 12))
d.rectangle([mx - 10, my - 520, mx + 10, my], fill=(255, 250, 220, 160)); pil = pil.filter(ImageFilter.GaussianBlur(10)); img.alpha_composite(pil)
for r, a in ((260, 200), (160, 120)): ImageDraw.Draw(img).ellipse([mx - r, my - r * .28, mx + r, my + r * .28], outline=(255, 214, 107, a), width=6)
# UI
cx, cy = W // 2, int(H * (1 - .72))
halo = Image.new("RGBA", (W, H)); ImageDraw.Draw(halo).ellipse([cx - 380, cy - 140, cx + 380, cy + 60], fill=(255, 204, 102, 70)); img.alpha_composite(halo.filter(ImageFilter.GaussianBlur(40)))
text(img, (cx, cy - 70 + 180 - 180), "LEVEL UP", F(DISP, 96), (255, 255, 255), "mm", glow=(255, 199, 89, 200), grad=((255, 247, 230), (219, 189, 140)))
text(img, (cx, cy + 30), "Lv.7  →  Lv.8", F(HEAD, 38), (255, 219, 128), "mm")
div = Image.open(os.path.join(R, "UI", "Ornament", "Divider.png")).convert("RGBA").resize((560, 22)); img.alpha_composite(div, (cx - 280, cy + 55))
rows = [("최대 체력  +12      공격력  +0.6", (235, 222, 194)), ("체력 · 스태미나 모두 회복", (191, 227, 255)), ("상대보다 3레벨 높을 때마다 코인 위력 +1 (최대 +3)", (201, 187, 160))]
for i, (t, c) in enumerate(rows): text(img, (cx, cy + 100 + i * 34), t, F(BODY, 26 if i < 2 else 20), c, "mm")
# 조준 대상 레벨 표시 예
text(img, (60, 120), "Lv.11", F(HEAD, 27), (232, 112, 96), "lm"); text(img, (140, 120), "(코인 위력 -1)", F(BODY, 17), (232, 112, 96), "lm"); text(img, (290, 120), "그린볼런", F(HEAD, 27), (255, 219, 128), "lm")
img.convert("RGB").save(os.path.join(PRE, "levelup_mock.png")); print("✔ levelup_mock.png")
