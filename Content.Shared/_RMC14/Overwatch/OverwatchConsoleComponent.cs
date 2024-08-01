using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Overwatch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedOverwatchConsoleSystem))]
public sealed partial class OverwatchConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetEntity? Squad;

    [DataField, AutoNetworkedField]
    public string? Operator;
}
