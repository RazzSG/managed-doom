namespace ManagedDoom.Compatibility.Boom.Lines;

/// <summary>
/// Activation type encoded by the low three bits of a generalized Boom linedef special.
/// The numeric values intentionally match the Boom generalized linedef bit layout.
/// </summary>
public enum BoomTriggerType
{
    WalkOnce = 0,
    WalkRepeat = 1,
    SwitchOnce = 2,
    SwitchRepeat = 3,
    GunOnce = 4,
    GunRepeat = 5,
    PushOnce = 6,
    PushRepeat = 7
}

public static class BoomTriggerTypeExtensions
{
    public static bool IsRepeatable(this BoomTriggerType trigger) => BoomTriggerSemantics.IsRepeatable(trigger);

    public static bool IsWalk(this BoomTriggerType trigger) => BoomTriggerSemantics.Matches(trigger, BoomActivationChannel.Walk);

    public static bool IsSwitch(this BoomTriggerType trigger) => BoomTriggerSemantics.Matches(trigger, BoomActivationChannel.Switch);

    public static bool IsGun(this BoomTriggerType trigger) => BoomTriggerSemantics.Matches(trigger, BoomActivationChannel.Gun);

    public static bool IsPush(this BoomTriggerType trigger) => BoomTriggerSemantics.Matches(trigger, BoomActivationChannel.Push);
}
