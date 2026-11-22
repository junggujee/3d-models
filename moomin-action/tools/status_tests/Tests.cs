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

        // ── 저장 슬롯 표시
        Eq(SaveFormat.SlotName(0), "기록 1", "슬롯 0 이름"); Eq(SaveFormat.SlotName(SaveFormat.AutoSlot), "자동 저장", "자동 저장 이름"); Eq(SaveFormat.FileName(2), "slot3", "파일 이름"); Eq(SaveFormat.FileName(3), "auto", "자동 파일 이름");
        Eq(SaveFormat.PlayTime(0), "00:00", "0초"); Eq(SaveFormat.PlayTime(65.9), "01:05", "1분 5초(내림)"); Eq(SaveFormat.PlayTime(3 * 3600 + 12 * 60 + 5), "3:12:05", "3시간"); Eq(SaveFormat.PlayTime(-3), "00:00", "음수 → 0"); Eq(SaveFormat.PlayTime(double.NaN), "00:00", "NaN → 0");
        var utc = new DateTime(2026, 11, 22, 5, 3, 0, DateTimeKind.Utc); string iso = SaveFormat.Stamp(utc); var seoul = TimeZoneInfo.CreateCustomTimeZone("KST", TimeSpan.FromHours(9), "KST", "KST");
        Eq(SaveFormat.Date(iso, seoul), "2026.11.22 14:03", "UTC → 한국 시각"); Eq(SaveFormat.Date("", seoul), "", "빈 시각"); Eq(SaveFormat.Date("엉망", seoul), "", "읽을 수 없는 시각");
        Eq(SaveFormat.TryParse(iso, out var back) && back == utc, true, "저장 시각 왕복");
        Eq(SaveFormat.Ago(iso, utc.AddSeconds(20)), "방금", "20초 전"); Eq(SaveFormat.Ago(iso, utc.AddMinutes(3)), "3분 전", "3분 전"); Eq(SaveFormat.Ago(iso, utc.AddHours(5).AddMinutes(59)), "5시간 전", "5시간 전");
        Eq(SaveFormat.Ago(iso, utc.AddDays(2)), "2일 전", "2일 전"); Eq(SaveFormat.Ago(iso, utc.AddDays(30), seoul), "2026.11.22 14:03", "일주일 넘으면 날짜");
        Eq(SaveFormat.WinRate(0, 0), "-", "합 없음"); Eq(SaveFormat.WinRate(8, 5), "62%", "8승 5패"); Eq(SaveFormat.WinRate(1, 1), "50%", "반반"); Eq(SaveFormat.WinRate(2, 0), "100%", "전승");
        Eq(SaveFormat.Latest(new (int, string)[0]), -1, "저장 없음 → -1");
        Eq(SaveFormat.Latest(new[] { (0, SaveFormat.Stamp(utc)), (3, SaveFormat.Stamp(utc.AddMinutes(5))), (1, SaveFormat.Stamp(utc.AddMinutes(-9))) }), 3, "가장 최근 = 자동 저장");
        Eq(SaveFormat.Latest(new[] { (2, "망가짐"), (1, SaveFormat.Stamp(utc)) }), 1, "망가진 시각은 건너뜀");
        Eq(SaveFormat.Summary(12, "마을 광장", 7), "Lv.12  ·  마을 광장  ·  웨이브 7", "슬롯 요약"); Eq(SaveFormat.Summary(1, "", 0), "Lv.1  ·  여정의 시작", "새 여정 요약");

        // ── 장비·공격력 공식
        Eq(EquipmentMath.Damage(13, 10f, 1f, 1f), 13, "공격력 10 = 예전 공식(P×M×R)"); Eq(EquipmentMath.Damage(13, 20f, 1f, 1f), 26, "공격력 2배 → 피해 2배");
        Eq(EquipmentMath.Damage(10, 15f, 1f, 1f) < EquipmentMath.Damage(13, 15f, 1f, 1f), true, "같은 공격력이면 코인 위력이 클수록 큼");
        Eq(EquipmentMath.Damage(13, 10f, 1.25f, 0.5f), 8, "배율·가드(8.125 → 8)"); Eq(EquipmentMath.Damage(0, 10f, 1f, 1f), 1, "최소 1");
        Eq(EquipmentMath.FinalPower(10, 3, true, 1), 14, "앞면 + 머리띠 +1"); Eq(EquipmentMath.FinalPower(10, 3, false, 1), 11, "뒷면도 +1"); Eq(EquipmentMath.FinalPower(2, 0, false, -5), 0, "0 아래 없음");
        var hb = new EquipStats { finalPower = 1 }; var ar = new EquipStats { attack = 2, damageReduction = 0.1f }; var sum = EquipmentMath.Sum(new[] { hb, ar, new EquipStats { damageReduction = 0.1f } });
        Eq(sum.finalPower, 1, "합: 최종 위력"); Eq(sum.attack, 2f, "합: 공격력", 1e-5f); Eq(sum.damageReduction, 0.19f, "피해 감소는 곱으로 겹침", 1e-5f);
        Eq(string.Join("/", EquipmentMath.Describe(hb)), "코인 최종 위력 +1", "머리띠 설명"); Eq(EquipmentMath.Describe(default).Count, 0, "빈 능력치");
        Eq(EquipmentMath.LevelAttack(1, 0.5f), 0f, "Lv1 공격력 보너스 0", 1e-5f); Eq(EquipmentMath.LevelAttack(11, 0.5f), 5f, "Lv11 +5", 1e-5f);
        Eq(EquipmentMath.CombatPower(10, 1, 100, 0, 1) > EquipmentMath.CombatPower(10, 0, 100, 0, 1), true, "머리띠 끼면 전투력 오름");

        // ── 집 장비 능력치(가드·회피 코인·이동 속도)
        var gear = EquipmentMath.Sum(new[] { new EquipStats { guard = 0.15f }, new EquipStats { evadePower = 2 }, new EquipStats { evadePower = 1, moveSpeed = 0.1f }, new EquipStats { health = 25, damageReduction = 0.08f } });
        Eq(gear.guard, 0.15f, "가드 보너스 합", 1e-5f); Eq(gear.evadePower, 3, "회피 코인 합"); Eq(gear.moveSpeed, 0.1f, "이동 속도 합", 1e-5f); Eq(gear.health, 25f, "체력 합", 1e-5f);
        Eq(string.Join("/", EquipmentMath.Describe(new EquipStats { guard = 0.15f })), "가드 피해 감소 +15%", "각티슈 방패 설명");
        Eq(string.Join("/", EquipmentMath.Describe(new EquipStats { moveSpeed = 0.1f, evadePower = 1 })), "회피 코인 위력 +1/이동 속도 +10%", "털 슬리퍼 설명");
        Eq(new EquipStats { evadePower = 1 }.IsEmpty, false, "회피만 있어도 비지 않음");

        Console.WriteLine(fail == 0 ? $"✔ 전투 로직 테스트 {pass}개 통과" : $"✘ {fail}개 실패 / {pass}개 통과");
        return fail == 0 ? 0 : 1;
    }
}
