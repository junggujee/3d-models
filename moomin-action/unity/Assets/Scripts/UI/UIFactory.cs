using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>uGUI 조립 도우미(프리팹 없이 코드로 UI를 만든다).</summary>
    public static class UIFactory
    {
        /// <summary>화면 덮개 캔버스(1920×1080 기준 크기 맞춤) + 클릭 판정. EventSystem 이 없으면 함께 만듦.</summary>
        public static Canvas MakeCanvas(Transform parent, string name, int order)
        {
            var cgo = new GameObject(name); cgo.transform.SetParent(parent, false); var canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var cs = cgo.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f; cgo.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
            return canvas;
        }

        public static RectTransform NewRect(string name, Transform parent) { var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false); return (RectTransform)go.transform; }
        public static RectTransform Set(RectTransform r, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size) { r.anchorMin = aMin; r.anchorMax = aMax; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size; return r; }
        public static RectTransform Stretch(RectTransform r, float pad = 0) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(pad, pad); r.offsetMax = new Vector2(-pad, -pad); return r; }
        /// <summary>앵커 = 피벗 = 같은 점(0~1), 위치/크기 지정.</summary>
        public static RectTransform At(RectTransform r, Vector2 anchor, Vector2 pos, Vector2 size) => Set(r, anchor, anchor, anchor, pos, size);

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool sliced = true, bool raycast = false)
        {
            var r = NewRect(name, parent); var im = r.gameObject.AddComponent<Image>(); im.sprite = sprite; im.color = color; im.type = sliced && sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple; im.raycastTarget = raycast; return im;
        }
        public static Image Box(Transform parent, string name, Color color) => Img(parent, name, UITheme.White, color, false);

        public static Text Txt(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, string name = "Text")
        {
            var r = NewRect(name, parent); var t = r.gameObject.AddComponent<Text>(); t.font = UITheme.Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false; t.supportRichText = true;
            var sh = r.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.85f); sh.effectDistance = new Vector2(1.5f, -1.5f); return t;
        }

        /// <summary>제목·장·보스 이름: Display 글꼴 + 크림→베이지 그라데이션 + 부드러운 그림자(+ 선택: 은은한 빛).</summary>
        public static Text StyleDisplay(Text t, bool glow = false)
        {
            t.font = UITheme.DisplayFont; t.fontStyle = FontStyle.Normal; t.color = Color.white;
            var g = t.gameObject.AddComponent<UIGradient>(); g.top = UITheme.DisplayTop; g.bottom = UITheme.DisplayBottom;
            if (t.TryGetComponent<Shadow>(out var sh)) { sh.effectColor = new Color(0.08f, 0.04f, 0.02f, 0.75f); sh.effectDistance = new Vector2(2f, -3f); }
            if (glow) { var gl = t.gameObject.AddComponent<TextGlow>(); gl.color = new Color(1f, 0.86f, 0.6f, 0.28f); gl.radius = 5f; }
            return t;
        }
        /// <summary>이름·퀘스트 제목·메뉴 머리글: Heading 글꼴(굵게 흉내 없이 글꼴 그대로).</summary>
        public static Text StyleHeading(Text t) { t.font = UITheme.HeadingFont; t.fontStyle = FontStyle.Normal; return t; }
        /// <summary>스킬 이름: Skill 글꼴 + 은은한 빛(glow 색 = 속성·코인 색 등) — "조금 빛나고 깔끔하게, 너무 컴퓨터스럽지 않게".</summary>
        public static Text StyleSkill(Text t, Color glow)
        {
            t.font = UITheme.SkillFont; t.fontStyle = FontStyle.Normal;
            var g = t.gameObject.AddComponent<UIGradient>(); g.top = Color.white; g.bottom = new Color(0.93f, 0.89f, 0.8f);
            var gl = t.gameObject.AddComponent<TextGlow>(); gl.color = new Color(glow.r, glow.g, glow.b, 0.55f); gl.radius = 4f; return t;
        }
        /// <summary>스타 세일러풍 장식선(가는 금선 + 양끝 마름모 + 가운데 네 갈래 별). 없으면 금색 구분선.</summary>
        public static Image Divider(Transform parent, string name, float width)
        {
            var sp = UITheme.Art("Ornament", "Divider"); var im = Img(parent, name, sp != null ? sp : UITheme.Highlight, Color.white, sp == null);
            im.rectTransform.sizeDelta = new Vector2(width, sp != null ? width * 24f / 600f : 12f); im.preserveAspect = sp != null; return im;
        }

        /// <summary>장식 패널(테두리 포함).</summary>
        public static Image Panel(Transform parent, string name, Vector2 size, Vector2 anchor = default, Vector2 pos = default)
        {
            var im = Img(parent, name, UITheme.Frame, Color.white, true, true); At(im.rectTransform, anchor == default ? new Vector2(.5f, .5f) : anchor, pos, size); return im;
        }

        /// <summary>제목 + 금색 구분선.</summary>
        public static void Header(Transform panel, string title, float width, float top = -26f)
        {
            var t = StyleHeading(Txt(panel, title, 46, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Normal, "Title")); At(t.rectTransform, new Vector2(.5f, 1), new Vector2(0, top), new Vector2(width - 80, 70)); t.rectTransform.pivot = new Vector2(.5f, 1);
            var line = Img(panel, "Divider", UITheme.Highlight, new Color(1, 1, 1, 1), true); At(line.rectTransform, new Vector2(.5f, 1), new Vector2(0, top - 78), new Vector2(width - 120, 12)); line.rectTransform.pivot = new Vector2(.5f, 1);
        }

        public static void OnHover(GameObject go, Action a) => AddTrigger(go, EventTriggerType.PointerEnter, _ => a());
        public static void OnClick(GameObject go, Action a) => AddTrigger(go, EventTriggerType.PointerClick, _ => a());
        public static void OnClick(GameObject go, Action<PointerEventData> a) => AddTrigger(go, EventTriggerType.PointerClick, a);
        public static void OnDrag(GameObject go, Action<PointerEventData> a) => AddTrigger(go, EventTriggerType.Drag, a);
        static void AddTrigger(GameObject go, EventTriggerType type, Action<PointerEventData> a)
        {
            var et = go.GetComponent<EventTrigger>(); if (et == null) et = go.AddComponent<EventTrigger>();   // Unity 의 '가짜 null' 때문에 ?? 를 쓰지 않음
            var e = new EventTrigger.Entry { eventID = type }; e.callback.AddListener(d => a((PointerEventData)d)); et.triggers.Add(e);
        }

        /// <summary>게이지(틀 + 채움). 채움 비율은 SetFill 로.</summary>
        public class Gauge
        {
            public RectTransform root, fill, trail; public Text label;
            public void SetFill(float v) { fill.anchorMax = new Vector2(Mathf.Clamp01(v), 1); }
            public void SetTrail(float v) { if (trail) trail.anchorMax = new Vector2(Mathf.Clamp01(v), 1); }
        }
        public static Gauge MakeGauge(Transform parent, string name, Color fill, Vector2 size, bool withTrail, bool label)
        {
            var g = new Gauge(); var frame = Img(parent, name, UITheme.Bar, Color.white); g.root = frame.rectTransform; frame.rectTransform.sizeDelta = size;
            var inner = NewRect("Inner", frame.transform); Stretch(inner, 3);
            if (withTrail) { var tr = Box(inner, "Trail", new Color(0.85f, 0.7f, 0.25f, 0.9f)); Stretch(tr.rectTransform); tr.rectTransform.anchorMax = Vector2.one; g.trail = tr.rectTransform; tr.rectTransform.offsetMax = Vector2.zero; }
            var f = Box(inner, "Fill", fill); Stretch(f.rectTransform); g.fill = f.rectTransform;
            var shine = Box(f.transform, "Shine", new Color(1, 1, 1, 0.12f)); Set(shine.rectTransform, new Vector2(0, .55f), Vector2.one, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            if (label) { g.label = Txt(frame.transform, "", 18, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.Normal, "Label"); Stretch(g.label.rectTransform); }
            return g;
        }
    }
}
