using UnityEngine;

namespace MoominAction
{
    /// <summary>바닥에 떨어진 아이템(빛나는 구슬). 가까이 가면 끌려와서 자동으로 줍는다. 금화는 id = "gold".</summary>
    public class ItemPickup : MonoBehaviour
    {
        public string id; public int amount = 1; public float magnetRange = 3.5f, collectRange = 1.1f, delay = 0.4f;
        Transform player; Inventory inv; float born, baseY; Vector3 vel; Light glow;

        public static ItemPickup Spawn(string id, int amount, Vector3 pos, float delay = 0.4f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.name = "Pickup_" + id; go.transform.position = pos; go.transform.localScale = Vector3.one * (id == "gold" ? 0.22f : 0.3f);
            Object.Destroy(go.GetComponent<Collider>());
            var p = go.AddComponent<ItemPickup>(); p.id = id; p.amount = amount; p.delay = delay;
            var d = ItemDatabase.Get(id); Color c = id == "gold" ? new Color(1f, .82f, .25f) : (d != null ? Color.Lerp(d.color, Color.white, .25f) : Color.white);
            var r = go.GetComponent<Renderer>(); r.sharedMaterial = MatFor(c); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(go.transform, false); l.type = LightType.Point; l.color = c; l.range = 3f; l.intensity = 1.2f;
            return p;
        }

        // 색마다 재질 하나를 같이 씀(예전엔 구슬마다 새 재질 → 지워지지 않고 쌓였음)
        static readonly System.Collections.Generic.Dictionary<Color, Material> mats = new System.Collections.Generic.Dictionary<Color, Material>();
        static Material MatFor(Color c)
        {
            if (mats.TryGetValue(c, out var m) && m != null) return m;
            var sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
            m = new Material(sh) { color = c }; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2.5f); mats[c] = m; return m;
        }

        /// <summary>장면에 있는 모든 줍기 아이템(찾기용, FindObjectsOfType 대신)</summary>
        public static readonly System.Collections.Generic.List<ItemPickup> All = new System.Collections.Generic.List<ItemPickup>();
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start() { var pl = GameObject.FindGameObjectWithTag("Player"); if (pl) { player = pl.transform; inv = pl.GetComponent<Inventory>(); } born = Time.time; baseY = transform.position.y; vel = new Vector3(Random.Range(-1f, 1f), 3f, Random.Range(-1f, 1f)); }

        void Update()
        {
            if (Time.timeScale == 0) return;
            if (vel.sqrMagnitude > 0.01f) { transform.position += vel * Time.deltaTime; vel.y -= 14f * Time.deltaTime; vel.x *= 0.96f; vel.z *= 0.96f; if (transform.position.y <= baseY && vel.y < 0) { vel = Vector3.zero; transform.position = new Vector3(transform.position.x, baseY, transform.position.z); } return; }
            transform.position = new Vector3(transform.position.x, baseY + 0.08f * Mathf.Sin(Time.time * 3f + baseY), transform.position.z); transform.Rotate(0, 90f * Time.deltaTime, 0);
            if (player == null || inv == null || Time.time - born < delay) return;
            Vector3 to = player.position + Vector3.up * 0.7f - transform.position; float d = to.magnitude;
            if (d < magnetRange) transform.position += to.normalized * Mathf.Lerp(2f, 9f, 1f - d / magnetRange) * Time.deltaTime;
            if (d < collectRange) { if (id == "gold") { inv.AddGold(amount); Destroy(gameObject); return; }
                int left = inv.Add(id, amount);
                if (left <= 0) { Destroy(gameObject); return; }
                amount = left; born = Time.time; delay = 2f;   // 가방이 가득 참: 남은 만큼 그대로 두고 2초 뒤 다시(예전엔 매 프레임 "가득 찼다" + 일부만 들어가면 나머지 사라짐)
            }
        }
    }
}
