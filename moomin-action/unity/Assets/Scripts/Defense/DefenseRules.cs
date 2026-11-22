using System.Collections.Generic;

namespace MoominAction
{
    /// <summary>
    /// 회피 상태(순수 로직). 회피를 시작할 때 코인을 <b>한 번만</b> 굴려 회피 위력을 정하고,
    /// 들어오는 공격 위력과 비교: 회피 위력 ≥ 공격 위력 → 성공(상태 유지 — 다음 공격에도 같은 위력으로 판정), 작으면 실패(상태 종료).
    /// </summary>
    public class EvadeState
    {
        public bool Active { get; private set; }
        public int Power { get; private set; }
        public bool Heads { get; private set; }
        public int Successes { get; private set; }
        public void Start(int power, bool heads) { Active = true; Power = power; Heads = heads; Successes = 0; }
        /// <summary>true = 회피 성공(피해 없음). 회피 중이 아니면 false.</summary>
        public bool TryEvade(int incomingPower)
        {
            if (!Active) return false;
            if (Power >= incomingPower) { Successes++; return true; }
            Active = false; return false;
        }
        public void End() { Active = false; }
    }

    public static class DefenseRules
    {
        /// <summary>가드 피해 배율 = 1 - 감소율(0~0.9). 가드 중이 아니면 1.</summary>
        public static float GuardMultiplier(bool guarding, float reduction) => guarding ? 1f - (reduction < 0f ? 0f : reduction > 0.9f ? 0.9f : reduction) : 1f;

        /// <summary>같은 캐릭터가 여러 번(콜라이더 여러 개) 잡혀도 한 번만, 처음 나온 순서대로.</summary>
        public static List<T> Unique<T>(IEnumerable<T> found) where T : class
        {
            var seen = new HashSet<T>(); var list = new List<T>();
            foreach (var x in found) if (x != null && seen.Add(x)) list.Add(x);
            return list;
        }
    }
}
