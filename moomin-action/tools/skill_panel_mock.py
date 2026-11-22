"""
스킬 패널 v3 + 클래시 연출 미리보기(Unity 없이). UI/SkillPanel.cs · SkillPanelArt.cs · ClashStage.cs 의 배치·좌표·스프라이트 생성(같은 해시)을 그대로 옮김.
  python3 tools/skill_panel_mock.py   →  previews/skill_panel_v3_mock.png (1920×1080 화면의 클래시 순간)
"""
import math, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__)); ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
from illustration_preview import render as render_illustration, hash2, noise  # noqa: E402

FONT_B = "/usr/share/fonts/truetype/nanum/NanumSquareB.ttf"
FONT_R = "/usr/share/fonts/truetype/nanum/NanumSquareR.ttf"
if not os.path.exists(FONT_R): FONT_R = FONT_B
ICONS = os.path.join(ROOT, "unity", "Assets", "Resources", "StatusIcons")

def H1(k): return float(hash2(k * 7919 + 13, k * 104729 + 7))

# ── SkillPanelArt 과 같은 알파 획 그리기 ────────────────
def seg(a, x0, y0, x1, y1, r0, r1, alpha):
    h, w = a.shape; m = max(r0, r1) + 2
    i0, i1 = max(0, int(min(x0, x1) - m)), min(w - 1, int(max(x0, x1) + m)); j0, j1 = max(0, int(min(y0, y1) - m)), min(h - 1, int(max(y0, y1) + m))
    if i1 < i0 or j1 < j0: return
    J, I = np.mgrid[j0:j1 + 1, i0:i1 + 1].astype(np.float32); px, py = I + .5 - x0, J + .5 - y0; bx, by = x1 - x0, y1 - y0
    t = np.clip((px * bx + py * by) / max(1e-6, bx * bx + by * by), 0, 1); d = np.hypot(px - bx * t, py - by * t) - (r0 + (r1 - r0) * t)
    v = np.clip(.5 - d, 0, 1) * alpha; a[j0:j1 + 1, i0:i1 + 1] = np.maximum(a[j0:j1 + 1, i0:i1 + 1], v)

def edge_seg(a, S, side, p0, o0, p1, o1, r, alpha):
    if side == 0: seg(a, p0, o0, p1, o1, r, r, alpha)
    elif side == 1: seg(a, p0, S - o0, p1, S - o1, r, r, alpha)
    elif side == 2: seg(a, o0, p0, o1, p1, r, r, alpha)
    else: seg(a, S - o0, p0, S - o1, p1, r, r, alpha)

def build_frame():
    S = 128; a = np.zeros((S, S), np.float32); k = 0
    for side in range(4):
        p = 2.0
        while p < S - 2:
            ln = 8 + H1(k) * 22; o0 = 5 + (H1(k + 500) - .5) * 1.6; o1 = 5 + (H1(k + 900) - .5) * 1.6; q = min(S - 2, p + ln)
            if H1(k + 1300) > .06: edge_seg(a, S, side, p, o0, q, o1, 1.3 + H1(k + 1700) * .7, 1)
            p = q + (2 if H1(k + 2100) > .8 else 0); k += 1
    for c in range(4):
        rx, ry = c in (1, 2), c >= 2
        X = (lambda v: S - v) if rx else (lambda v: v); Y = (lambda v: S - v) if ry else (lambda v: v)
        seg(a, X(11), Y(11), X(11 + 26 + H1(c + 40) * 10), Y(11), .7, .7, .75); seg(a, X(11), Y(11), X(11), Y(11 + 26 + H1(c + 50) * 10), .7, .7, .75)
        seg(a, X(1.5), Y(1.5), X(19), Y(1.5), 2, .8, 1); seg(a, X(1.5), Y(1.5), X(1.5), Y(19), 2, .8, 1)
        for n in range(4):
            sx, sy, l, ang = 3 + H1(k + 3000) * 26, 3 + H1(k + 3100) * 26, 8 + H1(k + 3200) * 18, math.radians(200 + H1(k + 3300) * 50)
            seg(a, X(sx), Y(sy), X(sx + math.cos(ang) * l), Y(sy + math.sin(ang) * l), .55 + H1(k + 3400) * .4, .1, .5 + H1(k + 3500) * .4); k += 1
    return a[::-1]          # Unity 텍스처는 아래가 j=0 → 이미지 좌표로 뒤집기

