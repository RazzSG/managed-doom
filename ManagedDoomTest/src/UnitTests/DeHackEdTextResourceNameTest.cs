using System;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdTextResourceNameTest
    {
        [TestMethod]
        public void ClassicTextUpdatesSpriteSoundAndMusicResourceNamesAndResetRestoresThem()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                TextBlock("TROO", "POSS") +
                TextBlock("pistol", "shotgn") +
                TextBlock("runnin", "stalks"));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("POSS", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
                Assert.AreEqual("shotgn", DoomInfo.SfxNames[(int)Sfx.PISTOL].ToString());
                Assert.AreEqual("stalks", DoomInfo.BgmNames[(int)Bgm.RUNNIN].ToString());

                Reset(wad);

                Assert.AreEqual("TROO", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
                Assert.AreEqual("pistol", DoomInfo.SfxNames[(int)Sfx.PISTOL].ToString());
                Assert.AreEqual("runnin", DoomInfo.BgmNames[(int)Bgm.RUNNIN].ToString());
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextSpriteNameReplacementIsConsumedBySpriteLookup()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(TextBlock("TROO", "POSS"));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var lookup = new SpriteLookup(wad);
                var replaced = lookup[Sprite.TROO];
                var source = lookup[Sprite.POSS];

                Assert.AreEqual("POSS", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
                Assert.IsTrue(replaced.Frames.Length > 0);
                Assert.AreEqual(source.Frames.Length, replaced.Frames.Length);

                for (var i = 0; i < replaced.Frames.Length; i++)
                {
                    Assert.AreEqual(source.Frames[i].Rotate, replaced.Frames[i].Rotate);
                }
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void LaterClassicTextReplacementOverridesEarlierReplacementForSameOriginalResourceName()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreatePatch(
                TextBlock("TROO", "POSS") +
                TextBlock("TROO", "SPOS"));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("SPOS", DoomInfo.SpriteNames[(int)Sprite.TROO].ToString());
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

        private static string TextBlock(string original, string replacement)
        {
            return
                "Text " + original.Length + " " + replacement.Length + "\n" +
                original + replacement + "\n\n";
        }

        private static string CreatePatch(string blocks)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-text-resources-" + Guid.NewGuid().ToString("N") + ".deh");

            var text =
                "Patch File for DeHackEd v3.0\n" +
                "Doom version = 19\n" +
                "Patch format = 6\n\n" +
                blocks;

            File.WriteAllText(path, text);
            return path;
        }
    }
}
