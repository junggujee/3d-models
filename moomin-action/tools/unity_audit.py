"""
Unity 를 실행하지 않고 '실행할 때 터지는' 오류 유형을 찾는 정적 점검.  python3 tools/unity_audit.py
  1) Resources.Load("경로") — Assets/Resources 아래에 실제 파일이 있는가
  2) 애니메이터 상태 — 스킬(attackAnimations/windupAnimation/recoveryAnimation)·코드(Play/CrossFade)가 부르는 상태가
     MoominSetup 이 만드는 컨트롤러(상태 목록)와 모델 GLB 의 애니메이션(클립)에 있는가
  3) MonoBehaviour/ScriptableObject — 프리팹·에셋으로 저장되는 클래스는 '클래스 이름 = 파일 이름'(아니면 Missing Script)
  4) 가짜 null — GetComponent…()?. / ?? 는 에디터에서 Unity 의 '가짜 null' 때문에 예외가 날 수 있음(TryGetComponent 권장)
  5) Shader.Find — 내장 파이프라인 빌드에 항상 들어가는 셰이더인가(아니면 대체 경로가 있는가)
  6) [RequireComponent(typeof(X))] — X 가 실제 컴포넌트 클래스인가
종료 코드: 문제(✘)가 있으면 1. 주의(△)는 0.
"""
import json, os, re, struct, sys
from glob import glob

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); A = os.path.join(ROOT, "unity", "Assets")
SRC = {p: open(p, encoding="utf-8").read() for p in glob(os.path.join(A, "**", "*.cs"), recursive=True)}
rel = lambda p: os.path.relpath(p, A)
errors, warns = [], []
def err(m): errors.append(m)
def warn(m): warns.append(m)
def line_of(text, idx): return text.count("\n", 0, idx) + 1

# ── 1) Resources.Load
res = os.path.join(A, "Resources")
for p, s in SRC.items():
    for m in re.finditer(r'Resources\.Load<\w+>\("([^"]+)"\s*(\+[^)]*)?\)', s):
        path, dyn = m.group(1), m.group(2)
        if dyn: base = os.path.join(res, path); ok = bool(glob(base + "*"))       # "UI/Bars/" + name → 접두사로 시작하는 파일이 있는가
        else: ok = bool(glob(os.path.join(res, path) + ".*"))
        if not ok: err(f"Resources 경로 없음: '{path}{'…' if dyn else ''}'  ({rel(p)}:{line_of(s, m.start())})")

# ── 2) 애니메이터 상태
def glb_anims(path):
    with open(path, "rb") as f:
        f.read(12); n, _ = struct.unpack("<I4s", f.read(8)); j = json.loads(f.read(n))
    return {a.get("name", "").split("|")[-1] for a in j.get("animations", [])}
setup = SRC[os.path.join(A, "Editor", "MoominSetup.cs")]
controllers = {}   # fbx 이름 → 상태 집합
for m in re.finditer(r'MakeController\("(\w+)\.fbx",[^,]+,\s*new\[\]\s*\{([^}]*)\}', setup):
    controllers[m.group(1)] = set(re.findall(r'"(\w+)"', m.group(2))) | {"Locomotion"}
for m in re.finditer(r'MakeController\(n \+ "\.fbx",[^,]+,\s*new\[\]\s*\{([^}]*)\}', setup):   # 여러 모델을 한 번에(foreach n) 만드는 컨트롤러
    fe = list(re.finditer(r'foreach \(var n in new\[\]\s*\{([^}]*)\}\)', setup[:m.start()]))[-1]
    for fbx in re.findall(r'"(\w+)"', fe.group(1)): controllers[fbx] = set(re.findall(r'"(\w+)"', m.group(1))) | {"Locomotion"}
moomin_states = set()
for m in re.finditer(r'foreach \(var n in new\[\]\s*\{([^}]*)\}\)\s*\{\s*var s = sm\.AddState\(n\)', setup): moomin_states |= set(re.findall(r'"(\w+)"', m.group(1)))
if moomin_states: controllers.setdefault("Moomin", set()).update(moomin_states | {"Locomotion"})
glbs = {n: glb_anims(os.path.join(ROOT, "exports", n + ".glb")) for n in ("Moomin", "SleepyBear", "Goraegorae", "PinkCube", "GreenBlock", "Seal") if os.path.exists(os.path.join(ROOT, "exports", n + ".glb"))}
for fbx, states in controllers.items():
    clips = glbs.get(fbx)
    if clips is None: continue
    for st in sorted(states - {"Locomotion"}):
        if st not in clips: err(f"{fbx} 컨트롤러 상태 '{st}' 에 해당하는 애니메이션이 모델에 없음")
    for c in ("Idle", "Walk", "Run"):
        if c not in clips: err(f"{fbx} 모델에 블렌드 트리용 '{c}' 애니메이션이 없음")
lib = SRC[os.path.join(A, "Scripts", "Combat", "SkillLibrary.cs")]
OWNER = {"BASIC": "Moomin", "SKILL": "Moomin", "HUMAN": "SleepyBear", "WHALE": "Goraegorae", "CUBE": "PinkCube", "BLOCK": "GreenBlock", "SEAL": "Seal"}   # ENEMY/BOSS = 애니메이터 없는 그림자
arrays = dict(re.findall(r'string\[\]\s*([^;]*);', lib) and [(k, v) for k, v in re.findall(r'(\w+)\s*=\s*\{([^}]*)\}', re.findall(r'string\[\][^;]*;', lib)[0])] or [])
arrays = {k: re.findall(r'"(\w+)"', v) for k, v in arrays.items()}
for m in re.finditer(r'Add\(S\("(\w+)",\s*"[^"]*",(.*?)\)\);', lib, re.S):
    sid, rest = m.group(1), m.group(2); owner = OWNER.get(sid.split("_")[0])
    if owner is None or owner not in controllers: continue
    am = re.search(r'new\[\]\s*\{([^}]*)\}', rest); names = re.findall(r'"(\w+)"', am.group(1)) if am else []
    if not am:
        for k, v in arrays.items():
            if re.search(rf',\s*{k}\s*,', rest): names = v
    for nm in names:
        if nm not in controllers[owner]: err(f"스킬 {sid} 의 공격 애니메이션 '{nm}' 이 {owner} 컨트롤러에 없음")
