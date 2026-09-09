using System.Collections.Generic;
using System.Linq;
using ManagedDoom;
using ManagedDoom.Audio;
using ManagedDoom.Compatibility;
using ManagedDoom.Compatibility.Mbf.Doors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ManagedDoomTest.UnitTests;

[TestClass]
public sealed class MbfBlazingDoorCompatibilityTest
{
    [TestMethod]
    public void CompatibilityGatePreservesVanillaAndBoomAndAddsMbfToggle()
    {
        Assert.IsFalse(MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
            GameCompatibility.Vanilla, compBlazing: false));
        Assert.IsFalse(MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
            GameCompatibility.Vanilla, compBlazing: true));

        Assert.IsTrue(MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
            GameCompatibility.Boom, compBlazing: false));
        Assert.IsTrue(MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
            GameCompatibility.Boom, compBlazing: true));

        Assert.IsTrue(MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
            GameCompatibility.Mbf, compBlazing: false));
        Assert.IsFalse(MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
            GameCompatibility.Mbf, compBlazing: true));

        Assert.IsTrue(MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
            GameCompatibility.Mbf21, compBlazing: false));
        Assert.IsFalse(MbfBlazingDoorCompatibility.UsesFixedBlazingDoorSounds(
            GameCompatibility.Mbf21, compBlazing: true));
    }
    [TestMethod]
    public void VerticalDoorEndpointConsumesCompBlazingAtRuntime()
    {
        Assert.AreEqual(1, CountExtraClosingSounds(
            GameCompatibility.Vanilla, compBlazing: false, VerticalDoorType.BlazeClose));
        Assert.AreEqual(0, CountExtraClosingSounds(
            GameCompatibility.Boom, compBlazing: true, VerticalDoorType.BlazeClose));
        Assert.AreEqual(0, CountExtraClosingSounds(
            GameCompatibility.Mbf, compBlazing: false, VerticalDoorType.BlazeClose));
        Assert.AreEqual(1, CountExtraClosingSounds(
            GameCompatibility.Mbf, compBlazing: true, VerticalDoorType.BlazeClose));
        Assert.AreEqual(1, CountExtraClosingSounds(
            GameCompatibility.Mbf21, compBlazing: true, VerticalDoorType.BlazeClose));

        // PrBoom applies comp_blazing to generalized blazing raise/close doors too.
        Assert.AreEqual(0, CountExtraClosingSounds(
            GameCompatibility.Mbf, compBlazing: false, VerticalDoorType.GeneralizedClose));
        Assert.AreEqual(1, CountExtraClosingSounds(
            GameCompatibility.Mbf, compBlazing: true, VerticalDoorType.GeneralizedClose));
    }

    private static int CountExtraClosingSounds(
        GameCompatibility compatibility,
        bool compBlazing,
        VerticalDoorType type)
    {
        using var content = GameContent.CreateDummy(WadPath.Doom2);
        var sound = new RecordingSound();
        var options = new GameOptions { Compatibility = compatibility };
        options.MbfOptions.CompBlazing = compBlazing;
        options.Sound = sound;

        var world = new World(content, options, null);
        sound.Started.Clear();

        var sector = world.Map.Sectors.First(candidate =>
            candidate.ThingList == null &&
            candidate.TouchingThingList == null &&
            candidate.SpecialData == null);

        sector.FloorHeight = Fixed.Zero;
        sector.CeilingHeight = Fixed.FromInt(4);

        var door = new VerticalDoor(world)
        {
            Sector = sector,
            Type = type,
            Direction = -1,
            Speed = Fixed.FromInt(8),
            TopHeight = Fixed.FromInt(128),
            TopWait = 150
        };

        world.Thinkers.Add(door);
        sector.CeilingData = door;
        door.Run();

        return sound.Started.Count(sfx => sfx == Sfx.BDCLS);
    }

    private sealed class RecordingSound : ISound
    {
        public List<Sfx> Started { get; } = new();

        public int MaxVolume => 15;

        public int Volume { get; set; }

        public void SetListener(Mobj listener) { }

        public void Update() { }

        public void StartSound(Sfx sfx) => Started.Add(sfx);

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type) => Started.Add(sfx);

        public void StartSound(Mobj mobj, Sfx sfx, SfxType type, int volume) => Started.Add(sfx);

        public void StopSound(Mobj mobj) { }

        public void Reset() => Started.Clear();

        public void Pause() { }

        public void Resume() { }
    }

}
