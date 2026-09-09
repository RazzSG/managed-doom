using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ManagedDoom;
using ManagedDoom.Compatibility;
using ManagedDoom.DefinitionPatches.DeHackEd;

namespace ManagedDoom.Compatibility.Detection;

/// <summary>
/// Finds gameplay compatibility requirements that are proven by embedded
/// DEHACKED/BEX data. Definition-patch syntax is a separate axis from gameplay
/// compatibility, so ordinary BEX sections do not promote the profile by
/// themselves; only semantics that actually require a compatibility layer do.
/// </summary>
public static class DeHackEdCompatibilityFeatureScanner
{
    private static readonly HashSet<string> mbfCodePointers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Spawn",
        "Turn",
        "Face",
        "Scratch",
        "PlaySound",
        "RandomJump",
        "LineEffect",
        "Die",
        "Detonate",
        "Mushroom",
        "BetaSkullAttack",
        "Stop"
    };

    public static bool TryDetect(Wad wad, out GameCompatibility compatibility)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));

        compatibility = GameCompatibility.Vanilla;
        var found = false;

        // Embedded patches are applied in directory/load order. Detection only
        // needs the highest minimum compatibility proven by any effective input,
        // so scan every DEHACKED lump without mutating DoomInfo.
        for (var i = 0; i < wad.LumpInfos.Count; i++)
        {
            if (!string.Equals(wad.LumpInfos[i].Name, "DEHACKED", StringComparison.OrdinalIgnoreCase))
                continue;

            var lumpCompatibility = Scan(wad.ReadLump(i));
            if (lumpCompatibility > compatibility)
                compatibility = lumpCompatibility;

            found |= lumpCompatibility != GameCompatibility.Vanilla;
        }

        return found;
    }

    public static bool TryDetectExternal(CommandLineArgs args, out GameCompatibility compatibility)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));

        compatibility = GameCompatibility.Vanilla;
        if (!args.deh.Present || args.deh.Value == null || args.deh.Value.Length == 0)
            return false;

        var found = false;

        // Explicit -deh files are applied after embedded DEHACKED lumps by the
        // runtime patch pipeline. Auto compatibility must inspect the same input
        // set; otherwise an MBF patch can be active while gameplay stays on a
        // Vanilla/Boom profile. Scan without mutating the process-global DoomInfo.
        foreach (var fileName in args.deh.Value)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            var fileCompatibility = ScanExternalFile(fileName, allowIncludes: true);
            if (fileCompatibility > compatibility)
                compatibility = fileCompatibility;

            found |= fileCompatibility != GameCompatibility.Vanilla;
        }

        return found;
    }

    private static GameCompatibility ScanExternalFile(string fileName, bool allowIncludes)
    {
        var fullPath = Path.GetFullPath(fileName);
        var compatibility = Scan(File.ReadAllBytes(fullPath));

        if (!allowIncludes || compatibility == GameCompatibility.Mbf)
            return compatibility;

        foreach (var includeFileName in EnumerateIncludeFiles(fullPath))
        {
            if (includeFileName.Length == 0)
                continue;

            var includedCompatibility = ScanExternalFile(
                ResolveIncludePath(fullPath, includeFileName),
                allowIncludes: false);

            if (includedCompatibility > compatibility)
                compatibility = includedCompatibility;

            if (compatibility == GameCompatibility.Mbf)
                return compatibility;
        }

        return compatibility;
    }

    private static IEnumerable<string> EnumerateIncludeFiles(string sourcePath)
    {
        var textCharactersRemaining = 0;
        var inBexStrings = false;
        var bexStringsContinuation = false;

        foreach (var rawLine in File.ReadLines(sourcePath))
        {
            if (textCharactersRemaining > 0)
            {
                textCharactersRemaining = ConsumeTextLine(textCharactersRemaining, rawLine);
                continue;
            }

            if (inBexStrings && bexStringsContinuation)
            {
                bexStringsContinuation = HasBexLineContinuation(rawLine);
                continue;
            }

            var trimmed = rawLine.Trim();
            if (trimmed.Length == 0)
            {
                inBexStrings = false;
                bexStringsContinuation = false;
                continue;
            }

            if (trimmed[0] == '#')
                continue;

            if (inBexStrings)
            {
                bexStringsContinuation = HasBexLineContinuation(rawLine);
                continue;
            }

            // INCLUDE is parsed from the complete trimmed logical line by the
            // runtime parser. In particular, an inline '#' is part of an
            // unquoted include filename rather than an inline comment. Keep
            // detection byte-for-byte compatible with that rule.
            if (TryReadTextHeader(trimmed, out var textLength))
            {
                textCharactersRemaining = textLength;
                continue;
            }

            if (IsBexStringsHeader(trimmed))
            {
                inBexStrings = true;
                bexStringsContinuation = false;
                continue;
            }

            if (TryReadIncludeDirective(trimmed, out var includeFileName))
                yield return includeFileName;
        }
    }

    private static bool IsBexStringsHeader(string line)
    {
        var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length > 0 &&
               tokens[0].Equals("[STRINGS]", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasBexLineContinuation(string line)
    {
        if (line == null)
            return false;

        var end = line.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(line[end]))
            end--;

        return end >= 0 && line[end] == '\\';
    }

    private static bool TryReadIncludeDirective(string line, out string includeFileName)
    {
        includeFileName = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var index = 0;
        while (index < line.Length && !char.IsWhiteSpace(line[index]))
            index++;

        if (!line.Substring(0, index).Equals("INCLUDE", StringComparison.OrdinalIgnoreCase))
            return false;

        var remainder = index < line.Length ? line.Substring(index).Trim() : string.Empty;
        if (remainder.Length >= 6 &&
            remainder.Substring(0, 6).Equals("NOTEXT", StringComparison.OrdinalIgnoreCase) &&
            (remainder.Length == 6 || char.IsWhiteSpace(remainder[6])))
        {
            remainder = remainder.Length == 6 ? string.Empty : remainder.Substring(6).TrimStart();
        }

        if (remainder.Length > 0 && remainder[0] == '"')
        {
            var closingQuote = remainder.IndexOf('"', 1);
            includeFileName = closingQuote == -1
                ? remainder.Substring(1)
                : remainder.Substring(1, closingQuote - 1);
        }
        else
        {
            includeFileName = remainder.Trim();
        }

        return true;
    }

    private static string ResolveIncludePath(string parentSourcePath, string includeFileName)
    {
        if (Path.IsPathRooted(includeFileName))
            return Path.GetFullPath(includeFileName);

        var parentDirectory = Path.GetDirectoryName(Path.GetFullPath(parentSourcePath));
        if (!string.IsNullOrEmpty(parentDirectory))
        {
            var siblingPath = Path.GetFullPath(Path.Combine(parentDirectory, includeFileName));
            if (File.Exists(siblingPath))
                return siblingPath;
        }

        return Path.GetFullPath(includeFileName);
    }

    internal static GameCompatibility Scan(byte[] data)
    {
        if (data == null || data.Length == 0)
            return GameCompatibility.Vanilla;

        using var stream = new MemoryStream(data, writable: false);
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: true);

        var compatibility = GameCompatibility.Vanilla;
        var block = PatchBlock.None;
        var textCharactersRemaining = 0;

        for (var rawLine = reader.ReadLine(); rawLine != null; rawLine = reader.ReadLine())
        {
            if (textCharactersRemaining > 0)
            {
                textCharactersRemaining = ConsumeTextLine(textCharactersRemaining, rawLine);
                continue;
            }

            var line = StripHashComment(rawLine).Trim();
            if (line.Length == 0)
                continue;

            if (TryReadTextHeader(line, out var textLength))
            {
                block = PatchBlock.None;
                textCharactersRemaining = textLength;
                continue;
            }

            if (line[0] == '[')
            {
                block = line.Equals("[CODEPTR]", StringComparison.OrdinalIgnoreCase)
                    ? PatchBlock.CodePointers
                    : PatchBlock.None;
                continue;
            }

            if (block == PatchBlock.CodePointers && TryReadCodePointer(line, out var pointerName))
            {
                if (mbfCodePointers.Contains(pointerName))
                    return GameCompatibility.Mbf;

                continue;
            }

            if (IsThingHeader(line))
            {
                block = PatchBlock.Thing;
                continue;
            }

            if (IsPointerHeader(line))
            {
                block = PatchBlock.Pointer;
                continue;
            }

            if (IsClassicBlockHeader(line))
            {
                block = PatchBlock.None;
                continue;
            }

            if (block == PatchBlock.Pointer && TryReadCodepFrame(line, out var sourceFrame))
            {
                if (DeHackEdCodePointerSources.TryGetActionName(sourceFrame, out var actionName) &&
                    mbfCodePointers.Contains(actionName))
                {
                    return GameCompatibility.Mbf;
                }

                continue;
            }

            if (block == PatchBlock.Thing && TryReadThingBits(line, out var bits))
            {
                if ((bits & DeHackEdThingBits.MbfBitsMask) != 0)
                    return GameCompatibility.Mbf;

                // TRANSLUCENT is a Boom BEX flag at 0x80000000. Unlike the MBF
                // TOUCHY/BOUNCES/FRIEND bits, it only requires Boom rendering.
                if ((bits & DeHackEdThingBits.BoomTranslucentBit) != 0 &&
                    compatibility < GameCompatibility.Boom)
                {
                    compatibility = GameCompatibility.Boom;
                }
            }
        }

        return compatibility;
    }

    private static bool TryReadCodePointer(string line, out string pointerName)
    {
        pointerName = string.Empty;

        var equals = line.IndexOf('=');
        if (equals <= 0)
            return false;

        var left = line.Substring(0, equals).Trim();
        var tokens = left.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 2 ||
            !tokens[0].Equals("FRAME", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(tokens[1], out _))
        {
            return false;
        }

        pointerName = line.Substring(equals + 1).Trim();
        return pointerName.Length != 0;
    }

    private static bool TryReadThingBits(string line, out uint bits)
    {
        bits = 0;

        var equals = line.IndexOf('=');
        if (equals <= 0 ||
            !line.Substring(0, equals).Trim().Equals("Bits", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return DeHackEdThingBits.TryParse(line.Substring(equals + 1), out bits);
    }

    private static bool TryReadTextHeader(string line, out int totalCharacters)
    {
        totalCharacters = 0;
        var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length < 3 ||
            !tokens[0].Equals("Text", StringComparison.OrdinalIgnoreCase) ||
            !int.TryParse(tokens[1], out var oldLength) ||
            !int.TryParse(tokens[2], out var newLength) ||
            oldLength < 0 || newLength < 0)
        {
            return false;
        }

        try
        {
            totalCharacters = checked(oldLength + newLength);
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static int ConsumeTextLine(int remaining, string line)
    {
        if (remaining <= line.Length)
            return 0;

        remaining -= line.Length;
        return remaining - 1;
    }

    private static bool TryReadCodepFrame(string line, out int sourceFrame)
    {
        sourceFrame = -1;

        var equals = line.IndexOf('=');
        if (equals < 0)
            return false;

        var key = line.Substring(0, equals).Trim();
        if (!key.Equals("Codep Frame", StringComparison.OrdinalIgnoreCase))
            return false;

        return int.TryParse(line.Substring(equals + 1).Trim(), out sourceFrame);
    }

    private static bool IsPointerHeader(string line)
    {
        var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length >= 2 &&
               tokens[0].Equals("Pointer", StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(tokens[1], out _);
    }

    private static bool IsThingHeader(string line)
    {
        var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length >= 2 &&
               tokens[0].Equals("Thing", StringComparison.OrdinalIgnoreCase) &&
               int.TryParse(tokens[1], out _);
    }

    private static bool IsClassicBlockHeader(string line)
    {
        var tokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return false;

        return tokens[0].Equals("Frame", StringComparison.OrdinalIgnoreCase) ||
               tokens[0].Equals("Pointer", StringComparison.OrdinalIgnoreCase) ||
               tokens[0].Equals("Sound", StringComparison.OrdinalIgnoreCase) ||
               tokens[0].Equals("Ammo", StringComparison.OrdinalIgnoreCase) ||
               tokens[0].Equals("Weapon", StringComparison.OrdinalIgnoreCase) ||
               tokens[0].Equals("Cheat", StringComparison.OrdinalIgnoreCase) ||
               tokens[0].Equals("Misc", StringComparison.OrdinalIgnoreCase) ||
               tokens[0].Equals("Sprite", StringComparison.OrdinalIgnoreCase);
    }

    private static string StripHashComment(string line)
    {
        if (line == null)
            return string.Empty;

        var hash = line.IndexOf('#');
        return hash < 0 ? line : line.Substring(0, hash);
    }

    private enum PatchBlock
    {
        None,
        Thing,
        Pointer,
        CodePointers
    }
}
