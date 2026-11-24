using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>
    /// F 상호작용 공용 창구(펫·NPC·앞으로의 상자·문 등). 대상은 매 프레임 Update 에서 Offer(거리, 안내 글자, 누르면 할 일)를 내고,
    /// 같은 프레임에서 가장 가까운 하나만 화면 아래 안내(Prompt)로 보이고 F 입력을 받음(InteractionRunner.LateUpdate 가 결정).
    /// onPress 가 null 이면 안내만(예: "이미 몽실이와 함께하고 있다").
    /// </summary>
    public static class Interaction
    {
        static int frame = -1; static float best; static string text; static Action act;
        /// <summary>지금 보이는 안내(없으면 null) — PetHUD 가 화면 아래 가운데에 그림.</summary>
        public static string Prompt { get; private set; }
        /// <summary>지금 안내 중인 대상(머리 위 표시 등에 씀).</summary>
        public static object Target { get; private set; }
        static object owner;

        public static void Offer(object who, float distance, string prompt, Action onPress)
        {
            if (frame != Time.frameCount) { frame = Time.frameCount; best = float.MaxValue; text = null; act = null; owner = null; }
            if (distance >= best) return;
            best = distance; text = prompt; act = onPress; owner = who;
        }

        /// <summary>한 프레임의 마지막(모든 Update 뒤)에 한 번.</summary>
        internal static void Resolve()
        {
            if (frame != Time.frameCount || UIState.Paused) { Prompt = null; Target = null; return; }
            Prompt = text; Target = owner;
            if (act != null && InputAdapter.InteractPressed()) { var a = act; act = null; a(); }
        }
        public static void Reset() { frame = -1; Prompt = null; Target = null; act = null; owner = null; }
    }

    /// <summary>Interaction 을 프레임 끝에 정리(GameManager 가 붙임).</summary>
    [DefaultExecutionOrder(1000)]
    public class InteractionRunner : MonoBehaviour
    {
        void LateUpdate() => Interaction.Resolve();
        void OnDestroy() => Interaction.Reset();
    }
}
