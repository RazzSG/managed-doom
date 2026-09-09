using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.Things;

/// <summary>
/// MBF state-control actor code pointers that use the legacy Misc1/Misc2
/// state fields: A_Turn, A_Face and A_RandomJump.
/// </summary>
public static class MbfStateControlCodePointers
{
    public static bool TurnFromState(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        actor.Angle += FromMbfDegrees(actor.State.Misc1);
        return true;
    }

    public static bool FaceFromState(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        actor.Angle = FromMbfDegrees(actor.State.Misc1);
        return true;
    }

    public static bool RandomJumpFromState(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        var stateNumber = actor.State.Misc1;
        if ((uint)stateNumber >= (uint)DoomInfo.States.Length)
            return false;

        if (world.Random.Next() >= actor.State.Misc2)
            return false;

        actor.SetState((MobjState)stateNumber);
        return true;
    }

    private static bool CanExecute(World world, Mobj actor)
    {
        return world != null &&
               actor != null &&
               actor.State != null &&
               GameCompatibilityFeatures.SupportsMbfStateControlCodePointers(world.Options.Compatibility);
    }

    private static Angle FromMbfDegrees(int degrees)
    {
        // Match MBF/PrBoom integer angle conversion exactly. Angle.FromDegree
        // rounds through double, while the original code truncates the 64-bit
        // integer quotient.
        var numerator = unchecked((ulong)(uint)degrees << 32);
        return new Angle((uint)(numerator / 360UL));
    }
}
