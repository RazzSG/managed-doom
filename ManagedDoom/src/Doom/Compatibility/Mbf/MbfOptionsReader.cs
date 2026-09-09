using System;
using System.Globalization;
using System.Text;
using ManagedDoom;

namespace ManagedDoom.Compatibility.Mbf;

/// <summary>
/// Parses the effective (last loaded) MBF OPTIONS lump. Unknown and malformed
/// entries are ignored so newer WADs remain loadable as support expands.
/// </summary>
public static class MbfOptionsReader
{
    public static MbfOptions Read(Wad wad)
    {
        if (wad == null)
            throw new ArgumentNullException(nameof(wad));

        var result = new MbfOptions();
        var lumpNumber = wad.GetLumpNumber("OPTIONS");
        if (lumpNumber == -1)
            return result;

        var text = Encoding.ASCII.GetString(wad.ReadLump(lumpNumber));
        Apply(text, result);
        return result;
    }

    public static MbfOptions Parse(string text)
    {
        var result = new MbfOptions();
        Apply(text, result);
        return result;
    }

    private static void Apply(string text, MbfOptions result)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (var rawLine in lines)
        {
            var line = StripComment(rawLine).Trim();
            if (line.Length == 0)
                continue;

            line = line.Replace('=', ' ');
            var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                continue;

            var key = parts[0].Trim().ToLowerInvariant();
            var value = parts[1].Trim();

            switch (key)
            {
                case "monsters_remember":
                    if (TryParseBool(value, out var remember))
                        result.MonstersRemember = remember;
                    break;

                case "monster_avoid_hazards":
                    if (TryParseBool(value, out var avoidHazards))
                        result.MonsterAvoidHazards = avoidHazards;
                    break;

                case "monster_friction":
                    if (TryParseBool(value, out var friction))
                        result.MonsterFriction = friction;
                    break;

                case "monster_infighting":
                    if (TryParseBool(value, out var infighting))
                        result.MonsterInfighting = infighting;
                    break;

                case "monster_backing":
                    if (TryParseBool(value, out var backing))
                        result.MonsterBacking = backing;
                    break;

                case "comp_pursuit":
                    if (TryParseBool(value, out var pursuit))
                        result.CompPursuit = pursuit;
                    break;

                case "comp_telefrag":
                    if (TryParseBool(value, out var telefrag))
                        result.CompTelefrag = telefrag;
                    break;

                case "comp_dropoff":
                    if (TryParseBool(value, out var dropoff))
                        result.CompDropoff = dropoff;
                    break;

                case "comp_vile":
                    if (TryParseBool(value, out var vile))
                        result.CompVile = vile;
                    break;

                case "comp_blazing":
                    if (TryParseBool(value, out var blazing))
                        result.CompBlazing = blazing;
                    break;

                case "comp_doorlight":
                    if (TryParseBool(value, out var doorLight))
                        result.CompDoorLight = doorLight;
                    break;

                case "comp_model":
                    if (TryParseBool(value, out var model))
                        result.CompModel = model;
                    break;

                case "comp_god":
                    if (TryParseBool(value, out var god))
                        result.CompGod = god;
                    break;

                case "comp_falloff":
                    if (TryParseBool(value, out var falloff))
                        result.CompFalloff = falloff;
                    break;

                case "comp_floors":
                    if (TryParseBool(value, out var floors))
                        result.CompFloors = floors;
                    break;

                case "comp_skymap":
                    if (TryParseBool(value, out var skyMap))
                        result.CompSkyMap = skyMap;
                    break;

                case "comp_zombie":
                    if (TryParseBool(value, out var zombie))
                        result.CompZombie = zombie;
                    break;

                case "comp_stairs":
                    if (TryParseBool(value, out var stairs))
                        result.CompStairs = stairs;
                    break;

                case "comp_infcheat":
                    if (TryParseBool(value, out var infCheat))
                        result.CompInfCheat = infCheat;
                    break;

                case "comp_zerotags":
                    if (TryParseBool(value, out var zeroTags))
                        result.CompZeroTags = zeroTags;
                    break;

                case "comp_moveblock":
                    if (TryParseBool(value, out var moveBlock))
                        result.CompMoveBlock = moveBlock;
                    break;

                case "comp_sound":
                    if (TryParseBool(value, out var sound))
                        result.CompSound = sound;
                    break;

                case "comp_666":
                    if (TryParseBool(value, out var boss666))
                        result.Comp666 = boss666;
                    break;

                case "comp_maskedanim":
                    if (TryParseBool(value, out var maskedAnim))
                        result.CompMaskedAnim = maskedAnim;
                    break;

                case "comp_ouchface":
                    if (TryParseBool(value, out var ouchFace))
                        result.CompOuchFace = ouchFace;
                    break;

                case "comp_maxhealth":
                    if (TryParseBool(value, out var maxHealth))
                        result.CompMaxHealth = maxHealth;
                    break;

                case "comp_translucency":
                    if (TryParseBool(value, out var translucency))
                        result.CompTranslucency = translucency;
                    break;

                case "comp_ledgeblock":
                    if (TryParseBool(value, out var ledgeBlock))
                        result.CompLedgeBlock = ledgeBlock;
                    break;

                case "comp_friendlyspawn":
                    if (TryParseBool(value, out var friendlySpawn))
                        result.CompFriendlySpawn = friendlySpawn;
                    break;

                case "comp_voodooscroller":
                    if (TryParseBool(value, out var voodooScroller))
                        result.CompVoodooScroller = voodooScroller;
                    break;

                case "comp_reservedlineflag":
                    if (TryParseBool(value, out var reservedLineFlag))
                        result.CompReservedLineFlag = reservedLineFlag;
                    break;

                case "comp_staylift":
                    if (TryParseBool(value, out var stayLift))
                        result.CompStayLift = stayLift;
                    break;

                case "comp_doorstuck":
                    if (TryParseBool(value, out var doorStuck))
                        result.CompDoorStuck = doorStuck;
                    break;

                case "comp_pain":
                    if (TryParseBool(value, out var pain))
                        result.CompPain = pain;
                    break;

                case "comp_skull":
                    if (TryParseBool(value, out var skull))
                        result.CompSkull = skull;
                    break;

                case "comp_respawn":
                    if (TryParseBool(value, out var respawn))
                        result.CompRespawn = respawn;
                    break;

                case "comp_soul":
                    if (TryParseBool(value, out var soul))
                        result.CompSoul = soul;
                    break;

                case "help_friends":
                    if (TryParseBool(value, out var helpFriends))
                        result.HelpFriends = helpFriends;
                    break;

                case "dog_jumping":
                    if (TryParseBool(value, out var dogJumping))
                        result.DogJumping = dogJumping;
                    break;

                case "monkeys":
                    if (TryParseBool(value, out var monkeys))
                        result.Monkeys = monkeys;
                    break;

                case "player_helpers":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var playerHelpers))
                        result.PlayerHelpers = playerHelpers;
                    break;

                case "friend_distance":
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var distance))
                        result.FriendDistance = distance;
                    break;
            }
        }
    }

    private static string StripComment(string line)
    {
        if (line == null)
            return string.Empty;

        var cut = line.Length;

        var hash = line.IndexOf('#');
        if (hash >= 0)
            cut = Math.Min(cut, hash);

        var semicolon = line.IndexOf(';');
        if (semicolon >= 0)
            cut = Math.Min(cut, semicolon);

        var slash = line.IndexOf("//", StringComparison.Ordinal);
        if (slash >= 0)
            cut = Math.Min(cut, slash);

        return line.Substring(0, cut);
    }

    private static bool TryParseBool(string value, out bool result)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "1":
            case "yes":
            case "true":
            case "on":
                result = true;
                return true;

            case "0":
            case "no":
            case "false":
            case "off":
                result = false;
                return true;

            default:
                result = false;
                return false;
        }
    }
}
