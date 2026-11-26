using System.Collections.Generic;

namespace MoominAction
{
    /// <summary>
    /// 대장간 계산(순수 C# — UnityEngine 없음, tools/status_tests 로 검증): 강화 수치 + 3 × 3 작업대 제작법 맞추기.
    ///
    /// ■ 강화: +0 ~ +5. 돈만 내면 누구나, 다만 올라갈수록 비싸지고 잘 실패한다.
    ///   성공률  +1 95% · +2 80% · +3 60% · +4 40% · +5 25%
    ///   비용    기준값 × (1·2·4·7·12)  (기준값 = 무기 가격의 1/4, 최소 40)
    ///   실패    (풍선고래는 무기를 부수지 않는다 — "내 손에서 부서진 적은 없다구!")
    ///            +3 미만: 그대로 (돈만 날림)
    ///            +3 이상: 한 단계 내려감
    ///   효과    단계마다 공격 배율 +8%, 두 단계마다 최종 위력 +1, 받는 피해 -1.5%
    /// </summary>
    public static class ForgeMath
    {
        public const int MaxLevel = 5;
        static readonly float[] Chance = { 0.95f, 0.80f, 0.60f, 0.40f, 0.25f };
        static readonly int[] CostMul = { 1, 2, 4, 7, 12 };

        /// <summary>+level → +(level+1) 성공 확률(0~1). 범위 밖이면 0.</summary>
        public static float SuccessChance(int level) => level < 0 || level >= MaxLevel ? 0f : Chance[level];
        /// <summary>강화 비용(골드). value = 무기의 기본 가격.</summary>
        public static int Cost(int level, int value)
        {
            if (level < 0 || level >= MaxLevel) return 0;
            int baseCost = value / 4; if (baseCost < 40) baseCost = 40;
            return baseCost * CostMul[level];
        }
        /// <summary>실패했을 때 떨어지는 단계(+3 이상에서만 하락).</summary>
        public static int OnFail(int level) => level >= 3 ? level - 1 : level;
        /// <summary>강화 단계로 붙는 공격 배율(1.0 = 그대로).</summary>
        public static float DamageBonus(int level) => 1f + 0.08f * Clamp(level);
        /// <summary>강화 단계로 붙는 최종 위력(두 단계마다 +1).</summary>
        public static int PowerBonus(int level) => Clamp(level) / 2;
        /// <summary>강화 단계로 붙는 받는 피해 감소(0.015 = 1.5%).</summary>
        public static float GuardBonus(int level) => 0.015f * Clamp(level);
        public static int Clamp(int level) => level < 0 ? 0 : level > MaxLevel ? MaxLevel : level;
        /// <summary>이름 앞에 붙일 글자("+3 " · +0 이면 빈 글자).</summary>
        public static string Prefix(int level) => Clamp(level) > 0 ? "+" + Clamp(level) + " " : "";

        // ── 제작(3 × 3 작업대 — 마인크래프트처럼 재료를 모양에 맞게 놓으면 결과가 나옴) ─────────────
        //    칸 하나 = 재료 1개. 모양은 작업대 어디에 놓아도 되고(위치 이동), 좌우를 뒤집어도 됨.
        //    모양 없는 제작법(shapeless)은 칸 위치와 상관없이 재료 종류·개수만 맞으면 됨.

        /// <summary>제작법 한 줄.</summary>
        public class Recipe
        {
            public string id, result, note; public int count = 1;
            /// <summary>모양(위에서 아래로 행, '.' = 빈칸, 글자 = keys 의 재료). null 이면 모양 없는 제작법.</summary>
            public string[] pattern;
            /// <summary>글자 → 재료 아이디. 예: "A=bat_alloy B=bear_bone"</summary>
            public string keys;
            /// <summary>모양 없는 제작법의 재료(같은 재료를 여러 번 적으면 그만큼).</summary>
            public string[] shapeless;
            /// <summary>특별 제작 연출(레이저 블레이더처럼 큰 물건).</summary>
            public bool showcase;

            public string Key(char c)
            {
                if (string.IsNullOrEmpty(keys)) return null;
                foreach (var part in keys.Split(' '))
                    if (part.Length > 2 && part[0] == c && part[1] == '=') return part.Substring(2);
                return null;
            }
            /// <summary>재료별 필요 개수.</summary>
            public Dictionary<string, int> Needs()
            {
                var d = new Dictionary<string, int>();
                void Add(string id) { if (string.IsNullOrEmpty(id)) return; d.TryGetValue(id, out var n); d[id] = n + 1; }
                if (pattern != null) { foreach (var row in pattern) foreach (var c in row) if (c != '.') Add(Key(c)); }
                else if (shapeless != null) foreach (var id in shapeless) Add(id);
                return d;
            }
        }

        public const int GridSize = 3, GridCells = 9;

