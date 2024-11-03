using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dialog;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DialogSystem))]
public sealed partial class DialogComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> Options = new();
}
