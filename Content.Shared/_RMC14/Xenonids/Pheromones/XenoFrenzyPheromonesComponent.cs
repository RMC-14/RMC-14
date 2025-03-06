using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.Xenonids.Pheromones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoFrenzyPheromonesComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_pheromones_hud.rsi"), "frenzy");

    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier;

    [DataField, AutoNetworkedField]
    public float AttackDamageAddPerMult = 2;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MovementSpeedModifier = 0.06;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PullMovementSpeedModifier = 0.03;

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> DamageGroup = "Brute";
}
