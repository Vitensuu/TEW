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

    // ── Характеристики, на которые действуют модификаторы (ТЗ §3 — StatModifier) ─
    public enum StatType { MaxHealth, MaxMana, Speed, Attack, Defense, CritChance, CritMultiplier }

    // ── Способ применения модификатора ──────────────────────────────────────────
    public enum ModifierMode { Flat, PercentAdd, PercentMult }
}
