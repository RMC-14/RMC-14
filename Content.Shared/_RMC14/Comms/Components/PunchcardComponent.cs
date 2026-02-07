using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Comms;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PunchcardComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Data = "";
}
