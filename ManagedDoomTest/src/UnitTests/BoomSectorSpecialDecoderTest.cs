using ManagedDoom;
using ManagedDoom.Compatibility.Boom.Sectors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class BoomSectorSpecialDecoderTest
{
    [TestMethod]
    public void ZeroDecodesToNoEffects()
    {
        var decoded = BoomSectorSpecialDecoder.Decode(0);

        Assert.AreEqual(0, decoded.RawValue);
        Assert.AreEqual(0, decoded.LightingSpecial);
        Assert.AreEqual(BoomSectorDamage.None, decoded.Damage);
        Assert.AreEqual(0, decoded.DamageAmount);
        Assert.IsFalse(decoded.IsSecret);
        Assert.IsFalse(decoded.FrictionEnabled);
        Assert.IsFalse(decoded.PusherEnabled);
    }

    [DataTestMethod]
    [DataRow(0x20, BoomSectorDamage.Five, 5)]
    [DataRow(0x40, BoomSectorDamage.Ten, 10)]
    [DataRow(0x60, BoomSectorDamage.Twenty, 20)]
    public void DamageBitsDecodeIndependently(int raw, BoomSectorDamage expectedDamage, int expectedAmount)
    {
        var decoded = BoomSectorSpecialDecoder.Decode(raw);

        Assert.AreEqual(expectedDamage, decoded.Damage);
        Assert.AreEqual(expectedAmount, decoded.DamageAmount);
        Assert.AreEqual(0, decoded.LightingSpecial);
        Assert.IsFalse(decoded.IsSecret);
        Assert.IsFalse(decoded.FrictionEnabled);
        Assert.IsFalse(decoded.PusherEnabled);
    }

    [TestMethod]
    public void CombinedPropertiesRemainIndependent()
    {
        const int lighting = 17;
        var raw = lighting |
            BoomSectorSpecialDecoder.DamageMask |
            BoomSectorSpecialDecoder.SecretMask |
            BoomSectorSpecialDecoder.FrictionMask |
            BoomSectorSpecialDecoder.PusherMask;

        var decoded = BoomSectorSpecialDecoder.Decode(raw);

        Assert.AreEqual(raw, decoded.RawValue);
        Assert.AreEqual(lighting, decoded.LightingSpecial);
        Assert.AreEqual(BoomSectorDamage.Twenty, decoded.Damage);
        Assert.AreEqual(20, decoded.DamageAmount);
        Assert.IsTrue(decoded.IsSecret);
        Assert.IsTrue(decoded.FrictionEnabled);
        Assert.IsTrue(decoded.PusherEnabled);
    }

    [TestMethod]
    public void LightingUsesOnlyLowFiveBits()
    {
        const int lighting = 13;
        var raw = lighting |
            BoomSectorSpecialDecoder.DamageMask |
            BoomSectorSpecialDecoder.SecretMask |
            BoomSectorSpecialDecoder.FrictionMask |
            BoomSectorSpecialDecoder.PusherMask;

        var decoded = BoomSectorSpecialDecoder.Decode(raw);

        Assert.AreEqual(lighting, decoded.LightingSpecial);
    }

    [TestMethod]
    public void ReservedSoundBitsDoNotChangeImplementedProperties()
    {
        var baseRaw = 8 |
            0x40 |
            BoomSectorSpecialDecoder.SecretMask |
            BoomSectorSpecialDecoder.FrictionMask |
            BoomSectorSpecialDecoder.PusherMask;
        var decoded = BoomSectorSpecialDecoder.Decode(baseRaw);
        var withReserved = BoomSectorSpecialDecoder.Decode(baseRaw | BoomSectorSpecialDecoder.ReservedSoundMask);

        Assert.AreEqual(decoded.LightingSpecial, withReserved.LightingSpecial);
        Assert.AreEqual(decoded.Damage, withReserved.Damage);
        Assert.AreEqual(decoded.IsSecret, withReserved.IsSecret);
        Assert.AreEqual(decoded.FrictionEnabled, withReserved.FrictionEnabled);
        Assert.AreEqual(decoded.PusherEnabled, withReserved.PusherEnabled);
        Assert.AreEqual(baseRaw | BoomSectorSpecialDecoder.ReservedSoundMask, withReserved.RawValue);
    }

    [TestMethod]
    public void SectorSpecialOverloadPreservesRawValue()
    {
        var raw = 3 |
            0x20 |
            BoomSectorSpecialDecoder.SecretMask |
            BoomSectorSpecialDecoder.PusherMask;

        var decoded = BoomSectorSpecialDecoder.Decode((SectorSpecial)raw);

        Assert.AreEqual(raw, decoded.RawValue);
        Assert.AreEqual(3, decoded.LightingSpecial);
        Assert.AreEqual(BoomSectorDamage.Five, decoded.Damage);
        Assert.IsTrue(decoded.IsSecret);
        Assert.IsFalse(decoded.FrictionEnabled);
        Assert.IsTrue(decoded.PusherEnabled);
    }

    [TestMethod]
    public void ClearLightingSpecialPreservesGeneralizedBits()
    {
        var flags = BoomSectorSpecialDecoder.DamageMask |
            BoomSectorSpecialDecoder.SecretMask |
            BoomSectorSpecialDecoder.FrictionMask |
            BoomSectorSpecialDecoder.PusherMask;
        var special = (SectorSpecial)(17 | flags);

        var cleared = BoomSectorSpecialDecoder.ClearLightingSpecial(special);

        Assert.AreEqual(flags, (int)cleared);
    }

}
