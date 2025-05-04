using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Impale;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoImpaleSystem))]
public sealed partial class XenoSecondImpaleComponent : Component
{
    [DataField]
    public TimeSpan ImpaleAt;

    [DataField]
    public DamageSpecifier Damage;

    [DataField]
    public EntProtoId Animation = "RMCEffectTailHit";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_tail_attack.ogg");

    [DataField]
    public int AP = 10;

    [DataField]
    public EntityUid Origin;
}
