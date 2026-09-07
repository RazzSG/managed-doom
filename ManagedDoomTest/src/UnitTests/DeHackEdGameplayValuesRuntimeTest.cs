using System;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdGameplayValuesRuntimeTest
    {
        [TestMethod]
        public void MiscInitialHealthAndBulletsAreConsumedByPlayerReborn()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "Misc 0\n" +
                "Initial Health = 137\n" +
                "Initial Bullets = 73\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var player = new Player(0);
                player.Reborn();

                Assert.AreEqual(137, player.Health);
                Assert.AreEqual(73, player.Ammo[(int)AmmoType.Clip]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void AmmoMaxAndPerAmmoAreConsumedByPlayerAndItemPickup()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "Ammo 0\n" +
                "Max ammo = 17\n" +
                "Per ammo = 3\n\n" +
                "Misc 0\n" +
                "Initial Bullets = 0\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var player = new Player(0);
                player.Reborn();
                Assert.AreEqual(17, player.MaxAmmo[(int)AmmoType.Clip]);
                Assert.AreEqual(0, player.Ammo[(int)AmmoType.Clip]);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);

                Assert.IsTrue(world.ItemPickup.GiveAmmo(player, AmmoType.Clip, 1));
                Assert.AreEqual(3, player.Ammo[(int)AmmoType.Clip]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void WeaponAmmoTypeIsConsumedByGiveWeaponAtRuntime()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "Weapon 2\n" +
                "Ammo type = 0\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                using var content = GameContent.CreateDummy(WadPath.Doom2);
                var world = new World(content, new GameOptions(), null);
                var player = new Player(0);
                player.Reborn();
                player.Ammo[(int)AmmoType.Clip] = 0;
                player.Ammo[(int)AmmoType.Shell] = 0;

                Assert.IsTrue(world.ItemPickup.GiveWeapon(player, WeaponType.Shotgun, false));

                Assert.IsTrue(player.WeaponOwned[(int)WeaponType.Shotgun]);
                Assert.AreEqual(WeaponType.Shotgun, player.PendingWeapon);
                Assert.AreEqual(
                    2 * DoomInfo.AmmoInfos.Clip[(int)AmmoType.Clip],
                    player.Ammo[(int)AmmoType.Clip]);
                Assert.AreEqual(0, player.Ammo[(int)AmmoType.Shell]);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void InvalidWeaponNumbersAreIgnoredAndLaterValidWeaponBlockStillApplies()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                "Weapon -1\n" +
                "Ammo type = 2\n\n" +
                "Weapon 999\n" +
                "Ammo type = 2\n\n" +
                "Weapon 2\n" +
                "Ammo type = 0\n");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual(AmmoType.Clip, DoomInfo.WeaponInfos[(int)WeaponType.Shotgun].Ammo);
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

        private static string CreatePatch(string body)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-runtime-values-" + Guid.NewGuid().ToString("N") + ".deh");

            var text =
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = 19\n" +
                "Patch format = 6\n\n" +
                body;

            File.WriteAllText(path, text);
            return path;
        }
    }
}
