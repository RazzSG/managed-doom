namespace ManagedDoom.Compatibility.Mbf.Rendering;

/// <summary>
/// PrBoom compatibility selector for Doom's two status-bar OUCH-face bugs:
/// the health delta was reversed, and monster damage could overwrite the
/// OUCH face on the following tic because its priority stayed too low.
/// </summary>
public static class MbfOuchFaceCompatibility
{
    public static bool UsesBuggyCode(
        GameCompatibility compatibility,
        bool compOuchFace)
    {
        if (!GameCompatibilityFeatures.SupportsMbfOuchFaceCompatibility(compatibility))
            return true;

        // MBF21 deoptionalizes comp_ouchface and always keeps the fixes enabled.
        if (GameCompatibilityFeatures.SupportsMbf21(compatibility))
            return false;

        return compOuchFace;
    }

    public static bool ShouldShowOuchFace(
        GameCompatibility compatibility,
        bool compOuchFace,
        int currentHealth,
        int previousHealth,
        int muchPain)
    {
        var delta = UsesBuggyCode(compatibility, compOuchFace)
            ? currentHealth - previousHealth
            : previousHealth - currentHealth;

        return delta > muchPain;
    }

    public static int ResolveMonsterDamagePriority(
        GameCompatibility compatibility,
        bool compOuchFace,
        int legacyPriority,
        int fixedPriority)
    {
        return UsesBuggyCode(compatibility, compOuchFace)
            ? legacyPriority
            : fixedPriority;
    }
}
