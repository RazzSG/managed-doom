namespace ManagedDoom.Compatibility.Boom.Sectors;

public static class BoomSectorSecretHandler
{
    private const string SecretMessage = "A secret is revealed!";

    public static void Count(World world, BoomSectorSpecial special)
    {
        if (special.IsSecret)
        {
            world.TotalSecrets++;
        }
    }

    public static void Reveal(Player player, Sector sector, BoomSectorSpecial special)
    {
        if (!special.IsSecret)
        {
            return;
        }

        player.SecretCount++;
        player.SendMessage(SecretMessage);
        sector.Special = BoomSectorSpecialDecoder.ClearSecretFlag(sector.Special);
    }
}
