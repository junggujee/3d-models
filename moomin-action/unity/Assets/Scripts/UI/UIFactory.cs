using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>uGUI 조립 도우미(프리팹 없이 코드로 UI를 만든다).</summary>
    public static class UIFactory
    {
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

        /// <summary>장식 패널(테두리 포함).</summary>
        public static Image Panel(Transform parent, string name, Vector2 size, Vector2 anchor = default, Vector2 pos = default)
        {
            var im = Img(parent, name, UITheme.Frame, Color.white, true, true); At(im.rectTransform, anchor == default ? new Vector2(.5f, .5f) : anchor, pos, size); return im;
        }

        /// <summary>제목 + 금색 구분선.</summary>
        public static void Header(Transform panel, string title, float width, float top = -26f)
        {
            var t = Txt(panel, title, 46, UITheme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, "Title"); At(t.rectTransform, new Vector2(.5f, 1), new Vector2(0, top), new Vector2(width - 80, 70)); t.rectTransform.pivot = new Vector2(.5f, 1);
            var line = Img(panel, "Divider", UITheme.Highlight, new Color(1, 1, 1, 1), true); At(line.rectTransform, new Vector2(.5f, 1), new Vector2(0, top - 78), new Vector2(width - 120, 12)); line.rectTransform.pivot = new Vector2(.5f, 1);
        }

        public static void OnHover(GameObject go, Action a) => AddTrigger(go, EventTriggerType.PointerEnter, _ => a());
        public static void OnClick(GameObject go, Action a) => AddTrigger(go, EventTriggerType.PointerClick, _ => a());
        public static void OnClick(GameObject go, Action<PointerEventData> a) => AddTrigger(go, EventTriggerType.PointerClick, a);
        static void AddTrigger(GameObject go, EventTriggerType type, Action<PointerEventData> a)
        {
            var et = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>(); var e = new EventTrigger.Entry { eventID = type }; e.callback.AddListener(d => a((PointerEventData)d)); et.triggers.Add(e);
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
