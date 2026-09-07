using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomSectorTouchingListTest
{
    [TestMethod]
    public void BoomWorldLinksPlayerToItsOriginSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer.Mobj;
        var sector = player.Subsector.Sector;

        Assert.IsTrue(ThingTouchesSector(player, sector));
        Assert.IsTrue(SectorContainsThing(sector, player));
    }

    [TestMethod]
    public void VanillaWorldDoesNotBuildBoomTouchingLists()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var player = world.ConsolePlayer.Mobj;

        Assert.IsNull(player.TouchingSectorList);
        Assert.IsNull(player.Subsector.Sector.TouchingThingList);
    }

    [TestMethod]
    public void RadiusCrossingTwoSidedLineLinksBothSectors()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer.Mobj;
        var line = FindTwoSidedLine(world, player.Subsector.Sector);

        MoveThingToLineMidpoint(world, player, line, Fixed.FromInt(16));

        Assert.IsTrue(ThingTouchesSector(player, line.FrontSector));
        Assert.IsTrue(ThingTouchesSector(player, line.BackSector));
        Assert.IsTrue(SectorContainsThing(line.FrontSector, player));
        Assert.IsTrue(SectorContainsThing(line.BackSector, player));
    }

    [TestMethod]
    public void RepositionRemovesVacatedTouchingSectorNodes()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer.Mobj;
        var originSector = player.Subsector.Sector;
        var originX = player.X;
        var originY = player.Y;
        var line = FindTwoSidedLine(world, originSector);

        MoveThingToLineMidpoint(world, player, line, Fixed.FromInt(16));
        Assert.IsTrue(ThingTouchesSector(player, line.FrontSector));
        Assert.IsTrue(ThingTouchesSector(player, line.BackSector));

        // Return to the original point with zero radius. The two sectors touched
        // at the remote line must be detached from both linked-list threads.
        world.ThingMovement.UnsetThingPosition(player);
        player.X = originX;
        player.Y = originY;
        player.Radius = Fixed.Zero;
        world.ThingMovement.SetThingPosition(player);

        Assert.AreEqual(1, CountThingSectors(player));
        Assert.AreSame(originSector, player.TouchingSectorList.Sector);
        Assert.IsFalse(SectorContainsThing(line.FrontSector, player));
        Assert.IsFalse(SectorContainsThing(line.BackSector, player));
    }

    [TestMethod]
    public void CheckSectorSkipsNoBlockMapThings()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer.Mobj;
        var sector = player.Subsector.Sector;
        var inert = new Mobj(world)
        {
            X = player.X,
            Y = player.Y,
            Radius = Fixed.FromInt(16),
            Flags = MobjFlags.NoBlockMap
        };

        world.ThingMovement.SetThingPosition(inert);
        Assert.IsTrue(ThingTouchesSector(inert, sector));

        var sawInert = false;
        world.ThingMovement.VisitThingsTouchingSector(sector, thing =>
        {
            if (ReferenceEquals(thing, inert))
            {
                sawInert = true;
            }
            return true;
        });

        Assert.IsFalse(sawInert);

        world.ThingMovement.UnsetThingPosition(inert);
        world.ThingMovement.RemoveTouchingSectorLinks(inert);
    }

    [TestMethod]
    public void CheckSectorIterationSurvivesTouchingListMutation()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var player = world.ConsolePlayer.Mobj;
        var sector = player.Subsector.Sector;

        var first = CreateLinkedThing(world, player.X, player.Y);
        var second = CreateLinkedThing(world, player.X, player.Y);
        Assert.IsTrue(ThingTouchesSector(first, sector));
        Assert.IsTrue(ThingTouchesSector(second, sector));

        var firstVisited = false;
        var secondVisited = false;
        world.ThingMovement.VisitThingsTouchingSector(sector, thing =>
        {
            if (ReferenceEquals(thing, first))
            {
                firstVisited = true;
                world.ThingMovement.RemoveTouchingSectorLinks(first);
            }
            else if (ReferenceEquals(thing, second))
            {
                secondVisited = true;
                world.ThingMovement.RemoveTouchingSectorLinks(second);
            }
            return true;
        });

        Assert.IsTrue(firstVisited);
        Assert.IsTrue(secondVisited);

        world.ThingMovement.UnsetThingPosition(first);
        world.ThingMovement.UnsetThingPosition(second);
    }

    private static Mobj CreateLinkedThing(World world, Fixed x, Fixed y)
    {
        var thing = new Mobj(world)
        {
            X = x,
            Y = y,
            Radius = Fixed.FromInt(8)
        };
        world.ThingMovement.SetThingPosition(thing);
        return thing;
    }

    private static LineDef FindTwoSidedLine(World world, Sector excludedSector)
    {
        foreach (var line in world.Map.Lines)
        {
            if (line.FrontSector != null &&
                line.BackSector != null &&
                !ReferenceEquals(line.FrontSector, line.BackSector) &&
                !ReferenceEquals(line.FrontSector, excludedSector) &&
                !ReferenceEquals(line.BackSector, excludedSector))
            {
                return line;
            }
        }

        Assert.Fail("MAP01 must contain a remote two-sided line between distinct sectors.");
        return null;
    }

    private static void MoveThingToLineMidpoint(World world, Mobj thing, LineDef line, Fixed radius)
    {
        world.ThingMovement.UnsetThingPosition(thing);
        thing.X = (line.Vertex1.X + line.Vertex2.X) / 2;
        thing.Y = (line.Vertex1.Y + line.Vertex2.Y) / 2;
        thing.Radius = radius;
        world.ThingMovement.SetThingPosition(thing);
    }

    private static bool ThingTouchesSector(Mobj thing, Sector sector)
    {
        for (var node = thing.TouchingSectorList; node != null; node = node.ThingNext)
        {
            if (ReferenceEquals(node.Sector, sector))
            {
                return true;
            }
        }
        return false;
    }

    private static bool SectorContainsThing(Sector sector, Mobj thing)
    {
        for (var node = sector.TouchingThingList; node != null; node = node.SectorNext)
        {
            if (ReferenceEquals(node.Thing, thing))
            {
                return true;
            }
        }
        return false;
    }

    private static int CountThingSectors(Mobj thing)
    {
        var count = 0;
        for (var node = thing.TouchingSectorList; node != null; node = node.ThingNext)
        {
            count++;
        }
        return count;
    }
}
