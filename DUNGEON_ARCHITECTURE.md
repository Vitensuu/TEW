# Новая система генерации уровня — Prefab-Based Room Assembly

Полная замена старой процедурной генерации (BSP / random rectangle / carving /
tile-painting) на **сборку этажа из готовых комнат-префабов**, которые дизайнер
делает вручную (стиль Enter the Gungeon / Soul Knight / Binding of Isaac / Hades).

> Генератор НЕ создаёт комнаты. Генератор СОБИРАЕТ этаж из готовых префабов.

---

## 0. Что удалено (старая система)

Удалены полностью, без обратной совместимости:

```
Assets/Scripts/Dungeon/Procedural/      ← вся папка
  DungeonGenerator.cs   (Room Placement + MST + random rectangle)
  TilemapPainter.cs     (Floor/Wall painting по случайным комнатам)
  CollisionBuilder.cs
  Corridor.cs           (L-shape коридоры)
  DungeonValidator.cs
  Room.cs / RoomType.cs (RectInt-комнаты)
  DungeonTileSet.cs / Direction.cs
Assets/Scripts/Dungeon/DungeonGrid.cs   (CarveRoom / CarveCorridor / BuildWalls)
Assets/Scripts/Dungeon/RoomPopulator.cs (по RectInt)
Assets/Scripts/Dungeon/NextFloorTrigger.cs
Assets/Scripts/Enemy/GridPathfinder.cs  (переехал в Navigation, репойнтнут на NavGrid)
```

A* сохранён как алгоритм, но источник проходимости теперь — **реальная геометрия
комнат** (коллайдеры), а не вырезанная процедурная сетка.

---

## 1. Новая структура папок

```
Assets/Scripts/Dungeon/            namespace Game.Dungeon
├── Rooms/                         «строительные блоки» комнаты
│   ├── RoomType.cs                enum (Start/Normal/Elite/Treasure/Shop/Event/Shrine/Boss/Secret/Exit)
│   ├── Direction.cs               enum N/S/E/W + повороты/стыковка
│   ├── DoorEnums.cs               DoorType (Normal/Locked/Boss/Secret), DoorState (Open/Closed/Locked)
│   ├── RoomData.cs                ScriptableObject — метаданные комнаты
│   ├── RoomLibrary.cs             ScriptableObject — библиотека всех комнат
│   ├── RoomInstance.cs            компонент на корне префаба («RoomPrefab»)
│   ├── RoomConnectionPoint.cs     дверной сокет стыковки
│   ├── DoorController.cs          дверь: направление/тип/состояние
│   └── SpawnPoint.cs              маркер точки (Enemy/Chest/NPC/Decoration/Trap/Entry/Exit)
├── Navigation/                    A* по реальной геометрии
│   ├── NavGrid.cs                 сетка проходимости (world↔cell)
│   ├── NavGridBuilder.cs          сканер коллайдеров → NavGrid (+ Refresh для дверей/динамики)
│   └── GridPathfinder.cs          A* (4-направленный) по NavGrid
├── Graph/                         граф этажа (ЭТАП 6)
│   ├── RoomNode.cs                узел: тип, соседи, depth, visited/cleared
│   └── RoomGraph.cs               граф: Start/Boss/Exit, связность, BFS-глубина
├── Assembly/                      сборка (ЭТАП 2–4)
│   ├── LevelGraphBuilder.cs       абстрактный граф из FloorConfig
│   ├── DungeonAssembler.cs        размещение префабов: стыковка/вращение/анти-наложение
│   └── RoomValidator.cs           проверка наложений/связности/обязательных комнат
└── Runtime/                       рантайм этажа (ЭТАП 5)
    ├── RoomManager.cs             оркестратор-синглтон (граф, навигация, encounters)
    ├── RoomPopulator.cs           заселение по точкам спавна (EnemyData/BossData/FloorConfig)
    ├── RoomEncounter.cs           «живая комната»: закрыть двери → бой → открыть/сундуки
    └── NextRoomPortal.cs          выход на следующий этаж
```

---

## 2. UML — связи классов

