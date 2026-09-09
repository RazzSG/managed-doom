using System;
using System.Collections.Generic;
using ManagedDoom;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Mbf.AI;

/// <summary>
/// Resolves MBF's configured single-player helper slots. Helpers occupy player
/// starts 2 through 4 in player-number order; the player-1 start is never used.
/// </summary>
public static class MbfPlayerHelpers
{
    public static IReadOnlyList<MapThing> CollectSpawnStarts(
        GameCompatibility compatibility,
        MbfOptions options,
        Player[] players,
        IReadOnlyList<MapThing> playerStarts,
        bool netGame,
        int deathmatch)
    {
        if (!GameCompatibilityFeatures.SupportsMbfPlayerHelpers(compatibility) ||
            options == null ||
            players == null ||
            playerStarts == null ||
            options.PlayerHelpers <= 0 ||
            netGame ||
            deathmatch != 0)
        {
            return Array.Empty<MapThing>();
        }

        var requested = Math.Min(options.PlayerHelpers, MbfOptions.MaxPlayerHelpers);
        var count = Math.Min(players.Length, playerStarts.Count);
        var result = new List<MapThing>(requested);

        // Original MBF assigns helpers to player starts 2, 3 and 4. This is
        // based on the configured helper count, not on synthetic InGame state.
        for (var i = 1; i < count && i <= requested; i++)
        {
            var start = playerStarts[i];
            if (start != null)
                result.Add(start);
        }

        return result;
    }
}
