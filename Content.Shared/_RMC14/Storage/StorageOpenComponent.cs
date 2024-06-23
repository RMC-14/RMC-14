using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StorageOpenComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntityUid, NetCoordinates> OpenedAt = new();
}
