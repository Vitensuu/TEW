using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Глобальная ссылка на игрока (ТЗ §6 — развязка вместо FindGameObjectWithTag в каждом враге).
    /// Устанавливается компонентом игрока при появлении (PlayerHealth), очищается при уничтожении.
    /// Враги/системы берут позицию игрока отсюда, без поиска по тегу каждый кадр.
    /// </summary>
    public static class PlayerRef
    {
        public static Transform Transform { get; private set; }

        public static GameObject GameObject => Transform != null ? Transform.gameObject : null;

        public static bool Exists => Transform != null;

        public static void Set(Transform t) => Transform = t;

        public static void Clear(Transform t)
        {
            if (Transform == t) Transform = null;
        }

        /// <summary>
        /// Игрок из реестра, c безопасным фолбэком на поиск по тегу
        /// (на случай иного порядка инициализации). Кэшируй результат у себя.
        /// </summary>
        public static GameObject Resolve()
        {
            if (Transform != null) return Transform.gameObject;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) Transform = go.transform;
            return go;
        }
    }
}
