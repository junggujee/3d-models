using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 펫 HUD: 왼쪽(목표 아래) 펫 카드 — 하프물범 그림 · 이름 Lv · 따라오는 중/기다리는 중/기절 n초 · 체력 막대 · 친밀도 하트 5개 · 경험치 실선.
    /// 화면 아래 가운데 상호작용 안내(PetSeal.Prompt — "F 땅콩빵 조각 주기 (1/3)" 등). 펫 소식은 HUD 알림으로(PetSeal.Notice).
    /// </summary>
    public class PetHUD
    {
        RectTransform card; Text title, mode, hearts; Gauge hp; Image expLine; Text prompt; CanvasGroup promptG; float promptA;

        public void Build(Transform core, Transform overlay)
        {
            var tl = new Vector2(0, 1);
            card = Img(core, "PetCard", UITheme.Slot, new Color(1, 1, 1, .92f), true).rectTransform; Set(card, tl, tl, tl, new Vector2(44, -300), new Vector2(380, 96));
            var face = Img(card, "Face", null, Color.white, false); Set(face.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, 0), new Vector2(76, 76));
            var tex = Resources.Load<Texture2D>("ItemIcons/pet_seal"); if (tex != null) face.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f));
            title = Txt(card, "", 24, UITheme.GoldBright, TextAnchor.MiddleLeft, FontStyle.Bold); Set(title.rectTransform, tl, tl, tl, new Vector2(96, -6), new Vector2(270, 30));
            mode = Txt(card, "", 18, UITheme.Muted, TextAnchor.MiddleRight); Set(mode.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -8), new Vector2(200, 26));
            hp = MakeGauge(card, "PetHP", new Color(.55f, .85f, 1f), new Vector2(270, 14), true, false); Set(hp.root, tl, tl, tl, new Vector2(96, -42), new Vector2(270, 14));
            hearts = Txt(card, "", 22, new Color(1f, .5f, .68f), TextAnchor.MiddleLeft); Set(hearts.rectTransform, tl, tl, tl, new Vector2(96, -60), new Vector2(170, 30));
            expLine = Box(card, "Exp", new Color(1f, .86f, .5f, .8f)); Set(expLine.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(96, 8), new Vector2(0, 3));
            card.gameObject.SetActive(false);
            prompt = Txt(overlay, "", 28, UITheme.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, "PetPrompt"); At(prompt.rectTransform, new Vector2(.5f, .24f), Vector2.zero, new Vector2(1100, 44));
            prompt.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .85f); promptG = prompt.gameObject.AddComponent<CanvasGroup>(); promptG.alpha = 0;
        }

        public void Update(float dt)
        {
            var p = PetSeal.Current; card.gameObject.SetActive(p != null);
            if (p != null)
            {
                title.text = $"{p.petName}   <size=20><color=#CFC6B0>Lv.{p.level}</color></size>";
                mode.text = p.Fainted ? $"<color=#E06060>기절 {p.FaintLeft:0}초</color>" : p.command == PetCommand.Stay ? "기다리는 중 (G)" : "따라오는 중 (G)";
                hp.SetFill(p.Stats.maxHealth > 0 ? p.Stats.currentHealth / p.Stats.maxHealth : 0);
                int full = Mathf.FloorToInt(p.affection / 20f + 0.001f); hearts.text = new string('♥', full) + "<color=#6A5560>" + new string('♥', 5 - full) + "</color>";
                expLine.rectTransform.sizeDelta = new Vector2(270f * Mathf.Clamp01((float)p.exp / p.ExpToNext), 3);
            }
            string m = PetSeal.Prompt; promptA = Mathf.MoveTowards(promptA, string.IsNullOrEmpty(m) ? 0 : 1, dt * 6f); promptG.alpha = promptA; if (!string.IsNullOrEmpty(m)) prompt.text = m;
        }
    }
}
