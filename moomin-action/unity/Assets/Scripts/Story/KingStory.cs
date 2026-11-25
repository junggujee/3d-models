namespace MoominAction
{
    /// <summary>
    /// 생일 파티 중인 고래고래 임금님과 대화(퀘스트 「삐진 고래고래 임금님」을 끝낸 뒤, 용궁 결투장 딸기 케이크 옆). 초상화 king_normal.
    /// 메뉴: 생일 축하해요! · 딸기 케이크 이야기 · 재대결! (바로 결투 — WhaleKing.StartRematch) · 파티 즐기기.
    /// </summary>
    public static class KingStory
    {
        public static Dialogue Party(WhaleKing king)
        {
            var d = new Dialogue { title = "고래고래 임금님의 생일 파티", speaker = "고래고래 임금님", portrait = "king" };
            d.Say("happy", "Talk", "끼룩~! 내 친구 털복숭이 왔구나! 어서 와, 오늘은 내 생일 파티야!");
            d.Say("normal", "Talk", "뭐 하고 놀까? 끼룩?").Label("menu")
             .Choose(new DialogueChoice { text = "생일 축하해요!", next = "hb", primary = true },
                     new DialogueChoice { text = "딸기 케이크 이야기", next = "cake" },
                     new DialogueChoice { text = "재대결!", next = "duel", act = () => { king.RequestRematch(); return true; } },
                     new DialogueChoice { text = "파티 즐기기", next = "bye" });
            d.Say("happy", "Laugh", "헤헤… 고마워! 올해는 다들 와 줘서 하나도 안 외로워. 조랭하프 친구들이 노래도 불러 줬어!").Label("hb");
            d.Say("happy", "Laugh", "끼룩끼룩~ 생일 축하합니다~ 사랑하는 나에게~♪ …앗, 내가 불렀네?").Then("menu");
            d.Say("happy", "Talk", "이 3단 딸기 케이크는 철썩이가 땅콩빵 가게에 부탁해서 새로 구워 준 거야. 딸기가 무려 마흔 개!").Label("cake");
            d.Say("troubled", "Talk", "…예전에 딸기를 몽땅 먹은 범인? 그건 아직 몰라. 근데 이제 괜찮아. 친구가 생겼으니까!");
            d.Say("happy", "Laugh", "케이크는 이따 다 같이 먹자! 맨 위 큰 딸기는 내 거야. 끼룩!").Then("menu");
            d.Say("surprised", "Talk", "재대결?! 좋아! 생일이라 힘이 펄펄 나거든! 이번엔 안 진다, 끼룩!").Label("duel").End();
            d.Say("happy", "Talk", "맘껏 놀다 가! 풍선은 터뜨리면 안 돼~").Label("bye").End();
            return d;
        }
    }
}