def build_banner_ink():
    W, Hh = 256, 64; a = np.zeros((Hh, W), np.float32); k = 7000
    for n in range(46):
        x0 = 40 + H1(k) ** .55 * 190; l = 20 + H1(k + 100) * 70; y = 6 + H1(k + 200) * 52
        seg(a, x0, y, min(232, x0 + l), y + (H1(k + 300) - .5) * 4, .4 + H1(k + 400) * 1.6, .2, .25 + .55 * min(1, max(0, (x0 - 40) / 170))); k += 1
    seg(a, 2, 4, 236, 5, .9, .5, .9); seg(a, 2, 60, 222, 59, .9, .5, .9)
    for n in range(6):
        x = 150 + H1(k) * 90; seg(a, x, 2, x + 10 + H1(k + 50) * 14, 62, .5, .2, .6); k += 1
    return a[::-1]

def build_banner():
    W, Hh = 256, 64; I, J = np.meshgrid(np.arange(W), np.arange(Hh)); x, y = I / 255, J / 63
    edge = .9 + (y - .5) * .2 + (noise(J * .5, np.full_like(J, 3.0, dtype=float)) - .5) * .06
    rough = ~((y < .08 + noise(I * .3, np.ones_like(I, float)) * .08) | (y > .92 - noise(I * .3, np.full_like(I, 5.0, dtype=float)) * .08))
    a = np.where(x > edge, 0, rough * (.82 + .18 * noise(I * .05, J * .3))); return a[::-1].astype(np.float32)

def build_slashes():
    S = 256; a = np.zeros((S, S), np.float32); k = 9000
    for n in range(9):
        x0 = H1(k) * 120 - 20; y0 = 256 - H1(k + 10) * 40; l = 150 + H1(k + 20) * 170; ang = math.radians(-38 - H1(k + 30) * 22); r = 1.2 + H1(k + 40) * 4.5
        for m in range(10):
            t0, t1 = m / 10, (m + 1) / 10; w0 = r * math.sin(math.pi * (.08 + .87 * t0)); w1 = r * math.sin(math.pi * (.08 + .87 * t1))
            seg(a, x0 + math.cos(ang) * l * t0, y0 + math.sin(ang) * l * t0, x0 + math.cos(ang) * l * t1, y0 + math.sin(ang) * l * t1, w0, w1, .55 + H1(k + 50) * .45)
        k += 1
    return a[::-1]

def hex_d(x, y): x = np.abs(x * 2 - 1); y = np.abs(y * 2 - 1); return np.maximum(x * .866 + y * .5, y) - .9
def build_hex():
    I, J = np.meshgrid(np.arange(128), np.arange(128)); x, y = (I + .5) / 128, (J + .5) / 128; d = hex_d(x, y); n = noise(I * .3, J * .3)
    fill = np.clip(.5 - d * 64, 0, 1); thick = (np.abs(d + .07) < .05 + n * .025) * (.75 + .25 * n); thin = (np.abs(d + .19) < .012) * .8
    return fill[::-1].astype(np.float32), np.maximum(thick, thin)[::-1].astype(np.float32)

def coin(look, size):
    I, J = np.meshgrid(np.arange(64), np.arange(64)); x, y = (I + .5) / 32 - 1, (J + .5) / 32 - 1
    octd = np.maximum(np.maximum(np.abs(x), np.abs(y)), (np.abs(x) + np.abs(y)) * .7071) - .9; a = np.clip(.5 - octd * 32, 0, 1); r = np.hypot(x, y)
    if look == 1:
        t = np.clip((y * -.5 + .5) * .8 + r * .3, 0, 1)[..., None]; c = np.array([1, .9, .5]) * (1 - t) + np.array([.8, .55, .15]) * t
        c = np.where((octd > -.12)[..., None], c * .7, c); c = np.where((np.abs(r - .45) < .04)[..., None], c * .4 + np.array([1, .97, .8]) * .6, c)
    elif look == 2:
        t = np.clip(r, 0, 1)[..., None]; c = np.array([.55, .53, .5]) * (1 - t) + np.array([.3, .29, .28]) * t; c = np.where((octd > -.12)[..., None], c * .75, c)
    else:
        c = np.broadcast_to(np.array([.75, .58, .25]), (64, 64, 3)); a = np.where(octd > -.14, a, 0)
    img = np.dstack([c, a])[::-1]; return Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8), "RGBA").resize((size, size), Image.LANCZOS)

