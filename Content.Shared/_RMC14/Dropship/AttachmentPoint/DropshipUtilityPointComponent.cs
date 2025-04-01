using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.AttachmentPoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem), typeof(DropshipUtilitySystem))]
public sealed partial class DropshipUtilityPointComponent : Component
{
    [DataField, AutoNetworkedField]
    public string UtilitySlotId = "rmc_dropship_utility_point_container_slot";
}
