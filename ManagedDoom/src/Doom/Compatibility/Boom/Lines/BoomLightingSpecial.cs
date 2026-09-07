namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomLightingTarget
{
    Light35,
    Light255,
    MaximumNeighbor,
    MinimumNeighbor,
    Blinking
}

public readonly struct BoomLightingSpecial
{
    public BoomLightingSpecial(BoomLightingTarget target, BoomTriggerType trigger)
    {
        Target = target;
        Trigger = trigger;
    }

    public BoomLightingTarget Target { get; }

    public BoomTriggerType Trigger { get; }

    public bool Repeatable => Trigger.IsRepeatable();
}
