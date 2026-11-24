#!/usr/bin/env bash
# 수정 뒤 한 번에 확인: ① C# 컴파일(Unity 참조) ② Unity 정적 점검(실행 중 터지는 유형) ③ 순수 로직 테스트.
# 사용: bash tools/check_all.sh   (실패하면 0 이 아닌 값으로 끝남)
set -e
cd "$(dirname "$0")/.."
echo "── ① 컴파일"; bash tools/compile_check.sh | tail -3
echo "── ② 정적 점검"; out=$(python3 tools/unity_audit.py); echo "$out" | grep "✘" | grep -v "개 · △" | head -20; echo "$out" | tail -1
echo "$out" | tail -1 | grep -q "✘ 0개" || { echo "정적 점검 오류가 있음"; exit 1; }
echo "── ③ 로직 테스트"; (cd tools && dotnet run --project status_tests | tail -3)
echo "✔ 모두 통과"
