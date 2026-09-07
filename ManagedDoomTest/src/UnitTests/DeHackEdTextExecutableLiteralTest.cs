using System;
using System.IO;
using System.Reflection;
using System.Text;
using ManagedDoom;
using ManagedDoom.Video;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdTextExecutableLiteralTest
    {
        [TestMethod]
        public void ClassicTextReplacementSelectsOpeningDemoLumpAtRuntime()
        {
            using var source = new Wad(WadPath.Doom2);
            var demoData = (byte[])source.ReadLump("DEMO1").Clone();
            Assert.IsTrue(demoData.Length >= 4);

            const int replacementMap = 31;
            demoData[3] = replacementMap;

            var pwad = WriteWad(("DALT1", demoData));
            var patch = CreateTextPatch("demo1", "DALT1");

            try
            {
                var args = ArgsForRuntimePatch(patch, pwad);

                using var content = new GameContent(args);
                var options = new GameOptions(args, content);
                var sequence = new OpeningSequence(content, options);

                var startDemo = typeof(OpeningSequence).GetMethod(
                    "StartDemo",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.IsNotNull(startDemo);
                startDemo.Invoke(sequence, new object[] { "demo1" });

                Assert.IsNotNull(sequence.DemoGame);
                Assert.AreEqual(replacementMap, sequence.DemoGame.Options.Map);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsCommercialBorderFlatAtRendererInitialization()
        {
            var patch = CreateTextPatch("GRNROCK", "SLIME16");

            try
            {
                var args = ArgsForRuntimePatch(patch);

                using var content = new GameContent(args);
                var screen = new DrawScreen(content.Wad, content.Palette, 320, 200);
                var renderer = new ThreeDRenderer(content, screen, ThreeDRenderer.MaxScreenSize);

                var field = typeof(ThreeDRenderer).GetField(
                    "backFlat",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.IsNotNull(field);

                var flat = (Flat)field.GetValue(renderer);
                Assert.IsNotNull(flat);
                Assert.AreEqual("SLIME16", flat.Name);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsHudFontLumpAtScreenInitialization()
        {
            var patch = CreateTextPatch("STCFN033", "STCFN034");

            try
            {
                var args = ArgsForRuntimePatch(patch);

                using var content = new GameContent(args);
                var screen = new DrawScreen(content.Wad, content.Palette, 320, 200);

                var field = typeof(DrawScreen).GetField(
                    "chars",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.IsNotNull(field);

                var chars = (Patch[])field.GetValue(screen);
                Assert.IsNotNull(chars[33]);
                Assert.AreEqual("STCFN034", chars[33].Name);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
            }
        }

        [TestMethod]
        public void ResetRestoresExecutableLiteralResolution()
        {
            using var wad = new Wad(WadPath.Doom2);
            Reset(wad);

            var patch = CreateTextPatch(
                ("demo1", "DALT1"),
                ("GRNROCK", "SLIME16"),
                ("STCFN033", "STCFN034"));

            try
            {
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                Assert.AreEqual("DALT1", DoomString.Resolve("demo1"));
                Assert.AreEqual("SLIME16", DoomString.Resolve("GRNROCK"));
                Assert.AreEqual("STCFN034", DoomString.Resolve("STCFN033"));

                Reset(wad);

                Assert.AreEqual("demo1", DoomString.Resolve("demo1"));
                Assert.AreEqual("GRNROCK", DoomString.Resolve("GRNROCK"));
                Assert.AreEqual("STCFN033", DoomString.Resolve("STCFN033"));
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

        private static CommandLineArgs ArgsForRuntimePatch(string patchPath, string pwad = null)
        {
            if (pwad == null)
            {
                return new CommandLineArgs(new[]
                {
                    "-iwad", WadPath.Doom2,
                    "-deh", patchPath,
                    "-nodeh"
                });
            }

            return new CommandLineArgs(new[]
            {
                "-iwad", WadPath.Doom2,
                "-file", pwad,
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
                "manageddoom-deh-text-executable-" + Guid.NewGuid().ToString("N") + ".deh");

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

        private static string WriteWad(params (string Name, byte[] Data)[] lumps)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-text-executable-" + Guid.NewGuid().ToString("N") + ".wad");

            var positions = new int[lumps.Length];

            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true);

            writer.Write(Encoding.ASCII.GetBytes("PWAD"));
            writer.Write(lumps.Length);
            writer.Write(0);

            for (var i = 0; i < lumps.Length; i++)
            {
                positions[i] = checked((int)stream.Position);
                writer.Write(lumps[i].Data);
            }

            var directoryOffset = checked((int)stream.Position);

            for (var i = 0; i < lumps.Length; i++)
            {
                writer.Write(positions[i]);
                writer.Write(lumps[i].Data.Length);

                var name = new byte[8];
                var encoded = Encoding.ASCII.GetBytes(lumps[i].Name);
                Buffer.BlockCopy(encoded, 0, name, 0, Math.Min(encoded.Length, name.Length));
                writer.Write(name);
            }

            stream.Position = 8;
            writer.Write(directoryOffset);
            writer.Flush();

            return path;
        }
    }
}
