using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// 토벌 퀘스트(반복 가능) — 정의는 HuntCatalog, 기록은 HuntLog(순수 C#).
    /// 적이 쓰러지면(EnemyCombatController.AnyKilled) 진행 중인 토벌 중 종류가 같고, 그 구역에서 생겼거나 구역 안에서 쓰러진 것만 1마리로 셈(보스 제외).
    /// 보상 = 경험치 · 금화(내 레벨에 따라 커지고, 처음 깨면 ×1.5). 저장 SaveData.hunts.
    /// GameManager 가 Hook/Unhook(정적 이벤트).
    /// </summary>
    public static class Hunts
    {
        public static readonly HuntLog Log = new HuntLog();
        /// <summary>한 마리 잡을 때(정의, 지금 수) — HUD 알림.</summary>
        public static event Action<HuntDef, int> Progressed;

        static Hunts() { Log.Changed += _ => Quests.Refresh(); }

        static bool hooked;
        public static void Hook() { if (hooked) return; hooked = true; EnemyCombatController.AnyKilled += OnKilled; }
        public static void Unhook() { if (!hooked) return; hooked = false; EnemyCombatController.AnyKilled -= OnKilled; }

        public static HuntDef Get(string id) => HuntCatalog.Get(id);
        static int PlayerLevel => Experience.Instance != null ? Experience.Instance.level : 1;
        public static long RewardExp(HuntDef h) => h == null ? 0 : HuntMath.Exp(h.baseExp, PlayerLevel, Log.Clears(h.id));
        public static int RewardGold(HuntDef h) => h == null ? 0 : HuntMath.Gold(h.baseGold, PlayerLevel, Log.Clears(h.id));
        public static string RewardLine(HuntDef h) => QuestText.Rewards(RewardExp(h), RewardGold(h), null) + (h != null && Log.Clears(h.id) == 0 ? "  (첫 보상 ×1.5)" : "");
        public static bool Ready(HuntDef h) => h != null && Log.Ready(h.id, h.need);

        public static bool Accept(HuntDef h)
        {
            if (h == null || !Log.Accept(h.id)) return false;
            Quests.AnnounceNow(Log.Clears(h.id) > 0 ? "토벌 다시 수락" : "퀘스트 수락", h.title); return true;
        }
        public static bool Abandon(HuntDef h) => h != null && Log.Abandon(h.id);

        /// <summary>다 잡았으면 완료 → 보상(경험치·금화). 보상은 완료 전에 계산(첫 보상 배율).</summary>
        public static bool TurnIn(HuntDef h)
        {
            if (!Ready(h)) return false;
            long exp = RewardExp(h); int gold = RewardGold(h);
            if (!Log.TurnIn(h.id, h.need)) return false;
            var gm = GameManager.Instance;
            if (gm != null && gm.player != null)
            {
                if (exp > 0 && gm.player.TryGetComponent<Experience>(out var xp)) xp.Gain(exp);
                if (gold > 0 && gm.player.TryGetComponent<Inventory>(out var inv)) inv.AddGold(gold);
            }
            Quests.AnnounceNow("토벌 완료", h.title); return true;
        }

        static void OnKilled(EnemyCombatController e)
        {
            if (e == null || e.isBoss || e.showBossBar) return;
            string kind = e.Kind; Vector3 pos = e.transform.position;
            foreach (var id in Log.Active())
            {
                var h = Get(id); if (h == null || h.monsterKind != kind) continue;
                bool inZone = e.zoneId == h.zoneId || HuntZone.Contains(h.zoneId, pos);
                if (!inZone || !Log.AddKill(h.id, h.need)) continue;
                int n = Log.Count(h.id); Progressed?.Invoke(h, n);
                if (n >= h.need) Quests.AnnounceNow($"{h.giver}에게 돌아가자", h.title);
            }
        }

        /// <summary>추적 줄: 진행("핑크큐브말랑이 3/5 · 말랑말랑 풀밭 남서 42m") / 다 잡으면 "몽글이에게 돌아가자".</summary>
        public static void AddTrackerLines(List<(string title, string progress, bool ready)> l)
        {
            foreach (var id in Log.Active())
            {
                var h = Get(id); if (h == null) continue; bool ready = Ready(h);
                string prog = HuntMath.Progress(h.monsterName, Log.Count(h.id), h.need);
                l.Add((h.title, ready ? $"{h.giver}에게 돌아가자  ({prog})" : prog + " · " + WhereText(h), ready));
            }
        }

        /// <summary>구역까지 방향·거리(지도 기준 8방위, 들어가 있으면 "안").</summary>
        public static string WhereText(HuntDef h)
        {
            var z = HuntZone.Find(h.zoneId); var gm = GameManager.Instance;
            if (z == null || gm == null || gm.player == null) return h.zoneName;
            Vector3 p = gm.player.transform.position, c = z.transform.position; Vector3 flat = c - p; flat.y = 0;
            float d = flat.magnitude; if (d <= z.radius) return h.zoneName + " 안";
            var md = MapPlaces.MapDelta(p, c);   // 월드맵 그림 기준(위 = 북)
            string dir = HuntMath.Compass(md.x, md.y);
            return $"{h.zoneName} {dir} {Mathf.RoundToInt(d)}m";
        }

        public static string[] Export() => Log.Export();
        public static void Import(string[] list) { Log.Import(list); Quests.Refresh(); }
        public static void NewGame() { Log.Clear(); Quests.Refresh(); }
    }
}
