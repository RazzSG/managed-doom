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
using System.Collections.Generic;
using System.IO;

namespace ManagedDoom
{
    public sealed class Patch
    {
        private string name;
        private int width;
        private int height;
        private int leftOffset;
        private int topOffset;
        private Column[][] columns;

        public Patch(
            string name,
            int width,
            int height,
            int leftOffset,
            int topOffset,
            Column[][] columns)
        {
            this.name = name;
            this.width = width;
            this.height = height;
            this.leftOffset = leftOffset;
            this.topOffset = topOffset;
            this.columns = columns;
        }

        public static Patch FromData(string name, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (data.Length < 8)
                throw InvalidPatch(name, $"lump is too small ({data.Length} bytes)");

            var width = BitConverter.ToInt16(data, 0);
            var height = BitConverter.ToInt16(data, 2);
            var leftOffset = BitConverter.ToInt16(data, 4);
            var topOffset = BitConverter.ToInt16(data, 6);

            if (width <= 0)
                throw InvalidPatch(name, $"invalid width {width}");

            if (height <= 0)
                throw InvalidPatch(name, $"invalid height {height}");

            var columnTableEnd = 8L + 4L * width;

            if (columnTableEnd > data.Length)
            {
                throw InvalidPatch(name, $"column table ends at {columnTableEnd}, but lump size is only {data.Length} bytes " + $"(width={width}, height={height})");
            }

            PadData(ref data, width, name);

            var columns = new Column[width][];
            for (var x = 0; x < width; x++)
            {
                var cs = new List<Column>();
                var p = BitConverter.ToInt32(data, 8 + 4 * x);
                while (true)
                {
                    ValidatePostPosition(name, data, x, p);
                    var topDelta = data[p];
                    if (topDelta == Column.Last)
                    {
                        break;
                    }

                    if (p + 1 >= data.Length)
                        throw InvalidColumn(name, x, p, data.Length, "missing post length");

                    var length = data[p + 1];

                    if ((long)p + length + 3 >= data.Length)
                    {
                        throw InvalidColumn(name, x, p, data.Length, $"post length {length} extends past the lump");
                    }

                    var offset = p + 3;
                    cs.Add(new Column(topDelta, data, offset, length));
                    p += length + 4;
                }
                columns[x] = cs.ToArray();
            }

            return new Patch(
                name,
                width,
                height,
                leftOffset,
                topOffset,
                columns);
        }

        public static Patch FromWad(Wad wad, string name)
        {
            return FromData(name, wad.ReadLump(name));
        }

        private static void PadData(ref byte[] data, int width, string name)
        {
            var need = 0;
            for (var x = 0; x < width; x++)
            {
                var tableOffset = 8 + 4 * x;

                if (tableOffset < 0 || tableOffset + 4 > data.Length)
                {
                    throw InvalidPatch(name, $"column offset table is truncated at x={x}, tableOffset={tableOffset}, size={data.Length}");
                }

                var p = BitConverter.ToInt32(data, tableOffset);

                while (true)
                {
                    ValidatePostPosition(name, data, x, p);
                    var topDelta = data[p];
                    if (topDelta == Column.Last)
                    {
                        break;
                    }

                    if (p + 1 >= data.Length)
                    {
                        throw InvalidColumn(name, x, p, data.Length, "missing post length");
                    }

                    var length = data[p + 1];

                    // A Doom patch post is:
                    // topdelta, length, unused, pixels[length], unused.
                    var next = (long)p + length + 4;

                    if (next > data.Length)
                    {
                        throw InvalidColumn(name, x, p, data.Length, $"post length {length} gives next offset {next}");
                    }

                    var offset = p + 3;
                    need = Math.Max(offset + 128, need);
                    p += length + 4;
                }
            }

            if (data.Length < need)
            {
                Array.Resize(ref data, need);
            }
        }

        private static void ValidatePostPosition(string name, byte[] data, int column, int position)
        {
            if (position < 0 || position >= data.Length)
            {
                throw InvalidColumn(name, column, position, data.Length, "column/post offset is outside the lump");
            }
        }

        private static InvalidDataException InvalidPatch(string name, string message)
        {
            return new InvalidDataException($"Invalid Doom patch '{name}': {message}.");
        }

        private static InvalidDataException InvalidColumn(string name, int column, int position, int size, string message)
        {
            return new InvalidDataException($"Invalid Doom patch '{name}', column={column}, offset={position}, lumpSize={size}: {message}.");
        }

        public override string ToString()
        {
            return name;
        }

        public string Name => name;
        public int Width => width;
        public int Height => height;
        public int LeftOffset => leftOffset;
        public int TopOffset => topOffset;
        public Column[][] Columns => columns;
    }
}
