using ManagedDoom;
using ManagedDoom.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfProjectileBlockMapRegressionTest
{
    [TestMethod]
    public void UnsetUsesLinkedBlockAfterSpawnCheckStyleCoordinateAdvance()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(
            content,
            new GameOptions { Compatibility = GameCompatibility.Mbf },
            null);

        var blockMap = world.Map.BlockMap;
        var player = world.ConsolePlayer.Mobj;
        var actor = world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Barrel);

        var oldBlockX = blockMap.GetBlockX(actor.X);
        var oldBlockY = blockMap.GetBlockY(actor.Y);
        var oldIndex = blockMap.GetIndex(oldBlockX, oldBlockY);

        Assert.AreNotEqual(-1, oldIndex);
        Assert.AreSame(actor, blockMap.ThingLists[oldIndex],
            "The freshly spawned actor must be the BLOCKMAP head for this regression setup.");

        // P_CheckMissileSpawn advances x/y before P_TryMove. MBFEDIT's grenade
        // clears NOBLOCKMAP, so unlike a vanilla rocket it is linked while this
        // happens. Move only the coordinate here to reproduce that exact unlink
        // precondition without involving weapon or state-table globals.
        if (oldBlockX + 1 < blockMap.Width)
        {
            actor.X += BlockMap.BlockSize;
        }
        else
        {
            Assert.IsTrue(oldBlockX > 0, "Expected an adjacent BLOCKMAP column.");
            actor.X -= BlockMap.BlockSize;
        }

        var coordinateIndex = blockMap.GetIndex(actor.X, actor.Y);
        Assert.AreNotEqual(oldIndex, coordinateIndex);
        Assert.AreNotEqual(-1, coordinateIndex);

        world.ThingMovement.UnsetThingPosition(actor);

        Assert.IsFalse(ContainsThing(blockMap.ThingLists[oldIndex], actor),
            "Unlinking must remove the actor from the block it was linked into, not the block implied by its already-advanced X/Y.");
    }

    private static bool ContainsThing(Mobj head, Mobj expected)
    {
        for (var thing = head; thing != null; thing = thing.BlockNext)
        {
            if (ReferenceEquals(thing, expected))
            {
                return true;
            }
        }

        return false;
    }
}
