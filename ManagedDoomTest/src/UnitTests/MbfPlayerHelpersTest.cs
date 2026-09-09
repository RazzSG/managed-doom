using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf;
using ManagedDoom.Compatibility.Mbf.AI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfPlayerHelpersTest
{
    [TestMethod]
    public void SinglePlayerUsesConfiguredCoopStartsInPlayerOrder()
    {
        var players = CreatePlayers();
        players[0].InGame = true;
        var starts = CreateStarts();
        var options = new MbfOptions { PlayerHelpers = 2 };

        var result = MbfPlayerHelpers.CollectSpawnStarts(
            GameCompatibility.Mbf,
            options,
            players,
            starts,
            netGame: false,
            deathmatch: 0);

        Assert.AreEqual(2, result.Count);
        Assert.AreSame(starts[1], result[0]);
        Assert.AreSame(starts[2], result[1]);
    }

    [TestMethod]
    public void MissingConfiguredStartsAreSkippedWithoutUsingPlayerOne()
    {
        var players = CreatePlayers();
        players[0].InGame = true;
        players[2].InGame = true; // Synthetic state must not change MBF slot numbering.
        var starts = CreateStarts();
        starts[1] = null;
        var options = new MbfOptions { PlayerHelpers = 3 };

        var result = MbfPlayerHelpers.CollectSpawnStarts(
            GameCompatibility.Mbf,
            options,
            players,
            starts,
            netGame: false,
            deathmatch: 0);

        Assert.AreEqual(2, result.Count);
        Assert.AreSame(starts[2], result[0]);
        Assert.AreSame(starts[3], result[1]);
        Assert.AreNotSame(starts[0], result[0]);
        Assert.AreNotSame(starts[0], result[1]);
    }

    [TestMethod]
    public void BoomDoesNotEnablePlayerHelpers()
    {
        var players = CreatePlayers();
        players[0].InGame = true;
        var options = new MbfOptions { PlayerHelpers = 3 };

        var result = MbfPlayerHelpers.CollectSpawnStarts(
            GameCompatibility.Boom,
            options,
            players,
            CreateStarts(),
            netGame: false,
            deathmatch: 0);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void NetworkAndDeathmatchGamesDoNotReserveHelperStarts()
    {
        var players = CreatePlayers();
        players[0].InGame = true;
        var starts = CreateStarts();
        var options = new MbfOptions { PlayerHelpers = 3 };

        Assert.AreEqual(0, MbfPlayerHelpers.CollectSpawnStarts(
            GameCompatibility.Mbf,
            options,
            players,
            starts,
            netGame: true,
            deathmatch: 0).Count);

        Assert.AreEqual(0, MbfPlayerHelpers.CollectSpawnStarts(
            GameCompatibility.Mbf,
            options,
            players,
            starts,
            netGame: false,
            deathmatch: 1).Count);
    }

    [TestMethod]
    public void WorldPreparesHelperSpawnSlotsAfterMapThingsAreLoaded()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.Players[0].Reborn();
        options.MbfOptions.PlayerHelpers = 3;

        var world = new World(content, options, null);

        Assert.IsTrue(world.ThingAllocation.MbfPlayerHelperStarts.Count > 0);
        Assert.IsTrue(world.ThingAllocation.MbfPlayerHelperStarts.Count <= 3);
        foreach (var start in world.ThingAllocation.MbfPlayerHelperStarts)
        {
            Assert.IsNotNull(start);
            Assert.IsTrue(start.Type >= 2 && start.Type <= 4);
        }

        // Doom II does not contain MBF's bundled DOGS graphics. Phase 19.12
        // must preserve normal IWAD loading instead of spawning an actor that
        // the renderer cannot draw.
        Assert.AreEqual(0, world.ThingAllocation.MbfPlayerHelperActors.Count);
    }

    [TestMethod]
    public void RuntimeSpawnUsesFirstHelperStartWhenDogResourcesExist()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.Players[0].Reborn();
        options.MbfOptions.PlayerHelpers = 1;

        var world = new World(content, options, null);
        var allocation = new ThingAllocation(world, new CompleteDogSpriteLookup());
        var player = world.ConsolePlayer.Mobj;

        var first = new MapThing(
            player.X,
            player.Y,
            Angle.Ang0,
            2,
            ThingFlags.Normal);
        var duplicate = new MapThing(
            player.X + Fixed.FromInt(8),
            player.Y,
            Angle.Ang90,
            2,
            ThingFlags.Normal);

        allocation.SpawnMapThing(first);
        allocation.SpawnMapThing(duplicate);
        allocation.PrepareMbfPlayerHelpers();

        var killsBefore = world.TotalKills;
        allocation.SpawnMbfPlayerHelpers();

        Assert.AreEqual(1, allocation.MbfPlayerHelperStarts.Count);
        Assert.AreSame(first, allocation.MbfPlayerHelperStarts[0]);
        Assert.AreEqual(1, allocation.MbfPlayerHelperActors.Count);

        var dog = allocation.MbfPlayerHelperActors[0];
        Assert.AreEqual(MobjType.Dog, dog.Type);
        Assert.AreSame(first, dog.SpawnPoint);
        Assert.IsTrue((dog.Flags & MobjFlags.Friend) != 0);
        Assert.AreEqual(killsBefore, world.TotalKills);
    }

    private sealed class CompleteDogSpriteLookup : ISpriteLookup
    {
        private readonly SpriteDef dog;
        private readonly SpriteDef empty = new SpriteDef(System.Array.Empty<SpriteFrame>());

        public CompleteDogSpriteLookup()
        {
            var frames = new SpriteFrame[MbfDogActor.RequiredSpriteFrameCount];
            var patch = DummyData.GetPatch();

            for (var i = 0; i < frames.Length; i++)
            {
                var patches = new Patch[8];
                for (var rotation = 0; rotation < patches.Length; rotation++)
                    patches[rotation] = patch;

                frames[i] = new SpriteFrame(false, patches, new bool[8]);
            }

            dog = new SpriteDef(frames);
        }

        public SpriteDef this[Sprite sprite] => sprite == Sprite.DOGS ? dog : empty;
    }

    private static Player[] CreatePlayers()
    {
        var players = new Player[Player.MaxPlayerCount];
        for (var i = 0; i < players.Length; i++)
            players[i] = new Player(i);
        return players;
    }

    private static MapThing[] CreateStarts()
    {
        var starts = new MapThing[Player.MaxPlayerCount];
        for (var i = 0; i < starts.Length; i++)
        {
            starts[i] = new MapThing(
                Fixed.FromInt(i * 64),
                Fixed.Zero,
                Angle.Ang0,
                i + 1,
                ThingFlags.Normal);
        }
        return starts;
    }
}
