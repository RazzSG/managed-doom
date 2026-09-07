namespace ManagedDoom.Compatibility.Boom.Lines;

/// <summary>
/// Translates the regular-numbered Boom ceiling extensions into the common
/// Boom ceiling specification consumed by SectorAction.
/// </summary>
public static class BoomExtendedCeilingTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomCeilingSpecial specification)
    {
        specification = (int)special switch
        {
            152 => Create(BoomTriggerType.WalkRepeat, BoomPlaneDirection.Down, BoomCeilingTarget.Floor, BoomActionSpeed.Fast),
            145 => Create(BoomTriggerType.WalkOnce, BoomPlaneDirection.Down, BoomCeilingTarget.Floor, BoomActionSpeed.Fast),

            186 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Up, BoomCeilingTarget.HighestNeighborCeiling),
            166 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Up, BoomCeilingTarget.HighestNeighborCeiling),
            151 => Create(BoomTriggerType.WalkRepeat, BoomPlaneDirection.Up, BoomCeilingTarget.HighestNeighborCeiling),

            187 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Down, BoomCeilingTarget.FloorPlus8),
            167 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Down, BoomCeilingTarget.FloorPlus8),

            205 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Down, BoomCeilingTarget.LowestNeighborCeiling),
            203 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Down, BoomCeilingTarget.LowestNeighborCeiling),
            201 => Create(BoomTriggerType.WalkRepeat, BoomPlaneDirection.Down, BoomCeilingTarget.LowestNeighborCeiling),
            199 => Create(BoomTriggerType.WalkOnce, BoomPlaneDirection.Down, BoomCeilingTarget.LowestNeighborCeiling),

            206 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Down, BoomCeilingTarget.HighestNeighborFloor),
            204 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Down, BoomCeilingTarget.HighestNeighborFloor),
            202 => Create(BoomTriggerType.WalkRepeat, BoomPlaneDirection.Down, BoomCeilingTarget.HighestNeighborFloor),
            200 => Create(BoomTriggerType.WalkOnce, BoomPlaneDirection.Down, BoomCeilingTarget.HighestNeighborFloor),

            _ => default
        };

        return IsExtendedCeilingSpecial(special);
    }

    public static BoomCeilingSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new System.ArgumentOutOfRangeException(nameof(special), special, "Not an extended Boom ceiling special.");

        return specification;
    }

    public static bool IsExtendedCeilingSpecial(LineSpecial special)
    {
        return (int)special is
            152 or 145 or
            186 or 166 or 151 or
            187 or 167 or
            205 or 203 or 201 or 199 or
            206 or 204 or 202 or 200;
    }

    private static BoomCeilingSpecial Create(
        BoomTriggerType trigger,
        BoomPlaneDirection direction,
        BoomCeilingTarget target,
        BoomActionSpeed speed = BoomActionSpeed.Slow)
    {
        return new BoomCeilingSpecial(
            new BoomActionSpecification(trigger, speed, false),
            direction,
            target,
            BoomChangeType.None,
            BoomModelType.Trigger,
            false);
    }
}
