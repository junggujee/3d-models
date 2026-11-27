namespace MoominAction
{
    /// <summary>
    /// 지하 광산 노역장 규칙(순수 C# — tools/status_tests 로 검증).
    ///  · 바위 하나 = 곡괭이질 HitsPerRock 번 → 부서짐 → 돌덩이 1~2(할당량에 셈) + 확률로 광석 하나(등급별).
    ///  · 할당량 Quota 개 돌덩이를 모아 간수 하프에게 내면 풀려남(돌덩이는 거둬 감, 광석은 가져도 됨).
    /// 광석 확률(바위 하나당 반드시 하나 — 사용자 지정): 구리(C) 56% · 철(B) 20% · 은(A) 16% · 꿀(S) 6% · 돌고래스톤(SS) 2%
    /// (구리 30 · 철 20 고정 + 나머지 광석 확률 2배 + '없음' 26% 는 구리에. 금 → 꿀(맨두의 꿀바른 땅콩빵 재료) · 사파이어 → 돌고래스톤)
    /// </summary>
    public static class MineMath
    {
        public const int HitsPerRock = 3, Quota = 12; public const float RespawnSeconds = 25f;
        public const string Rubble = "stone_rubble", Pickaxe = "prison_pickaxe";
        /// <summary>등급 낮은 것부터 (아이디, 확률).</summary>
        public static readonly (string id, float chance)[] Ores =
        {
            ("ore_copper", 0.56f), ("ore_iron", 0.20f), ("ore_silver", 0.16f), ("honey", 0.06f), ("dolphin_stone", 0.02f),
        };
        /// <summary>0~1 난수 하나로 광석 고르기(없으면 null).</summary>
        public static string RollOre(float r)
        {
            float acc = 0f;
            foreach (var (id, ch) in Ores) { acc += ch; if (r < acc) return id; }
            return Ores[0].id;                                                                     // 부동소수 끝자리 — 구리
        }
        /// <summary>돌덩이 개수(0~1 난수) — 70% 1개 · 30% 2개.</summary>
        public static int RollRubble(float r) => r < 0.3f ? 2 : 1;
        /// <summary>광석 등급 글자(C · B · A · S · SS).</summary>
        public static string Grade(string id) => id switch { "ore_copper" => "C", "ore_iron" => "B", "ore_silver" => "A", "honey" => "S", "dolphin_stone" => "SS", _ => "" };
        public static bool QuotaMet(int rubble) => rubble >= Quota;
        public static string Progress(int rubble) => $"돌덩이 {System.Math.Min(rubble, Quota)} / {Quota}";
    }
}
