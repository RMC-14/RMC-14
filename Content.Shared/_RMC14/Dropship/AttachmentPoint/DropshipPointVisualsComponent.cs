using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Dropship.AttachmentPoint;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDropshipSystem), typeof(DropshipUtilitySystem))]
public sealed partial class DropshipPointVisualsComponent : Component
{
    [DataField]
    public SpriteSpecifier.Rsi? AttachedSprite;
}

[Serializable, NetSerializable]
public enum DropshipPointVisualsLayers
{
    AttachmentBase,
    AttachedUtility,
}
