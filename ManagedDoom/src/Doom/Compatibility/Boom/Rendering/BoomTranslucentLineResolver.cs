using System;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Rendering;

/// <summary>
/// Resolves Boom linedef 260 translucency assignments once at map startup.
/// The controller line's first sidedef supplies the map selector; tag zero
/// affects only the controller, while a non-zero tag affects all matching lines.
/// </summary>
public static class BoomTranslucentLineResolver
{
    public const int Special = 260;
    public const string DefaultMapName = "TRANMAP";

    public static void Apply(World world, BoomTranslucencyMapLookup maps)
    {
        Apply(world.Map.Lines, world.Options.Compatibility, maps);
    }

    public static void Apply(LineDef[] lines, GameCompatibility compatibility)
    {
        Apply(lines, compatibility, null);
    }

    public static void Apply(
        LineDef[] lines,
        GameCompatibility compatibility,
        BoomTranslucencyMapLookup maps)
    {
        // SpawnSpecials can be called repeatedly by tests or compatibility changes.
        // Always clear previously resolved state first.
        foreach (var line in lines)
            line.TranslucencyMapName = null;

        if (!GameCompatibilityFeatures.SupportsTranslucentLines(compatibility))
            return;

        foreach (var controller in lines)
        {
            if ((int)controller.Special != Special || controller.FrontSide == null)
                continue;

            var side = controller.FrontSide;
            var mapName = ResolveMapName(side, maps, out var consumesMiddleTexture);

            // Boom overloads the control sidedef's middle name. TRANMAP and a
            // valid 64K custom lump are selectors, not wall textures. A valid
            // custom lump wins even if a wall texture shares the same name.
            if (consumesMiddleTexture)
                side.MiddleTexture = 0;

            if (controller.Tag == 0)
            {
                // Boom's setup code copies the sidedef translucency selector to
                // this line even for tag zero. This differs from the wording in
                // BOOMREF but matches the engine's actual tranlump behavior.
                controller.TranslucencyMapName = mapName;
                continue;
            }

            foreach (var line in lines)
            {
                if (line.Tag == controller.Tag)
                    line.TranslucencyMapName = mapName;
            }
        }
    }

    private static string ResolveMapName(
        SideDef side,
        BoomTranslucencyMapLookup maps,
        out bool consumesMiddleTexture)
    {
        var name = side.MiddleTextureName;
        if (string.IsNullOrWhiteSpace(name) || name[0] == '-')
        {
            consumesMiddleTexture = false;
            return DefaultMapName;
        }

        if (string.Equals(name, DefaultMapName, StringComparison.OrdinalIgnoreCase))
        {
            consumesMiddleTexture = true;
            return DefaultMapName;
        }

        // Production setup has the WAD lookup and reproduces Boom's exact
        // preference: a valid 64K lump is a custom filter even if a texture
        // with the same name exists. The two-argument overload used by focused
        // resolver tests keeps the pre-resolved sidedef interpretation.
        if (maps != null && maps.TryGetCustomMap(name, out _))
        {
            consumesMiddleTexture = true;
            return name;
        }

        if (maps == null && !side.MiddleTextureIsWallTexture)
        {
            consumesMiddleTexture = true;
            return name;
        }

        consumesMiddleTexture = false;
        return DefaultMapName;
    }
}
