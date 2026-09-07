namespace ManagedDoom.Compatibility.Boom.Lines;

public static class BoomCrusherTranslator
{
    public const int MinSpecial = 0x2F80;
    public const int MaxSpecial = 0x2FFF;

    private const int SilentMask = 0x0040;
    private const int MonsterMask = 0x0020;

    public static bool IsCrusherSpecial(LineSpecial special)
    {
        var value = (int)special;
        return value >= MinSpecial && value <= MaxSpecial;
    }

    public static bool TryTranslate(LineSpecial special, out BoomCrusherSpecial specification)
    {
        if (!IsCrusherSpecial(special))
        {
            specification = default;
            return false;
        }

        specification = Translate(special);
        return true;
    }

    public static BoomCrusherSpecial Translate(LineSpecial special)
    {
        var value = (int)special;
        var allowsMonsters = (value & MonsterMask) != 0;
        var silent = (value & SilentMask) != 0;
        var common = BoomGeneralizedSpecial.DecodeCommon(special, allowsMonsters);

        return new BoomCrusherSpecial(common, silent);
    }
}
