//
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using ManagedDoom.DefinitionPatches.DeHackEd;

namespace ManagedDoom
{
    public static class DeHackEd
    {
        private static Tuple<Action<World, Player, PlayerSpriteDef>, Action<World, Mobj>>[] sourcePointerTable;
        private static Dictionary<string, Action<World, Player, PlayerSpriteDef>> playerActionsByName;
        private static Dictionary<string, Action<World, Mobj>> mobjActionsByName;

        // Sprite block offsets are offsets into tables in specific DOS Doom
        // executables. Doom 1.9 is the normal default for unsigned/BEX-style
        // input, matching reference ports when no usable version is supplied.
        private const int DefaultDoomVersion = 19;
        private const int SpriteNameTableDelta = 22044;
        private static readonly Dictionary<int, int> spriteTextOffsets = new Dictionary<int, int>
        {
            { 16, 129044 },
            { 17, 129044 },
            { 19, 129284 },
            { 20, 129044 },
            { 21, 129380 }
        };

        private static int currentDoomVersion = DefaultDoomVersion;

        // Standard Boom BEX [STRINGS] mnemonics that do not correspond to
        // named DoomString fields. They alias executable hardcoded resource
        // strings and therefore feed the same Resolve()/ReplaceByValue path
        // used by classic Text replacements.
        private static readonly Dictionary<string, string> bexStringAliases =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "BGFLATE1", "FLOOR4_8" },
                { "BGFLATE2", "SFLR6_1" },
                { "BGFLATE3", "MFLR8_4" },
                { "BGFLATE4", "MFLR8_3" },
                { "BGFLAT06", "SLIME16" },
                { "BGFLAT11", "RROCK14" },
                { "BGFLAT20", "RROCK07" },
                { "BGFLAT30", "RROCK17" },
                { "BGFLAT15", "RROCK13" },
                { "BGFLAT31", "RROCK19" },
                { "BGCASTCALL", "BOSSBACK" }
            };

        public static void Initialize(CommandLineArgs args, Wad wad)
        {
            // DoomInfo is process-global and DeHackEd patches mutate it in place.
            // Always begin a new content load from the pristine executable baseline
            // so a patch from a previously loaded WAD cannot leak into the next one.
            DeHackEdBaseline.Restore();

            try
            {
                // Boom/PrBoom load embedded DEHACKED data from the WAD set
                // before explicit command-line -deh/-bex files. This lets an
                // explicitly supplied patch override bundled definitions while
                // preserving deterministic last-assignment-wins behavior.
                if (!args.nodeh.Present)
                {
                    ReadDeHackEdLumps(wad);
                }

                if (args.deh.Present)
                {
                    ReadFiles(args.deh.Value);
                }
            }
            catch
            {
                // A malformed/unsupported patch must not leave half-applied
                // process-global definitions behind after the load fails.
                DeHackEdBaseline.Restore();
                throw;
            }
        }

        private static void ReadFiles(params string[] fileNames)
        {
            string lastFileName = null;
            try
            {
                // Ensure the static members are initialized.
                DoomInfo.Strings.PRESSKEY.GetHashCode();

                Console.Write("Load DeHackEd patches: ");

                foreach (var fileName in fileNames)
                {
                    lastFileName = fileName;
                    ProcessExternalFile(fileName);
                }

                Console.WriteLine("OK (" + string.Join(", ", fileNames.Select(x => Path.GetFileName(x))) + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                throw new Exception("Failed to apply DeHackEd patch: " + lastFileName, e);
            }
        }

        private static void ReadDeHackEdLumps(Wad wad)
        {
            var lumps = new List<int>();

            // WAD entries are stored in file/load order. Process every embedded
            // DEHACKED lump in ascending directory order so independent changes
            // from earlier PWADs are preserved while later assignments still
            // override conflicting values deterministically.
            for (var i = 0; i < wad.LumpInfos.Count; i++)
            {
                if (wad.LumpInfos[i].Name == "DEHACKED")
                {
                    lumps.Add(i);
                }
            }

            if (lumps.Count == 0)
            {
                return;
            }

            // Ensure the static members are initialized.
            DoomInfo.Strings.PRESSKEY.GetHashCode();

            var currentLump = -1;
            try
            {
                Console.Write("Load DeHackEd patches from WAD: ");

                foreach (var lump in lumps)
                {
                    currentLump = lump;

                    // Embedded DEHACKED lumps are not allowed to include
                    // arbitrary files from disk. Reference Boom-family ports
                    // deliberately keep INCLUDE disabled for this source type.
                    ProcessLines(
                        ReadLines(wad.ReadLump(lump)),
                        sourcePath: null,
                        allowIncludes: false,
                        suppressClassicText: false,
                        isIncludedFile: false);
                }

                Console.WriteLine("OK (" + lumps.Count + ")");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed");
                throw new Exception(
                    "Failed to apply embedded DeHackEd patch from WAD lump #" + currentLump + "!",
                    e);
            }
        }

        private static IEnumerable<string> ReadLines(byte[] data)
        {
            using (var ms = new MemoryStream(data))
            using (var sr = new StreamReader(ms))
            {
                for (var line = sr.ReadLine(); line != null; line = sr.ReadLine())
                {
                    yield return line;
                }
            }
        }

        private static void ProcessExternalFile(string fileName)
        {
            var fullPath = Path.GetFullPath(fileName);
            ProcessLines(
                File.ReadLines(fullPath),
                fullPath,
                allowIncludes: true,
                suppressClassicText: false,
                isIncludedFile: false);
        }

        private static void ProcessIncludedFile(
            string parentSourcePath,
            string includeFileName,
            bool suppressClassicText)
        {
            var resolvedPath = ResolveIncludePath(parentSourcePath, includeFileName);
            if (!File.Exists(resolvedPath))
            {
                throw new FileNotFoundException(
                    "Included DeHackEd/BEX patch was not found: " + includeFileName,
                    resolvedPath);
            }

            // Loading an include temporarily switches parser header context.
            // Restore the parent's Doom version afterwards, matching reference
            // implementations which save/restore the parent patch state.
            var savedDoomVersion = currentDoomVersion;
            try
            {
                ProcessLines(
                    File.ReadLines(resolvedPath),
                    resolvedPath,
                    allowIncludes: false,
                    suppressClassicText: suppressClassicText,
                    isIncludedFile: true);
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Failed to include DeHackEd/BEX patch: " + resolvedPath, e);
            }
            finally
            {
                currentDoomVersion = savedDoomVersion;
            }
        }

        private static string ResolveIncludePath(string parentSourcePath, string includeFileName)
        {
            if (Path.IsPathRooted(includeFileName))
            {
                return Path.GetFullPath(includeFileName);
            }

            // Modern Boom-family reference ports first try an include relative
            // to the patch containing the directive. This also makes portable
            // mod folders independent of the process working directory.
            if (!string.IsNullOrEmpty(parentSourcePath))
            {
                var parentDirectory = Path.GetDirectoryName(Path.GetFullPath(parentSourcePath));
                if (!string.IsNullOrEmpty(parentDirectory))
                {
                    var siblingPath = Path.GetFullPath(Path.Combine(parentDirectory, includeFileName));
                    if (File.Exists(siblingPath))
                    {
                        return siblingPath;
                    }
                }
            }

            // Preserve Boom's traditional current-directory fallback.
            return Path.GetFullPath(includeFileName);
        }

        private static void ProcessLines(
            IEnumerable<string> lines,
            string sourcePath,
            bool allowIncludes,
            bool suppressClassicText,
            bool isIncludedFile)
        {
            EnsureCodePointerTables();

            // Each external patch/lump has its own header context. Do not let
            // one file's Doom version influence Sprite offsets in the next.
            currentDoomVersion = DefaultDoomVersion;

            var lineNumber = 0;
            var data = new List<string>();
            var lastBlock = Block.None;
            var lastBlockLine = 0;
            var textCharactersRemaining = -1;
            var bexStringsContinuation = false;

            foreach (var rawLine in lines)
            {
                lineNumber++;

                // A classic DeHackEd Text block is a raw character payload.
                // Once its header has been seen, lines that look like comments
                // or even other block headers are still text until the exact
                // old+new character count has been consumed.
                if (lastBlock == Block.Text && textCharactersRemaining > 0)
                {
                    data.Add(rawLine);
                    textCharactersRemaining = ConsumeTextLine(textCharactersRemaining, rawLine);

                    if (textCharactersRemaining == 0)
                    {
                        ProcessBlockWithOptions(lastBlock, data, lastBlockLine, suppressClassicText);
                        data.Clear();
                        lastBlock = Block.None;
                        lastBlockLine = 0;
                    }

                    continue;
                }

                // Boom BEX [STRINGS] uses an extended line reader: a trailing
                // backslash joins the next physical line before the parser sees
                // comments or section-looking text. Preserve that behavior so a
                // continued value beginning with section-like text cannot be
                // stolen by the generic classic-block scanner.
                if (lastBlock == Block.BexStrings && bexStringsContinuation)
                {
                    data.Add(rawLine);
                    bexStringsContinuation = HasBexLineContinuation(rawLine);
                    continue;
                }

                var trimmed = rawLine.Trim();

                if (trimmed.Length == 0)
                {
                    // A blank logical line terminates the current DeHackEd/BEX
                    // section. Classic Text payloads are handled above before
                    // whitespace/comment parsing, so embedded newlines inside a
                    // Text replacement are not mistaken for section boundaries.
                    //
                    // This mirrors the reference parser's current_section
                    // lifecycle and prevents detached assignments after a blank
                    // line from leaking back into the previous section.
                    if (lastBlock != Block.None)
                    {
                        ProcessBlockWithOptions(lastBlock, data, lastBlockLine, suppressClassicText);
                        data.Clear();
                        lastBlock = Block.None;
                        lastBlockLine = 0;
                        bexStringsContinuation = false;
                    }

                    continue;
                }

                // DeHackEd/BEX comments are line comments. Leading whitespace
                // before '#' is insignificant in reference parsers.
                if (trimmed[0] == '#')
                {
                    continue;
                }

                // While [STRINGS] is active, every nonblank non-comment logical
                // line belongs to that section. The reference parser does not
                // start a new section until [STRINGS] ends on a blank line.
                if (lastBlock == Block.BexStrings)
                {
                    data.Add(rawLine);
                    bexStringsContinuation = HasBexLineContinuation(rawLine);
                    continue;
                }

                if (TryParseIncludeDirective(trimmed, out var includeNotext, out var includeFileName))
                {
                    // INCLUDE is a top-level BEX directive. Flush the previous
                    // block first so the included definitions are applied at
                    // the exact point where the directive appears.
                    ProcessBlockWithOptions(lastBlock, data, lastBlockLine, suppressClassicText);
                    data.Clear();
                    lastBlock = Block.None;
                    lastBlockLine = 0;
                    bexStringsContinuation = false;

                    if (!allowIncludes)
                    {
                        if (isIncludedFile)
                        {
                            Console.WriteLine(
                                "Warning: nested BEX INCLUDE is not allowed; ignoring '" +
                                includeFileName + "'.");
                        }
                        else
                        {
                            Console.WriteLine(
                                "Warning: BEX INCLUDE is not allowed in an embedded DEHACKED lump; ignoring '" +
                                includeFileName + "'.");
                        }

                        continue;
                    }

                    if (includeFileName.Length == 0)
                    {
                        Console.WriteLine("Warning: BEX INCLUDE directive is missing a filename; ignoring it.");
                        continue;
                    }

                    ProcessIncludedFile(sourcePath, includeFileName, includeNotext);
                    continue;
                }

                // Sprite Offset values are executable-version dependent. The
                // classic header is outside any section, so capture it before
                // generic block detection. Other existing parser behavior is
                // intentionally left unchanged.
                if (lastBlock == Block.None &&
                    TryParsePatchHeaderInt(trimmed, "Doom version", out var doomVersion))
                {
                    currentDoomVersion = doomVersion;
                    continue;
                }

                var blockType = GetBlockType(SplitTokens(trimmed));
                if (blockType == Block.None)
                {
                    if (lastBlock != Block.None)
                    {
                        data.Add(rawLine);
                    }

                    continue;
                }

                ProcessBlockWithOptions(lastBlock, data, lastBlockLine, suppressClassicText);
                data.Clear();
                data.Add(trimmed);
                lastBlock = blockType;
                lastBlockLine = lineNumber;
                bexStringsContinuation = false;

                if (blockType == Block.Text)
                {
                    try
                    {
                        var lengths = ParseTextHeader(trimmed);
                        textCharactersRemaining = checked(lengths.Item1 + lengths.Item2);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(
                            "Failed to process block: Text (line " + lineNumber + ")", e);
                    }

                    if (textCharactersRemaining == 0)
                    {
                        ProcessBlockWithOptions(lastBlock, data, lastBlockLine, suppressClassicText);
                        data.Clear();
                        lastBlock = Block.None;
                        lastBlockLine = 0;
                    }
                }
            }

            if (lastBlock == Block.Text && textCharactersRemaining > 0)
            {
                throw new Exception(
                    "Failed to process block: Text (line " + lastBlockLine + ")",
                    new EndOfStreamException(
                        "Unexpected end of DeHackEd Text block; " + textCharactersRemaining + " character(s) are missing."));
            }

            ProcessBlockWithOptions(lastBlock, data, lastBlockLine, suppressClassicText);
        }

        private static void EnsureCodePointerTables()
        {
            if (sourcePointerTable != null)
            {
                return;
            }

            sourcePointerTable = new Tuple<Action<World, Player, PlayerSpriteDef>, Action<World, Mobj>>[DoomInfo.States.Length];
            playerActionsByName = new Dictionary<string, Action<World, Player, PlayerSpriteDef>>(StringComparer.OrdinalIgnoreCase);
            mobjActionsByName = new Dictionary<string, Action<World, Mobj>>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < sourcePointerTable.Length; i++)
            {
                var playerAction = DoomInfo.States[i].PlayerAction;
                var mobjAction = DoomInfo.States[i].MobjAction;
                sourcePointerTable[i] = Tuple.Create(playerAction, mobjAction);

                if (playerAction != null)
                {
                    var name = StripActionPrefix(playerAction.Method.Name);
                    playerActionsByName.TryAdd(name, playerAction);
                }

                if (mobjAction != null)
                {
                    var name = StripActionPrefix(mobjAction.Method.Name);
                    mobjActionsByName.TryAdd(name, mobjAction);
                }
            }
        }

        private static int ConsumeTextLine(int remaining, string line)
        {
            if (remaining <= line.Length)
            {
                return 0;
            }

            remaining -= line.Length;

            // ReadLine removes the physical newline. DeHackEd Text lengths count
            // the logical line break as one character, matching the existing
            // ProcessTextBlock reconstruction.
            return remaining - 1;
        }

        private static void ProcessBlockWithOptions(
            Block type,
            List<string> data,
            int lineNumber,
            bool suppressClassicText)
        {
            // INCLUDE NOTEXT suppresses only classic DeHackEd Text chunks.
            // BEX [STRINGS] is a separate section and remains active.
            if (suppressClassicText && type == Block.Text)
            {
                return;
            }

            ProcessBlock(type, data, lineNumber);
        }

        private static void ProcessBlock(Block type, List<string> data, int lineNumber)
        {
            try
            {
                switch (type)
                {
                    case Block.Thing:
                        ProcessThingBlock(data);
                        break;
                    case Block.Frame:
                        ProcessFrameBlock(data);
                        break;
                    case Block.Pointer:
                        ProcessPointerBlock(data);
                        break;
                    case Block.Sound:
                        ProcessSoundBlock(data);
                        break;
                    case Block.Ammo:
                        ProcessAmmoBlock(data);
                        break;
                    case Block.Weapon:
                        ProcessWeaponBlock(data);
                        break;
                    case Block.Cheat:
                        ProcessCheatBlock(data);
                        break;
                    case Block.Misc:
                        ProcessMiscBlock(data);
                        break;
                    case Block.Text:
                        ProcessTextBlock(data);
                        break;
                    case Block.Sprite:
                        ProcessSpriteBlock(data);
                        break;
                    case Block.BexStrings:
                        ProcessBexStringsBlock(data);
                        break;
                    case Block.BexPars:
                        ProcessBexParsBlock(data);
                        break;
                    case Block.BexCodePointers:
                        ProcessBexCodePointersBlock(data);
                        break;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to process block: " + type + " (line " + lineNumber + ")", e);
            }
        }

        private static void ProcessThingBlock(List<string> data)
        {
            var deHackEdThingNumber = ParseHeaderNumber(data[0], "Thing");
            var thingNumber = deHackEdThingNumber - 1;

            // Classic DeHackEd numbers Things from 1. Reference parsers warn
            // and ignore an out-of-range section instead of aborting the
            // entire patch, so keep later valid sections usable.
            if (thingNumber < 0 || thingNumber >= DoomInfo.MobjInfos.Length)
            {
                Console.WriteLine(
                    "Warning: invalid DeHackEd Thing number " + deHackEdThingNumber +
                    "; ignoring Thing block.");
                return;
            }

            var info = DoomInfo.MobjInfos[thingNumber];
            var dic = GetKeyValuePairs(data);

            info.DoomEdNum = GetInt(dic, "ID #", info.DoomEdNum);
            info.SpawnState = (MobjState)GetInt(dic, "Initial frame", (int)info.SpawnState);
            info.SpawnHealth = GetInt(dic, "Hit points", info.SpawnHealth);
            info.SeeState = (MobjState)GetInt(dic, "First moving frame", (int)info.SeeState);
            info.SeeSound = (Sfx)GetInt(dic, "Alert sound", (int)info.SeeSound);
            info.ReactionTime = GetInt(dic, "Reaction time", info.ReactionTime);
            info.AttackSound = (Sfx)GetInt(dic, "Attack sound", (int)info.AttackSound);
            info.PainState = (MobjState)GetInt(dic, "Injury frame", (int)info.PainState);
            info.PainChance = GetInt(dic, "Pain chance", info.PainChance);
            info.PainSound = (Sfx)GetInt(dic, "Pain sound", (int)info.PainSound);
            info.MeleeState = (MobjState)GetInt(dic, "Close attack frame", (int)info.MeleeState);
            info.MissileState = (MobjState)GetInt(dic, "Far attack frame", (int)info.MissileState);
            info.DeathState = (MobjState)GetInt(dic, "Death frame", (int)info.DeathState);
            info.XdeathState = (MobjState)GetInt(dic, "Exploding frame", (int)info.XdeathState);
            info.DeathSound = (Sfx)GetInt(dic, "Death sound", (int)info.DeathSound);
            info.Speed = GetInt(dic, "Speed", info.Speed);
            info.Radius = new Fixed(GetInt(dic, "Width", info.Radius.Data));
            info.Height = new Fixed(GetInt(dic, "Height", info.Height.Data));
            info.Mass = GetInt(dic, "Mass", info.Mass);
            info.Damage = GetInt(dic, "Missile damage", info.Damage);
            info.ActiveSound = (Sfx)GetInt(dic, "Action sound", (int)info.ActiveSound);
            info.Flags = (MobjFlags)GetInt(dic, "Bits", (int)info.Flags);
            info.Raisestate = (MobjState)GetInt(dic, "Respawn frame", (int)info.Raisestate);
        }

        private static void ProcessFrameBlock(List<string> data)
        {
            var frameNumber = ParseHeaderNumber(data[0], "Frame");
            if (!IsValidStateIndex(frameNumber))
            {
                Console.WriteLine("Warning: invalid DeHackEd Frame number " + frameNumber + "; ignoring Frame block.");
                return;
            }

            var info = DoomInfo.States[frameNumber];
            var dic = GetKeyValuePairs(data);

            info.Sprite = (Sprite)GetInt(dic, "Sprite number", (int)info.Sprite);
            info.Frame = GetInt(dic, "Sprite subnumber", info.Frame);
            info.Tics = GetInt(dic, "Duration", info.Tics);
            info.Next = (MobjState)GetInt(dic, "Next frame", (int)info.Next);
            info.Misc1 = GetInt(dic, "Unknown 1", info.Misc1);
            info.Misc2 = GetInt(dic, "Unknown 2", info.Misc2);
        }

        private static void ProcessPointerBlock(List<string> data)
        {
            if (!TryParsePointerHeader(data[0], out var targetFrameNumber))
            {
                Console.WriteLine("Warning: malformed DeHackEd Pointer header '" + data[0] + "'; ignoring Pointer block.");
                return;
            }

            if (!IsValidStateIndex(targetFrameNumber))
            {
                Console.WriteLine("Warning: invalid DeHackEd Pointer target frame " + targetFrameNumber + "; ignoring Pointer block.");
                return;
            }

            var dic = GetKeyValuePairs(data);
            var sourceFrameNumber = GetInt(dic, "Codep Frame", -1);
            if (sourceFrameNumber == -1)
            {
                return;
            }

            if (!IsValidStateIndex(sourceFrameNumber))
            {
                Console.WriteLine("Warning: invalid DeHackEd Codep Frame " + sourceFrameNumber + "; ignoring code pointer assignment.");
                return;
            }

            var info = DoomInfo.States[targetFrameNumber];
            var source = sourcePointerTable[sourceFrameNumber];

            // Classic DeHackEd Pointer sections copy from the immutable original
            // executable code-pointer table, not from a state that may already
            // have been modified by an earlier Pointer section in this patch.
            info.PlayerAction = source.Item1;
            info.MobjAction = source.Item2;
        }

        private static void ProcessSoundBlock(List<string> data)
        {
            var soundNumber = ParseHeaderNumber(data[0], "Sound");
            if (soundNumber < 0 || soundNumber >= DoomInfo.DeHackEdSoundInfos.Length)
            {
                Console.WriteLine(
                    "Warning: invalid DeHackEd Sound number " + soundNumber +
                    "; ignoring Sound block.");
                return;
            }

            var info = DoomInfo.DeHackEdSoundInfos[soundNumber];
            var dic = GetKeyValuePairs(data);

            // Offset and Zero 4 were raw executable/data pointers in the DOS
            // sound table. Modern reference ports do not safely emulate those
            // addresses, so accept the fields without dereferencing them.
            WarnUnsupportedSoundField(dic, "Offset");
            WarnUnsupportedSoundField(dic, "Zero 4");

            SetSoundField(dic, "Zero/One", value => info.Singularity = value);
            SetSoundField(dic, "Value", value => info.Priority = value);

            // Zero 1 was a raw pointer to another sfxinfo_t. Keep its integer
            // value as legacy metadata only; never treat it as a C# reference.
            SetSoundField(dic, "Zero 1", value => info.LegacyLink = value);
            SetSoundField(dic, "Zero 2", value => info.Pitch = value);
            SetSoundField(dic, "Zero 3", value => info.Volume = value);
            SetSoundField(dic, "Neg. One 1", value => info.Usefulness = value);
            SetSoundField(dic, "Neg. One 2", value => info.LumpNum = value);
        }

        private static void SetSoundField(
            Dictionary<string, string> dic,
            string key,
            Action<int> setter)
        {
            if (!dic.TryGetValue(key, out var text))
            {
                return;
            }

            if (!TryParseLeadingInt(text, out var value))
            {
                Console.WriteLine(
                    "Warning: invalid DeHackEd Sound value for '" + key + "': " + text);
                return;
            }

            setter(value);
        }

        private static void WarnUnsupportedSoundField(
            Dictionary<string, string> dic,
            string key)
        {
            if (dic.ContainsKey(key))
            {
                Console.WriteLine(
                    "Warning: DeHackEd Sound field '" + key +
                    "' is a legacy pointer field and is not supported by ManagedDoom.");
            }
        }

        private static void ProcessAmmoBlock(List<string> data)
        {
            // ProcessLines normally always supplies the block header as data[0],
            // but keep malformed/empty input from turning a bad patch into an
            // engine-level IndexOutOfRangeException.
            if (data == null || data.Count == 0)
            {
                Console.WriteLine("Warning: empty DeHackEd Ammo block; ignoring it.");
                return;
            }

            var ammoNumber = ParseHeaderNumber(data[0], "Ammo");
            var max = DoomInfo.AmmoInfos.Max;
            var clip = DoomInfo.AmmoInfos.Clip;

            // Classic DeHackEd numbers ammo types from zero. Invalid section
            // numbers are ignored so one bad block cannot abort the rest of the
            // patch. Check both tables before indexing either one.
            if (ammoNumber < 0 || ammoNumber >= max.Length || ammoNumber >= clip.Length)
            {
                Console.WriteLine(
                    "Warning: invalid DeHackEd Ammo number " + ammoNumber +
                    "; ignoring Ammo block.");
                return;
            }

            var dic = GetKeyValuePairs(data);
            max[ammoNumber] = GetInt(dic, "Max ammo", max[ammoNumber]);
            clip[ammoNumber] = GetInt(dic, "Per ammo", clip[ammoNumber]);
        }

        private static void ProcessWeaponBlock(List<string> data)
        {
            if (data == null || data.Count == 0)
            {
                Console.WriteLine("Warning: empty DeHackEd Weapon block; ignoring it.");
                return;
            }

            var weaponNumber = ParseHeaderNumber(data[0], "Weapon");
            if (weaponNumber < 0 || weaponNumber >= DoomInfo.WeaponInfos.Length)
            {
                Console.WriteLine(
                    "Warning: invalid DeHackEd Weapon number " + weaponNumber +
                    "; ignoring Weapon block.");
                return;
            }

            var info = DoomInfo.WeaponInfos[weaponNumber];
            var dic = GetKeyValuePairs(data);

            info.Ammo = (AmmoType)GetInt(dic, "Ammo type", (int)info.Ammo);
            info.UpState = (MobjState)GetInt(dic, "Deselect frame", (int)info.UpState);
            info.DownState = (MobjState)GetInt(dic, "Select frame", (int)info.DownState);
            info.ReadyState = (MobjState)GetInt(dic, "Bobbing frame", (int)info.ReadyState);
            info.AttackState = (MobjState)GetInt(dic, "Shooting frame", (int)info.AttackState);
            info.FlashState = (MobjState)GetInt(dic, "Firing frame", (int)info.FlashState);
        }

        private static void ProcessCheatBlock(List<string> data)
        {
        }

        private static void ProcessMiscBlock(List<string> data)
        {
            var dic = GetKeyValuePairs(data);

            DoomInfo.DeHackEdConst.InitialHealth = GetInt(dic, "Initial Health", DoomInfo.DeHackEdConst.InitialHealth);
            DoomInfo.DeHackEdConst.InitialBullets = GetInt(dic, "Initial Bullets", DoomInfo.DeHackEdConst.InitialBullets);
            DoomInfo.DeHackEdConst.MaxHealth = GetInt(dic, "Max Health", DoomInfo.DeHackEdConst.MaxHealth);
            DoomInfo.DeHackEdConst.MaxArmor = GetInt(dic, "Max Armor", DoomInfo.DeHackEdConst.MaxArmor);
            DoomInfo.DeHackEdConst.GreenArmorClass = GetInt(dic, "Green Armor Class", DoomInfo.DeHackEdConst.GreenArmorClass);
            DoomInfo.DeHackEdConst.BlueArmorClass = GetInt(dic, "Blue Armor Class", DoomInfo.DeHackEdConst.BlueArmorClass);
            DoomInfo.DeHackEdConst.MaxSoulsphere = GetInt(dic, "Max Soulsphere", DoomInfo.DeHackEdConst.MaxSoulsphere);
            DoomInfo.DeHackEdConst.SoulsphereHealth = GetInt(dic, "Soulsphere Health", DoomInfo.DeHackEdConst.SoulsphereHealth);
            DoomInfo.DeHackEdConst.MegasphereHealth = GetInt(dic, "Megasphere Health", DoomInfo.DeHackEdConst.MegasphereHealth);
            DoomInfo.DeHackEdConst.GodModeHealth = GetInt(dic, "God Mode Health", DoomInfo.DeHackEdConst.GodModeHealth);
            DoomInfo.DeHackEdConst.IdfaArmor = GetInt(dic, "IDFA Armor", DoomInfo.DeHackEdConst.IdfaArmor);
            DoomInfo.DeHackEdConst.IdfaArmorClass = GetInt(dic, "IDFA Armor Class", DoomInfo.DeHackEdConst.IdfaArmorClass);
            DoomInfo.DeHackEdConst.IdkfaArmor = GetInt(dic, "IDKFA Armor", DoomInfo.DeHackEdConst.IdkfaArmor);
            DoomInfo.DeHackEdConst.IdkfaArmorClass = GetInt(dic, "IDKFA Armor Class", DoomInfo.DeHackEdConst.IdkfaArmorClass);
            DoomInfo.DeHackEdConst.BfgCellsPerShot = GetInt(dic, "BFG Cells/Shot", DoomInfo.DeHackEdConst.BfgCellsPerShot);
            DoomInfo.DeHackEdConst.MonstersInfight = GetInt(dic, "Monsters Infight", 0) == 221;
        }

        private static void ProcessTextBlock(List<string> data)
        {
            if (data == null || data.Count == 0)
            {
                throw new FormatException("Empty DeHackEd Text block.");
            }

            var lengths = ParseTextHeader(data[0]);
            var oldLength = lengths.Item1;
            var newLength = lengths.Item2;
            var payloadLength = checked(oldLength + newLength);
            var payload = ReadClassicTextPayload(data, payloadLength);

            var original = payload.Substring(0, oldLength);
            var replacement = payload.Substring(oldLength, newLength);

            DoomString.ReplaceByValue(original, replacement);
        }

        private static string ReadClassicTextPayload(List<string> data, int expectedLength)
        {
            if (expectedLength == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder(expectedLength);

            for (var line = 1; line < data.Count && result.Length < expectedLength; line++)
            {
                if (line > 1 && result.Length < expectedLength)
                {
                    result.Append('\n');
                }

                var remaining = expectedLength - result.Length;
                if (remaining <= 0)
                {
                    break;
                }

                var value = data[line];
                if (value.Length <= remaining)
                {
                    result.Append(value);
                }
                else
                {
                    result.Append(value, 0, remaining);
                }
            }

            if (result.Length != expectedLength)
            {
                throw new EndOfStreamException(
                    "Unexpected end of DeHackEd Text block; " +
                    (expectedLength - result.Length) + " character(s) are missing.");
            }

            return result.ToString();
        }

        private static void ProcessSpriteBlock(List<string> data)
        {
            if (data == null || data.Count == 0)
            {
                Console.WriteLine("Warning: empty DeHackEd Sprite block; ignoring it.");
                return;
            }

            var spriteNumber = ParseHeaderNumber(data[0], "Sprite");
            if (spriteNumber < 0 || spriteNumber >= DoomInfo.SpriteNames.Length)
            {
                Console.WriteLine(
                    "Warning: invalid DeHackEd Sprite number " + spriteNumber +
                    "; ignoring Sprite block.");
                return;
            }

            var dic = GetKeyValuePairs(data);
            var offset = GetInt(dic, "Offset", 0);

            // Reference implementations only apply a Sprite rename for a
            // positive executable offset. Missing/zero/negative values are
            // therefore harmless no-ops.
            if (offset <= 0)
            {
                return;
            }

            var textOffset = GetSpriteTextOffset(currentDoomVersion);

            // DeHackEd stored sprite-name entries eight bytes apart. Preserve
            // the integer-division behavior of the original/reference parser
            // rather than imposing an extra alignment rule that old patches
            // never had.
            var sourceSpriteNumber = (offset - textOffset - SpriteNameTableDelta) / 8;
            if (sourceSpriteNumber < 0 || sourceSpriteNumber >= DoomInfo.SpriteNames.Length)
            {
                Console.WriteLine(
                    "Warning: DeHackEd Sprite Offset " + offset +
                    " resolves to invalid sprite name " + sourceSpriteNumber +
                    "; ignoring replacement.");
                return;
            }

            // Offset points at the pristine executable table, not at a name
            // that may already have been modified earlier in this patch.
            var sourceName = DoomInfo.SpriteNames[sourceSpriteNumber].OriginalValue;
            DoomInfo.SpriteNames[spriteNumber].SetDirectReplacement(sourceName);
        }

        private static int GetSpriteTextOffset(int doomVersion)
        {
            if (spriteTextOffsets.TryGetValue(doomVersion, out var offset))
            {
                return offset;
            }

            Console.WriteLine(
                "Warning: unknown DeHackEd Doom version " + doomVersion +
                "; assuming Doom 1.9 Sprite offsets.");
            return spriteTextOffsets[DefaultDoomVersion];
        }

        private static void ProcessBexStringsBlock(List<string> data)
        {
            if (data == null || data.Count == 0)
            {
                return;
            }

            foreach (var line in ReadBexStringLogicalLines(data.Skip(1)))
            {
                var eqPos = line.IndexOf('=');
                if (eqPos == -1)
                {
                    Console.WriteLine(
                        "Warning: malformed assignment in [STRINGS] block: '" + line + "'.");
                    continue;
                }

                var name = line.Substring(0, eqPos).Trim();
                var value = line.Substring(eqPos + 1).Trim();

                if (name.Length == 0)
                {
                    Console.WriteLine(
                        "Warning: empty mnemonic in [STRINGS] block; ignoring assignment.");
                    continue;
                }

                if (DoomString.TryReplaceByName(name, value))
                {
                    continue;
                }

                if (bexStringAliases.TryGetValue(name, out var original))
                {
                    DoomString.ReplaceByValue(original, value);
                }
                // Unknown BEX mnemonics are intentionally ignored. Reference
                // parsers simply leave them unmatched so later valid entries
                // in the same section can still apply.
            }
        }

        private static IEnumerable<string> ReadBexStringLogicalLines(IEnumerable<string> physicalLines)
        {
            var buffer = new StringBuilder();
            var continuation = false;

            foreach (var physicalLine in physicalLines)
            {
                var start = 0;

                // BEX continuation discards indentation on the next physical
                // line, but preserves blanks that occurred before the
                // backslash on the previous one.
                if (continuation)
                {
                    while (start < physicalLine.Length && char.IsWhiteSpace(physicalLine[start]))
                    {
                        start++;
                    }
                }

                continuation = false;

                for (var i = start; i < physicalLine.Length; i++)
                {
                    var ch = physicalLine[i];
                    if (ch != '\\')
                    {
                        buffer.Append(ch);
                        continue;
                    }

                    if (i + 1 >= physicalLine.Length)
                    {
                        // Backslash immediately before the physical newline:
                        // join the next line without inserting a newline.
                        continuation = true;
                        break;
                    }

                    var escaped = physicalLine[++i];
                    if (escaped == 'n')
                    {
                        buffer.Append('\n');
                    }
                    else
                    {
                        // The reference extended reader consumes the backslash
                        // for other escape pairs and keeps the escaped char.
                        buffer.Append(escaped);
                    }
                }

                if (!continuation)
                {
                    yield return buffer.ToString();
                    buffer.Clear();
                }
            }

            // At EOF the reference reader returns the accumulated logical line
            // even if its final physical character was a continuation slash.
            if (buffer.Length > 0 || continuation)
            {
                yield return buffer.ToString();
            }
        }

        private static bool HasBexLineContinuation(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            var trailingBackslashes = 0;
            for (var i = line.Length - 1; i >= 0 && line[i] == '\\'; i--)
            {
                trailingBackslashes++;
            }

            // An odd number means the final backslash is not itself escaped.
            return (trailingBackslashes & 1) != 0;
        }

        private static void ProcessBexParsBlock(List<string> data)
        {
            foreach (var line in data.Skip(1))
            {
                var split = SplitTokens(line);

                if (split.Length < 3 ||
                    !split[0].Equals("par", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Boom BEX defines two forms:
                //   par <map> <seconds>
                //   par <episode> <map> <seconds>
                // Prefer the three-value Doom I form when the third value is
                // numeric.  This also naturally allows an inline '# comment'
                // after the two-value Doom II form.
                if (split.Length >= 4 &&
                    int.TryParse(split[1], out var episode) &&
                    int.TryParse(split[2], out var map) &&
                    int.TryParse(split[3], out var doom1Seconds))
                {
                    if (episode < 1 || episode > DoomInfo.ParTimes.Doom1.Count)
                    {
                        continue;
                    }

                    var episodePars = DoomInfo.ParTimes.Doom1[episode - 1];
                    if (map < 1 || map > episodePars.Count)
                    {
                        continue;
                    }

                    episodePars[map - 1] = doom1Seconds;
                    continue;
                }

                if (!int.TryParse(split[1], out var doom2Map) ||
                    !int.TryParse(split[2], out var doom2Seconds))
                {
                    continue;
                }

                if (doom2Map < 1 || doom2Map > DoomInfo.ParTimes.Doom2.Count)
                {
                    continue;
                }

                DoomInfo.ParTimes.Doom2[doom2Map - 1] = doom2Seconds;
            }
        }

        private static void ProcessBexCodePointersBlock(List<string> data)
        {
            foreach (var line in data.Skip(1))
            {
                var eqPos = line.IndexOf('=');
                if (eqPos == -1)
                {
                    continue;
                }

                var left = line.Substring(0, eqPos).Trim();
                var pointerName = line.Substring(eqPos + 1).Trim();

                var leftSplit = SplitTokens(left);
                if (leftSplit.Length < 2 || !leftSplit[0].Equals("FRAME", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int frameNumber;
                if (!int.TryParse(leftSplit[1], out frameNumber))
                {
                    continue;
                }

                if (frameNumber < 0 || frameNumber >= DoomInfo.States.Length)
                {
                    continue;
                }

                var info = DoomInfo.States[frameNumber];

                if (!TryAssignBexCodePointer(info, pointerName))
                {
                    Console.WriteLine("Warning: unknown code pointer '" + pointerName + "' for frame " + frameNumber + " in [CODEPTR] block");
                }
            }
        }


        private static bool TryAssignBexCodePointer(MobjStateDef state, string pointerName)
        {
            // Boom BEX defines NULL as a real code-pointer mnemonic. It does
            // not mean "unknown": it explicitly removes the state's action.
            // ManagedDoom stores the two possible signatures separately, so a
            // NULL assignment must clear both delegates.
            if (pointerName.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            {
                state.PlayerAction = null;
                state.MobjAction = null;
                return true;
            }

            if (mobjActionsByName.TryGetValue(pointerName, out var mobjAction))
            {
                // Doom stores one code pointer per state. ManagedDoom splits the
                // two signatures into separate delegates, so assigning an actor
                // action must clear any player-action delegate left by the
                // original state or an earlier [CODEPTR] assignment.
                state.PlayerAction = null;
                state.MobjAction = mobjAction;
                return true;
            }

            if (playerActionsByName.TryGetValue(pointerName, out var playerAction))
            {
                state.MobjAction = null;
                state.PlayerAction = playerAction;
                return true;
            }

            // Unknown mnemonics are non-destructive: keep the previous pointer
            // and let the caller report a warning, matching the parser's normal
            // "ignore bad assignment and continue" behavior.
            return false;
        }

        private static string StripActionPrefix(string methodName)
        {
            if (methodName.Length > 2 && methodName[0] == 'A' && methodName[1] == '_')
            {
                return methodName.Substring(2);
            }

            return methodName;
        }

        private static bool TryParseIncludeDirective(
            string line,
            out bool suppressClassicText,
            out string includeFileName)
        {
            suppressClassicText = false;
            includeFileName = string.Empty;

            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            var index = 0;
            while (index < line.Length && !char.IsWhiteSpace(line[index]))
            {
                index++;
            }

            var keyword = line.Substring(0, index);
            if (!keyword.Equals("INCLUDE", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var remainder = index < line.Length ? line.Substring(index).Trim() : string.Empty;

            if (remainder.Length >= 6 &&
                remainder.Substring(0, 6).Equals("NOTEXT", StringComparison.OrdinalIgnoreCase) &&
                (remainder.Length == 6 || char.IsWhiteSpace(remainder[6])))
            {
                suppressClassicText = true;
                remainder = remainder.Length == 6 ? string.Empty : remainder.Substring(6).TrimStart();
            }

            // Reference-compatible convenience: quoted include names may
            // contain spaces. For an unquoted name the rest of the logical
            // line is the filename, matching Boom's Line2 behavior.
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

        private static string[] SplitTokens(string line)
        {
            return line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool TryParsePatchHeaderInt(string line, string key, out int value)
        {
            value = 0;

            var eqPos = line.IndexOf('=');
            if (eqPos <= 0)
            {
                return false;
            }

            var left = line.Substring(0, eqPos).Trim();
            if (!left.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return TryParseLeadingInt(line.Substring(eqPos + 1), out value);
        }

        private static int ParseHeaderNumber(string header, string blockName)
        {
            var split = SplitTokens(header);
            if (split.Length < 2 ||
                !split[0].Equals(blockName, StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(split[1], out var value))
            {
                throw new FormatException("Malformed " + blockName + " block header: " + header);
            }

            return value;
        }

        private static Tuple<int, int> ParseTextHeader(string header)
        {
            var split = SplitTokens(header);
            if (split.Length < 3 ||
                !split[0].Equals("Text", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(split[1], out var oldLength) ||
                !int.TryParse(split[2], out var newLength) ||
                oldLength < 0 || newLength < 0)
            {
                throw new FormatException("Malformed Text block header: " + header);
            }

            return Tuple.Create(oldLength, newLength);
        }

        private static bool TryParsePointerHeader(string header, out int targetFrameNumber)
        {
            targetFrameNumber = 0;

            var split = SplitTokens(header);
            if (split.Length < 2 ||
                !split[0].Equals("Pointer", StringComparison.OrdinalIgnoreCase) ||
                !int.TryParse(split[1], out _))
            {
                return false;
            }

            var start = header.IndexOf('(');
            var end = header.IndexOf(')', start + 1);
            if (start == -1 || end == -1 || end <= start + 1)
            {
                return false;
            }

            var contents = header.Substring(start + 1, end - start - 1);
            var contentsSplit = SplitTokens(contents);

            // DOS/reference parsers consume the word inside the parentheses but
            // do not require it to literally be "Frame". The numeric target is
            // the second token. Keep that permissive behavior for old patches.
            return contentsSplit.Length >= 2 &&
                   int.TryParse(contentsSplit[1], out targetFrameNumber);
        }

        private static bool IsValidStateIndex(int frameNumber)
        {
            return frameNumber >= 0 && frameNumber < DoomInfo.States.Length;
        }

        private static bool TryParseLeadingInt(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var span = text.AsSpan().TrimStart();
            var length = 0;
            if (span.Length > 0 && (span[0] == '+' || span[0] == '-'))
            {
                length++;
            }

            var digitStart = length;
            while (length < span.Length && char.IsDigit(span[length]))
            {
                length++;
            }

            if (length == digitStart)
            {
                return false;
            }

            return int.TryParse(span.Slice(0, length), out value);
        }

        private static Block GetBlockType(string[] split)
        {
            if (IsThingBlockStart(split))
            {
                return Block.Thing;
            }
            else if (IsFrameBlockStart(split))
            {
                return Block.Frame;
            }
            else if (IsPointerBlockStart(split))
            {
                return Block.Pointer;
            }
            else if (IsSoundBlockStart(split))
            {
                return Block.Sound;
            }
            else if (IsAmmoBlockStart(split))
            {
                return Block.Ammo;
            }
            else if (IsWeaponBlockStart(split))
            {
                return Block.Weapon;
            }
            else if (IsCheatBlockStart(split))
            {
                return Block.Cheat;
            }
            else if (IsMiscBlockStart(split))
            {
                return Block.Misc;
            }
            else if (IsTextBlockStart(split))
            {
                return Block.Text;
            }
            else if (IsSpriteBlockStart(split))
            {
                return Block.Sprite;
            }
            else if (IsBexStringsBlockStart(split))
            {
                return Block.BexStrings;
            }
            else if (IsBexParsBlockStart(split))
            {
                return Block.BexPars;
            }
            else if (IsBexCodePointersBlockStart(split))
            {
                return Block.BexCodePointers;
            }
            else
            {
                return Block.None;
            }
        }

        private static bool IsThingBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Thing", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // sscanf("Thing %i") used by classic/reference parsers accepts a
            // leading sign too. Recognize it as a Thing block first; bounds
            // handling belongs to ProcessThingBlock.
            return int.TryParse(split[1], out _);
        }

        private static bool IsFrameBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Frame", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Classic parsers use %i here, so signed frame indices are still
            // recognized as Frame block headers and can be rejected cleanly by
            // ProcessFrameBlock instead of leaking into the previous block.
            if (!int.TryParse(split[1], out _))
            {
                return false;
            }

            // BEX [CODEPTR] assignments use the syntax:
            //     FRAME <number> = <mnemonic>
            // They are data inside the current [CODEPTR] block, not classic
            // DeHackEd "Frame <number>" block headers.  Without this check
            // the generic block scanner steals the assignment before
            // ProcessBexCodePointersBlock can see it.
            for (var i = 2; i < split.Length; i++)
            {
                // Accept all normal BEX spacing variants, including
                // "FRAME 174 =Chase" where '=' is attached to the mnemonic.
                // Such a line belongs to [CODEPTR], not to a classic Frame block.
                if (split[i].IndexOf('=') != -1)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsPointerBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Pointer", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool IsSoundBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Sound", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Reference parsers use %i, so recognize signed indices as Sound
            // headers first and let ProcessSoundBlock perform range handling.
            return int.TryParse(split[1], out _);
        }

        private static bool IsAmmoBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Ammo", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Classic/reference parsers use %i here, so signed indices must
            // still be recognized as Ammo headers and rejected by the block
            // handler rather than being consumed as data by a previous block.
            return int.TryParse(split[1], out _);
        }

        private static bool IsWeaponBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Weapon", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Classic parsers use %i for the section number. Recognize signed
            // values here and let ProcessWeaponBlock validate the actual range
            // so an invalid Weapon header cannot leak into a previous block.
            return int.TryParse(split[1], out _);
        }

        private static bool IsCheatBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Cheat", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (split[1] != "0")
            {
                return false;
            }

            return true;
        }

        private static bool IsMiscBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Misc", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (split[1] != "0")
            {
                return false;
            }

            return true;
        }

        private static bool IsTextBlockStart(string[] split)
        {
            // Reference DeHackEd parsers select the section by its first token
            // and let the Text parser validate the two lengths afterwards.
            // Doing the same here ensures malformed headers such as
            // "Text -1 5" or "Text nope 5" are reported as Text errors
            // instead of silently becoming ordinary data in another block.
            return split.Length > 0 &&
                   split[0].Equals("Text", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSpriteBlockStart(string[] split)
        {
            if (split.Length < 2)
            {
                return false;
            }

            if (!split[0].Equals("Sprite", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Classic parsers use %i for the section number. Recognize signed
            // values here and let ProcessSpriteBlock handle the valid range so
            // an invalid Sprite header cannot leak into the previous block.
            return int.TryParse(split[1], out _);
        }

        private static bool IsBexStringsBlockStart(string[] split)
        {
            if (split[0].Equals("[STRINGS]", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsBexParsBlockStart(string[] split)
        {
            if (split[0].Equals("[PARS]", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsBexCodePointersBlockStart(string[] split)
        {
            if (split[0].Equals("[CODEPTR]", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsNumber(string value)
        {
            foreach (var ch in value)
            {
                if (!('0' <= ch && ch <= '9'))
                {
                    return false;
                }
            }

            return true;
        }

        private static Dictionary<string, string> GetKeyValuePairs(List<string> data)
        {
            var dic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in data)
            {
                var eqPos = line.IndexOf('=');
                if (eqPos > 0)
                {
                    dic[line.Substring(0, eqPos).Trim()] = line.Substring(eqPos + 1).Trim();
                }
            }
            return dic;
        }

        private static int GetInt(Dictionary<string, string> dic, string key, int defaultValue)
        {
            string value;
            if (dic.TryGetValue(key, out value))
            {
                int intValue;
                if (TryParseLeadingInt(value, out intValue))
                {
                    return intValue;
                }
            }

            return defaultValue;
        }



        private enum Block
        {
            None,
            Thing,
            Frame,
            Pointer,
            Sound,
            Ammo,
            Weapon,
            Cheat,
            Misc,
            Text,
            Sprite,
            BexStrings,
            BexPars,
            BexCodePointers
        }
    }
}
