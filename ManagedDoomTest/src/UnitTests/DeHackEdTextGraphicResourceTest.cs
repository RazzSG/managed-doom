using System;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdTextGraphicResourceTest
    {
        [TestMethod]
        public void PatchFromWadUsesClassicTextReplacementForGraphicLumpName()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreateTextPatch("WIMINUS", "WIPCNT");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var graphic = Patch.FromWad(wad, "WIMINUS");

                Assert.AreEqual("WIPCNT", graphic.Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void PatchCacheUsesClassicTextReplacementAndCachesRequestedGraphicName()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreateTextPatch("TITLEPIC", "CREDIT");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var cache = new PatchCache(wad);
                var first = cache["TITLEPIC"];
                var second = cache["TITLEPIC"];

                Assert.AreEqual("CREDIT", first.Name);
                Assert.AreSame(first, second);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void PatchCacheContainsChecksResolvedGraphicLumpName()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            const string original = "ZZDHC2A1";
            const string replacement = "M_LOADG";

            Assert.AreEqual(-1, wad.GetLumpNumber(original));
            Assert.AreNotEqual(-1, wad.GetLumpNumber(replacement));

            var patch = CreateTextPatch(original, replacement);

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var cache = new PatchCache(wad);

                Assert.IsTrue(cache.Contains(original));
                Assert.AreEqual(replacement, cache[original].Name);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ResetRestoresOriginalGraphicLumpLookup()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreateTextPatch("M_DOOM", "M_LOADG");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual("M_LOADG", Patch.FromWad(wad, "M_DOOM").Name);

                Reset(wad);

                Assert.AreEqual("M_DOOM", Patch.FromWad(wad, "M_DOOM").Name);
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

        private static string CreateTextPatch(string original, string replacement)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-text-graphics-" + Guid.NewGuid().ToString("N") + ".deh");

            using (var writer = new StreamWriter(path))
            {
                writer.NewLine = "\n";
                writer.WriteLine("Patch File for DeHackEd v3.0");
                writer.WriteLine("Doom version = 19");
                writer.WriteLine("Patch format = 6");
                writer.WriteLine();
                writer.WriteLine("Text " + original.Length + " " + replacement.Length);
                writer.Write(original);
                writer.WriteLine(replacement);
                writer.WriteLine();
            }

            return path;
        }
    }
}
