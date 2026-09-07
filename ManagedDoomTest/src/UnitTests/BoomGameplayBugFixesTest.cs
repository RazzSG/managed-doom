using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomGameplayBugFixesTest
{
    [TestMethod]
    public void GameplayBugFixesEnableAtBoomAndRemainEnabledForDescendants()
    {
        Assert.IsTrue(BoomGameplayBugFixes.EnforcesPainElementalLostSoulLimit(GameCompatibility.Vanilla));
        Assert.IsFalse(BoomGameplayBugFixes.UsesSafePainElementalLostSoulSpawn(GameCompatibility.Vanilla));
        Assert.IsFalse(BoomGameplayBugFixes.UsesFixedArchVileResurrection(GameCompatibility.Vanilla));
        Assert.IsTrue(BoomGameplayBugFixes.ClearsGodModeInExitDamageSector(GameCompatibility.Vanilla));

        foreach (var compatibility in new[]
                 {
                     GameCompatibility.Boom,
                     GameCompatibility.Mbf,
                     GameCompatibility.Mbf21
                 })
        {
            Assert.IsFalse(BoomGameplayBugFixes.EnforcesPainElementalLostSoulLimit(compatibility));
            Assert.IsTrue(BoomGameplayBugFixes.UsesSafePainElementalLostSoulSpawn(compatibility));
            Assert.IsTrue(BoomGameplayBugFixes.UsesFixedArchVileResurrection(compatibility));
            Assert.IsFalse(BoomGameplayBugFixes.ClearsGodModeInExitDamageSector(compatibility));
        }
    }

    [TestMethod]
    public void BoomArchVileFitCheckUsesOriginalMonsterDimensionsForCrushedCorpse()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var corpse = CreateCrushedCorpse(world);
        var info = corpse.Info;

        var state = BoomGameplayBugFixes.PrepareArchVileCorpseForFitCheck(
            corpse,
            GameCompatibility.Boom);

        Assert.AreEqual(info.Height.Data, corpse.Height.Data);
        Assert.AreEqual(info.Radius.Data, corpse.Radius.Data);
        Assert.IsTrue((corpse.Flags & MobjFlags.Solid) != 0);

        BoomGameplayBugFixes.RestoreArchVileCorpseAfterFitCheck(
            corpse,
            state,
            GameCompatibility.Boom);

        Assert.AreEqual(Fixed.Zero.Data, corpse.Height.Data);
        Assert.AreEqual(Fixed.Zero.Data, corpse.Radius.Data);
        Assert.IsFalse((corpse.Flags & MobjFlags.Solid) != 0);
    }

    [TestMethod]
    public void VanillaArchVileFitCheckPreservesOriginalGhostBug()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Vanilla);
        var corpse = CreateCrushedCorpse(world);

        var state = BoomGameplayBugFixes.PrepareArchVileCorpseForFitCheck(
            corpse,
            GameCompatibility.Vanilla);

        Assert.AreEqual(Fixed.Zero.Data, corpse.Height.Data);
        Assert.AreEqual(Fixed.Zero.Data, corpse.Radius.Data);
        Assert.IsFalse((corpse.Flags & MobjFlags.Solid) != 0);

        BoomGameplayBugFixes.RestoreArchVileCorpseAfterFitCheck(
            corpse,
            state,
            GameCompatibility.Vanilla);

        Assert.AreEqual(Fixed.Zero.Data, corpse.Height.Data);
        Assert.AreEqual(Fixed.Zero.Data, corpse.Radius.Data);
    }

    [TestMethod]
    public void BoomArchVileResurrectionRestoresRealDimensionsInsteadOfGhostDimensions()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var corpse = CreateCrushedCorpse(world);
        var info = corpse.Info;

        BoomGameplayBugFixes.ApplyArchVileResurrectionDimensions(
            corpse,
            GameCompatibility.Boom);

        Assert.AreEqual(info.Height.Data, corpse.Height.Data);
        Assert.AreEqual(info.Radius.Data, corpse.Radius.Data);

        corpse = CreateCrushedCorpse(world);
        BoomGameplayBugFixes.ApplyArchVileResurrectionDimensions(
            corpse,
            GameCompatibility.Vanilla);

        Assert.AreEqual(Fixed.Zero.Data, corpse.Height.Data);
        Assert.AreEqual(Fixed.Zero.Data, corpse.Radius.Data);
    }

    [TestMethod]
    public void BoomGodModeBlocksThousandDamageWhileInvulnerabilityKeepsVanillaThreshold()
    {
        Assert.IsTrue(BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
            GameCompatibility.Vanilla, 999, godMode: true, invulnerable: false));
        Assert.IsFalse(BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
            GameCompatibility.Vanilla, 1000, godMode: true, invulnerable: false));

        Assert.IsTrue(BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
            GameCompatibility.Boom, 1000, godMode: true, invulnerable: false));
        Assert.IsTrue(BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
            GameCompatibility.Boom, 10000, godMode: true, invulnerable: false));

        Assert.IsTrue(BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
            GameCompatibility.Boom, 999, godMode: false, invulnerable: true));
        Assert.IsFalse(BoomGameplayBugFixes.ShouldIgnorePlayerDamage(
            GameCompatibility.Boom, 1000, godMode: false, invulnerable: true));
    }

    [TestMethod]
    public void DamageMobjUsesBoomGodModeFixWithoutChangingVanilla()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanillaWorld = CreateWorld(content, GameCompatibility.Vanilla);
        var vanillaPlayer = vanillaWorld.ConsolePlayer;
        vanillaPlayer.Health = 2000;
        vanillaPlayer.Mobj.Health = 2000;
        vanillaPlayer.Cheats |= CheatFlags.GodMode;
        vanillaPlayer.Mobj.Subsector.Sector.Special = 0;

        vanillaWorld.ThingInteraction.DamageMobj(vanillaPlayer.Mobj, null, null, 1000);

        Assert.AreEqual(1000, vanillaPlayer.Health);
        Assert.AreEqual(1000, vanillaPlayer.Mobj.Health);

        var boomWorld = CreateWorld(content, GameCompatibility.Boom);
        var boomPlayer = boomWorld.ConsolePlayer;
        boomPlayer.Health = 2000;
        boomPlayer.Mobj.Health = 2000;
        boomPlayer.Cheats |= CheatFlags.GodMode;
        boomPlayer.Mobj.Subsector.Sector.Special = 0;

        boomWorld.ThingInteraction.DamageMobj(boomPlayer.Mobj, null, null, 1000);

        Assert.AreEqual(2000, boomPlayer.Health);
        Assert.AreEqual(2000, boomPlayer.Mobj.Health);
    }

    [TestMethod]
    public void ExitDamageSectorClearsGodModeOnlyBelowBoomCompatibility()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanillaWorld = CreateWorld(content, GameCompatibility.Vanilla);
        var vanillaPlayer = vanillaWorld.ConsolePlayer;
        vanillaPlayer.Cheats |= CheatFlags.GodMode;
        vanillaPlayer.Mobj.Subsector.Sector.Special = (SectorSpecial)11;
        vanillaPlayer.Mobj.Z = vanillaPlayer.Mobj.Subsector.Sector.FloorHeight;

        vanillaWorld.PlayerBehavior.PlayerThink(vanillaPlayer);

        Assert.IsFalse((vanillaPlayer.Cheats & CheatFlags.GodMode) != 0);

        var boomWorld = CreateWorld(content, GameCompatibility.Boom);
        var boomPlayer = boomWorld.ConsolePlayer;
        boomPlayer.Cheats |= CheatFlags.GodMode;
        boomPlayer.Mobj.Subsector.Sector.Special = (SectorSpecial)11;
        boomPlayer.Mobj.Z = boomPlayer.Mobj.Subsector.Sector.FloorHeight;

        boomWorld.PlayerBehavior.PlayerThink(boomPlayer);

        Assert.IsTrue((boomPlayer.Cheats & CheatFlags.GodMode) != 0);
    }

    [TestMethod]
    public void BoomPainElementalSpawnRejectsTrajectoryAcrossOneSidedLine()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Boom);
        var line = world.Map.Lines.First(l =>
            (l.Flags & LineFlags.TwoSided) == 0 &&
            (l.Dx == Fixed.Zero || l.Dy == Fixed.Zero));
        var actor = new Mobj(world);

        Fixed spawnX;
        Fixed spawnY;
        if (line.Dx == Fixed.Zero)
        {
            var midY = new Fixed((int)(((long)line.Vertex1.Y.Data + line.Vertex2.Y.Data) / 2));
            actor.X = line.Vertex1.X - Fixed.FromInt(1);
            actor.Y = midY;
            spawnX = line.Vertex1.X + Fixed.FromInt(1);
            spawnY = midY;
        }
        else
        {
            var midX = new Fixed((int)(((long)line.Vertex1.X.Data + line.Vertex2.X.Data) / 2));
            actor.X = midX;
            actor.Y = line.Vertex1.Y - Fixed.FromInt(1);
            spawnX = midX;
            spawnY = line.Vertex1.Y + Fixed.FromInt(1);
        }

        Assert.IsTrue(BoomGameplayBugFixes.IsPainElementalLostSoulSpawnBlocked(
            world,
            actor,
            spawnX,
            spawnY));
    }

    [TestMethod]
    public void VanillaPainElementalSpawnKeepsOldWallCrossingBehavior()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = CreateWorld(content, GameCompatibility.Vanilla);
        var actor = new Mobj(world)
        {
            X = Fixed.Zero,
            Y = Fixed.Zero
        };

        Assert.IsFalse(BoomGameplayBugFixes.IsPainElementalLostSoulSpawnBlocked(
            world,
            actor,
            Fixed.FromInt(128),
            Fixed.Zero));
    }

    [TestMethod]
    public void BoomPainElementalSpawnRejectsLostSoulOutsideSectorVerticalBounds()
    {
        var floor = Fixed.Zero;
        var ceiling = Fixed.FromInt(128);
        var height = Fixed.FromInt(56);

        Assert.IsFalse(BoomGameplayBugFixes.IsPainElementalLostSoulOutsideVerticalBounds(
            GameCompatibility.Boom,
            Fixed.FromInt(72),
            height,
            floor,
            ceiling));
        Assert.IsTrue(BoomGameplayBugFixes.IsPainElementalLostSoulOutsideVerticalBounds(
            GameCompatibility.Boom,
            Fixed.FromInt(73),
            height,
            floor,
            ceiling));
        Assert.IsTrue(BoomGameplayBugFixes.IsPainElementalLostSoulOutsideVerticalBounds(
            GameCompatibility.Boom,
            Fixed.FromInt(-1),
            height,
            floor,
            ceiling));

        Assert.IsFalse(BoomGameplayBugFixes.IsPainElementalLostSoulOutsideVerticalBounds(
            GameCompatibility.Vanilla,
            Fixed.FromInt(73),
            height,
            floor,
            ceiling));
    }

    private static World CreateWorld(GameContent content, GameCompatibility compatibility)
    {
        return new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        }, null);
    }

    private static Mobj CreateCrushedCorpse(World world)
    {
        return new Mobj(world)
        {
            Info = DoomInfo.MobjInfos[(int)MobjType.Possessed],
            Height = Fixed.Zero,
            Radius = Fixed.Zero,
            Flags = MobjFlags.Corpse,
            Tics = -1
        };
    }
}
