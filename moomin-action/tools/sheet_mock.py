"""캐릭터 시트 · 스킬 상세 · 도감 · 기절 효과 미리보기(Unity 없이) — UI/CharacterSheet.cs · CodexPage.cs 와 같은 배치·그림·글꼴.
결과: previews/sheet_mock.png · sheet_skill_popup_mock.png · codex_mock.png · stun_fx_mock.png
실행: python3 tools/sheet_mock.py"""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import illustration_preview as ill

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__))); R = os.path.join(ROOT, "unity", "Assets", "Resources"); PRE = os.path.join(ROOT, "previews")
F = lambda n, s: ImageFont.truetype(os.path.join(R, "Fonts", n), s)
HEAD, DISP, BODY = "HeirOfLight-Regular.ttf", "HeirOfLight-Bold.ttf", "NanumMyeongjo.ttf"
INK, SOFT, GOLD, GOOD, PAPER = (51, 43, 38), (92, 82, 72), (184, 128, 31), (56, 128, 51), (252, 247, 235)
W, H = 1920, 1080
SKILLS = [("베기", "좌클릭 1타", "Slash", (219, 41, 51), 8, 3), ("되베기", "좌클릭 2타", "Slash", (219, 41, 51), 8, 4), ("이어 베기", "좌클릭 3타", "Slash", (219, 41, 51), 9, 3),
          ("연속 베기", "Q", "Flurry", (219, 41, 51), 10, 3), ("불꽃 찌르기", "E", "Thrust", (242, 140, 38), 14, 6), ("별빛 난무", "R", "Flurry", (242, 199, 38), 14, 5)]
kind = lambda k: "필살기" if k == "R" else "스킬" if k in ("Q", "E") else "일반 공격"

def art(folder, name, size=None):
    im = Image.open(os.path.join(R, "UI", folder, name + ".png")).convert("RGBA")
    return im.resize(size, Image.LANCZOS) if size else im
