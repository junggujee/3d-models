"""
패배 화면 민미 얼굴 마무리: blender/render_defeat_face.py 렌더(exports/DefeatFace_raw.png + .json 눈 자리)에
눈 대신 짙은 ×× 를 그리고(둥근 끝 붓선, 살짝 비뚤게) 털을 조금 밝힘 → unity/Assets/Resources/UI/GameOver/DefeatFace.png
  python3 tools/defeat_face.py
"""
import json, math, os
import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, "exports", "DefeatFace_raw.png")
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "UI", "GameOver", "DefeatFace.png")
SS = 4


def main():
    im = Image.open(RAW).convert("RGBA"); W, H = im.size; meta = json.load(open(RAW.replace(".png", ".json")))
    a = np.asarray(im, np.float32) / 255; rgb = a[..., :3] ** 0.85 * 1.04   # 털을 조금 밝게
    im = Image.fromarray((np.dstack([np.clip(rgb, 0, 1), a[..., 3:]]) * 255).astype(np.uint8), "RGBA")
    big = Image.new("RGBA", (W * SS, H * SS)); d = ImageDraw.Draw(big)
    ink = (34, 38, 56, 255)
    for side, e in meta.items():
        cx, cy = e["uv"][0] * W * SS, (1 - e["uv"][1]) * H * SS; r = e["r"] * SS * 0.8; w = max(6, int(e["r"] * SS * 0.27))
        tilt = math.radians(e.get("tilt", 0)) * (-1)
        for k, ang in enumerate((45, -45)):
            t = math.radians(ang) + tilt + (0.06 if k == 0 else -0.04) * (1 if side == "L" else -1)   # 손으로 그은 듯 살짝 비뚤게
            dx, dy = math.cos(t) * r, math.sin(t) * r
            d.line((cx - dx, cy - dy, cx + dx, cy + dy), fill=ink, width=w)
            for px, py in ((cx - dx, cy - dy), (cx + dx, cy + dy)): d.ellipse((px - w / 2, py - w / 2, px + w / 2, py + w / 2), fill=ink)
    im.alpha_composite(big.resize((W, H), Image.LANCZOS))
    # 아래(잘린 몸통)는 서서히 투명하게 — UI 에서 띠 위로 자연스럽게 떠오름
    arr = np.asarray(im).copy(); y = np.linspace(0, 1, H)[:, None]; fade = np.clip((0.97 - y) / 0.3, 0, 1); fade = fade * fade * (3 - 2 * fade)
    arr[..., 3] = (arr[..., 3] * fade).astype(np.uint8); im = Image.fromarray(arr, "RGBA")
    os.makedirs(os.path.dirname(OUT), exist_ok=True); im.save(OUT); print("✔", OUT)


if __name__ == "__main__":
    main()
