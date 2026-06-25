namespace Game.Core
{
    /// <summary>
    /// Тип урона. Используется в формуле боя для elementalMod и статус-эффектов
    /// (ТЗ §4 «Система боя», §4 «Статус-эффекты»).
    /// </summary>
    public enum DamageType
    {
        Physical,
        Fire,    // → может наложить Burn
        Ice,     // → может наложить Freeze
        Poison,  // → может наложить Poison
        Lightning,
        Magic,
        True     // игнорирует защиту (для боссов/спец-эффектов)
    }
}
