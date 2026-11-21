# 3D 캐릭터·소품 모델 모음

three.js(r160) **코드만으로 만든** 3D 모델과 애니메이션 모음입니다. 외부 3D 파일 없음, 글씨·로고 없음.
각 폴더는 독립적으로 동작하며, 폴더 안 `CLAUDE.md`에 구조·조정 포인트·주의사항이 정리되어 있습니다.

| 폴더 | 내용 |
|---|---|
| [`gift-3d-models/`](gift-3d-models) | 하프물범 필통(복슬복슬 털, 헤엄) · 스타 스파크 검(무지개 날, 스파크) |
| [`spin-saber-3d/`](spin-saber-3d) | 회전 쌍검 광선검 (테마 3종, 24색, 회전·칼날 펼치기) |
| [`moomin-action/`](moomin-action) | **게임**: 민미로 도시를 돌아다니며 스파크 검으로 싸우는 3D 액션 (Blender 모델·애니메이션 + Unity C#) |
| [`moomin-3d/`](moomin-3d) | 민미(무민) 인형: 엎드린 모습 · 서 있는 모습 (털, 부드러운 눈, 깜빡임·숨쉬기) |

## 실행
ES 모듈이라 로컬 서버가 필요합니다.
```bash
cd moomin-3d   # 또는 gift-3d-models, spin-saber-3d
python3 -m http.server 8000   # → http://localhost:8000/demo/index.html
```
