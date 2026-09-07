using ManagedDoom.Compatibility.Boom.Lines;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomTriggerTypeTest
{
    [TestMethod]
    public void TriggerTypesMatchGeneralizedBoomBitLayout()
    {
        var cases = new[]
        {
            (BoomTriggerType.WalkOnce, 0, false, true, false, false, false),
            (BoomTriggerType.WalkRepeat, 1, true, true, false, false, false),
            (BoomTriggerType.SwitchOnce, 2, false, false, true, false, false),
            (BoomTriggerType.SwitchRepeat, 3, true, false, true, false, false),
            (BoomTriggerType.GunOnce, 4, false, false, false, true, false),
            (BoomTriggerType.GunRepeat, 5, true, false, false, true, false),
            (BoomTriggerType.PushOnce, 6, false, false, false, false, true),
            (BoomTriggerType.PushRepeat, 7, true, false, false, false, true)
        };

        foreach (var testCase in cases)
        {
            Assert.AreEqual(testCase.Item2, (int)testCase.Item1);
            Assert.AreEqual(testCase.Item3, testCase.Item1.IsRepeatable());
            Assert.AreEqual(testCase.Item4, testCase.Item1.IsWalk());
            Assert.AreEqual(testCase.Item5, testCase.Item1.IsSwitch());
            Assert.AreEqual(testCase.Item6, testCase.Item1.IsGun());
            Assert.AreEqual(testCase.Item7, testCase.Item1.IsPush());
        }
    }
}
