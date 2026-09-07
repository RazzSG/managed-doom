using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdThingBlockTest
    {
        [TestMethod]
        public void ClassicThingBlockMapsAllDoom19Fields()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2 (Former Human)
ID # = 4321
Initial frame = 10
Hit points = 1234
First moving frame = 11
Alert sound = 1
Reaction time = 17
Attack sound = 2
Injury frame = 12
Pain chance = 99
Pain sound = 3
Close attack frame = 13
Far attack frame = 14
Death frame = 15
Exploding frame = 16
Death sound = 4
Speed = 123456
Width = 1966080
Height = 4194304
Mass = 555
Missile damage = 13
Action sound = 5
Bits = 4194310
Respawn frame = 17
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var info = DoomInfo.MobjInfos[(int)MobjType.Possessed];
                Assert.AreEqual(4321, info.DoomEdNum);
                Assert.AreEqual(10, (int)info.SpawnState);
                Assert.AreEqual(1234, info.SpawnHealth);
                Assert.AreEqual(11, (int)info.SeeState);
                Assert.AreEqual(1, (int)info.SeeSound);
                Assert.AreEqual(17, info.ReactionTime);
                Assert.AreEqual(2, (int)info.AttackSound);
                Assert.AreEqual(12, (int)info.PainState);
                Assert.AreEqual(99, info.PainChance);
                Assert.AreEqual(3, (int)info.PainSound);
                Assert.AreEqual(13, (int)info.MeleeState);
                Assert.AreEqual(14, (int)info.MissileState);
                Assert.AreEqual(15, (int)info.DeathState);
                Assert.AreEqual(16, (int)info.XdeathState);
                Assert.AreEqual(4, (int)info.DeathSound);
                Assert.AreEqual(123456, info.Speed);
                Assert.AreEqual(1966080, info.Radius.Data);
                Assert.AreEqual(4194304, info.Height.Data);
                Assert.AreEqual(555, info.Mass);
                Assert.AreEqual(13, info.Damage);
                Assert.AreEqual(5, (int)info.ActiveSound);
                Assert.AreEqual(4194310, (int)info.Flags);
                Assert.AreEqual(17, (int)info.Raisestate);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void InvalidThingNumbersAreIgnoredAndLaterThingBlocksStillApply()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 0 (Invalid low)
Hit points = 9001

Thing -1 (Invalid signed low)
Hit points = 9002

Thing 9999 (Invalid high)
Hit points = 9003

Thing 2 (Former Human)
Hit points = 1337
Reaction time = 29
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var info = DoomInfo.MobjInfos[(int)MobjType.Possessed];
                Assert.AreEqual(1337, info.SpawnHealth);
                Assert.AreEqual(29, info.ReactionTime);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ThingHeaderUsesOneBasedDeHackEdNumbering()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 1 (Player)
Hit points = 321

Thing 2 (Former Human)
Hit points = 654
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(321, DoomInfo.MobjInfos[(int)MobjType.Player].SpawnHealth);
                Assert.AreEqual(654, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
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
            var path = Path.Combine(Path.GetTempPath(), "manageddoom-deh-thing-" + Guid.NewGuid().ToString("N") + ".deh");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
