using System.Collections.Generic;
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
        Eq(string.Join("/", EquipmentMath.Describe(new EquipStats { moveSpeed = 0.1f, evadePower = 1 })), "회피 코인 위력 +1/이동 속도 +10%", "토끼 털 슬리퍼 설명");
        Eq(new EquipStats { evadePower = 1 }.IsEmpty, false, "회피만 있어도 비지 않음");

        // ── 퀘스트 진행(땅콩빵이 먹고 싶은 조랭이)
        var ql = new QuestLog(); int changes = 0; ql.Changed += (_, __) => changes++;
        Eq(ql.State("peanut_craving"), QuestState.NotStarted, "처음엔 시작 전"); Eq(ql.Complete("peanut_craving"), false, "수락 전 완료 불가");
        Eq(ql.Accept("peanut_craving"), true, "수락"); Eq(ql.Accept("peanut_craving"), false, "두 번 수락 안 됨"); Eq(ql.IsActive("peanut_craving"), true, "진행 중");
        Eq(ql.Active().Count, 1, "진행 목록 1개"); Eq(ql.Complete("peanut_craving"), true, "완료"); Eq(ql.Complete("peanut_craving"), false, "두 번 완료 안 됨");
        Eq(ql.Abandon("peanut_craving"), false, "완료한 건 포기 불가"); Eq(ql.Active().Count, 0, "완료 후 진행 목록 비움"); Eq(changes, 2, "바뀜 알림 2번(수락·완료)");
        Eq(ql.Accept(""), false, "빈 id 수락 불가"); Eq(ql.Accept("b"), true, "다른 퀘스트 수락"); Eq(ql.Abandon("b"), true, "포기"); Eq(ql.State("b"), QuestState.NotStarted, "포기 → 시작 전");
        ql.Accept("c"); var saved = ql.Export(); Eq(string.Join(",", saved), "peanut_craving=Completed,c=Active", "저장 글자(수락 순서)");
        var ql2 = new QuestLog(); ql2.Import(new[] { "peanut_craving=Completed", "c=Active", "x=Weird", "=Active", "y=", "z=NotStarted", null, "c=Completed", "n=7" });
        Eq(ql2.State("peanut_craving"), QuestState.Completed, "불러오기: 완료"); Eq(ql2.State("c"), QuestState.Completed, "같은 id 는 뒤의 것"); Eq(ql2.State("x"), QuestState.NotStarted, "모르는 상태 무시");
        Eq(ql2.State("n"), QuestState.NotStarted, "숫자 상태 무시"); Eq(ql2.Export().Length, 2, "이상한 항목은 버림"); ql2.Import(null); Eq(ql2.Export().Length, 0, "null 불러오기 → 비움");
        Eq(QuestText.Progress("땅콩빵 조각", 1, 2), "땅콩빵 조각 1/2", "진행 글자"); Eq(QuestText.Progress("땅콩빵 조각", 5, 2), "땅콩빵 조각 2/2", "넘치면 2/2"); Eq(QuestText.Progress("x", -3, 2), "x 0/2", "음수 → 0");
        Eq(QuestText.Enough(2, 2), true, "2개면 건넬 수 있음"); Eq(QuestText.Enough(1, 2), false, "1개는 부족"); Eq(QuestText.Enough(0, 0), true, "필요 없음");
        Eq(QuestText.Rewards(120, 50, new[] { ("빨간 물약", 2) }), "경험치 120 · 금화 50 · 빨간 물약 ×2", "보상 줄"); Eq(QuestText.Rewards(0, 0, new[] { ("열쇠", 1), ("", 3) }), "열쇠", "하나짜리·빈 이름");

        // ── 토벌 퀘스트(반복): 수락 → 잡기 → 완료 → 다시
        var hl = new HuntLog(); int hc = 0; hl.Changed += _ => hc++;
        Eq(hl.AddKill("hunt_x", 3), false, "수락 전엔 안 셈"); Eq(hl.Accept("hunt_x"), true, "토벌 수락"); Eq(hl.Accept("hunt_x"), false, "진행 중 두 번 수락 안 됨");
        Eq(hl.Ready("hunt_x", 3), false, "0/3 은 아직"); hl.AddKill("hunt_x", 3); hl.AddKill("hunt_x", 3); Eq(hl.TurnIn("hunt_x", 3), false, "2/3 완료 안 됨");
        Eq(hl.AddKill("hunt_x", 3), true, "3번째"); Eq(hl.AddKill("hunt_x", 3), false, "다 잡으면 더 안 셈"); Eq(hl.Count("hunt_x"), 3, "3마리");
        Eq(hl.TurnIn("hunt_x", 3), true, "완료"); Eq(hl.Clears("hunt_x"), 1, "완료 1번"); Eq(hl.IsActive("hunt_x"), false, "완료 후 다시 받을 수 있음"); Eq(hl.Count("hunt_x"), 0, "수 비움");
        Eq(hl.Accept("hunt_x"), true, "반복 수락"); hl.AddKill("hunt_x", 3); Eq(hl.Abandon("hunt_x"), true, "포기"); Eq(hl.Clears("hunt_x"), 1, "포기해도 완료 수 그대로");
        Eq(hc, 8, "바뀜 알림 8번(수락·잡기3·완료·수락·잡기·포기)");
        hl.Accept("hunt_x"); hl.AddKill("hunt_x", 3); hl.Accept("hunt_y");
        Eq(string.Join(",", hl.Export()), "hunt_x=1:1:1,hunt_y=1:0:0", "토벌 저장 글자");
        var hl2 = new HuntLog(); hl2.Import(new[] { "hunt_x=1:1:1", "hunt_z=0:5:2", "bad", "=1:0:0", "q=2:0:0", "r=1:-1:0", "s=1:0", null });
        Eq(hl2.Count("hunt_x"), 1, "불러오기: 수"); Eq(hl2.Clears("hunt_x"), 1, "불러오기: 완료 수"); Eq(hl2.IsActive("hunt_z"), false, "멈춘 건 진행 아님");
        Eq(hl2.Count("hunt_z"), 0, "진행 아니면 수 0"); Eq(hl2.Clears("hunt_z"), 2, "완료 수만"); Eq(hl2.Export().Length, 2, "이상한 항목 버림");
        Eq(HuntMath.Exp(100, 1, 0), 150L, "첫 완료 ×1.5"); Eq(HuntMath.Exp(100, 1, 3), 100L, "반복 ×1"); Eq(HuntMath.Exp(100, 11, 1), 220L, "레벨 11 → ×2.2");
        Eq(HuntMath.Gold(50, 1, 0), 75, "금화 첫 ×1.5"); Eq(HuntMath.Gold(50, 11, 2), 80, "금화 레벨 11 → ×1.6");
        Eq(HuntMath.ZoneLevel(3, 1, -1), 3, "구역 최소 레벨"); Eq(HuntMath.ZoneLevel(1, 10, 1), 10, "내 레벨 따라감"); Eq(HuntMath.ZoneLevel(1, 1, -1), 1, "최소 1");
        Eq(HuntMath.Compass(0, 10), "북", "북"); Eq(HuntMath.Compass(-10, -10), "남서", "남서"); Eq(HuntMath.Compass(10, 0), "동", "동"); Eq(HuntMath.Compass(0.1f, 0.1f), "", "아주 가까우면 없음");
        Eq(HuntMath.Progress("곰", 5, 3), "곰 3/3", "토벌 진행 글자 넘침");
        var faces = new HashSet<string> { "normal", "happy", "sad", "surprised", "troubled" };
        var anims = new HashSet<string> { "Talk", "Wave", "Yes", "No", "Laugh", "Sad", "Surprised", "QuestAccept", "QuestComplete" };
        Eq(HuntCatalog.All.Count >= 4, true, "토벌 퀘스트 4개 이상"); var zones = new HashSet<string>();
        foreach (var h in HuntCatalog.All)
        {
            Eq(h.need > 0 && h.baseExp > 0 && h.baseGold > 0 && h.maxAlive > 0, true, h.id + " 수치"); Eq(zones.Add(h.zoneId), true, h.id + " 구역 겹치지 않음");
            Eq(h.intro.Length >= 2 && h.accept.Length >= 1 && h.repeats.Length >= 1, true, h.id + " 대사 있음");
            var lines = new List<string>(h.intro); lines.AddRange(h.accept); lines.AddRange(h.repeats); lines.AddRange(new[] { h.decline, h.waiting, h.hint, h.ready, h.thanks, h.reward });
            foreach (var line in lines)
            {
                var (f, an, t) = HuntMath.Split(line); string filled = HuntMath.Fill(t, h, 1);
                if (!faces.Contains(f) || !anims.Contains(an) || filled.Contains("{") || filled.Contains("(이)") || filled.Length == 0) Eq(line, "올바른 표정|동작|글", h.id + " 대사 형식");
            }
            Eq(HuntMath.Fill(h.waiting, h, 2).Contains("2/" + h.need), true, h.id + " 진행 대사에 숫자");
        }

        // ── 바다 물결(게르스트너, 셰이더와 같은 식)
        var ow = OceanMath.Waves(30f);
        Eq(OceanMath.MaxAmplitude(ow) < 0.7f, true, "파도 높이 합 < 0.7m(해수면 -0.8 → 뭍 0 을 넘지 않음)");
        Eq(OceanMath.Make(90f, 10f, 0.1f).dirX, 1f, "90° = +X 방향", 1e-5f); Eq(OceanMath.Make(0f, 10f, 0.1f).dirZ, 1f, "0° = +Z 방향", 1e-5f);
        { var w1 = OceanMath.Make(0f, 20f, 0.1f); Eq(w1.omega, (float)Math.Sqrt(9.81 * w1.k), "분산 관계 ω = √(g·k)", 1e-4f); Eq(w1.amp, 0.1f * 20f / (2f * (float)Math.PI), "진폭 = 가파름 / k", 1e-4f); }
        Eq(OceanMath.AmpScale(0f), 0.35f, "얕은 곳 35%", 1e-5f); Eq(OceanMath.AmpScale(6f), 1f, "6m 이상 100%", 1e-5f); Eq(OceanMath.AmpScale(-2f), 0.35f, "뭍 35%", 1e-5f);
        float worstH = 0f, maxAbs = 0f; var rnd = new Random(5);
        for (int n = 0; n < 200; n++)
        {   // 알갱이를 앞으로 옮긴 자리에서 HeightAt 으로 거꾸로 찾은 높이 = 그 알갱이 높이
            float x0 = (float)rnd.NextDouble() * 200f - 100f, z0 = (float)rnd.NextDouble() * 200f - 100f, t0 = (float)rnd.NextDouble() * 50f;
            OceanMath.Displace(ow, x0, z0, t0, 1f, out var ddx, out var ddy, out var ddz);
            worstH = Math.Max(worstH, Math.Abs(OceanMath.HeightAt(ow, x0 + ddx, z0 + ddz, t0) - ddy)); maxAbs = Math.Max(maxAbs, Math.Abs(ddy));
        }
        Eq(worstH < 0.02f, true, $"수면 높이 되풀이 오차 < 2cm (최대 {worstH:0.0000}m)"); Eq(maxAbs > 0.2f && maxAbs < 0.7f, true, "물결 높이 범위");
        Eq(Math.Abs(OceanMath.HeightAt(ow, 3f, 4f, 2f, (x, z) => 0f)) <= OceanMath.MaxAmplitude(ow) * 0.35f + 1e-4f, true, "얕은 곳 물결은 35% 이내");

        // ── 여러 발 투사체(고래고래 임금님): 회전 탄막 고리 · 부채꼴 물방울 · 딸기 포물선
        var ring = ProjectilePattern.Angles(14, 360f, 0f);
        Eq(ring.Length, 14, "고리 14발"); Eq(ring[1] - ring[0], 360f / 14f, "고리 같은 간격", 1e-4f); Eq(ring[0], 0f, "첫 발 = 목표 쪽", 1e-5f);
        var ring2 = ProjectilePattern.Angles(14, 360f, 0f, 1, 360f / 28f);
        Eq(ring2[0], 360f / 28f, "다음 코인은 반 칸 엇갈림(사이로 피할 틈)", 1e-4f);
        var fan = ProjectilePattern.Angles(3, 50f, 90f);
        Eq(fan[0], 65f, "부채꼴 왼쪽", 1e-4f); Eq(fan[1], 90f, "부채꼴 가운데 = 목표", 1e-4f); Eq(fan[2], 115f, "부채꼴 오른쪽", 1e-4f);
        Eq(ProjectilePattern.Angles(1, 0f, 30f)[0], 30f, "한 발", 1e-5f); Eq(ProjectilePattern.Angles(0, 0f, 0f).Length, 1, "0발 → 1발");
        Eq(ProjectilePattern.LobHeight(0f, 10f), 0f, "포물선 시작 0", 1e-5f); Eq(ProjectilePattern.LobHeight(1f, 10f), 0f, "포물선 끝 0", 1e-5f);
        Eq(ProjectilePattern.LobHeight(0.5f, 10f), ProjectilePattern.LobPeak(10f), "포물선 가운데 = 꼭대기", 1e-4f); Eq(ProjectilePattern.LobPeak(10f), 5f, "꼭대기 2.5 + 거리×0.25", 1e-4f);

        // ── 레벨: 차이 → 코인 위력(3레벨마다 ±1, 최대 ±3), 적 레벨·경험치·칭호
        Eq(LevelMath.CoinBonus(5, 5), 0, "같은 레벨 보정 없음"); Eq(LevelMath.CoinBonus(7, 5), 0, "2레벨 차이는 아직 0"); Eq(LevelMath.CoinBonus(8, 5), 1, "3레벨 높음 +1");
        Eq(LevelMath.CoinBonus(5, 8), -1, "3레벨 낮음 -1"); Eq(LevelMath.CoinBonus(5, 10), -1, "5레벨 낮음도 -1"); Eq(LevelMath.CoinBonus(30, 1), 3, "최대 +3"); Eq(LevelMath.CoinBonus(1, 40), -3, "최소 -3");
        Eq(LevelMath.CoinBonus(0, 5), 0, "레벨 0(모름) 보정 없음"); Eq(LevelMath.CoinPower(3, -3), 0, "코인 위력 0 아래 없음"); Eq(LevelMath.CoinPower(3, 2), 5, "코인 위력 3 + 2");
        Eq(LevelMath.CoinPower(4, -1) + 8, EquipmentMath.FinalPower(8, LevelMath.CoinPower(4, -1), true, 0), "보정은 앞면 코인에만(최종 위력 11)");
        Eq(LevelMath.EnemyLevel(4, -1, 0, false), 3, "웨이브 4 - 1"); Eq(LevelMath.EnemyLevel(4, 0, 2, true), 9, "고래 보스 4+2+3"); Eq(LevelMath.EnemyLevel(0, -1, 0, false), 1, "최소 1");
        Eq(LevelMath.EnemyHealthScale(11), 1.6f, "레벨 11 체력 ×1.6", 1e-4f); Eq(LevelMath.EnemyAttackBonus(1), 0f, "레벨 1 공격 보너스 0", 1e-5f);
        Eq(LevelMath.ExpReward(100, 5, 5), 100L, "같은 레벨 경험치 그대로"); Eq(LevelMath.ExpReward(100, 8, 5), 130L, "3레벨 높은 적 +30%"); Eq(LevelMath.ExpReward(100, 1, 20), 50L, "최소 50%"); Eq(LevelMath.ExpReward(100, 30, 1), 150L, "최대 150%");
        Eq(LevelMath.Title(9), "초보 검사", "칭호 9"); Eq(LevelMath.Title(10), "새싹 검사", "칭호 10"); Eq(LevelMath.NewTitleAt(20), true, "20 새 칭호"); Eq(LevelMath.NewTitleAt(25), false, "25 아님");
        Eq(LevelMath.BonusLabel(-2), "코인 위력 -2", "보정 글자"); Eq(LevelMath.BonusLabel(0), "", "보정 없음 글자");

        // ── 방어 코인: 방어 위력 ≥ 공격 위력이면 완벽 방어
        Eq(DefenseRules.IsPerfect(19, 19), true, "같으면 완벽 방어"); Eq(DefenseRules.IsPerfect(14, 15), false, "작으면 보통 가드"); Eq(DefenseRules.IsPerfect(0, 0), false, "방어 위력 0 은 완벽 아님");
        Eq(DefenseRules.GuardOutcome(19, 12, 0.65f, 0.9f), 0.1f, "베개 방패 완벽 방어 → 10%만", 1e-5f); Eq(DefenseRules.GuardOutcome(19, 12, 0.65f, 0.99f), 0.1f, "감소율 상한 90%", 1e-5f); Eq(DefenseRules.GuardOutcome(14, 20, 0.65f, 0.95f), 0.35f, "보통 가드 35%", 1e-5f);
        Eq(DefenseRules.GuardOutcome(8, 30, 0.5f, 0.8f), 0.5f, "스파크 검 보통 가드 50%", 1e-5f);
        // ── 무기 묶음
        foreach (var k in WeaponKits.All) { Eq(k.basics.Length, 3, k.id + " 평타 3개"); Eq(string.IsNullOrEmpty(k.ultimate), false, k.id + " 궁극 있음"); Eq(k.guardBase > 0 && k.guardReduction > 0 && k.perfectReduction >= k.guardReduction, true, k.id + " 방어 값"); }
        Eq(WeaponKits.Get("shield").guardBase > WeaponKits.Get("spark").guardBase, true, "방패가 방어 위력 더 높음"); Eq(WeaponKits.Get("dual").passive, WeaponPassive.DuelHeat, "쌍검 = 결투 고조");
        Eq(WeaponKits.Get("없음").id, "spark", "모르는 무기 → 스파크 검"); Eq(WeaponKits.Get(null).id, "spark", "null → 스파크 검");

        // ── 대장간: 강화(+0~+5) · 제작법
        Eq(ForgeMath.SuccessChance(0), 0.95f, "+1 성공률 95%", 1e-5f); Eq(ForgeMath.SuccessChance(4), 0.25f, "+5 성공률 25%", 1e-5f);
        Eq(ForgeMath.SuccessChance(5), 0f, "+5 에서는 더 못 올림", 1e-5f); Eq(ForgeMath.SuccessChance(-1), 0f, "범위 밖 0", 1e-5f);
        Eq(ForgeMath.Cost(0, 680), 170, "레이저 +1 비용"); Eq(ForgeMath.Cost(4, 680), 2040, "레이저 +5 비용"); Eq(ForgeMath.Cost(0, 40), 40, "싼 무기도 최소 40");
        Eq(ForgeMath.Cost(1, 680) > ForgeMath.Cost(0, 680), true, "올라갈수록 비쌈"); Eq(ForgeMath.Cost(5, 680), 0, "다 올린 무기는 비용 없음");
        Eq(ForgeMath.OnFail(0), 0, "+1 실패는 그대로"); Eq(ForgeMath.OnFail(2), 2, "+3 도전 실패는 그대로"); Eq(ForgeMath.OnFail(3), 2, "+4 도전 실패는 한 단계 하락"); Eq(ForgeMath.OnFail(4), 3, "+5 도전 실패는 +3 으로");
        Eq(ForgeMath.DamageBonus(0), 1f, "+0 배율 그대로", 1e-5f); Eq(ForgeMath.DamageBonus(5), 1.4f, "+5 배율 1.4", 1e-5f); Eq(ForgeMath.DamageBonus(9), 1.4f, "최대 단계로 제한", 1e-5f);
        Eq(ForgeMath.PowerBonus(3), 1, "+3 최종 위력 +1"); Eq(ForgeMath.PowerBonus(4), 2, "+4 최종 위력 +2"); Eq(ForgeMath.Prefix(0), "", "+0 은 글자 없음"); Eq(ForgeMath.Prefix(3), "+3 ", "+3 글자");
        Eq(ForgeMath.Clamp(-3), 0, "음수는 0"); Eq(ForgeMath.GuardBonus(4), 0.06f, "+4 피해 감소 6%", 1e-5f);
        foreach (var rc in ForgeMath.Recipes)
        {
            Eq(string.IsNullOrEmpty(rc.result), false, rc.id + " 결과 있음"); Eq(rc.Needs().Count > 0, true, rc.id + " 재료 있음");
            Eq(ForgeMath.Match(ForgeMath.Layout(rc)) == rc, true, rc.id + " 안내 배치가 그대로 맞음");
            if (rc.pattern != null) foreach (var row in rc.pattern) { Eq(row.Length, rc.pattern[0].Length, rc.id + " 모양 폭 같음"); foreach (var kc in row) if (kc != '.') Eq(rc.Key(kc) != null, true, rc.id + " 글자 " + kc + " 재료 있음"); }
        }
        string[] G(params string[] cells) { var g = new string[9]; for (int i = 0; i < cells.Length && i < 9; i++) g[i] = cells[i] == "-" ? null : cells[i]; return g; }
        string P = "peanut_piece", BA = "bat_alloy", BB = "bear_bone", RP = "rainbow_pearl";
        Eq(ForgeMath.Match(G(P, P, P, P, P, P, P, P, P))?.result, "peanut_bread", "땅콩빵 조각 9개 → 땅콩빵");
        Eq(ForgeMath.Match(G(P, P, P, P, "-", P, P, P, P)) == null, true, "조각 8개는 안 됨");
        Eq(ForgeMath.Match(G("-", "-", "-", "honey", "-", "-", "-", "-", "peanut_bread"))?.result, "honey_peanut_bread", "꿀 + 땅콩빵(아무 데나)");
        Eq(ForgeMath.Match(G("honey", "peanut_bread", "honey")) == null, true, "꿀이 하나 더 있으면 안 됨");
        Eq(ForgeMath.Match(G(BA, "-", BB, "-", RP, "-", BB, "-", BA))?.result, "laser_blader", "레이저 블레이더(X 모양)");
        Eq(ForgeMath.Match(G(BB, "-", BA, "-", RP, "-", BA, "-", BB))?.result, "laser_blader", "좌우 뒤집어도 됨");
        Eq(ForgeMath.Match(G(BA, BB, "-", "-", RP, "-", BB, "-", BA)) == null, true, "모양이 틀리면 안 됨");
        Eq(ForgeMath.Match(G(BA, "-", BB, "-", RP, "-", BB, "-", "-")) == null, true, "방망이 조각이 하나 모자람");
        Eq(ForgeMath.Match(G("-", "-", "-", "-", "-", "-", "shard_star", "feather_silver", "shard_star"))?.result, "headband_blue", "한 줄 모양은 아래 줄에 놓아도 됨");
        Eq(ForgeMath.Match(G("dust_fire", "-", "-", "mandu_core", "-", "-", "dust_fire"))?.result, "charm_tremor", "세로 한 줄(왼쪽)");
        Eq(ForgeMath.Match(G()) == null, true, "빈 작업대는 없음"); Eq(ForgeMath.Match(null) == null, true, "null 은 없음");
        var needs = ForgeMath.GetRecipe("craft_laser").Needs(); Eq(needs["bat_alloy"], 2, "레이저: 방망이 조각 2"); Eq(needs["bear_bone"], 2, "레이저: 사골곰뼈 2"); Eq(needs["rainbow_pearl"], 1, "레이저: 무지개 진주 1");
        Eq(ForgeMath.GetRecipe("craft_peanut").Needs()["peanut_piece"], 9, "땅콩빵: 조각 9");
        Eq(ForgeMath.GetRecipe("craft_laser").result, "laser_blader", "레이저 제작법"); Eq(ForgeMath.GetRecipe("없음") == null, true, "모르는 제작법은 null");

        // ── 지하 광산 노역장
        Eq(MineMath.RollOre(0.0f), "ore_copper", "가장 낮은 난수 = 구리"); Eq(MineMath.RollOre(0.349f), "ore_copper", "구리 35% 끝");
        Eq(MineMath.RollOre(0.36f), "ore_iron", "철"); Eq(MineMath.RollOre(0.55f), "ore_silver", "은"); Eq(MineMath.RollOre(0.62f), "ore_gold", "금");
        Eq(MineMath.RollOre(0.645f), "ore_sapphire", "사파이어 1%"); Eq(MineMath.RollOre(0.66f) == null, true, "나머지 35% 는 광석 없음"); Eq(MineMath.RollOre(0.999f) == null, true, "끝도 없음");
        float sumOre = 0; foreach (var o in MineMath.Ores) sumOre += o.chance; Eq(sumOre, 0.65f, "광석 확률 합 65%", 1e-4f);
        for (int k = 1; k < MineMath.Ores.Length; k++) Eq(MineMath.Ores[k].chance < MineMath.Ores[k - 1].chance, true, "등급이 높을수록 드묾 " + k);
        Eq(MineMath.RollRubble(0.1f), 2, "30% 돌덩이 2개"); Eq(MineMath.RollRubble(0.5f), 1, "70% 1개");
        Eq(MineMath.QuotaMet(11), false, "11개는 모자람"); Eq(MineMath.QuotaMet(12), true, "12개면 할당량"); Eq(MineMath.Progress(20), "돌덩이 12 / 12", "넘쳐도 12 로 표시");
        Eq(MineMath.Grade("ore_sapphire"), "SS", "사파이어 SS"); Eq(MineMath.Grade("ore_copper"), "C", "구리 C"); Eq(MineMath.Grade("x"), "", "모르는 건 빈 글자");

        Console.WriteLine(fail == 0 ? $"✔ 전투 로직 테스트 {pass}개 통과" : $"✘ {fail}개 실패 / {pass}개 통과");
        return fail == 0 ? 0 : 1;
    }
}
