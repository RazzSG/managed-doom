using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdAmmoBlockTest
    {
        [TestMethod]
        public void ClassicAmmoBlockMapsBothFieldsAndUsesZeroBasedNumbering()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Ammo 0
Max ammo = 321
Per ammo = 17

Ammo 1
Max ammo = 61
Per ammo = 7

Ammo 2
Max ammo = 444
Per ammo = 31

Ammo 3
Max ammo = 77
Per ammo = 3
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                CollectionAssert.AreEqual(new[] { 321, 61, 444, 77 }, DoomInfo.AmmoInfos.Max);
                CollectionAssert.AreEqual(new[] { 17, 7, 31, 3 }, DoomInfo.AmmoInfos.Clip);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void InvalidAmmoNumbersAreIgnoredAndLaterAmmoBlocksStillApply()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Ammo -1
Max ammo = 901
Per ammo = 91

Ammo 4
Max ammo = 902
Per ammo = 92

Ammo 9999
Max ammo = 903
Per ammo = 93

Ammo 2
Max ammo = 654
Per ammo = 23
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(200, DoomInfo.AmmoInfos.Max[0]);
                Assert.AreEqual(50, DoomInfo.AmmoInfos.Max[1]);
                Assert.AreEqual(654, DoomInfo.AmmoInfos.Max[2]);
                Assert.AreEqual(50, DoomInfo.AmmoInfos.Max[3]);

                Assert.AreEqual(10, DoomInfo.AmmoInfos.Clip[0]);
                Assert.AreEqual(4, DoomInfo.AmmoInfos.Clip[1]);
                Assert.AreEqual(23, DoomInfo.AmmoInfos.Clip[2]);
                Assert.AreEqual(1, DoomInfo.AmmoInfos.Clip[3]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void AmmoPatchAffectsPlayerCapacityAndPickupAmountAtRuntime()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Ammo 0
Max ammo = 321
Per ammo = 17
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var player = new Player(0);
                player.Reborn();
                Assert.AreEqual(321, player.MaxAmmo[(int)AmmoType.Clip]);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);

                player.Ammo[(int)AmmoType.Clip] = 0;
                Assert.IsTrue(world.ItemPickup.GiveAmmo(player, AmmoType.Clip, 1));
                Assert.AreEqual(17, player.Ammo[(int)AmmoType.Clip]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void SequentialInitializationRestoresAmmoDefinitions()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Ammo 0
Max ammo = 321
Per ammo = 17
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual(321, DoomInfo.AmmoInfos.Max[0]);
                Assert.AreEqual(17, DoomInfo.AmmoInfos.Clip[0]);

                Reset(wad);
                Assert.AreEqual(200, DoomInfo.AmmoInfos.Max[0]);
                Assert.AreEqual(10, DoomInfo.AmmoInfos.Clip[0]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
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
            var path = Path.Combine(Path.GetTempPath(), "manageddoom-deh-ammo-" + Guid.NewGuid().ToString("N") + ".deh");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
