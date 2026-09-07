namespace ManagedDoom.Compatibility.Boom.Lines;

/// <summary>
/// Applies the post-success lifecycle of Boom generalized/extended triggers.
/// Activation-channel matching is handled separately by <see cref="BoomTriggerSemantics"/>.
/// </summary>
public static class BoomTriggerLifecycle
{
    public static bool ConsumesSpecialOnSuccess(BoomTriggerType trigger) =>
        !BoomTriggerSemantics.IsRepeatable(trigger);

    public static bool UsesSwitchTexture(BoomTriggerType trigger)
    {
        var channel = BoomTriggerSemantics.GetChannel(trigger);
        return channel == BoomActivationChannel.Switch || channel == BoomActivationChannel.Gun;
    }

    public static bool ResetsSwitchTexture(BoomTriggerType trigger) =>
        UsesSwitchTexture(trigger) && BoomTriggerSemantics.IsRepeatable(trigger);

    /// <summary>
    /// Applies Boom's lifecycle only after the linedef action has succeeded.
    /// Failed actions must not consume one-shot triggers or start button resets.
    /// </summary>
    public static void ApplySuccess(World world, LineDef line, BoomTriggerType trigger)
    {
        if (UsesSwitchTexture(trigger))
        {
            world.Specials.ChangeSwitchTexture(line, BoomTriggerSemantics.IsRepeatable(trigger));
            return;
        }

        if (ConsumesSpecialOnSuccess(trigger))
            line.Special = 0;
    }
}
