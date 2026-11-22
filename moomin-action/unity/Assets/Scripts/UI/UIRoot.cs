using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MoominAction.UI
{
    /// <summary>
    /// UI 총괄: 캔버스·EventSystem 생성, HUD와 메뉴(휴식·가방·기록 남기기/불러오기·설정·조작법) 관리, Esc/I/1~4 입력 라우팅.
    /// 메뉴가 열리면 게임이 멈추고(UIState.Paused) 마우스 커서가 나타남. GameManager 가 자동으로 만들어 줌.
    /// </summary>
    public class UIRoot : MonoBehaviour, IMenuHost
    {
        public CharacterStats player; Canvas canvas; HUD hud; Inventory inventory;
        public WorldMapView worldMap; public PauseMenu pausePage; public SettingsMenu settingsPage; public InventoryMenu inventoryPage; public InfoPage controlsPage; public SaveSlotPage savePage, loadPage; public EquipmentMenu equipmentPage;
        readonly Stack<MenuPage> stack = new(); MenuPage Current => stack.Count > 0 ? stack.Peek() : null;

        void Awake()
        {
            GameSettings.Load();
            canvas = UIFactory.MakeCanvas(transform, "UICanvas", 50);
        }

        void Start()
        {
            if (player == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p) player = p.GetComponent<CharacterStats>(); }
            if (player == null) { Debug.LogError("[UIRoot] Player 태그 오브젝트(CharacterStats 포함)를 찾지 못했습니다."); enabled = false; return; }
            inventory = player.GetComponent<Inventory>(); if (inventory == null) inventory = player.gameObject.AddComponent<Inventory>();
            hud = new HUD(); hud.Build(this, canvas.transform);
            pausePage = new PauseMenu(); settingsPage = new SettingsMenu(); inventoryPage = new InventoryMenu();
            controlsPage = new InfoPage("조작법", new[] { "W A S D / Shift", "Space", "좌클릭", "우클릭", "Q / E / R", "Tab", "I  ·  1~4", "C", "M", "Esc" }, new[] { "이동 / 달리기", "회피 — 회피 코인으로 판정", "일반 공격 (코인 1~2개, 3연타)", "방어 — 피해 감소, 누른 직후는 패링", "연속 베기 · 불꽃 찌르기 · 별빛 난무", "타깃 고정 / 해제", "가방 · 퀵슬롯 사용", "장비(머리·가슴·다리·발·아티팩트)", "지도 펼치기 · 접기", "휴식 메뉴" });
            savePage = new SaveSlotPage(SaveSlotPage.Mode.Save, UITheme.InGame, false); loadPage = new SaveSlotPage(SaveSlotPage.Mode.Load, UITheme.InGame, true);
            equipmentPage = new EquipmentMenu();
            foreach (var pg in new MenuPage[] { pausePage, settingsPage, inventoryPage, controlsPage, savePage, loadPage, equipmentPage }) pg.Build(this, canvas.transform);
            worldMap = new WorldMapView(); worldMap.Build(canvas.transform, player.transform);   // M: 지도 펼치기·접기
            inventory.Message += m => { };   // HUD 가 이미 구독
        }

        void OnDestroy() { hud?.Dispose(); UIState.Reset(); }

        void Update()
        {
            if (hud == null) return;
            hud.Update();
            if (SceneFader.Busy) return;   // 장면을 바꾸는 중에는 입력 무시
            var gm = GameManager.Instance; if (gm != null && gm.IsOver) { if (Current != null) CloseAll(); if (worldMap.IsOpen) worldMap.Close(); worldMap.Update(); return; }
            worldMap.Update();
            if (worldMap.IsOpen) { if (InputAdapter.MapPressed() || InputAdapter.CancelPressed()) worldMap.Close(); return; }   // 지도가 열린 동안 다른 입력 막음
            if (Current == null)
            {
                if (InputAdapter.MapPressed() && worldMap.Available) { worldMap.Open(); return; }
                if (InputAdapter.PausePressed()) Open(pausePage);
                else if (InputAdapter.InventoryPressed()) Open(inventoryPage);
                else if (InputAdapter.EquipPressed()) Open(equipmentPage);
                else { int q = InputAdapter.QuickPressed(); if (q >= 0) inventory.UseQuick(q); }
            }
            else
            {
                Current.Tick();
                if (InputAdapter.CancelPressed() || (Current == inventoryPage && InputAdapter.InventoryPressed()) || (Current == equipmentPage && InputAdapter.EquipPressed())) Back();
                else if (Current == inventoryPage && InputAdapter.EquipPressed()) Open(equipmentPage);
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
        /// <summary>타이틀 화면으로(저장하지 않은 진행은 사라짐 — 자동 저장은 웨이브마다 남아 있음).</summary>
        public void ToTitle() { GameSettings.Save(); SaveSystem.ToTitle(); }
        public void QuitGame() { inventory?.Persist(); GameSettings.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
