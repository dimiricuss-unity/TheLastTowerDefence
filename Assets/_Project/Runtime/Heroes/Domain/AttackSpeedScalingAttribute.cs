namespace TheLastTowerDefence.Heroes.Domain
{
    /// <summary>
    /// Какая базовая характеристика входит в множитель APS: <c>1 + 0.03×стат</c>
    /// (<see cref="TheLastTowerDefence.Formulas.CharacterStatFormulas.ComputeAttacksPerSecond"/>).
    /// </summary>
    public enum AttackSpeedScalingAttribute
    {
        Dexterity = 0,
        Willpower = 1,
    }
}
