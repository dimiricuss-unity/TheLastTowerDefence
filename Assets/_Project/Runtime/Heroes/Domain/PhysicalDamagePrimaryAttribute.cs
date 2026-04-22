namespace TheLastTowerDefence.Heroes.Domain
{
    /// <summary>
    /// Какая базовая характеристика суммируется с уроном оружия в формулах мин/макс физического урона
    /// (<see cref="TheLastTowerDefence.Formulas.CharacterStatFormulas.ComputeMinPhysicalDamage"/> и Max).
    /// К ловкости по-прежнему добавляется <c>INT(Ловкость/2)</c> (в т.ч. когда основной стат — ловкость).
    /// </summary>
    public enum PhysicalDamagePrimaryAttribute
    {
        Strength = 0,
        Intelligence = 1,
        Dexterity = 2,
    }
}
