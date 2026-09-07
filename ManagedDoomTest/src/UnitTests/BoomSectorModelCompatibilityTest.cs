using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomSectorModelCompatibilityTest
{
    [TestMethod]
    public void BoomUsesActualSecondSidedefInsteadOfTwoSidedFlag()
    {
        var front = CreateSector(0);
        var back = CreateSector(1);
        var line = CreateLine(front, back, 0);

        Assert.IsFalse(BoomSectorModelCompatibility.IsTwoSided(line, GameCompatibility.Vanilla));
        Assert.IsTrue(BoomSectorModelCompatibility.IsTwoSided(line, GameCompatibility.Boom));
        Assert.IsTrue(BoomSectorModelCompatibility.IsTwoSided(line, GameCompatibility.Mbf));
        Assert.IsTrue(BoomSectorModelCompatibility.IsTwoSided(line, GameCompatibility.Mbf21));

        Assert.IsNull(BoomSectorModelCompatibility.GetNextSector(line, front, GameCompatibility.Vanilla));
        Assert.AreSame(back, BoomSectorModelCompatibility.GetNextSector(line, front, GameCompatibility.Boom));
    }

    [TestMethod]
    public void BoomIgnoresSelfReferencingLineForNeighborQueries()
    {
        var sector = CreateSector(0);
        var line = CreateLine(sector, sector, LineFlags.TwoSided);

        Assert.AreSame(sector, BoomSectorModelCompatibility.GetNextSector(
            line, sector, GameCompatibility.Vanilla));
        Assert.IsNull(BoomSectorModelCompatibility.GetNextSector(
            line, sector, GameCompatibility.Boom));
    }

    [TestMethod]
    public void BoomUsesSafeModelHeightSentinels()
    {
        Assert.AreEqual(Fixed.FromInt(-500).Data,
            BoomSectorModelCompatibility.HighestFloorInitial(GameCompatibility.Vanilla).Data);
        Assert.AreEqual(Fixed.Zero.Data,
            BoomSectorModelCompatibility.HighestCeilingInitial(GameCompatibility.Vanilla).Data);
        Assert.AreEqual(Fixed.MaxValue.Data,
            BoomSectorModelCompatibility.LowestCeilingInitial(GameCompatibility.Vanilla).Data);

        foreach (var compatibility in new[]
                 {
                     GameCompatibility.Boom,
                     GameCompatibility.Mbf,
                     GameCompatibility.Mbf21
                 })
        {
            Assert.AreEqual(Fixed.FromInt(-32000).Data,
                BoomSectorModelCompatibility.HighestFloorInitial(compatibility).Data);
            Assert.AreEqual(Fixed.FromInt(-32000).Data,
                BoomSectorModelCompatibility.HighestCeilingInitial(compatibility).Data);
            Assert.AreEqual(Fixed.FromInt(32000).Data,
                BoomSectorModelCompatibility.LowestCeilingInitial(compatibility).Data);
        }
    }

    [TestMethod]
    public void BoomShortestTextureRulesSkipDashPlaceholderAndClampHeight()
    {
        Assert.IsTrue(BoomSectorModelCompatibility.IsUsableShortestTexture(
            0, GameCompatibility.Vanilla));
        Assert.IsFalse(BoomSectorModelCompatibility.IsUsableShortestTexture(
            0, GameCompatibility.Boom));
        Assert.IsTrue(BoomSectorModelCompatibility.IsUsableShortestTexture(
            1, GameCompatibility.Boom));

        Assert.AreEqual(32000,
            BoomSectorModelCompatibility.ShortestTextureInitial(GameCompatibility.Boom));
        Assert.AreEqual(int.MaxValue,
            BoomSectorModelCompatibility.ShortestTextureInitial(GameCompatibility.Vanilla));

        Assert.AreEqual(Fixed.FromInt(32000).Data,
            BoomSectorModelCompatibility.AddShortestTextureHeight(
                Fixed.FromInt(1000), 32000, GameCompatibility.Boom).Data);
        Assert.AreEqual(Fixed.FromInt(1128).Data,
            BoomSectorModelCompatibility.AddShortestTextureHeight(
                Fixed.FromInt(1000), 128, GameCompatibility.Boom).Data);
    }

    [TestMethod]
    public void SectorActionUsesBoomActualSidedefForNeighborFloor()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanillaMover = CreateLowerFloorMover(content, GameCompatibility.Vanilla);
        Assert.AreEqual(Fixed.FromInt(-500).Data, vanillaMover.FloorDestHeight.Data);

        var boomMover = CreateLowerFloorMover(content, GameCompatibility.Boom);
        Assert.AreEqual(Fixed.FromInt(-64).Data, boomMover.FloorDestHeight.Data);
    }

    [TestMethod]
    public void BoomNextHighestFloorSearchHasNoFixedAdjoiningSectorLimit()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Boom
        }, null);

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var target = world.Map.Sectors[0];
        target.Tag = 30000;
        target.FloorHeight = Fixed.Zero;
        target.CeilingHeight = Fixed.FromInt(128);
        target.SpecialData = null;

        var lines = new LineDef[80];
        for (var i = 0; i < lines.Length; i++)
        {
            var neighbor = CreateSector(10000 + i, floorHeight: i + 1);
            lines[i] = CreateLine(target, neighbor, LineFlags.TwoSided);
        }
        target.Lines = lines;

        world.Map.BoomTags.Rebuild();
        var trigger = CreateLine(target, CreateSector(20000), 0);
        trigger.Tag = 30000;

        Assert.IsTrue(world.SectorAction.DoFloor(trigger, FloorMoveType.RaiseFloorToNearest));
        var mover = target.SpecialData as FloorMove;
        Assert.IsNotNull(mover);
        Assert.AreEqual(Fixed.FromInt(1).Data, mover.FloorDestHeight.Data);
    }

    [TestMethod]
    public void LightingChangeUsesBoomActualSidedefForSurroundingLight()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var vanillaGlow = CreateGlow(content, GameCompatibility.Vanilla);
        Assert.AreEqual(128, vanillaGlow.MinLight);

        var boomGlow = CreateGlow(content, GameCompatibility.Boom);
        Assert.AreEqual(32, boomGlow.MinLight);
    }

    private static FloorMove CreateLowerFloorMover(GameContent content, GameCompatibility compatibility)
    {
        var world = new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        }, null);

        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var target = world.Map.Sectors[0];
        target.Tag = 30000;
        target.FloorHeight = Fixed.Zero;
        target.CeilingHeight = Fixed.FromInt(128);
        target.SpecialData = null;

        var neighbor = CreateSector(10000, floorHeight: -64);
        var modelLine = CreateLine(target, neighbor, 0);
        target.Lines = new[] { modelLine };

        world.Map.BoomTags.Rebuild();

        var trigger = CreateLine(target, neighbor, 0);
        trigger.Tag = 30000;

        Assert.IsTrue(world.SectorAction.DoFloor(trigger, FloorMoveType.LowerFloor));
        var mover = target.SpecialData as FloorMove;
        Assert.IsNotNull(mover);
        return mover;
    }

    private static GlowingLight CreateGlow(GameContent content, GameCompatibility compatibility)
    {
        var world = new World(content, new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        }, null);

        var target = CreateSector(0, lightLevel: 128);
        var neighbor = CreateSector(1, lightLevel: 32);
        target.Lines = new[] { CreateLine(target, neighbor, 0) };

        world.LightingChange.SpawnGlowingLight(target);
        var glow = target.LightingData as GlowingLight;
        Assert.IsNotNull(glow);
        return glow;
    }

    private static Sector CreateSector(
        int number,
        int floorHeight = 0,
        int ceilingHeight = 128,
        int lightLevel = 128)
    {
        return new Sector(
            number,
            Fixed.FromInt(floorHeight),
            Fixed.FromInt(ceilingHeight),
            0,
            0,
            lightLevel,
            SectorSpecial.Normal,
            0)
        {
            Lines = System.Array.Empty<LineDef>()
        };
    }

    private static LineDef CreateLine(Sector front, Sector back, LineFlags flags)
    {
        var frontSide = new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, front);
        var backSide = back == null
            ? null
            : new SideDef(Fixed.Zero, Fixed.Zero, 0, 0, 0, back);

        return new LineDef(
            new Vertex(Fixed.Zero, Fixed.Zero),
            new Vertex(Fixed.FromInt(64), Fixed.Zero),
            flags,
            (LineSpecial)0,
            0,
            frontSide,
            backSide);
    }
}
