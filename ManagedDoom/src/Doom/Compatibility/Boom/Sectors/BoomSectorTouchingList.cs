using System;

namespace ManagedDoom.Compatibility.Boom.Sectors;

/// <summary>
/// Maintains Boom's touching-sector lists used by P_CheckSector.
/// Existing nodes are reused as things move and detached nodes are pooled,
/// avoiding per-tic garbage in normal movement.
/// </summary>
public sealed class BoomSectorTouchingList
{
    private readonly World world;
    private readonly Fixed[] thingBox;
    private readonly Func<LineDef, bool> collectSectorLine;

    private Mobj currentThing;
    private BoomSectorTouchNode freeNodes;

    // Keep these stamps disjoint from World.GetNewValidCount(), whose normal
    // traversal stamps are positive. This list is already built during world
    // startup, before World resets its global valid count.
    private int lineValidCount = -1;

    public BoomSectorTouchingList(World world)
    {
        this.world = world;
        thingBox = new Fixed[4];
        collectSectorLine = CollectSectorLine;
    }

    public void Update(Mobj thing)
    {
        if ((thing.Flags & MobjFlags.NoSector) != 0)
        {
            RemoveAll(thing);
            return;
        }

        for (var node = thing.TouchingSectorList; node != null; node = node.ThingNext)
        {
            node.Keep = false;
        }

        currentThing = thing;
        thingBox[Box.Top] = thing.Y + thing.Radius;
        thingBox[Box.Bottom] = thing.Y - thing.Radius;
        thingBox[Box.Right] = thing.X + thing.Radius;
        thingBox[Box.Left] = thing.X - thing.Radius;

        var blockMap = world.Map.BlockMap;
        var validCount = GetLineValidCount();
        var minBlockX = blockMap.GetBlockX(thingBox[Box.Left]);
        var maxBlockX = blockMap.GetBlockX(thingBox[Box.Right]);
        var minBlockY = blockMap.GetBlockY(thingBox[Box.Bottom]);
        var maxBlockY = blockMap.GetBlockY(thingBox[Box.Top]);

        for (var x = minBlockX; x <= maxBlockX; x++)
        {
            for (var y = minBlockY; y <= maxBlockY; y++)
            {
                blockMap.IterateLines(x, y, collectSectorLine, validCount);
            }
        }

        AddSector(thing.Subsector.Sector, thing);

        var current = thing.TouchingSectorList;
        while (current != null)
        {
            var next = current.ThingNext;
            if (!current.Keep)
            {
                RemoveNode(thing, current);
            }
            else
            {
                current.Keep = false;
            }
            current = next;
        }

        currentThing = null;
    }

    public void RemoveAll(Mobj thing)
    {
        var node = thing.TouchingSectorList;
        while (node != null)
        {
            var next = node.ThingNext;
            RemoveNode(thing, node);
            node = next;
        }

        thing.TouchingSectorList = null;
    }

    public void VisitSectorThings(Sector sector, Func<Mobj, bool> action)
    {
        for (var node = sector.TouchingThingList; node != null; node = node.SectorNext)
        {
            node.Visited = false;
        }

        while (true)
        {
            BoomSectorTouchNode pending = null;

            for (var node = sector.TouchingThingList; node != null; node = node.SectorNext)
            {
                if (!node.Visited)
                {
                    pending = node;
                    break;
                }
            }

            if (pending == null)
            {
                return;
            }

            pending.Visited = true;
            var thing = pending.Thing;

            // Boom's P_CheckSector ignores things that are not in the blockmap.
            if ((thing.Flags & MobjFlags.NoBlockMap) == 0)
            {
                action(thing);
            }
        }
    }

    private int GetLineValidCount()
    {
        if (lineValidCount == int.MinValue)
        {
            // This can only happen after more than two billion position updates.
            // Clear our private negative stamps before reusing the range.
            foreach (var line in world.Map.Lines)
            {
                if (line.ValidCount < 0)
                {
                    line.ValidCount = 0;
                }
            }
            lineValidCount = -1;
        }

        return lineValidCount--;
    }

    private bool CollectSectorLine(LineDef line)
    {
        if (thingBox[Box.Right] <= line.BoundingBox[Box.Left] ||
            thingBox[Box.Left] >= line.BoundingBox[Box.Right] ||
            thingBox[Box.Top] <= line.BoundingBox[Box.Bottom] ||
            thingBox[Box.Bottom] >= line.BoundingBox[Box.Top])
        {
            return true;
        }

        if (Geometry.BoxOnLineSide(thingBox, line) != -1)
        {
            return true;
        }

        AddSector(line.FrontSector, currentThing);

        if (line.BackSector != null && line.BackSector != line.FrontSector)
        {
            AddSector(line.BackSector, currentThing);
        }

        return true;
    }

    private void AddSector(Sector sector, Mobj thing)
    {
        if (sector == null)
        {
            return;
        }

        for (var node = thing.TouchingSectorList; node != null; node = node.ThingNext)
        {
            if (node.Sector == sector)
            {
                node.Thing = thing;
                node.Keep = true;
                return;
            }
        }

        var newNode = GetNode();
        newNode.Sector = sector;
        newNode.Thing = thing;
        newNode.Visited = false;
        newNode.Keep = true;

        newNode.ThingPrevious = null;
        newNode.ThingNext = thing.TouchingSectorList;
        if (thing.TouchingSectorList != null)
        {
            thing.TouchingSectorList.ThingPrevious = newNode;
        }
        thing.TouchingSectorList = newNode;

        newNode.SectorPrevious = null;
        newNode.SectorNext = sector.TouchingThingList;
        if (sector.TouchingThingList != null)
        {
            sector.TouchingThingList.SectorPrevious = newNode;
        }
        sector.TouchingThingList = newNode;
    }

    private void RemoveNode(Mobj thing, BoomSectorTouchNode node)
    {
        if (node.ThingPrevious != null)
        {
            node.ThingPrevious.ThingNext = node.ThingNext;
        }
        else
        {
            thing.TouchingSectorList = node.ThingNext;
        }

        if (node.ThingNext != null)
        {
            node.ThingNext.ThingPrevious = node.ThingPrevious;
        }

        if (node.SectorPrevious != null)
        {
            node.SectorPrevious.SectorNext = node.SectorNext;
        }
        else if (node.Sector != null)
        {
            node.Sector.TouchingThingList = node.SectorNext;
        }

        if (node.SectorNext != null)
        {
            node.SectorNext.SectorPrevious = node.SectorPrevious;
        }

        PutNode(node);
    }

    private BoomSectorTouchNode GetNode()
    {
        if (freeNodes == null)
        {
            return new BoomSectorTouchNode();
        }

        var node = freeNodes;
        freeNodes = node.ThingNext;
        node.ThingNext = null;
        return node;
    }

    private void PutNode(BoomSectorTouchNode node)
    {
        node.Sector = null;
        node.Thing = null;
        node.ThingPrevious = null;
        node.SectorPrevious = null;
        node.SectorNext = null;
        node.Visited = false;
        node.Keep = false;

        node.ThingNext = freeNodes;
        freeNodes = node;
    }
}
