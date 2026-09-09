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
public sealed class MbfMushroomCodePointerTest
{
    private static readonly Fixed Half = new(Fixed.FracUnit / 2);

    [TestMethod]
    public void CompatibilityBoundaryKeepsMushroomMbfOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomWorld = NewWorld(content, GameCompatibility.Boom);
        PreparePlayer(boomWorld);
        var boomActor = SpawnActor(boomWorld);
        boomActor.State = State(0, 0);
        var boomBefore = CountType(boomWorld, MobjType.Fatshot);

        Assert.IsFalse(MbfMushroomCodePointer.Execute(boomWorld, boomActor));
        Assert.AreEqual(boomBefore, CountType(boomWorld, MobjType.Fatshot));

        var mbfWorld = NewWorld(content, GameCompatibility.Mbf);
        PreparePlayer(mbfWorld);
        var mbfActor = SpawnActor(mbfWorld);
        mbfActor.State = State(0, 0);
        var mbfBefore = CountType(mbfWorld, MobjType.Fatshot);

        Assert.IsTrue(MbfMushroomCodePointer.Execute(mbfWorld, mbfActor));
        Assert.AreEqual(mbfBefore + 9, CountType(mbfWorld, MobjType.Fatshot));
    }

    [TestMethod]
    public void DefaultsUseFourForVerticalScaleAndHalfForMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var defaultWorld = NewWorld(content, GameCompatibility.Mbf);
        PreparePlayer(defaultWorld);
        var defaultActor = SpawnActor(defaultWorld);
        defaultActor.State = State(0, 0);
        var defaultBefore = CaptureType(defaultWorld, MobjType.Fatshot);

        Assert.IsTrue(MbfMushroomCodePointer.Execute(defaultWorld, defaultActor));
        var defaultMissiles = FindNewType(defaultWorld, defaultBefore, MobjType.Fatshot);

        var explicitWorld = NewWorld(content, GameCompatibility.Mbf);
        PreparePlayer(explicitWorld);
        var explicitActor = SpawnActor(explicitWorld);
        explicitActor.State = State(Fixed.FromInt(4).Data, Fixed.One.Data);
        var explicitBefore = CaptureType(explicitWorld, MobjType.Fatshot);

        Assert.IsTrue(MbfMushroomCodePointer.Execute(explicitWorld, explicitActor));
        var explicitMissiles = FindNewType(explicitWorld, explicitBefore, MobjType.Fatshot);

        Assert.AreEqual(9, defaultMissiles.Count);
        Assert.AreEqual(9, explicitMissiles.Count);

        for (var i = 0; i < defaultMissiles.Count; i++)
        {
            var actual = defaultMissiles[i];
            var unscaled = explicitMissiles[i];

            Assert.AreEqual(unscaled.X.Data, actual.X.Data);
            Assert.AreEqual(unscaled.Y.Data, actual.Y.Data);
            Assert.AreEqual(unscaled.Z.Data, actual.Z.Data);
            Assert.AreEqual((unscaled.MomX * Half).Data, actual.MomX.Data);
            Assert.AreEqual((unscaled.MomY * Half).Data, actual.MomY.Data);
            Assert.AreEqual((unscaled.MomZ * Half).Data, actual.MomZ.Data);
            Assert.AreEqual(0, (int)(actual.Flags & MobjFlags.NoGravity));
        }
    }

    [TestMethod]
    public void Misc1IsFixedPointAndChangesVerticalAim()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var highWorld = NewWorld(content, GameCompatibility.Mbf21);
        PreparePlayer(highWorld);
        var highActor = SpawnActor(highWorld);
        highActor.State = State(Fixed.FromInt(4).Data, Fixed.One.Data);
        var highBefore = CaptureType(highWorld, MobjType.Fatshot);
        MbfMushroomCodePointer.Execute(highWorld, highActor);
        var high = FindDiagonalMissile(FindNewType(highWorld, highBefore, MobjType.Fatshot));

        var lowWorld = NewWorld(content, GameCompatibility.Mbf21);
        PreparePlayer(lowWorld);
        var lowActor = SpawnActor(lowWorld);
        lowActor.State = State(Fixed.One.Data, Fixed.One.Data);
        var lowBefore = CaptureType(lowWorld, MobjType.Fatshot);
        MbfMushroomCodePointer.Execute(lowWorld, lowActor);
        var low = FindDiagonalMissile(FindNewType(lowWorld, lowBefore, MobjType.Fatshot));

        Assert.IsNotNull(high);
        Assert.IsNotNull(low);
        Assert.IsTrue(low.MomZ > Fixed.Zero);
        Assert.IsTrue(high.MomZ > low.MomZ);
        Assert.AreEqual(high.MomX.Data, low.MomX.Data);
        Assert.AreEqual(high.MomY.Data, low.MomY.Data);
    }

    [TestMethod]
    public void BexRegistersMushroomAndExecutesThroughState()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 194 = Mushroom
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            Assert.AreEqual("Mushroom", DoomInfo.States[194].MobjAction.Method.Name);

            DoomInfo.States[194].Misc1 = Fixed.FromInt(4).Data;
            DoomInfo.States[194].Misc2 = Fixed.One.Data;

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var world = NewWorld(content, GameCompatibility.Mbf);
            PreparePlayer(world);
            var actor = SpawnActor(world);
            var before = CountType(world, MobjType.Fatshot);

            Assert.IsTrue(actor.SetState((MobjState)194));
            Assert.AreEqual(before + 9, CountType(world, MobjType.Fatshot));
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    private static World NewWorld(GameContent content, GameCompatibility compatibility)
    {
        return new World(
            content,
            new GameOptions
            {
                Compatibility = compatibility
            },
            null);
    }

    private static void PreparePlayer(World world)
    {
        // Keep the player from blocking the immediately spawned projectiles or
        // receiving the preliminary radius effect in this focused test.
        world.ConsolePlayer.Mobj.Flags &= ~(MobjFlags.Solid | MobjFlags.Shootable);
    }

    private static Mobj SpawnActor(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Fatshot);
    }

    private static MobjStateDef State(int misc1, int misc2)
    {
        return new MobjStateDef(
            -1,
            Sprite.MANF,
            0,
            1,
            null,
            null,
            MobjState.Null,
            misc1,
            misc2);
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

    private static List<Mobj> FindNewType(World world, HashSet<Mobj> existing, MobjType type)
    {
        var result = new List<Mobj>();
        foreach (var thinker in world.Thinkers)
        {
            if (thinker is Mobj mobj && mobj.Type == type && !existing.Contains(mobj))
                result.Add(mobj);
        }

        return result;
    }

    private static Mobj FindDiagonalMissile(List<Mobj> missiles)
    {
        foreach (var missile in missiles)
        {
            if (missile.MomX < Fixed.Zero && missile.MomY < Fixed.Zero)
                return missile;
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
            "manageddoom-mbf-mushroom-" + Guid.NewGuid().ToString("N") + ".bex");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }
}
