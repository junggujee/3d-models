# Unity에서 처음 실행하기

저장소에는 `unity/Assets`만 있다(ProjectSettings는 Unity가 만든다).

1. Unity Hub에서 **새 프로젝트 → "3D (Built-In Render Pipeline)"** 템플릿으로 만든다. Unity 6 또는 2022.3 이상을 권장한다.
   - URP 템플릿이면 모델 재질(Standard 셰이더)이 **분홍색**으로 나온다. URP를 쓰려면 `Window ▸ Rendering ▸ Render Pipeline Converter`로 재질을 변환해야 한다.
2. 이 저장소의 `moomin-action/unity/Assets` 안의 폴더를 새 프로젝트의 `Assets`에 복사한다.
   - 대상: `Editor`, `Scripts`, `Models`, `Resources`, `City`
3. 메뉴 **Moomin ▸ 한 번에 설정 (전부 실행)**을 누른다. 다음이 차례로 진행된다.
   1. 임포트 설정
   2. 애니메이터
   3. 스킬 에셋
   4. 프리팹
   5. `Assets/Scenes/Main.unity` 생성 + 맵 정리
4. `Main.unity`를 열고 Play.
5. (선택) 메뉴 **Moomin ▸ 맵 가림 처리 굽기**를 누른다.

## 오류가 나면

- **Console 창:** 빨간 오류 줄을 우클릭해 복사한 뒤 그대로 보내 주면 된다. 첫 줄과 `at ...` 줄이 중요하다.
- **로그 파일:**
  - Windows: `%LOCALAPPDATA%\Unity\Editor\Editor.log`
  - macOS: `~/Library/Logs/Unity/Editor.log`
- 컴파일 오류가 있으면 메뉴 자체가 보이지 않는다. 이때는 Console의 첫 오류부터 보내 준다.

## 여기(클라우드)에서 미리 한 검사

- `tools/compile_check.sh`: 전체 C#를 Unity 2021.3 API로 실제 컴파일한다.
- `tools/unity_audit.py`: 실행할 때 터지기 쉬운 오류를 찾는다. 대상은 다음과 같다.
  - Resources 경로
  - 애니메이터 상태와 모델 클립
  - '클래스 이름 = 파일 이름'
  - 가짜 null(`GetComponent()?.`)
  - 셰이더 대체 경로
- `tools/status_tests`: 순수 로직 테스트 90개.
