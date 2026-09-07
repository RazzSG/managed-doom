using System;
using System.Collections.Generic;

namespace ManagedDoom.Compatibility.Boom;

/// <summary>
/// Parses Boom's SWITCHES resource once and converts texture names to texture
/// numbers so switch activation never needs to search the WAD directory.
/// </summary>
public sealed class BoomSwitches
{
    private readonly int[] switchList;

    public BoomSwitches(Wad wad, ITextureLookup textures)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));
        if (textures == null)
            throw new ArgumentNullException(nameof(textures));

        var lumpNumber = wad.GetLumpNumber("SWITCHES");
        if (lumpNumber == -1)
        {
            // ManagedDoom's built-in table is the equivalent of Boom's
            // predefined SWITCHES lump. Preserve it when no WAD overrides it.
            switchList = textures.SwitchList;
            HasCustomTable = false;
            return;
        }

        HasCustomTable = true;
        switchList = BuildSwitchList(wad.ReadLump(lumpNumber), textures, GetEpisode(wad.GameMode));
    }

    private static int[] BuildSwitchList(byte[] data, ITextureLookup textures, int gameEpisode)
    {
        var list = new List<int>();

        // A SWITCHES entry is exactly 20 bytes. Ignore an incomplete trailing
        // record rather than allowing malformed resource data to read past the
        // end of the lump.
        for (var offset = 0; offset + BoomSwitchDefinition.DataSize <= data.Length; offset += BoomSwitchDefinition.DataSize)
        {
            var definition = BoomSwitchDefinition.FromData(data, offset);

            if (definition.Episode == 0)
                break;

            if (definition.Episode < 0 || definition.Episode > gameEpisode)
                continue;

            var texture1 = textures.GetNumber(definition.OffTexture);
            var texture2 = textures.GetNumber(definition.OnTexture);

            // Boom/PrBoom deliberately ignores definitions whose textures do
            // not exist instead of aborting startup.
            if (texture1 == -1 || texture2 == -1)
                continue;

            list.Add(texture1);
            list.Add(texture2);
        }

        return list.ToArray();
    }

    private static int GetEpisode(GameMode gameMode)
    {
        return gameMode switch
        {
            GameMode.Registered => 2,
            GameMode.Retail => 2,
            GameMode.Commercial => 3,
            _ => 1
        };
    }

    public bool HasCustomTable { get; }
    public int[] SwitchList => switchList;
}
