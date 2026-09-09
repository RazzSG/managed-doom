namespace ManagedDoom.Compatibility.Mbf.Things;

/// <summary>
/// Resolves the MBF/PrBoom nightmare-respawn compatibility case for actors
/// that do not have a map spawn point. The compatibility bug sends them to
/// (0,0); the fixed path respawns them where the dead actor currently is.
/// </summary>
public static class MbfRespawnCompatibility
{
    public static bool TryResolveSpawnlessRespawn(
        GameCompatibility compatibility,
        bool compRespawn,
        Mobj actor,
        out Fixed x,
        out Fixed y,
        out Angle angle)
    {
        if (actor != null &&
            actor.SpawnPoint == null &&
            GameCompatibilityFeatures.SupportsMbfRespawnCompatibility(compatibility) &&
            !compRespawn)
        {
            x = actor.X;
            y = actor.Y;
            angle = actor.Angle;
            return true;
        }

        x = Fixed.Zero;
        y = Fixed.Zero;
        angle = Angle.Ang0;
        return false;
    }
}
