using System;
using MoominAction;

static class Tests
{
    static int fail, pass;
    static void Eq(float actual, float expected, string what, float tol) { if (Math.Abs(actual - expected) <= tol) pass++; else { fail++; Console.WriteLine($"  ✘ {what}: {actual} (기대 {expected})"); } }
    static void Eq<T>(T actual, T expected, string what) { if (Equals(actual, expected)) pass++; else { fail++; Console.WriteLine($"  ✘ {what}: {actual} (기대 {expected})"); } }

    static int Main()
    {
        // 출혈: 중첩(강도·횟수 따로), 공격 코인마다 1회, 같은 공격 id 중복 없음
        var b = new BleedEffect(); b.Add(new StatusAmount(2, 1)); b.Add(new StatusAmount(1, 2));
        Eq(b.Potency, 3, "출혈 강도 중첩"); Eq(b.Count, 3, "출혈 횟수 중첩");
        Eq(b.OnOwnerCoinUsed(10), 3, "출혈 발동 피해"); Eq(b.OnOwnerCoinUsed(10), 0, "같은 공격 id 중복 발동 없음"); Eq(b.Count, 2, "출혈 횟수 -1");
        b.OnOwnerCoinUsed(11); b.OnOwnerCoinUsed(12); Eq(b.Active, false, "출혈 횟수 0 → 제거"); Eq(b.OnOwnerCoinUsed(13), 0, "제거 후 발동 없음");
        var b2 = new BleedEffect(); b2.Add(new StatusAmount(3, 0)); Eq(b2.Count, 1, "강도만 부여 → 횟수 최소 1");
        var cap = new BleedEffect { MaxPotency = 5 }; cap.Add(new StatusAmount(4, 1)); cap.Add(new StatusAmount(4, 1)); Eq(cap.Potency, 5, "강도 상한");

        // 화상: 2초 간격, 프레임 속도와 무관
        int Run(float dt, float seconds) { var f = new BurnEffect(); f.Add(new StatusAmount(2, 5)); int total = 0; for (float t = 0; t < seconds - 1e-4f; t += dt) total += f.Advance(dt); return total; }
        Eq(Run(1f / 30, 5f), 4, "화상 30fps 5초 → 2회(2초·4초)"); Eq(Run(1f / 144, 5f), 4, "화상 144fps 동일"); Eq(Run(0.5f, 5f), 4, "화상 2fps 동일");
        var big = new BurnEffect(); big.Add(new StatusAmount(1, 3)); Eq(big.Advance(4.5f), 2, "긴 프레임 → 밀린 발동 모두 처리"); Eq(big.Count, 1, "화상 횟수 -2");
        Eq(big.Advance(1.9f), 1, "누적 시간 이어서 계산(0.5+1.9 ≥ 2)"); Eq(big.Active, false, "화상 소진");
        var r = new BurnEffect(); r.Add(new StatusAmount(1, 2)); r.Advance(1.5f); r.Add(new StatusAmount(1, 1)); Eq(r.Advance(0.6f), 2, "활성 중 중첩은 타이머 유지");
        var jitter = new BurnEffect(); jitter.Add(new StatusAmount(1, 99)); var rng = new Random(1); int hits = 0; double tt = 0; while (tt < 60) { float d = (float)(rng.NextDouble() * 0.1); tt += d; hits += jitter.Advance(d); }
        Eq(hits, 30, "불규칙 프레임 60초 → 30회");

        // 침잠: 맞을 때, 같은 공격 1회
        var s = new SinkingEffect(); s.Add(new StatusAmount(2, 2)); Eq(s.OnOwnerHit(1), 2, "침잠 정신력 손실"); Eq(s.OnOwnerHit(1), 0, "침잠 중복 없음"); Eq(s.Count, 1, "침잠 횟수 -1");

        // 진동: 자동 발동 없음, 폭발 = 강도×4 + 횟수×2, 횟수 -1
        var tr = new TremorEffect(); tr.Add(new StatusAmount(5, 3)); Eq(tr.PreviewBurst, 26, "진동 폭발 미리보기"); Eq(tr.Burst(7), 26, "진동 폭발 경직 피해");
        Eq(tr.Burst(7), 0, "같은 공격 폭발 중복 없음"); Eq(tr.Count, 2, "진동 횟수 -1"); Eq(tr.Potency, 5, "강도 유지"); Eq(tr.Burst(8), 24, "강도 5 횟수 2 → 24");

        // 지속시간
        var d1 = new SinkingEffect { Duration = 3f }; d1.Add(new StatusAmount(1, 5)); Eq(d1.TickDuration(2.9f), false, "지속시간 전"); Eq(d1.TickDuration(0.2f), true, "지속시간 만료"); Eq(d1.Active, false, "만료 후 제거");

        // 경직 게이지
        var g = new StaggerGauge(); Eq(g.Add(60), false, "경직 60/100"); Eq(g.Add(50), true, "경직 깨짐"); Eq(g.Current, 0f, "깨진 뒤 0");
        g.Add(30); g.Advance(3.9f); Eq(g.Current, 30f, "회복 지연 중 유지"); g.Advance(0.1f); g.Advance(1f); Eq(g.Current, 20f, "초당 10 회복");

        // 에너지
        var e = new EnergyPool(); Eq(e.Gain(70), 70, "에너지 획득"); Eq(e.Gain(50), 30, "최대 100 제한"); Eq(e.Current, 100, "에너지 100");
        Eq(e.Spend(60), true, "에너지 60 소비"); Eq(e.Spend(60), false, "부족 → 소비 불가"); Eq(e.Current, 40, "실패 시 그대로"); Eq(e.CanSpend(0), true, "비용 0 항상 가능");

        // 코인 위력 보정: 힘 · 위력 감소 · 마비 (코인 한 번에 횟수 -1, 같은 공격 중복 없음)
        var up = new PowerUpEffect(); up.Add(new StatusAmount(2, 2)); var dn = new PowerDownEffect(); dn.Add(new StatusAmount(1, 1)); var pz = new ParalysisEffect(); pz.Add(new StatusAmount(0, 1));
        Eq(pz.Potency, 1, "마비 강도 없이 부여 → 1"); 
        var m1 = new CoinModifiers { powerUp = up.UseOnCoin(1), powerDown = dn.UseOnCoin(1), paralyzed = pz.UseOnCoin(1) > 0 };
        Eq(m1.CoinValue(3), 0, "마비 → 코인 위력 0"); Eq(up.UseOnCoin(1), 0, "같은 코인 중복 소모 없음"); Eq(up.Count, 1, "힘 횟수 -1"); Eq(pz.Active, false, "마비 소진");
        var m2 = new CoinModifiers { powerUp = up.UseOnCoin(2), powerDown = dn.UseOnCoin(2), paralyzed = pz.UseOnCoin(2) > 0 };
        Eq(m2.CoinValue(3), 5, "힘 2 → 3+2"); Eq(dn.Active, false, "위력 감소 1회 뒤 소진");
        Eq(new CoinModifiers { powerDown = 9 }.CoinValue(3), 0, "위력 감소는 0 아래로 안 내려감");
        Eq(new CoinModifiers().CoinValue(4), 4, "보정 없음");

        // ── 명세 체크리스트: 가드 · 회피 · 광역 공통 코인 · 중복 피해 방지
        Eq(100f * DefenseRules.GuardMultiplier(true, 0.5f), 50f, "가드 50%: 100 → 50");
        Eq(DefenseRules.GuardMultiplier(false, 0.5f), 1f, "가드 안 함 → 그대로");
        Eq(DefenseRules.GuardMultiplier(true, 2f), 0.1f, "감소율 상한 0.9", 1e-4f);
        var ev = new EvadeState(); ev.Start(8 + 3, true);   // 회피 코인 앞면: 8 + 3 = 11
        Eq(ev.TryEvade(8), true, "회피 11 vs 8 성공"); Eq(ev.TryEvade(10), true, "회피 11 vs 10 성공(연속 유지)"); Eq(ev.TryEvade(11), true, "회피 11 vs 11 성공(같으면 성공)");
        Eq(ev.Active, true, "성공하는 동안 회피 유지"); Eq(ev.Successes, 3, "연속 성공 3회");
        Eq(ev.TryEvade(12), false, "회피 11 vs 12 실패"); Eq(ev.Active, false, "실패하면 회피 종료"); Eq(ev.TryEvade(1), false, "종료 뒤엔 회피 판정 없음");
        var ev2 = new EvadeState(); ev2.Start(8, false); Eq(ev2.Power, 8, "뒷면 회피 위력 8");
        string a1 = "곰A", b1 = "곰B";
        var u = DefenseRules.Unique(new[] { a1, a1, b1, null, a1 }); Eq(u.Count, 2, "콜라이더 여러 개 → 캐릭터당 한 번"); Eq(u[0], a1, "처음 나온 순서 유지");
        // 광역 공통 코인: 위력 13 하나를 세 대상에게 — 대상마다 방어는 따로
        int shared = 10 + 3;
        float D(EvadeState e, bool guard) => e != null && e.TryEvade(shared) ? 0f : shared * DefenseRules.GuardMultiplier(guard, 0.5f);
        var e14 = new EvadeState(); e14.Start(14, true); var e11 = new EvadeState(); e11.Start(11, true);
        Eq(D(null, false), 13f, "광역: 방어 없는 적 13"); Eq(D(null, true), 6.5f, "광역: 가드한 적 6.5"); Eq(D(e14, false), 0f, "광역: 회피 14 ≥ 13 → 0"); Eq(D(e11, false), 13f, "광역: 회피 11 < 13 → 13");

        // ── 체력바 피해 조각 · 경험치 · 숫자 표기
        var ch = new DamageChunk(1f); ch.Set(0.72f);
        Eq(ch.Active, true, "맞으면 조각 생김"); Eq(ch.Top, 1f, "조각 위쪽 = 맞기 전 체력"); Eq(ch.Current, 0.72f, "현재 = 맞은 뒤");
        ch.Advance(0.3f); Eq(ch.Top, 1f, "유지 시간 동안 그대로");
        ch.Set(0.6f); Eq(ch.Top, 1f, "연속 피해: 조각이 이어서 커짐"); Eq(ch.Age, 0f, "연속 피해: 타이머 다시 시작");
        ch.Advance(0.45f + 0.175f); Eq(ch.Top, 0.8f, "줄어드는 중간(절반)", 1e-3f);
        ch.Advance(0.2f); Eq(ch.Active, false, "다 줄면 사라짐"); Eq(ch.Top, 0.6f, "사라진 뒤 위쪽 = 현재");
        var ch2 = new DamageChunk(1f); ch2.Set(0.5f); ch2.Set(0.9f); Eq(ch2.Active, true, "조각보다 덜 회복 → 유지"); ch2.Set(1f); Eq(ch2.Active, false, "조각 위까지 회복 → 사라짐");
        var ch3 = new DamageChunk(1f); float t3 = 0; ch3.Set(0.5f); for (int i = 0; i < 200 && ch3.Active; i++) { ch3.Advance(1f / 144); t3 += 1f / 144; }
        Eq(Math.Abs(t3 - 0.8f) < 0.02f, true, "144fps 에서도 0.45+0.35초에 사라짐");
        Eq(ExpCurve.ToNext(1), 100L, "Lv1 → 100"); int lv = 1; long xp = 0;
        Eq(ExpCurve.Add(ref lv, ref xp, 350), 1, "350 → 레벨 1 오름(100, 다음 303)"); Eq(lv, 2, "Lv2"); Eq(xp, 250L, "남은 경험치 250");
        Eq(NumberFormat.Short(39100), "39.1K", "39100 → 39.1K"); Eq(NumberFormat.Short(2_600_000), "2.6M", "2.6M"); Eq(NumberFormat.Short(14_440_000), "14.44M", "14.44M");
        Eq(NumberFormat.Short(9999), "9,999", "9999 → 9,999"); Eq(NumberFormat.Comma(28347), "28,347", "쉼표");

        Console.WriteLine(fail == 0 ? $"✔ 전투 로직 테스트 {pass}개 통과" : $"✘ {fail}개 실패 / {pass}개 통과");
        return fail == 0 ? 0 : 1;
    }
}
