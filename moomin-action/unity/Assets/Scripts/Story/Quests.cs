using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>퀘스트 정의: 누가 · 무엇을 몇 개 · 보상.</summary>
    public class QuestDef
    {
        public string id, title, giver, summary, itemId, itemName; public int itemCount;
        public long rewardExp; public int rewardGold; public (string id, int count)[] rewardItems = new (string, int)[0];
        public string RewardLine
        {
            get
            {
                var l = new List<(string, int)>(); foreach (var (rid, n) in rewardItems) { var d = ItemDatabase.Get(rid); l.Add((d != null ? d.name : rid, n)); }
                return QuestText.Rewards(rewardExp, rewardGold, l);
            }
        }
    }

    /// <summary>
    /// 퀘스트 목록 + 진행 기록(QuestLog) + 건네기·보상. 저장은 SaveData.quests(SaveSystem 이 Export/Import).
    /// 첫 이야기: 땅콩빵이 먹고 싶은 조랭이(Story/JoraengStory) — 땅콩빵 조각 2개 → 경험치 120 · 금화 50 · 빨간 물약 2.
    /// </summary>
    public static class Quests
    {
        public const string PeanutCraving = "peanut_craving";
        public static readonly QuestLog Log = new QuestLog();
        static readonly Dictionary<string, QuestDef> defs = new Dictionary<string, QuestDef>();
        /// <summary>수락·완료·가방 변화로 추적 HUD 를 다시 그려야 할 때.</summary>
        public static event Action Updated;
        /// <summary>화면 가운데 큰 알림(퀘스트 수락 / 완료).</summary>
        public static event Action<string, string> Announce;

        static Quests()
        {
            Add(new QuestDef
            {
                id = PeanutCraving, title = "땅콩빵이 먹고 싶은 조랭이", giver = "조랭이", summary = "배고픈 조랭이에게 땅콩빵 조각 두 개를 가져다주자. 땅콩빵은 산 쪽에 있고, 때리면 조각이 떨어진다.",
                itemId = "peanut_piece", itemName = "땅콩빵 조각", itemCount = 2, rewardExp = 120, rewardGold = 50, rewardItems = new[] { ("potion_red", 2) },
            });
            Log.Changed += (_, __) => Updated?.Invoke();
        }
        static void Add(QuestDef q) => defs[q.id] = q;
        public static QuestDef Get(string id) => id != null && defs.TryGetValue(id, out var q) ? q : null;

        static Inventory Bag
        {
            get { var gm = GameManager.Instance; if (gm == null || gm.player == null) return null; return gm.player.TryGetComponent<Inventory>(out var inv) ? inv : null; }
        }
        public static int Have(QuestDef q) { var b = Bag; return q == null || b == null || string.IsNullOrEmpty(q.itemId) ? 0 : b.Count(q.itemId); }
        public static bool CanTurnIn(QuestDef q) => q != null && Log.IsActive(q.id) && QuestText.Enough(Have(q), q.itemCount);
        public static string ProgressLine(QuestDef q) => q == null ? "" : QuestText.Progress(q.itemName, Have(q), q.itemCount);

        public static bool Accept(QuestDef q)
        {
            if (q == null || !Log.Accept(q.id)) return false;
            Announce?.Invoke("퀘스트 수락", q.title); return true;
        }

        /// <summary>물건 건네기 → 완료 → 보상(경험치·금화·아이템).</summary>
        public static bool TurnIn(QuestDef q)
        {
            if (!CanTurnIn(q)) return false; var bag = Bag;
            if (bag != null && q.itemCount > 0 && !bag.Remove(q.itemId, q.itemCount)) return false;
            Log.Complete(q.id);
            var gm = GameManager.Instance;
            if (q.rewardExp > 0 && gm != null && gm.player != null && gm.player.TryGetComponent<Experience>(out var xp)) xp.Gain(q.rewardExp);
            if (bag != null)
            {
                if (q.rewardGold > 0) bag.AddGold(q.rewardGold);
                foreach (var (rid, n) in q.rewardItems)
                {   // 가방이 가득 차면 남은 보상은 발밑에 떨어뜨림(예전엔 그냥 사라짐)
                    int left = bag.Add(rid, n);
                    if (left > 0) ItemPickup.Spawn(rid, left, bag.transform.position + bag.transform.forward * 0.8f + Vector3.up * 0.4f, 2f);
                }
            }
            Announce?.Invoke("퀘스트 완료", q.title); return true;
        }

        /// <summary>추적 HUD 줄들: (제목, 진행 글자, 다 모았나).</summary>
        public static List<(string title, string progress, bool ready)> Tracker()
        {
            var l = new List<(string, string, bool)>();
            foreach (var id in Log.Active()) { var q = Get(id); if (q == null) continue; bool ready = CanTurnIn(q); l.Add((q.title, ready ? $"{q.giver}에게 돌아가자  ({ProgressLine(q)})" : ProgressLine(q), ready)); }
            Hunts.AddTrackerLines(l);   // 토벌 퀘스트
            return l;
        }

        public static void Refresh() => Updated?.Invoke();
        /// <summary>큰 알림 띄우기(토벌 퀘스트 등 다른 곳에서).</summary>
        public static void AnnounceNow(string kind, string title) => Announce?.Invoke(kind, title);
        public static string[] Export() => Log.Export();
        public static void Import(string[] entries) { Log.Import(entries); Updated?.Invoke(); }
        public static void NewGame() { Log.Clear(); Updated?.Invoke(); }
    }
}
