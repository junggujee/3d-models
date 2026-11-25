using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static MoominAction.UI.UIFactory;

namespace MoominAction.UI
{
    /// <summary>
    /// 퀘스트 추적(왼쪽, 펫 카드 아래 — 펫이 없으면 그 자리로 올라감): ◆ 퀘스트 제목(Heading 글꼴) + 진행("땅콩빵 조각 1/2", 다 모으면 "조랭이에게 돌아가자" 금색).
    /// 진행 중인 퀘스트 최대 3개. 가방이 바뀌거나 퀘스트가 바뀌면 다시 씀.
    /// </summary>
    public class QuestTracker
    {
        RectTransform root; readonly List<(RectTransform row, Text title, Text prog, Image dot)> rows = new(); Inventory inv; bool dirty = true;
        const int Max = 5; float nextTick;   // 토벌 방향·거리 글자는 0.5초마다 새로

        public void Build(Transform core, Inventory inventory)
        {
            inv = inventory; var tl = new Vector2(0, 1);
            root = NewRect("QuestTracker", core); Set(root, tl, tl, tl, new Vector2(44, -412), new Vector2(560, 380));
            for (int i = 0; i < Max; i++)
            {
                var row = NewRect("Quest" + i, root); Set(row, tl, new Vector2(1, 1), tl, new Vector2(0, -i * 66), new Vector2(0, 64));
                var dot = Img(row, "Dot", UITheme.Slot, Color.white, true); Set(dot.rectTransform, tl, tl, new Vector2(.5f, .5f), new Vector2(14, -18), new Vector2(18, 18)); dot.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var t = StyleHeading(Txt(row, "", 26, UITheme.GoldBright, TextAnchor.MiddleLeft)); Set(t.rectTransform, tl, new Vector2(1, 1), tl, new Vector2(34, 0), new Vector2(-34, 36));
                var p = Txt(row, "", 21, UITheme.Cream, TextAnchor.MiddleLeft); Set(p.rectTransform, tl, new Vector2(1, 1), tl, new Vector2(36, -34), new Vector2(-36, 30));
                rows.Add((row, t, p, dot));
            }
            Quests.Updated += MarkDirty; if (inv != null) inv.Changed += MarkDirty;
        }
        void MarkDirty() => dirty = true;
        public void Dispose() { Quests.Updated -= MarkDirty; if (inv != null) inv.Changed -= MarkDirty; }

        public void Update(bool petCardShown)
        {
            root.anchoredPosition = Vector2.Lerp(root.anchoredPosition, new Vector2(44, petCardShown ? -412 : -312), 0.25f);
            if (Time.unscaledTime >= nextTick) { nextTick = Time.unscaledTime + 0.5f; if (Hunts.Log.Active().Count > 0) dirty = true; }
            if (!dirty) return; dirty = false;
            var list = Quests.Tracker();
            for (int i = 0; i < Max; i++)
            {
                bool on = i < list.Count; rows[i].row.gameObject.SetActive(on); if (!on) continue;
                var (title, progress, ready) = list[i];
                rows[i].title.text = title; rows[i].prog.text = progress;
                rows[i].prog.color = ready ? UITheme.GoldBright : UITheme.Cream; rows[i].dot.color = ready ? UITheme.GoldBright : Color.white;
            }
        }
    }
}
