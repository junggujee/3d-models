"""적: 그림자 덩어리(Shadow Blob). 둥근 몸 + 반짝이는 노란 눈 + 작은 뿔. 움직임(통통 튀기)은 Unity 스크립트가 처리.
실행: python build_enemy.py [--preview]"""
import sys, os, math
sys.path.insert(0, os.path.dirname(__file__))
from lib import *
OUT = os.path.join(os.path.dirname(__file__), "..", "exports"); PRE = os.path.join(os.path.dirname(__file__), "..", "previews")
reset_scene()
BODY = mat("Blob_Body", (0.07, 0.05, 0.12), rough=0.7, sheen=0.6); EYE = mat("Blob_Eye", (1, 0.85, 0.2), rough=0.5, emit=(1, 0.8, 0.1), emit_strength=3.0)
HORN = mat("Blob_Horn", (0.25, 0.2, 0.3), rough=0.5); M = [BODY, EYE, HORN]
parts = [to_object("body", ellipsoid((0, 0.4, 0), (0.42, 0.4, 0.42), fuzz=0.03, seed=3, seg=36, rings=24), M)]
for s in (-1, 1):
    e = ellipsoid((0.17 * s, 0.5, 0.36), (0.07, 0.09, 0.03), mat_idx=1, seg=16, rings=10); parts.append(to_object(f"eye{s}", e, M))
    h = ellipsoid((0.2 * s, 0.82, 0.0), (0.05, 0.12, 0.05), rot=(0, 0, -0.35 * s), mat_idx=2, seg=14, rings=10); parts.append(to_object(f"horn{s}", h, M))
blob = join(parts, "ShadowBlob")
if "--preview" in sys.argv:
    os.makedirs(PRE, exist_ok=True); preview(os.path.join(PRE, "enemy.png"), T(1.2, 0.9, 3.2), T(0, 0.5, 0), size=480, light_energy=4)
glb, fbx = export_all("ShadowBlob", OUT, [blob]); print("EXPORTED", glb)
