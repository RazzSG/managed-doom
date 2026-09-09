namespace ManagedDoom.Compatibility.Mbf.Rendering;

/// <summary>
/// Selects MBF comp_skymap semantics for sky rendering.
///
/// Doom/Boom draw skies fullbright and ignore fixed colormaps. MBF changed the
/// default so fixed colormaps (most notably invulnerability) affect the sky;
/// comp_skymap = 1 intentionally restores the Doom-compatible fullbright path.
/// </summary>
public static class MbfSkyMapCompatibility
{
    public static bool UsesFixedColorMap(
        GameCompatibility compatibility,
        bool compSkyMap,
        int fixedColorMap) =>
        fixedColorMap != 0 &&
        GameCompatibilityFeatures.SupportsMbfSkyMapCompatibility(compatibility) &&
        !compSkyMap;

    public static int ResolveSkyColorMapIndex(
        GameCompatibility compatibility,
        bool compSkyMap,
        int fixedColorMap) =>
        UsesFixedColorMap(compatibility, compSkyMap, fixedColorMap)
            ? fixedColorMap
            : 0;
}
