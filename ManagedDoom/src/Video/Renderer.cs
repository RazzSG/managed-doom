//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;
using System.Runtime.InteropServices;

namespace ManagedDoom.Video
{
    public sealed class Renderer
    {
        private static double[] gammaCorrectionParameters = new double[]
        {
            1.00,
            0.95,
            0.90,
            0.85,
            0.80,
            0.75,
            0.70,
            0.65,
            0.60,
            0.55,
            0.50
        };

        private Config config;

        private Palette palette;

        private DrawScreen screen;

        private MenuRenderer menu;
        private ThreeDRenderer threeD;
        private StatusBarRenderer statusBar;
        private IntermissionRenderer intermission;
        private OpeningSequenceRenderer openingSequence;
        private AutoMapRenderer autoMap;
        private FinaleRenderer finale;

        private Patch pause;

        private int wipeBandWidth;
        private int wipeBandCount;
        private int wipeHeight;
        private byte[] wipeBuffer;
        
        private Hitscan crosshairHitscan;
        private World crosshairWorld;

        public Renderer(Config config, GameContent content)
        {
            this.config = config;

            palette = content.Palette;

            if (config.video_highresolution)
            {
                screen = new DrawScreen(content.Wad, 1280, 800);
            }
            else
            {
                screen = new DrawScreen(content.Wad, 640, 400);
            }

            config.video_gamescreensize = Math.Clamp(config.video_gamescreensize, 0, MaxWindowSize);
            config.video_gammacorrection = Math.Clamp(config.video_gammacorrection, 0, MaxGammaCorrectionLevel);

            menu = new MenuRenderer(content.Wad, screen);
            threeD = new ThreeDRenderer(content, screen, config.video_gamescreensize);
            statusBar = new StatusBarRenderer(content.Wad, screen);
            intermission = new IntermissionRenderer(content.Wad, screen);
            openingSequence = new OpeningSequenceRenderer(content.Wad, screen, this);
            autoMap = new AutoMapRenderer(content.Wad, screen);
            finale = new FinaleRenderer(content, screen);

            pause = Patch.FromWad(content.Wad, "M_PAUSE");

            var scale = screen.Width / 320;
            wipeBandWidth = 2 * scale;
            wipeBandCount = screen.Width / wipeBandWidth + 1;
            wipeHeight = screen.Height / scale;
            wipeBuffer = new byte[screen.Data.Length];

            palette.ResetColors(gammaCorrectionParameters[config.video_gammacorrection]);
        }

        public void RenderDoom(Doom doom, Fixed frameFrac)
        {
            if (doom.State == DoomState.Opening)
            {
                openingSequence.Render(doom.Opening, frameFrac);
            }
            else if (doom.State == DoomState.DemoPlayback)
            {
                RenderGame(doom.DemoPlayback.Game, frameFrac);
            }
            else if (doom.State == DoomState.Game)
            {
                RenderGame(doom.Game, frameFrac);
            }

            if (!doom.Menu.Active)
            {
                if (doom.State == DoomState.Game &&
                    doom.Game.State == GameState.Level &&
                    doom.Game.Paused)
                {
                    var scale = screen.Width / 320;
                    screen.DrawPatch(
                        pause,
                        (screen.Width - scale * pause.Width) / 2,
                        4 * scale,
                        scale);
                }
            }
        }

        public void RenderMenu(Doom doom)
        {
            if (doom.Menu.Active)
            {
                menu.Render(doom.Menu);
            }
        }

        public void RenderGame(DoomGame game, Fixed frameFrac)
        {
            if (game.Paused)
            {
                frameFrac = Fixed.One;
            }

            if (game.State == GameState.Level)
            {
                var consolePlayer = game.World.ConsolePlayer;
                var displayPlayer = game.World.DisplayPlayer;

                if (game.World.AutoMap.Visible)
                {
                    autoMap.Render(consolePlayer);
                    statusBar.Render(consolePlayer, true);
                }
                else
                {
                    threeD.Render(displayPlayer, frameFrac);
                    DrawCrosshair(consolePlayer);
                    if (threeD.WindowSize < 8)
                    {
                        statusBar.Render(consolePlayer, true);
                    }
                    else if (threeD.WindowSize == ThreeDRenderer.MaxScreenSize)
                    {
                        statusBar.Render(consolePlayer, false);
                    }
                }

                if (config.video_displaymessage || ReferenceEquals(consolePlayer.Message, (string)DoomInfo.Strings.MSGOFF))
                {
                    if (consolePlayer.MessageTime > 0)
                    {
                        var scale = screen.Width / 320;
                        screen.DrawText(consolePlayer.Message, 0, 7 * scale, scale);
                    }
                }
            }
            else if (game.State == GameState.Intermission)
            {
                intermission.Render(game.Intermission);
            }
            else if (game.State == GameState.Finale)
            {
                finale.Render(game.Finale);
            }
        }

