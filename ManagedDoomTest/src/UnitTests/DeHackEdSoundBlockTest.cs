using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class DeHackEdSoundBlockTest
    {
        [TestMethod]
        public void SoundMetadataTableMatchesCoreSfxTableWithoutStaticInitializationCycle()
        {
            Assert.AreEqual(Enum.GetValues<Sfx>().Length, DoomInfo.SfxNames.Length);
            Assert.AreEqual(DoomInfo.SfxNames.Length, DoomInfo.DeHackEdSoundInfos.Length);
        }

        [TestMethod]
        public void ClassicSoundBlockMapsSafeFieldsAndRetainsLegacyMetadata()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Sound 1
Offset = 123456
Zero/One = 7
Value = 42
Zero 1 = 305419896
Zero 2 = 144
Zero 3 = -6
Zero 4 = 987654
Neg. One 1 = 3
Neg. One 2 = 88
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var info = DoomInfo.DeHackEdSoundInfos[1];
                Assert.AreEqual(7, info.Singularity.Value);
                Assert.AreEqual(42, info.Priority.Value);
                Assert.AreEqual(305419896, info.LegacyLink.Value);
                Assert.AreEqual(144, info.Pitch.Value);
                Assert.AreEqual(-6, info.Volume.Value);
                Assert.AreEqual(3, info.Usefulness.Value);
                Assert.AreEqual(88, info.LumpNum.Value);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void SoundBlockUsesZeroBasedDeHackEdNumbering()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Sound 0
Value = 11

Sound 1
Value = 22
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(11, DoomInfo.DeHackEdSoundInfos[0].Priority.Value);
                Assert.AreEqual(22, DoomInfo.DeHackEdSoundInfos[1].Priority.Value);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void InvalidSoundNumbersAreIgnoredAndLaterSoundBlocksStillApply()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Sound -1
Value = 91

Sound 9999
Value = 92

Sound 1
Value = 37
Zero 2 = 151
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(37, DoomInfo.DeHackEdSoundInfos[1].Priority.Value);
                Assert.AreEqual(151, DoomInfo.DeHackEdSoundInfos[1].Pitch.Value);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void SequentialInitializationRestoresSoundPatchMetadata()
        {
            var patch = CreatePatch(@"
Patch File for DeHackEd v3.0
Doom version = 19
Patch format = 6

Sound 1
Zero/One = 5
Value = 19
Zero 3 = 4
");

            using var wad = new Wad(WadPath.Doom2);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual(5, DoomInfo.DeHackEdSoundInfos[1].Singularity.Value);
                Assert.AreEqual(19, DoomInfo.DeHackEdSoundInfos[1].Priority.Value);
                Assert.AreEqual(4, DoomInfo.DeHackEdSoundInfos[1].Volume.Value);

                Reset(wad);

                Assert.IsNull(DoomInfo.DeHackEdSoundInfos[1].Singularity);
                Assert.IsNull(DoomInfo.DeHackEdSoundInfos[1].Priority);
                Assert.IsNull(DoomInfo.DeHackEdSoundInfos[1].LegacyLink);
                Assert.IsNull(DoomInfo.DeHackEdSoundInfos[1].Pitch);
                Assert.IsNull(DoomInfo.DeHackEdSoundInfos[1].Volume);
                Assert.IsNull(DoomInfo.DeHackEdSoundInfos[1].Usefulness);
                Assert.IsNull(DoomInfo.DeHackEdSoundInfos[1].LumpNum);
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
            var path = Path.Combine(Path.GetTempPath(), "manageddoom-deh-sound-" + Guid.NewGuid().ToString("N") + ".deh");
            File.WriteAllText(path, text.Replace("\r\n", "\n").TrimStart());
            return path;
        }
    }
}