def tinted(alpha, color, size=None, alpha_mul=1.0):
    h, w = alpha.shape; img = np.zeros((h, w, 4), np.float32); img[..., :3] = color[:3]; img[..., 3] = alpha * (color[3] if len(color) > 3 else 1) * alpha_mul
    im = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8), "RGBA"); return im.resize(size, Image.LANCZOS) if size else im

def nine(alpha, color, w, h, b):
    """9분할(Unity Image.Type.Sliced)."""
    src = tinted(alpha, color); S = src.width; sb = 34; out = Image.new("RGBA", (int(w), int(h))); w, h = int(w), int(h)   # sb = 원본 테두리(px), b = 화면 테두리
    xs = [(0, sb, 0, b), (sb, S - sb, b, w - b), (S - sb, S, w - b, w)]; ys = [(0, sb, 0, b), (sb, S - sb, b, h - b), (S - sb, S, h - b, h)]
    for sx0, sx1, dx0, dx1 in xs:
        for sy0, sy1, dy0, dy1 in ys:
            if dx1 > dx0 and dy1 > dy0: out.alpha_composite(src.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return out

FRAME, BANNER, INK, SLASH = build_frame(), build_banner(), build_banner_ink(), build_slashes(); HEXF, HEXR = build_hex()
def glow(size, color, a=.45):
    I, J = np.meshgrid(np.linspace(-1, 1, size[0]), np.linspace(-1, 1, size[1])); al = np.clip(1 - np.hypot(I, J), 0, 1) ** 2
    return tinted(al.astype(np.float32), (*color, a))
def icon(name, size): return Image.open(os.path.join(ICONS, name + ".png")).resize((size, size), Image.LANCZOS)
def hexc(h): return tuple(int(h[i:i + 2], 16) / 255 for i in (1, 3, 5))

ATTR = {"Red": (.86, .16, .2), "Blue": (.2, .45, .95), "Yellow": (.95, .78, .15), "Purple": (.6, .3, .9), "Orange": (.98, .5, .12)}
STATUS_HEX = {"Bleed": "#DB333D", "Burn": "#FF8C26", "Sinking": "#4A63D9", "Tremor": "#FFB029", "PowerUp": "#FFD66B", "PowerDown": "#9EA8C7", "Paralysis": "#D1E64D"}
CREAM = (.94, .89, .78)

def text(img, xy, s, size, color, font=FONT_B, anchor="la", italic=False, outline=None, stroke=0):
    f = ImageFont.truetype(font, size)
    if not italic:
        ImageDraw.Draw(img).text(xy, s, font=f, fill=tuple(int(c * 255) for c in color[:3]) + (255,), anchor=anchor, stroke_width=stroke, stroke_fill=outline); return
    l, t, r, b = f.getbbox(s); w, h = r - l + 30, b - t + 20; tmp = Image.new("RGBA", (w, h))
    ImageDraw.Draw(tmp).text((10 - l, 10 - t), s, font=f, fill=tuple(int(c * 255) for c in color[:3]) + (255,), stroke_width=stroke, stroke_fill=outline)
    tmp = tmp.transform(tmp.size, Image.AFFINE, (1, .22, -h * .22 * .5, 0, 1, 0), Image.BICUBIC)
    x, y = xy; x = x - w + 10 if anchor[0] == "r" else x - w / 2 if anchor[0] == "m" else x - 10; img.alpha_composite(tmp, (int(x), int(y - 10)))

def rich(img, x, y, parts, size):
    """(글자, 색) 조각을 이어 붙여 쓰기. 색이 이름이면 그 상태이상 아이콘을 글자 앞에."""
    f = ImageFont.truetype(FONT_B, size); d = ImageDraw.Draw(img)
    for s, c in parts:
        if isinstance(c, str) and c in STATUS_HEX:
            ic = icon(c, int(size * 1.5)); img.alpha_composite(ic, (int(x), int(y - size * .25))); x += size * 1.35; c = hexc(STATUS_HEX[c])
        d.text((x, y), s, font=f, fill=tuple(int(v * 255) for v in c) + (255,)); x += d.textlength(s, font=f)

def panel(skill, state, scale):
    """SkillPanel(760×440 로컬 좌표) 한 장을 그려 RGBA 로 돌려줌."""
    S = 3; P = Image.new("RGBA", (800 * S, 560 * S)); OX, OY = 20, 80     # 위로 튀어나온 +코인 위력 여백
    def at(x, y): return (int((OX + x) * S), int((OY - y) * S))
    c = ATTR[skill["attr"]]; light = tuple(v + (1 - v) * .35 for v in c); deep = tuple(v * .55 for v in c)
    fa = np.clip(.86 + (noise(*np.meshgrid(np.arange(460 * S) * .02, np.arange(300 * S) * .02)) - .5) * .05, 0, 1)
    fill = tinted(fa.astype(np.float32), (.05, .04, .03, 1)); P.alpha_composite(fill, at(120, -150))
    P.alpha_composite(nine(FRAME, (*light, 1), (460 + 12) * S, (300 + 12) * S, 34 * S), at(114, -144))
    art = render_illustration(skill["motif"], skill["seed"], tuple(int(v * 255) for v in c)).convert("RGBA").resize(((416 - 16) * S, (158 - 16) * S), Image.LANCZOS)
    P.alpha_composite(art, at(120 + 22 + 8, -150 - 16 - 8)); P.alpha_composite(nine(FRAME, (*light, 1), 416 * S, 158 * S, 34 * S), at(142, -166))
    P.alpha_composite(glow((230 * S, 230 * S), c, .4), at(88 - 115, -138 + 115))
    P.alpha_composite(tinted(BANNER, (.07, .06, .05, .92), (470 * S, 74 * S)), at(128, -78)); P.alpha_composite(tinted(INK, (*light, 1), (470 * S, 74 * S)), at(128, -78))
    text(P, at(128 + 40, -78 - 37), skill["name"], 40 * S, (.96, .92, .82), anchor="lm", stroke=2 * S, outline=(0, 0, 0, 200))
    P.alpha_composite(tinted(HEXF, (.05, .045, .04, .97), (156 * S, 156 * S)), at(10, -60)); P.alpha_composite(tinted(HEXR, (*light, 1), (156 * S, 156 * S)), at(10, -60))
    text(P, at(88, -138), str(skill["base"]), 74 * S, (.97, .94, .86), anchor="mm", stroke=3 * S, outline=tuple(int(v * 255) for v in deep) + (255,))
    text(P, at(20, -2 + 60), f"+{skill['coinPower']}", 62 * S, light, italic=True, stroke=2 * S, outline=tuple(int(v * 255) for v in deep) + (255,))
    for i, (name, val, on) in enumerate(state["mods"]):
        ic = icon(name, 40 * S); ic.putalpha(ic.getchannel("A").point(lambda v: v if on else int(v * .3))); P.alpha_composite(ic, at(184 + i * 60 - 20, -12 + 20))
        col = tuple(v * .65 + w * .35 for v, w in zip((1, 1, 1), hexc(STATUS_HEX[name]))); col = col if on else tuple(v * .4 for v in col)
        text(P, at(184 + i * 60, -32 - 13), val, 24 * S, col, anchor="mm", stroke=S, outline=(0, 0, 0, 220))
    text(P, at(590, -56 - 22), skill["attrLine"], 18 * S, CREAM, font=FONT_R, anchor="rs")
    for i in range(skill["coins"]):
        look = state["results"][i][0] if i < len(state["results"]) else 0; P.alpha_composite(coin(look, 44 * S), at(166 + i * 50 - 22, -380 + 22))
    multi = sum(1 for _, c in skill["status"] if isinstance(c, str)) > 1
    rich(P, *at(148, -424 - (9 if multi else 11)), [("적중 시 ", CREAM)] + skill["status"], (18 if multi else 22) * S)
    P.alpha_composite(glow((240 * S, 150 * S), c, .5), at(500 - 120, -395 + 75))
    text(P, at(572, -340 - 26), state["label"], 24 * S, light, anchor="rs")
    total = sum(p for _, p, _ in state["results"]) if state["results"] else "?"
    text(P, at(580, -366 - 4), str(total), 84 * S, tuple(1 - (1 - v) * .55 for v in c), anchor="ra", italic=True, stroke=3 * S, outline=tuple(int(v * 255) for v in deep) + (255,))
    if state["results"]:
        n = len(state["results"]); sh = 52 + 40 * n; sf = tinted(np.full((8, 8), .86, np.float32), (.05, .04, .03, 1), (176 * S, sh * S))
        P.alpha_composite(sf, at(592, -186)); P.alpha_composite(nine(FRAME, (*light, 1), (176 + 8) * S, (sh + 8) * S, 34 * S), at(588, -182))
        text(P, at(592 + 88, -186 - 22), "개별 코인 결과", 18 * S, CREAM, anchor="mm")
        for i, (look, p, tag) in enumerate(state["results"]):
            P.alpha_composite(coin(look, 30 * S), at(592 + 36 - 15, -186 - 60 - i * 40 + 15))
            rich(P, *at(592 + 60, -186 - 60 - i * 40 - 11), [("앞면" if look == 1 else "뒷면", CREAM), (f"  {p}", CREAM)] + ([(f" {tag[0]}", hexc(STATUS_HEX[tag[1]]))] if tag else []), 20 * S)
    return P.resize((int(P.width / S * scale), int(P.height / S * scale)), Image.LANCZOS), (OX * scale, OY * scale)

def main():
    W, Hh = 1920, 1080
    bg = Image.open(os.path.join(ROOT, "previews", "map_town.png")).convert("RGB").resize((W, Hh), Image.LANCZOS).filter(ImageFilter.GaussianBlur(10))
    bg = Image.blend(bg, Image.new("RGB", (W, Hh), (0, 0, 0)), .55).convert("RGBA")
    bg.alpha_composite(Image.new("RGBA", (W, Hh), (0, 0, 0, int(.28 * 255))))
    slash = tinted(SLASH, (.72, .05, .07, .9), (640, 640)); bg.alpha_composite(slash, (0, 0)); bg.alpha_composite(slash.rotate(180), (W - 640, Hh - 640))
    scale = .62
    player = dict(name="연속 베기", attr="Red", motif="Flurry", seed=11, base=10, coinPower=3, coins=3, attrLine="빨강 속성 · 보라에 약함",
                  status=[("출혈", "Bleed"), (" 앞 2/1 · 뒤 1/1  ", CREAM), ("자신 ", CREAM), ("힘", "PowerUp"), (" 1/2", CREAM)])
    enemy = dict(name="베개 내려찍기", attr="Blue", motif="Smash", seed=4, base=10, coinPower=4, coins=2, attrLine="파랑 속성 · 초록에 약함",
                 status=[("침잠", "Sinking"), (" 앞 2/2 · 뒤 1/1", CREAM)])
    ps = dict(mods=[("PowerUp", "+1×2", True), ("PowerDown", "0", False), ("Paralysis", "0", False)], results=[(1, 14, ("+1", "PowerUp")), (1, 13, None), (2, 10, None)], label="최종 위력")
    es = dict(mods=[("PowerUp", "0", False), ("PowerDown", "-2×1", True), ("Paralysis", "1", True)], results=[(1, 10, ("마비", "Paralysis"))], label="중단")
    for sk, st, x, fade in ((player, ps, W / 2 - 560, 1.0), (enemy, es, W / 2 + 90, .45)):   # 진 쪽 패널은 흐리게
        img, (ox, oy) = panel(sk, st, scale); bottom = Hh / 2 - 70
        if fade < 1: img.putalpha(img.getchannel("A").point(lambda v: int(v * fade)))
        bg.alpha_composite(img, (int(x - ox), int(bottom - img.height + (560 - 80 - 460) * scale)))
    text(bg, (W / 2, Hh / 2 - 40), "13   VS   10", 44, (.95, .82, .5), anchor="mm", stroke=2, outline=(0, 0, 0, 230))
    # 합 승리 배너(아래쪽 1/3): 빛 번짐 + 가로 빛줄기 + 큰 글자 + 설명
    cy = Hh / 2 + 250
    bg.alpha_composite(glow((760, 220), (1, .84, .35), .55), (int(W / 2 - 380), int(cy - 110)))
    bg.alpha_composite(Image.new("RGBA", (900, 4), (255, 215, 90, 216)), (int(W / 2 - 450), int(cy + 6)))
    text(bg, (W / 2, cy - 64), "합 승리", 84, (1, .9, .6), anchor="ma", italic=True, stroke=3, outline=(90, 60, 10, 255))
    text(bg, (W / 2, cy + 62), "공격 유지 — 그대로 명중", 26, CREAM, anchor="mm", stroke=2, outline=(0, 0, 0, 230))
    out = os.path.join(ROOT, "previews", "skill_panel_v3_mock.png"); bg.convert("RGB").save(out); print("✔", out)

if __name__ == "__main__":
    main()
