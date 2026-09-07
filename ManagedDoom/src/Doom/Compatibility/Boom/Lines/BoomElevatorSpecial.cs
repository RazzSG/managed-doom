namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomElevatorTarget
{
    Up,
    Down,
    Current
}

public readonly struct BoomElevatorSpecial
{
    public BoomElevatorSpecial(BoomElevatorTarget target, BoomTriggerType trigger)
    {
        Target = target;
        Trigger = trigger;
    }

    public BoomElevatorTarget Target { get; }

    public BoomTriggerType Trigger { get; }

    public bool Repeatable => Trigger.IsRepeatable();
}
