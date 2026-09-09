using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfReservedLineFlagCompatibilityTest
{
    [TestMethod]
    public void EnabledReservedFlagClearsAllExtendedFlags()
    {
        var line = CreateLine((LineFlags)0x3e01);

        MbfReservedLineFlagCompatibility.Apply(
            new[] { line },
            GameCompatibility.Mbf,
            compReservedLineFlag: true);

        Assert.AreEqual(0x0001, (int)line.Flags);
    }

    [TestMethod]
    public void EnabledWithoutReservedFlagKeepsExtendedFlags()
    {
        var line = CreateLine((LineFlags)0x3201);

        MbfReservedLineFlagCompatibility.Apply(
            new[] { line },
            GameCompatibility.Mbf21,
            compReservedLineFlag: true);

        Assert.AreEqual(0x3201, (int)line.Flags);
    }

    [TestMethod]
    public void DisabledReservedFlagIsInert()
    {
        var line = CreateLine((LineFlags)0x2a01);

        MbfReservedLineFlagCompatibility.Apply(
            new[] { line },
            GameCompatibility.Mbf,
            compReservedLineFlag: false);

        Assert.AreEqual(0x2a01, (int)line.Flags);
    }

    [TestMethod]
    public void VanillaAndBoomAreNotChangedByMbfCompatibilityPass()
    {
        foreach (var compatibility in new[] { GameCompatibility.Vanilla, GameCompatibility.Boom })
        {
            var line = CreateLine((LineFlags)0x0a01);

            MbfReservedLineFlagCompatibility.Apply(
                new[] { line },
                compatibility,
                compReservedLineFlag: true);

            Assert.AreEqual(0x0a01, (int)line.Flags, compatibility.ToString());
        }
    }

    [TestMethod]
    public void OriginalDoomFlagsArePreservedExactly()
    {
        var line = CreateLine((LineFlags)0x09ff);

        MbfReservedLineFlagCompatibility.Apply(
            new[] { line },
            GameCompatibility.Mbf21,
            compReservedLineFlag: true);

        Assert.AreEqual(0x01ff, (int)line.Flags);
    }

    private static LineDef CreateLine(LineFlags flags)
    {
        var a = new Vertex(Fixed.Zero, Fixed.Zero);
        var b = new Vertex(Fixed.FromInt(64), Fixed.Zero);
        return new LineDef(a, b, flags, (LineSpecial)0, 0, null, null);
    }
}
