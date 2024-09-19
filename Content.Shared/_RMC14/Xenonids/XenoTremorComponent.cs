using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Tremor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoTremorSystem))]
public sealed partial class XenoTremorComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 100;

    [DataField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public TimeSpan ParalyzeTime = TimeSpan.FromSeconds(0.4);


    [DataField, AutoNetworkedField]
    public float Range = 3;

    [DataField, AutoNetworkedField]
    public EntProtoId SelfEffect = "CMEffectSelfStomp";

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "CMEffectStomp";

    // TODO RMC14 bang.ogg
    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_footstep_charge1.ogg");
}
