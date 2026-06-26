namespace Game.Data
{
    // ── Оружие (ТЗ §3 — WeaponData) ────────────────────────────────────────────
    public enum WeaponType   { Melee, Ranged, Magic }
    public enum AttackPattern { Single, Spread, Piercing, Boomerang }

    // ── Редкость/тир (ТЗ §3) ────────────────────────────────────────────────────
    public enum ItemTier { Common, Rare, Epic, Legendary }
    public enum Rarity   { Common, Rare, Epic, Legendary }

    // ── Расходники (ТЗ §3 — ConsumableData) ────────────────────────────────────
    public enum EffectType { Heal, ManaRestore, Damage, Buff }

    // ── Враги (ТЗ §3 — EnemyData) ──────────────────────────────────────────────
    public enum BehaviorType { Melee, Ranged, Charger, Summoner, Boss }

    /// <summary>
    /// Тактическая роль врага (флаги — враг может нести несколько).
    /// Читается EncounterDirector'ом для синергий и слоями
    /// (модификаторы/связи) при выборе целей. См. дизайн «Система ролей».
    /// </summary>
    [System.Flags]
    public enum EnemyRole
    {
        None       = 0,
        Bruiser    = 1 << 0,
        Tank       = 1 << 1,
        Support    = 1 << 2,
        Summoner   = 1 << 3,
        Controller = 1 << 4,
        Assassin   = 1 << 5,
        Hunter     = 1 << 6,
        Disruptor  = 1 << 7,
        Siege      = 1 << 8,
        Elite      = 1 << 9,
    }

    // ── Характеристики, на которые действуют модификаторы (ТЗ §3 — StatModifier) ─
    public enum StatType { MaxHealth, MaxMana, Speed, Attack, Defense, CritChance, CritMultiplier }

    // ── Способ применения модификатора ──────────────────────────────────────────
    public enum ModifierMode { Flat, PercentAdd, PercentMult }
}
