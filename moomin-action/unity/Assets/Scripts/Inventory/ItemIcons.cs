using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>이미지 파일 없이 아이콘을 코드로 그림(부호 있는 거리 함수 + 부드러운 가장자리). 아이템 id별로 캐시.</summary>
    public static class ItemIcons
    {
        static readonly Dictionary<string, Sprite> cache = new();
        const int N = 64;
        delegate float Sdf(float x, float y);
        static float Circ(float cx, float cy, float r, float x, float y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;
        static float Ell(float cx, float cy, float rx, float ry, float x, float y) => (Mathf.Sqrt(((x - cx) / rx) * ((x - cx) / rx) + ((y - cy) / ry) * ((y - cy) / ry)) - 1f) * Mathf.Min(rx, ry);
        static float Box(float cx, float cy, float hw, float hh, float x, float y) { float dx = Mathf.Abs(x - cx) - hw, dy = Mathf.Abs(y - cy) - hh; return Mathf.Min(Mathf.Max(dx, dy), 0f) + new Vector2(Mathf.Max(dx, 0f), Mathf.Max(dy, 0f)).magnitude; }
        static float Seg(float ax, float ay, float bx, float by, float r, float x, float y) { var pa = new Vector2(x - ax, y - ay); var ba = new Vector2(bx - ax, by - ay); float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba)); return (pa - ba * h).magnitude - r; }
        static float Diamond(float cx, float cy, float r, float x, float y) => (Mathf.Abs(x - cx) + Mathf.Abs(y - cy)) * 0.7071f - r;
        static float Ring(float cx, float cy, float r, float w, float x, float y) => Mathf.Abs(Circ(cx, cy, r, x, y)) - w;

        public static Sprite Get(ItemDef d)
        {
            if (d == null) return null; if (cache.TryGetValue(d.id, out var s)) return s;
            var art = Resources.Load<Texture2D>("ItemIcons/" + d.id);   // 렌더한 아이콘(blender 로 만든 장비 등)이 있으면 그것
            if (art != null) { s = Sprite.Create(art, new Rect(0, 0, art.width, art.height), new Vector2(.5f, .5f), 100f); cache[d.id] = s; return s; }
            var layers = new List<(Sdf sdf, Color c)>(); Color c = d.color, dark = c * 0.55f; dark.a = 1f; Color light = Color.Lerp(c, Color.white, 0.55f);
            switch (d.icon)
            {
                case IconKind.Potion:
                    layers.Add(((x, y) => Circ(.5f, .36f, .24f, x, y), c)); layers.Add(((x, y) => Box(.5f, .64f, .075f, .11f, x, y), new Color(.82f, .88f, .92f)));
                    layers.Add(((x, y) => Box(.5f, .78f, .1f, .045f, x, y), new Color(.45f, .3f, .15f))); layers.Add(((x, y) => Circ(.4f, .42f, .07f, x, y), new Color(1, 1, 1, .55f))); break;
                case IconKind.Bread:
                    layers.Add(((x, y) => Ell(.5f, .46f, .34f, .22f, x, y), c)); layers.Add(((x, y) => Seg(.32f, .5f, .4f, .62f, .025f, x, y), dark)); layers.Add(((x, y) => Seg(.48f, .5f, .56f, .64f, .025f, x, y), dark)); layers.Add(((x, y) => Seg(.64f, .5f, .7f, .6f, .025f, x, y), dark)); break;
                case IconKind.Dust:
                    layers.Add(((x, y) => Circ(.38f, .38f, .13f, x, y), c)); layers.Add(((x, y) => Circ(.62f, .44f, .1f, x, y), light)); layers.Add(((x, y) => Circ(.5f, .66f, .12f, x, y), c));
                    layers.Add(((x, y) => Seg(.5f, .2f, .5f, .82f, .015f, x, y), new Color(1, .95f, .6f))); layers.Add(((x, y) => Seg(.22f, .5f, .8f, .5f, .015f, x, y), new Color(1, .95f, .6f))); break;
                case IconKind.Shard:
                    layers.Add(((x, y) => Diamond(.5f, .5f, .26f, x, y) , c)); layers.Add(((x, y) => Diamond(.5f, .5f, .13f, x, y), light)); layers.Add(((x, y) => Seg(.5f, .12f, .5f, .88f, .012f, x, y), new Color(1, 1, 1, .6f))); break;
                case IconKind.Feather:
                    layers.Add(((x, y) => Seg(.3f, .22f, .72f, .8f, .08f, x, y), c)); layers.Add(((x, y) => Seg(.26f, .16f, .72f, .8f, .012f, x, y), dark)); layers.Add(((x, y) => Seg(.42f, .4f, .6f, .52f, .02f, x, y), light)); break;
                case IconKind.Sword:
                    layers.Add(((x, y) => Seg(.26f, .24f, .76f, .78f, .045f, x, y), c)); layers.Add(((x, y) => Seg(.3f, .28f, .72f, .74f, .012f, x, y), Color.white));
                    layers.Add(((x, y) => Seg(.22f, .4f, .4f, .22f, .03f, x, y), new Color(.8f, .1f, .1f))); layers.Add(((x, y) => Seg(.2f, .2f, .3f, .3f, .035f, x, y), new Color(.2f, .2f, .24f))); break;
                case IconKind.Headband:   // 주름진 고리(머리띠)
                    layers.Add(((x, y) => Mathf.Abs(Ell(.5f, .5f, .33f, .24f, x, y)) - .07f - .012f * Mathf.Sin(Mathf.Atan2(y - .5f, x - .5f) * 22f), c));
                    layers.Add(((x, y) => Mathf.Abs(Ell(.5f, .53f, .3f, .21f, x, y)) - .02f, light)); break;
                case IconKind.Artifact:   // 빛나는 보석
                    layers.Add(((x, y) => Circ(.5f, .5f, .3f, x, y), new Color(c.r, c.g, c.b, .35f))); layers.Add(((x, y) => Diamond(.5f, .5f, .2f, x, y), c)); layers.Add(((x, y) => Diamond(.46f, .55f, .07f, x, y), light)); break;
                case IconKind.Key:
                    layers.Add(((x, y) => Ring(.34f, .64f, .15f, .04f, x, y), c)); layers.Add(((x, y) => Seg(.45f, .53f, .78f, .2f, .035f, x, y), c));
                    layers.Add(((x, y) => Seg(.68f, .3f, .76f, .38f, .03f, x, y), c)); layers.Add(((x, y) => Seg(.6f, .38f, .67f, .45f, .03f, x, y), c)); break;
            }
            var px = new Color[N * N];
            for (int j = 0; j < N; j++) for (int i = 0; i < N; i++)
            {
                float x = (i + .5f) / N, y = (j + .5f) / N; Color o = new Color(0, 0, 0, 0);
                // 은은한 그림자
                float sh = 0; foreach (var l in layers) sh = Mathf.Max(sh, Mathf.Clamp01(.5f - l.sdf(x - .015f, y + .02f) * N * .5f)); o = new Color(0, 0, 0, sh * .35f);
                foreach (var l in layers) { float a = Mathf.Clamp01(.5f - l.sdf(x, y) * N * .5f) * l.c.a; if (a <= 0) continue; var cc = l.c; cc.a = 1; o = Color.Lerp(o, cc, a); o.a = Mathf.Max(o.a, a); }
                px[j * N + i] = o;
            }
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp }; tex.SetPixels(px); tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(.5f, .5f), 100f); cache[d.id] = s; return s;
        }
    }
}
