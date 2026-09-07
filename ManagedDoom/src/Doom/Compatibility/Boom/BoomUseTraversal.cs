namespace ManagedDoom.Compatibility.Boom;

/// <summary>
/// Boom-specific continuation rule for the player use trace.
/// </summary>
public static class BoomUseTraversal
{
    public static bool ShouldContinueAfterActivation(LineDef line, GameCompatibility compatibility)
    {
        return line != null &&
            GameCompatibilityFeatures.SupportsBoomPassThru(compatibility) &&
            (line.Flags & LineFlags.PassThru) != 0;
    }
}
