using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Shields;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoShieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public XenoShieldSystem.ShieldType Shield = XenoShieldSystem.ShieldType.Generic;

    [DataField, AutoNetworkedField]
    public FixedPoint2 ShieldAmount = 0;

    [DataField, AutoNetworkedField]
    public TimeSpan? Duration;

    [DataField, AutoNetworkedField]
    public TimeSpan ShieldDecayAt;

    [DataField, AutoNetworkedField]
    public double DecayPerSecond;

    [DataField, AutoNetworkedField]
    public bool Active = false;

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
