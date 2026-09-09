using System;
using System.IO;
using System.Text;
using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Detection;
using ManagedDoom.Compatibility.Mbf;
using ManagedDoom.Compatibility.Mbf.AI;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfOptionsTest
{
    [TestMethod]
    public void DefaultsMatchImplementedMbfBehavior()
    {
        var options = new MbfOptions();

        Assert.IsTrue(options.MonstersRemember);
        Assert.IsTrue(options.MonsterAvoidHazards);
        Assert.IsTrue(options.MonsterFriction);
        Assert.IsTrue(options.MonsterInfighting);
        Assert.IsFalse(options.MonsterBacking);
        Assert.IsFalse(options.CompPursuit);
        Assert.IsFalse(options.CompTelefrag);
        Assert.IsFalse(options.CompDropoff);
        Assert.IsFalse(options.CompVile);
        Assert.IsFalse(options.CompBlazing);
        Assert.IsFalse(options.CompDoorLight);
        Assert.IsFalse(options.CompModel);
        Assert.IsFalse(options.CompGod);
        Assert.IsFalse(options.CompFalloff);
        Assert.IsFalse(options.CompFloors);
        Assert.IsFalse(options.CompSkyMap);
        Assert.IsTrue(options.CompZombie);
        Assert.IsFalse(options.CompStairs);
        Assert.IsFalse(options.CompInfCheat);
        Assert.IsFalse(options.CompZeroTags);
        Assert.IsFalse(options.CompMoveBlock);
        Assert.IsFalse(options.CompSound);
        Assert.IsFalse(options.Comp666);
        Assert.IsFalse(options.CompMaskedAnim);
        Assert.IsFalse(options.CompOuchFace);
        Assert.IsFalse(options.CompMaxHealth);
        Assert.IsFalse(options.CompTranslucency);
        Assert.IsTrue(options.CompFriendlySpawn);
        Assert.IsTrue(options.CompVoodooScroller);
        Assert.IsFalse(options.HasCompVoodooScrollerOverride);
        Assert.IsTrue(options.CompReservedLineFlag);
        Assert.IsFalse(options.CompLedgeBlock);
        Assert.IsFalse(options.HasCompLedgeBlockOverride);
        Assert.IsFalse(options.CompStayLift);
        Assert.IsFalse(options.CompDoorStuck);
        Assert.IsFalse(options.CompPain);
        Assert.IsFalse(options.CompSkull);
        Assert.IsTrue(options.CompRespawn);
        Assert.IsTrue(options.CompSoul);
        Assert.IsFalse(options.HelpFriends);
        Assert.IsTrue(options.DogJumping);
        Assert.IsFalse(options.Monkeys);
        Assert.AreEqual(0, options.PlayerHelpers);
        Assert.AreEqual(128, options.FriendDistance);
        Assert.AreEqual(Fixed.FromInt(128).Data, options.FriendDistanceFixed.Data);
    }

    [TestMethod]
    public void ParserAppliesRecognizedValuesAndIgnoresUnknownEntries()
    {
        var options = MbfOptionsReader.Parse(@"
# MBF options
monsters_remember = 0
monster_avoid_hazards no
monster_friction off
monster_infighting no
monster_backing yes
comp_pursuit yes
comp_telefrag yes
comp_dropoff no
comp_vile yes
comp_blazing yes
comp_doorlight yes
comp_model yes
comp_god yes
comp_falloff yes
comp_floors yes
comp_skymap yes
comp_zombie no
comp_stairs yes
comp_infcheat yes
comp_zerotags yes
comp_moveblock yes
comp_sound yes
comp_666 yes
comp_maskedanim yes
comp_ouchface yes
comp_maxhealth yes
comp_translucency yes
comp_friendlyspawn no
comp_voodooscroller no
comp_reservedlineflag no
comp_ledgeblock yes
comp_staylift yes
comp_doorstuck yes
comp_pain yes
comp_skull yes
comp_respawn no
comp_soul no
help_friends on
dog_jumping off
monkeys yes
player_helpers 2
friend_distance 192
future_option 123
");

        Assert.IsFalse(options.MonstersRemember);
        Assert.IsFalse(options.MonsterAvoidHazards);
        Assert.IsFalse(options.MonsterFriction);
        Assert.IsFalse(options.MonsterInfighting);
        Assert.IsTrue(options.MonsterBacking);
        Assert.IsTrue(options.CompPursuit);
        Assert.IsTrue(options.CompTelefrag);
        Assert.IsFalse(options.CompDropoff);
        Assert.IsTrue(options.CompVile);
        Assert.IsTrue(options.CompBlazing);
        Assert.IsTrue(options.CompDoorLight);
        Assert.IsTrue(options.CompModel);
        Assert.IsTrue(options.CompGod);
        Assert.IsTrue(options.CompFalloff);
        Assert.IsTrue(options.CompFloors);
        Assert.IsTrue(options.CompSkyMap);
        Assert.IsFalse(options.CompZombie);
        Assert.IsTrue(options.CompStairs);
        Assert.IsTrue(options.CompInfCheat);
        Assert.IsTrue(options.CompZeroTags);
        Assert.IsTrue(options.CompMoveBlock);
        Assert.IsTrue(options.CompSound);
        Assert.IsTrue(options.Comp666);
        Assert.IsTrue(options.CompMaskedAnim);
        Assert.IsTrue(options.CompOuchFace);
        Assert.IsTrue(options.CompMaxHealth);
        Assert.IsTrue(options.CompTranslucency);
        Assert.IsFalse(options.CompFriendlySpawn);
        Assert.IsFalse(options.CompVoodooScroller);
        Assert.IsTrue(options.HasCompVoodooScrollerOverride);
        Assert.IsFalse(options.CompReservedLineFlag);
        Assert.IsTrue(options.CompLedgeBlock);
        Assert.IsTrue(options.HasCompLedgeBlockOverride);
        Assert.IsTrue(options.CompStayLift);
        Assert.IsTrue(options.CompDoorStuck);
        Assert.IsTrue(options.CompPain);
        Assert.IsTrue(options.CompSkull);
        Assert.IsFalse(options.CompRespawn);
        Assert.IsFalse(options.CompSoul);
        Assert.IsTrue(options.HelpFriends);
        Assert.IsFalse(options.DogJumping);
        Assert.IsTrue(options.Monkeys);
        Assert.AreEqual(2, options.PlayerHelpers);
        Assert.AreEqual(192, options.FriendDistance);
        Assert.AreEqual(Fixed.FromInt(192).Data, options.FriendDistanceFixed.Data);
    }

    [TestMethod]
    public void ParserUsesLastAssignmentAndSupportsCommonBooleanSpellings()
    {
        var options = MbfOptionsReader.Parse(@"
monsters_remember 0
monsters_remember yes
monster_avoid_hazards false
monster_avoid_hazards on
monster_friction no
monster_friction true
monster_infighting 0
monster_infighting on
monster_backing 1
monster_backing false
comp_pursuit 0
comp_pursuit true
comp_telefrag 1
comp_telefrag false
comp_dropoff 0
comp_dropoff true
comp_vile 1
comp_vile false
comp_blazing 1
comp_blazing false
comp_doorlight 1
comp_doorlight false
comp_model 1
comp_model false
comp_god 1
comp_god false
comp_falloff 1
comp_falloff false
comp_floors 1
comp_floors false
comp_skymap 1
comp_skymap false
comp_zombie 0
comp_zombie true
comp_stairs 1
comp_stairs false
comp_infcheat 1
comp_infcheat false
comp_zerotags 1
comp_zerotags false
comp_moveblock 1
comp_moveblock false
comp_sound 1
comp_sound false
comp_666 1
comp_666 false
comp_maskedanim 1
comp_maskedanim false
comp_ouchface 1
comp_ouchface false
comp_maxhealth 1
comp_maxhealth false
comp_translucency 1
comp_translucency false
comp_friendlyspawn 0
comp_friendlyspawn true
comp_voodooscroller 0
comp_voodooscroller true
comp_reservedlineflag 0
comp_reservedlineflag true
comp_ledgeblock 1
comp_ledgeblock false
comp_staylift 1
comp_staylift false
comp_doorstuck 1
comp_doorstuck false
comp_pain 1
comp_pain false
comp_skull 1
comp_skull false
comp_respawn 0
comp_respawn true
comp_soul 0
comp_soul true
help_friends 1
help_friends no
dog_jumping 0
dog_jumping yes
monkeys 1
monkeys false
player_helpers 1
player_helpers = 3
friend_distance 64
friend_distance = 96 ; later value wins
");

        Assert.IsTrue(options.MonstersRemember);
        Assert.IsTrue(options.MonsterAvoidHazards);
        Assert.IsTrue(options.MonsterFriction);
        Assert.IsTrue(options.MonsterInfighting);
        Assert.IsFalse(options.MonsterBacking);
        Assert.IsTrue(options.CompPursuit);
        Assert.IsFalse(options.CompTelefrag);
        Assert.IsTrue(options.CompDropoff);
        Assert.IsFalse(options.CompVile);
        Assert.IsFalse(options.CompBlazing);
        Assert.IsFalse(options.CompDoorLight);
        Assert.IsFalse(options.CompModel);
        Assert.IsFalse(options.CompGod);
        Assert.IsFalse(options.CompFalloff);
        Assert.IsFalse(options.CompFloors);
        Assert.IsFalse(options.CompSkyMap);
        Assert.IsTrue(options.CompZombie);
        Assert.IsFalse(options.CompStairs);
        Assert.IsFalse(options.CompInfCheat);
        Assert.IsFalse(options.CompZeroTags);
        Assert.IsFalse(options.CompMoveBlock);
        Assert.IsFalse(options.CompSound);
        Assert.IsFalse(options.Comp666);
        Assert.IsFalse(options.CompMaskedAnim);
        Assert.IsFalse(options.CompOuchFace);
        Assert.IsFalse(options.CompMaxHealth);
        Assert.IsFalse(options.CompTranslucency);
        Assert.IsTrue(options.CompFriendlySpawn);
        Assert.IsTrue(options.CompVoodooScroller);
        Assert.IsTrue(options.HasCompVoodooScrollerOverride);
        Assert.IsTrue(options.CompReservedLineFlag);
        Assert.IsFalse(options.CompLedgeBlock);
        Assert.IsTrue(options.HasCompLedgeBlockOverride);
        Assert.IsFalse(options.CompStayLift);
        Assert.IsFalse(options.CompDoorStuck);
        Assert.IsFalse(options.CompPain);
        Assert.IsFalse(options.CompSkull);
        Assert.IsTrue(options.CompRespawn);
        Assert.IsTrue(options.CompSoul);
        Assert.IsFalse(options.HelpFriends);
        Assert.IsTrue(options.DogJumping);
        Assert.IsFalse(options.Monkeys);
        Assert.AreEqual(3, options.PlayerHelpers);
        Assert.AreEqual(96, options.FriendDistance);
    }

    [TestMethod]
    public void InvalidLedgeBlockValueDoesNotCreateExplicitOverride()
    {
        var options = MbfOptionsReader.Parse("comp_ledgeblock maybe");

        Assert.IsFalse(options.CompLedgeBlock);
        Assert.IsFalse(options.HasCompLedgeBlockOverride);
    }

    [TestMethod]
    public void ClonePreservesCompatibilityFlags()
    {
        var options = new MbfOptions
        {
            CompTelefrag = true,
            CompSound = true,
            Comp666 = true,
            CompMaskedAnim = true,
            CompOuchFace = true,
            CompMaxHealth = true,
            CompTranslucency = true,
            CompFriendlySpawn = false,
            CompVoodooScroller = false,
            CompReservedLineFlag = false,
            CompLedgeBlock = true
        };

        var clone = options.Clone();
        Assert.IsTrue(clone.CompTelefrag);
        Assert.IsTrue(clone.CompSound);
        Assert.IsTrue(clone.Comp666);
        Assert.IsTrue(clone.CompMaskedAnim);
        Assert.IsTrue(clone.CompOuchFace);
        Assert.IsTrue(clone.CompMaxHealth);
        Assert.IsTrue(clone.CompTranslucency);
        Assert.IsFalse(clone.CompFriendlySpawn);
        Assert.IsFalse(clone.CompVoodooScroller);
        Assert.IsTrue(clone.HasCompVoodooScrollerOverride);
        Assert.IsFalse(clone.CompReservedLineFlag);
        Assert.IsTrue(clone.CompLedgeBlock);
        Assert.IsTrue(clone.HasCompLedgeBlockOverride);
    }

    [TestMethod]
    public void ClonePreservesAbsentLedgeBlockOverride()
    {
        var options = new MbfOptions();
        var clone = options.Clone();

        Assert.IsFalse(clone.CompLedgeBlock);
        Assert.IsFalse(clone.HasCompLedgeBlockOverride);
        Assert.IsTrue(clone.CompVoodooScroller);
        Assert.IsFalse(clone.HasCompVoodooScrollerOverride);
    }

    [TestMethod]
    public void InvalidVoodooScrollerValueDoesNotCreateExplicitOverride()
    {
        var options = MbfOptionsReader.Parse("comp_voodooscroller maybe");

        Assert.IsTrue(options.CompVoodooScroller);
        Assert.IsFalse(options.HasCompVoodooScrollerOverride);
    }

    [TestMethod]
    public void InvalidReservedLineFlagValueKeepsMbfDefault()
    {
        var options = MbfOptionsReader.Parse("comp_reservedlineflag maybe");

        Assert.IsTrue(options.CompReservedLineFlag);
    }

    [TestMethod]
    public void PlayerHelpersAndFriendDistanceAreClampedToSafeRuntimeRanges()
    {
        var negative = MbfOptionsReader.Parse("player_helpers -5\nfriend_distance -10");
        var huge = MbfOptionsReader.Parse("player_helpers 99\nfriend_distance 999999");

        Assert.AreEqual(0, negative.PlayerHelpers);
        Assert.AreEqual(0, negative.FriendDistance);
        Assert.AreEqual(MbfOptions.MaxPlayerHelpers, huge.PlayerHelpers);
        Assert.AreEqual(999, MbfOptions.MaxFriendDistance);
        Assert.AreEqual(MbfOptions.MaxFriendDistance, huge.FriendDistance);
    }

    [TestMethod]
    public void OptionsLumpSelectsMbfInAutomaticCompatibility()
    {
        var path = WriteWad(("OPTIONS", Encoding.ASCII.GetBytes("friend_distance 96\n")));

        try
        {
            using var wad = new Wad(path);
            var result = CompatibilityDetector.Detect(wad);

            Assert.AreEqual(GameCompatibility.Mbf, result.Compatibility);
            Assert.AreEqual(CompatibilityDetectionSource.FeatureScan, result.Source);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void LastLoadedOptionsLumpWins()
    {
        var first = WriteWad(("OPTIONS", Encoding.ASCII.GetBytes("friend_distance 64\nmonster_friction 0\n")));
        var second = WriteWad(("OPTIONS", Encoding.ASCII.GetBytes("friend_distance 160\nmonster_friction 1\n")));

        try
        {
            using var wad = new Wad(first, second);
            var options = MbfOptionsReader.Read(wad);

            Assert.AreEqual(160, options.FriendDistance);
            Assert.IsTrue(options.MonsterFriction);
        }
        finally
        {
            File.Delete(first);
            File.Delete(second);
        }
    }

    [TestMethod]
    public void GameOptionsLoadsMbfOptionsFromContentWhenMbfIsDetected()
    {
        var path = WriteWad(("OPTIONS", Encoding.ASCII.GetBytes(@"
monsters_remember 0
monster_avoid_hazards 0
monster_friction 0
monster_infighting 0
monster_backing 1
comp_pursuit 1
comp_telefrag 1
comp_dropoff 0
comp_vile 1
comp_blazing 1
comp_doorlight 1
comp_model 1
comp_god 1
comp_falloff 1
comp_floors 1
comp_skymap 1
comp_zombie 0
comp_stairs 1
comp_infcheat 1
comp_zerotags 1
comp_moveblock 1
comp_sound 1
comp_666 1
comp_maskedanim 1
comp_ouchface 1
comp_maxhealth 1
comp_friendlyspawn 0
comp_reservedlineflag 0
comp_staylift 1
comp_doorstuck 1
comp_pain 1
comp_skull 1
comp_respawn 0
comp_soul 0
help_friends 1
dog_jumping 0
monkeys 1
player_helpers 2
friend_distance 72
")));

        try
        {
            using var content = GameContent.CreateDummy(WadPath.Doom2, path);
            var options = new GameOptions(new CommandLineArgs(Array.Empty<string>()), content);

            Assert.AreEqual(GameCompatibility.Mbf, options.Compatibility);
            Assert.IsFalse(options.MbfOptions.MonstersRemember);
            Assert.IsFalse(options.MbfOptions.MonsterAvoidHazards);
            Assert.IsFalse(options.MbfOptions.MonsterFriction);
            Assert.IsFalse(options.MbfOptions.MonsterInfighting);
            Assert.IsTrue(options.MbfOptions.MonsterBacking);
            Assert.IsTrue(options.MbfOptions.CompPursuit);
            Assert.IsTrue(options.MbfOptions.CompTelefrag);
            Assert.IsFalse(options.MbfOptions.CompDropoff);
            Assert.IsTrue(options.MbfOptions.CompVile);
            Assert.IsTrue(options.MbfOptions.CompBlazing);
            Assert.IsTrue(options.MbfOptions.CompDoorLight);
            Assert.IsTrue(options.MbfOptions.CompModel);
            Assert.IsTrue(options.MbfOptions.CompGod);
            Assert.IsTrue(options.MbfOptions.CompFalloff);
            Assert.IsTrue(options.MbfOptions.CompFloors);
            Assert.IsTrue(options.MbfOptions.CompSkyMap);
            Assert.IsFalse(options.MbfOptions.CompZombie);
            Assert.IsTrue(options.MbfOptions.CompStairs);
            Assert.IsTrue(options.MbfOptions.CompInfCheat);
            Assert.IsTrue(options.MbfOptions.CompZeroTags);
            Assert.IsTrue(options.MbfOptions.CompMoveBlock);
            Assert.IsTrue(options.MbfOptions.CompSound);
            Assert.IsTrue(options.MbfOptions.Comp666);
            Assert.IsTrue(options.MbfOptions.CompMaskedAnim);
            Assert.IsTrue(options.MbfOptions.CompOuchFace);
            Assert.IsTrue(options.MbfOptions.CompMaxHealth);
            Assert.IsFalse(options.MbfOptions.CompFriendlySpawn);
            Assert.IsFalse(options.MbfOptions.CompReservedLineFlag);
            Assert.IsTrue(options.MbfOptions.CompStayLift);
            Assert.IsTrue(options.MbfOptions.CompDoorStuck);
            Assert.IsTrue(options.MbfOptions.CompPain);
            Assert.IsTrue(options.MbfOptions.CompSkull);
            Assert.IsFalse(options.MbfOptions.CompRespawn);
            Assert.IsFalse(options.MbfOptions.CompSoul);
            Assert.IsTrue(options.MbfOptions.HelpFriends);
            Assert.IsFalse(options.MbfOptions.DogJumping);
            Assert.IsTrue(options.MbfOptions.Monkeys);
            Assert.AreEqual(2, options.MbfOptions.PlayerHelpers);
            Assert.AreEqual(72, options.MbfOptions.FriendDistance);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void RuntimeFeatureHelpersCanBeDisabledByOptions()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var actor = world.ThingAllocation.SpawnMobj(
            world.ConsolePlayer.Mobj.X,
            world.ConsolePlayer.Mobj.Y,
            world.ConsolePlayer.Mobj.Z,
            MobjType.Troop);

        Assert.IsFalse(MbfMonsterFriction.Applies(GameCompatibility.Mbf, false, actor));
        Assert.IsFalse(MbfMonsterHazardAvoidance.Applies(GameCompatibility.Mbf, false));

        actor.LastEnemy = world.ConsolePlayer.Mobj;
        Assert.IsFalse(MbfMonsterTargetMemory.TryRestorePreviousEnemy(
            GameCompatibility.Mbf,
            false,
            actor));
    }

    private static string WriteWad(params (string Name, byte[] Data)[] lumps)
    {
        var path = Path.Combine(Path.GetTempPath(), $"manageddoom_mbf_options_{Guid.NewGuid():N}.wad");
        using var stream = File.Create(path);
        using var writer = new BinaryWriter(stream);

        writer.Write(Encoding.ASCII.GetBytes("PWAD"));
        writer.Write(lumps.Length);
        writer.Write(0);

        var positions = new int[lumps.Length];
        for (var i = 0; i < lumps.Length; i++)
        {
            positions[i] = (int)stream.Position;
            writer.Write(lumps[i].Data);
        }

        var directoryOffset = (int)stream.Position;
        for (var i = 0; i < lumps.Length; i++)
        {
            writer.Write(positions[i]);
            writer.Write(lumps[i].Data.Length);

            var name = new byte[8];
            Encoding.ASCII.GetBytes(lumps[i].Name).CopyTo(name, 0);
            writer.Write(name);
        }

        stream.Position = 8;
        writer.Write(directoryOffset);
        return path;
    }
}
