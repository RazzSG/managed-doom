using System;
using System.Collections.Generic;

namespace ManagedDoom.Compatibility.Boom;

/// <summary>
/// Parses Boom's ANIMATED resource once and converts it into ManagedDoom's
/// existing TextureAnimation scheduler.
/// </summary>
public sealed class BoomAnimated
{
    private readonly TextureAnimation animation;

    public BoomAnimated(
        Wad wad,
        ITextureLookup textures,
        IFlatLookup flats,
        TextureAnimation vanillaAnimation)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));
        if (textures == null)
            throw new ArgumentNullException(nameof(textures));
        if (flats == null)
            throw new ArgumentNullException(nameof(flats));
        if (vanillaAnimation == null)
            throw new ArgumentNullException(nameof(vanillaAnimation));

        var lumpNumber = wad.GetLumpNumber("ANIMATED");
        if (lumpNumber == -1)
        {
            HasCustomTable = false;
            animation = vanillaAnimation;
            return;
        }

        HasCustomTable = true;
        var definitions = ParseDefinitions(wad.ReadLump(lumpNumber));
        animation = new TextureAnimation(textures, flats, definitions);
    }

    private static AnimationDef[] ParseDefinitions(byte[] data)
    {
        var list = new List<AnimationDef>();

        // ANIMATED is an array of packed 23-byte records. Ignore an incomplete
        // trailing record so malformed resource data cannot read past the lump.
        for (var offset = 0; offset + BoomAnimationDefinition.DataSize <= data.Length; offset += BoomAnimationDefinition.DataSize)
        {
            var definition = BoomAnimationDefinition.FromData(data, offset);

            if (definition.IsTerminator)
                break;

            list.Add(new AnimationDef(
                definition.IsTexture,
                definition.EndName,
                definition.StartName,
                definition.Speed));
        }

        return list.ToArray();
    }

    public bool HasCustomTable { get; }
    public TextureAnimation Animation => animation;
}