```
ScriptableObjects (данные дизайнера):
  RoomLibrary ◇──many──> RoomData ──prefab──> [Room Prefab]
  FloorConfig (Game.Data) ──enemyPool──> EnemyData / BossData

Room Prefab (иерархия на сцене):
  RoomInstance (root)
    ├─ owns ──> RoomConnectionPoint []   (двери-сокеты)
    ├─ owns ──> DoorController []         (двери)
    ├─ owns ──> SpawnPoint []             (Enemy/Chest/NPC/Entry/Exit/...)
    └─ ref  ──> RoomData
    └─ ref  ──> RoomNode (ставит ассемблер)

Сборка:
  DungeonAssembler
    ├─ uses ─> LevelGraphBuilder ──> List<NodeSpec>      (ЭТАП 2)
    ├─ uses ─> RoomLibrary.Pick(type,floor)              (ЭТАП 3)
    ├─ instantiates ─> RoomInstance (+rotate +align)     (ЭТАП 4)
    ├─ uses ─> RoomValidator (overlap/connectivity)
    ├─ builds ─> RoomGraph  ◇──> RoomNode                (ЭТАП 6)
    └─ builds ─> NavGrid (via NavGridBuilder)

Рантайм:
  RoomManager (Singleton)
    ├─ ref ─> DungeonAssembler (Graph, Nav, Rooms, CurrentConfig)
    ├─ ref ─> RoomPopulator
    ├─ adds ─> RoomEncounter   (на боевые комнаты)
    └─ adds ─> NextRoomPortal  (на Exit)

  RoomEncounter ──uses──> RoomPopulator.PopulateRoom()
                ──drives─> DoorController.Close()/Open()
                ──marks──> RoomNode.Visited/Cleared
                ──calls──> RoomManager.RefreshNav()

  EnemyAI / Ranged/Charger/Summoner/Boss
                ──uses──> RoomManager.Instance.Nav
                ──uses──> GridPathfinder.FindPath(NavGrid,...)

  MinimapController ──reads──> RoomManager.Instance.Graph (RoomNode.Visited/Type)
```

Интеграция с существующими системами (НЕ менялись по сути):
`EnemyBase`, `BossController`, `LootDropper`, `ItemDatabase`, `InventoryManager`,
`FloorConfig`, `EnemyData`, `BossData` — используются как есть.

---

## 3. ScriptableObject-структура

### RoomData  (`TEW/Rooms/RoomData`)
| Поле | Назначение |
|------|-----------|
| roomID | стабильный ID |
| roomType | RoomType |
| prefab | ссылка на Room Prefab (с RoomInstance) |
| difficultyWeight | баланс сложности этажа |
| spawnWeight | вес в рулетке выбора |
| minFloor / maxFloor | диапазон этажей |
| canRotate | можно ли вращать ×90° при стыковке |

> Точки спавна (Enemy/Chest/...) и двери НЕ хранятся в RoomData — они живут на
> префабе как дочерние объекты с `SpawnPoint`/`RoomConnectionPoint`/`DoorController`.
> SO не может ссылаться на сценовые трансформы; `RoomInstance.Collect()` собирает их
> в рантайме. Так дизайнер расставляет всё визуально внутри префаба.

### RoomLibrary  (`TEW/Rooms/RoomLibrary`)
Список всех RoomData. API: `Query(type,floor)`, `Pick(type,floor,rng)` —
взвешенный выбор без повтора подряд (ЭТАП 3).

### FloorConfig (существующий, `TEW/FloorConfig`)
Определяет `minRooms/maxRooms`, `enemyPool`, `hasBoss/bossData`,
`shop/shrine/treasureChance`. Драйвит `LevelGraphBuilder` и `RoomPopulator`.

---

## 4. Конвейер (ЭТАПЫ ТЗ → классы)

