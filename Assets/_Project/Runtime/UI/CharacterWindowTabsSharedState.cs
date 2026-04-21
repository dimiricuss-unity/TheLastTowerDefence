namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Какая вкладка выбрана в окнах героя (Warrior / Archer / Cleric). Сбрасывается при закрытии <c>CharacterWindowsParent</c>.
    /// </summary>
    public enum CharacterWindowTabKind
    {
        Inventory = 0,
        Characteristics = 1,
        Parameters = 2,
        Info = 3,
    }

    public static class CharacterWindowTabsSharedState
    {
        static CharacterWindowTabKind _lastSelected = CharacterWindowTabKind.Inventory;

        public static CharacterWindowTabKind LastSelected => _lastSelected;

        public static void SetSelected(CharacterWindowTabKind tab) => _lastSelected = tab;

        public static void ResetToInventory() => _lastSelected = CharacterWindowTabKind.Inventory;
    }
}
