using System.Linq;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTransferHeightTest
{
    private const int SkyFlat = 999;

    [TestMethod]
    public void BoomSpawnResolvesStatic242ControlSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var line = world.Map.Lines.First(l => l.FrontSector != null);
        var control = line.FrontSector;
        var target = world.Map.Sectors.First(s => s != control);

        ResetTransferSetup(world);
        line.Special = (LineSpecial)BoomTransferHeightResolver.TransferHeightsSpecial;
        line.Tag = 30200;
        target.Tag = 30200;
        world.Map.BoomTags.Rebuild();

        world.Specials.SpawnSpecials();

        Assert.AreSame(control, target.HeightSector);
        Assert.AreSame(line, target.HeightSectorLine);
    }

    [TestMethod]
    public void VanillaSpawnDoesNotResolveStatic242ControlSector()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Vanilla }, null);
        var line = world.Map.Lines.First(l => l.FrontSector != null);
        var target = world.Map.Sectors.First(s => s != line.FrontSector);

        ResetTransferSetup(world);
        line.Special = (LineSpecial)BoomTransferHeightResolver.TransferHeightsSpecial;
        line.Tag = 30201;
        target.Tag = 30201;
        world.Map.BoomTags.Rebuild();

        world.Specials.SpawnSpecials();

        Assert.IsNull(target.HeightSector);
        Assert.IsNull(target.HeightSectorLine);
    }

    [TestMethod]
    public void MbfEditTransferHeightFenceKeepsRealFloorPegging()
    {
        // MBFEDIT MAP01: target sector 133 is physically closed at z=128,
        // but linedef 242 makes it render as 0..128. Its MIDBARS3 sides use
        // lower-unpegged plus row offset -128. Pegging to fake floor 0 shifts
        // the whole texture below the visible floor; Boom pegs to real floor 128.
        var textureAlt = BoomTransferHeightResolver.ResolveMaskedMiddleTextureAlt(
            LineFlags.TwoSided | LineFlags.DontPegBottom,
            Fixed.FromInt(128),
            Fixed.FromInt(128),
            Fixed.Zero,
            Fixed.FromInt(128),
            textureHeight: 128,
            rowOffset: Fixed.FromInt(-128),
            viewZ: Fixed.FromInt(41));

        Assert.AreEqual(Fixed.FromInt(128 - 41).Data, textureAlt.Data);
    }

    [TestMethod]
    public void NonTransferredMaskedMiddleTextureKeepsExistingPeggingMath()
    {
        var lowerUnpegged = BoomTransferHeightResolver.ResolveMaskedMiddleTextureAlt(
            LineFlags.TwoSided | LineFlags.DontPegBottom,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            Fixed.FromInt(32),
            Fixed.FromInt(128),
            textureHeight: 64,
            rowOffset: Fixed.FromInt(8),
            viewZ: Fixed.FromInt(41));

        var upperPegged = BoomTransferHeightResolver.ResolveMaskedMiddleTextureAlt(
            LineFlags.TwoSided,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            Fixed.FromInt(32),
            Fixed.FromInt(96),
            textureHeight: 64,
            rowOffset: Fixed.FromInt(8),
            viewZ: Fixed.FromInt(41));

        Assert.AreEqual(Fixed.FromInt(32 + 64 + 8 - 41).Data, lowerUnpegged.Data);
        Assert.AreEqual(Fixed.FromInt(96 + 8 - 41).Data, upperPegged.Data);
    }

    [TestMethod]
    public void NormalZoneUsesControlHeightsAndTargetAppearance()
    {
        var (target, control, viewSector) = CreateTransferSetup();

        var state = Resolve(target, viewSector, Fixed.FromInt(64));

        Assert.IsTrue(state.Transferred);
        Assert.AreEqual(BoomTransferHeightZone.Normal, state.Zone);
         Assert.AreEqual(Fixed.FromInt(32).Data, state.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(96).Data, state.CeilingHeight.Data);
        Assert.AreEqual(target.FloorFlat, state.FloorFlat);
        Assert.AreEqual(target.CeilingFlat, state.CeilingFlat);
        Assert.AreEqual(target.LightLevel, state.LightLevel);
        Assert.AreSame(target, state.FloorPlaneSector);
        Assert.AreSame(target, state.CeilingPlaneSector);
    }

    [TestMethod]
    public void UnderwaterFrontUsesRealFloorFakeFloorCeilingAndControlAppearance()
    {
        var (target, control, viewSector) = CreateTransferSetup();
        MakeViewSectorUseTransfer(viewSector);

        var state = Resolve(target, viewSector, Fixed.FromInt(16));

        Assert.AreEqual(BoomTransferHeightZone.BelowFakeFloor, state.Zone);
        Assert.AreEqual(Fixed.Zero.Data, state.FloorHeight.Data);
        Assert.AreEqual((Fixed.FromInt(32) - Fixed.Epsilon).Data, state.CeilingHeight.Data);
        Assert.AreEqual(control.FloorFlat, state.FloorFlat);
        Assert.AreEqual(control.CeilingFlat, state.CeilingFlat);
        Assert.AreEqual(control.LightLevel, state.LightLevel);
        Assert.AreSame(control, state.FloorPlaneSector);
        Assert.AreSame(control, state.CeilingPlaneSector);
    }

    [TestMethod]
    public void UnderwaterBackKeepsTargetAppearanceButUsesFakePortalGeometry()
    {
        var (target, _, viewSector) = CreateTransferSetup();
        MakeViewSectorUseTransfer(viewSector);

        var state = Resolve(target, viewSector, Fixed.FromInt(16), back: true);

        Assert.AreEqual(BoomTransferHeightZone.BelowFakeFloor, state.Zone);
        Assert.AreEqual(Fixed.Zero.Data, state.FloorHeight.Data);
        Assert.AreEqual((Fixed.FromInt(32) - Fixed.Epsilon).Data, state.CeilingHeight.Data);
        Assert.AreEqual(target.FloorFlat, state.FloorFlat);
        Assert.AreEqual(target.CeilingFlat, state.CeilingFlat);
        Assert.AreEqual(target.LightLevel, state.LightLevel);
        Assert.AreSame(target, state.FloorPlaneSector);
        Assert.AreSame(target, state.CeilingPlaneSector);
    }

    [TestMethod]
    public void AboveFakeCeilingUsesControlAppearanceAndRealCeiling()
    {
        var (target, control, viewSector) = CreateTransferSetup();
        MakeViewSectorUseTransfer(viewSector);

        var state = Resolve(target, viewSector, Fixed.FromInt(112));

        Assert.AreEqual(BoomTransferHeightZone.AboveFakeCeiling, state.Zone);
        Assert.AreEqual((Fixed.FromInt(96) + Fixed.Epsilon).Data, state.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(128).Data, state.CeilingHeight.Data);
        Assert.AreEqual(control.FloorFlat, state.FloorFlat);
        Assert.AreEqual(control.CeilingFlat, state.CeilingFlat);
        Assert.AreEqual(control.LightLevel, state.LightLevel);
        Assert.AreSame(control, state.FloorPlaneSector);
        Assert.AreSame(control, state.CeilingPlaneSector);
    }

    [TestMethod]
    public void ViewingZoneComesFromPlayersHeightSectorNotTargetHeightSector()
    {
        var (target, _, viewSector) = CreateTransferSetup();
        var viewControl = MakeViewSectorUseTransfer(viewSector);
        viewControl.FloorHeight = Fixed.FromInt(-64);
        viewControl.CeilingHeight = Fixed.FromInt(192);

        // This Z is below the target's fake floor (32), but not below the
        // player's own transferred floor (-64), so Boom remains in normal space.
        var state = Resolve(target, viewSector, Fixed.FromInt(16));

        Assert.AreEqual(BoomTransferHeightZone.Normal, state.Zone);
        Assert.AreEqual(Fixed.FromInt(32).Data, state.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(96).Data, state.CeilingHeight.Data);
    }

    [TestMethod]
    public void TransferHeightTracksMovingControlSectorWithoutRebuild()
    {
        var (target, control, viewSector) = CreateTransferSetup();

        var before = Resolve(target, viewSector, Fixed.FromInt(64));
        Assert.AreEqual(Fixed.FromInt(32).Data, before.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(96).Data, before.CeilingHeight.Data);

        control.FloorHeight = Fixed.FromInt(48);
        control.CeilingHeight = Fixed.FromInt(80);

        var after = Resolve(target, viewSector, Fixed.FromInt(64));
        Assert.AreEqual(BoomTransferHeightZone.Normal, after.Zone);
        Assert.AreEqual(Fixed.FromInt(48).Data, after.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(80).Data, after.CeilingHeight.Data);
    }

    [TestMethod]
    public void NormalAndUnderwaterZonesComposeWithIndependentLightTransfers()
    {
        var (target, control, viewSector) = CreateTransferSetup();
        var targetFloorLight = new Sector(3, Fixed.Zero, Fixed.FromInt(128), 0, 0, 72, 0, 0);
        var controlCeilingLight = new Sector(4, Fixed.Zero, Fixed.FromInt(128), 0, 0, 208, 0, 0);
        target.FloorLightSector = targetFloorLight;
        control.CeilingLightSector = controlCeilingLight;

        var normal = Resolve(target, viewSector, Fixed.FromInt(64));
        MakeViewSectorUseTransfer(viewSector);
        var below = Resolve(target, viewSector, Fixed.FromInt(16));

        Assert.AreEqual(72, normal.FloorLightLevel);
        Assert.AreEqual(target.CeilingLightLevel, normal.CeilingLightLevel);
        Assert.AreEqual(control.FloorLightLevel, below.FloorLightLevel);
        Assert.AreEqual(208, below.CeilingLightLevel);
    }

    [TestMethod]
    public void UnderwaterSkyControlCeilingCollapsesAtFakeFloor()
    {
        var (target, control, viewSector) = CreateTransferSetup();
        MakeViewSectorUseTransfer(viewSector);
        control.CeilingFlat = SkyFlat;

        var state = Resolve(target, viewSector, Fixed.FromInt(16));

        Assert.AreEqual(BoomTransferHeightZone.BelowFakeFloor, state.Zone);
         Assert.AreEqual(Fixed.FromInt(32).Data, state.FloorHeight.Data);
        Assert.AreEqual((Fixed.FromInt(32) - Fixed.Epsilon).Data, state.CeilingHeight.Data);
        Assert.AreEqual(control.FloorFlat, state.FloorFlat);
        Assert.AreEqual(control.FloorFlat, state.CeilingFlat);
        Assert.IsTrue(state.ForceFloorPlane);
    }

    [TestMethod]
    public void AboveSkyControlFloorKeepsCollapsedFakeCeilingPair()
    {
        var (target, control, viewSector) = CreateTransferSetup();
        MakeViewSectorUseTransfer(viewSector);
        control.FloorFlat = SkyFlat;

        var state = Resolve(target, viewSector, Fixed.FromInt(112));

        Assert.AreEqual(BoomTransferHeightZone.AboveFakeCeiling, state.Zone);
        Assert.AreEqual((Fixed.FromInt(96) + Fixed.Epsilon).Data, state.FloorHeight.Data);
        Assert.AreEqual(Fixed.FromInt(96).Data, state.CeilingHeight.Data);
        Assert.AreEqual(control.CeilingFlat, state.FloorFlat);
        Assert.AreEqual(control.CeilingFlat, state.CeilingFlat);
        Assert.IsTrue(state.ForceCeilingPlane);
    }

    private static BoomTransferHeightRenderState Resolve(
        Sector target,
        Sector viewSector,
        Fixed viewZ,
        bool back = false)
    {
        var viewZone = BoomTransferHeightResolver.ResolveViewZone(viewSector, viewZ, Fixed.One);
        return BoomTransferHeightResolver.Resolve(
            target,
            viewZone,
            Fixed.One,
            SkyFlat,
            back);
    }

    private static (Sector Target, Sector Control, Sector ViewSector) CreateTransferSetup()
    {
        var target = new Sector(
            0,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            10,
            11,
            96,
            0,
            0);

        var control = new Sector(
            1,
            Fixed.FromInt(32),
            Fixed.FromInt(96),
            20,
            21,
            160,
            0,
            0);

        var viewSector = new Sector(
            2,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            30,
            31,
            128,
            0,
            0);

        target.HeightSector = control;
        return (target, control, viewSector);
    }

    private static Sector MakeViewSectorUseTransfer(Sector viewSector)
    {
        var control = new Sector(
            5,
            Fixed.FromInt(32),
            Fixed.FromInt(96),
            40,
            41,
            144,
            0,
            0);
        viewSector.HeightSector = control;
        return control;
    }

    private static void ResetTransferSetup(World world)
    {
        foreach (var sector in world.Map.Sectors)
        {
            sector.Tag = 0;
            sector.Special = 0;
            sector.HeightSector = null;
            sector.HeightSectorLine = null;
        }

        foreach (var line in world.Map.Lines)
        {
            if ((int)line.Special == BoomTransferHeightResolver.TransferHeightsSpecial)
                line.Special = 0;
        }
    }
}
