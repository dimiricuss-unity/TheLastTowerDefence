namespace TheLastTowerDefence.Common.Combat
{
    /// <summary>
    /// Цель, по которой можно нанести урон (башня, щит и т.д.).
    /// </summary>
    public interface IDamageable
    {
        void ApplyDamage(float amount);
    }
}