| Этап | Что делает | Класс |
|------|-----------|-------|
| 1 | Загрузить все Room Prefabs | `RoomLibrary` |
| 2 | Случайный граф связей по FloorConfig | `LevelGraphBuilder.Build()` → `List<NodeSpec>` |
| 3 | Выбор подходящих префабов (вес, без повтора) | `RoomLibrary.Pick()` |
| 4 | Стыковка через двери: поиск совместимых сокетов, вращение, позиционирование, анти-наложение | `DungeonAssembler.PlaceChild()` + `RoomValidator` |
| 5 | Заселение врагами/боссом/лутом | `RoomPopulator` (по `RoomEncounter`) |
| 6 | Room Graph для миникарты/навигации/соседей/дверей | `RoomGraph` + `NavGrid` |

### Алгоритм стыковки (ЭТАП 4) — ядро
```
для родительского сокета ps (свободного):
  worldDir   = ps.WorldDirection(parent.RotationStepsCW)
  needChild  = worldDir.Opposite()
  для дочернего сокета cs (свободного):
    steps = canRotate ? cs.direction.StepsTo(needChild)
                      : (cs.direction==needChild ? 0 : skip)
    child.SetRotationSteps(steps)                 // вращаем комнату ×90°
    desired = ps.worldPos + worldDir * (cell+gap) // совмещаем дверь к двери
    child.position += desired - cs.worldPos
    если !overlap(placed, child):  ← RoomValidator
        пометить ps, cs занятыми; добавить ребро в граф; OK
```

---

## 5. Система дверей

`DoorController` на каждом дверном объекте префаба:
- **direction**: North/South/East/West
- **type**: Normal / Locked / Boss / Secret
- **state**: Open / Closed / Locked
- `Open()/Close()/Lock()/Unlock()` переключают блокирующий `Collider2D` и визуал.

`RoomConnectionPoint` — «сокет» рядом с дверью; задаёт direction/doorType, по нему
ассемблер стыкует комнаты. Закрытая дверь = коллайдер включён → `NavGrid` после
`Refresh()` делает эти клетки непроходимыми (A* их обходит).

---

## 6. Боевая «живая комната» (ЭТАП 5 + Combat)

