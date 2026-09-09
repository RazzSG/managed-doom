using ManagedDoom;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class SpriteDefTest
{
    [TestMethod]
    public void EmptySpriteDefinitionSafelyRejectsFrameZero()
    {
        var sprite = new SpriteDef(System.Array.Empty<SpriteFrame>());

        Assert.IsFalse(sprite.TryGetFrame(0, out var frame));
        Assert.IsNull(frame);
    }

    [TestMethod]
    public void ValidSpriteDefinitionReturnsRequestedFrame()
    {
        var expected = new SpriteFrame(false, new Patch[8], new bool[8]);
        var sprite = new SpriteDef(new[] { expected });

        Assert.IsTrue(sprite.TryGetFrame(0, out var actual));
        Assert.AreSame(expected, actual);
        Assert.IsFalse(sprite.TryGetFrame(1, out _));
    }
}
