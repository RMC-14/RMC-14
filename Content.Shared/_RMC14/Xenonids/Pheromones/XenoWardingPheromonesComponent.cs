using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._RMC14.Xenonids.Pheromones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoWardingPheromonesComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier Icon = new Rsi(new ResPath("/Textures/_RMC14/Interface/xeno_pheromones_hud.rsi"), "warding");

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> CritDamageGroup = "Brute";

    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier;
}
