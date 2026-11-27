namespace MoominAction
{
    /// <summary>
    /// 돌고래 죄수 대사(초상화 dolphin_normal). 말끝에 "끼룩". 매번 무작위로 두 갈래:
    ///  · 억울파 — 고래 말 한마디 했다가 잡혀 왔다는 하소연
    ///  · 광석파 — "광석 얻을 거야 끼룩!!!" 신난 노역 돌고래
    /// 감방 안 돌고래는 철창 너머로 하는 말.
    /// </summary>
    public static class DolphinStory
    {
        static readonly string[][] Sorry =
        {
            new[] { "끼룩… 나는 그냥 \"끼룩\" 하고 기침 한 번 했을 뿐인데 끼룩…", "여왕님이 \"고래족이로구나!\" 하시더니 여기로 보내 버렸어 끼룩. 억울해 끼루룩…" },
            new[] { "나 고래 아니야 끼룩! 돌고래라고! 고래랑 돌고래는 완전 다르다고 끼룩!!", "…그 말을 고래 말로 해 버려서 잡혀 왔지만 끼룩." },
            new[] { "바다에서 헤엄치다 맛있는 땅콩빵 냄새를 따라왔는데 끼룩…", "정신 차려 보니 곡괭이를 들고 있었어. 이상하지 끼룩?" },
            new[] { "여기 온 지 벌써 백 일째야 끼룩. 날마다 노을이 창살 사이로 들어오는 것만 봐 끼룩…", "그래도 노을은 예뻐. 바다 생각이 나 끼루룩." },
            new[] { "간수 하프님은 무섭게 생겼는데 몰래 간식도 줘 끼룩.", "그래도 집에 가고 싶어 끼룩… 끼룩…" },
        };
        static readonly string[][] Ore =
        {
            new[] { "광석 얻을 거야 끼룩!!!", "오늘은 꼭 돌고래스톤을 캘 거야! 돌고래 모양이래 끼룩끼룩!!" },
            new[] { "통통통! 바쁘다 바빠 끼룩!", "구리… 구리… 또 구리 끼룩! 은은 언제 나와 끼룩?!" },
            new[] { "쉿, 비밀인데 끼룩… 바위 틈에서 꿀이 나와!", "몰래 핥아 먹으면 엄청 달아 끼룩! 간수님한테 말하면 안 돼 끼룩!!" },
            new[] { "곡괭이질은 이렇게! 끼룩! 끼룩! 끼루룩!!", "세 번 치면 바위가 와르르! 너도 해 봐 끼룩!" },
            new[] { "할당량 다 채우면 나도 나갈 수 있을까 끼룩?", "근데 나는 벌써 열두 번 채웠는데 아직 여기 있어 끼룩… 어라? 끼룩??" },
        };
        static readonly string[][] Cage =
        {
            new[] { "끼룩… 거기 털복숭이! 나 좀 꺼내 줘 끼룩!", "…안 되는구나 끼룩. 그럼 노을이라도 같이 봐 줘 끼룩." },
            new[] { "감방 바닥이 차가워 끼룩. 지푸라기가 까슬까슬해 끼룩…", "바다 물결 소리가 그리워 끼루룩…" },
            new[] { "나는 반성 중이야 끼룩. 무엇을 반성하는지는 모르겠지만 끼룩!", "아무튼 반성 중이야 끼룩!!" },
        };
        public static Dialogue Build(DolphinPrisoner who)
        {
            var d = new Dialogue { title = "구래성 · 지하 광산 노역장", speaker = $"돌고래 죄수 {who.number}번", portrait = "dolphin" };
            string[] pick = who.mode == DolphinPrisoner.Mode.Caged ? Cage[UnityEngine.Random.Range(0, Cage.Length)]
                : UnityEngine.Random.value < 0.5f ? Sorry[UnityEngine.Random.Range(0, Sorry.Length)] : Ore[UnityEngine.Random.Range(0, Ore.Length)];
            for (int i = 0; i < pick.Length; i++) { var l = d.Say(i == 0 ? "normal" : "happy", "Idle", pick[i]); if (i == pick.Length - 1) l.End(); }
            return d;
        }
    }
}
