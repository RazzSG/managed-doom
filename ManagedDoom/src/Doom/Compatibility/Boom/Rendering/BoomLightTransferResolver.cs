namespace ManagedDoom.Compatibility.Boom.Rendering;

public static class BoomLightTransferResolver
{
    private const int FloorLightTransferSpecial = 213;
    private const int CeilingLightTransferSpecial = 261;

    public static void Apply(World world)
    {
        foreach (var line in world.Map.Lines)
        {
            var special = (int)line.Special;
            if (special != FloorLightTransferSpecial && special != CeilingLightTransferSpecial)
                continue;

            var source = line.FrontSector;
            if (source == null)
                continue;

            var targets = world.Map.BoomTags.GetSectors(line.Tag);
            for (var i = 0; i < targets.Length; i++)
            {
                if (special == FloorLightTransferSpecial)
                    targets[i].FloorLightSector = source;
                else
                    targets[i].CeilingLightSector = source;
            }
        }
    }
}
