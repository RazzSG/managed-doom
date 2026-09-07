namespace ManagedDoom.Compatibility.Boom.Lines;

/// <summary>
/// Extended Boom donut action. The underlying movement is the existing
/// Doom donut behavior; Boom only adds additional trigger variants.
/// </summary>
public readonly struct BoomDonutSpecial
{
    public BoomDonutSpecial(BoomTriggerType trigger)
    {
        Trigger = trigger;
    }

    public BoomTriggerType Trigger { get; }

    public bool Repeatable => Trigger.IsRepeatable();
}
