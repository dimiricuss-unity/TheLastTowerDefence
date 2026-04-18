using UnityEngine;

namespace TheLastTowerDefence.Heroes.Domain
{
    [CreateAssetMenu(fileName = "ClericHealSpell", menuName = "TLTD/Heroes/Cleric Heal Spell")]
    public sealed class ClericHealSpellConfig : ScriptableObject
    {
        [Min(0.01f)]
        [Tooltip("Сколько HP восстановить за один каст (фактически не больше недостающего до максимума цели).")]
        public float healAmount = 25f;

        [Min(0.01f)]
        [Tooltip("Сколько маны списать за один каст. Нужна полная стоимость — при нехватке маны каст не выполняется.")]
        public float manaCost = 15f;
    }
}
