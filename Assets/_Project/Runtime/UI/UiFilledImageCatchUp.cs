using UnityEngine;
using UnityEngine.UI;

namespace TheLastTowerDefence.UI
{
    /// <summary>
    /// Общая логика: вторая <see cref="Image"/> (filled) плавно догоняет первую по <c>fillAmount</c>.
    /// </summary>
    public static class UiFilledImageCatchUp
    {
        public static void Tick(Image follower, Image leader, float speed)
        {
            if (follower == null || leader == null)
                return;

            var target = leader.fillAmount;
            var t = 1f - Mathf.Exp(-speed * Time.deltaTime);
            var next = Mathf.Lerp(follower.fillAmount, target, t);
            if (Mathf.Abs(next - target) < 0.0005f)
                next = target;
            follower.fillAmount = next;
        }
    }
}
