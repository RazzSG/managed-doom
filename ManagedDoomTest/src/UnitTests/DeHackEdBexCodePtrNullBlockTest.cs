using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdBexCodePtrNullBlockTest
    {
        [TestMethod]
        public void NullMnemonicClearsAnExistingCodePointer()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 174 = NULL
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.IsNull(DoomInfo.States[174].PlayerAction);
                Assert.IsNull(DoomInfo.States[174].MobjAction);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void NullMnemonicIsCaseInsensitive()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 174 = nUlL
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.IsNull(DoomInfo.States[174].PlayerAction);
                Assert.IsNull(DoomInfo.States[174].MobjAction);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ArbitraryValidFrameCanReceiveCodePointer()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 0 = Look
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var state = DoomInfo.States[0];
                Assert.IsNull(state.PlayerAction);
                Assert.IsNotNull(state.MobjAction);
                Assert.AreEqual("Look", state.MobjAction.Method.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void NullThenLaterValidAssignmentUsesTheLaterPointer()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 0 = Look
FRAME 0 = NULL
FRAME 0 = Chase
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var state = DoomInfo.States[0];
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
        public void ResetRestoresArbitraryFrameAfterCodePointerAssignment()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

[CODEPTR]
FRAME 0 = Look
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.IsNotNull(DoomInfo.States[0].MobjAction);

                Reset(wad);

                Assert.IsNull(DoomInfo.States[0].PlayerAction);
                Assert.IsNull(DoomInfo.States[0].MobjAction);
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
            var path = Path.Combine(Path.GetTempPath(), "manageddoom-bex-codeptr-null-" + Guid.NewGuid().ToString("N") + ".bex");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
