using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 하프물범 여왕 하프리나(백설궁 알현실, 초상화 queen_* — 금박 메달). 말투: 짐 · ~니라 · ~구나 · ~거라(고풍스럽고 다정하게).
    /// 처음: 여왕 소개 → 민미 소개(선택지) → 왕국 소개 → 퀘스트 「여왕의 등불」(곰기름 2) 제시.
    /// 그 뒤: 선택지 ① 이 왕국에 대해 자세히 소개해줘 민미 ② 장비에 축복을 부탁해 민미(퀘스트 끝내기 전엔 잠김)
    ///             ③ 끼루룩 끼룩끼룩 민미민미 → "? 뭐야 하프 너 고래족이었나." → "어쩔 수 없군." → "넌 지하감옥으로 가라" → 지하감옥
    ///         (+ 곰기름을 다 모았으면 바치기 · 물러나기)
    /// </summary>
    public static class QueenStory
    {
        public static Dialogue Build(Npc npc)
        {
            var d = new Dialogue { title = "백설궁 · 알현실", speaker = "하프물범 여왕", portrait = "queen", npc = npc };
            var q = Quests.Get(Quests.QueenLamps);
            if (!Quests.Log.IsDone(Quests.QueenMet)) First(d, q); else Menu(d, q);
            return d;
        }

        static void MarkMet() { Quests.Log.Accept(Quests.QueenMet); Quests.Log.Complete(Quests.QueenMet); }

        // ── 처음 만남: 서로 소개 · 왕국 소개 · 퀘스트
        static void First(Dialogue d, QuestDef q)
        {
            d.Say("surprised", "Surprised", "오호… 맨두가 성문을 열어 주었다니. 꿀바른 땅콩빵을 들고 온 손님이 바로 그대로구나.").OnShow(MarkMet);
            d.Say("normal", "Talk", "짐은 이 백설궁의 주인이자 눈처럼 흰 하프물범들의 여왕, 하프리나니라.");
            d.Say("normal", "Idle", "그대는 누구인가? 이름을 들려주겠느냐?")
             .Choose(new DialogueChoice { text = "민미라고 해요! 조랭하프 마을에서 왔어요 민미", next = "me", primary = true });
            d.Say("happy", "Laugh", "민미… 동글동글 귀여운 이름이로구나. 반갑다, 민미.").Label("me");
            d.Say("normal", "Talk", "이곳은 하프 왕국. 바다와 산 사이, 이 언덕 위에 하프물범들이 세운 나라란다.");
            d.Say("happy", "Yes", "성문은 근위대 엘리트 맨두가, 언덕 아래 마을은 짐의 사랑스러운 조랭하프 백성들이 지키고 있지.");
            d.Say("normal", "Talk", "그리고 이 알현실의 별빛 샹들리에는 왕국의 자랑이니라. 그런데…");
            d.Say("sad", "Sad", "겨울이 가까워지며 등불이 하나둘 꺼져 가고 있구나. 이대로면 성 안이 어둠에 잠길 게다.");
            if (q == null) { d.Say("normal", "Idle", "…부디 편히 쉬다 가거라.").End(); return; }
            d.Say("troubled", "Talk", "등불을 밝히는 데는 곰곰이가 겨울잠용으로 모아 둔 진한 곰기름이 제일이지. 보스 웨이브에 나타나는 그 잠꾸러기 곰 말이다.");
            d.Say("normal", "QuestAccept", $"민미, 곰곰이를 혼내 주고 곰기름 두 병을 가져와 줄 수 있겠느냐?\n<color=#8A5A1E>퀘스트 — {q.title}</color>")
             .Choose(new DialogueChoice { text = "맡겨 주세요 민미!", next = "yes", primary = true, act = () => Quests.Accept(q) }, new DialogueChoice { text = "조금 생각해 볼게요 민미", next = "later" });
            d.Say("happy", "Laugh", "고맙구나! 등불이 다시 켜지면, 짐이 그대의 장비에 왕실의 축복을 내려 주마.").Label("yes").End();
            d.Say("normal", "Yes", "그래, 서두를 필요는 없다. 마음이 정해지면 다시 오너라.").Label("later").End();
        }

        // ── 그 뒤: 선택지
        static void Menu(Dialogue d, QuestDef q)
        {
            var state = q != null ? Quests.Log.State(q.id) : QuestState.NotStarted;
            bool done = state == QuestState.Completed, ready = q != null && Quests.CanTurnIn(q);
            string hello = ready ? "그 반짝이는 병은… 곰기름이로구나!" : state == QuestState.Active ? "곰기름은 구했느냐? 등불이 그대를 기다리고 있단다." : done ? "민미, 왔구나. 샹들리에가 다시 환하게 빛나는구나." : "다시 왔구나, 민미.";
            d.Say(ready ? "surprised" : "normal", ready ? "Surprised" : "Talk", hello);
            var choices = new List<DialogueChoice>
            {
                new DialogueChoice { text = "이 왕국에 대해 자세히 소개해줘 민미", next = "lore", primary = !ready },
                new DialogueChoice { text = done ? "장비에 축복을 부탁해 민미" : "장비에 축복을 부탁해 민미  (잠김)", next = done ? "bless" : "locked" },
                new DialogueChoice { text = "끼루룩 끼룩끼룩 민미민미", next = "whale" },
            };
            if (ready) choices.Insert(0, new DialogueChoice { text = "곰기름 두 병을 바친다", next = "turnin", primary = true, act = () => Quests.TurnIn(q), failNext = "menu" });
            if (q != null && state == QuestState.NotStarted) choices.Add(new DialogueChoice { text = "등불 일을 맡을게요 민미", next = "accept", act = () => Quests.Accept(q) });
            choices.Add(new DialogueChoice { text = "물러날게요 민미", next = "bye" });
            d.Say("normal", "Idle", "무슨 일로 왔느냐, 민미?").Label("menu").Choose(choices.ToArray());

            // ① 왕국 소개
            d.Say("normal", "Talk", "하프 왕국은 아주 먼 옛날, 얼음 바다에서 올라온 하프물범들이 세운 나라란다.").Label("lore");
            d.Say("happy", "Yes", "첫 여왕께서 진주 지팡이로 이 언덕을 콩 두드리시자, 눈처럼 하얀 성이 솟아올랐다고 전해지지. 그게 바로 백설궁이니라.");
            d.Say("normal", "Talk", "언덕 아래 조랭하프 마을은 짐의 백성들의 마을이고, 남쪽 바다 밑에는 고래족의 용궁이 있단다.");
            d.Say("troubled", "No", "고래족과는… 흠, 오래전 '끼룩 전쟁' 이후로 사이가 조금 서먹하구나. 요즘 고래고래 임금님이 생일 잔치를 연다니 다행이다만.");
            d.Say("normal", "Talk", "성문은 맨두가, 성 안은 근위대가 지킨다. 성 깊은 곳엔 짐도 함부로 열지 않는 보물고와…");
            d.Say("happy", "Laugh", "…말 안 듣는 이들을 위한 지하감옥이 있지. 후후, 그대는 갈 일이 없겠지?").Then("menu");

            // ② 축복(잠김 / 열림)
            d.Say("troubled", "No", "그 축복은 왕국의 등불을 밝힌 이에게만 내리는 것이니라. 곰기름 두 병을 먼저 부탁하마.").Label("locked").Then("menu");
            d.Say("normal", "QuestAccept", "좋다. 장비를 가까이 내밀어 보거라…").Label("bless").OnShow(Bless);
            d.Say("happy", "Laugh", BlessText()).Then("menu");

            // ③ 고래 말 → 지하감옥
            d.Say("surprised", "Surprised", "-? 뭐야 하프 너 고래족이었나.").Label("whale");
            d.Say("troubled", "No", "-어쩔 수 없군.");
            d.Say("troubled", "Idle", "-넌 지하감옥으로 가라").OnShow(() => { if (CastleInterior.Instance != null) CastleInterior.Instance.pendingDungeon = true; }).End();

            // 퀘스트 완료 · 수락 · 작별
            d.Say("happy", "QuestComplete", q != null ? $"오오… 이 진한 향! 등불이 다시 별처럼 빛나겠구나. 고맙다, 민미.\n<color=#8A5A1E>보상 — {q.RewardLine}</color>" : "고맙구나.").Label("turnin");
            d.Say("happy", "Yes", "약속대로 이제부터 짐의 축복을 청할 수 있느니라. '장비에 축복을 부탁해'라고 말하거라.").Then("menu");
            d.Say("happy", "Laugh", "고맙구나! 곰곰이는 보스 웨이브에 나타난다. 곰기름 두 병, 기다리마.").Label("accept").Then("menu");
            d.Say("normal", "Yes", "그래, 조심히 가거라. 백설궁의 문은 언제나 열려 있단다.").Label("bye").End();
        }

        static List<string> lastBlessed = new();
        static void Bless()
        {
            lastBlessed.Clear();
            var gm = GameManager.Instance; if (gm == null || gm.player == null || !gm.player.TryGetComponent<Inventory>(out var inv)) return;
            lastBlessed = inv.BlessEquipped();
            var p = gm.player.transform.position + Vector3.up * 0.8f;
            SparkBurst.Spawn(p, Vector3.up, 70, 6f, new Color(1f, 0.88f, 0.5f)); SparkBurst.Spawn(p, Vector3.up, 30, 4f, new Color(0.5f, 0.75f, 1f));
            if (lastBlessed.Count > 0) Quests.AnnounceNow("여왕의 축복", string.Join(" · ", lastBlessed));
        }
        /// <summary>축복 대사(OnShow 뒤에 보이는 줄이라, 글자는 그때 정해짐 — 대사 생성 시점엔 미리 계산).</summary>
        static string BlessText()
        {
            var gm = GameManager.Instance; if (gm == null || gm.player == null || !gm.player.TryGetComponent<Inventory>(out var inv)) return "짐, 하프리나의 이름으로… 축복을!";
            var names = new List<string>(); bool any = false;
            foreach (var id in inv.EquippedIds()) { var dd = ItemDatabase.Get(id); if (dd == null || !dd.Equippable) continue; any = true; if (!inv.Blessed(id)) names.Add(dd.name); }
            if (!any) return "흠… 그대는 지금 낀 장비가 없구나. 장비를 갖추고 다시 오거라.";
            if (names.Count == 0) return "이미 그대의 장비엔 짐의 축복이 깃들어 있구나. 새 장비를 끼면 다시 오거라.";
            return $"짐, 하프리나의 이름으로… 「{string.Join(", ", names)}」에 왕실의 축복을!\n<color=#8A5A1E>무기: 공격 ×1.06 · 최종 위력 +1 / 방어구·아티팩트: 받는 피해 -2% · 최대 체력 +10</color>";
        }
    }
}
