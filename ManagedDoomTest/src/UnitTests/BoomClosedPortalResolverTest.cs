using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomClosedPortalResolverTest
{
    [TestMethod]
    public void BoomTreatsCollapsedBackSectorAsClosedPortal()
    {
        // BOOMEDIT crusher-style case: the moving floor has reached the
        // sector ceiling, while the neighboring front sector is still open.
        Assert.IsTrue(BoomClosedPortalResolver.IsClosed(
            GameCompatibility.Boom,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            Fixed.FromInt(64),
            Fixed.FromInt(64),
            hasTopTexture: true,
            hasBottomTexture: true,
            bothCeilingsAreSky: false));
    }

    [TestMethod]
    public void VanillaPreservesOriginalSelfClosedPortalBehavior()
    {
        Assert.IsFalse(BoomClosedPortalResolver.IsClosed(
            GameCompatibility.Vanilla,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            Fixed.FromInt(64),
            Fixed.FromInt(64),
            hasTopTexture: true,
            hasBottomTexture: true,
            bothCeilingsAreSky: false));
    }

    [TestMethod]
    public void DirectGeometricClosureIsClosedInEveryCompatibilityLevel()
    {
        foreach (var compatibility in new[]
                 {
                     GameCompatibility.Vanilla,
                     GameCompatibility.Boom,
                     GameCompatibility.Mbf,
                     GameCompatibility.Mbf21
                 })
        {
            Assert.IsTrue(BoomClosedPortalResolver.IsClosed(
                compatibility,
                Fixed.FromInt(64),
                Fixed.FromInt(128),
                Fixed.FromInt(0),
                Fixed.FromInt(64),
                hasTopTexture: false,
                hasBottomTexture: false,
                bothCeilingsAreSky: false));
        }
    }

    [TestMethod]
    public void BoomPreservesTransparentLiftUpperTextureEscape()
    {
        Assert.IsFalse(BoomClosedPortalResolver.IsClosed(
            GameCompatibility.Boom,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            Fixed.FromInt(64),
            Fixed.FromInt(64),
            hasTopTexture: false,
            hasBottomTexture: true,
            bothCeilingsAreSky: false));
    }

    [TestMethod]
    public void BoomPreservesTransparentLiftLowerTextureEscape()
    {
        Assert.IsFalse(BoomClosedPortalResolver.IsClosed(
            GameCompatibility.Boom,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            Fixed.FromInt(64),
            Fixed.FromInt(64),
            hasTopTexture: true,
            hasBottomTexture: false,
            bothCeilingsAreSky: false));
    }

    [TestMethod]
    public void BoomKeepsSharedSkyCeilingsOpen()
    {
        // Exercise Boom's self-closed back-sector sky exception without also
        // triggering the unconditional cross-sector closure checks.
        Assert.IsFalse(BoomClosedPortalResolver.IsClosed(
            GameCompatibility.Boom,
            Fixed.FromInt(0),
            Fixed.FromInt(128),
            Fixed.FromInt(64),
            Fixed.FromInt(64),
            hasTopTexture: true,
            hasBottomTexture: true,
            bothCeilingsAreSky: true));
    }
}
