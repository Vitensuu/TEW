// Если в твоём DungeonGrid нет метода InBounds — добавь этот фрагмент в класс DungeonGrid:
//
//   public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
//
// Также убедись что BuildWalls() помечает TileType.Wall все клетки,
// которые граничат с Floor но сами не Floor.
// Пример реализации:
//
// public void BuildWalls()
// {
//     for (int x = 0; x < Width; x++)
//     for (int y = 0; y < Height; y++)
//     {
//         if (this[x, y] != TileType.Empty) continue;
//         if (HasFloorNeighbour(x, y))
//             this[x, y] = TileType.Wall;
//     }
// }
//
// private bool HasFloorNeighbour(int x, int y)
// {
//     for (int dx = -1; dx <= 1; dx++)
//     for (int dy = -1; dy <= 1; dy++)
//     {
//         if (dx == 0 && dy == 0) continue;
//         int nx = x + dx, ny = y + dy;
//         if (InBounds(nx, ny) && this[nx, ny] == TileType.Floor)
//             return true;
//     }
//     return false;
// }

namespace Dungeon
{
    // Этот файл — справочный комментарий, не требует компиляции.
    // Все изменения вноси напрямую в DungeonGrid.cs
}
