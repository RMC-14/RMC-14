using Robust.Shared.GameStates;

namespace Content.Shared.Containers;

/// <summary>
/// This is used to delete a storage container when empty
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(DeleteOnContainerEmptySystem))]
public sealed partial class DeleteOnContainerEmptyComponent : Component
{
    /// <summary>
    /// ID of the container which will have itself deleted
    /// </summary>
    [DataField]
    public string ContainerId = "storagebase";
}
