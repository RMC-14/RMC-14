using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Admin;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCAdminSystem))]
public sealed partial class RMCAdminSpawnedComponent : Component;
