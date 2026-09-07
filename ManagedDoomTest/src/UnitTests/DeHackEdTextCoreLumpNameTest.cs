using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public sealed class DeHackEdTextCoreLumpNameTest
    {
        [TestMethod]
        public void ClassicTextReplacementSelectsPaletteLump()
        {
            using var source = new Wad(WadPath.Doom2);
            var data = (byte[])source.ReadLump("PLAYPAL").Clone();
            data[0] = 17;
            data[1] = 34;
            data[2] = 51;

            var pwad = WriteWad(("PALALT", data));
            var patch = CreateTextPatch("PLAYPAL", "PALALT");

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var palette = new Palette(wad);
                var expected = 17u | (34u << 8) | (51u << 16) | (255u << 24);

                Assert.AreEqual(expected, palette.BaseColors[0]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsColorMapLump()
        {
            using var source = new Wad(WadPath.Doom2);
            var data = (byte[])source.ReadLump("COLORMAP").Clone();
            data[0] = data[0] == 123 ? (byte)124 : (byte)123;

            var pwad = WriteWad(("CMAPALT", data));
            var patch = CreateTextPatch("COLORMAP", "CMAPALT");

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var colorMap = new ColorMap(wad);

                Assert.AreEqual(data[0], colorMap.FullBright[0]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsTextureDefinitionLump()
        {
            using var source = new Wad(WadPath.Doom2);
            var data = (byte[])source.ReadLump("TEXTURE1").Clone();
            var firstOffset = BitConverter.ToInt32(data, 4);
            WriteName(data, firstOffset, "ZZDHTEX1");

            var pwad = WriteWad(("TXTALT1", data));
            var patch = CreateTextPatch("TEXTURE1", "TXTALT1");

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var textures = new DummyTextureLookup(wad);

                Assert.AreNotEqual(-1, textures.GetNumber("ZZDHTEX1"));
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsPnamesLump()
        {
            using var source = new Wad(WadPath.Doom2);
            var data = (byte[])source.ReadLump("PNAMES").Clone();
            WriteName(data, 4, "ZZDH0001");

            var pwad = WriteWad(("PNALT", data));
            var patch = CreateTextPatch("PNAMES", "PNALT");

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var method = typeof(TextureLookup).GetMethod(
                    "LoadPatchNames",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.IsNotNull(method);

                var names = (string[])method.Invoke(null, new object[] { wad });

                Assert.AreEqual("ZZDH0001", names[0]);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsFlatNamespaceMarkers()
        {
            var pwad = WriteWad(
                ("FX_START", Array.Empty<byte>()),
                ("F_SKY1", new byte[4096]),
                ("ZZDHFLAT", new byte[4096]),
                ("FX_END", Array.Empty<byte>()));

            var patch = CreateTextPatch(
                ("F_START", "FX_START"),
                ("F_END", "FX_END"));

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var flats = new FlatLookup(wad);

                Assert.AreEqual(2, flats.Count);
                Assert.AreNotEqual(-1, flats.GetNumber("ZZDHFLAT"));
                Assert.AreEqual(flats.GetNumber("F_SKY1"), flats.SkyFlatNumber);
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
                File.Delete(pwad);
            }
        }

        [TestMethod]
        public void ClassicTextReplacementSelectsPrimarySpriteNamespaceMarkers()
        {
            var pwad = WriteWad(
                ("XX_START", Array.Empty<byte>()),
                ("ZZSPA0", new byte[] { 1 }),
                ("XX_END", Array.Empty<byte>()));

            var patch = CreateTextPatch(
                ("S_START", "XX_START"),
                ("S_END", "XX_END"));

            try
            {
                using var wad = new Wad(WadPath.Doom2, pwad);
                Reset(wad);
                DeHackEd.Initialize(ArgsForPatch(patch), wad);

                var method = typeof(SpriteLookup).GetMethod(
                    "EnumerateSprites",
                    BindingFlags.NonPublic | BindingFlags.Static);

                Assert.IsNotNull(method);

                var result = (IEnumerable<int>)method.Invoke(null, new object[] { wad });
                var customLump = wad.GetLumpNumber("ZZSPA0");

                Assert.AreNotEqual(-1, customLump);
                Assert.IsTrue(result.Contains(customLump));
            }
            finally
            {
                ResetGlobalState();
                File.Delete(patch);
                File.Delete(pwad);
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
                "manageddoom-deh-text-core-lumps-" + Guid.NewGuid().ToString("N") + ".deh");

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

        private static void WriteName(byte[] data, int offset, string name)
        {
            Array.Clear(data, offset, 8);
            var bytes = Encoding.ASCII.GetBytes(name);
            Buffer.BlockCopy(bytes, 0, data, offset, Math.Min(8, bytes.Length));
        }

        private static string WriteWad(params (string Name, byte[] Data)[] lumps)
        {
            var path = Path.Combine(
                Path.GetTempPath(),
                "manageddoom-deh-text-core-lumps-" + Guid.NewGuid().ToString("N") + ".wad");

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
