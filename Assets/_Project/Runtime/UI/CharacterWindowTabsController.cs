using UnityEngine;
using UnityEngine.UI;

namespace TheLastTowerDefence.UI
{
    [DisallowMultipleComponent]
    public sealed class CharacterWindowTabsController : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private GameObject inventoryTab;
        [SerializeField] private GameObject characteristicsTab;
        [SerializeField] private GameObject parametersTab;
        [SerializeField] private GameObject infoTab;

        [Header("Tab Buttons")]
        [SerializeField] private Button inventoryButton;
        [SerializeField] private Button characterButton;
        [SerializeField] private Button paramsButton;
        [SerializeField] private Button infoButton;

        [Header("Shared UI")]
        [Tooltip("Общий инвентарь справа (родительский объект CharacterWindows/Inventory). Показывается только на вкладке Inventory.")]
        [SerializeField] private GameObject sharedInventoryRoot;

        private bool _listenersBound;

        private void Awake()
        {
            AutoAssignMissingReferences();
            BindListeners();
        }

        private void OnEnable()
        {
            ApplySharedSelectedTab();
        }

        private void OnDisable()
        {
            HideSharedInventory();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        private void OnValidate()
        {
            AutoAssignMissingReferences();
        }

        private void BindListeners()
        {
            if (_listenersBound)
            {
                return;
            }

            if (inventoryButton != null)
            {
                inventoryButton.onClick.AddListener(ShowInventoryTab);
            }

            if (characterButton != null)
            {
                characterButton.onClick.AddListener(ShowCharacteristicsTab);
            }

            if (paramsButton != null)
            {
                paramsButton.onClick.AddListener(ShowParametersTab);
            }

            if (infoButton != null)
            {
                infoButton.onClick.AddListener(ShowInfoTab);
            }

            _listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!_listenersBound)
            {
                return;
            }

            if (inventoryButton != null)
            {
                inventoryButton.onClick.RemoveListener(ShowInventoryTab);
            }

            if (characterButton != null)
            {
                characterButton.onClick.RemoveListener(ShowCharacteristicsTab);
            }

            if (paramsButton != null)
            {
                paramsButton.onClick.RemoveListener(ShowParametersTab);
            }

            if (infoButton != null)
            {
                infoButton.onClick.RemoveListener(ShowInfoTab);
            }

            _listenersBound = false;
        }

        private void ShowInventoryTab()
        {
            CharacterWindowTabsSharedState.SetSelected(CharacterWindowTabKind.Inventory);
            SetActiveTab(inventoryTab);
        }

        private void ShowCharacteristicsTab()
        {
            CharacterWindowTabsSharedState.SetSelected(CharacterWindowTabKind.Characteristics);
            SetActiveTab(characteristicsTab);
        }

        private void ShowParametersTab()
        {
            CharacterWindowTabsSharedState.SetSelected(CharacterWindowTabKind.Parameters);
            SetActiveTab(parametersTab);
        }

        private void ShowInfoTab()
        {
            CharacterWindowTabsSharedState.SetSelected(CharacterWindowTabKind.Info);
            SetActiveTab(infoTab);
        }

        private void ApplySharedSelectedTab()
        {
            switch (CharacterWindowTabsSharedState.LastSelected)
            {
                case CharacterWindowTabKind.Inventory:
                    SetActiveTab(inventoryTab);
                    break;
                case CharacterWindowTabKind.Characteristics:
                    SetActiveTab(characteristicsTab);
                    break;
                case CharacterWindowTabKind.Parameters:
                    SetActiveTab(parametersTab);
                    break;
                case CharacterWindowTabKind.Info:
                    SetActiveTab(infoTab);
                    break;
            }
        }

        private void SetActiveTab(GameObject activeTab)
        {
            SetTabActive(inventoryTab, activeTab == inventoryTab);
            SetTabActive(characteristicsTab, activeTab == characteristicsTab);
            SetTabActive(parametersTab, activeTab == parametersTab);
            SetTabActive(infoTab, activeTab == infoTab);
            UpdateSharedInventoryVisibility(activeTab);
        }

        private void AutoAssignMissingReferences()
        {
            inventoryTab = FindChildIfMissing(inventoryTab, "InventoryTab");
            characteristicsTab = FindChildIfMissing(characteristicsTab, "CharacteristicsTab");
            parametersTab = FindChildIfMissing(parametersTab, "ParametersTab");
            infoTab = FindChildIfMissing(infoTab, "InfoTab");

            inventoryButton = FindButtonIfMissing(inventoryButton, "InventoryButton");
            characterButton = FindButtonIfMissing(characterButton, "CharacterButton");
            paramsButton = FindButtonIfMissing(paramsButton, "ParamsButton");
            infoButton = FindButtonIfMissing(infoButton, "InfoButton");

            sharedInventoryRoot = FindSharedInventoryIfMissing(sharedInventoryRoot);
        }

        private void UpdateSharedInventoryVisibility(GameObject activeTab)
        {
            sharedInventoryRoot = FindSharedInventoryIfMissing(sharedInventoryRoot);
            if (sharedInventoryRoot == null)
            {
                return;
            }

            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            var show = activeTab == inventoryTab;
            sharedInventoryRoot.SetActive(show);
        }

        private void HideSharedInventory()
        {
            sharedInventoryRoot = FindSharedInventoryIfMissing(sharedInventoryRoot);
            if (sharedInventoryRoot == null)
            {
                return;
            }

            sharedInventoryRoot.SetActive(false);
        }

        private GameObject FindSharedInventoryIfMissing(GameObject current)
        {
            if (current != null)
            {
                return current;
            }

            var characterWindows = transform.parent;
            if (characterWindows == null)
            {
                return null;
            }

            var inv = characterWindows.Find("Inventory");
            return inv != null ? inv.gameObject : null;
        }

        private GameObject FindChildIfMissing(GameObject current, string childName)
        {
            if (current != null)
            {
                return current;
            }

            Transform child = transform.Find(childName);
            return child != null ? child.gameObject : null;
        }

        private Button FindButtonIfMissing(Button current, string childName)
        {
            if (current != null)
            {
                return current;
            }

            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static void SetTabActive(GameObject tab, bool isActive)
        {
            if (tab != null)
            {
                tab.SetActive(isActive);
            }
        }
    }
}
