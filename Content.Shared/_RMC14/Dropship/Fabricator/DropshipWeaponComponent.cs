using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.Fabricator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DropshipFabricatorSystem))]
public sealed partial class DropshipWeaponComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Abbreviation = string.Empty;
}
