namespace ManagedDoom.Compatibility.Mbf.Rendering;

/// <summary>
/// PrBoom comp_translucency compatibility for the 17 Things that Boom made
/// translucent by default. DeHackEd Bits overrides always win, matching
/// deh_changeCompTranslucency(): the compatibility option only edits actors
/// whose Bits were not replaced by a patch.
/// </summary>
public static class MbfTranslucencyCompatibility
{
    public const uint CanonicalTranslucentBit = 0x80000000u;

    public static bool UsesPredefinedTranslucency(
        GameCompatibility compatibility,
        bool compTranslucency)
    {
        if (!GameCompatibilityFeatures.SupportsTranslucentSprites(compatibility))
            return false;

        // Boom introduced the predefined translucent Things before this became
        // a user-selectable PrBoom compatibility option in our MBF layer.
        if (!GameCompatibilityFeatures.SupportsMbfTranslucencyCompatibility(compatibility))
            return true;

        // MBF21 deoptionalizes comp_translucency and forces the fixed value 0.
        if (GameCompatibilityFeatures.SupportsMbf21(compatibility))
            return true;

        return !compTranslucency;
    }

    public static bool ResolveActorTranslucency(
        GameCompatibility compatibility,
        bool compTranslucency,
        MobjType type,
        MobjInfo info)
    {
        if (info == null)
            return false;

        // PrBoom's DEH_mobjinfo_bits[] guard means an explicit Thing/Bits patch
        // is never overwritten by comp_translucency. The renderer sees that bit
        // even under an older compatibility level.
        if (info.HasDeHackEdBitsOverride)
            return info.Translucent;

        if (!GameCompatibilityFeatures.SupportsTranslucentSprites(compatibility))
            return false;

        return IsPredefinedTranslucentType(type) &&
               UsesPredefinedTranslucency(compatibility, compTranslucency);
    }

    public static bool IsPredefinedTranslucentType(MobjType type)
    {
        return type is
            MobjType.Fire or
            MobjType.Smoke or
            MobjType.Fatshot or
            MobjType.Bruisershot or
            MobjType.Spawnfire or
            MobjType.Troopshot or
            MobjType.Headshot or
            MobjType.Plasma or
            MobjType.Bfg or
            MobjType.Arachplaz or
            MobjType.Puff or
            MobjType.Tfog or
            MobjType.Ifog or
            MobjType.Misc12 or
            MobjType.Inv or
            MobjType.Ins or
            MobjType.Mega;
    }
}
