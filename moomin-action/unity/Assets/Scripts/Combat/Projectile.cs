using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 원거리 투사체(SkillDefinition.isProjectile). 코인이 판정되면 CombatResolver.ResolveAttack 이 Launch → 이미 정해진 AttackContext(위력·앞뒷면)를 싣고 날아감.
    ///  · 날아가다 상대 팀 캐릭터에 닿으면 쏜 쪽 CombatResolver.ApplyHit(회피 → 가드/패링 → 피해 → 상태이상) — 근접 공격과는 합하지 않음(일방 공격)
    ///  · 다른 팀의 투사체와 공중에서 만나면 <b>원거리끼리의 합</b>: 위력(+상성 보너스)이 큰 쪽이 그대로 날아가고 진 쪽은 부서짐(같으면 둘 다)
    ///  · 벽·땅에 닿거나 사거리(range)를 넘으면 부서짐. 쏜 쪽이 사라져도 계속 날아가지만 맞혀도 피해는 없음(판정할 주인이 없으므로)
    /// 모양은 코드로 만듦(ProjectileKind.Gear = 그린볼런의 톱니바퀴: 이 8개 + 가운데 구멍, 날면서 빙글빙글).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        static readonly List<Projectile> live = new(); static readonly Collider[] buf = new Collider[16];
        static Mesh gearMesh; static Material gearMat;
        CombatResolver owner; AttackContext attack; SkillDefinition skill; CharacterStats target; Team team;
        Vector3 vel; float traveled, maxDist; Transform spin;
        public AttackContext Attack => attack;

        public static Projectile Launch(CombatResolver owner, AttackContext attack, CharacterStats target, Vector3 from)
        {
            var skill = attack.skill; var go = new GameObject("Projectile_" + skill.skillId); go.transform.position = from;
            var p = go.AddComponent<Projectile>(); p.owner = owner; p.attack = attack; p.skill = skill; p.target = target;
            p.team = owner.TryGetComponent<CharacterStats>(out var st) ? st.team : Team.Enemy;
            Vector3 aim = target != null ? target.transform.position + Vector3.up * 0.6f : from + owner.transform.forward * 10f;
            Vector3 dir = aim - from; dir.y *= 0.6f; if (dir.sqrMagnitude < 1e-4f) dir = owner.transform.forward;
            p.vel = dir.normalized * skill.projectileSpeed; p.maxDist = skill.range + 2f;
            p.BuildVisual(attack.isHeads); live.Add(p);
            SparkBurst.Spawn(from, p.vel.normalized, attack.isHeads ? 14 : 8, 3f, attack.isHeads ? SparkBurst.Gold : new Color(0.5f, 0.95f, 0.4f));
            return p;
        }

        void OnDestroy() { live.Remove(this); }

        void BuildVisual(bool heads)
        {
            var v = new GameObject("Gear"); v.transform.SetParent(transform, false); v.transform.localRotation = Quaternion.Euler(0, 90, 0); spin = v.transform;   // 톱니 면이 옆을 봄 → 바퀴처럼 세로로 돌며 날아감
            v.AddComponent<MeshFilter>().sharedMesh = GearMesh();
            var mr = v.AddComponent<MeshRenderer>(); mr.sharedMaterial = GearMaterial(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            float sc = skill.projectileRadius / 0.15f * 0.85f; v.transform.localScale = Vector3.one * sc;
            if (heads) { var glow = new GameObject("Glow").AddComponent<Light>(); glow.transform.SetParent(transform, false); glow.type = LightType.Point; glow.range = 2.2f; glow.intensity = 1.6f; glow.color = new Color(1f, 0.85f, 0.4f); }
            var tr = gameObject.AddComponent<TrailRenderer>(); tr.time = 0.18f; tr.startWidth = skill.projectileRadius * 1.4f; tr.endWidth = 0f;
            tr.material = new Material(Shader.Find("Sprites/Default")); tr.startColor = heads ? new Color(1f, 0.86f, 0.4f, 0.7f) : new Color(0.45f, 0.95f, 0.35f, 0.55f); tr.endColor = new Color(1, 1, 1, 0);
        }

        void Update()
        {
            if (UIState.Paused) return;
            float dt = Time.deltaTime;
            if (target != null && target.IsAlive && skill.projectileHoming > 0f)   // 살짝 따라감(피할 수 있을 만큼만)
            {
                Vector3 want = (target.transform.position + Vector3.up * 0.6f - transform.position).normalized;
                vel = Vector3.RotateTowards(vel, want * vel.magnitude, skill.projectileHoming * Mathf.Deg2Rad * dt, 0f);
            }
            Vector3 step = vel * dt; float r = skill.projectileRadius;
            // 벽·땅(캐릭터가 아닌 것)에 막힘
            if (Physics.SphereCast(transform.position, r * 0.5f, step.normalized, out var wall, step.magnitude, ~0, QueryTriggerInteraction.Ignore) && wall.collider.GetComponentInParent<CharacterStats>() == null)
            { Break(wall.point, -step.normalized); return; }
            transform.position += step; traveled += step.magnitude;
            if (spin) spin.Rotate(Vector3.forward, 900f * dt, Space.Self);
            transform.rotation = Quaternion.LookRotation(vel.normalized, Vector3.up);
            if (ClashWithOthers()) return;
            // 맞힘: 상대 팀 캐릭터
            int n = Physics.OverlapSphereNonAlloc(transform.position, r, buf, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var cs = buf[i].GetComponentInParent<CharacterStats>();
                if (cs == null || cs.team == team || !cs.IsAlive || attack.hitTargets.Contains(cs)) continue;
                if (owner != null) owner.ApplyHit(attack, cs);
                Break(transform.position, -vel.normalized); return;
            }
            if (traveled > maxDist || attack.cancelled) Break(transform.position, vel.normalized);
        }

        /// <summary>원거리끼리의 합: 다른 팀 투사체와 닿으면 위력(+상성 보너스) 비교 — 진 쪽만 부서지고 이긴 쪽은 계속 날아감, 같으면 둘 다.</summary>
        bool ClashWithOthers()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var o = live[i]; if (o == null || o == this || o.team == team) continue;
                float rr = skill.projectileRadius + o.skill.projectileRadius; if ((o.transform.position - transform.position).sqrMagnitude > rr * rr) continue;
                int aff = SkillAttributes.Affinity(skill.attribute, o.skill.attribute);
                int mine = attack.power + (aff > 0 ? SkillAttributes.AdvantageClashBonus : 0), theirs = o.attack.power + (aff < 0 ? SkillAttributes.AdvantageClashBonus : 0);
                Vector3 mid = (transform.position + o.transform.position) * 0.5f;
                SparkBurst.Spawn(mid, Vector3.up, 36, 6f, SparkBurst.Gold); SparkBurst.Spawn(mid, -vel.normalized, 18, 5f, SparkBurst.Yellow);
                attack.clashed = o.attack.clashed = true;
                if (mine <= theirs) Break(mid, -vel.normalized);
                if (theirs <= mine) o.Break(mid, -o.vel.normalized);
                return mine <= theirs;
            }
            return false;
        }

        void Break(Vector3 at, Vector3 dir)
        {
            SparkBurst.Spawn(at, dir + Vector3.up * 0.5f, attack.isHeads ? 22 : 12, 4f, attack.isHeads ? SparkBurst.Gold : new Color(0.5f, 0.95f, 0.4f));
            Destroy(gameObject);
        }

        // ── 톱니바퀴 메시(톱니는 XY 평면, 회전축 = 로컬 Z) ──
        static Mesh GearMesh()
        {
            if (gearMesh != null) return gearMesh;
            const int teeth = 8; const float rOut = 0.15f, rIn = 0.11f, hole = 0.04f, th = 0.045f;
            var prof = new List<Vector2>();
            for (int k = 0; k < teeth; k++)
            {
                float a0 = 2 * Mathf.PI * k / teeth, w = 2 * Mathf.PI / teeth * 2.2f;
                foreach (var (da, r) in new[] { (-0.16f, rIn), (-0.13f, rOut), (0.13f, rOut), (0.16f, rIn) }) { float a = a0 + da * w; prof.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r)); }
            }
            int n = prof.Count; var verts = new List<Vector3>(); var tris = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { int i = verts.Count; verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d); tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 }); }
            for (int k = 0; k < n; k++)
            {
                int m = (k + 1) % n; Vector2 o0 = prof[k], o1 = prof[m];
                Vector2 h0 = new Vector2(Mathf.Cos(2 * Mathf.PI * k / n), Mathf.Sin(2 * Mathf.PI * k / n)) * hole, h1 = new Vector2(Mathf.Cos(2 * Mathf.PI * m / n), Mathf.Sin(2 * Mathf.PI * m / n)) * hole;
                Vector3 F(Vector2 p, float z) => new Vector3(p.x, p.y, z);
                Quad(F(o0, -th / 2), F(o1, -th / 2), F(o1, th / 2), F(o0, th / 2));      // 바깥 둘레
                Quad(F(h1, -th / 2), F(h0, -th / 2), F(h0, th / 2), F(h1, th / 2));      // 구멍 둘레
                Quad(F(o0, th / 2), F(o1, th / 2), F(h1, th / 2), F(h0, th / 2));        // 앞면
                Quad(F(o1, -th / 2), F(o0, -th / 2), F(h0, -th / 2), F(h1, -th / 2));    // 뒷면
            }
            gearMesh = new Mesh { name = "GearProjectile" }; gearMesh.SetVertices(verts); gearMesh.SetTriangles(tris, 0); gearMesh.RecalculateNormals(); gearMesh.RecalculateBounds();
            return gearMesh;
        }
        static Material GearMaterial()
        {
            if (gearMat != null) return gearMat;
            var sh = Shader.Find("Standard"); if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            gearMat = new Material(sh) { color = new Color(0.08f, 0.55f, 0.06f) };
            if (gearMat.HasProperty("_Glossiness")) gearMat.SetFloat("_Glossiness", 0.75f);
            return gearMat;
        }
    }
}
