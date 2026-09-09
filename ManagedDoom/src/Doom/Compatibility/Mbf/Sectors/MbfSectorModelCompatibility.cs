using ManagedDoom.Compatibility.Boom.Sectors;

namespace ManagedDoom.Compatibility.Mbf.Sectors;

/// <summary>
/// Selects the sector/linedef model semantics used by MBF comp_model.
/// MBF defaults to Boom's corrected model; comp_model = 1 intentionally
/// restores Doom's original trigger-model quirks.
/// </summary>
public static class MbfSectorModelCompatibility
{
    public static bool UsesFixedModelSemantics(
        GameCompatibility compatibility,
        bool compModel)
    {
        if (!GameCompatibilityFeatures.SupportsMbfSectorModelCompatibility(compatibility))
            return BoomSectorModelCompatibility.UsesFixedModelSemantics(compatibility);

        return !compModel;
    }

    public static bool IsTwoSided(
        LineDef line,
        GameCompatibility compatibility,
        bool compModel) =>
        BoomSectorModelCompatibility.IsTwoSided(
            line, ResolveModelCompatibility(compatibility, compModel));

    public static Sector GetNextSector(
        LineDef line,
        Sector sector,
        GameCompatibility compatibility,
        bool compModel) =>
        BoomSectorModelCompatibility.GetNextSector(
            line, sector, ResolveModelCompatibility(compatibility, compModel));

    public static Fixed HighestFloorInitial(
        GameCompatibility compatibility,
        bool compModel) =>
        BoomSectorModelCompatibility.HighestFloorInitial(
            ResolveModelCompatibility(compatibility, compModel));

    public static Fixed LowestCeilingInitial(
        GameCompatibility compatibility,
        bool compModel) =>
        BoomSectorModelCompatibility.LowestCeilingInitial(
            ResolveModelCompatibility(compatibility, compModel));

    public static Fixed HighestCeilingInitial(
        GameCompatibility compatibility,
        bool compModel) =>
        BoomSectorModelCompatibility.HighestCeilingInitial(
            ResolveModelCompatibility(compatibility, compModel));

    public static int ShortestTextureInitial(
        GameCompatibility compatibility,
        bool compModel) =>
        BoomSectorModelCompatibility.ShortestTextureInitial(
            ResolveModelCompatibility(compatibility, compModel));

    public static bool IsUsableShortestTexture(
        int textureNumber,
        GameCompatibility compatibility,
        bool compModel) =>
        BoomSectorModelCompatibility.IsUsableShortestTexture(
            textureNumber, ResolveModelCompatibility(compatibility, compModel));

    public static Fixed AddShortestTextureHeight(
        Fixed currentHeight,
        int textureHeight,
        GameCompatibility compatibility,
        bool compModel) =>
        BoomSectorModelCompatibility.AddShortestTextureHeight(
            currentHeight, textureHeight, ResolveModelCompatibility(compatibility, compModel));

    private static GameCompatibility ResolveModelCompatibility(
        GameCompatibility compatibility,
        bool compModel) =>
        UsesFixedModelSemantics(compatibility, compModel)
            ? GameCompatibility.Boom
            : GameCompatibility.Vanilla;
}
