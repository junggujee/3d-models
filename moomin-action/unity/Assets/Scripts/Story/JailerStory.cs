namespace MoominAction
{
    /// <summary>
    /// 지하 노역장 간수 하프(초상화 jailer_*). 무뚝뚝하지만 속은 여린 말투 — "~다" · "흠흠".
    /// 갇힌 동안: 할당량 안내 → 다 채웠으면 "돌덩이를 낸다" → 곡괭이를 돌려받고 출구 쇠창살을 올려 줌.
    /// 풀려난 뒤: 다시 고래 말을 하면 또 온다고 잔소리.
    /// </summary>
    public static class JailerStory
    {
        public static Dialogue Build(Npc npc)
        {
            var d = new Dialogue { title = "지하 광산 노역장", speaker = "간수 하프", portrait = "jailer", npc = npc };
            var ci = CastleInterior.Instance; var gm = GameManager.Instance; Inventory inv = null; if (gm != null && gm.player != null) gm.player.TryGetComponent(out inv);
            int have = inv != null ? inv.Count(MineMath.Rubble) : 0;
            if (ci == null || !ci.Imprisoned)
            {
                d.Say("normal", "Talk", "흠흠. 이제 넌 자유의 몸이다. 출구 쇠창살은 올려 뒀다.");
                d.Say("troubled", "No", "…여왕님 앞에서 또 끼룩거리면 다시 여기로 온다. 알았지?").End();
                return d;
            }
            if (inv != null && inv.Count(MineMath.Pickaxe) <= 0)
            {
                d.Say("normal", "Talk", "신입이군. 여왕님 앞에서 고래 말을 했다고? 흠흠… 대담한 녀석이다.");
                d.Say("normal", "QuestAccept", $"규칙은 간단하다. 이 곡괭이로 노역장 바위를 캐서 돌덩이 {MineMath.Quota}개를 가져와라. 그럼 내보내 주지.")
                 .OnShow(() => { if (inv != null && inv.Count(MineMath.Pickaxe) <= 0) inv.Add(MineMath.Pickaxe, 1, silent: true); Quests.AnnounceNow("노역 시작", $"돌덩이 {MineMath.Quota}개 모으기"); });
                d.Say("happy", "Yes", "운이 좋으면 바위 속에서 광석도 나온다. 구리 · 철 · 은 · 금… 하프 사파이어 원석은 나도 딱 한 번 봤다. 광석은 네가 가져도 된다.");
                d.Say("normal", "Talk", "바위 앞에서 F 로 곡괭이질. 세 번이면 부서진다. 부서진 바위는 시간이 지나면 다시 솟는다.").End();
                return d;
            }
            if (!MineMath.QuotaMet(have))
            {
                d.Say("troubled", "No", $"아직 멀었다. {MineMath.Progress(have)}. 손을 놀려라.");
                d.Say("normal", "Talk", "…힘들면 잠깐 쉬어도 된다. 흠흠. 아무한테도 말하지 마라.").End();
                return d;
            }
            d.Say("surprised", "Surprised", $"벌써 다 캤나? {MineMath.Progress(have)}… 제법이군.")
             .Choose(new DialogueChoice { text = "할당량 채웠어요 민미!", next = "free", primary = true, act = () => ci.Release() }, new DialogueChoice { text = "조금 더 캘게요 민미", next = "more" });
            d.Say("happy", "Laugh", "좋다. 곡괭이는 돌려받는다. 출구 쇠창살을 올려 주지. 광석은 챙겨 가라.").Label("free");
            d.Say("normal", "Yes", "출구 계단으로 올라가면 알현실이다. 이번엔 사람 말로 인사해라. 흠흠.").End();
            d.Say("happy", "Yes", "광석이 탐나는 거군. 마음대로 캐라. 다 캐면 다시 말해라.").Label("more").End();
            return d;
        }
    }
}
