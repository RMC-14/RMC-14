using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

/// <summary>
/// Makes a locked entity storage (e.g. locker/cabinet) block all manual interaction until DropshipHijackStartEvent fires.
/// The storage automatically unlocks and opens on hijack.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCStorageSystem))]
public sealed partial class RMCLockerOpenOnHijackComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool DidHijackStart;
}
