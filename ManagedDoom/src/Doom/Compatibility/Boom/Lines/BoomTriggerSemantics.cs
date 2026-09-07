namespace ManagedDoom.Compatibility.Boom.Lines;

/// <summary>
/// Physical activation channel encoded by the low three bits of a generalized
/// Boom linedef. The low bit selects once/repeat; the upper two bits select
/// walk, switch, gun or push activation.
/// </summary>
public enum BoomActivationChannel
{
    Walk = 0,
    Switch = 1,
    Gun = 2,
    Push = 3
}

/// <summary>
/// Centralized Boom trigger-channel semantics. This intentionally does not
/// apply one-shot/repeat reset behavior; that is handled after a successful
/// action by the trigger lifecycle code.
/// </summary>
public static class BoomTriggerSemantics
{
    public static BoomActivationChannel GetChannel(BoomTriggerType trigger) =>
        (BoomActivationChannel)((int)trigger >> 1);

    public static bool IsRepeatable(BoomTriggerType trigger) =>
        ((int)trigger & 1) != 0;

    public static bool Matches(BoomTriggerType trigger, BoomActivationChannel channel) =>
        GetChannel(trigger) == channel;

    public static bool CanActivateFromCross(BoomTriggerType trigger) =>
        Matches(trigger, BoomActivationChannel.Walk);

    public static bool CanActivateFromSwitchUse(BoomTriggerType trigger, int side) =>
        side == 0 && Matches(trigger, BoomActivationChannel.Switch);

    public static bool CanActivateFromPushUse(BoomTriggerType trigger, int side) =>
        side == 0 && Matches(trigger, BoomActivationChannel.Push);

    public static bool CanActivateFromUse(BoomTriggerType trigger, int side) =>
        CanActivateFromSwitchUse(trigger, side) || CanActivateFromPushUse(trigger, side);

    public static bool CanActivateFromShoot(BoomTriggerType trigger) =>
        Matches(trigger, BoomActivationChannel.Gun);

    /// <summary>
    /// P1/PR (PushOnce/PushMany in the Boom source) operate directly on the
    /// sector behind the activating line. All other generalized trigger
    /// channels select sectors through the linedef tag.
    /// </summary>
    public static bool UsesTagForTargeting(BoomTriggerType trigger) =>
        GetChannel(trigger) != BoomActivationChannel.Push;
}
