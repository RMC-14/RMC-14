using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.MedicalPods;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSleeperSystem))]
public sealed partial class SleeperConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedSleeper;
}
