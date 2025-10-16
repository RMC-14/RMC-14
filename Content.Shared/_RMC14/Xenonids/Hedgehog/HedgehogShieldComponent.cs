using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hedgehog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HedgehogShieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ShieldAmount;

    [DataField, AutoNetworkedField]
    public TimeSpan EndAt;

    [DataField, AutoNetworkedField]
    public double DecayPerSecond;

    [DataField, AutoNetworkedField]
    public bool Active = true;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ShieldBreak = new SoundPathSpecifier("/Audio/_RMC14/Bullets/shield_break_c1.ogg");
}