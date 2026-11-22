namespace MoominAction
{
    /// <summary>
    /// 여정 기록(저장 파일에 함께 저장 — 불러오면 이어서 셈): 처치 수·합 승패·최고 위력. 게임 오버 결과 화면이 보여 줌.
    /// GameManager 가 Hook/Unhook(정적 이벤트 구독), 적 처치는 EnemyCombatController 가 Kills++.
    /// </summary>
    public static class RunStats
    {
        public static int Kills, ClashWins, ClashLosses, BestPower, BossKills;
        public static void Reset() { Kills = ClashWins = ClashLosses = BestPower = BossKills = 0; }

        static bool hooked;
        public static void Hook()
        {
            if (hooked) return; hooked = true;
            ClashManager.ClashEnded += OnClash; CombatResolver.DamageDealt += OnDamage;
        }
        public static void Unhook()
        {
            if (!hooked) return; hooked = false;
            ClashManager.ClashEnded -= OnClash; CombatResolver.DamageDealt -= OnDamage;
        }
        static void OnClash(ClashOutcome o)
        {
            if (o == null || o.parry) return;
            if (o.result == ClashResult.PlayerWin) ClashWins++; else if (o.result == ClashResult.EnemyWin) ClashLosses++;
        }
        static void OnDamage(AttackContext a, CharacterStats target, int dmg)
        {
            if (a == null || a.owner == null || target == null || target.team != Team.Enemy) return;
            if (a.power > BestPower) BestPower = a.power;
        }
    }
}
