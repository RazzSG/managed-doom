using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTransferHeightSpriteVisibilityTest
{
    [TestMethod]
    public void SectorWithoutTransferHeightIsAlwaysVisible()
    {
        var sector = CreateSector(0, 0, 128);
        var viewSector = CreateSector(1, 0, 128);

        Assert.IsTrue(IsVisible(sector, viewSector, 64, -128, -64));
    }

    [TestMethod]
    public void NormalViewRejectsSpriteWhoseTopIsBelowFakeFloor()
    {
        var (sector, viewSector) = CreateTransferredPair();

        Assert.IsFalse(IsVisible(sector, viewSector, 64, 0, 31));
    }

    [TestMethod]
    public void NormalViewKeepsSpriteCrossingFakeFloor()
    {
        var (sector, viewSector) = CreateTransferredPair();

        Assert.IsTrue(IsVisible(sector, viewSector, 64, 0, 32));
    }

    [TestMethod]
    public void NormalViewRejectsSpriteWhoseOriginIsAtFakeCeiling()
    {
        var (sector, viewSector) = CreateTransferredPair();

        Assert.IsFalse(IsVisible(sector, viewSector, 64, 96, 120));
    }

    [TestMethod]
    public void UnderwaterViewRejectsSpriteWhoseOriginIsAtFakeFloor()
    {
        var (sector, viewSector) = CreateTransferredPair(withViewTransfer: true);

        Assert.IsFalse(IsVisible(sector, viewSector, 16, 32, 64));
    }

    [TestMethod]
    public void UnderwaterViewKeepsSpriteWhoseOriginIsBelowFakeFloorEvenWhenTopCrossesIt()
    {
        var (sector, viewSector) = CreateTransferredPair(withViewTransfer: true);

        Assert.IsTrue(IsVisible(sector, viewSector, 16, 16, 48));
    }

    [TestMethod]
    public void ViewExactlyAtFakeFloorUsesNormalSpriteBoundaryRule()
    {
        var (sector, viewSector) = CreateTransferredPair(withViewTransfer: true);

        // R_ProjectSprite uses viewZ < control floor, not <=. At exactly the
        // fake floor this sprite crosses the plane and therefore stays visible.
        Assert.IsTrue(IsVisible(sector, viewSector, 32, 32, 64));
    }

    [TestMethod]
    public void AboveCeilingViewRejectsSpriteCompletelyBelowFakeCeiling()
    {
        var (sector, viewSector) = CreateTransferredPair(withViewTransfer: true);

        Assert.IsFalse(IsVisible(sector, viewSector, 112, 48, 95));
    }

    [TestMethod]
    public void AboveCeilingViewKeepsSpriteThatReachesFakeCeiling()
    {
        var (sector, viewSector) = CreateTransferredPair(withViewTransfer: true);

        Assert.IsTrue(IsVisible(sector, viewSector, 112, 64, 96));
    }

    [TestMethod]
    public void ViewExactlyAtFakeCeilingUsesNormalSpriteBoundaryRule()
    {
        var (sector, viewSector) = CreateTransferredPair(withViewTransfer: true);

        // R_ProjectSprite uses viewZ > control ceiling, not >=. In normal-space
        // rules an origin at the fake ceiling is hidden.
        Assert.IsFalse(IsVisible(sector, viewSector, 96, 96, 128));
    }

    [TestMethod]
    public void AboveOwnCeilingDoesNotUseHigherTargetCeilingUntilViewerReachesIt()
    {
        var target = CreateSector(0, 0, 128);
        target.HeightSector = CreateSector(1, 32, 160);

        var viewSector = CreateSector(2, 0, 128);
        viewSector.HeightSector = CreateSector(3, 32, 96);

        // The viewer is above its own fake ceiling (96), but still below this
        // target's fake ceiling (160). Boom's extra viewZ >= target ceiling
        // guard keeps the sprite visible here.
        Assert.IsTrue(IsVisible(target, viewSector, 112, 64, 120));
    }

    [TestMethod]
    public void MovingControlHeightIsReadDynamically()
    {
        var (sector, viewSector) = CreateTransferredPair();

        Assert.IsTrue(IsVisible(sector, viewSector, 64, 0, 40));

        sector.HeightSector.FloorHeight = Fixed.FromInt(48);

        Assert.IsFalse(IsVisible(sector, viewSector, 64, 0, 40));
    }

    private static bool IsVisible(
        Sector sector,
        Sector viewSector,
        int viewZ,
        int thingZ,
        int spriteTopZ)
    {
        var fixedViewZ = Fixed.FromInt(viewZ);
        var viewZone = BoomTransferHeightSpriteVisibility.ResolveViewZone(
            viewSector,
            fixedViewZ,
            Fixed.One);

        var control = sector.HeightSector;
        if (control == null)
            return true;

        return BoomTransferHeightSpriteVisibility.IsVisible(
            viewZone,
            fixedViewZ,
            control.GetInterpolatedFloorHeight(Fixed.One),
            control.GetInterpolatedCeilingHeight(Fixed.One),
            Fixed.FromInt(thingZ),
            Fixed.FromInt(spriteTopZ));
    }

    private static (Sector Sector, Sector ViewSector) CreateTransferredPair(bool withViewTransfer = false)
    {
        var sector = CreateSector(0, 0, 128);
        sector.HeightSector = CreateSector(1, 32, 96);

        var viewSector = CreateSector(2, 0, 128);
        if (withViewTransfer)
            viewSector.HeightSector = CreateSector(3, 32, 96);

        return (sector, viewSector);
    }

    private static Sector CreateSector(int number, int floor, int ceiling)
    {
        return new Sector(
            number,
            Fixed.FromInt(floor),
            Fixed.FromInt(ceiling),
            0,
            0,
            128,
            0,
            0);
    }
}
