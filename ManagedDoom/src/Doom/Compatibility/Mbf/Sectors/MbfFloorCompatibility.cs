using ManagedDoom.Compatibility.Boom.Sectors;

namespace ManagedDoom.Compatibility.Mbf.Sectors;

/// <summary>
/// Selects the floor/plane movement semantics controlled by MBF comp_floors.
/// MBF defaults to Boom's corrected behavior; comp_floors = 1 intentionally
/// restores Doom's original floor-motion quirks for demo/WAD compatibility.
/// </summary>
public static class MbfFloorCompatibility
{
    public static bool UsesDoomFloorSemantics(
        GameCompatibility compatibility,
        bool compFloors)
    {
        if (GameCompatibilityFeatures.SupportsMbfFloorCompatibility(compatibility))
            return compFloors;

        return compatibility == GameCompatibility.Vanilla;
    }

    public static bool ShouldClampRaisingFloorDestinationToCeiling(
        GameCompatibility compatibility,
        bool compFloors) =>
        !UsesDoomFloorSemantics(compatibility, compFloors);

    public static bool ShouldClampLoweringCeilingDestinationToFloor(
        GameCompatibility compatibility,
        bool compFloors) =>
        !UsesDoomFloorSemantics(compatibility, compFloors);

    public static bool ShouldRestoreIntermediateLoweringFloorAfterNoFit(
        GameCompatibility compatibility,
        bool compFloors) =>
        BoomSectorMovementQuirks.ShouldRestoreIntermediateLoweringFloorAfterNoFit(
            ResolveFloorCompatibility(compatibility, compFloors));

    public static bool ShouldRestoreIntermediateRaisingFloorAfterNoFit(
        GameCompatibility compatibility,
        bool compFloors,
        bool crush) =>
        BoomSectorMovementQuirks.ShouldRestoreIntermediateRaisingFloorAfterNoFit(
            ResolveFloorCompatibility(compatibility, compFloors),
            crush);

    public static bool RemovesBouncedPureRaisePlatform(
        GameCompatibility compatibility,
        bool compFloors) =>
        BoomClassicMoverCompatibility.RemovesBouncedPureRaisePlatform(
            ResolveFloorCompatibility(compatibility, compFloors));

    public static bool BlocksDonutWhenPoolFloorIsBusy(
        GameCompatibility compatibility,
        bool compFloors) =>
        !UsesDoomFloorSemantics(compatibility, compFloors);

    private static GameCompatibility ResolveFloorCompatibility(
        GameCompatibility compatibility,
        bool compFloors) =>
        UsesDoomFloorSemantics(compatibility, compFloors)
            ? GameCompatibility.Vanilla
            : GameCompatibility.Boom;
}
