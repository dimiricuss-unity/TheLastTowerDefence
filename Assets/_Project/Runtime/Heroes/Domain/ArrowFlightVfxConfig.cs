using UnityEngine;

namespace TheLastTowerDefence.Heroes.Domain
{
    /// <summary>
    /// Настройки визуала полёта стрелы (префаб Bolt, скорость, дуга, след TrailRenderer).
    /// </summary>
    [CreateAssetMenu(fileName = "ArrowFlightVfx", menuName = "TLTD/Heroes/Arrow Flight VFX")]
    public sealed class ArrowFlightVfxConfig : ScriptableObject
    {
        [Tooltip("Префаб стрелы (например Bolt).")]
        public GameObject boltPrefab;

        [Min(0.5f)]
        [Tooltip("Скорость полёта по прямой (ед/с). Ниже — заметнее полёт.")]
        public float flySpeed = 10f;

        [Min(1f)]
        [Tooltip("Макс. дистанция полёта = дистанция до цели в момент выстрела × множитель (промах, если цель ушла с линии).")]
        public float maxTravelDistanceFactor = 2.75f;

        [Min(0f)]
        [Tooltip("Устарело для топдауна: дуга не используется, полёт строго по прямой. Оставлено для совместимости ассетов.")]
        public float arcHeightWorld = 0f;

        [Tooltip("Точка вылета в локале героя (ось +X обычно вперёд по спрайту).")]
        public Vector2 spawnOffsetLocal = new Vector2(0.42f, 0.18f);

        [Tooltip("Поворот Z спрайта стрелы относительно направления полёта (градусы).")]
        public float boltRotationOffsetDegrees = -90f;

        [Tooltip("Цвет начала следа (белый с альфой).")]
        public Color trailStartColor = new Color(1f, 1f, 1f, 0.5f);

        [Tooltip("Цвет конца следа (обычно прозрачный).")]
        public Color trailEndColor = new Color(1f, 1f, 1f, 0f);

        [Min(0.01f)]
        public float trailWidthStart = 0.13f;

        [Min(0.01f)]
        public float trailWidthEnd = 0.02f;

        [Min(0.05f)]
        [Tooltip("Время затухания вершин следа (сек).")]
        public float trailTime = 0.42f;

        [Min(0.001f)]
        public float trailMinVertexDistance = 0.035f;

        [Min(0f)]
        [Tooltip("После прилёта: ждать перед Destroy, чтобы след дорисовался.")]
        public float destroyDelayAfterHit = 0.45f;

        [Min(0f)]
        [Tooltip("После попадания в цель: ещё столько метров по направлению полёта (визуально входит в тело). Урон один раз, коллайдер болта сразу отключается.")]
        public float embedDistanceAfterHitWorld = 0.12f;
    }
}
