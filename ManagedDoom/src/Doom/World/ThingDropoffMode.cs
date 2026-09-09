namespace ManagedDoom;

/// <summary>
/// Controls the exceptional ledge policy used by movement callers. Normal
/// Doom movement keeps tall dropoffs blocked; Boom physical momentum/slide
/// movement may push actors across them. MBF can later re-enable the old block
/// through comp_dropoff, or request a narrowly constrained helper-dog drop.
/// </summary>
internal enum ThingDropoffMode
{
    Disallow = 0,
    Allow = 1,
    Targeted128 = 2
}
