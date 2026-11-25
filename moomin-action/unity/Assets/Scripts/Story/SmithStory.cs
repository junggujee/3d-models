namespace MoominAction
{
    /// <summary>
    /// 풍선고래 대장장이와의 대화(초상화 balloon_normal). 말투는 둥실둥실 느긋하고, 제 실력에 자부심이 가득하다.
    /// 선택지: 제작(재료로 무기·장신구) · 강화(돈으로 +5까지) · 어떻게 하는 거야(설명) · 다음에 올게
    /// </summary>
    public static class SmithStory
    {
        static int met;
        public static Dialogue Greet(BalloonSmith smith)
        {
            var d = new Dialogue { title = "마을 대장간", speaker = "풍선고래", portrait = "balloon", playAnim = smith.Play };
            if (met++ == 0)
            {
                d.Say("happy", "Cheer", "어서 와~ 여기가 마을 대장간이야. 원래는 창고였는데 내가 화덕을 놓고 고쳤지.");
                d.Say("normal", "Talk", "보다시피 나는 풍선고래야. 가볍다고 무시하면 곤란해. 둥실 떠 있으니까 망치를 내리칠 때 온몸으로 떨어질 수 있거든.");
                d.Say("happy", "Hammer", "재료만 가져오면 무기든 장신구든 뚝딱, 돈만 주면 쓰던 무기도 더 단단하게 해 줄게.");
            }
            else d.Say("happy", "Talk", "또 왔구나~ 화덕은 늘 달궈 두고 있어. 뭘 해 줄까?");
            d.Say("normal", "Talk", "뭘 맡길래?").Label("menu").Choose(
                new DialogueChoice { text = "만들어 줘 (제작)", next = "end", primary = true, act = () => { smith.OpenForge(0); return true; } },
                new DialogueChoice { text = "강하게 해 줘 (강화)", next = "end", act = () => { smith.OpenForge(1); return true; } },
                new DialogueChoice { text = "강화가 뭐야?", next = "howto" },
                new DialogueChoice { text = "다음에 올게", next = "bye" });
            d.Say("normal", "Think", "강화는 쓰던 무기를 화덕에 넣고 두들겨서 더 날카롭게 만드는 거야. +5 까지 올라가.").Label("howto");
            d.Say("normal", "Talk", "단계가 오를수록 돈이 많이 들고, 솔직히… 잘 안 돼. +1 은 거의 되는데 +5 는 네 번에 한 번쯤 될까 말까야.");
            d.Say("happy", "Cheer", "그래도 걱정 마! 내 손에서 무기가 부서진 적은 한 번도 없어. 실패해도 무기는 멀쩡해.");
            d.Say("troubled", "Fail", "…다만 +4 부터는 실패하면 한 단계 내려가. 그건 내 탓이 아니라 쇠가 삐진 거야.").Then("menu");
            d.Say("happy", "Yes", "그래~ 화덕 식히지 않고 기다릴게. 재료 모아서 또 와!").Label("bye").End();
            d.Say("normal", "Hammer", "").Label("end").End();
            return d;
        }
    }
}
