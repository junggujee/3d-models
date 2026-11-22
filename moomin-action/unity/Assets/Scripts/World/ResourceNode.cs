using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 자원 오브젝트 — 땅콩빵(blender/build_peanut.py). 공격으로 부숴 자원(땅콩빵 조각)을 얻음.
    ///  · 적 레이어 + CharacterStats(team = Enemy) 라 플레이어 공격(단일·광역·투사체)이 그대로 맞음. 조준(LockOn)·웨이브 수에는 안 들어감(EnemyCombatController 없음)
    ///  · 맞을 때마다 말랑하게 눌렸다 튕김 + 하얀 솜털 튐, 가끔(25%) 조각 1개
    ///  · 부서지면 두 쪽(LobeL·LobeR)이 갈라져 튀어 나가 굴렀다가 사라지고, 조각 3~5개 + 가끔 금화
    ///  · 부서지면 Broken 이벤트 → NatureSpawner 가 다른 자리에 다시 생성
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class ResourceNode : MonoBehaviour
    {
        public static readonly List<ResourceNode> All = new();
        public string dropId = "peanut_piece"; public int dropMin = 3, dropMax = 5; [Range(0, 1)] public float dropOnHitChance = 0.25f;
        public event Action<ResourceNode> Broken;
        CharacterStats stats; Transform visual, lobeL, lobeR; Vector3 baseScale; float squashT = -1; bool broken;

        void Awake()
        {
            stats = GetComponent<CharacterStats>(); visual = transform.childCount > 0 ? transform.GetChild(0) : transform; baseScale = visual.localScale;
            lobeL = Find(transform, "LobeL"); lobeR = Find(transform, "LobeR");
            stats.Damaged += OnHit; stats.Died += OnBroken;
        }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        static Transform Find(Transform t, string n) { if (t.name == n) return t; foreach (Transform c in t) { var r = Find(c, n); if (r != null) return r; } return null; }

        void OnHit(float amount, Vector3 point, Vector3 knock)
        {
            if (broken) return; squashT = 0f;
            SparkBurst.Spawn(point, Vector3.up, 16, 3.5f, new Color(1f, 0.97f, 0.88f));   // 솜털
            if (UnityEngine.Random.value < dropOnHitChance) ItemPickup.Spawn(dropId, 1, point);
        }

        void OnBroken()
        {
            if (broken) return; broken = true;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            Vector3 p = transform.position + Vector3.up * 0.3f;
            ItemPickup.Spawn(dropId, UnityEngine.Random.Range(dropMin, dropMax + 1), p);
            if (UnityEngine.Random.value < 0.35f) ItemPickup.Spawn("gold", UnityEngine.Random.Range(3, 9), p);
            if (UnityEngine.Random.value < 0.06f) ItemPickup.Spawn(HomeGear.Random(), 1, p);   // 베개 속에서 집 장비가 나올 때도
            SparkBurst.Spawn(p, Vector3.up, 40, 5f, new Color(1f, 0.95f, 0.8f)); SparkBurst.Spawn(p, Vector3.up, 20, 3f, new Color(0.95f, 0.85f, 0.6f));
            StartCoroutine(Split()); Broken?.Invoke(this);
        }

        /// <summary>두 쪽이 갈라져 튀어 나가 굴렀다가(1.4초) 작아지며 사라짐.</summary>
        System.Collections.IEnumerator Split()
        {
            var parts = new List<(Transform t, Vector3 v, Vector3 w)>();
            foreach (var (t, s) in new[] { (lobeL, -1f), (lobeR, 1f) })
                if (t != null) parts.Add((t, transform.TransformDirection(new Vector3(s * 2.2f, 0, UnityEngine.Random.Range(-0.6f, 0.6f))) + Vector3.up * 3.2f, new Vector3(UnityEngine.Random.Range(-200f, 200f), UnityEngine.Random.Range(-90f, 90f), s * 260f)));
            float t0 = Time.time, groundY = transform.position.y;
            while (Time.time - t0 < 1.4f)
            {
                float dt = Time.deltaTime;
                for (int i = 0; i < parts.Count; i++)
                {
                    var (tr, v, w) = parts[i]; v.y -= 14f * dt; tr.position += v * dt; tr.Rotate(w * dt, Space.World);
                    if (tr.position.y < groundY + 0.12f && v.y < 0) { tr.position = new Vector3(tr.position.x, groundY + 0.12f, tr.position.z); v = new Vector3(v.x * 0.5f, -v.y * 0.35f, v.z * 0.5f); w *= 0.5f; }
                    parts[i] = (tr, v, w);
                }
                yield return null;
            }
            for (float k = 0; k < 0.4f; k += Time.deltaTime) { foreach (var p in parts) p.t.localScale = Vector3.one * Mathf.Lerp(1f, 0.01f, k / 0.4f); yield return null; }
            Destroy(gameObject);
        }

        void Update()
        {
            if (squashT < 0 || broken) return;
            squashT += Time.deltaTime; float u = squashT / 0.45f;
            if (u >= 1f) { visual.localScale = baseScale; squashT = -1; return; }
            float k = Mathf.Sin(u * Mathf.PI * 3f) * (1f - u) * 0.22f;   // 말랑: 눌렸다 튕기며 잦아듦
            visual.localScale = new Vector3(baseScale.x * (1 + k * 0.6f), baseScale.y * (1 - k), baseScale.z * (1 + k * 0.6f));
        }
    }
}
