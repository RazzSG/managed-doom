namespace ManagedDoom.DefinitionPatches.DeHackEd;

/// <summary>
/// Original executable state indices used as source entries by classic
/// DeHackEd Pointer blocks. ManagedDoom intentionally keeps a compact runtime
/// state table, so a few original MBF source states need action aliases.
/// </summary>
internal static class DeHackEdCodePointerSources
{
    public static bool TryGetActionName(int sourceFrameNumber, out string actionName)
    {
        switch (sourceFrameNumber)
        {
            // MBF states inserted immediately after the vanilla state table.
            case 968:
                actionName = "Die";
                return true;
            case 969:
                actionName = "Scream";
                return true;
            case 970:
                actionName = "Detonate";
                return true;

            // Original MBF beta-state region omitted from ManagedDoom's compact
            // runtime state table. These implemented action sources still have
            // stable classic DeHackEd frame numbers in MBF patches.
            case 1062:
                actionName = "BetaSkullAttack";
                return true;
            case 1074:
                actionName = "Stop";
                return true;
            case 1075:
                actionName = "Mushroom";
                return true;
            default:
                actionName = null;
                return false;
        }
    }
}
