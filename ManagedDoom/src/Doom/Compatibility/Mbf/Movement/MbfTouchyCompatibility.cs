using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Movement;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// MBF TOUCHY actor semantics. Non-sentient actors become armed after they
/// have come completely to rest. Once armed, a TOUCHY actor reacts to a
/// qualifying solid contact or a downward floor impact. Sentient TOUCHY
/// actors do not need the armed marker for actor-to-actor contact, matching
/// MBF's PIT_CheckThing rule.
/// </summary>
public static class MbfTouchyCompatibility
{
    public static bool IsEnabled(GameCompatibility compatibility) =>
        GameCompatibilityFeatures.SupportsMbfActorPhysicsFlags(compatibility);

    /// <summary>
    /// Returns whether the MBF-only TOUCHY bit is allowed to make an otherwise
    /// non-interactive actor participate in PIT_CheckThing. Outside MBF the bit
    /// must be completely invisible to the shared Doom/Boom collision path.
    /// </summary>
    public static bool RequiresThingCollisionCheck(
        GameCompatibility compatibility,
        Mobj actor)
    {
        return IsEnabled(compatibility) &&
               actor != null &&
               (actor.Flags & MobjFlags.Touchy) != 0;
    }

    public static void ArmAtRest(GameCompatibility compatibility, Mobj actor)
    {
        if (actor == null ||
            !IsEnabled(compatibility) ||
            BoomLedgeTorque.IsSentient(actor))
        {
            return;
        }

        actor.MbfTouchyArmed = true;
    }

    public static bool ShouldActivateOnThingContact(
        GameCompatibility compatibility,
        Mobj touchy,
        Mobj mover)
    {
        if (!IsEnabled(compatibility) || touchy == null || mover == null)
            return false;

        if ((touchy.Flags & MobjFlags.Touchy) == 0 ||
            (mover.Flags & MobjFlags.Solid) == 0 ||
            touchy.Health <= 0)
        {
            return false;
        }

        if (!touchy.MbfTouchyArmed && !BoomLedgeTorque.IsSentient(touchy))
            return false;

        // Same-type contacts are ignored, except player actors. MBF also
        // treats the Pain Elemental/Lost Soul pair as the same species for
        // this purpose.
        if (touchy.Type == mover.Type && touchy.Type != MobjType.Player)
            return false;

        if ((touchy.Type == MobjType.Pain && mover.Type == MobjType.Skull) ||
            (touchy.Type == MobjType.Skull && mover.Type == MobjType.Pain))
        {
            return false;
        }

        return touchy.Z + touchy.Height >= mover.Z &&
               mover.Z + mover.Height >= touchy.Z;
    }

    public static bool ShouldActivateOnFloorImpact(
        GameCompatibility compatibility,
        Mobj actor,
        Fixed incomingMomentumZ)
    {
        return IsEnabled(compatibility) &&
               actor != null &&
               (actor.Flags & MobjFlags.Touchy) != 0 &&
               actor.MbfTouchyArmed &&
               actor.Health > 0 &&
               incomingMomentumZ < Fixed.Zero;
    }
}
