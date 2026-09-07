using System;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Gameplay;

/// <summary>
/// Compatibility-selected gameplay bug fixes introduced by Boom.
/// Vanilla keeps the original Doom behavior while Boom and descendants
/// use the corrected behavior.
/// </summary>
public static class BoomGameplayBugFixes
{
    public static bool EnforcesPainElementalLostSoulLimit(GameCompatibility compatibility)
    {
        return !GameCompatibilityFeatures.SupportsBoom(compatibility);
    }

    public static bool UsesSafePainElementalLostSoulSpawn(GameCompatibility compatibility)
    {
        return GameCompatibilityFeatures.SupportsBoom(compatibility);
    }

    public static bool UsesFixedArchVileResurrection(GameCompatibility compatibility)
    {
        return GameCompatibilityFeatures.SupportsBoom(compatibility);
    }

    public readonly struct ArchVileCorpseFitState
    {
        public ArchVileCorpseFitState(Fixed height, Fixed radius)
        {
            Height = height;
            Radius = radius;
        }

        public Fixed Height { get; }
        public Fixed Radius { get; }
    }

    public static ArchVileCorpseFitState PrepareArchVileCorpseForFitCheck(
        Mobj corpse,
        GameCompatibility compatibility)
    {
        var state = new ArchVileCorpseFitState(corpse.Height, corpse.Radius);

        if (UsesFixedArchVileResurrection(compatibility))
        {
            corpse.Height = corpse.Info.Height;
            corpse.Radius = corpse.Info.Radius;
            corpse.Flags |= MobjFlags.Solid;
        }
        else
        {
            corpse.Height <<= 2;
        }

        return state;
    }

    public static void RestoreArchVileCorpseAfterFitCheck(
        Mobj corpse,
        ArchVileCorpseFitState state,
        GameCompatibility compatibility)
    {
        if (UsesFixedArchVileResurrection(compatibility))
        {
            corpse.Height = state.Height;
            corpse.Radius = state.Radius;
            corpse.Flags &= ~MobjFlags.Solid;
        }
        else
        {
            corpse.Height >>= 2;
        }
    }

    public static void ApplyArchVileResurrectionDimensions(
        Mobj corpse,
        GameCompatibility compatibility)
    {
        if (UsesFixedArchVileResurrection(compatibility))
        {
            corpse.Height = corpse.Info.Height;
            corpse.Radius = corpse.Info.Radius;
        }
        else
        {
            corpse.Height <<= 2;
        }
    }

    public static bool ClearsGodModeInExitDamageSector(GameCompatibility compatibility)
    {
        return !GameCompatibilityFeatures.SupportsBoom(compatibility);
    }

    /// <summary>
    /// Doom's original damage check lets damage of 1000 or more bypass both
    /// god mode and invulnerability. Boom fixes god mode so that it blocks all
    /// damage, while the invulnerability power still keeps the old threshold.
    /// </summary>
    public static bool ShouldIgnorePlayerDamage(
        GameCompatibility compatibility,
        int damage,
        bool godMode,
        bool invulnerable)
    {
        if (GameCompatibilityFeatures.SupportsBoom(compatibility))
            return godMode || (damage < 1000 && invulnerable);

        return damage < 1000 && (godMode || invulnerable);
    }

    public static bool IsPainElementalLostSoulOutsideVerticalBounds(
        GameCompatibility compatibility,
        Fixed z,
        Fixed height,
        Fixed floorHeight,
        Fixed ceilingHeight)
    {
        return UsesSafePainElementalLostSoulSpawn(compatibility) &&
            (z > ceilingHeight - height || z < floorHeight);
    }

    /// <summary>
    /// Boom's Check_Sides bug fix used by Pain Elementals. It rejects a Lost
    /// Soul spawn when the PE-to-spawn trajectory crosses a one-sided,
    /// impassable, or monster-blocking line.
    /// </summary>
    public static bool IsPainElementalLostSoulSpawnBlocked(
        World world,
        Mobj actor,
        Fixed x,
        Fixed y)
    {
        if (!UsesSafePainElementalLostSoulSpawn(world.Options.Compatibility))
            return false;

        var blockMap = world.Map.BlockMap;
        var left = Fixed.Min(actor.X, x);
        var right = Fixed.Max(actor.X, x);
        var top = Fixed.Max(actor.Y, y);
        var bottom = Fixed.Min(actor.Y, y);

        var minBlockX = Math.Clamp(blockMap.GetBlockX(left), 0, blockMap.Width - 1);
        var maxBlockX = Math.Clamp(blockMap.GetBlockX(right), 0, blockMap.Width - 1);
        var minBlockY = Math.Clamp(blockMap.GetBlockY(bottom), 0, blockMap.Height - 1);
        var maxBlockY = Math.Clamp(blockMap.GetBlockY(top), 0, blockMap.Height - 1);
        var validCount = world.GetNewValidCount();

        bool CheckLine(LineDef line)
        {
            if ((line.Flags & LineFlags.TwoSided) != 0 &&
                (line.Flags & (LineFlags.Blocking | LineFlags.BlockMonsters)) == 0)
            {
                return true;
            }

            var box = line.BoundingBox;
            if (left > box[Box.Right] ||
                right < box[Box.Left] ||
                top < box[Box.Bottom] ||
                bottom > box[Box.Top])
            {
                return true;
            }

            return Geometry.PointOnLineSide(actor.X, actor.Y, line) ==
                Geometry.PointOnLineSide(x, y, line);
        }

        for (var blockX = minBlockX; blockX <= maxBlockX; blockX++)
        {
            for (var blockY = minBlockY; blockY <= maxBlockY; blockY++)
            {
                if (!blockMap.IterateLines(blockX, blockY, CheckLine, validCount))
                    return true;
            }
        }

        return false;
    }
}
