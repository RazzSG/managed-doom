namespace ManagedDoom.Compatibility.Boom.Lines;

/// <summary>
/// Boom-compatible zero-tag rules for regular linedef specials.
/// Mirrors Boom/MBF P_CheckTag: with Boom compatibility enabled, most
/// activatable regular specials require a non-zero tag, while a small
/// compatibility whitelist is allowed to operate with tag 0.
/// </summary>
public static class BoomTagRules
{
    public static bool CanActivate(LineDef line, GameCompatibility compatibility)
    {
        if (!GameCompatibilityFeatures.SupportsBoom(compatibility))
            return true;

        return line.Tag != 0 || AllowsZeroTag(line.Special);
    }

    public static bool AllowsZeroTag(LineSpecial special)
    {
        switch ((int)special)
        {
            // Manual doors.
            case 1:
            case 26:
            case 27:
            case 28:
            case 31:
            case 32:
            case 33:
            case 34:
            case 117:
            case 118:

            // Lighting specials.
            case 139:
            case 170:
            case 79:
            case 35:
            case 138:
            case 171:
            case 81:
            case 13:
            case 192:
            case 169:
            case 80:
            case 12:
            case 194:
            case 173:
            case 157:
            case 104:
            case 193:
            case 172:
            case 156:
            case 17:

            // Thing teleporters.
            case 195:
            case 174:
            case 97:
            case 39:
            case 126:
            case 125:
            case 210:
            case 209:
            case 208:
            case 207:

            // Exits.
            case 11:
            case 52:
            case 197:
            case 51:
            case 124:
            case 198:

            // Scrolling walls.
            case 48:
            case 85:
                return true;

            default:
                return false;
        }
    }
}
