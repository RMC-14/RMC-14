using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.AttachmentPoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class DropshipUtilityPointComponent : Component
{
    [DataField, AutoNetworkedField]
    public string UtilitySlotId = "rmc_dropship_utility_point_container_slot";

    [DataField, AutoNetworkedField]
    public string DirOffset;
}

[Serializable, NetSerializable]
public enum DropshipUtilityPointLayers
{
    Layer,
}
