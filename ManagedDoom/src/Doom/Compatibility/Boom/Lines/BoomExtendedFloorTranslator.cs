namespace ManagedDoom.Compatibility.Boom.Lines;

/// <summary>
/// Translates the non-generalized Boom floor linedefs which extend the
/// regular Doom trigger matrix. The resulting specification is executed by
/// the same floor path used by generalized Boom floors.
/// </summary>
public static class BoomExtendedFloorTranslator
{
    public static bool TryTranslate(LineSpecial special, out BoomFloorSpecial specification)
    {
        specification = (int)special switch
        {
            177 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Down, BoomFloorTarget.LowestNeighborFloor,
                BoomChangeType.TextureAndSpecial, BoomModelType.Numeric),
            159 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Down, BoomFloorTarget.LowestNeighborFloor,
                BoomChangeType.TextureAndSpecial, BoomModelType.Numeric),

            222 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Down, BoomFloorTarget.NextNeighborFloor),
            221 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Down, BoomFloorTarget.NextNeighborFloor),
            220 => Create(BoomTriggerType.WalkRepeat, BoomPlaneDirection.Down, BoomFloorTarget.NextNeighborFloor),
            219 => Create(BoomTriggerType.WalkOnce, BoomPlaneDirection.Down, BoomFloorTarget.NextNeighborFloor),

            180 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Up, BoomFloorTarget.By24),
            161 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Up, BoomFloorTarget.By24),

            179 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Up, BoomFloorTarget.By24,
                BoomChangeType.TextureAndSpecial, BoomModelType.Trigger),
            160 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Up, BoomFloorTarget.By24,
                BoomChangeType.TextureAndSpecial, BoomModelType.Trigger),

            176 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Up, BoomFloorTarget.ShortestLowerTexture),
            158 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Up, BoomFloorTarget.ShortestLowerTexture),

            178 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Up, BoomFloorTarget.By512),
            147 => Create(BoomTriggerType.WalkRepeat, BoomPlaneDirection.Up, BoomFloorTarget.By512),
            142 => Create(BoomTriggerType.WalkOnce, BoomPlaneDirection.Up, BoomFloorTarget.By512),

            190 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Up, BoomFloorTarget.None,
                BoomChangeType.TextureAndSpecial, BoomModelType.Trigger),
            189 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Up, BoomFloorTarget.None,
                BoomChangeType.TextureAndSpecial, BoomModelType.Trigger),
            154 => Create(BoomTriggerType.WalkRepeat, BoomPlaneDirection.Up, BoomFloorTarget.None,
                BoomChangeType.TextureAndSpecial, BoomModelType.Trigger),
            153 => Create(BoomTriggerType.WalkOnce, BoomPlaneDirection.Up, BoomFloorTarget.None,
                BoomChangeType.TextureAndSpecial, BoomModelType.Trigger),

            78 => Create(BoomTriggerType.SwitchRepeat, BoomPlaneDirection.Up, BoomFloorTarget.None,
                BoomChangeType.TextureAndSpecial, BoomModelType.Numeric),
            241 => Create(BoomTriggerType.SwitchOnce, BoomPlaneDirection.Up, BoomFloorTarget.None,
                BoomChangeType.TextureAndSpecial, BoomModelType.Numeric),
            240 => Create(BoomTriggerType.WalkRepeat, BoomPlaneDirection.Up, BoomFloorTarget.None,
                BoomChangeType.TextureAndSpecial, BoomModelType.Numeric),
            239 => Create(BoomTriggerType.WalkOnce, BoomPlaneDirection.Up, BoomFloorTarget.None,
                BoomChangeType.TextureAndSpecial, BoomModelType.Numeric),

            _ => default
        };

        return IsExtendedFloorSpecial(special);
    }

    public static BoomFloorSpecial Translate(LineSpecial special)
    {
        if (!TryTranslate(special, out var specification))
            throw new System.ArgumentOutOfRangeException(nameof(special), special, "Not an extended Boom floor special.");

        return specification;
    }

    public static bool IsExtendedFloorSpecial(LineSpecial special)
    {
        return (int)special is
            177 or 159 or
            222 or 221 or 220 or 219 or
            180 or 161 or
            179 or 160 or
            176 or 158 or
            178 or 147 or 142 or
            190 or 189 or 154 or 153 or
            78 or 241 or 240 or 239;
    }

    private static BoomFloorSpecial Create(
        BoomTriggerType trigger,
        BoomPlaneDirection direction,
        BoomFloorTarget target,
        BoomChangeType change = BoomChangeType.None,
        BoomModelType model = BoomModelType.Trigger)
    {
        return new BoomFloorSpecial(
            new BoomActionSpecification(trigger, BoomActionSpeed.Slow, false),
            direction,
            target,
            change,
            model,
            false);
    }
}
