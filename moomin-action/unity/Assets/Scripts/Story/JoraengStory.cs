namespace MoominAction
{
    /// <summary>
    /// 이야기 대본 — 조랭하프족 마을.
    /// 「땅콩빵이 먹고 싶은 조랭이」: 배고픈 조랭이가 땅콩빵 조각 두 개를 부탁 → 수락/거절 → 모아 오면 건네주기 → 고마워하며 경험치·금화·물약.
    /// 표정(초상화): normal · happy · sad · surprised · troubled   동작(NPC): Talk · Wave · Yes · No · Laugh · Sad · Surprised · QuestAccept · QuestComplete
    /// </summary>
    public static class JoraengStory
    {
        public static Dialogue Quest(Npc npc)
        {
            var q = Quests.Get(npc.questId); if (q == null) return Chatter(npc, 0);
            var d = new Dialogue { title = q.title, speaker = npc.npcName, portrait = npc.portrait, npc = npc };
            switch (Quests.Log.State(q.id))
            {
                case QuestState.NotStarted:
                    d.Say("normal", "Wave", "어… 저기, 거기 털복숭이 여행자님! 잠깐만요!");
                    d.Say("sad", "Sad", "배가 너무 고파서… 꼬리지느러미에 힘이 하나도 안 들어가요.");
                    d.Say("normal", "Talk", "저는 조랭하프족 조랭이예요. 우리 조랭하프족은 고소한 땅콩빵을 세상에서 제일 좋아하거든요.");
                    d.Say("troubled", "No", "그런데 땅콩빵은 높은 산 쪽에서만 자라서… 이 짧은 지느러미로는 도저히 못 올라가요.");
                    d.Say("happy", "Talk", "혹시… 땅콩빵 조각 두 개만 구해다 주실 수 있나요? 보답은 꼭 할게요!")
                     .Choose(new DialogueChoice { text = "수락", next = "accept", primary = true, act = () => Quests.Accept(q) },
                             new DialogueChoice { text = "거절", next = "decline" });
                    d.Say("happy", "QuestAccept", "정말요?! 고마워요, 여행자님! 땅콩빵은 때리면 퐁 하고 조각이 떨어져요.").Label("accept");
                    d.Say("normal", "Yes", $"산 쪽으로 가 보세요. 저는 여기서 꼬르륵… 아니, 얌전히 기다릴게요!\n<color=#8A5A1E>퀘스트 — {q.title}: {q.itemName} {q.itemCount}개</color>").End();
                    d.Say("sad", "Sad", "아… 그렇죠. 여행자님도 바쁘시겠죠…").Label("decline");
                    d.Say("troubled", "No", "괜찮아요! 마음이 바뀌면 다시 와 주세요. 저는 계속 여기 있을 거예요…").End();
                    break;

                case QuestState.Active:
                    if (!Quests.CanTurnIn(q))
                    {
                        d.Say("sad", "Sad", $"꼬르륵… 땅콩빵 조각은 아직인가요? (지금 {Quests.Have(q)}/{q.itemCount}개)");
                        d.Say("normal", "Yes", "산 쪽의 커다란 땅콩빵을 때리면 조각이 떨어져요. 조심해서 다녀오세요!").End();
                        break;
                    }
                    d.Say("surprised", "Surprised", "어?! 이 고소한 냄새… 설마, 땅콩빵 조각?!")
                     .Choose(new DialogueChoice { text = $"건네주기  ({q.itemName} ×{q.itemCount})", next = "give", primary = true, act = () => Quests.TurnIn(q), failNext = "missing" },
                             new DialogueChoice { text = "나중에", next = "later" });
                    d.Say("happy", "Laugh", "우와아! 진짜 땅콩빵이다! 냠… 냠냠… 으음~ 고소해!").Label("give");
                    d.Say("happy", "QuestComplete", "몸이 따뜻해지고 지느러미에 힘이 솟아요! 정말정말 고마워요, 여행자님!");
                    d.Say("normal", "Yes", $"이건 제 비상 간식 주머니에서 꺼낸 거예요. 받아 주세요!\n<color=#8A5A1E>보상 — {q.RewardLine}</color>").End();
                    d.Say("troubled", "No", "으앙… 냄새만 맡고 기다릴게요… 꼭 다시 와 주세요!").Label("later").End();
                    d.Say("surprised", "Surprised", "어라? 주머니가 비었네요…? 땅콩빵 조각을 다시 확인해 주세요!").Label("missing").End();
                    break;

                default:
                    d.Say("happy", "Laugh", "여행자님 덕분에 오늘도 배부르게 굴러다녀요!");
                    d.Say("normal", "Wave", "또 땅콩빵 냄새가 나면 제일 먼저 알려 드릴게요!").End();
                    break;
            }
            return d;
        }

        /// <summary>주민 잡담: chatter 의 "표정|동작|대사" 를 차례로 하나씩.</summary>
        public static Dialogue Chatter(Npc npc, int index)
        {
            var d = new Dialogue { title = npc.place, speaker = npc.npcName, portrait = npc.portrait, npc = npc };
            if (npc.chatter == null || npc.chatter.Length == 0) { d.Say("normal", "Wave", "안녕하세요, 여행자님! 좋은 하루예요.").End(); return d; }
            var parts = npc.chatter[((index % npc.chatter.Length) + npc.chatter.Length) % npc.chatter.Length].Split('|');
            if (parts.Length >= 3) d.Say(parts[0], parts[1], parts[2]).End(); else d.Say("normal", "Talk", parts[parts.Length - 1]).End();
            return d;
        }
    }
}
