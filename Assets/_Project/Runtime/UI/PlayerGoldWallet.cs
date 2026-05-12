using System.Globalization;
using TMPro;
using UnityEngine;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Кошелёк золота: отображение в дочернем TMP <see cref="goldCountChildName"/> (по умолчанию Gold_count).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerGoldWallet : MonoBehaviour
    {
        [SerializeField] TMP_Text goldCountText;
        [SerializeField] string goldCountChildName = "Gold_count";

        int _gold;

        public int Balance => _gold;

        void Awake()
        {
            if (goldCountText == null && !string.IsNullOrEmpty(goldCountChildName))
            {
                var t = transform.Find(goldCountChildName);
                if (t != null)
                {
                    goldCountText = t.GetComponent<TMP_Text>();
                }
            }

            _gold = 0;
            RefreshText();
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _gold += amount;
            RefreshText();
        }

        void RefreshText()
        {
            if (goldCountText != null)
            {
                goldCountText.text = _gold.ToString(CultureInfo.InvariantCulture);
            }
        }
    }
}
