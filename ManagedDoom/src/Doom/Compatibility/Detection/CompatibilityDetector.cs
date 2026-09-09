using System;

using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Detection;

public static class CompatibilityDetector
{
    public static CompatibilityDetectionResult Detect(Wad wad) =>
        Detect(wad, null);

    public static CompatibilityDetectionResult Detect(Wad wad, CommandLineArgs args)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));

        if (ComplvlReader.TryRead(wad, out var compatibility))
            return new CompatibilityDetectionResult(compatibility, CompatibilityDetectionSource.Complvl);

        if (CompatibilityFeatureScanner.TryDetect(wad, args, out compatibility))
            return new CompatibilityDetectionResult(compatibility, CompatibilityDetectionSource.FeatureScan);

        return new CompatibilityDetectionResult(GameCompatibility.Vanilla, CompatibilityDetectionSource.Default);
    }
}
