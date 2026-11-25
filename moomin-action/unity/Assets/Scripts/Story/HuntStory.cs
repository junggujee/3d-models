namespace MoominAction
{
    /// <summary>
    /// 토벌 퀘스트 대화(반복 가능) — 대사는 HuntCatalog 의 각 정의("표정|동작|글").
    ///  · 처음: 이야기 → 수락/거절 → 위치 안내
    ///  · 다시(완료한 적 있음): 새 소동 한 줄(완료 횟수에 따라 돌아가며) → 또 부탁 → 수락/다음에
    ///  · 진행 중: 지금 몇 마리 + 힌트 → 계속/포기
    ///  · 다 잡음: 놀람 → "다 혼내 줬어!" → 고마움 + 보상
    /// </summary>
    public static class HuntStory
    {
        public static Dialogue Build(Npc npc, HuntDef h)
        {
            var d = new Dialogue { title = h.title, speaker = npc.npcName, portrait = npc.portrait, npc = npc };
            var log = Hunts.Log; int clears = log.Clears(h.id), n = log.Count(h.id);
            string reward = Hunts.RewardLine(h);   // 완료하기 전에(첫 보상 배율)

            if (!log.IsActive(h.id))
            {
                DialogueLine ask;
                if (clears == 0) { ask = null; foreach (var line in h.intro) ask = Say(d, h, line, n); }
                else
                {
                    if (h.repeats.Length > 0) Say(d, h, h.repeats[(clears - 1) % h.repeats.Length], n);
                    ask = d.Say("normal", "Talk", $"{h.monsterName} {h.need}마리만 다시 혼내 주실래요? 이번이 {clears + 1}번째 부탁이네요. 헤헤.");
                }
                if (ask == null) ask = d.Say("normal", "Talk", "도와주실래요?");
                ask.Choose(new DialogueChoice { text = clears == 0 ? "수락" : "또 도와줄게", next = "accept", primary = true, act = () => Hunts.Accept(h) },
                           new DialogueChoice { text = clears == 0 ? "거절" : "다음에", next = "decline" });
                DialogueLine last = null;
                if (clears == 0) foreach (var line in h.accept) { var l = Say(d, h, line, n); if (last == null) l.Label("accept"); last = l; }
                else last = d.Say("happy", "QuestAccept", $"역시 여행자님! {h.zoneName}에서 기다릴게요… 아니, 여기서 응원할게요!").Label("accept");
                if (last == null) last = d.Say("happy", "QuestAccept", "고마워요!").Label("accept");
                last.text += $"\n<color=#8A5A1E>토벌 — {h.monsterName} {h.need}마리 · {h.zoneName} · 보상 {reward}</color>"; last.End();
                Say(d, h, clears == 0 ? h.decline : "troubled|No|그럼 마음이 바뀌면 또 와 주세요. 기다릴게요!", n).Label("decline").End();
                return d;
            }

            if (!Hunts.Ready(h))
            {
                Say(d, h, h.waiting, n);
                Say(d, h, h.hint, n).Choose(new DialogueChoice { text = "계속 할게", next = "go", primary = true },
                                            new DialogueChoice { text = "포기하기", next = "giveup", act = () => Hunts.Abandon(h) });
                d.Say("happy", "Yes", "힘내세요, 여행자님! 조심해서 다녀오세요!").Label("go").End();
                d.Say("sad", "Sad", "알겠어요… 그래도 고마웠어요. 나중에 또 부탁해도 될까요?").Label("giveup").End();
                return d;
            }

            Say(d, h, h.ready, n).Choose(new DialogueChoice { text = $"다 혼내 줬어!  ({h.monsterName} {h.need}/{h.need})", next = "done", primary = true, act = () => Hunts.TurnIn(h), failNext = "oops" },
                                         new DialogueChoice { text = "나중에", next = "later" });
            Say(d, h, h.thanks, n).Label("done");
            var r = Say(d, h, h.reward, n); r.text += $"\n<color=#8A5A1E>보상 — {reward}</color>"; r.End();
            d.Say("normal", "Wave", "그럼 보상은 나중에 드릴게요. 꼭 다시 와 주세요!").Label("later").End();
            d.Say("surprised", "Surprised", "어라? 아직 덜 끝난 것 같아요…").Label("oops").End();
            return d;
        }

        static DialogueLine Say(Dialogue d, HuntDef h, string line, int n)
        {
            var (face, anim, text) = HuntMath.Split(line);
            return d.Say(face, anim, HuntMath.Fill(text, h, n));
        }
    }
}
