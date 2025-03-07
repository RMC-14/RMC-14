using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Food;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCOpenChangeFillLevelsComponent : Component
{
    [DataField, AutoNetworkedField]
    public int FillLevelsOpen;

    [DataField, AutoNetworkedField]
    public int FillLevelsClosed;
}
