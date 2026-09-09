using System;
using System.Collections.Generic;

namespace ManagedDoom.DefinitionPatches.DeHackEd;

/// <summary>
/// Parser for the original 32-bit DeHackEd/BEX Thing "Bits" field.
///
/// Boom added mnemonic names for the classic flags and MBF reused three of
/// the previously-unused high bits. Keep the canonical patch representation
/// separate from ManagedDoom's runtime storage so the Boom TRANSLUCENT bit
/// never collides with MBF FRIEND.
/// </summary>
internal static class DeHackEdThingBits
{
    public const uint MbfTouchyBit = 0x10000000u;
    public const uint MbfBouncesBit = 0x20000000u;
    public const uint MbfFriendBit = 0x40000000u;
    public const uint BoomTranslucentBit = 0x80000000u;
    public const uint MbfBitsMask = MbfTouchyBit | MbfBouncesBit | MbfFriendBit;

    private static readonly Dictionary<string, uint> bitNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SPECIAL"] = 0x00000001u,
        ["SOLID"] = 0x00000002u,
        ["SHOOTABLE"] = 0x00000004u,
        ["NOSECTOR"] = 0x00000008u,
        ["NOBLOCKMAP"] = 0x00000010u,
        ["AMBUSH"] = 0x00000020u,
        ["JUSTHIT"] = 0x00000040u,
        ["JUSTATTACKED"] = 0x00000080u,
        ["SPAWNCEILING"] = 0x00000100u,
        ["NOGRAVITY"] = 0x00000200u,
        ["DROPOFF"] = 0x00000400u,
        ["PICKUP"] = 0x00000800u,
        ["NOCLIP"] = 0x00001000u,
        ["SLIDE"] = 0x00002000u,
        ["FLOAT"] = 0x00004000u,
        ["TELEPORT"] = 0x00008000u,
        ["MISSILE"] = 0x00010000u,
        ["DROPPED"] = 0x00020000u,
        ["SHADOW"] = 0x00040000u,
        ["NOBLOOD"] = 0x00080000u,
        ["CORPSE"] = 0x00100000u,
        ["INFLOAT"] = 0x00200000u,
        ["COUNTKILL"] = 0x00400000u,
        ["COUNTITEM"] = 0x00800000u,
        ["SKULLFLY"] = 0x01000000u,
        ["NOTDMATCH"] = 0x02000000u,
        ["TRANSLATION1"] = 0x04000000u,
        ["TRANSLATION2"] = 0x08000000u,
        ["TOUCHY"] = MbfTouchyBit,
        ["BOUNCES"] = MbfBouncesBit,
        ["FRIEND"] = MbfFriendBit,
        ["TRANSLUCENT"] = BoomTranslucentBit,

        // Boom compatibility aliases documented by the BEX parser.
        ["TRANSLATION"] = 0x04000000u,
        ["UNUSED1"] = 0x08000000u,
        ["UNUSED2"] = MbfTouchyBit,
        ["UNUSED3"] = MbfBouncesBit,
        ["UNUSED4"] = MbfFriendBit
    };

    public static bool TryParse(string text, out uint bits)
    {
        bits = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        var hash = value.IndexOf('#');
        if (hash >= 0)
            value = value.Substring(0, hash).TrimEnd();

        if (value.Length == 0)
            return false;

        // Classic patches normally write a signed decimal int, while BEX
        // tools/hand-written patches may use the full unsigned 32-bit value.
        // Preserve the old parser's permissive "leading integer" behavior for
        // numeric Bits lines, but do not swallow a mnemonic expression that
        // intentionally uses '+' or '|'.  Real MBF patches commonly use the
        // DeHackEd-style "BOUNCES | DROPOFF" spelling.
        var hasExpressionSeparator =
            value.IndexOf('|') >= 0 ||
            value.IndexOf('+', value.Length > 0 && value[0] == '+' ? 1 : 0) >= 0;

        if (!hasExpressionSeparator && TryParseNumeric(value, allowTrailing: true, out bits))
            return true;

        uint result = 0;
        var tokens = value.Split(new[] { '+', '|' }, StringSplitOptions.None);
        if (tokens.Length == 0)
            return false;

        foreach (var tokenText in tokens)
        {
            var token = tokenText.Trim();
            if (token.Length == 0)
                return false;

            if (bitNames.TryGetValue(token, out var namedBit))
            {
                result |= namedBit;
                continue;
            }

            // Accept a numeric term in a mnemonic expression as a harmless
            // extension; this also makes mixed hand-written patches robust.
            if (TryParseNumeric(token, allowTrailing: false, out var numericBit))
            {
                result |= numericBit;
                continue;
            }

            return false;
        }

        bits = result;
        return true;
    }

    private static bool TryParseNumeric(string text, bool allowTrailing, out uint bits)
    {
        bits = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var span = text.AsSpan().TrimStart();
        var length = 0;
        if (span.Length > 0 && (span[0] == '+' || span[0] == '-'))
            length++;

        var digitStart = length;
        while (length < span.Length && char.IsDigit(span[length]))
            length++;

        if (length == digitStart)
            return false;

        if (!allowTrailing && !span.Slice(length).Trim().IsEmpty)
            return false;

        if (!long.TryParse(span.Slice(0, length), out var value))
            return false;

        if (value < int.MinValue || value > uint.MaxValue)
            return false;

        bits = value < 0
            ? unchecked((uint)(int)value)
            : (uint)value;
        return true;
    }
}
