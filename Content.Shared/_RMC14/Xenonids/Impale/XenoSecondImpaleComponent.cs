using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Impale;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoImpaleSystem))]
public sealed partial class XenoSecondImpaleComponent : Component
{
    //Time to Hit, Damage, and Origin
    [DataField]
    public List<(TimeSpan ImpaleAt, DamageSpecifier Damage, EntityUid Origin)> ExtraImpales = new();

    [DataField]
    public EntProtoId Animation = "RMCEffectTailHit";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_tail_attack.ogg");

    [DataField]
    public int AP = 10;
}
