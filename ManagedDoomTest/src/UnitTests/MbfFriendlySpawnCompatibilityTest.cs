using System;
using System.Collections.Generic;
using System.IO;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Things;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfFriendlySpawnCompatibilityTest
{
    [TestMethod]
    public void CompatibilityBoundaryKeepsASpawnMbfOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomWorld = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Boom },
            null);
        var boomSource = SpawnSource(boomWorld);
        boomSource.State = SpawnState(MobjType.Troop, 8);

        Assert.IsNull(MbfFriendlySpawnCompatibility.SpawnFromState(boomWorld, boomSource));

        var mbfWorld = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);
        var mbfSource = SpawnSource(mbfWorld);
        mbfSource.State = SpawnState(MobjType.Troop, 8);

        Assert.IsNotNull(MbfFriendlySpawnCompatibility.SpawnFromState(mbfWorld, mbfSource));
    }

    [TestMethod]
    public void ASpawnUsesOneBasedTypeAndWholeMapUnitZOffset()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);
        var source = SpawnSource(world);
        source.State = SpawnState(MobjType.Troop, 8);

        var spawned = MbfFriendlySpawnCompatibility.SpawnFromState(world, source);

        Assert.IsNotNull(spawned);
        Assert.AreEqual(MobjType.Troop, spawned.Type);
        Assert.AreEqual(source.X.Data, spawned.X.Data);
        Assert.AreEqual(source.Y.Data, spawned.Y.Data);
        Assert.AreEqual((source.Z + Fixed.FromInt(8)).Data, spawned.Z.Data);
    }

    [TestMethod]
    public void CompFriendlySpawnCopiesFriendBitFromSource()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.MbfOptions.CompFriendlySpawn = true;
        var world = new World(content, options, null);
        var source = SpawnSource(world);
        source.State = SpawnState(MobjType.Troop, 0);
        source.Flags |= MobjFlags.Friend;

        var spawned = MbfFriendlySpawnCompatibility.SpawnFromState(world, source);

        Assert.IsNotNull(spawned);
        Assert.IsTrue((spawned.Flags & MobjFlags.Friend) != 0);
    }

    [TestMethod]
    public void CompFriendlySpawnAlsoClearsDefaultFriendBitWhenSourceIsHostile()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.MbfOptions.CompFriendlySpawn = true;
        var world = new World(content, options, null);
        var source = SpawnSource(world);
        source.State = SpawnState(MobjType.Troop, 0);
        source.Flags &= ~MobjFlags.Friend;

        var info = DoomInfo.MobjInfos[(int)MobjType.Troop];
        var originalFlags = info.Flags;

        try
        {
            info.Flags |= MobjFlags.Friend;

            var spawned = MbfFriendlySpawnCompatibility.SpawnFromState(world, source);

            Assert.IsNotNull(spawned);
            Assert.IsTrue((spawned.Flags & MobjFlags.Friend) == 0);
        }
        finally
        {
            info.Flags = originalFlags;
        }
    }

    [TestMethod]
    public void DisabledCompFriendlySpawnPreservesSpawnedTypeDefault()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.MbfOptions.CompFriendlySpawn = false;
        var world = new World(content, options, null);
        var source = SpawnSource(world);
        source.State = SpawnState(MobjType.Troop, 0);
        source.Flags |= MobjFlags.Friend;

        var spawned = MbfFriendlySpawnCompatibility.SpawnFromState(world, source);

        Assert.IsNotNull(spawned);
        Assert.IsTrue((spawned.Flags & MobjFlags.Friend) == 0);
    }

    [TestMethod]
    public void Mbf21HonorsExplicitCompFriendlySpawnFalse()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf21 };
        options.MbfOptions.CompFriendlySpawn = false;
        var world = new World(content, options, null);
        var source = SpawnSource(world);
        source.State = SpawnState(MobjType.Troop, 0);
        source.Flags |= MobjFlags.Friend;

        var spawned = MbfFriendlySpawnCompatibility.SpawnFromState(world, source);

        Assert.IsNotNull(spawned);
        Assert.IsTrue((spawned.Flags & MobjFlags.Friend) == 0);
    }

    [TestMethod]
    public void BexSpawnCodePointerExecutesRuntimeASpawn()
    {
        var patch = CreatePatch($@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Frame 186
Unknown 1 = {(int)MobjType.Troop + 1}
Unknown 2 = 8

[CODEPTR]
FRAME 186 = Spawn
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            Assert.IsNotNull(DoomInfo.States[186].MobjAction);
            Assert.AreEqual("Spawn", DoomInfo.States[186].MobjAction.Method.Name);

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
            var world = new World(content, options, null);
            var source = SpawnSource(world);
            source.Flags |= MobjFlags.Friend;

            var oldTroopCount = CountType(world, MobjType.Troop);
            var existingTroops = CaptureType(world, MobjType.Troop);
            var sourceZ = source.Z;

            Assert.IsTrue(source.SetState((MobjState)186));
            Assert.AreEqual(oldTroopCount + 1, CountType(world, MobjType.Troop));

            var spawned = FindNewType(world, existingTroops, MobjType.Troop);
            Assert.IsNotNull(spawned);
            Assert.AreEqual((sourceZ + Fixed.FromInt(8)).Data, spawned.Z.Data);
            Assert.IsTrue((spawned.Flags & MobjFlags.Friend) != 0);
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    private static Mobj SpawnSource(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Possessed);
    }

    private static MobjStateDef SpawnState(MobjType type, int zOffset)
    {
        return new MobjStateDef(
            -1,
            Sprite.POSS,
            0,
            1,
            null,
            null,
            MobjState.Null,
            (int)type + 1,
            zOffset);
    }

    private static int CountType(World world, MobjType type)
    {
        var count = 0;
        foreach (var thinker in world.Thinkers)
        {
            if (thinker is Mobj mobj && mobj.Type == type)
                count++;
        }

        return count;
    }

    private static HashSet<Mobj> CaptureType(World world, MobjType type)
    {
        var result = new HashSet<Mobj>();

        foreach (var thinker in world.Thinkers)
        {
            if (thinker is Mobj mobj && mobj.Type == type)
                result.Add(mobj);
        }

        return result;
    }

    private static Mobj FindNewType(World world, HashSet<Mobj> existing, MobjType type)
    {
        foreach (var thinker in world.Thinkers)
        {
            if (thinker is Mobj mobj && mobj.Type == type && !existing.Contains(mobj))
                return mobj;
        }

        return null;
    }

    private static CommandLineArgs ArgsForPatch(string patchPath)
    {
        return new CommandLineArgs(new[] { "-deh", patchPath, "-nodeh" });
    }

    private static void Reset(Wad wad)
    {
        DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
    }

    private static string CreatePatch(string text)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "manageddoom-mbf-friendlyspawn-" + Guid.NewGuid().ToString("N") + ".bex");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }
}
