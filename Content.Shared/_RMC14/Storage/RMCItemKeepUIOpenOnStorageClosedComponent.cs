using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCStorageSystem))]
public sealed partial class RMCItemKeepUIOpenOnStorageClosedComponent : Component;
