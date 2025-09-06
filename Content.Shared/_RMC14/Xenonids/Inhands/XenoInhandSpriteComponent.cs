using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Inhands;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoInhandSpriteComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? StateName;
}

[Serializable, NetSerializable]
public enum XenoInhandVisualLayers
{
    Left,
    Right,
}

[Serializable, NetSerializable]
public enum XenoInhandVisuals
{
    LeftHand,
    RightHand,
}
