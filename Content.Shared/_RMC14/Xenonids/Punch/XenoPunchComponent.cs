using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Punch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoPunchSystem))]
public sealed partial class XenoPunchComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField, AutoNetworkedField]
    public float Range = 5;

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "CMEffectPunch";

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_claw_block.ogg");
}
