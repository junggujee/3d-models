"""레이저 블레이더 광선 잔상 미리보기 합성(Unity SaberTrail 느낌): 회전부 10각도(blender/render_laser_trail.py) → 뒤로 갈수록 옅고 푸르게 + 빛 번짐 + 빛 가루.
결과: previews/laser_trail_mock.png"""
import os
import numpy as np
from PIL import Image, ImageFilter, ImageDraw
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); D = os.path.join(ROOT, "exports", "laser_trail")
rng = np.random.default_rng(3)
W = H = 520
bg = Image.new("RGBA", (W, H), (14, 20, 34, 255))
glow = np.zeros((H, W, 3), np.float32)
for i in range(9, 0, -1):                                   # 잔상(오래된 것부터)
    im = np.asarray(Image.open(os.path.join(D, f"rotor_{i}.png")).convert("RGBA")).astype(np.float32) / 255
    lum = im[..., :3].mean(-1); bright = (lum > 0.82) * im[..., 3]  # 칼날(밝은 곳)만
    k = (1 - i / 10) ** 1.5 * 0.85
    glow += bright[..., None] * np.array([0.25, 0.65, 1.0]) * k
g = Image.fromarray((np.clip(glow, 0, 1) * 255).astype(np.uint8))
blur = np.asarray(g.filter(ImageFilter.GaussianBlur(9))).astype(np.float32) / 255
layer = np.clip(np.asarray(g).astype(np.float32) / 255 * 0.9 + blur * 1.4, 0, 1)
out = np.asarray(bg).astype(np.float32) / 255; out[..., :3] = 1 - (1 - out[..., :3]) * (1 - layer)   # 가산(스크린)
img = Image.fromarray((out * 255).astype(np.uint8), "RGBA")
img.alpha_composite(Image.open(os.path.join(D, "body.png")).convert("RGBA"))
cur = Image.open(os.path.join(D, "rotor_0.png")).convert("RGBA"); img.alpha_composite(cur)
cg = np.asarray(cur).astype(np.float32) / 255; core = ((cg[..., :3].mean(-1) > 0.82) * cg[..., 3])
halo = Image.fromarray((core * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6)); hl = Image.new("RGBA", (W, H), (120, 210, 255, 0)); hl.putalpha(halo.point(lambda v: min(255, v * 2))); img.alpha_composite(hl)
d = ImageDraw.Draw(img); ys, xs = np.nonzero(np.clip(glow.mean(-1), 0, 1) > 0.05)
for k in rng.choice(len(xs), size=min(90, len(xs)), replace=False):   # 빛 가루
    x, y = xs[k] + rng.normal(0, 8), ys[k] + rng.normal(0, 8); r = rng.uniform(1, 2.6); c = (150, 225, 255, 230) if rng.random() < .6 else (90, 140, 255, 220)
    d.ellipse([x - r, y - r, x + r, y + r], fill=c)
img.convert("RGB").save(os.path.join(ROOT, "previews", "laser_trail_mock.png")); print("✔ laser_trail_mock.png")
