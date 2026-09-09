using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfSectorModelCompatibilityTest
{
    [TestMethod]
    public void CompatibilityBoundarySelectsDoomOrBoomModelSemantics()
    {
        Assert.IsFalse(MbfSectorModelCompatibility.UsesFixedModelSemantics(
            GameCompatibility.Vanilla, compModel: false));
        Assert.IsTrue(MbfSectorModelCompatibility.UsesFixedModelSemantics(
            GameCompatibility.Boom, compModel: true));

        Assert.IsTrue(MbfSectorModelCompatibility.UsesFixedModelSemantics(
            GameCompatibility.Mbf, compModel: false));
        Assert.IsFalse(MbfSectorModelCompatibility.UsesFixedModelSemantics(
            GameCompatibility.Mbf, compModel: true));

        Assert.IsTrue(MbfSectorModelCompatibility.UsesFixedModelSemantics(
            GameCompatibility.Mbf21, compModel: false));
        Assert.IsFalse(MbfSectorModelCompatibility.UsesFixedModelSemantics(
            GameCompatibility.Mbf21, compModel: true));
    }

    [TestMethod]
    public void CompModelRestoresDoomTwoSidedAndSelfReferenceRules()
    {
        var front = CreateSector(0);
        var back = CreateSector(1);
        var unflagged = CreateLine(front, back, 0);

        Assert.IsTrue(MbfSectorModelCompatibility.IsTwoSided(
            unflagged, GameCompatibility.Mbf, compModel: false));
        Assert.IsFalse(MbfSectorModelCompatibility.IsTwoSided(
            unflagged, GameCompatibility.Mbf, compModel: true));

        var self = CreateLine(front, front, LineFlags.TwoSided);
        Assert.IsNull(MbfSectorModelCompatibility.GetNextSector(
            self, front, GameCompatibility.Mbf, compModel: false));
        Assert.AreSame(front, MbfSectorModelCompatibility.GetNextSector(
            self, front, GameCompatibility.Mbf, compModel: true));
    }

    [TestMethod]
    public void CompModelRestoresDoomHeightAndTextureSentinels()
    {
        Assert.AreEqual(Fixed.FromInt(-32000).Data,
            MbfSectorModelCompatibility.HighestFloorInitial(
                GameCompatibility.Mbf, compModel: false).Data);
        Assert.AreEqual(Fixed.FromInt(-500).Data,
            MbfSectorModelCompatibility.HighestFloorInitial(
                GameCompatibility.Mbf, compModel: true).Data);

        Assert.AreEqual(Fixed.FromInt(32000).Data,
            MbfSectorModelCompatibility.LowestCeilingInitial(
                GameCompatibility.Mbf, compModel: false).Data);
        Assert.AreEqual(Fixed.MaxValue.Data,
            MbfSectorModelCompatibility.LowestCeilingInitial(
                GameCompatibility.Mbf, compModel: true).Data);

        Assert.AreEqual(32000,
            MbfSectorModelCompatibility.ShortestTextureInitial(
                GameCompatibility.Mbf, compModel: false));
        Assert.AreEqual(int.MaxValue,
            MbfSectorModelCompatibility.ShortestTextureInitial(
                GameCompatibility.Mbf, compModel: true));

        Assert.IsFalse(MbfSectorModelCompatibility.IsUsableShortestTexture(
            0, GameCompatibility.Mbf, compModel: false));
        Assert.IsTrue(MbfSectorModelCompatibility.IsUsableShortestTexture(
            0, GameCompatibility.Mbf, compModel: true));
    }

    [TestMethod]
    public void SectorActionUsesCompModelForClassicNeighborSearch()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedMover = CreateLowerFloorMover(content, GameCompatibility.Mbf, compModel: false);
        Assert.AreEqual(Fixed.FromInt(-64).Data, fixedMover.FloorDestHeight.Data);

        var doomMover = CreateLowerFloorMover(content, GameCompatibility.Mbf, compModel: true);
        Assert.AreEqual(Fixed.FromInt(-500).Data, doomMover.FloorDestHeight.Data);

        var boomMover = CreateLowerFloorMover(content, GameCompatibility.Boom, compModel: true);
        Assert.AreEqual(Fixed.FromInt(-64).Data, boomMover.FloorDestHeight.Data);
    }

    [TestMethod]
    public void CompModelKeepsMbfNextHighestFloorSearchWithoutDoomArrayLimit()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = GameCompatibility.Mbf
        };
        options.MbfOptions.CompModel = true;

        var world = new World(content, options, null);
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
    public void LightingNeighborSearchUsesCompModelSelector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        var fixedGlow = CreateGlow(content, GameCompatibility.Mbf, compModel: false);
        Assert.AreEqual(32, fixedGlow.MinLight);

        var doomGlow = CreateGlow(content, GameCompatibility.Mbf, compModel: true);
        Assert.AreEqual(128, doomGlow.MinLight);
    }

    private static FloorMove CreateLowerFloorMover(
        GameContent content,
        GameCompatibility compatibility,
        bool compModel)
    {
        var options = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        };
        options.MbfOptions.CompModel = compModel;

        var world = new World(content, options, null);
        foreach (var mapSector in world.Map.Sectors)
            mapSector.Tag = 0;

        var target = world.Map.Sectors[0];
        target.Tag = 30000;
        target.FloorHeight = Fixed.Zero;
        target.CeilingHeight = Fixed.FromInt(128);
        target.SpecialData = null;

        var neighbor = CreateSector(10000, floorHeight: -64);
        target.Lines = new[] { CreateLine(target, neighbor, 0) };

        world.Map.BoomTags.Rebuild();
        var trigger = CreateLine(target, neighbor, 0);
        trigger.Tag = 30000;

        Assert.IsTrue(world.SectorAction.DoFloor(trigger, FloorMoveType.LowerFloor));
        var mover = target.SpecialData as FloorMove;
        Assert.IsNotNull(mover);
        return mover;
    }

    private static GlowingLight CreateGlow(
        GameContent content,
        GameCompatibility compatibility,
        bool compModel)
    {
        var options = new GameOptions
        {
            GameMode = content.Wad.GameMode,
            Compatibility = compatibility
        };
        options.MbfOptions.CompModel = compModel;

        var world = new World(content, options, null);
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
