using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ManagedDoom;
using ManagedDoom.Video;

namespace ManagedDoomTest.UnitTests
{
    [TestClass]
    public class TrueColorRendererIntegrationTest
    {
        private const byte IndexedSentinel = 0xA5;
        private const uint TrueColorSentinel = 0x11223344;

        private GameContent content;
        private DoomGame game;
        private DrawScreen screen;
        private ThreeDRenderer renderer;

        [TestInitialize]
        public void Initialize()
        {
            content = GameContent.CreateDummy(WadPath.Doom2);

            content.Palette.ResetColors(1.0);
            content.TrueColorMap.Rebuild();

            var options = new GameOptions
            {
                GameMode = GameMode.Commercial,
                Map = 1
            };

            game = new DoomGame(content, options);
            game.DeferedInitNew();

            var commands = new TicCmd[Player.MaxPlayerCount];

            for (var i = 0; i < commands.Length; i++)
            {
                commands[i] = new TicCmd();
            }

            game.Update(commands);

            screen = new DrawScreen(content.Wad, content.Palette, 640, 400);
            renderer = new ThreeDRenderer(content, screen, ThreeDRenderer.MaxScreenSize);
        }

        [TestMethod]
        public void TrueColorRenderingWritesOnlyTrueColorFramebuffer()
        {
            Array.Fill(screen.Data, IndexedSentinel);
            Array.Fill(screen.TrueColorData, TrueColorSentinel);

            screen.ColorMode = ColorMode.TrueColor;

            renderer.Render(game.World.DisplayPlayer, Fixed.One);

            Assert.IsTrue(
                Array.TrueForAll(screen.Data, value => value == IndexedSentinel),
                "True Color rendering wrote into the indexed framebuffer.");

            Assert.IsTrue(
                Array.Exists(screen.TrueColorData, value => value != TrueColorSentinel),
                "True Color rendering did not write any pixels.");
        }

        [TestMethod]
        public void IndexedRenderingWritesOnlyIndexedFramebuffer()
        {
            Array.Fill(screen.Data, IndexedSentinel);
            Array.Fill(screen.TrueColorData, TrueColorSentinel);

            screen.ColorMode = ColorMode.Indexed;

            renderer.Render(game.World.DisplayPlayer, Fixed.One);

            Assert.IsTrue(
                Array.Exists(screen.Data, value => value != IndexedSentinel),
                "Indexed rendering did not write any pixels.");

            Assert.IsTrue(
                Array.TrueForAll(screen.TrueColorData, value => value == TrueColorSentinel),
                "Indexed rendering wrote into the True Color framebuffer.");
        }

        [TestMethod]
        public void TrueColorFrameIsFullyOpaque()
        {
            Array.Fill(screen.TrueColorData, TrueColorSentinel);

            screen.ColorMode = ColorMode.TrueColor;

            renderer.Render(game.World.DisplayPlayer, Fixed.One);

            var renderedPixels = 0;

            foreach (var color in screen.TrueColorData)
            {
                if (color == TrueColorSentinel)
                {
                    continue;
                }

                renderedPixels++;

                Assert.AreEqual((byte)255, (byte)(color >> 24), "True Color renderer produced a non-opaque rendered pixel.");
            }

            Assert.IsTrue(renderedPixels > 0);
        }
        
        [TestMethod]
        public void TrueColorMap01FrameMatchesReference()
        {
            const ulong expected = 0x8C761F0674043769UL;

            Array.Clear(screen.TrueColorData, 0, screen.TrueColorData.Length);

            screen.ColorMode = ColorMode.TrueColor;
            renderer.Render(game.World.DisplayPlayer, Fixed.One);

            var actual = ComputeFrameHash(screen.TrueColorData);

            Assert.AreEqual(expected, actual, $"True Color frame changed. Expected 0x{expected:X16}, actual 0x{actual:X16}.");
        }
        
        private static ulong ComputeFrameHash(uint[] pixels)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;

            var hash = offset;

            foreach (var pixel in pixels)
            {
                hash ^= (byte)pixel;
                hash *= prime;

                hash ^= (byte)(pixel >> 8);
                hash *= prime;

                hash ^= (byte)(pixel >> 16);
                hash *= prime;

                hash ^= (byte)(pixel >> 24);
                hash *= prime;
            }

            return hash;
        }   
    }
}