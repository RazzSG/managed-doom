using System;

namespace ManagedDoom.Video
{
    public sealed class TrueColorPaletteEffect
    {
        private const int CubeBits = 5;
        private const int CubeSize = 1 << CubeBits;
        private const int CubeShift = 8 - CubeBits;

        private readonly Palette palette;
        private readonly byte[] nearestColor;
        private readonly short[][] deltaR;
        private readonly short[][] deltaG;
        private readonly short[][] deltaB;

        public TrueColorPaletteEffect(Palette palette)
        {
            this.palette = palette;

            nearestColor = new byte[CubeSize * CubeSize * CubeSize];

            deltaR = new short[palette.Count][];
            deltaG = new short[palette.Count][];
            deltaB = new short[palette.Count][];

            for (var i = 0; i < palette.Count; i++)
            {
                deltaR[i] = new short[256];
                deltaG[i] = new short[256];
                deltaB[i] = new short[256];
            }
        }

        public void Rebuild()
        {
            BuildNearestColors();
            BuildPaletteDeltas();
        }

        private void BuildNearestColors()
        {
            var colors = palette[0];

            for (var r5 = 0; r5 < CubeSize; r5++)
            {
                var r = (r5 << CubeShift) | (r5 >> (2 * CubeBits - 8));

                for (var g5 = 0; g5 < CubeSize; g5++)
                {
                    var g = (g5 << CubeShift) | (g5 >> (2 * CubeBits - 8));

                    for (var b5 = 0; b5 < CubeSize; b5++)
                    {
                        var b = (b5 << CubeShift) | (b5 >> (2 * CubeBits - 8));

                        var bestIndex = 0;
                        var bestDistance = int.MaxValue;

                        for (var i = 0; i < 256; i++)
                        {
                            var color = colors[i];
                            var dr = r - (int)(color & 0xFF);
                            var dg = g - (int)((color >> 8) & 0xFF);
                            var db = b - (int)((color >> 16) & 0xFF);
                            var distance = dr * dr + dg * dg + db * db;

                            if (distance < bestDistance)
                            {
                                bestDistance = distance;
                                bestIndex = i;
                            }
                        }

                        nearestColor[(r5 << 10) | (g5 << 5) | b5] = (byte)bestIndex;
                    }
                }
            }
        }

        private void BuildPaletteDeltas()
        {
            var normal = palette[0];

            for (var p = 0; p < palette.Count; p++)
            {
                var effect = palette[p];

                for (var i = 0; i < 256; i++)
                {
                    var source = normal[i];
                    var target = effect[i];

                    deltaR[p][i] = (short)((target & 0xFF) - (source & 0xFF));
                    deltaG[p][i] = (short)(((target >> 8) & 0xFF) - ((source >> 8) & 0xFF));
                    deltaB[p][i] = (short)(((target >> 16) & 0xFF) - ((source >> 16) & 0xFF));
                }
            }
        }

        public void Write(ReadOnlySpan<uint> source, Span<uint> destination, int paletteNumber)
        {
            if (paletteNumber == 0)
            {
                source.CopyTo(destination);
                return;
            }

            var dr = deltaR[paletteNumber];
            var dg = deltaG[paletteNumber];
            var db = deltaB[paletteNumber];

            for (var i = 0; i < source.Length; i++)
            {
                var color = source[i];

                var r = (int)(color & 0xFF);
                var g = (int)((color >> 8) & 0xFF);
                var b = (int)((color >> 16) & 0xFF);

                var index = nearestColor[((r >> CubeShift) << 10) | ((g >> CubeShift) << 5) | (b >> CubeShift)];

                r = Math.Clamp(r + dr[index], 0, 255);
                g = Math.Clamp(g + dg[index], 0, 255);
                b = Math.Clamp(b + db[index], 0, 255);

                destination[i] = (uint)(r | (g << 8) | (b << 16) | (255 << 24));
            }
        }
    }
}