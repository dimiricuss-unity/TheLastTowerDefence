using UnityEditor;
using UnityEngine;
using TheLastTowerDefence.Enemies.Domain;

namespace TheLastTowerDefence.Editor
{
    [CustomPropertyDrawer(typeof(LootDropSection))]
    public sealed class EnemyLootDropSectionDrawer : PropertyDrawer
    {
        const float Spacing = 6f;
        const float ChanceWidth = 120f;
        const float ToggleWidth = 70f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var rarityProp = property.FindPropertyRelative("rarity");
            var chanceProp = property.FindPropertyRelative("lootDropChancePercent");
            var enabledProp = property.FindPropertyRelative("isEnabledInRoll");

            var line = new Rect(
                position.x,
                position.y,
                position.width,
                EditorGUIUtility.singleLineHeight);

            var content = EditorGUI.PrefixLabel(line, label);
            var rarityWidth = content.width - ChanceWidth - ToggleWidth - Spacing * 2f;

            var rarityRect = new Rect(content.x, content.y, rarityWidth, content.height);
            var chanceRect = new Rect(rarityRect.xMax + Spacing, content.y, ChanceWidth, content.height);
            var toggleRect = new Rect(chanceRect.xMax + Spacing, content.y, ToggleWidth, content.height);

            EditorGUI.PropertyField(rarityRect, rarityProp, GUIContent.none);
            EditorGUI.Slider(chanceRect, chanceProp, 0f, 100f, GUIContent.none);
            EditorGUI.PropertyField(toggleRect, enabledProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
