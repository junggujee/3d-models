"""대화창 초상화 마무리: blender/build_joraeng.py 가 렌더한 두 패스(평면 고유색 + 외곽선 / 흰 재질 음영)를 합쳐 셀 채색 일러스트로.
  색 = 고유색 × 2~3단 명암(부드러운 경계) × (1 + 털결 잔무늬) + 바깥 테두리 살짝 두껍게.
  결과: unity/Assets/Resources/UI/Portraits/<이름>_<표정>.png (투명 배경, 600×720) · previews/portraits_sheet.png
실행: python3 tools/portrait_art.py [이름=joraeng]"""
import os, sys
import numpy as np
from PIL import Image, ImageFilter, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, "exports", "portraits_raw"); OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "Portraits")
NAME = sys.argv[1] if len(sys.argv) > 1 else "joraeng"
EXPR = ["normal", "happy", "sad", "surprised", "troubled"]
LINE = np.array([0.16, 0.085, 0.045])

def smooth(a, b, x): t = np.clip((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t)

def cel(flat_p, shade_p):
    fl = np.asarray(Image.open(flat_p).convert("RGBA")).astype(np.float32) / 255
    sh = np.asarray(Image.open(shade_p).convert("RGBA")).astype(np.float32) / 255
    rgb, a = fl[..., :3], fl[..., 3:4]
    L = (sh[..., :3] @ np.array([0.3, 0.55, 0.15]))[..., None]
    Lb = np.asarray(Image.fromarray((L[..., 0] * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(5))).astype(np.float32)[..., None] / 255
    tone = 0.8 + 0.12 * smooth(0.34, 0.4, Lb) + 0.08 * smooth(0.64, 0.7, Lb)       # 3단 명암
    tex = 1 + 0.55 * np.clip(L - Lb, -0.12, 0.12)                                       # 테리 털결 잔무늬
    warm = np.array([1.0, 0.97, 0.93]) * (1 - smooth(0.36, 0.42, Lb)) + (1 - (1 - smooth(0.36, 0.42, Lb)))   # 그늘은 살짝 따뜻하게
    out = np.clip(rgb * tone * tex * warm, 0, 1)
    ink = np.abs(rgb - LINE).sum(-1, keepdims=True) < 0.09                              # Freestyle 선은 명암 영향 없이 그대로
    out = np.where(ink, rgb, out)
    # 바깥 테두리 두껍게(알파 팽창 → 선 색)
    A = Image.fromarray((a[..., 0] * 255).astype(np.uint8)); grown = np.asarray(A.filter(ImageFilter.MaxFilter(5))).astype(np.float32)[..., None] / 255
    edge = np.clip(grown - a, 0, 1)
    rgb2 = out * a + LINE * edge; a2 = np.clip(a + edge, 0, 1)
    rgb2 = np.where(a2 > 0, rgb2 / np.maximum(a2, 1e-4), 0)
    return Image.fromarray((np.concatenate([np.clip(rgb2, 0, 1), a2], -1) * 255).astype(np.uint8), "RGBA")

os.makedirs(OUT, exist_ok=True); tiles = []
for e in EXPR:
    f, s = os.path.join(RAW, f"{NAME}_{e}_flat.png"), os.path.join(RAW, f"{NAME}_{e}_shade.png")
    if not os.path.exists(f): print("없음:", f); continue
    img = cel(f, s); img.save(os.path.join(OUT, f"{NAME}_{e}.png")); tiles.append((e, img)); print("✔", e)
if tiles:
    W, Hh = 300, 360; sheet = Image.new("RGB", (W * len(tiles), Hh + 34), (226, 214, 188)); d = ImageDraw.Draw(sheet)
    try: font = ImageFont.truetype(os.path.join(ROOT, "unity", "Assets", "Resources", "Fonts", "NanumMyeongjoExtraBold.ttf"), 20)
    except OSError: font = ImageFont.load_default()
    for i, (e, img) in enumerate(tiles):
        sheet.paste(img.resize((W, Hh), Image.LANCZOS), (i * W, 0), img.resize((W, Hh), Image.LANCZOS))
        d.text((i * W + W // 2, Hh + 17), e, fill=(60, 40, 25), font=font, anchor="mm")
    os.makedirs(os.path.join(ROOT, "previews"), exist_ok=True); sheet.save(os.path.join(ROOT, "previews", "portraits_sheet.png"))
