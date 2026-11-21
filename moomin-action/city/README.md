# 도시 배경 (octoho fantasy city)

원본 `octoho_fantasy_city.skp`(SketchUp, 약 34MB)는 **이 폴더에 로컬로만 두고 Git에는 올리지 않습니다**(`.gitignore`). 재배포 가능한 라이선스인지 확인되면 올려도 됩니다.

## 연결 방법 (둘 중 하나)
`.skp`는 Blender·Unity가 직접 읽지 못해 **변환이 한 번 필요**합니다(변환 도구는 Linux 클라우드에서 쓸 수 없었음).
1. **SketchUp에서 내보내기** (가장 간단): 파일 → 내보내기 → 3D 모델 → **FBX** 또는 **glTF/COLLADA(.dae)** (Pro 필요할 수 있음).
2. **Blender SketchUp Importer 애드온**(무료, Windows/Mac) 으로 불러온 뒤 **FBX/GLB** 로 내보내기.

변환한 파일을 `unity/Assets/City/` 에 넣고 Unity 메뉴 **Moomin ▸ 4. 메인 씬 만들기** 를 다시 실행하면 씬에 자동 배치됩니다(메시 콜라이더 자동 추가, 기본 바닥은 숨김).
변환 파일을 저에게 주시면 Blender로 최적화(폴리곤·재질 정리, 충돌용 단순 메시)까지 해 드립니다.
