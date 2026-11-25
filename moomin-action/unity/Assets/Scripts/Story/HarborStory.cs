namespace MoominAction
{
    /// <summary>
    /// 남쪽 선착장 어부 철썩이(조랭하프족, 노란 어부 모자 — 초상화 fisher_<표정>).
    /// 인사 → 무엇이 궁금하니? [통통배 타는 법 · 헤엄·잠수하는 법 · 바다 이야기 · 괜찮아요] → 설명 뒤 다시 고르기.
    /// </summary>
    public static class HarborStory
    {
        static int talks;

        /// <summary>퀘스트 「삐진 고래고래 임금님」: 처음이면 부탁 · 진주를 가져오면 건네기(메뉴 전에 끼어듦).</summary>
        static void KingQuest(Dialogue d)
        {
            var q = Quests.Get(Quests.GrumpyKing); if (q == null) return;
            switch (Quests.Log.State(q.id))
            {
                case QuestState.NotStarted:
                    d.Say("surprised", "Surprised", "아참! 털복숭이 친구, 혹시 요즘 바다가 이상한 거 느꼈어?");
                    d.Say("troubled", "No", "바다 위로 딸기가 퐁퐁 튀어 오르고, 물고기들이 공기방울에 갇혀 둥둥 떠다녀…");
                    d.Say("sad", "Sad", "바다 밑 용궁의 고래고래 임금님이 단단히 삐졌대. 생일 케이크 위 딸기를 누가 몽땅 먹어 버렸다나?");
                    d.Say("normal", "Talk", "임금님은 '나보다 센 친구가 오면 기분이 풀릴 거다! 끼룩!' 하고 큰소리쳤대. 혹시… 한번 겨뤄 봐 줄래?")
                     .Choose(new DialogueChoice { text = "수락", next = "kq_yes", primary = true, act = () => Quests.Accept(q) }, new DialogueChoice { text = "나중에", next = "kq_no" });
                    d.Say("happy", "QuestAccept", "고마워! 용궁은 다리 끝에서 바다 쪽으로 쭉, 노란 왕관 부표가 떠 있는 곳 바로 아래야.").Label("kq_yes");
                    d.Say("normal", "Yes", "Z 로 잠수해서 내려가면 커다란 공기방울이 있어. 그 안은 숨도 쉬고 걸어 다닐 수 있지! 공기방울 기둥을 따라가면 돼.");
                    d.Say("troubled", "No", $"임금님이 각성하면 딸기 폭탄이랑 버블 감옥을 쓴대. 떨어질 자리에 분홍 원이 생기면 얼른 밖으로!\n<color=#8A5A1E>퀘스트 — {q.title}</color>").Then("menu");
                    d.Say("sad", "Sad", "그래… 마음이 바뀌면 말해 줘. 임금님이 계속 끼룩끼룩 울고 있대.").Label("kq_no").Then("menu");
                    break;
                case QuestState.Active:
                    if (!Quests.CanTurnIn(q)) { d.Say("troubled", "No", "임금님은 아직도 삐져 있대… 노란 왕관 부표 아래, 용궁 결투장으로 가 봐!"); break; }
                    d.Say("surprised", "Surprised", "그 반짝이는 건… 무지개 진주?! 임금님이 준 거야?")
                     .Choose(new DialogueChoice { text = "보여 주기  (임금님의 무지개 진주)", next = "kq_done", primary = true, act = () => Quests.TurnIn(q), failNext = "menu" }, new DialogueChoice { text = "나중에", next = "menu" });
                    d.Say("happy", "Laugh", "임금님이 친구로 인정했구나! 바다가 다시 잔잔해졌어. 딸기도 더는 안 튀어 오르고!").Label("kq_done");
                    d.Say("happy", "QuestComplete", $"이건 선착장 사람들이 모은 고마움의 선물이야. 받아 줘!\n<color=#8A5A1E>보상 — {q.RewardLine}</color>").Then("menu");
                    break;
            }
        }
        public static Dialogue Fisher(Npc npc)
        {
            var d = new Dialogue { title = "남쪽 선착장", speaker = npc.npcName, portrait = npc.portrait, npc = npc };
            if (talks++ == 0)
            {
                d.Say("happy", "Wave", "어이, 털복숭이 친구! 이 바닷바람 냄새, 처음 맡아 보는 얼굴인데?");
                d.Say("normal", "Talk", "나는 이 선착장을 지키는 조랭하프족 어부 철썩이야. 파도 소리가 철썩철썩해서 철썩이지. 헤헤.");
            }
            else d.Say("happy", "Wave", "또 왔구나! 오늘 바다는 잔잔해서 놀기 딱 좋아.");
            KingQuest(d);
            d.Say("normal", "Talk", "무엇이 궁금하니?").Label("menu")
             .Choose(new DialogueChoice { text = "통통배 타는 법", next = "boat", primary = true },
                     new DialogueChoice { text = "헤엄 · 잠수하는 법", next = "swim" },
                     new DialogueChoice { text = "바다 이야기", next = "story" },
                     new DialogueChoice { text = "용궁 이야기", next = "palace" },
                     new DialogueChoice { text = "괜찮아요", next = "bye" });
            d.Say("happy", "Yes", "다리 끝 오른쪽에 내 통통배를 매어 뒀어. 가까이 가서 F 를 누르면 탈 수 있지.").Label("boat");
            d.Say("normal", "Talk", "W 는 앞으로, S 는 뒤로, A·D 로 방향을 틀어. Shift 를 누르면 굴뚝이 통통통 신나게 연기를 뿜지!");
            d.Say("troubled", "No", "내릴 땐 다시 F. 다리 옆이면 다리 위로 내려 주지만… 바다 한가운데서 내리면 풍덩이야!").Then("menu");
            d.Say("normal", "Talk", "물에 들어가면 저절로 헤엄쳐져. Shift 로 빨리 헤엄칠 수 있지만 기운이 금방 빠져.").Label("swim");
            d.Say("surprised", "Surprised", "Z 를 누르면 잠수! 물속에선 바라보는 쪽으로 헤엄치고, Space 는 위로, 왼쪽 Ctrl 은 아래로 가.");
            d.Say("troubled", "No", "숨이 다 떨어지기 전에 Z 를 다시 눌러 물 위로 올라와야 해. 물에서 나올 땐 모래밭이나 다리 끝 계단으로!").Then("menu");
            d.Say("happy", "Laugh", "이 바다 밑엔 불가사리랑 해초, 분홍 산호가 잔뜩 있어. 다리 기둥 아래가 특히 예쁘지.").Label("story");
            d.Say("surprised", "Surprised", "가끔 산 너머에서 고래고래가 끼룩대는 소리가 들리는데… 설마 바다까지 오는 건 아니겠지?");
            d.Say("happy", "Laugh", "땅콩빵 부스러기를 바다에 뿌리면 물고기들이 줄을 선다니까. 하하!").Then("menu");
            d.Say("happy", "Laugh", Quests.Log.IsDone(Quests.GrumpyKing) ? "요즘 고래고래 임금님이 가끔 선착장까지 와서 땅콩빵을 얻어먹고 가. 생일 케이크도 다시 구웠대!" : "바다 밑 용궁은 노란 왕관 부표 바로 아래야. 커다란 공기방울 안은 숨을 쉴 수 있지!").Label("palace").Then("menu");
            d.Say("happy", "Wave", "바닷바람 조심하고! 언제든 또 놀러 와.").Label("bye").End();
            return d;
        }
    }
}
