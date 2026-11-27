"""여왕이 좋아하는 무늬(사용자 사진: 장미빛 바탕 + 흰 다마스크·격자·꽃 무늬 이불) → 망토 텍스처.
사진의 주름·조명 얼룩을 걷어 내고(큰 흐림으로 나눠 밝기 평탄화) 두 색(장미빛 · 크림)으로 깨끗하게 인쇄한 무늬로.
출력: unity/Assets/Resources/Npc/QueenPattern.png(1024×768) · previews/queen_pattern.png
실행: python3 tools/queen_pattern.py"""
import os
import numpy as np
from PIL import Image, ImageFilter
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "docs", "refs", "queen_pattern_photo.jpg"); OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "Npc", "QueenPattern.png")
ROSE, CREAM = np.array([0.74, 0.42, 0.46]), np.array([0.98, 0.95, 0.9])
im = Image.open(SRC).convert("RGB")
g = np.asarray(im.convert("L")).astype(np.float32) / 255
low = np.asarray(Image.fromarray((g * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(40))).astype(np.float32) / 255
flat = np.clip(g / np.maximum(low, 0.05) * 0.62, 0, 1)                          # 밝기 평탄화(주름·그늘 제거)
flat = np.asarray(Image.fromarray((flat * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.1))).astype(np.float32) / 255
t = np.clip((flat - 0.52) / 0.16, 0, 1); t = t * t * (3 - 2 * t)                 # 두 색 사이 부드러운 경계
rgb = ROSE * (1 - t[..., None]) + CREAM * t[..., None]
out = Image.fromarray((rgb * 255).astype(np.uint8)).resize((1024, 768), Image.LANCZOS)
os.makedirs(os.path.dirname(OUT), exist_ok=True); out.save(OUT); out.resize((512, 384)).save(os.path.join(ROOT, "previews", "queen_pattern.png")); print("✔", OUT)