        public void Render(Doom doom, byte[] destination, Fixed frameFrac)
        {
            if (doom.Wiping)
            {
                RenderWipe(doom, destination);
                return;
            }

            RenderDoom(doom, frameFrac);
            RenderMenu(doom);

            var colors = palette[0];
            if (doom.State == DoomState.Game &&
                doom.Game.State == GameState.Level)
            {
                colors = palette[GetPaletteNumber(doom.Game.World.ConsolePlayer)];
            }
            else if (doom.State == DoomState.Opening &&
                doom.Opening.State == OpeningSequenceState.Demo &&
                doom.Opening.DemoGame.State == GameState.Level)
            {
                colors = palette[GetPaletteNumber(doom.Opening.DemoGame.World.ConsolePlayer)];
            }
            else if (doom.State == DoomState.DemoPlayback &&
                doom.DemoPlayback.Game.State == GameState.Level)
            {
                colors = palette[GetPaletteNumber(doom.DemoPlayback.Game.World.ConsolePlayer)];
            }

            WriteData(colors, destination);
        }

        private void RenderWipe(Doom doom, byte[] destination)
        {
            RenderDoom(doom, Fixed.One);

            var wipe = doom.WipeEffect;
            var scale = screen.Width / 320;
            for (var i = 0; i < wipeBandCount - 1; i++)
            {
                var x1 = wipeBandWidth * i;
                var x2 = x1 + wipeBandWidth;
                var y1 = Math.Max(scale * wipe.Y[i], 0);
                var y2 = Math.Max(scale * wipe.Y[i + 1], 0);
                var dy = (float)(y2 - y1) / wipeBandWidth;
                for (var x = x1; x < x2; x++)
                {
                    var y = (int)MathF.Round(y1 + dy * ((x - x1) / 2 * 2));
                    var copyLength = screen.Height - y;
                    if (copyLength > 0)
                    {
                        var srcPos = screen.Height * x;
                        var dstPos = screen.Height * x + y;
                        Array.Copy(wipeBuffer, srcPos, screen.Data, dstPos, copyLength);
                    }
                }
            }

            RenderMenu(doom);

            WriteData(palette[0], destination);
        }

        public void InitializeWipe()
        {
            Array.Copy(screen.Data, wipeBuffer, screen.Data.Length);
        }

        private void WriteData(uint[] colors, byte[] destination)
        {
            var screenData = screen.Data;
            var p = MemoryMarshal.Cast<byte, uint>(destination.AsSpan());
            for (var i = 0; i < p.Length; i++)
            {
                p[i] = colors[screenData[i]];
            }
        }

        private static int GetPaletteNumber(Player player)
        {
            var count = player.DamageCount;

            if (player.Powers[(int)PowerType.Strength] != 0)
            {
                // Slowly fade the berzerk out.
                var bzc = 12 - (player.Powers[(int)PowerType.Strength] >> 6);
                if (bzc > count)
                {
                    count = bzc;
                }
            }

            int palette;

            if (count != 0)
            {
                palette = (count + 7) >> 3;

                if (palette >= Palette.DamageCount)
                {
                    palette = Palette.DamageCount - 1;
                }

                palette += Palette.DamageStart;
            }
            else if (player.BonusCount != 0)
            {
                palette = (player.BonusCount + 7) >> 3;

                if (palette >= Palette.BonusCount)
                {
                    palette = Palette.BonusCount - 1;
                }

                palette += Palette.BonusStart;
            }
            else if (player.Powers[(int)PowerType.IronFeet] > 4 * 32 ||
                (player.Powers[(int)PowerType.IronFeet] & 8) != 0)
            {
                palette = Palette.IronFeet;
            }
            else
            {
                palette = 0;
            }

            return palette;
        }
        
        private void DrawCrosshair(Player player)
        {
            if (!config.video_crosshair)
            {
                return;
            }

            var x = threeD.WindowCenterX;
            var y = threeD.WindowCenterY;

            var size = Math.Clamp(config.video_crosshair_size, 1, 7);
            var thickness = Math.Clamp(config.video_crosshair_thickness, 1, 3);
            var color = config.video_crosshair_targethealthcolor ? GetCrosshairHealthColor(player) : GetCrosshairColor(config.video_crosshair_color);

            switch (config.video_crosshair_type)
            {
                case 0:
                    DrawCrosshairCross(x, y, size, thickness, color);
                    break;

                case 1:
                    DrawCrosshairSmallCross(x, y, size, thickness, color);
                    break;

                case 2:
                    DrawCrosshairDot(x, y, size, color);
                    break;

                case 3:
                    DrawCrosshairCircle(x, y, size, thickness, color);
                    break;
            }
        }
        
