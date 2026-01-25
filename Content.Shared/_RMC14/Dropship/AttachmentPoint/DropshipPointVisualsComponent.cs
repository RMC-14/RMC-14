using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.AttachmentPoint;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDropshipSystem), typeof(DropshipUtilitySystem))]
public sealed partial class DropshipPointVisualsComponent : Component;

[Serializable, NetSerializable]
public enum DropshipPointVisualsLayers
{
    AttachmentBase,
    AttachedUtility,
}
