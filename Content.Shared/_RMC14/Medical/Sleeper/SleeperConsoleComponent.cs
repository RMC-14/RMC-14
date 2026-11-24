using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Sleeper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSleeperSystem))]
public sealed partial class SleeperConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedSleeper;

    [DataField]
    public TimeSpan UpdateAt;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool IsUpgraded;
}
