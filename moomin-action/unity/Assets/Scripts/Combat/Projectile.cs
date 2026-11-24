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
        static Mesh gearMesh, ringMesh; static Material gearMat, bubbleMat, bubbleHiMat, trailMat, discMat, discCoreMat;   // 재질·메시는 한 번만 만들어 함께 씀(예전엔 투사체마다 새 재질)
        static Material Trail() { if (trailMat == null) trailMat = new Material(Shader.Find("Sprites/Default")); return trailMat; }
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

        /// <summary>납작한 고리 메시(안 반지름 r0 · 바깥 r1, 반지름 1 기준) — 같은 값이면 하나를 함께 씀.</summary>
        static readonly Dictionary<(float, float), Mesh> rings = new();
        static Mesh RingMesh(float r0, float r1)
        {
            if (rings.TryGetValue((r0, r1), out var m) && m != null) return m;
            const int N = 48; var v = new Vector3[N * 2]; var tri = new int[N * 12];
            for (int i = 0; i < N; i++) { float a = i * Mathf.PI * 2f / N; var c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0); v[i] = c * r0; v[N + i] = c * r1; }
            for (int i = 0, t = 0; i < N; i++)
            {
                int j = (i + 1) % N; int[] q = { i, N + i, N + j, i, N + j, j };
                foreach (var x in q) tri[t++] = x; foreach (var x in new[] { q[0], q[2], q[1], q[3], q[5], q[4] }) tri[t++] = x;   // 양면
            }
            m = new Mesh { name = "Ring", vertices = v, triangles = tri }; m.RecalculateBounds(); rings[(r0, r1)] = m; return m;
        }

        void BuildVisual(bool heads)
        {
            if (skill.projectileKind == ProjectileKind.Bubble)
            {   // 물방울: 반투명 하늘색 공 + 흰 반짝임
                var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(b.GetComponent<Collider>()); b.transform.SetParent(transform, false); b.transform.localScale = Vector3.one * skill.projectileRadius * 1.6f; spin = null;
                var sh = Shader.Find("Standard");
                if (bubbleMat == null)
                {
                    var m = new Material(sh) { color = new Color(0.55f, 0.82f, 1f, 0.55f) };
                    m.SetFloat("_Mode", 3); m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetInt("_ZWrite", 0);
                    m.EnableKeyword("_ALPHABLEND_ON"); m.renderQueue = 3000; m.SetFloat("_Glossiness", 0.95f); bubbleMat = m;
                }
                b.GetComponent<Renderer>().sharedMaterial = bubbleMat;
                var hi = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(hi.GetComponent<Collider>()); hi.transform.SetParent(b.transform, false); hi.transform.localPosition = new Vector3(-0.2f, 0.25f, -0.25f); hi.transform.localScale = Vector3.one * 0.25f;
                if (bubbleHiMat == null) { bubbleHiMat = new Material(sh) { color = Color.white }; bubbleHiMat.EnableKeyword("_EMISSION"); bubbleHiMat.SetColor("_EmissionColor", Color.white * 0.8f); }
                hi.GetComponent<Renderer>().sharedMaterial = bubbleHiMat;
                var trB = gameObject.AddComponent<TrailRenderer>(); trB.time = 0.15f; trB.startWidth = skill.projectileRadius; trB.endWidth = 0f; trB.material = Trail(); trB.startColor = new Color(0.7f, 0.9f, 1f, 0.5f); trB.endColor = new Color(1, 1, 1, 0);
                return;
            }
            if (skill.projectileKind == ProjectileKind.LaserDisc)
            {   // 광륜: 푸른 빛 고리 두 겹(바깥 시안 · 안 흰 심) + 시안 잔상 + 빛
                var d = new GameObject("LaserDisc"); d.transform.SetParent(transform, false); d.transform.localRotation = Quaternion.Euler(90, 0, 0); spin = d.transform;
                if (discMat == null) { discMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.35f, 0.82f, 1f, 0.85f) }; discCoreMat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.9f, 0.98f, 1f, 1f) }; }
                float R = skill.projectileRadius;
                foreach (var (r0, r1, m) in new[] { (R * 0.72f, R, discMat), (R * 0.8f, R * 0.9f, discCoreMat) })
                {
                    var o = new GameObject("Ring"); o.transform.SetParent(d.transform, false); o.AddComponent<MeshFilter>().sharedMesh = RingMesh(r0 / R, r1 / R); var rr = o.AddComponent<MeshRenderer>(); rr.sharedMaterial = m; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    o.transform.localScale = Vector3.one * R;
                }
                var lg = new GameObject("Glow").AddComponent<Light>(); lg.transform.SetParent(transform, false); lg.type = LightType.Point; lg.range = 3f; lg.intensity = heads ? 2.2f : 1.4f; lg.color = new Color(0.4f, 0.8f, 1f);
                var trD = gameObject.AddComponent<TrailRenderer>(); trD.time = 0.22f; trD.startWidth = R * 1.6f; trD.endWidth = 0f; trD.material = Trail(); trD.startColor = new Color(0.45f, 0.88f, 1f, 0.6f); trD.endColor = new Color(0.15f, 0.4f, 1f, 0f);
                return;
            }
            var v = new GameObject("Gear"); v.transform.SetParent(transform, false); v.transform.localRotation = Quaternion.Euler(0, 90, 0); spin = v.transform;   // 톱니 면이 옆을 봄 → 바퀴처럼 세로로 돌며 날아감
            v.AddComponent<MeshFilter>().sharedMesh = GearMesh();
            var mr = v.AddComponent<MeshRenderer>(); mr.sharedMaterial = GearMaterial(); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            float sc = skill.projectileRadius / 0.15f * 0.85f; v.transform.localScale = Vector3.one * sc;
            if (heads) { var glow = new GameObject("Glow").AddComponent<Light>(); glow.transform.SetParent(transform, false); glow.type = LightType.Point; glow.range = 2.2f; glow.intensity = 1.6f; glow.color = new Color(1f, 0.85f, 0.4f); }
            var tr = gameObject.AddComponent<TrailRenderer>(); tr.time = 0.18f; tr.startWidth = skill.projectileRadius * 1.4f; tr.endWidth = 0f;
            tr.material = Trail(); tr.startColor = heads ? new Color(1f, 0.86f, 0.4f, 0.7f) : new Color(0.45f, 0.95f, 0.35f, 0.55f); tr.endColor = new Color(1, 1, 1, 0);
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
