using UnityEngine;

namespace MoominAction
{
    /// <summary>플레이어 피격/사망 연출(애니메이션 Hit, Death, 입력 잠금).</summary>
    [RequireComponent(typeof(Health))]
    public class PlayerHealth : MonoBehaviour
    {
        public Animator animator; Health hp; PlayerController pc;
        void Awake() { hp = GetComponent<Health>(); pc = GetComponent<PlayerController>(); if (!animator) animator = GetComponentInChildren<Animator>(); hp.Damaged += OnDamaged; hp.Died += OnDied; }
        void OnDamaged(float a, Vector3 p, Vector3 k) { if (hp.IsDead) return; if (animator) animator.CrossFadeInFixedTime("Hit", 0.04f); pc?.Knockback(k); StopAllCoroutines(); StartCoroutine(Stun(0.4f)); }
        System.Collections.IEnumerator Stun(float s) { if (pc) pc.locked = true; yield return new WaitForSeconds(s); if (pc && !hp.IsDead) pc.locked = false; }
        void OnDied() { if (pc) pc.locked = true; if (animator) animator.CrossFadeInFixedTime("Death", 0.05f); }
    }
}