        private void DrawCrosshairCross(int x, int y, int size, int thickness, int color)
        {
            size = Math.Clamp(size, 1, 7);
            thickness = Math.Clamp(thickness, 1, 3);

            var halfThickness = thickness / 2;

            var leftX = x - size - 1;
            var rightX = x + 2;

            var topY = y - size - 1;
            var bottomY = y + 2;

            screen.FillRect(leftX, y - halfThickness, size, thickness, color);
            screen.FillRect(rightX, y - halfThickness, size, thickness, color);
            screen.FillRect(x - halfThickness, topY, thickness, size, color);
            screen.FillRect(x - halfThickness, bottomY, thickness, size, color);
        }

        private void DrawCrosshairDot(int x, int y, int size, int color)
        {
            size = Math.Clamp(size, 1, 7);

            var offset = size / 2;

            screen.FillRect(x - offset, y - offset, size, size, color);
        }

        private void DrawCrosshairSmallCross(int x, int y, int size, int thickness, int color)
        {
            size = Math.Clamp(size, 1, 7);
            thickness = Math.Clamp(thickness, 1, 3);

            var left = x - size;
            var top = y - size;
            var totalSize = size * 2 + 1;

            screen.FillRect(left, y - thickness / 2, totalSize, thickness, color);

            screen.FillRect(x - thickness / 2, top, thickness, totalSize, color);
        }

        private void DrawCrosshairCircle(int x, int y, int size, int thickness, int color)
        {
            size = Math.Clamp(size, 1, 7);
            thickness = Math.Clamp(thickness, 1, 3);

            var radius = size;

            for (var offset = 0; offset < thickness; offset++)
            {
                DrawCrosshairCircleLine(x, y, radius + offset - thickness / 2, color);
            }
        }
        
        private void DrawCrosshairCircleLine(int x, int y, int radius, int color)
        {
            radius = Math.Max(1, radius);

            const int segments = 32;

            var previousX = x + radius;
            var previousY = y;

            for (var i = 1; i <= segments; i++)
            {
                var angle = MathF.PI * 2.0F * i / segments;

                var currentX = x + (int)MathF.Round(MathF.Cos(angle) * radius);

                var currentY = y + (int)MathF.Round(MathF.Sin(angle) * radius);

                screen.DrawLine(previousX, previousY, currentX, currentY, color);

                previousX = currentX;
                previousY = currentY;
            }
        }

