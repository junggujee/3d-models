using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    public enum ClashResult { PlayerWin, EnemyWin, Draw }
    /// <summary>StageCompare = 명세 v2: 지금 공격(코인 1개)의 위력만 비교. LimbusRounds = 림버스식: 양쪽 남은 코인을 매 라운드 다시 던져 지는 쪽이 코인 1개씩 잃음.</summary>
    public enum ClashMode { StageCompare, LimbusRounds }

    public class ClashOutcome
    {
        public string playerName, enemyName; public int playerPower, enemyPower, playerAffinity; public ClashResult result; public ClashMode mode; public bool parry;
        /// <summary>맞부딪힌 두 공격(가운데 연출용, 패링이면 null).</summary>
        public AttackContext playerAttack, enemyAttack;
        public int playerCoinsLeft, enemyCoinsLeft; public readonly List<(int player, int enemy, int pc, int ec)> rounds = new();
    }

    /// <summary>
    /// 양측 공격 위력 비교(명세 v2 7장) + <b>합 연출</b>. CombatResolver 가 양쪽 히트박스가 동시에 열려 있음을 확인한 뒤 호출.
    /// 판정은 부딪힌 순간에 끝나고(이미 던진 코인), 결과 적용은 연출 뒤:
    ///   부딪힘(금·노랑 불꽃 + 섬광 + 흔들림) → 둘 다 뒤로 물러남 → 다시 달려들어 부딪힘 … (라운드 수만큼, 최소 minExchanges 번)
    ///   → 결과: 승리 → 내 공격 유지(그대로 명중) + 승리 연출(큰 불꽃·슬로모션·상대 튕겨 나감), 패배 → 반대, 무승부 → 양쪽 공격 종료.
    /// 진 쪽은 남은 코인을 소모하지 않고 스킬을 중단. 정신력 승 +5 / 패 -5, 승리 에너지.
    /// 연출 동안 두 캐릭터는 CharacterStats.InClash + SkillExecutor.Held 로 묶임(이동·입력·AI·스킬 시간 정지).
    /// </summary>
    public class ClashManager : MonoBehaviour
    {
        static ClashManager instance;
        public static ClashManager Instance { get { if (instance == null) { var go = new GameObject("ClashManager"); instance = go.AddComponent<ClashManager>(); } return instance; } }
        public ClashMode mode = ClashMode.StageCompare;
        public float loserStagger = 1.0f, loserVulnerable = 1.3f, drawRecovery = 0.4f;
        [Header("합 연출")]
        [Tooltip("끄면 예전처럼 즉시 결과")] public bool choreography = true;
        [Tooltip("부딪히는 최소 횟수(StageCompare 는 라운드가 1개라 이 값만큼 부딪힘)")] [Min(1)] public int minExchanges = 2;
        public float recoilDistance = 1.1f, recoilTime = 0.16f, holdTime = 0.12f, dashTime = 0.1f;
        public float impactHitStop = 0.05f, winSlowMo = 0.35f, winSlowScale = 0.25f, loserKnockback = 2.2f;

        /// <summary>합 시작(가운데 패널). 패링도 이 이벤트(parry = true).</summary>
        public static event Action<ClashOutcome> Clashed;
        /// <summary>부딪힐 때마다: (결과, 이번이 몇 번째 부딪힘, 보여 줄 라운드 번호 — 아직 결과 전 부딪힘이면 -1).</summary>
        public static event Action<ClashOutcome, int, int> ClashExchange;
        /// <summary>결과 적용 직후(승리 연출).</summary>
        public static event Action<ClashOutcome> ClashEnded;

        void Awake() { if (instance == null) instance = this; }
        void OnDestroy() { if (instance == this) instance = null; }

        public ClashResult ComparePower(int playerPower, int enemyPower) => playerPower > enemyPower ? ClashResult.PlayerWin : playerPower < enemyPower ? ClashResult.EnemyWin : ClashResult.Draw;

        /// <summary>a·b 는 동시에 활성화된 두 공격. 플레이어 쪽을 player 로 정렬해서 처리.</summary>
        public ClashOutcome Resolve(AttackContext a, AttackContext b)
        {
            var p = a.owner.stats.team == Team.Player ? a : b; var e = p == a ? b : a;
            a.clashed = b.clashed = true;
            var o = new ClashOutcome { playerName = p.owner.stats.displayName, enemyName = e.owner.stats.displayName, mode = mode, playerAttack = p, enemyAttack = e };
            // 속성 상성: 유리한 쪽 클래시 위력 +2
            o.playerAffinity = SkillAttributes.Affinity(p.skill.attribute, e.skill.attribute);
            int pb = o.playerAffinity > 0 ? SkillAttributes.AdvantageClashBonus : 0, eb = o.playerAffinity < 0 ? SkillAttributes.AdvantageClashBonus : 0;
            if (mode == ClashMode.StageCompare)
            {
                o.playerPower = p.power + pb; o.enemyPower = e.power + eb; o.result = ComparePower(o.playerPower, o.enemyPower); o.rounds.Add((o.playerPower, o.enemyPower, 1, 1));
            }
            else LimbusRounds(p, e, o, pb, eb);

            Clashed?.Invoke(o);
            if (choreography && isActiveAndEnabled) StartCoroutine(Sequence(o, p, e)); else { ApplyResult(o, p, e); ClashEnded?.Invoke(o); }
            return o;
        }

        // ── 연출 ────────────────────────────────────
        IEnumerator Sequence(ClashOutcome o, AttackContext p, AttackContext e)
        {
            Lock(p, e, true);
            int n = Mathf.Max(minExchanges, o.rounds.Count), first = n - o.rounds.Count;   // 앞쪽 부딪힘은 결과 없이, 뒤쪽은 라운드 결과를 하나씩
            for (int i = 0; i < n; i++)
            {
                if (!Alive(p, e)) break;
                Face(p, e);
                bool last = i == n - 1; Impact(p, e, last ? 1.35f : 1f);
                ClashExchange?.Invoke(o, i, i >= first ? i - first : -1);
                if (last) break;
                yield return Wait(holdTime * 0.5f);
                // 둘 다 뒤로 물러났다가(감속) → 잠깐 멈춤 → 다시 달려들어(가속) 부딪힘
                Pose(p, "Guard", 0f); Pose(e, "Guard", 0f);                                     // 물러날 땐 막는 자세
                yield return Slide(p, e, -recoilDistance, recoilTime, easeOut: true);
                yield return Wait(holdTime);
                Pose(p, null, dashTime); Pose(e, null, dashTime);                                // 다시 공격 동작: 부딪히는 순간이 타격 프레임
                yield return Slide(p, e, recoilDistance, dashTime, easeOut: false);
            }
            Lock(p, e, false);
            ApplyResult(o, p, e);
            ClashEnded?.Invoke(o);
            if (o.result != ClashResult.Draw && Alive(p, e)) yield return Victory(o, p, e);
        }

        /// <summary>애니메이터가 있으면 자세 전환. state = null 이면 이 코인의 공격 동작을 '타격 프레임 - lead 초' 지점부터.</summary>
        static void Pose(AttackContext x, string state, float lead)
        {
            var an = x.owner != null ? x.owner.animator : null; if (an == null) return;
            if (state == null) { var anims = x.skill.attackAnimations; if (anims == null || anims.Length == 0) return; state = anims[x.coinIndex % anims.Length]; }
            if (!an.HasState(0, Animator.StringToHash(state))) return;
            an.CrossFadeInFixedTime(state, 0.04f, 0, state == "Guard" ? 0f : Mathf.Max(0f, x.skill.hitTime - lead));
        }

        static bool Alive(AttackContext p, AttackContext e) => p.owner != null && e.owner != null && p.owner.stats.IsAlive && e.owner.stats.IsAlive;
        static void Lock(AttackContext p, AttackContext e, bool on)
        {
            foreach (var x in new[] { p, e }) if (x.owner != null) { x.owner.Held = on; x.owner.stats.InClash = on; }
        }
        static void Face(AttackContext p, AttackContext e)
        {
            Vector3 d = e.owner.transform.position - p.owner.transform.position; d.y = 0; if (d.sqrMagnitude < 0.0001f) return;
            p.owner.transform.rotation = Quaternion.LookRotation(d); e.owner.transform.rotation = Quaternion.LookRotation(-d);
        }
        static Vector3 Mid(AttackContext p, AttackContext e) => (p.owner.transform.position + e.owner.transform.position) * 0.5f + Vector3.up * 0.9f;

        /// <summary>부딪힘: 금·노랑 불꽃이 가운데서 터지고 양옆으로 튐 + 섬광 + 짧은 히트스톱 + 화면 흔들림.</summary>
        void Impact(AttackContext p, AttackContext e, float k)
        {
            Vector3 mid = Mid(p, e), d = (e.owner.transform.position - p.owner.transform.position); d.y = 0; d = d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
            SparkBurst.Spawn(mid, Vector3.up, Mathf.RoundToInt(45 * k), 8f * k, SparkBurst.Gold);
            SparkBurst.Spawn(mid, -d + Vector3.up * 0.35f, Mathf.RoundToInt(26 * k), 7f * k, SparkBurst.Yellow);
            SparkBurst.Spawn(mid, d + Vector3.up * 0.35f, Mathf.RoundToInt(26 * k), 7f * k, SparkBurst.Yellow);
            ClashFlash.Spawn(mid, new Color(1f, 0.85f, 0.4f), 7f * k, 4.5f, 0.14f);
            HitStop.Do(impactHitStop * k); ThirdPersonCamera.Shake(0.07f * k, 0.18f);
        }

        /// <summary>두 캐릭터를 서로의 반대 방향(dist &lt; 0 = 물러남)으로 time 동안 이동. 게임 시간 기준(히트스톱이면 같이 멈춤).</summary>
        IEnumerator Slide(AttackContext p, AttackContext e, float dist, float time, bool easeOut)
        {
            Vector3 d = e.owner.transform.position - p.owner.transform.position; d.y = 0; d = d.sqrMagnitude > 0.0001f ? d.normalized : p.owner.transform.forward;
            float t = 0f, done = 0f;
            while (t < time)
            {
                if (UIState.Paused) { yield return null; continue; }
                if (!Alive(p, e)) yield break;
                t = Mathf.Min(time, t + Time.deltaTime); float u = t / time, f = easeOut ? 1f - (1f - u) * (1f - u) : u * u;
                float step = (f - done) * dist; done = f;
                Displace(p.owner.transform, d * step); Displace(e.owner.transform, -d * step);
                yield return null;
            }
        }
        static IEnumerator Wait(float seconds) { float t = 0f; while (t < seconds) { if (!UIState.Paused) t += Time.deltaTime; yield return null; } }

        /// <summary>캐릭터 이동(CharacterController → NavMeshAgent → transform 순).</summary>
        public static void Displace(Transform t, Vector3 delta)
        {
            if (t == null) return;
            var cc = t.GetComponent<CharacterController>(); if (cc != null && cc.enabled) { cc.Move(delta); return; }
            var ag = t.GetComponent<UnityEngine.AI.NavMeshAgent>(); if (ag != null && ag.enabled && ag.isOnNavMesh) { ag.Move(delta); return; }
            t.position += delta;
        }

        /// <summary>합 승리 연출: 진 쪽에 큰 금빛 폭발 + 섬광 + 슬로모션 + 흔들림, 진 쪽은 뒤로 튕겨 나가고 이긴 쪽은 반 걸음 밀고 들어감.</summary>
        IEnumerator Victory(ClashOutcome o, AttackContext p, AttackContext e)
        {
            var win = o.result == ClashResult.PlayerWin ? p : e; var lose = win == p ? e : p;
            Vector3 d = lose.owner.transform.position - win.owner.transform.position; d.y = 0; d = d.sqrMagnitude > 0.0001f ? d.normalized : win.owner.transform.forward;
            Vector3 at = lose.owner.transform.position + Vector3.up * 0.9f - d * 0.3f;
            Color c = win == p ? SparkBurst.Gold : SparkBurst.Crimson;
            SparkBurst.Spawn(at, Vector3.up, 80, 11f, c); SparkBurst.Spawn(at, d + Vector3.up * 0.4f, 50, 12f, SparkBurst.Yellow);
            ClashFlash.Spawn(at, win == p ? new Color(1f, 0.82f, 0.35f) : new Color(1f, 0.3f, 0.25f), 12f, 7f, 0.3f);
            ThirdPersonCamera.Shake(0.16f, 0.35f); HitStop.Do(winSlowMo, winSlowScale);
            float t = 0f, done = 0f, time = 0.28f;
            while (t < time)
            {
                if (lose.owner == null || win.owner == null) yield break;
                if (!UIState.Paused) t = Mathf.Min(time, t + Time.unscaledDeltaTime);
                float u = t / time, f = 1f - (1f - u) * (1f - u), step = f - done; done = f;
                Displace(lose.owner.transform, d * loserKnockback * step); Displace(win.owner.transform, d * 0.45f * step);
                yield return null;
            }
        }

        /// <summary>결과 적용: 이긴 공격은 그대로 명중, 진 쪽은 스킬 중단·경직. 무승부면 양쪽 종료.</summary>
        void ApplyResult(ClashOutcome o, AttackContext p, AttackContext e)
        {
            if (p.owner == null || e.owner == null) return;
            AttackContext win = o.result == ClashResult.PlayerWin ? p : o.result == ClashResult.EnemyWin ? e : null, lose = win == null ? null : (win == p ? e : p);
            Vector3 mid = Mid(p, e);
            if (win != null)
            {
                win.owner.stats.ChangeSanity(CharacterStats.ClashWin, "클래시 승리");
                if (win.owner.energy != null) win.owner.energy.Gain(win.owner.energy.onClashWinGain, "클래시 승리");
                lose.owner.stats.ChangeSanity(CharacterStats.ClashLose, "클래시 패배");
                lose.cancelled = true; lose.owner.Interrupt(); lose.owner.GetComponent<ICombatant>()?.Stagger(loserStagger, loserVulnerable);
                if (lose.owner.stats.IsAlive) win.owner.resolver.ApplyHit(win, lose.owner.stats);   // 이긴 공격은 그대로 명중
                if (!choreography) { SparkBurst.Spawn(mid, Vector3.up, 45, 7f, win == p ? SparkBurst.Gold : SparkBurst.Crimson); HitStop.Do(0.12f); }
            }
            else
            {
                p.cancelled = e.cancelled = true; p.owner.Interrupt(); e.owner.Interrupt();
                p.owner.GetComponent<ICombatant>()?.Stagger(drawRecovery); e.owner.GetComponent<ICombatant>()?.Stagger(drawRecovery);
                SparkBurst.Spawn(mid, Vector3.up, 30, 5f); HitStop.Do(0.08f);
            }
        }

        /// <summary>림버스식(선택): 지금 공격 + 남은 코인으로 라운드 진행, 매 라운드 남은 코인 전부 다시 던짐. 이긴 쪽 위력은 마지막 라운드 값.</summary>
        void LimbusRounds(AttackContext p, AttackContext e, ClashOutcome o, int pb, int eb)
        {
            int pc = p.skill.coinCount - p.coinIndex, ec = e.skill.coinCount - e.coinIndex, ps = p.owner.stats.currentSanity, es = e.owner.stats.currentSanity; int pp = p.power + pb, ep = e.power + eb;
            for (int round = 0; pc > 0 && ec > 0 && round < 12; round++)
            {
                if (round > 0) { pp = p.skill.basePower + CoinMath.TossMany(pc, ps) * p.skill.coinPower + pb; ep = e.skill.basePower + CoinMath.TossMany(ec, es) * e.skill.coinPower + eb; }
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
