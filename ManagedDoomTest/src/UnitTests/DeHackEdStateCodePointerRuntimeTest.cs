using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdStateCodePointerRuntimeTest
    {
        [TestMethod]
        public void PatchedFrameDurationAndNextFrameDriveRuntimeStateTransition()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Frame 186
Duration = 0
Next frame = 187
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);
                var actor = SpawnSolidActor(world);

                Assert.IsTrue(actor.SetState((MobjState)186));

                Assert.AreSame(DoomInfo.States[187], actor.State);
                Assert.AreEqual(187, actor.State.Number);
                Assert.AreEqual(DoomInfo.States[187].Tics, actor.Tics);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicPointerCopiedActionExecutesWhenTargetStateIsEntered()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Pointer 1 (Frame 186)
Codep Frame = 191
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);
                var actor = SpawnSolidActor(world);

                Assert.IsTrue((actor.Flags & MobjFlags.Solid) != 0);
                Assert.IsTrue(actor.SetState((MobjState)186));
                Assert.IsTrue((actor.Flags & MobjFlags.Solid) == 0,
                    "The Fall action copied by the classic Pointer block was not executed by Mobj.SetState().");
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void BexCodePtrMobjActionExecutesWhenStateIsEntered()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 186 = Fall
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);
                var actor = SpawnSolidActor(world);

                Assert.IsTrue((actor.Flags & MobjFlags.Solid) != 0);
                Assert.IsTrue(actor.SetState((MobjState)186));
                Assert.IsTrue((actor.Flags & MobjFlags.Solid) == 0,
                    "The BEX Fall action was not executed by Mobj.SetState().");
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void BexCodePtrPlayerActionExecutesThroughPlayerSpriteState()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 186 = Light0
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);
                var player = world.ConsolePlayer;
                player.ExtraLight = 7;

                world.PlayerBehavior.SetPlayerSprite(
                    player,
                    PlayerSprite.Weapon,
                    (MobjState)186);

                Assert.AreEqual(0, player.ExtraLight,
                    "The BEX Light0 player action was not executed by SetPlayerSprite().");
                Assert.AreSame(
                    DoomInfo.States[186],
                    player.PlayerSprites[(int)PlayerSprite.Weapon].State);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void NullCodePointerSuppressesOriginalRuntimeAction()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 191 = NULL
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);
                var actor = SpawnSolidActor(world);

                Assert.IsTrue((actor.Flags & MobjFlags.Solid) != 0);
                Assert.IsTrue(actor.SetState((MobjState)191));
                Assert.IsTrue((actor.Flags & MobjFlags.Solid) != 0,
                    "FRAME 191 normally executes Fall; NULL should suppress that runtime action.");
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void SequentialPatchLoadsDoNotLeaveStaleRuntimeCodePointers()
        {
            var firstPatch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 186 = Fall
");
            var secondPatch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 186 = NULL
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(firstPatch), wad);

                using (var content = GameContent.CreateDummy(WadPath.Doom2))
                {
                    var world = new World(content, new GameOptions(), null);
                    var actor = SpawnSolidActor(world);
                    actor.SetState((MobjState)186);
                    Assert.IsTrue((actor.Flags & MobjFlags.Solid) == 0);
                }

                DeHackEd.Initialize(ArgsForPatch(secondPatch), wad);

                using (var content = GameContent.CreateDummy(WadPath.Doom2))
                {
                    var world = new World(content, new GameOptions(), null);
                    var actor = SpawnSolidActor(world);
                    actor.SetState((MobjState)186);
                    Assert.IsTrue((actor.Flags & MobjFlags.Solid) != 0,
                        "The Fall action from the previous patch load leaked into the next definition set.");
                }

                Reset(wad);

                using (var content = GameContent.CreateDummy(WadPath.Doom2))
                {
                    var world = new World(content, new GameOptions(), null);
                    var actor = SpawnSolidActor(world);
                    actor.SetState((MobjState)186);
                    Assert.IsTrue((actor.Flags & MobjFlags.Solid) != 0,
                        "Vanilla frame 186 should have no action after a full reset.");
                }
            }
            finally
            {
                Reset(wad);
                File.Delete(firstPatch);
                File.Delete(secondPatch);
            }
        }

        private static Mobj SpawnSolidActor(World world)
        {
            var player = world.ConsolePlayer.Mobj;
            var actor = world.ThingAllocation.SpawnMobj(
                player.X,
                player.Y,
                Mobj.OnFloorZ,
                MobjType.Possessed);

            Assert.IsTrue((actor.Flags & MobjFlags.Solid) != 0);
            return actor;
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
                "manageddoom-deh-state-codeptr-runtime-" + Guid.NewGuid().ToString("N") + ".bex");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
