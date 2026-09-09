using ManagedDoom.Compatibility.Boom.Lines;

namespace ManagedDoom.Compatibility.Mbf.Lines;

/// <summary>
/// MBF selector for the regular-linedef P_CheckTag compatibility option.
/// comp_zerotags only relaxes the regular walk/use/shoot zero-tag gate;
/// generalized Boom linedefs keep their own explicit tag requirements.
/// </summary>
public static class MbfZeroTagCompatibility
{
    public static bool CanActivateRegularLine(
        LineDef line,
        GameCompatibility compatibility,
        bool compZeroTags)
    {
        if (GameCompatibilityFeatures.SupportsMbf(compatibility) && compZeroTags)
            return true;

        return BoomTagRules.CanActivate(line, compatibility);
    }
}
