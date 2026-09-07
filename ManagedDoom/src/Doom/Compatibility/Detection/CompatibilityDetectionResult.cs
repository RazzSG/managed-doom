using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Detection;

public enum CompatibilityDetectionSource
{
    UserOverride,
    Complvl,
    FeatureScan,
    Default
}

public readonly struct CompatibilityDetectionResult(GameCompatibility compatibility, CompatibilityDetectionSource source)
{
    public GameCompatibility Compatibility { get; } = compatibility;

    public CompatibilityDetectionSource Source { get; } = source;
}
