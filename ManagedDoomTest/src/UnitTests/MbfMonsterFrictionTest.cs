using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Boom.Friction;
using ManagedDoom.Compatibility.Mbf.Movement;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfMonsterFrictionTest
{
    [TestMethod]
    public void MbfLivingMonsterUsesResolvedFloorFriction()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var monster = SpawnGroundedMonster(world);
        var sector = monster.Subsector.Sector;
        var custom = new Fixed(0xd800);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = custom;

        Assert.IsTrue(MbfMonsterFriction.Applies(GameCompatibility.Mbf, monster));
        Assert.AreEqual(custom.Data, MbfMonsterFriction.GetCoastingFriction(GameCompatibility.Mbf, monster).Data);
    }

    [TestMethod]
    public void BoomMonsterKeepsOriginalFriction()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var monster = SpawnGroundedMonster(world);
        var sector = monster.Subsector.Sector;

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xd800);

        Assert.IsFalse(MbfMonsterFriction.Applies(GameCompatibility.Boom, monster));
        Assert.AreEqual(
            BoomFrictionTranslator.OriginalFrictionData,
            MbfMonsterFriction.GetCoastingFriction(GameCompatibility.Boom, monster).Data);
    }

    [TestMethod]
    public void DeadOrNonSentientObjectsDoNotGainMbfMonsterFriction()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var monster = SpawnGroundedMonster(world);

        monster.Health = 0;
        Assert.IsFalse(MbfMonsterFriction.Applies(GameCompatibility.Mbf, monster));

        var objectThing = world.ThingAllocation.SpawnMobj(
            monster.X,
            monster.Y,
            Mobj.OnFloorZ,
            MobjType.Clip);
        objectThing.Z = objectThing.FloorZ;

        Assert.IsFalse(MbfMonsterFriction.Applies(GameCompatibility.Mbf, objectThing));
    }

    [TestMethod]
    public void MbfThingMovementAppliesCustomFrictionToMonsterMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var monster = SpawnGroundedMonster(world);
        var sector = monster.Subsector.Sector;
        var custom = new Fixed(0xd800);
        var initial = new Fixed(0x1001);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = custom;
        monster.MomX = initial;
        monster.MomY = Fixed.Zero;

        world.ThingMovement.XYMovement(monster);

        Assert.AreEqual((initial * custom).Data, monster.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, monster.MomY.Data);
    }

    [TestMethod]
    public void BoomThingMovementDoesNotApplyCustomFrictionToMonsterMomentum()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var monster = SpawnGroundedMonster(world);
        var sector = monster.Subsector.Sector;
        var initial = new Fixed(0x1001);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xd800);
        monster.MomX = initial;
        monster.MomY = Fixed.Zero;

        world.ThingMovement.XYMovement(monster);

        Assert.AreEqual(
            (initial * BoomFrictionTranslator.OriginalFriction).Data,
            monster.MomX.Data);
    }

    [TestMethod]
    public void GenericFloorFrictionDoesNotChangeLegacyPlayerOnlyApiContract()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var monster = SpawnGroundedMonster(world);
        var sector = monster.Subsector.Sector;
        var custom = new Fixed(0xd800);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = custom;

        Assert.AreEqual(BoomFrictionTranslator.OriginalFrictionData, BoomSectorFriction.GetFriction(monster).Data);
        Assert.AreEqual(custom.Data, BoomSectorFriction.GetFloorFriction(monster).Data);
    }


    [TestMethod]
    public void MbfCorpseCoastingStillUsesResolvedFloorFriction()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var corpse = SpawnGroundedMonster(world);
        var sector = corpse.Subsector.Sector;
        var fullIce = BoomFrictionTranslator.Resolve(
            Fixed.FromInt(392),
            Fixed.FromInt(8));

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = fullIce.Friction;
        sector.MoveFactor = fullIce.MoveFactor;
        corpse.Health = 0;
        corpse.Flags |= MobjFlags.Corpse;

        Assert.IsFalse(MbfMonsterFriction.Applies(GameCompatibility.Mbf, corpse));
        Assert.IsTrue(MbfMonsterFriction.AppliesToCorpseCoasting(
            GameCompatibility.Mbf,
            enabled: true,
            thing: corpse));
        Assert.AreEqual(
            Fixed.FracUnit,
            MbfMonsterFriction.GetCorpseCoastingFriction(
                GameCompatibility.Mbf,
                enabled: true,
                thing: corpse).Data);
    }

    [TestMethod]
    public void MbfCorpsePreservesDeathMomentumAcrossFullIceTic()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var corpse = SpawnGroundedMonster(world);
        var sector = corpse.Subsector.Sector;
        var fullIce = BoomFrictionTranslator.Resolve(
            Fixed.FromInt(392),
            Fixed.FromInt(8));
        var deathMomentum = Fixed.FromInt(1);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = fullIce.Friction;
        sector.MoveFactor = fullIce.MoveFactor;
        corpse.MomX = deathMomentum;
        corpse.MomY = Fixed.Zero;
        corpse.Health = 0;

        world.ThingInteraction.KillMobj(null, corpse);

        Assert.IsTrue((corpse.Flags & MobjFlags.Corpse) != 0);
        Assert.AreEqual(deathMomentum.Data, corpse.MomX.Data);

        world.ThingMovement.XYMovement(corpse);

        // Full MBFEDIT ice resolves to FRACUNIT friction, so the corpse keeps
        // the death impulse instead of immediately falling back to 0xe800.
        Assert.AreEqual(deathMomentum.Data, corpse.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, corpse.MomY.Data);
    }

    [TestMethod]
    public void MbfCorpseReturnsToOriginalFrictionAfterLeavingIce()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var corpse = SpawnGroundedMonster(world);
        var sector = corpse.Subsector.Sector;
        var initial = Fixed.FromInt(1);

        corpse.Health = 0;
        corpse.Flags |= MobjFlags.Corpse;
        corpse.MomX = initial;
        corpse.MomY = Fixed.Zero;
        sector.Special = 0;
        sector.Friction = Fixed.One;

        world.ThingMovement.XYMovement(corpse);

        Assert.AreEqual(
            (initial * BoomFrictionTranslator.OriginalFriction).Data,
            corpse.MomX.Data);
    }

    [TestMethod]
    public void BoomProfileDoesNotEnableMbfCorpseCoastingRule()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Boom }, null);
        var corpse = SpawnGroundedMonster(world);

        corpse.Health = 0;
        corpse.Flags |= MobjFlags.Corpse;

        Assert.IsFalse(MbfMonsterFriction.AppliesToCorpseCoasting(
            GameCompatibility.Boom,
            enabled: true,
            thing: corpse));
    }

    [TestMethod]
    public void SludgeStepSpeedMatchesOriginalMbfIntegerFormula()
    {
        var friction = new Fixed(0xd800);
        var moveFactor = new Fixed(1024);

        Assert.AreEqual(6, MbfMonsterFriction.GetAdjustedStepSpeed(8, friction, moveFactor));
        Assert.AreEqual(1, MbfMonsterFriction.GetAdjustedStepSpeed(0, friction, moveFactor));
        Assert.AreEqual(8, MbfMonsterFriction.GetAdjustedStepSpeed(
            8,
            BoomFrictionTranslator.OriginalFriction,
            moveFactor));
    }

    [TestMethod]
    public void SludgeChaseStepUsesReducedImmediateMovement()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var actor = PrepareEastboundChase(world);
        var sector = actor.Subsector.Sector;
        var friction = new Fixed(0xd800);
        var moveFactor = new Fixed(1024);

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = friction;
        sector.MoveFactor = moveFactor;

        var startX = actor.X;
        var expectedSpeed = MbfMonsterFriction.GetAdjustedStepSpeed(
            actor.Info.Speed,
            friction,
            moveFactor);

        world.MonsterBehavior.Chase(actor);

        Assert.AreEqual((startX + Fixed.FromInt(expectedSpeed)).Data, actor.X.Data);
        Assert.AreEqual(Fixed.Zero.Data, actor.MomX.Data);
    }

    [TestMethod]
    public void IceChaseStepBecomesMomentumWithoutImmediatePositionChange()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var world = new World(content, new GameOptions { Compatibility = GameCompatibility.Mbf }, null);
        var actor = PrepareEastboundChase(world);
        var sector = actor.Subsector.Sector;
        var friction = new Fixed(0xf000);
        var moveFactor = BoomFrictionTranslator.OriginalMoveFactor;

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = friction;
        sector.MoveFactor = moveFactor;

        var startX = actor.X;
        var startY = actor.Y;
        var deltaX = actor.Info.Speed * Fixed.One;
        var expectedMomentum = deltaX * MbfMonsterFriction.GetMomentumStepScale(moveFactor);

        world.MonsterBehavior.Chase(actor);

        Assert.AreEqual(startX.Data, actor.X.Data);
        Assert.AreEqual(startY.Data, actor.Y.Data);
        Assert.AreEqual(expectedMomentum.Data, actor.MomX.Data);
        Assert.AreEqual(Fixed.Zero.Data, actor.MomY.Data);
    }

    [TestMethod]
    public void MbfEditFullIceGivesMonsterOnlySubStopSpeedSelfImpulse()
    {
        var resolved = BoomFrictionTranslator.Resolve(
            Fixed.FromInt(392),
            Fixed.FromInt(8));
        var deltaX = 8 * Fixed.One;
        var selfImpulse = deltaX * MbfMonsterFriction.GetMomentumStepScale(resolved.MoveFactor);

        Assert.AreEqual(Fixed.FracUnit, resolved.Friction.Data);
        Assert.AreEqual(BoomFrictionTranslator.MinimumMoveFactorData, resolved.MoveFactor.Data);
        Assert.AreEqual(Fixed.FracUnit / 32, selfImpulse.Data);
        Assert.IsTrue(selfImpulse < Fixed.One / 16);
    }

    [TestMethod]
    public void DisabledMonsterFrictionKeepsClassicImmediateStepOnIce()
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var options = new GameOptions { Compatibility = GameCompatibility.Mbf };
        options.MbfOptions.MonsterFriction = false;
        var world = new World(content, options, null);
        var actor = PrepareEastboundChase(world);
        var sector = actor.Subsector.Sector;

        sector.Special = (SectorSpecial)BoomFrictionTranslator.FrictionMask;
        sector.Friction = new Fixed(0xf000);
        sector.MoveFactor = BoomFrictionTranslator.OriginalMoveFactor;

        var startX = actor.X;
        world.MonsterBehavior.Chase(actor);

        Assert.AreEqual((startX + Fixed.FromInt(actor.Info.Speed)).Data, actor.X.Data);
        Assert.AreEqual(Fixed.Zero.Data, actor.MomX.Data);
    }

    private static Mobj PrepareEastboundChase(World world)
    {
        var actor = SpawnGroundedMonster(world);
        var target = world.ThingAllocation.SpawnMobj(
            actor.X + Fixed.FromInt(128),
            actor.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        // Put the target on the opposite MBF friend side so normal attack
        // validation accepts it, then keep the actor in reaction time so this
        // tick reaches the movement path deterministically.
        target.Flags |= MobjFlags.Friend;
        target.Z = target.FloorZ;

        actor.Target = target;
        actor.MoveDir = Direction.East;
        actor.MoveCount = 1;
        actor.ReactionTime = 2;
        actor.Flags &= ~(MobjFlags.JustAttacked | MobjFlags.JustHit);
        actor.MomX = Fixed.Zero;
        actor.MomY = Fixed.Zero;
        return actor;
    }

    private static Mobj SpawnGroundedMonster(World world)
    {
        var player = world.ConsolePlayer.Mobj;
        player.Flags &= ~MobjFlags.Solid;

        var monster = world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            MobjType.Troop);

        monster.Flags &= ~(MobjFlags.NoClip | MobjFlags.NoGravity | MobjFlags.Corpse);
        monster.Z = monster.FloorZ;
        monster.MomX = Fixed.Zero;
        monster.MomY = Fixed.Zero;
        return monster;
    }
}
