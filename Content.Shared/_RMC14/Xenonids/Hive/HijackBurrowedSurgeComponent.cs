using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HijackBurrowedSurgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan NextSurgeAt;

    [DataField, AutoNetworkedField]
    public int PooledLarva;

    [DataField, AutoNetworkedField]
    public TimeSpan SurgeEvery = TimeSpan.FromSeconds(90);

    [DataField, AutoNetworkedField]
    public TimeSpan ReduceSurgeBy = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan MinSurgeTime = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public TimeSpan ResetSurgeTime = TimeSpan.FromSeconds(90);
}
