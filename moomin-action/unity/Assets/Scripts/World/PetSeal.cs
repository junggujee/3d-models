using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    public enum PetCommand { Follow, Stay }

    /// <summary>
    /// 펫 하프물범(blender/build_seal.py). 자연(초록 구역)에 야생으로 나타나고, 땅콩빵 조각을 3번 건네면(F) 길들여짐(한 마리만).
    /// 길들이면:
    ///  · 따라오기 / 기다려(G) — 멀어지면 달려오고, 너무 멀면(26m) 곁으로 순간 이동
    ///  · 전투 보조 — 주인의 조준 대상(없으면 가까운 적)을 몽실 박치기(근접) · 물방울 뿜기(원거리, 침잠)로 공격. 위력이 낮아 일방 공격이 주력
    ///  · 막아서기 — 주인을 노리는 적의 예고(windup)를 보면 사이로 뛰어들어(친밀도가 높을수록 자주) 타이밍을 맞춰 박치기 → 적의 공격이 물범에게 오고, 둘 다 공격 중이면 합
    ///  · 응원 — 주인 체력이 30% 아래면 '꾸잉' 하고 체력 회복(90초마다, 친밀도 80 이상이면 더 많이)
    ///  · 물어 오기 — 주인에게서 먼 곳에 떨어진 아이템·금화를 물어 옴
    ///  · 냄새 맡기 — 한가할 때 가까운 땅콩빵 냄새를 맡고 방향을 알려 줌
    ///  · 쓰다듬기·간식(F) — 친밀도 ↑(간식은 체력도 회복), 기절 — 쓰러지면 20초 뒤 절반 체력으로 일어남(죽지 않음)
    ///  · 친밀도 0~100 · 레벨(전투로 경험치): 레벨마다 최대 체력 +10, 2레벨마다 코인 최종 위력 +1
    /// 저장: SaveData.pet(이름·레벨·경험치·친밀도·체력) → 불러오면 NatureSpawner 가 곁에 다시 데려옴.
    /// </summary>
    [RequireComponent(typeof(CharacterStats), typeof(NavMeshAgent))]
    public class PetSeal : MonoBehaviour
    {
        public static PetSeal Current { get; private set; }
        public static readonly List<PetSeal> All = new();
        /// <summary>펫 소식(HUD 알림).</summary>
        public static event Action<string> Notice;

        public string petName = "몽실이"; public bool tamed; public int level = 1; public long exp; [Range(0, 100)] public float affection;
        public const int FeedToTame = 3; public int fed;
        public PetCommand command = PetCommand.Follow;
        public SkillDefinition bump, bubble;
        public float followDistance = 2.2f, assistRange = 9f, teleportDistance = 26f, healCooldown = 90f, sniffInterval = 45f, interactRange = 2.6f;
        public float baseHealth = 60f, walkSpeed = 3.3f, runSpeed = 6.6f;

        NavMeshAgent agent; Animator anim; CharacterStats stats; SkillExecutor ex; Transform player; CharacterStats owner; Inventory inv; LockOnSystem ownerLock;
        Vector3 home; float nextWander, nextHeal, nextSniff, faintUntil = -1, nextIntercept, nextPet, idleSince, nextThink; bool intercepting; EnemyCombatController interceptFrom; ItemPickup fetching; string playing = "";

        public CharacterStats Stats => stats; public bool Fainted => faintUntil > 0;
        public float FaintLeft => Fainted ? Mathf.Max(0, faintUntil - Time.time) : 0;
        public long ExpToNext => 40L * level;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>(); stats = GetComponent<CharacterStats>(); ex = GetComponent<SkillExecutor>(); anim = GetComponentInChildren<Animator>();
            if (agent) { agent.updateRotation = true; agent.angularSpeed = 540f; agent.acceleration = 18f; agent.stoppingDistance = 0.5f; }
            stats.Died += OnFaint; home = transform.position; nextSniff = Time.time + sniffInterval; ApplyLevel(false);
        }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); if (Current == this) Current = null; if (CombatResolver.Interceptor == stats) CombatResolver.Interceptor = null; }
        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) { player = p.transform; owner = p.GetComponent<CharacterStats>(); inv = p.GetComponent<Inventory>(); ownerLock = p.GetComponent<LockOnSystem>(); }
            if (tamed) Current = this;
        }

        // ── 길들이기 · 상호작용 ─────────────────────
        public void Tame(bool announce)
        {
            tamed = true; Current = this; command = PetCommand.Follow; affection = Mathf.Max(affection, 30); stats.team = Team.Player;
            if (announce) { Play("Happy"); Hearts(14); Say($"{Ga} 친구가 되었다!  G 기다려·따라와"); }
        }

        /// <summary>불러오기 등으로 상태를 넣고 바로 길들인 상태로.</summary>
        public void Restore(string name, int lv, long xp, float aff, float health)
        {
            petName = string.IsNullOrEmpty(name) ? petName : name; level = Mathf.Max(1, lv); exp = Math.Max(0, xp); affection = aff; Tame(false); ApplyLevel(false);
            if (health > 0) stats.currentHealth = Mathf.Min(health, stats.maxHealth);
        }

        void ApplyLevel(bool heal)
        {
            float before = stats.maxHealth; stats.level = level; stats.maxHealth = baseHealth + 10f * (level - 1); stats.equipFinalPower = (level - 1) / 2;
            if (heal) stats.currentHealth = Mathf.Min(stats.maxHealth, stats.currentHealth + (stats.maxHealth - before)); else stats.currentHealth = Mathf.Min(stats.currentHealth <= 0 ? stats.maxHealth : stats.currentHealth, stats.maxHealth);
        }
        public void GainExp(long amount)
        {
            if (!tamed) return; exp += amount;
            while (exp >= ExpToNext && level < 20) { exp -= ExpToNext; level++; ApplyLevel(true); Hearts(10); Say($"{petName} 레벨 업!  Lv.{level}"); }
        }
        public void AddAffection(float a) { float b = affection; affection = Mathf.Clamp(affection + a, 0, 100); if (b < 50 && affection >= 50) Say($"{Ga} 민미를 무척 믿는다 — 더 자주 막아서 준다"); if (b < 80 && affection >= 80) Say($"{petName}의 응원이 더 따뜻해졌다"); }

        void HandleInteraction(float distToPlayer)
        {
            if (owner == null || !owner.IsAlive || distToPlayer > interactRange) return;
            bool hasPiece = inv != null && inv.Contains("peanut_piece");
            if (!tamed)
            {
                if (Current != null && Current != this) { Interaction.Offer(this, distToPlayer, $"이미 {Josa(Current.petName, "과", "와")} 함께하고 있다", null); return; }
                if (!hasPiece) { Interaction.Offer(this, distToPlayer, "야생 하프물범 — 땅콩빵 조각이 있으면 길들일 수 있다", null); return; }
                Interaction.Offer(this, distToPlayer, $"F   땅콩빵 조각 주기  ({fed}/{FeedToTame})", () =>
                {
                    if (!inv.Remove("peanut_piece", 1)) return; fed++; Play("Eat"); Hearts(4);
                    if (fed >= FeedToTame) Tame(true); else Say($"하프물범이 땅콩빵 조각을 먹었다  ({fed}/{FeedToTame})");
                });
                return;
            }
            if (Fainted) { Interaction.Offer(this, distToPlayer, $"{petName} 기절 — {FaintLeft:0}초 뒤 일어남", null); return; }
            Interaction.Offer(this, distToPlayer, hasPiece ? $"F   {petName}에게 간식 주기" : $"F   {petName} 쓰다듬기", () =>
            {
                if (Time.time < nextPet) return; nextPet = Time.time + 2.5f;
                if (hasPiece && inv.Remove("peanut_piece", 1)) { stats.Heal(stats.maxHealth * 0.3f); AddAffection(5); Play("Eat"); Hearts(6); Say($"{petName}: 냠냠! 체력 회복 · 친밀도 {affection:0}"); }
                else { AddAffection(2); Play("Happy"); Hearts(5); }
            });
        }

        // ── 매 프레임 ───────────────────────────
        void Update()
        {
            if (UIState.Paused) return;
            if (player == null) { Start(); if (player == null) return; }
            float d = Vector3.Distance(transform.position, player.position);
            HandleInteraction(d);
            if (anim) anim.SetFloat("Speed", agent != null && agent.enabled ? Mathf.Clamp01(agent.velocity.magnitude / runSpeed) : 0f);
            if (Fainted) { if (Time.time >= faintUntil) Revive(); return; }
            if (!tamed) { Wander(); return; }
            if (tamed && Current == this && InputAdapter.PetCommandPressed()) { command = command == PetCommand.Follow ? PetCommand.Stay : PetCommand.Follow; Say(command == PetCommand.Stay ? $"{petName}: 여기서 기다릴게!" : $"{petName}: 따라갈게!"); Play("Happy"); }
            if (ex != null && ex.IsExecuting) { FaceTarget(); return; }
            if (owner != null && !owner.IsAlive) { Stop(); return; }
            if (d > teleportDistance && command == PetCommand.Follow) { Warp(player.position - player.forward * 1.5f); return; }
            if (Time.time < nextThink) return; nextThink = Time.time + 0.12f;
            if (TryIntercept()) return;
            if (TryAssist()) return;
            CombatResolver.Interceptor = null; intercepting = false;
            if (TryCheer()) return;
            if (TryFetch()) return;
            TrySniff();
            if (command == PetCommand.Stay) { Stop(); IdleNap(); return; }
            Vector3 side = Vector3.Cross(Vector3.up, player.forward).normalized; Vector3 spot = player.position - player.forward * followDistance * 0.7f - side * followDistance * 0.7f;
            if (d > followDistance + 0.8f) MoveTo(spot, d > 6f ? runSpeed : walkSpeed); else { Stop(); IdleNap(); }
        }

        // ── 야생: 집 둘레를 어슬렁, 가끔 졸기
        void Wander()
        {
            if (Time.time < nextWander) return; nextWander = Time.time + UnityEngine.Random.Range(4f, 9f);
            if (UnityEngine.Random.value < 0.25f) { Stop(); Play("Sleep"); return; }
            var r = UnityEngine.Random.insideUnitCircle * 6f; MoveTo(home + new Vector3(r.x, 0, r.y), walkSpeed * 0.6f);
        }
        void IdleNap() { if (agent != null && agent.velocity.sqrMagnitude > 0.05f) idleSince = Time.time; if (Time.time - idleSince > 18f && playing != "Sleep") Play("Sleep"); }

        // ── 막아서기: 주인을 노리는 예고 중인 적 → 사이로 → 적 첫 타격 시각에 맞춰 박치기
        bool TryIntercept()
        {
            if (bump == null || ex == null || owner == null || Time.time < nextIntercept) { return intercepting && interceptFrom != null && Block(); }
            if (!intercepting)
            {
                foreach (var e in EnemyCombatController.All)
                {
                    if (e == null || !e.Stats.IsAlive || e.Executor == null || e.Executor.Phase != SkillPhase.Windup) continue;
                    if (e.Target != player || (e.transform.position - player.position).sqrMagnitude > 25f || (e.transform.position - transform.position).sqrMagnitude > 64f) continue;
                    var s = e.Executor.CurrentSkill; if (s == null || s.isAreaAttack || s.isProjectile) continue;
                    float chance = 0.3f + 0.4f * affection / 100f; nextIntercept = Time.time + 1.5f;
                    if (UnityEngine.Random.value > chance) return false;
                    intercepting = true; interceptFrom = e; Say($"{Ga} 막아선다!"); break;
                }
            }
            return intercepting && Block();
        }
        bool Block()
        {
            var e = interceptFrom; if (e == null || !e.Stats.IsAlive || e.Executor == null || (!e.Executor.IsExecuting)) { intercepting = false; CombatResolver.Interceptor = null; return false; }
            Vector3 to = (e.transform.position - player.position); to.y = 0; Vector3 spot = player.position + to.normalized * Mathf.Min(1.3f, to.magnitude * 0.5f);
            CombatResolver.Interceptor = stats;
            if ((transform.position - spot).sqrMagnitude > 0.25f) MoveTo(spot, runSpeed * 1.2f); else Stop();
            FaceTo(e.transform.position);
            // 적의 첫 타격 시각 ≈ 내 박치기 타격 시각이 되도록 시작(양쪽 히트박스가 겹치면 합)
            float mine = bump.windup + bump.hitTime, theirs = e.Executor.TimeToFirstHit;
            if (e.Executor.Phase == SkillPhase.Windup && theirs <= mine + 0.05f && ex.IsReady(bump)) { ex.UseSkill(bump); GainExp(2); }
            return true;
        }

        // ── 전투 보조: 주인 조준 대상 → 없으면 주인 근처의 가장 가까운 적
        bool TryAssist()
        {
            EnemyCombatController target = ownerLock != null ? ownerLock.Target : null;
            if (target == null || !target.Stats.IsAlive)
            {
                float best = assistRange * assistRange; target = null;
                foreach (var e in EnemyCombatController.All) { if (e == null || !e.Stats.IsAlive) continue; float dd = (e.transform.position - player.position).sqrMagnitude; if (dd < best) { best = dd; target = e; } }
            }
            if (target == null) return false;
            float dist = Vector3.Distance(transform.position, target.transform.position); FaceTo(target.transform.position);
            if (bump != null && dist <= bump.range * 0.95f && ex.IsReady(bump)) { Stop(); ex.UseSkill(bump); GainExp(3); return true; }
            if (bubble != null && dist <= bubble.range * 0.95f && dist > 2.5f && ex.IsReady(bubble)) { Stop(); ex.UseSkill(bubble); GainExp(3); return true; }
            MoveTo(target.transform.position - (target.transform.position - transform.position).normalized * 1.2f, runSpeed); return true;
        }

        // ── 응원(회복)
        bool TryCheer()
        {
            if (owner == null || !owner.IsAlive || Time.time < nextHeal || owner.HealthRatio > 0.3f) return false;
            nextHeal = Time.time + healCooldown; float amt = owner.maxHealth * (affection >= 80 ? 0.22f : 0.15f); owner.Heal(amt);
            Play("Happy"); Hearts(12); SparkBurst.Spawn(player.position + Vector3.up, Vector3.up, 30, 3f, new Color(1f, 0.55f, 0.7f)); Say($"{petName}: 꾸잉! (체력 +{amt:0})"); return true;
        }

        // ── 물어 오기: 주인에게서 먼 아이템
        bool TryFetch()
        {
            if (fetching == null)
            {
                foreach (var p in FindObjectsOfTypeAll())
                {
                    if (p == null) continue; float dp = Vector3.Distance(p.transform.position, player.position);
                    if (dp > 4f && dp < 14f && Vector3.Distance(p.transform.position, transform.position) < 16f) { fetching = p; break; }
                }
                if (fetching == null) return false;
            }
            if (Vector3.Distance(transform.position, fetching.transform.position) < 0.9f)
            {
                var d = ItemDatabase.Get(fetching.id); bool ok = fetching.id == "gold" ? Give(() => inv.AddGold(fetching.amount)) : inv != null && inv.Add(fetching.id, fetching.amount) > 0;
                if (ok) { Say($"{Ga} {Eul(fetching.id == "gold" ? "금화" : d != null ? d.name : "아이템")} 물어 왔다"); Destroy(fetching.gameObject); AddAffection(0.5f); }
                fetching = null; return true;
            }
            MoveTo(fetching.transform.position, runSpeed); return true;
        }
        bool Give(Action a) { if (inv == null) return false; a(); return true; }
        static ItemPickup[] FindObjectsOfTypeAll()
        {
#if UNITY_2023_1_OR_NEWER
            return FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
#else
            return FindObjectsOfType<ItemPickup>();
#endif
        }

        // ── 냄새 맡기: 가까운 땅콩빵 방향 알려 주기
        void TrySniff()
        {
            if (Time.time < nextSniff) return; nextSniff = Time.time + sniffInterval;
            ResourceNode best = null; float bd = 60f * 60f;
            foreach (var n in ResourceNode.All) { if (n == null) continue; float dd = (n.transform.position - player.position).sqrMagnitude; if (dd < bd) { bd = dd; best = n; } }
            if (best == null) return;
            var cam = Camera.main; Vector3 to = best.transform.position - player.position; to.y = 0; Vector3 fwd = cam != null ? cam.transform.forward : player.forward; fwd.y = 0;
            float ang = Vector3.SignedAngle(fwd, to, Vector3.up); string dir = Mathf.Abs(ang) < 35 ? "앞쪽" : Mathf.Abs(ang) > 145 ? "뒤쪽" : ang > 0 ? "오른쪽" : "왼쪽";
            Play("Eat"); Say($"{petName}: 킁킁! 땅콩빵 냄새 — {dir} {Mathf.Sqrt(bd):0}m");
        }

        // ── 기절 · 회복
        void OnFaint()
        {
            faintUntil = Time.time + 20f; Stop(); Play("Death"); if (CombatResolver.Interceptor == stats) CombatResolver.Interceptor = null; intercepting = false;
            if (ex != null) ex.Interrupt(); Say($"{Ga} 기절했다… 20초 뒤 일어난다");
        }
        void Revive() { faintUntil = -1; stats.currentHealth = stats.maxHealth * 0.5f; Play("Happy"); Say($"{Ga} 다시 일어났다!"); }

        // ── 도우미
        void MoveTo(Vector3 p, float speed) { if (agent == null || !agent.enabled || !agent.isOnNavMesh) return; agent.speed = speed; agent.isStopped = false; agent.SetDestination(p); if (playing == "Sleep") Play("Locomotion"); idleSince = Time.time; }
        void Stop() { if (agent != null && agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); } }
        void Warp(Vector3 p) { if (NavMesh.SamplePosition(p, out var h, 4f, NavMesh.AllAreas)) { if (agent != null && agent.enabled) agent.Warp(h.position); else transform.position = h.position; } }
        void FaceTo(Vector3 p) { Vector3 to = p - transform.position; to.y = 0; if (to.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 12f * Time.deltaTime); }
        void FaceTarget() { if (intercepting && interceptFrom != null) FaceTo(interceptFrom.transform.position); }
        void Play(string state) { if (anim == null || playing == state && state != "Happy" && state != "Eat") return; playing = state; int h = Animator.StringToHash(state); if (anim.HasState(0, h)) anim.CrossFadeInFixedTime(h, 0.12f); }
        void Hearts(int n) { SparkBurst.Spawn(transform.position + Vector3.up * 0.6f, Vector3.up, n, 2.2f, new Color(1f, 0.45f, 0.65f)); }
        void Say(string m) => Notice?.Invoke(m);
        string Ga => Josa(petName, "이", "가");
        /// <summary>받침이 있으면 a, 없으면 b 를 붙임(몽실이 → 몽실이가, 단단 → 단단이).</summary>
        public static string Josa(string w, string withBatchim, string without)
        {
            if (string.IsNullOrEmpty(w)) return w; char c = w[w.Length - 1];
            bool batchim = c >= 0xAC00 && c <= 0xD7A3 && (c - 0xAC00) % 28 != 0; return w + (batchim ? withBatchim : without);
        }
        static string Eul(string w) => Josa(w, "을", "를");
    }
}
