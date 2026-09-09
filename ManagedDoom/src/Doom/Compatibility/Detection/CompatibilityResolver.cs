using System;

using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Detection;

public static class CompatibilityResolver
{
    public static CompatibilityDetectionResult Resolve(Wad wad, GameCompatibilityMode mode) =>
        Resolve(wad, mode, null);

    public static CompatibilityDetectionResult Resolve(
        Wad wad,
        GameCompatibilityMode mode,
        CommandLineArgs args)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));

        if (mode == GameCompatibilityMode.Auto)
            return CompatibilityDetector.Detect(wad, args);

        var compatibility = mode switch
        {
            GameCompatibilityMode.Vanilla => GameCompatibility.Vanilla,
            GameCompatibilityMode.Boom => GameCompatibility.Boom,
            GameCompatibilityMode.Mbf => GameCompatibility.Mbf,
            GameCompatibilityMode.Mbf21 => GameCompatibility.Mbf21,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        return new CompatibilityDetectionResult(compatibility, CompatibilityDetectionSource.UserOverride);
    }
}
