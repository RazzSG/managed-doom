using System;
using System.Buffers.Binary;

namespace ManagedDoom.Compatibility.Boom;

/// <summary>
/// One packed 23-byte entry from Boom's ANIMATED lump.
/// </summary>
public readonly struct BoomAnimationDefinition
{
    public const int DataSize = 23;

    public BoomAnimationDefinition(sbyte type, string endName, string startName, int speed)
    {
        Type = type;
        EndName = endName;
        StartName = startName;
        Speed = speed;
    }

    /// <summary>
    /// -1 terminates the table, 0 denotes a flat, any other value denotes a texture.
    /// </summary>
    public sbyte Type { get; }
    public string EndName { get; }
    public string StartName { get; }
    public int Speed { get; }

    public bool IsTerminator => Type == -1;
    public bool IsTexture => Type != 0 && Type != -1;

    public static BoomAnimationDefinition FromData(byte[] data, int offset)
    {
        return new BoomAnimationDefinition(
            unchecked((sbyte)data[offset]),
            DoomInterop.ToString(data, offset + 1, 9),
            DoomInterop.ToString(data, offset + 10, 9),
            BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset + 19, 4)));
    }
}
