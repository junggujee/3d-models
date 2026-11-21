using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>
    /// UI 총괄: 캔버스·EventSystem 생성, HUD와 메뉴(휴식·가방·설정·조작법) 관리, Esc/I/1~4 입력 라우팅.
    /// 메뉴가 열리면 게임이 멈추고(UIState.Paused) 마우스 커서가 나타남. GameManager 가 자동으로 만들어 줌.
    /// </summary>
    public class UIRoot : MonoBehaviour
    {
        public Health player; Canvas canvas; HUD hud; Inventory inventory;
        public PauseMenu pausePage; public SettingsMenu settingsPage; public InventoryMenu inventoryPage; public InfoPage controlsPage;
        readonly Stack<MenuPage> stack = new(); MenuPage Current => stack.Count > 0 ? stack.Peek() : null;

        void Awake()
        {
            GameSettings.Load();
            var cgo = new GameObject("UICanvas"); cgo.transform.SetParent(transform, false); canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var cs = cgo.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 0.5f; cgo.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        void Start()
        {
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.GetComponent<Health>(); }
            if (player == null) { Debug.LogError("[UIRoot] Player 태그 오브젝트(Health 포함)를 찾지 못했습니다."); enabled = false; return; }
            inventory = player.GetComponent<Inventory>(); if (inventory == null) inventory = player.gameObject.AddComponent<Inventory>();
            hud = new HUD(); hud.Build(this, canvas.transform);
            pausePage = new PauseMenu(); settingsPage = new SettingsMenu(); inventoryPage = new InventoryMenu();
            controlsPage = new InfoPage("조작법", new[] { "W A S D / Shift", "Space", "좌클릭", "우클릭", "Q / E / R", "Tab", "I  ·  1~4", "Esc" }, new[] { "이동 / 달리기", "회피 — 짧은 무적", "일반 공격 (코인 1~2개, 3연타)", "방어 — 누른 직후는 패링", "연속 베기 · 불꽃 찌르기 · 별빛 난무", "타깃 고정 / 해제", "가방 · 퀵슬롯 사용", "휴식 메뉴" });
            foreach (var pg in new MenuPage[] { pausePage, settingsPage, inventoryPage, controlsPage }) pg.Build(this, canvas.transform);
            inventory.Message += m => { };   // HUD 가 이미 구독
        }

        void OnDestroy() { hud?.Dispose(); UIState.Reset(); }

        void Update()
        {
            if (hud == null) return;
            hud.Update();
            var gm = GameManager.Instance; if (gm != null && gm.IsOver) { if (Current != null) CloseAll(); return; }
            if (Current == null)
            {
                if (InputAdapter.PausePressed()) Open(pausePage);
                else if (InputAdapter.InventoryPressed()) Open(inventoryPage);
                else { int q = InputAdapter.QuickPressed(); if (q >= 0) inventory.UseQuick(q); }
            }
            else
            {
                Current.Tick();
                if (InputAdapter.CancelPressed() || (Current == inventoryPage && InputAdapter.InventoryPressed())) Back();
            }
        }

        public void Open(MenuPage page) { if (Current != null) Current.Close(); stack.Push(page); page.Open(); UIState.SetPaused(true); }
        public void Back()
        {
            if (stack.Count == 0) return; var top = stack.Pop(); top.Close();
            if (stack.Count == 0) { UIState.SetPaused(false); GameSettings.Save(); inventory?.Persist(); } else stack.Peek().Open();
        }
        public void CloseAll() { while (stack.Count > 0) { var t = stack.Pop(); t.Close(); } UIState.SetPaused(false); GameSettings.Save(); inventory?.Persist(); }
        public void RestartGame() { CloseAll(); UIState.Reset(); SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public void QuitGame() { inventory?.Persist(); GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
