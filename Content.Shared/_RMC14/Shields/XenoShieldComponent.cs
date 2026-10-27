using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Shields;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoShieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<XenoShield> Shields = new();

    [DataField, AutoNetworkedField]
    public SoundSpecifier ShieldBreak = new SoundPathSpecifier("/Audio/_RMC14/Bullets/shield_break_c1.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier ShieldImpact = new SoundCollectionSpecifier("RMCShieldImpact", AudioParams.Default.WithVolume(-4));
}

[Serializable, NetSerializable]
public enum RMCShieldVisuals
{
    Base,
    Current,
    Max,
    Active,
    Prefix
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class XenoShield
{
    [DataField]
    public XenoShieldSystem.ShieldType Shield;

    [DataField]
    public FixedPoint2 ShieldAmount;

    [DataField]
    public FixedPoint2 ShieldInitialAmount;

    [DataField]
    public TimeSpan? ShieldDecayAt;

    [DataField]
    public double DecayPerSecond;

    [DataField]
    public string? Visual;

    public XenoShield (XenoShieldSystem.ShieldType shield, FixedPoint2 shieldAmount, double decay, string? visual)
    {
        Shield = shield;
        ShieldAmount = shieldAmount;
        DecayPerSecond = decay;
        Visual = visual;
        ShieldInitialAmount = shieldAmount;
    }
}
