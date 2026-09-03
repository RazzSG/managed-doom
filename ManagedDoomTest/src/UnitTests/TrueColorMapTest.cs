using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    public class TrueColorMapTest
    {
        private Wad wad;
        private Palette palette;
        private ColorMap colorMap;
        private TrueColorMap trueColorMap;

        [TestInitialize]
        public void Initialize()
        {
            wad = new Wad(WadPath.Doom2);

            palette = new Palette(wad);
            palette.ResetColors(1.0);

            colorMap = new ColorMap(wad);

            trueColorMap = new TrueColorMap(palette, colorMap);
            trueColorMap.Rebuild();
        }

        [TestCleanup]
        public void Cleanup()
        {
            wad.Dispose();
        }

        [TestMethod]
        public void FullBrightMatchesBasePalette()
        {
            for (var i = 0; i < 256; i++)
            {
                Assert.AreEqual(palette[0][i], trueColorMap[0][i], $"Palette index {i}");
            }
        }

        [TestMethod]
        public void DarkestNormalMapIsDarkerThanFullBright()
        {
            var fullBright = GetTotalBrightness(trueColorMap[0]);
            var dark = GetTotalBrightness(trueColorMap[TrueColorMap.NormalMapCount - 1]);

            Assert.IsTrue(dark < fullBright);
        }

        [TestMethod]
        public void NormalLightingPreservesHue()
        {
            const int map = 8;

            var index = FindMixedColor();
            var source = palette[0][index];
            var result = trueColorMap[map][index];

            var sr = (int)(source & 0xFF);
            var sg = (int)((source >> 8) & 0xFF);
            var sb = (int)((source >> 16) & 0xFF);

            var rr = (int)(result & 0xFF);
            var rg = (int)((result >> 8) & 0xFF);
            var rb = (int)((result >> 16) & 0xFF);

            var scale = rr / (double)sr;

            Assert.AreEqual(sg * scale, rg, 2.0);
            Assert.AreEqual(sb * scale, rb, 2.0);
        }

        [TestMethod]
        public void NormalLightingProducesTrueColorValues()
        {
            var basePalette = new HashSet<uint>(palette[0]);
            var foundTrueColor = false;

            for (var map = 1; map < TrueColorMap.NormalMapCount && !foundTrueColor; map++)
            {
                for (var i = 0; i < 256; i++)
                {
                    if (!basePalette.Contains(trueColorMap[map][i]))
                    {
                        foundTrueColor = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(foundTrueColor);
        }

        [TestMethod]
        public void InverseMapUsesExactColorMapMapping()
        {
            var map = ColorMap.Inverse;

            for (var i = 0; i < 256; i++)
            {
                var expected = palette[0][colorMap[map][i]];
                Assert.AreEqual(expected, trueColorMap[map][i], $"Palette index {i}");
            }
        }

        [TestMethod]
        public void ApplyLightMatchesGeneratedLightMap()
        {
            const int map = 6;

            for (var i = 0; i < 256; i++)
            {
                var expected = trueColorMap[map][i];
                var actual = trueColorMap.ApplyLight(palette[0][i], map);

                Assert.AreEqual(expected, actual, $"Palette index {i}");
            }
        }

        [TestMethod]
        public void RebuildUsesUpdatedGammaCorrectedPalette()
        {
            var before = (uint[])trueColorMap[0].Clone();

            palette.ResetColors(0.5);
            trueColorMap.Rebuild();

            var changed = false;

            for (var i = 0; i < 256; i++)
            {
                Assert.AreEqual(palette[0][i], trueColorMap[0][i], $"Palette index {i}");

                if (before[i] != trueColorMap[0][i])
                {
                    changed = true;
                }
            }

            Assert.IsTrue(changed);
        }

        private int FindMixedColor()
        {
            for (var i = 0; i < 256; i++)
            {
                var color = palette[0][i];

                var r = (int)(color & 0xFF);
                var g = (int)((color >> 8) & 0xFF);
                var b = (int)((color >> 16) & 0xFF);

                if (r >= 32 && g >= 32 && b >= 32 && (r != g || g != b))
                {
                    return i;
                }
            }

            Assert.Fail("Could not find a suitable mixed palette color.");
            return 0;
        }

        private static long GetTotalBrightness(uint[] colors)
        {
            long result = 0;

            foreach (var color in colors)
            {
                result += color & 0xFF;
                result += (color >> 8) & 0xFF;
                result += (color >> 16) & 0xFF;
            }

            return result;
        }
    }
}