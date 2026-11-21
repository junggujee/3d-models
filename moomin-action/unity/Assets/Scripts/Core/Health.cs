using System;
using UnityEngine;

namespace MoominAction
{
    public interface IDamageable { void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockback); bool IsDead { get; } }

    /// <summary>체력·무적(구르기 등)·회복·사망 이벤트. 플레이어와 적 모두 사용.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f;
        public float invulnerableTime = 0.25f;
        public bool Invulnerable;                 // 구르기 무적 프레임 동안 true
        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public event Action<float, Vector3, Vector3> Damaged;   // (amount, hitPoint, knockback)
        public event Action<float> Healed; public event Action Died;
        float lastHit = -99f;

        void Awake() { Current = maxHealth; }
        public void ResetHealth() { Current = maxHealth; IsDead = false; Invulnerable = false; }
        public void Heal(float amount) { if (IsDead || amount <= 0) return; float before = Current; Current = Mathf.Min(maxHealth, Current + amount); if (Current > before) Healed?.Invoke(Current - before); }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockback)
        {
            if (IsDead || Invulnerable || Time.time - lastHit < invulnerableTime) return;
            lastHit = Time.time; Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(amount, hitPoint, knockback);
            if (Current <= 0) { IsDead = true; Died?.Invoke(); }
        }
    }
}
