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

    /// <summary>
    /// If true, this component will deal with showing the AttachedSprite as a simple sprite.
    /// Otherwise, it is the duty of the specific Utility System Component to render the AttachedSprite.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool WillRender = true;
}

[Serializable, NetSerializable]
public enum DropshipUtilityPointLayers
{
    AttachementBase,
    AttachedUtility
}
