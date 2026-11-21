# 도시 배경 (octoho fantasy city)

원본 `octoho_fantasy_city.skp` 는 **SketchUp 2013(v13) 형식**입니다. 이 폴더에 로컬로만 두고 Git 에는 올리지 않습니다(`.gitignore`, 공개 저장소).

## 왜 변환이 필요한가
`.skp` 는 SketchUp 전용 형식이라 Blender·Unity 가 직접 읽지 못하고, Linux 클라우드 환경에서 쓸 수 있는 변환 도구도 없습니다(SketchUp SDK 는 Windows/Mac 전용).
또 Blender 5 는 COLLADA(.dae)를 읽지 못하므로 **OBJ, FBX, glTF/GLB** 중 하나로 내보내야 합니다.

## SketchUp 에서 내보내기 (원본을 만든 PC 에서)
1. 파일 열기 → **파일 ▸ 내보내기 ▸ 3D 모델**
2. 형식: **OBJ** (또는 FBX). 옵션에서 "텍스처 맵 내보내기"·"삼각형으로" 켜기, 단위는 미터(인치여도 스크립트가 자동 보정)
3. 만들어진 `.obj` + `.mtl` + 텍스처 폴더를 **zip 으로 묶어서** 올려 주세요
- 무료 SketchUp(웹)은 OBJ/FBX 내보내기가 안 될 수 있습니다. 그때는 SketchUp Pro 체험판, 또는 Blender SketchUp Importer 애드온(Windows, Blender 3.x)으로 불러와 FBX/GLB 로 내보내면 됩니다.

## 변환 파일을 받으면 (자동)
```bash
python blender/build_city.py <도시.obj|.fbx|.glb> --preview
```
→ 미터 단위·바닥 0·가운데 원점 정리, 재질별 병합, 삼각형이 많으면 단순화(기본 40만), 충돌용 `City_Collision`, 시작 위치 `PlayerSpawn` 자동 탐색
→ `unity/Assets/City/City.fbx` 생성 → Unity 메뉴 **Moomin ▸ 4. 메인 씬 만들기** 를 다시 실행하면 도시가 깔리고 민미가 `PlayerSpawn` 에서 시작합니다.
