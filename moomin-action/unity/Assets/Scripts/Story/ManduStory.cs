namespace MoominAction
{
    /// <summary>
    /// 성문 지기 맨두와 대화(초상화 mandu_normal). 말끝마다 "~맨두".
    /// 처음: 멈춰라맨두! → 메뉴 [들여보내 줘 · 꿀바른 땅콩빵 바치기(있을 때) · 억지로 들어간다(전투) · 그냥 갈게]
    /// 통과한 뒤: 반갑게 경례 + 잡담.
    /// </summary>
    public static class ManduStory
    {
        static int met;
        public static Dialogue Gate(ManduGuard m)
        {
            var d = new Dialogue { title = "성문", speaker = "맨두", portrait = "mandu", playAnim = m.Play };
            var gm = GameManager.Instance; Inventory inv = null; if (gm != null && gm.player != null) gm.player.TryGetComponent(out inv);
            bool hasBread = inv != null && inv.Count("honey_peanut_bread") > 0;
            if (CastleGate.Passed)
            {
                d.Say("happy", "Salute", "충성맨두! 꿀바른 땅콩빵의 은인, 어서 지나가라맨두!");
                d.Say("happy", "Laugh", "그 빵… 정말 맛있었다맨두. 근무 중에 몰래 반쪽 먹었다맨두. 비밀이다맨두!").End();
                return d;
            }
            if (met++ == 0)
            {
                d.Say("surprised", "Salute", "멈춰라맨두! 여기는 왕실근위대가 지키는 성문이다맨두!");
                d.Say("normal", "Talk", "나는 왕실근위대 엘리트, 맨두다맨두. 허락 없이는 털 한 가닥도 못 지나간다맨두.");
                d.Say("normal", "Salute", "옆구리에 찬 이 파란 방망이가 보이냐맨두? 괜히 꺼내게 하지 마라맨두.");
            }
            else d.Say("normal", "Salute", "또 왔냐맨두. 근무 중이다맨두.");
            var ask = d.Say("normal", "Talk", "무슨 일이냐맨두?").Label("menu");
            if (hasBread)
                ask.Choose(new DialogueChoice { text = "꿀바른 땅콩빵 바치기", next = "bread", primary = true, act = () => m.GiveBread(), failNext = "nobread" },
                           new DialogueChoice { text = "들여보내 줘", next = "please" }, new DialogueChoice { text = "억지로 들어간다", next = "force" }, new DialogueChoice { text = "그냥 갈게", next = "bye" });
            else
                ask.Choose(new DialogueChoice { text = "들여보내 줘", next = "please", primary = true }, new DialogueChoice { text = "억지로 들어간다", next = "force" }, new DialogueChoice { text = "그냥 갈게", next = "bye" });
            d.Say("troubled", "No", "안 된다맨두! 근위대 규칙 제1조, 성문은 아무나 못 지나간다맨두!").Label("please");
            d.Say("normal", "Talk", "…하지만 규칙 제2조가 있다맨두. 근위대에게 꿀바른 땅콩빵을 바치는 자는 지나가도 된다맨두.");
            d.Say("happy", "Talk", "땅콩빵 조각 9개를 모아 땅콩빵을 굽고, 금빛 꿀을 듬뿍 발라 오면 된다맨두. 꿀은… 마을 어딘가에서 구할 수 있다맨두.");
            d.Say("troubled", "No", "절대 내가 먹고 싶어서 그러는 게 아니다맨두. 규칙이다맨두. 정말이다맨두!").Then("menu");
            d.Say("happy", "Laugh", "오오… 이 냄새는…! 꿀바른 땅콩빵이 맞다맨두!").Label("bread");
            d.Say("happy", "Salute", "규칙 제2조에 따라 통과를 허락한다맨두! 충성맨두!").End();
            d.Say("surprised", "Talk", "어라? 빵이 없다맨두? 장난치면 안 된다맨두!").Label("nobread").Then("menu");
            d.Say("surprised", "No", "억지로?! 감히 왕실근위대 엘리트 앞에서 그런 말을 하다니맨두!").Label("force");
            d.Say("troubled", "Windup", "…좋다맨두. (옆구리에 찬 파란 방망이를 스윽 뽑아 든다) 이건 근위대 지급품이다맨두.");
            d.Say("troubled", "No", "몽둥이 찜질부터 홈런까지, 한 대도 안 맞을 자신 있냐맨두? 덤벼라맨두!").OnShow(() => m.RequestFight()).End();
            d.Say("happy", "Yes", "그렇다맨두. 착한 여행자다맨두. 빵을 구하면 다시 오너라맨두.").Label("bye").End();
            return d;
        }
    }
}
