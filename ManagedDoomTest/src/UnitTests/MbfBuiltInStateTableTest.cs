using System;
using System.IO;
using ManagedDoom;
using ManagedDoom.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfBuiltInStateTableTest
{
    [TestMethod]
    public void CanonicalMbfProjectileStatesOccupySlots968Through971()
    {
        using var wad = new Wad(WadPath.Doom2);
        Reset(wad);

        var invisible = DoomInfo.States[967];
        Assert.AreEqual(Sprite.TNT1, invisible.Sprite);
        Assert.AreEqual(-1, invisible.Tics);
        Assert.AreEqual(MobjState.MbfReservedState1, invisible.Next);

        var timed = DoomInfo.States[968];
        Assert.AreEqual(Sprite.MISL, timed.Sprite);
        Assert.AreEqual(32768, timed.Frame);
        Assert.AreEqual(1000, timed.Tics);
        Assert.IsNotNull(timed.MobjAction);
        Assert.AreEqual("Die", timed.MobjAction.Method.Name);
        Assert.AreEqual(MobjState.MbfReservedState2, timed.Next);

        var detonate1 = DoomInfo.States[969];
        Assert.AreEqual(Sprite.MISL, detonate1.Sprite);
        Assert.AreEqual(32769, detonate1.Frame);
        Assert.AreEqual(4, detonate1.Tics);
        Assert.IsNotNull(detonate1.MobjAction);
        Assert.AreEqual("Scream", detonate1.MobjAction.Method.Name);
        Assert.AreEqual(MobjState.MbfReservedState4, detonate1.Next);

        var detonate2 = DoomInfo.States[970];
        Assert.AreEqual(Sprite.MISL, detonate2.Sprite);
        Assert.AreEqual(32770, detonate2.Frame);
        Assert.AreEqual(6, detonate2.Tics);
        Assert.IsNotNull(detonate2.MobjAction);
        Assert.AreEqual("Detonate", detonate2.MobjAction.Method.Name);
        Assert.AreEqual(MobjState.MbfReservedState5, detonate2.Next);

        var detonate3 = DoomInfo.States[971];
        Assert.AreEqual(Sprite.MISL, detonate3.Sprite);
        Assert.AreEqual(32771, detonate3.Frame);
        Assert.AreEqual(10, detonate3.Tics);
        Assert.IsNull(detonate3.MobjAction);
        Assert.AreEqual(MobjState.Null, detonate3.Next);
    }

    [TestMethod]
    public void MbfEditStylePatchChangesDurationWithoutErasingBuiltInVisualOrActions()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 21
Patch format = 6

Thing 34
Bits = BOUNCES | DROPOFF
Hit points = 10
Initial frame = 968
Death frame = 969
Missile damage = 128
Mass = 200

Frame 968
Duration = 135
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-deh", patch, "-nodeh" }), wad);

            var info = DoomInfo.MobjInfos[33];
            Assert.AreEqual((MobjState)968, info.SpawnState);
            Assert.AreEqual((MobjState)969, info.DeathState);
            Assert.IsTrue((info.Flags & MobjFlags.Bounces) != 0);
            Assert.IsTrue((info.Flags & MobjFlags.DropOff) != 0);
            Assert.IsFalse((info.Flags & MobjFlags.Missile) != 0,
                "A mnemonic Bits override replaces the original rocket flags; MBFEDIT's grenade must not use missile collision semantics.");
            Assert.IsFalse((info.Flags & MobjFlags.NoGravity) != 0,
                "BOUNCES | DROPOFF must clear the original rocket NOGRAVITY flag so the grenade falls and floor-bounces.");

            var state = DoomInfo.States[968];
            Assert.AreEqual(Sprite.MISL, state.Sprite);
            Assert.AreEqual(32768, state.Frame);
            Assert.AreEqual(135, state.Tics);
            Assert.IsNotNull(state.MobjAction);
            Assert.AreEqual("Die", state.MobjAction.Method.Name);
            Assert.AreEqual((MobjState)968, state.Next);

            Assert.AreEqual("Scream", DoomInfo.States[969].MobjAction.Method.Name);
            Assert.AreEqual("Detonate", DoomInfo.States[970].MobjAction.Method.Name);
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }


    [TestMethod]
    public void MbfTimedBouncerExpiresAfterPatchedDurationAndEntersDeathState()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 21
Patch format = 6

Thing 34
Bits = BOUNCES | DROPOFF
Hit points = 10
Initial frame = 968
Death frame = 969
Missile damage = 128
Mass = 200

