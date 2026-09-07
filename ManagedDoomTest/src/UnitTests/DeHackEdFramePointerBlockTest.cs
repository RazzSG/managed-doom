using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdFramePointerBlockTest
    {
        [TestMethod]
        public void ClassicFrameBlockMapsAllStandardFields()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Frame 174
Sprite number = 2
Sprite subnumber = 32771
Duration = 7
Next frame = 176
Unknown 1 = 12345
Unknown 2 = -6789
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var state = DoomInfo.States[174];
                Assert.AreEqual(2, (int)state.Sprite);
                Assert.AreEqual(32771, state.Frame);
                Assert.AreEqual(7, state.Tics);
                Assert.AreEqual(176, (int)state.Next);
                Assert.AreEqual(12345, state.Misc1);
                Assert.AreEqual(-6789, state.Misc2);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void InvalidFrameNumbersAreIgnoredAndLaterFrameBlocksStillApply()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Frame -1
Duration = 91

Frame 9999
Duration = 92

Frame 174
Duration = 13
Unknown 1 = 77
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(13, DoomInfo.States[174].Tics);
                Assert.AreEqual(77, DoomInfo.States[174].Misc1);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicPointerCopiesActionFromOriginalSourceTable()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Pointer 1 (Frame 176)
Codep Frame = 174

Pointer 2 (Frame 174)
Codep Frame = 176
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                // Frame 176 originally uses Chase; frame 174 originally uses Look.
                // The second assignment must still copy the original Chase pointer,
                // not the Look pointer written into frame 176 by the first block.
                Assert.AreEqual("Look", DoomInfo.States[176].MobjAction.Method.Name);
                Assert.AreEqual("Chase", DoomInfo.States[174].MobjAction.Method.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicPointerCanCopyAnOriginalNullAction()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Pointer 1 (Frame 174)
Codep Frame = 186
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
        public void InvalidPointerTargetsAndSourcesAreIgnoredAndParsingContinues()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Pointer 1 (Frame -1)
Codep Frame = 176

Pointer 2 (Frame 174)
Codep Frame = 9999

Pointer nope (Frame 174)
Codep Frame = 175

Pointer 3 (Frame 174)
Codep Frame = 176
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("Chase", DoomInfo.States[174].MobjAction.Method.Name);
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
            var path = Path.Combine(Path.GetTempPath(), "manageddoom-deh-frame-pointer-" + Guid.NewGuid().ToString("N") + ".deh");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
