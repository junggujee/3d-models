"""
천 질감 노멀 맵(이음매 없음, 512):
  Weave_Normal.png — 땅콩빵(메모리폼 베개 커버)의 바구니 짜임: 가로·세로로 번갈아 볼록한 직사각 짜임 칸(사진처럼 엇갈린 벽돌 무늬)
  Fur_Normal.png   — 하프물범 털: 한쪽으로 쓸린 가는 털결
  python3 tools/fabric_normals.py → unity/Assets/Models/Textures/
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "unity", "Assets", "Models", "Textures")
N = 512

def to_normal(h, strength):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5; gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    n = np.dstack([-gx * strength, gy * strength, np.ones_like(h)]); n /= np.linalg.norm(n, axis=2, keepdims=True)
    return Image.fromarray(((n * 0.5 + 0.5) * 255).astype(np.uint8))

def weave():
    """칸 32px. 짝수 줄은 가로로 긴 볼록(16×8 두 개), 홀수 줄은 반 칸 밀림 — 사진의 엇갈린 짜임."""
    y, x = np.mgrid[0:N, 0:N].astype(np.float32); cw, ch = 32.0, 16.0   # 512 의 약수(이음매 없이 반복)
    row = np.floor(y / ch); off = (row % 2) * cw / 2
    u = ((x + off) % cw) / cw; v = (y % ch) / ch
    bump = np.maximum(0, np.sin(np.pi * np.clip((u - 0.06) / 0.88, 0, 1))) ** 0.6 * np.maximum(0, np.sin(np.pi * np.clip((v - 0.12) / 0.76, 0, 1))) ** 0.8
    rng = np.random.default_rng(3); fib = np.sin(x * 1.7 + rng.random() * 6) * 0.08   # 실 가닥
    return to_normal(bump + fib * bump, 2.2)

def fur():
    """가는 털결: 세로로 긴 노이즈(털이 아래로 쓸림) + 잔털."""
    rng = np.random.default_rng(9); h = np.zeros((N, N), np.float32)
    base = rng.random((N // 2, N // 16)).astype(np.float32)
    big = np.array(Image.fromarray((base * 255).astype(np.uint8)).resize((N, N), Image.BICUBIC), np.float32) / 255
    fine = rng.random((N, N)).astype(np.float32); fine = (fine + np.roll(fine, 1, 0) + np.roll(fine, 2, 0) + np.roll(fine, 3, 0)) / 4   # 세로로 이어진 가닥
    h = big * 0.6 + fine * 0.4
    return to_normal(h, 3.0)

if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    weave().save(os.path.join(OUT, "Weave_Normal.png")); fur().save(os.path.join(OUT, "Fur_Normal.png")); print("✔ Weave_Normal · Fur_Normal")
