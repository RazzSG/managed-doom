using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomActionSpecificationTest
{
    [TestMethod]
    public void CommonGeneralizedFieldsDecodeToSemanticValues()
    {
        var pushTurbo = (LineSpecial)(0x6000 | (3 << 3) | (int)BoomTriggerType.PushRepeat);
        var pushSpec = BoomGeneralizedSpecial.DecodeCommon(pushTurbo, true);

        Assert.AreEqual(BoomTriggerType.PushRepeat, pushSpec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Turbo, pushSpec.Speed);
        Assert.IsTrue(pushSpec.AllowsMonsters);
        Assert.IsTrue(pushSpec.Repeatable);
        Assert.IsFalse(pushSpec.UsesTagForTargeting);

        var switchNormal = (LineSpecial)(0x4000 | (1 << 3) | (int)BoomTriggerType.SwitchOnce);
        var switchSpec = BoomGeneralizedSpecial.DecodeCommon(switchNormal, false);

        Assert.AreEqual(BoomTriggerType.SwitchOnce, switchSpec.Trigger);
        Assert.AreEqual(BoomActionSpeed.Normal, switchSpec.Speed);
        Assert.IsFalse(switchSpec.AllowsMonsters);
        Assert.IsFalse(switchSpec.Repeatable);
        Assert.IsTrue(switchSpec.UsesTagForTargeting);

        Assert.AreEqual(-1, (int)BoomPlaneDirection.Down);
        Assert.AreEqual(1, (int)BoomPlaneDirection.Up);
        Assert.AreEqual(1, (int)BoomChangeType.TextureAndZeroSpecial);
        Assert.AreEqual(2, (int)BoomChangeType.TextureOnly);
        Assert.AreEqual(3, (int)BoomChangeType.TextureAndSpecial);
        Assert.AreEqual(0, (int)BoomModelType.Trigger);
        Assert.AreEqual(1, (int)BoomModelType.Numeric);
    }
}
