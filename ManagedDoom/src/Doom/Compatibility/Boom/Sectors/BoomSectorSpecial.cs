namespace ManagedDoom.Compatibility.Boom.Sectors;

public enum BoomSectorDamage
{
    None = 0,
    Five = 1,
    Ten = 2,
    Twenty = 3
}

public readonly struct BoomSectorSpecial
{
    internal BoomSectorSpecial(
        int rawValue,
        int lightingSpecial,
        BoomSectorDamage damage,
        bool isSecret,
        bool frictionEnabled,
        bool pusherEnabled)
    {
        RawValue = rawValue;
        LightingSpecial = lightingSpecial;
        Damage = damage;
        IsSecret = isSecret;
        FrictionEnabled = frictionEnabled;
        PusherEnabled = pusherEnabled;
    }

    public int RawValue { get; }

    public int LightingSpecial { get; }

    public BoomSectorDamage Damage { get; }

    public int DamageAmount => Damage switch
    {
        BoomSectorDamage.Five => 5,
        BoomSectorDamage.Ten => 10,
        BoomSectorDamage.Twenty => 20,
        _ => 0
    };

    public bool IsSecret { get; }

    public bool FrictionEnabled { get; }

    public bool PusherEnabled { get; }
}
