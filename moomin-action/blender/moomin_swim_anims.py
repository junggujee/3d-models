"""민미 물놀이 애니메이션(moomin_weapon_anims.build 안에서 같은 clip() 으로 만듦 — 채널 뜻은 그 파일 머리말).
  SwimIdle 40f : 물 위 제자리 — 몸을 세우고 팔을 옆으로 젓고 다리로 물장구(머리만 물 밖)
  Swim     30f : 물 위 앞으로 — 몸을 거의 눕히고(lean 1.3) 머리를 들고, 팔 번갈아 크게 저음(자유형) + 다리 물장구
  DiveIdle 48f : 물속 제자리 — 비스듬히 떠서 팔·다리를 천천히 저음
  Dive     36f : 물속 앞으로 — 몸을 완전히 눕혀(lean 1.5) 평영(팔 모아 뻗기 → 크게 벌려 당기기 → 개구리 발차기)
  BoatSit  30f : 통통배 의자에 앉기(다리 앞으로, 살짝 출렁)
Unity: PlayerSwim(물 위 = 발이 수면 아래 0.85m / 눕는 수영 0.35m · 물속은 자유) · Boat(의자 Mark_Seat). 모두 루프."""
import math


def build(clip):
    def cyc(n, fn, step=3):
        """루프용: 1..n 프레임 키(마지막 = 처음 값)"""
        ks = [(f, fn((f - 1) / (n - 1))) for f in range(1, n, step)]
        return ks + [(n, fn(0.0))]
    P = 2 * math.pi
    A = {}
    A["SwimIdle"] = clip("SwimIdle", 40, {
        "lean": cyc(40, lambda t: 0.2 + 0.03 * math.sin(P * t)), "up": cyc(40, lambda t: 0.03 * math.sin(P * t)),
        "lr": cyc(40, lambda t: -1.0 - 0.3 * math.sin(P * t)), "rr": cyc(40, lambda t: 1.0 + 0.3 * math.sin(P * t)),
        "lp": cyc(40, lambda t: -0.5 + 0.15 * math.cos(P * t)), "rp": cyc(40, lambda t: -0.5 + 0.15 * math.cos(P * t)),
        "llp": cyc(40, lambda t: 0.45 * math.sin(P * t)), "rlp": cyc(40, lambda t: -0.45 * math.sin(P * t)),
        "hp": cyc(40, lambda t: -0.05 + 0.04 * math.sin(P * t)), "tl": cyc(40, lambda t: 0.25 * math.sin(P * t))}, loop=True)
    A["Swim"] = clip("Swim", 30, {
        "lean": cyc(30, lambda t: 1.3 + 0.04 * math.sin(2 * P * t)), "hp": cyc(30, lambda t: -0.95), "hy": cyc(30, lambda t: 0.12 * math.sin(P * t)),
        "rp": cyc(30, lambda t: -1.45 - 1.55 * math.cos(P * t)), "lp": cyc(30, lambda t: -1.45 - 1.55 * math.cos(P * t + math.pi)),
        "rr": cyc(30, lambda t: 0.25 + 0.2 * math.sin(P * t)), "lr": cyc(30, lambda t: -0.25 - 0.2 * math.sin(P * t + math.pi)),
        "llp": cyc(30, lambda t: 0.35 * math.sin(2 * P * t)), "rlp": cyc(30, lambda t: -0.35 * math.sin(2 * P * t)),
        "tl": cyc(30, lambda t: 0.3 * math.sin(2 * P * t))}, loop=True)
    A["DiveIdle"] = clip("DiveIdle", 48, {
        "lean": cyc(48, lambda t: 0.6 + 0.05 * math.sin(P * t)), "up": cyc(48, lambda t: 0.04 * math.sin(P * t)),
        "lr": cyc(48, lambda t: -0.9 - 0.25 * math.sin(P * t)), "rr": cyc(48, lambda t: 0.9 + 0.25 * math.sin(P * t)),
        "lp": cyc(48, lambda t: -0.7 + 0.2 * math.cos(P * t)), "rp": cyc(48, lambda t: -0.7 + 0.2 * math.cos(P * t)),
        "llp": cyc(48, lambda t: 0.3 * math.sin(P * t)), "rlp": cyc(48, lambda t: 0.3 * math.sin(P * t + math.pi)),
        "hp": cyc(48, lambda t: -0.25), "tl": cyc(48, lambda t: 0.3 * math.sin(P * t))}, loop=True)
    A["Dive"] = clip("Dive", 36, {
        "lean": [(1, 1.5), (20, 1.42), (27, 1.56), (36, 1.5)], "hp": [(1, -0.55), (36, -0.55)], "fwd": [(1, 0), (27, 0.05), (36, 0)],
        "lp": [(1, -3.05), (10, -3.0), (20, -1.5), (27, 0.25), (36, -3.05)], "rp": [(1, -3.05), (10, -3.0), (20, -1.5), (27, 0.25), (36, -3.05)],
        "lr": [(1, 0), (10, -0.2), (20, -1.4), (27, -0.5), (36, 0)], "rr": [(1, 0), (10, 0.2), (20, 1.4), (27, 0.5), (36, 0)],
        "llp": [(1, 0.1), (20, -0.9), (27, 0.35), (36, 0.1)], "rlp": [(1, 0.1), (20, -0.9), (27, 0.35), (36, 0.1)],
        "tl": [(1, 0), (20, 0.4), (27, -0.4), (36, 0)]}, loop=True)
    A["BoatSit"] = clip("BoatSit", 30, {
        "llp": cyc(30, lambda t: -1.5), "rlp": cyc(30, lambda t: -1.5), "lean": cyc(30, lambda t: -0.05 + 0.02 * math.sin(P * t)),
        "lp": cyc(30, lambda t: -0.55), "rp": cyc(30, lambda t: -0.6 + 0.05 * math.sin(P * t)), "up": cyc(30, lambda t: 0.01 * math.sin(P * t)),
        "hp": cyc(30, lambda t: 0.05), "hy": cyc(30, lambda t: 0.15 * math.sin(P * t)), "tl": cyc(30, lambda t: 0.2 * math.sin(P * t))}, loop=True)
    return A
