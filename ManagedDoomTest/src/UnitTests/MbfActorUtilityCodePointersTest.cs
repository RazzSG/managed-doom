using System;
using System.IO;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Things;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfActorUtilityCodePointersTest
{
    [TestMethod]
    public void CompatibilityBoundaryKeepsActorUtilityPointersMbfOnly()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var boomWorld = NewWorld(content, GameCompatibility.Boom);
        var boomActor = SpawnActor(boomWorld, MobjType.Possessed);
        boomActor.MomX = Fixed.FromInt(1);
        boomActor.MomY = Fixed.FromInt(2);
        boomActor.MomZ = Fixed.FromInt(3);
        var boomHealth = boomActor.Health;

        Assert.IsFalse(MbfActorUtilityCodePointers.Die(boomWorld, boomActor));
        Assert.IsFalse(MbfActorUtilityCodePointers.Detonate(boomWorld, boomActor));
        Assert.IsFalse(MbfActorUtilityCodePointers.Stop(boomWorld, boomActor));
        Assert.AreEqual(boomHealth, boomActor.Health);
        Assert.AreEqual(Fixed.FromInt(1).Data, boomActor.MomX.Data);
        Assert.AreEqual(Fixed.FromInt(2).Data, boomActor.MomY.Data);
        Assert.AreEqual(Fixed.FromInt(3).Data, boomActor.MomZ.Data);

        var mbfWorld = NewWorld(content, GameCompatibility.Mbf);
        var mbfActor = SpawnActor(mbfWorld, MobjType.Possessed);
        mbfActor.MomX = Fixed.FromInt(1);
        mbfActor.MomY = Fixed.FromInt(2);
        mbfActor.MomZ = Fixed.FromInt(3);

        Assert.IsTrue(MbfActorUtilityCodePointers.Stop(mbfWorld, mbfActor));
        Assert.AreEqual(Fixed.Zero.Data, mbfActor.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, mbfActor.MomY.Data);
        Assert.AreEqual(Fixed.Zero.Data, mbfActor.MomZ.Data);
    }

    [TestMethod]
    public void DieUsesExistingDamageLifecycle()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewWorld(content, GameCompatibility.Mbf);
        var actor = SpawnActor(world, MobjType.Troop);

        Assert.IsTrue(actor.Health > 0);
        Assert.IsTrue((actor.Flags & MobjFlags.Shootable) != 0);

        Assert.IsTrue(MbfActorUtilityCodePointers.Die(world, actor));

        Assert.IsTrue(actor.Health <= 0);
        Assert.IsTrue((actor.Flags & MobjFlags.Shootable) == 0);
        Assert.IsTrue((actor.Flags & MobjFlags.Corpse) != 0);
    }

    [TestMethod]
    public void DetonateUsesActorInfoDamageForRadiusAttack()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewWorld(content, GameCompatibility.Mbf);
        var player = world.ConsolePlayer.Mobj;
        var source = world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Rocket);
        var target = SpawnActor(world, MobjType.Troop);
        target.X = source.X;
        target.Y = source.Y;

        var before = target.Health;
        var expectedDamage = source.Info.Damage;

        Assert.IsTrue(expectedDamage > 0);
        Assert.IsTrue(MbfActorUtilityCodePointers.Detonate(world, source));
        Assert.AreEqual(before - expectedDamage, target.Health);
    }

    [TestMethod]
    public void StopClearsAllMomentumComponents()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = NewWorld(content, GameCompatibility.Mbf21);
        var actor = SpawnActor(world, MobjType.Possessed);
        actor.MomX = Fixed.FromInt(4);
        actor.MomY = Fixed.FromInt(-5);
        actor.MomZ = Fixed.FromInt(6);

        Assert.IsTrue(MbfActorUtilityCodePointers.Stop(world, actor));
        Assert.AreEqual(Fixed.Zero.Data, actor.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, actor.MomY.Data);
        Assert.AreEqual(Fixed.Zero.Data, actor.MomZ.Data);
    }

    [TestMethod]
    public void BexRegistersDieDetonateAndStopAndExecutesStop()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 186 = Die
FRAME 187 = Detonate
FRAME 188 = Stop
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(ArgsForPatch(patch), wad);

            Assert.AreEqual("Die", DoomInfo.States[186].MobjAction.Method.Name);
            Assert.AreEqual("Detonate", DoomInfo.States[187].MobjAction.Method.Name);
            Assert.AreEqual("Stop", DoomInfo.States[188].MobjAction.Method.Name);

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var world = NewWorld(content, GameCompatibility.Mbf);
            var actor = SpawnActor(world, MobjType.Possessed);
            actor.MomX = Fixed.FromInt(7);
            actor.MomY = Fixed.FromInt(8);
            actor.MomZ = Fixed.FromInt(9);

            Assert.IsTrue(actor.SetState((MobjState)188));
            Assert.AreEqual(Fixed.Zero.Data, actor.MomX.Data);
            Assert.AreEqual(Fixed.Zero.Data, actor.MomY.Data);
            Assert.AreEqual(Fixed.Zero.Data, actor.MomZ.Data);
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
            new GameOptions { Compatibility = compatibility },
            null);
    }

    private static Mobj SpawnActor(World world, MobjType type)
    {
        var player = world.ConsolePlayer.Mobj;
        return world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            type);
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
            "manageddoom-mbf-actor-utility-" + Guid.NewGuid().ToString("N") + ".bex");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }
}
