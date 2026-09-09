using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Gameplay;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
[DoNotParallelize]
public sealed class MbfMaxHealthCompatibilityTest
{
    [TestMethod]
    public void CompatibilityBoundaryMatchesPrBoomAndMbf21Rules()
    {
        Assert.IsTrue(MbfMaxHealthCompatibility.MaxHealthAppliesOnlyToBonuses(
            GameCompatibility.Vanilla, compMaxHealth: false));
        Assert.IsTrue(MbfMaxHealthCompatibility.MaxHealthAppliesOnlyToBonuses(
            GameCompatibility.Boom, compMaxHealth: false));

        Assert.IsFalse(MbfMaxHealthCompatibility.MaxHealthAppliesOnlyToBonuses(
            GameCompatibility.Mbf, compMaxHealth: false));
        Assert.IsTrue(MbfMaxHealthCompatibility.MaxHealthAppliesOnlyToBonuses(
            GameCompatibility.Mbf, compMaxHealth: true));

        Assert.IsFalse(MbfMaxHealthCompatibility.MaxHealthAppliesOnlyToBonuses(
            GameCompatibility.Mbf21, compMaxHealth: false));
        Assert.IsFalse(MbfMaxHealthCompatibility.MaxHealthAppliesOnlyToBonuses(
            GameCompatibility.Mbf21, compMaxHealth: true));
    }

    [TestMethod]
    public void ResolverKeepsDistinctRegularAndBonusHealthCaps()
    {
        Assert.AreEqual(100, MbfMaxHealthCompatibility.ResolveRegularHealthCap(
            GameCompatibility.Mbf, false, 100, 200, hasDehMaxHealth: false));
        Assert.AreEqual(200, MbfMaxHealthCompatibility.ResolveBonusHealthCap(
            GameCompatibility.Mbf, false, 200, 200, hasDehMaxHealth: false));

        Assert.AreEqual(150, MbfMaxHealthCompatibility.ResolveRegularHealthCap(
            GameCompatibility.Mbf, false, 100, 150, hasDehMaxHealth: true));
        Assert.AreEqual(300, MbfMaxHealthCompatibility.ResolveBonusHealthCap(
            GameCompatibility.Mbf, false, 200, 150, hasDehMaxHealth: true));

        Assert.AreEqual(100, MbfMaxHealthCompatibility.ResolveRegularHealthCap(
            GameCompatibility.Mbf, true, 100, 150, hasDehMaxHealth: true));
        Assert.AreEqual(150, MbfMaxHealthCompatibility.ResolveBonusHealthCap(
            GameCompatibility.Mbf, true, 200, 150, hasDehMaxHealth: true));

        Assert.AreEqual(150, MbfMaxHealthCompatibility.ResolveRegularHealthCap(
            GameCompatibility.Mbf21, true, 100, 150, hasDehMaxHealth: true));
        Assert.AreEqual(300, MbfMaxHealthCompatibility.ResolveBonusHealthCap(
            GameCompatibility.Mbf21, true, 200, 150, hasDehMaxHealth: true));
    }

    [TestMethod]
    public void PreMbfCompatibilityKeepsExistingManagedDoomCaps()
    {
        Assert.AreEqual(137, MbfMaxHealthCompatibility.ResolveRegularHealthCap(
            GameCompatibility.Boom, false, 137, 175, hasDehMaxHealth: true));
        Assert.AreEqual(175, MbfMaxHealthCompatibility.ResolveBonusHealthCap(
            GameCompatibility.Boom, false, 175, 175, hasDehMaxHealth: true));
    }

    [TestMethod]
    public void ItemPickupConsumesRuntimeCompMaxHealth()
    {
        var oldMaxHealth = DoomInfo.DeHackEdConst.MaxHealth;
        var oldHasMaxHealth = DoomInfo.DeHackEdConst.HasMaxHealthOverride;

        try
        {
            DoomInfo.DeHackEdConst.MaxHealth = 150;
            DoomInfo.DeHackEdConst.HasMaxHealthOverride = true;

            using var content = GameContent.CreateDummy(WadPath.Doom2);

            var fixedOptions = new GameOptions { Compatibility = GameCompatibility.Mbf };
            var fixedWorld = new World(content, fixedOptions, null);
            SetHealth(fixedWorld, 140);
            Touch(fixedWorld, MobjType.Misc10);
            Assert.AreEqual(150, fixedWorld.ConsolePlayer.Health);

            SetHealth(fixedWorld, 299);
            Touch(fixedWorld, MobjType.Misc2);
            Assert.AreEqual(300, fixedWorld.ConsolePlayer.Health);

            var potionOnlyOptions = new GameOptions { Compatibility = GameCompatibility.Mbf };
            potionOnlyOptions.MbfOptions.CompMaxHealth = true;
            var potionOnlyWorld = new World(content, potionOnlyOptions, null);
            SetHealth(potionOnlyWorld, 95);
            Touch(potionOnlyWorld, MobjType.Misc10);
            Assert.AreEqual(100, potionOnlyWorld.ConsolePlayer.Health);

            SetHealth(potionOnlyWorld, 149);
            Touch(potionOnlyWorld, MobjType.Misc2);
            Assert.AreEqual(150, potionOnlyWorld.ConsolePlayer.Health);

            var mbf21Options = new GameOptions { Compatibility = GameCompatibility.Mbf21 };
            mbf21Options.MbfOptions.CompMaxHealth = true;
            var mbf21World = new World(content, mbf21Options, null);
            SetHealth(mbf21World, 140);
            Touch(mbf21World, MobjType.Misc10);
            Assert.AreEqual(150, mbf21World.ConsolePlayer.Health);

            SetHealth(mbf21World, 299);
            Touch(mbf21World, MobjType.Misc2);
            Assert.AreEqual(300, mbf21World.ConsolePlayer.Health);
        }
        finally
        {
            DoomInfo.DeHackEdConst.MaxHealth = oldMaxHealth;
            DoomInfo.DeHackEdConst.HasMaxHealthOverride = oldHasMaxHealth;
        }
    }

    private static void SetHealth(World world, int health)
    {
        world.ConsolePlayer.Health = health;
        world.ConsolePlayer.Mobj.Health = health;
    }

    private static void Touch(World world, MobjType type)
    {
        var player = world.ConsolePlayer.Mobj;
        var item = world.ThingAllocation.SpawnMobj(
            player.X,
            player.Y,
            Mobj.OnFloorZ,
            type);

        world.ItemPickup.TouchSpecialThing(item, player);
    }
}
