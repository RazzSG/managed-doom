using System;

namespace ManagedDoom
{
    public sealed class TrueColorMap
    {
        public const int NormalMapCount = 32;

        private readonly Palette palette;
        private readonly ColorMap colorMap;
        private readonly uint[][] data;
        private readonly float[] brightness;

        public TrueColorMap(Palette palette, ColorMap colorMap)
        {
            this.palette = palette;
            this.colorMap = colorMap;

            data = new uint[colorMap.Count][];
            brightness = new float[Math.Min(NormalMapCount, colorMap.Count)];

            for (var i = 0; i < data.Length; i++)
            {
                data[i] = new uint[256];
            }
        }

        public void Rebuild()
        {
            var colors = palette[0];

            for (var map = 0; map < brightness.Length; map++)
            {
                brightness[map] = GetBrightness(map, colors);
            }

            for (var map = 0; map < data.Length; map++)
            {
                if (map < NormalMapCount)
                {
                    BuildLightMap(map, colors);
                }
                else
                {
                    BuildSpecialMap(map, colors);
                }
            }
        }

        private void BuildLightMap(int map, uint[] colors)
        {
            var target = data[map];
            var level = brightness[map];

            for (var i = 0; i < 256; i++)
            {
                target[i] = ScaleColor(colors[i], level);
            }
        }

        private float GetBrightness(int map, uint[] colors)
        {
            var indexedMap = colorMap[map];

            double sourceEnergy = 0;
            double targetEnergy = 0;

            for (var i = 0; i < 256; i++)
            {
                var source = colors[i];
                var mapped = colors[indexedMap[i]];

                var sr = (int)(source & 0xFF);
                var sg = (int)((source >> 8) & 0xFF);
                var sb = (int)((source >> 16) & 0xFF);

                var mr = (int)(mapped & 0xFF);
                var mg = (int)((mapped >> 8) & 0xFF);
                var mb = (int)((mapped >> 16) & 0xFF);

                sourceEnergy += sr * sr + sg * sg + sb * sb;
                targetEnergy += sr * mr + sg * mg + sb * mb;
            }

            if (sourceEnergy <= 0)
            {
                return 1.0f;
            }

            return Math.Clamp((float)(targetEnergy / sourceEnergy), 0.0f, 1.0f);
        }

        private void BuildSpecialMap(int map, uint[] colors)
        {
            var indexedMap = colorMap[map];
            var target = data[map];

            for (var i = 0; i < 256; i++)
            {
                target[i] = colors[indexedMap[i]];
            }
        }

        private static uint ScaleColor(uint color, float brightness)
        {
            var r = (byte)(color & 0xFF);
            var g = (byte)((color >> 8) & 0xFF);
            var b = (byte)((color >> 16) & 0xFF);

            r = (byte)Math.Clamp((int)MathF.Round(r * brightness), 0, 255);
            g = (byte)Math.Clamp((int)MathF.Round(g * brightness), 0, 255);
            b = (byte)Math.Clamp((int)MathF.Round(b * brightness), 0, 255);

            return (uint)(r | (g << 8) | (b << 16) | (255 << 24));
        }

        public uint ApplyLight(uint color, int map)
        {
            if (map < 0 || map >= brightness.Length)
            {
                return color;
            }

            return ScaleColor(color, brightness[map]);
        }

        public int Count => data.Length;
        public uint[] this[int index] => data[index];
        public uint[] FullBright => data[0];
    }
}