using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Movement;

/// <summary>
/// Teleport behavior that differs between vanilla Doom and Boom 2.02.
/// </summary>
public static class BoomTeleportQuirks
{
    /// <summary>
    /// Boom lets boss-spawn teleports telefrag on any map and no longer gives
    /// ordinary monster teleports a MAP30 exception. Vanilla Doom keeps the
    /// original MAP30 rule. Players can always telefrag.
    /// </summary>
    public static bool CanTelefragAtDestination(
        Mobj thing,
        bool bossTeleport,
        int mapNumber,
        GameCompatibility compatibility)
    {
        if (thing.Player != null)
            return true;

        if (GameCompatibilityFeatures.SupportsBoom(compatibility))
            return bossTeleport;

        return mapNumber == 30;
    }

    /// <summary>
    /// Boom excludes voodoo dolls from player-view updates performed by a
    /// normal teleport. Vanilla behavior is preserved below Boom.
    /// </summary>
    public static Player GetPlayerForTeleportViewUpdate(
        Mobj thing,
        GameCompatibility compatibility)
    {
        var player = thing.Player;
        if (player == null)
            return null;

        if (!GameCompatibilityFeatures.SupportsBoom(compatibility))
            return player;

        return player.Mobj == thing ? player : null;
    }
}
