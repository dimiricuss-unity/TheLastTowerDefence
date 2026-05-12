using UnityEngine;
using UnityEngine.UI;
using TheLastTowerDefence.Heroes.Systems;
using TheLastTowerDefence.Inventory.Systems;

namespace TheLastTowerDefence.UI
{
    [DisallowMultipleComponent]
    public sealed class CharacterWindowsController : MonoBehaviour
    {
        [Header("Open/Close")]
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private GameObject characterWindowsParent;

        [Header("Character Buttons")]
        [SerializeField] private Button warriorButton;
        [SerializeField] private Button archerButton;
        [SerializeField] private Button clericButton;

        [Header("Character Windows")]
        [SerializeField] private GameObject warriorWindow;
        [SerializeField] private GameObject archerWindow;
        [SerializeField] private GameObject clericWindow;

        /// <summary>
        /// Окно персонажа, которое сейчас активно в иерархии (вкладки/инвентарь общие).
        /// </summary>
        public GameObject GetActiveCharacterWindow()
        {
            if (warriorWindow != null && warriorWindow.activeInHierarchy)
            {
                return warriorWindow;
            }

            if (archerWindow != null && archerWindow.activeInHierarchy)
            {
                return archerWindow;
            }

            if (clericWindow != null && clericWindow.activeInHierarchy)
            {
                return clericWindow;
            }

            return null;
        }

        private void Awake()
        {
            if (inventoryButton != null)
            {
                inventoryButton.onClick.AddListener(OpenCharacterWindows);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(CloseCharacterWindows);
            }

            if (warriorButton != null)
            {
                warriorButton.onClick.AddListener(ShowWarriorWindow);
            }

            if (archerButton != null)
            {
                archerButton.onClick.AddListener(ShowArcherWindow);
            }

            if (clericButton != null)
            {
                clericButton.onClick.AddListener(ShowClericWindow);
            }

            // Keep parent closed on startup.
            SetParentVisible(false);
            SetActiveCharacterWindow(warriorWindow);
        }

        private void OnDestroy()
        {
            if (inventoryButton != null)
            {
                inventoryButton.onClick.RemoveListener(OpenCharacterWindows);
            }

            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(CloseCharacterWindows);
            }

            if (warriorButton != null)
            {
                warriorButton.onClick.RemoveListener(ShowWarriorWindow);
            }

            if (archerButton != null)
            {
                archerButton.onClick.RemoveListener(ShowArcherWindow);
            }

            if (clericButton != null)
            {
                clericButton.onClick.RemoveListener(ShowClericWindow);
            }
        }

        private void OpenCharacterWindows()
        {
            SetParentVisible(true);
            SetActiveCharacterWindow(warriorWindow);
        }

        private void CloseCharacterWindows()
        {
            var popup = FindFirstObjectByType<InventoryItemPopupController>(FindObjectsInactive.Include);
            if (popup != null)
            {
                popup.Hide();
            }

            var grid = FindFirstObjectByType<InventoryGridView>(FindObjectsInactive.Include);
            var wallet = FindFirstObjectByType<PlayerGoldWallet>(FindObjectsInactive.Include);
            if (grid != null)
            {
                var gold = grid.SellUnequippedGridItemsAndReturnGold();
                if (gold > 0 && wallet != null)
                {
                    wallet.AddGold(gold);
                }
                else if (gold > 0 && wallet == null)
                {
                    Debug.LogWarning(
                        $"[{nameof(CharacterWindowsController)}] Продано предметов на {gold} золота, но на сцене нет {nameof(PlayerGoldWallet)} — баланс не обновлён.",
                        this);
                }
            }

            var heroes = FindObjectsByType<CharacterHeroStats>(FindObjectsSortMode.None);
            for (var i = 0; i < heroes.Length; i++)
            {
                if (heroes[i] != null)
                {
                    heroes[i].CommitSkillAllocationSession();
                }
            }

            CharacterWindowTabsSharedState.ResetToInventory();
            SetParentVisible(false);
        }

        private void ShowWarriorWindow()
        {
            SetActiveCharacterWindow(warriorWindow);
        }

        private void ShowArcherWindow()
        {
            SetActiveCharacterWindow(archerWindow);
        }

        private void ShowClericWindow()
        {
            SetActiveCharacterWindow(clericWindow);
        }

        private void SetParentVisible(bool visible)
        {
            if (characterWindowsParent != null)
            {
                characterWindowsParent.SetActive(visible);
            }
        }

        private void SetActiveCharacterWindow(GameObject activeWindow)
        {
            // Deactivate all first so the previously active window's OnDisable runs before
            // the new window's OnEnable (avoids shared Inventory being hidden after shown).
            SetWindowActive(warriorWindow, false);
            SetWindowActive(archerWindow, false);
            SetWindowActive(clericWindow, false);

            if (activeWindow == warriorWindow)
            {
                SetWindowActive(warriorWindow, true);
            }
            else if (activeWindow == archerWindow)
            {
                SetWindowActive(archerWindow, true);
            }
            else if (activeWindow == clericWindow)
            {
                SetWindowActive(clericWindow, true);
            }
        }

        private static void SetWindowActive(GameObject window, bool active)
        {
            if (window != null)
            {
                window.SetActive(active);
            }
        }
    }
}
