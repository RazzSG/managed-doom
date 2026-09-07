using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdTextHardcodedResourceTest
    {
        [TestMethod]
        public void ClassicTextStoresReplacementForUnregisteredHardcodedString()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreateTextPatch("F_SKY1", "SLIME16");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("SLIME16", DoomString.Resolve("F_SKY1"));
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsSkyFlatDuringContentInitialization()
        {
            var patch = CreateTextPatch("F_SKY1", "SLIME16");

            try
            {
                var args = ArgsForRuntimePatch(patch);

                using (var content = new GameContent(args))
                {
                    var expected = content.Flats.GetNumber("SLIME16");

                    Assert.AreNotEqual(-1, expected);
                    Assert.AreEqual(expected, content.Flats.SkyFlatNumber);
                    Assert.AreSame(content.Flats["SLIME16"], content.Flats.SkyFlat);
                }
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsSkyTextureAtRuntime()
        {
            var patch = CreateTextPatch("SKY1", "SKY2");

            try
            {
                var args = ArgsForRuntimePatch(patch);

                using (var content = new GameContent(args))
                {
                    var options = new GameOptions(args, content)
                    {
                        Map = 1
                    };
                    var world = new World(content, options, null);

                    Assert.AreEqual("SKY2", world.Map.SkyTexture.Name);
                }
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsFinaleFlatAtRuntime()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreateTextPatch("SLIME16", "RROCK14");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var options = new GameOptions
                {
                    GameMode = GameMode.Commercial,
                    MissionPack = MissionPack.Doom2,
                    Map = 6
                };
                var finale = new Finale(options);

                Assert.AreEqual("RROCK14", finale.Flat);
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementIsUsedByVanillaAnimationDefinitions()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreateTextPatch(
                ("ANIMSTRT", "NEWSTART"),
                ("ANIMEND0", "NEWEND00"));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var flats = new RecordingFlatLookup(new Dictionary<string, int>
                {
                    ["NEWSTART"] = 10,
                    ["NEWEND00"] = 12
                });

                var animation = new TextureAnimation(
                    null,
                    flats,
                    new[] { new AnimationDef(false, "ANIMEND0", "ANIMSTRT", 8) });

                Assert.AreEqual(1, animation.Animations.Length);
                Assert.AreEqual(10, animation.Animations[0].BasePic);
                Assert.AreEqual(12, animation.Animations[0].PicNum);
                Assert.AreEqual(3, animation.Animations[0].NumPics);
                CollectionAssert.Contains(flats.Queries, "NEWSTART");
                CollectionAssert.Contains(flats.Queries, "NEWEND00");
                Assert.IsFalse(flats.Queries.Contains("ANIMSTRT"));
                Assert.IsFalse(flats.Queries.Contains("ANIMEND0"));
            }
            finally
            {
                Reset(wad);
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ResetRemovesHardcodedStringReplacement()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreateTextPatch("F_SKY1", "SLIME16");

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);
                Assert.AreEqual("SLIME16", DoomString.Resolve("F_SKY1"));

                Reset(wad);

                Assert.AreEqual("F_SKY1", DoomString.Resolve("F_SKY1"));
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

        private static CommandLineArgs ArgsForRuntimePatch(string patchPath)
        {
            return new CommandLineArgs(new[]
            {
                "-iwad", WadPath.Doom2,
                "-deh", patchPath,
                "-nodeh"
            });
        }

        private static void Reset(Wad wad)
        {
            DeHackEd.Initialize(new CommandLineArgs(new[] { "-nodeh" }), wad);
        }

        private static void ResetGlobalState()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);
        }

        private static string CreateTextPatch(string original, string replacement)
        {
            return CreateTextPatch((original, replacement));
        }

        private static string CreateTextPatch(params (string Original, string Replacement)[] replacements)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-text-hardcoded-" + Guid.NewGuid().ToString("N") + ".deh");

            using (var writer = new StreamWriter(path))
            {
                writer.NewLine = "\n";
                writer.WriteLine("Patch File for DeHackEd v3.0");
                writer.WriteLine("Doom version = 19");
                writer.WriteLine("Patch format = 6");
                writer.WriteLine();

                foreach (var item in replacements)
                {
                    writer.WriteLine("Text " + item.Original.Length + " " + item.Replacement.Length);
                    writer.Write(item.Original);
                    writer.WriteLine(item.Replacement);
                    writer.WriteLine();
                }
            }

            return path;
        }

        private sealed class RecordingFlatLookup : IFlatLookup
        {
            private readonly Dictionary<string, int> numbers;

            public RecordingFlatLookup(Dictionary<string, int> numbers)
            {
                this.numbers = numbers;
                Queries = new List<string>();
            }

            public List<string> Queries { get; }

            public int GetNumber(string name)
            {
                Queries.Add(name);
                return numbers.TryGetValue(name, out var number) ? number : -1;
            }

            public int Count => 0;

            public Flat this[int index] => throw new IndexOutOfRangeException();

            public Flat this[string name] => throw new KeyNotFoundException();

            public int SkyFlatNumber => -1;

            public Flat SkyFlat => null;

            public IEnumerator<Flat> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
