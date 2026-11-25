using UnityEngine;

namespace MoominAction
{
    /// <summary>생일 파티 손님(조랭하프족 Npc 에 붙음): 몇 초마다 웃기 · 손 흔들기 · 끄덕이기(대화 중이 아닐 때).</summary>
    public class PartyGuest : MonoBehaviour
    {
        static readonly string[] Moves = { "Laugh", "Wave", "Yes", "Laugh", "QuestComplete" };
        Npc npc; float next;
        void Awake() { npc = GetComponent<Npc>(); next = Time.time + Random.Range(1f, 4f); }
        void Update()
        {
            if (UIState.Paused || npc == null || npc.Talking || Time.time < next) return;
            next = Time.time + Random.Range(3.5f, 7f); npc.Play(Moves[Random.Range(0, Moves.Length)]);
        }
    }
}
