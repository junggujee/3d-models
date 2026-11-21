using System;
using UnityEngine;

namespace MoominAction
{
    public enum StatusType { None, Bleed, Burn, Tremor }

    public static class StatusInfo
    {
        public static string Name(StatusType t) => t switch { StatusType.Bleed => "출혈", StatusType.Burn => "화상", StatusType.Tremor => "진동", _ => "" };
        public static Color ColorOf(StatusType t) => t switch { StatusType.Bleed => new Color(0.95f, 0.25f, 0.25f), StatusType.Burn => new Color(1f, 0.55f, 0.15f), StatusType.Tremor => new Color(0.75f, 0.4f, 1f), _ => Color.white };
        public static string Rule(StatusType t) => t switch
        {
            StatusType.Bleed => "공격(코인)할 때마다 출혈 수치만큼 체력을 잃고 수치 1 감소",
            StatusType.Burn => "3초마다 화상 수치만큼 체력을 잃고 수치 1 감소",
            StatusType.Tremor => "공격에 맞을 때마다 진동 수치만큼 정신력을 잃고 수치 1 감소",
            _ => "",
        };
    }

    /// <summary>
    /// 상태이상(림버스의 출혈·화상·진동을 실시간에 맞게 단순화). 스킬이 적중하면 부여(SkillDefinition.onHitStatus).
    ///  · 출혈: 공격(코인)할 때마다 수치만큼 체력 손실, 수치 -1  · 화상: 3초마다 수치만큼 체력 손실, 수치 -1  · 진동: 맞을 때마다 수치만큼 정신력 손실, 수치 -1
    /// 상태이상 피해는 회피·방어로 막을 수 없고 경직도 일으키지 않음.
    /// </summary>
    [RequireComponent(typeof(CharacterStats))]
    public class StatusEffects : MonoBehaviour
    {
        public int bleed, burn, tremor; public float burnInterval = 3f; public int maxStacks = 20;
        public event Action Changed;
        CharacterStats stats; float burnTimer;
        void Awake() { stats = GetComponent<CharacterStats>(); }

        public int Get(StatusType t) => t switch { StatusType.Bleed => bleed, StatusType.Burn => burn, StatusType.Tremor => tremor, _ => 0 };
        public void Apply(StatusType t, int stacks)
        {
            if (t == StatusType.None || stacks <= 0 || !stats.IsAlive) return;
            if (t == StatusType.Bleed) bleed = Mathf.Min(maxStacks, bleed + stacks); else if (t == StatusType.Burn) { if (burn == 0) burnTimer = 0; burn = Mathf.Min(maxStacks, burn + stacks); } else tremor = Mathf.Min(maxStacks, tremor + stacks);
            Changed?.Invoke();
        }
        public void OnCoinTossed() { if (bleed <= 0) return; stats.LoseHealth(bleed, "출혈"); bleed--; Changed?.Invoke(); }
        public void OnHitTaken() { if (tremor <= 0) return; stats.ChangeSanity(-tremor, "진동"); tremor--; Changed?.Invoke(); }
        public void Clear() { bleed = burn = tremor = 0; Changed?.Invoke(); }

        void Update()
        {
            if (UIState.Paused || burn <= 0 || !stats.IsAlive) return;
            burnTimer += Time.deltaTime; if (burnTimer < burnInterval) return;
            burnTimer = 0; stats.LoseHealth(burn, "화상"); burn--; Changed?.Invoke();
        }

        /// <summary>HUD 용: "출혈 3 · 화상 2" (색 포함).</summary>
        public string Summary()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in new[] { StatusType.Bleed, StatusType.Burn, StatusType.Tremor }) { int n = Get(t); if (n <= 0) continue; if (sb.Length > 0) sb.Append("  "); sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(StatusInfo.ColorOf(t))}>{StatusInfo.Name(t)} {n}</color>"); }
            return sb.ToString();
        }
    }
}
