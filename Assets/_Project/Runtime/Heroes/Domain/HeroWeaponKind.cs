namespace TheLastTowerDefence.Heroes.Domain
{
    /// <summary>
    /// Тип базового оружия (колонка «Тип» / класс оружия в дизайне; на формулы Character_stats пока не влияет).
    /// </summary>
    public enum HeroWeaponKind
    {
        Unknown = 0,
        Melee = 1,
        Ranged = 2,
        Staff = 3,
    }
}
