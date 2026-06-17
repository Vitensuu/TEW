using UnityEngine;

namespace Dungeon
{
    /// <summary>
    /// Данные одной сгенерированной комнаты.
    /// Когда дизайнер сделает свои префабы комнат — этот класс легко расширить
    /// полем TemplateId / RoomType, чтобы выбирать готовый префаб вместо
    /// процедурного прямоугольника (см. README).
    /// </summary>
    public struct RoomInfo
    {
        public RectInt Area;

        public RoomInfo(RectInt area)
        {
            Area = area;
        }

        public Vector2Int Center => new Vector2Int(
            Area.x + Area.width / 2,
            Area.y + Area.height / 2);
    }
}
