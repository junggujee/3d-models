namespace MoominAction
{
    /// <summary>
    /// 지하 광산 노역장 규칙(순수 C# — tools/status_tests 로 검증).
    ///  · 바위 하나 = 곡괭이질 HitsPerRock 번 → 부서짐 → 돌덩이 1~2(할당량에 셈) + 확률로 광석 하나(등급별).
    ///  · 할당량 Quota 개 돌덩이를 모아 간수 하프에게 내면 풀려남(돌덩이는 거둬 감, 광석은 가져도 됨).
    /// 광석 확률(바위 하나당, 하나만): 구리(C) 35% · 철(B) 18% · 은(A) 8% · 금(S) 3% · 하프 사파이어 원석(SS) 1% · 없음 35%
    /// </summary>
    public static class MineMath
    {
        public const int HitsPerRock = 3, Quota = 12; public const float RespawnSeconds = 25f;
        public const string Rubble = "stone_rubble", Pickaxe = "prison_pickaxe";
        /// <summary>등급 낮은 것부터 (아이디, 확률).</summary>
        public static readonly (string id, float chance)[] Ores =
        {
            ("ore_copper", 0.35f), ("ore_iron", 0.18f), ("ore_silver", 0.08f), ("ore_gold", 0.03f), ("ore_sapphire", 0.01f),
        };
        /// <summary>0~1 난수 하나로 광석 고르기(없으면 null).</summary>
        public static string RollOre(float r)
        {
            float acc = 0f;
            foreach (var (id, ch) in Ores) { acc += ch; if (r < acc) return id; }
            return null;
        }
        /// <summary>돌덩이 개수(0~1 난수) — 70% 1개 · 30% 2개.</summary>
        public static int RollRubble(float r) => r < 0.3f ? 2 : 1;
        /// <summary>광석 등급 글자(C · B · A · S · SS).</summary>
        public static string Grade(string id) => id switch { "ore_copper" => "C", "ore_iron" => "B", "ore_silver" => "A", "ore_gold" => "S", "ore_sapphire" => "SS", _ => "" };
        public static bool QuotaMet(int rubble) => rubble >= Quota;
        public static string Progress(int rubble) => $"돌덩이 {System.Math.Min(rubble, Quota)} / {Quota}";
    }
}
