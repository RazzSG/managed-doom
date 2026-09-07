using System;
using System.Collections.Generic;
using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Pushers;

public sealed class BoomPointPusher
{
    public const int PushSourceThingType = 5001;
    public const int PullSourceThingType = 5002;

    // Boom defines MT_PUSH/MT_PULL with a raw fixed-point height of 8.
    private static readonly Fixed SourceHeight = new Fixed(8);

    private readonly World world;
    private readonly Sector sourceSector;
    private readonly Fixed sourceX;
    private readonly Fixed sourceY;
    private readonly int magnitude;
    private readonly bool pushesAway;

    public BoomPointPusher(
        World world,
        Sector sourceSector,
        Fixed sourceX,
        Fixed sourceY,
        int magnitude,
        bool pushesAway)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.sourceSector = sourceSector ?? throw new ArgumentNullException(nameof(sourceSector));
        this.sourceX = sourceX;
        this.sourceY = sourceY;
        this.magnitude = magnitude;
        this.pushesAway = pushesAway;
    }

    public static BoomPointPusher[] Resolve(World world)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(world.Options.Compatibility))
            return Array.Empty<BoomPointPusher>();

        var sources = ResolveSources(world);
        if (sources.Count == 0)
            return Array.Empty<BoomPointPusher>();

        var pushers = new List<BoomPointPusher>();

        foreach (var line in world.Map.Lines)
        {
            if ((int)line.Special != 226)
                continue;

            var magnitude = BoomPusherTranslator.ResolvePointMagnitude(line.Dx, line.Dy);

            foreach (var sector in world.Map.BoomTags.GetSectors(line.Tag))
            {
                if (!sources.TryGetValue(sector.Number, out var source))
                    continue;

                pushers.Add(
                    new BoomPointPusher(
                        world,
                        sector,
                        source.X,
                        source.Y,
                        magnitude,
                        source.PushesAway));
            }
        }

        return pushers.ToArray();
    }

    public void Apply(Mobj thing)
    {
        if (thing?.Player == null)
            return;

        if ((thing.Flags & (MobjFlags.NoClip | MobjFlags.NoGravity)) != 0)
            return;

        // Boom lets the sector special dynamically turn an already spawned pusher off.
        if (((int)sourceSector.Special & BoomPusherTranslator.PushMask) == 0)
            return;

        var speed = BoomPusherTranslator.ResolvePointSpeed(
            magnitude,
            thing.X - sourceX,
            thing.Y - sourceY);

        if (speed <= Fixed.Zero)
            return;

        if (!world.VisibilityCheck.CheckSightToPoint(
                thing,
                sourceX,
                sourceY,
                sourceSector.FloorHeight,
                SourceHeight,
                sourceSector))
        {
            return;
        }

        var angle = Geometry.PointToAngle(thing.X, thing.Y, sourceX, sourceY);
        if (pushesAway)
            angle += Angle.Ang180;

        thing.MomX += speed * Trig.Cos(angle);
        thing.MomY += speed * Trig.Sin(angle);
    }

    private static Dictionary<int, PointSource> ResolveSources(World world)
    {
        var result = new Dictionary<int, PointSource>();

        // SpawnMapThing inserts sector-linked mobjs at the head of ThingList.
        // Iterating map things in map order and overwriting therefore reproduces
        // P_GetPushThing() selecting the last eligible push/pull marker in a sector.
        foreach (var mapThing in world.Map.Things)
        {
            if (mapThing.Type != PushSourceThingType && mapThing.Type != PullSourceThingType)
                continue;

            if (!IsMapThingActive(world, mapThing))
                continue;

            var sector = Geometry.PointInSubsector(mapThing.X, mapThing.Y, world.Map).Sector;
            result[sector.Number] = new PointSource(
                mapThing.X,
                mapThing.Y,
                mapThing.Type == PushSourceThingType);
        }

        return result;
    }

    private static bool IsMapThingActive(World world, MapThing mapThing)
    {
        var options = world.Options;

        if (!BoomThingSpawnFilter.IsAllowed(options, mapThing.Flags))
            return false;

        var flags = (int)mapThing.Flags;
        int skillBit;
        if (options.Skill == GameSkill.Baby)
            skillBit = 1;
        else if (options.Skill == GameSkill.Nightmare)
            skillBit = 4;
        else
            skillBit = 1 << ((int)options.Skill - 1);

        return (flags & skillBit) != 0;
    }

    public Sector SourceSector => sourceSector;
    public Fixed SourceX => sourceX;
    public Fixed SourceY => sourceY;
    public int Magnitude => magnitude;
    public bool PushesAway => pushesAway;

    private readonly struct PointSource
    {
        public PointSource(Fixed x, Fixed y, bool pushesAway)
        {
            X = x;
            Y = y;
            PushesAway = pushesAway;
        }

        public Fixed X { get; }
        public Fixed Y { get; }
        public bool PushesAway { get; }
    }
}
