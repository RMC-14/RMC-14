using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Shields;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CrusherShieldComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan ExplosionOffAt;

    [DataField, AutoNetworkedField]
    public TimeSpan ShieldOffAt;

    [DataField, AutoNetworkedField]
    public bool ExplosionResistApplying = false;

    [DataField, AutoNetworkedField]
    public int ExplosionResistance = 1000;

    [DataField, AutoNetworkedField]
    public int DamageReduction = 10;

    [DataField, AutoNetworkedField]
    public TimeSpan ExplosionResistanceDuration = TimeSpan.FromSeconds(2.5);

    [DataField, AutoNetworkedField]
    public TimeSpan ShieldDuration = TimeSpan.FromSeconds(7);

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = FixedPoint2.New(50);

    [DataField, AutoNetworkedField]
    public FixedPoint2 Amount = FixedPoint2.New(200);

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "RMCEffectEmpowerBrown";
}
