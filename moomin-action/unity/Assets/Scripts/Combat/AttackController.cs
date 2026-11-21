using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 스킬의 타격 하나를 실행: (1) 공격 중인 적과 부딪히면 합 → (2) 이번 타격의 코인 판정 → (3) 명중한 대상에게 피해.
    /// 코인은 <b>타격 시점에 하나씩</b> 확정하고, 빗나가도 소모(설계서 3-2). 타격마다 위력 Pᵢ = B + (지금까지 앞면 수)×C, 피해 D = P×M×R.
    /// 판정 후 정신력이 바뀌어도 이미 나온 결과는 바뀌지 않음(던질 때의 정신력으로 판정).
    /// </summary>
    [RequireComponent(typeof(CoinSystem), typeof(SanitySystem))]
    public class AttackController : MonoBehaviour
    {
        public Transform sparkOrigin; public LayerMask hitMask = ~0; public float weaponMultiplier = 1f;
        public SkillData Skill { get; private set; }
        public int CoinsLeft { get; private set; } public int Heads { get; private set; } public int Tossed { get; private set; } public int CoinsLost { get; private set; } public int LastPower { get; private set; }
        public event Action<SkillData, CoinResult, int> CoinResolved;   // (스킬, 코인 결과, 지금 위력)
        public event Action<SkillData, int> CoinsDestroyed;              // 합에서 잃은 코인 수
        public event Action<IDamageable, int> DamageDealt;
        CoinSystem coin; SanitySystem sanity;
        readonly List<(Collider col, IDamageable dmg, EnemyAI enemy)> targets = new();

        void Awake() { coin = GetComponent<CoinSystem>(); sanity = GetComponent<SanitySystem>(); }

        public void BeginSkill(SkillData s) { Skill = s; CoinsLeft = s.coinCount; Heads = 0; Tossed = 0; CoinsLost = 0; LastPower = s.basePower; }
        public void EndSkill() { Skill = null; }

        /// <summary>애니메이션 이벤트용(설계서 7-3): 현재 스킬의 다음 코인 하나만 판정.</summary>
        public void ExecuteCoin() { if (Skill != null && CoinsLeft > 0) TossOne(); }

        bool TossOne()
        {
            var r = coin.Toss(sanity.Value, Tossed); if (r.heads) Heads++; Tossed++; CoinsLeft--;
            LastPower = CoinMath.Power(Skill.basePower, Skill.coinPower, Heads); CoinResolved?.Invoke(Skill, r, LastPower); return r.heads;
        }

        /// <summary>타격 하나 실행. forcedClash 가 있으면(적 공격이 먼저 닿을 때) 그 적과 반드시 합. 합이 일어났으면 true.</summary>
        public bool ExecuteHit(int hitIndex, EnemyAI forcedClash = null)
        {
            if (Skill == null || CoinsLeft <= 0) return false;
            var s = Skill; Vector3 origin = transform.position + Vector3.up * 0.8f; targets.Clear();
            foreach (var c in Physics.OverlapSphere(origin, s.range, hitMask, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(transform)) continue;
                var d = c.GetComponentInParent<IDamageable>(); if (d == null || d.IsDead) continue;
                bool dup = false; foreach (var t in targets) if (t.dmg == d) dup = true; if (dup) continue;
                Vector3 to = c.bounds.center - origin; to.y = 0;
                if (s.angle < 360f && Vector3.Angle(transform.forward, to) > s.angle * 0.5f && to.magnitude > 0.8f) continue;
                targets.Add((c, d, c.GetComponentInParent<EnemyAI>()));
            }

            // (1) 합
            EnemyAI clashWith = forcedClash;
            if (clashWith == null && s.clashEnabled) foreach (var t in targets) if (t.enemy != null && t.enemy.IsStriking) { clashWith = t.enemy; break; }
            bool clashed = false;
            if (clashWith != null && s.clashEnabled && clashWith.IsStriking)
            {
                clashed = true; int before = CoinsLeft;
                var o = ClashManager.ResolveAttack(this, clashWith);
                if (o.result != ClashResult.PlayerWin) return true;            // 스킬은 ClashManager 가 취소
                CoinsLeft = o.playerCoinsLeft; CoinsLost += before - CoinsLeft; if (before > CoinsLeft) CoinsDestroyed?.Invoke(s, before - CoinsLeft);
                bool has = false; foreach (var t in targets) if (t.enemy == clashWith) has = true;
                if (!has) { var col = clashWith.GetComponent<Collider>(); targets.Add((col, clashWith.Health, clashWith)); }
            }

            // (2) 코인 판정: PerHit = 이번 타격에 1개, SingleMotion = 남은 코인 전부
            int n = s.mode == CoinMode.PerHit ? 1 : CoinsLeft; bool lastHeads = false;
            for (int i = 0; i < n && CoinsLeft > 0; i++) lastHeads = TossOne();
            bool allHeads = CoinsLeft == 0 && CoinsLost == 0 && Heads == s.coinCount;

            // (3) 피해
            float mult = s.damageMultiplier * weaponMultiplier * sanity.DamageDealtMultiplier; bool any = false;
            foreach (var t in targets)
            {
                if (t.dmg == null || t.dmg.IsDead) continue;
                float def = t.enemy != null ? t.enemy.DamageTakenMultiplier : 1f; int dmg = DamageCalculator.Final(LastPower, mult, def);
                Vector3 to = (t.col != null ? t.col.bounds.center : origin + transform.forward) - origin; to.y = 0;
                Vector3 hp = t.col != null ? t.col.ClosestPoint(origin + transform.forward * 0.5f) : origin + transform.forward;
                t.dmg.TakeDamage(dmg, hp, to.normalized * s.knockback + Vector3.up * 2f); DamageDealt?.Invoke(t.dmg, dmg); any = true;
                SparkBurst.Spawn(hp, -to.normalized + Vector3.up * 0.5f, lastHeads ? 40 : 22, lastHeads ? 7f : 4.5f, lastHeads ? SparkBurst.Gold : (Color?)null);
                if (allHeads && s.coinEffect == CoinEffect.StaggerOnAllHeads && sanity.ExtraEffectsEnabled && t.enemy != null) t.enemy.Stagger(1.6f, 1.3f);
            }
            if (any) sanity.Add(SanitySystem.HitLanded, "명중");
            else if (sparkOrigin) SparkBurst.Spawn(sparkOrigin.position, transform.forward, 8, 3f, lastHeads ? SparkBurst.Gold : (Color?)null);
            if (allHeads && s.coinEffect == CoinEffect.SanityOnAllHeads && sanity.ExtraEffectsEnabled) sanity.Add(10, s.skillName + " 전부 앞면");
            return clashed;
        }
    }
}
