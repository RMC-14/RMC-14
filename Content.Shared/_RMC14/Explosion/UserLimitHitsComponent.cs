using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Explosion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMClusterGrenadeSystem))]
public sealed partial class UserLimitHitsComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<Hit> HitBy = new();

    [DataField, AutoNetworkedField]
    public TimeSpan Expire = TimeSpan.FromSeconds(5);
}

[DataRecord]
[Serializable, NetSerializable]
public partial record struct Hit(
    int Id,
    [field: DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    TimeSpan ExpireAt
);
