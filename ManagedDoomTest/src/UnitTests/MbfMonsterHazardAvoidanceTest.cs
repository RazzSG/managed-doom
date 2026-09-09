using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Sectors;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfMonsterHazardAvoidanceTest
{
    [TestMethod]
    public void FeatureStartsAtMbfCompatibility()
    {
        Assert.IsFalse(MbfMonsterHazardAvoidance.Applies(GameCompatibility.Vanilla));
        Assert.IsFalse(MbfMonsterHazardAvoidance.Applies(GameCompatibility.Boom));
        Assert.IsTrue(MbfMonsterHazardAvoidance.Applies(GameCompatibility.Mbf));
        Assert.IsTrue(MbfMonsterHazardAvoidance.Applies(GameCompatibility.Mbf21));
    }

    [TestMethod]
    public void ActiveUpwardCeilingIsModerateHazard()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var sector = CreateSector();
        sector.CeilingData = new CeilingMove(world) { Sector = sector, Direction = 1 };
        var actor = CreateActorTouching(world, sector);

        Assert.AreEqual(1, MbfMonsterHazardAvoidance.GetCurrentHazardDirection(actor));
        Assert.IsTrue(MbfMonsterHazardAvoidance.IsUnderDamage(actor));
    }

    [TestMethod]
    public void ActiveDownwardCeilingIsSeriousHazard()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var sector = CreateSector();
        sector.CeilingData = new CeilingMove(world) { Sector = sector, Direction = -1 };
        var actor = CreateActorTouching(world, sector);

        Assert.AreEqual(-1, MbfMonsterHazardAvoidance.GetCurrentHazardDirection(actor));
        Assert.IsTrue(MbfMonsterHazardAvoidance.IsUnderDamage(actor));
    }

    [TestMethod]
    public void WaitingCeilingMoverIsNotCurrentlyHazardous()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var sector = CreateSector();
        sector.CeilingData = new CeilingMove(world) { Sector = sector, Direction = 0 };
        var actor = CreateActorTouching(world, sector);

        Assert.AreEqual(0, MbfMonsterHazardAvoidance.GetCurrentHazardDirection(actor));
        Assert.IsFalse(MbfMonsterHazardAvoidance.IsUnderDamage(actor));
    }

    [TestMethod]
    public void DownwardHazardWinsAcrossTouchingSectorList()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var upwardSector = CreateSector(number: 1);
        var downwardSector = CreateSector(number: 2);
        upwardSector.CeilingData = new CeilingMove(world) { Sector = upwardSector, Direction = 1 };
        downwardSector.CeilingData = new CeilingMove(world) { Sector = downwardSector, Direction = -1 };
        var actor = CreateActorTouching(world, upwardSector, downwardSector);

        Assert.AreEqual(-1, MbfMonsterHazardAvoidance.GetCurrentHazardDirection(actor));
    }

    [TestMethod]
    public void SeriousNewHazardIsAlwaysAvoided()
    {
        Assert.IsTrue(MbfMonsterHazardAvoidance.ShouldAbandonDirection(
            GameCompatibility.Mbf,
            wasUnderDamage: false,
            currentHazardDirection: -1,
            randomByte: 255));
    }

    [TestMethod]
    public void ModerateNewHazardUsesOriginal200Of256Threshold()
    {
        Assert.IsTrue(MbfMonsterHazardAvoidance.ShouldAbandonDirection(
            GameCompatibility.Mbf,
            wasUnderDamage: false,
            currentHazardDirection: 1,
            randomByte: 199));

        Assert.IsFalse(MbfMonsterHazardAvoidance.ShouldAbandonDirection(
            GameCompatibility.Mbf,
            wasUnderDamage: false,
            currentHazardDirection: 1,
            randomByte: 200));
    }

    [TestMethod]
    public void MonsterAlreadyInHazardMayKeepMovingToEscape()
    {
        Assert.IsFalse(MbfMonsterHazardAvoidance.ShouldAbandonDirection(
            GameCompatibility.Mbf,
            wasUnderDamage: true,
            currentHazardDirection: -1,
            randomByte: 0));
    }

    [TestMethod]
    public void BoomNeverEnablesMbfHazardAvoidance()
    {
        Assert.IsFalse(MbfMonsterHazardAvoidance.ShouldAbandonDirection(
            GameCompatibility.Boom,
            wasUnderDamage: false,
            currentHazardDirection: -1,
            randomByte: 0));
    }

    private static Sector CreateSector(int number = 0) => new Sector(
        number,
        Fixed.Zero,
        Fixed.FromInt(128),
        0,
        0,
        160,
        SectorSpecial.Normal,
        0);

    private static Mobj CreateActorTouching(World world, params Sector[] sectors)
    {
        var actor = new Mobj(world);
        BoomSectorTouchNode first = null;
        BoomSectorTouchNode previous = null;

        foreach (var sector in sectors)
        {
            var node = new BoomSectorTouchNode
            {
                Sector = sector,
                Thing = actor
            };

            if (first == null)
                first = node;

            if (previous != null)
            {
                previous.ThingNext = node;
                node.ThingPrevious = previous;
            }

            previous = node;
        }

        actor.TouchingSectorList = first;
        return actor;
    }
}
