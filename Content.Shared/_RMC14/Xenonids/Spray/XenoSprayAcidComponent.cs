using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Spray;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSprayAcidSystem))]
public sealed partial class XenoSprayAcidComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Acid = "XenoAcidSprayWeak";

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 40;

    [DataField, AutoNetworkedField]
    public TimeSpan DoAfter;

    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(0.2);

    [DataField, AutoNetworkedField]
    public DamageSpecifier BarricadeDamage;

    [DataField, AutoNetworkedField]
    public TimeSpan BarricadeDuration = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Effects/refill.ogg");

    [DataField, AutoNetworkedField]
    public float Range = 6;
}
