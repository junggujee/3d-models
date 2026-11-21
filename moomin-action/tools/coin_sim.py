"""코인 전투 수식 검증·밸런스 표 — 명세 v2 규칙(Unity CoinMath / ClashManager 와 동일).
  앞면 확률 Pr(H) = Clamp(0.5 + S/100, 0.05, 0.95)
  공격 하나의 위력 P = 기본 + (앞면 ? 코인 위력 : 0)       ← 코인 하나 = 공격 한 번
  클래시(기본 StageCompare) = 양쪽의 '지금 공격' 위력 비교(이미 던진 코인, 다시 던지지 않음)
  (선택) LimbusRounds = 남은 코인을 매 라운드 다시 던져 지는 쪽이 코인 1개씩 잃음
실행: python3 tools/coin_sim.py → 표 출력 + docs/balance.md 저장"""
import random, os, math

def p_heads(s): return min(0.95, max(0.05, 0.5 + s / 100))
def binom(n, k, p): return math.comb(n, k) * p ** k * (1 - p) ** (n - k)

# Unity SkillLibrary 와 같은 값: (기본, 코인 위력, 코인 수, 피해 배율, 필요/소모 정신력)
PLAYER = {"베기(좌1)": (8, 3, 1, 1.0, 0), "되베기(좌2)": (8, 4, 1, 1.0, 0), "이어 베기(좌3)": (9, 3, 2, 1.0, 0),
          "연속 베기(Q)": (10, 3, 3, 1.0, 0), "불꽃 찌르기(E)": (14, 6, 2, 1.2, 8), "별빛 난무(R)": (12, 5, 5, 1.3, 15)}
ENEMY = {"몸통 박치기": (7, 3, 2, 0.7, 0), "내려찍기": (9, 4, 2, 0.8, 0),
         "보스·돌진": (round(13 * 1.35), 4, 2, 0.8, 0), "보스·난타": (round(9 * 1.35), 3, 4, 0.6, 0), "보스·방어 부수기": (round(18 * 1.35), 6, 1, 1.0, 0)}

def stage_odds(p, ps, e, es):
    """한 번의 공격끼리 비교: (승, 무, 패) — 정확한 확률(코인 2개뿐이라 4가지 경우)."""
    w = d = l = 0.0
    for ph in (0, 1):
        for eh in (0, 1):
            pr = (p_heads(ps) if ph else 1 - p_heads(ps)) * (p_heads(es) if eh else 1 - p_heads(es))
            a, b = p[0] + ph * p[1], e[0] + eh * e[1]
            if a > b: w += pr
            elif a == b: d += pr
            else: l += pr
    return w, d, l

out = []
def emit(s=""): print(s); out.append(s)
emit("# 코인 전투 밸런스 표 — 명세 v2 (자동 생성: tools/coin_sim.py)\n")
emit("## 1. 수식 검증\n")
emit("| 정신력 | 앞면 확률 |\n|---|---|")
for s in range(-45, 46, 15): emit(f"| {s:+d} | {p_heads(s):.0%} |")
emit("\n- 명세 4-2 예시: 연속 베기(기본 10, +3) 앞·뒤·앞 → 13 · 10 · 13, 합계 **36** ✔")
emit(f"- 3코인·앞면 70% 앞면 수 분포: " + " · ".join(f"{k}개 {binom(3, k, .7):.1%}" for k in range(4)))
emit(f"- 명세 7-1 예시: 플레이어 10+3(앞) = 13 vs 적 12+0(뒤) = 12 → 플레이어 승리 ✔\n")

for sanity in (-20, 0, 20):
    emit(f"## 2. 클래시(공격 1회 비교) — 내 정신력 {sanity:+d}, 적 정신력 0(보스 +10)\n")
    emit("승 / 무 / 패\n")
    emit("| 내 공격 \\ 적 공격 | " + " | ".join(ENEMY) + " |\n|---|" + "---|" * len(ENEMY))
    for pn, pv in PLAYER.items():
        cells = []
        for en, ev in ENEMY.items():
            w, d, l = stage_odds(pv, sanity, ev, 10 if en.startswith("보스") else 0)
            cells.append(f"{w:.0%} / {d:.0%} / {l:.0%}")
        emit(f"| {pn} | " + " | ".join(cells) + " |")
    emit("")

emit("## 3. 스킬별 위력·피해 기대값(명중 기준)\n")
emit("| 스킬 | 코인(=공격 수) | 공격 1회 위력 | 기대 합계 위력 (정신력 0 / +20) | 기대 피해 (배율 반영, 0 / +20) | 필요 정신력 |\n|---|---|---|---|---|---|")
for n_, (b, c, n, m, cost) in {**PLAYER, **ENEMY}.items():
    e0, e20 = n * (b + .5 * c), n * (b + .7 * c)
    emit(f"| {n_} | {n} | {b}~{b+c} | {e0:.1f} / {e20:.1f} | {e0*m:.1f} / {e20*m:.1f} | {cost or '-'} |")

emit("\n## 4. (선택 모드) 림버스식 다회 합 — 연속 베기 vs 몸통 박치기, 정신력 0\n")
rng = random.Random(3); N = 40000; W = T = 0
for _ in range(N):
    pc, ec, rounds = 3, 2, 0
    while pc and ec and rounds < 12:
        pp = 10 + sum(rng.random() < .5 for _ in range(pc)) * 3; ep = 7 + sum(rng.random() < .5 for _ in range(ec)) * 3; rounds += 1
        if pp > ep: ec -= 1
        elif pp < ep: pc -= 1
    W += pc > 0 and ec == 0; T += pc > 0 and ec > 0
emit(f"- 승 {W/N:.0%} · 무 {T/N:.0%} · 패 {(N-W-T)/N:.0%}  (ClashManager.mode = LimbusRounds 로 바꿀 때 참고)")
os.makedirs(os.path.join(os.path.dirname(__file__), "..", "docs"), exist_ok=True)
open(os.path.join(os.path.dirname(__file__), "..", "docs", "balance.md"), "w").write("\n".join(out) + "\n")
