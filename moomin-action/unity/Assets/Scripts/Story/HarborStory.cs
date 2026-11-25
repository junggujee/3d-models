namespace MoominAction
{
    /// <summary>
    /// 남쪽 선착장 어부 철썩이(조랭하프족, 노란 어부 모자 — 초상화 fisher_<표정>).
    /// 인사 → 무엇이 궁금하니? [통통배 타는 법 · 헤엄·잠수하는 법 · 바다 이야기 · 괜찮아요] → 설명 뒤 다시 고르기.
    /// </summary>
    public static class HarborStory
    {
        static int talks;
        public static Dialogue Fisher(Npc npc)
        {
            var d = new Dialogue { title = "남쪽 선착장", speaker = npc.npcName, portrait = npc.portrait, npc = npc };
            if (talks++ == 0)
            {
                d.Say("happy", "Wave", "어이, 털복숭이 친구! 이 바닷바람 냄새, 처음 맡아 보는 얼굴인데?");
                d.Say("normal", "Talk", "나는 이 선착장을 지키는 조랭하프족 어부 철썩이야. 파도 소리가 철썩철썩해서 철썩이지. 헤헤.");
            }
            else d.Say("happy", "Wave", "또 왔구나! 오늘 바다는 잔잔해서 놀기 딱 좋아.");
            d.Say("normal", "Talk", "무엇이 궁금하니?").Label("menu")
             .Choose(new DialogueChoice { text = "통통배 타는 법", next = "boat", primary = true },
                     new DialogueChoice { text = "헤엄 · 잠수하는 법", next = "swim" },
                     new DialogueChoice { text = "바다 이야기", next = "story" },
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
            d.Say("happy", "Wave", "바닷바람 조심하고! 언제든 또 놀러 와.").Label("bye").End();
            return d;
        }
    }
}
