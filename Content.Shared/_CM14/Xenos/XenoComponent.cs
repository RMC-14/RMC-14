using Content.Shared.Access;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._CM14.Xenos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoSystem))]
public sealed partial class XenoComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntProtoId> ActionIds = new();

    [DataField]
    public Dictionary<EntProtoId, EntityUid> Actions = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 HealthRegenOnWeeds = 1.25;

    [DataField, AutoNetworkedField]
    public TimeSpan RegenCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan NextRegenTime;

    [DataField, AutoNetworkedField]
    public EntityUid Hive;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessLevels = new() { "CMAccessXeno" };

    [DataField, AutoNetworkedField]
    public bool OnWeeds;

    [DataField, AutoNetworkedField]
    public int Tier;
}
