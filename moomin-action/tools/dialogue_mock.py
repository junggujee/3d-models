"""대화창 미리보기(Unity 없이): UI/DialogueBox.cs 와 같은 배치·그림·글꼴로 1920×1080 장면을 그림.
글꼴: 제목 띠·이름 = 빛의 계승자 Bold(크림→베이지 그라데이션), 대사 = 나눔명조.
결과: previews/dialogue_mock.png(수락/거절 선택지) · previews/dialogue_mock_complete.png(퀘스트 완료 + 보상) · previews/font_roles.png(글꼴 역할표)
실행: python3 tools/dialogue_mock.py"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
R = os.path.join(ROOT, "unity", "Assets", "Resources"); DLG = os.path.join(R, "UI", "Dialogue"); FONTS = os.path.join(R, "Fonts")
PRE = os.path.join(ROOT, "previews")
W, H = 1920, 1080

def font(name, size): return ImageFont.truetype(os.path.join(FONTS, name), size)
TITLE = "HeirOfLight-Bold.ttf"; HEAD = "HeirOfLight-Regular.ttf"; BODY = "NanumMyeongjo.ttf"; BODYB = "NanumMyeongjoExtraBold.ttf"

def art(name, size=None):
    im = Image.open(os.path.join(DLG, name + ".png")).convert("RGBA")
    return im.resize(size, Image.LANCZOS) if size else im.resize((im.width // 2, im.height // 2), Image.LANCZOS)

def sliced(name, w, h, border):
    """9분할(가로만): 좌우 border 단위 유지, 가운데 늘림."""
    im = Image.open(os.path.join(DLG, name + ".png")).convert("RGBA"); im = im.resize((im.width // 2, im.height // 2), Image.LANCZOS)
    hh = im.height; im = im.resize((im.width, h), Image.LANCZOS) if hh != h else im
    b = border; out = Image.new("RGBA", (w, h)); out.paste(im.crop((0, 0, b, h)), (0, 0)); out.paste(im.crop((im.width - b, 0, im.width, h)), (w - b, 0))
    out.paste(im.crop((b, 0, im.width - b, h)).resize((w - 2 * b, h), Image.LANCZOS), (b, 0)); return out

def grad_text(img, xy, text, f, top, bottom, anchor="mm", shadow=True, glow=None):
    """크림→베이지 세로 그라데이션 글자 + 그림자(+ 빛)."""
    d = ImageDraw.Draw(img); bb = d.textbbox(xy, text, font=f, anchor=anchor)
    mask = Image.new("L", img.size, 0); ImageDraw.Draw(mask).text(xy, text, font=f, fill=255, anchor=anchor)
    if glow:
        g = Image.new("RGBA", img.size, glow[:3] + (0,)); ga = mask.filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.GaussianBlur(6)).point(lambda v: v * glow[3] // 255); g.putalpha(ga); img.alpha_composite(g)
    if shadow:
        sh = Image.new("RGBA", img.size, (20, 10, 4, 0)); sa = mask.filter(ImageFilter.GaussianBlur(2)).point(lambda v: v * 170 // 255); sh.putalpha(sa); img.alpha_composite(sh, (2, 3))
    y0, y1 = bb[1], bb[3]; col = Image.new("RGBA", img.size)
    ys = np.clip((np.arange(img.size[1]) - y0) / max(1, y1 - y0), 0, 1)[:, None, None]
    arr = (np.array(top, np.float32) * (1 - ys) + np.array(bottom, np.float32) * ys) * np.ones((1, img.size[0], 1))
    col = Image.fromarray(np.concatenate([arr.clip(0, 255), np.zeros((img.size[1], img.size[0], 1))], -1).astype(np.uint8), "RGBA"); col.putalpha(mask); img.alpha_composite(col)

def wrap(text, f, width):
    lines = []
    for para in text.split("\n"):
        cur = ""
        for word in para.split(" "):
            t = (cur + " " + word).strip()
            if f.getlength(t) <= width: cur = t
            else: lines.append(cur); cur = word
        lines.append(cur)
    return lines

def scene(title, name, face, text, choices, out, reward=None):
    bgp = os.path.join(PRE, "map_town.png")
    bg = Image.open(bgp).convert("RGB").resize((W, H)) if os.path.exists(bgp) else Image.new("RGB", (W, H), (90, 110, 130))
    img = bg.convert("RGBA"); img.alpha_composite(Image.new("RGBA", (W, H), (10, 8, 6, 70)))
    # 판(가로 1560 × 세로 400, 아래 여백 36) — 그림에 그림자 여백 20
    PX, PY, PW, PH = (W - 1560) // 2, H - 36 - 400, 1560, 400
    img.alpha_composite(art("Panel"), (PX - 20, PY - 20))
    # 초상화 틀(왼쪽, 여백 30) + 그림(틀보다 크게 → 머리가 위로 튀어나옴) + 아래·옆 테두리
    FX, FY = PX + 30, PY + PH - 30 - 340
    img.alpha_composite(art("PortraitBg"), (FX, FY))
    por = Image.open(os.path.join(R, "UI", "Portraits", f"joraeng_{face}.png")).convert("RGBA"); ph = 430; pw = int(por.width * ph / por.height)
    por = por.resize((pw, ph), Image.LANCZOS); img.alpha_composite(por, (FX + 200 - pw // 2, FY + 340 - ph))
    img.alpha_composite(art("PortraitFrame"), (FX, FY))
    # 모서리 장식
    img.alpha_composite(art("CornerGem"), (PX - 14, PY - 14)); c = art("Corner")
    img.alpha_composite(c.transpose(Image.FLIP_LEFT_RIGHT), (PX + PW - 64 + 8, PY - 8)); img.alpha_composite(c.transpose(Image.FLIP_TOP_BOTTOM), (PX - 8, PY + PH - 64 + 8))
    img.alpha_composite(c.transpose(Image.ROTATE_180), (PX + PW - 64 + 8, PY + PH - 64 + 8))
    # 글상자 + 이름표 + 화살표
    TX, TY = PX + 460, PY + PH - 30 - 300
    img.alpha_composite(art("TextBox"), (TX, TY))
    img.alpha_composite(sliced("NamePlate", 250, 56, 30), (TX + 24, TY - 28)); grad_text(img, (TX + 24 + 125, TY), name, font(TITLE, 30), (255, 244, 220), (226, 196, 146))
    ar = art("Arrow"); img.alpha_composite(ar.transpose(Image.FLIP_TOP_BOTTOM), (TX + 1070 - 48, TY + 20)); img.alpha_composite(ar, (TX + 1070 - 48, TY + 300 - 46))
    d = ImageDraw.Draw(img); fb = font(BODY, 33); y = TY + 50
    for ln in wrap(text, fb, 1070 - 44 - 90): d.text((TX + 44, y), ln, font=fb, fill=(62, 40, 24)); y += 50
    if reward: d.text((TX + 44, y + 4), reward, font=font(BODYB, 29), fill=(138, 90, 30))
    # 제목 띠(판 위 가운데) + 장식선
    bw = 640; img.alpha_composite(sliced("TitleBanner", bw, 78, 44), (W // 2 - bw // 2, PY - 39))
    grad_text(img, (W // 2, PY), title, font(TITLE, 34), (255, 246, 226), (222, 190, 140))
    # 선택지: 글상자 위 오른쪽(판 위)
    if choices:
        bx = TX + 1070
        for i, (label, primary, hover) in enumerate(reversed(choices)):
            bwid = 340; bx -= bwid; b = sliced("ButtonPrimary" if primary else ("ButtonHover" if hover else "Button"), bwid, 68, 30)
            img.alpha_composite(b, (bx, PY - 39 - 18 - 68)); grad_text(img, (bx + bwid // 2, PY - 39 - 18 - 34), label, font(HEAD, 30), (255, 246, 226) if primary else (240, 226, 200), (232, 200, 140) if primary else (206, 186, 150))
            if hover or primary:
                sp = Image.open(os.path.join(R, "UI", "Ornament", "Sparkle.png")).convert("RGBA").resize((26, 26)); img.alpha_composite(sp, (bx + 22, PY - 39 - 18 - 34 - 13))
            bx -= 18
    # 안내
    d.text((TX + 1070 - 10, PY + PH + 8), "Space · F · 클릭  다음      ← →  고르기      Esc  닫기", font=font(BODY, 20), fill=(230, 220, 200), anchor="ra")
    img.convert("RGB").save(os.path.join(PRE, out)); print("✔", out)

def font_roles():
    img = Image.new("RGBA", (1600, 900), (18, 16, 22, 255)); d = ImageDraw.Draw(img)
    d.text((60, 40), "글꼴 역할 — 민미의 모험", font=font(BODYB, 30), fill=(200, 190, 170))
    rows = [("제목 · 장(칸토) · 보스 이름", "빛의 계승자 Bold + 크림→베이지 + 장식선", lambda y: (grad_text(img, (800, y), "고래고래, 심해의 포효", font(TITLE, 64), (255, 247, 230), (214, 178, 124), glow=(255, 220, 160, 60)), )),
            ("스킬 이름", "빛의 계승자 Regular + 은은한 빛", lambda y: grad_text(img, (800, y), "별빛 난무", font(HEAD, 54), (255, 255, 248), (236, 226, 200), glow=(255, 214, 120, 150))),
            ("이름 · 퀘스트 제목 · 메뉴 머리글", "빛의 계승자 Regular", lambda y: grad_text(img, (800, y), "땅콩빵이 먹고 싶은 조랭이", font(HEAD, 44), (240, 220, 170), (204, 166, 100))),
            ("대사 · 설명 · 숫자(가독성)", "나눔명조", lambda y: ImageDraw.Draw(img).text((800, y), "혹시… 땅콩빵 조각 두 개만 구해다 주실 수 있나요?", font=font(BODY, 34), fill=(236, 228, 210), anchor="mm"))]
    y = 170
    for title, sub, draw in rows:
        d.text((60, y - 22), title, font=font(BODYB, 24), fill=(214, 186, 132)); d.text((60, y + 14), sub, font=font(BODY, 18), fill=(150, 140, 125)); draw(y); y += 170
    div = Image.open(os.path.join(R, "UI", "Ornament", "Divider.png")).convert("RGBA").resize((600, 24)); img.alpha_composite(div, (500, 214))
    img.convert("RGB").save(os.path.join(PRE, "font_roles.png")); print("✔ font_roles.png")

scene("땅콩빵이 먹고 싶은 조랭이", "조랭이", "happy", "혹시… 땅콩빵 조각 두 개만 구해다 주실 수 있나요? 보답은 꼭 할게요!", [("수락", True, False), ("거절", False, False)], "dialogue_mock.png")
scene("땅콩빵이 먹고 싶은 조랭이", "조랭이", "happy", "이건 제 비상 간식 주머니에서 꺼낸 거예요. 받아 주세요!", None, "dialogue_mock_complete.png", reward="보상 — 경험치 120 · 금화 50 · 빨간 물약 ×2")
font_roles()
