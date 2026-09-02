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

            if (data.Length < 8 || (data.Length & 1) != 0)
            {
                throw new Exception("Invalid BLOCKMAP lump.");
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

            if (width <= 0 || height <= 0) throw new Exception("Invalid BLOCKMAP dimensions.");

            if (TryBuildWadIndex(table, width, height, lines, out var blockStart, out var blockCount, out var blockLines))
            {
                return new BlockMap(originX, originY, width, height, lines, blockStart, blockCount, blockLines);
            }

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
                        blockCount[row + blockX]++;
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
            var writePositions = (int[])blockStart.Clone();

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

            return new BlockMap(originX, originY, width, height, lines, blockStart, blockCount, blockLines);
        }
        
        private static void GetLineBlockBounds(LineDef line, Fixed originX, Fixed originY, int width, int height, out int minBlockX, out int maxBlockX, out int minBlockY, out int maxBlockY)
        {
            var box = line.BoundingBox;

            minBlockX = (box[Box.Left] - originX).Data >> FracToBlockShift;
            maxBlockX = (box[Box.Right] - originX).Data >> FracToBlockShift;
            minBlockY = (box[Box.Bottom] - originY).Data >> FracToBlockShift;
            maxBlockY = (box[Box.Top] - originY).Data >> FracToBlockShift;
            minBlockX = Math.Clamp(minBlockX, 0, width - 1);
            maxBlockX = Math.Clamp(maxBlockX, 0, width - 1);
            minBlockY = Math.Clamp(minBlockY, 0, height - 1);
            maxBlockY = Math.Clamp(maxBlockY, 0, height - 1);
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

                var position = offset;

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
                        return false;
                }

                if (position >= table.Length)
                    return false;
            }

            var starts = new int[blockTotal];

            var total = 0;

            for (var block = 0; block < blockTotal; block++)
            {
                starts[block] = total;
                total = checked(total + counts[block]);
            }

            var flattenedLines = new int[total];
            var writePositions = (int[])starts.Clone();

            for (var block = 0; block < blockTotal; block++)
            {
                var position = table[4 + block];

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
            return (x - originX).Data >> FracToBlockShift;
        }

        public int GetBlockY(Fixed y)
        {
            return (y - originY).Data >> FracToBlockShift;
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
        public Mobj[] ThingLists { get; }
    }
}