        private int GetCrosshairColor(int color)
        {
            color = Math.Clamp(color, 0, 6);

            byte targetR;
            byte targetG;
            byte targetB;

            switch (color)
            {
                case 1:
                    targetR = 255;
                    targetG = 0;
                    targetB = 0;
                    break;

                case 2:
                    targetR = 0;
                    targetG = 255;
                    targetB = 0;
                    break;

                case 3:
                    targetR = 0;
                    targetG = 0;
                    targetB = 255;
                    break;

                case 4:
                    targetR = 255;
                    targetG = 255;
                    targetB = 0;
                    break;

                case 5:
                    targetR = 0;
                    targetG = 255;
                    targetB = 255;
                    break;

                case 6:
                    targetR = 255;
                    targetG = 0;
                    targetB = 255;
                    break;

                default:
                    targetR = 255;
                    targetG = 255;
                    targetB = 255;
                    break;
            }

            var colors = palette[0];

            var bestIndex = 0;
            var bestDistance = ulong.MaxValue;

            for (var i = 0; i < colors.Length; i++)
            {
                var current = colors[i];

                var r = (byte)(current & 0xFF);
                var g = (byte)((current >> 8) & 0xFF);
                var b = (byte)((current >> 16) & 0xFF);

                var dr = r - targetR;
                var dg = g - targetG;
                var db = b - targetB;

                var distance = (ulong)(dr * dr + dg * dg + db * db);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
        
        private Hitscan GetCrosshairHitscan(World world)
        {
            if (!ReferenceEquals(crosshairWorld, world))
            {
                crosshairWorld = world;
                crosshairHitscan = new Hitscan(world);
            }

            return crosshairHitscan;
        }
        
        private Mobj GetCrosshairTarget(Player player)
        {
            if (player?.Mobj == null)
            {
                return null;
            }

            var hitscan = GetCrosshairHitscan(player.Mobj.World);
            var range = Fixed.FromInt(2048);
            hitscan.AimLineAttack(player.Mobj, player.Mobj.Angle, range);
            var target = hitscan.LineTarget;

            if (target == null)
            {
                return null;
            }

            if (target.Health <= 0)
            {
                return null;
            }

            if ((target.Flags & MobjFlags.CountKill) == 0)
            {
                return null;
            }

            if (target.Info is not {SpawnHealth: > 0})
            {
                return null;
            }

            return target;
        }
        
        private int GetCrosshairHealthColor(Player player)
        {
            var target = GetCrosshairTarget(player);

            float healthPercent = 1.0f;

            if (target != null)
            {
                var maxHealth = target.Info.SpawnHealth;

                if (maxHealth > 0)
                {
                    var health = Math.Clamp(target.Health, 0, maxHealth);
                    healthPercent = health / (float)maxHealth;
                }
            }

            byte r;
            byte g;

            if (healthPercent >= 0.5f)
            {
                var t = (1.0f - healthPercent) * 2.0f;

                r = LerpByte(0, 255, t);
                g = 255;
            }
            else
            {
                var t = healthPercent * 2.0f;

                r = 255;
                g = LerpByte(0, 255, t);
            }

            return FindNearestPaletteColor(r, g, 0);
        }
        
        private int FindNearestPaletteColor(byte targetR, byte targetG, byte targetB)
        {
            var colors = palette[0];

            var bestIndex = 0;
            var bestDistance = ulong.MaxValue;

            for (var i = 0; i < colors.Length; i++)
            {
                var current = colors[i];

                var r = (byte)(current & 0xFF);
                var g = (byte)((current >> 8) & 0xFF);
                var b = (byte)((current >> 16) & 0xFF);

                var dr = r - targetR;
                var dg = g - targetG;
                var db = b - targetB;

                var distance = (ulong)(dr * dr + dg * dg + db * db);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
        
        private static byte LerpByte(byte a, byte b, float t)
        {
            t = Math.Clamp(t, 0.0f, 1.0f);
            return (byte)Math.Clamp((int)MathF.Round(a + (b - a) * t), 0, 255);
        }

        public int Width => screen.Width;
        public int Height => screen.Height;

        public int WipeBandCount => wipeBandCount;
        public int WipeHeight => wipeHeight;

        public int MaxWindowSize
        {
            get
            {
                return ThreeDRenderer.MaxScreenSize;
            }
        }

        public int WindowSize
        {
            get
            {
                return threeD.WindowSize;
            }

            set
            {
                config.video_gamescreensize = value;
                threeD.WindowSize = value;
            }
        }

        public bool DisplayMessage
        {
            get
            {
                return config.video_displaymessage;
            }

            set
            {
                config.video_displaymessage = value;
            }
        }
        
        public bool Crosshair
        {
            get
            {
                return config.video_crosshair;
            }

            set
            {
                config.video_crosshair = value;
            }
        }

        public int CrosshairType
        {
            get
            {
                return config.video_crosshair_type;
            }

            set
            {
                config.video_crosshair_type = Math.Clamp(value, 0, 3);
            }
        }

        public int CrosshairSize
        {
            get
            {
                return config.video_crosshair_size;
            }

            set
            {
                config.video_crosshair_size = Math.Clamp(value, 1, 7);
            }
        }
        
        public int CrosshairThickness
        {
            get
            {
                return config.video_crosshair_thickness;
            }

            set
            {
                config.video_crosshair_thickness = Math.Clamp(value, 1, 3);
            }
        }
        
        public bool CrosshairTargetHealthColor
        {
            get
            {
                return config.video_crosshair_targethealthcolor;
            }

            set
            {
                config.video_crosshair_targethealthcolor = value;
            }
        }

        public int CrosshairColor
        {
            get
            {
                return config.video_crosshair_color;
            }

            set
            {
                config.video_crosshair_color = Math.Clamp(value, 0, 6);
            }
        }

        public int MaxGammaCorrectionLevel
        {
            get
            {
                return gammaCorrectionParameters.Length - 1;
            }
        }

        public int GammaCorrectionLevel
        {
            get
            {
                return config.video_gammacorrection;
            }

            set
            {
                config.video_gammacorrection = value;
                palette.ResetColors(gammaCorrectionParameters[config.video_gammacorrection]);
            }
        }
    }
}
