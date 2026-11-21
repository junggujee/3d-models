#!/usr/bin/env bash
# Unity 없이 C# 스크립트 전체(에디터 스크립트 포함)를 실제로 컴파일해 오류를 찾는다.
# 필요: .NET SDK 8 (Ubuntu: apt-get install -y dotnet-sdk-8.0). Unity 참조 어셈블리는 NuGet 에서 받아 캐시.
#   UnityEngine 모듈 2021.3.33(unityengine.modules) · uGUI 2020.3(unity3d.unityengine.ui) · UnityEditor 2021.1(unity3d.sdk)
# 한계: 기존 Input Manager 경로만 컴파일(ENABLE_INPUT_SYSTEM 분기는 Input System 패키지가 없어 검사하지 않음), Unity 2023+ 전용 API 분기도 제외.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; ASSETS="$HERE/../unity/Assets"
CACHE="${UNITY_REF_CACHE:-$HOME/.cache/unity-ref}"; WORK="$(mktemp -d)"; mkdir -p "$CACHE"
get() { [ -d "$CACHE/$1" ] || { curl -sSL --retry 3 -o "$CACHE/$1.zip" "https://api.nuget.org/v3-flatcontainer/$1/$2/$1.$2.nupkg" && mkdir -p "$CACHE/$1" && (cd "$CACHE/$1" && unzip -oq ../$1.zip); }; }
get unityengine.modules 2021.3.33; get unity3d.unityengine.ui 2020.3.21; get unity3d.sdk 2021.1.14.1
cat > "$WORK/Check.csproj" <<XML
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework><LangVersion>9.0</LangVersion><Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems><GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <DefineConstants>UNITY_EDITOR;UNITY_2021_3_OR_NEWER</DefineConstants><NoWarn>CS1701;CS1702;CS0414;CS0649;CS0169</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$ASSETS/**/*.cs" />
    <Reference Include="$CACHE/unityengine.modules/lib/net45/*.dll" />
    <Reference Include="$CACHE/unity3d.unityengine.ui/lib/UnityEngine.UI.dll" />
    <Reference Include="$CACHE/unity3d.sdk/lib/UnityEditor.dll" />
  </ItemGroup>
</Project>
XML
cd "$WORK"; export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
if dotnet build -nologo -v q 2>&1 | tee build.log | grep -E "error|warning CS" | sed "s#$ASSETS/##" | sort -u; then :; fi
grep -q "Build succeeded" build.log && echo "✔ 컴파일 성공 ($(find "$ASSETS" -name '*.cs' | wc -l)개 파일)" || { echo "✘ 컴파일 실패"; exit 1; }
rm -rf "$WORK"
