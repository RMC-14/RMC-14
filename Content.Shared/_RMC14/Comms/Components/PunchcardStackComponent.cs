using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Comms;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PunchcardStackComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Count = 5;
}
