using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdBexCodePtrBlockTest
    {
        [TestMethod]
        public void KnownMobjMnemonicReplacesTheSingleStateCodePointer()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 1 = Chase
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var state = DoomInfo.States[1];
                Assert.IsNull(state.PlayerAction);
                Assert.IsNotNull(state.MobjAction);
                Assert.AreEqual("Chase", state.MobjAction.Method.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void KnownPlayerMnemonicClearsThePreviousMobjDelegate()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 174 = Light0
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var state = DoomInfo.States[174];
                Assert.IsNull(state.MobjAction);
                Assert.IsNotNull(state.PlayerAction);
                Assert.AreEqual("Light0", state.PlayerAction.Method.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void CodePtrAcceptsEqualsAttachedToMnemonic()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 174 =Chase
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.IsNotNull(DoomInfo.States[174].MobjAction);
                Assert.AreEqual("Chase", DoomInfo.States[174].MobjAction.Method.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void LaterValidAssignmentWinsAndClearsTheEarlierActionFamily()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 174 = Light0
FRAME 174 = cHaSe
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var state = DoomInfo.States[174];
                Assert.IsNull(state.PlayerAction);
                Assert.IsNotNull(state.MobjAction);
                Assert.AreEqual("Chase", state.MobjAction.Method.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void InvalidOrUnknownAssignmentsAreNonDestructiveAndParsingContinues()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME -1 = Chase
FRAME 9999 = Chase
FRAME nope = Chase
FRAME 174 = DefinitelyNotARealPointer
FRAME 174 = Chase
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.IsNull(DoomInfo.States[174].PlayerAction);
                Assert.IsNotNull(DoomInfo.States[174].MobjAction);
                Assert.AreEqual("Chase", DoomInfo.States[174].MobjAction.Method.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ResetRestoresOriginalCodePointerPair()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 174 = Light0
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.IsNull(DoomInfo.States[174].MobjAction);
                Assert.AreEqual("Light0", DoomInfo.States[174].PlayerAction.Method.Name);

                Reset(wad);

                Assert.IsNull(DoomInfo.States[174].PlayerAction);
                Assert.IsNotNull(DoomInfo.States[174].MobjAction);
                Assert.AreEqual("Look", DoomInfo.States[174].MobjAction.Method.Name);
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
            var path = Path.Combine(Path.GetTempPath(), "manageddoom-bex-codeptr-" + Guid.NewGuid().ToString("N") + ".bex");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
