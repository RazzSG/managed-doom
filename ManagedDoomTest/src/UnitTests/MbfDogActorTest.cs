using ManagedDoom;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfDogActorTest
{
    [TestMethod]
    public void DogKeepsCanonicalMbfTableNumbers()
    {
        Assert.AreEqual(138, (int)Sprite.TNT1);
        Assert.AreEqual(139, (int)Sprite.DOGS);
        Assert.AreEqual(137, (int)MobjType.MbfPushSource);
        Assert.AreEqual(138, (int)MobjType.MbfPullSource);
        Assert.AreEqual(139, (int)MobjType.Dog);
        Assert.AreEqual(967, (int)MobjState.MbfReservedState1);
        Assert.AreEqual(972, (int)MobjState.DogsStnd);
    }

    [TestMethod]
    public void DogDefinitionUsesMbfGameplayValues()
    {
        var info = DoomInfo.MobjInfos[(int)MobjType.Dog];

        Assert.AreEqual(MbfDogActor.DoomEdNum, info.DoomEdNum);
        Assert.AreEqual(MobjState.DogsStnd, info.SpawnState);
        Assert.AreEqual(500, info.SpawnHealth);
        Assert.AreEqual(MobjState.DogsRun1, info.SeeState);
        Assert.AreEqual(8, info.ReactionTime);
        Assert.AreEqual(MobjState.DogsPain, info.PainState);
        Assert.AreEqual(180, info.PainChance);
        Assert.AreEqual(MobjState.DogsAtk1, info.MeleeState);
        Assert.AreEqual(MobjState.DogsDie1, info.DeathState);
        Assert.AreEqual(10, info.Speed);
        Assert.AreEqual(Fixed.FromInt(12).Data, info.Radius.Data);
        Assert.AreEqual(Fixed.FromInt(28).Data, info.Height.Data);
        Assert.AreEqual(100, info.Mass);
        Assert.AreEqual(MobjState.DogsRaise1, info.Raisestate);
        Assert.AreEqual(
            MobjFlags.Solid | MobjFlags.Shootable | MobjFlags.CountKill,
            info.Flags);
    }

    [TestMethod]
    public void DogStatesUseFourteenSpriteFrames()
    {
        Assert.AreEqual(Sprite.DOGS, DoomInfo.States[(int)MobjState.DogsStnd].Sprite);
        Assert.AreEqual(0, DoomInfo.States[(int)MobjState.DogsStnd].Frame);
        Assert.AreEqual(10, DoomInfo.States[(int)MobjState.DogsStnd].Tics);

        Assert.AreEqual(Sprite.DOGS, DoomInfo.States[(int)MobjState.DogsAtk3].Sprite);
        Assert.AreEqual(6, DoomInfo.States[(int)MobjState.DogsAtk3].Frame);
        Assert.AreEqual(8, DoomInfo.States[(int)MobjState.DogsAtk3].Tics);

        Assert.AreEqual(Sprite.DOGS, DoomInfo.States[(int)MobjState.DogsDie6].Sprite);
        Assert.AreEqual(13, DoomInfo.States[(int)MobjState.DogsDie6].Frame);
        Assert.AreEqual(-1, DoomInfo.States[(int)MobjState.DogsDie6].Tics);
        Assert.AreEqual(MobjState.Null, DoomInfo.States[(int)MobjState.DogsDie6].Next);
    }

    [TestMethod]
    public void PlainDoom2ResourcesDoNotClaimDogSprites()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);

        Assert.IsFalse(MbfDogActor.HasRenderableSprites(content.Sprites));
    }
    [TestMethod]
    public void CompleteDogSpriteSetPassesResourceGate()
    {
        Assert.IsTrue(MbfDogActor.HasRenderableSprites(new StubSpriteLookup(14)));
        Assert.IsFalse(MbfDogActor.HasRenderableSprites(new StubSpriteLookup(13)));
    }

    private sealed class StubSpriteLookup : ISpriteLookup
    {
        private readonly SpriteDef dog;
        private readonly SpriteDef empty;

        public StubSpriteLookup(int frameCount)
        {
            var frames = new SpriteFrame[frameCount];
            var patch = DummyData.GetPatch();

            for (var i = 0; i < frames.Length; i++)
            {
                var patches = new Patch[8];
                for (var rotation = 0; rotation < patches.Length; rotation++)
                    patches[rotation] = patch;

                frames[i] = new SpriteFrame(false, patches, new bool[8]);
            }

            dog = new SpriteDef(frames);
            empty = new SpriteDef(System.Array.Empty<SpriteFrame>());
        }

        public SpriteDef this[Sprite sprite] => sprite == Sprite.DOGS ? dog : empty;
    }
}
