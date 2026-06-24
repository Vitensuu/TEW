using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dungeon.Procedural
{
    /// <summary>
    /// Гарантирует корректную физику подземелья:
    ///   • на слое стен (или отдельном слое коллизий) есть TilemapCollider2D,
    ///     объединённый CompositeCollider2D (меньше коллайдеров → быстрее и без
    ///     «застреваний» на швах между тайлами);
    ///   • на слое пола НЕТ коллайдера (пол всегда проходим);
    ///   • тело статическое (Rigidbody2D = Static) — стены не двигаются.
    ///
    /// Вызывается один раз при инициализации генератора. Идемпотентен:
    /// повторный вызов ничего не ломает (компоненты переиспользуются).
    /// </summary>
    public static class CollisionBuilder
    {
        /// <summary>
        /// Настроить коллизии. collisionTilemap — слой, который физически
        /// останавливает игрока (обычно это сам Wall-слой; передай его же,
        /// если отдельного слоя коллизий нет).
        /// </summary>
        public static void Configure(Tilemap collisionTilemap, Tilemap floorTilemap)
        {
            if (collisionTilemap != null)
                EnsureSolid(collisionTilemap.gameObject);

            if (floorTilemap != null)
                EnsureNoCollider(floorTilemap.gameObject);
        }

        /// <summary>Стена: TilemapCollider2D + Composite + статический Rigidbody2D.</summary>
        static void EnsureSolid(GameObject go)
        {
            var tilemapCollider = go.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
                tilemapCollider = go.AddComponent<TilemapCollider2D>();

            var body = go.GetComponent<Rigidbody2D>();
            if (body == null)
                body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var composite = go.GetComponent<CompositeCollider2D>();
            if (composite == null)
                composite = go.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;

            // Включаем объединение тайловых коллайдеров в композит.
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            tilemapCollider.isTrigger = false;
            composite.isTrigger = false;

            // Принудительная пересборка геометрии после перерисовки тайлов.
            composite.GenerateGeometry();
        }

        /// <summary>Пол: убрать любой коллайдер, если случайно появился.</summary>
        static void EnsureNoCollider(GameObject go)
        {
            var tc = go.GetComponent<TilemapCollider2D>();
            if (tc != null) Object.Destroy(tc);

            var comp = go.GetComponent<CompositeCollider2D>();
            if (comp != null) Object.Destroy(comp);
        }
    }
}
