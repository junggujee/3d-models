using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 베개 방패 고유 효과 '포근한 보호막': 내가 합에서 이기면 최대 체력의 8% 보호막(최대 25%, 8초 — 다시 얻으면 갱신).
    /// 보호막은 CharacterStats.barrier(직접 피해를 먼저 막음). HUD 체력바 위 하얀 솜털 막(VitalBars).
    /// </summary>
    public class ShieldBarrier : MonoBehaviour
    {
        public const float PerWin = 0.08f, Cap = 0.25f, Duration = 8f;
        CharacterStats stats; SpriteRenderer aura; static Sprite soft;
        void Awake() { stats = GetComponent<CharacterStats>(); }
        void OnEnable() { ClashManager.ClashEnded += OnClash; }
        void OnDisable() { ClashManager.ClashEnded -= OnClash; if (stats != null) stats.ClearBarrier(); if (aura != null) aura.enabled = false; }
        /// <summary>보호막이 있는 동안 몸 둘레에 하얀 솜털 빛(카메라를 향함, 남은 양만큼 진하게·살랑).</summary>
        void LateUpdate()
        {
            if (stats == null) return; float k = stats.maxHealth > 0 ? Mathf.Clamp01(stats.barrier / (stats.maxHealth * Cap)) : 0f;
            if (aura == null)
            {
                if (k <= 0f) return;
                if (soft == null) { int n = 64; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false); var c = new Color[n * n]; for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { float d = new Vector2(i - 31.5f, j - 31.5f).magnitude / 32f; float a = Mathf.Clamp01(1f - d); a = a * a * (0.6f + 0.4f * Mathf.SmoothStep(0.55f, 0.85f, d)); c[j * n + i] = new Color(1f, 0.99f, 0.95f, a); } tex.SetPixels(c); tex.Apply(); soft = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 32f); }
                var go = new GameObject("BarrierAura"); go.transform.SetParent(transform, false); aura = go.AddComponent<SpriteRenderer>(); aura.sprite = soft; aura.sortingOrder = 5;
            }
            aura.enabled = k > 0f; if (k <= 0f) return;
            var cam = Camera.main; aura.transform.position = transform.position + Vector3.up * 0.75f + (cam != null ? (cam.transform.position - transform.position).normalized * 0.2f : Vector3.zero);
            if (cam != null) aura.transform.rotation = Quaternion.LookRotation(aura.transform.position - cam.transform.position);
            aura.transform.localScale = Vector3.one * (1.15f + 0.04f * Mathf.Sin(Time.time * 3f)); aura.color = new Color(1, 1, 1, 0.25f + 0.35f * k);
        }
        void OnClash(ClashOutcome o)
        {
            if (stats == null || o == null || o.parry) return;
            var win = o.result == ClashResult.PlayerWin ? o.playerAttack : o.result == ClashResult.EnemyWin ? o.enemyAttack : null;
            if (win == null || win.owner == null || win.owner.stats != stats) return;
            stats.AddBarrier(stats.maxHealth * PerWin, stats.maxHealth * Cap, Duration);
            SparkBurst.Spawn(transform.position + Vector3.up, Vector3.up, 26, 3.5f, new Color(1f, 0.98f, 0.92f));
        }
    }

    /// <summary>
    /// 회전 쌍검 고유 효과 '결투 고조': 명중(공격마다 한 번) · 합 승리 · 완벽 방어마다 1스택(최대 10).
    /// 스택마다 피해 +2%(SkillExecutor.bonusMultiplier) · 쌍검 회전이 빨라짐(WeaponSpin). 최대면 궁극 - 회절원참격(requiresDuelHeat) — 쓰면 0.
    /// 5초 동안 치지도 맞지도 않으면 1초에 1스택씩 식음.
    /// </summary>
    public class DuelHeat : MonoBehaviour
    {
        public const int MaxStacks = 10; public const float PerStack = 0.02f, CoolDelay = 5f, CoolPerSecond = 1f;
        public int Stacks { get; private set; }
        public bool Full => Stacks >= MaxStacks;
        public float Ratio => (float)Stacks / MaxStacks;
        public event Action<int> Changed;
        public static DuelHeat Player { get; private set; }
        CharacterStats stats; SkillExecutor ex; DefenseController def; float lastAction, coolAcc; int lastAttackId = -1;

        void Awake() { stats = GetComponent<CharacterStats>(); ex = GetComponent<SkillExecutor>(); def = GetComponent<DefenseController>(); }
        void OnEnable()
        {
            CombatResolver.DamageDealt += OnHit; ClashManager.ClashEnded += OnClash; DefenseController.AnyGuardResolved += OnGuard;
            if (stats != null) stats.Damaged += OnDamaged; if (stats != null && stats.team == Team.Player) Player = this; Set(0);
        }
        void OnDisable()
        {
            CombatResolver.DamageDealt -= OnHit; ClashManager.ClashEnded -= OnClash; DefenseController.AnyGuardResolved -= OnGuard;
            if (stats != null) stats.Damaged -= OnDamaged; if (Player == this) Player = null; Set(0);
        }
        void OnDamaged(float d, Vector3 p, Vector3 k) => lastAction = Time.time;
        void Start() { if (ex != null) { ex.SkillStarted += OnSkill; ex.CoinAttacked += OnCoin; } }
        void OnDestroy() { if (ex != null) { ex.SkillStarted -= OnSkill; ex.CoinAttacked -= OnCoin; } }
        /// <summary>궁극 - 회절원참격 연출: 시작에 붉은 해방 불꽃 + 흔들림, 갈아 버리는 코인마다 앞쪽에 불꽃·작은 흔들림(마지막 내려찍기는 충격파 — vfxLastCoinOnly).</summary>
        void OnSkill(SkillDefinition s) { if (!enabled || s == null || !s.requiresDuelHeat) return; SparkBurst.Spawn(transform.position + Vector3.up, Vector3.up, 60, 8f, new Color(1f, 0.25f, 0.2f)); ThirdPersonCamera.Shake(0.1f, 0.3f); }
        void OnCoin(AttackContext a)
        {
            if (!enabled || a == null || a.skill == null || !a.skill.requiresDuelHeat || a.coinIndex >= a.skill.coinCount - 1) return;
            Vector3 p = transform.position + transform.forward * 0.9f + Vector3.up * 0.7f;
            SparkBurst.Spawn(p, transform.forward + Vector3.up * 0.3f, 34, 7f, a.isHeads ? new Color(1f, 0.35f, 0.25f) : new Color(1f, 0.85f, 0.6f)); ThirdPersonCamera.Shake(0.05f, 0.12f);
        }
        void OnHit(AttackContext a, CharacterStats target, int dmg) { if (a == null || a.owner != ex || a.id == lastAttackId) return; lastAttackId = a.id; Add(1); }
        void OnClash(ClashOutcome o)
        {
            if (o == null || o.parry) return; var win = o.result == ClashResult.PlayerWin ? o.playerAttack : o.result == ClashResult.EnemyWin ? o.enemyAttack : null;
            if (win != null && win.owner == ex) Add(1);
        }
        void OnGuard(DefenseController d, bool perfect, int gp, int ap) { if (d == def && perfect) Add(1); }

        public void Add(int n) { lastAction = Time.time; Set(Mathf.Clamp(Stacks + n, 0, MaxStacks)); }
        public void Consume() { lastAction = Time.time; Set(0); }
        void Set(int v)
        {
            bool full = Stacks < MaxStacks && v >= MaxStacks; Stacks = v; if (ex != null) ex.bonusMultiplier = 1f + PerStack * Stacks; Changed?.Invoke(Stacks);
            if (full) SparkBurst.Spawn(transform.position + Vector3.up, Vector3.up, 40, 6f, new Color(1f, 0.3f, 0.25f));   // 최대: 붉은 불꽃
        }
        void Update()
        {
            if (UIState.Paused || Stacks <= 0 || Time.time - lastAction < CoolDelay) { coolAcc = 0; return; }
            coolAcc += CoolPerSecond * Time.deltaTime; if (coolAcc >= 1f) { coolAcc -= 1f; Set(Stacks - 1); }
        }
    }

    /// <summary>
    /// 회전 쌍검의 날 돌리기: 장착 모델(Weapons/SpinBlade_*)의 "Rotor*" 를 손잡이("Grip*")→날 축으로 돌림.
    /// 속도 = 기본 300°/초 + 결투 고조 스택마다 +140°/초, 공격 중엔 ×1.6, 방어 중엔 ×2(앞에서 빠르게 돌며 막음).
    /// </summary>
    public class WeaponSpin : MonoBehaviour
    {
        public float baseSpeed = 300f, perStack = 140f;
        DuelHeat heat; SkillExecutor ex; CharacterStats stats; float scanT;
        readonly System.Collections.Generic.List<(Transform grip, Transform rotor)> rotors = new();
        void Awake() { heat = GetComponent<DuelHeat>(); ex = GetComponent<SkillExecutor>(); stats = GetComponent<CharacterStats>(); }
        void Scan()
        {
            rotors.Clear(); Transform gR = null, gL = null, rR = null, rL = null;
            foreach (var t in GetComponentsInChildren<Transform>())
            {
                if (!t.gameObject.activeInHierarchy) continue;
                if (t.name.StartsWith("Grip_R")) gR = t; else if (t.name.StartsWith("Grip_L")) gL = t; else if (t.name.StartsWith("Rotor_R")) rR = t; else if (t.name.StartsWith("Rotor_L")) rL = t;
            }
            if (gR != null && rR != null) rotors.Add((gR, rR)); if (gL != null && rL != null) rotors.Add((gL, rL));
        }
        void LateUpdate()
        {
            if ((scanT -= Time.unscaledDeltaTime) <= 0f || rotors.Count == 0) { scanT = 1f; Scan(); }
            float sp = baseSpeed + perStack * (heat != null ? heat.Stacks : 0);
            if (ex != null && ex.IsExecuting) sp *= 1.6f; if (stats != null && stats.Guarding) sp *= 2f;
            float dt = Time.timeScale > 0 ? Time.deltaTime : Time.unscaledDeltaTime * 0.3f;   // 메뉴(멈춤) 중엔 캐릭터 시트에서 천천히
            foreach (var (g, r) in rotors) { if (g == null || r == null) continue; var axis = r.position - g.position; if (axis.sqrMagnitude < 1e-6f) axis = g.forward; r.Rotate(axis.normalized, sp * dt * (r.name.Contains("Laser") ? 1.5f : 1f), Space.World); }   // 레이저 블레이더는 1.5배
        }
    }
}
