using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoFragileEggComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan? ExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan? ShortExpireAt;

    [DataField, AutoNetworkedField]
    public TimeSpan? CheckSustainAt;

    [DataField, AutoNetworkedField]
    public EntityUid? SustainedBy;

    [DataField, AutoNetworkedField]
    public float SustainRange = 14;

    [DataField, AutoNetworkedField]
    public TimeSpan? BurstAt;

    [DataField]
    public TimeSpan BurstDelay = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan SustainDuration = TimeSpan.FromMinutes(1);

    [DataField]
    public TimeSpan SustainCheckEvery = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public bool InRange = true;
}