Frame 968
Duration = 135
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-deh", patch, "-nodeh" }), wad);

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var world = new World(
                content,
                new GameOptions { Compatibility = GameCompatibility.Mbf },
                null);

            var player = world.ConsolePlayer.Mobj;
            var actor = world.ThingAllocation.SpawnMobj(
                player.X,
                player.Y,
                Mobj.OnFloorZ,
                MobjType.Rocket);

            Assert.AreEqual(968, actor.State.Number);
            Assert.AreEqual(135, actor.Tics);
            Assert.IsTrue((actor.Flags & MobjFlags.Bounces) != 0);
            Assert.IsFalse((actor.Flags & MobjFlags.Shootable) != 0,
                "The MBFEDIT-style Bits replacement intentionally leaves this bouncer non-SHOOTABLE.");
            Assert.AreEqual(10, actor.Health);

            for (var i = 0; i < 134; i++)
            {
                actor.Run();
            }

            Assert.AreEqual(968, actor.State.Number);
            Assert.AreEqual(1, actor.Tics);
            Assert.AreEqual(10, actor.Health);

            actor.Run();

            Assert.IsTrue(actor.Health <= 0,
                "A_Die must be allowed to damage an MBF BOUNCES actor when the 135-tic state expires.");
            Assert.AreEqual(969, actor.State.Number);
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    [TestMethod]
    public void MbfDetonateDamagesNearbyNonShootableBouncer()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 21
Patch format = 6

Thing 34
Bits = BOUNCES | DROPOFF
Hit points = 10
Initial frame = 968
Death frame = 969
Missile damage = 128
Mass = 200

Frame 968
Duration = 135
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-deh", patch, "-nodeh" }), wad);

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var world = new World(
                content,
                new GameOptions { Compatibility = GameCompatibility.Mbf },
                null);

            var player = world.ConsolePlayer.Mobj;
            var first = world.ThingAllocation.SpawnMobj(
                player.X,
                player.Y,
                Mobj.OnFloorZ,
                MobjType.Rocket);
            var second = world.ThingAllocation.SpawnMobj(
                player.X + Fixed.FromInt(32),
                player.Y,
                Mobj.OnFloorZ,
                MobjType.Rocket);

            Assert.IsTrue((first.Flags & MobjFlags.Bounces) != 0);
            Assert.IsTrue((second.Flags & MobjFlags.Bounces) != 0);
            Assert.IsFalse((second.Flags & MobjFlags.Shootable) != 0);
            Assert.AreEqual(10, second.Health);
            Assert.AreEqual(968, second.State.Number);

            // In the real MBF sequence A_Die has already killed the first
            // bouncer before state 970 runs A_Detonate. Reproduce that point
            // in the lifecycle so the radius attack only needs to affect the
            // neighboring bouncer.
            first.Health = 0;
            first.Target = player;
            first.SetState((MobjState)970);

            Assert.IsTrue(second.Health <= 0,
                "MBF A_Detonate must include nearby BOUNCES actors even when SHOOTABLE was removed by DeHackEd Bits.");
            Assert.AreEqual(969, second.State.Number,
                "The neighboring bouncer must enter its DeathState, producing the PrBoom-style chain detonation.");
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    [TestMethod]
    public void MbfNonMissileProjectileSkipsImmediateSpawnCollisionExplosion()
    {
        var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 21
Patch format = 6

Thing 34
Bits = BOUNCES | DROPOFF
Hit points = 10
Initial frame = 968
Death frame = 969
Missile damage = 128
Mass = 200

Frame 968
Duration = 135
");

        using var wad = new Wad(WadPath.Doom2);

        try
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-deh", patch, "-nodeh" }), wad);

            using var content = GameContent.CreateDummy(WadPath.Doom2);
            var world = new World(
                content,
                new GameOptions { Compatibility = GameCompatibility.Mbf },
                null);

            var source = world.ConsolePlayer.Mobj;
            var blocker = world.ThingAllocation.SpawnMobj(
                source.X + Fixed.FromInt(10),
                source.Y,
                Mobj.OnFloorZ,
                MobjType.Barrel);

            var actor = world.ThingAllocation.SpawnMissile(
                source,
                blocker,
                MobjType.Rocket);

            Assert.IsTrue((actor.Flags & MobjFlags.Bounces) != 0);
            Assert.IsFalse((actor.Flags & MobjFlags.Missile) != 0,
                "The MBFEDIT-style projectile is deliberately a non-MISSILE bouncer.");
            Assert.AreEqual(968, actor.State.Number,
                "MBF P_CheckMissileSpawn must not run the immediate collision/explosion path for non-MISSILE projectile actors.");
            Assert.AreEqual(10, actor.Health);
            Assert.IsTrue(actor.MomX > Fixed.Zero,
                "The half-step spawn advance must preserve forward momentum so normal XY movement can resolve the contact as a bounce on the next tic.");
        }
        finally
        {
            Reset(wad);
            File.Delete(patch);
        }
    }

    private static void Reset(Wad wad)
    {
        DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
    }

    private static string CreatePatch(string text)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "manageddoom-mbf-built-in-states-" + Guid.NewGuid().ToString("N") + ".deh");
        File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
        return path;
    }
}
