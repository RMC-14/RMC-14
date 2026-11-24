using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Fax;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpecialFaxComponent : Component
{
    [DataField, AutoNetworkedField]
    public string FaxId = string.Empty;
}
