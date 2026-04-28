using System.Collections.Generic;
using System.Globalization;
using System;
using System.Text;
using TMPro;
using TheLastTowerDefence.Inventory.Domain;
using TheLastTowerDefence.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TheLastTowerDefence.Inventory.Systems
{
    [DisallowMultipleComponent]
    public sealed class InventoryItemPopupController : MonoBehaviour
    {
        [Header("Popup roots")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private RectTransform rightInfoPopup;
        [SerializeField] private RectTransform leftInfoPopup;
        [SerializeField] private GameObject background;

        [Header("Content")]
        [SerializeField] private TMP_Text rightInformText;
        [SerializeField] private TMP_Text leftInformText;

        [Header("Buttons")]
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button closeButton;

        private Button _backgroundCloseButton;
        private InventoryItemView _currentItem;
        private EquipmentSlotCellView _currentSourceSlot;

        private void Awake()
        {
            BindMissingReferences();
            BindListeners();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        public void ShowForItem(InventoryItemView itemView)
        {
            BindMissingReferences();
            if (itemView == null || itemView.Config == null)
            {
                return;
            }

            _currentItem = itemView;
            _currentSourceSlot = itemView.GetComponentInParent<EquipmentSlotCellView>();

            var clickedFromSlot = _currentSourceSlot != null && _currentSourceSlot.EquippedItem == itemView;
            var equippedForCompare = clickedFromSlot ? itemView : FindEquippedForSameSlot(itemView.Config);

            SetRightPanelState(!clickedFromSlot, itemView.Config);
            SetLeftPanelState(equippedForCompare != null, equippedForCompare != null ? equippedForCompare.Config : null);
            SetActionButtonsState(clickedFromSlot, equippedForCompare != null);

            if (popupRoot != null)
            {
                popupRoot.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            _currentItem = null;
            _currentSourceSlot = null;
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnEquipPressed()
        {
            if (_currentItem == null || _currentItem.Config == null)
            {
                Hide();
                return;
            }

            if (TryEquipToActiveCharacterSlot(_currentItem))
            {
                Hide();
            }
        }

        private void OnUnequipPressed()
        {
            if (_currentSourceSlot == null || _currentItem == null)
            {
                Hide();
                return;
            }

            if (_currentSourceSlot.EquippedItem == _currentItem && _currentSourceSlot.TryUnequipToInventory())
            {
                Hide();
            }
        }

        private void OnClosePressed()
        {
            Hide();
        }

        private void BindListeners()
        {
            if (equipButton != null)
            {
                equipButton.onClick.RemoveListener(OnEquipPressed);
                equipButton.onClick.AddListener(OnEquipPressed);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnClosePressed);
                closeButton.onClick.AddListener(OnClosePressed);
            }

            if (unequipButton != null)
            {
                unequipButton.onClick.RemoveListener(OnUnequipPressed);
                unequipButton.onClick.AddListener(OnUnequipPressed);
            }

            if (_backgroundCloseButton != null)
            {
                _backgroundCloseButton.onClick.RemoveListener(OnClosePressed);
                _backgroundCloseButton.onClick.AddListener(OnClosePressed);
            }
        }

        private void UnbindListeners()
        {
            if (equipButton != null)
            {
                equipButton.onClick.RemoveListener(OnEquipPressed);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(OnClosePressed);
            }

            if (unequipButton != null)
            {
                unequipButton.onClick.RemoveListener(OnUnequipPressed);
            }

            if (_backgroundCloseButton != null)
            {
                _backgroundCloseButton.onClick.RemoveListener(OnClosePressed);
            }
        }

        private void BindMissingReferences()
        {
            if (popupRoot == null)
            {
                popupRoot = gameObject;
            }

            if (background == null)
            {
                var backgroundTransform = transform.Find("Background");
                if (backgroundTransform != null)
                {
                    background = backgroundTransform.gameObject;
                }
            }

            if (rightInfoPopup == null)
            {
                var rightTransform = transform.Find("RightInfoPopUp");
                if (rightTransform != null)
                {
                    rightInfoPopup = rightTransform as RectTransform;
                }
            }

            if (leftInfoPopup == null)
            {
                var leftTransform = transform.Find("LeftInfoPopUp");
                if (leftTransform != null)
                {
                    leftInfoPopup = leftTransform as RectTransform;
                }
            }

            if (rightInformText == null && rightInfoPopup != null)
            {
                var textTransform = rightInfoPopup.Find("InformText");
                if (textTransform != null)
                {
                    rightInformText = textTransform.GetComponent<TMP_Text>();
                }
            }

            if (leftInformText == null && leftInfoPopup != null)
            {
                var textTransform = leftInfoPopup.Find("InformText");
                if (textTransform != null)
                {
                    leftInformText = textTransform.GetComponent<TMP_Text>();
                }
            }

            if (equipButton == null && rightInfoPopup != null)
            {
                var equipTransform = rightInfoPopup.Find("Equip");
                if (equipTransform != null)
                {
                    equipButton = equipTransform.GetComponent<Button>();
                }
            }

            if (closeButton == null && rightInfoPopup != null)
            {
                var closeTransform = rightInfoPopup.Find("Close");
                if (closeTransform != null)
                {
                    closeButton = closeTransform.GetComponent<Button>();
                }
            }

            if (unequipButton == null && leftInfoPopup != null)
            {
                var unequipTransform = leftInfoPopup.Find("Unequip");
                if (unequipTransform != null)
                {
                    unequipButton = unequipTransform.GetComponent<Button>();
                }
            }

            _backgroundCloseButton = null;
            if (background != null)
            {
                _backgroundCloseButton = background.GetComponent<Button>();
                if (_backgroundCloseButton == null)
                {
                    _backgroundCloseButton = background.AddComponent<Button>();
                    _backgroundCloseButton.transition = Selectable.Transition.None;
                }
            }
        }

        private void SetRightPanelState(bool visible, InventoryItemConfig config)
        {
            if (rightInfoPopup != null)
            {
                rightInfoPopup.gameObject.SetActive(visible);
            }

            if (rightInformText != null)
            {
                rightInformText.text = visible ? BuildInfoText(config) : string.Empty;
            }
        }

        private void SetLeftPanelState(bool visible, InventoryItemConfig config)
        {
            if (leftInfoPopup != null)
            {
                leftInfoPopup.gameObject.SetActive(visible);
            }

            if (leftInformText != null)
            {
                leftInformText.text = visible ? BuildInfoText(config) : string.Empty;
            }
        }

        private void SetActionButtonsState(bool clickedFromSlot, bool hasComparisonItem)
        {
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(!clickedFromSlot);
            }

            if (unequipButton != null)
            {
                unequipButton.gameObject.SetActive(clickedFromSlot && hasComparisonItem);
            }
        }

        private static bool TryEquipToActiveCharacterSlot(InventoryItemView itemView)
        {
            var windows = UnityEngine.Object.FindFirstObjectByType<CharacterWindowsController>();
            if (windows == null)
            {
                return false;
            }

            var activeRoot = windows.GetActiveCharacterWindow();
            if (activeRoot == null || !activeRoot.activeInHierarchy)
            {
                return false;
            }

            var slots = activeRoot.GetComponentsInChildren<EquipmentSlotCellView>(true);
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || !slot.CanEquip(itemView.Config) || slot.EquippedItem != null)
                {
                    continue;
                }

                if (slot.TryEquipItem(itemView))
                {
                    return true;
                }
            }

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.CanEquip(itemView.Config) && slot.TryEquipItem(itemView))
                {
                    return true;
                }
            }

            return false;
        }

        private static InventoryItemView FindEquippedForSameSlot(InventoryItemConfig itemConfig)
        {
            if (itemConfig == null)
            {
                return null;
            }

            var windows = UnityEngine.Object.FindFirstObjectByType<CharacterWindowsController>();
            if (windows == null)
            {
                return null;
            }

            var activeRoot = windows.GetActiveCharacterWindow();
            if (activeRoot == null || !activeRoot.activeInHierarchy)
            {
                return null;
            }

            var slots = activeRoot.GetComponentsInChildren<EquipmentSlotCellView>(true);
            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || !slot.CanEquip(itemConfig))
                {
                    continue;
                }

                if (slot.EquippedItem != null)
                {
                    return slot.EquippedItem;
                }
            }

            return null;
        }

        private static string BuildInfoText(InventoryItemConfig config)
        {
            if (config == null)
            {
                return string.Empty;
            }

            var lines = new List<string>(24);
            AddHeaderLines(lines, config);

            var stats = config.statsConfig;
            if (stats != null)
            {
                AddFlooredFloatLine(lines, "Мин. урон", stats.weaponBaseMinDamage);
                AddFlooredFloatLine(lines, "Макс. урон", stats.weaponBaseMaxDamage);
                AddAttackSpeedLine(lines, "Скорость атаки", stats.weaponBaseAttacksPerSecond);
                AddFlooredFloatLine(lines, "Модификатор крита", stats.weaponBaseCritModifier);
                AddNumberLine(lines, "Броня", stats.weaponArmorRating);

                AddNumberLine(lines, "Сила", stats.bonusStrength, true);
                AddNumberLine(lines, "Ловкость", stats.bonusDexterity, true);
                AddNumberLine(lines, "Выносливость", stats.bonusStamina, true);
                AddNumberLine(lines, "Интеллект", stats.bonusIntelligence, true);
                AddNumberLine(lines, "Сила воли", stats.bonusWillpower, true);
                AddNumberLine(lines, "Удача", stats.bonusLuck, true);

                AddFlooredFloatLine(lines, "Макс. HP", stats.bonusMaxHp);
                AddFloatLine(lines, "Восст. HP/сек", stats.bonusHpRegenPerSecond, "0.0");
                AddFlooredFloatLine(lines, "Макс. мана", stats.bonusMaxMana);
                AddFlooredFloatLine(lines, "Реген маны/сек", stats.bonusManaRegenPerSecond);
                AddFlooredFloatLine(lines, "Критический урон", stats.bonusCriticalDamage);

                AddNumberLine(lines, "Цена", stats.price);
                AddFlooredFloatLine(lines, "Шанс дропа (%)", stats.dropChancePercent);
            }

            if (lines.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(lines.Count * 20);
            for (var i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append('\n');
                }

                sb.Append(lines[i]);
            }

            return sb.ToString();
        }

        private static void AddHeaderLines(List<string> lines, InventoryItemConfig config)
        {
            var displayName = string.IsNullOrWhiteSpace(config.displayName) ? config.name : config.displayName;
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                lines.Add(displayName.Trim());
            }

            if (!string.IsNullOrWhiteSpace(config.description))
            {
                lines.Add(config.description.Trim());
            }

            if (lines.Count > 0 && config.statsConfig != null)
            {
                lines.Add(string.Empty);
            }
        }

        private static void AddFlooredFloatLine(List<string> lines, string statName, float value)
        {
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            var roundedDown = Mathf.Floor(value);
            if (Mathf.Approximately(roundedDown, 0f))
            {
                return;
            }

            lines.Add(string.Concat(statName, ": ", roundedDown.ToString("0", CultureInfo.InvariantCulture)));
        }

        private static void AddAttackSpeedLine(List<string> lines, string statName, float value)
        {
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            var roundedDownToTenths = Mathf.Floor(value * 10f) / 10f;
            if (roundedDownToTenths <= 0f)
            {
                return;
            }

            lines.Add(string.Concat(statName, ": ", roundedDownToTenths.ToString("0.0", CultureInfo.InvariantCulture)));
        }

        private static void AddFloatLine(List<string> lines, string statName, float value, string format)
        {
            if (Mathf.Approximately(value, 0f))
            {
                return;
            }

            lines.Add(string.Concat(statName, ": ", value.ToString(format, CultureInfo.InvariantCulture)));
        }

        private static void AddNumberLine(List<string> lines, string statName, int value, bool withPlusSign = false)
        {
            if (value == 0)
            {
                return;
            }

            var valueText = value.ToString(CultureInfo.InvariantCulture);
            if (withPlusSign && value > 0)
            {
                valueText = string.Concat("+", valueText);
            }

            lines.Add(string.Concat(statName, ": ", valueText));
        }
    }
}
