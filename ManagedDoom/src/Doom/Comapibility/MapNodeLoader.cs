//
// Extended/classic BSP node loading for ManagedDoom.
// The gameplay/rendering code sees one common 32-bit child-reference representation.
//

using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace ManagedDoom
{
    public sealed class MapNodeData(Vertex[] vertices, Seg[] segs, Subsector[] subsectors, Node[] nodes)
    {
        public Vertex[] Vertices { get; } = vertices;
        public Seg[] Segs { get; } = segs;
        public Subsector[] Subsectors { get; } = subsectors;
        public Node[] Nodes { get; } = nodes;
    }

    /// <summary>
    /// Loads BSP data without leaking the on-disk node format into World/renderer code.
    /// Classic Doom, XNOD and ZNOD all become the same runtime Vertex/Seg/Subsector/Node arrays.
    /// </summary>
    public static class MapNodeLoader
    {
        public static MapNodeData Load(Wad wad, int mapLump, Vertex[] originalVertices, LineDef[] lines)
        {
            if (wad == null)
                throw new ArgumentNullException(nameof(wad));
            if (originalVertices == null)
                throw new ArgumentNullException(nameof(originalVertices));
            if (lines == null)
                throw new ArgumentNullException(nameof(lines));

            var nodeLump = mapLump + 7;
            var data = wad.ReadLump(nodeLump);

            if (HasSignature(data, "XNOD"))
                return LoadExtendedNodes(data.AsSpan(4), originalVertices, lines, "XNOD");

            if (HasSignature(data, "ZNOD"))
            {
                var decompressed = DecompressZlib(data.AsSpan(4));
                return LoadExtendedNodes(decompressed, originalVertices, lines, "ZNOD");
            }

            if (HasSignature(data, "xNd4\0\0\0\0"))
            {
                throw new InvalidDataException(
                    "DeePBSP extended v4 nodes (xNd4) are not supported yet. " +
                    "Add their decoder here without changing the runtime Node representation.");
            }

            if (HasSignature(data, "XGLN") || HasSignature(data, "XGL2") || HasSignature(data, "XGL3") ||
                HasSignature(data, "ZGLN") || HasSignature(data, "ZGL2") || HasSignature(data, "ZGL3"))
            {
                throw new InvalidDataException(
                    "ZDoom GL extended nodes are not supported by the software Doom BSP path. " +
                    "A normal NODES stream (classic, XNOD or ZNOD) is required.");
            }

            var segs = Seg.FromWad(wad, mapLump + 5, originalVertices, lines);
            var subsectors = Subsector.FromWad(wad, mapLump + 6, segs);
            var nodes = Node.FromWad(wad, nodeLump, subsectors);

            return new MapNodeData(originalVertices, segs, subsectors, nodes);
        }

        private static MapNodeData LoadExtendedNodes(ReadOnlySpan<byte> data, Vertex[] originalVertices, LineDef[] lines, string formatName)
        {
            var reader = new Reader(data);
            var originalVertexCount = reader.ReadCount("original vertex count");
            var newVertexCount = reader.ReadCount("new vertex count");

            if (originalVertexCount != originalVertices.Length)
            {
                throw new InvalidDataException(
                    $"{formatName} original vertex count mismatch: stream says {originalVertexCount}, " +
                    $"VERTEXES contains {originalVertices.Length}.");
            }

            var totalVertexCount = checked(originalVertexCount + newVertexCount);
            var vertices = new Vertex[totalVertexCount];
            Array.Copy(originalVertices, vertices, originalVertices.Length);

            for (var i = 0; i < newVertexCount; i++)
            {
                var x = new Fixed(reader.ReadInt32($"extended vertex {i} X"));
                var y = new Fixed(reader.ReadInt32($"extended vertex {i} Y"));
                vertices[originalVertexCount + i] = new Vertex(x, y);
            }

            var subsectorCount = reader.ReadCount("subsector count");
            var subsectorSegCounts = new int[subsectorCount];
            long expectedSegCount = 0;

            for (var i = 0; i < subsectorCount; i++)
            {
                var count = reader.ReadCount($"subsector {i} seg count");
                subsectorSegCounts[i] = count;
                expectedSegCount += count;

                if (expectedSegCount > int.MaxValue)
                    throw new InvalidDataException($"{formatName} contains too many segs.");
            }

            var segCount = reader.ReadCount("seg count");

            if (segCount != expectedSegCount)
            {
                throw new InvalidDataException(
                    $"{formatName} seg count mismatch: subsectors reference {expectedSegCount} segs, " +
                    $"but the seg section contains {segCount}.");
            }

            var segs = new Seg[segCount];

            for (var i = 0; i < segCount; i++)
            {
                var vertex1Number = reader.ReadCount($"seg {i} vertex 1");
                var vertex2Number = reader.ReadCount($"seg {i} vertex 2");
                var lineNumber = reader.ReadUInt16($"seg {i} linedef");
                var side = reader.ReadByte($"seg {i} side");

                if ((uint)vertex1Number >= (uint)vertices.Length ||
                    (uint)vertex2Number >= (uint)vertices.Length)
                {
                    throw new InvalidDataException(
                        $"{formatName} seg {i} references vertices {vertex1Number}, {vertex2Number}; " +
                        $"vertex count={vertices.Length}.");
                }

                if (lineNumber >= lines.Length)
                {
                    throw new InvalidDataException($"{formatName} seg {i} references linedef {lineNumber}; linedef count={lines.Length}.");
                }

                if (side > 1)
                    throw new InvalidDataException($"{formatName} seg {i} has invalid side {side}.");

                var line = lines[lineNumber];
                var frontSide = side == 0 ? line.FrontSide : line.BackSide;
                var backSide = side == 0 ? line.BackSide : line.FrontSide;

                if (frontSide == null)
                {
                    throw new InvalidDataException($"{formatName} seg {i} references missing side {side} of linedef {lineNumber}.");
                }

                var v1 = vertices[vertex1Number];
                var v2 = vertices[vertex2Number];
                var angle = Geometry.PointToAngle(v1.X, v1.Y, v2.X, v2.Y);
                var lineOrigin = side == 0 ? line.Vertex1 : line.Vertex2;
                var segOffset = Geometry.PointToDist(lineOrigin.X, lineOrigin.Y, v1.X, v1.Y);

                segs[i] = new Seg(
                    v1,
                    v2,
                    segOffset,
                    angle,
                    frontSide,
                    line,
                    frontSide.Sector,
                    (line.Flags & LineFlags.TwoSided) != 0 ? backSide?.Sector : null);
            }

            var subsectors = new Subsector[subsectorCount];
            var firstSeg = 0;

            for (var i = 0; i < subsectorCount; i++)
            {
                var count = subsectorSegCounts[i];

                if (count <= 0)
                    throw new InvalidDataException($"{formatName} subsector {i} contains no segs.");

                if ((long)firstSeg + count > segs.Length)
                    throw new InvalidDataException($"{formatName} subsector {i} exceeds the seg array.");

                subsectors[i] = new Subsector(segs[firstSeg].SideDef.Sector, count, firstSeg);
                firstSeg += count;
            }

            var nodeCount = reader.ReadCount("node count");
            var nodes = new Node[nodeCount];

            for (var i = 0; i < nodeCount; i++)
            {
                var x = reader.ReadInt16($"node {i} X");
                var y = reader.ReadInt16($"node {i} Y");
                var dx = reader.ReadInt16($"node {i} dX");
                var dy = reader.ReadInt16($"node {i} dY");

                var top0 = reader.ReadInt16($"node {i} bbox0 top");
                var bottom0 = reader.ReadInt16($"node {i} bbox0 bottom");
                var left0 = reader.ReadInt16($"node {i} bbox0 left");
                var right0 = reader.ReadInt16($"node {i} bbox0 right");

                var top1 = reader.ReadInt16($"node {i} bbox1 top");
                var bottom1 = reader.ReadInt16($"node {i} bbox1 bottom");
                var left1 = reader.ReadInt16($"node {i} bbox1 left");
                var right1 = reader.ReadInt16($"node {i} bbox1 right");

                var child0 = reader.ReadUInt32($"node {i} child0");
                var child1 = reader.ReadUInt32($"node {i} child1");

                ValidateChild(formatName, child0, i, nodeCount, subsectorCount);
                ValidateChild(formatName, child1, i, nodeCount, subsectorCount);

                nodes[i] = new Node(
                    Fixed.FromInt(x),
                    Fixed.FromInt(y),
                    Fixed.FromInt(dx),
                    Fixed.FromInt(dy),
                    Fixed.FromInt(top0),
                    Fixed.FromInt(bottom0),
                    Fixed.FromInt(left0),
                    Fixed.FromInt(right0),
                    Fixed.FromInt(top1),
                    Fixed.FromInt(bottom1),
                    Fixed.FromInt(left1),
                    Fixed.FromInt(right1),
                    unchecked((int)child0),
                    unchecked((int)child1));
            }

            if (!reader.AtEnd)
            {
                throw new InvalidDataException($"{formatName} contains {reader.Remaining} unexpected trailing bytes.");
            }

            return new MapNodeData(vertices, segs, subsectors, nodes);
        }

        private static byte[] DecompressZlib(ReadOnlySpan<byte> compressed)
        {
            if (compressed.IsEmpty)
                throw new InvalidDataException("ZNOD contains an empty zlib stream.");

            try
            {
                using var source = new MemoryStream(compressed.ToArray(), false);
                using var zlib = new ZLibStream(source, CompressionMode.Decompress, false);
                using var destination = new MemoryStream();
                zlib.CopyTo(destination);
                return destination.ToArray();
            }
            catch (InvalidDataException e)
            {
                throw new InvalidDataException("ZNOD contains an invalid zlib stream.", e);
            }
        }

        private static void ValidateChild(string formatName, uint child, int nodeNumber, int nodeCount, int subsectorCount)
        {
            if ((child & Node.ExtendedSubsectorFlag) != 0)
            {
                var subsector = child & ~Node.ExtendedSubsectorFlag;

                if (subsector >= subsectorCount)
                {
                    throw new InvalidDataException(
                        $"{formatName} node {nodeNumber} references subsector {subsector}; " +
                        $"subsector count={subsectorCount}.");
                }

                return;
            }

            if (child >= nodeCount)
            {
                throw new InvalidDataException(
                    $"{formatName} node {nodeNumber} references node {child}; node count={nodeCount}.");
            }
        }

        private static bool HasSignature(byte[] data, string signature)
        {
            if (data.Length < signature.Length)
                return false;

            for (var i = 0; i < signature.Length; i++)
            {
                if (data[i] != (byte)signature[i])
                    return false;
            }

            return true;
        }

        private ref struct Reader(ReadOnlySpan<byte> data)
        {
            private readonly ReadOnlySpan<byte> data = data;
            private int position = 0;

            public bool AtEnd => position == data.Length;
            public int Remaining => data.Length - position;

            public byte ReadByte(string field)
            {
                Ensure(1, field);
                return data[position++];
            }

            public short ReadInt16(string field)
            {
                Ensure(2, field);
                var value = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(position, 2));
                position += 2;
                return value;
            }

            public ushort ReadUInt16(string field)
            {
                Ensure(2, field);
                var value = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(position, 2));
                position += 2;
                return value;
            }

            public int ReadInt32(string field)
            {
                Ensure(4, field);
                var value = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(position, 4));
                position += 4;
                return value;
            }

            public uint ReadUInt32(string field)
            {
                Ensure(4, field);
                var value = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(position, 4));
                position += 4;
                return value;
            }

            public int ReadCount(string field)
            {
                var value = ReadUInt32(field);

                if (value > int.MaxValue)
                    throw new InvalidDataException($"Extended node field '{field}' is too large: {value}.");

                return (int)value;
            }

            private void Ensure(int size, string field)
            {
                if (size < 0 || position < 0 || position > data.Length - size)
                {
                    throw new InvalidDataException(
                        $"Unexpected end of extended node stream while reading {field} " +
                        $"at offset {position}; size={data.Length}.");
                }
            }
        }
    }
}
