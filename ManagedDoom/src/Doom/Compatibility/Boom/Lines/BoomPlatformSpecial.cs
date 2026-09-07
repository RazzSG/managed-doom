namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomPlatformAction
{
    RaiseAndChange,
    Perpetual,
    Stop,
    Toggle
}

public readonly struct BoomPlatformSpecial
{
    public BoomPlatformSpecial(
        BoomPlatformAction action,
        BoomTriggerType trigger,
        int amount = 0)
    {
        Action = action;
        Trigger = trigger;
        Amount = amount;
    }

    public BoomPlatformAction Action { get; }
    public BoomTriggerType Trigger { get; }
    public int Amount { get; }
    public bool Repeatable => Trigger.IsRepeatable();
}
