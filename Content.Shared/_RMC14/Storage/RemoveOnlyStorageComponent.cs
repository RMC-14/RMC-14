using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMStorageSystem))]
public sealed partial class RemoveOnlyStorageComponent : Component;
