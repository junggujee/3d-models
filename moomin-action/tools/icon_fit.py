"""아이템 아이콘 맞추기: Resources/ItemIcons/*.png 를 그림 부분(알파)에 맞게 잘라 가운데에 칸의 86% 크기로 다시 놓고,
색을 조금 진하게(채도 ×1.15). 3D 렌더 각도에 따라 너무 작거나(레이저 블레이더·회전 쌍검) 흐린 그림을 칸에 꽉 차게.
여러 번 돌려도 결과가 같음(이미 맞춰진 그림은 거의 그대로). 실행: python3 tools/icon_fit.py"""
import os
from PIL import Image, ImageEnhance
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); ICONS = os.path.join(ROOT, "unity", "Assets", "Resources", "ItemIcons")
S, FILL = 256, 0.86
for f in sorted(os.listdir(ICONS)):
    if not f.endswith(".png"): continue
    p = os.path.join(ICONS, f); im = Image.open(p).convert("RGBA")
    a = im.split()[3].point(lambda v: 255 if v > 18 else 0); bb = a.getbbox()
    if not bb: continue
    crop = im.crop(bb); w, h = crop.size; k = S * FILL / max(w, h)
    crop = crop.resize((max(1, round(w * k)), max(1, round(h * k))), Image.LANCZOS)
    rgb = ImageEnhance.Color(crop.convert("RGB")).enhance(1.15); crop = Image.merge("RGBA", (*rgb.split(), crop.split()[3]))
    out = Image.new("RGBA", (S, S), (0, 0, 0, 0)); out.alpha_composite(crop, ((S - crop.width) // 2, (S - crop.height) // 2)); out.save(p)
    print("맞춤", f, bb)
