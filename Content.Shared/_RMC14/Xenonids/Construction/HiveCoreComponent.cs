using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoConstructionSystem), typeof(SharedXenoHiveCoreSystem))]
public sealed partial class HiveCoreComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Spawns = "XenoHiveWeeds";

    [DataField]
    public int MinimumLesserDrones = 2;

    [DataField]
    public int XenosPerLesserDrone = 3;

    [DataField]
    public int CurrentLesserDrones;

    [DataField]
    public int MaxLesserDrones;

    [DataField]
    public List<EntityUid> LiveLesserDrones = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextLesserDroneAt;

    [DataField]
    public TimeSpan NextLesserDroneOviCooldown = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan NextLesserDroneCooldown = TimeSpan.FromSeconds(125);

    [DataField]
    public FixedPoint2 Heal = 100;

    [DataField]
    public TimeSpan HealEvery = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan HealAt;
}
