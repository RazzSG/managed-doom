using System;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdSpriteBlockTest
    {
        [TestMethod]
        public void ClassicSpriteOffsetCopiesOriginalSpriteNameForDoom19()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                19,
                SpriteBlock((int)Sprite.TROO, SpriteOffset(19, Sprite.POSS)));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("POSS", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
                Assert.AreEqual("POSS", DoomInfo.SpriteNames[(int)Sprite.POSS].ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicSpriteReplacementIsConsumedByRealAndDummySpriteLookups()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                19,
                SpriteBlock((int)Sprite.TROO, SpriteOffset(19, Sprite.POSS)));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var realLookup = new SpriteLookup(wad);
                var realReplaced = realLookup[Sprite.TROO];
                var realSource = realLookup[Sprite.POSS];

                Assert.IsTrue(realReplaced.Frames.Length > 0);
                Assert.AreEqual(realSource.Frames.Length, realReplaced.Frames.Length);
                for (var i = 0; i < realReplaced.Frames.Length; i++)
                {
                    Assert.AreEqual(realSource.Frames[i].Rotate, realReplaced.Frames[i].Rotate);
                }

                var dummyLookup = new DummySpriteLookup(wad);
                Assert.AreEqual(
                    dummyLookup[Sprite.POSS].Frames.Length,
                    dummyLookup[Sprite.TROO].Frames.Length);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicSpriteOffsetUsesDoomVersionSpecificExecutableBase()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                21,
                SpriteBlock((int)Sprite.TROO, SpriteOffset(21, Sprite.POSS)));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("POSS", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void InvalidSpriteNumbersAreIgnoredAndLaterSpriteBlocksStillApply()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                19,
                SpriteBlock((int)Sprite.TROO, SpriteOffset(19, Sprite.POSS)) +
                SpriteBlock(-1, SpriteOffset(19, Sprite.SPOS)) +
                SpriteBlock((int)Sprite.Count, SpriteOffset(19, Sprite.SPOS)) +
                SpriteBlock((int)Sprite.PLAY, int.MaxValue) +
                SpriteBlock((int)Sprite.PLAY, SpriteOffset(19, Sprite.SPOS)));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("POSS", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
                Assert.AreEqual("SPOS", DoomInfo.SpriteNames[(int)Sprite.PLAY].ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void SpriteOffsetReadsFromPristineSourceNameTable()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                19,
                SpriteBlock((int)Sprite.POSS, SpriteOffset(19, Sprite.SPOS)) +
                SpriteBlock((int)Sprite.TROO, SpriteOffset(19, Sprite.POSS)));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("SPOS", DoomInfo.SpriteNames[(int)Sprite.POSS].ToString());
                Assert.AreEqual(
                    "POSS",
                    DoomInfo.SpriteNames[(int)Sprite.TROO].ToString(),
                    "The second Offset must read the original executable sprite table, not the already patched source slot.");
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void SpriteReplacementIsResetBetweenContentLoads()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                19,
                SpriteBlock((int)Sprite.TROO, SpriteOffset(19, Sprite.POSS)));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual("POSS", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());

                Reset(wad);
                Assert.AreEqual("TROO", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
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

        private static string SpriteBlock(int targetSprite, int offset)
        {
            return
                "Sprite " + targetSprite + "\n" +
                "Offset = " + offset + "\n\n";
        }

        private static int SpriteOffset(int doomVersion, Sprite sourceSprite)
        {
            int textOffset;
            switch (doomVersion)
            {
                case 16:
                case 17:
                case 20:
                    textOffset = 129044;
                    break;
                case 19:
                    textOffset = 129284;
                    break;
                case 21:
                    textOffset = 129380;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(doomVersion));
            }

            return textOffset + 22044 + (int)sourceSprite * 8;
        }

        private static string CreatePatch(int doomVersion, string blocks)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-sprite-" + Guid.NewGuid().ToString("N") + ".deh");

            var text =
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = " + doomVersion + "\n" +
                "Patch format = 6\n\n" +
                blocks;

            File.WriteAllText(path, text);
            return path;
        }
    }
}