        /// <summary>작업대 9칸(행 우선, 빈칸 = null/"") 에 맞는 제작법. 없으면 null.</summary>
        public static Recipe Match(IList<string> grid)
        {
            if (grid == null || grid.Count < GridCells) return null;
            int r0 = GridSize, r1 = -1, c0 = GridSize, c1 = -1, filled = 0;
            for (int i = 0; i < GridCells; i++)
            {
                if (string.IsNullOrEmpty(grid[i])) continue;
                int r = i / GridSize, c = i % GridSize; filled++;
                if (r < r0) r0 = r; if (r > r1) r1 = r; if (c < c0) c0 = c; if (c > c1) c1 = c;
            }
            if (filled == 0) return null;
            int h = r1 - r0 + 1, w = c1 - c0 + 1;
            foreach (var rc in Recipes)
            {
                if (rc.pattern == null)
                {
                    if (rc.shapeless == null || rc.shapeless.Length != filled) continue;
                    var need = rc.Needs(); var have = new Dictionary<string, int>();
                    for (int i = 0; i < GridCells; i++) if (!string.IsNullOrEmpty(grid[i])) { have.TryGetValue(grid[i], out var n); have[grid[i]] = n + 1; }
                    if (SameCounts(need, have)) return rc;
                    continue;
                }
                if (rc.pattern.Length != h || rc.pattern[0].Length != w) continue;
                if (Fits(rc, grid, r0, c0, h, w, false) || Fits(rc, grid, r0, c0, h, w, true)) return rc;
            }
            return null;
        }
        static bool Fits(Recipe rc, IList<string> grid, int r0, int c0, int h, int w, bool mirror)
        {
            for (int r = 0; r < h; r++)
                for (int c = 0; c < w; c++)
                {
                    char k = rc.pattern[r][mirror ? w - 1 - c : c];
                    string want = k == '.' ? null : rc.Key(k), got = grid[(r0 + r) * GridSize + c0 + c];
                    if (string.IsNullOrEmpty(want) != string.IsNullOrEmpty(got)) return false;
                    if (!string.IsNullOrEmpty(want) && want != got) return false;
                }
            return true;
        }
        static bool SameCounts(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a) if (!b.TryGetValue(kv.Key, out var n) || n != kv.Value) return false;
            return true;
        }
        /// <summary>제작법을 작업대에 놓는 자리(가운데 정렬) — 제작법 책에서 자동으로 채울 때·흐린 안내 그림.</summary>
        public static string[] Layout(Recipe rc)
        {
            var g = new string[GridCells];
            if (rc == null) return g;
            if (rc.pattern != null)
            {
                int h = rc.pattern.Length, w = rc.pattern[0].Length, or = (GridSize - h) / 2, oc = (GridSize - w) / 2;
                for (int r = 0; r < h; r++) for (int c = 0; c < w; c++) { char k = rc.pattern[r][c]; if (k != '.') g[(or + r) * GridSize + oc + c] = rc.Key(k); }
            }
            else if (rc.shapeless != null)
            {
                int[] order = { 4, 3, 5, 1, 7, 0, 2, 6, 8 };   // 가운데부터
                for (int i = 0; i < rc.shapeless.Length && i < GridCells; i++) g[order[i]] = rc.shapeless[i];
            }
            return g;
        }

        /// <summary>풍선고래 대장간 작업대의 제작법.</summary>
        public static readonly List<Recipe> Recipes = new()
        {
            new Recipe { id = "craft_laser", result = "laser_blader", showcase = true, note = "방망이 조각을 대각선으로, 반대 대각선엔 사골곰뼈, 한가운데 무지개 진주. 푸른 광선검이 깨어난다.",
                         pattern = new[] { "A.B", ".C.", "B.A" }, keys = "A=bat_alloy B=bear_bone C=rainbow_pearl" },
            new Recipe { id = "craft_peanut", result = "peanut_bread", note = "땅콩빵 조각 9개로 작업대를 꽉 채우면 동그란 땅콩빵이 된다.",
                         pattern = new[] { "PPP", "PPP", "PPP" }, keys = "P=peanut_piece" },
            new Recipe { id = "craft_honey", result = "honey_peanut_bread", note = "땅콩빵 + 꿀. 어디에 놓아도 된다. 성문 지기 맨두가 제일 좋아하는 빵.",
                         shapeless = new[] { "peanut_bread", "honey" } },
            new Recipe { id = "craft_spin", result = "spin_blades", note = "네 귀퉁이에 별 조각, 가운데 줄에 불씨 가루 · 은빛 깃털 · 불씨 가루.",
                         pattern = new[] { "S.S", "DFD", "S.S" }, keys = "S=shard_star D=dust_fire F=feather_silver" },
            new Recipe { id = "craft_pillow", result = "pillow_shield", note = "별 조각 사이에 은빛 깃털을 세로로 두 줄.",
                         pattern = new[] { "SFS", "SFS" }, keys = "S=shard_star F=feather_silver" },
            new Recipe { id = "craft_headband", result = "headband_blue", note = "별 조각 · 은빛 깃털 · 별 조각을 한 줄로.",
                         pattern = new[] { "SFS" }, keys = "S=shard_star F=feather_silver" },
            new Recipe { id = "craft_pearl_ring", result = "ring_pearl", note = "무지개 진주를 별 조각 네 개로 감싼다.",
                         pattern = new[] { ".S.", "SPS", ".S." }, keys = "S=shard_star P=rainbow_pearl" },
            new Recipe { id = "craft_core_charm", result = "charm_tremor", note = "맨두의 진동 코어 위아래로 불씨 가루.",
                         pattern = new[] { "D", "M", "D" }, keys = "D=dust_fire M=mandu_core" },
        };
        public static Recipe GetRecipe(string id) { foreach (var r in Recipes) if (r.id == id) return r; return null; }
    }
}
