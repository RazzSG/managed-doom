namespace ManagedDoom.Compatibility.Boom.Rendering;

public static class BoomTransferHeightResolver
{
    public const int TransferHeightsSpecial = 242;

    // Linedef 242 is static map setup. Resolve only the control-sector references here;
    // the control heights themselves stay live so moving control sectors remain dynamic.
    public static void Apply(World world)
    {
        foreach (var sector in world.Map.Sectors)
        {
            sector.HeightSector = null;
            sector.HeightSectorLine = null;
        }

        if (!GameCompatibilityFeatures.SupportsTransferHeights(world.Options.Compatibility))
            return;

        foreach (var line in world.Map.Lines)
        {
            if ((int)line.Special != TransferHeightsSpecial)
                continue;

            PrepareColormapTextureSlots(line.FrontSide);

            var control = line.FrontSector;
            if (control == null)
                continue;

            var targets = world.Map.BoomTags.GetSectors(line.Tag);
            for (var i = 0; i < targets.Length; i++)
            {
                // Boom scans linedefs in map order, so a later 242 naturally wins
                // if several control lines target the same sector.
                targets[i].HeightSector = control;
                targets[i].HeightSectorLine = line;
            }
        }
    }


    // Boom 242 overloads the front sidedef texture names with colormap names.
    // Names that were not valid wall textures must not survive as -1 texture
    // indices, otherwise an otherwise harmless control line can reach normal
    // wall rendering and index the texture table with a negative value.
    private static void PrepareColormapTextureSlots(SideDef side)
    {
        if (side == null)
            return;

        if (side.TopTexture < 0)
            side.TopTexture = 0;

        if (side.MiddleTexture < 0)
            side.MiddleTexture = 0;

        if (side.BottomTexture < 0)
            side.BottomTexture = 0;
    }

    // Boom's three 242 view partitions select colormaps from the defining
    // front sidedef: middle = normal space, bottom = below fake floor,
    // top = above fake ceiling. A valid wall texture keeps its normal texture
    // meaning and therefore implies the default COLORMAP instead.
    public static string ResolveColorMapName(Sector viewSector, BoomTransferHeightZone viewZone)
    {
        var side = viewSector?.HeightSectorLine?.FrontSide;
        if (side == null)
            return null;

        return viewZone switch
        {
            BoomTransferHeightZone.BelowFakeFloor => GetColorMapName(
                side.BottomTextureName,
                side.BottomTextureIsWallTexture),

            BoomTransferHeightZone.AboveFakeCeiling => GetColorMapName(
                side.TopTextureName,
                side.TopTextureIsWallTexture),

            _ => GetColorMapName(
                side.MiddleTextureName,
                side.MiddleTextureIsWallTexture)
        };
    }

    private static string GetColorMapName(string name, bool isWallTexture)
    {
        if (isWallTexture || string.IsNullOrEmpty(name) || name[0] == '-')
            return null;

        return name;
    }

    // Boom's R_FakeFlat chooses the global viewing side from the player's own
    // height-transfer sector, not independently from every sector being drawn.
    // The 'back' flag reproduces the special underwater back-sector geometry used
    // to keep two-sided portals from becoming false solid walls.
    public static BoomTransferHeightZone ResolveViewZone(
        Sector viewSector,
        Fixed viewZ,
        Fixed frameFrac)
    {
        var control = viewSector?.HeightSector;
        if (control == null)
            return BoomTransferHeightZone.Normal;

        if (viewZ <= control.GetInterpolatedFloorHeight(frameFrac))
            return BoomTransferHeightZone.BelowFakeFloor;

        if (viewZ >= control.GetInterpolatedCeilingHeight(frameFrac))
            return BoomTransferHeightZone.AboveFakeCeiling;

        return BoomTransferHeightZone.Normal;
    }

