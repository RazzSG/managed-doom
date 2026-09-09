using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Friction;

namespace ManagedDoom.Compatibility.Mbf.Movement;

/// <summary>
/// MBF and later independent player bobbing momentum. The physical actor
/// uses the sector move factor, while bobbing reflects the player's movement
/// effort. On ice that effort stays at the original Doom move factor; on sludge
/// it follows the reduced move factor.
/// </summary>
public static class MbfPlayerBobbing
{
    public static bool Applies(GameCompatibility compatibility) =>
        GameCompatibilityFeatures.SupportsMbf(compatibility);

    public static Fixed GetEffortMoveFactor(
        Mobj thing,
        Fixed physicalMoveFactor,
        GameCompatibility compatibility)
    {
        if (!Applies(compatibility))
            return physicalMoveFactor;

        var friction = BoomSectorFriction.GetFriction(thing);
        return friction < BoomFrictionTranslator.OriginalFriction
            ? physicalMoveFactor
            : BoomFrictionTranslator.OriginalMoveFactor;
    }

    public static void ApplyOriginalFriction(Player player, GameCompatibility compatibility)
    {
        if (!Applies(compatibility))
            return;

        player.BobMomX *= BoomFrictionTranslator.OriginalFriction;
        player.BobMomY *= BoomFrictionTranslator.OriginalFriction;
    }

    public static void Stop(Player player, GameCompatibility compatibility)
    {
        if (!Applies(compatibility))
            return;

        player.BobMomX = Fixed.Zero;
        player.BobMomY = Fixed.Zero;
    }
}
