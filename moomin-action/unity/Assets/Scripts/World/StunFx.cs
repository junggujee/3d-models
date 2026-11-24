using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 경직(경직 게이지가 가득 차 깨짐) 효과 — 적이든 민미든 몸 위에 주황 폭발 + 둥근 굵은 "기절" 글자(사용자 참고 이미지, '흐트러짐' 대신 '기절').
    /// 그림: tools/sheet_art.py → Resources/UI/Fx/StunBurst.png · StunText.png. CharacterStats.AnyStaggerBroken 을 GameManager 가 Hook.
    ///  · 톡 튀어나옴(0.25초) → 폭발 빛이 천천히 돌며 깜빡, 글자는 살짝 흔들 → 경직이 끝나면 0.25초 동안 사라짐
    ///  · 시작할 때 주황 불꽃 튐, 0.5초마다 작은 불꽃
    ///  · 카메라를 향함 + 몸보다 카메라 쪽으로 조금 당겨 가려지지 않게
    /// </summary>
    public class StunFx : MonoBehaviour
    {
        static Sprite burst, text; static bool hooked;
        CharacterStats stats; Transform fx, burstT, textT; SpriteRenderer burstR, textR; float t, until, nextSpark, endAt = -1f, height = 1.2f;

        public static void Hook() { if (hooked) return; hooked = true; CharacterStats.AnyStaggerBroken += OnBroken; }
        public static void Unhook() { if (!hooked) return; hooked = false; CharacterStats.AnyStaggerBroken -= OnBroken; }
        static void OnBroken(CharacterStats s)
        {
            if (s == null || !s.IsAlive) return;
            if (!s.TryGetComponent<StunFx>(out var fx)) fx = s.gameObject.AddComponent<StunFx>();
            fx.Play(s.staggerDuration);
        }

        static Sprite Load(string n)
        {
            var tex = Resources.Load<Texture2D>("UI/Fx/" + n); if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 256f);
        }

        void Build()
        {
            stats = GetComponent<CharacterStats>();
            if (burst == null) burst = Load("StunBurst"); if (text == null) text = Load("StunText");
            fx = new GameObject("StunFx").transform; fx.SetParent(transform, false);
            burstT = new GameObject("Burst").transform; burstT.SetParent(fx, false); burstR = burstT.gameObject.AddComponent<SpriteRenderer>(); burstR.sprite = burst; burstR.sortingOrder = 50;
            textT = new GameObject("Text").transform; textT.SetParent(fx, false); textR = textT.gameObject.AddComponent<SpriteRenderer>(); textR.sprite = text; textR.sortingOrder = 51;
            // 몸 높이(렌더러 경계) → 머리 조금 아래 ~ 위
            var b = new Bounds(transform.position + Vector3.up, Vector3.zero); bool any = false;
            foreach (var r in GetComponentsInChildren<Renderer>()) { if (r is SpriteRenderer || r is ParticleSystemRenderer) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            height = any ? Mathf.Max(0.6f, b.max.y - transform.position.y) * 0.82f : 1.2f;
            fx.gameObject.SetActive(false);
        }

        public void Play(float duration)
        {
            if (fx == null) Build();
            t = 0; endAt = -1f; until = Time.time + Mathf.Max(0.6f, duration); nextSpark = Time.time + 0.5f; fx.gameObject.SetActive(true);
            SparkBurst.Spawn(transform.position + Vector3.up * height, Vector3.up, 26, 4.5f, new Color(1f, .6f, .15f));
        }

        void LateUpdate()
        {
            if (fx == null || !fx.gameObject.activeSelf) return;
            float dt = Time.deltaTime; t += dt;
            bool ending = stats == null || !stats.IsAlive || Time.time >= until || (!stats.IsStaggerBroken && t > 0.6f);
            if (ending && endAt < 0) endAt = Time.time;
            float pop = t < 0.25f ? Mathf.Sin(t / 0.25f * Mathf.PI * 0.75f) / Mathf.Sin(Mathf.PI * 0.75f) : 1f;   // 0 → 1.4 → 1 톡
            float fade = endAt < 0 ? 1f : 1f - (Time.time - endAt) / 0.25f;
            if (fade <= 0f) { fx.gameObject.SetActive(false); return; }
            var cam = Camera.main;
            Vector3 basePos = transform.position + Vector3.up * height;
            if (cam != null) { var toCam = (cam.transform.position - basePos).normalized; fx.position = basePos + toCam * 0.6f; fx.rotation = Quaternion.LookRotation(fx.position - cam.transform.position, cam.transform.up); }
            else fx.position = basePos;
            float s = 0.55f * pop;
            burstT.localScale = Vector3.one * s * (1.1f + 0.08f * Mathf.Sin(t * 9f)); burstT.localRotation = Quaternion.Euler(0, 0, t * 25f);
            textT.localScale = Vector3.one * s * 0.62f; textT.localPosition = new Vector3(0, 0.03f * Mathf.Sin(t * 6f), -0.01f); textT.localRotation = Quaternion.Euler(0, 0, 4f * Mathf.Sin(t * 11f));
            burstR.color = new Color(1, 1, 1, fade * (0.75f + 0.25f * Mathf.Sin(t * 14f))); textR.color = new Color(1, 1, 1, fade);
            if (!ending && Time.time >= nextSpark) { nextSpark = Time.time + 0.5f; SparkBurst.Spawn(fx.position, Random.onUnitSphere + Vector3.up, 6, 2.5f, new Color(1f, .7f, .25f)); }
        }
    }
}
