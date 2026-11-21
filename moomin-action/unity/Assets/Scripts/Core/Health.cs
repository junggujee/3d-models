using System;
using UnityEngine;

namespace MoominAction
{
    public interface IDamageable { void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockback); bool IsDead { get; } }

    /// <summary>체력·무적 시간·사망 이벤트. 플레이어와 적 모두 사용.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        public float maxHealth = 100f;
        public float invulnerableTime = 0.25f;
        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public event Action<float, Vector3, Vector3> Damaged;   // (amount, hitPoint, knockback)
        public event Action Died;
        float lastHit = -99f;

        void Awake() { Current = maxHealth; }
        public void ResetHealth() { Current = maxHealth; IsDead = false; }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 knockback)
        {
            if (IsDead || Time.time - lastHit < invulnerableTime) return;
            lastHit = Time.time; Current = Mathf.Max(0, Current - amount);
            Damaged?.Invoke(amount, hitPoint, knockback);
            if (Current <= 0) { IsDead = true; Died?.Invoke(); }
        }
    }
}
