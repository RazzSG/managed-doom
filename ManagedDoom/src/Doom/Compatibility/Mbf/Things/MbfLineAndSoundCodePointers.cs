using System.Runtime.CompilerServices;
using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf.Things;

/// <summary>
/// MBF state-driven utility code pointers A_PlaySound and A_LineEffect.
/// </summary>
public static class MbfLineAndSoundCodePointers
{
    // PrBoom uses one static line_t for A_LineEffect. Keep one stable synthetic
    // LineDef per World so button identity and the shallow first-line template
    // behavior match without retaining finished worlds forever.
    private static readonly ConditionalWeakTable<World, LineDef> lineEffectLines = new();

    public static bool PlaySoundFromState(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        var sfx = (Sfx)actor.State.Misc1;

        if (actor.State.Misc2 != 0)
            world.Options.Sound.StartSound(sfx);
        else
            world.StartSound(actor, sfx, SfxType.Misc);

        return true;
    }

    public static bool LineEffectFromState(World world, Mobj actor)
    {
        if (!CanExecute(world, actor))
            return false;

        var special = unchecked((short)actor.State.Misc1);
        if (special == 0)
            return true;

        if (world.Map.Lines.Length == 0)
            return false;

        var junk = GetLineEffectLine(world, special, unchecked((short)actor.State.Misc2));

        var oldPlayer = actor.Player;
        var fakePlayer = new Player(0)
        {
            Health = 100,
            Mobj = actor
        };

        actor.Player = fakePlayer;

        try
        {
            if (!world.MapInteraction.UseSpecialLine(actor, junk, 0))
                world.MapInteraction.CrossSpecialLine(junk, 0, actor);

            // The original code writes the synthetic linedef's possibly-cleared
            // special back into the current state. A one-shot special therefore
            // disables later A_LineEffect executions that share this state.
            actor.State.Misc1 = unchecked((short)(int)junk.Special);
        }
        finally
        {
            actor.Player = oldPlayer;
        }

        return true;
    }

    private static LineDef GetLineEffectLine(World world, short special, short tag)
    {
        var template = world.Map.Lines[0];
        var junk = lineEffectLines.GetValue(
            world,
            _ => new LineDef(
                template.Vertex1,
                template.Vertex2,
                template.Flags,
                LineSpecial.Normal,
                0,
                template.FrontSide,
                template.BackSide));

        // MBF/PrBoom does "junk = *lines" on every call. Mutable fields are
        // refreshed from line 0, while the SideDef references remain shared.
        // That deliberately preserves the historical quirk where a synthetic
        // switch can change the first real sidedef's wall texture.
        junk.Flags = template.Flags;
        junk.Special = (LineSpecial)special;
        junk.Tag = tag;
        junk.SoundOrigin = template.SoundOrigin;
        junk.SpecialData = template.SpecialData;
        junk.TranslucencyMapName = template.TranslucencyMapName;

        return junk;
    }

    private static bool CanExecute(World world, Mobj actor)
    {
        return world != null &&
               actor != null &&
               actor.State != null &&
               GameCompatibilityFeatures.SupportsMbfLineAndSoundCodePointers(world.Options.Compatibility);
    }
}