`RoomEncounter` (вешается RoomManager'ом на Normal/Elite/Boss):
1. Игрок вошёл (триггер по габаритам) → `CloseDoors()` (двери закрываются).
2. `RoomPopulator.PopulateRoom()` спавнит врагов в точках `SpawnKind.Enemy`
   (босс — `BossData`/`bossPrefab`).
3. Каждый кадр считает живых; когда 0 → `Clear()`:
   - `OpenDoors()` (двери открываются),
   - `ActivateChests()` (включает сундуки под `ChestPoints`),
   - `RoomNode.Cleared = true`, `RoomManager.RefreshNav()`,
   - Exit разблокируется (`NextRoomPortal` проверяет `Boss.Cleared`).

Враги обязаны использовать геометрию: `EnemyAI` строит путь по `NavGrid`, который
сканирует **колонны/стены/узкие проходы/ловушки-коллайдеры** комнаты-префаба.

---

## 7. A* и динамика

- `NavGridBuilder.Build(totalBounds, obstacleMask)` — растеризует все коллайдеры
  слоя препятствий в проходимость (ЭТАП «A* учитывает стены/препятствия/двери»).
- `NavGridBuilder.Refresh(grid, mask)` — пересчёт при открытии дверей и движении
  динамических объектов (вызывается из `RoomEncounter`/`RoomManager.RefreshNav`).
- `GridPathfinder.FindPath(NavGrid, from, to)` — 4-направленный A*.

---

## 8. Порядок разработки (как собрано)

1. **R1 Data**: RoomType, Direction, DoorEnums, RoomData, RoomLibrary.
2. **R2 Prefab-компоненты**: RoomInstance, RoomConnectionPoint, DoorController, SpawnPoint.
3. **R3 Navigation**: NavGrid, NavGridBuilder, GridPathfinder (репойнт A*).
4. **R4 Assembly**: RoomNode/RoomGraph, LevelGraphBuilder, DungeonAssembler, RoomValidator.
5. **R5 Runtime**: RoomManager, RoomEncounter, RoomPopulator, NextRoomPortal.
6. **R6 Интеграция**: рерайт EnemyAI/Minimap на NavGrid/RoomGraph, удаление старого.

---

## 9. Сборка в Unity Editor (Newmap)

### 9.1. Сделать Room Prefab
1. Пустой GameObject `Room_Normal_01` → добавить **RoomInstance**.
2. Внутри: нарисовать тайлами пол/стены (Tilemap + `TilemapCollider2D` на стенах,
   слой **Obstacle**). Добавить колонны/препятствия/ловушки (коллайдеры слоя Obstacle).
3. `Bounds`: задать `footprint` в RoomInstance или повесить дочерний `BoxCollider2D`.
4. **Connections/**: на каждый проход — пустышка с `RoomConnectionPoint` (direction).
5. **Doors/**: на каждый проход — `DoorController` (blocker-коллайдер, визуалы).
6. **Spawns/**: пустышки с `SpawnPoint` (Enemy/Chest/NPC/Entry/Exit/...).
7. Сохранить как Prefab. Создать `RoomData` (TEW/Rooms/RoomData) → указать prefab/type.

### 9.2. Собрать библиотеку и сцену
1. `TEW/Rooms/RoomLibrary` → добавить все RoomData. Нужны минимум: Start, несколько
   Normal, Boss, Exit (иначе валидатор не соберёт этаж).
2. На сцене **Newmap**: объект `Dungeon` с `DungeonAssembler` (library, floorConfigs,
   obstacleMask=Obstacle, player), `RoomPopulator`, `RoomManager`.
3. Слой **Obstacle** на все стены/препятствия/двери-блокеры.
4. Player с тегом `Player`. Камера `CameraFollow` + Pixel Perfect.

> `RoomManager` — снять галку `persistAcrossScenes`, если он живёт в игровой сцене.

---

## 10. Примеры кода (ключевые точки)

**Выбор префаба без повтора (RoomLibrary):**
```csharp
public RoomData Pick(RoomType type, int floor, System.Random rng) {
    var pool = Query(type, floor);
    if (pool.Count > 1 && _lastByType.TryGetValue(type, out var lastId))
        pool.RemoveAll(r => r.RoomID == lastId);      // не повторять подряд
    // ... взвешенная рулетка по spawnWeight ...
}
```

**Стыковка с вращением (DungeonAssembler.PlaceChild):**
```csharp
Direction worldDir   = ps.WorldDirection(parent.RotationStepsCW);
Direction needChild  = worldDir.Opposite();
int steps = cs.direction.StepsTo(needChild);          // на сколько ×90° повернуть
candidate.SetRotationSteps(steps);
Vector3 desired = ps.transform.position + (Vector3)((Vector2)worldDir.ToVector()*(cell+gap));
candidate.transform.position += desired - cs.transform.position;
if (!RoomValidator.AnyOverlap(_placed, candidate)) { /* принять */ }
```

**A* по реальной геометрии (NavGridBuilder):**
```csharp
bool blocked = Physics2D.OverlapBox(cellCenter, box, 0f, obstacleMask) != null;
grid.SetWalkable(x, y, !blocked);   // двери-коллайдеры → авто-непроходимость
```

**Живая комната (RoomEncounter):**
```csharp
void Begin()  { CloseDoors(); _enemies = populator.PopulateRoom(room,cfg,floor); }
void Update() { _enemies.RemoveAll(e=>!e); if(_enemies.Count==0) Clear(); }
void Clear()  { OpenDoors(); ActivateChests(); room.Node.Cleared=true; RoomManager.Instance.RefreshNav(); }
```

---

## 11. Совместимость

Используются без изменений: `EnemyBase`/`EnemyAI`/`RangedEnemy`/`ChargerEnemy`/
`SummonerEnemy`/`BossController`, `LootDropper`, `ItemDatabase`, `InventoryManager`,
`FloorConfig`, `EnemyData`, `BossData`, бой/предметы/мета-прогрессия.
`EnemyAI` и `MinimapController` переведены на `RoomManager`/`NavGrid`/`RoomGraph`.

> Сцены, всё ещё содержащие старый `DungeonGenerator`, покажут missing-script —
> это ожидаемо: их надо пересобрать новыми компонентами (раздел 9).
```
