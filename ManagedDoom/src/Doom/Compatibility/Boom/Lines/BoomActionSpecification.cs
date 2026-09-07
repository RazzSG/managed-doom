namespace ManagedDoom.Compatibility.Boom.Lines;

/// <summary>
/// Semantic speed encoded by generalized Boom linedefs.
/// The actual Fixed speed is resolved by the concrete action because
/// floors, doors, lifts, stairs and crushers use different base scales.
/// </summary>
public enum BoomActionSpeed
{
    Slow = 0,
    Normal = 1,
    Fast = 2,
    Turbo = 3
}

/// <summary>
/// Plane movement direction. Values intentionally match the existing
/// SectorAction mover convention.
/// </summary>
public enum BoomPlaneDirection
{
    Down = -1,
    Up = 1
}

/// <summary>
/// Generalized floor/ceiling texture and sector-special change mode.
/// Numeric values intentionally match the two-bit Boom change field.
/// </summary>
public enum BoomChangeType
{
    None = 0,
    TextureAndZeroSpecial = 1,
    TextureOnly = 2,
    TextureAndSpecial = 3
}

/// <summary>
/// Source sector used when a generalized floor/ceiling change copies data.
/// </summary>
public enum BoomModelType
{
    Trigger = 0,
    Numeric = 1
}

/// <summary>
/// Fields shared by every generalized Boom action.
/// Category-specific specifications add direction, target, delay, crush,
/// change/model information and any other action-specific state.
/// </summary>
public readonly struct BoomActionSpecification
{
    public BoomActionSpecification(BoomTriggerType trigger, BoomActionSpeed speed, bool allowsMonsters)
    {
        Trigger = trigger;
        Speed = speed;
        AllowsMonsters = allowsMonsters;
    }

    public BoomTriggerType Trigger { get; }

    public BoomActionSpeed Speed { get; }

    public bool AllowsMonsters { get; }

    public bool Repeatable => Trigger.IsRepeatable();

    /// <summary>
    /// Push triggers target the sector behind the activating linedef directly.
    /// Other trigger types use the linedef tag to select target sectors.
    /// This property refers only to sector targeting; individual actions may
    /// still use the tag for secondary effects.
    /// </summary>
    public bool UsesTagForTargeting => BoomTriggerSemantics.UsesTagForTargeting(Trigger);
}

/// <summary>
/// Shared bitfield decoding for generalized Boom linedefs.
/// Category translators should decode their remaining bits exactly once and
/// return readonly specifications instead of exposing raw bit operations to
/// the gameplay code.
/// </summary>
public static class BoomGeneralizedSpecial
{
    public const int Min = 0x2F80;
    public const int Max = 0x7FFF;

    private const int TriggerMask = 0x0007;
    private const int SpeedMask = 0x0018;
    private const int SpeedShift = 3;

    public static bool IsGeneralized(LineSpecial special)
    {
        var value = (int)special;
        return value >= Min && value <= Max;
    }

    public static BoomTriggerType DecodeTrigger(LineSpecial special) =>
        (BoomTriggerType)((int)special & TriggerMask);

    public static BoomActionSpeed DecodeSpeed(LineSpecial special) =>
        (BoomActionSpeed)(((int)special & SpeedMask) >> SpeedShift);

    public static BoomActionSpecification DecodeCommon(LineSpecial special, bool allowsMonsters) =>
        new(DecodeTrigger(special), DecodeSpeed(special), allowsMonsters);
}
