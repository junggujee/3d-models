using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 레벨업 연출(월드): 발밑에서 솟는 금빛 기둥(카메라를 향하는 세로 빛) + 바닥에 퍼지는 고리 두 겹 + 위로 오르는 금빛 불꽃 + 금빛 점광원.
    /// 1.8초, 캐릭터를 따라감. Experience.Gain 이 부름. UI 쪽은 UI/LevelUpPopup.
    /// </summary>
    public class LevelUpFx : MonoBehaviour
    {
        static Sprite pillar; static Material lineMat;
        Transform follow, pillarT; SpriteRenderer pillarR; LineRenderer[] rings = new LineRenderer[2]; Light glow; float t; const float Dur = 1.8f;
        static readonly Color Gold = new Color(1f, 0.84f, 0.42f);

        public static void Play(Transform who, int level)
        {
            if (who == null) return;
            var go = new GameObject("LevelUpFx"); var fx = go.AddComponent<LevelUpFx>(); fx.follow = who; go.transform.position = who.position; fx.Build();
            SparkBurst.Spawn(who.position + Vector3.up * 0.4f, Vector3.up, 40, 6f, Gold);
        }

        static Sprite Pillar()
        {
            if (pillar != null) return pillar;
            int w = 64, h = 256; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; var c = new Color[w * h];
            for (int j = 0; j < h; j++) for (int i = 0; i < w; i++)
            {
                float x = Mathf.Abs((i + .5f) / w * 2f - 1f), y = (j + .5f) / h;
                float core = Mathf.Exp(-x * x * 18f), side = Mathf.Exp(-x * x * 3.5f) * 0.45f, fade = Mathf.Pow(1f - y, 1.3f) * Mathf.Clamp01(y * 12f);
                float a = Mathf.Clamp01((core + side) * fade); c[j * w + i] = new Color(1f, 0.86f + 0.14f * core, 0.5f + 0.5f * core, a);
            }
            tex.SetPixels(c); tex.Apply(); pillar = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, 0f), 64f); return pillar;
        }

        void Build()
        {
            pillarT = new GameObject("Pillar").transform; pillarT.SetParent(transform, false); pillarR = pillarT.gameObject.AddComponent<SpriteRenderer>(); pillarR.sprite = Pillar(); pillarR.sortingOrder = 40;
            if (lineMat == null) lineMat = new Material(Shader.Find("Sprites/Default"));
            for (int k = 0; k < 2; k++)
            {
                var lr = new GameObject("Ring" + k).AddComponent<LineRenderer>(); lr.transform.SetParent(transform, false); lr.loop = true; lr.positionCount = 48; lr.useWorldSpace = false;
                lr.material = lineMat; lr.widthMultiplier = 0.08f; lr.numCapVertices = 2; rings[k] = lr;
            }
            glow = new GameObject("Glow").AddComponent<Light>(); glow.transform.SetParent(transform, false); glow.transform.localPosition = Vector3.up * 1.2f; glow.type = LightType.Point; glow.color = Gold; glow.range = 6f;
        }

        void Update()
        {
            t += Time.deltaTime; float u = t / Dur; if (u >= 1f) { Destroy(gameObject); return; }
            if (follow != null) transform.position = follow.position;
            var cam = Camera.main;
            if (cam != null) { var to = cam.transform.position - transform.position; to.y = 0; if (to.sqrMagnitude > 0.001f) pillarT.rotation = Quaternion.LookRotation(-to.normalized); }
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u * 4f)), fade = 1f - Mathf.SmoothStep(0.55f, 1f, u);
            pillarT.localScale = new Vector3(1.6f * (1f - 0.4f * u), 4.2f * rise, 1f); pillarR.color = new Color(1, 1, 1, fade * 0.9f);
            for (int k = 0; k < 2; k++)
            {
                float v = Mathf.Clamp01((u - k * 0.18f) / 0.7f); float r = Mathf.Lerp(0.3f, 3.2f, 1f - (1f - v) * (1f - v)); float a = v > 0f ? (1f - v) : 0f;
                for (int i = 0; i < 48; i++) { float ang = i / 48f * Mathf.PI * 2f; rings[k].SetPosition(i, new Vector3(Mathf.Cos(ang) * r, 0.05f, Mathf.Sin(ang) * r)); }
                rings[k].startColor = rings[k].endColor = new Color(Gold.r, Gold.g, Gold.b, a); rings[k].widthMultiplier = 0.12f * (1f - v) + 0.02f;
            }
            glow.intensity = 3.5f * fade * (0.8f + 0.2f * Mathf.Sin(t * 20f));
            if (Mathf.FloorToInt((t - Time.deltaTime) / 0.3f) != Mathf.FloorToInt(t / 0.3f) && u < 0.7f) SparkBurst.Spawn(transform.position + Vector3.up * Random.Range(0.3f, 1.6f) + Random.insideUnitSphere * 0.4f, Vector3.up, 10, 3.5f, Gold);
        }
    }
}
