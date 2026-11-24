using System;
using System.Collections.Generic;

namespace MoominAction
{
    public enum QuestState { NotStarted = 0, Active = 1, Completed = 2 }

    /// <summary>
    /// 퀘스트 진행 기록(순수 C# — UnityEngine 없음, tools/status_tests 로 검증).
    /// 상태: 시작 전 → 진행 중(수락) → 완료. 거절은 기록하지 않음(다시 말을 걸면 또 부탁함). 포기하면 시작 전으로.
    /// 저장: "id=Active" 문자열 배열(SaveData.quests). 모르는 상태 글자는 무시.
    /// </summary>
    public class QuestLog
    {
        readonly Dictionary<string, QuestState> states = new Dictionary<string, QuestState>();
        readonly List<string> order = new List<string>();   // 수락한 순서(추적 HUD 표시 순서)
        public event Action<string, QuestState> Changed;

        public QuestState State(string id) => id != null && states.TryGetValue(id, out var s) ? s : QuestState.NotStarted;
        public bool IsActive(string id) => State(id) == QuestState.Active;
        public bool IsDone(string id) => State(id) == QuestState.Completed;

        public bool Accept(string id)
        {
            if (string.IsNullOrEmpty(id) || State(id) != QuestState.NotStarted) return false;
            Set(id, QuestState.Active); return true;
        }
        public bool Complete(string id)
        {
            if (State(id) != QuestState.Active) return false;
            Set(id, QuestState.Completed); return true;
        }
        public bool Abandon(string id)
        {
            if (State(id) != QuestState.Active) return false;
            states.Remove(id); order.Remove(id); Changed?.Invoke(id, QuestState.NotStarted); return true;
        }

        /// <summary>진행 중인 퀘스트(수락한 순서).</summary>
        public List<string> Active() { var l = new List<string>(); foreach (var id in order) if (State(id) == QuestState.Active) l.Add(id); return l; }
        public int CompletedCount { get { int n = 0; foreach (var kv in states) if (kv.Value == QuestState.Completed) n++; return n; } }

        void Set(string id, QuestState s)
        {
            states[id] = s; if (!order.Contains(id)) order.Add(id); Changed?.Invoke(id, s);
        }

        public string[] Export()
        {
            var l = new List<string>(); foreach (var id in order) if (states.TryGetValue(id, out var s) && s != QuestState.NotStarted) l.Add(id + "=" + s); return l.ToArray();
        }
        public void Import(string[] entries)
        {
            states.Clear(); order.Clear(); if (entries == null) return;
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e)) continue; int k = e.IndexOf('='); if (k <= 0 || k == e.Length - 1) continue;
                string id = e.Substring(0, k).Trim(); if (id.Length == 0) continue;
                if (!Enum.TryParse(e.Substring(k + 1).Trim(), false, out QuestState s) || !Enum.IsDefined(typeof(QuestState), s) || s == QuestState.NotStarted) continue;
                states[id] = s; if (!order.Contains(id)) order.Add(id);
            }
        }
        public void Clear() { states.Clear(); order.Clear(); }
    }

    /// <summary>퀘스트 글자(순수 C#).</summary>
    public static class QuestText
    {
        /// <summary>"땅콩빵 조각 1/2" (넘치면 2/2).</summary>
        public static string Progress(string item, int have, int need) => $"{item} {Math.Max(0, Math.Min(have, need))}/{need}";
        public static bool Enough(int have, int need) => need <= 0 || have >= need;
        /// <summary>보상 줄: "경험치 120 · 금화 50 · 빨간 물약 ×2".</summary>
        public static string Rewards(long exp, int gold, IList<(string name, int count)> items)
        {
            var parts = new List<string>();
            if (exp > 0) parts.Add("경험치 " + exp); if (gold > 0) parts.Add("금화 " + gold);
            if (items != null) foreach (var (name, count) in items) if (!string.IsNullOrEmpty(name) && count > 0) parts.Add(count > 1 ? name + " ×" + count : name);
            return string.Join(" · ", parts);
        }
    }
}
