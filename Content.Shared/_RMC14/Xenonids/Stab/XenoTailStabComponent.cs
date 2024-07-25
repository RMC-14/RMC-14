using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Stab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoTailStabSystem))]
public sealed partial class XenoTailStabComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId TailAnimationId = "WeaponArcThrust";

    [DataField, AutoNetworkedField]
    public FixedPoint2 TailRange = 3;

    [DataField]
    public DamageSpecifier TailDamage = new();

    [DataField, AutoNetworkedField]
    public SoundSpecifier TailHitSound = new SoundCollectionSpecifier("XenoTailSwipe");

    [DataField, AutoNetworkedField]
    public float ChargeTime;

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2>? Inject;
}
