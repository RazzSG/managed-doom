namespace ManagedDoom.Compatibility.Boom;

/// <summary>
/// One packed 20-byte entry from Boom's SWITCHES lump.
/// </summary>
public readonly struct BoomSwitchDefinition
{
    public const int DataSize = 20;

    public BoomSwitchDefinition(string offTexture, string onTexture, short episode)
    {
        OffTexture = offTexture;
        OnTexture = onTexture;
        Episode = episode;
    }

    public string OffTexture { get; }
    public string OnTexture { get; }
    public short Episode { get; }

    public static BoomSwitchDefinition FromData(byte[] data, int offset)
    {
        return new BoomSwitchDefinition(
            DoomInterop.ToString(data, offset, 9),
            DoomInterop.ToString(data, offset + 9, 9),
            System.BitConverter.ToInt16(data, offset + 18));
    }
}
