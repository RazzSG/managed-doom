using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomLightTransferTest
{
    [TestMethod]
    public void FloorLightTransferTracksControlSectorDynamically()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = world.Map.Lines.First(l => l.FrontSector != null);
        var source = line.FrontSector;
        var target = world.Map.Sectors.First(s => s != source);

        ResetTransferSetup(world);
        line.Special = (LineSpecial)213;
        line.Tag = 30100;
        target.Tag = 30100;
        source.LightLevel = 192;
        target.LightLevel = 64;
        world.Map.BoomTags.Rebuild();

        world.Specials.SpawnSpecials();

        Assert.AreSame(source, target.FloorLightSector);
        Assert.IsNull(target.CeilingLightSector);
        Assert.AreEqual(192, target.FloorLightLevel);
        Assert.AreEqual(64, target.CeilingLightLevel);
        Assert.AreEqual(64, target.LightLevel);

        source.LightLevel = 96;
        Assert.AreEqual(96, target.FloorLightLevel);
        Assert.AreEqual(64, target.CeilingLightLevel);
    }

    [TestMethod]
    public void FloorAndCeilingTransfersUseIndependentControlSectors()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var floorLine = world.Map.Lines.First(l => l.FrontSector != null);
        var ceilingLine = world.Map.Lines.First(l => l.FrontSector != null && l.FrontSector != floorLine.FrontSector);
        var floorSource = floorLine.FrontSector;
        var ceilingSource = ceilingLine.FrontSector;
        var target = world.Map.Sectors.First(s => s != floorSource && s != ceilingSource);

        ResetTransferSetup(world);
        floorLine.Special = (LineSpecial)213;
        floorLine.Tag = 30101;
        ceilingLine.Special = (LineSpecial)261;
        ceilingLine.Tag = 30101;
        target.Tag = 30101;
        target.LightLevel = 128;
        floorSource.LightLevel = 224;
        ceilingSource.LightLevel = 48;
        world.Map.BoomTags.Rebuild();

        BoomLightTransferResolver.Apply(world);

        Assert.AreSame(floorSource, target.FloorLightSector);
        Assert.AreSame(ceilingSource, target.CeilingLightSector);
        Assert.AreEqual(224, target.FloorLightLevel);
        Assert.AreEqual(48, target.CeilingLightLevel);
        Assert.AreEqual(128, target.LightLevel);

        floorSource.LightLevel = 176;
        ceilingSource.LightLevel = 80;
        Assert.AreEqual(176, target.FloorLightLevel);
        Assert.AreEqual(80, target.CeilingLightLevel);
    }

    [TestMethod]
    public void VanillaSpawnDoesNotApplyBoomLightTransfer()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var line = world.Map.Lines.First(l => l.FrontSector != null);
        var source = line.FrontSector;
        var target = world.Map.Sectors.First(s => s != source);

        ResetTransferSetup(world);
        line.Special = (LineSpecial)213;
        line.Tag = 30102;
        target.Tag = 30102;
        source.LightLevel = 224;
        target.LightLevel = 137;
        world.Map.BoomTags.Rebuild();

        world.Specials.SpawnSpecials();

        Assert.IsNull(target.FloorLightSector);
        Assert.IsNull(target.CeilingLightSector);
        Assert.AreEqual(137, target.LightLevel);
        Assert.AreEqual(137, target.FloorLightLevel);
        Assert.AreEqual(137, target.CeilingLightLevel);
    }

    private static void ResetTransferSetup(World world)
    {
        foreach (var sector in world.Map.Sectors)
        {
            sector.Tag = 0;
            sector.Special = 0;
            sector.FloorLightSector = null;
            sector.CeilingLightSector = null;
        }

        foreach (var line in world.Map.Lines)
        {
            if ((int)line.Special is 213 or 261)
                line.Special = 0;
        }
    }
}
