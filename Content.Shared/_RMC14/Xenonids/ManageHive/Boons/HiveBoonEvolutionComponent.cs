using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HiveBoonSystem))]
public sealed partial class HiveBoonEvolutionComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Multiplier = 2;

    [DataField, AutoNetworkedField]
    public bool BypassOvipositor;

    [DataField, AutoNetworkedField]
    public bool FrozenEarlyEvolutionBoost;

    [DataField, AutoNetworkedField]
    public FixedPoint2 FrozenBonus;

    [DataField, AutoNetworkedField]
    public bool HasFrozenOverride;

    [DataField, AutoNetworkedField]
    public FixedPoint2 FrozenOverride;
}
