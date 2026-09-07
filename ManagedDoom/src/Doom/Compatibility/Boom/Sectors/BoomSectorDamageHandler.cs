namespace ManagedDoom.Compatibility.Boom.Sectors;

public static class BoomSectorDamageHandler
{
    private const int DamageIntervalMask = 0x1f;
    private const int SuitLeakChance = 5;

    public static void Apply(World world, Player player, BoomSectorSpecial special)
    {
        if (special.Damage == BoomSectorDamage.None ||
            (world.LevelTime & DamageIntervalMask) != 0)
        {
            return;
        }

        var hasSuit = player.Powers[(int)PowerType.IronFeet] != 0;
        if (hasSuit)
        {
            if (special.Damage != BoomSectorDamage.Twenty ||
                world.Random.Next() >= SuitLeakChance)
            {
                return;
            }
        }

        world.ThingInteraction.DamageMobj(
            player.Mobj,
            null,
            null,
            special.DamageAmount);
    }
}
