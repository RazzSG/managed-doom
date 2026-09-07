namespace ManagedDoom.Compatibility.Boom.Rendering;

// Boom R_ProjectSprite rejects sprites that are completely separated from the
// viewer by a transfer-height fake floor or fake ceiling. This is deliberately
// separate from R_FakeFlat's view-zone resolver: the original sprite test uses
// strict < / > camera comparisons at the control planes.
public static class BoomTransferHeightSpriteVisibility
{
    public static BoomTransferHeightZone ResolveViewZone(
        Sector viewSector,
        Fixed viewZ,
        Fixed frameFrac)
    {
        var control = viewSector?.HeightSector;
        if (control == null)
            return BoomTransferHeightZone.Normal;

        if (viewZ < control.GetInterpolatedFloorHeight(frameFrac))
            return BoomTransferHeightZone.BelowFakeFloor;

        if (viewZ > control.GetInterpolatedCeilingHeight(frameFrac))
            return BoomTransferHeightZone.AboveFakeCeiling;

        return BoomTransferHeightZone.Normal;
    }

    public static bool IsVisible(
        BoomTransferHeightZone viewZone,
        Fixed viewZ,
        Fixed fakeFloor,
        Fixed fakeCeiling,
        Fixed thingZ,
        Fixed spriteTopZ)
    {
        // Boom uses the thing origin (fz), not the bottom of the patch image,
        // for the underwater side of this test. On the normal side, only a
        // sprite whose top is completely below the fake floor is rejected.
        if (viewZone == BoomTransferHeightZone.BelowFakeFloor
            ? thingZ >= fakeFloor
            : spriteTopZ < fakeFloor)
        {
            return false;
        }

        // The extra viewZ >= target fake ceiling condition is intentional.
        // The player's own control ceiling can differ from the target sector's.
        if (viewZone == BoomTransferHeightZone.AboveFakeCeiling
            ? spriteTopZ < fakeCeiling && viewZ >= fakeCeiling
            : thingZ >= fakeCeiling)
        {
            return false;
        }

        return true;
    }
}
