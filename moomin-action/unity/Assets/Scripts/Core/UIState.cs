using System;
using UnityEngine;

namespace MoominAction
{
    /// <summary>메뉴가 열려 있는지(=게임 일시정지). 플레이어·카메라·전투 입력이 이 값을 확인해 멈춤.</summary>
    public static class UIState
    {
        public static bool Paused { get; private set; }
        public static event Action<bool> PausedChanged;
        public static void SetPaused(bool p)
        {
            if (Paused == p) return; Paused = p; Time.timeScale = p ? 0f : 1f;
            Cursor.lockState = p ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = p; PausedChanged?.Invoke(p);
        }
        public static void Reset() { Paused = false; Time.timeScale = 1f; }
    }
}
