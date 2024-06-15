using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenos.Pheromones;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoPheromonesSystem))]
public sealed partial class XenoPheromonesComponent : Component
{
    [DataField, AutoNetworkedField]
    public int PheromonesPlasmaCost = 35;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PheromonesPlasmaUpkeep = 2.5;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPheromonesPlasmaUse;

    [DataField, AutoNetworkedField]
    public int PheromonesRange = 8;

    [DataField, AutoNetworkedField]
    public FixedPoint2 PheromonesMultiplier = 1;
}
