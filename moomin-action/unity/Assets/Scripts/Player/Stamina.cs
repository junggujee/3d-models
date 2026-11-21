using UnityEngine;

namespace MoominAction
{
    /// <summary>스태미나: 구르기·공격·달리기가 소모하고, 잠깐 쉬면 빠르게 차오름.</summary>
    public class Stamina : MonoBehaviour
    {
        public float max = 100f, regenPerSecond = 32f, regenDelay = 0.7f;
        public float Current { get; private set; }
        float lastSpend = -99f;
        void Awake() { Current = max; }
        void Update() { if (Time.time - lastSpend > regenDelay) Current = Mathf.Min(max, Current + regenPerSecond * Time.deltaTime); }
        public bool Has(float cost) => Current >= Mathf.Min(cost, max * 0.2f);          // 조금 모자라도 시도는 가능(엘든링식)
        public bool Spend(float cost) { if (!Has(cost)) return false; Current = Mathf.Max(0, Current - cost); lastSpend = Time.time; return true; }
        public void Drain(float perSecond) { Current = Mathf.Max(0, Current - perSecond * Time.deltaTime); lastSpend = Time.time; }
        public void Restore(float amount) { Current = Mathf.Min(max, Current + amount); }
    }
}
