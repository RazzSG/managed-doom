namespace ManagedDoom.Compatibility.Mbf.Rendering;

/// <summary>
/// MBF linedefs 271/272: transfer the first sidedef's upper wall texture to
/// F_SKY1 planes in tagged sectors. PrBoom exposes this rendering feature to
/// Boom-profile maps, so the feature gate intentionally starts at Boom.
/// </summary>
public static class MbfSkyTransferResolver
{
    public const int TransferSkySpecial = 271;
    public const int TransferSkyFlippedSpecial = 272;

    public static void Apply(World world)
    {
        foreach (var sector in world.Map.Sectors)
            sector.SkyTransferLine = null;

        if (!GameCompatibilityFeatures.SupportsBoomProfileSkyTransfer(world.Options.Compatibility))
            return;

        foreach (var line in world.Map.Lines)
        {
            var special = (int)line.Special;
            if (special != TransferSkySpecial && special != TransferSkyFlippedSpecial)
                continue;

            if (line.FrontSide == null)
                continue;

            var targets = world.Map.BoomTags.GetSectors(line.Tag);
            for (var i = 0; i < targets.Length; i++)
            {
                // PrBoom scans linedefs in map order. A later 271/272 therefore
                // replaces an earlier sky source targeting the same sector.
                targets[i].SkyTransferLine = line;
            }
        }
    }

    public static bool TryResolveRenderState(
        Sector sector,
        int[] textureTranslation,
        int textureCount,
        out MbfSkyTransferRenderState state)
    {
        state = default;

        var line = sector?.SkyTransferLine;
        var side = line?.FrontSide;
        if (side == null)
            return false;

        var special = (int)line.Special;
        if (special != TransferSkySpecial && special != TransferSkyFlippedSpecial)
            return false;

        var textureNumber = side.TopTexture;
        if (textureNumber <= 0 ||
            textureTranslation == null ||
            (uint)textureNumber >= (uint)textureTranslation.Length)
        {
            return false;
        }

        textureNumber = textureTranslation[textureNumber];
        if ((uint)textureNumber >= (uint)textureCount)
            return false;

        state = new MbfSkyTransferRenderState(
            textureNumber,
            side,
            special == TransferSkySpecial);

        return true;
    }

    public static Angle ResolveSkyAngle(Angle baseAngle, MbfSkyTransferRenderState state)
    {
        var angle = baseAngle + state.AngleOffset;
        return state.InvertAngleBits ? new Angle(~angle.Data) : angle;
    }
}

public readonly struct MbfSkyTransferRenderState
{
    private readonly SideDef sourceSide;

    public MbfSkyTransferRenderState(
        int textureNumber,
        SideDef sourceSide,
        bool invertAngleBits)
    {
        TextureNumber = textureNumber;
        this.sourceSide = sourceSide;
        InvertAngleBits = invertAngleBits;
    }

    public int TextureNumber { get; }

    // PrBoom reads these values from the defining sidedef while rendering.
    // Keeping the sidedef reference here prevents a render-state snapshot from
    // becoming stale when Boom wall scrollers modify its offsets.
    public Angle AngleOffset => new Angle(sourceSide.TextureOffset.Data);
    public Fixed TextureAlt => sourceSide.RowOffset - Fixed.FromInt(28);

    public bool InvertAngleBits { get; }
}
