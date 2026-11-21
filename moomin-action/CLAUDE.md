# moomin-action 인수인계 (Claude용)

Blender(bpy 5.3, headless) → FBX/GLB → Unity(C#) 파이프라인. 민미로 도시를 돌며 스파크 검으로 싸우는 웨이브 액션.

- `blender/lib.py`: 공용 도우미. **좌표 규칙**: three.js 좌표(Y 위, +Z 앞)를 `T(x,y,z)`로 Blender(Z 위, 앞 = -Y)로 변환, `K=0.6`배(미터). 캐릭터는 Blender에서 -Y를 바라봄 → FBX(`axis_forward=-Z, axis_up=Y`)/GLB 에서 Unity +Z 전방.
- Blender 5.x 는 **레이어드 액션**이라 `action.fcurves` 가 없음(쓰지 말 것). 키는 `pose_bone.keyframe_insert`.
- 뼈 회전은 `rot_world(arm, bone, 월드축, 각도)` 로 월드 축 기준 값을 로컬 오일러로 변환해서 넣음(뼈 롤 신경 안 써도 됨).
- 조각마다 뼈 1개에 100% 결합(인형처럼 단단하게). 눈은 `eye.L/R` 뼈 Y 스케일로 깜빡임.
- 애니메이션 길이: Idle 61, Walk 31, Run 21, Attack1/2 24, Attack3 30, Hit 18, Death 40 (30fps). 타격 프레임 ≈ 11 → `SwordCombat` 의 `hitTime`(≈0.37s) 과 맞춤. 클립 길이를 바꾸면 `SwordCombat.combo` 도 같이 수정.
- Unity 쪽은 애니메이션 이벤트 없이 타이머로 타격 판정. `Animator` 상태명 = 클립명(`Attack1` 등), 이동은 `Speed` 파라미터 블렌드 트리.
- `Editor/MoominSetup.cs` 가 임포트 설정·컨트롤러·프리팹·씬을 자동 생성(`Assets/City/` 의 모델은 씬에 자동 배치).
- 입력은 `InputAdapter` 한 곳에서만(새/기존 Input System 모두 지원). 다른 곳에서 `UnityEngine.Input` 직접 사용 금지.
- **Unity 미검증**. Unity 에서 오류가 보고되면 그 메시지부터 고칠 것.
- 도시 `.skp` 는 변환 전. 변환 파일을 받으면 Blender 로 폴리곤·재질 정리 + 충돌용 단순 메시 생성 예정.
