namespace ManagedDoom.Compatibility.Boom.Lines;

public enum BoomTeleportDestination
{
    Thing,
    Line
}

public readonly struct BoomTeleportSpecial
{
    public BoomTeleportSpecial(
        BoomTriggerType trigger,
        BoomTeleportDestination destination,
        bool silent,
        bool preserveOrientation,
        bool reverse,
        bool playersAllowed,
        bool allowsZeroTag)
    {
        Trigger = trigger;
        Destination = destination;
        Silent = silent;
        PreserveOrientation = preserveOrientation;
        Reverse = reverse;
        PlayersAllowed = playersAllowed;
        AllowsZeroTag = allowsZeroTag;
    }

    public BoomTriggerType Trigger { get; }

    public BoomTeleportDestination Destination { get; }

    public bool Silent { get; }

    public bool PreserveOrientation { get; }

    public bool Reverse { get; }

    public bool PlayersAllowed { get; }

    public bool MonstersAllowed => true;

    public bool AllowsZeroTag { get; }

    public bool Repeatable => Trigger.IsRepeatable();
}