for m in re.finditer(r'(\w+)\.(windupAnimation|recoveryAnimation)\s*=\s*"(\w+)"', lib):
    for sid in re.findall(r'TryGetValue\("(\w+)",\s*out var ' + m.group(1) + r'\)', lib):
        owner = OWNER.get(sid.split("_")[0])
        if owner in controllers and m.group(3) not in controllers[owner]: err(f"스킬 {sid} 의 {m.group(2)} '{m.group(3)}' 이 {owner} 컨트롤러에 없음")
all_states = set().union(*controllers.values()) if controllers else set()
for p, s in SRC.items():
    if "Editor" in p: continue
    for m in re.finditer(r'(?:Play|CrossFade\w*)\("(\w+)"', s):
        if m.group(1) not in all_states: warn(f"애니메이터 상태 '{m.group(1)}' 을 어떤 컨트롤러도 만들지 않음 ({rel(p)}:{line_of(s, m.start())}) — HasState 로 막혀 있으면 무시 가능")

# ── 3) 클래스 이름 = 파일 이름
added = set(re.findall(r'AddComponent<(\w+)>', setup)) | set(re.findall(r'CreateInstance<(\w+)>', setup))
for p, s in SRC.items():
    for m in re.finditer(r'class\s+(\w+)\s*:\s*(MonoBehaviour|ScriptableObject)\b', s):
        cls = m.group(1); fname = os.path.splitext(os.path.basename(p))[0]
        if cls != fname and (cls in added or "CreateAssetMenu" in s[max(0, m.start() - 200):m.start()]):
            err(f"{cls} 가 {rel(p)} 안에 있음 — 프리팹/에셋에 저장되면 Missing Script (파일 이름을 {cls}.cs 로)")
        elif cls != fname: warn(f"{cls} 는 {rel(p)} 안에 있음(실행 중 AddComponent 만 하면 괜찮음)")

# ── 4) 가짜 null
for p, s in SRC.items():
    for m in re.finditer(r'(GetComponent(?:InChildren|InParent)?<[^>]+>\(\))\s*(\?\.|\?\?)', s):
        err(f"가짜 null 위험: {m.group(1)}{m.group(2)} ({rel(p)}:{line_of(s, m.start())}) — TryGetComponent 로")

# ── 4b) 주석이 코드를 삼킴: 한 줄 안에서 "코드;   // 설명 다음코드;" 처럼 주석 뒤에 문장이 이어지면 그 코드는 실행되지 않음
for p, s in SRC.items():
    for i, line in enumerate(s.split("\n"), 1):
        m = re.search(r'(?<![:/"])//(?!/)(.*)$', line)
        if not m: continue
        c = m.group(1)
        if re.search(r'[\w\]\)]\s*(=|\+=|-=|\*=)\s*[^=].*;|\b(var|return|if|foreach)\b[^;]*;|\b[A-Z]\w*(\.\w+)*\([^()]*\)\s*;', c) and c.count(";") >= 1 and re.search(r';\s*$|;\s*\w', c):
            err(f"주석 안에 코드처럼 보이는 문장 — 실행되지 않음: {rel(p)}:{i}  ({c.strip()[:60]}…)")

# ── 5) Shader.Find
ALWAYS = {"Sprites/Default", "UI/Default", "Standard", "Unlit/Color", "Unlit/Texture", "Unlit/Transparent", "Particles/Standard Unlit", "Hidden/InternalErrorShader"}
for p, s in SRC.items():
    if "Editor" in p: continue
    for m in re.finditer(r'Shader\.Find\("([^"]+)"\)', s):
        nm = m.group(1); head = s[max(0, m.start() - 40):m.start()]
        if nm not in ALWAYS and not re.search(r'(\?\?|==\s*null\)\s*\w+\s*=)\s*$', head):   # 다른 셰이더가 없을 때의 대체 경로면 괜찮음
            warn(f"Shader.Find(\"{nm}\") — 빌드에 없을 수 있음, 대체 경로 확인 ({rel(p)}:{line_of(s, m.start())})")

# ── 6) RequireComponent
classes = set(re.findall(r'class\s+(\w+)\s*:', "\n".join(SRC.values())))
for p, s in SRC.items():
    for m in re.finditer(r'RequireComponent\(([^)]*)\)', s):
        for t in re.findall(r'typeof\((\w+)\)', m.group(1)):
            if t not in classes and t not in {"Animator", "Rigidbody", "Collider", "CharacterController", "AudioSource", "Camera", "Canvas", "RectTransform", "NavMeshAgent"}:
                err(f"RequireComponent 대상 {t} 클래스 없음 ({rel(p)})")

print(f"Unity 정적 점검: 스크립트 {len(SRC)}개 · 컨트롤러 {', '.join(f'{k}({len(v)})' for k, v in controllers.items())} · 모델 애니메이션 {', '.join(f'{k}({len(v)})' for k, v in glbs.items())}")
for e in errors: print("  ✘", e)
for w in warns: print("  △", w)
print(f"✘ {len(errors)}개 · △ {len(warns)}개")
sys.exit(1 if errors else 0)
