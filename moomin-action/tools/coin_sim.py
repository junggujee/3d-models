"""코인 전투 수식 검증·밸런스 표 (Unity C# CoinMath / ClashManager 와 같은 규칙).
실행: python3 tools/coin_sim.py  → 표를 출력하고 docs/balance.md 로 저장."""
import random, os, math
from itertools import product

def p_heads(s): return min(0.95, max(0.05, 0.5 + s / 100))
def power(b, c, h): return b + h * c

def clash(p, e, rng, max_rounds=12):
    """p, e = (기본 위력, 코인 위력, 코인 수, 정신력). 반환: (결과 'P'|'E'|'T', 남은 내 코인, 남은 적 코인, 라운드 수)"""
    pc, ec, rounds = p[2], e[2], 0
    while pc > 0 and ec > 0 and rounds < max_rounds:
        ph = sum(rng.random() < p_heads(p[3]) for _ in range(pc)); eh = sum(rng.random() < p_heads(e[3]) for _ in range(ec))
        pp, ep = power(p[0], p[1], ph), power(e[0], e[1], eh); rounds += 1
        if pp > ep: ec -= 1
        elif pp < ep: pc -= 1
    return ('P' if pc > 0 and ec <= 0 else 'E' if ec > 0 and pc <= 0 else 'T'), pc, ec, rounds

def binom(n, k, p): return math.comb(n, k) * p ** k * (1 - p) ** (n - k)

out = []
def emit(s=""): print(s); out.append(s)

emit("# 코인 전투 밸런스 표 (자동 생성: tools/coin_sim.py)\n")
emit("## 1. 설계서 수치 검증\n")
emit("| 정신력 | 앞면 확률 Pr(H) |\n|---|---|")
for s in range(-45, 46, 15): emit(f"| {s:+d} | {p_heads(s):.0%} |")
e = 10 + 3 * 0.7 * 3
emit(f"\n- 기대 위력 E(P) = 10 + 3×0.7×3 = **{e:.1f}** (설계서 16.3 ✔)")
d = [binom(3, k, 0.7) for k in range(4)]
emit("- 3코인·앞면 70% 분포: " + " · ".join(f"{k}개 {v:.1%}" for k, v in enumerate(d)) + " (설계서: 2개가 최빈, 3개 34.3% ✔)")
emit(f"- 피해 예시 D = 16 × 1.5 × 0.8 = **{16*1.5*0.8:.1f}** → 반올림 19 (설계서 19.2 ✔)\n")

rng = random.Random(7); N = 20000
skills = {  # 이름: (기본, 코인 위력, 코인 수)
    "베기(좌클릭1)": (8, 3, 1), "내려찍기(좌클릭3)": (9, 3, 2), "연속 베기(Q)": (10, 3, 3), "불꽃 찌르기(E)": (16, 6, 2), "별빛 난무(R)": (14, 5, 5),
    "방어": (6, 4, 2), "패링(방어+8)": (14, 4, 2)}
enemies = {"몸통 박치기": (6, 3, 2), "내려찍기": (9, 4, 2), "보스·난타": (round(7 * 1.35), 3, 4), "보스·돌진": (round(10 * 1.35), 4, 2), "보스·방어 부수기": (round(14 * 1.35), 6, 1)}
for sanity in (-20, 0, 20):
    emit(f"## 2. 합 승률 — 내 정신력 {sanity:+d} (적 정신력: 일반 0, 보스 +10)\n")
    emit("| 내 스킬 \\ 적 공격 | " + " | ".join(enemies) + " |\n|---|" + "---|" * len(enemies))
    for sn, sv in skills.items():
        row = []
        for en, ev in enemies.items():
            es = 10 if en.startswith("보스") else 0; w = t = 0
            for _ in range(N):
                r = clash((*sv, sanity), (*ev, es), rng)[0]; w += r == 'P'; t += r == 'T'
            row.append(f"{w/N:.0%}" + (f" (무 {t/N:.0%})" if t / N >= 0.01 else ""))
        emit(f"| {sn} | " + " | ".join(row) + " |")
    emit("")

emit("## 3. 스킬별 기대 위력 / 최소~최대 (정신력 0 · +20)\n")
emit("| 스킬 | 코인 | 최소 | 최대 | 기대(0) | 기대(+20) |\n|---|---|---|---|---|---|")
for sn, (b, c, n) in skills.items():
    emit(f"| {sn} | {n} | {b} | {b+n*c} | {b+n*0.5*c:.1f} | {b+n*0.7*c:.1f} |")
os.makedirs(os.path.join(os.path.dirname(__file__), "..", "docs"), exist_ok=True)
open(os.path.join(os.path.dirname(__file__), "..", "docs", "balance.md"), "w").write("\n".join(out) + "\n")
