namespace ManagedDoom.Compatibility.Boom.Rendering;

public readonly struct BoomTransferHeightRenderState
{
    public BoomTransferHeightRenderState(
        Sector sector,
        Sector floorPlaneSector,
        Sector ceilingPlaneSector,
        Fixed floorHeight,
        Fixed ceilingHeight,
        int floorFlat,
        int ceilingFlat,
        int lightLevel,
        int floorLightLevel,
        int ceilingLightLevel,
        BoomTransferHeightZone zone,
        bool transferred,
        bool forceFloorPlane = false,
        bool forceCeilingPlane = false)
    {
        Sector = sector;
        FloorPlaneSector = floorPlaneSector;
        CeilingPlaneSector = ceilingPlaneSector;
        FloorHeight = floorHeight;
        CeilingHeight = ceilingHeight;
        FloorFlat = floorFlat;
        CeilingFlat = ceilingFlat;
        LightLevel = lightLevel;
        FloorLightLevel = floorLightLevel;
        CeilingLightLevel = ceilingLightLevel;
        Zone = zone;
        Transferred = transferred;
        ForceFloorPlane = forceFloorPlane;
        ForceCeilingPlane = forceCeilingPlane;
    }

    public Sector Sector { get; }

    // Sector supplying the flat scrolling offsets for each rendered plane.
    // The plane cache also keys on the resolved height, so several targets can
    // safely share the same control sector while keeping different real heights.
    public Sector FloorPlaneSector { get; }
    public Sector CeilingPlaneSector { get; }

    public Fixed FloorHeight { get; }
    public Fixed CeilingHeight { get; }
    public int FloorFlat { get; }
    public int CeilingFlat { get; }
    public int LightLevel { get; }
    public int FloorLightLevel { get; }
    public int CeilingLightLevel { get; }
    public BoomTransferHeightZone Zone { get; }
    public bool Transferred { get; }

    // Boom's 242 sky handling can make a fake plane visible even when its
    // resolved height is on the normally culled side of the camera.
    public bool ForceFloorPlane { get; }
    public bool ForceCeilingPlane { get; }
}
