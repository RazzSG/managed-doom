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

namespace ManagedDoom
{
    public sealed class BlockMap
    {
        public static readonly int IntBlockSize = 128;
        public static readonly Fixed BlockSize = Fixed.FromInt(IntBlockSize);
        public static readonly int BlockMask = BlockSize.Data - 1;
        public static readonly int FracToBlockShift = Fixed.FracBits + 7;
        public static readonly int BlockToFracShift = FracToBlockShift - Fixed.FracBits;

        private Fixed originX;
        private Fixed originY;

        private int width;
        private int height;

        private readonly LineDef[] lines;

        private readonly int[] blockStart;
        private readonly int[] blockCount;
        private readonly int[] blockLines;

        private BlockMap(Fixed originX, Fixed originY, int width, int height, LineDef[] lines, int[] blockStart, int[] blockCount, int[] blockLines)
        {
            this.originX = originX;
            this.originY = originY;
            this.width = width;
            this.height = height;
            this.lines = lines;
            this.blockStart = blockStart;
            this.blockCount = blockCount;
            this.blockLines = blockLines;
            ThingLists = new Mobj[checked(width * height)];
        }

        public static BlockMap FromWad(Wad wad, int lump, LineDef[] lines)
        {
            var data = wad.ReadLump(lump);

            // Some very large maps deliberately ship an empty/invalid classic
            // BLOCKMAP because its 16-bit offset table cannot describe the map.
            // ManagedDoom uses int-based runtime indices, so rebuild it instead.
            if (data.Length < 8 || (data.Length & 1) != 0)
            {
                Console.WriteLine(
                    $"BLOCKMAP unavailable/invalid ({data.Length} bytes). " +
                    $"Rebuilding from {lines.Length} LINEDEFS.");

                return BuildFromLines(lines);
            }

            var table = new ushort[data.Length >> 1];

            for (var i = 0; i < table.Length; i++)
            {
                table[i] = (ushort)(data[i * 2] | (data[i * 2 + 1] << 8));
            }

            var originX = Fixed.FromInt((short)table[0]);
            var originY = Fixed.FromInt((short)table[1]);

            var width = table[2];
            var height = table[3];

            if (width <= 0 || height <= 0)
            {
                Console.WriteLine(
                    $"BLOCKMAP has invalid dimensions {width}x{height}. " +
                    $"Rebuilding from {lines.Length} LINEDEFS.");

                return BuildFromLines(lines);
            }

            // Classic BLOCKMAP line references are 16-bit. Once the map has more
            // linedefs than that, a classic table cannot represent every possible
            // linedef index even if its offsets happen to look structurally valid.
            if (lines.Length > ushort.MaxValue)
            {
                Console.WriteLine(
                    $"BLOCKMAP cannot address {lines.Length} LINEDEFS with 16-bit indices. " +
                    $"Rebuilding {width}x{height} runtime grid.");

                return BuildFromLines(originX, originY, width, height, lines);
            }

            if (TryBuildWadIndex(table, width, height, lines, out var blockStart, out var blockCount, out var blockLines))
            {
                return new BlockMap(
                    originX,
                    originY,
                    width,
                    height,
                    lines,
                    blockStart,
                    blockCount,
                    blockLines);
            }

            // The header/grid can still be useful even when the classic 16-bit
            // offset lists wrapped or are otherwise incompatible.
            Console.WriteLine(
                $"BLOCKMAP offsets are incompatible. " +
                $"Rebuilding {width}x{height} grid from {lines.Length} LINEDEFS.");

            return BuildFromLines(originX, originY, width, height, lines);
        }

        /// <summary>
        /// Builds a runtime blockmap when the WAD has no usable BLOCKMAP header.
        /// The grid is derived from linedef extents and uses int-based arrays,
        /// so it is not limited by the classic 16-bit BLOCKMAP offsets.
        /// </summary>
        private static BlockMap BuildFromLines(LineDef[] lines)
        {
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            if (lines.Length == 0)
            {
                return new BlockMap(
                    Fixed.Zero,
                    Fixed.Zero,
                    1,
                    1,
                    lines,
                    new int[1],
                    new int[1],
                    Array.Empty<int>());
            }

            var first = lines[0];

            long minX = Math.Min(first.Vertex1.X.Data, first.Vertex2.X.Data);
            long maxX = Math.Max(first.Vertex1.X.Data, first.Vertex2.X.Data);
            long minY = Math.Min(first.Vertex1.Y.Data, first.Vertex2.Y.Data);
            long maxY = Math.Max(first.Vertex1.Y.Data, first.Vertex2.Y.Data);

            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i];

