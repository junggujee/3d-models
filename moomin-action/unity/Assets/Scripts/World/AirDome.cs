using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 바다 밑 공기방울 돔(용궁): 공 모양 안은 물이 없음 — 헤엄이 끝나 걷고 싸우고, 숨이 다시 참, 물속 안개가 꺼짐.
    /// 위에서 들어오면 둥실둥실 천천히 내려옴(PlayerController 가 떨어지는 속도를 줄임).
    /// 질문: AirDome.Inside(위치) — PlayerSwim · Ocean · PlayerController 가 씀. 막 그림은 Bubble 셰이더.
    /// </summary>
    public class AirDome : MonoBehaviour
    {
        public static readonly List<AirDome> All = new();
        public float radius = 36f;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>돔 안(가장자리에서 margin 만큼 안쪽).</summary>
        public static bool Inside(Vector3 p, float margin = 0f)
        {
            for (int i = 0; i < All.Count; i++) { var d = All[i]; if (d != null && (p - d.transform.position).sqrMagnitude < (d.radius - margin) * (d.radius - margin)) return true; }
            return false;
        }

        static Material bubbleMat;
        /// <summary>공기방울 막 재질(돔 · 버블 감옥 공용). 셰이더가 없으면 반투명 하늘색.</summary>
        public static Material BubbleMaterial()
        {
            if (bubbleMat != null) return bubbleMat;
            var sh = Shader.Find("MoominAction/Bubble");
            if (sh != null) bubbleMat = new Material(sh) { name = "Bubble" };
            else { bubbleMat = new Material(Shader.Find("Sprites/Default")) { name = "Bubble", color = new Color(0.7f, 0.9f, 1f, 0.18f) }; }
            return bubbleMat;
        }
    }

    /// <summary>
    /// 버블 감옥: 물방울에 갇혀 잠시 못 움직임(플레이어 = CombatManager.Stagger, 적 = ICombatant.Stagger) + 몸을 감싼 방울(일렁이다 퐁 터짐).
    /// </summary>
    public class BubbleTrap : MonoBehaviour
    {
        float until, born; Transform owner; Transform ball;

        public static void Apply(CharacterStats target, float seconds)
        {
            if (target == null || !target.IsAlive) return;
            if (target.TryGetComponent<CombatManager>(out var cm)) cm.Stagger(seconds);
            else if (target.TryGetComponent<ICombatant>(out var ic)) ic.Stagger(seconds);
            var old = target.GetComponentInChildren<BubbleTrap>(); if (old != null) { old.until = Time.time + seconds; return; }
            var go = new GameObject("BubbleTrap"); go.transform.SetParent(target.transform, false); var t = go.AddComponent<BubbleTrap>(); t.owner = target.transform; t.until = Time.time + seconds; t.born = Time.time;
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere); Destroy(b.GetComponent<Collider>()); b.transform.SetParent(go.transform, false); b.transform.localPosition = new Vector3(0, 0.7f, 0);
            var r = b.GetComponent<Renderer>(); r.sharedMaterial = AirDome.BubbleMaterial(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; t.ball = b.transform;
            if (target.team == Team.Player) Toast.Show("물방울 감옥에 갇혔다! (잠시 못 움직여요)");
            WaterFx.Bubble(target.transform.position + Vector3.up * 0.7f, 10);
        }

        void Update()
        {
            float a = Time.time - born; float s = 1.9f * Mathf.Min(1f, a / 0.18f) * (1f + 0.05f * Mathf.Sin(Time.time * 9f));
            if (ball != null) ball.localScale = new Vector3(s, s * 1.05f, s) / Mathf.Max(0.01f, transform.lossyScale.x);
            if (Time.time >= until || owner == null)
            {
                Vector3 p = ball != null ? ball.position : transform.position;
                SparkBurst.Spawn(p, Vector3.up, 24, 4f, new Color(0.75f, 0.95f, 1f)); WaterFx.Bubble(p, 8); Destroy(gameObject);
            }
        }
    }
}
