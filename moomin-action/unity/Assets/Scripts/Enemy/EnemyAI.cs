using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    public enum EnemyState { Idle, Chase, Windup, Strike, Recovery, Stagger, Dead }
    /// <summary>Pattern = 정해진 순서로 공격(일반 적), Reactive = 플레이어 상태를 보고 공격을 고름(보스).</summary>
    public enum EnemyBehavior { Pattern, Reactive }

    /// <summary>
    /// 그림자 적: 쫓아와서 공격을 <b>예고</b>(바닥 경고)한 뒤 코인을 던져 공격한다.
    ///  · 플레이어 공격과 부딪히면 <b>합</b>(ClashManager) — 지면 경직(받는 피해 ×1.5 반격 기회), 이기면 남은 코인으로 플레이어를 때림.
    ///  · 부딪히지 않으면 일방 공격: 코인마다 한 번씩 타격(Pᵢ = B + 앞면 누적×C).
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class EnemyAI : MonoBehaviour
    {
        public static readonly List<EnemyAI> All = new();
        public static event Action<EnemyAI, CoinResult, int> AnyCoinTossed;   // (적, 코인, 지금 위력)
        public static event Action<EnemyAI> AnyWindup;

        public string displayName = "그림자"; public EnemyBehavior behavior = EnemyBehavior.Pattern; public bool isBoss;
        [UnityEngine.Range(-45, 45)] public int sanity = 0; public float powerScale = 1f;
        public float speed = 2.4f, aggroRange = 25f, gravity = -20f, attackCooldown = 0.6f; public int score = 10;
        public List<EnemyAttack> attacks = new() { EnemyAttack.Bump(), EnemyAttack.Bump(), EnemyAttack.Slam() };
        public Transform visual;

        public EnemyState State { get; private set; }
        public EnemyAttack CurrentAttack { get; private set; }
        public EnemyAttack PlannedAttack { get; private set; }
        public bool IsStriking => State == EnemyState.Strike && !strikeResolved;
        public float DamageTakenMultiplier => Time.time < vulnerableUntil ? vulnerableMult : 1f;
        public float WindupProgress => State == EnemyState.Windup && CurrentAttack != null ? Mathf.Clamp01(stateT / CurrentAttack.windup) : 0f;
        public float AimHeight => isBoss ? 1.5f : 0.6f;
        public Health Health => hp;

        Transform target; CombatManager targetCm; SanitySystem targetSanity; CharacterController cc; Health hp; Vector3 knock, vel;
        float stateT, staggerDur, vulnerableUntil, vulnerableMult = 1f, nextAttack, deadT; int patternIdx; bool strikeResolved; Coroutine hitsCo; TelegraphIndicator tele; Vector3 baseScale;

        void OnEnable() { All.Add(this); } void OnDisable() { All.Remove(this); }
        void Awake()
        {
            cc = GetComponent<CharacterController>(); hp = GetComponent<Health>(); if (!visual) visual = transform.childCount > 0 ? transform.GetChild(0) : transform; baseScale = visual.localScale;
            hp.Damaged += (a, p, k) => { knock = k * (isBoss ? 0.25f : 1f); if (State != EnemyState.Windup && State != EnemyState.Strike) visual.localScale = baseScale * 0.9f; };
            hp.Died += OnDied;
        }
        void Start() { var p = GameObject.FindGameObjectWithTag("Player"); if (p) { target = p.transform; targetCm = p.GetComponent<CombatManager>(); targetSanity = p.GetComponent<SanitySystem>(); } State = EnemyState.Chase; PlannedAttack = Choose(); }

        /// <summary>보스로 바꿈(GameManager 가 웨이브 3마다 호출): 체력 ×6, 크기 ×1.8, 위력 ×1.35, 상황 대응형 AI.</summary>
        public void ConfigureBoss()
        {
            isBoss = true; behavior = EnemyBehavior.Reactive; displayName = "거대한 그림자"; sanity = 10; powerScale = 1.35f; speed *= 0.85f; score = 100;
            hp.maxHealth *= 6f; hp.ResetHealth(); transform.localScale *= 1.8f;
            attacks = new List<EnemyAttack> { EnemyAttack.Bump(), EnemyAttack.Slam(), EnemyAttack.GuardBreak(), EnemyAttack.Lunge(), EnemyAttack.Flurry() };
            foreach (var a in attacks) { a.range *= 1.5f; a.radius *= 1.6f; }
            PlannedAttack = Choose();
        }

        public ClashSide AsClashSide() => new ClashSide { name = displayName, basePower = Mathf.RoundToInt(CurrentAttack.basePower * powerScale), coinPower = CurrentAttack.coinPower, coins = CurrentAttack.coinCount, sanity = sanity };

        EnemyAttack Choose()
        {
            if (attacks.Count == 0) attacks.Add(EnemyAttack.Bump());
            if (behavior == EnemyBehavior.Pattern || target == null) return attacks[patternIdx++ % attacks.Count];
            EnemyAttack Find(string n) { foreach (var a in attacks) if (a.name == n) return a; return null; }
            float d = Vector3.Distance(target.position, transform.position); var st = targetCm != null ? targetCm.State : CombatState.Idle;
            if (st == CombatState.Guard && Find("방어 부수기") != null) return Find("방어 부수기");                   // 막고 있으면 방어 불가 공격
            if (d > 4.5f && Find("돌진") != null) return Find("돌진");                                              // 멀면 돌진
            if (targetSanity != null && targetSanity.Value <= -25 && Find("난타") != null) return Find("난타");     // 흔들릴 때 몰아붙임
            if (st == CombatState.Attack && UnityEngine.Random.value < 0.6f) return Find("몸통 박치기") ?? attacks[0];   // 공격 중이면 빠른 공격으로 합을 노림
            return attacks[UnityEngine.Random.Range(0, attacks.Count)];
        }

        void Update()
        {
            if (UIState.Paused) return;
            if (State == EnemyState.Dead) { deadT += Time.deltaTime; visual.localScale = baseScale * Mathf.Max(0, 1 - deadT * 2.5f); if (deadT > 0.4f) Destroy(gameObject); return; }
            knock = Vector3.Lerp(knock, Vector3.zero, 8f * Time.deltaTime); stateT += Time.deltaTime;
            Vector3 move = Vector3.zero; Vector3 to = target ? target.position - transform.position : Vector3.zero; to.y = 0; float d = to.magnitude;
            switch (State)
            {
                case EnemyState.Idle:
                case EnemyState.Chase:
                    if (!target || d > aggroRange) break;
                    Face(to, 10f);
                    if (d > PlannedAttack.range * 0.9f) { move = to.normalized * speed; Squash(Mathf.Abs(Mathf.Sin(Time.time * 8f))); }
                    else if (Time.time >= nextAttack) BeginWindup(PlannedAttack);
                    break;
                case EnemyState.Windup:
                    Face(to, 4f); float w = WindupProgress; visual.localScale = Vector3.Lerp(baseScale, new Vector3(baseScale.x * 1.18f, baseScale.y * 0.78f, baseScale.z * 1.18f), w);
                    if (stateT >= CurrentAttack.windup) { State = EnemyState.Strike; stateT = 0; strikeResolved = false; visual.localScale = new Vector3(baseScale.x * 0.85f, baseScale.y * 1.2f, baseScale.z * 0.85f); tele?.Hide(); tele = null; }
                    break;
                case EnemyState.Strike:
                    move = transform.forward * CurrentAttack.lunge * Mathf.Max(0, 1f - stateT / CurrentAttack.strikeTime);
                    if (!strikeResolved && stateT >= CurrentAttack.impactDelay) Impact(d, to);
                    if (stateT >= CurrentAttack.strikeTime && hitsCo == null) EnterRecovery(CurrentAttack.recovery);
                    break;
                case EnemyState.Recovery:
                    visual.localScale = Vector3.Lerp(visual.localScale, baseScale, 8f * Time.deltaTime);
                    if (stateT >= staggerDur) { State = EnemyState.Chase; stateT = 0; PlannedAttack = Choose(); nextAttack = Time.time + attackCooldown; }
                    break;
                case EnemyState.Stagger:
                    visual.localScale = baseScale * (1f + 0.06f * Mathf.Sin(stateT * 30f)) ; visual.localRotation = Quaternion.Euler(0, 0, 12f * Mathf.Sin(stateT * 18f));
                    if (stateT >= staggerDur) { visual.localRotation = Quaternion.identity; visual.localScale = baseScale; State = EnemyState.Chase; stateT = 0; PlannedAttack = Choose(); }
                    break;
            }
            if (cc.isGrounded && vel.y < 0) vel.y = -2f; vel.y += gravity * Time.deltaTime;
            cc.Move((move + knock + new Vector3(0, vel.y, 0)) * Time.deltaTime);
        }

        void Face(Vector3 to, float speedK) { if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), speedK * Time.deltaTime); }
        void Squash(float bob) { visual.localScale = Vector3.Lerp(visual.localScale, new Vector3(baseScale.x * (1 + 0.08f * bob), baseScale.y * (1 - 0.12f * bob), baseScale.z * (1 + 0.08f * bob)), 12f * Time.deltaTime); }

        void BeginWindup(EnemyAttack a)
        {
            CurrentAttack = a; State = EnemyState.Windup; stateT = 0; strikeResolved = false;
            float r = a.aoe ? a.radius : Mathf.Max(a.radius, 1f); Vector3 off = a.aoe ? Vector3.zero : Vector3.forward * (a.range * 0.55f / transform.localScale.z);
            tele = TelegraphIndicator.Show(transform, off, r / transform.localScale.x, a.windup, a.unblockable ? new Color(0.65f, 0.2f, 1f, 0.45f) : new Color(1f, 0.15f, 0.1f, 0.4f));
            AnyWindup?.Invoke(this);
        }

        void Impact(float d, Vector3 to)
        {
            bool inRange = target && d <= CurrentAttack.range + 0.6f && (CurrentAttack.aoe || Vector3.Angle(transform.forward, to) < 75f);
            if (inRange && targetCm != null) targetCm.ReceiveEnemyAttack(this);   // 여기서 합이 일어나면 OnClash* 가 상태를 바꿈
            strikeResolved = true;
        }

        void EnterRecovery(float seconds) { State = EnemyState.Recovery; stateT = 0; staggerDur = seconds; }

        // ── 합/일방 공격 결과(ClashManager, CombatManager 가 호출)
        public void OnClashLost(float stagger, float vulnerable) { strikeResolved = true; Stagger(stagger, vulnerable); }
        public void OnClashTie() { strikeResolved = true; StopHits(); EnterRecovery(0.6f); }
        public void OnClashWon(CombatManager cm, int coinsLeft, float damageScale) { strikeResolved = true; HitPlayerOneSided(cm, coinsLeft, damageScale); }
        public void OnAttackEvaded() { strikeResolved = true; }

        public void HitPlayerOneSided(CombatManager cm, int coins, float damageScale) { StopHits(); if (coins > 0) hitsCo = StartCoroutine(Hits(cm, coins, damageScale)); }

        IEnumerator Hits(CombatManager cm, int coins, float damageScale)
        {
            var a = CurrentAttack; int heads = 0, bp = Mathf.RoundToInt(a.basePower * powerScale);
            for (int i = 0; i < coins; i++)
            {
                if (State == EnemyState.Dead || State == EnemyState.Stagger || cm == null) break;
                var r = new CoinResult { index = i, sanity = sanity, probability = CoinMath.HeadsProbability(sanity), heads = CoinMath.Toss(sanity) };
                if (r.heads) heads++; int p = CoinMath.Power(bp, a.coinPower, heads); AnyCoinTossed?.Invoke(this, r, p);
                if (cm.TakeEnemyHit(this, p, a.damageMultiplier * damageScale, a.knockback)) SparkBurst.Spawn(cm.transform.position + Vector3.up * 0.8f, Vector3.up, r.heads ? 22 : 12, 4f, r.heads ? SparkBurst.Crimson : SparkBurst.Violet);
                yield return new WaitForSeconds(0.14f);
            }
            hitsCo = null;
        }
        void StopHits() { if (hitsCo != null) { StopCoroutine(hitsCo); hitsCo = null; } }

        /// <summary>경직: 그동안 받는 피해 × vulnerable(반격 기회).</summary>
        public void Stagger(float seconds, float vulnerable = 1f)
        {
            if (State == EnemyState.Dead) return; StopHits(); tele?.Hide(); tele = null;
            State = EnemyState.Stagger; stateT = 0; staggerDur = isBoss ? seconds * 0.6f : seconds; vulnerableUntil = Time.time + staggerDur; vulnerableMult = vulnerable;
        }

        /// <summary>드랍: 금화 항상, 별조각 40%, 붉은 약병 25%, 불꽃 가루 30%, 은빛 깃털 8%, 무기 3%(보스는 더 많이).</summary>
        void DropLoot()
        {
            Vector3 p = transform.position + Vector3.up * 0.5f; int rolls = isBoss ? 4 : 1;
            ItemPickup.Spawn("gold", UnityEngine.Random.Range(5, 16) * (isBoss ? 8 : 1), p);
            for (int k = 0; k < rolls; k++)
            {
                if (UnityEngine.Random.value < 0.40f) ItemPickup.Spawn("shard_star", 1, p); if (UnityEngine.Random.value < 0.25f) ItemPickup.Spawn("potion_red", 1, p);
                if (UnityEngine.Random.value < 0.30f) ItemPickup.Spawn("dust_fire", UnityEngine.Random.Range(1, 4), p); if (UnityEngine.Random.value < 0.08f) ItemPickup.Spawn("feather_silver", 1, p);
                if (UnityEngine.Random.value < (isBoss ? 0.25f : 0.03f)) ItemPickup.Spawn(UnityEngine.Random.value < 0.5f ? "sword_ember" : "sword_frost", 1, p);
            }
        }

        void OnDied() { StopHits(); tele?.Hide(); tele = null; State = EnemyState.Dead; deadT = 0; GameManager.Instance?.AddScore(score); DropLoot(); }
    }
}
