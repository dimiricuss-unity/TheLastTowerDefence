using TheLastTowerDefence.Enemies.Domain;

namespace TheLastTowerDefence.Enemies.Spawning
{
    /// <summary>
    /// Одноразовая передача SO в Awake <c>EnemyStatsBootstrap</c> при инстансе из волны.
    /// </summary>
    public static class EnemyWaveSpawnContext
    {
        static EnemyStatsConfig _pendingStats;
        static LootDropConfig _pendingLoot;
        static bool _hasPending;

        public static bool HasPending => _hasPending;

        public static void SetPending(EnemyStatsConfig stats, LootDropConfig loot)
        {
            _pendingStats = stats;
            _pendingLoot = loot;
            _hasPending = true;
        }

        public static bool TryConsume(out EnemyStatsConfig stats, out LootDropConfig loot)
        {
            stats = null;
            loot = null;
            if (!_hasPending)
                return false;

            stats = _pendingStats;
            loot = _pendingLoot;
            _pendingStats = null;
            _pendingLoot = null;
            _hasPending = false;
            return true;
        }

        public static void ClearPending()
        {
            _pendingStats = null;
            _pendingLoot = null;
            _hasPending = false;
        }
    }
}
