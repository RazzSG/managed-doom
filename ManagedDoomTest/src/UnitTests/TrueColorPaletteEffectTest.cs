using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom;
using ManagedDoom.Video;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    public class TrueColorPaletteEffectTest
    {
        private Wad wad;
        private Palette palette;
        private TrueColorPaletteEffect effect;

        [TestInitialize]
        public void Initialize()
        {
            wad = new Wad(WadPath.Doom2);

            palette = new Palette(wad);
            palette.ResetColors(1.0);

            effect = new TrueColorPaletteEffect(palette);
            effect.Rebuild();
        }

        [TestCleanup]
        public void Cleanup()
        {
            wad.Dispose();
        }

        [TestMethod]
        public void PaletteZeroCopiesPixelsExactly()
        {
            var source = new uint[]
            {
                Pack(255, 0, 0),
                Pack(10, 120, 230),
                Pack(37, 82, 19),
                Pack(255, 255, 255)
            };

            var destination = new uint[source.Length];

            effect.Write(source, destination, 0);

            CollectionAssert.AreEqual(source, destination);
        }

        [TestMethod]
        public void EffectDoesNotModifySource()
        {
            var source = new uint[]
            {
                Pack(255, 0, 0),
                Pack(100, 80, 60),
                Pack(20, 160, 220),
                Pack(255, 255, 255)
            };

            var original = (uint[])source.Clone();
            var destination = new uint[source.Length];

            effect.Write(source, destination, Palette.DamageStart);

            CollectionAssert.AreEqual(original, source);
        }

        [TestMethod]
        public void DamagePaletteChangesTrueColorPixels()
        {
            var source = CreatePalettePixels();
            var destination = new uint[source.Length];

            effect.Write(source, destination, Palette.DamageStart);

            Assert.IsTrue(HasDifference(source, destination));
        }

        [TestMethod]
        public void BonusPaletteChangesTrueColorPixels()
        {
            var source = CreatePalettePixels();
            var destination = new uint[source.Length];

            effect.Write(source, destination, Palette.BonusStart);

            Assert.IsTrue(HasDifference(source, destination));
        }

        [TestMethod]
        public void IronFeetPaletteChangesTrueColorPixels()
        {
            var source = CreatePalettePixels();
            var destination = new uint[source.Length];

            effect.Write(source, destination, Palette.IronFeet);

            Assert.IsTrue(HasDifference(source, destination));
        }

        [TestMethod]
        public void EffectAlwaysProducesOpaquePixels()
        {
            var source = CreatePalettePixels();
            var destination = new uint[source.Length];

            for (var p = 1; p < palette.Count; p++)
            {
                effect.Write(source, destination, p);

                for (var i = 0; i < destination.Length; i++)
                {
                    var alpha = (byte)(destination[i] >> 24);
                    Assert.AreEqual((byte)255, alpha, $"Palette {p}, pixel {i}");
                }
            }
        }

        [TestMethod]
        public void RebuildUsesUpdatedGammaCorrectedPalette()
        {
            var source = CreatePalettePixels();
            var before = new uint[source.Length];
            var after = new uint[source.Length];

            effect.Write(source, before, Palette.DamageStart);

            palette.ResetColors(0.5);
            effect.Rebuild();

            effect.Write(source, after, Palette.DamageStart);

            Assert.IsTrue(HasDifference(before, after));
        }

        private uint[] CreatePalettePixels()
        {
            var result = new uint[256];
            Array.Copy(palette[0], result, result.Length);
            return result;
        }

        private static bool HasDifference(uint[] a, uint[] b)
        {
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static uint Pack(byte r, byte g, byte b)
        {
            return (uint)(r | (g << 8) | (b << 16) | (255 << 24));
        }
    }
}