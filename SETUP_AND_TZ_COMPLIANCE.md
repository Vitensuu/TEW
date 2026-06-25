# TEW — Reализация ТЗ «2D Pixel Roguelike» и инструкция по сборке

Документ описывает, что реализовано в коде по ТЗ, и что нужно досборкой в Unity
Editor (сцены, префабы, ScriptableObject-ассеты, Animator), т.к. это нельзя
создать из кода вне редактора.

> Движок: Unity 6 (`6000.3.16f1`). Все скрипты компилируются в `Assembly-CSharp`
> (asmdef не используются). Новый код — в неймспейсах `Game.*`.

---

## 1. Что реализовано (код)

| Модуль ТЗ | Файлы | Статус |
|-----------|-------|--------|
| Core: FSM, EventBus, SceneLoader, интерфейсы | `Scripts/Core/*` | ✅ |
| Data (ScriptableObjects) | `Scripts/Data/*` | ✅ |
| Combat: атака, урон, снаряды, паттерны | `Scripts/Combat/*` | ✅ |
| Status-эффекты Burn/Freeze/Poison/Stun | `Scripts/Combat/StatusEffectHandler.cs` | ✅ |
| AI: Melee/Ranged/Charger/Summoner | `Scripts/Enemy/*` | ✅ |
| Boss: фазы, спавн каждые 5 этажей | `Scripts/Enemy/BossController.cs`, `RoomPopulator.cs` | ✅ |
| Items/Inventory/Loot/Synergy | `Scripts/Items/*` | ✅ |
| Meta-прогрессия + Save (JSON) | `Scripts/SaveSystem/*` | ✅ |
| UI: HUD, меню, мини-карта | `Scripts/UI/*` | ✅ |
| Audio: Manager + слоистая музыка | `Scripts/Audio/*` | ✅ |
| Генерация подземелья | `Scripts/Dungeon/*` (уже было) | ✅ |

**Архитектурные паттерны (ТЗ §5):** Singleton (`GameManager`, `SaveSystem`,
`AudioManager`, `InventoryManager`, `UIManager`), Observer (`EventBus`),
Template Method (`EnemyBase`), Strategy (attack patterns в `PlayerCombat`),
Registry (`ItemDatabase`), Service (`SaveSystem`), Factory (`DungeonGenerator`).

> Примечание: генерация — Room Placement + MST-коридоры (не BSP). Это сильнее
> покрывает критерий MVP «≥8 комнат, все соединены, есть выход», чем чистый BSP.
> Если нужен именно BSP — это отдельная замена в `DungeonGenerator`.

---

## 2. Обязательная досборка в редакторе

### 2.1. Слои и теги (Project Settings ▸ Tags and Layers)
- Теги: **Player** (на объекте игрока), **Wall** (опц., для снарядов).
- Слои: **Player**, **Enemy**, **Wall**. Настрой Physics2D Layer Matrix.
- В `PlayerCombat.enemyMask` → слой Enemy. В снарядах врагов `playerMask` → Player.

### 2.2. ScriptableObject-ассеты (Create ▸ TEW ▸ …)
Минимум для MVP:
1. **Character_Warrior** (TEW/Character) — назначь `startingWeapon`.
2. **Weapon_Sword** (TEW/Items/Weapon) — Melee, Single.
   Для дальнего класса — `Weapon_Bow`: Ranged, projectilePrefab = префаб снаряда.
3. **Enemy_Slime** (TEW/Enemy) — behaviorType Melee, заполни lootTable (опц.).
4. **Boss_Floor5** (TEW/Boss) — добавь 2 `phases` (healthThreshold 1.0 и 0.5).
5. **ItemDatabase** (TEW/ItemDatabase) — перетащи все предметы.
6. Узлы дерева **Upgrade_** (TEW/Meta/UpgradeNode) — 3 ветки × 10 (для полного ТЗ).

### 2.3. Префабы
- **Player**: Rigidbody2D, Collider2D, тег Player, компоненты
  `PlayerController`, `PlayerAnimator`, `PlayerHealth`, `PlayerMana`,
  `PlayerStats`, `PlayerCombat` (+ `firePoint` — пустой child спереди),
  опц. `ItemSynergyDetector`.
- **Enemy**: Rigidbody2D, Collider2D, Animator, SpriteRenderer, слой Enemy,
  один из `EnemyAI`/`RangedEnemy`/`ChargerEnemy`/`SummonerEnemy`,
  `StatusEffectHandler`, ссылка `data` = EnemyData.
