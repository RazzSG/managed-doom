using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdDoomEdNumRuntimeTest
    {
        [TestMethod]
        public void PatchedDoomEdNumSpawnsRenumberedActorAtRuntime()
        {
            var patch = CreatePatch($@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing {(int)MobjType.Keen + 1} (Commander Keen)
ID # = 4321
Hit points = 777
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);

                var spawned = SpawnAndFind(world, 4321);

                Assert.AreEqual(MobjType.Keen, spawned.Type);
                Assert.AreSame(DoomInfo.MobjInfos[(int)MobjType.Keen], spawned.Info);
                Assert.AreEqual(777, spawned.Health);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void DeHackEdCanExposeOriginallyUnplaceableActorThroughCustomDoomEdNum()
        {
            var puffThingNumber = (int)MobjType.Puff + 1;
            var patch = CreatePatch($@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing {puffThingNumber} (Bullet Puff)
ID # = 4322
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Reset(wad);
                Assert.AreEqual(-1, DoomInfo.MobjInfos[(int)MobjType.Puff].DoomEdNum);

                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual(4322, DoomInfo.MobjInfos[(int)MobjType.Puff].DoomEdNum);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);

                var spawned = SpawnAndFind(world, 4322);

                Assert.AreEqual(MobjType.Puff, spawned.Type);
                Assert.AreSame(DoomInfo.MobjInfos[(int)MobjType.Puff], spawned.Info);
                Assert.AreEqual((int)MobjState.Puff1, spawned.State.Number);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void OriginalDoomEdNumStopsMatchingAfterActorIsRenumbered()
        {
            var patch = CreatePatch($@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing {(int)MobjType.Keen + 1} (Commander Keen)
ID # = 4323
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);

                Assert.AreEqual(MobjType.Keen, SpawnAndFind(world, 4323).Type);

                var oldThing = CreateMapThingAtPlayer(world, 72);
                var ex = Assert.ThrowsExactly<Exception>(() => world.ThingAllocation.SpawnMapThing(oldThing));
                Assert.AreEqual("Unknown type!", ex.Message);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void SequentialPatchLoadsDoNotLeaveStaleDoomEdNumMappings()
        {
            var firstPatch = CreatePatch($@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing {(int)MobjType.Keen + 1} (Commander Keen)
ID # = 4324
");
            var secondPatch = CreatePatch($@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing {(int)MobjType.Keen + 1} (Commander Keen)
ID # = 5324
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(firstPatch), wad);
                using (var content = GameContent.CreateDummy(WadPath.Doom2))
                {
                    var world = new World(content, new GameOptions(), null);
                    Assert.AreEqual(MobjType.Keen, SpawnAndFind(world, 4324).Type);
                }

                DeHackEd.Initialize(ArgsForPatch(secondPatch), wad);
                Assert.AreEqual(5324, DoomInfo.MobjInfos[(int)MobjType.Keen].DoomEdNum);

                using (var content = GameContent.CreateDummy(WadPath.Doom2))
                {
                    var world = new World(content, new GameOptions(), null);
                    Assert.AreEqual(MobjType.Keen, SpawnAndFind(world, 5324).Type);

                    var staleThing = CreateMapThingAtPlayer(world, 4324);
                    Assert.ThrowsExactly<Exception>(() => world.ThingAllocation.SpawnMapThing(staleThing));
                }
            }
            finally
            {
                Reset(wad);
                File.Delete(firstPatch);
                File.Delete(secondPatch);
            }
        }

        [TestMethod]
        public void ResetRestoresOriginalDoomEdNumForRuntimeSpawning()
        {
            var patch = CreatePatch($@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing {(int)MobjType.Keen + 1} (Commander Keen)
ID # = 4325
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual(4325, DoomInfo.MobjInfos[(int)MobjType.Keen].DoomEdNum);

                Reset(wad);
                Assert.AreEqual(72, DoomInfo.MobjInfos[(int)MobjType.Keen].DoomEdNum);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);

                Assert.AreEqual(MobjType.Keen, SpawnAndFind(world, 72).Type);

                var staleThing = CreateMapThingAtPlayer(world, 4325);
                Assert.ThrowsExactly<Exception>(() => world.ThingAllocation.SpawnMapThing(staleThing));
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        private static Mobj SpawnAndFind(World world, int doomEdNum)
        {
            var mapThing = CreateMapThingAtPlayer(world, doomEdNum);
            world.ThingAllocation.SpawnMapThing(mapThing);

            foreach (var thinker in world.Thinkers)
            {
                if (thinker is Mobj mobj && ReferenceEquals(mobj.SpawnPoint, mapThing))
                {
                    return mobj;
                }
            }

            Assert.Fail($"No runtime mobj was spawned for DoomEdNum {doomEdNum}.");
            return null;
        }

        private static MapThing CreateMapThingAtPlayer(World world, int doomEdNum)
        {
            var player = world.ConsolePlayer.Mobj;
            return new MapThing(player.X, player.Y, Angle.Ang0, doomEdNum, ThingFlags.Normal);
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
                "manageddoom-deh-doomednum-runtime-" + Guid.NewGuid().ToString("N") + ".deh");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
