using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Stab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoTailStabSystem))]
public sealed partial class XenoTailStabComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId TailAnimationId = "WeaponArcThrust";

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TailRange = 3;

    [DataField]
    public DamageSpecifier TailDamage = new();

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TailHitSound = new SoundCollectionSpecifier("XenoTailSwipe");
}
