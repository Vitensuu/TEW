using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// Временная стена «живой комнаты» (дизайн Фаза 1): спавнит твёрдый коллайдер
    /// на слое препятствий и пересчитывает NavGrid, чтобы A* СРАЗУ видел стену
    /// (роли Siege/Controller режут комнату, босс перестраивает арену). По истечении
    /// времени снимает коллайдер и снова освобождает клетки сетки.
    ///
    /// Опирается на готовый хук <see cref="RoomManager.RefreshNav"/>, который
    /// пересканирует проходимость по реальным коллайдерам слоя obstacleMask —
    /// поэтому стена-коллайдер на слое "Obstacle" автоматически попадает в навигацию.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class TempWall : MonoBehaviour
    {
        float _life;
        bool  _timed;

        /// <summary>
        /// Создать временную стену. lifetime &lt;= 0 — бессрочно (снимается вручную
        /// через <see cref="Remove"/>). layerName должен входить в obstacleMask
        /// ассемблера (по умолчанию "Obstacle", как у стен комнат).
        /// </summary>
        public static TempWall Spawn(Vector3 pos, Vector2 size, float lifetime,
            string layerName = "Obstacle")
        {
            var go = new GameObject("TempWall");
            go.transform.position = pos;

            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0) go.layer = layer;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            var tw = go.AddComponent<TempWall>();
            tw._life  = lifetime;
            tw._timed = lifetime > 0f;

            RoomManager.Instance?.RefreshNav();   // A* увидит новую стену
            return tw;
        }

        void Update()
        {
            if (!_timed) return;
            _life -= Time.deltaTime;
            if (_life <= 0f) Remove();
        }

        /// <summary>Снять стену и вернуть проходимость клеток.</summary>
        public void Remove()
        {
            // Сначала гасим коллайдер, ЗАТЕМ пересчитываем сетку — иначе
            // OverlapBox ещё нашёл бы эту стену и оставил клетки занятыми.
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            RoomManager.Instance?.RefreshNav();
            Destroy(gameObject);
        }
    }
}
