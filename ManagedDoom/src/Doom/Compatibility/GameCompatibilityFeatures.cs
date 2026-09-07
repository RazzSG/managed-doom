namespace ManagedDoom.Compatibility;

public static class GameCompatibilityFeatures
{
    public static bool SupportsBoom(GameCompatibility compatibility) => (int)compatibility >= (int)GameCompatibility.Boom;

    public static bool SupportsBoomLineSpecials(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsBoomPassThru(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsGeneralizedSectorSpecials(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsTransferHeights(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsTranslucentLines(GameCompatibility compatibility) => SupportsBoom(compatibility);

    public static bool SupportsMbf(GameCompatibility compatibility) => (int)compatibility >= (int)GameCompatibility.Mbf;

    public static bool SupportsMbf21(GameCompatibility compatibility) => (int)compatibility >= (int)GameCompatibility.Mbf21;
}
