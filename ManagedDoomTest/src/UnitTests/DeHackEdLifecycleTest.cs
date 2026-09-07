using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdLifecycleTest
    {
        [TestMethod]
        public void SequentialInitializationsRestorePristineDefinitionsBeforeApplyingNextPatch()
        {
            var patchA = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2 (Former Human)
Hit points = 1234

Frame 10
Duration = 9

Ammo 0
Max ammo = 321
Per ammo = 17

Misc 0
Initial Health = 77

[STRINGS]
PRESSKEY = patched prompt

[PARS]
par 1 999

[CODEPTR]
FRAME 174 = Chase
");

            var patchB = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2 (Former Human)
Hit points = 2222
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patchA), wad);

                Assert.AreEqual(1234, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                Assert.AreEqual(9, DoomInfo.States[10].Tics);
                Assert.AreEqual(321, DoomInfo.AmmoInfos.Max[0]);
                Assert.AreEqual(17, DoomInfo.AmmoInfos.Clip[0]);
                Assert.AreEqual(77, DoomInfo.DeHackEdConst.InitialHealth);
                Assert.AreEqual("patched prompt", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(999, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual("Chase", DoomInfo.States[174].MobjAction.Method.Name);

                DeHackEd.Initialize(ArgsForPatch(patchB), wad);

                Assert.AreEqual(2222, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                Assert.AreEqual(1, DoomInfo.States[10].Tics);
                Assert.AreEqual(200, DoomInfo.AmmoInfos.Max[0]);
                Assert.AreEqual(10, DoomInfo.AmmoInfos.Clip[0]);
                Assert.AreEqual(100, DoomInfo.DeHackEdConst.InitialHealth);
                Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString());
                Assert.AreEqual(30, DoomInfo.ParTimes.Doom2[0]);
                Assert.AreEqual("Look", DoomInfo.States[174].MobjAction.Method.Name);

                DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);

                Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
            }
            finally
            {
                // Keep the process-global definition tables pristine even when an
                // assertion fails, because many existing tests share DoomInfo.
                DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
                File.Delete(patchA);
                File.Delete(patchB);
            }
        }

        [TestMethod]
        public void FailedPatchRollsBackPartialDefinitionChanges()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2 (Former Human)
Hit points = 1234

Text 12 20
press a key.too short
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                Assert.ThrowsExactly<Exception>(() => DeHackEd.Initialize(ArgsForPatch(patch), wad));

                Assert.AreEqual(20, DoomInfo.MobjInfos[(int)MobjType.Possessed].SpawnHealth);
                Assert.AreEqual(100, DoomInfo.DeHackEdConst.InitialHealth);
                Assert.AreEqual("press a key.", DoomInfo.Strings.PRESSKEY.ToString());
            }
            finally
            {
                DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void MultipleExternalPatchesStillApplyInCommandLineOrderWithinOneLoad()
        {
            var patchA = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2 (Former Human)
Hit points = 1111
Reaction time = 23
");

            var patchB = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Thing 2 (Former Human)
Hit points = 2222
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                var args = new CommandLineArgs(new[] { "-deh", patchA, patchB, "-nodeh" });
                DeHackEd.Initialize(args, wad);

                var info = DoomInfo.MobjInfos[(int)MobjType.Possessed];
                Assert.AreEqual(2222, info.SpawnHealth);
                Assert.AreEqual(23, info.ReactionTime);
            }
            finally
            {
                DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
                File.Delete(patchA);
                File.Delete(patchB);
            }
        }

        private static CommandLineArgs ArgsForPatch(string patchPath)
        {
            return new CommandLineArgs(new[] { "-deh", patchPath, "-nodeh" });
        }

        private static string CreatePatch(string text)
        {
            var path = Path.Combine(Path.GetTempPath(), "manageddoom-deh-" + Guid.NewGuid().ToString("N") + ".deh");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
