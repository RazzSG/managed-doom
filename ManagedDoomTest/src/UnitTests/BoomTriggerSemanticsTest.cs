using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTriggerSemanticsTest
{
    [TestMethod]
    public void TriggerPairsMapToBoomActivationChannels()
    {
        var cases = new[]
        {
            (BoomTriggerType.WalkOnce, BoomActivationChannel.Walk, false),
            (BoomTriggerType.WalkRepeat, BoomActivationChannel.Walk, true),
            (BoomTriggerType.SwitchOnce, BoomActivationChannel.Switch, false),
            (BoomTriggerType.SwitchRepeat, BoomActivationChannel.Switch, true),
            (BoomTriggerType.GunOnce, BoomActivationChannel.Gun, false),
            (BoomTriggerType.GunRepeat, BoomActivationChannel.Gun, true),
            (BoomTriggerType.PushOnce, BoomActivationChannel.Push, false),
            (BoomTriggerType.PushRepeat, BoomActivationChannel.Push, true)
        };

        foreach (var item in cases)
        {
            Assert.AreEqual(item.Item2, BoomTriggerSemantics.GetChannel(item.Item1));
            Assert.AreEqual(item.Item3, BoomTriggerSemantics.IsRepeatable(item.Item1));
        }
    }

    [TestMethod]
    public void WalkTriggersOnlyMatchCrossing()
    {
        foreach (var trigger in new[] { BoomTriggerType.WalkOnce, BoomTriggerType.WalkRepeat })
        {
            Assert.IsTrue(BoomTriggerSemantics.CanActivateFromCross(trigger));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromUse(trigger, 0));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromShoot(trigger));
        }
    }

    [TestMethod]
    public void SwitchTriggersOnlyMatchFrontSideUse()
    {
        foreach (var trigger in new[] { BoomTriggerType.SwitchOnce, BoomTriggerType.SwitchRepeat })
        {
            Assert.IsTrue(BoomTriggerSemantics.CanActivateFromSwitchUse(trigger, 0));
            Assert.IsTrue(BoomTriggerSemantics.CanActivateFromUse(trigger, 0));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromSwitchUse(trigger, 1));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromUse(trigger, 1));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromCross(trigger));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromShoot(trigger));
        }
    }

    [TestMethod]
    public void GunTriggersOnlyMatchShooting()
    {
        foreach (var trigger in new[] { BoomTriggerType.GunOnce, BoomTriggerType.GunRepeat })
        {
            Assert.IsTrue(BoomTriggerSemantics.CanActivateFromShoot(trigger));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromCross(trigger));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromUse(trigger, 0));
        }
    }

    [TestMethod]
    public void PushTriggersOnlyMatchFrontSideUseAndDoNotUseTagForTargeting()
    {
        foreach (var trigger in new[] { BoomTriggerType.PushOnce, BoomTriggerType.PushRepeat })
        {
            Assert.IsTrue(BoomTriggerSemantics.CanActivateFromPushUse(trigger, 0));
            Assert.IsTrue(BoomTriggerSemantics.CanActivateFromUse(trigger, 0));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromPushUse(trigger, 1));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromUse(trigger, 1));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromCross(trigger));
            Assert.IsFalse(BoomTriggerSemantics.CanActivateFromShoot(trigger));
            Assert.IsFalse(BoomTriggerSemantics.UsesTagForTargeting(trigger));
        }

        foreach (var trigger in new[]
                 {
                     BoomTriggerType.WalkOnce,
                     BoomTriggerType.WalkRepeat,
                     BoomTriggerType.SwitchOnce,
                     BoomTriggerType.SwitchRepeat,
                     BoomTriggerType.GunOnce,
                     BoomTriggerType.GunRepeat
                 })
        {
            Assert.IsTrue(BoomTriggerSemantics.UsesTagForTargeting(trigger));
        }
    }
}