    public static BoomTransferHeightRenderState Resolve(
        Sector sector,
        BoomTransferHeightZone viewZone,
        Fixed frameFrac,
        int skyFlatNumber,
        bool back)
    {
        var control = sector.HeightSector;
        if (control == null)
            return ResolveNormalSector(sector, frameFrac);

        var realFloor = sector.GetInterpolatedFloorHeight(frameFrac);
        var realCeiling = sector.GetInterpolatedCeilingHeight(frameFrac);
        var fakeFloor = control.GetInterpolatedFloorHeight(frameFrac);
        var fakeCeiling = control.GetInterpolatedCeilingHeight(frameFrac);

        var belowFakeFloor = viewZone == BoomTransferHeightZone.BelowFakeFloor;
        var aboveFakeCeiling = viewZone == BoomTransferHeightZone.AboveFakeCeiling;

        // Default 242 view: target appearance rendered at the control heights.
        var floorHeight = fakeFloor;
        var ceilingHeight = fakeCeiling;
        var floorFlat = sector.FloorFlat;
        var ceilingFlat = sector.CeilingFlat;
        var floorPlaneSector = sector;
        var ceilingPlaneSector = sector;
        var lightLevel = sector.LightLevel;
        var floorLightLevel = sector.FloorLightLevel;
        var ceilingLightLevel = sector.CeilingLightLevel;
        var zone = BoomTransferHeightZone.Normal;

        if (belowFakeFloor)
        {
            // R_FakeFlat performs these height assignments even for a back sector.
            floorHeight = realFloor;
            ceilingHeight = fakeFloor - Fixed.Epsilon;
            zone = BoomTransferHeightZone.BelowFakeFloor;

            if (!back)
            {
                floorFlat = control.FloorFlat;
                floorPlaneSector = control;

                if (control.CeilingFlat == skyFlatNumber)
                {
                    // Original Boom sky-water special case: collapse the fake gap
                    // at the control floor and mirror its floor appearance above it.
                    floorHeight = ceilingHeight + Fixed.Epsilon;
                    ceilingFlat = floorFlat;
                    ceilingPlaneSector = floorPlaneSector;
                }
                else
                {
                    ceilingFlat = control.CeilingFlat;
                    ceilingPlaneSector = control;
                }

                lightLevel = control.LightLevel;
                floorLightLevel = control.FloorLightLevel;
                ceilingLightLevel = control.CeilingLightLevel;
            }
        }
        else if (aboveFakeCeiling && realCeiling > fakeCeiling)
        {
            // Reflected fake-ceiling case from Boom's R_FakeFlat.
            ceilingHeight = fakeCeiling;
            floorHeight = fakeCeiling + Fixed.Epsilon;
            floorFlat = control.CeilingFlat;
            ceilingFlat = control.CeilingFlat;
            floorPlaneSector = control;
            ceilingPlaneSector = control;
            zone = BoomTransferHeightZone.AboveFakeCeiling;

            if (control.FloorFlat != skyFlatNumber)
            {
                ceilingHeight = realCeiling;
                floorFlat = control.FloorFlat;
                floorPlaneSector = control;
            }

            lightLevel = control.LightLevel;
            floorLightLevel = control.FloorLightLevel;
            ceilingLightLevel = control.CeilingLightLevel;
        }

        return new BoomTransferHeightRenderState(
            sector,
            floorPlaneSector,
            ceilingPlaneSector,
            floorHeight,
            ceilingHeight,
            floorFlat,
            ceilingFlat,
            lightLevel,
            floorLightLevel,
            ceilingLightLevel,
            zone,
            true,
            control.CeilingFlat == skyFlatNumber,
            control.FloorFlat == skyFlatNumber);
    }

    public static BoomTransferHeightRenderState ResolveNormalSector(Sector sector, Fixed frameFrac)
    {
        return new BoomTransferHeightRenderState(
            sector,
            sector,
            sector,
            sector.GetInterpolatedFloorHeight(frameFrac),
            sector.GetInterpolatedCeilingHeight(frameFrac),
            sector.FloorFlat,
            sector.CeilingFlat,
            sector.LightLevel,
            sector.FloorLightLevel,
            sector.CeilingLightLevel,
            BoomTransferHeightZone.Normal,
            false);
    }
}
