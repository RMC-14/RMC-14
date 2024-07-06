using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access([typeof(SharedDropshipSystem)])]
public abstract partial class SharedDropshipNavigationComputerComponent : Component
{
    /// <summary>
    /// Key is the name of a group of lockable doors,
    /// Value is a list of the EntityUids of those lockable doors
    /// </summary>
    [AutoNetworkedField]
    public Dictionary<string, List<EntityUid>> LockableDoors = new();

    /// <summary>
    /// Key is the name of a group of lockable doors,
    /// Value is the whether the group is locked or not.
    ///
    /// This is mainly to keep consistency, with respect to the door group lock states,
    /// when opening and closing the navigation UI
    /// </summary>
    [AutoNetworkedField]
    public Dictionary<string, bool> DoorGroupLockStates = new();
}
