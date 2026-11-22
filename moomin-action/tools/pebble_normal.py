"""
오돌도돌한 말랑이 표면(핑크큐브말랑이)용 이음매 없는 노멀 맵: 작은 돌기(지름 3~6px)를 고르게 흩뿌린 높이 → 법선.
  python3 tools/pebble_normal.py → unity/Assets/Models/Textures/Pebble_Normal.png (512, 바둑판처럼 이어짐)
Unity: MoominSetup 이 핑크큐브 재질의 _BumpMap(노멀 맵, 타일 6)으로 씀. Blender 미리보기도 같은 그림.
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "unity", "Assets", "Models", "Textures", "Pebble_Normal.png")
N = 512
rng = np.random.default_rng(7)
h = np.zeros((N, N), np.float32)
yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
# 격자 + 흔들림(지터)으로 고르게 — 실제 장난감처럼 촘촘한 돌기
step = 7.0
for gy in np.arange(0, N, step):
    for gx in np.arange(0, N, step * 1.0):
        cx = (gx + (gy / step % 2) * step / 2 + rng.uniform(-1.6, 1.6)) % N; cy = (gy + rng.uniform(-1.6, 1.6)) % N; r = rng.uniform(2.4, 3.4)
        x0, y0 = int(cx - r - 1), int(cy - r - 1)
        for y in range(y0, y0 + int(2 * r + 3)):
            for x in range(x0, x0 + int(2 * r + 3)):
                d = np.hypot(x + .5 - cx, y + .5 - cy) / r
                if d < 1: h[y % N, x % N] = max(h[y % N, x % N], np.sqrt(1 - d * d))
gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5; gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
s = 1.6   # 세기
n = np.dstack([-gx * s, gy * s, np.ones_like(h)]); n /= np.linalg.norm(n, axis=2, keepdims=True)
os.makedirs(os.path.dirname(OUT), exist_ok=True)
Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8)).save(OUT); print("✔", OUT)
