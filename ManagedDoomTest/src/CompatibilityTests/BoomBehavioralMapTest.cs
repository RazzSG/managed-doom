using System;
using System.IO;
using ManagedDoom;
using ManagedDoom.Compatibility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.CompatibilityTests;

/// <summary>
/// Phase 4.68 behavioral Boom tests. Unlike the focused unit tests, these
/// scenarios load Boom specials from a real PWAD and exercise the normal
/// Map -> dispatcher/static-special -> thinker -> tic pipeline.
/// </summary>
[TestClass]
public sealed class BoomBehavioralMapTest
{
    private static readonly string BehaviorWadPath = Path.Combine("data", "boom_behavior_test.wad");

    [TestMethod]
    public void GeneralizedFloorLoadedFromMapActivatesAndReachesDestination()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2, BehaviorWadPath);
        var harness = CreateBoomGame(content, 1);
        var world = harness.Game.World;
        var line = world.Map.Lines[4];
        var sector = line.BackSector;

        Assert.AreEqual(0x634E, (int)line.Special);
        Assert.IsNotNull(sector);
        var startHeight = sector.FloorHeight;
        Assert.IsTrue(world.MapInteraction.UseSpecialLine(world.ConsolePlayer.Mobj, line, 0));
        Assert.AreEqual(0, (int)line.Special, "PushOnce should be consumed after successful activation.");

        RunUntil(harness.Game, harness.Commands, () => sector.SpecialData == null, 128);

        Assert.AreEqual((startHeight + Fixed.FromInt(24)).Data, sector.FloorHeight.Data);
    }

    [TestMethod]
    public void GeneralizedDoorLoadedFromMapOpensAndFinishesItsThinker()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2, BehaviorWadPath);
        var harness = CreateBoomGame(content, 2);
        var world = harness.Game.World;
        var line = world.Map.Lines[0];
        var sector = line.BackSector;

        Assert.AreEqual(0x3C2E, (int)line.Special);
        Assert.IsNotNull(sector);
        Assert.AreEqual(Fixed.Zero.Data, sector.CeilingHeight.Data);
        Assert.IsTrue(world.MapInteraction.UseSpecialLine(world.ConsolePlayer.Mobj, line, 0));
        Assert.AreEqual(0, (int)line.Special, "PushOnce should be consumed after successful activation.");

        RunUntil(harness.Game, harness.Commands, () => sector.SpecialData == null, 128);

        Assert.AreEqual(Fixed.FromInt(124).Data, sector.CeilingHeight.Data);
    }

    [TestMethod]
    public void SilentTeleportLoadedFromMapPreservesHeightAndVerticalMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2, BehaviorWadPath);
        var harness = CreateBoomGame(content, 3);
        var world = harness.Game.World;
        var line = world.Map.Lines[5];
        var thing = world.ConsolePlayer.Mobj;
        var destination = FindTeleportDestination(world, line.Tag);

        Assert.AreEqual(207, (int)line.Special);
        Assert.IsNotNull(destination);

        thing.Z = thing.FloorZ + Fixed.FromInt(8);
        thing.MomX = Fixed.FromInt(3);
        thing.MomY = Fixed.FromInt(1);
        thing.MomZ = Fixed.FromInt(2);
        thing.ReactionTime = 7;

        var heightAboveFloor = thing.Z - thing.FloorZ;

        world.MapInteraction.CrossSpecialLine(line, 0, thing);

        Assert.AreEqual(0, (int)line.Special, "WalkOnce should be consumed after successful activation.");
        Assert.AreEqual(destination.X.Data, thing.X.Data);
        Assert.AreEqual(destination.Y.Data, thing.Y.Data);
        Assert.AreEqual(heightAboveFloor.Data, (thing.Z - thing.FloorZ).Data);
        Assert.AreEqual(Fixed.FromInt(2).Data, thing.MomZ.Data);
        Assert.AreEqual(7, thing.ReactionTime);
    }

    [TestMethod]
    public void StaticFloorScrollerLoadedFromMapAdvancesEveryWorldTic()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2, BehaviorWadPath);
        var harness = CreateBoomGame(content, 4);
        var world = harness.Game.World;
        var line = world.Map.Lines[0];
        var sector = world.ConsolePlayer.Mobj.Subsector.Sector;

        Assert.AreEqual(251, (int)line.Special);
        Assert.AreEqual(32000, sector.Tag);
        var startX = sector.FloorXOffset;
        var startY = sector.FloorYOffset;
        var stepX = -(line.Dx >> 5);
        var stepY = line.Dy >> 5;

        for (var i = 0; i < 3; i++)
            harness.Game.Update(harness.Commands);

        Assert.AreEqual((startX + stepX * 3).Data, sector.FloorXOffset.Data);
        Assert.AreEqual((startY + stepY * 3).Data, sector.FloorYOffset.Data);
    }

    [TestMethod]
    public void GeneralizedDamageAndSecretLoadedFromMapWorkTogether()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2, BehaviorWadPath);
        var harness = CreateBoomGame(content, 5);
        var world = harness.Game.World;
        var player = world.ConsolePlayer;
        var sector = player.Mobj.Subsector.Sector;

        // CreateBoomGame advances the freshly loaded map by one real game tic.
        // The generalized sector must therefore already have applied both parts.
        Assert.AreEqual(1, world.TotalSecrets);
        Assert.AreEqual(90, player.Health);
        Assert.AreEqual(90, player.Mobj.Health);
        Assert.AreEqual(1, player.SecretCount);
        Assert.AreEqual(0x40, (int)sector.Special, "The secret bit should clear while generalized damage remains active.");
    }

    private static (DoomGame Game, TicCmd[] Commands) CreateBoomGame(GameContent content, int map)
    {
        var options = new GameOptions
        {
            Compatibility = GameCompatibility.Boom,
            GameVersion = GameVersion.Version109,
            GameMode = GameMode.Commercial,
            Map = map,
            NoMonsters = true
        };

        var game = new DoomGame(content, options);
        var commands = CreateCommands();
        game.DeferedInitNew();
        game.Update(commands);
        return (game, commands);
    }

    private static TicCmd[] CreateCommands()
    {
        var commands = new TicCmd[Player.MaxPlayerCount];
        for (var i = 0; i < commands.Length; i++)
            commands[i] = new TicCmd();

        return commands;
    }

    private static void RunUntil(DoomGame game, TicCmd[] commands, Func<bool> completed, int maxTics)
    {
        for (var i = 0; i < maxTics && !completed(); i++)
            game.Update(commands);

        Assert.IsTrue(completed(), $"Behavior did not finish within {maxTics} tics.");
    }

    private static Mobj FindTeleportDestination(World world, short tag)
    {
        foreach (var thinker in world.Thinkers)
        {
            if (thinker is Mobj thing &&
                thing.Type == MobjType.Teleportman &&
                thing.Subsector.Sector.Tag == tag)
            {
                return thing;
            }
        }

        return null;
    }
}
