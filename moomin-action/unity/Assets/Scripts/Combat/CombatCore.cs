using System.Collections;
using UnityEngine;

namespace MoominAction
{
    /// <summary>캐릭터 행동 상태(설계서 2-2). Idle/Move 는 이동 여부로 자동 결정.</summary>
    public enum CombatState { Idle, Move, Attack, Recovery, Dodge, Guard, Clash, Stagger, Dead }

    /// <summary>실제 피해량 D = P × M × R (설계서 6-3). 반올림, 최소 1.</summary>
    public static class DamageCalculator
    {
        public static int Final(int power, float multiplier, float defense = 1f) => Mathf.Max(1, Mathf.RoundToInt(power * multiplier * defense));
    }

    /// <summary>짧은 히트스톱(시간을 잠깐 느리게). 메뉴가 열려 있으면 무시.</summary>
    public static class HitStop
    {
        class Runner : MonoBehaviour
        {
            Coroutine c;
            public void Run(float seconds, float scale) { if (c != null) StopCoroutine(c); c = StartCoroutine(Go(seconds, scale)); }
            IEnumerator Go(float s, float scale) { Time.timeScale = scale; yield return new WaitForSecondsRealtime(s); if (!UIState.Paused) Time.timeScale = 1f; c = null; }
        }
        static Runner runner;
        public static void Do(float seconds, float scale = 0.05f)
        {
            if (UIState.Paused) return;
            if (runner == null) { var go = new GameObject("HitStop"); Object.DontDestroyOnLoad(go); runner = go.AddComponent<Runner>(); }
            runner.Run(seconds, scale);
        }
    }
}
