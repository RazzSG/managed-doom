using ManagedDoom.Compatibility;

namespace ManagedDoom.Compatibility.Boom.Rendering;

/// <summary>
/// Boom-family closed two-sided portal rules used by the software renderer.
/// </summary>
public static class BoomClosedPortalResolver
{
    public static bool IsClosed(
        GameCompatibility compatibility,
        Fixed frontFloor,
        Fixed frontCeiling,
        Fixed backFloor,
        Fixed backCeiling,
        bool hasTopTexture,
        bool hasBottomTexture,
        bool bothCeilingsAreSky)
    {
        // These two cases are already treated as solid by vanilla Doom.
        if (backCeiling <= frontFloor || backFloor >= frontCeiling)
            return true;

        // Preserve the original ManagedDoom/vanilla behavior. Boom added the
        // self-closed back-sector case below as part of its renderer fixes.
        if (!GameCompatibilityFeatures.SupportsBoom(compatibility))
            return false;

        if (backCeiling > backFloor)
            return false;

        // Boom/PrBoom's RF_CLOSED test preserves intentionally transparent
        // lift/door effects when the missing upper/lower texture is meant to
        // leave the portal visually open. Two sky ceilings are also kept open.
        if (backCeiling < frontCeiling && !hasTopTexture)
            return false;

        if (backFloor > frontFloor && !hasBottomTexture)
            return false;

        if (bothCeilingsAreSky)
            return false;

        return true;
    }
}
