using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Anchor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(DeployableItemSystem))]
public sealed partial class DeployableItemComponent : Component
{
    [DataField, AutoNetworkedField]
    public DeployableItemPosition Position;
}

[Serializable, NetSerializable]
public enum DeployableItemPosition
{
    None,
    Lower,
    Upper,
}

[Serializable, NetSerializable]
public enum DeployableItemVisuals
{
    Deployed,
}
