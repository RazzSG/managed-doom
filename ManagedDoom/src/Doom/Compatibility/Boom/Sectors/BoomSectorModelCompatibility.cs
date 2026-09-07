using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Sectors;

/// <summary>
/// Boom compatibility fixes for the sector-model helpers from p_spec.c.
/// Vanilla Doom trusted ML_TWOSIDED and allowed an intra-sector line to model
/// the sector itself. Boom instead uses the actual presence of a second
/// sidedef, ignores self-referencing lines for neighbor queries, and clamps
/// several sentinel values to the safe +/-32000 map-height range.
/// </summary>
public static class BoomSectorModelCompatibility
{
    private static readonly Fixed BoomMinHeight = Fixed.FromInt(-32000);
    private static readonly Fixed BoomMaxHeight = Fixed.FromInt(32000);

    public static bool UsesFixedModelSemantics(GameCompatibility compatibility) =>
        GameCompatibilityFeatures.SupportsBoom(compatibility);

    public static bool IsTwoSided(LineDef line, GameCompatibility compatibility)
    {
        if (!UsesFixedModelSemantics(compatibility))
            return (line.Flags & LineFlags.TwoSided) != 0;

        return line.FrontSide != null && line.BackSide != null;
    }

    public static Sector GetNextSector(LineDef line, Sector sector, GameCompatibility compatibility)
    {
        if (!IsTwoSided(line, compatibility))
            return null;

        if (line.FrontSector == sector)
        {
            if (UsesFixedModelSemantics(compatibility) && line.BackSector == sector)
                return null;

            return line.BackSector;
        }

        return line.FrontSector;
    }

    public static Fixed HighestFloorInitial(GameCompatibility compatibility) =>
        UsesFixedModelSemantics(compatibility) ? BoomMinHeight : Fixed.FromInt(-500);

    public static Fixed LowestCeilingInitial(GameCompatibility compatibility) =>
        UsesFixedModelSemantics(compatibility) ? BoomMaxHeight : Fixed.MaxValue;

    public static Fixed HighestCeilingInitial(GameCompatibility compatibility) =>
        UsesFixedModelSemantics(compatibility) ? BoomMinHeight : Fixed.Zero;

    public static int ShortestTextureInitial(GameCompatibility compatibility) =>
        UsesFixedModelSemantics(compatibility) ? 32000 : int.MaxValue;

    public static bool IsUsableShortestTexture(int textureNumber, GameCompatibility compatibility)
    {
        // Doom's '-' placeholder is texture number 0 in ManagedDoom. Boom skips
        // it for shortest-texture searches, while the Vanilla path keeps the
        // original ManagedDoom behavior unchanged.
        return UsesFixedModelSemantics(compatibility)
            ? textureNumber > 0
            : textureNumber >= 0;
    }

    public static Fixed AddShortestTextureHeight(Fixed currentHeight, int textureHeight, GameCompatibility compatibility)
    {
        if (!UsesFixedModelSemantics(compatibility))
            return currentHeight + Fixed.FromInt(textureHeight);

        var height = currentHeight.ToIntFloor() + textureHeight;
        if (height > 32000)
            height = 32000;

        return Fixed.FromInt(height);
    }
}
