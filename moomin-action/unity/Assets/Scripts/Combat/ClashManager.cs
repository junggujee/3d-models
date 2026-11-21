using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    public enum ClashResult { PlayerWin, EnemyWin, Draw }
    /// <summary>StageCompare = 명세 v2: 지금 공격(코인 1개)의 위력만 비교. LimbusRounds = 림버스식: 양쪽 남은 코인을 매 라운드 다시 던져 지는 쪽이 코인 1개씩 잃음.</summary>
    public enum ClashMode { StageCompare, LimbusRounds }

    public class ClashOutcome
    {
        public string playerName, enemyName; public int playerPower, enemyPower; public ClashResult result; public ClashMode mode; public bool parry;
        public int playerCoinsLeft, enemyCoinsLeft; public readonly List<(int player, int enemy, int pc, int ec)> rounds = new();
    }

    /// <summary>
    /// 양측 공격 위력 비교(명세 v2 7장). CombatResolver 가 양쪽 히트박스가 동시에 열려 있음을 확인한 뒤 호출.
    /// 승리 → 내 공격 유지(그 공격이 그대로 명중), 상대 공격 취소·경직 · 패배 → 반대 · 무승부 → 양쪽 공격 종료.
    /// 진 쪽은 남은 코인을 소모하지 않고 스킬을 중단. 정신력 승 +5 / 패 -5.
    /// </summary>
    public class ClashManager : MonoBehaviour
    {
        static ClashManager instance;
        public static ClashManager Instance { get { if (instance == null) { var go = new GameObject("ClashManager"); instance = go.AddComponent<ClashManager>(); } return instance; } }
        public ClashMode mode = ClashMode.StageCompare;
        public float loserStagger = 1.0f, loserVulnerable = 1.3f, drawRecovery = 0.4f;
        public static event Action<ClashOutcome> Clashed;
        void Awake() { if (instance == null) instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }

        public ClashResult ComparePower(int playerPower, int enemyPower) => playerPower > enemyPower ? ClashResult.PlayerWin : playerPower < enemyPower ? ClashResult.EnemyWin : ClashResult.Draw;

        /// <summary>a·b 는 동시에 활성화된 두 공격. 플레이어 쪽을 player 로 정렬해서 처리.</summary>
        public ClashOutcome Resolve(AttackContext a, AttackContext b)
        {
            var p = a.owner.stats.team == Team.Player ? a : b; var e = p == a ? b : a;
            a.clashed = b.clashed = true;
            var o = new ClashOutcome { playerName = p.owner.stats.displayName, enemyName = e.owner.stats.displayName, mode = mode };
            if (mode == ClashMode.StageCompare)
            {
                o.playerPower = p.power; o.enemyPower = e.power; o.result = ComparePower(p.power, e.power); o.rounds.Add((p.power, e.power, 1, 1));
            }
            else LimbusRounds(p, e, o);

            AttackContext win = o.result == ClashResult.PlayerWin ? p : o.result == ClashResult.EnemyWin ? e : null, lose = win == null ? null : (win == p ? e : p);
            Vector3 mid = (p.owner.transform.position + e.owner.transform.position) * 0.5f + Vector3.up * 0.9f;
            if (win != null)
            {
                win.owner.stats.ChangeSanity(CharacterStats.ClashWin, "클래시 승리"); lose.owner.stats.ChangeSanity(CharacterStats.ClashLose, "클래시 패배");
                lose.cancelled = true; lose.owner.Interrupt(); lose.owner.GetComponent<ICombatant>()?.Stagger(loserStagger, loserVulnerable);
                win.owner.resolver.ApplyHit(win, lose.owner.stats);                                   // 이긴 공격은 그대로 명중
                SparkBurst.Spawn(mid, Vector3.up, 45, 7f, win == p ? SparkBurst.Gold : SparkBurst.Crimson); HitStop.Do(0.12f);
            }
            else
            {
                p.cancelled = e.cancelled = true; p.owner.Interrupt(); e.owner.Interrupt();
                p.owner.GetComponent<ICombatant>()?.Stagger(drawRecovery); e.owner.GetComponent<ICombatant>()?.Stagger(drawRecovery);
                SparkBurst.Spawn(mid, Vector3.up, 30, 5f); HitStop.Do(0.08f);
            }
            Clashed?.Invoke(o); return o;
        }

        /// <summary>림버스식(선택): 지금 공격 + 남은 코인으로 라운드 진행, 매 라운드 남은 코인 전부 다시 던짐. 이긴 쪽 위력은 마지막 라운드 값.</summary>
        void LimbusRounds(AttackContext p, AttackContext e, ClashOutcome o)
        {
            int pc = p.skill.coinCount - p.coinIndex, ec = e.skill.coinCount - e.coinIndex, ps = p.owner.stats.currentSanity, es = e.owner.stats.currentSanity; int pp = p.power, ep = e.power;
            for (int round = 0; pc > 0 && ec > 0 && round < 12; round++)
            {
                if (round > 0) { pp = p.skill.basePower + CoinMath.TossMany(pc, ps) * p.skill.coinPower; ep = e.skill.basePower + CoinMath.TossMany(ec, es) * e.skill.coinPower; }
                o.rounds.Add((pp, ep, pc, ec)); if (pp > ep) ec--; else if (pp < ep) pc--;
            }
            o.playerPower = pp; o.enemyPower = ep; o.playerCoinsLeft = pc; o.enemyCoinsLeft = ec;
            o.result = pc > 0 && ec <= 0 ? ClashResult.PlayerWin : ec > 0 && pc <= 0 ? ClashResult.EnemyWin : ClashResult.Draw;
        }

        /// <summary>패링도 HUD 에 클래시처럼 표시.</summary>
        public static void RaiseParry(CharacterStats defender, CharacterStats attacker)
        {
            bool playerParried = defender.team == Team.Player;
            Clashed?.Invoke(new ClashOutcome { parry = true, playerName = playerParried ? defender.displayName : attacker.displayName, enemyName = playerParried ? attacker.displayName : defender.displayName, result = playerParried ? ClashResult.PlayerWin : ClashResult.EnemyWin });
        }
    }
}
