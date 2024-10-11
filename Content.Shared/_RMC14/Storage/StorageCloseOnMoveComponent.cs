using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StorageCloseOnMoveComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool SkipInHand = false;
}
