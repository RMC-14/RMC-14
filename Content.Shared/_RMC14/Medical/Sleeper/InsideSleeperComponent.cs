using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Sleeper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSleeperSystem))]
public sealed partial class InsideSleeperComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Sleeper;
}
