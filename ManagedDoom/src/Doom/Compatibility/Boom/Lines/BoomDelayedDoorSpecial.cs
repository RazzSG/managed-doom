namespace ManagedDoom.Compatibility.Boom.Lines;

public readonly struct BoomDelayedDoorSpecial
{
    public BoomDelayedDoorSpecial(BoomTriggerType trigger)
    {
        Trigger = trigger;
    }

    public BoomTriggerType Trigger { get; }

    public bool Repeatable => Trigger.IsRepeatable();
}
