using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Blood_Drain;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoBloodDrainComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodDrain = 28;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BaseEvoPointsGranted = 2.5;

    [DataField, AutoNetworkedField]
    public float BonusEvoMult = 2.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan DrainTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public DamageSpecifier BiteDamage = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 Healing = 50;

    [DataField, AutoNetworkedField]
    public EntProtoId BiteEffect = "RMCEffectBite";

    [DataField, AutoNetworkedField]
    public FixedPoint2 PlasmaCost = 50;

    [DataField, AutoNetworkedField]
    public SoundSpecifier DrainSound = new SoundPathSpecifier("/Audio/Effects/Fluids/blood1.ogg");

    [DataField, AutoNetworkedField]
    public EntProtoId HealEffect = "RMCEffectHealHeadbite";
}
