using UnityEngine;

namespace MoominAction
{
    /// <summary>플레이어 피격 → 짧은 경직(스킬·회피 취소, Hit 애니메이션), 사망 → Death 애니메이션.</summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        public float hitStagger = 0.35f; public Animator animator; Health hp; PlayerController pc; CombatManager cm;
        void Awake() { hp = GetComponent<Health>(); pc = GetComponent<PlayerController>(); cm = GetComponent<CombatManager>(); if (!animator) animator = GetComponentInChildren<Animator>(); hp.Damaged += OnDamaged; hp.Died += OnDied; }
        void OnDamaged(float a, Vector3 p, Vector3 k) { if (hp.IsDead) return; pc?.Knockback(k); if (cm != null) cm.Stagger(hitStagger); else if (animator) animator.CrossFadeInFixedTime("Hit", 0.04f); }
        void OnDied() { pc?.CancelRoll(); if (animator) animator.CrossFadeInFixedTime("Death", 0.05f); }
    }
}
