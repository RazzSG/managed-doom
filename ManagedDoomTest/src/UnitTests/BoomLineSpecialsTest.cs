using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomLineSpecialsTest
{
    [TestMethod]
    public void GeneralizedSpecialsDecodeTriggerFromLowThreeBits()
    {
        for (var i = 0; i < 8; i++)
        {
            Assert.IsTrue(BoomLineSpecials.TryGetGeneralizedTrigger((LineSpecial)(0x2F80 + i), out var trigger));
            Assert.AreEqual((BoomTriggerType)i, trigger);
        }

        Assert.IsTrue(BoomLineSpecials.TryGetGeneralizedTrigger((LineSpecial)0x7FFF, out var lastTrigger));
        Assert.AreEqual(BoomTriggerType.PushRepeat, lastTrigger);

        Assert.IsFalse(BoomLineSpecials.TryGetGeneralizedTrigger((LineSpecial)0x2F7F, out _));
        Assert.IsFalse(BoomLineSpecials.TryGetGeneralizedTrigger((LineSpecial)0x8000, out _));
    }
}
