using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 월드맵(M): 게임 맵을 비스듬히 내려다본 입체 일러스트 지도(tools/world_map_art.py → Resources/UI/WorldMap).
    /// 주요 장소 5곳(성 · 마을 광장 · 산지 · 동쪽 마을 · 서쪽 마을)은 지도 위에서 빛나고, 금테 핀과 이름표가 붙음. 내 위치·방향 화살표.
    /// <b>펼치기</b>: 세 폭으로 접힌 종이가 아래에서 떠오름 → 왼쪽 폭이 펼쳐짐 → 오른쪽 폭이 펼쳐짐 → 핀이 하나씩 튀어나옴.
    /// <b>접기</b>: 핀이 들어감 → 오른쪽 · 왼쪽 폭이 접힘 → 접힌 종이가 아래로 내려가며 사라짐. 열려 있는 동안 게임은 멈춤(실제 시간으로 애니메이션).
    /// </summary>
    public class WorldMapView
    {
        [Serializable] class Ground { public float[] origin, perX, perY; }
        [Serializable] class Landmark { public string id, name; public float[] blender, uv; }
        [Serializable] class MapData { public int[] image; public Ground ground; public float[] spawnBlender; public Landmark[] landmarks; }

        public bool IsOpen => state != State.Closed;
        public bool Available => tex != null && data != null;
        enum State { Closed, Opening, Open, Closing }
        State state = State.Closed; float t;
        const float OpenTime = 1.15f, CloseTime = 0.75f;

        RectTransform root, sheet, pinLayer, player; CanvasGroup rootGroup; Image dim; Text title, hint;
        readonly RectTransform[] panel = new RectTransform[3]; readonly RawImage[] front = new RawImage[3], back = new RawImage[3]; readonly Image[] shade = new Image[3];
        readonly List<RectTransform> pins = new(); Texture2D tex, backTex; MapData data; Vector2 size;
        Transform playerTf; Matrix4x4 worldToBlender = Matrix4x4.identity; bool calibrated;

        public void Build(Transform canvas, Transform playerTransform)
        {
            playerTf = playerTransform;
            tex = Resources.Load<Texture2D>("UI/WorldMap/WorldMap"); backTex = Resources.Load<Texture2D>("UI/WorldMap/WorldMapBack");
            var json = Resources.Load<TextAsset>("UI/WorldMap/WorldMapData"); if (json != null) data = JsonUtility.FromJson<MapData>(json.text);
            root = NewRect("WorldMap", canvas); Stretch(root); rootGroup = root.gameObject.AddComponent<CanvasGroup>();
            dim = Box(root, "Dim", new Color(0, 0, 0, .65f)); Stretch(dim.rectTransform);
            float h = 900f, w = h * (tex != null ? (float)tex.width / tex.height : 1.6f); size = new Vector2(w, h);
            sheet = NewRect("Sheet", root); At(sheet, new Vector2(.5f, .5f), new Vector2(0, -10), size);
            // 세 폭: 가운데(고정) · 왼쪽(오른쪽 끝이 접는 선) · 오른쪽(왼쪽 끝이 접는 선). 접힌 폭은 가운데 위에 겹쳐 뒷면이 보임
            int[] order = { 1, 0, 2 };
            foreach (int i in order)
            {
                var p = NewRect("Panel" + i, sheet); panel[i] = p; p.anchorMin = p.anchorMax = new Vector2(i == 0 ? 1f / 3 : i == 1 ? .5f : 2f / 3, .5f);
                p.pivot = new Vector2(i == 0 ? 1 : i == 1 ? .5f : 0, .5f); p.sizeDelta = new Vector2(w / 3f, h); p.anchoredPosition = Vector2.zero;
                front[i] = Raw(p, "Front", tex, new Rect(i / 3f, 0, 1 / 3f, 1)); back[i] = Raw(p, "Back", backTex, new Rect(i / 3f, 0, 1 / 3f, 1));
                shade[i] = Box(p, "Shade", new Color(0, 0, 0, 0)); Stretch(shade[i].rectTransform);
            }
            pinLayer = NewRect("Pins", sheet); Stretch(pinLayer);
            if (data != null && data.landmarks != null) foreach (var lm in data.landmarks) pins.Add(MakePin(lm));
            var pm = Resources.Load<Texture2D>("UI/WorldMap/PlayerMarker");
            var pr = Raw(pinLayer, "Player", pm, new Rect(0, 0, 1, 1)); player = pr.rectTransform; player.anchorMin = player.anchorMax = Vector2.zero; player.sizeDelta = new Vector2(46, 46);
            title = Txt(root, "세계 지도", 46, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); At(title.rectTransform, new Vector2(.5f, .5f), new Vector2(0, h / 2 + 34), new Vector2(800, 60)); title.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            hint = Txt(root, "M · Esc   지도 접기", 24, UITheme.Cream, TextAnchor.MiddleCenter); At(hint.rectTransform, new Vector2(.5f, .5f), new Vector2(0, -h / 2 - 38), new Vector2(800, 40)); hint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            root.gameObject.SetActive(false);
        }

        static RawImage Raw(Transform parent, string name, Texture t, Rect uv)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage)); go.transform.SetParent(parent, false);
            var r = go.GetComponent<RawImage>(); r.texture = t; r.uvRect = uv; r.raycastTarget = false; Stretch(r.rectTransform); return r;
        }

        /// <summary>
        /// 핀 위치 보정(px)과 이름표 위/아래(tools/world_map_mock.py 의 LAYOUT 과 같게). 가까이 붙은 장소끼리 이름표가 겹치지 않도록:
        /// 산지 = 성 바로 뒤라 오른쪽 봉우리로 옮기고 이름표 위 · 광장 = 이름표 위 · 서쪽/동쪽 마을 = 바깥으로 조금씩.
        /// </summary>
        public static (Vector2 offset, bool labelAbove) Layout(string id) => id switch
        {
            "highland" => (new Vector2(70, 12), true), "plaza" => (Vector2.zero, true),
            "west" => (new Vector2(-60, 0), false), "east" => (new Vector2(60, 0), false), _ => (Vector2.zero, false),
        };

        RectTransform MakePin(Landmark lm)
        {
            var (off, above) = Layout(lm.id);
            var p = NewRect("Pin_" + lm.id, pinLayer); p.anchorMin = p.anchorMax = new Vector2(lm.uv[0], lm.uv[1]); p.sizeDelta = new Vector2(10, 10); p.anchoredPosition = off;
            var glow = Img(p, "Glow", SkillPanelArt.Glow, new Color(1f, .85f, .4f, .55f), false); At(glow.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(150, 150));
            var icon = Raw(p, "Icon", Resources.Load<Texture2D>("UI/WorldMap/Pin_" + lm.id), new Rect(0, 0, 1, 1)); At(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 46), new Vector2(72, 72));
            var stem = Box(p, "Stem", new Color(.85f, .7f, .35f)); At(stem.rectTransform, new Vector2(.5f, .5f), new Vector2(0, 6), new Vector2(4, 16));
            var label = Img(p, "Label", UITheme.Slot, new Color(1, 1, 1, .95f), true); At(label.rectTransform, new Vector2(.5f, .5f), new Vector2(0, above ? 106 : -22), new Vector2(40 + lm.name.Length * 26, 40));
            var t = Txt(label.transform, lm.name, 26, UITheme.GoldBright, TextAnchor.MiddleCenter, FontStyle.Bold); Stretch(t.rectTransform); t.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .9f);
            p.localScale = Vector3.zero; return p;
        }

        // ── 열기/닫기 ─────────────────────────────
        public void Open()
        {
            if (!Available || state == State.Open || state == State.Opening) return;
            if (!calibrated) Calibrate();
            if (state == State.Closed) t = 0f; else t = OpenTime * (1f - t / CloseTime);   // 접는 도중이면 그 자리에서 다시 펼침
            state = State.Opening; root.gameObject.SetActive(true); UIState.SetPaused(true); Apply(0f);
        }
        public void Close()
        {
            if (state == State.Closed || state == State.Closing) return;
            t = state == State.Opening ? CloseTime * (1f - t / OpenTime) : 0f; state = State.Closing;
        }
        public void Toggle() { if (state == State.Open || state == State.Opening) Close(); else Open(); }

        public void Update()
        {
            if (state == State.Closed) return;
            float dt = Time.unscaledDeltaTime; t += dt;
            if (state == State.Opening) { float u = Mathf.Clamp01(t / OpenTime); Apply(u); if (u >= 1f) state = State.Open; }
            else if (state == State.Closing) { float u = 1f - Mathf.Clamp01(t / CloseTime); Apply(u); if (u <= 0f) { state = State.Closed; root.gameObject.SetActive(false); UIState.SetPaused(false); } }
            else Apply(1f);
            UpdatePlayer();
            // 핀이 살짝 숨 쉬듯
            if (state == State.Open) for (int i = 0; i < pins.Count; i++) pins[i].localScale = Vector3.one * (1f + .04f * Mathf.Sin(Time.unscaledTime * 3f + i));
        }

        /// <summary>u = 0(접혀 숨음) ~ 1(다 펼침). 펼칠 때·접을 때 같은 곡선을 앞뒤로 씀.</summary>
        void Apply(float u)
        {
            float a = Seg(u, 0f, .22f);                       // 떠오름
            float l = Ease(Seg(u, .2f, .52f)), r = Ease(Seg(u, .42f, .74f));   // 왼쪽 · 오른쪽 폭 펼침
            rootGroup.alpha = Mathf.Clamp01(a * 1.5f); dim.color = new Color(0, 0, 0, .65f * a);
            sheet.anchoredPosition = new Vector2(0, -10 - 140 * (1 - Ease(a))); float s = Mathf.Lerp(.72f, 1f, Ease(a)); sheet.localScale = new Vector3(s, s, 1);
            Fold(0, 180f * (1 - l)); Fold(2, -180f * (1 - r)); Fold(1, 0f);
            title.color = new Color(title.color.r, title.color.g, title.color.b, Seg(u, .7f, .85f)); hint.color = new Color(hint.color.r, hint.color.g, hint.color.b, Seg(u, .75f, .9f));
            for (int i = 0; i < pins.Count; i++)                // 핀이 하나씩 튀어나옴(넘쳤다가 1)
            {
                float k = Seg(u, .72f + i * .04f, .82f + i * .04f); float pop = k <= 0 ? 0 : k < .7f ? Mathf.Lerp(0, 1.2f, k / .7f) : Mathf.Lerp(1.2f, 1f, (k - .7f) / .3f);   // 마지막 핀도 u = 1 전에 끝남
                if (state != State.Open) pins[i].localScale = Vector3.one * pop;
            }
            player.gameObject.SetActive(u > .8f);
        }

        /// <summary>폭 하나를 접는 선 기준으로 회전. 90° 넘으면 뒷면이 보이고, 기울수록 그늘.</summary>
        void Fold(int i, float angle)
        {
            panel[i].localRotation = Quaternion.Euler(0, angle, 0);
            float k = Mathf.Abs(Mathf.Sin(angle * Mathf.Deg2Rad)); panel[i].localScale = new Vector3(1, 1f + .05f * k, 1);   // 들린 폭이 살짝 가까워 보이게
            bool showBack = Mathf.Abs(angle) > 90f; front[i].enabled = !showBack; back[i].enabled = showBack;
            shade[i].color = new Color(0, 0, 0, .55f * k);
        }
        static float Seg(float u, float a, float b) => Mathf.Clamp01((u - a) / (b - a));
        static float Ease(float x) => 1f - (1f - x) * (1f - x) * (1f - x);

        // ── 내 위치 ──────────────────────────────
        void UpdatePlayer()
        {
            if (playerTf == null || data == null) return;
            Vector2 b = ToBlender(playerTf.position), f = ToBlender(playerTf.position + playerTf.forward) - b;
            Vector2 uv = BlenderToUv(b), duv = BlenderToUv(b + f.normalized) - uv;
            player.anchorMin = player.anchorMax = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y)); player.anchoredPosition = Vector2.zero;
            player.localRotation = Quaternion.Euler(0, 0, -Mathf.Atan2(duv.x * size.x, duv.y * size.y) * Mathf.Rad2Deg);
            float pulse = 1f + .12f * Mathf.Sin(Time.unscaledTime * 6f); player.localScale = new Vector3(pulse, pulse, 1);
        }
        Vector2 BlenderToUv(Vector2 b)
        {
            var g = data.ground; return new Vector2(g.origin[0] + g.perX[0] * b.x + g.perY[0] * b.y, g.origin[1] + g.perX[1] * b.x + g.perY[1] * b.y);
        }
        Vector2 ToBlender(Vector3 w) { var p = worldToBlender.MultiplyPoint3x4(w); return new Vector2(p.x, p.y); }

        /// <summary>게임(Unity) 좌표 → 지도를 만든 Blender 좌표. 축 맞추기는 MapPlaces 가 함(PlayerSpawn ↔ spawnBlender).</summary>
        void Calibrate() { calibrated = true; worldToBlender = MapPlaces.WorldToBlender; }
    }
}