                minX = Math.Min(minX, Math.Min(line.Vertex1.X.Data, line.Vertex2.X.Data));
                maxX = Math.Max(maxX, Math.Max(line.Vertex1.X.Data, line.Vertex2.X.Data));
                minY = Math.Min(minY, Math.Min(line.Vertex1.Y.Data, line.Vertex2.Y.Data));
                maxY = Math.Max(maxY, Math.Max(line.Vertex1.Y.Data, line.Vertex2.Y.Data));
            }

            // Linedef vertices in Doom-format maps are 16-bit map coordinates,
            // therefore these values fit Fixed.Data. Keep the exact minimum as
            // the blockmap origin, matching normal node-builder behavior.
            var originX = new Fixed(checked((int)minX));
            var originY = new Fixed(checked((int)minY));
            
            var widthLong = ((maxX - minX) >> FracToBlockShift) + 1;
            var heightLong = ((maxY - minY) >> FracToBlockShift) + 1;

            if (widthLong <= 0 ||
                heightLong <= 0 ||
                widthLong > int.MaxValue ||
                heightLong > int.MaxValue ||
                widthLong * heightLong > int.MaxValue)
            {
                throw new Exception($"Cannot rebuild BLOCKMAP: derived dimensions are " + $"{widthLong}x{heightLong}.");
            }

            var width = (int)widthLong;
            var height = (int)heightLong;

            Console.WriteLine(
                $"Fallback BLOCKMAP: origin=({originX.ToIntFloor()},{originY.ToIntFloor()}), " +
                $"size={width}x{height}.");

