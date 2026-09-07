namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomMonsterActivationRules
{
    public static bool CanActivate(Mobj thing, bool allowsMonsters)
    {
        if (thing == null)
            return false;

        return thing.Player != null || allowsMonsters;
    }

    public static bool CanActivatePlayerOnly(Mobj thing)
    {
        return thing?.Player != null;
    }

    public static bool CanActivateDoor(LineDef line, Mobj thing, bool allowsMonsters)
    {
        if (!CanActivate(thing, allowsMonsters))
            return false;

        return thing.Player != null || (line.Flags & LineFlags.Secret) == 0;
    }

    public static bool CanActivateTeleport(
        Mobj thing,
        bool playersAllowed,
        bool monstersAllowed)
    {
        if (thing == null || (thing.Flags & MobjFlags.Missile) != 0)
            return false;

        return thing.Player != null ? playersAllowed : monstersAllowed;
    }
}
