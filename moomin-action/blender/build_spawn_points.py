"""맵에서 자원·펫 생성 자리 뽑기 → unity/Assets/Resources/World/SpawnPoints.json (Blender 좌표 — 게임에서 MapPlaces 로 바꿈).
  peaks : 산 정상 — 산·바위 면(Mountain_*·Rock_*)을 15m 칸으로 나눠 칸마다 가장 높은 점 → 반경 30m 안에서 가장 높은 칸만(봉우리), 높은 순 12개
  nature: 자연(초록) — 나무(Foliage)가 빽빽한 10m 칸 + 낮은 풀밭 산기슭(Mountain_Grass, 높이 25m 아래), 마을 지붕에서 10m 이상 떨어진 곳, 서로 12m 이상
실행: <bpy 파이썬> blender/build_spawn_points.py"""
import bpy, json, os, numpy as np
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "unity", "Assets", "Resources", "World", "SpawnPoints.json")
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "exports", "City.glb"))
def faces(obj):
    me = obj.data; n = len(me.polygons); C = np.empty(n * 3, np.float32); me.polygons.foreach_get("center", C); C = C.reshape(-1, 3)
    MI = np.empty(n, np.int32); me.polygons.foreach_get("material_index", MI); M = np.array(obj.matrix_world); C = C @ M[:3, :3].T + M[:3, 3]
    return C, MI, [m.name for m in me.materials]
city = bpy.data.objects["City"]; C, MI, names = faces(city)
def sel(pred): return C[np.isin(MI, [i for i, s in enumerate(names) if pred(s)])]
mount = sel(lambda s: s.startswith("Mountain_") or s.startswith("Rock_")); roofs = sel(lambda s: s.startswith("Roof_")); grass = sel(lambda s: s == "Mountain_Grass")
# 봉우리
cell = 15.0; k = np.floor(mount[:, :2] / cell).astype(int); best = {}
for (i, j), p in zip(map(tuple, k), mount):
    if (i, j) not in best or p[2] > best[(i, j)][2]: best[(i, j)] = p
pts = np.array(list(best.values())); peaks = []
for p in pts:
    near = pts[np.linalg.norm(pts[:, :2] - p[:2], axis=1) < 30]
    if p[2] >= near[:, 2].max() - 1e-3 and p[2] > 12: peaks.append(p)
peaks = sorted(peaks, key=lambda p: -p[2])[:12]
# 자연
fol = faces(bpy.data.objects["Foliage"])[0]
cand = []
for src, cs, minc in ((fol, 10.0, 12), (grass[grass[:, 2] < 25], 12.0, 8)):
    kk = np.floor(src[:, :2] / cs).astype(int); u, inv, cnt = np.unique(kk, axis=0, return_inverse=True, return_counts=True)
    for idx in np.where(cnt >= minc)[0]: m = inv.ravel() == idx; cand.append(src[m].mean(0))
cand = np.array(cand); nature = []
for p in cand[np.random.default_rng(2).permutation(len(cand))]:
    if len(roofs) and np.min(np.linalg.norm(roofs[:, :2] - p[:2], axis=1)) < 10: continue
    if all(np.linalg.norm(p[:2] - q[:2]) >= 12 for q in nature): nature.append(p)
r = lambda a: [round(float(a[0]), 2), round(float(a[1]), 2), round(float(a[2]), 2)]
os.makedirs(os.path.dirname(OUT), exist_ok=True)
json.dump({"peaks": [r(p) for p in peaks], "nature": [r(p) for p in nature[:80]]}, open(OUT, "w"), indent=1)
print(f"✔ peaks {len(peaks)} nature {min(80, len(nature))}  peak heights {[round(float(p[2]),1) for p in peaks]}")