            return BuildFromLines(originX, originY, width, height, lines);
        }

        private static BlockMap BuildFromLines(Fixed originX, Fixed originY, int width, int height, LineDef[] lines)
        {
            var blockTotal = checked(width * height);

            var blockCount = new int[blockTotal];

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];

                GetLineBlockBounds(line, originX, originY, width, height, out var minBlockX, out var maxBlockX, out var minBlockY, out var maxBlockY);

                for (var blockY = minBlockY; blockY <= maxBlockY; blockY++)
                {
                    var row = blockY * width;

                    for (var blockX = minBlockX; blockX <= maxBlockX; blockX++)
                    {
                        checked
                        {
                            blockCount[row + blockX]++;
                        }
                    }
                }
            }

            var blockStart = new int[blockTotal];
            var totalReferences = 0;

            for (var block = 0; block < blockTotal; block++)
            {
                blockStart[block] = totalReferences;

                totalReferences = checked(totalReferences + blockCount[block]);
            }

            var blockLines = new int[totalReferences];
            var writePositions = (int[]) blockStart.Clone();

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];

                GetLineBlockBounds(line, originX, originY, width, height, out var minBlockX, out var maxBlockX, out var minBlockY, out var maxBlockY);

                for (var blockY = minBlockY; blockY <= maxBlockY; blockY++)
                {
                    var row = blockY * width;

                    for (var blockX = minBlockX; blockX <= maxBlockX; blockX++)
                    {
                        var block = row + blockX;

                        blockLines[writePositions[block]++] = lineIndex;
                    }
                }
            }

            Console.WriteLine($"Fallback BLOCKMAP built: {width}x{height}, " + $"{totalReferences} line references.");

            return new BlockMap(
                originX,
                originY,
                width,
                height,
                lines,
                blockStart,
                blockCount,
                blockLines);
        }

        private static void GetLineBlockBounds(LineDef line, Fixed originX, Fixed originY, int width, int height, out int minBlockX, out int maxBlockX, out int minBlockY, out int maxBlockY)
        {
            var box = line.BoundingBox;

            // Use 64-bit subtraction here. A map can span almost the complete
            // signed 16-bit coordinate range; doing Fixed - Fixed first can
            // overflow its 32-bit 16.16 backing integer.
            minBlockX = ToBlockCoordinate(box[Box.Left], originX);
            maxBlockX = ToBlockCoordinate(box[Box.Right], originX);
            minBlockY = ToBlockCoordinate(box[Box.Bottom], originY);
            maxBlockY = ToBlockCoordinate(box[Box.Top], originY);
            minBlockX = Math.Clamp(minBlockX, 0, width - 1);
            maxBlockX = Math.Clamp(maxBlockX, 0, width - 1);
            minBlockY = Math.Clamp(minBlockY, 0, height - 1);
            maxBlockY = Math.Clamp(maxBlockY, 0, height - 1);
        }

        private static int ToBlockCoordinate(Fixed coordinate, Fixed origin)
        {
            return ToBlockCoordinate(coordinate.Data, origin.Data);
        }
        
        private static int ToBlockCoordinate(long coordinateData, long originData)
        {
            var block = (coordinateData - originData) >> FracToBlockShift;

            if (block < int.MinValue)
                return int.MinValue;

            if (block > int.MaxValue)
                return int.MaxValue;

            return (int)block;
        }

        private static bool TryBuildWadIndex(ushort[] table, int width, int height, LineDef[] lines, out int[] blockStart, out int[] blockCount, out int[] blockLines)
        {
            blockStart = null;
            blockCount = null;
            blockLines = null;

            int blockTotal;

            try
            {
                blockTotal = checked(width * height);
            }
            catch (OverflowException)
            {
                return false;
            }

            if ((long)4 + blockTotal > table.Length)
                return false;

            var counts = new int[blockTotal];

            long totalReferences = 0;

            for (var block = 0; block < blockTotal; block++)
            {
                var offset = table[4 + block];

                if (offset >= table.Length)
                    return false;

                var position = (int)offset;

                while (position < table.Length && table[position] != ushort.MaxValue)
                {
                    var lineIndex = table[position];

                    if (lineIndex >= lines.Length)
                    {
                        return false;
                    }

                    counts[block]++;
                    position++;

                    totalReferences++;

                    if (totalReferences > int.MaxValue)
                    {
                        return false;
                    }
                }

                if (position >= table.Length)
                {
                    return false;
                }
            }

            var starts = new int[blockTotal];

            var total = 0;

            try
            {
                for (var block = 0; block < blockTotal; block++)
                {
                    starts[block] = total;

                    total = checked(total + counts[block]);
                }
            }
            catch (OverflowException)
            { 
                return false;
            }

            var flattenedLines = new int[total];
            var writePositions = (int[]) starts.Clone();

            for (var block = 0; block < blockTotal; block++)
            {
                var position = (int)table[4 + block];

                while (table[position] != ushort.MaxValue)
                {
                    flattenedLines[writePositions[block]++] = table[position++];
                }
            }

            blockStart = starts;
            blockCount = counts;
            blockLines = flattenedLines;
            
            return true;
        }

        public int GetBlockX(Fixed x)
        {
            return ToBlockCoordinate(x, originX);
        }

        public int GetBlockY(Fixed y)
        {
            return ToBlockCoordinate(y, originY);
        }

        public int GetBlockX(Fixed x, Fixed offset)
        {
            return ToBlockCoordinate((long)x.Data + offset.Data, originX.Data);
        }

        public int GetBlockY(Fixed y, Fixed offset)
        {
            return ToBlockCoordinate((long)y.Data + offset.Data, originY.Data);
        }

        public int GetIndex(int blockX, int blockY)
        {
            if ((uint)blockX >= (uint)width || (uint)blockY >= (uint)height)
            {
                return -1;
            }

            return width * blockY + blockX;
        }

        public int GetIndex(Fixed x, Fixed y)
        {
            return GetIndex(GetBlockX(x), GetBlockY(y));
        }

        public bool IterateLines(int blockX, int blockY, Func<LineDef, bool> func, int validCount)
        {
            var index = GetIndex(blockX, blockY);

            if (index == -1)
                return true;

            var start = blockStart[index];
            var end = start + blockCount[index];

            for (var i = start; i < end; i++)
            {
                var line = lines[blockLines[i]];

                if (line.ValidCount == validCount)
                    continue;

                line.ValidCount = validCount;

                if (!func(line))
                    return false;
            }

            return true;
        }

        public bool IterateThings(int blockX, int blockY, Func<Mobj, bool> func)
        {
            var index = GetIndex(blockX, blockY);

            if (index == -1)
                return true;

            for (var mobj = ThingLists[index]; mobj != null; mobj = mobj.BlockNext)
            {
                if (!func(mobj))
                    return false;
            }

            return true;
        }

        public Fixed OriginX => originX;
        public Fixed OriginY => originY;
        public int Width => width;
        public int Height => height;

        public Mobj[] ThingLists
        {
            get;
        }
    }
}
