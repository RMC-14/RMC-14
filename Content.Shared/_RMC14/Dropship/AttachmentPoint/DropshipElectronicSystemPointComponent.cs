using Content.Shared._RMC14.Dropship.ElectronicSystem;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.AttachmentPoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem), typeof(SharedDropshipElectronicSystemSystem))]
public sealed partial class DropshipElectronicSystemPointComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_dropship_electronic_system_point_container";
}