def sliced(folder, name, w, h, b):
    im = Image.open(os.path.join(R, "UI", folder, name + ".png")).convert("RGBA"); im = im.resize((im.width // 2, im.height // 2), Image.LANCZOS)
    out = Image.new("RGBA", (w, h)); iw, ih = im.size
    for (sx0, sx1, dx0, dx1) in ((0, b, 0, b), (b, iw - b, b, w - b), (iw - b, iw, w - b, w)):
        for (sy0, sy1, dy0, dy1) in ((0, b, 0, b), (b, ih - b, b, h - b), (ih - b, ih, h - b, h)):
            if dx1 > dx0 and dy1 > dy0: out.paste(im.crop((sx0, sy0, sx1, sy1)).resize((dx1 - dx0, dy1 - dy0)), (dx0, dy0))
    return out
def text(img, xy, s, font, fill, anchor="la", glow=None, grad=None):
    d = ImageDraw.Draw(img)
    if glow or grad:
        m = Image.new("L", img.size, 0); ImageDraw.Draw(m).text(xy, s, font=font, fill=255, anchor=anchor)
        if glow:
            g = Image.new("RGBA", img.size, glow[:3] + (0,)); g.putalpha(m.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(4)).point(lambda v: v * glow[3] // 255)); img.alpha_composite(g)
        if grad:
            bb = d.textbbox(xy, s, font=font, anchor=anchor); ys = np.clip((np.arange(img.size[1]) - bb[1]) / max(1, bb[3] - bb[1]), 0, 1)[:, None, None]
            arr = (np.array(grad[0], np.float32) * (1 - ys) + np.array(grad[1], np.float32) * ys) * np.ones((1, img.size[0], 1))
            c = Image.fromarray(np.concatenate([arr, np.zeros((img.size[1], img.size[0], 1))], -1).astype(np.uint8), "RGBA"); c.putalpha(m); img.alpha_composite(c); return
    d.text(xy, s, font=font, fill=fill, anchor=anchor)
def backdrop():
    p = os.path.join(PRE, "map_town.png"); bg = Image.open(p).convert("RGBA").resize((W, H)) if os.path.exists(p) else Image.new("RGBA", (W, H), (90, 110, 130, 255))
    bg.alpha_composite(Image.new("RGBA", (W, H), (13, 13, 15, 158))); return bg
def skill_art(name, motif, acc, size):
    return ill.render(motif, sum(map(ord, name)) % 97, acc).convert("RGBA").resize(size, Image.LANCZOS)

def sheet():
    img = backdrop(); SX, SY = (W - 1840) // 2, (H - 1020) // 2; S = art("Sheet", "Sheet"); img.alpha_composite(S, (SX, SY))
    P = lambda x, y: (SX + int(x), SY + int(y))
    # A
    img.alpha_composite(art("Sheet", "Arch", (320, 440)), P(166, 64))
    mp = os.path.join(PRE, "headband_worn.png")
    if os.path.exists(mp):
        m = Image.open(mp).convert("RGBA"); m = m.crop((m.width * .2, m.height * .05, m.width * .8, m.height * .95)).resize((300, 410)); mask = art("Sheet", "Arch", (320, 440)).getchannel("A").crop((10, 20, 310, 430))
        m.putalpha(Image.fromarray(np.minimum(np.asarray(mask), 235).astype(np.uint8))); img.alpha_composite(m, P(176, 84))
    text(img, P(326, 482), "끌어서 돌려 보기", F(BODY, 17), SOFT, "mm")
    slots = [("머리", 60, 76, "headband_blue"), ("가슴", 60, 184, None), ("다리", 60, 292, None), ("발", 60, 400, "slippers_fuzzy"), ("무기", 528, 76, "sword"), ("아티팩트", 528, 184, None), ("아티팩트", 528, 292, None), ("아티팩트", 528, 400, None)]
    for i, (lab, x, y, icon) in enumerate(slots):
        img.alpha_composite(sliced("Sheet", "SlotSel" if i == 0 else "Slot", 88, 88, 14), P(x, y))
        ip = os.path.join(R, "ItemIcons", (icon or "") + ".png")
        if icon and os.path.exists(ip): img.alpha_composite(Image.open(ip).convert("RGBA").resize((62, 62)), P(x + 13, y + 9))
        elif icon == "sword": ImageDraw.Draw(img).line([P(x + 24, y + 62), P(x + 64, y + 18)], fill=(120, 150, 200), width=7)
        else: text(img, P(x + 44, y + 40), "◇" if lab == "아티팩트" else "+", F(BODY, 30), (150, 140, 128), "mm")
        text(img, P(x + 44, y + 76), lab, F(BODY, 14), SOFT, "mm")
    text(img, P(326, 533), "민미", F(DISP, 46), INK, "mm", grad=((64, 51, 43), (140, 107, 71)))
    text(img, P(326, 582), "Lv.3  ·  스파크 검사  ·  전투력 1,284", F(BODY, 21), INK, "mm")
    text(img, P(568, 517), "묶음", F(BODY, 15), SOFT, "mm")
    for i in range(3): img.alpha_composite(sliced("Sheet", "SlotSel" if i == 0 else "Button", 30, 30, 12), P(520 + i * 33, 528)); text(img, P(535 + i * 33, 543), str(i + 1), F(BODY, 18), GOLD if i == 0 else INK, "mm")
    # B
    def head(t, x, y): text(img, P(x + 22, y + 31), t, F(HEAD, 30), PAPER, "lm")
    head("능력치", 640, 40)
    bars = [("경험치", "260 / 580", .45, (219, 168, 66)), ("체력", "112 / 120", .93, (204, 69, 61)), ("스태미나", "100 / 100", 1, (107, 168, 82)), ("정신력", "12 (-45~45)", .63, (87, 128, 209))]
    for i, (n, v, r, c) in enumerate(bars):
        y = 110 + i * 58; text(img, P(664, y + 13), n, F(BODY, 22), INK, "lm"); text(img, P(1160, y + 13), v, F(BODY, 20), SOFT, "rm")
        img.alpha_composite(sliced("Sheet", "BarFrame", 500, 20, 12), P(660, y + 28)); ImageDraw.Draw(img).rectangle([P(664, y + 32), P(664 + 492 * r, y + 44)], fill=c)
    text(img, P(664, 365), "공격", F(BODY, 24), INK, "lm"); text(img, P(924, 365), "방어", F(BODY, 24), INK, "lm")
    a = ["공격력", "코인 최종 위력", "무기 배율", "레벨 보너스", "장비 공격력", "이동 속도"]; av = ["11", "+1", "×1.00", "+1", "+0", "110%"]
    dd = ["최대 체력", "받는 피해", "가드 감소", "회피 코인", "경직 한계", "몸 속성"]; dv = ["120", "-8%", "65%", "+1", "100", "검정"]
    for i in range(6):
        y = 400 + i * 34; text(img, P(664, y), a[i], F(BODY, 19), SOFT, "lm"); text(img, P(904, y), av[i], F(BODY, 19), INK, "rm")
        text(img, P(924, y), dd[i], F(BODY, 19), SOFT, "lm"); text(img, P(1164, y), dv[i], F(BODY, 19), INK, "rm")
    # D
    head("자세히", 1204, 40); img.alpha_composite(sliced("Sheet", "Button", 146, 40, 12), P(1640, 56)); text(img, P(1713, 76), "도감  (K)", F(BODY, 20), INK, "mm")
    img.alpha_composite(art("Sheet", "Circle", (250, 250)), P(1226, 110)); img.alpha_composite(Image.open(os.path.join(R, "ItemIcons", "headband_blue.png")).convert("RGBA").resize((160, 160)), P(1271, 155))
    text(img, P(1490, 135), "파란 머리띠", F(HEAD, 34), INK, "lm"); text(img, P(1490, 175), "장신구 · 머리", F(BODY, 20), SOFT, "lm")
    text(img, P(1490, 206), "★★★", F(BODY, 24), GOLD, "lm"); text(img, P(1563, 206), "☆☆", F(BODY, 24), (201, 187, 160), "lm")
    text(img, P(1490, 249), "대표 효과", F(BODY, 19), SOFT, "lm"); text(img, P(1490, 293), "+1", F(DISP, 54), INK, "lm")
    text(img, P(1230, 386), "코인 최종 위력 +1", F(BODY, 20), GOOD, "lm")
    text(img, P(1230, 470), "“민미가 아끼는 파란 머리띠. 매면 마음이 또렷해진다.”", F(BODY, 20), SOFT, "la")
    img.alpha_composite(sliced("Sheet", "Button", 156, 44, 12), P(1630, 562)); text(img, P(1708, 584), "해제", F(BODY, 21), INK, "mm")
    text(img, P(1230, 572), "Enter 장착·사용   X 해제·버리기", F(BODY, 15), SOFT, "lm"); text(img, P(1230, 594), "1~4 퀵슬롯   R 정렬   [ ] 분류", F(BODY, 15), SOFT, "lm")
    # E
    head("가방", 36, 644); text(img, P(900, 672), "금화  1,240", F(BODY, 22), GOLD, "rm")
    for i, t in enumerate(["TabAll", "TabPotion", "TabGear", "TabMat", "TabKey"]): img.alpha_composite(art("Sheet", t, (48, 48)), P(56, 712 + i * 54))
    items = ["headband_blue", "potion_red", "potion_red", "peanut_piece", "robe_blanket", "shield_tissue", "cap_sleep", "pants_pajama", "slippers_fuzzy"]
    for i in range(30):
        x, y = 124 + (i % 10) * 78, 712 + (i // 10) * 78; img.alpha_composite(sliced("Sheet", "SlotSel" if i == 0 else "Slot", 70, 70, 14), P(x, y))
        if i < len(items):
            ip = os.path.join(R, "ItemIcons", items[i] + ".png")
            if os.path.exists(ip): img.alpha_composite(Image.open(ip).convert("RGBA").resize((52, 52)), P(x + 9, y + 9))
            else: ImageDraw.Draw(img).ellipse([P(x + 20, y + 18), P(x + 50, y + 52)], fill=(200, 50, 60))
            if i == 0: text(img, P(x + 6, y + 12), "장착", F(BODY, 13), GOLD, "lm")
            if i == 1: text(img, P(x + 64, y + 57), "3", F(BODY, 16), INK, "rm"); text(img, P(x + 6, y + 12), "Q1", F(BODY, 13), GOLD, "lm")
    text(img, P(124, 964), "머리 칸 — 낄 수 있는 장비 2개", F(BODY, 18), GOLD, "lm")
    # F
    head("스킬", 940, 644); text(img, P(1786, 672), "휠 · 끌기로 넘기고, 누르면 자세히", F(BODY, 16), SOFT, "rm")
    view = Image.new("RGBA", (824, 270)); scroll = 70
    for i, (n, key, motif, acc, b, c) in enumerate(SKILLS):
        cx = i * 210 - scroll
        if cx > 824 or cx + 196 < 0: continue
        card = sliced("Sheet", "Card", 196, 262, 16); d = ImageDraw.Draw(card)
        d.polygon([(178, 6), (185, 13), (178, 20), (171, 13)], fill=acc); d.text((10, 18), key, font=F(BODY, 15), fill=SOFT, anchor="lm")
        d.rectangle([8, 32, 188, 128], fill=(20, 18, 15)); card.alpha_composite(skill_art(n, motif, acc, (176, 92)), (10, 34))
        d.rounded_rectangle([4, 134, 192, 172], 10, fill=(31, 26, 23)); text(card, (98, 153), n, F(HEAD, 23), (255, 255, 255), "mm", glow=acc + (200,), grad=((255, 255, 255), (237, 227, 204)))
        d.text((98, 186), kind(key), font=F(BODY, 14), fill=SOFT, anchor="mm")
        d.rounded_rectangle([10, 198, 186, 254], 8, fill=(18, 15, 12))
        text(card, (178, 226), str(b + c), F(DISP, 44), (255, 255, 255), "rm", glow=acc + (180,), grad=((255, 248, 230), (219, 189, 140)))
        d.text((20, 214), "최종 위력", font=F(BODY, 14), fill=(217, 204, 178), anchor="lm"); d.text((20, 240), f"{b}~", font=F(BODY, 14), fill=(217, 204, 178), anchor="lm")
        view.alpha_composite(card, (max(0, cx), 4), (max(0, -cx), 0))
    img.alpha_composite(view, P(960, 704))
    ar = art("Sheet", "Arrow", (20, 30)); img.alpha_composite(ar, P(1786, 812)); img.alpha_composite(ar.transpose(Image.FLIP_LEFT_RIGHT), P(938, 812))
    img.convert("RGB").save(os.path.join(PRE, "sheet_mock.png")); print("✔ sheet_mock.png")
    # 스킬 상세 창
    pop = img.copy(); pop.alpha_composite(Image.new("RGBA", (W, H), (13, 13, 15, 140))); PX, PY = (W - 1000) // 2, (H - 620) // 2
    pop.alpha_composite(art("Sheet", "Popup"), (PX, PY)); n, key, motif, acc, b, c = SKILLS[4]; d = ImageDraw.Draw(pop)
    d.rectangle([PX + 52, PY + 52, PX + 600, PY + 340], fill=(20, 18, 15)); pop.alpha_composite(skill_art(n, motif, acc, (542, 282)), (PX + 55, PY + 55))
    d.rounded_rectangle([PX + 630, PY + 52, PX + 960, PY + 110], 12, fill=(31, 26, 23)); text(pop, (PX + 795, PY + 81), n, F(HEAD, 32), (255, 255, 255), "mm", glow=acc + (200,), grad=((255, 255, 255), (237, 227, 204)))
    d.text((PX + 636, PY + 132), f"{kind(key)}  ·  {key}", font=F(BODY, 20), fill=SOFT, anchor="lm"); d.text((PX + 636, PY + 162), "주황 속성 · 코인 2개", font=F(BODY, 20), fill=INK, anchor="lm")
    d.rounded_rectangle([PX + 630, PY + 192, PX + 960, PY + 340], 10, fill=(18, 15, 12)); d.text((PX + 646, PY + 216), "최종 위력", font=F(BODY, 20), fill=(230, 217, 189), anchor="lm")
    text(pop, (PX + 944, PY + 268), str(b + c), F(DISP, 74), (255, 255, 255), "rm", glow=acc + (180,), grad=((255, 248, 230), (219, 189, 140)))
    d.text((PX + 646, PY + 322), f"뒷면 {b}  ·  앞면 {b + c}", font=F(BODY, 18), fill=(230, 217, 189), anchor="lm")
    body = ["기본 위력 14  ·  코인 위력 +6 × 2개  →  공격마다 최종 위력 14~20", "피해 배율 ×1.2  ·  재사용 8초  ·  정신력 8", "사거리 2.8m  ·  합 가능", "앞면이면 경직 피해(모든 앞면)", "불꽃을 두른 검으로 앞으로 뛰어들며 두 번 찌른다."]
    for i, l in enumerate(body): d.text((PX + 56, PY + 380 + i * 34), l, font=F(BODY, 21), fill=INK if i < 4 else SOFT)
    d.text((PX + 960, PY + 594), "클릭 · Esc  닫기", font=F(BODY, 16), fill=SOFT, anchor="rm")
    pop.convert("RGB").save(os.path.join(PRE, "sheet_skill_popup_mock.png")); print("✔ sheet_skill_popup_mock.png")

def codex():
    img = backdrop(); CX, CY = (W - 1700) // 2, (H - 940) // 2; img.alpha_composite(art("Codex", "Codex"), (CX, CY)); P = lambda x, y: (CX + int(x), CY + int(y)); d = ImageDraw.Draw(img)
    text(img, P(70, 82), "도감", F(HEAD, 56), INK, "lm"); text(img, P(214, 90), "Codex · 가진 장비와 스킬", F(BODY, 20), SOFT, "lm")
    for x, k in ((560, "["), (900, "]")): img.alpha_composite(sliced("Sheet", "Button", 40, 44, 12), P(x, 62)); text(img, P(x + 20, 84), k, F(BODY, 26), SOFT, "mm")
    text(img, P(675, 84), "장비", F(HEAD, 32), (51, 43, 38, 110), "mm"); text(img, P(815, 84), "스킬", F(HEAD, 32), INK, "mm"); d.rectangle([P(770, 111), P(860, 114)], fill=INK)
    text(img, P(1650, 90), "모은 장비 7  ·  익힌 스킬 6", F(BODY, 20), SOFT, "rm")
    for i, c in enumerate(["전체", "일반 공격", "스킬", "필살기"]): img.alpha_composite(sliced("Codex", "Chip", 96, 38, 14).copy() if i else Image.alpha_composite(sliced("Codex", "Chip", 96, 38, 14), Image.new("RGBA", (96, 38), (255, 220, 140, 60))), P(60 + i * 102, 166)); text(img, P(108 + i * 102, 185), c, F(BODY, 17), INK, "mm")
    for i, (n, key, motif, acc, b, c) in enumerate(SKILLS):
        x, y = 64 + (i % 4) * 172, 224 + (i // 4) * 222; card = sliced("Codex", "Card", 156, 208, 16)
        gl = art("Codex", "CardGlow", (150, 202)); arr = np.asarray(gl).astype(np.float32); arr[..., :3] = np.array(acc); arr[..., 3] *= .8; card.alpha_composite(Image.fromarray(arr.astype(np.uint8)), (3, 3))
        cd = ImageDraw.Draw(card); cd.rectangle([8, 44, 148, 118], fill=(20, 18, 15)); card.alpha_composite(skill_art(n, motif, acc, (136, 70)), (10, 46))
        cd.text((10, 16), key, font=F(BODY, 13), fill=GOLD, anchor="lm"); cd.text((78, 173), n, font=F(BODY, 17), fill=INK, anchor="mm")
        if i == 5: card = card.resize((162, 216)); x -= 3; y -= 4
        img.alpha_composite(card, P(x, y))
    img.alpha_composite(art("Codex", "Circle", (520, 520)), P(850, 196)); n, key, motif, acc, b, c = SKILLS[5]
    d.rectangle([P(870, 340), P(1350, 590)], fill=(20, 18, 15)); img.alpha_composite(skill_art(n, motif, acc, (474, 244)), P(873, 343))
    text(img, P(1390, 220), n, F(HEAD, 38), INK, "lm"); text(img, P(1390, 306), f"{kind(key)}  ·  {key}", F(BODY, 20), SOFT, "lm")
    text(img, P(1390, 347), "최종 위력", F(BODY, 19), SOFT, "lm"); text(img, P(1390, 400), f"{b}~{b + c}", F(DISP, 64), INK, "lm")
    text(img, P(1390, 463), "◆", F(BODY, 26), acc, "lm"); text(img, P(1418, 463), "노랑 속성 · 코인 5개", F(BODY, 20), INK, "lm")
    for i, l in enumerate(["기본 위력 14", "코인 위력 +5 × 5개", "→ 공격마다 최종 위력 14~19", "피해 배율 ×1.3", "재사용 20초", "광역 원 3.2m"]): text(img, P(1390, 500 + i * 30), l, F(BODY, 18), INK, "lm")
    text(img, P(870, 760), "“별빛을 두른 검으로 주위를 다섯 번 휘몰아친다. 앞면이 나올 때마다 정신력이 차오른다.”", F(BODY, 21), SOFT, "la")
    text(img, P(850, 889), "Esc · K  뒤로      [ ]  장비 ↔ 스킬      ←→↑↓  고르기      휠  넘기기", F(BODY, 18), SOFT, "mm")
    img.convert("RGB").save(os.path.join(PRE, "codex_mock.png")); print("✔ codex_mock.png")

def stun():
    p = os.path.join(PRE, "pinkcube_front.png"); img = Image.open(p).convert("RGBA").resize((720, 720)) if os.path.exists(p) else Image.new("RGBA", (720, 720), (120, 140, 160, 255))
    b = art("Fx", "StunBurst", (420, 420)); t = art("Fx", "StunText", (300, 127)); img.alpha_composite(b, (150, 40)); img.alpha_composite(t.rotate(4, expand=False), (210, 187))
    img.convert("RGB").save(os.path.join(PRE, "stun_fx_mock.png")); print("✔ stun_fx_mock.png")

if __name__ == "__main__": sheet(); codex(); stun()
