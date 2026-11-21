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
            var r = go.GetComponent<Renderer>(); var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); m.color = c; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 2.5f); r.material = m;
            var l = new GameObject("Glow").AddComponent<Light>(); l.transform.SetParent(go.transform, false); l.type = LightType.Point; l.color = c; l.range = 3f; l.intensity = 1.2f;
            return p;
        }

        void Start() { var pl = GameObject.FindGameObjectWithTag("Player"); if (pl) { player = pl.transform; inv = pl.GetComponent<Inventory>(); } born = Time.time; baseY = transform.position.y; vel = new Vector3(Random.Range(-1f, 1f), 3f, Random.Range(-1f, 1f)); }

        void Update()
        {
            if (Time.timeScale == 0) return;
            if (vel.sqrMagnitude > 0.01f) { transform.position += vel * Time.deltaTime; vel.y -= 14f * Time.deltaTime; vel.x *= 0.96f; vel.z *= 0.96f; if (transform.position.y <= baseY && vel.y < 0) { vel = Vector3.zero; transform.position = new Vector3(transform.position.x, baseY, transform.position.z); } return; }
            transform.position = new Vector3(transform.position.x, baseY + 0.08f * Mathf.Sin(Time.time * 3f + baseY), transform.position.z); transform.Rotate(0, 90f * Time.deltaTime, 0);
            if (player == null || inv == null || Time.time - born < delay) return;
            Vector3 to = player.position + Vector3.up * 0.7f - transform.position; float d = to.magnitude;
            if (d < magnetRange) transform.position += to.normalized * Mathf.Lerp(2f, 9f, 1f - d / magnetRange) * Time.deltaTime;
            if (d < collectRange) { if (id == "gold") inv.AddGold(amount); else if (inv.Add(id, amount) >= amount) return; Destroy(gameObject); }
        }
    }
}
