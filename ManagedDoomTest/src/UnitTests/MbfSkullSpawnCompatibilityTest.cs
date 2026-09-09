using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfSkullSpawnCompatibilityTest
{
    [TestMethod]
    public void VanillaKeepsClassicUnsafeSpawn()
    {
        Assert.IsFalse(MbfSkullSpawnCompatibility.UsesSafeLostSoulSpawn(
            GameCompatibility.Vanilla,
            compSkull: false));

        Assert.IsFalse(MbfSkullSpawnCompatibility.UsesSafeLostSoulSpawn(
            GameCompatibility.Vanilla,
            compSkull: true));
    }

    [TestMethod]
    public void BoomKeepsCorrectedSafeSpawn()
    {
        Assert.IsTrue(MbfSkullSpawnCompatibility.UsesSafeLostSoulSpawn(
            GameCompatibility.Boom,
            compSkull: false));

        Assert.IsTrue(MbfSkullSpawnCompatibility.UsesSafeLostSoulSpawn(
            GameCompatibility.Boom,
            compSkull: true));
    }

    [TestMethod]
    public void MbfUsesCompSkullOption()
    {
        Assert.IsTrue(MbfSkullSpawnCompatibility.UsesSafeLostSoulSpawn(
            GameCompatibility.Mbf,
            compSkull: false));

        Assert.IsFalse(MbfSkullSpawnCompatibility.UsesSafeLostSoulSpawn(
            GameCompatibility.Mbf,
            compSkull: true));
    }

    [TestMethod]
    public void Mbf21InheritsMbfCompSkullSelection()
    {
        Assert.IsTrue(MbfSkullSpawnCompatibility.UsesSafeLostSoulSpawn(
            GameCompatibility.Mbf21,
            compSkull: false));

        Assert.IsFalse(MbfSkullSpawnCompatibility.UsesSafeLostSoulSpawn(
            GameCompatibility.Mbf21,
            compSkull: true));
    }

    [TestMethod]
    public void RuntimeVerticalBoundsCheckIsBypassedOnlyWhenCompSkullIsEnabled()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        var world = new World(content, options, null);

        Assert.IsTrue(MbfSkullSpawnCompatibility.IsLostSoulOutsideVerticalBounds(
            world,
            Fixed.FromInt(100),
            Fixed.FromInt(56),
            Fixed.Zero,
            Fixed.FromInt(128)));

        world.Options.MbfOptions.CompSkull = true;

        Assert.IsFalse(MbfSkullSpawnCompatibility.IsLostSoulOutsideVerticalBounds(
            world,
            Fixed.FromInt(100),
            Fixed.FromInt(56),
            Fixed.Zero,
            Fixed.FromInt(128)));
    }
}
