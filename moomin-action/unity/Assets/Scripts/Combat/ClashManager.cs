using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>합에 참가하는 한쪽의 수치(그 순간 고정).</summary>
    public struct ClashSide { public string name; public int basePower, coinPower, coins, sanity; }
    public struct ClashRound { public int playerPower, enemyPower, playerHeads, enemyHeads, playerCoins, enemyCoins, winner; }   // winner: 1 플레이어, -1 적, 0 동점
    public enum ClashResult { PlayerWin, EnemyWin, Tie }

    public class ClashOutcome
    {
        public ClashSide player, enemy; public ClashResult result; public int playerCoinsLeft, enemyCoinsLeft; public bool guard, parry;
        public readonly List<ClashRound> rounds = new();
    }

    /// <summary>
    /// 림버스식 "합"(설계서 4장 + 추가 요청).
    ///   매 라운드: 양쪽이 <b>남은 코인을 전부</b> 던져 위력 P = B + 앞면×C 를 비교 → 진 쪽이 코인 1개를 잃음(동점이면 다시).
    ///   한쪽 코인이 0개가 될 때까지 반복(최대 12라운드, 넘으면 무승부).
    ///   이긴 쪽은 <b>남은 코인으로</b> 공격을 이어 감(코인마다 다시 던져 한 번씩 타격).
    /// 합 자체는 즉시 계산하고, 연출(히트스톱·HUD 라운드 표시)만 짧게 보여 줘서 실시간 흐름을 유지.
    /// </summary>
    public static class ClashManager
    {
        public const int MaxRounds = 12;
        public const float ParryBonus = 8f;
        public static event Action<ClashOutcome> Clashed;

        public static ClashOutcome Simulate(ClashSide p, ClashSide e, System.Random rng = null)
        {
            var o = new ClashOutcome { player = p, enemy = e }; int pc = p.coins, ec = e.coins;
            while (pc > 0 && ec > 0 && o.rounds.Count < MaxRounds)
            {
                int ph = CoinMath.TossMany(pc, p.sanity, rng), eh = CoinMath.TossMany(ec, e.sanity, rng);
                int pp = CoinMath.Power(p.basePower, p.coinPower, ph), ep = CoinMath.Power(e.basePower, e.coinPower, eh);
                int w = pp > ep ? 1 : pp < ep ? -1 : 0;
                o.rounds.Add(new ClashRound { playerPower = pp, enemyPower = ep, playerHeads = ph, enemyHeads = eh, playerCoins = pc, enemyCoins = ec, winner = w });
                if (w > 0) ec--; else if (w < 0) pc--;
            }
            o.playerCoinsLeft = pc; o.enemyCoinsLeft = ec;
            o.result = pc > 0 && ec <= 0 ? ClashResult.PlayerWin : ec > 0 && pc <= 0 ? ClashResult.EnemyWin : ClashResult.Tie;
            return o;
        }

        static ClashSide PlayerSide(SkillData s, int coins, SanitySystem san, int bonus = 0) => new ClashSide { name = "민미", basePower = s.basePower + bonus, coinPower = s.coinPower, coins = coins, sanity = san.Value };

        /// <summary>플레이어 공격 ↔ 적 공격 충돌. 결과 효과까지 적용하고 결과를 돌려줌.</summary>
        public static ClashOutcome ResolveAttack(AttackController ac, EnemyAI enemy)
        {
            var cm = ac.GetComponent<CombatManager>(); var san = ac.GetComponent<SanitySystem>(); var skills = ac.GetComponent<SkillManager>();
            var o = Simulate(PlayerSide(ac.Skill, ac.CoinsLeft, san), enemy.AsClashSide());
            Vector3 mid = (ac.transform.position + enemy.transform.position) * 0.5f + Vector3.up * 0.9f;
            switch (o.result)
            {
                case ClashResult.PlayerWin:
                    san.Add(SanitySystem.ClashWin, "합 승리"); enemy.OnClashLost(1.2f, 1.5f); SparkBurst.Spawn(mid, Vector3.up, 45, 7f, SparkBurst.Gold); HitStop.Do(0.12f); break;
                case ClashResult.EnemyWin:
                    san.Add(SanitySystem.ClashLose, "합 패배"); skills.Cancel(); cm.Stagger(0.5f, false); enemy.OnClashWon(cm, o.enemyCoinsLeft, 1f); SparkBurst.Spawn(mid, Vector3.up, 30, 5f, SparkBurst.Crimson); HitStop.Do(0.1f); break;
                default:
                    skills.Cancel(); cm.Recover(0.3f); enemy.OnClashTie(); SparkBurst.Spawn(mid, Vector3.up, 30, 5f); HitStop.Do(0.08f); break;
            }
            Clashed?.Invoke(o); return o;
        }

        /// <summary>방어(우클릭) 중 적 공격을 받음 → 방어 코인으로 합. 누른 직후면 패링(기본 위력 +8).</summary>
        public static ClashOutcome ResolveGuard(CombatManager cm, EnemyAI enemy, bool parry)
        {
            var san = cm.GetComponent<SanitySystem>(); var g = SkillLibrary.Get("GUARD");
            var o = Simulate(PlayerSide(g, g.coinCount, san, parry ? (int)ParryBonus : 0), enemy.AsClashSide()); o.guard = true; o.parry = parry;
            Vector3 mid = (cm.transform.position + enemy.transform.position) * 0.5f + Vector3.up * 0.9f;
            switch (o.result)
            {
                case ClashResult.PlayerWin:
                    if (parry) { san.Add(SanitySystem.ClashWin, "패링"); enemy.OnClashLost(1.0f, 1.5f); HitStop.Do(0.15f); SparkBurst.Spawn(mid, Vector3.up, 55, 8f, SparkBurst.Gold); }
                    else { san.Add(SanitySystem.GuardBlock, "방어"); enemy.OnClashTie(); SparkBurst.Spawn(mid, Vector3.up, 20, 4f); }
                    break;
                case ClashResult.EnemyWin:
                    san.Add(SanitySystem.ClashLose, "방어 붕괴"); cm.Stagger(0.4f, false); enemy.OnClashWon(cm, o.enemyCoinsLeft, 0.5f); SparkBurst.Spawn(mid, Vector3.up, 25, 4f, SparkBurst.Crimson); break;
                default: enemy.OnClashTie(); SparkBurst.Spawn(mid, Vector3.up, 15, 3f); break;
            }
            Clashed?.Invoke(o); return o;
        }
    }
}
