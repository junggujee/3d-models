using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MoominAction
{
    /// <summary>
    /// 자원·야생 펫 생성(GameManager 가 붙임). 자리 = Resources/World/SpawnPoints.json(blender/build_spawn_points.py, Blender 좌표 → MapPlaces 로 변환 + 걸을 수 있는 곳으로 맞춤).
    ///  · 땅콩빵: 산 정상(peaks)에 peakCount 개 + 자연(nature)에서 낮은 확률(natureChance) — 부서지면 respawnSeconds 뒤 다른 자리에
    ///  · 야생 하프물범: 자연(nature) 자리에 wildSeals 마리(길들여 데려가면 시간이 지나 새로 나타남)
    ///  · 불러온 저장에 길들인 물범이 있으면 곁에 다시 데려옴
    ///  · testNearStart: 시작 위치 가까이에 땅콩빵 1개·물범 1마리(처음에 바로 해 볼 수 있게, 끄면 지도 자리에만)
    /// </summary>
    public class NatureSpawner : MonoBehaviour
    {
        public GameObject peanutPrefab, sealPrefab;
        public int peakCount = 5, wildSeals = 3; [Range(0, 1)] public float natureChance = 0.15f; public float respawnSeconds = 90f;
        public bool testNearStart = true;
        readonly List<Vector3> peaks = new(), nature = new();
        readonly List<float> pendingPeanut = new(), pendingSeal = new();
        int wild;

        void Start()
        {
            LoadPoints();
            var player = GameObject.FindGameObjectWithTag("Player");
            Vector3 start = player != null ? player.transform.position : Vector3.zero;
            if (peanutPrefab != null)
            {
                Shuffle(peaks); for (int i = 0; i < Mathf.Min(peakCount, peaks.Count); i++) SpawnPeanut(peaks[i]);
                foreach (var n in nature) if (UnityEngine.Random.value < natureChance) SpawnPeanut(n);
                if (testNearStart) SpawnPeanut(Near(start, 7f));
            }
            if (sealPrefab != null)
            {
                wild = Mathf.Min(wildSeals, nature.Count) + (testNearStart ? 1 : 0);   // 유지할 야생 물범 수
                Shuffle(nature); for (int i = 0; i < Mathf.Min(wildSeals, nature.Count); i++) SpawnSeal(nature[i]);
                if (testNearStart) SpawnSeal(Near(start, 9f));
                var pet = SaveSystem.LoadedPet; SaveSystem.LoadedPet = null;
                if (pet != null && pet.tamed && player != null) { var s = SpawnSeal(Near(start, 2f)); if (s != null) s.Restore(pet.name, pet.level, pet.exp, pet.affection, pet.health); }
            }
        }

        void Update()
        {
            if (UIState.Paused) return;
            for (int i = pendingPeanut.Count - 1; i >= 0; i--) if (Time.time >= pendingPeanut[i]) { pendingPeanut.RemoveAt(i); if (peaks.Count > 0) SpawnPeanut(peaks[UnityEngine.Random.Range(0, peaks.Count)]); else if (nature.Count > 0) SpawnPeanut(nature[UnityEngine.Random.Range(0, nature.Count)]); }
            for (int i = pendingSeal.Count - 1; i >= 0; i--) if (Time.time >= pendingSeal[i]) { pendingSeal.RemoveAt(i); if (nature.Count > 0) SpawnSeal(nature[UnityEngine.Random.Range(0, nature.Count)]); }
            // 야생이 길들여져 줄었으면 나중에 새로
            int w = 0; foreach (var s in PetSeal.All) if (s != null && !s.tamed) w++;
            if (w + pendingSeal.Count < wild) { pendingSeal.Add(Time.time + respawnSeconds); }
        }

        void LoadPoints()
        {
            var ta = Resources.Load<TextAsset>("World/SpawnPoints"); if (ta == null) { Debug.LogWarning("[자연] Resources/World/SpawnPoints.json 없음 — blender/build_spawn_points.py"); return; }
            // JsonUtility 는 중첩 배열을 못 읽어서 직접 파싱: {"peaks": [[x,y,z],...], "nature": [[x,y,z],...]}
            foreach (var (key, list) in new[] { ("\"peaks\"", peaks), ("\"nature\"", nature) })
            {
                string t = ta.text; int a = t.IndexOf(key, StringComparison.Ordinal); if (a < 0) continue;
                int s = t.IndexOf('[', a), depth = 0, e = s;
                for (; e < t.Length; e++) { if (t[e] == '[') depth++; else if (t[e] == ']' && --depth == 0) break; }
                foreach (var chunk in t.Substring(s + 1, e - s - 1).Split(']'))
                {
                    var nums = chunk.Replace("[", "").Replace(",", " ").Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (nums.Length < 2) continue;
                    float bx = float.Parse(nums[0], System.Globalization.CultureInfo.InvariantCulture), by = float.Parse(nums[1], System.Globalization.CultureInfo.InvariantCulture);
                    list.Add(MapPlaces.BlenderToWorld(bx, by));
                }
            }
        }

        /// <summary>걸을 수 있는 가장 가까운 곳(산꼭대기가 너무 가파르면 가장 높은 걸을 수 있는 곳 근처).</summary>
        static bool Walkable(Vector3 p, out Vector3 at)
        {
            at = p; foreach (float r in new[] { 3f, 8f, 20f }) if (NavMesh.SamplePosition(p, out var h, r, NavMesh.AllAreas)) { at = h.position; return true; }
            return false;
        }
        static Vector3 Near(Vector3 c, float r) { float a = UnityEngine.Random.value * Mathf.PI * 2; return c + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r; }

        void SpawnPeanut(Vector3 p)
        {
            if (!Walkable(p, out var at)) return;
            var go = Instantiate(peanutPrefab, at, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0));
            if (go.TryGetComponent<ResourceNode>(out var n)) n.Broken += _ => pendingPeanut.Add(Time.time + respawnSeconds);
        }
        PetSeal SpawnSeal(Vector3 p)
        {
            if (!Walkable(p, out var at)) return null;
            var go = Instantiate(sealPrefab, at, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0));
            return go.TryGetComponent<PetSeal>(out var s) ? s : null;
        }
        static void Shuffle<T>(List<T> l) { for (int i = l.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); (l[i], l[j]) = (l[j], l[i]); } }
    }
}