- **Boss**: как Enemy, но `BossController` + `data` = BossData.
- **Projectile**: Rigidbody2D (Dynamic, gravity 0), Collider2D (IsTrigger),
  компонент `Projectile`, SpriteRenderer.
- **ItemPickup** (опц.): Collider2D (IsTrigger), SpriteRenderer, `ItemPickup`.

### 2.4. Менеджеры в сцене
Помести в **MainMenu** один объект с `Bootstrap` — он создаст
`GameManager`/`SaveSystem`/`AudioManager`/`InventoryManager` (DontDestroyOnLoad).
Добавь `MetaProgression` на тот же объект, что `SaveSystem` (назначь узлы).

### 2.5. Сцены (Build Settings, имена строго как в `SceneLoader`)
`MainMenu`, `CharacterSelect`, `GameScene`, `BossRoom`, `GameOver`, `MetaUpgrades`.
Добавь все в File ▸ Build Profiles ▸ Scene List.

- **GameScene**: Grid + Tilemap(Floor/Wall), объект с `DungeonGenerator` +
  `RoomPopulator` (назначь `enemyPrefabs`, `bossPrefab`), Player, Canvas с
  `UIManager`+`HUDManager`+`MinimapController`, камера с `CameraFollow` +
  Pixel Perfect Camera.

### 2.6. Pixel Perfect Camera (ТЗ §8)
На Main Camera добавь **Pixel Perfect Camera**: `Assets Pixels Per Unit = 16`,
`Reference Resolution = 320×180`, Crop Frame = None, Grid Snapping = Pixel Snap.

### 2.7. Animator (ТЗ §7)
Параметры контроллера игрока/врага: `isMoving`(Bool), `isAttacking`(Trigger),
`isDead`(Trigger), `dirX`(Float), `dirY`(Float). Клипы Idle/Run/Attack/Death.

### 2.8. Audio (ТЗ §7)
Создай AudioMixer с группами Master/Music/SFX и exposed-параметрами
`MasterVolume`/`MusicVolume`/`SFXVolume`. Назначь в `AudioManager`.

---

## 3. Критерии приёмки MVP (ТЗ §9) — статус

| # | Критерий | Покрытие кодом | Нужна досборка |
|---|----------|----------------|----------------|
| 1 | Выбор класса и старт забега | `CharacterSelectUI`→`GameManager.StartNewRun` | сцена+карточки |
| 2 | Этаж ≥8 комнат, соединены, выход | `DungeonGenerator`(+validator) | tilemaps |
| 3 | Боевой цикл атака→урон→смерть→лут | `PlayerCombat`/`DamageCalculator`/`EnemyBase`/`LootDropper` | префабы |
| 4 | Подбор предметов, инвентарь, пересчёт статов | `ItemPickup`/`InventoryManager`/`PlayerStats` | UI инвентаря |
| 5 | Смерть → экран результатов | `PlayerHealth`→`EventBus`→`GameManager`→`GameOverUI` | сцена GameOver |
| 6 | Осколки сохраняются, мета-апгрейды | `SaveSystem`/`MetaProgression`/`MetaUpgradesUI` | дерево узлов |
| 7 | Босс на 5-м этаже, 2 фазы, гарант-дроп | `BossController`/`BossData`/`RoomPopulator` | префаб+BossData |
| 8 | Мини-карта пройденных/нераскрытых комнат | `MinimapController` | Canvas |
| 9 | 60 FPS на среднем ПК | архитектура лёгкая (A*, пулы) | профайлинг |
| 10 | Pixel Perfect без артефактов | — | настройка камеры |

Логика всех 10 критериев реализована; пункты с «досборкой» требуют только
визуальной привязки в редакторе (сцены/префабы/ассеты), не нового кода.

---

## 4. Поток игры (smoke-test)

1. MainMenu → «Новая игра» → CharacterSelect.
2. Выбор класса → `StartNewRun` создаёт `PlayerRunData`, грузит GameScene.
3. `DungeonGenerator` строит этаж, `RoomPopulator` спавнит врагов.
4. ЛКМ/E — атака; враги преследуют (A*), получают урон, дропают лут.
5. Доходишь до выхода → `NextFloorTrigger` → следующий этаж (на 5-м — босс).
6. Смерть → `GameOver`, Осколки начислены в `MetaSaveData.json`.
7. MetaUpgrades — тратишь Осколки на узлы дерева (бонусы к следующим забегам).
